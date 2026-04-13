using UnityEngine;

namespace LLMFramework.Agents
{
    /// <summary>
    /// Configuration of an NPC-agent as a ScriptableObject.
    ///
    /// Why use ScriptableObject instead of fields in MonoBehaviour?
    /// - Allows creating an asset for each type of NPC (GuardConfig, MerchantConfig, QuestGiverConfig)
    /// and reuse the same LLMAgent prefab for all.
    /// - Editable at runtime during Playmode without recompiling.
    /// - Can be versioned as an asset in Git separate from the prefab.
    ///
    /// Creation: right-click on Project > LLMFramework > Agent Config
    /// </summary>
    [CreateAssetMenu(
        filename = "AgentConfig",
        menuName = "LLMFramework/Agent Config",
        order = 0
    )]
    public sealed class AgentConfig : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("NPC Name — used in logs and later in the conversation history.")]
        public string agentName = "NPC";

        [TextArea(3, 6)]
        [Tooltip("Personality and role of the NPC. Defines how he speaks and acts.")] 
        public string personality = "You are an NPC in a medieval fantasy world. Speak in a medieval manner.." 

        [Header("Behavior")]
        [TextArea(2, 4)]
        [Tooltip("Technical instruction for the LLM — not visible to the player. Limits the format and size of the responses.")] 
        public string systemInstruction = "Respond in character. Keep the answers short (1 to 3 sentences). Never break character." 

        [Header("Context")]
        [Tooltip("If true, the LLMAgent injects Unity world context into the prompt (position, time, nearby objects).")]
        public bool includeWorldContext = true;

        [Header("Debug")]
        [Tooltip("Logs full prompts and responses to the Console — disable in production.")]
        public bool verboseLogging = false;
    }
