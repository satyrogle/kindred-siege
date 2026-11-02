using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        IEnumerator ValidatePalFloor()
        {
            yield return null;
            if(Argument("-ksPalFloorReopen")!=null)
            {
                Check(Click("continue"),"separate-process Pal save Continue");yield return null;
                Check(CampaignSave.Write(_campaign)==File.ReadAllText(Path.Combine(_capture,"restart-expected.txt")),"all held Pal stores and both slots survive process restart");
                Check(PalFloorText().Contains("Sanctuary")&&_campaign.Reserve.Any(f=>f.Rank==3&&!string.IsNullOrEmpty(f.GearId2)),"restarted held picks and second piece remain visible");
                Capture("floor-reopened");yield break;
            }
            IntegrationCap(23);NewReviewRoute();yield return null;
            string empty=CampaignSave.Write(_campaign);
            ShowFavours();yield return null;
            Check(PalFloorText().Contains("Glutton talents: 0")&&PalFloorText().Contains("not offered on the road"),"empty stores are honest about unavailable road progression");
            Check(CampaignSave.Write(_campaign)==empty,"reading held picks grants nothing");Capture("floor-empty");CloseModal();

            // Authored QA checkpoint, never a product reward: public store APIs seed held picks.
            Check(_campaign.TryLendTalent(GluttonTalent.LightStake)&&_campaign.TryFeedTalent(GluttonTalent.LightStake),"fixture holds and feeds a Glutton talent");
            Check(_campaign.TryTakeRoute(ShadeRoute.Sanctuary)&&_campaign.TryTakeRoute(ShadeRoute.Backstab),"fixture holds two Shade buttons");
            Check(!_campaign.TryTakeRoute(ShadeRoute.Fear),"third enhanced button is still rejected by Campaign");
            Check(_campaign.TrySetHabit(WhispererHabit.Spotter)&&_campaign.TrySetPair(SquadPair.PinAndShoot),"fixture holds a habit and pair");
            var fighter=_campaign.Reserve[0];string fighterId=fighter.Id;
            Check(_campaign.TryRankUp(fighterId)&&_campaign.TryRankUp(fighterId),"fixture reaches rank3 through public APIs");
            var bends=Enumerable.Range((int)ClassBend.BlackHoleRound,15).Select(i=>(ClassBend)i).Where(b=>ClassSpells.IsFor(b,fighter.Class)).Take(2).ToArray();
            var save=CampaignSave.Write(_campaign).Split('\n').ToList();
            foreach(var bend in bends)save.Insert(save.FindLastIndex(l=>l.StartsWith("fighter\t"))+1,
                string.Join("\t","piece",Campaign.ComboGearId(bend),CampaignTribes.ClarthId,"0","0","0",ClassSpells.Name(bend),((int)fighter.Class).ToString(),((int)bend).ToString()));
            _campaign=CampaignSave.Read(string.Join("\n",save),ReviewTuning);_notice="";
            Check(_campaign.LoadNote.Length==0,"authored two-piece save reads without recovery");
            Check(Click("offer-0"),"floor fixture enters actual squad page");yield return null;
            Check(Click("gear-"+fighterId),"rank3 fighter opens real gear picker");yield return null;
            Check(_buttons.ContainsKey("gear-slot2"),"second-slot selector exists for rank3");
            Check(Click("equip-"+Campaign.ComboGearId(bends[0])),"real picker equips slot1");yield return null;
            Check(Click("gear-"+fighterId),"reopen gear picker for second slot");yield return null;
            Check(Click("gear-slot2"),"real picker selects slot2");yield return null;
            Check(!ClickIfPresent("unequip"),"unsupported second-slot removal is not forged by the UI");
            Check(Click("equip-"+Campaign.ComboGearId(bends[1])),"real picker equips second unused combo");yield return null;
            fighter=FindReserve(fighterId);
            Check(fighter.GearId==Campaign.ComboGearId(bends[0])&&fighter.GearId2==Campaign.ComboGearId(bends[1]),"slot2 equip preserves slot1");
            Check(ComboPlacement(bends[1]).Contains("slot 2")&&AboutFighter(fighter,SelectedEncounter).Contains("Slot 2:"),"held and fighter hints identify actual second piece");
            Check(Click("gear-"+fighterId),"reopen equipped fighter gear");yield return null;
            Check(Click("gear-slot2"),"reopen equipped second slot");yield return null;
            string before=CampaignSave.Write(_campaign);
            Check(Click("equip-"+Campaign.ComboGearId(bends[0])),"attempt to reuse the first slot item uses the real command");
            Check(CampaignSave.Write(_campaign)==before,"Campaign rejects already-worn second piece without mutation");Capture("floor-slot2");CloseModal();
            ShowGearPicker(fighterId,0,1);yield return null;
            Check(Click("unequip"),"second slot removes through handed-back Campaign API");yield return null;
            Check(FindReserve(fighterId).GearId==Campaign.ComboGearId(bends[0])&&FindReserve(fighterId).GearId2=="","removing slot2 preserves slot1");
            ShowGearPicker(fighterId,0,1);yield return null;Check(Click("equip-"+Campaign.ComboGearId(bends[1])),"restore second piece through actual picker");yield return null;
            ShowGearPicker(fighterId);yield return null;Check(Click("unequip"),"remove first piece through its supported control");yield return null;
            Check(FindReserve(fighterId).GearId==""&&FindReserve(fighterId).GearId2==Campaign.ComboGearId(bends[1]),"removing slot1 preserves equipped slot2");
            ShowGearPicker(fighterId);yield return null;Check(Click("equip-"+Campaign.ComboGearId(bends[0])),"restore first piece while retaining second");yield return null;
            var rank1=_campaign.Reserve.First(f=>f.Rank==1);ShowGearPicker(rank1.Id);yield return null;
            Check(!_buttons.ContainsKey("gear-slot2"),"rank1 cannot select a second slot");CloseModal();
            ShowFavours();yield return null;
            Check(HeldFavourText().Contains("Light Stake / FED")&&HeldFavourText().Contains("Spotter")&&HeldFavourText().Contains("Pin And Shoot"),"held Pal page reads all actual saved stores");
            Check(HeldFavourText().Contains("Effect not available"),"store-only talents and habits do not claim gameplay effects");Capture("floor-held");CloseModal();
            string held=CampaignSave.Write(_campaign);
            Check(CampaignSave.Write(CampaignSave.Read(held,ReviewTuning))==held,"Pal stores and two slots round-trip exactly");

            foreach(var route in new[]{ShadeRoute.Sanctuary,ShadeRoute.Backstab,ShadeRoute.Fear})
            {
                var kit=new PalKit(new[]{new KeyValuePair<ShadeRoute,int>(route,1)},0);
                var ours=new[]{new UnitSpec("clarth.pal",UnitClass.Marksman,Row.Back,UnitRole.Pal,100,3000),
                    new UnitSpec("clarth.s",UnitClass.Skirmisher,Row.Front,UnitRole.Fighter,100,3000),
                    new UnitSpec("clarth.m",UnitClass.Marksman,Row.Back,UnitRole.Fighter,100,3000),
                    new UnitSpec("clarth.t",UnitClass.Trapper,Row.Back,UnitRole.Fighter,100,3000)};
                var theirs=route==ShadeRoute.Fear?new[]{
                    new UnitSpec("raiders.m1",UnitClass.Skirmisher,Row.Front,UnitRole.Fighter,100,40),
                    new UnitSpec("raiders.m2",UnitClass.Skirmisher,Row.Front,UnitRole.Fighter,100,40),
                    new UnitSpec("raiders.m3",UnitClass.Skirmisher,Row.Front,UnitRole.Fighter,100,40)}:
                    new[]{new UnitSpec("raiders.s",UnitClass.Skirmisher,Row.Front,UnitRole.Fighter,100,3000),new UnitSpec("raiders.chief",UnitClass.Warden,Row.Front,UnitRole.Chief,100,8000)};
                bool chief=route!=ShadeRoute.Fear;
                var setup=new BattleSetup("camp."+(6000+(int)route)+".raiders.warparty",chief?11UL:4UL,ours,theirs,new TacticsProfile(),new TacticsProfile(),
                    3000,BattleSetup.DefaultDirectivePoints,chief?NerveRules.On(NerveRules.Full,NerveRules.Full):NerveRules.Off,chief?10:0,400,
                    new SideRules(1,0,true).WithPals(kit),chief?new SideRules(2,0,false,0,ChiefSpell.Eclipse,kit:ChiefKit.Bloodridge,moves:ChiefMove.Cleave):null,
                    (chief?FightRules.CampaignChiefTactics.WithParryCenter():FightRules.Campaign).WithLayers().WithFloor());
                var sim=new BattleSimulation(setup);var session=new SimSession(sim);long effectAt=-1,cashAt=-1;int affected=0;
                while(!sim.IsFinished)
                {
                    if(route==ShadeRoute.Sanctuary&&effectAt<0&&sim.Threats(0).Any(t=>t.Name==ChiefKits.CleaveName&&t.BeatTicks==1))
                        session.TryIssue(Directives.CommandKind,"0","",0,0,Directives.Name(Directive.Hold),out _);
                    if(route==ShadeRoute.Backstab&&sim.CurrentTick==1)
                        session.TryIssue(Directives.CommandKind,"0","",0,0,Directives.Name(Directive.FallBack),out _);
                    if(route==ShadeRoute.Fear&&effectAt<0&&sim.CurrentTick%60==0)
                        session.TryIssue(Directives.CommandKind,"0","raiders.m2",0,0,Directives.Name(Directive.FocusFire),out _);
                    session.Tick();
                    foreach(var e in sim.Events.Skip(Math.Max(0,sim.Events.Count-30)))
                    {
                        if(effectAt<0&&e.Kind==BattleEventKind.Trick){effectAt=e.Tick;affected=e.Amount;}
                        if(cashAt<0&&e.Kind==BattleEventKind.Cash&&e.Detail==ShadeRules.BackstabDetail)cashAt=e.Tick;
                    }
                    if(sim.AwaitingVerdict)session.TryIssue(Directives.CommandKind,"0",sim.KneelingChief.Id,0,0,Directives.Name(Directive.Hold),out _);
                }
                Check(effectAt>=0&&affected>0,"real "+route+" event exists in fixed QA fight");
                var record=_campaign.Archive.Add(setup,session.Log,sim);WatchFight(record,0,record.Ticks);yield return null;
                SeekPlayback(effectAt+1);yield return null;
                Check(CombatOrderHint(ShadeRules.ButtonOf(route)).Contains(route.ToString()),"recorded button hint names its actual route "+route);
                if(route==ShadeRoute.Sanctuary)
                {
                    foreach(var unit in ViewSim.Units.Where(u=>u.Side==0&&u.Alive))
                        Check(_floorUnits[unit.Id].shield.key.Contains("SH "+unit.Statuses[StatusKind.Shield].Size)&&_floorUnits[unit.Id].shieldFill.enabled,"shield points/marker match sim "+unit.Id);
                }
                if(route==ShadeRoute.Backstab)
                {
                    Check(_floorUnits.Values.Any(r=>r.backstab.key.StartsWith("X2|")),"Backstab readiness is visible on actual allies");
                    Check(cashAt>=0,"Backstab has a real cash event");
                    SeekPlayback(cashAt+1);yield return null;
                    Check(ViewSim.Events.Any(e=>e.Kind==BattleEventKind.Cash&&e.Detail==ShadeRules.BackstabDetail),"replay contains actual Backstab payoff");
                }
                if(route==ShadeRoute.Fear)Check(ViewSim.Units.Any(u=>u.Side==1&&u.Alive&&u.PanicTicks>0),"Fear replay shows the actual panicking survivor");
                Capture("floor-"+route);SeekPlayback(0);yield return null;
                Check(_floorUnits.Values.All(r=>!r.shieldFill.enabled&&r.backstab.key.Split('|')[0]==""),"rewind clears future Pal state "+route);
                SeekPlayback(record.Ticks);yield return null;Check(_playback.Matched,"full "+route+" archive checksum matches");LeavePlayback();CloseModal();
            }
            // A second-only combo must activate the same readouts as a primary combo. Then check
            // two independent combo ranks on one fighter; neither hint may borrow the other slot.
            foreach(bool twoCombos in new[]{false,true})
            {
                var wearer=new UnitSpec("clarth.combo",UnitClass.Marksman).WithBend(twoCombos?ClassBend.BlackHoleRound:ClassBend.Deadeye)
                    .WithComboRank(1).WithSecondPiece(ClassBend.StoppedBullets,3);
                var setup=new BattleSetup("camp."+(twoCombos?6201:6200)+".raiders.warparty",4UL,new[]{wearer},
                    new[]{new UnitSpec("raiders.target",UnitClass.Medic,Row.Front,UnitRole.Fighter,4,5000)},new TacticsProfile(),new TacticsProfile(),
                    1200,BattleSetup.DefaultDirectivePoints,NerveRules.Off,0,400,null,null,FightRules.Campaign.WithLayers().WithMarks().WithFloor());
                var sim=new BattleSimulation(setup);var session=new SimSession(sim);
                while(!sim.IsFinished)session.Tick();
                var receipt=sim.Events.First(e=>e.Kind==BattleEventKind.Freeze&&e.Detail==ClassSpells.Name(ClassBend.StoppedBullets));
                var record=_campaign.Archive.Add(setup,session.Log,sim);WatchFight(record,0,record.Ticks);yield return null;
                SeekPlayback(receipt.Tick+1);yield return null;
                Check(_butcherActive&&_butcherUnits.ContainsKey(wearer.Id),"second-slot combo activates actual effect readouts / dual="+twoCombos);
                string hint=ButcherFighterHint(ViewSim,ViewSim.FindById(wearer.Id));
                Check(hint.Contains("SLOT 2 "+ClassSpells.Name(ClassBend.StoppedBullets)+" RANK 3"),"second combo keeps its own name and rank / dual="+twoCombos);
                if(twoCombos)Check(hint.Contains(ClassSpells.Name(ClassBend.BlackHoleRound)+" RANK 1"),"primary combo keeps independent rank1");
                Check(ClassFighterHint(ViewSim.FindById(wearer.Id)).Contains("SLOT 2:"),"fighter Details includes second-piece mechanics / dual="+twoCombos);
                Capture("floor-second-combo-"+twoCombos);SeekPlayback(record.Ticks);yield return null;
                Check(_playback.Matched,"second-piece replay checksum / dual="+twoCombos);LeavePlayback();CloseModal();
            }
            _campaign=CampaignSave.Read(held,ReviewTuning);_fight=null;_playback=null;Save();SetPage(Page.Squad);yield return null;
            File.WriteAllText(Path.Combine(_capture,"restart-expected.txt"),CampaignSave.Write(_campaign));
            Log("Scripted Pal foundation QA uses authored held picks. It does not establish road acquisition, native input, combat feel or balance approval.");
        }
    }
}
