using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NPC_AI.Config;
using UnityEngine;
using UnityEngine.Networking;

namespace NPC_AI.LLM
{
    /// Calls a locally-running Ollama server (http://localhost:11434) for inference.
    ///
    /// Supports an optional tool-call loop: if LLMRequest.Tools is non-empty,
    /// the adapter sends the tool schemas to Ollama, intercepts tool_calls responses,
    /// invokes LLMRequest.ToolHandler for each call, appends the results, and
    /// re-sends — repeating up to MaxToolRounds times until the model returns plain text.
    public class OllamaAdapter : ILLMService
    {
        private const int MaxToolRounds = 5;

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
                bool useTools = request.Tools != null && request.Tools.Count > 0 && request.ToolHandler != null;
                var messages = BuildInitialMessages(request);

                for (int round = 0; round <= MaxToolRounds; round++)
                {
                    var body = BuildBody(messages, request, includeTools: useTools && round == 0);
                    var rawJson = await PostAsync(body, ct);

                    if (rawJson == null)
                    {
                        sw.Stop();
                        return LLMResponse.Failure("HTTP request failed or was cancelled.");
                    }

                    if (useTools && HasToolCalls(rawJson))
                    {
                        var calls = ExtractToolCallList(rawJson);
                        if (calls.Count == 0)
                        {
                            // Malformed tool_calls treat as plain response
                            break;
                        }

                        // Append the assistant's tool_calls message then each tool result
                        messages.Add(BuildAssistantToolCallMessage(calls));
                        foreach (var (name, argsJson) in calls)
                        {
                            var result = request.ToolHandler.HandleToolCall(name, argsJson);
                            messages.Add($"{{\"role\":\"tool\",\"content\":{EscapeJson(result)}}}");
                        }
                        continue;
                    }

                    sw.Stop();
                    var text = ExtractMessageContent(rawJson);
                    return LLMResponse.Ok(text, sw.ElapsedMilliseconds);
                }

                sw.Stop();
                return LLMResponse.Failure("Max tool-call rounds exceeded without a final response.");
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

        // HTTP helpers 

        private async Task<string> PostAsync(string body, CancellationToken ct)
        {
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
                    return null;
                await Task.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[OllamaAdapter] HTTP error: {req.error}");
                return null;
            }

            return req.downloadHandler.text;
        }

        // Request building 

        private List<string> BuildInitialMessages(LLMRequest request)
        {
            var messages = new List<string>();

            if (!string.IsNullOrEmpty(request.SystemPrompt))
                messages.Add($"{{\"role\":\"system\",\"content\":{EscapeJson(request.SystemPrompt)}}}");

            foreach (var msg in request.History)
            {
                var role = msg.Role == ChatRole.User ? "user" : "assistant";
                messages.Add($"{{\"role\":\"{role}\",\"content\":{EscapeJson(msg.Content)}}}");
            }

            if (!string.IsNullOrEmpty(request.UserMessage))
                messages.Add($"{{\"role\":\"user\",\"content\":{EscapeJson(request.UserMessage)}}}");
            else if (messages.Count > 0 && !messages[messages.Count - 1].Contains("\"role\":\"system\""))
            {
                // History already ends with the user turn — nothing to add.
            }

            return messages;
        }

        private string BuildBody(List<string> messages, LLMRequest request, bool includeTools)
        {
            var messagesJson = "[" + string.Join(",", messages) + "]";
            var temp = request.Temperature.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

            // Only apply format:json when not in tool-call mode — Ollama's tool response
            // format conflicts with the grammar-constrained JSON output mode.
            var format = (!includeTools && request.ResponseFormat == "json") ? ",\"format\":\"json\"" : "";
            var toolsStr = (includeTools && request.Tools != null && request.Tools.Count > 0)
                ? ",\"tools\":" + BuildToolsArray(request.Tools)
                : "";

            return $"{{\"model\":{EscapeJson(_config.OllamaModelName)},\"messages\":{messagesJson}" +
                   $",\"stream\":false,\"options\":{{\"temperature\":{temp},\"num_predict\":{request.MaxTokens}}}" +
                   $"{format}{toolsStr}}}";
        }

        private static string BuildToolsArray(List<ToolDefinition> tools)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < tools.Count; i++)
            {
                if (i > 0) sb.Append(",");
                var t = tools[i];
                sb.Append($"{{\"type\":\"function\",\"function\":{{\"name\":{EscapeJson(t.Name)}" +
                          $",\"description\":{EscapeJson(t.Description)}" +
                          $",\"parameters\":{t.ParametersJson}}}}}");
            }
            sb.Append("]");
            return sb.ToString();
        }

        // Tool-call parsing 

        private static bool HasToolCalls(string json) =>
            json.IndexOf("\"tool_calls\"", StringComparison.Ordinal) >= 0;
        
        /// Extracts (toolName, argsJson) pairs from an Ollama tool_calls response.
        private static List<(string Name, string ArgsJson)> ExtractToolCallList(string json)
        {
            var result = new List<(string, string)>();
            int pos = json.IndexOf("\"tool_calls\"", StringComparison.Ordinal);
            if (pos < 0) return result;

            while (pos < json.Length)
            {
                int namePos = json.IndexOf("\"name\":", pos, StringComparison.Ordinal);
                if (namePos < 0) break;
                namePos += 7;
                while (namePos < json.Length && json[namePos] != '"') namePos++;
                if (namePos >= json.Length) break;
                namePos++;
                int nameEnd = json.IndexOf('"', namePos);
                if (nameEnd < 0) break;
                var name = json.Substring(namePos, nameEnd - namePos);

                int argsPos = json.IndexOf("\"arguments\":", nameEnd, StringComparison.Ordinal);
                if (argsPos < 0) break;
                argsPos += 12;
                while (argsPos < json.Length && json[argsPos] != '{') argsPos++;
                if (argsPos >= json.Length) break;

                int depth = 0, argsEnd = argsPos;
                while (argsEnd < json.Length)
                {
                    if (json[argsEnd] == '{') depth++;
                    else if (json[argsEnd] == '}') { if (--depth == 0) { argsEnd++; break; } }
                    argsEnd++;
                }
                var argsJson = json.Substring(argsPos, argsEnd - argsPos);

                result.Add((name, argsJson));
                pos = argsEnd;
            }

            return result;
        }
        
        /// Rebuilds the assistant tool_calls message to echo back in the next request.
        private static string BuildAssistantToolCallMessage(List<(string Name, string ArgsJson)> calls)
        {
            var sb = new StringBuilder();
            sb.Append("{\"role\":\"assistant\",\"content\":\"\",\"tool_calls\":[");
            for (int i = 0; i < calls.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"{{\"function\":{{\"name\":{EscapeJson(calls[i].Name)}" +
                          $",\"arguments\":{calls[i].ArgsJson}}}}}");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        // Response parsing 

        private static string ExtractMessageContent(string json)
        {
            // Ollama returns {"message":{"role":"assistant","content":"..."}}
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
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        default:  sb.Append(esc);  break;
                    }
                }
                else if (c == '"') break;
                else sb.Append(c);
            }
            return sb.ToString();
        }

        // Utilities 

        private static string EscapeJson(string s) =>
            "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r") + "\"";

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
