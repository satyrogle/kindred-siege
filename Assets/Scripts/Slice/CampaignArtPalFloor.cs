using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        bool PalFloorRun => _campaign!=null&&Math.Min(Campaign.RulesVersion,_campaign.Tuning.RulesCap)>=Campaign.FloorRules;
        static string PalPickName(object value)=>Regex.Replace(value.ToString(),"([a-z])([A-Z])","$1 $2");

        string SecondGearHint(CampaignFighter fighter)
        {
            if(!PalFloorRun)return "";
            var second=_campaign.FindGear(fighter.GearId2);
            return " Rank "+fighter.Rank+". "+(fighter.Rank<PalStoreRules.SecondSlotRank?
                "Second gear slot opens at rank "+PalStoreRules.SecondSlotRank+".":
                second==null?"Slot 2: empty.":"Slot 2: "+second.Name+". "+second.Describe());
        }

        string PalFloorText()
        {
            var lines=new List<string>{"PAL PROGRESSION", WhispererRun?"Base habits stay. Other picks and habit upgrades leave when the run ends.":"These picks leave when the run ends."};
            lines.Add("Glutton talents: "+_campaign.Talents.Count);
            foreach(var held in _campaign.Talents)lines.Add(PalPickName(held.Talent)+(held.Fed?" / FED":" / LENT")+". Effect not available in this build.");
            lines.Add("Shade buttons: "+_campaign.ShadeRoutes.Count+" / "+ShadeRules.MaxButtons);
            foreach(var held in _campaign.ShadeRoutes)
                lines.Add(OrderName(ShadeRules.ButtonOf(held.Route))+": "+held.Route+" / form "+held.Form+". "+ShadeRouteHint(held.Route,held.Form));
            lines.Add("Whisperer habits: "+_campaign.Habits.Count);
            foreach(var held in _campaign.Habits)lines.Add(SliceClasses.Name(held.Class)+": "+PalPickName(held.Habit)+
                (WhispererRun?(held.Upgraded?" / UPGRADED. ":". ")+HabitEffect(held.Habit,held.Upgraded):". Effect not available in this build."));
            lines.Add("Pair: "+PalPickName(_campaign.Pair)+(_campaign.Pair==SquadPair.None?".":WhispererRun?". "+PairEffect(_campaign.Pair):". Effect not available in this build."));
            lines.Add(WhispererRun?"Whisperer roads offer fighter ranks, pairs and habit upgrades.":"New progression picks are not offered on the road in this build.");
            return string.Join("\n",lines);
        }

        static string ShadeRouteHint(ShadeRoute route,int form)
        {
            string effect=route==ShadeRoute.Sanctuary?"A clean parry shields each standing ally for "+ShadeRules.ShieldPercent+"% max HP / "+Seconds(ShadeRules.ShieldTicks)+".":
                route==ShadeRoute.Backstab?"Fall Back readies a double-damage next blow / "+Seconds(ShadeRules.BackstabTicks)+".":
                route==ShadeRoute.Fear?"Kill the focused enemy or make them flee: nearby enemies lose "+ShadeRules.FearNerve+" nerve; the weakest panics / "+Seconds(ShadeRules.FearPanicTicks)+". Melee last stands still need a kill.":
                "Effect not available in this build.";
            if(form>1&&(route==ShadeRoute.Sanctuary||route==ShadeRoute.Backstab||route==ShadeRoute.Fear))effect+=" Plays its first-form effect in this build.";
            return effect;
        }

        string PalFloorOrderHint(Directive order)
        {
            if((_fight==null&&_playback==null)||!ViewSim.FloorOn)return "";
            var route=ViewSim.Setup.Rules(0).Pals.RouteOn(order);
            return route==ShadeRoute.None?"":" "+route+": "+ShadeRouteHint(route,ViewSim.Setup.Rules(0).Pals.FormOf(route));
        }

        static bool ButcherHasBend(BattleSimulation sim,string id,ClassBend bend)
        {
            foreach(var spec in sim.Setup.Side(sim.FindById(id).Side))
                if(spec.Id==id)return spec.Bend==bend||(sim.FloorOn&&spec.Bend2==bend);
            return false;
        }
        static bool ButcherHasCombo(BattleSimulation sim,BattleUnit unit)
        {
            foreach(var spec in sim.Setup.Side(unit.Side))
                if(spec.Id==unit.Id)return ClassSpells.IsCombo(spec.Bend)||(sim.FloorOn&&ClassSpells.IsCombo(spec.Bend2));
            return false;
        }

        sealed class PalFloorReadout
        {
            public Image shieldBack,shieldFill;
            public PxText shield,backstab;
        }
        readonly Dictionary<string,PalFloorReadout> _floorUnits=new Dictionary<string,PalFloorReadout>();
        RectTransform _floorScreen;

        void DrawPalFloor(BattleSimulation sim)
        {
            if(_floorScreen!=_screen)
            {
                _floorScreen=_screen;_floorUnits.Clear();
                if(sim.FloorOn)foreach(var figure in _figures)
                {
                    var read=new PalFloorReadout();_floorUnits.Add(figure.unit.Id,read);
                    read.shieldBack=Panel(figure.readoutRoot,0,0,1,1,PxMuted);read.shieldBack.name="Shield capacity";
                    read.shieldFill=Panel(figure.readoutRoot,0,0,1,1,PxBlue);read.shieldFill.name="Shield remaining";
                    read.shield=PxLabel(figure.readoutRoot,0,-13,1);
                    read.backstab=PxLabel(figure.readoutRoot,-Mathf.RoundToInt(figure.barWidth/2)-4,0,2);
                }
            }
            foreach(var figure in _figures)
            {
                if(!_floorUnits.TryGetValue(figure.unit.Id,out var read))continue;
                var unit=figure.unit;
                bool visible=sim.FloorOn&&!ViewOver&&!sim.AwaitingVerdict&&!ChiefCutInVisible&&unit.Alive&&!unit.Fled&&!unit.Benched;
                var shield=unit.Statuses[StatusKind.Shield];
                bool shielded=visible&&shield.On&&shield.Size>0;
                read.shieldBack.enabled=read.shieldFill.enabled=shielded;
                int width=Mathf.RoundToInt(figure.barWidth);
                Place(read.shieldBack,-width/2,-5,width,2);
                Place(read.shieldFill,-width/2,-5,Mathf.Max(1,Mathf.RoundToInt(width*Mathf.Clamp01(shield.Size/(float)unit.MaxHp))),2);
                SetPx(read.shield,shielded?"SH "+shield.Size:"",PxBlue);
                SetPx(read.backstab,visible&&unit.Statuses[StatusKind.Backstab].On?"X2":"",PxLilac);
            }
        }

        static string PalFloorFighterHint(BattleSimulation sim,BattleUnit unit)
        {
            if(!sim.FloorOn)return "";
            var shield=unit.Statuses[StatusKind.Shield];var backstab=unit.Statuses[StatusKind.Backstab];
            return (shield.On?"   SHIELD "+shield.Size+" HP / "+Seconds(shield.Ticks):"")+
                (backstab.On?"   BACKSTAB X2 NEXT BLOW / "+Seconds(backstab.Ticks):"");
        }

        void PalFloorEvent(BattleEvent e)
        {
            if(!ViewSim.FloorOn)return;
            if(e.Kind==BattleEventKind.Trick)
            {
                string text=e.Detail==ShadeRules.SanctuaryDetail?"SHIELDS UP":e.Detail==ShadeRules.BackstabDetail?"BACKSTAB READY":
                    e.Detail==ShadeRules.FearDetail?"FEAR / "+e.Amount+" SHAKEN":"";
                if(text.Length>0)Callout(e.ActorSide,text,e.Detail==ShadeRules.SanctuaryDetail?PxBlue:PxLilac);
            }
            if(e.Kind==BattleEventKind.Cash&&e.Detail==ShadeRules.BackstabDetail)
                Callout(e.ActorSide,"BACKSTAB / +"+e.Amount+" DAMAGE",PxLilac);
        }
    }
}
