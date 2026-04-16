using System.Threading;
using NPC_AI.Config;
using UnityEngine;

namespace NPC_AI.LLM
{
    /// <summary>
    /// Sprint 1 verification script. Attach to any GameObject, assign an LLMConfig asset,
    /// press Play, and watch the Console for the LLM response and latency.
    /// Remove or disable this script before Sprint 2.
    /// </summary>
    public class LLMTestRunner : MonoBehaviour
    {
        [SerializeField] private LLMConfig llmConfig;

        [TextArea(2, 5)]
        [SerializeField] private string testPrompt = "You are a medieval knight NPC. The player just entered the castle. What do you say? Reply in one sentence.";

        private ILLMService _llm;
        private CancellationTokenSource _cts;

        private async void Start()
        {
            if (llmConfig == null)
            {
                Debug.LogError("[LLMTestRunner] Assign an LLMConfig asset in the Inspector.");
                return;
            }

            _cts = new CancellationTokenSource();
            _llm = LLMServiceFactory.GetShared(llmConfig);

            Debug.Log("[LLMTestRunner] Loading model…");
            await _llm.InitializeAsync(_cts.Token);
            Debug.Log("[LLMTestRunner] Model ready. Sending test prompt…");

            var request = new LLMRequest
            {
                SystemPrompt = "You are a helpful NPC in a fantasy game.",
                UserMessage = testPrompt,
                MaxTokens = 128,
                Temperature = 0.8f
            };

            var response = await _llm.CompleteAsync(request, _cts.Token);

            if (response.Success)
                Debug.Log($"[LLMTestRunner] Response ({response.LatencyMs:F0} ms):\n{response.Text}");
            else
                Debug.LogError($"[LLMTestRunner] Failed: {response.ErrorMessage}");
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
