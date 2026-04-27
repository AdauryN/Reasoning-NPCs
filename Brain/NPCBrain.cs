using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NPC_AI.Config;
using NPC_AI.Core;
using NPC_AI.LLM;
using NPC_AI.Memory;
using NPC_AI.PlayerAnalysis;
using UnityEngine;

namespace NPC_AI.Brain
{
    /// Orchestrates a single NPCs decision pipeline:
    /// Always falls back to a rule-based policy if the LLM fails or times out.
    /// The LLM is the enhancement layer, not the safety-critical layer.
    public class NPCBrain : INPCBrain
    {
        private static readonly List<ToolDefinition> NpcTools = new List<ToolDefinition>
        {
            new ToolDefinition
            {
                Name = "get_npc_health",
                Description = "Returns the NPC's current health as a value between 0.0 (dead) and 1.0 (full health).",
                ParametersJson = "{\"type\":\"object\",\"properties\":{\"npc_id\":{\"type\":\"string\",\"description\":\"The NPC's identifier\"}},\"required\":[\"npc_id\"]}"
            },
            new ToolDefinition
            {
                Name = "get_player_position",
                Description = "Returns the player's current world position as {x, y, z}.",
                ParametersJson = "{\"type\":\"object\",\"properties\":{}}"
            },
            new ToolDefinition
            {
                Name = "get_nearby_enemies",
                Description = "Returns the number of enemy units within the given radius.",
                ParametersJson = "{\"type\":\"object\",\"properties\":{\"radius\":{\"type\":\"number\",\"description\":\"Search radius in world units\"}},\"required\":[\"radius\"]}"
            },
            new ToolDefinition
            {
                Name = "get_distance_to_player",
                Description = "Returns the distance in world units between the NPC and the player.",
                ParametersJson = "{\"type\":\"object\",\"properties\":{\"npc_id\":{\"type\":\"string\",\"description\":\"The NPC's identifier\"}},\"required\":[\"npc_id\"]}"
            },
        };

        private readonly ILLMService _llm;
        private readonly NPCConfig _config;
        private readonly PersonalityProfile _personality;
        private readonly PlayerBehaviorTracker _behaviorTracker;
        private readonly ActionRegistry _actionRegistry;
        private readonly ContextBuilder _contextBuilder;
        private readonly ActionParser _actionParser;
        private INPCMemoryStore _memoryStore;
        private bool _deciding;

        public bool IsIdle => !_deciding;

        public NPCBrain(
            ILLMService llm,
            NPCConfig config,
            PersonalityProfile personality,
            PlayerBehaviorTracker behaviorTracker)
        {
            _llm = llm;
            _config = config;
            _personality = personality;
            _behaviorTracker = behaviorTracker;
            _actionRegistry = new ActionRegistry();
            _contextBuilder = new ContextBuilder(personality, _actionRegistry);
            _actionParser = new ActionParser(_actionRegistry);
        }

        public Task InitializeAsync(CancellationToken ct = default)
        {
            _memoryStore = new InMemoryStore();
            return Task.CompletedTask;
        }

        public async Task<ActionCommand> DecideAsync(NPCWorldView worldView, CancellationToken ct = default)
        {
            _deciding = true;
            try
            {
                var playerSummary = _behaviorTracker?.GetSummary();
                var memories = _memoryStore?.GetRelevant(worldView, topK: 5) ?? new List<MemoryEntry>();

                var request = _contextBuilder.Build(worldView, playerSummary, memories);
                request.Tools = NpcTools;
                request.ToolHandler = new WorldStateToolHandler(worldView);

                LLMResponse response = await _llm.CompleteAsync(request, ct);

                NPCAIEvents.RaiseInferenceCompleted(worldView.NpcId, response.LatencyMs);
                NPCAIEvents.RaiseLLMExchangeLogged(request, response.Text);

                if (!response.Success)
                {
                    Debug.LogWarning($"[NPCBrain] LLM failed for {worldView.NpcId}: {response.ErrorMessage}");
                    return GetFallback(worldView);
                }

                var parseResult = _actionParser.Parse(response.Text);
                if (!parseResult.Success)
                {
                    Debug.LogWarning($"[NPCBrain] Parse failed for {worldView.NpcId}: {parseResult.ErrorReason}");
                    return GetFallback(worldView);
                }

                // Persist this decision as a short-term memory entry.
                await _memoryStore.AddAsync(new MemoryEntry
                {
                    Content = $"Decided to {parseResult.Command.ActionType} against {parseResult.Command.Target}. Reason: {parseResult.Command.Reasoning}",
                    Importance = parseResult.Command.Urgency == "high" ? 0.8f : 0.4f,
                    Timestamp = DateTime.UtcNow,
                    Tags = new[] { "decision", parseResult.Command.ActionType.ToLower() }
                });

                return parseResult.Command;
            }
            catch (OperationCanceledException)
            {
                return GetFallback(worldView);
            }
            finally
            {
                _deciding = false;
            }
        }
        
        /// Rule-based fallback: flee when low HP, attack otherwise.
        /// This runs even when the LLM is offline — NPCs never freeze.
        private ActionCommand GetFallback(NPCWorldView worldView)
        {
            if (worldView.NpcHealthPct < _config.FallbackFleeHealthThreshold)
                return ActionCommand.Fallback("RETREAT", "HP critical — rule-based fallback");

            return ActionCommand.Fallback("ATTACK_MELEE", "LLM unavailable — rule-based fallback");
        }
    }
}
