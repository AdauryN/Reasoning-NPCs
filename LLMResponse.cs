namespace NPC_AI.LLM
{
    public class LLMResponse
    {
        public string Text { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public float LatencyMs { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }

        public static LLMResponse Failure(string error) =>
            new LLMResponse { Success = false, ErrorMessage = error };

        public static LLMResponse Ok(string text, float latencyMs, int promptTokens = 0, int completionTokens = 0) =>
            new LLMResponse
            {
                Success = true,
                Text = text,
                LatencyMs = latencyMs,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens
            };
    }
}
