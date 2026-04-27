using System;
using UnityEngine;

namespace NPC_AI.Brain
{
    /// Deserializes the LLM's JSON output into an ActionCommand.
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

                if (raw == null || string.IsNullOrEmpty(raw.action))
                    return ParseResult.Fail("Could not parse action field.", llmOutput);

                if (!_registry.Contains(raw.action))
                    return ParseResult.Fail($"Unknown action type: {raw.action}", llmOutput);

                return ParseResult.Ok(new ActionCommand
                {
                    ActionType = raw.action,
                    Target = raw.target ?? "player",
                    Reasoning = raw.reasoning ?? "",
                    Urgency = raw.urgency ?? "medium",
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
