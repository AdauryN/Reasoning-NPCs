using System;
using NPC_AI.Brain;
using NPC_AI.LLM;
using NPC_AI.PlayerAnalysis;

namespace NPC_AI.Core
{
    /// Decoupled event bus for the NPC AI system.
    /// Subscribe here to hook in debug displays, logging, or analytics
    /// without coupling directly to NPCBrain or NPCController.
    public static class NPCAIEvents
    {
        ///Fired after NPCBrain resolves a decision. (LLM or fallback)
        public static event Action<NPCController, ActionCommand> OnDecisionMade;

        ///Fired when the player behavior summary is recomputed.
        public static event Action<PlayerBehaviorSummary> OnPlayerProfileUpdated;

        ///Fired after each LLM inference completes.
        public static event Action<string, float> OnInferenceCompleted;  // npcId, latencyMs

        ///Fired with the raw LLM exchange for debugging.
        public static event Action<LLMRequest, string> OnLLMExchangeLogged;

        internal static void RaiseDecisionMade(NPCController npc, ActionCommand cmd) =>
            OnDecisionMade?.Invoke(npc, cmd);

        internal static void RaisePlayerProfileUpdated(PlayerBehaviorSummary summary) =>
            OnPlayerProfileUpdated?.Invoke(summary);

        internal static void RaiseInferenceCompleted(string npcId, float latencyMs) =>
            OnInferenceCompleted?.Invoke(npcId, latencyMs);

        internal static void RaiseLLMExchangeLogged(LLMRequest request, string response) =>
            OnLLMExchangeLogged?.Invoke(request, response);
    }
}
