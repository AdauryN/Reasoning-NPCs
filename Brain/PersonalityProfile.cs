using System.Collections.Generic;
using UnityEngine;

namespace NPC_AI.Brain
{
    /// ScriptableObject that defines an NPC's identity, disposition, and allowed action set.
    /// Create via Assets > Create > NPC AI > Personality Profile.
    [CreateAssetMenu(fileName = "PersonalityProfile", menuName = "NPC AI/Personality Profile")]
    public class PersonalityProfile : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Injected verbatim as the first section of the system prompt.")]
        [TextArea(4, 12)]
        public string IdentityPrompt = "You are a guard NPC. You protect your post.";

        [Header("Personality Traits (0 = none, 1 = extreme)")]
        [Range(0f, 1f)] public float Aggression = 0.5f;
        [Range(0f, 1f)] public float Caution = 0.5f;
        [Range(0f, 1f)] public float Loyalty = 0.5f;

        [Header("Behavior Constraints")]
        [Tooltip("Hard rules appended to the identity prompt, e.g. 'Never attack allies.'")]
        [TextArea(2, 6)]
        public string BehaviorGuidelines;

        [Header("Actions")]
        [Tooltip("Which action types this NPC may choose. Must match ActionRegistry keys exactly.")]
        public List<string> AllowedActionTypes = new List<string>
        {
            "ATTACK_MELEE", "DODGE", "RETREAT", "PATROL", "IDLE"
        };

        public string ToPromptText()
        {
            var traits = $"Aggression: {Aggression:F1}/1.0, Caution: {Caution:F1}/1.0, Loyalty: {Loyalty:F1}/1.0";
            var guidelines = string.IsNullOrEmpty(BehaviorGuidelines)
                ? ""
                : $"\nBehavior rules: {BehaviorGuidelines}";
            return $"{IdentityPrompt}\nPersonality traits: {traits}{guidelines}";
        }
    }
}
