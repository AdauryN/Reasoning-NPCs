using System;
using System.Threading;
using System.Threading.Tasks;
using NPC_AI.Brain;
using NPC_AI.Config;
using NPC_AI.LLM;
using NPC_AI.PlayerAnalysis;
using UnityEngine;

namespace NPC_AI.Core
{
    /// MonoBehaviour entry point for one NPC. Owns the brain and drives the decision loop.
    /// Attach this to your NPC GameObject. Wire up the config assets in the Inspector.
    public class NPCController : MonoBehaviour
    {
        [SerializeField] private LLMConfig llmConfig;
        [SerializeField] private NPCConfig npcConfig;
        [SerializeField] private PersonalityProfile personalityProfile;

        [Header("Runtime References (auto-assigned if not set)")]
        [SerializeField] private PlayerBehaviorTracker behaviorTracker;

        private INPCBrain _brain;
        private ILLMService _llm;
        private float _decisionTimer;
        private CancellationTokenSource _lifetimeCts;

        public string NpcId { get; private set; }
        public bool IsInitialized { get; private set; }
        public ActionCommand LastCommand { get; private set; }

        private void Awake()
        {
            NpcId = $"{gameObject.name}_{GetInstanceID()}";
            _lifetimeCts = new CancellationTokenSource();
        }

        private async void Start()
        {
            if (behaviorTracker == null)
                behaviorTracker = FindObjectOfType<PlayerBehaviorTracker>();

            _llm = LLMServiceFactory.GetShared(llmConfig);
            await _llm.InitializeAsync(_lifetimeCts.Token);

            _brain = new NPCBrain(_llm, npcConfig, personalityProfile, behaviorTracker);
            await _brain.InitializeAsync(_lifetimeCts.Token);

            IsInitialized = true;
            Debug.Log($"[NPCController] {NpcId} ready.");
        }

        private void Update()
        {
            if (!IsInitialized || !_brain.IsIdle) return;

            _decisionTimer += Time.deltaTime;
            if (_decisionTimer >= npcConfig.DecisionIntervalSeconds)
            {
                _decisionTimer = 0f;
                _ = RequestDecisionAsync();
            }
        }

        private async Task RequestDecisionAsync()
        {
            var worldView = BuildWorldView();

            using var timeoutCts = new CancellationTokenSource(
                TimeSpan.FromSeconds(npcConfig.InferenceTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _lifetimeCts.Token, timeoutCts.Token);

            try
            {
                var command = await _brain.DecideAsync(worldView, linkedCts.Token);
                LastCommand = command;
                NPCAIEvents.RaiseDecisionMade(this, command);
                ExecuteCommand(command);
            }
            catch (OperationCanceledException)
            {
                // Timeout or scene unload — silently skip this tick.
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NPCController] {NpcId} decision error: {ex.Message}");
            }
        }
        
        /// Override this in a subclass to translate ActionCommand into actual Unity actions
        /// (animations, NavMesh movement, attack logic, etc).
        protected virtual void ExecuteCommand(ActionCommand command)
        {
            Debug.Log($"[NPCController] {NpcId} executing: {command.ActionType} → {command.Target} (reason: {command.Reasoning})");
        }
        
        /// Populate a world-view snapshot from the current game state.
        /// Override in a subclass to pull real values from your game systems.
        protected virtual NPCWorldView BuildWorldView()
        {
            return new NPCWorldView
            {
                NpcId = NpcId,
                NpcHealthPct = 1f,
                PlayerHealthPct = 1f,
                DistanceToPlayer = 10f,
                PlayerFacingNpc = false,
                AlliesNearby = 0,
                EnemiesNearby = 1,
                LastPlayerAction = "UNKNOWN",
                LastPlayerActionSecondsAgo = 0f,
                LastNpcAction = LastCommand?.ActionType ?? "NONE",
                LastNpcActionSecondsAgo = _decisionTimer,
                NpcPosition = transform.position,
                PlayerPosition = transform.position
            };
        }

        private void OnDestroy()
        {
            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
        }
    }
}
