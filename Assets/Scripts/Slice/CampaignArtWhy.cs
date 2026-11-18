using System.Collections.Generic;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // Why a fight goes the way it does (Jacob, 11 Oct 2026: "I never knew how I won or how I actually
        // failed ... All I was doing was spamming focus fire on healer, but that did nothing"). Everything
        // here reads the fight's own rules and record; no rule changes. Live words when an order cannot land
        // or gets covered, and a panel at the end with the authoritative ending and recorded highlights.
        float _coverCalloutAt=-100;
        RectTransform _whyPanel;
        RectTransform _focusScreen;
        readonly Image[] _focusCorners = new Image[8];
        readonly Dictionary<string,Image> _focusTraces = new Dictionary<string,Image>();
        int _focusDamage;
        PxText _focusTargetLabel;
        string _focusHitTarget = "";
        long _focusHitTick = -1000, _focusDrawTick = -1;

        bool FightWon()=>_campaign.FightWon(_fight);

        // Translate the simulation's answer; never infer a reach rule from class, row or active orders.
        static string StrikeNote(StrikeBlock reason)
        {
            switch(reason)
            {
                case StrikeBlock.FrontStands:return "CAN'T REACH";
                case StrikeBlock.SquadProtectsLeader:return "SQUAD PROTECTS";
                case StrikeBlock.FallBack:return "LINES APART";
                case StrikeBlock.WaitsOutTrap:return "WAITING OUT TRAP";
                default:return "";
            }
        }

        /// Called for every event as it is drawn, after the usual words, so these replace them.
        void WhyEvent(BattleEvent e)
        {
            ReadoutEvent(e);
            var sim=ViewSim;
            if(e.Kind==BattleEventKind.Directive&&e.ActorSide==0&&e.Detail==Directives.Name(Directive.FocusFire)&&e.TargetId.Length>0)
            {
                BattleUnit target=sim.FindById(e.TargetId);
                if(target!=null&&target.Alive)
                {
                    int reach=sim.StrikersOn(target,out int all);
                    if(sim.SquadScreenOn)Callout(0,"FOCUS: "+RoleName(target,ViewKind).ToUpperInvariant()+" / "+reach+" OF "+all+" CAN STRIKE NOW",reach<all?PxOrange:PxRed);
                    else if(reach<all)Callout(0,"ONLY "+reach+" OF "+all+" CAN STRIKE NOW",PxOrange);
                }
            }
            if(sim.SquadScreenOn&&e.Kind==BattleEventKind.Hit&&e.ActorSide==0&&e.Amount>0&&e.TargetId==sim.FocusedId(0))
            { _focusHitTarget=e.TargetId; _focusHitTick=e.Tick; }
            // Their Warden covers their most hurt fighter, so a focus target soon has its hits taken for it.
            if(e.Kind==BattleEventKind.Guard&&e.ActorSide==1&&e.TargetId.Length>0&&e.TargetId==sim.FocusedId(0)&&_visualTime-_coverCalloutAt>3f)
            {
                _coverCalloutAt=_visualTime;
                Callout(0,"THEIR WARDEN TAKES YOUR HITS",PxBlue);
            }
        }

        /// Tags under a fighter for what the rules are doing to it right now.
        void WhyTags(Figure f,BattleSimulation sim,List<string> texts,List<Color32> colours)
        {
            var u=f.unit;
            // One word on both sides: their callout already names the Warden, and the long tag ran into the next
            // fighter's (Claude's playtest, 14 Oct 2026).
            if(sim.CoveredBy(u)!=null){texts.Add("COVERED");colours.Add(u.Side==0?PxMint:PxBlue);}
            if(u.Side!=0)return;
            var target=sim.FindById(sim.FocusedId(0));
            string note=StrikeNote(sim.CanStrike(u,target));
            if(note.Length>0){texts.Add(note);colours.Add(PxOrange);}
        }

        /// The panel once the fight is over: shown a second after the end, with SEE RESULT under it.
        void DrawWhy()
        {
            if(_playback!=null||_fight==null||!_fight.Over||_endAge<1f||_whyPanel!=null)return;
            var lines=WhyLines();
            _whyPanel=Rect(_screen,"Why",0,0,1,1);
            int height=50+11*PxLineCount(lines,388);
            PxPanel(140,76,440,height,lines,_whyPanel);
            _resultButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(260*Px,-(76+height+6)*Px);
        }

        PxLines WhyLines()
        {
            if(RoadHud)return RoadSummary(_fight.Sim);
            var sim=_fight.Sim;var record=FightRecord.From(sim);bool won=FightWon();
            var lines=new PxLines();
            lines.Add(won?(_fight.Encounter.Kind==EncounterKind.HoldTheLine?"Why you held":"Why you won"):"Why you lost",won?PxMint:PxRed);
            lines.Add(Ending(sim.Ending,_fight.Encounter.Kind,won),PxBone);
            ChiefOutcomeLines(lines,sim,won);
            // The moments that swung it, from an exact replay ("fix all 9", 13 Oct 2026); the totals when none did.
            if(WhySwingLines(lines,won))return lines;
            string help=Helped(record,out _),hurt=Hurt(record,out _);
            if(won){if(help.Length>0)lines.Add("Your side: "+help,PxMint);if(hurt.Length>0)lines.Add("Their side: "+hurt,PxRed);}
            else{if(hurt.Length>0)lines.Add("Their side: "+hurt,PxRed);if(help.Length>0)lines.Add("Your side: "+help,PxMint);}
            return lines;
        }

        static void ChiefOutcomeLines(PxLines lines,BattleSimulation sim,bool won)
        {
            if(!sim.SquadScreenOn||!sim.RaidChiefOn)return;
            var leader=sim.Leader(0);var chief=sim.Leader(1);
            if(!won&&sim.Ending.How==FightEnd.NerveBreak&&sim.Ending.Side==0)
                lines.Add("Zero team nerve ends the fight, even with fighters alive.",PxRed);
            if(leader!=null&&chief!=null)lines.Add("Leader "+leader.Hp+"/"+leader.MaxHp+" HP / Chief "+chief.Hp+"/"+chief.MaxHp+" HP left.",PxBone);
        }

        // The Focus command already chooses the target in the sim. Keep that choice readable on the
        // fighter, rather than making its only persistent marker a seven-pixel status icon.
        void DrawFocusTarget(BattleSimulation sim) => DrawFocusFeedback(sim);

        static string Ending(FightEnding end,EncounterKind kind,bool won)
        {
            string at=" at "+Seconds(end.Tick);
            switch(end.How)
            {
                case FightEnd.LeaderFell:
                    return (end.Side==1?"Their":"Your")+" leader fell, so "+(end.Side==1?"their":"your")+" nerve broke"+at+".";
                case FightEnd.NerveBreak:
                    return (end.Side==1?"Their":"Your")+" nerve broke"+at+" with "+end.Standing+" still standing.";
                case FightEnd.Wipe:
                    return end.Side<0?"Both sides went down.":end.Side==1?"Every one of theirs went down.":"Every one of yours went down.";
                case FightEnd.TimeUp:
                    if(kind==EncounterKind.HoldTheLine&&won)return "Time ran out and you held the line.";
                    return "Time ran out. The campaign counts this as a "+(won?"win.":"loss.");
            }
            return "The fight ended.";
        }

        // The weights only rank one line against another: damage, healing and damage stopped count as they
        // are, and the extra from Unleash is a third of the damage dealt while it ran (+50%). Starting values.
        string Helped(FightRecord r,out int best)
        {
            best=0;string line="";
            int[] dealt=r.DamageTakenByClass(1,out int total);
            int top=0;for(int c=1;c<dealt.Length;c++)if(dealt[c]>dealt[top])top=c;
            Pick(ref best,ref line,dealt[top],"your "+SliceClasses.Name((UnitClass)top)+" did "+dealt[top]+" of your "+total+" damage.");
            int healed=r.HealedBy(0,UnitClass.Medic,out int heals,out _,out _);
            Pick(ref best,ref line,healed,"your Medic healed "+healed+" in "+heals+" heals.");
            int stopped=r.GuardPrevented(0,out int guards,out _,out _);
            Pick(ref best,ref line,stopped,"your Warden covered "+guards+" hits and stopped "+stopped+" damage.");
            int unleash=r.UnleashDamage(0,out int unleashes,out _);
            Pick(ref best,ref line,unleash/3,"you did "+unleash+" damage while your Unleash ran.");
            int kills=r.FocusKills(0,false,out int focuses,out _,out _,out _);
            Pick(ref best,ref line,kills*90,kills+" of your Focus Fire orders ended with the target falling during the order.");
            int dodged=Releases(r,1,false);
            Pick(ref best,ref line,dodged*80,"you dodged or halved their cast "+dodged+(dodged==1?" time.":" times."));
            return line;
        }

        string Hurt(FightRecord r,out int best)
        {
            best=0;string line="";
            int[] dealt=r.DamageTakenByClass(0,out int total);
            int top=0;for(int c=1;c<dealt.Length;c++)if(dealt[c]>dealt[top])top=c;
            Pick(ref best,ref line,dealt[top],"their "+SliceClasses.Name((UnitClass)top)+" did "+dealt[top]+" of their "+total+" damage to you.");
            int healed=r.HealedBy(1,UnitClass.Medic,out int heals,out _,out _);
            Pick(ref best,ref line,healed,"their Medic healed "+healed+" in "+heals+" heals.");
            int covered=CoveredFocus(r,out int coverHits);
            int stopped=r.GuardPrevented(1,out int guards,out _,out _);
            if(coverHits>0)Pick(ref best,ref line,stopped,"their Warden took "+coverHits+" of your hits on your Focus Fire target, "+covered+" damage stopped.");
            else Pick(ref best,ref line,stopped,"their Warden covered "+guards+" hits and stopped "+stopped+" of your damage.");
            int unleash=r.UnleashDamage(1,out _,out _);
            Pick(ref best,ref line,unleash/3,"they did "+unleash+" damage while their Unleash ran.");
            int casts=Releases(r,1,true);
            Pick(ref best,ref line,casts*80,"their cast hit your front line "+casts+(casts==1?" time.":" times."));
            return line;
        }

        static void Pick(ref int best,ref string line,int weight,string text)
        {
            if(weight>best){best=weight;line=char.ToUpperInvariant(text[0])+text.Substring(1);}
        }

        /// Casts by a side that went off: landed in full, or dodged and halved.
        static int Releases(FightRecord r,int side,bool landed)
        {
            int count=0;
            foreach(var e in r.Events)
                if(e.Kind==BattleEventKind.Release&&e.ActorSide==side&&(e.Detail==Moves.Full)==landed)count++;
            return count;
        }

        /// Their Warden's covers of whoever your Focus Fire was on at that moment, and the damage they stopped.
        static int CoveredFocus(FightRecord r,out int hits)
        {
            hits=0;int stopped=0;string focus="";long until=-1;
            foreach(var e in r.Events)
            {
                if(e.Kind==BattleEventKind.Directive&&e.ActorSide==0&&e.Detail==Directives.Name(Directive.FocusFire)){focus=e.TargetId;until=e.Tick+BattleSimulation.FocusTicks;}
                else if(e.Kind==BattleEventKind.Guard&&e.ActorSide==1&&e.Tick<until&&e.TargetId==focus){hits++;stopped+=e.Amount;}
            }
            return stopped;
        }

    }
}
