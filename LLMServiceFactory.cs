using NPC_AI.Config;

namespace NPC_AI.LLM
{
    /// <summary>
    /// Creates the appropriate ILLMService based on the LLMConfig backend setting.
    /// Returns a singleton LLMRequestQueue so multiple NPCs share one inference pipeline.
    /// </summary>
    public static class LLMServiceFactory
    {
        private static LLMRequestQueue _sharedQueue;
        private static LLMConfig _lastConfig;

        /// <summary>
        /// Returns a shared <see cref="LLMRequestQueue"/> that serializes calls from all NPCs
        /// through a single underlying adapter. Call InitializeAsync() on the returned service
        /// before using it.
        /// </summary>
        public static ILLMService GetShared(LLMConfig config)
        {
            if (_sharedQueue != null && _lastConfig == config)
                return _sharedQueue;

            _sharedQueue?.Dispose();
            _lastConfig = config;

            

            _sharedQueue = new LLMRequestQueue(new OllamaAdapter(config));
            return _sharedQueue;
        }

        /// <summary>
        /// Creates a new (non-shared) adapter. Use this when you need an isolated instance,
        /// e.g. for a dedicated embedding model.
        /// </summary>
        public static ILLMService Create(LLMConfig config) =>
            new OllamaAdapter(config);
    }
}
