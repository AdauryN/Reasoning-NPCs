using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NPC_AI.Config;
using UnityEngine;
using UnityEngine.Networking;

namespace NPC_AI.LLM
{
    /// Calls a locally-running Ollama server (http://localhost:11434) for inference.
    /// Useful for development and rapid prototyping without bundling the model into the build.
    public class OllamaAdapter : ILLMService
    {
        private readonly LLMConfig _config;
        private bool _ready;
        private bool _disposed;

        public bool IsReady => _ready && !_disposed;

        public OllamaAdapter(LLMConfig config)
        {
            _config = config;
        }

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            // Verify Ollama is reachable and the model is available.
            try
            {
                var url = $"{_config.OllamaBaseUrl}/api/tags";
                using var req = UnityWebRequest.Get(url);
                var op = req.SendWebRequest();

                while (!op.isDone)
                {
                    if (ct.IsCancellationRequested) return;
                    await Task.Yield();
                }

                if (req.result == UnityWebRequest.Result.Success)
                {
                    _ready = true;
                    Debug.Log($"[OllamaAdapter] Connected to Ollama at {_config.OllamaBaseUrl}");
                }
                else
                {
                    Debug.LogWarning($"[OllamaAdapter] Could not reach Ollama: {req.error}. NPCs will use fallback policy.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OllamaAdapter] Init failed: {ex.Message}");
            }
        }

        public async Task<LLMResponse> CompleteAsync(LLMRequest request, CancellationToken ct = default)
        {
            if (!IsReady)
                return LLMResponse.Failure("Ollama adapter not ready.");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var body = BuildRequestBody(request);
                var bodyBytes = Encoding.UTF8.GetBytes(body);
                var url = $"{_config.OllamaBaseUrl}/api/chat";

                using var req = new UnityWebRequest(url, "POST");
                req.uploadHandler = new UploadHandlerRaw(bodyBytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    if (ct.IsCancellationRequested)
                        return LLMResponse.Failure("Request cancelled.");
                    await Task.Yield();
                }

                sw.Stop();

                if (req.result != UnityWebRequest.Result.Success)
                    return LLMResponse.Failure(req.error);

                var text = ExtractMessageContent(req.downloadHandler.text);
                return LLMResponse.Ok(text, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                Debug.LogError($"[OllamaAdapter] Error: {ex.Message}");
                return LLMResponse.Failure(ex.Message);
            }
        }

        public async Task StreamAsync(LLMRequest request, Action<string> onToken, CancellationToken ct = default)
        {
            var response = await CompleteAsync(request, ct);
            if (response.Success)
                onToken?.Invoke(response.Text);
        }

        private string BuildRequestBody(LLMRequest request)
        {
            var messages = new StringBuilder("[");

            if (!string.IsNullOrEmpty(request.SystemPrompt))
                messages.Append($"{{\"role\":\"system\",\"content\":{EscapeJson(request.SystemPrompt)}}},");

            foreach (var msg in request.History)
            {
                var role = msg.Role == ChatRole.User ? "user" : "assistant";
                messages.Append($"{{\"role\":\"{role}\",\"content\":{EscapeJson(msg.Content)}}},");
            }

            if (!string.IsNullOrEmpty(request.UserMessage))
                messages.Append($"{{\"role\":\"user\",\"content\":{EscapeJson(request.UserMessage)}}}");
            else if (messages[messages.Length - 1] == ',')
                messages.Remove(messages.Length - 1, 1);

            messages.Append("]");

            var temp = request.Temperature.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            var format = request.ResponseFormat == "json" ? ",\"format\":\"json\"" : "";
            return $"{{\"model\":{EscapeJson(_config.OllamaModelName)},\"messages\":{messages},\"stream\":false,\"options\":{{\"temperature\":{temp},\"num_predict\":{request.MaxTokens}}}{format}}}";
        }

        private static string ExtractMessageContent(string json)
        {
            // Minimal JSON extraction — avoid pulling in a full JSON library for a small field.
            const string key = "\"content\":";
            int start = json.IndexOf(key, StringComparison.Ordinal);
            if (start < 0) return json;
            start += key.Length;
            while (start < json.Length && json[start] != '"') start++;
            if (start >= json.Length) return json;
            start++;
            var sb = new StringBuilder();
            while (start < json.Length)
            {
                char c = json[start++];
                if (c == '\\' && start < json.Length)
                {
                    char esc = json[start++];
                    switch (esc)
                    {
                        case 'n' : sb.Append('n'); break;
                        case 'r' : sb.Append('r'); break;
                        case 't' : sb.Append('t'); break;
                        default: sb.Append(esc); break;
                    }
                }
                else if (c == '"')
                {
                    break;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string EscapeJson(string s) =>
            "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") + "\"";

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
