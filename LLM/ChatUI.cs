using System.Collections.Generic;
using System.Threading;
using NPC_AI.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NPC_AI.LLM
{
    /// Simple in-game chat UI for conversing with the LLM.
    public class ChatUI : MonoBehaviour
    {
        [Header("LLM")]
        [SerializeField] private LLMConfig llmConfig;

        [Header("Persona")]
        [TextArea(3, 6)]
        [SerializeField] private string systemPrompt = "You are a helpful assistant.";

        [Header("UI References")]
        [SerializeField] private TMP_Text conversationLog;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private ScrollRect scrollRect;

        private ILLMService _llm;
        private CancellationTokenSource _cts;
        private readonly List<ChatMessage> _history = new List<ChatMessage>();
        private bool _busy;

        private async void Start()
        {
            if (llmConfig == null)
            {
                Debug.LogError("[ChatUI] Assign an LLMConfig asset in the Inspector.");
                enabled = false;
                return;
            }

            sendButton.onClick.AddListener(SendMessage);
            if (clearButton != null)
                clearButton.onClick.AddListener(ClearHistory);

            _cts = new CancellationTokenSource();
            _llm = LLMServiceFactory.GetShared(llmConfig);

            SetBusy(true);
            AppendLine("<color=#888888>Connecting to Ollama…</color>");
            await _llm.InitializeAsync(_cts.Token);

            if (_llm.IsReady)
                AppendLine("<color=#888888>Ready. Type a message and press Send or Enter.</color>\n");
            else
                AppendLine("<color=#ff4444>Could not reach Ollama. Check that it is running.</color>");

            SetBusy(!_llm.IsReady);
        }

        private void Update()
        {
            if (!_busy && Input.GetKeyDown(KeyCode.Return))
                SendMessage();
        }

        private async void SendMessage()
        {
            var userText = inputField.text.Trim();
            if (string.IsNullOrEmpty(userText) || _busy) return;

            inputField.text = string.Empty;
            SetBusy(true);

            AppendLine($"<color=#aaddff>You:</color> {EscapeRichText(userText)}");

            _history.Add(new ChatMessage { Role = ChatRole.User, Content = userText });

            var request = new LLMRequest
            {
                SystemPrompt = systemPrompt,
                History      = new List<ChatMessage>(_history),
                MaxTokens    = llmConfig.MaxResponseTokens,
                Temperature  = llmConfig.Temperature
            };

            // Remove the last user message from history before sending — OllamaAdapter
            // appends request.UserMessage separately; here we pass it via History instead,
            // so leave UserMessage null and let the full history carry the conversation.
            // (History already contains the user turn we just added.)
            request.UserMessage = null;

            AppendLine("<color=#888888><i>Thinking…</i></color>");

            var response = await _llm.CompleteAsync(request, _cts.Token);

            // Remove the "Thinking…" line
            var log = conversationLog.text;
            var thinkingTag = "<color=#888888><i>Thinking…</i></color>\n";
            int idx = log.LastIndexOf(thinkingTag, System.StringComparison.Ordinal);
            if (idx >= 0)
                conversationLog.text = log.Remove(idx, thinkingTag.Length);

            if (response.Success)
            {
                AppendLine($"<color=#aaffaa>NPC:</color> {EscapeRichText(response.Text)}\n");
                _history.Add(new ChatMessage { Role = ChatRole.Assistant, Content = response.Text });
            }
            else
            {
                AppendLine($"<color=#ff4444>Error: {EscapeRichText(response.ErrorMessage)}</color>\n");
                // Roll back the user message that failed
                if (_history.Count > 0 && _history[_history.Count - 1].Role == ChatRole.User)
                    _history.RemoveAt(_history.Count - 1);
            }

            SetBusy(false);
            ScrollToBottom();
        }

        private void ClearHistory()
        {
            _history.Clear();
            conversationLog.text = string.Empty;
            AppendLine("<color=#888888>History cleared.</color>\n");
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            sendButton.interactable = !busy;
            inputField.interactable = !busy;
            if (inputField.placeholder is TMP_Text ph)
                ph.text = busy ? "Waiting for response…" : "Type a message…";
            if (!busy)
                inputField.ActivateInputField();
        }

        private void AppendLine(string richText)
        {
            conversationLog.text += richText + "\n";
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        private static string EscapeRichText(string s) =>
            s.Replace("<", "\u003C").Replace(">", "\u003E");

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
