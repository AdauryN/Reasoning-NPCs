namespace NPC_AI.LLM
{
    public class ToolDefinition
    {
        public string Name;
        public string Description;
        
        /// Raw JSON string for the parameters schema, e.g.{"type":"object","properties":{"npc_id":{"type":"string"}},"required":["npc_id"]}
        public string ParametersJson;
    }
}
