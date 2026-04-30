using UnityEngine;

namespace NPC_AI.Config
{
    /// ScriptableObject holding all LLM backend configuration.
    [CreateAssetMenu(fileName = "LLMConfig", menuName = "NPC AI/LLM Config")]
    public class LLMConfig : ScriptableObject
    {
        [Header("Ollama Settings")]
        public string OllamaBaseUrl = "http://localhost:11434";
        public string OllamaModelName = "llama3.2:3b";
        public string OllamaEmbeddingModelName = "nomic-embed-text";

        [Header("Generation Settings")]
        [Range(0f, 2f)]
        public float Temperature = 0.7f;

        [Tooltip("Max tokens the model may generate per NPC decision.")]
        public int MaxResponseTokens = 256;
    }
}
