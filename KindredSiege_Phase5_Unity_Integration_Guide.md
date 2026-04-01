# Kindred Siege - Phase 3 to 5 Editor Integration Guide

This document outlines the full list of scripts created or modified since the beginning of Phase 2 (The Nemesis System), along with all the manual steps required to hook the new Phase 3, 4, and 5 systems into the Unity Editor.

## 1. List of Edited Scripts (Since Phase 2 Start)

Since the original Phase 2 integration, the underlying simulation has expanded massively to accommodate the Sanity system, Phobias, Gambits, the City District grid, and Mythos Exposure.

**Battle Layer Scripts:**
- `BattleEnums.cs`
- `BattleManager.cs`
- `BehaviourTree/BTPresets.cs`
- `BehaviourTree/BattleActions.cs`
- `DirectiveSystem.cs`
- `DreadContestSystem.cs`
- `EncounterType.cs`
- `GambitLibrary.cs`
- `RosterManager.cs`
- `TraumaPhobiaSystem.cs`
- `UnitController.cs`
- `FatigueSystem.cs`
- `SanitySystem.cs` 

**City Layer Scripts:**
- `BuildingData.cs`
- `CityBattleBridge.cs`
- `CityManager.cs`
- `DistrictManager.cs`
- `MythosExposure.cs`

**Core / Foundation Scripts:**
- `EventBus.cs`
- `GameManager.cs`
- `SaveData.cs`
- `SaveManager.cs`

**UI Scripts:**
- `BattleHUD.cs`
- `CityFallenPanel.cs`
- `CityGridPanel.cs`
- `CityHUD.cs`
- `CityRestPanel.cs`
- `GambitSetupPanel.cs`
- `LighthouseMapPanel.cs`
- `MainMenuPanel.cs`
- `PauseMenuPanel.cs`
- `SeasonEndPanel.cs`
- `SettingsPanel.cs`
- `TalentTreePanel.cs`
- `TutorialSystem.cs`
- `VictoryPanel.cs`
- `UnitHealthBar.cs`
- `UnitRecruitPanel.cs`
- `RivalryBoardPanel.cs`

**Other Logic Systems:**
- `ExpeditionPath.cs`
- `MutationEngine.cs`
- `RivalryEngine.cs`
- `BondSystem.cs`
- `TalentNodeId.cs`
- `UnitData.cs`
- `RivalData.cs`

---

## 2. Unity Editor Changes Since the Last PDF

Because our UI architecture relies heavily on `OnGUI` calls to draw panels dynamically, you **do not** need to create complex Canvas prefabs or attach buttons manually.

However, you *do* need to attach our newly created Manager classes to the empty GameObjects in your scenes so their `Awake()` and `Start()` lifecycle hooks run correctly!

### A. The Persistent Manager GameObject (Global Scope)
In your main bootloader scene or city scene, find the persistent root GameObject that currently holds `GameManager` and `CityManager` (it should carry a `DontDestroyOnLoad` declaration). 
**Attach the following new scripts to this GameObject in the Inspector:**
- **`MythosExposure.cs`** (Drives the game over state and dread escalation)
- **`DistrictManager.cs`** (Tracks liberated/locked districts)
- **`SaveManager.cs`** (Handles the serialization of runs)
- **`CityRestPanel.cs`** (Sanatorium and phobia treatment loop)
- **`CityHUD.cs`** (If not already attached; updated to orchestrate sub-panels)
- **`CityFallenPanel.cs`** (Game Over screen)
- **`SettingsPanel.cs`** (Audio/Options)
- **`SeasonEndPanel.cs`** (Season transitions)
- **`PauseMenuPanel.cs`** (Mid-game pause)
- **`MainMenuPanel.cs`** (Title screen)
- **`VictoryPanel.cs`** (Campaign won)
- **`TutorialSystem.cs`** (Onboarding tooltips)

### B. The Battle Arena GameObject (Battle Scope)
In your dedicated Battle Scene, locate the GameObject that currently holds `BattleManager.cs`. 
**Attach the following new Battle-System scripts to this GameObject:**
- **`DirectiveSystem.cs`** (Calculates manual player target actions)
- **`DreadContestSystem.cs`** (Calculates backend psychological saving throws)
- **`FatigueSystem.cs`** (Tracks cumulative unit tiredness)
- **`RosterManager.cs`** (Manages unit arrays during the bout)
- **`TraumaPhobiaSystem.cs`** (Assigns phobias upon unit breakdown)
- **`GambitSetupPanel.cs`** (The UI that allows players to slot their tactics)

### C. Prefabs
- **`UnitHealthBar.cs`**: If you aren't manually injecting it via code, make sure this is attached to the core prefab for your allied and enemy units so that the health and sanity bars render above their heads during the auto-battles.

*(Note: There is no need for manual Canvas alignment. The UI system will auto-draw GUI Rects using predefined layout math on top of the Unity camera bounds!)*
