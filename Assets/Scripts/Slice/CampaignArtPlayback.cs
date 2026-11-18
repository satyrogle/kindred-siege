using System;
using System.Collections.Generic;
using UnityEngine.UI;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // Only the current layered replay owns these nodes. On a seek they keep their
        // already-decoded textures. All simulation-bound figures and HUDs get rebuilt.
        sealed class ReplayScenery
        {
            public Transform[] rear,front;
            public RectTransform parking;
            public readonly Dictionary<string,RawImage> contacts=new Dictionary<string,RawImage>();
        }
        ReplayScenery _replayScenery;

        Transform[] StageChildrenFrom(int first)
        {
            var result=new Transform[_stage.childCount-first];
            for(int i=0;i<result.Length;i++)result[i]=_stage.GetChild(first+i);
            return result;
        }
        void ParkReplayScenery()
        {
            var scenery=_replayScenery;
            scenery.parking=Rect(_root,"Scenery during replay seek",0,0,W,H);
            scenery.parking.gameObject.SetActive(false);
            foreach(var layer in scenery.rear)layer.SetParent(scenery.parking,false);
            foreach(var layer in scenery.front)layer.SetParent(scenery.parking,false);
            foreach(var pair in _bloodridgeContacts)
            {
                pair.Value.transform.SetParent(scenery.parking,false);
                scenery.contacts.Add(pair.Key.unit.Id,pair.Value);
            }
            _bloodridgeContacts.Clear();
        }
        void RestoreReplayForeground()
        {
            var scenery=_replayScenery;
            foreach(var layer in scenery.front)layer.SetParent(_stage,false);
            foreach(var f in _figures)
            {
                var contact=scenery.contacts[f.unit.Id];
                contact.transform.SetParent(f.root,false);
                contact.transform.SetAsFirstSibling();
                _bloodridgeContacts.Add(f,contact);
            }
            scenery.contacts.Clear();
            Destroy(scenery.parking.gameObject);
            scenery.parking=null;
        }

        FightPlayback _playback;
        Page _playbackReturn;
        bool _returnPaused;
        float _returnSpeed;
        long _lessonFrom=-1,_lessonTo=-1;
        int _playbackCursor;
        long _pendingPlaybackSeek=-1;
        bool _seeking;
        bool _lessonStop;
        BattleSimulation ViewSim=>_playback!=null?_playback.Sim:_fight.Sim;
        // Over once the sim is over and its last blow has played on screen (CampaignArtBeats.cs).
        bool ViewOver=>(_playback!=null?_playback.AtEnd:_fight.Over)&&!BeatsPending;
        // Campaign.BuildSetup writes camp.counter.tribe.kind. Only recognised authored IDs
        // choose art; an old or unknown archive never borrows the current run's tribe.
        string[] PlaybackParts=>_playback.FightId.Split('.');
        string ViewTribe=>_playback==null?_fight.Encounter.Tribe.Id:
            PlaybackParts.Length==4&&PlaybackParts[0]=="camp"?PlaybackParts[2]:"";
        EncounterKind ViewKind=>_playback==null?_fight.Encounter.Kind:
            PlaybackParts.Length==4&&Enum.TryParse(PlaybackParts[3],true,out EncounterKind kind)?kind:EncounterKind.WarParty;
        string FigureTribe(BattleUnit unit)
        {
            if(_playback==null)
            {
                if(unit.Side==0)foreach(var f in _fight.Squad)if(f.Id==unit.Id)return f.TribeId;
                return ViewTribe;
            }
            if(unit.Side==1)return ViewTribe;
            if(unit.Id.StartsWith("tutorial.",StringComparison.Ordinal))return CampaignTribes.ClarthId;
            string prefix=unit.Id.Split('.')[0];
            return CampaignTribes.Find(prefix)!=null?prefix:"";
        }
        void WatchFight(ArchivedFight fight,long from=-1,long to=-1)
        {
            _pendingPlaybackSeek=-1;
            if(!FightPlayback.TryOpen(_campaign.Archive,fight.FightId,out var playback,out string why))
            {ShowArchivedFight(fight,why);return;}
            _playbackReturn=_page;_returnPaused=_paused;_returnSpeed=_speed;
            _playback=playback;_lessonFrom=from;_lessonTo=to;
            _lessonStop=from>=0;
            _paused=true;_speed=1;_targeting=false;_notice="";_feed.Clear();
            SetPage(Page.Playback);
            if(from>=0)SeekPlayback(from);
        }
        void LeavePlayback()
        {
            if(_playback==null)return;
            _pendingPlaybackSeek=-1;
            var fight=_playback.Fight;
            _playback=null;_paused=_returnPaused;_speed=_returnSpeed;
            SetPage(_playbackReturn);
            ShowArchivedFight(fight);
        }
        void BuildPlaybackControls()
        {
            _coachShown=false;_coachPlate.enabled=false;_playbackCursor=0;_accum=0;
            SetPx(_pxTitle,"RECORDED FIGHT  /  "+TribeName(ViewTribe),PxBone);
            PxButton("pause","PLAY",528,6,64,28,()=>_paused=!_paused);
            PxButton("speed","1X",594,6,40,28,()=>_speed=_speed>=4?1:_speed*2);
            PxButton("playback-back","BACK",636,6,80,28,LeavePlayback);
            PxButton("playback-start","RESTART",16,418,126,28,()=>SeekPlayback(0));
            PxButton("playback-prev","BACK 5S",146,418,126,28,()=>SeekPlayback(_playback.Tick-5*SimClock.TicksPerSecond));
            PxButton("playback-next","AHEAD 5S",276,418,126,28,()=>SeekPlayback(_playback.Tick+5*SimClock.TicksPerSecond));
            PxButton("playback-end","END",406,418,126,28,()=>SeekPlayback(_playback.Length));
            if(_lessonFrom>=0)PxButton("playback-lesson","LESSON MOMENT",536,418,168,28,()=>SeekPlayback(_lessonFrom));
            else PxButton("playback-sound","SOUND",536,418,168,28,ShowSound);
            RefreshPlayback();DrawBattle();
        }
        void UpdatePlayback()
        {
            float dt=_auto?1f/30:Mathf.Min(Time.unscaledDeltaTime,.1f);
            if(!_paused&&StageHolding)AdvanceStage(ref dt);
            if(!_paused&&dt>0&&!StageHolding)
            {
                float step=dt*(ViewOver?1:_speed);
                _visualTime+=step;
                AdvanceCombatPoses(step);
                AdvanceBeats(step);
                if(!BeatsWait)_accum+=dt*_speed*SimClock.TicksPerSecond;
                while(_accum>=1&&!StageHolding&&!BeatsWait&&!_playback.AtEnd&&_playback.Failure.Length==0)
                {
                    Anticipate();if(!_playback.Step())break;
                    DrainPlayback();_accum--;
                    if(_lessonStop&&_lessonTo>=0&&_playback.Tick>=_lessonTo){_paused=true;_lessonStop=false;break;}
                }
                if(BeatsWait)_accum=Mathf.Min(_accum,.99f);
                if(ViewOver)_endAge+=dt;
                if(_playback.Failure.Length>0)_paused=true;
            }
            RefreshPlayback();DrawBattle();
        }
        void DrainPlayback()
        {
            var events=_playback.Sim.Events;
            bool staged=ViewSim.StageOn;
            bool beats=!staged&&_playbackCursor<events.Count&&BeatsOn;
            if(staged)PresentBattleEvents(events,_playbackCursor);
            else if(beats)PresentBeats(events,_playbackCursor);
            for(;_playbackCursor<events.Count;_playbackCursor++)
            {
                var e=events[_playbackCursor];if(!staged&&!beats)AnimateEvent(e);
                if(e.Kind==BattleEventKind.Kill)Feed(Seconds(e.Tick)+"  "+NameOf(e.TargetId)+" fell.");
                else if(e.Kind==BattleEventKind.Windup)Feed(Seconds(e.Tick)+"  "+NameOf(e.ActorId)+
                    (e.Detail==HeavyRules.Heavy?" winds up a heavy swing at "+NameOf(e.TargetId)+".":" begins "+e.Detail+"."));
                else if(e.Kind==BattleEventKind.Directive&&Directives.TryParse(e.Detail,out Directive d))
                    Feed(Seconds(e.Tick)+"  "+(e.ActorSide==0?"Your side: ":"Their side: ")+OrderName(d)+".");
            }
        }
        void SeekPlayback(long tick)
        {
            tick=Math.Max(0,Math.Min(_playback.Length,tick));_paused=true;StopSound();
            _lessonStop=_lessonFrom>=0&&tick<=_lessonFrom;
            // Restart and rebuild the figures: Restart replaces the units, so old references are invalid.
            _pendingPlaybackSeek=tick;
            _feed.Clear();
            // The builder already has this replay at tick zero. Queue the newest target
            // without invalidating the units it is currently binding.
            if(_buildingBattle)return;
            _playback.Restart();SetPage(Page.Playback,true);
            if(!_buildingBattle)ApplyPendingPlaybackSeek();
        }
        void ApplyPendingPlaybackSeek()
        {
            if(_pendingPlaybackSeek<0||_page!=Page.Playback||_playback==null)return;
            long tick=_pendingPlaybackSeek;_pendingPlaybackSeek=-1;
            _seeking=true;
            try
            {
                while(_playback.Tick<tick&&_playback.Failure.Length==0)
                {
                    float step=1f/SimClock.TicksPerSecond;_visualTime+=step;AdvanceCombatPoses(step);
                    Anticipate();if(!_playback.Step())break;DrainPlayback();
                }
            }
            finally{_seeking=false;}
            SnapPairPositions();RestoreChiefEffectsAfterSeek();
            RefreshPlayback();DrawBattle();
        }
        void RefreshPlayback()
        {
            var sim=_playback.Sim;
            EnsureReadouts(sim);DrawNerve(sim);DrawLeaderMarks(sim);
            if(!CleanCombatHud){DrawCastTimer(sim);DrawHeavyTarget(sim);}
            DrawFallenTokens();
            DrawParryMeter(sim);
            if(sim.MeterOn)SetPx(_pxTitle,Fit("RECORDED FIGHT / "+TribeName(ViewTribe),300),PxBone);
            SetPx(_pxStats,Seconds(_playback.Tick)+" / "+Seconds(_playback.Length),PxMuted);
            string status=_playback.Failure.Length>0?"Replay stopped: "+_playback.Failure:
                _playback.AtEnd?(_playback.Matched?"Verified: this fight matches its saved record.":"End reached. Verification unavailable."):
                _paused?"Paused. Space or Play to watch.":"Recorded orders only. Your campaign is unchanged.";
            if(CleanCombatHud&&!_playback.AtEnd&&_playback.Failure.Length==0&&_notice.Length==0&&OrderReceiptPriority(sim))
                status="RECORDED / "+Receipt(_orderReads[_orderReads.Count-1]);
            SetPx(_pxBanner,Fit(status,660),_playback.Failure.Length>0?PxRed:PxGold);
            SetPx(_pxBanner2,_hover.Length>0?_hover:_notice.Length>0?_notice:_feed.Count>0?_feed[_feed.Count-1]:"Point at a fighter to see their name and health.",PxBone);
            SetPx(_pxButtonLabels["pause"],_paused?"PLAY":"PAUSE",PxBone);
            _buttons["pause"].interactable=(!_playback.AtEnd||StageHolding||BeatsPending)&&_playback.Failure.Length==0;
            SetPx(_pxButtonLabels["speed"],_speed+"X",PxBone);
        }
        void MoveModalCloseToTop()
        {
            _buttons["modal-close"].GetComponent<RectTransform>().anchoredPosition=new Vector2(508*Px,-8*Px);
        }
    }
}
