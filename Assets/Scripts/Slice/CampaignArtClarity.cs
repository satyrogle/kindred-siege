using System;
using System.Collections.Generic;
using System.Text;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // One ranked read from the sim. The cache belongs to a screen AND simulation instance,
        // so a replay seek backwards or a new battle cannot retain another fight's answers.
        RectTransform _clarityScreen, _threatPanel;
        PxText _threatHeading, _threatTarget, _threatAnswers, _threatOthers, _orderReceipt;
        BattleSimulation _claritySim;
        long _threatTick = -1, _receiptTick = -1;
        int _threatEvents = -1, _receiptEvents = -1;
        bool _receiptStopped;
        IReadOnlyList<Threat> _threatReads = Array.Empty<Threat>();
        IReadOnlyList<OrderResult> _orderReads = Array.Empty<OrderResult>();
        Threat CurrentThreat => _threatReads.Count == 0 ? Threat.None : _threatReads[0];

        void ReadClarity(BattleSimulation sim)
        {
            if (_claritySim != sim)
            {
                ResetHoldFeedback();
                _claritySim = sim; _threatTick = _receiptTick = -1;
                _threatEvents = _receiptEvents = -1;
                _orderReads = Array.Empty<OrderResult>();
            }
            if(sim.CurrentTick<_threatTick)ResetHoldFeedback();
            if (_threatTick != sim.CurrentTick || _threatEvents != sim.Events.Count)
            {
                _threatReads = sim.Threats(0); // first entry is exactly TopThreat, no frontend ranking
                _threatTick = sim.CurrentTick; _threatEvents = sim.Events.Count;
            }
            // Results can become final without a new event. Refresh on events or at 4 Hz in sim time,
            // never on every rendered frame; a paused command at the same tick still invalidates.
            bool receiptEvent=false;
            for(int i=Math.Max(0,_receiptEvents);i<sim.Events.Count;i++)
            {
                var k=sim.Events[i].Kind;
                if(k==BattleEventKind.Reaction || k==BattleEventKind.Directive || k==BattleEventKind.Parry || k==BattleEventKind.Land || k==BattleEventKind.Release ||
                    (k==BattleEventKind.Hit&&sim.Events[i].ActorSide==0))receiptEvent=true;
            }
            _receiptEvents=sim.Events.Count;
            if (receiptEvent || _receiptStopped != (sim.IsFinished || sim.AwaitingVerdict) || _receiptTick < 0 || sim.CurrentTick < _receiptTick ||
                sim.CurrentTick - _receiptTick >= Math.Max(1, SimClock.TicksPerSecond / 4))
            {
                _orderReads = sim.OrderResults(0);
                _receiptStopped = sim.IsFinished || sim.AwaitingVerdict;
                _receiptTick = sim.CurrentTick; _receiptEvents = sim.Events.Count;
            }
        }

        string ThreatAnswers(BattleSimulation sim, Threat t, bool detailed = false)
        {
            if (t.IsNone) return "No incoming tell. Watch the field; save points for the next beat.";
            var choices = new List<string>();
            void Add(ThreatAnswer flag, Directive order, string label)
            {
                if ((t.Answers & flag) == 0) return;
                bool allowed = sim.CanUse(0, order, out string why);
                if (order == Directive.FallBack && _playback == null && _fight != null && _fight.Encounter.NoFallBack)
                { allowed = false; why = "not in this fight"; }
                choices.Add(label + (allowed ? "" : detailed ? " (" + why + ")" : " [unavailable]"));
            }
            if ((t.Answers & ThreatAnswer.Parry) != 0)
                Add(ThreatAnswer.Parry, Directive.Hold, _playback!=null ? "Hold: recorded answer" : _pilotOn ? "Auto: timed defence" : _paused ? "1 Hold: resume to parry" :
                    t.FakeFirst ? "1 Hold: fake first" : t.WaitsOutHold ? "1 Hold: this move waits" :
                    sim.HoldRefreshTicksLeft(0)>0 ? HoldRecoveryCue(sim) : t.ParryWindow ? "1 Parry available" : "1 Time Hold");
            else Add(ThreatAnswer.Hold, Directive.Hold, "1 Hold");
            Add(ThreatAnswer.FallBack, Directive.FallBack, "2 Fall Back");
            Add(ThreatAnswer.FocusFire, Directive.FocusFire, "3 Focus " + NameOf(t.FocusId));
            Add(ThreatAnswer.Unleash, Directive.Unleash, "4 Unleash");
            if (detailed) foreach (var id in sim.ThreatAnswerers(0, t)) choices.Add(NameOf(id) + " can help automatically");
            if(!detailed)
            {
                var automatic=new List<string>();
                foreach(var flag in new[]{ThreatAnswer.Warden,ThreatAnswer.Medic,ThreatAnswer.Trapper,ThreatAnswer.Marksman})
                    if((t.Answers&flag)!=0)automatic.Add(flag.ToString());
                if(automatic.Count>0)choices.Add("Auto: "+string.Join(" / ",automatic));
            }
            if (choices.Count == 0) choices.Add("No listed answer currently available");
            return string.Join(" / ", choices);
        }

        void DrawClarity(BattleSimulation sim)
        {
            ReadClarity(sim);
            if(CleanCombatHud){DrawCleanThreat(sim);return;}
            if (_clarityScreen != _screen)
            {
                _clarityScreen = _screen;
                _threatPanel = Rect(_readoutLayer, "Priority threat", 0, 0, W, H);
                // Beasts keep their selectable part cards. Other chief panels stay in their own scope.
                int y = sim.BeastOn ? 161 : 102;
                Panel(_threatPanel, 22*Px, y*Px, 676*Px, (sim.BeastOn&&sim.TightParryOn?32:70)*Px, new Color(.06f,.05f,.07f,.94f));
                _threatHeading = PxLabel(_threatPanel, 28, y+5);
                _threatTarget = PxLabel(_threatPanel, 28, y+18);
                _threatAnswers = PxLabel(_threatPanel, 28, y+34);
                _threatOthers = PxLabel(_threatPanel, 28, y+46);
                _orderReceipt = PxLabel(_threatPanel, 28, y+57);
                PxButton("fight-read", "DETAILS", 620, y+3, 70, 14, ShowClarityDetails, _threatPanel);
                if(sim.Kit(1)==ChiefKit.Bloodridge || sim.Kit(1)==ChiefKit.Skullpeak)
                    PxButton("chief-moves","MOVES",620,y+18,70,14,ShowChiefMoves,_threatPanel);
            }
            _threatPanel.gameObject.SetActive(!ViewOver && !sim.AwaitingVerdict && !ChiefCutInVisible);
            // Preserve forecast target markers and the MOVES modal, but replace the competing text wall.
            if (_tacticsPanel != null) _tacticsPanel.gameObject.SetActive(false);
            var t = CurrentThreat;
            if(sim.BeastOn&&sim.TightParryOn)
            {
                SetPx(_threatHeading,t.IsNone?"SQUAD ATTACKING":Fit(t.Name.ToUpperInvariant(),578),PxBone);
                string answers=sim.WhiffTicksLeft(0)>0?"WHIFF / VULNERABLE "+Seconds(sim.WhiffTicksLeft(0)):
                    _paused?"PAUSED / SPACE TO RESUME":t.IsNone?"3 FOCUS A PART":
                    ((t.Answers&ThreatAnswer.Parry)!=0?"1 PARRY / WATCH ATTACKER":"")+
                    ((t.Answers&ThreatAnswer.FallBack)!=0?" / 2 DODGE":"")+
                    ((t.Answers&ThreatAnswer.FocusFire)!=0?" / 3 FOCUS":"");
                SetPx(_threatTarget,Fit(answers,578),PxMint);
                SetPx(_threatAnswers,"",PxMuted);SetPx(_threatOthers,"",PxMuted);SetPx(_orderReceipt,"",PxMuted);
                return;
            }
            string glory = sim.GloryOn(0) ? " / GLORY " + ChiefKits.Letter(sim.GloryRankOf(0)) : "";
            string beat = t.BeatTicks >= 0 ? " / BEAT " + Seconds(t.BeatTicks) : " / " + Seconds(t.TicksLeft) + " LEFT";
            SetPx(_threatHeading, Fit(t.IsNone ? "WATCH THEIR NEXT MOVE" + glory : "ANSWER FIRST: " + t.Name + beat + glory, 578), LiveParryCue(sim,t) ? PxGold : PxYellow);
            SetPx(_threatTarget, Fit(t.IsNone ? "Fighters attack automatically. Your orders change the fight." :
                NameOf(t.ActorId) + " -> " + (t.TargetId.Length > 0 ? NameOf(t.TargetId) : "AREA / VARIABLE TARGET") +
                " / " + (t.Amount > 0 ? "~" + t.Amount + " HP before armour / misses" : "Effect / no direct HP estimate"), 578), PxBone);
            SetPx(_threatAnswers, Fit(ThreatAnswers(sim, t), 660), PxMint);
            var small = new List<string>();
            for (int i=1; i<Math.Min(3,_threatReads.Count); i++) small.Add(_threatReads[i].Name + " " + Seconds(_threatReads[i].TicksLeft));
            if (_threatReads.Count > 3) small.Add("+"+(_threatReads.Count-3)+" in DETAILS");
            bool delayed=t.WaitsOutHold, feint=t.FakeFirst;
            string rhythm=(delayed?"*May delay once. ":"")+(feint?"*May feint. ":"");
            string warning = rhythm.Length>0 ? rhythm+"Follow the real beat." : sim.ParryCenterOn ?
                sim.SquadFirstOn&&!t.Duel?"Hold guards your front row. Your squad can earn points through marks.":"Early Hold may reduce damage but can cost nerve. Paused Hold cannot parry." : "These are the recorded fight's rules.";
            SetPx(_threatOthers, Fit(small.Count > 0 ? rhythm+"ALSO: " + string.Join(" / ",small) : warning, 660), PxMuted);
            OrderResult last = _orderReads.Count > 0 ? _orderReads[_orderReads.Count-1] : OrderResult.None;
            SetPx(_orderReceipt, Fit(last.IsNone ? (_playback!=null?"No commanded orders at this recorded moment.":"ORDER FEEDBACK: give an order to see what it did.") : Receipt(last),660),
                last.Helped ? PxMint : last.Final ? PxOrange : PxBone);
            DrawLearningThreat(sim,t);
        }

        static string Receipt(OrderResult r)
        {
            string head = OrderName(r.Order) + (r.Paused ? " (paused)" : "") + ": ";
            if(r.Order==Directive.Unleash)
            {
                bool blocked=false;int damage=0;
                foreach(var effect in r.Effects)
                {blocked|=effect.Kind==OrderEffectKind.ScareBlocked;if(effect.Kind==OrderEffectKind.Damage)damage+=effect.Amount;}
                if(blocked)return head+"+"+(BattleSimulation.UnleashDamagePercent-100)+"% DMG / SCARE BLOCKED"+(damage>0?" / "+damage+" HIT":"");
            }
            if (r.Effects.Count > 0)
            {
                // Prefer the newest concrete answer over aggregate damage; preserve partial success.
                OrderEffect effect = r.Effects[r.Effects.Count-1];
                for (int i=r.Effects.Count-1;i>=0;i--) if(r.Effects[i].Kind!=OrderEffectKind.Damage) { effect=r.Effects[i]; break; }
                string what = effect.Kind == OrderEffectKind.Damage ? effect.Amount + " damage dealt while active" :
                    effect.Kind == OrderEffectKind.Soaked ? "SOAKED " + effect.Move + (effect.CouldParry ? "; fresh live Hold could parry" : "") :
                    effect.Kind == OrderEffectKind.ScareBlocked ? "scare blocked" :
                    effect.Kind == OrderEffectKind.OutOfReach ? effect.Move+" hit outside guard / "+effect.Amount+" health taken" :
                    effect.Kind == OrderEffectKind.StaleBlock ? effect.Move+" block cost / "+effect.Amount+" nerve" :
                    effect.Kind == OrderEffectKind.Feinted ? effect.Move+" faked Hold / real beat "+Seconds(effect.Amount)+" later" :
                    effect.Kind == OrderEffectKind.WaitedOut ? effect.Move+" waited out Hold / "+Seconds(effect.Amount) : effect.Kind.ToString() + " " + effect.Move;
                return head + (r.Helped && !effect.Helped ? "helped, but " : "") + what + (!r.Final ? " / resolving" : "");
            }
            if (!r.Final) return head + (r.Running ? "active; waiting for an outcome" : "resolving; not final yet");
            switch(r.Miss)
            {
                case OrderMiss.NothingCame: return head+"nothing came while it lasted";
                case OrderMiss.TooEarly: return head+"too early for "+r.MissMove;
                case OrderMiss.TooLate: return head+"too late for "+r.MissMove;
                case OrderMiss.Covered: return head+"target covered by their Warden";
                case OrderMiss.OutOfReach: return head+(r.Order==Directive.Hold?"hit landed outside guard":"no blows landed; target out of reach");
                case OrderMiss.Feinted: return head+"faked by "+r.MissMove;
                case OrderMiss.WaitedOut: return head+"waited out by "+r.MissMove;
                case OrderMiss.Blocked: return head+"their Hold blocked the scare";
                case OrderMiss.WrongOrder: return head+"does not answer "+r.MissMove;
                case OrderMiss.MetFirst: return head+"another answer met "+r.MissMove+" first";
                default: return head+"no recorded effect";
            }
        }

        void ShowClarityDetails()
        {
            _paused = true;
            var sim = ViewSim; ReadClarity(sim);
            string meter=sim.MeterOn?MeterHint()+"\n\n":"";
            var text = new StringBuilder(_playback!=null?"RECORDED FIGHT. These are historical answers; playback accepts no orders.\n\n":"PAUSED. Close, then Resume to time a parry.\n\n");
            text.Append(meter);
            if(RoadSystemsHud)RoadDetails(text,sim);
            if(CleanCombatHud)
            {
                text.Append("YOUR POINTS: ").Append(sim.DirectivePoints(0)).Append(EarnText(0)).Append(" / THEIR POINTS: ").Append(sim.DirectivePoints(1)).Append(EarnText(1)).Append("\n");
                if(_pxGoal!=null)text.Append(_pxGoal.key.Split('|')[0]).Append("\n");
                text.Append("\n");
            }
            text.Append(sim.TightParryOn?TightGuide+"\n\n":sim.DangerOn?"SWINGS: Hold guards your front row; a timed live Hold parries.\nBLASTS (Eclipse, Brand, Burning Ring): time a live parry. Early or paused Hold does not reduce their damage.\nA parry opens the Riposte. Where allowed, Fall Back dodges but stops your attacks.\n\n":sim.SquadFirstOn ? "SQUAD FIRST: fighters attack automatically. Cashing marks earns a point and cracks enemy nerve.\nHOLD guards the front row. No block nerve cost. A live timed Hold can parry.\nSMALL beats: parry opens the attacker; no nerve crack or chain step. BIG beats also reward other safe answers.\nPAUSED HOLD: guards but never parries.\n\n" : sim.ParryCenterOn ? "PERFECT HOLD: live, in the last half second of a beat. Negates the move and staggers its doer.\nEARLY HOLD: may soak, but costs your nerve.\nPAUSED HOLD: never parries. Resume, then issue a fresh Hold.\n\n" : "Historical fight: its original parry and counter rules apply.\n\n");

            foreach (var t in _threatReads)
                text.Append(t.Name).Append(" / ").Append(NameOf(t.ActorId)).Append(" -> ").Append(t.TargetId.Length==0?"area or variable target":NameOf(t.TargetId))
                    .Append(" / ").Append(Seconds(t.TicksLeft)).Append(" left / ~").Append(t.Amount).Append(" HP before armour, misses and answers\n")
                    .Append(BeatCue(t)).Append(t.FakeFirst?" / real beat in "+Seconds(t.RealBeatTicks):"").Append("\n").Append(ThreatAnswers(sim,t,true)).Append("\n\n");
            if(sim.GloryOn(0))text.Append("GLORY ").Append(ChiefKits.Letter(sim.GloryRankOf(0))).Append(" / ").Append(sim.Glory(0)).Append("/").Append(ChiefKits.GloryMax).Append("\n");
            foreach(var f in sim.Forecasts(1))
            {
                text.Append("FORECAST (not yet a tell): ").Append(ChiefKits.Name(f.Move)).Append(" / ").Append(Seconds(f.TicksLeft))
                    .Append(" / ").Append(ForecastAmount(f)).Append(" before armour / ")
                    .Append(f.TargetId.Length>0?NameOf(f.TargetId):f.Move==ChiefMove.Eclipse?"front row":f.Move==ChiefMove.SunMantle?"strikers":"area");
                if(f.SecondTargetId.Length>0)
                {
                    text.Append(" / second: ").Append(NameOf(f.SecondTargetId));
                    for(int i=sim.Events.Count-1;i>=0;i--)
                    {
                        var e=sim.Events[i];
                        if(e.Kind!=BattleEventKind.Forecast||e.ActorSide!=1||e.TargetId!=f.SecondTargetId||e.Detail!=ChiefKits.Name(f.Move))continue;
                        text.Append(" / ").Append(e.Amount).Append(" HP before armour");break;
                    }
                }
                text.Append("\n");
            }
            if(sim.Kit(1)==ChiefKit.Skullpeak&&sim.ChiefTacticsOn)
                text.Append("SHIELDBEARER ").Append(JobStatus(sim,BandJob.Shieldbearer)).Append(" / SPOTTER ").Append(JobStatus(sim,BandJob.Spotter)).Append("\n");
            text.Append("\nRECENT ORDERS\n");
            for(int i=Math.Max(0,_orderReads.Count-6);i<_orderReads.Count;i++)
            {
                var r=_orderReads[i]; text.Append(Seconds(r.Tick)).Append(" ").Append(Receipt(r)).Append("\n");

                foreach(var e in r.Effects) text.Append("  ").Append(e.Kind).Append(" ").Append(e.Move).Append(e.TargetId.Length>0?" / "+NameOf(e.TargetId):"")
                    .Append(e.Kind==OrderEffectKind.Damage||e.Kind==OrderEffectKind.OutOfReach?" / "+e.Amount+" health":e.Kind==OrderEffectKind.StaleBlock?" / "+e.Amount+" nerve":e.Kind==OrderEffectKind.Feinted||e.Kind==OrderEffectKind.WaitedOut?" / "+Seconds(e.Amount):"").Append("\n");
                string lesson=sim.LessonBehind(0,r); if(lesson.Length>0)text.Append("  Their lesson: ").Append(lesson).Append("\n");
            }
            Modal("THREATS AND ORDERS",text.ToString());
        }

        PxLines SquadDecision(Encounter e)
        {
            if(ThemePresentation)
            {
                var quick=new PxLines().Add(e.Kind==EncounterKind.Duel?"1 FIGHTER / LEADER COMMANDS":e.Kind==EncounterKind.Chief?"SPARE = ALLIES / EXECUTE = GEAR":
                    e.Kind==EncounterKind.HoldTheLine?"SURVIVE / BREAK ENEMIES":e.Kind==EncounterKind.Beast?"BREAK PARTS = TROPHIES":"WIN = REWARD",PxGold);
                if(e.NoFallBack)quick.Add("FALL BACK DISABLED",PxRed);
                if(e.Node!=null&&e.Node.Objective!=FightObjective.None)quick.Add("BONUS: "+GoalShort(e.Node.Objective),PxMint);
                quick.Gap().Add("YOUR SQUAD",PxGold);
                foreach(UnitClass c in Enum.GetValues(typeof(UnitClass)))
                {
                    int count=0;foreach(var id in _squad)if(FindReserve(id)?.Class==c)count++;
                    if(count>0)quick.Add(count+" "+SliceClasses.Name(c),PxBone);
                }
                return quick.Gap().Add(LivesRun?"DEFEAT = -1 LIFE":LossWarning(e),PxOrange);
            }
            if(e.Kind==EncounterKind.Duel&&Math.Min(Campaign.RulesVersion,_campaign.Tuning.RulesCap)>=Campaign.DuelRules)
            {
                var duel=new PxLines().Add("CHOOSE ONE FIGHTER",PxGold)
                    .Add("Your leader gives orders. Only your chosen fighter faces the champion.",PxBone)
                    .Gap().Add("WATCH HIS HEAVY SWING",PxGold)
                    .Add("Time Hold on the beat: stagger him, open him up and crack his nerve. A chain makes him yield.",PxBone)
                    .Add("Fall Back dodges for a smaller crack. Early Hold cracks your nerve; a missed swing hits hard.",PxMuted)
                    .Gap().Add("YOUR DUELIST",PxGold);
                duel.Add(_squad.Count==1?FighterName(FindReserve(_squad[0])):"Nobody selected. Pick a fighter on the left.",_squad.Count==1?PxMint:PxOrange);
                return duel.Gap().Add(LossWarning(e),PxRed);
            }
            var lines=new PxLines().Add("YOUR OBJECTIVE",PxGold).Add(e.Kind==EncounterKind.Chief?
                "Break his nerve. Spare for allies and his order; execute for his piece.":e.Kind==EncounterKind.HoldTheLine?
                "Survive to the time limit or break them.":e.Kind==EncounterKind.Beast?(e.Beast==BeastKit.CageBear?"Break the heart before time runs out. Only broken parts give trophies.":"Break all four parts or survive until it flees. Keep broken-part trophies."):"Win the fight and keep your fighters.",PxBone);
            if(e.Node!=null&&e.Node.Objective!=FightObjective.None)lines.Add("Bonus: "+FightObjectives.Text(e.Node.Objective),PxMuted);
            lines.Gap().Add("WATCH FOR",PxGold).Add(e.Kind==EncounterKind.Beast?"Each part has its own move. Read its tell and time Hold at the beat.":HunterFight(e.Kind)?"The hunter's traits and big moves. Open DETAILS for their counters.":e.EnemyCasts?ChiefSpells.Name(e.Tribe.Spell)+". Watch the big beat; Details lists its answers.":
                e.NoFallBack?"No Fall Back here. Protect your leader; Hold guards the front row.":"Small heavy swings. Build openings with your squad; choose when to spend.",PxBone);
            lines.Gap().Add("YOUR SELECTED SQUAD",PxGold);
            foreach(UnitClass c in Enum.GetValues(typeof(UnitClass)))
            {
                int count=0;foreach(var id in _squad)if(FindReserve(id)?.Class==c)count++;
                if(count>0)lines.Add(count+" "+SliceClasses.Name(c)+": "+(c==UnitClass.Warden?"cover":c==UnitClass.Medic?"healing":c==UnitClass.Trapper?"snares":c==UnitClass.Marksman?"ranged focus":"frontline pressure"),PxMint);
            }
            return lines.Gap().Add(e.Kind==EncounterKind.Beast?(LivesRun?"Lose: no trophies, no life lost. Squad returns.":"Lose: no trophies. Fallen stay down; nobody captured."):LossWarning(e),PxRed);
        }
    }
}
