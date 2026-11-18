using System.Collections.Generic;
using KindredSiege.Sim.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        sealed class ClassReadout
        {
            public readonly PxText[] marks = new PxText[4];
            public readonly Image[] shield = new Image[3];
            public Image[] aim;
            public PxText aimText;
            public PxText surrender;
        }
        RectTransform _classScreen;
        readonly Dictionary<string, ClassReadout> _classReadouts = new Dictionary<string, ClassReadout>();
        static readonly string[] ClassMarkNames = { "SNARE", "BLEED", "OPEN", "GUARD" };
        static Color32 ClassMarkColour(int i) => i == 0 ? PxLilac : i == 1 ? PxRed : i == 2 ? PxOrange : PxBlue;

        void DrawClassSpells(BattleSimulation sim)
        {
            if (_classScreen != _screen)
            {
                _classScreen = _screen; _classReadouts.Clear();
                if (!sim.ClassSpellsOn) return;
                foreach (var f in _figures)
                {
                    var r = new ClassReadout();
                    r.surrender = PxLabel(f.readoutRoot, 0, -19, 1);
                    for (int i = 0; i < r.marks.Length; i++)
                        r.marks[i] = PxLabel(f.readoutRoot, 0, 7, 1);
                    r.shield[0] = Panel(f.readoutRoot, 0, 0, 5 * Px, 3 * Px, PxBlue);
                    r.shield[1] = Panel(f.readoutRoot, 0, 3 * Px, 3 * Px, 2 * Px, PxBlue);
                    r.shield[2] = Panel(f.readoutRoot, 0, 5 * Px, Px, Px, PxBlue);
                    for (int i = 0; i < r.shield.Length; i++)
                        r.shield[i].rectTransform.anchoredPosition = new Vector2((-f.barWidth / 2 - 8 + i) * Px, -i * 2 * Px);
                    if (f.unit.Class == UnitClass.Marksman)
                    {
                        r.aim = new Image[24];
                        for (int i = 0; i < r.aim.Length; i++)
                        {
                            r.aim[i] = Panel(_readoutLayer, 0, 0, 4 * Px, Px, PxGold);
                            var outline = r.aim[i].gameObject.AddComponent<Outline>();
                            outline.effectColor = Color.black; outline.effectDistance = new Vector2(1, -1);
                        }
                        r.aimText = PxLabel(f.readoutRoot, 0, -19, 1);
                    }
                    _classReadouts.Add(f.unit.Id, r);
                    f.tag.y = 19; f.tag.key = "";
                }
            }
            if (!sim.ClassSpellsOn) return;
            foreach (var f in _figures)
            {
                var r = _classReadouts[f.unit.Id]; var u = f.unit;
                bool visible = !ViewOver && u.Alive && !ChiefCutInVisible;
                bool guard = visible && sim.CoveredBy(u) != null;
                if (visible && u.Yielded)
                    SetPx(r.surrender, new[] { "YIELDED", "\n", sim.ChiefKitsOn?"SPARE / EXECUTE":"HOLD TO SPARE" }, new[] { PxGold, PxGold, PxGold });
                else SetPx(r.surrender, "", PxGold);
                int count = (visible && u.SnareTicks > 0 ? 1 : 0) + (visible && u.BleedTicks > 0 ? 1 : 0) +
                    (visible && u.OpeningTicks > 0 ? 1 : 0) + (guard ? 1 : 0);
                int tagY = (count > 2 ? 29 : 19)+((RoadHud||KitHud)?5:0);
                if (f.tag.y != tagY) { f.tag.y = tagY; f.tag.key = ""; }
                int slot = 0;
                for (int i = 0; i < r.marks.Length; i++)
                {
                    bool on = visible && (i == 0 ? u.SnareTicks > 0 : i == 1 ? u.BleedTicks > 0 : i == 2 ? u.OpeningTicks > 0 : guard);
                    // At most two marks on each line, with a clear gap between words.
                    int rowCount = slot < 2 ? Mathf.Min(2, count) : count - 2;
                    int x = on ? (slot % 2 * 40 - (rowCount - 1) * 20) : 0;
                    int y = (on ? 7 + slot / 2 * 10 : 7)+((RoadHud||KitHud)?5:0);
                    if (on) slot++;
                    if (r.marks[i].x != x || r.marks[i].y != y)
                    { r.marks[i].x = x; r.marks[i].y = y; r.marks[i].key = ""; }
                    SetPx(r.marks[i], on && !sim.ClearRoadOn ? ClassMarkNames[i] : "", ClassMarkColour(i));
                }
                foreach (var part in r.shield) part.enabled = guard;
                if (r.aim == null) continue;
                var target = visible && u.AimTicks > 0 ? FindFigure(u.AimTargetId) : null;
                bool aiming = target != null && target.unit.Alive;
                if(sim.RoadKitOn&&sim.FallBackTicksLeft(u.Side)>0)aiming=false;
                SetPx(r.aimText, aiming && !sim.ClearRoadOn ? (sim.RoadKitOn&&u.Steady?"STEADY ":"AIM ") + Seconds(u.AimTicks) : "", u.Side == 0 ? PxMint : PxRed);
                for (int i = 0; i < r.aim.Length; i++)
                {
                    var dot = r.aim[i]; dot.enabled = aiming;
                    if (!aiming) continue;
                    // Native whole-pixel dots, anchored to the actual attacker and target.
                    var from = new Vector2(f.feet.x, f.feet.y - 45);
                    var to = new Vector2(target.feet.x, target.feet.y - 45);
                    var point = Vector2.Lerp(from, to, (i + 1f) / (r.aim.Length + 1));
                    dot.rectTransform.anchoredPosition = new Vector2(Mathf.Round(point.x / Px) * Px, -Mathf.Round(point.y / Px) * Px);
                    dot.color = PitPresentation?PxRed:u.Side == 0 ? PxMint : PxRed;
                }
            }
        }

        string ClassFighterHint(BattleUnit unit)
        {
            if(ViewSim.TryPartOf(unit,out var part))return BeastKits.Name(part)+" / "+(unit.Alive?unit.Hp+" / "+unit.MaxHp+" HP":"DOWN")+(part==BeastPart.Heart&&ViewSim.BeastCaged?" / SEALED: BREAK RIBCAGE":" / "+BeastKits.MoveName(part));
            string hint = UnitName(unit) + "   " + (unit.Alive ? unit.Hp + " / " + unit.MaxHp + " HP" : "DOWN");
            if(RoadHud)hint+=(unit.Fled?" / FLED":" / NERVE "+unit.UnitNerve+"/"+RoadRules.UnitNerve);
            hint+=KitFighterHint(unit);
            hint+=ButcherFighterHint(ViewSim,unit);
            hint+=PalFloorFighterHint(ViewSim,unit);
            foreach(var spec in ViewSim.Setup.Side(unit.Side))if(spec.Id==unit.Id&&spec.BeastScars>0)hint+="   ROAD SCAR: "+BeastKits.ScarText(unit.Class);
            if(ViewSim.BeastHeldId==unit.Id)hint+="   HELD: break claw to free";
            if (!ViewSim.ClassSpellsOn || !unit.Alive) return hint;
            if (unit.Yielded) return hint + (ViewSim.ChiefKitsOn?
                "   KNEELING. Hold spares; Focus Fire executes. No timer.":"   YIELDED. Hold to spare; keep attacking to kill.");
            if (unit.SnareTicks > 0) hint += "   Snare " + Seconds(unit.SnareTicks);
            if (unit.BleedTicks > 0) hint += "   Bleed " + Seconds(unit.BleedTicks);
            if (unit.OpeningTicks > 0) hint += "   Opening " + Seconds(unit.OpeningTicks);
            foreach (var spec in ViewSim.Setup.Side(unit.Side))
                if (spec.Id == unit.Id)
                {
                    if(spec.Bend!=ClassBend.None)hint+="   "+ClassSpells.Name(spec.Bend)+": "+ClassSpells.Describe(spec.Bend);
                    if(ViewSim.FloorOn&&spec.Bend2!=ClassBend.None)hint+="   SLOT 2: "+ClassSpells.Name(spec.Bend2)+": "+ClassSpells.Describe(spec.Bend2);
                }
            return hint;
        }

        void ClassSpellEvent(BattleEvent e)
        {
            if (_seeking) return;
            PalFloorEvent(e);
            if(RoadSystemsHud&&!RoadHud&&e.Kind==BattleEventKind.Bond)
                Callout(e.ActorSide,"BOND FORMED",PxGold);
            if (e.Kind == BattleEventKind.Phase && !ViewSim.BeastOn)
            {
                string phase = string.IsNullOrEmpty(e.Detail) ? "SECOND PHASE" : e.Detail;
                Callout(e.ActorSide, phase + (e.Amount > 0 ? ": " + e.Amount + " RESERVES" : ""), PxOrange);
                Feed(NameOf(e.ActorId) + ": " + phase + (e.Amount > 0 ? "; " + e.Amount + " reserves join." : "."));
            }
            if (e.Kind == BattleEventKind.Yield)
            {
                Callout(e.ActorSide, "CHIEF YIELDS", PxGold);
                bool verdict=ViewSim.ChiefKitsOn;
                Teach("The chief yields. HOLD to spare him.",verdict?"FOCUS FIRE executes him. No timer.":"Keep attacking to kill him.", PxGold);
                Feed(verdict?"Chief kneels: Hold spares; Focus Fire executes. No timer.":"Chief yields: Hold spares him; keep attacking to kill.");
                if(verdict&&_playback==null){_targeting=false;_pilotOn=false;Save();}
            }
            if (e.Kind == BattleEventKind.Broken)
            {
                string broken = e.Detail == ChiefKits.RingDetail ? "RING BROKEN" :
                    e.Detail == ChiefKits.RifleDetail ? "RIFLE SHOT BROKEN" : "AIM BROKEN";
                Callout(1 - e.ActorSide, broken, PxMuted);
                Feed((e.Detail == ClassSpells.AimDetail ? "Aimed shot lost" : broken) + ": " + NameOf(e.TargetId) + ".");
            }
            if (e.Kind != BattleEventKind.Cash) return;
            // A thaw cashes an Opening without another strike. Mercy reads blood to strengthen a
            // treatment; it does not consume that blood or claim another fighter's finisher.
            bool flurry=ButcherFlurryCash(ViewSim,e);
            if(flurry||e.Detail==ClassSpells.MercyDetail)
            {
                string combo=flurry?"FROZEN FLURRY / OPENING CASHED":"MERCY WELL / BLOOD-FED TREATMENT";
                if(!RoadHud)Callout(e.ActorSide,combo,flurry?PxGold:PxMint);
                Feed(combo+": "+NameOf(e.ActorId)+" / "+NameOf(e.TargetId)+".");
                return;
            }
            if(RoadHud)return; // Road HUD stamps confirmed cash once per originating chain.
            string text = e.Detail == ClassSpells.PinDetail ? "SNARE -> PIN SHOT" :
                e.Detail == ClassSpells.OpeningMark ? "OPENING -> STRIKE" :
                e.Detail == ClassSpells.RipDetail ? "BLEED -> RIP" :
                e.Detail == ClassSpells.CoveredDetail ? "GUARD -> TREATMENT" : "";
            if (text.Length == 0) return;
            Callout(e.ActorSide, text, e.Detail == ClassSpells.CoveredDetail ? PxMint : PxGold);
            Feed(text + ": " + NameOf(e.ActorId) + " / " + NameOf(e.TargetId) + ".");
        }
    }
}

