using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KindredSiege.Sim;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // Films a rules 43 champion fight one blow at a time (Jacob, 8 Nov 2026: "Go, take it"). The same run as the
        // champion clip (seed 5145, enemies at 40 percent, the tutorial and Bloodridge's road played headless by the
        // reference pilots up to the champion's fight, as the scratch FightDump4 did), then that fight live at 1x through
        // the real screen, one frame per 1/30 s, to a lossless native 1440x900 file. Your only orders are Holds through
        // the plate whenever a parry window is open, so the fight is not the clip's to the tick.
        // -ksArtValidate <dir> -ksRecordChampion43 1 -ksReviewEncoder <ffmpeg.exe> [-ksTable42 1] [-ksRecordSeed <n>]
        IEnumerator RecordChampion43()
        {
            yield return null;
            string encoderPath=Argument("-ksReviewEncoder");
            Check(encoderPath!=null&&File.Exists(encoderPath),"-ksReviewEncoder names the encoder");
            ulong seed=ulong.TryParse(Argument("-ksRecordSeed"),out ulong given)?given:5145;
            _savePath=Path.Combine(_capture,"champion43-save.txt");
            _campaign=Campaign.New(seed,new CampaignTuning{EnemyScaleTier1=40});
            Check(CampaignPilot.PlayTutorial(_campaign,PilotStyle.Reference,seed,10),"the tutorial is won headless");
            _campaign.ChooseTribe(CampaignTribes.BloodridgeId);
            var picker=new CampaignPilot(PilotStyle.Reference,seed);
            int offer=-1;
            for(int n=0;_campaign.Stage==CampaignStage.InRun&&n<12&&_campaign.Offers.Count>0&&_campaign.Offers.All(o=>o.Kind!=EncounterKind.Chief);n++)
            {
                int pick=picker.PickOffer(_campaign);
                offer=_campaign.Offers.ToList().FindIndex(o=>o.Champion);
                if(offer>=0)break;
                var road=_campaign.StartEncounter(pick);
                CampaignPilot.PlayFight(_campaign,road,new CampaignPilot(PilotStyle.Reference,seed*31UL+(ulong)n));
                if(_campaign.FavourOffer!=null)_campaign.TryTakeFavour(0,out _);
                else if(_campaign.SpoilsChoices.Count>0)_campaign.TryTakeSpoils(0,out _);
            }
            Check(offer>=0,"Bloodridge's road reaches its champion's fight");
            SetPage(Page.Offers);yield return null;
            Check(Click("offer-"+offer),"the champion's fight on the road");yield return null;
            Check(Click("start-fight"),"the champion's fight starts from the squad page");
            _pilotOn=false;_paused=true;_speed=1;
            yield return null;
            Check(_fight!=null&&_fight.Sim.CurrentTick==0&&_fight.Sim.ChampionFightOn&&BeatsOn,"a rules 43 champion fight, one blow at a time, at its first tick");
            var sim=_fight.Sim;const string name="champion43";
            var trace=new StringBuilder("frame,tick,beat,queue,actor,target,lead,hold,live,warn\n");
            var screen=new StringBuilder("frame,figures,items\n");
            int frames=0,tail=-1,holds=0,blows=0;Beat seen=null;
            _paused=false;
            var encoder=TrailerEncoder(encoderPath,name);
            try
            {
                while(frames<9000)
                {
                    TrailerFrame(encoder);frames++;
                    if(_beat!=null&&_beat!=seen){blows++;seen=_beat;}
                    trace.Append(frames).Append(',').Append(sim.CurrentTick).Append(',').Append(_beat!=null?1:0).Append(',').Append(_beatQueue.Count).Append(',')
                        .Append(_beat!=null?_beat.blow.ActorId:"").Append(',').Append(_beat!=null?_beat.blow.TargetId:"").Append(',')
                        .Append(_beat!=null?_beat.lead.ToString("F3"):"").Append(',').Append(_beat!=null?_beat.hold.ToString("F3"):"").Append(',')
                        .Append(BeatsLive(sim)?1:0).Append(',').Append(BeatsWarning?1:0).Append('\n');
                    ScreenAudit(screen,frames);
                    if(ViewOver&&tail<0)tail=90;
                    if(tail>=0&&tail--==0)break;
                    if(!_fight.Over&&sim.Threats(0).Any(t=>t.ParryWindow)&&sim.CanUse(0,Directive.Hold,out _)&&Click("order-0"))holds++;
                    yield return null;
                }
            }
            finally{TrailerEncoderDone(encoder,name);}
            File.WriteAllText(Path.Combine(_capture,name+"-trace.csv"),trace.ToString());
            File.WriteAllText(Path.Combine(_capture,name+"-screen.csv"),screen.ToString());
            Log(name+": seed "+seed+", "+frames+" frames ("+(frames/30f).ToString("F1")+" s on screen), sim "+(sim.CurrentTick/30f).ToString("F1")+
                " s, "+blows+" blows played one at a time, "+holds+" Holds, winner side "+sim.Winner);
            Check(_fight.Over&&blows>0,"the champion's fight plays to its end one blow at a time");
        }

        readonly List<Graphic> _auditGraphics=new List<Graphic>();
        readonly SortedDictionary<string,int> _auditCounts=new SortedDictionary<string,int>(StringComparer.Ordinal);
        // What is on screen this frame, for the noise audit (Jacob, 8 Nov 2026: "either too much noise or no noise at
        // all"): each fighter's pose kind and frame, his part in the blow playing (* actor, > target, + guard), r while he
        // runs, then every visible picture on the screen counted by its object name.
        void ScreenAudit(StringBuilder line,int frame)
        {
            line.Append(frame).Append(',');
            for(int i=0;i<_figures.Count;i++)
            {
                var f=_figures[i];var c=f.drawnClip;
                string kind=c==null?"N":c==f.idle?"I":c==f.hit?"H":c==f.block?"B":c==f.death?"D":c==f.victory?"V":
                    c==f.attack||f.table!=null&&(c==f.table.power||c==f.table.alternate||c==f.table.heal)?"A":"O";
                if(i>0)line.Append('|');
                line.Append(kind).Append(f.drawnFrame);
                if(_beat!=null){if(f==_beat.actor)line.Append('*');if(f==_beat.target)line.Append('>');if(f==_beat.guard)line.Append('+');}
                if(_beatRuns.ContainsKey(f))line.Append('r');
                if(_beatDim.TryGetValue(f,out float dim)&&dim<.99f)line.Append('d').Append(Mathf.RoundToInt(dim*100));
            }
            line.Append(',');
            _auditCounts.Clear();
            _screen.GetComponentsInChildren(false,_auditGraphics);
            foreach(var g in _auditGraphics)
            {
                if(!g.enabled||g.color.a<.05f||g.canvasRenderer.GetAlpha()<.05f)continue;
                if(g is RawImage r&&r.texture==null)continue;
                var size=g.rectTransform.rect.size;if(size.x<1||size.y<1)continue;
                var key=new StringBuilder();
                foreach(char ch in g.gameObject.name)if(!char.IsDigit(ch)&&ch!=','&&ch!=';'&&ch!=':')key.Append(ch);
                string k=key.ToString().Trim();_auditCounts.TryGetValue(k,out int n);_auditCounts[k]=n+1;
            }
            bool first=true;
            foreach(var kv in _auditCounts){if(!first)line.Append(';');first=false;line.Append(kv.Key).Append(':').Append(kv.Value);}
            line.Append('\n');
        }
    }
}
