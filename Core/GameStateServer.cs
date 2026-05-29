using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NPC_AI.PlayerAnalysis;
using UnityEngine;

namespace NPC_AI.Core
{
    /// Serves live Unity game state over HTTP so the MCP server can read it.
    /// Responds to GET http://localhost:7654/state with a JSON snapshot.
    public class GameStateServer : MonoBehaviour
    {
        [SerializeField] private PlayerBehaviorTracker behaviorTracker;
        [SerializeField] private NPCController npcController;
        [SerializeField] private Transform playerTransform;

        private HttpListener _listener;
        private CancellationTokenSource _cts;

        private readonly object _lock = new object();
        private StateSnapshot _snapshot;

        private struct StateSnapshot
        {
            public float NpcHealth;
            public float PlayerX, PlayerY, PlayerZ;
            public int EnemiesNearby;
            public float DistanceToPlayer;
            public string DominantStyle;
            public string RecentTrend;
            public float AttackFrequency;
            public float DodgeFrequency;
            public bool PrefersRanged;
            public float HealThreshold;
            public float FlankingTendency;
        }

        private void Start()
        {
            if (behaviorTracker == null)
                behaviorTracker = FindAnyObjectByType<PlayerBehaviorTracker>();
            if (npcController == null)
                npcController = FindAnyObjectByType<NPCController>();

            _cts = new CancellationTokenSource();

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://localhost:7654/");
                _listener.Start();
                Debug.Log("[GameStateServer] Listening on http://localhost:7654/");
                _ = ListenAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameStateServer] Failed to start: {ex.Message}");
            }
        }

        private void Update()
        {
            var snap = new StateSnapshot
            {
                DominantStyle = "Unknown",
                RecentTrend = "Unknown"
            };

            if (npcController != null)
            {
                snap.NpcHealth = npcController.HealthPct;

                if (playerTransform != null)
                {
                    snap.PlayerX = playerTransform.position.x;
                    snap.PlayerY = playerTransform.position.y;
                    snap.PlayerZ = playerTransform.position.z;
                    snap.DistanceToPlayer = Vector3.Distance(
                        npcController.transform.position,
                        playerTransform.position);
                }

                snap.EnemiesNearby = 1;
            }

            if (behaviorTracker != null)
            {
                var summary = behaviorTracker.GetSummary();
                snap.DominantStyle    = summary.DominantStyle.ToString();
                snap.RecentTrend      = summary.RecentTrend.ToString();
                snap.AttackFrequency  = summary.AttackFrequency;
                snap.DodgeFrequency   = summary.DodgeFrequency;
                snap.PrefersRanged    = summary.PrefersRanged;
                snap.HealThreshold    = summary.HealThreshold;
                snap.FlankingTendency = summary.FlankingTendency;
            }

            lock (_lock) { _snapshot = snap; }
        }

        private async Task ListenAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await Task.Run(() => _listener.GetContext(), ct);
                }
                catch (OperationCanceledException) { break; }
                catch (HttpListenerException) { break; }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GameStateServer] Listener error: {ex.Message}");
                    continue;
                }

                _ = Task.Run(() => HandleRequest(ctx), ct);
            }
        }

        private void HandleRequest(HttpListenerContext ctx)
        {
            try
            {
                StateSnapshot snap;
                lock (_lock) { snap = _snapshot; }

                var ic = System.Globalization.CultureInfo.InvariantCulture;

                var json = "{"
                    + $"\"npcHealth\":{snap.NpcHealth.ToString("F2", ic)},"
                    + $"\"playerPosition\":{{\"x\":{snap.PlayerX.ToString("F1", ic)},\"y\":{snap.PlayerY.ToString("F1", ic)},\"z\":{snap.PlayerZ.ToString("F1", ic)}}},"
                    + $"\"enemiesNearby\":{snap.EnemiesNearby},"
                    + $"\"distanceToPlayer\":{snap.DistanceToPlayer.ToString("F1", ic)},"
                    + "\"playerBehavior\":{"
                    + $"\"dominantStyle\":\"{snap.DominantStyle}\","
                    + $"\"recentTrend\":\"{snap.RecentTrend}\","
                    + $"\"attackFrequency\":{snap.AttackFrequency.ToString("F2", ic)},"
                    + $"\"dodgeFrequency\":{snap.DodgeFrequency.ToString("F2", ic)},"
                    + $"\"prefersRanged\":{(snap.PrefersRanged ? "true" : "false")},"
                    + $"\"healThreshold\":{snap.HealThreshold.ToString("F2", ic)},"
                    + $"\"flankingTendency\":{snap.FlankingTendency.ToString("F2", ic)}"
                    + "}}";

                var bytes = Encoding.UTF8.GetBytes(json);
                ctx.Response.ContentType = "application/json";
                ctx.Response.ContentLength64 = bytes.Length;
                ctx.Response.StatusCode = 200;
                ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameStateServer] Response error: {ex.Message}");
            }
            finally
            {
                ctx.Response.Close();
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            try { _listener?.Stop(); _listener?.Close(); } catch { }
        }
    }
}
