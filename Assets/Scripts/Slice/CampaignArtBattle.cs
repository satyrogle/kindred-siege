using System;
using System.Collections.Generic;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // The world is drawn at 1x (Jacob, 11 Oct 2026: "yes go 1x, do it"; at 2x "everybody is cooped up in a
        // singular place ... because every unit is so big"): the stage and the fighters at their own pixel
        // size, one screen pixel each. The menus, buttons, bars and text stay on the 2x kit.
        const float ArtScale=1;
        // The stage on the 2x UI grid. The top bar is 40 tall and covers the stage's top 7 rows, so both of its
        // text rows sit between the plate's gold trim lines. At 1x the stage art is StageW*Px by StageH*Px.
        const int StageX=16,StageY=33,StageW=688,StageH=384,TopBarH=40;
        // Fight feedback (Jacob, 10 Oct 2026: "i dont see any feedback from healing or damage bars are
        // same i dont see what orders do"): a bar under every fighter, numbers, a hit flash, and each order
        // shown on the fighters it touches while it lasts.
        static readonly Color BarBack=Hex("141117");
        static readonly string[] OrderIcons={"hold","fallback","focus","unleash","eclipse"};
        [Serializable] sealed class FrameRect { public int x,y,w,h; }
        [Serializable] sealed class Frame { public FrameRect frame;public int duration; }
        [Serializable] sealed class Sheet { public Frame[] frames;public float anchorX,anchorY;public int hitFrame;public float scale; }
        sealed class Clip
        {
            public Texture2D texture;public Sheet sheet;public float seconds;
            public int FrameAt(float time,bool loop)
            {
                float ms=(loop?Mathf.Repeat(time,seconds):Mathf.Clamp(time,0,seconds))*1000;
                for(int i=0;i<sheet.frames.Length;i++){if(ms<sheet.frames[i].duration)return i;ms-=sheet.frames[i].duration;}
                return sheet.frames.Length-1;
            }
        }
        sealed class Figure
        {
            public BattleUnit unit;public RectTransform root,readoutRoot;public RawImage body;
            public Image marker;public Button target;
            public Clip idle,attack,hit,death;public Vector2 feet;
            public string artKey;
            public float attackAt=-100,hitAt=-100,deathAt=-1,attackLength;
            public bool anticipated,staticFront;public int lane;
            // The bar under the fighter (marker is its frame), the pale chip that shows a hit before it
            // drains away, and the tag for what is on this fighter right now.
            public Image barFill,barLag;public PxText tag;
            public float barWidth,head,shownHp,lagUntil,chipAt,flashAt=-100;public Color flash=Color.white;
            // The number on screen for this fighter's hits and heals, so blows that land together add up.
            public Number lastHit,lastHeal;
            // Only the leader has these so far: a guard raised while Hold runs, and a victory.
            public Clip block,victory;public float hitLength=.22f,blockAt=-100,blockEndAt=-100;public bool blocking;
            public float poseTime,poseHold,strikeAt=-100,recoilAt=-100,whiteAt=-100,strikePixels,recoilPixels;
            public Material hitMaterial;
            public float hitPoseAt,staggerUntil=-100;
            public Clip drawnClip;public int drawnFrame;
            public bool majorFreeze;public float majorWhiteUntil=-100;
        }
        sealed class Number { public RawImage image;public Vector2 origin;public float at,life=.95f,rise=20;public int sum;public bool payoff; }
        readonly Dictionary<string,Clip> _clips=new Dictionary<string,Clip>();
        readonly List<Figure> _figures=new List<Figure>();
        readonly List<Number> _numbers=new List<Number>();
        readonly PxText[] _pxLane=new PxText[2];
        readonly PxText[] _orderTitle=new PxText[5],_orderEffect=new PxText[5],_orderCost=new PxText[5];
        PxText _pxTitle,_pxStats,_pxBanner,_pxBanner2;
        // The latest big word per side, shown in that side's lane at the top of the stage (Codex re-audit,
        // 10 Oct 2026: words stacked over the fighters). The newest replaces the one before.
        readonly string[] _flashText={"",""};readonly Color32[] _flashColour={PxBone,PxBone};readonly float[] _flashAt={-100,-100};
        RectTransform _stage,_effects;
        Texture2D _stageArt;
        string _selected="";
        void ClearBattle()
        {
            ClearBloodridgeReview();
            ClearRegionalStage();
            if(_sharkMaterial!=null){Destroy(_sharkMaterial);_sharkMaterial=null;}
            foreach(var f in _figures)if(f.hitMaterial!=null)Destroy(f.hitMaterial);
            _figures.Clear();_numbers.Clear();
            _shakeAt=_lastShakeAt=-100;
            _lastWhiteAt=-100;
            _flashAt[0]=_flashAt[1]=-100;
            _whyPanel=null;
            _coverCalloutAt=-100;
            ClearFightRules();
            _windupName.Clear();
            // These textures are owned only by this presenter; unloading between fights bounds memory.
            foreach(var c in _clips.Values)if(c.texture!=null)Resources.UnloadAsset(c.texture);
            _clips.Clear();
            if(_stageArt!=null){Resources.UnloadAsset(_stageArt);_stageArt=null;}
        }
        Clip LoadClip(string key)
        {
            if(_clips.TryGetValue(key,out Clip c))return c;
            string path="CampaignArt/"+key;
            var json=Resources.Load<TextAsset>(path);
            if(json==null){path="ClarthStudy/"+key;json=Resources.Load<TextAsset>(path);}
            var tex=Resources.Load<Texture2D>(path);
            if(json==null||tex==null)throw new InvalidOperationException("Approved art missing: "+key);
            c=new Clip{texture=tex,sheet=JsonUtility.FromJson<Sheet>(json.text)};
            foreach(var f in c.sheet.frames)c.seconds+=f.duration/1000f;
            _clips[key]=c;return c;
        }
        string ArtKey(BattleUnit u)
        {
            if(ViewSim.TryPartOf(u,out _))return null;
            string tribe=FigureTribe(u);
            if(u.Id==Campaign.LeaderId)return null;
            // The two new tribes' PixelLab sheets (Jacob, 19 Oct 2026: "bind both tribes in ArtKey", then "do both and fix em"):
            // everyone faces SW as an enemy, and the five squad classes also face SE for when their band fights for you. Chiefs,
            // champions and the Stoneveil trial's shaman only ever fight against you. In a hunt or an ambush
            // the hunters keep their own sheets below.
            if((tribe==CampaignTribes.StoneveilId||tribe==CampaignTribes.ThundercoastId)&&!(HunterFight(ViewKind)&&u.Side==1))
            {
                bool special=u.Id.EndsWith(".chief")||u.Id.EndsWith(".champion")||u.Id.EndsWith(".shaman");
                if(u.Side!=1)return special?null:tribe+"_"+u.Class.ToString().ToLowerInvariant()+"_se";
                return tribe+"_"+(u.Id.EndsWith(".chief")?"chief":u.Id.EndsWith(".champion")?"champion":u.Id.EndsWith(".shaman")&&tribe==CampaignTribes.StoneveilId?"shaman":u.Class.ToString().ToLowerInvariant())+"_sw";
            }
            // Jacob approved these delivered SW sheets as-is on 18 Oct; other special facings stay pending.
            if(u.Id.EndsWith(".shaman"))return u.Side==1&&tribe==CampaignTribes.BloodridgeId?"bloodridge_shaman_sw":null;
            if(u.Id.EndsWith(".champion"))return u.Side!=1?null:tribe==CampaignTribes.BloodridgeId?"bloodridge_champion_sw":tribe==CampaignTribes.SkullpeakId?"skullpeak_champion_sw":null;
            // The hunters' placeholder sprites (PixelLab, Jacob's picks, 16 Oct 2026), in the hunt and an ambush alike: each by
            // class, the named hunter at the captain's height.
            if(HunterFight(ViewKind)&&u.Side==1)return u.IsLeader?"hunter_captain_sw":"hunter_"+u.Class.ToString().ToLowerInvariant()+"_sw";
            string prefix=tribe==CampaignTribes.ClarthId?"clarth":tribe==CampaignTribes.BloodridgeId?"blood_rot":tribe==CampaignTribes.SkullpeakId?"skull":null;
            if(prefix==null)return null;
            if(u.Id.EndsWith(".chief"))prefix=tribe==CampaignTribes.BloodridgeId?"bloodridge_chief":tribe==CampaignTribes.SkullpeakId?"skullpeak_chief":"clarth_chief";
            else prefix+="_"+u.Class.ToString().ToLowerInvariant();
            return prefix+(u.Side==0?"_se":"_sw");
        }
        /// Legacy regional backdrops at 1x. Bloodridge normally uses the packaged layered burial court;
        /// the tutorial and other regions keep their existing bindings until replacement art is selected.
        Texture2D StageArt()
        {
            if(_stageArt!=null)return _stageArt;
            string tribe=ViewTribe;
            string region=ViewKind==EncounterKind.Tutorial||ViewSim.Beast(1)==BeastKit.SandShark?"broken_wastes":
                tribe==CampaignTribes.BloodridgeId?"bloodridge":tribe==CampaignTribes.SkullpeakId?"skullpeak":"broken_wastes";
            _stageArt=Resources.Load<Texture2D>("CampaignArt/Stages/"+region);
            if(_stageArt==null)throw new InvalidOperationException("Stage art missing: CampaignArt/Stages/"+region);
            _stageArt.filterMode=FilterMode.Point;
            return _stageArt;
        }
        void BuildBattle()
        {
            LoadPixelKit();
            _visualTime=_endAge=0;_selected="";_hover="";
            _majorUntil=_majorLast=-100;
            _stage=Rect(_screen,"Battlefield",0,0,W,H);
            var bg=Raw(_stage,"Stage",StageX*Px,StageY*Px,StageW*Px,StageH*Px);
            bool shark=ViewSim.Beast(1)==BeastKit.SandShark;
            if(shark||!UseBloodridgeCourt&&!UseRegionalStage)bg.texture=ReviewStage()??StageArt();
            if(!shark){BuildBloodridgeReviewRear(bg);BuildRegionalStageRear(bg);}
            var total=new int[2,2];var placed=new int[2,2];
            bool chiefPresent=false;foreach(var u in ViewSim.Units)if(u.Side==1&&u.Id.EndsWith(".chief"))chiefPresent=true;
            foreach(var u in ViewSim.Units)if(!u.IsLeader)total[u.Side,(int)u.Row]++;
            // The 1x formation: the armies meet in the lower middle of the stage, and the top row's heads stay
            // under the landmark at the foot of the cliff. Screen pixels, starting values.
            foreach(var u in ViewSim.Units)
            {
                int row=(int)u.Row;int slot=u.IsLeader?0:placed[u.Side,row]++;int count=total[u.Side,row];
                float y=count<=1?560:420+slot*(280f/Mathf.Max(1,count-1));
                float x=u.Side==0?(u.Row==Row.Front?600:420):(u.Row==Row.Front?840:1020);
                if(chiefPresent&&u.Side==1&&u.Row==Row.Back&&!u.IsLeader)y=420+slot*110;
                if(u.IsLeader){x=u.Side==0?250:1160;y=u.Id.EndsWith(".chief")?620:640;}
                if(ViewKind==EncounterKind.Duel){x=u.Side==0?560:860;y=620;}
                if(HunterFight(ViewKind)&&u.Side==1)
                {
                    // Approved hunter sheets retain their front/back lanes and clear targets.
                    x=u.IsLeader?1220:(u.Row==Row.Front?790:1030)+slot/3*104;
                    y=u.IsLeader?630:430+slot%3*135;
                }
                if(ViewSim.BeastOn&&!u.IsLeader)y=count<=1?580:480+slot*(220f/Mathf.Max(1,count-1));
                var feet=new Vector2(Mathf.Round(x),Mathf.Round(y));
                if(IsSharkPart(u))feet=SharkPartFeet(u);
                var f=new Figure{unit=u,feet=feet,artKey=ArtKey(u),shownHp=u.Hp};
                f.root=Rect(_stage,u.Id,feet.x,feet.y,1,1);
                if(u.Id==Campaign.LeaderId)
                {
                    // His own battle clips (PixelLab, 10 Oct 2026, owner verdict pending), facing SE like the
                    // squad. Parry and movement are drawn too, but no clip plays them yet.
                    f.idle=LoadClip("Leader/leader_se_idle");f.attack=LoadClip("Leader/leader_se_attack");
                    f.hit=LoadClip("Leader/leader_se_hit");f.death=LoadClip("Leader/leader_se_death");
                    f.block=LoadClip("Leader/leader_se_block");f.victory=LoadClip("Leader/leader_se_victory");
                    f.body=Raw(f.root,"Leader battle clips",0,0,1,1);
                    f.attackLength=u.Stats.CooldownTicks/(float)SimClock.TicksPerSecond;
                    f.hitLength=f.hit.seconds;
                    f.head=f.idle.sheet.anchorY*ArtScale;
                }
                else if(f.artKey!=null)
                {
                    f.idle=LoadClip(f.artKey+"_idle");f.attack=LoadClip(f.artKey+"_attack");
                    f.hit=LoadClip(f.artKey+"_hit");f.death=LoadClip(f.artKey+"_death");
                    f.body=Raw(f.root,"Approved sprite",0,0,1,1);
                    f.attackLength=u.Stats.CooldownTicks/(float)SimClock.TicksPerSecond;
                    f.head=f.idle.sheet.anchorY*ArtScale;
                }
                else if(IsSharkPart(u)) { f.head=20; }
                else
                {
                    f.head=55*Px;
                    PxImage(f.root,"Token",Plate(_kitButton,12,46,52),-23,-52);
                    string title=ViewSim.TryPartOf(u,out var markerPart)?BeastKits.Name(markerPart).ToUpperInvariant():HunterFight(ViewKind)&&u.Side==1?(u.IsLeader?"THE HUNTER":"HUNTER"):
                        u.Id.EndsWith(".champion")?"CHAMPION":"SHAMAN";
                    SetPx(PxLabel(f.root,0,-40,1),title,PxBone);
                    SetPx(PxLabel(f.root,0,-26,1),"ART",PxMuted);
                    SetPx(PxLabel(f.root,0,-15,1),"PENDING",PxMuted);
                }
                // Point at a fighter to see who it is; click an enemy while Focus Fire is armed to target it.
                // The area covers the fighter's body at 1x.
                float reach=Mathf.Clamp(f.head*.8f,60,140);
                var hitbox=ActionButton(f.root,-28,-reach,56,reach+6,"",()=>SelectFigure(u));
                hitbox.image.color=Color.clear;hitbox.name="target-"+u.Id;_buttons["target-"+u.Id]=hitbox;f.target=hitbox;
                OnHover(hitbox.gameObject,()=>ClassFighterHint(u));
                PrepareCombatFeedback(f);
                _figures.Add(f);
            }
            _figures.Sort((a,b)=>a.feet.y.CompareTo(b.feet.y));foreach(var f in _figures)f.root.SetAsLastSibling();
            BuildBloodridgeReviewFront();
            // Bars and tags sit on a layer above every fighter, so one standing lower never hides them.
            // They go under the feet: the columns are stacked, so above the head they would sit on the
            // feet of the fighter above.
            var overlay=Rect(_stage,"Bars and order tags",0,0,W,H);
            foreach(var f in _figures)
            {
                f.barWidth=f.unit.IsLeader?44:30;
                f.readoutRoot=Rect(overlay,"Readouts "+f.unit.Id,f.feet.x,f.feet.y,1,1);
                float left=-f.barWidth/2*Px;
                f.marker=Panel(f.readoutRoot,left-Px,Px,(f.barWidth+2)*Px,5*Px,BarBack);
                f.barLag=Panel(f.readoutRoot,left,2*Px,f.barWidth*Px,3*Px,PxBone);
                f.barFill=Panel(f.readoutRoot,left,2*Px,f.barWidth*Px,3*Px,f.unit.Side==0?PxMint:PxRed);
                f.tag=PxLabel(f.readoutRoot,0,7,1);
            }
            _effects=Rect(_stage,"Numbers above sprites",0,0,W,H);
            _pxLane[0]=PxLabel(_screen,StageX+6,TopBarH+4,0);
            _pxLane[1]=PxLabel(_screen,StageX+StageW-6,TopBarH+4,2);
            // The tutorial's dark band behind the banner while a step is up (CampaignArtCoach.cs). Plain, because
            // a kit plate's gold trims ran through both lines of text.
            _coachPlate=Raw(_screen,"Tutorial and order feedback",22*Px,(CleanCombatHud?378:StageY+StageH-29)*Px,676*Px,28*Px);
            _coachPlate.color=new Color(.07f,.06f,.08f,.88f);
            _coachPlate.enabled=false;
            _pxBanner=PxLabel(_screen,360,CleanCombatHud?382:StageY+StageH-24,1);
            _pxBanner2=PxLabel(_screen,360,CleanCombatHud?395:StageY+StageH-13,1);
            // Top bar, over the stage's top edge: the fight and where it sits on the run, both sides' nerve
            // and points, and the controls.
            PxImage(_screen,"Top bar",Plate(_kitButton,12,720,TopBarH),0,0);
            _pxTitle=PxLabel(_screen,14,8);
            _pxStats=PxLabel(_screen,14,21);
            if(_playback!=null){BuildPlaybackControls();return;}
            string heading=ViewKind==EncounterKind.Tutorial?"Tutorial: "+_fight.Encounter.Title:_fight.Encounter.Title+" / "+PathPlace(_campaign);
            SetPx(_pxTitle,ViewSim.MeterOn?Fit(heading,300):heading,PxBone);
            PxButton("pause","PAUSE",490,6,64,28,()=>_paused=!_paused);
            PxButton("speed",LearningSpeedLabel,556,6,78,28,CycleLearningSpeed);
            OnHover(_buttons["speed"].gameObject,()=>"Pace: 0.5x -> 0.25x -> 1x -> 2x -> 4x. Changes time, not combat rules.");
            PxButton("tactics","TACTICS",636,6,80,28,()=>
            {
                if(ViewSim.BeastOn){ShowBeastMoves();return;}
                _paused=true;Modal("TACTICS",Tracker(ViewSim,_fight.Encounter)+"\n\nTHEIR HABITS\n"+_theyKnow+DoctrineText(ViewSim)+"\n\nYOUR HABITS\n"+_campaign.CultMind.HabitsText()+"\n\n"+States(ViewSim)+"\n\nCombat remains paused when you close this panel. Press Resume or Space.");
            });
            // The order bar: five plates, each with its icon, cost, name and what it does, between the trims.
            for(int i=0;i<Orders.Length;i++)
            {
                int n=i;int x=16+i*138;
                var plate=PxButton("order-"+i,"",x,CleanCombatHud?408:418,134,CleanCombatHud?42:32,()=>Order(Orders[n]));
                var orderIcon=PxImage(plate.transform,"Icon",_kitIcons[OrderIcons[i]],2,CleanCombatHud?4:0);
                if(i==4)_chiefOrderIcon=orderIcon;
                _orderCost[i]=PxLabel(plate.transform,32,CleanCombatHud?30:20,2);
                _orderTitle[i]=PxLabel(plate.transform,38,CleanCombatHud?8:6,0);
                _orderEffect[i]=PxLabel(plate.transform,38,CleanCombatHud?22:15,0);
                Directive d=Orders[i];
                OnHover(plate.gameObject,()=>CombatOrderHint(d));
            }
            _resultButton=PxButton("see-result","SEE RESULT",260,StageY+StageH/2-14,200,28,ShowResult);
            _resultButton.gameObject.SetActive(false);
            CoachBegin();
            DrawBattle();RefreshFight();
        }
        void SelectFigure(BattleUnit u)
        {
            if(_playback!=null){_selected=u.Id;_notice=UnitName(u)+"   "+u.Hp+" / "+u.MaxHp+" HP";return;}
            if(!u.Alive||ViewOver)return;
            _selected=u.Id;
            if(_targeting&&u.Side==1)Focus(u);
            else _notice=UnitName(u)+"   "+u.Hp+" / "+u.MaxHp+" HP, "+(u.Row==Row.Front?"front":"back")+" row.";
        }
        void Anticipate()
        {
            foreach(var f in _figures)
            {
                if(f.attack==null||!f.unit.Alive)continue;
                if(ViewSim.RoadKitOn&&ViewSim.FallBackTicksLeft(f.unit.Side)>0){f.anticipated=false;continue;}
                if(ViewSim.HeavyTicks(f.unit.Side)>0&&ViewSim.HeavySwingerId(f.unit.Side)==f.unit.Id)continue;
                float lead=f.attackLength*.4f;
                if(f.unit.Cooldown>0&&f.unit.Cooldown<=lead*SimClock.TicksPerSecond&&!f.anticipated)
                {if(!f.majorFreeze)f.poseHold=0;f.attackAt=_visualTime-(lead-f.unit.Cooldown/(float)SimClock.TicksPerSecond);f.anticipated=true;}
            }
        }
        Figure FindFigure(string id){foreach(var f in _figures)if(f.unit.Id==id)return f;return null;}
        void AnimateEvent(BattleEvent e)
        {
            var actor=FindFigure(e.ActorId);var target=FindFigure(e.TargetId);
            if(e.Kind==BattleEventKind.Hit||e.Kind==BattleEventKind.Heal||e.Kind==BattleEventKind.TrapArmed)
            {
                if(actor!=null&&actor.attack!=null){if(!actor.majorFreeze)actor.poseHold=0;actor.attackAt=_visualTime-actor.attackLength*.4f;actor.anticipated=false;}
                if(target!=null&&e.Amount>0)
                {
                    bool heal=e.Kind==BattleEventKind.Heal;
                    if(!_seeking)FloatNumber(target,e.Amount,heal,e.Tick,e.ActorId);
                    target.flashAt=_visualTime;target.flash=heal?new Color(.72f,1f,.78f):new Color(1f,.58f,.52f);
                    if(e.Kind==BattleEventKind.Hit){target.hitAt=_visualTime;target.hitPoseAt=target.poseTime;target.lagUntil=_visualTime+.35f;}
                }
            }
            SoundEvent(e);
            Impact(e,actor,target);
            if(e.Kind==BattleEventKind.Kill&&target!=null&&!target.unit.Alive){target.poseHold=0;target.deathAt=target.poseTime;}
            MajorMoment(e,actor,target);
            TightParryEvent(e,actor,target);
            CashWordEvent(e,actor,target);
            // The move a fighter winds up, so its Release can say what it was (CampaignArtHunters.cs).
            if(e.Kind==BattleEventKind.Windup)_windupName[e.ActorId]=e.Detail;
            HeavyPresentationEvent(e,actor);
            ChiefSpellEvent(e);
            ClassSpellEvent(e);
            ChiefTacticsEvent(e);
            BeastEvent(e);
            SharkEvent(e);
            // A big word in the lane of the side an order or a cast lands on, as it happens.
            switch(e.Kind)
            {
                case BattleEventKind.Wall:
                    Callout(e.ActorSide,ChiefWallWord(e.Detail),
                        e.Detail==ChiefSpells.WallSmashed?PxOrange:PxBlue);
                    break;
                case BattleEventKind.Directive:
                    if(e.Detail==BattleSimulation.BlockedDetail)Callout(e.ActorSide,"SHIELDS HELD",PxBlue);
                    else if(Directives.TryParse(e.Detail,out Directive d)&&!DoctrineOrder(e))
                        Callout(e.ActorSide,(d==Directive.Eclipse?ChiefSpells.Name(ViewSim.Spell(e.ActorSide)):OrderName(d))+"!",
                            OrderColour(d==Directive.Eclipse?ViewSim.SpellAnswer(e.ActorSide):d));
                    break;
                case BattleEventKind.Release:
                    if(e.Detail==ChiefSpells.Cried){Callout(e.ActorSide,"WAR CRY",PxOrange);break;}
                    if(e.Detail==FightRules.Parried){Callout(1-e.ActorSide,"PARRIED",PxBlue);break;}
                    Callout(1-e.ActorSide,e.Detail==Moves.Dodged?"DODGED":e.Detail==Moves.Soaked?"HALVED":!string.IsNullOrEmpty(e.TargetId)?"HEAVY HITS":"CAST HITS",
                        e.Detail==Moves.Dodged?PxLilac:e.Detail==Moves.Soaked?PxBlue:PxYellow);
                    break;
                case BattleEventKind.Interrupt:
                    Callout(1-e.ActorSide,"INTERRUPTED",PxBone);
                    break;
                case BattleEventKind.Break:
                    Callout(e.ActorSide,"NERVE BROKEN",PxRed);
                    break;
            }
            // The 13 Oct 2026 fight rules: misses, parries, earned points, snaps, combos and doctrines.
            FightRulesEvent(e,actor,target);
            // Why an order did not land as hoped (CampaignArtWhy.cs), and the tutorial's next step.
            WhyEvent(e);if(_playback==null)CoachHeard(e);
        }
        static Color32 OrderColour(Directive d)
        {
            switch(d)
            {
                case Directive.Hold: return PxBlue;
                case Directive.FallBack: return PxLilac;
                case Directive.FocusFire: return PxRed;
                case Directive.Unleash: return PxOrange;
                default: return PxYellow;
            }
        }
        /// What an order does, from the sim's own numbers; shown when you point at its plate.
        static string Effect(Directive d)
        {
            switch(d)
            {
                case Directive.Hold: return "Front takes "+(100-BattleSimulation.HoldDamagePercent)+"% less for "+Seconds(BattleSimulation.HoldTicks)+"; drains your nerve";
                case Directive.FallBack: return "No melee lands, dodges casts, "+Seconds(BattleSimulation.FallBackTicks);
                case Directive.FocusFire: return "Your fighters hit one target, "+Seconds(BattleSimulation.FocusTicks);
                case Directive.Unleash: return "+"+(BattleSimulation.UnleashDamagePercent-100)+"% damage, "+Seconds(BattleSimulation.UnleashTicks)+"; drains your nerve at half Hold's rate";
                default: return "Winds up, then hits their front line";
            }
        }
        /// The same, short enough for the plate.
        static string ShortEffect(Directive d)
        {
            switch(d)
            {
                case Directive.Hold: return "-"+(100-BattleSimulation.HoldDamagePercent)+"% hit / -nerve";
                case Directive.FallBack: return "No melee, "+Seconds(BattleSimulation.FallBackTicks);
                case Directive.FocusFire: return "One target, "+Seconds(BattleSimulation.FocusTicks);
                case Directive.Unleash: return "+"+(BattleSimulation.UnleashDamagePercent-100)+"% dmg / -nerve";
                default: return "Big hit, front";
            }
        }
        /// A side's lane shows the word that just landed for a moment, then the orders running on that side.
        void DrawLane(BattleSimulation sim,int side)
        {
            if(CleanCombatHud&&!_paused&&!ViewOver){SetPx(_pxLane[side],"",PxMuted);return;}
            float age=_visualTime-_flashAt[side];
            if(age>=0&&age<.9f){SetPx(_pxLane[side],_flashText[side],_flashColour[side]);return;}
            var texts=new List<string>();var colours=new List<Color32>();
            if(!ViewOver)
            {
                if(sim.HoldTicksLeft(side)>0){texts.Add("HOLD "+Seconds(sim.HoldTicksLeft(side)));colours.Add(PxBlue);}
                if(sim.UnleashTicksLeft(side)>0){texts.Add("UNLEASH "+Seconds(sim.UnleashTicksLeft(side)));colours.Add(PxOrange);}
                if(sim.FallBackTicksLeft(side)>0){texts.Add("FALL BACK "+Seconds(sim.FallBackTicksLeft(side)));colours.Add(PxLilac);}
                if(sim.FocusTicksLeft(side)>0){texts.Add("FOCUS FIRE "+Seconds(sim.FocusTicksLeft(side)));colours.Add(PxRed);}
                if(sim.WindupTicks(side)>0){texts.Add("CASTING "+Seconds(sim.WindupTicks(side)));colours.Add(PxYellow);}
                if(sim.WallTicks(side)>0){texts.Add("WALL "+Seconds(sim.WallTicks(side)));colours.Add(PxBlue);}
            }
            if(texts.Count>0)
            {
                if(side==0){texts.Insert(0,"YOU");colours.Insert(0,PxBone);}
                else{texts.Add("THEM");colours.Add(PxBone);}
            }
            SetPx(_pxLane[side],texts.ToArray(),colours.ToArray());
        }
        void DrawBar(Figure f,BattleSimulation sim)
        {
            var u=f.unit;bool alive=u.Alive;
            f.marker.enabled=f.barLag.enabled=f.barFill.enabled=alive;
            // The chip drains on the fight's own clock (Codex re-audit, 10 Oct 2026): it holds while paused
            // and does not move when a frame is drawn again or captured.
            float dt=Mathf.Max(0f,_visualTime-f.chipAt);f.chipAt=_visualTime;
            if(!alive){DrawTags(f,null,null);return;}
            if(f.shownHp<u.Hp)f.shownHp=u.Hp;
            else if(_visualTime>=f.lagUntil)f.shownHp=Mathf.MoveTowards(f.shownHp,u.Hp,u.MaxHp*1.2f*dt);
            f.barFill.rectTransform.sizeDelta=new Vector2(Mathf.Round(f.barWidth*u.Hp/u.MaxHp)*Px,3*Px);
            f.barLag.rectTransform.sizeDelta=new Vector2(Mathf.Round(f.barWidth*Mathf.Clamp01(f.shownHp/u.MaxHp))*Px,3*Px);
            // The frame takes the colour of the order running on this fighter's side, while the fight runs.
            int s=u.Side;bool live=!ViewOver;
            f.marker.color=_selected==u.Id?(Color)PxBone:!live?BarBack:sim.HoldTicksLeft(s)>0?(Color)PxBlue:sim.UnleashTicksLeft(s)>0?(Color)PxOrange:sim.FallBackTicksLeft(s)>0?(Color)PxLilac:BarBack;
            var texts=new List<string>();var colours=new List<Color32>();
            if(live&&sim.FocusedId(1-s)==u.Id){texts.Add((s==1?"YOUR FOCUS ":"THEIR FOCUS ")+Seconds(sim.FocusTicksLeft(1-s)));colours.Add(PxRed);}
            if(live&&sim.WindupTicks(s)>0&&sim.WindupCasterId(s)==u.Id){texts.Add("CASTING "+Seconds(sim.WindupTicks(s)));colours.Add(PxYellow);}
            if(live){if(_selected==u.Id||_targeting)WhyTags(f,sim,texts,colours);FightRulesTags(f,sim,texts,colours);BandTags(f,sim,texts,colours);}
            if(_playback==null&&u.Side==0&&u.Id==_campaign.TabardWearer){texts.Add("LAVA SUN TABARD");colours.Add(PxGold);}
            // Two lines at most, so the tags of fighters side by side never run into each other (CampaignArtReadouts.cs).
            DrawTags(f,texts,colours);
        }
        void DrawBattle()
        {
            var sim=ViewSim;
            // New fight/replay screens must replace readout references before chief overlays inspect them.
            EnsureReadouts(sim);
            if(RoadSystemsHud)ReadRoadEvents(sim);
            DrawChiefSpells(sim);
            DrawTribeMechanics(sim);
            DrawClassSpells(sim);
            DrawChiefTactics(sim);
            DrawBeasts(sim);
            DrawClarity(sim);
            DrawFocusTarget(sim);
            if(CleanCombatHud)CleanThreatMarkers(sim);
            DrawCombatShake();
            foreach(var f in _figures)
            {
                var u=f.unit;
                bool visible=!u.Benched&&!(RoadHud&&u.Fled);
                f.root.gameObject.SetActive(visible);
                f.readoutRoot.gameObject.SetActive(visible);
                if(_bloodridgeContacts.TryGetValue(f,out RawImage reserveContact))reserveContact.enabled=visible;
                if(!visible)continue;
                f.target.interactable=u.Alive;
                DrawBar(f,sim);
                DrawRoadUnit(f);
                DrawCombatFeedback(f);
                DrawBloodridgeContact(f);
                if(f.body==null)continue;
                Color tint=!_paused&&u.Alive&&_visualTime-f.flashAt<.1f?f.flash:Color.white;
                tint=MomentTint(f,tint);
                if(RoadHud&&u.Alive&&u.Frenzied)tint=Color.Lerp(tint,new Color(1f,.30f,.20f),.55f);
                if(f.staticFront)
                {
                    // Front-only asset pass: show the actual sprite; do not fake unmade animation clips.
                    f.body.color=u.Alive?tint:new Color(.55f,.55f,.55f,.4f);
                    continue;
                }
                f.body.color=tint;
                // The guard goes up when Hold starts, stays up while it runs, and comes down after.
                bool holding=f.block!=null&&sim.HoldTicksLeft(u.Side)>0;
                if(holding&&!f.blocking){f.blocking=true;f.blockAt=f.poseTime;}
                else if(!holding&&f.blocking){f.blocking=false;f.blockEndAt=f.poseTime;}
                Clip c=f.idle;float time=f.poseTime+u.Index*.41f;bool loop=true;
                if(!u.Alive)
                {if(f.deathAt<0)f.deathAt=f.poseTime;c=f.death;time=f.poseTime-f.deathAt;loop=false;}
                // Until a surrender clip is handed over, the yielded chief rests in his idle.
                // Do not keep showing the interrupted attack or cast as if it were still active.
                else if(u.Yielded){c=f.idle;}
                else if(HeavyAttackPose(f,sim,out float heavyTime)){c=f.attack;time=heavyTime;loop=false;}
                // Hit-reaction poses may hold locally, but their selection window and every attack
                // use the unheld action clock. A hit pause can never cover extra attack frames.
                else if(_visualTime-f.hitAt<f.hitLength&&!(_combatFeedback&&f.poseTime-f.strikeAt>=0&&f.poseTime-f.strikeAt<StrikeHoldSeconds))
                {c=f.hit;time=(f.poseTime-f.hitPoseAt)/f.hitLength*c.seconds;loop=false;}
                else if(_visualTime-f.attackAt<f.attackLength){c=f.attack;time=(_visualTime-f.attackAt)/f.attackLength*c.seconds;loop=false;}
                else if(f.victory!=null&&ViewOver&&sim.Winner==u.Side){c=f.victory;time=_endAge;loop=false;}
                else if(f.blocking){c=f.block;time=Mathf.Min(f.poseTime-f.blockAt,c.seconds/2);loop=false;}
                else if(f.block!=null&&f.poseTime-f.blockEndAt<f.block.seconds/2){c=f.block;time=f.block.seconds/2+f.poseTime-f.blockEndAt;loop=false;}
                if(f.majorFreeze&&f.poseHold>0&&f.drawnClip!=null&&!u.Yielded&&!(sim.RoadKitOn&&sim.FallBackTicksLeft(u.Side)>0))c=f.drawnClip;
                else {f.drawnClip=c;f.drawnFrame=c.FrameAt(time,loop);}
                FrameRect frame=c.sheet.frames[f.drawnFrame].frame;
                f.body.texture=c.texture;
                f.body.uvRect=new UnityEngine.Rect(frame.x/(float)c.texture.width,1-(frame.y+frame.h)/(float)c.texture.height,frame.w/(float)c.texture.width,frame.h/(float)c.texture.height);
                f.body.rectTransform.anchoredPosition=new Vector2(-c.sheet.anchorX*ArtScale,c.sheet.anchorY*ArtScale);
                f.body.rectTransform.sizeDelta=new Vector2(frame.w*ArtScale,frame.h*ArtScale);
                SetFlashMask(f,c,frame);
                DrawTightBody(sim,f);
            }
            DrawTightParry(sim);
            // Confirmed effects share the existing feedback area; compact state marks sit by HP.
            DrawButcherEffects(sim);
            DrawPalFloor(sim);
            DrawShark(sim);
            DrawCashWords();
            DrawLane(sim,0);DrawLane(sim,1);
            DrawBloodridgeWind();
            DrawRegionalWind();
            for(int i=_numbers.Count-1;i>=0;i--)
            {
                var n=_numbers[i];float age=_visualTime-n.at;
                if(age>n.life)
                {
                    if(n.image.texture!=null){_screenTextures.Remove((Texture2D)n.image.texture);Destroy(n.image.texture);}
                    Destroy(n.image.gameObject);_numbers.RemoveAt(i);continue;
                }
                var size=n.image.rectTransform.sizeDelta;
                float rise=Mathf.Floor(age/n.life*n.rise)*ArtScale;
                n.image.rectTransform.anchoredPosition=new Vector2(Mathf.Round(n.origin.x-size.x/2),-(Mathf.Round(n.origin.y-size.y/2)-rise));
                n.image.color=new Color(1,1,1,Mathf.Clamp01((n.life-age)*3));
            }
        }
        /// One number per fighter at a time: blows that land within 0.6 s add up, so they never stack into
        /// one unreadable number. It starts at chest height and stays inside the stage. Wounds on your
        /// fighters are red, healing green, wounds on theirs bone. Drawn in the game's display font at the
        /// world's 1x, so a number never dwarfs the fighter it is on.
        void FloatNumber(Figure f,int amount,bool heal,long eventTick=-1,string actorId=null)
        {
            bool payoff=!heal&&eventTick>=0&&CashHitAt(f,eventTick,actorId);
            if(!payoff&&!heal&&_selected!=f.unit.Id&&!f.unit.IsLeader&&amount<Mathf.Max(1,f.unit.MaxHp/10))return;
            Number last=heal?f.lastHeal:f.lastHit;
            if(!payoff&&last!=null&&!last.payoff&&_numbers.Contains(last)&&_visualTime-last.at<.6f){last.sum+=amount;DrawNumber(last,heal,f);return;}
            float offset=(f.lane++%3-1)*22*ArtScale;
            var n=new Number{sum=amount,payoff=payoff,at=_visualTime,origin=new Vector2(f.feet.x+offset,Mathf.Max(payoff?380:(StageY+40)*Px,f.feet.y-f.head*.45f))};
            n.image=Raw(_effects,"Number",0,0,1,1);
            DrawNumber(n,heal,f);
            _numbers.Add(n);
            if(heal)f.lastHeal=n;else f.lastHit=n;
        }
        void DrawNumber(Number n,bool heal,Figure f)
        {
            if(n.image.texture!=null){_screenTextures.Remove((Texture2D)n.image.texture);Destroy(n.image.texture);}
            Color32 fill=heal?PxMint:f.unit.Side==0?PxRed:PxBone;
            Color32 lower=heal?new Color32(104,150,128,255):f.unit.Side==0?new Color32(160,64,48,255):PxMuted;
            var t=_logoFont.Render((heal?"+":"")+n.sum,fill,lower,1,true);_screenTextures.Add(t);
            float scale=ArtScale*(n.payoff?1.5f:1);
            n.image.texture=t;n.image.rectTransform.sizeDelta=new Vector2(t.width*scale,t.height*scale);
        }
        void Callout(int side,string value,Color32 colour)
        {
            _flashText[side]=value;_flashColour[side]=colour;_flashAt[side]=_visualTime;
        }
        void RefreshFight()
        {
            var sim=ViewSim;
            bool verdict=sim.AwaitingVerdict;
            if(verdict){_targeting=false;_pilotOn=false;}
            Coach();
            if(!CleanCombatHud)SetPx(_pxStats,new[]{"POINTS "+sim.DirectivePoints(0),EarnText(0),"   "+Seconds(sim.CurrentTick)+" / "+Seconds(_fight.Setup.TickLimit)+"   ",
                "THEIR ORDERS "+sim.DirectivePoints(1),EarnText(1)},new[]{PxMint,PxGold,PxMuted,PxRed,PxOrange});
            if(sim.BeastOn)SetPx(_pxStats,"POINTS "+sim.DirectivePoints(0)+" / "+Seconds(sim.CurrentTick)+" / "+Seconds(_fight.Setup.TickLimit)+" / BEAST HAS NO NERVE",PxGold);
            if(verdict)SetPx(_pxStats,"CHIEF KNEELS  /  CHOOSE WITHOUT A TIME LIMIT",PxGold);
            RefreshGoal(sim);
            if(CleanCombatHud)
            {
                SetPx(_pxTitle,Fit(_fight.Encounter.Title+" / "+PathPlace(_campaign),sim.MeterOn?300:465),PxBone);
                SetPx(_pxStats,verdict?"CHIEF KNEELS / CHOOSE SPARE OR EXECUTE":"ORDERS: "+sim.DirectivePoints(0)+" PTS / "+Seconds(sim.CurrentTick)+" OF "+Seconds(_fight.Setup.TickLimit)+
                    (sim.GloryOn(0)?" / GLORY "+ChiefKits.Letter(sim.GloryRankOf(0)):""),PxBone);
                // Bonus objectives remain in the preparation/results screens and Details.
                if(_pxGoal!=null)_pxGoal.root.gameObject.SetActive(false);
            }
            // The line at the foot of the stage: a problem first, then the tutorial's step, then what you point
            // at, then the fight's state, then the latest thing that happened. Their cast says when a Hold parries
            // it, and nothing says to focus a caster their Warden covers ("fix all 9", 13 Oct 2026).
            string first,second="";Color32 colour=PxBone;
            if(verdict)
            {
                first="1 SPARE: the chief, tribe and chief spell join you.";
                second="3 EXECUTE: take the chief's piece. Both choices are free.";colour=PxGold;
                if(_notice.Length>0){first=_notice;colour=PxRed;}
            }
            else if(_notice.Length>0){first=_notice;colour=PxRed;}
            else if(CoachText(out first,out second))colour=PxGold;
            else if(ViewOver)first="Fight over: "+(FightWon()?"you won.":"you lost.");
            else if(HeavyBanner(sim,out first,out second,out colour)){}
            else if(sim.WindupTicks(1)>0)
            {
                var caster=sim.FindById(sim.WindupCasterId(1));bool covered=caster!=null&&sim.CoveredBy(caster)!=null;
                // A spell other than the Eclipse says what it is and the one button that answers it (CampaignArtHunters.cs).
                if(sim.Spell(1)!=ChiefSpell.Eclipse)
                {
                    first=ChiefSpells.Name(sim.Spell(1))+" in "+Seconds(sim.WindupTicks(1))+".";
                    second=SpellCounter(sim.Spell(1),sim.ParryCenterOn);colour=OrderColour(sim.SpellAnswer(1));
                }
                else if(_paused&&sim.HeavyOn)
                {
                    first="PAUSED: their cast has "+Seconds(sim.WindupTicks(1))+" left.";
                    second="Resume before timing Hold. A paused Hold cannot parry.";
                }
                else if(sim.WaitsOutParry(1)&&!sim.HeavyOn)
                {
                    first="They wait out your Hold: it halves this cast, but cannot parry it.";
                    second=(covered?"Their Warden covers the caster. ":"Focus the caster to try to interrupt. ")+(_fight.Encounter.NoFallBack?"Fall Back is unavailable here.":"Fall Back can dodge the cast.");
                    colour=PxYellow;
                }
                else if(sim.WindupTicks(1)<=FightRules.PerfectHoldTicks)
                {
                    first=sim.CanUse(0,Directive.Hold,out _)?"NOW: Hold to parry it!":sim.HoldTicksLeft(0)>0?"Your Hold is up: it halves their cast.":"No point left to Hold: their cast lands.";
                    colour=PxGold;
                    if(sim.HeavyOn&&sim.WaitsOutParry(1))second=sim.WindupWaited(1)?"Delayed cast: time a fresh Hold to this release.":"They may delay once. Watch the timer and Hold again.";
                }
                else
                {
                    first=NameOf(sim.WindupCasterId(1))+" is casting: "+Seconds(sim.WindupTicks(1))+(covered?". Their Warden covers it.":".");
                    second=(covered?"":"Focus the caster. ")+"Hold in its last half second to parry, earlier to halve"+(_fight.Encounter.NoFallBack?".":", or Fall Back to dodge.");
                    colour=PxYellow;
                }
            }
            else if(RecentWallSmash(1))
            {
                first="WALL SMASHED: their Steel Storm is gone.";
                second=sim.HoldTicksLeft(1)>0?"Their Hold remains, but the wall's nerve loss still landed.":"Their fighters are exposed.";
                colour=PxOrange;
            }
            else if(sim.WallTicks(1)>0||sim.InLockdown(1))
            {
                first=sim.InLockdown(1)?"Lockdown: your hits land at "+sim.WallPercentNow(1)+"%.":"Their Steel Storm wall: "+Seconds(sim.WallTicks(1))+" left.";
                second=!sim.WallSmashable(1)?(sim.WallRebuildTicks(1)>0?"Wall rebuilds in "+Seconds(sim.WallRebuildTicks(1))+". Nothing to smash yet.":"No wall standing."):
                    sim.CanUse(0,Directive.Unleash,out string wallWhy)?"Unleash now to smash it open.":"Unleash unavailable: "+wallWhy;
                colour=PxOrange;
            }
            else if(sim.WornOutTicksLeft(1)>0)
            {
                var worn=sim.Leader(1);
                first=worn!=null&&sim.CoveredBy(worn)!=null?"Their caster is worn out, but their Warden covers it: your hits on it go to the Warden.":
                    "Their caster is worn out for "+Seconds(sim.WornOutTicksLeft(1))+": focus them for double damage.";
            }
            else if(_targeting){first=TargetPrompt();colour=PxRed;}
            else if(ReadoutTeach(ref first,ref second,ref colour)){}
            else if(_hover.Length>0)first=_hover;
            else if(_paused)first="Paused: Space or Resume to carry on.";
            else if(sim.WindupTicks(0)>0)first="Your chief order is winding up: "+Seconds(sim.WindupTicks(0))+".";
            else if(Time.unscaledTime<_keyboardHintUntil&&_keyboardHint.Length>0)first=_keyboardHint;
            else first=_feed.Count>0?_feed[_feed.Count-1]:"Fighters act on their own. Your orders change the fight.";
            if (!verdict && !ViewOver && _notice.Length == 0 && _coachStep < 0 &&
                (sim.ParryCenterOn || sim.HeavyTicks(1) == 0))
            {
                first = _targeting ? TargetPrompt() : _hover.Length > 0 ? _hover :
                    _paused ? "PAUSED: inspect DETAILS. Resume before timing a fresh Hold." : "Read ANSWER FIRST above. Keys 1-4 command; Space pauses.";
                second = sim.TightParryOn?"Watch the attacker flash gold. A mistimed Hold leaves your squad vulnerable.":sim.SquadFirstOn ? "Hold guards the front row. Focus and Unleash help your squad cash marks." : sim.ParryCenterOn ? "Perfect Hold: last half second, live. Early Hold soaks at a nerve cost. Paused Hold never parries." : "Historical fight: original counters and timing apply.";
                colour = PxBone;
            }
            if(!verdict&&!ViewOver&&_coachStep<0&&sim.ParryCenterOn)
            {
                if(!_targeting&&_notice.Length==0)HoldFeedback(sim,out first,out second,out colour);
                _coachPlate.enabled=true;
            }
            if(CleanCombatHud&&!verdict&&!ViewOver&&_coachStep<0)
                CleanCombatFeedback(sim,out first,out second,out colour);
            SetPx(_pxBanner,sim.ParryCenterOn?Fit(first,660):first,colour);
            SetPx(_pxBanner2,sim.ParryCenterOn?Fit(second,660):second,colour);
            // The plate of the order that finishes a combo you started lights up (CampaignArtFightRules.cs).
            var combo=ViewOver?ComboHint.None:sim.NextCombo(0);
            for(int i=0;i<Orders.Length;i++)
            {
                var d=Orders[i];var b=_buttons["order-"+i];
                int pointCost=DisplayOrderCost(sim,d);
                string title=(i+1)+" "+OrderName(d),effect=sim.RoadKitOn&&d==Directive.FallBack?"Dodge; no attacks":ShortEffect(d),cost=pointCost==0?"FREE":pointCost.ToString();
                bool allowed=sim.CanUse(0,d,out string unavailable);
                bool ok=!ViewOver&&allowed&&!_pilotOn;
                if(d==Directive.FallBack&&_fight.Encounter.NoFallBack){effect="Not here";ok=false;}
                if(d==Directive.Eclipse)
                {
                    if(_fight.Assist==null){title="5 Chief order";effect="None yet";cost="";}
                    else
                    {
                        title="5 "+_fight.Assist.Name;effect=ChiefEffect(sim.Spell(0));
                        if(sim.WindupTicks(0)>0)effect="Casting "+Seconds(sim.WindupTicks(0));
                        else if(sim.WallTicks(0)>0)effect="Wall "+Seconds(sim.WallTicks(0));
                        else if(_fight.AssistUsed){effect="Used";cost="";}
                        else if(sim.GloryOn(0)&&sim.GloryRankOf(0)==GloryRank.S)effect="Glory: empowered";
                    }
                    if(_chiefOrderIcon!=null)_chiefOrderIcon.texture=_kitIcons[SpellIcon(sim.Spell(0))];
                    ok&=_fight.CanCallAssist;
                }
                bool lit=ok&&ComboPlate(combo,i,d,ref title,ref effect);
                // A plain Unleash their shields will stop says so (CampaignArtReadouts.cs).
                string blocked=!lit&&d==Directive.Unleash&&!sim.WallSmashable(1)?UnleashBlockedText(sim):"";
                if(blocked.Length>0)effect=blocked;
                if(d==Directive.Unleash&&sim.WallSmashable(1)){effect="Smash their wall";lit=ok;}
                if(d==Directive.Hold&&_paused&&(sim.ParryCenterOn||sim.HeavyOn||sim.Beast(1)==BeastKit.SandShark))effect="Paused: no parry";
                if(d==Directive.Hold && sim.ParryCenterOn && !_paused && !verdict)
                {
                    ReadClarity(sim);
                    if(LiveParryCue(sim,CurrentThreat))
                    { effect="Parry available"; lit=ok; }
                }
                if(d==Directive.Hold&&!verdict&&!ViewOver&&!allowed)effect="Can't Hold: click";
                if(CleanCombatHud&&!verdict)
                {
                    if(d==Directive.FocusFire&&_targeting){effect="Pick target / Esc";lit=ok;}
                    if(d==Directive.Unleash&&!lit&&!sim.WallSmashable(1))effect="+"+(BattleSimulation.UnleashDamagePercent-100)+"% DMG";
                    if(d==Directive.Hold&&allowed&&!_paused)
                    {effect=sim.SquadFirstOn?"Guard front row":"Timed defence";lit=false;}
                    if(d==Directive.Hold&&sim.SquadScreenOn&&allowed&&!_paused&&!sim.TightParryOn)
                    {
                        bool window=false, incoming=false;
                        foreach(var threat in _threatReads)
                        { incoming|=!threat.CastBar&&threat.BeatTicks>=0&&(threat.Answers&ThreatAnswer.Parry)!=0;
                          window|=LiveParryCue(sim,threat); }
                        int recovery=sim.HoldRefreshTicksLeft(0);
                        effect=recovery>0?"Wait "+HoldRecoveryTime(recovery):window?"PARRY NOW":incoming?"Wait for gold":"Guard / no beat";
                        lit=window&&ok;
                    }
                    if(d==Directive.Hold&&sim.TightParryOn&&!verdict)
                    {effect=sim.WhiffTicksLeft(0)>0?"WHIFF / +50%":_paused?"Paused: no parry":"Watch attacker";lit=false;}
                    if(!allowed)effect=sim.DirectivePoints(0)<pointCost?"Need "+pointCost+" pts":unavailable;
                    if(d==Directive.FallBack&&_fight.Encounter.NoFallBack)effect="Not in this fight";
                    if(d==Directive.Eclipse)
                    {
                        if(_fight.Assist==null)effect="No chief yet";
                        else if(_fight.AssistUsed)effect="Already used";
                        else if(allowed&&!_fight.CanCallAssist)effect="Not ready";
                    }
                    if(d==Directive.Hold&&sim.HoldRefreshTicksLeft(0)>0&&!_paused&&allowed&&(!sim.TightParryOn||sim.WhiffTicksLeft(0)==0))
                    {effect="Wait "+HoldRecoveryTime(sim.HoldRefreshTicksLeft(0));lit=false;}
                    title=Fit(title,90);
                    effect=blocked.Length>0?"+"+(BattleSimulation.UnleashDamagePercent-100)+"% DMG":Fit(effect,90);
                }
                if(d==Directive.Eclipse)MeterSpellPlate(sim,ok,ref effect,ref cost,ref lit);
                if(verdict)
                {
                    bool spare=d==Directive.Hold,execute=d==Directive.FocusFire;
                    ok=(spare||execute)&&sim.CanUse(0,d,out _);lit=ok;blocked="";
                    title=spare?"1 SPARE":execute?"3 EXECUTE":(i+1)+" "+OrderName(d);
                    effect=spare?"Recruit tribe":execute?"Chief's piece":"Unavailable";
                    cost=ok?"FREE":"";
                }
                SetPx(_orderTitle[i],title,lit?PxGold:ok?PxBone:PxMuted);
                SetPx(_orderEffect[i],effect,d==Directive.Hold&&sim.TightParryOn&&sim.WhiffTicksLeft(0)>0?PxRed:lit?PxGold:blocked.Length>0?PxRed:PxMuted);
                SetPx(_orderCost[i],cost,PxGold);
                // A disabled Hold used to swallow clicks without explaining the cost/cooldown.
                // Let the command API reject it and show that reason, without changing the sim.
                b.interactable=ok||(d==Directive.Hold&&!verdict&&!ViewOver&&!_pilotOn);
            }
            SetPx(_pxButtonLabels["pause"],_paused?"RESUME":"PAUSE",PxBone);
            SetPx(_pxButtonLabels["speed"],LearningSpeedLabel,_speed<1?PxMint:PxBone);
            bool resultReady=ViewOver&&_endAge>2.3f;
            bool newlyReady=resultReady&&!_resultButton.gameObject.activeSelf;
            _resultButton.gameObject.SetActive(resultReady);
            if(newlyReady&&_modal==null)SelectControl("see-result");
            DrawWhy();
            RefreshReadouts(sim);
            if(sim.MeterOn)SetPx(_pxStats,verdict?"CHIEF KNEELS":Fit("ORDERS "+sim.DirectivePoints(0)+" PTS / "+Seconds(sim.CurrentTick)+" / "+Seconds(_fight.Setup.TickLimit)+
                (sim.GloryOn(0)?" / GLORY "+ChiefKits.Letter(sim.GloryRankOf(0)):""),300),PxBone);
            DrawParryMeter(sim);
        }
    }
}
