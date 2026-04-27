using NPC_AI.Core;
using UnityEngine;

namespace NPC_AI.PlayerAnalysis
{
    /// MonoBehaviour attached to the player GameObject.
    /// Records player action events and maintains a cached behavioral summary.
    /// Call RecordEvent() from your player controller whenever the player performs a meaningful action. 
    /// NPCBrain calls GetSummary() each decision tick.
    public class PlayerBehaviorTracker : MonoBehaviour
    {
        [Tooltip("How often (seconds) the behavior summary is recomputed from the event buffer.")]
        [SerializeField] private float summaryRefreshInterval = 2f;

        private readonly CircularBuffer<BehaviorEvent> _events = new CircularBuffer<BehaviorEvent>(100);
        private readonly PatternRecognizer _recognizer = new PatternRecognizer();
        private PlayerBehaviorSummary _cachedSummary = new PlayerBehaviorSummary();
        private float _summaryAge;

        public void RecordEvent(BehaviorEvent evt)
        {
            _events.Push(evt);
            _summaryAge = summaryRefreshInterval; // mark dirty immediately
        }
        
        /// Returns the current behavioral summary. Always non-blocking — returns the cached
        /// value until the refresh interval elapses, then recomputes.
        public PlayerBehaviorSummary GetSummary()
        {
            if (_summaryAge >= summaryRefreshInterval)
            {
                _cachedSummary = _recognizer.Analyze(_events.ToArray());
                _summaryAge = 0f;
                NPCAIEvents.RaisePlayerProfileUpdated(_cachedSummary);
            }
            return _cachedSummary;
        }

        private void Update()
        {
            _summaryAge += Time.deltaTime;
        }

        // ── Convenience helpers so game code doesn't need to construct BehaviorEvent manually ──

        public void OnPlayerAttack(bool hit, Vector3 playerPos, Vector3 npcPos, float playerHealthPct)
        {
            RecordEvent(new BehaviorEvent
            {
                Type = BehaviorEventType.DirectAttack,
                Timestamp = Time.time,
                PlayerPosition = playerPos,
                NpcPosition = npcPos,
                PlayerHealthAtTime = playerHealthPct,
                WasSuccessful = hit
            });
        }

        public void OnPlayerDodge(bool leftSide, Vector3 playerPos)
        {
            RecordEvent(new BehaviorEvent
            {
                Type = leftSide ? BehaviorEventType.DodgeLeft : BehaviorEventType.DodgeRight,
                Timestamp = Time.time,
                PlayerPosition = playerPos,
                PlayerHealthAtTime = 1f,
                WasSuccessful = true
            });
        }

        public void OnPlayerHeal(float playerHealthPct, Vector3 playerPos)
        {
            RecordEvent(new BehaviorEvent
            {
                Type = BehaviorEventType.UseHealItem,
                Timestamp = Time.time,
                PlayerPosition = playerPos,
                PlayerHealthAtTime = playerHealthPct,
                WasSuccessful = true
            });
        }

        public void OnPlayerFlank(Vector3 playerPos, Vector3 npcPos)
        {
            RecordEvent(new BehaviorEvent
            {
                Type = BehaviorEventType.Flank,
                Timestamp = Time.time,
                PlayerPosition = playerPos,
                NpcPosition = npcPos,
                PlayerHealthAtTime = 1f,
                WasSuccessful = true
            });
        }

        public void OnPlayerRangedAttack(bool hit, Vector3 playerPos, float playerHealthPct)
        {
            RecordEvent(new BehaviorEvent
            {
                Type = BehaviorEventType.UseRangedWeapon,
                Timestamp = Time.time,
                PlayerPosition = playerPos,
                PlayerHealthAtTime = playerHealthPct,
                WasSuccessful = hit
            });
        }
    }
}
