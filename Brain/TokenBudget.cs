namespace NPC_AI.Brain
{
    /// Hard token budget constants for each section of the system prompt.
    /// ContextBuilder enforces these to prevent overflowing small model contexts.
    public static class TokenBudget
    {
        public const int Total            = 2048;
        public const int Identity         = 200;
        public const int WorldState       = 300;
        public const int PlayerProfile    = 150;
        public const int Memory           = 400;
        public const int ActionList       = 250;
        public const int ResponseReserve  = 256;  // must match LLMConfig.MaxResponseTokens

        // Approximate characters-per-token for English prose.
        // Used for lightweight budget checks without invoking the tokenizer.
        private const float CharsPerToken = 3.5f;

        public static int ApproxTokenCount(string text) =>
            text == null ? 0 : (int)(text.Length / CharsPerToken);

        public static string Truncate(string text, int tokenBudget)
        {
            int maxChars = (int)(tokenBudget * CharsPerToken);
            if (text == null || text.Length <= maxChars) return text;
            return text.Substring(0, maxChars) + "…";
        }
    }
}
