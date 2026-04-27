using System.Collections.Generic;
using System.Text;
using NPC_AI.Core;
using NPC_AI.LLM;
using NPC_AI.Memory;
using NPC_AI.PlayerAnalysis;

namespace NPC_AI.Brain
{
    /// Assembles the LLMRequest from all contextual inputs.
    /// Enforces token budgets per section to keep prompts within model context limits.
    public class ContextBuilder
    {
        private readonly PersonalityProfile _personality;
        private readonly ActionRegistry _actionRegistry;

        public ContextBuilder(PersonalityProfile personality, ActionRegistry actionRegistry)
        {
            _personality = personality;
            _actionRegistry = actionRegistry;
        }

        public LLMRequest Build(
            NPCWorldView worldView,
            PlayerBehaviorSummary playerSummary,
            List<MemoryEntry> memories)
        {
            var sb = new StringBuilder();

            // 1. Identity
            sb.AppendLine(TokenBudget.Truncate(_personality.ToPromptText(), TokenBudget.Identity));
            sb.AppendLine();

            // 2. World State
            sb.AppendLine(TokenBudget.Truncate(worldView.ToPromptText(), TokenBudget.WorldState));
            sb.AppendLine();

            // 3. Player Behavior Profile
            if (playerSummary != null)
            {
                sb.AppendLine(TokenBudget.Truncate(playerSummary.ToPromptText(), TokenBudget.PlayerProfile));
                sb.AppendLine();
            }

            // 4. Memory
            if (memories != null && memories.Count > 0)
            {
                sb.AppendLine("MEMORIES:");
                int memoryTokensUsed = 0;
                foreach (var entry in memories)
                {
                    int tokens = TokenBudget.ApproxTokenCount(entry.Content);
                    if (memoryTokensUsed + tokens > TokenBudget.Memory) break;
                    sb.AppendLine($"- {entry.Content}");
                    memoryTokensUsed += tokens;
                }
                sb.AppendLine();
            }

            // 5. Available Actions
            sb.AppendLine(BuildActionList());
            sb.AppendLine();
            sb.AppendLine("Reply ONLY with a single valid JSON object matching the action schema above. No explanation outside the JSON.");

            return new LLMRequest
            {
                SystemPrompt = sb.ToString().TrimEnd(),
                UserMessage = "What is your next action?",
                Temperature = 0.7f,
                MaxTokens = TokenBudget.ResponseReserve,
                ResponseFormat = "json",
                StopSequences = new[] { "```", "\n\n\n" }
            };
        }

        private string BuildActionList()
        {
            var sb = new StringBuilder();
            sb.AppendLine("AVAILABLE ACTIONS (choose one):");
            sb.AppendLine("Schema: {\"action\": \"ACTION_TYPE\", \"target\": \"target\", \"reasoning\": \"brief reason\", \"urgency\": \"low|medium|high\"}");
            sb.AppendLine();

            foreach (var actionType in _personality.AllowedActionTypes)
            {
                var action = _actionRegistry.Get(actionType);
                if (action != null)
                    sb.AppendLine($"- {actionType}: {action.PromptDescription}");
            }

            return TokenBudget.Truncate(sb.ToString(), TokenBudget.ActionList);
        }
    }
}
