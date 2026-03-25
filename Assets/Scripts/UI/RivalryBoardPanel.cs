using UnityEngine;
using KindredSiege.Rivalry;

namespace KindredSiege.UI
{
    /// <summary>
    /// PILLAR 3: War Table / Rivalry Board UI
    /// 
    /// Allows the player to view the active dominators (Rivals) mapping the world.
    /// Exposes hidden Nemesis mechanics: Ranks, Horror Rating, Traits, and Grudges.
    /// </summary>
    public class RivalryBoardPanel : MonoBehaviour
    {
        public static RivalryBoardPanel Instance { get; private set; }

        private bool _visible;
        private Vector2 _scrollPos;
        private bool _stylesReady;

        // Layout
        private const int PanelW = 700;
        private const int PanelH = 500;
        private const int Margin = 20;

        // Styles
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _nameStyle;
        private GUIStyle _descStyle;
        private GUIStyle _grudgeStyle;
        private GUIStyle _btnStyle;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Show() => _visible = true;
        public void Hide() => _visible = false;

        private void OnGUI()
        {
            if (!_visible) return;

            EnsureStyles();

            // Dark backdrop
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int px = (Screen.width - PanelW) / 2;
            int py = (Screen.height - PanelH) / 2;

            GUI.Box(new Rect(px, py, PanelW, PanelH), GUIContent.none, _panelStyle);

            GUI.Label(new Rect(px + Margin, py + Margin, PanelW - 100, 30), "THE WAR TABLE (Active Dominions)", _titleStyle);

            if (GUI.Button(new Rect(px + PanelW - 100, py + Margin, 80, 25), "Close", _btnStyle))
            {
                Hide();
            }

            var engine = RivalryEngine.Instance;
            if (engine == null || engine.ActiveRivals == null || engine.ActiveRivals.Count == 0)
            {
                GUI.Label(new Rect(px + Margin, py + 80, PanelW - Margin * 2, 30), "No active rivals yet. The frontier is quiet.", _descStyle);
                return;
            }

            var rivals = engine.GetActiveRivals(); // Sorted by rank

            int scrollH = rivals.Count * 140; // Approx height per rival
            Rect viewRect = new Rect(px + Margin, py + 70, PanelW - Margin * 2, PanelH - 90);
            Rect contentRect = new Rect(0, 0, PanelW - Margin * 2 - 20, scrollH);

            _scrollPos = GUI.BeginScrollView(viewRect, _scrollPos, contentRect);

            int iy = 0;
            foreach (var rival in rivals)
            {
                // Background block for rival
                GUI.color = GetRankColor(rival.Rank);
                GUI.DrawTexture(new Rect(0, iy, contentRect.width, 130), Texture2D.whiteTexture);
                GUI.color = Color.white;

                int ix = 10;
                int ry = iy + 10;

                // Name & Rank
                GUI.Label(new Rect(ix, ry, contentRect.width - 20, 25), $"{rival.FullName} [{rival.Rank}]", _nameStyle);
                
                // Stats
                string stats = $"HP: {rival.BaseHP}  |  Dmg: {rival.BaseDamage}  |  Dread: {rival.DreadPower}  |  Horror Rating: {rival.HorrorRating}";
                if (rival.IsUndying) stats += "  |  UNDYING";
                GUI.Label(new Rect(ix, ry + 25, contentRect.width - 20, 20), stats, _descStyle);

                // Traits & Weakness
                string traitList = rival.Traits.Count > 0 ? string.Join(", ", rival.Traits) : "None";
                GUI.Label(new Rect(ix, ry + 45, contentRect.width - 20, 20), $"Traits: {traitList}", _descStyle);
                GUI.Label(new Rect(ix, ry + 65, contentRect.width - 20, 20), $"Weakness: {rival.Weakness}", _descStyle);

                // Grudge warning
                if (rival.Memory.HasGrudge)
                {
                    GUI.Label(new Rect(ix, ry + 85, contentRect.width - 20, 20), 
                        $"VENDETTA TARGET: {rival.Memory.GrudgeTargetUnitName.ToUpper()} (will focus attack)", _grudgeStyle);
                }
                else
                {
                    GUI.Label(new Rect(ix, ry + 85, contentRect.width - 20, 20), "No active grudges.", _descStyle);
                }

                iy += 140;
            }

            GUI.EndScrollView();
        }

        private Color GetRankColor(RivalRank rank) => rank switch
        {
            RivalRank.Grunt => new Color(0.15f, 0.15f, 0.18f, 0.8f),
            RivalRank.Lieutenant => new Color(0.2f, 0.25f, 0.2f, 0.8f),
            RivalRank.Captain => new Color(0.3f, 0.2f, 0.15f, 0.8f),
            RivalRank.Overlord => new Color(0.3f, 0.1f, 0.1f, 0.8f),
            _ => new Color(0.1f, 0.1f, 0.1f, 0.8f)
        };

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.normal.background = MakeTex(new Color(0.05f, 0.05f, 0.08f, 0.95f));

            _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            _titleStyle.normal.textColor = new Color(0.9f, 0.8f, 0.5f);

            _nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            _nameStyle.normal.textColor = Color.white;

            _descStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            _descStyle.normal.textColor = new Color(0.7f, 0.7f, 0.75f);

            _grudgeStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            _grudgeStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);

            _btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 13 };
        }

        private static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}
