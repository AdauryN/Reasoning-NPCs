namespace NPC_AI.PlayerAnalysis
{
    public enum BehaviorStyle
    {
        Unknown,
        Aggressive,   // high attack frequency, low dodging
        Defensive,    // high blocking/dodging, low attack frequency
        Evasive,      // high movement, flanking, hit-and-run
        Tactical      // mixed, situational — adapts based on context
    }


    /// Compact statistical profile of the player's recent behavior.
    /// Injected into the NPC system prompt (~150 tokens).
    public class PlayerBehaviorSummary
    {
        public BehaviorStyle DominantStyle { get; set; } = BehaviorStyle.Unknown;
        public BehaviorStyle RecentTrend { get; set; } = BehaviorStyle.Unknown;

        /// Attacks per second over the last 30 seconds.
        public float AttackFrequency { get; set; }

        /// Dodge events per second over the last 30 seconds.
        public float DodgeFrequency { get; set; }

        /// True if more than 30% of attack events used ranged weapons.
        public bool PrefersRanged { get; set; }

        /// Estimated HP fraction at which the player typically uses a heal item.
        public float HealThreshold { get; set; } = 0.4f;

        /// 0–1 score: how often the player repositions to attack from the side or behind.
        public float FlankingTendency { get; set; }

        public string ToPromptText()
        {
            return
                $"PLAYER BEHAVIOR PROFILE:\n" +
                $"- Fighting style: {DominantStyle} (recent trend: {RecentTrend})\n" +
                $"- Attack rate: {AttackFrequency:F1}/sec | Dodge rate: {DodgeFrequency:F1}/sec\n" +
                $"- Heals when HP drops below ~{HealThreshold:P0}\n" +
                $"- Flanking tendency: {FlankingTendency:P0} | Prefers ranged: {PrefersRanged}";
        }
    }
}
