using KindredSiege.Sim.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        RectTransform _meterScreen, _meterPanel;
        RawImage _meterSuns;
        readonly Texture2D[] _meterStates = new Texture2D[MeterRules.Bars+1];
        PxText _meterLabel, _meterBonus;

        void DrawParryMeter(BattleSimulation sim)
        {
            if(_meterScreen!=_screen)
            {
                _meterScreen=_screen;
                _meterPanel=Rect(_screen,"Parry meter",330*Px,4*Px,144*Px,32*Px);
                for(int i=0;i<_meterStates.Length;i++)
                {
                    if(_meterStates[i]==null)_meterStates[i]=Resources.Load<Texture2D>("CampaignArt/UI/Meter/parry_meter_"+i);
                    if(_meterStates[i]==null)throw new System.InvalidOperationException("Approved sun meter missing: "+i);
                }
                _meterSuns=Raw(_meterPanel,"Three brass suns",0,0,144*Px,32*Px);
                _meterSuns.raycastTarget=true;OnHover(_meterSuns.gameObject,MeterHint);
                _meterLabel=PxLabel(_meterPanel,36,33,1);
                _meterBonus=PxLabel(_meterPanel,108,33,1);
            }
            _meterPanel.gameObject.SetActive(sim.MeterOn);
            if(!sim.MeterOn)return;
            int bars=sim.ParryBars(MeterRules.Side);
            _meterSuns.texture=_meterStates[bars];
            SetPx(_meterLabel,"PARRY "+bars+"/"+MeterRules.Bars,bars==MeterRules.Bars?PxGold:PxBone);
            SetPx(_meterBonus,"COMBO +"+sim.ParryComboBonusPercent(MeterRules.Side)+"%",bars>0?PxMint:PxMuted);
        }

        string MeterHint()
        {
            var sim=ViewSim;int bars=sim.ParryBars(MeterRules.Side);
            return bars+"/"+MeterRules.Bars+" parry bars / +"+sim.ParryComboBonusPercent(MeterRules.Side)+
                "% on Combo and Cash hits. A parry fills one; a miss loses none. Your chief spell spends all three, costs no order points, and remains once per fight.";
        }

        void MeterSpellPlate(BattleSimulation sim,bool usable,ref string effect,ref string cost,ref bool lit)
        {
            if(!sim.MeterOn)return;
            cost="";
            if(_fight.Assist==null||ViewOver||sim.AwaitingVerdict)return;
            if(sim.WindupTicks(0)>0){effect="CAST "+Seconds(sim.WindupTicks(0));return;}
            if(sim.WallTicks(0)>0){effect="WALL "+Seconds(sim.WallTicks(0));return;}
            if(_fight.AssistUsed)return;
            int bars=sim.ParryBars(MeterRules.Side);
            if(bars<MeterRules.Bars&&!sim.CanUse(0,Directive.Eclipse,out string why)&&why.Contains("parry bars"))effect=bars+"/"+MeterRules.Bars+" BARS";
            else if(usable){effect="READY";lit=true;}
        }
    }
}
