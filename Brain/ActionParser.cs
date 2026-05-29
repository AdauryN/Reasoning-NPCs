using System;
using UnityEngine;

namespace NPC_AI.Brain
{
    /// Deserializes the LLM's JSON output into an ActionCommand.
    /// Grammar-constrained sampling means this should rarely fail,
    /// but a fallback is always applied when it does.
    public class ActionParser
    {
        private readonly ActionRegistry _registry;

        public ActionParser(ActionRegistry registry)
        {
            _registry = registry;
        }

        public ParseResult Parse(string llmOutput)
        {
            if (string.IsNullOrWhiteSpace(llmOutput))
                return ParseResult.Fail("Empty LLM output.", llmOutput);

            try
            {
                // Use Unity's built-in JsonUtility for zero-dependency parsing.
                var raw = JsonUtility.FromJson<RawActionJson>(StripCodeFences(llmOutput));

                // Support both {"action":"X",...} and Ollama tool-call {"name":"X","parameters":{...}}
                string actionType = !string.IsNullOrEmpty(raw.action) ? raw.action : raw.name;

                if (raw == null || string.IsNullOrEmpty(actionType))
                    return ParseResult.Fail("Could not parse action field.", llmOutput);

                if (!_registry.Contains(actionType))
                    return ParseResult.Fail($"Unknown action type: {actionType}", llmOutput);

                string target    = raw.target    ?? raw.parameters?.target    ?? "player";
                string reasoning = raw.reasoning ?? raw.parameters?.reasoning ?? "";
                string urgency   = raw.urgency   ?? raw.parameters?.urgency   ?? "medium";

                return ParseResult.Ok(new ActionCommand
                {
                    ActionType = actionType,
                    Target = target,
                    Reasoning = reasoning,
                    Urgency = urgency,
                    IsFromFallback = false
                }, llmOutput);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ActionParser] Parse failed: {ex.Message}\nRaw: {llmOutput}");
                return ParseResult.Fail(ex.Message, llmOutput);
            }
        }

        private static string StripCodeFences(string text)
        {
            text = text.Trim();
            if (text.StartsWith("```"))
            {
                int firstNewline = text.IndexOf('\n');
                int lastFence = text.LastIndexOf("```");
                if (firstNewline > 0 && lastFence > firstNewline)
                    return text.Substring(firstNewline + 1, lastFence - firstNewline - 1).Trim();
            }
            return text;
        }

        [Serializable]
        private class RawActionJson
        {
            public string action;
            public string name;        // Ollama tool-call format uses "name"
            public string target;
            public string reasoning;
            public string urgency;
            public RawParameters parameters;
        }

        [Serializable]
        private class RawParameters
        {
            public string target;
            public string reasoning;
            public string urgency;
        }
    }

    public class ParseResult
    {
        public bool Success { get; private set; }
        public ActionCommand Command { get; private set; }
        public string ErrorReason { get; private set; }
        public string RawOutput { get; private set; }

        public static ParseResult Ok(ActionCommand cmd, string raw) =>
            new ParseResult { Success = true, Command = cmd, RawOutput = raw };

        public static ParseResult Fail(string reason, string raw) =>
            new ParseResult { Success = false, ErrorReason = reason, RawOutput = raw };
    }
}
