using System;
using System.Collections.Generic;
using System.IO;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // Placeholder sound from ElevenLabs, Jacob's 28 Oct picks (lane a, grim and heavy, for every effect; camp a, fight a,
        // chief fight a; the Clyde voice for the Bloodridge chief). Files live in Resources/CampaignArt/Audio; candidates and
        // prompts are in ComfyUI-Shared/output/ArtLab/audio_fx28, audio_music28 and audio_voice28. A missing file is silent,
        // except the first six cues, which fall back to the old local tones.
        enum Cue {Accepted,Unavailable,Shield,Cast,Break,Reward,Hit,Heal,Kill,Trap,Interrupt,Combo,Win,Lose}
        const int CueCount=14,SynthCues=6;
        const string AudioFolder="CampaignArt/Audio/";
        // The file each cue plays. Accepted is the UI accept; orders play their own sound through SoundEvent.
        // Shield is the parry: CampaignArtFightRules plays it on a Parry event.
        static readonly string[] CueFile={"click","order_refused","parry","windup","side_breaks","gear_mid",
            "hit","heal","fall","spell_snare","shield_bash","combo","Win","Lose"};
        // Frequent hits and heals sit under orders and warnings.
        static readonly float[] CueLevel={1,1,1,1,1,1,.5f,.6f,.85f,1,1,1,1,1};
        [Serializable] sealed class SoundSettings {public float volume=.65f;public bool muted;}
        SoundSettings _soundSettings=new SoundSettings();
        readonly AudioClip[] _soundClips=new AudioClip[CueCount];
        readonly bool[] _soundMade=new bool[CueCount];
        readonly AudioSource[] _soundVoices=new AudioSource[5];
        readonly double[] _cueAt=new double[CueCount];
        readonly int[] _cuePlayed=new int[CueCount];
        readonly Dictionary<string,AudioClip> _named=new Dictionary<string,AudioClip>();
        readonly Dictionary<string,double> _namedAt=new Dictionary<string,double>();
        AudioSource _lineVoice;
        // Music: map loop outside fights, battle loop in fights and playback, chief loop in chief fights.
        // The loop seam is baked into the files.
        const float MusicLevel=.45f,MusicFadeSeconds=1.2f;
        static readonly string[] MusicTracks={"MapLoop","BattleLoop","ChiefLoop"};
        readonly AudioSource[] _musicVoices=new AudioSource[3];
        int _voiceCursor;
        bool _soundWasPaused;
        string SoundPath=>Path.Combine(Path.GetDirectoryName(_savePath),"sound-settings.json");
        void InitSound()
        {
            LoadSoundSettings();
            if(FindObjectOfType<AudioListener>()==null)Camera.main.gameObject.AddComponent<AudioListener>();
            for(int i=0;i<CueCount;i++)
            {
                _cueAt[i]=-100;_soundClips[i]=Resources.Load<AudioClip>(AudioFolder+CueFile[i]);
                // The import leaves Preload Audio Data off: load the cues now so the first play has no hitch and GetData works.
                if(_soundClips[i]!=null)_soundClips[i].LoadAudioData();
                else if(i<SynthCues){_soundClips[i]=MakeCue((Cue)i);_soundMade[i]=true;}
            }
            for(int i=0;i<_soundVoices.Length;i++)
            {
                var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;
                s.loop=false;s.dopplerLevel=0;s.priority=i>=3?32:128;_soundVoices[i]=s;
            }
            _lineVoice=gameObject.AddComponent<AudioSource>();_lineVoice.playOnAwake=false;_lineVoice.spatialBlend=0;
            _lineVoice.loop=false;_lineVoice.dopplerLevel=0;_lineVoice.priority=16;
            for(int i=0;i<_musicVoices.Length;i++)
            {
                var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;
                s.loop=true;s.dopplerLevel=0;s.priority=0;s.volume=0;
                s.clip=Resources.Load<AudioClip>(AudioFolder+MusicTracks[i]);_musicVoices[i]=s;
            }
        }
        void LoadSoundSettings()
        {
            try{if(File.Exists(SoundPath))_soundSettings=JsonUtility.FromJson<SoundSettings>(File.ReadAllText(SoundPath))??new SoundSettings();}
            catch(Exception e){Debug.LogWarning("Sound settings reset: "+e.Message);_soundSettings=new SoundSettings();}
            _soundSettings.volume=float.IsNaN(_soundSettings.volume)? .65f:Mathf.Clamp01(_soundSettings.volume);
        }
        void StoreSoundSettings()
        {
            try{File.WriteAllText(SoundPath,JsonUtility.ToJson(_soundSettings));}
            catch(Exception e){_notice="Sound settings could not be saved: "+e.Message;}
        }
        // Fallback for the first six cues when no clip is in Resources. Original, local PCM synthesis.
        // Soft attack/release envelopes prevent clicks.
        static AudioClip MakeCue(Cue cue)
        {
            const int rate=44100;
            float duration=cue==Cue.Cast?.72f:cue==Cue.Break?.65f:cue==Cue.Reward?.62f:cue==Cue.Shield?.16f:.18f;
            var samples=new float[Mathf.CeilToInt(rate*duration)];
            uint noise=12345;
            for(int i=0;i<samples.Length;i++)
            {
                float t=i/(float)rate,p=t/duration;
                float envelope=Mathf.Min(1,t/.008f)*Mathf.Pow(1-p,2);
                float tone;
                switch(cue)
                {
                    case Cue.Accepted:tone=Mathf.Sin(2*Mathf.PI*(300*t+500*t*t))*.32f;break;
                    case Cue.Unavailable:tone=(Mathf.Sin(2*Mathf.PI*120*t)+.3f*Mathf.Sin(2*Mathf.PI*127*t))*.24f;break;
                    case Cue.Shield:
                        noise=1664525*noise+1013904223;
                        tone=(Mathf.Sin(2*Mathf.PI*190*t)*.24f+Mathf.Sin(2*Mathf.PI*511*t)*.12f+(noise/(float)uint.MaxValue-.5f)*.16f)*Mathf.Exp(-t*10);break;
                    case Cue.Cast:tone=(Mathf.Sin(2*Mathf.PI*180*t)+.4f*Mathf.Sin(2*Mathf.PI*270*t))*.22f*(.5f+.5f*Mathf.Cos(2*Mathf.PI*5*t));break;
                    case Cue.Break:tone=(Mathf.Sin(2*Mathf.PI*(150*t-65*t*t))+.3f*Mathf.Sin(2*Mathf.PI*71*t))*.28f;break;
                    default:
                        tone=(Mathf.Sin(2*Mathf.PI*330*t)+.6f*Mathf.Sin(2*Mathf.PI*412.5f*t)+.4f*Mathf.Sin(2*Mathf.PI*495*t))*.19f;break;
                }
                samples[i]=tone*envelope;
            }
            var clip=AudioClip.Create("KS "+cue,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        void PlayCue(Cue cue)
        {
            if(_seeking||_soundSettings.muted||_soundSettings.volume<=0)return;
            int n=(int)cue;if(_soundClips[n]==null)return;
            double now=AudioSettings.dspTime;
            double cooldown=cue==Cue.Shield?.12:cue==Cue.Unavailable?.18:cue==Cue.Heal?.15:cue==Cue.Hit?.06:.08;
            if(now-_cueAt[n]<cooldown)return;
            _cueAt[n]=now;
            // Reserve a voice for warnings, nerve breaks, rewards and fight results; ordinary feedback cannot steal it.
            bool alert=cue==Cue.Cast||cue==Cue.Break||cue==Cue.Reward||cue==Cue.Win||cue==Cue.Lose;
            var source=_soundVoices[cue==Cue.Shield?4:alert?3:_voiceCursor++%3];if(source==null)return;
            source.Stop();source.clip=_soundClips[n];source.volume=_soundSettings.volume*CueLevel[n];
            source.Play();_cuePlayed[n]++;
        }
        AudioClip Named(string name)
        {
            if(!_named.TryGetValue(name,out var clip)){clip=Resources.Load<AudioClip>(AudioFolder+name);_named[name]=clip;}
            return clip;
        }
        // Plays one file from Resources/CampaignArt/Audio by name. Screens can call this for their own moments
        // (path picked, omen, gear drop, forge and so on). counts keeps the old cue counters right for the sprint checks;
        // when the file is missing it falls back to that cue.
        void PlaySound(string name,float level=1,double cooldown=.08,bool alert=false,Cue? counts=null,float startAt=0)
        {
            if(_seeking||_soundSettings.muted||_soundSettings.volume<=0||string.IsNullOrEmpty(name))return;
            var clip=Named(name);
            if(clip==null){if(counts.HasValue)PlayCue(counts.Value);return;}
            double now=AudioSettings.dspTime;
            if(_namedAt.TryGetValue(name,out double at)&&now-at<cooldown)return;
            _namedAt[name]=now;
            var source=_soundVoices[name=="parry"||name=="whiff"?4:alert?3:_voiceCursor++%3];if(source==null)return;
            source.Stop();source.clip=clip;source.volume=_soundSettings.volume*level;
            if(startAt>0)source.time=Mathf.Min(startAt,clip.length*.95f);
            source.Play();
            if(counts.HasValue)_cuePlayed[(int)counts.Value]++;
        }
        // The wind up swell (windup.wav) is loudest WindupPeak seconds in. It starts from the fight clock, not at the
        // Windup or Tell event, so its loud point lands two ticks after the parry window opens (a Hold in the last
        // FightRules.PerfectHoldTicks before a hit lands parries it) at any fight speed, and it waits through pauses.
        const float WindupPeak=.77f;
        readonly List<(long start,long peak)> _swells=new List<(long start,long peak)>();
        object _swellSim;
        object SwellSim=>_page==Page.Playback?_playback?.Sim:_page==Page.Fight?(object)_fight?.Sim:null;
        void QueueSwell(BattleEvent e)
        {
            if(_seeking||_soundSettings.muted||_soundSettings.volume<=0)return;
            object sim=SwellSim;if(sim!=_swellSim){_swells.Clear();_swellSim=sim;}
            if(sim!=null&&_swells.Count<4)_swells.Add((e.Tick,e.Tick+Math.Max(0,e.Amount-((BattleSimulation)sim).ParryWindowTicks+2)));
        }
        void SwellTick()
        {
            if(_swells.Count==0)return;
            object sim=SwellSim;if(sim==null||sim!=_swellSim){_swells.Clear();return;}
            if(_paused)return;
            long tick=_page==Page.Playback?_playback.Tick:_fight.Sim.CurrentTick;
            for(int i=_swells.Count-1;i>=0;i--)
            {
                // Seconds until the loud point, at the speed the fight runs now; _accum is the part of a tick already run.
                float toPeak=(_swells[i].peak-tick-_accum)/SimClock.TicksPerSecond/Mathf.Max(.05f,_speed);
                bool stale=tick<_swells[i].start||toPeak<-.1f;
                if(!stale&&toPeak>WindupPeak)continue;
                _swells.RemoveAt(i);
                if(!stale)PlaySound("windup",1,.15,alert:true,counts:Cue.Cast,startAt:WindupPeak-Mathf.Max(0,toPeak));
            }
        }
        // Chief and beast moves arrive as their name plus an outcome; longest names first so "burning ring" beats "ring".
        static readonly string[][] MoveSounds={
            new[]{"burning ring","br_ring"},new[]{"sun mantle","br_mantle"},new[]{"rifle line","sp_rifle_line"},
            new[]{"iron advance","sp_wall_up"},new[]{"open veins","sv_open_veins"},new[]{"sluice gate","tc_tide"},
            new[]{"tidal bore","tc_tidal_bore"},new[]{"storm surge","tc_horn"},new[]{"transfusion","sv_transfusion"},
            new[]{"overclock","sp_overclock"},new[]{"biteback","shark_bite"},new[]{"riptide","tc_riptide"},
            new[]{"leeches","sv_leeches"},new[]{"cleave","br_cleave"},new[]{"brand","br_brand"},new[]{"mantle","br_mantle"},
            new[]{"advance","sp_wall_up"},new[]{"leech","sv_leeches"},new[]{"basin","sv_transfusion"},new[]{"veins","sv_open_veins"},
            new[]{"rifle","sp_rifle_line"},new[]{"volley","shark_volley"},new[]{"corner","hunt_corner"},new[]{"surge","tc_horn"},
            new[]{"track","hunt_track"},new[]{"drive","hunt_drive"},new[]{"sweep","shark_bite"},new[]{"crush","bear_grab"},
            new[]{"pulse","bear_spike"},new[]{"ring","br_ring"},new[]{"hook","sp_hook"},new[]{"bore","tc_tidal_bore"},
            new[]{"bite","shark_bite"},new[]{"dive","shark_dive"},new[]{"grab","bear_grab"},new[]{"roar","bear_roar"},
            new[]{"kill","hunt_kill"}};
        static string MoveSound(string detail)
        {
            string d=(detail??"").ToLowerInvariant();
            foreach(var m in MoveSounds)if(d.StartsWith(m[0],StringComparison.Ordinal))return m[1];
            return null;
        }
        static string SpellSound(string spell)
        {
            switch((spell??"").ToLowerInvariant())
            {
                case "eclipse":return "br_eclipse";
                case "pyre":case "firebomb":return "br_ring";
                case "steel storm":return "sp_wall_up";
                case "war cry":return "order_unleash";
                case "dust shark":return "shark_roar";
                case "blood call":return "sv_blood_call";
                case "tide":return "tc_tide";
                default:return null;
            }
        }
        static string PhaseSound(string phase)
        {
            switch((phase??"").ToLowerInvariant())
            {
                case "blood sun":return "br_blood_sun";
                case "lockdown":return "sp_lockdown";
                case "pack":return "hunt_drive";
                case "red rite":return "sv_blood_call";
                case "spring tide":return "tc_tide";
                case "blood frenzy":return "shark_roar";
                case "overdrive":case "last stand":return "bear_roar";
                default:return "omen";
            }
        }
        static string LineSound(string text)
        {
            switch(text)
            {
                case "Lucky.":return "line_lucky";
                case "Kneel.":return "line_kneel";
                case "Now you burn.":return "line_burn";
                case "You again.":return "line_again";
                case "Finish it.":return "line_finish";
                default:return null;
            }
        }
        void PlayLine(BattleEvent e)
        {
            if(_seeking||_soundSettings.muted||_soundSettings.volume<=0||_lineVoice==null)return;
            // The same text the fight shows (CampaignArtChiefTactics): playback archives keep no MetBefore, so no kneel line there.
            string text=_playback==null?(_fight!=null?_fight.ChiefLine(e.Detail):""):
                e.Detail==ChiefKits.KneelLine?"":ChiefKits.Line(_playback.Sim.Kit(e.ActorSide),e.Detail,false);
            string file=LineSound(text);if(file==null)return;
            var clip=Named(file);if(clip==null)return;
            _lineVoice.Stop();_lineVoice.clip=clip;_lineVoice.volume=_soundSettings.volume;_lineVoice.Play();
        }
        ChiefKit SoundKit(int side)
        {
            var sim=_playback!=null?_playback.Sim:_fight!=null?_fight.Sim:null;
            return sim==null?ChiefKit.None:sim.Kit(side);
        }
        void SoundEvent(BattleEvent e)
        {
            string d=e.Detail??"";string low=d.ToLowerInvariant();
            switch(e.Kind)
            {
                case BattleEventKind.Directive:
                    if(d==BattleSimulation.BlockedDetail)PlaySound("guard",.9f,.12);
                    else if(low=="cover")PlaySound("spell_guard",.9f);
                    else if(e.ActorSide==0&&Directives.TryParse(d,out Directive order))
                        PlaySound(order==Directive.Hold?"order_hold":order==Directive.FallBack?"order_fallback":
                            order==Directive.FocusFire?"order_focus":order==Directive.Unleash?"order_unleash":"order_chief",counts:Cue.Accepted);
                    break;
                // A shield soak counts as Shield, as the 13 Oct tones did; the sprint check listens for it in the chief replay.
                case BattleEventKind.Guard:if(e.Amount>0)PlaySound("guard",.8f,.12,counts:Cue.Shield);break;
                case BattleEventKind.Windup:
                {
                    string spell=SpellSound(d);
                    if(spell!=null)PlaySound(spell,1,.3,alert:e.ActorSide==1,counts:e.ActorSide==1?Cue.Cast:(Cue?)null);
                    else if(e.ActorSide==1)QueueSwell(e);
                    break;
                }
                case BattleEventKind.Release:
                    if(low=="full")PlaySound("release",1,.1);
                    else if(low=="soaked")PlaySound("guard",.9f,.12);
                    else if(low=="dodged")PlaySound("miss",.7f);
                    else if(low=="cried")PlaySound("order_unleash",.9f);
                    break;
                case BattleEventKind.Interrupt:PlaySound("shield_bash",1,.1,counts:Cue.Interrupt);break;
                case BattleEventKind.Combo:
                {
                    string c=d==FightRules.Riposte?"riposte":d==ChiefKits.Sunbreaker?"br_sunbreaker":d==ChiefKits.Breach?"sp_wall_smash":
                        d==ChiefKits.TurnedHunt?"hunt_kill":d==ChiefKits.CleanHands?"sv_open_veins":d==ChiefKits.TurnOfTheTide?"tc_tidal_bore":"combo";
                    PlaySound(c,1,.15,counts:Cue.Combo);
                    break;
                }
                case BattleEventKind.Transform:PlaySound("snap_rise");break;
                case BattleEventKind.Empowered:PlaySound("glory_up",.8f,.5);break;
                case BattleEventKind.Hit:if(e.Amount>0)PlaySound("hit",CueLevel[(int)Cue.Hit],.05,counts:Cue.Hit);break;
                case BattleEventKind.Heal:if(e.Amount>0)PlaySound("heal",CueLevel[(int)Cue.Heal],.15,counts:Cue.Heal);break;
                case BattleEventKind.TrapArmed:PlaySound("snare",.7f);break;
                case BattleEventKind.TrapSprung:PlaySound("spell_snare",1,.1,counts:Cue.Trap);break;
                case BattleEventKind.Kill:PlaySound("fall",CueLevel[(int)Cue.Kill],.08,counts:Cue.Kill);break;
                case BattleEventKind.Miss:PlaySound("miss",.6f);break;
                case BattleEventKind.Snap:PlaySound(low==FightRules.Rise||low=="frenzy"?"snap_rise":"snap_panic",.9f,.2);break;
                case BattleEventKind.Earn:if(e.ActorSide==0)PlaySound("point_earned",.5f,.2);break;
                case BattleEventKind.Reaction:
                    if(low==TightRules.WhiffDetail)PlaySound("whiff",1,0);
                    else if(low=="bannertorn")PlaySound("banner_down",1,.5,alert:true);
                    else if(low=="berserk")PlaySound("snap_rise",.9f,.3);
                    else if(low=="secondwind")PlaySound("heal",.8f,.3);
                    else if(low=="rally")PlaySound("order_unleash",.7f,.5);
                    break;
                case BattleEventKind.Wall:
                    if(low=="smashed"||low=="fell")PlaySound("sp_wall_smash",1,.3);
                    else if(low=="lockdown")PlaySound("sp_lockdown",1,.3);
                    else PlaySound("sp_wall_up",1,.3);
                    break;
                case BattleEventKind.Aim:PlaySound("spell_aimed",.8f,.2);break;
                case BattleEventKind.Mark:
                    if(low=="snare")PlaySound("snare",.8f);else if(low=="bleed")PlaySound("bleed",.8f);else if(low=="opening")PlaySound("opening",.8f);
                    break;
                case BattleEventKind.Cash:PlaySound("cash_in",.9f,.1);break;
                case BattleEventKind.Broken:PlaySound("opening",.9f,.2);break;
                case BattleEventKind.Yield:PlaySound(SoundKit(e.ActorSide)==ChiefKit.Bloodridge?"br_kneel":"chief_yields",1,.5,alert:true);break;
                case BattleEventKind.Phase:PlaySound(PhaseSound(d),1,.5,alert:true);break;
                case BattleEventKind.Join:PlaySound("commence",.8f,.5);break;
                case BattleEventKind.Tell:if(e.ActorSide==1)QueueSwell(e);break;
                case BattleEventKind.Land:PlaySound(MoveSound(d),1,.1);break;
                case BattleEventKind.Glory:PlaySound("glory_up",.8f,1);break;
                case BattleEventKind.Line:PlayLine(e);break;
                case BattleEventKind.Spare:PlaySound("capture",1,.5,alert:true);break;
                case BattleEventKind.Execute:PlaySound("chief_kill",1,.5,alert:true);break;
                case BattleEventKind.Sever:PlaySound("part_break",1,.2);break;
                case BattleEventKind.Flee:PlaySound("shark_dive",1,.5);break;
                case BattleEventKind.Forecast:PlaySound("omen",.7f,1);break;
                case BattleEventKind.Tide:PlaySound("tc_tide",.8f,2);break;
                case BattleEventKind.Bond:PlaySound("bond",.9f,.5);break;
                case BattleEventKind.Defence:
                    PlaySound(low=="brace"?"brace":low=="sidestep"||low=="stepback"?"sidestep":low=="snaptrap"?"spell_snare":low=="selfpatch"?"heal":null,.8f,.1);
                    break;
                case BattleEventKind.Chain:PlaySound("riposte",.8f,.1);break;
                case BattleEventKind.Boost:
                    PlaySound(low=="shieldbash"?"shield_bash":low=="steady"?"brace":low=="twincut"?"spell_strike":low=="rearm"?"snare":low=="stim"?"spell_treat":null,.8f,.1);
                    break;
                case BattleEventKind.ComboFinish:PlaySound("combo",1,.15);break;
                case BattleEventKind.Break:PlayCue(Cue.Break);break;
                case BattleEventKind.End:if(e.ActorSide>=0)PlayCue(e.ActorSide==0?Cue.Win:Cue.Lose);break;
            }
        }
        void StopSound()
        {
            foreach(var source in _soundVoices)if(source!=null)source.Stop();
            if(_lineVoice!=null)_lineVoice.Stop();
        }
        void SoundPauseTick()
        {
            bool paused=_paused&&(_page==Page.Fight||_page==Page.Playback);
            if(paused&&!_soundWasPaused)StopSound();_soundWasPaused=paused;
            SwellTick();MusicTick();
        }
        void MusicTick()
        {
            bool inFight=_page==Page.Fight||_page==Page.Playback;
            int want=!inFight?0:_page==Page.Fight&&_fight!=null&&_fight.Encounter.Kind==EncounterKind.Chief?2:1;
            if(_musicVoices[want]==null||_musicVoices[want].clip==null)want=inFight?1:0;
            float level=_soundSettings.muted?0:_soundSettings.volume*MusicLevel;
            for(int i=0;i<_musicVoices.Length;i++)
            {
                var s=_musicVoices[i];if(s==null||s.clip==null)continue;
                float goal=i==want?level:0;
                s.volume=Mathf.MoveTowards(s.volume,goal,Time.unscaledDeltaTime*MusicLevel/MusicFadeSeconds);
                if(goal>0&&!s.isPlaying)s.Play();
                else if(goal<=0&&s.volume<=0&&s.isPlaying)s.Stop();
            }
        }
        void ClearSound()
        {
            StopSound();for(int i=0;i<CueCount;i++)if(_soundMade[i]&&_soundClips[i]!=null)Destroy(_soundClips[i]);
            foreach(var source in _soundVoices)if(source!=null)Destroy(source);
            foreach(var source in _musicVoices)if(source!=null)Destroy(source);
            if(_lineVoice!=null)Destroy(_lineVoice);
        }
        void ShowSound()
        {
            if(_page==Page.Fight||_page==Page.Playback)_paused=true;
            if(_campOpen)CloseCamp();CloseModal();
            Modal("SOUND","Sound and music: "+(_soundSettings.muted?"MUTED":Mathf.RoundToInt(_soundSettings.volume*100)+"%")+
                "\n\nEvery order, hit, parry, combo and fall, chief and beast moves, the Bloodridge chief's voice, and camp, fight and chief fight music. Placeholder sound.\n\nF8 opens this panel. M toggles mute.\n\nA fight stays paused when you close this panel. Space resumes.");
            PxButton("sound-mute",_soundSettings.muted?"UNMUTE":"MUTE",16,398,180,28,()=>{ToggleMute();ShowSound();},_modal);
            PxButton("sound-volume","VOLUME +25%",504,398,200,28,()=>
            {
                _soundSettings.volume=_soundSettings.volume>=.99f?0:Mathf.Min(1,(Mathf.Floor(_soundSettings.volume*4)+1)/4);
                StoreSoundSettings();StopSound();ShowSound();PlayCue(Cue.Accepted);
            },_modal);
        }
        void ToggleMute(){_soundSettings.muted=!_soundSettings.muted;StopSound();StoreSoundSettings();}
    }
}
