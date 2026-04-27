namespace NPC_AI.Brain
{
    /// Parsed output of one LLM decision. Passed to NPCActionExecutor.
    public class ActionCommand
    {
        public string ActionType { get; set; }   // e.g. "ATTACK_MELEE"
        public string Target { get; set; }        // e.g. "player"
        public string Reasoning { get; set; }     // LLM's reasoning text (for debug HUD)
        public string Urgency { get; set; }       // "low" | "medium" | "high"
        public bool IsFromFallback { get; set; }  // true when rule-based, not LLM

        public static ActionCommand Fallback(string actionType, string reason) =>
            new ActionCommand
            {
                ActionType = actionType,
                Target = "player",
                Reasoning = reason,
                Urgency = "medium",
                IsFromFallback = true
            };
    }
}
