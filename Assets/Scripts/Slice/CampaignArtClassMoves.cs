using System.Collections;
using System.Collections.Generic;
using KindredSiege.Sim.Battle;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // Claude's handed-over class moves, packed without redrawing pixels. Both bands
        // have their own authored colours; they are not mirrored or recoloured copies.
        bool ClassMovesOn => TablePresentation && ViewSim.LeadersRulesOn;
        readonly List<ReviewFx> _classMoveFx = new List<ReviewFx>();
        Beat _classMoveBeat;
        Figure _classBlink;
        Vector2 _classBlinkHome, _classBlinkContact;
        float _classBlinkOut, _classBlinkBack;

        IEnumerator PreloadClassMoves()
        {
            if (!ClassMovesOn) yield break;
            foreach (string side in new[] { "ours", "theirs" })
                foreach (string move in new[] { "strike", "aim", "treat", "rise", "jaws" })
                {
                    string key = "Table42Review/ClassMoves/" + side + "/" + move;
                    bool cached = _clips.ContainsKey(key); LoadClip(key);
                    if (!cached) yield return null;
                }
        }

        void ClearClassMoves()
        { _classMoveFx.Clear(); _classMoveBeat = null; _classBlink = null; }

        ReviewFx ClassMoveFx(string move, Figure target, int side, float delay = 0,
            float duration = -1, Vector2? point = null)
        {
            var fx = SpawnReviewFx("ClassMoves/" + (side == 0 ? "ours" : "theirs") + "/" + move,
                target, delay, duration: duration, point: point);
            _classMoveFx.Add(fx); return fx;
        }

        static bool ClassStrike(BattleEvent e) => e.Kind == BattleEventKind.Hit &&
            e.ActorClass == (int)UnitClass.Skirmisher && e.Detail == ClassSpells.StrikeDetail;

        // Start the authored windup art with the actual queued blow, not when the
        // simulation emits it (which may be several presentation beats earlier).
        void PrepareClassMoves()
        {
            if (!ClassMovesOn || _seeking || _beat == null || _beat == _classMoveBeat) return;
            var b = _classMoveBeat = _beat;
            if (b.landed || b.actor == null || b.target == null) return;
            float untilContact = Mathf.Max(0, b.lead - _beatAge);
            if (b.blow.Detail.StartsWith("aimed", System.StringComparison.Ordinal))
            {
                var ring = ClassMoveFx("aim", b.target, b.actor.unit.Side, duration: untilContact + .32f);
                // Five windup frames reach the contact frame at the screen's contact.
                ring.phase = Mathf.Max(0, .4f - untilContact);
            }
            if (!ClassStrike(b.blow)) return;
            _classBlink = b.actor; _classBlinkHome = BeatNow(b.actor);
            _classBlinkContact = b.target.feet;
            _classBlinkContact.x -= Mathf.Sign(b.target.feet.x - b.actor.feet.x) * BeatGap;
            _classBlinkOut = _visualTime + Mathf.Max(0, untilContact - .08f);
            _classBlinkBack = _visualTime + untilContact + .24f;
            _beatRuns.Remove(b.actor); // Only this confirmed Strike replaces the normal run.
            var puff = ClassMoveFx("strike", null, b.actor.unit.Side, point: _classBlinkHome, duration: .32f);
            puff.phase = .4f;
        }

        void DrawClassMovePosition(Figure f)
        {
            if (f != _classBlink) return;
            if (_visualTime >= _classBlinkBack)
            {
                var puff = ClassMoveFx("strike", null, f.unit.Side, point: _classBlinkContact, duration: .32f);
                puff.phase = .4f; _classBlink = null;
                f.root.anchoredPosition = new Vector2(f.feet.x, -f.feet.y);
            }
            else
            {
                var p = _visualTime < _classBlinkOut ? _classBlinkHome : _classBlinkContact;
                f.root.anchoredPosition = new Vector2(Mathf.Round(p.x), -Mathf.Round(p.y));
            }
            if (f.readoutRoot != null)
                f.readoutRoot.anchoredPosition = new Vector2(f.root.anchoredPosition.x, -f.feet.y);
        }

        void ClassMoveEvent(BattleEvent e, Figure actor, Figure target)
        {
            if (!ClassMovesOn || _seeking || target == null || actor == null) return;
            if (ClassStrike(e)) ClassMoveFx("strike", target, actor.unit.Side);
            else if (e.Kind == BattleEventKind.Heal && e.ActorClass == (int)UnitClass.Medic)
                ClassMoveFx("treat", target, actor.unit.Side);
            else if (e.Kind == BattleEventKind.Hit && e.Detail == BattleSimulation.TrapDetail)
                ClassMoveFx("jaws", target, actor.unit.Side, duration: 1.51f);
            else if (e.Kind == BattleEventKind.Guard)
            {
                var ring = ClassMoveFx("rise", actor, actor.unit.Side, duration: .76f); ring.fixedFrame = 0;
                var wall = ClassMoveFx("rise", target, actor.unit.Side, duration: .8f); wall.phase = .08f;
            }
            else if (e.Kind == BattleEventKind.Hit && e.Detail.StartsWith("aimed", System.StringComparison.Ordinal) &&
                (_classMoveBeat == null || _classMoveBeat.blow.Tick != e.Tick ||
                    _classMoveBeat.blow.ActorId != e.ActorId || _classMoveBeat.blow.TargetId != e.TargetId))
            { var ring = ClassMoveFx("aim", target, actor.unit.Side, duration: .32f); ring.phase = .4f; }
        }

        void OrderClassMoves()
        {
            // BeatOrder moves fighter roots as runs cross. Re-anchor FX layers after
            // that operation so the blade/guard rings remain beneath their fighters.
            for (int i = _classMoveFx.Count - 1; i >= 0; i--)
            {
                var fx = _classMoveFx[i];
                if (fx.image == null) { _classMoveFx.RemoveAt(i); continue; }
                if (fx.clip.sheet.layer == "under")
                {
                    int first = int.MaxValue;
                    foreach (var f in _figures) first = Mathf.Min(first, f.root.GetSiblingIndex());
                    if (first != int.MaxValue) fx.image.transform.SetSiblingIndex(first);
                }
                else fx.image.transform.SetSiblingIndex(_readoutLayer.GetSiblingIndex());
            }
        }
    }
}
