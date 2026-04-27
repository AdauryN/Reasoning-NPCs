using System.Globalization;
using NPC_AI.Core;
using NPC_AI.LLM;

namespace NPC_AI.Brain
{
    /// Answers LLM tool calls using the NPCWorldView snapshot that was captured
    /// at the start of the current decision cycle. Responses are synchronous and no additional network calls required.
    public class WorldStateToolHandler : IToolHandler
    {
        private readonly NPCWorldView _view;

        public WorldStateToolHandler(NPCWorldView view)
        {
            _view = view;
        }

        public string HandleToolCall(string toolName, string argsJson)
        {
            var ic = CultureInfo.InvariantCulture;
            switch (toolName)
            {
                case "get_npc_health":
                    return _view.NpcHealthPct.ToString("F2", ic);

                case "get_player_position":
                    return $"{{\"x\":{_view.PlayerPosition.x.ToString("F1", ic)}" +
                           $",\"y\":{_view.PlayerPosition.y.ToString("F1", ic)}" +
                           $",\"z\":{_view.PlayerPosition.z.ToString("F1", ic)}}}";

                case "get_nearby_enemies":
                    return _view.EnemiesNearby.ToString(ic);

                case "get_distance_to_player":
                    return _view.DistanceToPlayer.ToString("F1", ic);

                default:
                    return $"{{\"error\":\"unknown tool '{toolName}'\"}}";
            }
        }
    }
}
