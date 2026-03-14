# Kindred Siege

**An AI-driven auto-battler × city-builder where gameplay generates real charitable donations.**

> Train AI armies. Build strategic cities. Change the real world.

---

## What Is This?

Kindred Siege is a hybrid game where:

- **City-Builder Loop** — Construct a city that generates resources, trains units, and funds charitable infrastructure
- **Auto-Battler Loop** — Your AI-controlled army fights battles automatically, using behaviour trees and reinforcement learning to adapt over time
- **Charity Integration** — Every battle generates Kindness Points that translate into real-world donations to partnered charities

The AI isn't just an opponent — it's your champion. Players configure, observe, and shape their army's behaviour through a visual editor, creating a feedback loop between human strategy and machine learning.

## Tech Stack

- **Engine:** Unity 2022 LTS (C#)
- **AI:** Custom behaviour tree system + Unity ML-Agents (reinforcement learning)
- **Architecture:** Event-driven, ScriptableObject-based data, modular systems

## Project Status

In Active Development — Solo developer project

| System | Status |
|--------|--------|
| Behaviour Tree Engine | Core complete |
| Battle Grid | Core complete |
| Unit Controller | Core complete |
| Battle Manager | Core complete |
| 6 Unit AI Presets | Complete |
| Resource Economy | Core complete |
| KP / Charity Tracker | Core complete |
| City Builder Grid | Next up |
| Building System | Planned |
| ML-Agents RL Layer | Phase 2 |
| Visual AI Editor | Phase 2 |
| Battle Replay System | Planned |

## Architecture

```
Scripts/
├── Core/               GameManager, EventBus, ResourceManager
├── Battle/             BattleManager, BattleGrid, UnitController
│   └── BehaviourTree/  BT engine, action nodes, unit presets
├── AI/                 RL integration (Phase 2)
├── City/               City grid, buildings, adjacency
├── Charity/            KP tracking, donation system
└── UI/                 HUD, menus, battle replay
```

### AI Architecture (Three Layers)

1. **Behaviour Trees** — Hand-authored decision trees for each unit type. Configurable by players.
2. **Reinforcement Learning** — Unity ML-Agents layer that adjusts behaviour weights based on battle outcomes (Phase 2).
3. **Meta-Strategy Advisor** — Rule-based city planning suggestions (Phase 3).

## Getting Started

1. Clone this repo
2. Open in Unity 2022.3 LTS or later
3. Open the BattleTest scene
4. Create UnitData ScriptableObjects (Assets > Create > KindredSiege > Unit Data)
5. Assign them to the BattleManager's team arrays
6. Hit Play and watch the AI fight

## Research Foundation

This project extends research from my MSc thesis at Brunel University London — *Psychological Resilience in Life Simulations* — which investigated how behavioural algorithms model adaptive decision-making in game agents.

## Licence

TBD

---

*Built in Surrey, UK*
