# Kindred Siege — Phase 6 Editor Integration Guide
## Changes Since the Phase 5 PDF

This document covers all scripts modified or created since commit `4ee5369` ("docs: add Phase 2 changes PDF and editor integration guide"). It lists the full set of changed files for reference, then walks through every manual Unity Editor step required to wire the new systems in.

---

## 1. Full List of Changed Scripts Since the Last PDF

### New Scripts (did not exist before)
| Script | Location | Purpose |
|--------|----------|---------|
| `SettingsPanel.cs` | `Assets/Scripts/UI/` | Audio, battle speed, and tooltip settings |
| `PauseMenuPanel.cs` | `Assets/Scripts/UI/` | In-battle pause overlay (Escape key) |
| `VictoryPanel.cs` | `Assets/Scripts/UI/` | Campaign-won screen with stat snapshot |
| `TutorialSystem.cs` | `Assets/Scripts/UI/` | First-play contextual hints |
| `CityGridPanel.cs` | `Assets/Scripts/UI/` | Visual city district grid drawn inside CityHUD |
| `CityFallenPanel.cs` | `Assets/Scripts/UI/` | Game Over screen |
| `MainMenuPanel.cs` | `Assets/Scripts/UI/` | Title screen with Settings button |

### Modified Scripts
| Script | Location | Key Changes |
|--------|----------|-------------|
| `BattleManager.cs` | `Assets/Scripts/Battle/` | SanitySiege encounter type, SetTargetDistrict(), ExistentialDread fix, DrownedGround fix |
| `BattleEnums.cs` | `Assets/Scripts/Battle/` | Victory/defeat enum additions |
| `BTPresets.cs` | `Assets/Scripts/BehaviourTree/` | Preset updates for new encounter types |
| `BattleActions.cs` | `Assets/Scripts/BehaviourTree/` | SanitySiege physical-damage guard |
| `DirectiveSystem.cs` | `Assets/Scripts/Battle/` | DirectivePoints property rename |
| `DreadContestSystem.cs` | `Assets/Scripts/Battle/` | Dread contest adjustments |
| `EncounterType.cs` | `Assets/Scripts/Battle/` | Added `SanitySiege = 6` |
| `GambitLibrary.cs` | `Assets/Scripts/Battle/` | Dark gambit discount at Scholar+ Mythos |
| `RosterManager.cs` | `Assets/Scripts/Battle/` | ClearRoster() for NewGame |
| `TraumaPhobiaSystem.cs` | `Assets/Scripts/Battle/` | Added `CurePhobia(UnitData)` static method |
| `UnitController.cs` | `Assets/Scripts/Battle/` | SanitySiege: TakeDamage early-return for physical hits |
| `FatigueSystem.cs` | `Assets/Scripts/Battle/` | Minor adjustments |
| `SanitySystem.cs` | `Assets/Scripts/Battle/` | Minor adjustments |
| `BuildingData.cs` | `Assets/Scripts/City/` | ScriptableObject field updates |
| `CityBattleBridge.cs` | `Assets/Scripts/City/` | OnBattleEnd: LiberateDistrict on SanitySiege victory |
| `CityManager.cs` | `Assets/Scripts/City/` | PlacedBuildings list exposed |
| `DistrictManager.cs` | `Assets/Scripts/City/` | LiberateDistrict(), AllDistrictsLiberated, IsLiberated(), GetRandomUnliberatedDistrict() |
| `MythosExposure.cs` | `Assets/Scripts/City/` | Reduce(int) restored; ExposureTier doc updated |
| `EventBus.cs` | `Assets/Scripts/Core/` | BattleEndEvent: ActiveEncounter + TargetDistrict fields |
| `GameManager.cs` | `Assets/Scripts/Core/` | Added Victory GameState and valid transitions |
| `SaveData.cs` | `Assets/Scripts/Core/` | Save format updated |
| `SaveManager.cs` | `Assets/Scripts/Core/` | Save/load for new fields |
| `BattleHUD.cs` | `Assets/Scripts/UI/` | Minor HUD adjustments |
| `CityHUD.cs` | `Assets/Scripts/UI/` | Calls CityGridPanel.DrawGrid() in centre panel |
| `CityRestPanel.cs` | `Assets/Scripts/UI/` | Treatment cost: 300G + 2KP; calls CurePhobia() |
| `GambitSetupPanel.cs` | `Assets/Scripts/UI/` | Fully-qualified GameManager references |
| `LighthouseMapPanel.cs` | `Assets/Scripts/UI/` | SanitySiege path generation; sets TargetDistrict |
| `SeasonEndPanel.cs` | `Assets/Scripts/UI/` | Minor updates |
| `TalentTreePanel.cs` | `Assets/Scripts/UI/` | Minor updates |
| `UnitHealthBar.cs` | `Assets/Scripts/UI/` | Minor updates |
| `UnitRecruitPanel.cs` | `Assets/Scripts/UI/` | Minor updates |
| `RivalryBoardPanel.cs` | `Assets/Scripts/UI/` | Minor updates |
| `ExpeditionPath.cs` | `Assets/Scripts/Modifiers/` | Added TargetDistrict field |
| `MutationEngine.cs` | `Assets/Scripts/Modifiers/` | Minor updates |

---

## 2. Unity Editor Changes Since the Last PDF

The previous PDF already listed `CityFallenPanel.cs`, `CityHUD.cs`, and `MainMenuPanel.cs` as needing attachment. If you have already done those steps, skip them below — only the four new MonoBehaviours and one important note about `CityGridPanel` are genuinely new work.

### A. Persistent Manager GameObject — New Scripts to Attach

Find the persistent root GameObject that holds `GameManager` and `CityManager` (the one with `DontDestroyOnLoad`). Add the following **new** components:

| Component | What it does |
|-----------|-------------|
| **`SettingsPanel.cs`** | Draws the audio/speed/tooltip settings overlay. Required by TutorialSystem and PauseMenuPanel to check tooltip state. |
| **`PauseMenuPanel.cs`** | Listens for the Escape key during BattlePhase and pauses/resumes the simulation. Also provides a "Quit to Main Menu" button. |
| **`VictoryPanel.cs`** | Activates automatically when `GameManager` enters the `Victory` state. Shows final campaign stats and a return-to-menu button. |
| **`TutorialSystem.cs`** | Delivers first-play hints at key state transitions. Reads from `SettingsPanel.ShowTooltips` to decide whether to display. |

> **Order matters for initialization:** Unity calls `Awake()` in component order. Attach `SettingsPanel` before `TutorialSystem` on the same GameObject, or ensure `SettingsPanel` lives on an earlier-processed object, so its singleton is ready when `TutorialSystem.Start()` runs.

---

### B. CityGridPanel — No Attachment Needed

`CityGridPanel` is a **static class**, not a MonoBehaviour. You do **not** attach it to any GameObject. It is called automatically by `CityHUD.DrawCentrePanel()` on every `OnGUI` frame. No Inspector work required.

---

### C. Verify These Were Done in the Previous PDF

The prior guide listed these — double-check they are already attached to the Persistent Manager GO before building:

- `CityFallenPanel.cs`
- `CityHUD.cs`
- `SeasonEndPanel.cs`
- `PauseMenuPanel.cs` *(was listed — now confirmed created)*
- `MainMenuPanel.cs`

---

### D. GameManager — Victory State

`GameManager.GameState` now includes a **`Victory`** value. No Inspector field change is required — the enum is code-only. However:

- The `Victory` state is reached via `CityBattleBridge` when a `SanitySiege` encounter is won and the final district is liberated.
- `VictoryPanel` subscribes to `GameManager.OnStateChanged` automatically on `Start()`.
- The valid transition `Victory → MainMenu` is already implemented; no scene-load script is needed.

---

### E. DistrictManager — Liberation Wiring

`DistrictManager` now has:
- `LiberateDistrict(DistrictType)` — called by `CityBattleBridge` on SanitySiege victory
- `AllDistrictsLiberated` bool — checked by `CityBattleBridge` to trigger `Victory`
- `IsLiberated(DistrictType)` — used by `CityGridPanel` to draw gold borders
- `GetRandomUnliberatedDistrict()` — used by `LighthouseMapPanel` to generate SanitySiege paths

No Inspector wiring is required. All calls happen through the singleton. Confirm `DistrictManager` is already attached to the Persistent Manager GO (it was included in the previous PDF).

---

### F. EncounterType — SanitySiege

A new encounter value `SanitySiege = 6` has been added to `EncounterType.cs`. This encounter:
- Disables physical damage to all units
- Ends after 90 seconds of in-battle time
- On victory, triggers district liberation via `CityBattleBridge`

No ScriptableObject or Inspector change is needed. The encounter is generated programmatically by `LighthouseMapPanel` when an unliberated district is available.

---

### G. Settings Persistence (PlayerPrefs Keys)

`SettingsPanel` reads and writes three PlayerPrefs keys on startup:

| Key | Type | Default |
|-----|------|---------|
| `KS_MasterVolume` | float | 0.8 |
| `KS_DefaultBattleSpeed` | float | 1.0 |
| `KS_ShowTooltips` | int (0/1) | 1 |

These are created automatically on first launch. No manual setup required.

---

### H. Tutorial Persistence (PlayerPrefs Key)

`TutorialSystem` uses one PlayerPrefs key:

| Key | Type | Notes |
|-----|------|-------|
| `KS_TutorialFlags` | int (bitmask) | Bit 0=Welcome, 1=PreBattle, 2=Battle, 3=PostBattle, 4=SeasonEnd |

Reset automatically on `GameManager.NewGame()` via `TutorialSystem.ResetTutorial()`.

---

## 3. Summary Checklist

Work through this list top to bottom after pulling the latest code:

- [ ] Attach **`SettingsPanel.cs`** to the Persistent Manager GO
- [ ] Attach **`PauseMenuPanel.cs`** to the Persistent Manager GO
- [ ] Attach **`VictoryPanel.cs`** to the Persistent Manager GO
- [ ] Attach **`TutorialSystem.cs`** to the Persistent Manager GO
- [ ] Confirm `SettingsPanel` component is ordered *before* `TutorialSystem` in the Inspector
- [ ] Confirm `CityFallenPanel`, `CityHUD`, `SeasonEndPanel`, and `MainMenuPanel` are already attached (from previous PDF)
- [ ] Confirm `DistrictManager` is already attached (from previous PDF)
- [ ] No changes needed for `CityGridPanel` — static class, no attachment
- [ ] No ScriptableObject changes needed for `SanitySiege` encounter type
- [ ] Build & Play — enter CityPhase to confirm the district grid renders in `CityHUD`
- [ ] Confirm Escape key opens pause overlay during a battle
- [ ] Confirm Settings button appears on the Main Menu
- [ ] Liberate all 5 districts to verify the Victory screen triggers

---

*Generated for Kindred Siege · Phase 6 · Build target: Unity 2022.3 LTS*
