using System;
using UnityEngine;

namespace NPC_AI.RAG
{
    [Serializable]
    public class KnowledgeChunk
    {
        [Tooltip("Short identifier, e.g. 'faction_orc_lore'")]
        public string Id;

        [TextArea(2, 6)]
        [Tooltip("The text content injected into the prompt when this chunk is retrieved.")]
        public string Content;

        [Tooltip("Topic tags for pre-filtering before vector search.")]
        public string[] Tags;

        /// <summary>Precomputed embedding — populated offline by GameKnowledgeBase.BakeEmbeddings().</summary>
        [HideInInspector]
        public float[] Embedding;
    }
}
