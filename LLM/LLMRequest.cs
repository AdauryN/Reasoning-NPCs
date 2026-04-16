using System.Collections.Generic;

namespace NPC_AI.LLM
{
    public class LLMRequest
    {
        public string SystemPrompt { get; set; }
        public List<ChatMessage> History { get; set; } = new List<ChatMessage>();
        public string UserMessage { get; set; }
        public float Temperature { get; set; } = 0.7f;
        public int MaxTokens { get; set; } = 256;
        public string[] StopSequences { get; set; }

        /// <summary>
        /// Set to "json" to enable grammar-constrained JSON output (llama.cpp GBNF).
        /// </summary>
        public string ResponseFormat { get; set; }

        /// <summary>
        /// Optional GBNF grammar string for llama.cpp constrained sampling.
        /// If null and ResponseFormat == "json", the adapter uses a built-in JSON grammar.
        /// </summary>
        public string GbnfGrammar { get; set; }
    }

    public class ChatMessage
    {
        public ChatRole Role { get; set; }
        public string Content { get; set; }
    }

    public enum ChatRole
    {
        System,
        User,
        Assistant
    }
}
