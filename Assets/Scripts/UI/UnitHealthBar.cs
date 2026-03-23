using UnityEngine;
using UnityEngine.UI;
using KindredSiege.Battle;
using KindredSiege.Core;

namespace KindredSiege.UI
{
    /// <summary>
    /// World-space health and sanity bars displayed above a unit during battle (GDD §HUD).
    ///
    /// Creates its own Canvas + Image bars in Awake — no prefab required.
    /// Attach to a unit GameObject or call Initialise(unit) after spawning.
    ///
    /// Bar colours:
    ///   HP     — green → red as HP falls
    ///   Sanity — purple fading by state (Resolute → Stressed → Afflicted → Broken)
    /// </summary>
    public class UnitHealthBar : MonoBehaviour
    {
        // ─── Tunables ───
        private const float BarWidth        = 1.2f;
        private const float BarHeight       = 0.12f;
        private const float BarSpacing      = 0.16f;
        private const float HeightOffset    = 1.6f;   // Units above unit pivot
        private const float CanvasScale     = 0.01f;  // World-space canvas scale

        // ─── References ───
        private UnitController _unit;
        private Transform      _canvasTransform;
        private Image          _hpFill;
        private Image          _sanityFill;
        private Text           _nameLabel;

        // Sanity state colours
        private static readonly Color ColResolute  = new Color(0.35f, 0.20f, 0.75f); // deep purple
        private static readonly Color ColStressed   = new Color(0.55f, 0.30f, 0.55f); // muted violet
        private static readonly Color ColAfflicted  = new Color(0.75f, 0.40f, 0.20f); // amber
        private static readonly Color ColBroken     = new Color(0.80f, 0.10f, 0.10f); // red

        // ════════════════════════════════════════════
        // SETUP
        // ════════════════════════════════════════════

        /// <summary>Call this after spawning a unit to initialise its health bars.</summary>
        public void Initialise(UnitController unit)
        {
            _unit = unit;
            BuildCanvas();
            EventBus.Subscribe<SanityChangedEvent>(OnSanityChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SanityChangedEvent>(OnSanityChanged);
        }

        private void BuildCanvas()
        {
            // Root canvas object — positioned above the unit
            var canvasGO = new GameObject($"HealthBar_{_unit.UnitName}");
            canvasGO.transform.SetParent(transform, false);
            canvasGO.transform.localPosition = new Vector3(0f, HeightOffset, 0f);
            canvasGO.transform.localScale    = Vector3.one * CanvasScale;

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            canvasGO.AddComponent<CanvasScaler>();

            var rt = canvasGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(BarWidth / CanvasScale, (BarHeight * 2 + BarSpacing + 0.20f) / CanvasScale);

            _canvasTransform = canvasGO.transform;

            // Unit name label
            _nameLabel = CreateLabel(canvasGO.transform, "Name", _unit.UnitName,
                new Vector2(0, (BarHeight + BarSpacing * 1.5f) / CanvasScale));

            // HP bar
            float hpY = (BarSpacing * 0.5f) / CanvasScale;
            CreateBarBackground(canvasGO.transform, "HPBg", hpY);
            _hpFill = CreateBarFill(canvasGO.transform, "HPFill", Color.green, hpY);

            // Sanity bar
            float sanityY = -(BarSpacing * 0.5f + BarHeight) / CanvasScale;
            CreateBarBackground(canvasGO.transform, "SanityBg", sanityY);
            _sanityFill = CreateBarFill(canvasGO.transform, "SanityFill", ColResolute, sanityY);

            RefreshBars();
        }

        private Image CreateBarBackground(Transform parent, string name, float yPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(BarWidth / CanvasScale, BarHeight / CanvasScale);
            rt.anchoredPosition = new Vector2(0, yPos);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            return img;
        }

        private Image CreateBarFill(Transform parent, string name, Color colour, float yPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            // Inset slightly from background
            float inset = 4f;
            float fullWidth = (BarWidth / CanvasScale) - inset * 2;
            float height = (BarHeight / CanvasScale) - inset;


            rt.sizeDelta = new Vector2(fullWidth, height);
            rt.anchoredPosition = new Vector2(0f, yPos);

            var img = go.AddComponent<Image>();
            img.color = colour;
            return img;
        }

        private Text CreateLabel(Transform parent, string name, string text, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(BarWidth / CanvasScale, 20f);
            rt.anchoredPosition = anchoredPos;

            var txt = go.AddComponent<Text>();
            txt.text      = text;
            txt.fontSize  = 14;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color     = Color.white;
            // Font defaults to Arial if available
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return txt;
        }

        // ════════════════════════════════════════════
        // RUNTIME
        // ════════════════════════════════════════════

        private void Update()
        {
            if (_unit == null || _canvasTransform == null) return;

            // Refresh bars every frame (HP changes don't have an event yet)
            RefreshBars();

            // Billboard: face the camera so bars are always readable
            if (Camera.main != null)
                _canvasTransform.LookAt(
                    _canvasTransform.position + Camera.main.transform.rotation * Vector3.forward,
                    Camera.main.transform.rotation * Vector3.up
                );

            // Hide bars when unit is dead
            _canvasTransform.gameObject.SetActive(_unit.IsAlive);
        }

        private void RefreshBars()
        {
            if (_unit == null) return;

            float inset = 4f;
            float maxWidth = (BarWidth / CanvasScale) - inset * 2;

            // HP bar — resize width based on ratio
            if (_hpFill != null)
            {
                float hpRatio = _unit.MaxHP > 0
                    ? (float)_unit.CurrentHP / _unit.MaxHP : 0f;

                var rt = _hpFill.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(maxWidth * hpRatio, rt.sizeDelta.y);
                // Offset so bar shrinks from right to left
                rt.anchoredPosition = new Vector2(-(maxWidth * (1f - hpRatio)) * 0.5f, rt.anchoredPosition.y);

                _hpFill.color = Color.Lerp(Color.red, Color.green, hpRatio);
            }

            // Sanity bar — resize width based on ratio
            if (_sanityFill != null)
            {
                float sanityRatio = _unit.MaxSanity > 0
                    ? (float)_unit.CurrentSanity / _unit.MaxSanity : 0f;

                var rt = _sanityFill.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(maxWidth * sanityRatio, rt.sizeDelta.y);
                rt.anchoredPosition = new Vector2(-(maxWidth * (1f - sanityRatio)) * 0.5f, rt.anchoredPosition.y);

                _sanityFill.color = SanityColour(_unit.SanityState);
            }
        }

        private void OnSanityChanged(SanityChangedEvent evt)
        {
            if (evt.UnitId != _unit?.UnitId) return;
            RefreshBars();
        }

        private static Color SanityColour(SanityState state)
        {
            return state switch
            {
                SanityState.Resolute  => ColResolute,
                SanityState.Stressed  => ColStressed,
                SanityState.Afflicted => ColAfflicted,
                SanityState.Broken    => ColBroken,
                _                     => Color.black
            };
        }
    }
}
