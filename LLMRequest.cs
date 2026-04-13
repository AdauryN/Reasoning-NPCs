using System.Collections.Generic;

namespace LLMFramework.Core
{
    /// <summary>
    /// Represents a complete request for any LLM.
    /// </summary>
    public class LLMRequest
    {
        public string SystemPrompt { get; set; }
        public List<Message> Messages { get; set; } = new List<Message>();
        public float Temperature { get; set; } = 0.7f;
        public int MaxTokens { get; set; } = 256;

        /// <summary>
        /// Tools available in this request.
        /// If empty, the LLM responds normally (without tool calling).
        /// If filled, the LLM may choose to call a tool.
        /// </summary>
        public List<MCPTool> Tools { get; set; } = new List<MCPTool>();

        public bool HasTools => Tools != null && Tools.Count > 0;
    }

    /// <summary>
    /// A message in a multi-turn conversation.
    /// Sprint 3: fields for tool calls and results added.
    /// Sprint 3: added fields for tool calls and results. ///
    /// Possible roles:
    /// "user" → player's message
    /// "assistant" → LLM's response (text or tool call)
    /// "tool" → result of an executed tool
    /// "system" → system prompt (does not appear in the history, goes in the LLMRequest)
    /// </summary>
    public class Message
    {
        public string Role    { get; set; }
        public string Content { get; set; }

        // ─── Campos para tool calling ──────────────────────────────────────────

        /// <summary>
        /// Filled when Role == "assistant" and the LLM decided to call a tool.
        /// Contains the raw JSON of the tool_calls array (serialized by OpenAISerializer).
        /// </summary>
        public string ToolCallsJson { get; set; }

        /// <summary>
        /// Filled in when Role == "tool" — references which tool_call generated this result.
        /// Mandatory by the OpenAI protocol to keep track of which result
        /// belongs to which call in a scenario with multiple simultaneous tools.
        /// </summary>
        public string ToolCallId { get; set; }

        public bool IsToolCall   => Role == "assistant" && !string.IsNullOrEmpty(ToolCallsJson);
        public bool IsToolResult => Role == "tool";

        // ─── Factory methods ───────────────────────────────────────────────────

        public static Message User(string content)
            => new Message { Role = "user", Content = content };

        public static Message Assistant(string content)
            => new Message { Role = "assistant", Content = content };

        public static Message System(string content)
            => new Message { Role = "system", Content = content };

        /// <summary>
        /// Assistant message that contains a tool call (not text).
        /// The toolCallsJson is the raw array returned by the API.
        /// </summary>
        public static Message AssistantToolCall(string toolCallsJson)
            => new Message { Role = "assistant", Content = null, ToolCallsJson = toolCallsJson };

        /// <summary>
        /// Result of a tool, associated with the Id of the original call.
        /// </summary>
        public static Message ToolResult(string toolCallId, string content)
            => new Message { Role = "tool", Content = content, ToolCallId = toolCallId };
    }
}
