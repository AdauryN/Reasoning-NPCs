using System.Threading;
using System.Threading.Tasks;
using NPC_AI.Brain;

namespace NPC_AI.Core
{
    public interface INPCBrain
    {
        bool IsIdle { get; }

        Task InitializeAsync(CancellationToken ct = default);

        Task<ActionCommand> DecideAsync(NPCWorldView worldView, CancellationToken ct = default);
    }
}
