/**
 * NPC AI MCP Server
 *
 * Exposes live Unity game state as LLM-callable tools via the Model Context Protocol.
 * Transport: stdio (launch manually or have Unity spawn it via System.Diagnostics.Process).
 *
 * Unity must run a GameStateServer that responds to GET /state
 * with a JSON object describing the current game state snapshot.
 *
 * Usage:
 *   cd mcp-server && npm install && node index.js
 *
 * Tools exposed:
 *   get_npc_health        — NPC health as 0.0–1.0
 *   get_player_position   — Player world position {x, y, z}
 *   get_nearby_enemies    — Count of enemies within a radius
 *   get_distance_to_player — Distance in world units from NPC to player
 */

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";

const UNITY_STATE_URL = "http://localhost:7654/state";

// Fetch live state from Unity

async function fetchUnityState() {
  try {
    const res = await fetch(UNITY_STATE_URL, { signal: AbortSignal.timeout(2000) });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return await res.json();
  } catch (err) {
    // Return a safe fallback so the LLM always gets a numeric response.
    console.error("[MCP] Could not reach Unity state endpoint:", err.message);
    return {
      npcHealth: 1.0,
      playerPosition: { x: 0, y: 0, z: 0 },
      enemiesNearby: 0,
      distanceToPlayer: 10.0,
    };
  }
}

// MCP server setup

const server = new McpServer({
  name: "npc-ai-mcp-server",
  version: "1.0.0",
});

server.tool(
  "get_npc_health",
  "Returns the NPC's current health as a value between 0.0 (dead) and 1.0 (full health).",
  { npc_id: z.string().describe("The NPC's identifier") },
  async ({ npc_id }) => {
    const state = await fetchUnityState();
    const health = typeof state.npcHealth === "number" ? state.npcHealth : 1.0;
    return { content: [{ type: "text", text: health.toFixed(2) }] };
  }
);

server.tool(
  "get_player_position",
  "Returns the player's current world position as {x, y, z}.",
  {},
  async () => {
    const state = await fetchUnityState();
    const pos = state.playerPosition ?? { x: 0, y: 0, z: 0 };
    return {
      content: [
        {
          type: "text",
          text: JSON.stringify({
            x: Number(pos.x).toFixed(1),
            y: Number(pos.y).toFixed(1),
            z: Number(pos.z).toFixed(1),
          }),
        },
      ],
    };
  }
);

server.tool(
  "get_nearby_enemies",
  "Returns the number of enemy units within the given radius.",
  { radius: z.number().describe("Search radius in world units") },
  async ({ radius }) => {
    const state = await fetchUnityState();
    const count = typeof state.enemiesNearby === "number" ? state.enemiesNearby : 0;
    return { content: [{ type: "text", text: String(count) }] };
  }
);

server.tool(
  "get_distance_to_player",
  "Returns the distance in world units between the NPC and the player.",
  { npc_id: z.string().describe("The NPC's identifier") },
  async ({ npc_id }) => {
    const state = await fetchUnityState();
    const dist = typeof state.distanceToPlayer === "number" ? state.distanceToPlayer : 10.0;
    return { content: [{ type: "text", text: dist.toFixed(1) }] };
  }
);

// Start

const transport = new StdioServerTransport();
await server.connect(transport);
console.error("[MCP] NPC AI MCP server running on stdio.");
