using System.Text;

namespace LLMFramework.Core
{
    /// <summary>
    /// Builds structured prompts using the fluent builder pattern.
    ///
    /// Why not concatenate strings directly in the agent?
    /// In Sprint 3, you will add context from the Unity world.
    /// In Sprint 4, you will add definitions for MCP tools.
    /// With this builder, each sprint adds a new method without touching the existing code.
    ///Usage:
        /// string prompt = new PromptBuilder().
        /// WithPersonality("You are a medieval guard.")
    /// . WithWorldContext("Location: North Gate. Time: night.")
    /// . WithUserInput("Can I come in?")
    /// . Construir();
    /// </summary>
    public sealed class PromptBuilder
    {
        private readonly StringBuilder _sb = new StringBuilder();

        public PromptBuilder WithPersonality(string personality)
        {
            if (!string.IsNullOrWhiteSpace(personality))
                _sb.AppendLine($"[PERSONALITY]\n{personality}\n");
            return this;
        }

        public PromptBuilder WithSystemInstruction(string instruction)
        {
            if (!string.IsNullOrWhiteSpace(instruction))
                _sb.AppendLine($"[INSTRUCTION]\n{instruction}\n");
            return this;
        }

        /// <summary>
        /// Unity world context — NPC position, time of day, nearby objects, etc.
        /// This field will grow in Sprint 3 when Unity feeds spatial data.
        /// </summary>
        public PromptBuilder WithWorldContext(string worldContext)
        {
            if (!string.IsNullOrWhiteSpace(worldContext))
                _sb.AppendLine($"[WORLD CONTEXT]\n{worldContext}\n");
            return this;
        }

        public PromptBuilder WithUserInput(string input)
        {
            if (!string.IsNullOrWhiteSpace(input))
                _sb.AppendLine($"[PLAYER]\n{input}\n");
            return this;
        }

        /// <summary>
        /// Free block for future extensions (e.g., conversation history, tool results).
        /// </summary>
        public PromptBuilder WithCustomBlock(string label, string content)
        {
            if (!string.IsNullOrWhiteSpace(content))
                _sb.AppendLine($"[{label.ToUpper()}]\n{content}\n");
            return this;
        }

        public string Build() => _sb.ToString().Trim();

        public PromptBuilder Reset()
        {
            _sb.Clear();
            return this;
        }
    }
}
