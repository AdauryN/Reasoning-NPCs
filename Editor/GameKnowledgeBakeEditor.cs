using System;
using System.Threading;
using NPC_AI.Config;
using NPC_AI.RAG;
using UnityEditor;
using UnityEngine;

namespace NPC_AI.Editor
{
    [CustomEditor(typeof(GameKnowledgeBase))]
    public class GameKnowledgeBakeEditor : UnityEditor.Editor
    {
        private LLMConfig _llmConfig;
        private bool _isBaking;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Embedding Baking", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Requires Ollama running with nomic-embed-text pulled.\n" +
                "Run: ollama pull nomic-embed-text",
                MessageType.Info);

            _llmConfig = (LLMConfig)EditorGUILayout.ObjectField(
                "LLM Config", _llmConfig, typeof(LLMConfig), false);

            var kb = (GameKnowledgeBase)target;
            int chunkCount = kb.Chunks.Count;

            EditorGUILayout.LabelField($"Chunks in asset: {chunkCount}");

            using (new EditorGUI.DisabledGroupScope(_isBaking || _llmConfig == null || chunkCount == 0))
            {
                string label = _isBaking ? "Baking…" : $"Bake {chunkCount} Chunk(s)";
                if (GUILayout.Button(label, GUILayout.Height(30)))
                    BakeAsync(kb);
            }

            if (_llmConfig == null)
                EditorGUILayout.HelpBox("Assign a LLMConfig asset to enable baking.", MessageType.Warning);

            if (chunkCount == 0)
                EditorGUILayout.HelpBox("Add chunks to the GameKnowledgeBase asset first.", MessageType.Warning);
        }

        private async void BakeAsync(GameKnowledgeBase kb)
        {
            _isBaking = true;
            Repaint();

            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            var adapter = new OllamaEmbeddingAdapter(_llmConfig);

            try
            {
                EditorUtility.DisplayProgressBar("Baking Embeddings", "Connecting to Ollama…", 0f);
                await adapter.InitializeAsync(cts.Token);

                if (!adapter.IsReady)
                {
                    EditorUtility.DisplayDialog(
                        "Bake Failed",
                        "Could not connect to Ollama.\n\nMake sure Ollama is running and nomic-embed-text is pulled:\n  ollama pull nomic-embed-text",
                        "OK");
                    return;
                }

                EditorUtility.DisplayProgressBar("Baking Embeddings", "Embedding chunks…", 0.2f);
                await kb.BakeEmbeddingsAsync(adapter, cts.Token);

                EditorUtility.SetDirty(kb);
                AssetDatabase.SaveAssets();

                Debug.Log($"[GameKnowledgeBakeEditor] Baked {kb.Chunks.Count} chunks successfully.");
                EditorUtility.DisplayDialog(
                    "Bake Complete",
                    $"Successfully baked {kb.Chunks.Count} chunk(s).\nEmbeddings saved to asset.",
                    "OK");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[GameKnowledgeBakeEditor] Baking cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameKnowledgeBakeEditor] Bake failed: {ex.Message}");
                EditorUtility.DisplayDialog("Bake Failed", ex.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                adapter.Dispose();
                _isBaking = false;
                Repaint();
            }
        }
    }
}
