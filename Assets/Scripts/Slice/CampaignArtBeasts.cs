using System;
using System.Collections.Generic;
using System.Text;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // Presentation only. Availability, targets, receipts and purchases belong to Campaign/Sim.
        Encounter SelectedEncounter => _offer == -2 ? _campaign.BeastOffer : _campaign.Offers[_offer];
        RectTransform _beastScreen, _beastPanel;
        PxText _beastState;
        readonly Dictionary<BeastPart, PxText> _beastParts = new Dictionary<BeastPart, PxText>();
        readonly Dictionary<string, PxText> _beastMarks = new Dictionary<string, PxText>();

        void TakeBeast()
        {
            if (!_campaign.CanStartBeast(out string why)) { _notice = why; return; }
            _offer = -2; _squad.Clear();
            foreach (var f in _campaign.DefaultSquad(SelectedEncounter.SquadSize)) _squad.Add(f.Id);
            SetPage(Page.Squad);
        }

        void BeastRoadButton()
        {
            var offer = _campaign.BeastOffer;
            if (offer == null) return;
            PxButton("beast-offer", "HUNT: " + BeastKits.Name(offer.Beast), 16, 420, 224, 26, TakeBeast);
            SetPx(PxLabel(_screen, 250, 426), "OPTIONAL / NO ROAD STEP / TROPHIES AT CAMP", PxGold);
        }

        string BeastGuide(BeastKit kit, bool parryCenter = false)
        {
            bool lives=(_fight!=null||_playback!=null)?ViewSim.RoadCombatOn:LivesRun;
            bool scenes=(_fight!=null||_playback!=null)?ViewSim.BeastScenesOn:ReviewRulesCap>=Campaign.BeastSceneRules;
            string shared = "Optional fight. Costs no road step. No enemy nerve: break parts. Your nerve and leader still matter.\n" +
                (lives?"Win: broken parts become trophies at camp. Lose: no trophies or life lost; your whole squad returns.\n":
                "Win: broken parts become trophies at camp, safe even if the run is lost. Lose: no trophies; fallen stay down; nobody captured.\n") +
                "Each breaker earns a road scar for this run, strengthening their spell against beasts.\n\n";
            if((_fight!=null||_playback!=null)&&ViewSim.TightParryOn) shared += TightGuide+"\n\n";
            else if(parryCenter) shared += "EVERY TOLD BEAT: time a fresh live Hold in its last half second to parry. Paused Hold never parries. Early Hold costs nerve. The following are safe alternatives.\n";
            if (kit == BeastKit.CageBear) return shared +
                "RIBCAGE: halves hits on the bear and seals the heart. Unleash doubles hits on the cage while active.\n" +
                (scenes?"CLAW / SLAM + GRAB: wave hits both rows before the grab. Fall Back dodges both; Warden halves the wave and blocks the grab.":"CLAW / GRAB: Fall Back or Warden cover.")+" Held fighters cannot act or be healed. Break the claw to free them.\n" +
                "MAW / ROAR: Hold blocks it. Focus the maw during its tell to interrupt.\n" +
                "HEART / PULSE: mends parts while caged; hits your whole squad once exposed. Hold or Medic; no dodge.\n" +
                "Break the heart to kill it. Other standing parts drop without trophies.\n" +
                "OVERDRIVE: after two broken parts, faster moves and doubled crush. LAST STAND: only heart remains, Pulse every 4s.\n" +
                "It never flees. Time runs out: you lose.";
            return shared +
                (scenes?"JAW / BITE: hits the marked fighter's whole row. ":"JAW / BITE: ")+"Time Hold while running to parry, or Warden cover reflects the bite. Paused Hold never parries.\n" +
                "FIN / DIVE: mostly avoids your hits while submerged. Fall Back dodges; a Trapper snare holds it up.\n" +
                "SPINE / VOLLEY: back row hit. Fall Back or Warden cover.\n" +
                "TAIL / SWEEP: "+(((_fight!=null||_playback!=null)?ViewSim.BeastScenesOn:ReviewRulesCap>=Campaign.BeastSceneRules)?"both rows hit.":"front row hit.")+" Fall Back, Warden cover or Medic." + (parryCenter ? " Time Hold to parry.\n" : " Hold does not soak it.\n") +
                "BLOOD FRENZY: after two parts break, faster moves and bleeding hits.\n" +
                "LAST STAND: after three break, 15s to break the last; otherwise it flees with three trophies left behind.\n" +
                "Break all four to kill it. At the fight time limit it flees; keep what you broke.";
        }

        void BeastBrief(PxLines lines, Encounter encounter)
        {
            lines.Add("OPTIONAL / NO ROAD STEP", PxGold)
                .Add("No enemy nerve. Break parts to win. Your nerve and leader still matter.", PxBone)
                .Add(encounter.Beast == BeastKit.CageBear ?
                    "Crack the ribcage to reach the heart. Break the heart to kill it; only broken parts give trophies. Timeout: you lose." :
                    "Break all four, or hold out until it flees. Last stand: 15s to break the last part.", PxBone)
                .Add(LivesRun?"Win: trophies safe at camp. Lose: no trophies or life lost; your whole squad returns.":"Win: trophies safe at camp. Lose: none; fallen stay down, nobody captured.", PxMuted)
                .Add(encounter.Beast==BeastKit.SandShark?"Named parts share one shark. Select a part to Focus it.":"Bear art pending. Named part markers show the targets.", PxMuted);
            PxButton("beast-guide", "BEAST MOVES", 464, 362, 232, 26, () => { CloseModal(); Modal("BEAST MOVES", BeastGuide(encounter.Beast, true)); });
        }

        void ShowBeastMoves()
        {
            _paused = true; CloseModal(); Modal("BEAST MOVES", BeastGuide(ViewSim.Beast(1), ViewSim.ParryCenterOn));
        }

        bool BeastSevered(BattleSimulation sim, BeastPart part)
        {
            foreach(var e in sim.Events)if(e.Kind==BattleEventKind.Sever&&e.Detail==BeastKits.Name(part))return true;
            return false;
        }

        string BeastPartLine(BattleSimulation sim, BeastPart part)
        {
            var unit = sim.Part(part);
            string name = BeastKits.Name(part).ToUpperInvariant();
            if (unit == null) return name + " --";
            if (!unit.Alive) return name + (BeastSevered(sim,part) ? " BROKEN" : "\nDOWN / NO TROPHY");
            string hp = name + " " + unit.Hp + "/" + unit.MaxHp;
            if (part == BeastPart.Ribcage) return hp + "\nFRONT / ARMOUR\nUNLEASH CRACKS CAGE";
            if(ViewOver)return hp+"\nSURVIVED";
            if(part==BeastPart.Claw&&sim.BeastHeldId.Length>0)return hp+"\nHOLDING / BREAK TO FREE";

            int tell = sim.BeastTellTicks(part);
            bool jawNow = sim.BiteParryable && tell <= FightRules.PerfectHoldTicks && sim.HoldRefreshTicksLeft(0)==0 && sim.CanUse(0,Directive.Hold,out _);
            if (part==BeastPart.Jaw && sim.ParryCenterOn)
            {
                jawNow=false; ReadClarity(sim);
                foreach(var threat in _threatReads) if(threat.ActorId==unit.Id && LiveParryCue(sim,threat)) {jawNow=true;break;}
            }
            string answer = part==BeastPart.Jaw?(_playback!=null?"REPLAY / RECORDED DEFENCE":_pilotOn?"AUTO / TIMED DEFENCE":_paused?"RESUME TO PARRY":jawNow?"HOLD NOW / PARRY":"TIME HOLD / WARDEN GUARD"):
                part==BeastPart.Maw?"HOLD / FOCUS MAW":part==BeastPart.Heart?(sim.BeastCaged?"SEALED / MENDS PARTS":"HOLD / MEDIC; NO DODGE"):
                part==BeastPart.Fin?"FALL BACK / TRAPPER":part==BeastPart.Tail?"FALL BACK / COVER / MEDIC":"FALL BACK / COVER";
            if(sim.TightParryOn&&part==BeastPart.Jaw)answer="PARRY / WATCH BODY";
            return hp + "\n" + BeastKits.MoveName(part) + (tell > 0 ? " NOW " + Seconds(tell) : " IN " + Seconds(sim.BeastNextTicks(part)))+"\n"+(tell>0?answer:part==BeastPart.Heart&&sim.BeastCaged?"SEALED / BACK":unit.Row==Row.Front?"FRONT":"BACK");
        }

        void DrawBeasts(BattleSimulation sim)
        {
            if (_beastScreen != _screen)
            {
                _beastScreen = _screen; _beastPanel = null; _beastParts.Clear(); _beastMarks.Clear();
                if (!sim.BeastOn) return;
                _beastPanel = Rect(_readoutLayer, "Beast parts", 0, 0, W, H);
                Panel(_beastPanel, 22*Px, 70*Px, 676*Px, 88*Px, new Color(.06f,.05f,.07f,.92f));
                _beastState = PxLabel(_beastPanel, 28, 75);
                PxButton("beast-moves", "MOVES", 616, 73, 76, 18, ShowBeastMoves, _beastPanel);
                int i=0;
                foreach (var part in BeastKits.Parts(sim.Beast(1)))
                {
                    int slot=i++;
                    var unit=sim.Part(part);
                    var button=PxButton("beast-part-"+part, "", 28+slot*166, 104, 160, 46, () => SelectFigure(unit), _beastPanel);
                    _beastParts[part]=PxLabel(button.transform, 5, 7);
                    OnHover(button.gameObject, () => ClassFighterHint(unit));
                }
                foreach(var f in _figures) _beastMarks[f.unit.Id]=PxLabel(f.readoutRoot,0,-Mathf.CeilToInt(f.head/Px)-14,1);
            }
            if (_beastPanel==null) return;
            var kit=sim.Beast(1);
            string state=BeastKits.Name(kit)+" / "+(sim.BeastPhase>=2?BeastKits.PhaseName(kit,sim.BeastPhase):"BREAK PARTS");
            if(ViewOver) state="FIGHT OVER / "+BeastKits.Name(kit);
            else if(sim.BeastEscapeTicks>0) state+=" / ESCAPES IN "+Seconds(sim.BeastEscapeTicks);
            state+="\n3 FOCUS -> PART / "+(kit==BeastKit.CageBear?(sim.BeastCaged?"CAGE UP / HEART SEALED":"HEART EXPOSED / TIMEOUT LOSES"):
                sim.BeastUnder?"SUBMERGED / BLOWS MOSTLY MISS":"NO NERVE / BREAK ALL FOUR OR SURVIVE");
            SetTacticsLines(_beastState,state,PxGold);
            foreach(var part in BeastKits.Parts(kit))
            {
                var u=sim.Part(part); bool live=!ViewOver&&u!=null&&u.Alive;
                SetTacticsLines(_beastParts[part],BeastPartLine(sim,part),sim.BeastTellTicks(part)>0&&live?PxYellow:live?PxBone:PxMuted);
                _buttons["beast-part-"+part].interactable=live&&_playback==null;
            }
            foreach(var f in _figures)
            {
                var marks=new List<string>();
                if(!ViewOver&&f.unit.Alive)
                {
                    if(sim.BeastHeldId==f.unit.Id) marks.Add("HELD / BREAK CLAW");
                    foreach(var part in BeastKits.Parts(kit))
                        if(sim.BeastTellTicks(part)>0)
                        {
                            var victim=sim.FindById(sim.BeastTellTargetId(part));
                            bool marked=sim.BeastTellTargetId(part)==f.unit.Id;
                            if(sim.BeastScenesOn&&f.unit.Side==0)
                                marked=part==BeastPart.Tail||part==BeastPart.Claw||part==BeastPart.Jaw&&victim!=null&&victim.Row==f.unit.Row;
                            if(marked)marks.Add(BeastKits.MoveName(part)+" TARGET");
                        }
                }
                SetTacticsLines(_beastMarks[f.unit.Id],string.Join("\n",marks),PxYellow);
            }
        }

        void BeastEvent(BattleEvent e)
        {
            if(!ViewSim.BeastOn||_seeking)return;
            if(e.Kind==BattleEventKind.Phase){Callout(1,e.Detail.ToUpperInvariant(),PxOrange);Feed(e.Detail+".");}
            if(e.Kind==BattleEventKind.Sever){Callout(1,e.Detail.ToUpperInvariant()+" BROKEN",PxGold);Feed(e.Detail+" broken. Its move is gone.");}
            if(e.Kind==BattleEventKind.Flee){Callout(1,"BEAST FLED",PxGold);Feed("The beast fled. Broken parts go to camp.");}
        }

        void BeastResultLines(PxLines lines, EncounterResult r)
        {
            if(r.Encounter.Kind!=EncounterKind.Beast)return;
            lines.Gap().Add("TROPHIES BANKED: "+r.Trophies.Count,PxGold);
            foreach(var part in r.Trophies)lines.Add(BeastKits.Name(part),PxBone);
            if(!r.Won)lines.Add("No trophies from this defeat.",PxMuted);
            else lines.Add("Safe at camp. The Butcher forges each into one permanent class piece.",PxMuted);
            foreach(var scar in r.ScarsEarned)lines.Add(scar.FighterName+": "+scar.Name,PxMint);
            if(r.ScarsEarned.Count>0)lines.Add("Scars strengthen spells against beasts for this run.",PxMuted);
        }

        void ShowTrophies(BeastKit kit=BeastKit.SandShark)
        {
            CloseModal(); Modal("BEAST TROPHIES", "");
            PxWrite(_modal,42,60,636,new PxLines().Add(BeastKits.Name(kit),PxGold)
                .Add("One trophy -> one permanent class piece. No material cost. Forge at camp.",PxBone)
                .Add("Every future run carries your forged pieces; first available fighter of each class wears one.",PxMuted));
            int i=0;
            foreach(var part in BeastKits.Parts(kit))
            {
                var p=part;
                PxButton("trophy-"+part, BeastKits.Name(part)+" x"+_campaign.TrophyCount(part)+" / RECIPES",42,118+i++*34,636,28,()=>ShowTrophyRecipes(p),_modal);
            }
            var forged=new List<string>();foreach(var piece in BeastPieces.Of(kit))if(_campaign.BeastPieceForged(piece))forged.Add(BeastPieces.Name(piece));
            PxWrite(_modal,42,266,636,new PxLines().Add("FORGED: "+(forged.Count==0?"none":string.Join(", ",forged)),PxMint));
            PxButton("trophy-other",kit==BeastKit.SandShark?"CAGE BEAR":"SAND SHARK",16,398,220,28,()=>ShowTrophies(kit==BeastKit.SandShark?BeastKit.CageBear:BeastKit.SandShark),_modal);
            PxButton("trophy-forge","TABARD FORGE",484,398,220,28,()=>ShowForge(),_modal);
        }

        void ShowTrophyRecipes(BeastPart part, string receipt="")
        {
            CloseModal(); Modal("TROPHY RECIPES", "");
            PxWrite(_modal,42,60,636,new PxLines().Add(BeastKits.Name(BeastKits.KitOf(part))+" / "+BeastKits.Name(part)+" x"+_campaign.TrophyCount(part),PxGold)
                .Add("Spend ONE trophy on ONE piece below. Permanent; no materials charged.",PxBone).Add(receipt,PxMint));
            int i=0;
            foreach(var piece in BeastPieces.From(part))
            {
                var p=piece; bool can=_campaign.CanForgeTrophy(part,p,out string why);
                var b=PxButton("forge-beast-"+p,"",42,96+i++*48,636,46,()=>ForgeBeast(part,p),_modal);
                SetPx(PxLabel(b.transform,10,8),BeastPieces.Name(p)+" / "+BeastPieces.ClassOf(p)+" / "+ClassSpells.Name(BeastPieces.BendOf(p))+" / "+CampaignGearRules.StatText(BeastPieces.StatOf(p),BeastPieces.Gear(p).Bonus),PxBone);
                SetPx(PxLabel(b.transform,10,28),can?"FORGE: 1 "+BeastKits.Name(part)+" trophy":why,can?PxGold:PxMuted);
                SetPx(PxLabel(b.transform,10,18),ClassSpells.Describe(BeastPieces.BendOf(p)),PxMuted);
                b.interactable=can;
            }
            PxButton("trophy-back","ALL TROPHIES",16,398,220,28,()=>ShowTrophies(BeastKits.KitOf(part)),_modal);
        }

        void ForgeBeast(BeastPart part, BeastPiece piece)
        {
            if(!_campaign.TryForgeTrophy(part,piece,out string why)){ShowTrophyRecipes(part,why);return;}
            Save(); ShowTrophyRecipes(part,BeastPieces.Name(piece)+" forged for every future run.");
        }
    }
}
