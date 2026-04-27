using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace NPC_AI.Memory
{
    /// Persists NPC memories to disk between sessions as JSON.
    /// Files are stored in Application.persistentDataPath/npc_memories/{npcId}.json
    public static class MemorySerializer
    {
        public static async Task SaveAsync(string npcId, List<MemoryEntry> entries)
        {
            await Task.Run(() =>
            {
                try
                {
                    var dir = Path.Combine(Application.persistentDataPath, "npc_memories");
                    Directory.CreateDirectory(dir);
                    var path = Path.Combine(dir, $"{SanitizeId(npcId)}.json");

                    var wrapper = new MemoryListWrapper { entries = entries };
                    var json = JsonUtility.ToJson(wrapper, prettyPrint: false);
                    File.WriteAllText(path, json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MemorySerializer] Save failed for {npcId}: {ex.Message}");
                }
            });
        }

        public static async Task<List<MemoryEntry>> LoadAsync(string npcId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var path = Path.Combine(
                        Application.persistentDataPath, "npc_memories",
                        $"{SanitizeId(npcId)}.json");

                    if (!File.Exists(path)) return new List<MemoryEntry>();

                    var json = File.ReadAllText(path);
                    var wrapper = JsonUtility.FromJson<MemoryListWrapper>(json);
                    return wrapper?.entries ?? new List<MemoryEntry>();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MemorySerializer] Load failed for {npcId}: {ex.Message}");
                    return new List<MemoryEntry>();
                }
            });
        }

        private static string SanitizeId(string id) =>
            string.Concat(id.Split(Path.GetInvalidFileNameChars()));

        [Serializable]
        private class MemoryListWrapper
        {
            public List<MemoryEntry> entries;
        }
    }
}
