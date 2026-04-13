using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using LLMFramework.Core;

namespace LLMFramework.Agents
{
    /// <summary>
    /// The central component of the framework — the "brain" of the NPC.
    /// Sprint 3: added support for tool calling via IMCPClient.
    /// Sprint 3: added support for tool calling via IMCPClient. /// /// Flow with tools:
    /// Flow with tools:
    /// AskAsync("How many coins do I have?")
    /// → LLM decides to call "get_player_gold"
    /// → MCPClient executes the tool → returns "150"
    /// → Result injected into history
    /// → LLM generates: "You have 150 gold coins, adventurer!"
    /// → AskAsync returns the final answer
    /// </summary>
    public class LLMAgent : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private AgentConfig config;

        private ILLMClient  _client;
        private IMCPClient  _mcpClient;
        private PromptBuilder _promptBuilder;
        private readonly List<Message> _history = new List<Message>();

        // Maximum number of tool calling iterations per turn.
        // Prevents infinite loops in case the LLM calls tools recursively.
        private const int MaxToolIterations = 5;

        // ─── Public State ────────────────────────────────────────────────────

        public bool IsReady    => _client != null;
        public string AgentName => gameObject.name;
        public bool HasTools   => _mcpClient != null && _mcpClient.GetTools().Count > 0;

        // ─── Initialization ─────────────────────────────────────────────────────

        public void Initialize(ILLMClient client)
        {
            if (client == null)
            {
                Debug.LogError($"[LLMAgent:{AgentName}] ILLMClient can't be null.");
                return;
            }

            _client        = client;
            _promptBuilder = new PromptBuilder();
            _history.Clear();

            if (config != null && config.logRequests)
                Debug.Log($"[LLMAgent:{AgentName}] Initialization with {client.GetType().Name}.");
        }

        /// <summary>
        /// Injects the MCP tool provider.
        /// It can be called after Initialize(), even at runtime.
        ///Passing null disables tool calling (the agent returns to pure text mode).
        /// </summary>
        public void SetMCPClient(IMCPClient mcpClient)
        {
            _mcpClient = mcpClient;

            if (mcpClient != null && config != null && config.logRequests)
                Debug.Log($"[LLMAgent:{AgentName}] MCPClient configurado: " +
                          $"{mcpClient.GetTools().Count} ferramenta(s) disponível(is).");
        }

        // ─── Interação ─────────────────────────────────────────────────────────

        /// <summary>
        /// Sends a message to the NPC and waits for the final response (after tool calls if necessary).
        /// Returns null in case of error.
        /// </summary>
        public async Task<string> AskAsync(string userInput)
        {
            if (!IsReady)
            {
                Debug.LogError($"[LLMAgent:{AgentName}] Not initialized. Call Initialize() first.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(userInput))
            {
                Debug.LogWarning($"[LLMAgent:{AgentName}] empty message ignore.");
                return null;
            }

            _history.Add(Message.User(userInput));
            TrimHistory();

            if (config != null && config.logRequests)
                Debug.Log($"[LLMAgent:{AgentName}] → \"{userInput}\"");

            // ─── Tool calling Loop ──────────────────────────────────────────
            // The LLM can chain multiple tool calls before responding in text.
            LLMResponse response = null;
            int iterations = 0;

            while (iterations < MaxToolIterations)
            {
                LLMRequest request = BuildRequest();
                response = await _client.GenerateAsync(request);

                if (!response.IsSuccess)
                {
                    Debug.LogError($"[LLMAgent:{AgentName}] Erro: {response.ErrorMessage}");
                    _history.RemoveAt(_history.Count - 1); // reverte mensagem do usuário
                    return null;
                }

                // Sem tool call → resposta de texto → sai do loop
                if (!response.HasToolCall)
                    break;

                // ─── Executa as tool calls ─────────────────────────────────────
                if (_mcpClient == null)
                {
                    Debug.LogWarning($"[LLMAgent:{AgentName}] LLM asked tool call but there wasn't a MCPClient configured. " +
                                     "Call SetMCPClient() to enable tools.");
                    break;
                }

                // Preserve the assistant's message with the tool_calls JSON in the history.
                // The LLM needs to see its own call to continue the reasoning.
                _history.Add(Message.AssistantToolCall(response.Content));

                foreach (var toolCall in response.ToolCalls)
                {
                    if (config != null && config.logRequests)
                        Debug.Log($"[LLMAgent:{AgentName}] 🔧 Calling tool: {toolCall.FunctionName}({toolCall.ArgumentsJson})");

                    ToolCallResult result = await _mcpClient.CallToolAsync(
                        toolCall.FunctionName, toolCall.ArgumentsJson);

                    if (config != null && config.logResponses)
                        Debug.Log($"[LLMAgent:{AgentName}] 🔧 Resul: {result.Content}");

                    // Inject the result into the history for the next iteration.
                    _history.Add(Message.ToolResult(toolCall.Id, result.Content));
                }

                iterations++;
            }

            if (iterations >= MaxToolIterations)
                Debug.LogWarning($"[LLMAgent:{AgentName}] limit of {MaxToolIterations} of tool calling.");

            // response.Content is the final answer
            string finalContent = response?.Content;

            if (string.IsNullOrEmpty(finalContent))
            {
                Debug.LogError($"[LLMAgent:{AgentName}] empty response after tool calling.");
                return null;
            }

            _history.Add(Message.Assistant(finalContent));

            if (config != null && config.logResponses)
                Debug.Log($"[LLMAgent:{AgentName}] ← \"{finalContent}\"");

            return finalContent;
        }

        // ─── State Manager ───────────────────────────────────────────

        public void ClearHistory()
        {
            _history.Clear();
            if (config != null && config.logRequests)
                Debug.Log($"[LLMAgent:{AgentName}] clear  history.");
        }

        public void SetConfig(AgentConfig newConfig) => config = newConfig;

        // ─── Intern ──────────────────────────────────────────────────────────

        private LLMRequest BuildRequest()
        {
            _promptBuilder.Clear();

            if (config != null)
                _promptBuilder.WithSystemPrompt(config.personality);

            foreach (var msg in _history)
                _promptBuilder.AddMessage(msg);

            var request = _promptBuilder.Build(
                temperature: config != null ? config.temperature : 0.7f,
                maxTokens:   config != null ? config.maxTokens   : 256);

            // Inject tools if MCPClient configured
            if (_mcpClient != null)
            {
                request.Tools = new System.Collections.Generic.List<MCPTool>(_mcpClient.GetTools());
            }

            return request;
        }

        private void TrimHistory()
        {
            int max = config != null ? config.maxHistoryMessages : 10;
            while (_history.Count > max && _history.Count >= 2)
            {
                _history.RemoveAt(0);
                _history.RemoveAt(0);
            }
        }
    }
}

