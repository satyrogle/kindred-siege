using System;
using System.IO;
using KindredSiege.Sim.Learning;
using UnityEngine;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        bool _gatesReview;
        bool _raidReview, _sprintReview;
        bool _roadReview, _kitReview, _clearReview, _squadReview, _butcherReview, _layersReview, _themeReview, _palRoadReview, _payoffReview, _dangerReview, _meterReview, _floorReview;

        int ReviewRulesCap => _sprintReview?Campaign.TightParryRules:_floorReview?Campaign.FloorRules:_meterReview?Campaign.MeterRules:_dangerReview?Campaign.DangerRules:_payoffReview?Campaign.PayoffRules:_palRoadReview?Campaign.PalRoadRules:_themeReview?Campaign.MarksRules:_layersReview?Campaign.LayersRules:_butcherReview?Campaign.ButcherComboRules:
            _squadReview?Campaign.SquadScreenRules:_clearReview?Campaign.ClearRoadRules:
            _kitReview?Campaign.RoadKitRules:Campaign.RoadCombatRules;
        CampaignTuning ReviewTuning => _roadReview?new CampaignTuning{RulesCap=ReviewRulesCap}:null;

        void PrepareReviewRoute()
        {
            _sprintReview=Argument("-ksRoad26Review")!=null;
            _floorReview=_sprintReview||Argument("-ksRoad23Review")!=null;
            _meterReview=_floorReview||Argument("-ksRoad22Review")!=null;
            _dangerReview=_meterReview||Argument("-ksRoad21Review")!=null;
            _payoffReview=_dangerReview||Argument("-ksRoad20Review")!=null;
            _palRoadReview=_payoffReview||Argument("-ksRoad19Review")!=null;
            _themeReview=_palRoadReview||Argument("-ksRoad18Review")!=null;
            _layersReview=_themeReview||Argument("-ksRoad17Review")!=null;
            _butcherReview=_layersReview||Argument("-ksRoad15Review")!=null;
            _squadReview=_butcherReview||Argument("-ksRoad14Review")!=null;
            _clearReview=_squadReview||Argument("-ksRoad13Review")!=null;
            _kitReview=_clearReview||Argument("-ksRoad12Review")!=null;
            string road=Argument("-ksRoad26Review")??Argument("-ksRoad23Review")??Argument("-ksRoad22Review")??Argument("-ksRoad21Review")??Argument("-ksRoad20Review")??Argument("-ksRoad19Review")??Argument("-ksRoad18Review")??Argument("-ksRoad17Review")??Argument("-ksRoad15Review")??Argument("-ksRoad14Review")??Argument("-ksRoad13Review")??Argument("-ksRoad12Review")??Argument("-ksRoadReview");
            if(road!=null)
            {
                _roadReview=_gatesReview=true;
                string roadRoot=Path.Combine(Path.GetFullPath(road),_sprintReview?"road26":_floorReview?"road23":_meterReview?"road22":_dangerReview?"road21":_payoffReview?"road20":_palRoadReview?"road19":_themeReview?"road18":_layersReview?"road17":_butcherReview?"road15":_squadReview?"road14":_clearReview?"road13":_kitReview?"road12":"road11");
                Directory.CreateDirectory(roadRoot);_savePath=Path.Combine(roadRoot,"campaign.txt");
                if(!File.Exists(_savePath))File.WriteAllText(_savePath,CampaignSave.Write(FreshRoadPreview()));
                return;
            }
            string raid=Argument("-ksRaidReview");
            if(raid!=null)
            {
                _raidReview=_gatesReview=true;
                string raidRoot=Path.Combine(Path.GetFullPath(raid),"raid10");
                Directory.CreateDirectory(raidRoot);
                _savePath=Path.Combine(raidRoot,"campaign.txt");
                if(!File.Exists(_savePath))File.WriteAllText(_savePath,CampaignSave.Write(FreshRaidPreview()));
                return;
            }
            string directory = Argument("-ksGatesReview");
            _gatesReview = directory != null;
            if (!_gatesReview) return;
            // A dedicated directory, even when a caller supplies the ordinary save directory.
            string root = Path.Combine(Path.GetFullPath(directory), "gates012");
            Directory.CreateDirectory(root);
            _savePath = Path.Combine(root, ReviewCheckpoint + "_campaign.txt");
            if (!File.Exists(_savePath)) File.Copy(ReviewCampPath, _savePath);
        }

        string ReviewCheckpoint => Argument("-ksGatesCheckpoint")=="chief" ? "chief" : Argument("-ksGatesCheckpoint")=="rematch" ? "rematch" : "camp";
        string ReviewCampPath => Path.Combine(Application.streamingAssetsPath, "ReviewRoute", ReviewCheckpoint+".txt");

        void NewReviewRoute()
        {
            // Only NEW REVIEW resets this isolated save. Continue preserves commands, rewards and lessons.
            _campaign = _roadReview?FreshRoadPreview():_raidReview?FreshRaidPreview():CampaignSave.Read(File.ReadAllText(ReviewCampPath));
            _fight = null; _playback = null;
            _notice = _roadReview?(_sprintReview?"RULES 26 / TIGHT PARRY + WHISPERER / SEPARATE SAVE":_floorReview?"RULES 23 / PAL FOUNDATIONS / SEPARATE SAVE":_meterReview?"RULES 22 / PARRY METER / SEPARATE SAVE":_dangerReview?"RULES 21 / SEPARATE SAVE":_payoffReview?"RULES 20 / SEPARATE SAVE":_palRoadReview?"Rules 19 / Pal roads, one reward, ranked combos and bursts / separate save.":_themeReview?"Rules 18 / ranked Butcher combos and mark bursts / separate save.":_layersReview?"Rules 17 / Butcher combos, champion duels and damage layers / separate save.":_butcherReview?"Rules 15 / Butcher combos / separate save.":_squadReview?"Rules 14 / protected leader and visible teamwork / separate save.":_clearReview?"Rules 13 / four steps to the chief / separate save.":_kitReview?"Rules 12 / automatic squad kit / separate save.":"Rules 11 preview / Fall Back spam remains too strong."):_raidReview?"Bloodridge raid preview / separate save.":"Review checkpoint: "+ReviewCheckpoint+". Separate save; normal fight rules.";
            Save(); GoToStage();
        }

        Campaign FreshRaidPreview(int seed=2)
        {
            // The historical file stores the camp before its active fight. Keep it untouched and
            // start a new encounter via Campaign so the preview uses the current rules honestly.
            string old=File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"ReviewRoute","chief.txt"));
            int at=old.IndexOf("fight\t",StringComparison.Ordinal);
            if(at<0)throw new InvalidDataException("Chief review checkpoint has no active fight section.");
            string camp=old.Substring(0,at)+"end\n";
            // Explicit preview fixture: seed 2 rolls Cleave/Eclipse/Ring. Seed 21 remains in the
            // shipped historical checkpoint and is checked separately for Mantle coverage.
            camp=System.Text.RegularExpressions.Regex.Replace(camp,@"(?m)^seed\t\d+", "seed\t"+seed);
            var campaign=CampaignSave.Read(camp,ReviewTuning);
            campaign.StartEncounter(0);
            return campaign;
        }

        Campaign FreshRoadPreview()
        {
            var campaign=CampaignSave.Read(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"ReviewRoute","camp.txt")),ReviewTuning);
            campaign.ChooseTribe(CampaignTribes.BloodridgeId);
            return campaign;
        }
    }
}
