using System.Collections.Generic;
using UnityEngine;
using KindredSiege.Battle;
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

            bool isDomainExpansion = false;
            KindredSiege.Modifiers.MutationFamily domainFamily = KindredSiege.Modifiers.MutationFamily.Void;
            if (KindredSiege.City.MythosExposure.Instance != null && KindredSiege.City.MythosExposure.Instance.Exposure >= 75)
            {
                var pendingRival = KindredSiege.Rivalry.RivalEncounterSystem.Instance?.PendingRival;
                if (pendingRival != null && pendingRival.Rank == KindredSiege.Rivalry.RivalRank.Overlord)
                {
                    isDomainExpansion = true;
                    // Hash the Rival's name to pick a deterministic family
                    int pick = Mathf.Abs(pendingRival.FullName.GetHashCode()) % 4;
                    domainFamily = (KindredSiege.Modifiers.MutationFamily)pick;
                }
            }

            // Each path gets a distinct encounter type — no duplicates
            var targetDistrict = KindredSiege.City.DistrictManager.Instance?.GetRandomUnliberatedDistrict();

            var encounterPool = new List<EncounterType>
            {
                EncounterType.Annihilation,
                EncounterType.Survival,
                EncounterType.Ambush,
                EncounterType.Ritual,
                EncounterType.Rescue,
                EncounterType.RivalHunt,
            };

            if (targetDistrict != null)
                encounterPool.Add(EncounterType.SanitySiege);

            // Shuffle
            for (int n = encounterPool.Count - 1; n > 0; n--)
            {
                int k = Random.Range(0, n + 1);
                (encounterPool[n], encounterPool[k]) = (encounterPool[k], encounterPool[n]);
            }

            // Guarantee one path is a Sanity Siege if there is a district available
            if (targetDistrict != null && !encounterPool.GetRange(0, 3).Contains(EncounterType.SanitySiege))
            {
                encounterPool[0] = EncounterType.SanitySiege;
            }

            for (int i = 0; i < 3; i++)
            {
                var encounter = encounterPool[i];

                // RivalHunt forces a rival to be present; other types use the random pool
                var rival = encounter == EncounterType.RivalHunt && activeRivals.Count > 0
                    ? activeRivals[0] // highest-rank rival
                    : activeRivals.Count > 0 && Random.value < 0.5f
                        ? activeRivals[Random.Range(0, activeRivals.Count)]
                        : null;

                _paths.Add(new ExpeditionPath
                {
                    Mutations = MutationEngine.Instance?.GenerateMutationsForPath(isDomainExpansion, domainFamily) ?? new List<MutationType>(),
                    Rival     = rival,
                    Encounter = encounter,
                    Reward    = Random.value > 0.5f ? "Standard Supplies" : "Archive Unlock",
                    IsDomainExpansion = isDomainExpansion,
                    TargetDistrict = encounter == EncounterType.SanitySiege ? targetDistrict : null
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

            GUI.Label(new Rect(px, py, lw, 25), $"PATH {num} — {EncounterTypeInfo.GetName(path.Encounter)}", _titleStyle);
            py += 22;
            GUI.Label(new Rect(px, py, lw, 20), EncounterTypeInfo.GetDescription(path.Encounter), _btnStyle);
            py += 28;

            // Mutation Block
            if (path.IsDomainExpansion)
            {
                GUI.color = new Color(0.9f, 0.1f, 0.2f);
                GUI.Label(new Rect(px, py, lw, 30), "⚠ ANOMALY DETECTED ⚠", _titleStyle);
                GUI.color = Color.white;
                py += 35;
                GUI.Label(new Rect(px, py, lw, 30), "DOMAIN EXPANSION", _titleStyle);
                py += 35;
            }
            else
            {
                GUI.Label(new Rect(px, py, lw, 20), "Reality Mutations:", _mutStyle);
                py += 22;
            }
            
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
                    GUI.color = path.IsDomainExpansion ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.8f, 0.3f, 0.9f); // Red for domain, Purple for normal
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

            MutationEngine.Instance?.SetActiveMutations(path.Mutations);

            var bm = BattleManager.Instance;
            var gm = KindredSiege.Core.GameManager.Instance;
            if (bm != null && gm != null)
            {
                bm.SetActiveRival(path.Rival);
                bm.SetActiveEncounterType(path.Encounter);
                bm.SetTargetDistrict(path.TargetDistrict);
                gm.StartBattle();
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
