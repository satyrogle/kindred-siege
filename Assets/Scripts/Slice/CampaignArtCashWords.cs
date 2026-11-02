using System.Collections.Generic;
using KindredSiege.Sim.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        sealed class CashWord
        {
            public RawImage image;public float at;public Vector2 position;
        }
        sealed class MarkShot
        {
            public Image image;public float at;public Vector2 from,to;
        }
        readonly Dictionary<string,CashWord> _cashWords=new Dictionary<string,CashWord>();
        readonly List<MarkShot> _markShots=new List<MarkShot>();
        RectTransform _cashScreen;
        static string CashWordFor(BattleEvent e)
        {
            if(e.Kind==BattleEventKind.Cash)
                return e.Detail==ClassSpells.PinDetail?"PINNED":e.Detail==ClassSpells.OpeningMark?"OPENED":
                    e.Detail==ClassSpells.RipDetail?"RIPPED":e.Detail==MarkRules.WideOpenDetail?"EXPOSED":"";
            return e.Kind==BattleEventKind.Burst?(e.Detail==MarkRules.Crush?"CRUSHED":e.Detail==MarkRules.Stasis?"FROZEN":""):"";
        }
        static Color32 CashColour(string word)=>word=="PINNED"?PxMint:word=="OPENED"?PxOrange:word=="RIPPED"?PxRed:
            word=="CRUSHED"?PxLilac:word=="EXPOSED"?PxBlue:PxYellow;
        void CashWordEvent(BattleEvent e,Figure actor,Figure target)
        {
            if(_seeking)return;
            if(e.Kind==BattleEventKind.Whisper)target=actor;
            if(target==null)return;
            if(_cashScreen!=_screen){_cashScreen=_screen;_cashWords.Clear();_markShots.Clear();}
            string word=CashWordFor(e);
            if(e.Kind==BattleEventKind.Whisper)word=e.Detail==WhisperRules.InHandDetail?"READY":e.Detail==WhisperRules.StoppedDetail?"STOPPED":
                e.Detail==WhisperRules.AmbushDetail?"AMBUSH":e.Detail==WhisperRules.CounterDetail?"COUNTER":"FREE ANSWER";
            if(word.Length>0)
            {
                if(!_cashWords.TryGetValue(target.unit.Id,out var pop))
                {_cashWords[target.unit.Id]=pop=new CashWord{image=Raw(_readoutLayer,"Mark payoff",0,0,1,1)};pop.image.raycastTarget=false;}
                pop.at=_visualTime;pop.position=new Vector2(target.feet.x,Mathf.Max(380,target.feet.y-target.head*.45f)-80);
                var old=pop.image.texture as Texture2D;
                if(old!=null&&_screenTextures.Remove(old))Destroy(old);
                Texture2D texture;
                if(e.Kind==BattleEventKind.Whisper)
                {texture=_logoFont.Render(word,PxMint,PxMint,1,true);_screenTextures.Add(texture);}
                else texture=Resources.Load<Texture2D>("CampaignArt/UI/Marks/cash_"+word.ToLowerInvariant());
                if(texture==null)throw new System.InvalidOperationException("Approved cash-in word missing: "+word);
                pop.image.texture=texture;
            }
            if(e.Kind==BattleEventKind.Mark&&actor!=null&&
                (e.Detail==ClassSpells.SnareMark||e.Detail==ClassSpells.OpeningMark||e.Detail==ClassSpells.BleedMark))
            {
                if(_markShots.Count>=8)return;
                var shot=new MarkShot{image=Panel(_readoutLayer,0,0,12,4,e.Detail==ClassSpells.SnareMark?PxMint:e.Detail==ClassSpells.OpeningMark?PxOrange:PxRed),
                    at=_visualTime,from=new Vector2(actor.feet.x,actor.feet.y-actor.head*.5f),to=new Vector2(target.feet.x,target.feet.y-target.head*.5f)};
                shot.image.raycastTarget=false;_markShots.Add(shot);
            }
        }
        void DrawCashWords()
        {
            if(_cashScreen!=_screen)return;
            foreach(var pop in _cashWords.Values)
            {
                float age=_visualTime-pop.at;
                pop.image.gameObject.SetActive(age>=0&&age<.75f);
                if(age<0||age>=.75f)continue;
                if(_comboCall!=null)SetPx(_comboCall,"",PxMint);
                float scale=age<.12f?1.22f-.22f*age/.12f:1;
                pop.image.rectTransform.localScale=new Vector3(scale,scale,1);
                float crop=Mathf.Clamp01((.75f-age)/.35f);
                pop.image.uvRect=new UnityEngine.Rect(0,1-crop,1,crop);
                pop.image.rectTransform.sizeDelta=new Vector2(pop.image.texture.width,pop.image.texture.height*crop);
                pop.image.rectTransform.anchoredPosition=new Vector2(Mathf.Round(pop.position.x-pop.image.texture.width*.5f),-Mathf.Round(pop.position.y-Mathf.Max(0,age-.4f)*55));
            }
            for(int i=_markShots.Count-1;i>=0;i--)
            {
                var shot=_markShots[i];float progress=(_visualTime-shot.at)/.28f;
                if(progress>=1){Destroy(shot.image.gameObject);_markShots.RemoveAt(i);continue;}
                var position=Vector2.Lerp(shot.from,shot.to,Mathf.Clamp01(progress));
                shot.image.rectTransform.anchoredPosition=new Vector2(Mathf.Round(position.x),-Mathf.Round(position.y));
            }
        }
        bool CashHitAt(Figure figure,long eventTick,string actorId)
        {
            var events=ViewSim.Events;
            for(int i=events.Count-1;i>=0&&events[i].Tick>=eventTick;i--)
                if(events[i].Tick==eventTick&&events[i].ActorId==actorId&&events[i].TargetId==figure.unit.Id&&CashWordFor(events[i]).Length>0)return true;
            return false;
        }
    }
}
