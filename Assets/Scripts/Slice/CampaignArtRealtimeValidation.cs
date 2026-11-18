using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.Profiling;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // Opt-in QA only. No pilot, tuning, owner save, or fast simulation clock during measurement.
        // Run with -ksArtValidate <isolated output> -ksRealtimeQa <frozen fixture directory>.
        IEnumerator ValidateRealtimeQa()
        {
            string fixtures=Argument("-ksRealtimeQa");
            float seconds=20;
            if(float.TryParse(Argument("-ksRealtimeSeconds"),NumberStyles.Float,CultureInfo.InvariantCulture,out float requested))
                seconds=Mathf.Clamp(requested,5,120);
            int passes=3;
            if(int.TryParse(Argument("-ksRealtimePasses"),out int requestedPasses))passes=Mathf.Clamp(requestedPasses,1,10);
            var cases=new List<(string name,Campaign camp,ArchivedFight fight)>();
            IntegrationCap(39);
            foreach(bool chief in new[]{false,true})
            {
                var camp=chief?FreshRaidPreview():FreshRoadPreview();
                var fight=chief?camp.Active:camp.StartEncounter(0);
                bool parried=false;
                // Build one fixed archive per scenario, solely to exercise presentation. Not a balance sweep.
                for(int i=0;i<6000&&!fight.Over;i++)
                {
                    if(fight.Sim.AwaitingVerdict)fight.TryOrder(Directive.Hold,null,false,out _);
                    else if(!parried&&fight.Sim.Threats(0).Any(t=>t.ParryWindow))
                        parried=fight.TryOrder(Directive.Hold,null,true,out _);
                    fight.Tick();
                }
                Check(fight.Over,"fixed realtime QA fixture completes: "+(chief?"chief":"road"));
                camp.Finish(fight);
                cases.Add((chief?"chief39":"road39",camp,camp.Archive.Fights.Last()));
            }
            foreach(string name in new[]{"SandShark","CageBear"})
            {
                var camp=CampaignSave.Read(File.ReadAllText(Path.Combine(fixtures,"beast-"+name+"-save.txt")));
                cases.Add((name+"39",camp,camp.Archive.Fights.Last()));
            }
            var legacy=CampaignSave.Read(File.ReadAllText(Path.Combine(fixtures,"legacy37.txt")));
            cases.Add(("legacy37",legacy,legacy.Archive.Fights.Last()));
            string[] snapshots=cases.Select(c=>CampaignSave.Write(c.camp)).ToArray();
            foreach(var c in cases)
                Check(c.camp.Archive.TryReplay(c.fight.FightId,out _,out var why),"fixture checksum before measurement: "+c.name+" "+why);

            // Restore the ordinary on-screen rendering path. Capture PNGs only outside measurement intervals.
            _auto=false;_pilotOn=false;_fight=null;_playback=null;
            Camera.main.targetTexture=null;_canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            var seekTimings=new StringBuilder("case,target,actionMs,readyMs,maxFrameMs,maxBuildSliceMs\n");
            foreach(var c in cases)
            {
                _playback=null;_campaign=c.camp;_page=Page.Offers;WatchFight(c.fight);
                yield return RealtimeQaWaitForBattle();
                var scenery=_replayScenery;
                var textures=_bloodridgeTextures.ToArray();
                long target=Math.Min(150,c.fight.Ticks);
                yield return MeasureQaSeek(c.name,target,seekTimings);
                Check(_playback.Tick==target&&_buttons.ContainsKey("pause")&&_pendingPlaybackSeek<0,"async seek reaches target with ready controls: "+c.name);
                Check(_replayScenery==scenery&&_bloodridgeTextures.SequenceEqual(textures),"seek retains this replay's loaded layers: "+c.name);
                if(scenery!=null)
                    Check(scenery.parking==null&&scenery.front.All(n=>n.parent==_stage)&&scenery.rear.All(n=>n.parent==_stage)
                        &&_bloodridgeContacts.Count==_figures.Count&&_figures.All(f=>_bloodridgeContacts[f].transform.parent==f.root),
                        "seek restores scenery and contact order on new figures: "+c.name);
                ScreenCapture.CaptureScreenshot(Path.Combine(_capture,c.name+"-seek.png"));
                yield return null;yield return null;
                Check(FightPlayback.TryOpen(c.camp.Archive,c.fight.FightId,out var expected,out _),"independent seek replay opens: "+c.name);
                while(expected.Tick<target)expected.Step();
                Check(ViewSim.OrderedStateSnapshot==expected.Sim.OrderedStateSnapshot,"async seek preserves exact state: "+c.name);
                yield return MeasureQaSeek(c.name,c.fight.Ticks,seekTimings);
                Check(_playback.AtEnd&&_playback.Matched,"async end seek verifies archive: "+c.name);
                yield return MeasureQaSeek(c.name,0,seekTimings);
                Check(_playback.Tick==0,"restart reaches first tick: "+c.name);
                SeekPlayback(0);SeekPlayback(target);yield return RealtimeQaWaitForBattle();
                Check(_playback.Tick==target&&ViewSim.OrderedStateSnapshot==expected.Sim.OrderedStateSnapshot,
                    "newest seek wins during screen rebuild: "+c.name);
                if(scenery!=null)
                {
                    SeekPlayback(0);yield return null;yield return null;
                    WatchFight(c.fight);yield return RealtimeQaWaitForBattle();
                    Check(_replayScenery!=scenery&&textures.All(t=>t==null)&&_playback.Tick==0,
                        "reopening during seek releases the previous replay scenery: "+c.name);
                }
            }
            File.WriteAllText(Path.Combine(_capture,"replay-seeks.csv"),seekTimings.ToString());
            var report=new StringBuilder("Rendered Windows player QA; 1440x900, target60, vsync0, realtime1x.\n");
            report.AppendLine("Thresholds: steady p95<=20ms, p99<=33.34ms; >100ms frames flagged. Transition frames reported separately.");
            report.AppendLine("Memory: compare warmed pass2 to pass3 per fixture; >32MiB Unity used growth or >16 textures flagged. No forced GC.");
            report.AppendLine("This is automated replay/render evidence, not native input, perceived audio or owner approval.");
            report.AppendLine(SystemInfo.operatingSystem+" | "+SystemInfo.processorType+" | "+SystemInfo.graphicsDeviceName);
            report.AppendLine("Unity "+Application.unityVersion+" audio "+AudioSettings.outputSampleRate+"Hz; output="+_capture);
            var frames=new StringBuilder("pass,case,frame,ms,tick,transition\n");
            var memory=new StringBuilder("pass,case,unity_used,unity_reserved,managed,working_set,textures,gameobjects\n");
            var warnings=new List<string>();
            var warmMemory=new Dictionary<string,long>();var warmTextures=new Dictionary<string,int>();
            var tap=Camera.main.gameObject.AddComponent<RealtimeQaAudioTap>();
            tap.Configure(AudioSettings.outputSampleRate,30);
            try
            {
                for(int pass=0;pass<passes;pass++)for(int index=0;index<cases.Count;index++)
                {
                    var c=cases[index];_playback=null;_campaign=c.camp;_page=Page.Offers;
                    WatchFight(c.fight);_speed=1;_paused=false;
                    float warm=Time.realtimeSinceStartup+2;
                    while(Time.realtimeSinceStartup<warm)yield return null;
                    int music=ViewKind==EncounterKind.Chief?2:1;
                    Check(_musicVoices[music].isPlaying&&_musicVoices[music].volume>.1f,"correct music in "+c.name+": "+_musicVoices[music].clip.name);
                    Check(_musicVoices.Where((v,i)=>i!=music).All(v=>v.volume<.01f),"other music faded out in "+c.name);
                    if(pass==0)
                    {
                        ScreenCapture.CaptureScreenshot(Path.Combine(_capture,c.name+"-render.png"));
                        yield return null;yield return null;
                        warm=Time.realtimeSinceStartup+1;
                        while(Time.realtimeSinceStartup<warm)yield return null;
                    }
                    tap.Begin();
                    float until=Time.realtimeSinceStartup+seconds,settleUntil=Time.realtimeSinceStartup;
                    var steady=new List<float>(8192);var transitions=new List<float>(256);
                    int n=0,restarts=0;long firstTick=_playback.Tick;
                    while(Time.realtimeSinceStartup<until)
                    {
                        yield return null;
                        float ms=Time.unscaledDeltaTime*1000;
                        bool transition=Time.realtimeSinceStartup<settleUntil;
                        (transition?transitions:steady).Add(ms);
                        frames.Append(pass+1).Append(',').Append(c.name).Append(',').Append(n++).Append(',')
                            .Append(ms.ToString("F3",CultureInfo.InvariantCulture)).Append(',').Append(_playback.Tick).Append(',').Append(transition?1:0).Append('\n');
                        CheckRealtimePlayback();
                        if(_playback.AtEnd)
                        {
                            Check(_playback.Matched,"realtime replay matches at end: "+c.name);
                            SeekPlayback(0);_speed=1;_paused=false;restarts++;settleUntil=Time.realtimeSinceStartup+1;
                        }
                    }
                    _paused=true;
                    tap.End(pass==0?Path.Combine(_capture,c.name+"-mix.wav"):null,out var sound);
                    yield return null;yield return null;
                    Check(_playback.Tick!=firstTick||restarts>0,"realtime clock advanced: "+c.name);
                    Check(steady.Count>60,"steady frame sample count: "+c.name);
                    steady.Sort();transitions.Sort();
                    float p95=QaPercentile(steady,.95f),p99=QaPercentile(steady,.99f),max=steady[steady.Count-1];
                    string line="pass"+(pass+1)+" "+c.name+": frames="+n+", p95="+p95.ToString("F2")+"ms p99="+p99.ToString("F2")+"ms max="+max.ToString("F2")+
                        "ms >100ms="+steady.Count(v=>v>100)+" transitionMax="+(transitions.Count>0?transitions.Last():0).ToString("F2")+"ms restarts="+restarts+"; "+sound;
                    report.AppendLine(line);Debug.Log("[RealtimeQA] "+line);
                    if(p95>20||p99>33.34f||max>100)warnings.Add("frame pacing: pass"+(pass+1)+" "+c.name);
                    if(tap.Samples==0||tap.Peak<.00001f)warnings.Add("no measured output: "+c.name);
                    if(tap.Invalid>0||tap.Clipped>0)warnings.Add("audio invalid/overload samples: "+c.name+" "+tap.Invalid+"/"+tap.Clipped);
                    long used=Profiler.GetTotalAllocatedMemoryLong(),reserved=Profiler.GetTotalReservedMemoryLong(),managed=GC.GetTotalMemory(false);
                    int textures=Resources.FindObjectsOfTypeAll<Texture2D>().Length,objects=Resources.FindObjectsOfTypeAll<GameObject>().Length;
                    using(var process=System.Diagnostics.Process.GetCurrentProcess())
                        memory.Append(pass+1).Append(',').Append(c.name).Append(',').Append(used).Append(',').Append(reserved).Append(',')
                            .Append(managed).Append(',').Append(process.WorkingSet64).Append(',').Append(textures).Append(',').Append(objects).Append('\n');
                    if(pass==1){warmMemory[c.name]=used;warmTextures[c.name]=textures;}
                    if(pass>1&&(used-warmMemory[c.name]>32L*1024*1024||textures-warmTextures[c.name]>16))warnings.Add("retained memory growth: "+c.name);
                    File.WriteAllText(Path.Combine(_capture,"realtime-summary.txt"),report.ToString());
                    File.WriteAllText(Path.Combine(_capture,"realtime-frames.csv"),frames.ToString());
                    File.WriteAllText(Path.Combine(_capture,"realtime-memory.csv"),memory.ToString());
                }
                for(int i=0;i<cases.Count;i++)Check(CampaignSave.Write(cases[i].camp)==snapshots[i],"realtime QA leaves archive/campaign unchanged: "+cases[i].name);
                // Pause, mute and audio resumption use the real live sound path, without altering stored settings.
                long held=_playback.Tick;
                yield return new WaitForSecondsRealtime(.2f);
                Check(_playback.Tick==held&&_soundVoices.All(v=>!v.isPlaying),"paused replay stops combat and effect voices");
                _soundSettings.muted=true;SoundPauseTick();
                yield return new WaitForSecondsRealtime(2);
                tap.Begin();yield return new WaitForSecondsRealtime(.3f);tap.End(null,out var mutedSignal);
                Check(_musicVoices.All(v=>v.volume==0)&&tap.Samples>0&&tap.Peak<.00001f,"mute silences output: "+mutedSignal);
                _soundSettings.muted=false;_paused=false;
                yield return new WaitForSecondsRealtime(2);
                tap.Begin();yield return new WaitForSecondsRealtime(.3f);tap.End(null,out var resumedSignal);
                Check(tap.Samples>0&&tap.Peak>.00001f,"unmute/resume restores output: "+resumedSignal);
                _paused=true;
                report.AppendLine(warnings.Count==0?"Measured checks within thresholds.":"REVIEW REQUIRED:\n"+string.Join("\n",warnings));
                report.AppendLine("Native capture failed separately; hands-on input/feel and listening remain NOT CHECKED.");
                File.WriteAllText(Path.Combine(_capture,"realtime-summary.txt"),report.ToString());
            }
            finally {tap.End(null,out _);Destroy(tap);_paused=true;}
        }
        IEnumerator MeasureQaSeek(string name,long target,StringBuilder results)
        {
            // Begin on a clean frame, include the button handler, and sample through the
            // frame AFTER the builder finishes so its final canvas update is counted.
            yield return null;
            double start=Time.realtimeSinceStartupAsDouble;
            SeekPlayback(target);
            double actionMs=(Time.realtimeSinceStartupAsDouble-start)*1000;
            Check(_buildingBattle&&_pendingPlaybackSeek==target,"seek waits for async HUD: "+name);
            float maxFrame=0;
            int frames=0;
            do
            {
                yield return null;
                maxFrame=Mathf.Max(maxFrame,Time.unscaledDeltaTime*1000);
            } while(_buildingBattle&&++frames<600);
            Check(!_buildingBattle,"async battle construction completes");
            double readyMs=(Time.realtimeSinceStartupAsDouble-start)*1000;
            yield return null;
            maxFrame=Mathf.Max(maxFrame,Time.unscaledDeltaTime*1000);
            results.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2:F3},{3:F3},{4:F3},{5:F3}\n",
                name,target,actionMs,readyMs,maxFrame,_buildSliceMaxMs);
        }
        void CheckRealtimePlayback()
        {if(_playback==null||_playback.Failure.Length>0)throw new InvalidOperationException("Realtime replay failed: "+_playback?.Failure);}
        IEnumerator RealtimeQaWaitForBattle()
        {
            int frames=0;
            while(_buildingBattle&&frames++<600)yield return null;
            Check(!_buildingBattle,"async battle construction completes");
        }
        static float QaPercentile(List<float> sorted,float p)=>sorted[Mathf.Clamp(Mathf.CeilToInt(sorted.Count*p)-1,0,sorted.Count-1)];
    }

    // Read-only listener tap. Preallocated storage, no disk/Unity calls on the audio thread, samples never modified.
    public sealed class RealtimeQaAudioTap:MonoBehaviour
    {
        readonly object _gate=new object();float[] _data;bool _running;int _rate,_channels,_written;
        public long Samples{get;private set;}public long Clipped{get;private set;}public long Invalid{get;private set;}
        public float Peak{get;private set;}double _squares;
        public void Configure(int rate,int seconds){_rate=rate;_data=new float[rate*2*seconds];}
        public void Begin()
        {lock(_gate){Samples=Clipped=Invalid=0;Peak=0;_squares=0;_written=0;_channels=0;_running=true;}}
        void OnAudioFilterRead(float[] data,int channels)
        {
            lock(_gate)
            {
                if(!_running)return;_channels=channels;
                foreach(float v in data)
                {Samples++;if(float.IsNaN(v)||float.IsInfinity(v)){Invalid++;continue;}float a=Math.Abs(v);if(a>Peak)Peak=a;if(a>=1)Clipped++;_squares+=v*(double)v;}
                int count=Math.Min(data.Length,_data.Length-_written);if(count>0){Array.Copy(data,0,_data,_written,count);_written+=count;}
            }
        }
        public void End(string path,out string summary)
        {
            lock(_gate){_running=false;}
            summary="audio samples="+Samples+" peak="+Peak.ToString("F4")+" RMS="+Math.Sqrt(_squares/Math.Max(1,Samples)).ToString("F4")+" >=1="+Clipped+" invalid="+Invalid;
            if(path==null||_written==0||_channels==0)return;
            // IEEE float WAV preserves overload peaks for analysis instead of hiding them with integer clamping.
            using(var w=new BinaryWriter(File.Create(path)))
            {
                w.Write(Encoding.ASCII.GetBytes("RIFF"));w.Write(36+_written*4);w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16);w.Write((short)3);w.Write((short)_channels);w.Write(_rate);w.Write(_rate*_channels*4);
                w.Write((short)(_channels*4));w.Write((short)32);w.Write(Encoding.ASCII.GetBytes("data"));w.Write(_written*4);
                for(int i=0;i<_written;i++)w.Write(_data[i]);
            }
        }
    }
}
