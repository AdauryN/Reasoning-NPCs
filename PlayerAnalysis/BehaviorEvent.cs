using UnityEngine;

namespace NPC_AI.PlayerAnalysis
{
    public enum BehaviorEventType
    {
        DirectAttack,
        DodgeLeft,
        DodgeRight,
        Retreat,
        Flank,
        UseHealItem,
        UseRangedWeapon,
        GuardBreak,
        Wait,
        Block
    }

    public class BehaviorEvent
    {
        public BehaviorEventType Type { get; set; }
        public float Timestamp { get; set; }          // Time when event occurred
        public Vector3 PlayerPosition { get; set; }
        public Vector3 NpcPosition { get; set; }
        public float PlayerHealthAtTime { get; set; } // 0–1
        public bool WasSuccessful { get; set; }       // did the action suceed?
    }
}
