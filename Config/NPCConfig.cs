using UnityEngine;

namespace NPC_AI.Config
{
    /// ScriptableObject holding per-NPC configuration (independent of personality).
    /// Create via Assets > Create > NPC AI > NPC Config.
    [CreateAssetMenu(fileName = "NPCConfig", menuName = "NPC AI/NPC Config")]
    public class NPCConfig : ScriptableObject
    {
        [Header("Decision Timing")]
        [Tooltip("How often (in seconds) this NPC queries the LLM for a decision.")]
        [Range(0.5f, 10f)]
        public float DecisionIntervalSeconds = 2f;

        [Tooltip("How long to wait (seconds) for an LLM response before falling back to the rule-based policy.")]
        [Range(1f, 30f)]
        public float InferenceTimeoutSeconds = 5f;

        [Header("Fallback Policy")]
        [Tooltip("If the LLM is unavailable or times out, flee when HP is below this threshold.")]
        [Range(0f, 1f)]
        public float FallbackFleeHealthThreshold = 0.3f;
    }
}
