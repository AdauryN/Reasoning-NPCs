using System.Threading;
using System.Threading.Tasks;
using LLMFramework.Core;

namespace LLMFramework.Mock
{
    public sealed class MockLLMClient : ILLMClient
    {
        private readonly int  _delayMs;
        private readonly bool _simulateFailure;

        public bool IsAvailable => !_simulateFailure;

        /// <param name="delayMs">Simulates response latency in ms (default 600ms)</param>
        /// <param name="simulateFailure">If true, always returns failure — useful for testing error handling</param>
        public MockLLMClient(int delayMs = 600, bool simulateFailure = false)
        {
            _delayMs        = delayMs;
            _simulateFailure = simulateFailure;
        }

        public async Task<LLMResponse> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
        {
            // Simulate LLM processing time
            await Task.Delay(_delayMs, cancellationToken);

            if (_simulateFailure)
                return LLMResponse.Failure("MockLLMClient: simulated failure for testing.");

            string response = GenerateContextualResponse(prompt);
            return LLMResponse.Success(response, _delayMs);
        }

        // ---------------------------------------------------------------------------
        // Keyword-based responses — makes the mock useful for testing real flows
        // ---------------------------------------------------------------------------
        private string GenerateContextualResponse(string prompt)
        {
            string lower = prompt.ToLower();

            if (ContainsAny(lower, "saudação", "olá", "oi", "greet", "hello"))
                return "Greetings, traveler. How can I help you today?";

            if (ContainsAny(lower, "ataque", "inimigo", "perigo", "attack", "enemy", "danger"))
                return "Attention! There is a hostile presence nearby. Hold your position!";

            if (ContainsAny(lower, "item", "inventário", "comprar", "vender", "inventory", "trade"))
                return "I have rare goods. What are you looking for, wanderer?";

            if (ContainsAny(lower, "quest", "missão", "tarefa", "mission"))
                return "An urgent task has been entrusted to me. Are you capable of helping?";

            if (ContainsAny(lower, "localização", "onde", "caminho", "location", "where", "path"))
                return "Follow the main road north. Beware of the forest creatures.";

            // Generic fallback — shows the start of the prompt for debug
            string preview = prompt.Length > 60 ? prompt.Substring(0, 60) + "..." : prompt;
            return $"[MOCK] Prompt received: \"{preview}\"";
        }

        private static bool ContainsAny(string text, params string[] keywords)
        {
            foreach (string kw in keywords)
                if (text.Contains(kw)) return true;
            return false;
        }
    }
}
