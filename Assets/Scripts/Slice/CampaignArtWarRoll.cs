using System.Collections;
using System.Linq;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        bool WarRollReview => Argument("-ksWarRoll") != null;
        bool _warBriefing;
        bool _warPrint;
        string _warInspect;
        int _warRosterPage;
        const int WarPageSize = 5;

        void BuildWarRoll()
        {
            LoadPixelKit();
            var encounter = SelectedEncounter;
            var roster = _campaign.Reserve;
            var fighter = roster.FirstOrDefault(f => f.Id == _warInspect)
                ?? roster.FirstOrDefault(HasRollPrint) ?? roster.FirstOrDefault();
            _warInspect = fighter?.Id;
            int lastPage = Mathf.Max(0, (roster.Count - 1) / WarPageSize);
            _warRosterPage = Mathf.Clamp(_warRosterPage, 0, lastPage);

            var ground = Resources.Load<Texture2D>("WarRollReview/camp5");
            Raw(_screen, "Camp 5 at native scale", StageX * Px, StageY * Px, ground.width, ground.height).texture = ground;
            bool print = fighter != null && HasRollPrint(fighter) && _warPrint;
            if (print)
            {
                var engraving = Resources.Load<Texture2D>("WarRollReview/brakka-print");
                Raw(_screen, "War Roll engraving", 580, 83, engraving.width, engraving.height).texture = engraving;
            }
            PxImage(_screen, "Obsidian roster edge", Resources.Load<Texture2D>("WarRollReview/roster-edge"), StageX, StageY);
            PxHeader("War Roll", PxBone, PxGold, new PxLines());
            PxButton("war-brief", "BRIEFING", 566, 4, 128, 25, () => { _warBriefing = true; SetPage(Page.Squad); });
            SetPx(PxLabel(_screen, 29, 55), "MUSTER   " + _squad.Count + " / " + encounter.SquadSize, PxGold);

            int first = _warRosterPage * WarPageSize;
            for (int i = first; i < Mathf.Min(first + WarPageSize, roster.Count); i++)
            {
                var entry = roster[i];
                int top = 77 + 48 * (i - first);
                var inspect = PxButton("inspect-" + entry.Id, "", 27, top, 187, 40, () =>
                {
                    _warInspect = entry.Id;
                    SetPage(Page.Squad);
                    SelectControl("inspect-" + entry.Id);
                });
                SetPx(PxLabel(inspect.transform, 15, 8), Fit(entry.Name, 128), entry == fighter ? PxGold : PxBone);
                SetPx(PxLabel(inspect.transform, 15, 23), SliceClasses.Name(entry.Class), PxMuted);
                if (_squad.Contains(entry.Id)) SetPx(PxLabel(inspect.transform, 158, 15), "IN", PxMint);
                if (entry == fighter) SetPx(PxLabel(inspect.transform, 4, 8), ">", PxGold);
            }
            if (lastPage > 0)
            {
                PxButton("war-prev", "PREV", 27, 325, 83, 24, () => { _warRosterPage--; SetPage(Page.Squad); }).interactable = _warRosterPage > 0;
                PxButton("war-next", "NEXT", 117, 325, 95, 24, () => { _warRosterPage++; SetPage(Page.Squad); }).interactable = _warRosterPage < lastPage;
            }
            if (fighter != null)
            {
                if (!print) WarRollFighter(fighter);
                var legend = PxImage(_screen, "Fighter legend", Plate(_kitButton, 12, 389, 83), 302, 325);
                var nameArt = _logoFont.Render(Fit(fighter.Name, 133).ToUpperInvariant(), PxBone, PxGold, 1, true);
                _screenTextures.Add(nameArt);
                PxImage(legend.transform, "Name", nameArt, 17, 6);
                SetPx(PxLabel(legend.transform, 19, 40), SliceClasses.Name(fighter.Class) + "   /   RANK " + fighter.Rank, PxGold);
                SetPx(PxLabel(legend.transform, 19, 57), WarRole(fighter.Class), PxBone);
                if (HasRollPrint(fighter))
                    PxButton("war-art", print ? "VIEW SPRITE" : "VIEW PRINT", 443, 4, 115, 25, () =>
                    {
                        _warPrint = !_warPrint;
                        SetPage(Page.Squad);
                        SelectControl("war-art");
                    });
                bool taken = _squad.Contains(fighter.Id);
                PxButton("fighter-" + fighter.Id, taken ? "LEAVE OUT" : "TAKE", 27, 365, 88, 28,
                    () => { ToggleSquadPick(fighter, encounter); SelectControl("fighter-" + fighter.Id); })
                    .interactable = taken || encounter.Kind == EncounterKind.Duel || _squad.Count < encounter.SquadSize;
                PxButton("row-" + fighter.Id, fighter.Row == Row.Front ? "FRONT" : "BACK", 119, 365, 85, 28, () => FlipRow(fighter.Id));
                PxButton("gear-" + fighter.Id, "GEAR", 205, 421, 76, 24, () => ShowGearPicker(fighter.Id));
            }
            PxButton("back", "ROAD", 16, 421, 90, 24, () => SetPage(Page.Offers));
            FavourButton(112, 421, 86);
            PxButton("start-fight", "ENTER FIGHT", 544, 420, 158, 26, StartEncounterFight)
                .interactable = _squad.Count > 0 && _squad.Count <= encounter.SquadSize;
            _pxNotice = PxLabel(_screen, 289, 429);
        }

        static bool HasRollPrint(CampaignFighter f) => f.TribeId == CampaignTribes.ClarthId && f.Class == UnitClass.Skirmisher;

        static string WarRole(UnitClass role)
        {
            switch (role)
            {
                case UnitClass.Warden: return "Guards an ally. Takes their hit.";
                case UnitClass.Marksman: return "Aimed shots punish snares.";
                case UnitClass.Skirmisher: return "Strikes harder into openings.";
                case UnitClass.Trapper: return "Snares foes for the squad.";
                case UnitClass.Medic: return "Heals injured allies.";
                default: return SliceClasses.Name(role);
            }
        }

        void WarRollFighter(CampaignFighter fighter)
        {
            string prefix = fighter.TribeId == CampaignTribes.ClarthId ? "clarth"
                : fighter.TribeId == CampaignTribes.BloodridgeId ? "blood_rot"
                : fighter.TribeId == CampaignTribes.SkullpeakId ? "skull"
                : fighter.TribeId == CampaignTribes.StoneveilId ? "stoneveil"
                : fighter.TribeId == CampaignTribes.ThundercoastId ? "thundercoast" : null;
            if (prefix == null) return;
            bool windup = prefix == "clarth" && fighter.Class == UnitClass.Warden;
            var clip = LoadClip(prefix + "_" + fighter.Class.ToString().ToLowerInvariant() + (windup ? "_se_attack" : "_se_idle"));
            var frame = clip.sheet.frames[windup ? Mathf.Min(16,clip.sheet.frames.Length-1) : 0].frame;
            const int scale = 3;
            var shadow = Resources.Load<Texture2D>("WarRollReview/foot-shadow");
            Raw(_screen, "Foot shadow", 844 - shadow.width * scale / 2, 638, shadow.width * scale, shadow.height * scale).texture = shadow;
            var body = Raw(_screen, "Original fighter at 3x", 844 - Mathf.RoundToInt(clip.sheet.anchorX) * scale,
                645 - Mathf.RoundToInt(clip.sheet.anchorY) * scale, frame.w * scale, frame.h * scale);
            body.texture = clip.texture;
            body.uvRect = new UnityEngine.Rect((float)frame.x/clip.texture.width,1f-(float)(frame.y+frame.h)/clip.texture.height,
                (float)frame.w/clip.texture.width,(float)frame.h/clip.texture.height);
        }

        IEnumerator ValidateWarRoll()
        {
            yield return null;
            _auto = false;
            Camera.main.targetTexture = null;
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.worldCamera = null;
            _campaign = FreshRoadPreview();
            SetPage(Page.Offers);
            yield return null;
            Check(Click("offer-0") && _page == Page.Squad, "road node opens live War Roll");
            yield return null;
            var skirmisher = _campaign.Reserve.First(HasRollPrint);
            Check(_warInspect == skirmisher.Id, "skirmisher inspected from campaign roster");
            StageCapture("01-pixel-roll");
            yield return new WaitForSecondsRealtime(.3f);
            Check(Click("war-art") && _warPrint, "real print switch");
            yield return null;
            StageCapture("02-print-roll");
            yield return new WaitForSecondsRealtime(.3f);
            var warden = _campaign.Reserve.First(f => f.Class == UnitClass.Warden);
            Check(Click("inspect-" + warden.Id) && _warInspect == warden.Id, "inspect another actual fighter");
            yield return null;
            bool before = _squad.Contains(warden.Id);
            Check(Click("fighter-" + warden.Id) && _squad.Contains(warden.Id) != before, "squad membership changes");
            yield return null;
            Check(Click("fighter-" + warden.Id) && _squad.Contains(warden.Id) == before, "squad membership restored");
            yield return null;
            var row = warden.Row;
            Check(Click("row-" + warden.Id) && warden.Row != row, "row reaches campaign");
            yield return null;
            Check(Click("row-" + warden.Id) && warden.Row == row, "row restored");
            yield return null;
            Check(Click("gear-" + warden.Id) && _modal != null, "gear picker reached");
            CloseModal();
            yield return null;
            Check(Click("war-brief") && _warBriefing, "existing squad briefing available");
            yield return null;
            Check(Click("war-return") && !_warBriefing, "return to roll");
            yield return null;
            StageCapture("03-warden");
            yield return new WaitForSecondsRealtime(.3f);
            Check(Click("start-fight"), "enter actual encounter");
            yield return RealtimeQaWaitForBattle();
            yield return new WaitForSecondsRealtime(1);
            Check(_fight != null && ViewSim.CurrentTick > 0, "live encounter ticking");
            StageCapture("04-live-fight");
            yield return new WaitForSecondsRealtime(.5f);
        }
    }
}
