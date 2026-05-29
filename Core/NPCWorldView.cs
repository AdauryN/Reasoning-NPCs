using UnityEngine;

namespace NPC_AI.Core
{
    /// A snapshot of the game state relevant to one NPC's decision.
    /// Populated each decision tick by NPCController and passed to NPCBrain.
    /// Every field becomes tokens in the prompt.
    public class NPCWorldView
    {
        public string NpcId { get; set; }
        public float NpcHealthPct { get; set; }       // 0–1
        public float PlayerHealthPct { get; set; }    // 0–1
        public float DistanceToPlayer { get; set; }   // world units
        public bool PlayerFacingNpc { get; set; }
        public int AlliesNearby { get; set; }
        public int EnemiesNearby { get; set; }
        public string LastPlayerAction { get; set; } 
        public float LastPlayerActionSecondsAgo { get; set; }
        public string LastNpcAction { get; set; }
        public float LastNpcActionSecondsAgo { get; set; }
        public Vector3 NpcPosition { get; set; }
        public Vector3 PlayerPosition { get; set; }

        public string ToPromptText()
        {
            string hpStatus = NpcHealthPct > 0.6f ? "HEALTHY" : NpcHealthPct > 0.3f ? "WOUNDED" : "CRITICAL - retreat recommended";
            string distStatus = DistanceToPlayer < 8f ? "IN MELEE RANGE" : "OUT OF MELEE RANGE - melee attacks will miss";
            string playerHpStatus = PlayerHealthPct > 0.6f ? "healthy" : PlayerHealthPct > 0.3f ? "wounded" : "critical";

            return
                $"WORLD STATE:\n" +
                $"- Your HP: {NpcHealthPct:P0} ({hpStatus}) | Player HP: {PlayerHealthPct:P0} (player is {playerHpStatus})\n" +
                $"- Distance to player: {DistanceToPlayer:F1}m ({distStatus})\n" +
                $"- Nearby allies: {AlliesNearby} | Nearby enemies: {EnemiesNearby}\n" +
                $"- Last player action: {LastPlayerAction ?? "NONE"}\n" +
                $"- Your last action: {LastNpcAction ?? "NONE"}";
        }
    }
}
