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
        
        /// Set to json to enable grammar-constrained JSON output.
        /// Ignored when Tools is non-empty (tool-call mode takes precedence).
        public string ResponseFormat { get; set; }
        
        /// Optional GBNF grammar string for llama.cpp constrained sampling.
        public string GbnfGrammar { get; set; }
        
        /// Tool schemas to send to Ollama. When non-empty, the adapter enters the
        /// tool-call loop: it sends these definitions, handles tool_calls responses,
        /// invokes ToolHandler, appends results, and re-sends until the model
        /// returns a plain text response.
        public List<ToolDefinition> Tools { get; set; }
        
        /// Executes tool calls that the LLM requests during the tool-call loop.
        /// Required when Tools is non-empty; ignored otherwise.
        public IToolHandler ToolHandler { get; set; }
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
