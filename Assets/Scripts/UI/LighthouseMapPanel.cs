using System.Collections.Generic;
using UnityEngine;
using KindredSiege.Modifiers;
using KindredSiege.Rivalry;

namespace KindredSiege.UI
{
    /// <summary>
    /// PILLAR 1/2/3: The Lighthouse Expedition Map
    /// Before deploying, the player must evaluate risk vs reward by looking at 
    /// the active Reality Mutations and the Rival they will face.
    /// </summary>
    public class LighthouseMapPanel : MonoBehaviour
    {
        public static LighthouseMapPanel Instance { get; private set; }

        private bool _visible;
        private bool _stylesReady;
        private List<ExpeditionPath> _paths = new();

        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _pathBoxStyle;
        private GUIStyle _mutStyle;
        private GUIStyle _btnStyle;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Show()
        {
            GeneratePaths();
            _visible = true;
        }

        public void Hide() => _visible = false;

        private void GeneratePaths()
        {
            _paths.Clear();
            var engine = RivalryEngine.Instance;
            var activeRivals = engine != null ? engine.GetActiveRivals() : new List<RivalData>();

            string[] encounters = { "Annihilation", "Survival", "Rival Hunt" };

            // Generate 3 random paths
            for (int i = 0; i < 3; i++)
            {
                var rival = activeRivals.Count > 0 
                    ? activeRivals[Random.Range(0, activeRivals.Count)] 
                    : null;

                _paths.Add(new ExpeditionPath
                {
                    Mutations = MutationEngine.Instance?.GenerateMutationsForPath() ?? new List<MutationType>(),
                    Rival = rival,
                    EncounterType = encounters[Random.Range(0, encounters.Length)],
                    Reward = Random.value > 0.5f ? "Standard Supplies" : "Archive Unlock"
                });
            }
        }

        private void OnGUI()
        {
            if (!_visible) return;
            EnsureStyles();

            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int w = 900, h = 500;
            int x = (Screen.width - w) / 2;
            int y = (Screen.height - h) / 2;

            GUI.Box(new Rect(x, y, w, h), GUIContent.none, _panelStyle);
            GUI.Label(new Rect(x + 20, y + 20, w, 30), "THE LIGHTHOUSE (Choose Your Path)", _titleStyle);

            if (GUI.Button(new Rect(x + w - 100, y + 20, 80, 30), "Cancel", _btnStyle))
            {
                Hide();
            }

            int pathW = 270;
            int px = x + 30;

            for (int i = 0; i < _paths.Count; i++)
            {
                var path = _paths[i];
                DrawPathBox(new Rect(px, y + 70, pathW, 400), path, i + 1);
                px += pathW + 20;
            }
        }

        private void DrawPathBox(Rect rect, ExpeditionPath path, int num)
        {
            GUI.Box(rect, GUIContent.none, _pathBoxStyle);

            int px = (int)rect.x + 15;
            int py = (int)rect.y + 15;
            int lw = (int)rect.width - 30;

            GUI.Label(new Rect(px, py, lw, 25), $"PATH {num}", _titleStyle);
            py += 35;

            // Mutation Block
            GUI.Label(new Rect(px, py, lw, 20), "Reality Mutations:", _mutStyle);
            py += 22;
            if (path.Mutations.Count == 0)
            {
                GUI.Label(new Rect(px, py, lw, 20), "None", _btnStyle);
                py += 25;
            }
            else
            {
                foreach (var mut in path.Mutations)
                {
                    var details = MutationEngine.Instance.GetMutationDetails(mut);
                    GUI.color = new Color(0.8f, 0.3f, 0.9f); // Mutation colour
                    GUI.Label(new Rect(px, py, lw, 20), details.Name, _btnStyle);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(px, py + 20, lw, 35), details.Desc, _btnStyle);
                    py += 60;
                }
            }

            // Rival Block
            py = (int)rect.y + 220; // Fixed start for rival info
            GUI.Label(new Rect(px, py, lw, 20), "Dominion Presence:", _mutStyle);
            py += 22;

            if (path.Rival != null)
            {
                GUI.color = new Color(0.9f, 0.4f, 0.3f);
                GUI.Label(new Rect(px, py, lw, 20), $"{path.Rival.FullName} [{path.Rival.Rank}]", _btnStyle);
                GUI.color = Color.white;
                py += 22;
                GUI.Label(new Rect(px, py, lw, 40), $"Traits: {string.Join(", ", path.Rival.Traits)}", _btnStyle);
            }
            else
            {
                GUI.Label(new Rect(px, py, lw, 20), "No Rival detected.", _btnStyle);
            }

            // Button
            int by = (int)(rect.y + rect.height - 45);
            GUI.color = new Color(0.3f, 0.8f, 0.4f);
            if (GUI.Button(new Rect(px, by, lw, 30), "ENTER THE FOG", _btnStyle))
            {
                ConfirmPath(path);
            }
            GUI.color = Color.white;
        }

        private void ConfirmPath(ExpeditionPath path)
        {
            Hide();
            
            // Lock in the rule changes
            MutationEngine.Instance?.SetActiveMutations(path.Mutations);

            // Lock in the rival
            if (KindredSiege.Core.GameManager.Instance != null && KindredSiege.Battle.BattleManager.Instance != null)
            {
                KindredSiege.Battle.BattleManager.Instance.SetActiveRival(path.Rival);
                KindredSiege.Core.GameManager.Instance.StartBattle(); // Moves state to PreBattle
            }
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            var tex = new Texture2D(1, 1); tex.SetPixel(0, 0, new Color(0.05f, 0.05f, 0.08f, 0.95f)); tex.Apply();
            _panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = tex } };

            var bx = new Texture2D(1, 1); bx.SetPixel(0, 0, new Color(0.12f, 0.12f, 0.15f, 1f)); bx.Apply();
            _pathBoxStyle = new GUIStyle(GUI.skin.box) { normal = { background = bx } };

            _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.9f, 0.85f, 0.5f) } };
            _mutStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.5f, 0.8f, 0.9f) } };
            
            _btnStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true, normal = { textColor = new Color(0.8f, 0.8f, 0.85f) } };
        }
    }
}
