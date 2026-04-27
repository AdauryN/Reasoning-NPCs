namespace NPC_AI.LLM
{
    public interface IToolHandler
    {
        string HandleToolCall(string toolName, string argsJson);
    }
}
