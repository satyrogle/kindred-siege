using System;
using System.Linq;
using System.Reflection;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    public class Freeze37Tests
    {
        const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static object Call(object o, string name, params object[] a) => o.GetType().GetMethods(F).First(m => m.Name == name &&
            m.GetParameters().Length == a.Length && m.GetParameters().Select((p,i) => a[i] == null || p.ParameterType.IsInstanceOfType(a[i])).All(x => x)).Invoke(o,a);
        static void Set(object o, string name, object v) => o.GetType().GetProperty(name,F).SetValue(o,v);
        static void Field(object o, string name, object v) => o.GetType().GetField(name,F).SetValue(o,v);
        static object State(BattleSimulation s, int side) => ((Array)typeof(BattleSimulation).GetField("_sides",F).GetValue(s)).GetValue(side);
        static int Clock(object s, string name) => (int)s.GetType().GetField(name,F).GetValue(s);
        static FightRules Rules(FightRules r, bool current = true)
        {
            r = r.WithRoadKit().WithClearRoad(4).WithSquadScreen().WithLayers().WithMarks().WithPayoffs().WithDanger()
                .WithMeter().WithFloor().WithWhisper().WithShadeForms().WithGlutton().WithDuos().WithSized().WithFlavours()
                .WithVows().WithDuoMoves().WithTight().WithCorrections().WithContracts();
            return current ? r.WithFreezeContracts() : r;
        }
        static UnitSpec U(string id, UnitClass c = UnitClass.Marksman, UnitRole role = UnitRole.Fighter, int rank = 1) =>
            new UnitSpec(id,c,Row.Front,role,100,30000).WithRank(rank);
        static BattleSimulation Fight(bool current = true, int freezerRank = 1, int donorRank = 1, UnitSpec? target = null,
            SideRules enemy = null, FightRules rules = null, UnitSpec[] support = null, int comboRank = 1)
        {
            var foes = new[] { target ?? U("x") }.Concat(support ?? new UnitSpec[0]);
            return new BattleSimulation(new BattleSetup("freeze37",11,
                new[] { U("a",UnitClass.Skirmisher,rank:freezerRank).WithBend(ClassBend.FrozenFlurry).WithComboRank(comboRank), U("b",rank:donorRank) },
                foes.ToArray(),new TacticsProfile(),new TacticsProfile(),BattleSetup.DefaultTickLimit,20,NerveRules.Off,10,100,
                SideRules.None,enemy ?? SideRules.None,Rules(rules ?? FightRules.Campaign,current)));
        }
        static void Quiet(BattleSimulation s)
        {
            foreach (var u in s.Units) { Set(u,"Cooldown",10000); Set(u,"SpellTicks",10000); }
        }
        static void Unpin(BattleUnit u) => Call(u.Statuses,"Clear",StatusKind.Pin);
        static void Hit(BattleSimulation s, int amount = 100) => Call(s,"ApplyDamage",s.FindById("b"),0,s.FindById("x"),amount,"audit",false);
        static void Freeze(BattleSimulation s) => Call(s,"FlurryStruck",s.FindById("a"),s.FindById("x"));
        static int Release(BattleSimulation s)
        {
            var x = s.FindById("x"); int hp = x.Hp; Call(s,"Thaw",x);
            int damage = hp-x.Hp; Call(s,"Thaw",x); Assert.AreEqual(hp-damage,x.Hp,"release only once");
            Assert.AreEqual(0,x.FlurryStore); return damage;
        }

        [TestCase(ChiefMove.Cleave,false)] [TestCase(ChiefMove.BurningRing,false)]
        [TestCase(ChiefMove.Eclipse,true)] [TestCase(ChiefMove.Cleave,true)]
        public void StasisBlocksNewActorMovesAndTheyResumeAfterRelease(ChiefMove move, bool chief)
        {
            foreach (bool current in new[] { false,true })
            {
                var side = chief ? new SideRules(move == ChiefMove.Eclipse ? 1 : 0,move == ChiefMove.Eclipse ? 30 : 0,false,
                    kit:ChiefKit.Bloodridge,moves:move) : SideRules.None;
                var s = Fight(current,target:U("x",UnitClass.Skirmisher,chief ? UnitRole.Chief : UnitRole.Fighter)
                    .WithRoadKit(0,chief ? ChiefMove.None : move),enemy:side,rules:chief ? FightRules.CampaignChiefTactics : FightRules.Campaign,comboRank:3);
                Quiet(s); var x = s.FindById("x"); Call(s,"Burst",s.FindById("a"),x,StatusKind.Stasis);
                int start = s.Events.Count; var ses = new SimSession(s);
                for (int i = 0; i < 210; i++) ses.Tick();
                Assert.Greater(x.PinTicks,0);
                Assert.AreEqual(!current,s.Events.Skip(start).Any(e => e.ActorId == "x" &&
                    (e.Kind == BattleEventKind.Tell || e.Kind == BattleEventKind.Windup || e.Kind == BattleEventKind.Release || e.Kind == BattleEventKind.Land)),"version boundary");
                if (!current) continue;
                Unpin(x); start = s.Events.Count;
                for (int i = 0; i < 130; i++) ses.Tick();
                Assert.IsTrue(s.Events.Skip(start).Any(e => e.ActorId == "x" &&
                    (e.Kind == BattleEventKind.Land || e.Kind == BattleEventKind.Release)),"move resumes");
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void AnExistingCleaveWaitsAndDoesNotAdvertiseAFrozenParry(bool chief)
        {
            var s = Fight(target:U("x",UnitClass.Skirmisher,chief ? UnitRole.Chief : UnitRole.Fighter).WithRoadKit(0,chief ? ChiefMove.None : ChiefMove.Cleave),
                enemy:chief ? new SideRules(0,0,false,kit:ChiefKit.Bloodridge,moves:ChiefMove.Cleave) : null,
                rules:chief ? FightRules.CampaignChiefTactics : null);
            Quiet(s); var x = s.FindById("x"); var st = State(s,1);
            if (chief) { Field(st,"CleaveWindup",4); Field(st,"CleaveTargetId","a"); }
            else { Set(x,"CarryTell",4); Set(x,"CarryMove",ChiefMove.Cleave); Set(x,"CarryTargetId","a"); }
            Call(s,"Pin",x,100); var before = s.OrderedStateSnapshot;
            Assert.IsFalse(s.Threats(0).Any(t => t.Name == ChiefKits.CleaveName));
            Assert.AreEqual(before,s.OrderedStateSnapshot,"read does not mutate state");
            int clock = chief ? Clock(st,"CleaveWindup") : x.CarryTell;
            var ses = new SimSession(s); ses.Tick();
            Assert.AreEqual(clock,chief ? Clock(st,"CleaveWindup") : x.CarryTell);
            Unpin(x); Assert.IsTrue(s.Threats(0).Any(t => t.Name == ChiefKits.CleaveName));
            for (int i = 0; i < clock; i++) ses.Tick();
            Assert.IsTrue(s.Events.Any(e => e.ActorId == "x" && e.Kind == BattleEventKind.Land && e.Detail.StartsWith(ChiefKits.CleaveName)));
        }
        [Test] public void AnExistingEclipseWaitsWithoutAFalseParryCue()
        {
            var s = Fight(target:U("x",role:UnitRole.Chief),enemy:new SideRules(1,30,false,kit:ChiefKit.Bloodridge,moves:ChiefMove.Eclipse),rules:FightRules.CampaignChiefTactics);
            Quiet(s); var ses = new SimSession(s); for (int i=0;i<35;i++) ses.Tick();
            var st=State(s,1); Assert.Greater(Clock(st,"Windup"),0); int before=Clock(st,"Windup");
            Call(s,"Pin",s.FindById("x"),100); ses.Tick();
            Assert.AreEqual(before,Clock(st,"Windup")); Assert.IsFalse(s.Threats(0).Any(t=>t.Tier==ThreatTier.Spell));
            Unpin(s.FindById("x")); for(int i=0;i<before;i++)ses.Tick();
            Assert.IsTrue(s.Events.Any(e=>e.ActorId=="x"&&e.Kind==BattleEventKind.Release));
        }
        [Test] public void APlacedBrandStillCountsDownAndHitsWhileItsOwnerIsPinned()
        {
            var s=Fight(target:U("x").WithRoadKit(0,ChiefMove.Brand)); Quiet(s);
            var a=s.FindById("a"); var x=s.FindById("x"); Set(a,"BrandTicks",1); Set(a,"BrandById","x"); Call(s,"Pin",x,100);
            Assert.IsTrue(s.Threats(0).Any(t=>t.Name==ChiefKits.BrandName)); int hp=a.Hp;
            new SimSession(s).Tick(); Assert.Less(a.Hp,hp); Assert.Greater(x.PinTicks,0);
        }
        [Test] public void APinnedMenderWaitsUntilItCanAct()
        {
            var s=Fight(target:U("x",role:UnitRole.Chief),enemy:new SideRules(0,0,false,band:BandTrait.Mender)); Quiet(s);
            var x=s.FindById("x"); Set(x,"Hp",x.MaxHp/2); Call(s,"Pin",x,100); int hp=x.Hp; var ses=new SimSession(s);
            for(int i=0;i<65;i++)ses.Tick(); Assert.AreEqual(hp,x.Hp);
            Unpin(x); ses.Tick(); Assert.Greater(x.Hp,hp);
        }
        [TestCase(1,3,700)] [TestCase(3,1,100)]
        public void FlurryKeepsTheDonorsRankInsteadOfTheFreezers(int freezer,int donor,int expected)
        {
            var s=Fight(freezerRank:freezer,donorRank:donor); Freeze(s); int hp=s.FindById("x").Hp; Hit(s);
            Assert.AreEqual(hp,s.FindById("x").Hp); Assert.AreEqual(expected,s.FindById("x").FlurryStore); Assert.AreEqual(expected,Release(s));
            var old=Fight(false,freezer,donor); Freeze(old); Hit(old); Assert.AreEqual(freezer==3?700:100,Release(old));
        }
        [TestCase(0,false,100)] [TestCase(3,false,130)] [TestCase(0,true,200)] [TestCase(3,true,260)]
        public void ThawReceivesTheHeldBarBonusWithOrWithoutAnOpening(int bars,bool opening,int expected)
        {
            var s=Fight(); Field(State(s,0),"ParryBars",bars); Freeze(s); Hit(s);
            if(opening)Call(s,"Open",s.FindById("b"),s.FindById("x"));
            Assert.AreEqual(expected,Release(s));
        }
        [Test] public void StoredPayoffsDoNotReceiveParryBoostTwice()
        {
            var s=Fight(); Field(State(s,0),"ParryBars",3); Freeze(s);
            Call(s,"ApplyHit",s.FindById("b"),0,s.FindById("x"),new Hit(100,true),"audit",false);
            Assert.AreEqual(100,s.FindById("x").FlurryStore); Assert.AreEqual(130,Release(s));
        }
        [Test] public void EachContributionKeepsItsLayersAndShieldsOnlyAbsorbTheRelease()
        {
            var s=Fight(donorRank:3,enemy:new SideRules(0,0,false,spell:ChiefSpell.SteelStorm)); var x=s.FindById("x"); Field(State(s,1),"WallTicks",100);
            Freeze(s); Call(s,"ApplyHit",s.FindById("b"),0,x,new Hit(100).Add(HitLayer.Armor,-50),"audit",false);
            int stored=x.FlurryStore; Assert.Greater(stored,0); Assert.Less(stored,350);
            Set(x.Statuses[StatusKind.Shield],"Ticks",100); Set(x.Statuses[StatusKind.Shield],"Size",10);
            Assert.AreEqual(stored-10,Release(s),"wall and armour resolved once; live shield consumes ten");
        }
        [Test] public void StoredDamageSurvivesItsFreezerFallingAndReleasesOnlyOnce()
        {
            var s=Fight(freezerRank:3,donorRank:1); Freeze(s); Hit(s); Set(s.FindById("a"),"Alive",false);
            Field(State(s,0),"ParryBars",3); Assert.AreEqual(130,Release(s));
        }
        [Test] public void MixedContributorsKeepTheirOwnDamageInOneThaw()
        {
            var s=Fight(freezerRank:1,donorRank:3); Freeze(s); Hit(s);
            Call(s,"ApplyDamage",s.FindById("a"),0,s.FindById("x"),100,"audit",false); Assert.AreEqual(800,Release(s));
        }
        [Test] public void OrdinaryHitsStillIgnoreTheHeldBarBonus()
        {
            var s=Fight(); Field(State(s,0),"ParryBars",3); int hp=s.FindById("x").Hp; Hit(s); Assert.AreEqual(100,hp-s.FindById("x").Hp);
        }
        [Test] public void TheThawsOwnFlavourGrowthStillAppliesOnce()
        {
            var s=Fight(donorRank:3,comboRank:3); Freeze(s); Hit(s); Assert.AreEqual(700,s.FindById("x").FlurryStore);
            Assert.AreEqual(4900,Release(s));
        }
        [TestCase(ChiefKit.Skullpeak,"JabTicks","JabMedicId","AdvanceJab")]
        [TestCase(ChiefKit.Stoneveil,"BasinTicks","BasinCarrierId","AdvanceBasin")]
        [TestCase(ChiefKit.Thundercoast,"CrankTicks","EngineerId","AdvanceSluice")]
        public void FrozenSupportActorsPauseTheirOwnMove(ChiefKit kit,string clock,string actor,string advance)
        {
            var s=Fight(target:U("x",role:UnitRole.Chief),support:new[]{U("helper",UnitClass.Medic)},
                enemy:new SideRules(0,0,false,kit:kit),rules:FightRules.CampaignChiefTactics);
            var st=State(s,1); Field(st,clock,2); Field(st,actor,"helper");
            Set(s.FindById("helper").Statuses[StatusKind.Pin],"Ticks",100);
            Call(s,advance,1); Assert.AreEqual(2,Clock(st,clock));
            Unpin(s.FindById("helper")); Call(s,advance,1); Assert.AreEqual(1,Clock(st,clock));
        }
        [Test] public void ASharedRifleVolleyLosesOnlyTheFrozenShootersShot()
        {
            var s=Fight(target:U("x",role:UnitRole.Chief),support:new[]{U("frozen"),U("ready")},
                enemy:new SideRules(0,0,false,kit:ChiefKit.Skullpeak),rules:FightRules.CampaignChiefTactics);
            var st=State(s,1); Field(st,"RifleTicks",1); Field(st,"RifleTargetId","a");
            Set(s.FindById("frozen"),"RifleTicks",1); Set(s.FindById("ready"),"RifleTicks",1);
            Set(s.FindById("frozen").Statuses[StatusKind.Pin],"Ticks",100);
            Assert.AreEqual("ready",s.Threats(0).Single(t=>t.Name==ChiefKits.RifleName).ActorId);
            Call(s,"AdvanceRifle",1);
            Assert.IsFalse(s.Events.Any(e=>e.Kind==BattleEventKind.Hit&&e.ActorId=="frozen"));
            Assert.IsTrue(s.Events.Any(e=>e.Kind==BattleEventKind.Hit&&e.ActorId=="ready"));
        }
        [Test] public void IronAdvanceWaitsForItsChief()
        {
            var s=Fight(target:U("x",role:UnitRole.Chief),enemy:new SideRules(0,0,false,kit:ChiefKit.Skullpeak),rules:FightRules.CampaignChiefTactics);
            var st=State(s,1); Field(st,"AdvanceTicks",2); Set(s.FindById("x").Statuses[StatusKind.Pin],"Ticks",100);
            Call(s,"MarchOn",1); Assert.AreEqual(2,Clock(st,"AdvanceTicks"));
            Unpin(s.FindById("x")); Call(s,"MarchOn",1); Assert.AreEqual(1,Clock(st,"AdvanceTicks"));
        }
        [Test] public void AllBuildersPreserveTheNewVersionSwitch()
        {
            foreach(var input in new[]{FightRules.Off.WithFreezeContracts(),Rules(FightRules.Campaign)})
            foreach(var m in typeof(FightRules).GetMethods().Where(m=>m.Name.StartsWith("With")&&m.ReturnType==typeof(FightRules)))
            {
                var r=(FightRules)m.Invoke(input,m.GetParameters().Length==0?new object[0]:new object[]{4});
                Assert.IsTrue(r.FreezeContracts&&r.Contracts&&r.Corrections,m.Name);
            }
        }
        [Test] public void NaturalFlurriesReplayExactlyIncludingAnActiveFreeze()
        {
            var s=Fight(donorRank:3); var ses=new SimSession(s); bool active=false;
            for(int i=0;i<1200&&!s.IsFinished;i++)
            {
                ses.Tick();
                if(!active&&s.FindById("x").FlurryStore>0)
                {
                    active=true; var paused=ReplayRunner.Run(new BattleSimulation(s.Setup),ses.Log.CloneForReplay(),s.CurrentTick);
                    Assert.IsTrue(paused.Success,paused.Failure); Assert.AreEqual(ses.Checksum,paused.Checksum);
                }
            }
            Assert.IsTrue(active,"naturally entered an active freeze with stored damage");
            Assert.IsTrue(s.Events.Any(e=>e.Kind==BattleEventKind.Hit&&e.Detail==ClassSpells.FlurryDetail));
            var replay=ReplayRunner.Run(new BattleSimulation(s.Setup),ses.Log.CloneForReplay(),s.CurrentTick);
            Assert.IsTrue(replay.Success,replay.Failure); Assert.AreEqual(ses.Checksum,replay.Checksum);
        }
        [Test] public void ActiveSaveAndArchivedEvidenceKeepTheirVersionAcrossTheBoundary()
        {
            foreach(int version in new[]{36,37})
            {
                var c=Campaign.New(11,new CampaignTuning{RulesCap=version}); var f=c.StartTutorial();
                for(int i=0;i<60;i++)f.Tick(); var loaded=CampaignSave.Read(CampaignSave.Write(c));
                Assert.IsEmpty(loaded.LoadNote); Assert.AreEqual(f.Session.Checksum,loaded.Active.Session.Checksum);
                Assert.AreEqual(version==37,loaded.Active.Setup.Fight.FreezeContracts);
                CampaignPilot.PlayFight(c,f,new CampaignPilot(PilotStyle.Reference,11));
                loaded=CampaignSave.Read(CampaignSave.Write(c)); Assert.IsEmpty(loaded.LoadNote);
                Assert.IsTrue(loaded.Archive.TryReplay(f.Setup.FightId,out BattleSimulation replay,out string why),why);
                Assert.AreEqual(version==37,replay.Setup.Fight.FreezeContracts);
                Assert.AreEqual(f.Session.Checksum,StateChecksum.Compute(replay.OrderedStateSnapshot));
            }
        }
    }
}
