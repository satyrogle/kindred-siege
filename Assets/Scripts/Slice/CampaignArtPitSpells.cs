using System.Collections.Generic;
using KindredSiege.Sim.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        sealed class PitSpellRead
        {
            public Image[] guard=new Image[2],net=new Image[6],slashes=new Image[2],trap=new Image[24];public Image heal;
            public PxText word;public string text="";public Color32 colour;
            public float wordAt=-100,healAt=-100,guardAt=-100,dashAt=-100,aimBrokenAt=-100;
            public string patient="",ward="",dashTarget="",aimTarget="";
        }
        sealed class PitWire {public Image first,second;public string origin,target,finisher;public float at;}
        RectTransform _pitSpellScreen;
        readonly Dictionary<string,PitSpellRead> _pitSpellReads=new Dictionary<string,PitSpellRead>();
        readonly List<PitWire> _pitWires=new List<PitWire>();
        BattleEvent _pitComboSetup;
        int _pitGuardSeen,_pitHealSeen,_pitComboSeen,_pitSnareSeen;
        void PreparePitSpells()
        {
            if(_pitSpellScreen==_screen)return;
            _pitSpellScreen=_screen;_pitSpellReads.Clear();_pitWires.Clear();_pitComboSetup=default;
            _pitGuardSeen=_pitHealSeen=_pitComboSeen=_pitSnareSeen=0;
        }
        PitSpellRead PitRead(Figure f)
        {
            PreparePitSpells();
            if(!_pitSpellReads.TryGetValue(f.unit.Id,out var read))
            {
                read=new PitSpellRead{word=PxLabel(_readoutLayer,0,0,1)};_pitSpellReads[f.unit.Id]=read;
            }
            return read;
        }
        void PitWord(Figure f,string word,Color32 colour)
        {var read=PitRead(f);read.text=word;read.colour=colour;read.wordAt=_visualTime;}
        void PitSpellEvent(BattleEvent e,Figure actor,Figure target)
        {
            PreparePitSpells();
            if(e.Kind==BattleEventKind.Guard&&actor!=null&&target!=null)
            {
                var read=PitRead(actor);read.guardAt=_visualTime;read.ward=target.unit.Id;_pitGuardSeen++;
                // One blow at a time: the guarded fighter's number already says GUARDED (CampaignArtBeats.cs).
                if(!BeatsOn)PitWord(actor,"GUARDED",PxMint);
                if(!TablePresentation)actor.majorWhiteUntil=_visualTime+1f/60;
            }
            if(e.Kind==BattleEventKind.Heal&&e.Amount>0&&actor!=null&&target!=null)
            {var read=PitRead(actor);read.healAt=_visualTime;read.patient=target.unit.Id;_pitHealSeen++;}
            if(e.Kind==BattleEventKind.Hit&&actor!=null&&target!=null)
            {
                if(e.Detail==ClassSpells.StrikeDetail||e.Detail==ClassSpells.StrikeOpenDetail)
                {var read=PitRead(actor);read.dashAt=_visualTime;read.dashTarget=target.unit.Id;}
                if(target.unit.Class==UnitClass.Marksman)
                {var read=PitRead(target);read.aimBrokenAt=_visualTime;}
            }
            if(e.Kind==BattleEventKind.Mark&&target!=null)
            {
                // One blow at a time shows marks with its own status words (CampaignArtBeats.cs), so these stay quiet there.
                bool quiet=BeatsOn;
                if(e.Detail==ClassSpells.SnareMark){if(!quiet)PitWord(target,"SNARED",PxLilac);_pitSnareSeen++;}
                else if(e.Detail==ClassSpells.BleedMark){if(!quiet)PitWord(target,"BLEED",PxRed);}
                else if(e.Detail==ClassSpells.OpeningMark){if(!quiet)PitWord(target,"OPEN",PxOrange);}
            }
            if(e.Kind==BattleEventKind.TrapSprung&&target!=null)PitWord(target,"SNARED",PxLilac);
            if(e.Kind==BattleEventKind.ComboSetup)_pitComboSetup=e;
            if(e.Kind==BattleEventKind.ComboFinish&&actor!=null&&target!=null&&
                _pitComboSetup.Kind==BattleEventKind.ComboSetup&&_pitComboSetup.Tick==e.Tick&&
                _pitComboSetup.TargetId==e.TargetId&&_pitComboSetup.Amount==e.Amount&&_pitWires.Count<12)
            {
                // Placeholder wires stay hidden while one blow at a time plays (CampaignArtBeats.cs, fix 3).
                if(!BeatsQuiet)
                    _pitWires.Add(new PitWire{origin=_pitComboSetup.ActorId,target=e.TargetId,finisher=e.ActorId,at=_visualTime,
                        first=PitLine("Combo setter to mark",_readoutLayer),second=PitLine("Combo mark to finisher",_readoutLayer)});
                _pitComboSeen++;
            }
        }
        void DrawPitSpells()
        {
            PreparePitSpells();
            foreach(var f in _figures)
            {
                if(StageBystander(f))continue;
                var u=f.unit;var read=PitRead(f);bool live=u.Alive&&!ViewOver&&!ChiefCutInVisible;
                var trap=ViewSim.ReadTrap(u.Side);
                // The placeholder pieces (ART PENDING) stay hidden while one blow at a time plays, until their art exists;
                // the Warden's tether shows only while he steps in for the blow (CampaignArtBeats.cs, fix 3).
                bool quiet=BeatsQuiet;
                bool armed=trap.Armed&&trap.OwnerId==u.Id&&!ViewOver&&!quiet;
                for(int t=0;t<read.trap.Length;t++)
                {
                    if(armed&&read.trap[t]==null)read.trap[t]=PitLine("Armed trap ring",_readoutLayer);
                    if(read.trap[t]==null)continue;read.trap[t].enabled=armed;
                    if(armed)
                    {
                        var point=new Vector2(f.feet.x+(u.Side==0?85:-85),f.feet.y+12);
                        float a=t*Mathf.PI*2/read.trap.Length,b=(t+.75f)*Mathf.PI*2/read.trap.Length;
                        PitLineAt(read.trap[t],point+new Vector2(Mathf.Cos(a)*30,Mathf.Sin(a)*10),point+new Vector2(Mathf.Cos(b)*30,Mathf.Sin(b)*10),PxBone,2);
                    }
                }
                if(u.Class==UnitClass.Warden)
                {
                    for(int g=0;g<2;g++)
                    {
                        var ward=live?FindFigure(g==0?u.GuardTargetId:u.GuardTargetId2):null;
                        bool active=ward!=null&&ward.unit.Alive&&ViewSim.CoveredBy(ward.unit)==u&&(!quiet||BeatGuarding(f));
                        if(active&&read.guard[g]==null)read.guard[g]=PitLine("Warden tether",_readoutLayer);
                        if(read.guard[g]!=null)
                        {
                            read.guard[g].enabled=active;
                            if(active)PitLineAt(read.guard[g],PitChest(f),PitChest(ward),new Color(.5f,.85f,.68f,.6f),1);
                        }
                    }
                    float age=_visualTime-read.guardAt;
                    var mark=age>=0&&age<.3f?FindFigure(read.ward):null;
                    if(mark!=null&&live)
                    {
                        var step=(mark.feet-f.feet);step=Vector2.ClampMagnitude(step,24)*(1-age/.3f);
                        f.root.anchoredPosition+=new Vector2(step.x,-step.y);
                    }
                }
                var patient=_visualTime-read.healAt<.35f&&!quiet?FindFigure(read.patient):null;
                if(patient!=null&&read.heal==null)read.heal=PitLine("Medic thread",_readoutLayer);
                if(read.heal!=null)
                {
                    read.heal.enabled=patient!=null;
                    if(patient!=null)PitLineAt(read.heal,PitChest(f),PitChest(patient),PxMint,2);
                }
                for(int n=0;n<read.net.Length;n++)
                {
                    bool net=live&&u.SnareTicks>0&&!quiet;
                    if(net&&read.net[n]==null)read.net[n]=PitLine("Snare net",_readoutLayer);
                    if(read.net[n]==null)continue;read.net[n].enabled=net;
                    if(net)
                    {
                        var p=new Vector2(f.root.anchoredPosition.x,f.feet.y-25);
                        float offset=(n%3-1)*12;
                        PitLineAt(read.net[n],p+new Vector2(-24,offset+(n<3?-17:17)),p+new Vector2(24,offset+(n<3?17:-17)),new Color(.75f,.65f,.85f,.7f),1);
                    }
                }
                float dash=_visualTime-read.dashAt;
                var dashTarget=dash>=0&&dash<.22f?FindFigure(read.dashTarget):null;
                if(dashTarget!=null&&live)
                {
                    float progress=1-dash/.22f;
                    float dx=(dashTarget.feet.x-f.feet.x-Mathf.Sign(dashTarget.feet.x-f.feet.x)*28)*progress;
                    if(!StageHolding&&!LeaderParryPresentation)f.root.anchoredPosition=new Vector2(f.feet.x+dx,-f.feet.y);
                }
                bool slash=dashTarget!=null&&!quiet;
                for(int i=0;i<2;i++)
                {
                    if(slash&&read.slashes[i]==null)read.slashes[i]=PitLine("Strike slash",_readoutLayer);
                    if(read.slashes[i]==null)continue;read.slashes[i].enabled=slash;
                    if(slash)
                    {var p=PitChest(dashTarget);float sign=i==0?1:-1;PitLineAt(read.slashes[i],p+new Vector2(-25,-25*sign),p+new Vector2(25,25*sign),PxBone,3);}
                }
                if(_classReadouts.TryGetValue(u.Id,out var aim)&&aim.aim!=null)
                {
                    if(u.AimTicks>0)read.aimTarget=u.AimTargetId;
                    var aimTarget=FindFigure(read.aimTarget);
                    if(_visualTime-read.aimBrokenAt<.18f&&aimTarget!=null&&u.AimTicks==0)
                        foreach(var line in aim.aim){line.enabled=true;line.color=PxMuted;}
                }
                if(f.readoutRoot!=null)f.readoutRoot.anchoredPosition=f.root.anchoredPosition;
                // A payoff word takes priority over its mark; the two never compete on the victim.
                bool cash=_cashWords.TryGetValue(u.Id,out var pop)&&_visualTime-pop.at<.75f;
                read.word.root.anchoredPosition=new Vector2(f.root.anchoredPosition.x,-(f.feet.y-f.head-14));
                SetPx(read.word,!cash&&!(BeatsOn&&ChiefCutInVisible)&&_visualTime-read.wordAt<.65f?read.text:"",read.colour);
            }
            for(int i=_pitWires.Count-1;i>=0;i--)
            {
                var wire=_pitWires[i];float age=_visualTime-wire.at;
                if(age>=.3f)
                {Destroy(wire.first.gameObject);Destroy(wire.second.gameObject);_pitWires.RemoveAt(i);continue;}
                var origin=FindFigure(wire.origin);var target=FindFigure(wire.target);var finish=FindFigure(wire.finisher);
                if(origin==null||target==null||finish==null)continue;
                PitLineAt(wire.first,PitChest(origin),PitChest(target),PxLilac,3);
                PitLineAt(wire.second,PitChest(target),PitChest(finish),PxMint,3);
            }
            foreach(var ring in _comboHalos)if(ring!=null)ring.enabled=false;
            foreach(var line in _comboLinks)if(line!=null)line.enabled=false;
            SetPx(_squadNames,"",PxMint);SetPx(_comboCall,"",PxMint);
        }
    }
}
