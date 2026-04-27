using System;
using System.Linq;

namespace NPC_AI.PlayerAnalysis
{
    /// Rule-based analysis of a BehaviorEvent buffer.
    /// Runs in microseconds no second LLM call needed for pattern recognition.
    /// The LLM's job is to interpret the summary, not to compute it.
    public class PatternRecognizer
    {
        private const float AnalysisWindowSeconds = 30f;
        private const float RecentWindowSeconds = 8f;

        public PlayerBehaviorSummary Analyze(BehaviorEvent[] events)
        {
            if (events == null || events.Length == 0)
                return new PlayerBehaviorSummary();

            float now = events[events.Length - 1].Timestamp;
            var window = events.Where(e => now - e.Timestamp <= AnalysisWindowSeconds).ToArray();
            var recent = events.Where(e => now - e.Timestamp <= RecentWindowSeconds).ToArray();

            return new PlayerBehaviorSummary
            {
                DominantStyle = ClassifyStyle(window),
                RecentTrend   = ClassifyStyle(recent),
                AttackFrequency = ComputeFrequency(window, AnalysisWindowSeconds,
                    BehaviorEventType.DirectAttack, BehaviorEventType.GuardBreak),
                DodgeFrequency = ComputeFrequency(window, AnalysisWindowSeconds,
                    BehaviorEventType.DodgeLeft, BehaviorEventType.DodgeRight),
                PrefersRanged     = ComputeRangedPreference(window),
                HealThreshold     = EstimateHealThreshold(events),
                FlankingTendency  = ComputeFlankingScore(window)
            };
        }

        private BehaviorStyle ClassifyStyle(BehaviorEvent[] events)
        {
            if (events.Length == 0) return BehaviorStyle.Unknown;

            int attacks = Count(events, BehaviorEventType.DirectAttack, BehaviorEventType.GuardBreak, BehaviorEventType.UseRangedWeapon);
            int dodges  = Count(events, BehaviorEventType.DodgeLeft, BehaviorEventType.DodgeRight);
            int flanks  = Count(events, BehaviorEventType.Flank);
            int total   = events.Length;

            float attackRatio = (float)attacks / total;
            float dodgeRatio  = (float)dodges  / total;
            float flankRatio  = (float)flanks  / total;

            if (attackRatio > 0.5f) return BehaviorStyle.Aggressive;
            if (dodgeRatio  > 0.35f) return BehaviorStyle.Defensive;
            if (flankRatio  > 0.25f) return BehaviorStyle.Evasive;
            return BehaviorStyle.Tactical;
        }

        private float ComputeFrequency(BehaviorEvent[] events, float windowSeconds, params BehaviorEventType[] types)
        {
            if (windowSeconds <= 0) return 0f;
            int count = Count(events, types);
            return count / windowSeconds;
        }

        private bool ComputeRangedPreference(BehaviorEvent[] events)
        {
            int attacks = Count(events, BehaviorEventType.DirectAttack, BehaviorEventType.UseRangedWeapon, BehaviorEventType.GuardBreak);
            if (attacks == 0) return false;
            int ranged = Count(events, BehaviorEventType.UseRangedWeapon);
            return (float)ranged / attacks > 0.3f;
        }

        private float EstimateHealThreshold(BehaviorEvent[] events)
        {
            // Find heal events and look at the player's HP just before each one.
            var healEvents = events
                .Where(e => e.Type == BehaviorEventType.UseHealItem)
                .Select(e => e.PlayerHealthAtTime)
                .ToArray();

            if (healEvents.Length == 0) return 0.4f; // sensible default
            return healEvents.Average();
        }

        private float ComputeFlankingScore(BehaviorEvent[] events)
        {
            int flanks = Count(events, BehaviorEventType.Flank);
            if (events.Length == 0) return 0f;
            return Math.Min(1f, (float)flanks / events.Length * 3f); // scale up small values
        }

        private int Count(BehaviorEvent[] events, params BehaviorEventType[] types)
        {
            int count = 0;
            foreach (var e in events)
                foreach (var t in types)
                    if (e.Type == t) { count++; break; }
            return count;
        }
    }
}
