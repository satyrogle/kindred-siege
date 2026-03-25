# Kindred Siege - Phase 2 (Nemesis System) Integration Guide

This document outlines how to handle the Phase 2 code changes inside the Unity Editor. Because the new systems were designed to inject cleanly into the existing runtime, **the required Editor changes are extremely minimal!**

## 1. The Rivalry Board UI (War Table)
You **do not** need to manually build a UI Canvas or attach any new scripts for the Rivalry Board.
- **How it works:** In `CityHUD.cs`, clicking the "Inspect Dominions" button will automatically call `gameObject.AddComponent<RivalryBoardPanel>()` if it doesn't already exist.
- **Editor Action:** None required! The script uses Unity's immediate mode UI (`OnGUI`) to draw the panel dynamically on top of the City screen.

## 2. Rivalry Engine
The memory and adaptation mathematics are managed entirely by `RivalryEngine.cs`.
- **Editor Action:** Verify that the `RivalryEngine` script is attached to a permanent GameObject in your starting scene (such as your `GameManager` or a dedicated `Systems` prefab). It uses `DontDestroyOnLoad` to persist across battles.

## 3. Testing Adaptive Traits in the Inspector
We added three new class-counter traits to the `RivalTraitType` enum:
- `VanguardSlayer` (+20% damage to Vanguards)
- `WardenBreaker` (Ignores Warden armour)
- `ShadowCatcher` (Prevents Shadows from vanishing)
- **Editor Action:** If you have any pre-configured `RivalData` ScriptableObjects in your Assets folder for testing, you can now select these new traits from the inspector dropdown. Otherwise, the AI will learn them procedurally during the campaign.

## 4. The Grudge System & Dread Contests
These are pure backend mechanical hooks.
- **Dread Contests:** Hooked directly into `UnitController.TakeDamage`. When hit by an Overlord, units will flash **magenta** if they fail the contest and get stunned.
- **Grudges:** Hooked into `BattleManager.OnUnitDefeated`. `BattleManager.Update()` will automatically force the Rival AI to hunt their grudge target.
- **Editor Action:** None required! Just make sure your Overlord/Captain classes are properly mapped to your Rival definitions.

---
*Happy hunting! The Nemesis system is now fully autonomous.*
