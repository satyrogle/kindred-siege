using System;
using System.Collections.Generic;
using KindredSiege.Sim.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        [Serializable] sealed class ReviewFxIndex {public string[] keys;}
        System.Collections.IEnumerator PreloadReviewFx()
        {
            var index=JsonUtility.FromJson<ReviewFxIndex>(Resources.Load<TextAsset>("Table42Review/fx-index").text);
            string a="Chief/"+ChiefFolder(ViewSim.Spell(0))+"/",b="Chief/"+ChiefFolder(ViewSim.Spell(1))+"/";
            foreach(var key in index.keys)if(key.StartsWith("Effects/")||key.StartsWith(a)||key.StartsWith(b))
            {bool cached=_clips.ContainsKey("Table42Review/"+key);LoadClip("Table42Review/"+key);if(!cached)yield return null;}
        }
        [Serializable] sealed class FxCue {public int frame,victimDx,victimDy;public string victim;public int[] shake,victimTint;}
        sealed class ReviewFx
        {
            public Clip clip;public RawImage image;public Figure target;public Vector2 point,to;
            public float start,end,phase;public bool mirror,hold,loop,moving,cues;public bool shake=true;public float shakeScale=1;public FxCue[] cueOverride;public int fixedFrame=-1,lastCue=-1;
        }
        sealed class ChiefCast
        {
            public ChiefSpell spell;public Figure caster;public int side;public long releaseTick=-1;
            public float start,release=-1;public bool answered,wall;
            public Vector2 centre,sun;public readonly List<Figure> targets=new List<Figure>();
            public readonly List<ReviewFx> tells=new List<ReviewFx>();
            public readonly Dictionary<string,float> contacts=new Dictionary<string,float>();
        }
        sealed class ChiefReceipt {public BattleEvent value;public float at;}
        readonly List<ReviewFx> _reviewFx=new List<ReviewFx>();
        readonly List<ChiefReceipt> _chiefReceipts=new List<ChiefReceipt>();
        readonly ChiefCast[] _chiefCasts=new ChiefCast[2];
        bool _deliveringChief;
        int _chiefFxImpacts;
        void ClearChiefEffects(){_reviewFx.Clear();_chiefReceipts.Clear();_chiefCasts[0]=_chiefCasts[1]=null;_deliveringChief=false;_chiefFxImpacts=0;}
        static string ChiefFolder(ChiefSpell spell)=>spell==ChiefSpell.BloodCall?"blood-call":spell==ChiefSpell.DustShark?"dust-shark":
            spell==ChiefSpell.Firebomb?"firebomb":spell==ChiefSpell.SteelStorm?"steel-storm":spell==ChiefSpell.Tide?"tide":spell==ChiefSpell.WarCry?"war-cry":"eclipse";
        ReviewFx SpawnReviewFx(string key,Figure f,float delay,bool mirror=false,Vector2? point=null,bool hold=false,bool loop=false,float duration=-1,bool cues=false)
        {
            var clip=LoadClip("Table42Review/"+key);
            var image=Raw(_stage,"Review FX "+key,0,0,1,1);image.raycastTarget=false;image.texture=clip.texture;
            if(clip.sheet.layer=="under"&&_figures.Count>0)
                image.transform.SetSiblingIndex(_figures[0].root.GetSiblingIndex());
            else if(f!=null&&clip.sheet.layer!="over")
                image.transform.SetSiblingIndex(f.root.GetSiblingIndex()+(clip.sheet.layer=="behind"?0:1));
            else image.transform.SetSiblingIndex(_readoutLayer.GetSiblingIndex());
            var fx=new ReviewFx{clip=clip,image=image,target=point.HasValue?null:f,point=point??(f==null?Vector2.zero:f.feet),
                start=_visualTime+delay,end=duration<0?clip.seconds:duration,mirror=key.StartsWith("Effects/")?f!=null&&f.unit.Side==1:mirror,hold=hold,loop=loop,cues=cues};
            _reviewFx.Add(fx);return fx;
        }
        ReviewFx ChiefFx(ChiefCast cast,string name,Figure f,float delay=0,bool hold=false,bool loop=false,float duration=-1,Vector2? point=null,bool cues=false)
            =>SpawnReviewFx("Chief/"+ChiefFolder(cast.spell)+"/"+name,f,delay,cast.side==0,point,hold,loop,duration,cues);
        void StopTells(ChiefCast c)
        {foreach(var fx in c.tells){fx.hold=false;fx.end=Mathf.Max(0,_visualTime-fx.start);}c.tells.Clear();}
        void Tell(ChiefCast c,string name,Figure f,Vector2? point=null,bool loop=false)
        {c.tells.Add(ChiefFx(c,name,f,0,true,loop,-1,point));}
        void KeepWaitingTell(ReviewFx[] tells,Figure target,float delay)
        {
            if(delay<=0)return;
            foreach(var fx in tells)if(fx.target==target&&!fx.loop)
            {fx.end=Mathf.Max(0,_visualTime-fx.start)+delay;fx.fixedFrame=fx.clip.sheet.frames.Length-1;}
        }
        Vector2 CentreOf(List<Figure> figures,Figure fallback)
        {Vector2 p=Vector2.zero;foreach(var f in figures)p+=f.feet;return figures.Count>0?p/figures.Count:fallback.feet;}
        void BeginChiefFx(BattleEvent e)
        {
            int side=e.ActorSide;var caster=FindFigure(e.ActorId);if(caster==null)return;
            if(_chiefCasts[side]!=null)StopTells(_chiefCasts[side]);
            var c=new ChiefCast{spell=ViewSim.Spell(side),side=side,caster=caster,start=_visualTime};_chiefCasts[side]=c;
            bool own=c.spell==ChiefSpell.SteelStorm||c.spell==ChiefSpell.WarCry;
            // Authored warning placement only. Confirmed victims come exclusively from Hit events below.
            Row row=c.spell==ChiefSpell.DustShark||c.spell==ChiefSpell.Tide?Row.Back:Row.Front;
            foreach(var f in _figures)if(f.unit.Alive&&!f.unit.Benched&&f.unit.Side==(own?side:1-side)&&
                (own||c.spell==ChiefSpell.BloodCall?own||f.unit.BleedTicks>0:f.unit.Row==row))c.targets.Add(f);
            c.targets.Sort((a,b)=>a.feet.y.CompareTo(b.feet.y));c.centre=CentreOf(c.targets,caster);
            c.sun=c.centre+new Vector2(side==0?-180:180,-380);
            string stem=ChiefFolder(c.spell).Replace('-','_');
            foreach(var f in c.targets)
            {
                if(c.spell==ChiefSpell.SteelStorm)Tell(c,stem+"_tell_under_"+(f.unit.IsLeader?"m":"w"),f);
                else if(c.spell==ChiefSpell.WarCry)Tell(c,"war_cry_hunter_tell_under",f);
                else
                {
                    Tell(c,stem+"_tell_under",f);
                    if(c.spell==ChiefSpell.DustShark)
                    {
                        Tell(c,"dust_shark_tell_front",f);
                        foreach(string fin in new[]{"dust_shark_fin_behind","dust_shark_fin_front"})
                        {var fx=ChiefFx(c,fin,f,.32f,true,true);fx.phase=(4+6*c.targets.IndexOf(f))*.08f;c.tells.Add(fx);}
                    }
                    if(c.spell==ChiefSpell.BloodCall)Tell(c,"blood_call_wound",f,new Vector2(f.feet.x+(side==0?-4:4),f.feet.y-f.head*.5f));
                }
            }
            if(c.spell==ChiefSpell.Eclipse){Tell(c,"eclipse_sun_tell_over",null,c.sun);if(!caster.unit.Id.EndsWith(".shaman"))Tell(c,"eclipse_staff_tell_front",caster);}
            if(c.spell==ChiefSpell.Firebomb)Tell(c,"firebomb_sky_tell",null,c.centre+new Vector2(side==0?-2:2,0));
            if(c.spell==ChiefSpell.BloodCall)Tell(c,"blood_call_bowl_tell",caster);
            if(c.spell==ChiefSpell.Tide){Tell(c,"tide_caster_tell_behind",caster);Tell(c,"tide_caster_tell_front",caster);}
            if(c.spell==ChiefSpell.WarCry){Tell(c,"war_cry_tell_under",caster);Tell(c,"war_cry_tell_over",caster);}
        }
        void ChiefFxEvent(BattleEvent e)
        {
            if(!TablePresentation||_seeking||e.ActorSide<0||e.ActorSide>1)return;
            int side=e.ActorSide;
            if(e.Kind==BattleEventKind.Interrupt){var interrupted=_chiefCasts[1-side];if(interrupted!=null&&interrupted.caster.unit.Id==e.TargetId&&interrupted.release<0)ReleaseChiefFx(interrupted,e,true);return;}
            if(e.Kind==BattleEventKind.Windup&&(e.Detail==ChiefSpells.Name(ViewSim.Spell(side))||e.Detail==Moves.Pyre))
            {BeginChiefFx(e);return;}
            var c=_chiefCasts[side];
            if(e.Kind==BattleEventKind.Hit&&!ChiefDamage(e))
            {var victim=FindFigure(e.TargetId);if(victim!=null){var shield=_chiefCasts[victim.unit.Side];if(shield!=null&&shield.wall&&ViewSim.WallTicks(victim.unit.Side)>0)ChiefFx(shield,"steel_storm_struck_w",victim,cues:true);}}
            if(e.Kind==BattleEventKind.Wall)
            {
                if(e.Detail==ChiefSpells.WallUp||e.Detail==ChiefKits.WallHalf)
                {
                    if(c==null){BeginChiefFx(new BattleEvent(e.Tick,BattleEventKind.Windup,side,e.ActorId,e.ActorClass,"",-1,0,"Steel Storm"));c=_chiefCasts[side];}
                    if(c!=null){var waiting=c.tells.ToArray();StopTells(c);c.release=_visualTime;c.releaseTick=e.Tick;c.wall=true;RaiseChiefDomes(c,waiting);}
                }
                else if(c!=null&&c.wall)EndChiefDomes(c,e.Detail==ChiefSpells.WallSmashed);
                return;
            }
            if(c==null)return;
            if(e.Kind==BattleEventKind.Interrupt&&c.release<0){ReleaseChiefFx(c,e,true);return;}
            if(e.Kind==BattleEventKind.Release&&e.ActorId==c.caster.unit.Id&&c.release<0&&string.IsNullOrEmpty(e.TargetId))
                ReleaseChiefFx(c,e,e.Detail==Moves.Dodged||e.Detail==FightRules.Parried||e.Detail==PitRules.Lost);
        }
        void ReleaseChiefFx(ChiefCast c,BattleEvent e,bool answered)
        {
            var waiting=c.tells.ToArray();StopTells(c);c.release=_visualTime;c.releaseTick=e.Tick;c.answered=answered;
            if(answered)
            {
                if(c.spell==ChiefSpell.Eclipse){foreach(var f in c.targets)ChiefFx(c,"eclipse_break_under",f);ChiefFx(c,"eclipse_sun_out_over",null,point:c.sun);}
                if(c.spell==ChiefSpell.WarCry)
                {
                    var broken=ChiefFx(c,"war_cry_broken_under",c.caster,cues:true);
                    broken.cueOverride=new[]{new FxCue{frame=0,victim="white",victimDx=2,shake=new[]{3,-1}},new FxCue{frame=1,victim="red",victimDx=3},new FxCue{frame=2,victimDx=-1},new FxCue{frame=3,victim="red"},new FxCue{frame=4,victimDx=-1},new FxCue{frame=5,victimDx=1},new FxCue{frame=6,victimDx=-1}};
                    ChiefFx(c,"war_cry_broken_over",c.caster);
                }
                return;
            }
            if(c.spell==ChiefSpell.WarCry)
            {
                var cry=ChiefFx(c,"war_cry_hit_under",c.caster,cues:true);
                cry.cueOverride=new[]{new FxCue{frame=1,victim="white",victimDx=3,shake=new[]{4,-2}},new FxCue{frame=2,victimDx=1,shake=new[]{-4,2}},new FxCue{frame=3,shake=new[]{2,-1}}};
                ChiefFx(c,"war_cry_hit_over",c.caster);ChiefFx(c,"war_cry_air_rings",c.caster);
                for(int part=1;part<=4;part++)ChiefFx(c,"war_cry_shockwave_"+part,c.caster,(part-1)*6*.08f);
                foreach(var f in c.targets)
                {
                    var gap=f.feet-c.caster.feet;gap.y/=.7f;float dist=gap.magnitude;int[] radii={24,70,140,220,305,390,470,545};int n=0;while(n<7&&dist>radii[n])n++;
                    var aura=ChiefFx(c,"war_cry_aura",f,(n+1)*.08f,cues:true);
                    aura.cueOverride=new[]{new FxCue{frame=0,victim="white"},new FxCue{frame=1,victimTint=new[]{230,179,255},victimDy=-3},new FxCue{frame=2,victimDy=-1}};
                }
                return;
            }
            // The event list already contains this complete tick, including guard redirection and deaths.
            c.targets.Clear();
            foreach(var hit in ViewSim.Events)if(hit.Tick==e.Tick&&hit.ActorSide==c.side&&ChiefDamage(hit))
            {var f=FindFigure(hit.TargetId);if(f!=null&&!c.targets.Contains(f))c.targets.Add(f);}
            c.targets.Sort((a,b)=>a.feet.y.CompareTo(b.feet.y));
            if(c.spell==ChiefSpell.Eclipse){ReleaseEclipseFx(c);return;}
            string stem=ChiefFolder(c.spell).Replace('-','_');
            if(c.spell==ChiefSpell.Firebomb)
            {
                ChiefFx(c,"firebomb_sky_hit",null,point:c.centre+new Vector2(c.side==0?-2:2,0));
                ChiefFx(c,"firebomb_land_under",null,point:c.centre,cues:true);ChiefFx(c,"firebomb_land_over",null,point:c.centre);
            }
            if(c.spell==ChiefSpell.BloodCall)ChiefFx(c,"blood_call_bowl_hit",c.caster);
            if(c.spell==ChiefSpell.Tide)ChiefFx(c,"tide_caster_hit_front",c.caster);
            for(int i=0;i<c.targets.Count;i++)
            {
                var f=c.targets[i];float delay=i*(c.spell==ChiefSpell.DustShark?2:1)*.08f;
                KeepWaitingTell(waiting,f,delay);
                int impactFrame=c.spell==ChiefSpell.DustShark?9:c.spell==ChiefSpell.Tide?10:8;
                c.contacts[f.unit.Id]=_visualTime+delay+impactFrame*.08f;
                var impact=ChiefFx(c,stem+"_hit_under",f,delay,cues:true);impact.shake=i==0||c.spell==ChiefSpell.DustShark;
                if(c.spell==ChiefSpell.DustShark){ChiefFx(c,"dust_shark_hit_behind",f,delay);ChiefFx(c,"dust_shark_hit_front",f,delay);}
                if(c.spell==ChiefSpell.Firebomb)ChiefFx(c,"firebomb_pillar",f,delay);
                if(c.spell==ChiefSpell.Tide)ChiefFx(c,"tide_hit_front",f,delay);
                if(c.spell==ChiefSpell.BloodCall){ChiefFx(c,"blood_call_hit_over",f,delay);ChiefFx(c,"blood_call_stream",f,delay,point:new Vector2(f.feet.x+(c.side==0?-4:4),f.feet.y-f.head*.5f));}
            }
        }
        void ReleaseEclipseFx(ChiefCast c)
        {
            ChiefFx(c,"eclipse_sun_dark_over",null,point:c.sun);ChiefFx(c,"eclipse_sun_flash_over",null,.08f,point:c.sun);
            if(!c.caster.unit.Id.EndsWith(".shaman"))ChiefFx(c,"eclipse_staff_hit_front",c.caster);
            float launch=.08f,last=launch;
            for(int i=0;i<c.targets.Count;i++)
            {
                var f=c.targets[i];int fall=Mathf.Clamp(Mathf.RoundToInt((f.feet-c.sun).magnitude/110),4,6);
                float hit=launch+(fall-1)*.08f;last=hit;
                c.contacts[f.unit.Id]=_visualTime+hit;
                ChiefFx(c,"eclipse_dark_under",f);
                ChiefFx(c,"eclipse_wait_under",f,.08f,loop:true,duration:Mathf.Max(.08f,hit-.08f));
                ChiefFx(c,"eclipse_shadow"+fall+"_under",f,launch);
                var comet=ChiefFx(c,"eclipse_comet_over",null,launch,duration:(fall-1)*.08f,point:c.sun);
                comet.moving=true;comet.to=f.feet-new Vector2(0,2);comet.fixedFrame=(f.unit.Row==Row.Back?3:0)+Mathf.Clamp(f.lane,0,2);
                if(i>0)ChiefFx(c,"eclipse_sun_launch_over",null,launch,point:c.sun);
                var burst=ChiefFx(c,"eclipse_hit_behind",f,hit,cues:true);burst.shakeScale=i==0?1:.8f;ChiefFx(c,"eclipse_hit_front",f,hit);
                ChiefFx(c,"eclipse_break_under",f,hit+14*.08f);
                launch=hit;
            }
            if(last>.16f)ChiefFx(c,"eclipse_sun_loop_over",null,.16f,loop:true,duration:last-.08f,point:c.sun);
            ChiefFx(c,"eclipse_sun_out_over",null,last+.08f,point:c.sun);
        }
        void RaiseChiefDomes(ChiefCast c,ReviewFx[] waiting)
        {
            foreach(var f in c.targets)
            {
                string size=f.unit.IsLeader?"m":"w";float distance=float.MaxValue;
                foreach(var w in c.targets)if(w.unit.Class==UnitClass.Warden){var d=f.feet-w.feet;d.y/=.36f;distance=Mathf.Min(distance,d.magnitude);}
                float delay=distance==float.MaxValue?0:Mathf.Ceil(distance/70)*.08f;
                KeepWaitingTell(waiting,f,delay);
                ChiefFx(c,"steel_storm_dark_under_"+size,f,delay);
                c.tells.Add(ChiefFx(c,"steel_storm_glass_under_"+size,f,delay+.08f,hold:true));
                ChiefFx(c,"steel_storm_rise_"+size,f,delay+.08f);
                c.tells.Add(ChiefFx(c,"steel_storm_hold_"+size,f,delay+.72f,hold:true,loop:true));
                if(f.unit.Class==UnitClass.Warden)
                {
                    var ring=ChiefFx(c,"steel_storm_cast_ring",f,cues:true);
                    ring.cueOverride=new[]{new FxCue{frame=1,victim="white",shake=new[]{3,-1}}};
                }
            }
        }
        void EndChiefDomes(ChiefCast c,bool smashed)
        {
            c.wall=false;var standing=c.tells.ToArray();StopTells(c);
            // The glass floor survives the first nine smash frames.
            foreach(var fx in standing)if(fx.clip.texture.name.StartsWith("steel_storm_glass_under"))
                fx.end=Mathf.Max(0,_visualTime-fx.start)+9*.08f;
            foreach(var f in c.targets)
            {
                var smash=ChiefFx(c,"steel_storm_smash_"+(f.unit.IsLeader?"m":"w"),f,cues:true);
                smash.cueOverride=new[]{new FxCue{frame=1,victimDx=3,shake=new[]{6,-3}},new FxCue{frame=2,victimDx=2,shake=new[]{-6,3}},new FxCue{frame=3,victimDx=1,shake=new[]{3,-1}}};
            }
            if(smashed)ChiefFx(c,"steel_storm_smash_ring",null,point:CentreOf(c.targets,c.caster));
        }
        bool DeferChiefReceipt(BattleEvent e)
        {
            if(!TablePresentation||_seeking||_deliveringChief)return false;
            if(ChiefDamage(e)&&e.ActorSide>=0&&e.ActorSide<2)
            {
                var c=_chiefCasts[e.ActorSide];
                if(c!=null&&c.releaseTick==e.Tick&&c.contacts.TryGetValue(e.TargetId,out float at))
                {_chiefReceipts.Add(new ChiefReceipt{value=e,at=at});return true;}
            }
            if(e.Kind==BattleEventKind.Kill)
                foreach(var receipt in _chiefReceipts)if(receipt.value.TargetId==e.TargetId&&receipt.value.Tick==e.Tick)
                {_chiefReceipts.Add(new ChiefReceipt{value=e,at=receipt.at});return true;}
            return false;
        }
        bool PendingChiefImpact(Figure f)
        {foreach(var receipt in _chiefReceipts)if(receipt.value.TargetId==f.unit.Id)return true;return false;}
        void RestoreChiefEffectsAfterSeek()
        {
            if(!TablePresentation||ViewOver)return;
            for(int side=0;side<2;side++)
            {
                bool winding=ViewSim.WindupTicks(side)>0,wall=ViewSim.WallTicks(side)>0;
                if(!winding&&!wall)continue;
                var events=ViewSim.Events;
                for(int i=events.Count-1;i>=0;i--)
                {
                    var e=events[i];if(e.ActorSide!=side||e.Kind!=BattleEventKind.Windup||e.Detail!=ChiefSpells.Name(ViewSim.Spell(side)))continue;
                    BeginChiefFx(e);var c=_chiefCasts[side];if(c==null)break;
                    if(winding)
                    {
                        float elapsed=(ViewSim.CurrentTick-e.Tick)/30f;c.start-=elapsed;
                        foreach(var fx in c.tells)fx.start-=elapsed;
                    }
                    else
                    {
                        StopTells(c);c.wall=true;c.release=_visualTime;
                        foreach(var f in c.targets)
                        {
                            string size=f.unit.IsLeader?"m":"w";
                            c.tells.Add(ChiefFx(c,"steel_storm_glass_under_"+size,f,hold:true));
                            c.tells.Add(ChiefFx(c,"steel_storm_hold_"+size,f,hold:true,loop:true));
                        }
                    }
                    break;
                }
            }
        }
        void DeliverChiefReceipts()
        {
            for(int i=0;i<_chiefReceipts.Count;)
            {
                var r=_chiefReceipts[i];if(_visualTime<r.at){i++;continue;}
                _chiefReceipts.RemoveAt(i);_deliveringChief=true;
                try{AnimateEvent(r.value);if(r.value.Kind==BattleEventKind.Hit)_chiefFxImpacts++;}finally{_deliveringChief=false;}
            }
        }
        void DrawReviewFx()
        {
            if(!TablePresentation)return;
            // Authored domes replace the old shield-column placeholder for this cast.
            for(int side=0;side<2;side++)if(_chiefCasts[side]!=null&&_chiefCasts[side].wall&&_wallPanels[side]!=null)
                _wallPanels[side].gameObject.SetActive(false);
            // Not while his Release or death still waits its turn as a blow (CampaignArtBeats.cs): the sim is a tick ahead then.
            foreach(var cast in _chiefCasts)if(cast!=null&&cast.release<0&&(!cast.caster.unit.Alive||ViewSim.WindupTicks(cast.side)==0)&&!BeatPendingFor(cast.caster))
            {StopTells(cast);cast.release=_visualTime;cast.answered=true;}
            for(int i=_reviewFx.Count-1;i>=0;i--)
            {
                var fx=_reviewFx[i];float age=_visualTime-fx.start;
                if(!fx.hold&&age>=fx.end){Destroy(fx.image.gameObject);_reviewFx.RemoveAt(i);continue;}
                fx.image.enabled=age>=0;if(age<0)continue;
                if(fx.clip.texture.name.StartsWith("steel_storm_hold"))
                    foreach(var other in _reviewFx)if(other.target==fx.target&&other.clip.texture.name=="steel_storm_struck_w"&&_visualTime>=other.start&&_visualTime-other.start<other.end)
                        fx.image.enabled=false;
                int frame=fx.fixedFrame>=0?fx.fixedFrame:fx.clip.FrameAt(age+fx.phase,fx.loop);
                if(fx.hold&&!fx.loop&&fx.clip.sheet.frames.Length>8&&age>=fx.clip.seconds)frame=fx.clip.sheet.frames.Length-8+(int)((age-fx.clip.seconds)/.08f)%8;
                frame=Mathf.Clamp(frame,0,fx.clip.sheet.frames.Length-1);var rect=fx.clip.sheet.frames[frame].frame;
                float tw=fx.clip.texture.width,th=fx.clip.texture.height;
                fx.image.uvRect=new UnityEngine.Rect((rect.x+(fx.mirror?rect.w:0))/tw,1-(rect.y+rect.h)/th,(fx.mirror?-rect.w:rect.w)/tw,rect.h/th);
                var p=fx.target==null?fx.point:new Vector2(fx.target.root.anchoredPosition.x,-fx.target.root.anchoredPosition.y);
                if(fx.moving)p=Vector2.Lerp(fx.point,fx.to,Mathf.Clamp01((age+.08f)/(fx.end+.08f)));
                float ax=fx.mirror?rect.w-fx.clip.sheet.anchorX:fx.clip.sheet.anchorX;
                fx.image.rectTransform.anchoredPosition=new Vector2(Mathf.Round(p.x-ax),-Mathf.Round(p.y-fx.clip.sheet.anchorY));
                fx.image.rectTransform.sizeDelta=new Vector2(rect.w,rect.h);
                var cues=fx.cueOverride??fx.clip.sheet.cues;
                if(fx.cues&&cues!=null)
                    foreach(var cue in cues)if(cue.frame==frame)
                    {
                        if(fx.target!=null)
                        {
                            fx.target.root.anchoredPosition+=new Vector2(cue.victimDx*(fx.mirror?-1:1),-cue.victimDy);
                            if(!_paused&&fx.target.hitMaterial!=null&&cue.victim=="white")fx.target.hitMaterial.SetFloat(FlatWhite,1);
                            if(!_paused&&fx.target.body!=null&&cue.victim=="red")fx.target.body.color=new Color(1,.4f,.35f);
                            if(!_paused&&fx.target.body!=null&&cue.victimTint!=null)fx.target.body.color=new Color(cue.victimTint[0]/255f,cue.victimTint[1]/255f,cue.victimTint[2]/255f);
                        }
                        if(!_paused&&fx.shake&&cue.shake!=null&&cue.shake.Length==2)_stage.anchoredPosition=new Vector2(Mathf.Round(cue.shake[0]*fx.shakeScale),-cue.shake[1]);
                    }
                if(fx.target?.hitMaterial!=null&&!_paused&&fx.clip.texture.name=="blood_call_hit_under"&&(frame==0||frame==4||frame==6||frame==8))
                {
                    fx.target.hitMaterial.SetFloat(CounterRim,1);fx.target.hitMaterial.SetFloat("_CounterWidth",2);
                    fx.target.hitMaterial.SetColor(CounterColor,frame==0?new Color32(68,15,21,255):new Color32(209,27,27,255));
                }
            }
            DrawChiefWarningRims();
        }
        void DrawChiefWarningRims()
        {
            if(_paused)return;
            foreach(var c in _chiefCasts)
            {
                if(c==null||c.release>=0)continue;
                int frame=Mathf.Min(37,Mathf.FloorToInt((_visualTime-c.start)/.08f));
                if(c.spell==ChiefSpell.WarCry&&frame>=36)c.caster.root.anchoredPosition+=Vector2.up;
                bool rim=false;Color colour=PxBone;
                if(c.spell==ChiefSpell.Eclipse){rim=frame>=26&&frame<=36&&frame%2==0;colour=new Color32(229,95,35,255);}
                if(c.spell==ChiefSpell.SteelStorm){rim=frame>=26&&frame%2==0;colour=PxMint;}
                if(c.spell==ChiefSpell.BloodCall){rim=frame<26?frame%8<4:frame%2==0;colour=new Color32(209,27,27,255);}
                if(!rim)continue;
                foreach(var f in c.targets)if(f.hitMaterial!=null&&(c.spell!=ChiefSpell.SteelStorm||f.unit.Class==UnitClass.Warden))
                {f.hitMaterial.SetFloat(CounterRim,1);f.hitMaterial.SetFloat("_CounterWidth",2);f.hitMaterial.SetColor(CounterColor,colour);}
            }
        }
        bool ChiefCasterPose(Figure f,out Clip c,out float time)
        {
            c=null;time=0;if(!TablePresentation||f.artKey!="hunter_captain_sw")return false;
            var cast=_chiefCasts[f.unit.Side];if(cast==null||cast.caster!=f||cast.spell!=ChiefSpell.Firebomb||cast.answered)return false;
            int frame=cast.release<0?Mathf.FloorToInt((_visualTime-cast.start)/.08f):38+Mathf.FloorToInt((_visualTime-cast.release)/.08f);
            if(frame<8||frame>47)return false;
            int attackFrame=frame==8?4:frame==9?5:frame<=43?6:frame<=45?36:38;
            c=f.attack;time=PitFrameTime(c,Mathf.Min(attackFrame,c.sheet.frames.Length-1));return true;
        }
    }
}
