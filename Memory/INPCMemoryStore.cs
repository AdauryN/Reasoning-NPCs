using System.Collections.Generic;
using System.Threading.Tasks;
using NPC_AI.Core;

namespace NPC_AI.Memory
{
    public interface INPCMemoryStore
    {
        Task AddAsync(MemoryEntry entry);
        
        /// Returns the top-K most relevant memories for the given world context.
        /// Implementations may use recency, importance, or vector similarity.
        List<MemoryEntry> GetRelevant(NPCWorldView context, int topK);

        Task ClearAsync();
    }
}
