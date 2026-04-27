using System.Threading.Tasks;
using NPC_AI.Brain;
using NPC_AI.Core;

namespace NPC_AI.Actions
{
    /// Represents one action an NPC can take. Implement this interface to add custom actions.
    /// Register new actions via ActionRegistry.Register() in NPCController.Start().
    public interface INPCAction
    {
        /// Must match the string in PersonalityProfile.AllowedActionTypes exactly.
        string ActionType { get; }

        /// Injected into the system prompt so the LLM knows what this action does.
        string PromptDescription { get; }

        /// Called when NPCController receives a command with this action type.
        Task ExecuteAsync(ActionCommand command, NPCController npc);

        /// Returns false if preconditions aren't met (e.g. no target in range).
        bool CanExecute(NPCWorldView worldView);
    }
}
