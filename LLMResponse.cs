using System.Collections.Generic;

namespace LLMFramework.Core
{
    /// <summary>
    /// Encapsulates the result of a call to the LLM.
    /// Sprint 3: added support for ToolCalls — when the LLM decides
    /// use a tool instead of responding in text.
    /// </summary>
    public class LLMResponse
    {
        public string Content   { get; set; }
        public bool   IsSuccess { get; set; }
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Tool calls that the LLM wants to execute.
        /// If HasToolCall == true, Content is empty and the agent must
        /// execute the tools before obtaining the final answer.
        /// </summary>
        public List<ToolCall> ToolCalls { get; set; }

        public bool HasToolCall => ToolCalls != null && ToolCalls.Count > 0;

        public static LLMResponse Success(string content)
            => new LLMResponse { Content = content, IsSuccess = true };

        public static LLMResponse WithToolCalls(List<ToolCall> calls, string rawToolCallsJson)
            => new LLMResponse
            {
                IsSuccess       = true,
                Content         = rawToolCallsJson, // preserved to inject into the history
                ToolCalls       = calls
            };

        public static LLMResponse Failure(string error)
            => new LLMResponse { IsSuccess = false, ErrorMessage = error, Content = string.Empty };
    }
}
