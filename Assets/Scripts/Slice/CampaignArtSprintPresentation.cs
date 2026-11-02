using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        float _majorUntil=-100, _majorLast=-100;
        string _majorActor="",_majorTarget="";
        int _majorSide=-1;
        string _parriedGuard="";

        void MajorMoment(BattleEvent e, Figure actor, Figure target)
        {
            if (!_combatFeedback || _seeking) return;
            if(e.Kind==BattleEventKind.Release&&e.Detail==FightRules.Parried)_parriedGuard=e.TargetId;
            bool kill=e.Kind==BattleEventKind.Kill;
            bool parry=e.Kind==BattleEventKind.Parry;
            bool riposte=e.Kind==BattleEventKind.Combo && e.Detail==FightRules.Riposte;
            bool classFinish=e.Kind==BattleEventKind.Cash ||
                e.Kind==BattleEventKind.Hit && ClassSpells.TierOf(e.Detail)==HitTier.Finisher;
            bool phase=e.Kind==BattleEventKind.Phase;
            bool eclipse=e.Kind==BattleEventKind.Release && string.IsNullOrEmpty(e.TargetId) &&
                CastName(e)=="Eclipse" && (e.Detail==Moves.Full || e.Detail==Moves.Soaked);
            if (!kill&&!parry&&!riposte&&!eclipse&&!classFinish&&!phase) return;
            if(parry&&actor==null)actor=FindFigure(_parriedGuard);
            if(parry&&actor==null&&ViewSim.TightParryOn&&target!=null)
                foreach(var threat in _threatReads)
                    if(threat.ActorId==target.unit.Id){actor=FindFigure(threat.TargetId);break;}
            // A multi-kill or row hit is one moment, not a chain of screen flashes.
            if (_visualTime-_majorLast<.45f&&!parry) return;
            float hold=kill?.33f:classFinish?.2f:.15f;
            _majorLast=_visualTime;_majorUntil=_visualTime+hold;
            _majorActor=actor==null?e.ActorId:actor.unit.Id;_majorTarget=e.TargetId;
            _majorSide=eclipse?1-e.ActorSide:parry&&actor==null&&!ViewSim.TightParryOn?e.ActorSide:-1;
            foreach(var f in _figures)
                if(f==actor||f==target||f.unit.Side==_majorSide)
                {
                    f.poseHold=Mathf.Max(f.poseHold,hold);f.majorFreeze=true;
                    f.majorWhiteUntil=parry&&ViewSim.TightParryOn?(f==target?_visualTime+1f/12:-100):_visualTime+.065f;
                }
        }

        Color MomentTint(Figure f,Color tint)
        {
            if(_paused||!_combatFeedback||_visualTime>=_majorUntil)return tint;
            return f.unit.Id==_majorActor||f.unit.Id==_majorTarget||f.unit.Side==_majorSide
                ?tint:new Color(tint.r*.48f,tint.g*.48f,tint.b*.48f,tint.a);
        }

        static string PlainHabits(TribeMind mind,bool squadFirst=false)
        {
            var lines=new List<string>();
            if(mind.HasHabit(Habit.CoverFocused))lines.Add("They protect whoever you focus.");
            if(mind.HasHabit(Habit.HoldOnUnleash))lines.Add("They raise their guard when you Unleash.");
            if(mind.HasHabit(Habit.PressOnFallBack))lines.Add("They press forward when you Fall Back.");
            if(mind.HasHabit(Habit.AvoidGuarded))lines.Add("They seek targets outside your guard.");
            if(mind.HasHabit(Habit.TrapCaution))lines.Add("They approach traps cautiously.");
            if(mind.HasHabit(Habit.WaitOutParry))lines.Add(mind.WaitOutMove.Length>0?"They wait out Hold on "+mind.WaitOutMove+".":squadFirst?"Old wait-out lesson: no active move.":"They delay a blow to catch an early Hold.");
            if(mind.HasHabit(Habit.Feint))lines.Add(mind.FeintMove.Length>0?"They fake "+mind.FeintMove+".":squadFirst?"Old feint lesson: no active move.":"They may fake a beat.");
            return lines.Count==0?"No learned counters yet.":string.Join("\n",lines);
        }

        void SquadPortrait(Transform parent,CampaignFighter fighter)
        {
            string prefix=fighter.TribeId==CampaignTribes.ClarthId?"clarth":fighter.TribeId==CampaignTribes.BloodridgeId?"blood_rot":fighter.TribeId==CampaignTribes.SkullpeakId?"skull":
                fighter.TribeId==CampaignTribes.StoneveilId?"stoneveil":fighter.TribeId==CampaignTribes.ThundercoastId?"thundercoast":null;
            if(prefix==null)return;
            var clip=LoadClip(prefix+"_"+fighter.Class.ToString().ToLowerInvariant()+"_se_idle");
            var frame=clip.sheet.frames[0].frame;
            const int size=48;
            int x=frame.x+Mathf.Clamp(Mathf.RoundToInt(clip.sheet.anchorX)-size/2,0,frame.w-size);
            int y=frame.y+Mathf.Clamp(frame.h-Mathf.RoundToInt(clip.sheet.anchorY),0,frame.h-size);
            var portrait=Raw(parent,"Fighter portrait",22,4,size,size);
            portrait.texture=clip.texture;
            portrait.uvRect=new UnityEngine.Rect(x/(float)clip.texture.width,1-(y+size)/(float)clip.texture.height,size/(float)clip.texture.width,size/(float)clip.texture.height);
        }

        void BuildCampHome()
        {
            var ground=Resources.Load<Texture2D>("CampaignArt/Camp/camp");
            if(ground==null)throw new InvalidOperationException("Approved camp background missing");
            ground.filterMode=FilterMode.Point;
            Raw(_screen,"Open camp",StageX*Px,StageY*Px,StageW*Px,StageH*Px).texture=ground;
            PxHeader("Camp",PxBone,PxGold,new PxLines());
            SetPx(PxLabel(_screen,16,42),"MATERIALS "+_campaign.Materials+"   LADDER RANK "+_campaign.Company.PlayerRank,PxGold);
            PxButton("ladder","LADDER",468,6,76,26,ShowLadder);
            PxButton("title","MENU",548,6,76,26,ShowTitle);
            PxButton("camp","PALS",628,6,76,26,ShowCamp);
            PxButton("camp-keeps","WHAT YOU KEEP",16,62,150,24,()=>Modal("KEPT AT CAMP",CampKeeps()));
            PxButton("camp-run","NEXT RUN",174,62,116,24,()=>Modal("NEXT RUN",NextRun()));
            PxButton("camp-learned","THEY REMEMBER",298,62,146,24,()=>Modal("THEY REMEMBER",TheyLearned()));
            PxButton("omens",OmenButtonLabel(228),452,62,252,24,ShowOmens);
            for(int i=0;i<PalArtIds.Length;i++)
            {
                var feet=PalFeet[i];var clip=LoadClip("CampLoops/"+PalArtIds[i]+"_camp");
                var body=Raw(_screen,PalNames[i]+" at work",feet.x-96,feet.y-192,192,192);
                body.texture=clip.texture;
                var label=PxLabel(_screen,(int)feet.x/Px,(int)feet.y/Px+3,1);
                SetPx(label,PalNames[i],PxBone);
                StartCoroutine(CampHomeLoop(_screen,body,clip));
            }
            var choices=_campaign.Choosable();
            if(choices.Count>0){var first=_gatesReview?CampaignTribes.Bloodridge:choices[0];PxButton("tribe-"+first.Id,_gatesReview?"BLOODRIDGE ROAD":"START RUN",264,398,192,28,()=>GoAfter(first.Id));}
            PxButton("forge","BUTCHER'S FORGE",16,398,216,28,()=>ShowForge());
            PxButton("sound","SOUND",464,398,140,28,ShowSound);
            PxButton("codex","CODEX",612,398,92,28,ShowCodex);
            _pxNotice=PxLabel(_screen,16,434);
        }

        IEnumerator CampHomeLoop(RectTransform screen,RawImage body,Clip clip)
        {
            float time=0;
            while(_screen==screen && body!=null)
            {
                bool hover=RectTransformUtility.RectangleContainsScreenPoint(body.rectTransform,Input.mousePosition,null);
                time=hover?time+Time.unscaledDeltaTime:0;
                var f=clip.sheet.frames[clip.FrameAt(time,true)].frame;
                body.uvRect=new UnityEngine.Rect(f.x/(float)clip.texture.width,1-(f.y+f.h)/(float)clip.texture.height,f.w/(float)clip.texture.width,f.h/(float)clip.texture.height);
                yield return null;
            }
        }

        void ResultScore(int x,int y,string label,string value,Color32 colour)
        {
            PxImage(_screen,label,Plate(_kitButton,12,224,50),x,y);
            SetPx(PxLabel(_screen,x+14,y+7),label,PxMuted);
            var text=_logoFont.Render(value,colour,colour,1,true);_screenTextures.Add(text);
            PxImage(_screen,label+" value",text,x+14,y+21);
        }

        // Unapproved stage candidates are available only through an explicit review launch.
        Texture2D ReviewStage()
        {
            string folder=Argument("-ksStageReview");
            if(string.IsNullOrEmpty(folder))return null;
            string key=ViewTribe==CampaignTribes.SkullpeakId?"skullpeak":HunterFight(ViewKind)?"hunters":null;
            if(key==null)return null;
            string path=Path.Combine(folder,key,key+".png");
            if(!File.Exists(path))return null;
            var image=new Texture2D(2,2,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
            if(!image.LoadImage(File.ReadAllBytes(path))||image.width!=StageW*Px||image.height!=StageH*Px)
            {Destroy(image);throw new InvalidOperationException("Review stage must match the native stage dimensions: "+path);}
            _screenTextures.Add(image);return image;
        }
    }
}
