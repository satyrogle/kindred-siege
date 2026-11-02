using System;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        bool RaidHud => (_playback != null || _fight != null) && ViewSim.RaidChiefOn && ViewSim.Kit(1) == ChiefKit.Bloodridge;
        RectTransform _raidScreen;
        RawImage _raidBeatRing;
        Image _raidFlash;
        string _raidCastKey = "";
        long _raidDrawTick = -1;
        int _raidCastTotal;
        bool _raidWindowSeen;
        float _raidFlashUntil = -1;

        // Cost presentation only. Eligibility and spending remain authoritative in CanUse/TryOrder.
        static int DisplayOrderCost(BattleSimulation sim, Directive order) =>
            sim.OrderCost(0, order);

        void EnsureRaidHud()
        {
            if (_raidScreen == _screen) return;
            _raidScreen = _screen; _raidCastKey = ""; _raidCastTotal = 0;
            _raidDrawTick = -1; _raidWindowSeen = false; _raidFlashUntil = -1;
            var tex = new Texture2D(48,48,TextureFormat.RGBA32,false) { filterMode=FilterMode.Point };
            var pixels = new Color32[48*48];
            for(int y=0;y<48;y++) for(int x=0;x<48;x++)
            {
                float d=(x-23.5f)*(x-23.5f)+(y-23.5f)*(y-23.5f);
                pixels[y*48+x]=d>=19*19&&d<=23*23?new Color32(255,255,255,255):new Color32(0,0,0,0);
            }
            tex.SetPixels32(pixels);tex.Apply();_screenTextures.Add(tex);
            _raidBeatRing=PxImage(_readoutLayer,"Raid parry beat ring",tex,0,0);
            _raidBeatRing.raycastTarget=false;
            _raidFlash=Panel(_readoutLayer,0,0,W,H,new Color(1,.83f,.45f,0));
            _raidFlash.raycastTarget=false;
        }

        void DrawRaidThreat(BattleSimulation sim)
        {
            EnsureRaidHud();
            var t=CurrentThreat;
            bool live=!ViewOver&&!sim.AwaitingVerdict;
            var casting=Threat.None;
            foreach(var threat in _threatReads)if(threat.CastBar){casting=threat;break;}
            bool cast=live&&!casting.IsNone;
            bool primaryCast=!t.IsNone&&t.CastBar;
            bool beat=live&&!t.IsNone&&!t.CastBar&&t.BeatTicks>=0;
            bool parryNow=LiveParryCue(sim,t);
            string key=casting.IsNone?t.ActorId+"/"+t.Name:casting.ActorId+"/"+casting.Name;
            if(key!=_raidCastKey||sim.CurrentTick<_raidDrawTick)
            { _raidCastKey=key;_raidCastTotal=0;_raidWindowSeen=false;_raidFlashUntil=-1; }
            _raidDrawTick=sim.CurrentTick;
            if(cast) _raidCastTotal=Math.Max(_raidCastTotal,casting.TicksLeft);
            _beatTrack.enabled=cast;_beatWindow.enabled=cast;_beatCursor.enabled=false;
            if(cast)
            {
                Place(_beatWindow,30,116,Mathf.RoundToInt(566f*casting.TicksLeft/Math.Max(1,_raidCastTotal)),7);
                _beatWindow.color=PxBlue;
            }
            string heading=t.IsNone?"RAID CHIEF / WATCH HIS MOVES":
                (primaryCast?"INTERRUPT: ":beat?"PARRY MOVE: ":"ACTIVE: ")+t.Name;
            SetPx(_threatHeading,Fit(heading,566),primaryCast?PxBlue:PxBone);
            SetPx(_threatTarget,Fit(cast&&!primaryCast?"ALSO CASTING: "+casting.Name+" / 3 FOCUS TO INTERRUPT":t.IsNone?"Hurt him to slow his posture recovery.":
                FighterLabel(t.ActorId)+" TO "+FighterLabel(t.TargetId),566),PxBone);
            string cue=primaryCast?"3 FOCUS THE CHIEF TO INTERRUPT":BeatCue(t);
            if(t.IsNone)cue="1 HOLD IS FREE / 3 FOCUS / 4 UNLEASH";
            if(!t.IsNone&&t.BeatTicks<0)cue=ThreatAnswers(sim,t);
            if(beat&&sim.HoldRefreshTicksLeft(0)>0)cue=HoldRecoveryCue(sim);
            if(_playback!=null)cue=cast?"REPLAY / FOCUS INTERRUPTS THE CAST":"REPLAY / RECORDED COMMANDS";
            if(_paused&&_playback==null)cue="PAUSED / SPACE TO RESUME";
            SetPx(_threatAnswers,Fit(cue,566),_paused?PxMuted:cast?PxBlue:parryNow?PxGold:PxBone);
            // A readable ring replaces the timing bar on parry moves. It follows the actual phase API.
            var actor=beat?FindFigure(t.ActorId):null;
            _raidBeatRing.enabled=actor!=null&&!ChiefCutInVisible;
            if(_raidBeatRing.enabled)
            {
                int size=parryNow?36:48;
                Place(_raidBeatRing,Mathf.RoundToInt(actor.feet.x/Px)-size/2,
                    Mathf.Max(145,Mathf.RoundToInt((actor.feet.y-actor.head)/Px)-size),size,size);
                _raidBeatRing.color=t.FakeFirst||t.WaitsOutHold||sim.HoldRefreshTicksLeft(0)>0?PxOrange:parryNow?PxGold:PxBone;
            }
            bool window=beat&&t.Tier==ThreatTier.Spell&&parryNow;
            if(window&&!_raidWindowSeen&&!_paused&&!_seeking&&_playback==null)
                _raidFlashUntil=Time.unscaledTime+.12f;
            _raidWindowSeen=window&&!_paused;
            _raidFlash.enabled=live&&!_paused&&Time.unscaledTime<_raidFlashUntil;
            _raidFlash.color=new Color(1,.83f,.45f,.16f);
            _raidFlash.transform.SetAsLastSibling();
        }

        string RaidGuide => "SOLO BLOODRIDGE CHIEF\n\n"+
            "HEALTH: your squad's hits wear him down and slow posture recovery.\n"+
            "POSTURE: answer his moves to empty it. At zero he kneels: Spare recruits his tribe; Execute takes his piece.\n\n"+
            "BLUE CAST BAR: Focus Fire on him interrupts Burning Ring or Sun Mantle.\n"+
            "BEAT RING: time a live Hold to parry. Eclipse cannot be interrupted; parry or Fall Back.\n"+
            "Hold costs no points. Repeated presses within one second do not refresh its parry timing. Paused Hold cannot parry.\n\n"+
            "Below 30% health his Eclipse comes more often. Your squad keeps fighting while you answer him.";
    }
}
