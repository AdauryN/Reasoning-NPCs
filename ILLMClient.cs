using System.Threading;
using System.Threading.Tasks;

namespace LLMFramework.Core
{
    /// <summary>
    /// Contract that every LLM implementation must comply with.
    /// OpenAIClient, LocalLLMClient, MockLLMClient — all implement this interface.
    /// The LLMAgent never knows the concrete implementation, only this contract.
    /// </summary>
    public interface ILLMClient
    {
        /// <summary>
        /// Sends a prompt and returns the model's response.
        /// CancellationToken allows cancelation if the GameObject is destroyed during the wait.
        /// </summary>
        Task<LLMResponse> GenerateAsync(string prompt, CancellationToken cancellationToken = default);

        /// <summary>
        /// Indicates if the client is ready to receive requests.
        /// Useful to check before sending (e.g., API key configured, local server running).
        /// </summary>
        bool IsAvailable { get; }
    }
}
