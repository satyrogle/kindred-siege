using System.Collections.Generic;
using System.Text;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    // The 13 Oct 2026 fight rules on screen (Jacob, 13 Oct 2026: "Let's fix sprint one"; Claude builds the six
    // screens of the Sprint 1 handoff). A blow that misses floats MISS over the fighter it missed; a parried
    // cast reads PARRIED and staggers its caster; earned points show by the counter; the order that finishes a
    // combo lights up on its plate; a snapped fighter wears PANIC or RISES; a tribe's doctrine names itself
    // when it acts. No chance is ever shown (Jacob, 13 Oct 2026). Plain 2x kit and the existing fonts.
    public sealed partial class CampaignArtLoop
    {
        // Points a side just earned, shown by its counter for a moment; points for the same reason that come
        // close together add up.
        const float EarnShown=1.5f;
        readonly int[] _earnSum=new int[2];
        readonly string[] _earnWhy={"",""};
        readonly float[] _earnAt={-100,-100};
        // The tick of a side's latest doctrine that gives an order (Kite, Hold Formation): that order shows as
        // the doctrine, not as a plain order.
        readonly long[] _reactedAt={-1,-1};
        // The MISS showing on each fighter, so blows that miss together never stack.
        readonly Dictionary<Figure,Number> _lastMiss=new Dictionary<Figure,Number>();
        static readonly Color32 MissLower=new Color32(122,104,150,255);

        void ClearFightRules()
        {
            for(int s=0;s<2;s++){_earnSum[s]=0;_earnWhy[s]="";_earnAt[s]=-100;_reactedAt[s]=-1;}
            _lastMiss.Clear();
        }

        /// The new events, from AnimateEvent: live fights and playback alike.
        void FightRulesEvent(BattleEvent e,Figure actor,Figure target)
        {
            int side=e.ActorSide;
            if(side<0||side>1)return;
            switch(e.Kind)
            {
                case BattleEventKind.Miss:
                    // The swing still plays; the word floats over the fighter it missed.
                    if(actor!=null&&actor.attack!=null){actor.poseHold=0;actor.attackAt=_visualTime-actor.attackLength*.4f;actor.anticipated=false;}
                    if(target!=null&&!_seeking&&!TablePresentation)FloatMiss(target);
                    break;
                case BattleEventKind.Parry:
                    // The target is the caster who was parried; it loses e.Amount ticks.
                    if(target!=null)target.staggerUntil=_visualTime+e.Amount/(float)SimClock.TicksPerSecond;
                    PlayCue(Cue.Shield);
                    break;
                case BattleEventKind.Earn:
                    if(_visualTime-_earnAt[side]>EarnShown||_earnWhy[side]!=e.Detail)_earnSum[side]=0;
                    _earnSum[side]+=e.Amount;_earnWhy[side]=e.Detail;_earnAt[side]=_visualTime;
                    break;
                case BattleEventKind.Combo:
                    Callout(side,e.Detail.ToUpperInvariant()+"!",PxGold);
                    break;
                case BattleEventKind.Snap:
                    if(e.Detail==RoadRules.Fled&&ViewKind==EncounterKind.Duel)
                    {Callout(side,"CHAMPION YIELDS",PxGold);break;}
                    if(RoadHud)break; // Individual state/ring and enemy count carry the road outcome.
                    bool rises=e.Detail==FightRules.Rise;
                    Callout(side,rises?"RISES!":"PANIC!",rises?PxGold:PxLilac);
                    break;
                case BattleEventKind.Reaction:
                    // Guard the caster keeps the cast's own callout; the cover shows as COVERED under the caster.
                    if(e.Detail==BattleSimulation.GuardCasterDetail)break;
                    if(e.Detail==BattleSimulation.KiteDetail||e.Detail==BattleSimulation.HoldFormationDetail)_reactedAt[side]=e.Tick;
                    Callout(side,ReactionWord(e.Detail),PxOrange);
                    break;
            }
        }

        /// True when this order came from a doctrine that already called out its own name.
        bool DoctrineOrder(BattleEvent e)
        {
            return e.ActorSide>=0&&e.ActorSide<=1&&_reactedAt[e.ActorSide]==e.Tick;
        }

        static string ReactionWord(string detail)
        {
            switch(detail)
            {
                case BattleSimulation.RallyDetail: return "RALLY!";
                case BattleSimulation.HoldFormationDetail: return "HOLD FORMATION!";
                case BattleSimulation.KiteDetail: return "KITE!";
                case BattleSimulation.WaitOutDetail: return "WAITS OUT YOUR HOLD!";
                default: return BandWord(detail)??detail.ToUpperInvariant()+"!";
            }
        }

        /// One line for the fight feed when a tribe's doctrine acts.
        string ReactionLine(BattleEvent e)
        {
            // A hunter band's move (BandTrait) says what it did and the button that answers it (CampaignArtHunters.cs).
            string band=BandLine(e);
            if(band!=null)return band;
            if(e.ActorSide!=1)return "Your fighters: "+e.Detail+".";
            switch(e.Detail)
            {
                case BattleSimulation.RallyDetail: return "They rally: the rest hit "+(BattleSimulation.RallyDamagePercent-100)+"% harder for "+Seconds(e.Amount)+".";
                case BattleSimulation.GuardCasterDetail: return "Their Wardens cover "+NameOf(e.TargetId)+".";
                case BattleSimulation.HoldFormationDetail: return "Their nerve fell below half: they hold formation.";
                case BattleSimulation.KiteDetail: return "They Fall Back out of your Unleash.";
                default: return TribeDoctrines.Describe(e.Detail);
            }
        }

        /// Tags over a fighter: a snapped fighter and a staggered caster.
        void FightRulesTags(Figure f,BattleSimulation sim,List<string> texts,List<Color32> colours)
        {
            var u=f.unit;
            if(!RoadHud&&u.PanicTicks>0){texts.Add("PANIC "+Seconds(u.PanicTicks));colours.Add(PxLilac);}
            if(!RoadHud&&u.RiseTicks>0){texts.Add("RISES "+Seconds(u.RiseTicks));colours.Add(PxGold);}
            if(_visualTime<f.staggerUntil){texts.Add("STAGGERED");colours.Add(PxBlue);}
        }

        /// What a side just earned, for its counter: "+1 KILL". Empty when nothing came lately.
        string EarnText(int side)
        {
            if(_earnSum[side]<=0||_visualTime-_earnAt[side]>EarnShown)return "";
            return "+"+_earnSum[side]+" "+_earnWhy[side].ToUpperInvariant();
        }

        /// The plate of the order that finishes a combo you started: the combo's name and the time left.
        static bool ComboPlate(ComboHint combo,int index,Directive d,ref string title,ref string effect)
        {
            if(combo.IsNone||d!=combo.Next)return false;
            title=(index+1)+" "+combo.Combo.ToUpperInvariant();
            string left=Seconds(combo.TicksLeft);
            effect=combo.Step+1>=combo.Steps?left:(combo.Step+1)+"/"+combo.Steps+" / "+left;
            return true;
        }

        /// The enemy's doctrines for the tactics panel: what their tribe always does, learned or not.
        static string DoctrineText(BattleSimulation sim)
        {
            IReadOnlyList<string> list=sim.Doctrines(1);
            if(list.Count==0)return "";
            var b=new StringBuilder("\n\nTHEIR DOCTRINE");
            foreach(string d in list)b.Append('\n').Append(TribeDoctrines.Describe(d));
            return b.ToString();
        }

        /// MISS over a fighter at the world's 1x, at head height so it clears the damage numbers at the chest,
        /// rising and fading like a number. One at a time per fighter: misses that come together show once.
        void FloatMiss(Figure f)
        {
            if(_selected!=f.unit.Id&&!f.unit.IsLeader)return;
            if(_lastMiss.TryGetValue(f,out Number last)&&_numbers.Contains(last)&&_visualTime-last.at<.6f)return;
            var n=new Number{at=_visualTime,origin=new Vector2(f.feet.x,Mathf.Max((StageY+40)*Px,f.feet.y-f.head*.85f))};
            n.image=Raw(_effects,"Miss",0,0,1,1);
            var t=_logoFont.Render("MISS",PxLilac,MissLower,1,true);_screenTextures.Add(t);
            n.image.texture=t;n.image.rectTransform.sizeDelta=new Vector2(t.width*ArtScale,t.height*ArtScale);
            _numbers.Add(n);_lastMiss[f]=n;
        }
    }
}
