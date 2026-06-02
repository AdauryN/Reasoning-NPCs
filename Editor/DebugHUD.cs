using NPC_AI.Brain;
using NPC_AI.Core;
using NPC_AI.PlayerAnalysis;
using UnityEngine;

namespace NPC_AI.Demo
{

    /// Onscreen debug overlay showing the live NPC AI state.
    public class DebugHUD : MonoBehaviour
    {
        private string _lastReasoning = "—";
        private string _lastAction = "—";
        private float _lastLatencyMs;
        private int _lastPromptTokens;
        private string _playerStyle = "Unknown";
        private float _playerAttackRate;
        private bool _lastWasFallback;
        private string _lastNpcId = "—";

        private readonly GUIStyle _boxStyle = new GUIStyle();
        private bool _stylesInitialized;

        private void OnEnable()
        {
            NPCAIEvents.OnDecisionMade += OnDecision;
            NPCAIEvents.OnPlayerProfileUpdated += OnPlayerProfile;
            NPCAIEvents.OnInferenceCompleted += OnInference;
        }

        private void OnDisable()
        {
            NPCAIEvents.OnDecisionMade -= OnDecision;
            NPCAIEvents.OnPlayerProfileUpdated -= OnPlayerProfile;
            NPCAIEvents.OnInferenceCompleted -= OnInference;
        }

        private void OnDecision(NPCController npc, ActionCommand cmd)
        {
            _lastNpcId = npc.NpcId;
            _lastAction = cmd.ActionType;
            _lastReasoning = cmd.Reasoning ?? "—";
            _lastWasFallback = cmd.IsFromFallback;
        }

        private void OnPlayerProfile(PlayerBehaviorSummary summary)
        {
            _playerStyle = summary.DominantStyle.ToString();
            _playerAttackRate = summary.AttackFrequency;
        }

        private void OnInference(string npcId, float latencyMs)
        {
            _lastLatencyMs = latencyMs;
        }

        private void OnGUI()
        {
            InitStyles();

            var rect = new Rect(10, 10, 380, 260);
            GUI.Box(rect, GUIContent.none, _boxStyle);

            GUILayout.BeginArea(new Rect(18, 18, 364, 244));

            GUILayout.Label("<b>NPC AI Debug</b>", RichLabel());
            GUILayout.Space(4);

            GUILayout.Label($"<b>NPC:</b> {_lastNpcId}", RichLabel());
            GUILayout.Label($"<b>Last Action:</b> {_lastAction}{(_lastWasFallback ? " (fallback)" : "")}", RichLabel());
            GUILayout.Space(2);
            GUILayout.Label("<b>Reasoning:</b>", RichLabel());
            GUILayout.Label(Truncate(_lastReasoning, 200), RichLabelWrapped());
            GUILayout.Space(4);
            GUILayout.Label($"<b>Inference:</b> {_lastLatencyMs:F0} ms", RichLabel());
            GUILayout.Space(4);
            GUILayout.Label("<b>── Player Profile ──</b>", RichLabel());
            GUILayout.Label($"<b>Style:</b> {_playerStyle}  |  <b>Attack rate:</b> {_playerAttackRate:F2}/sec", RichLabel());
            GUILayout.Space(6);

            GUILayout.EndArea();
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;
            _boxStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.72f));
        }

        private static GUIStyle RichLabel()
        {
            var s = new GUIStyle(GUI.skin.label);
            s.richText = true;
            s.normal.textColor = Color.white;
            s.fontSize = 12;
            return s;
        }

        private static GUIStyle RichLabelWrapped()
        {
            var s = new GUIStyle(GUI.skin.label);
            s.richText = true;
            s.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            s.fontSize = 11;
            s.wordWrap = true;
            return s;
        }

        private static string Truncate(string s, int max) =>
            s != null && s.Length > max ? s.Substring(0, max) + "…" : s ?? "—";

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
