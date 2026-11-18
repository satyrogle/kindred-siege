# Indie and small team unit and enemy AI

Tags used below. DEV = the developer said or wrote it. FAN = wiki, modder or player documentation. PRESS = a journalist's summary of a developer. COMMENT = a third party's opinion or tutorial.

## Deep simulation AI: Rain World, Kenshi, RimWorld, Dwarf Fortress, Caves of Qud

### Takeaway
Rain World is the best sourced case: each creature is a bundle of small tracker modules plus a relationship table with a 0 to 1 strength per pairing, six personality numbers and a reputation value, and the creatures pursue their own needs instead of serving the player. RimWorld's lesson is the opposite end: Tynan Sylvester says players read far more intent into pawns than is simulated, so names and a small cast do most of the work.

### Cited Findings
- DEV. Rain World creatures were built to survive for themselves, not as obstacles. Joar Jakobsson: "they exist in their own right, they exist there for themselves." [Game Developer](https://gamedeveloper.com/design/crafting-the-complex-chaotic-ecosystem-of-i-rain-world-i-)
- DEV. Jakobsson's target was enemies "smart enough to be outwitted", and he noted "an entity that is stupid enough can be difficult to outsmart." [Game Developer](https://gamedeveloper.com/design/crafting-the-complex-chaotic-ecosystem-of-i-rain-world-i-)
- DEV. Jakobsson admits the cost: "If you design an enemy to care for itself then, that might actually be super difficult and frustrating." James Therrien said over restricting the creatures turned the game into "a basic platformer". [Game Developer](https://gamedeveloper.com/design/crafting-the-complex-chaotic-ecosystem-of-i-rain-world-i-)
- DEV. The team tuned the world, not the creature brains. A wall six tiles high instead of five changed creature paths. Therrien ran regions 15 to 20 times to find trouble spots. A "room attractiveness" value keeps species near suitable rooms. [Game Developer](https://gamedeveloper.com/design/crafting-the-complex-chaotic-ecosystem-of-i-rain-world-i-)
- DEV (Akupara Games, the Watcher DLC team, with Videocult quoted). Creature AI is a set of optional modules attached and configured per creature: PreyTracker, ThreatTracker, FriendTracker (forms squads), ItemTracker, DenFinder, DiscomfortTracker. Each creature also has a table of its relationships to other creatures, objects and places. Attaching modules changes which behaviours exist and their priorities. [Unity blog](https://unity.com/blog/exploring-procedural-design-rain-world)
- DEV. Emergent bug example: a Daddy Long Legs meant as a major threat fixated on tiny worms and ignored the player until small code changes. [Unity blog](https://unity.com/blog/exploring-procedural-design-rain-world)
- FAN. Relationship types are a fixed list: Ignores, Eats, Afraid, StayOutOfWay, AgressiveRival, Attacks, Uncomfortable, Antagonizes, PlaysWith, SocialDependent, Pack, DoesntTrack. Each has an intensity from 0 to 1. Relationships shift at run time from reputation, personality and what the player does (a Pink Lizard's Eats value rises on contact and after being attacked). [Rain World wiki, Behavior](https://rainworld.miraheze.org/wiki/Behavior)
- FAN. Trackers on top of that: ThreatTracker, PreyTracker, RainTracker, InjuryTracker, AgressionTracker, NoiseTracker (investigate sounds), FriendTracker. [Rain World wiki, Behavior](https://rainworld.miraheze.org/wiki/Behavior)
- FAN. Six personality traits per individual: Aggression, Bravery, Dominance, Energy, Nervous, Sympathy. They feed each other (Bravery raises Aggression and Dominance, lowers Nervous) and have visible thresholds: Scavengers' eyes boggle under 30% Bravery, Squidcadas over 50% Bravery never run from the player, low Nervous Scavengers stare longer before switching gaze, high Energy Scavengers move faster. [Rain World wiki, Behavior](https://rainworld.miraheze.org/wiki/Behavior)
- FAN. Reputation runs from -1 to 1, held as regional, global and local values that blend. Killing a Lantern Mouse is the largest single drop listed (-7.5 on the -100 to 100 display). Low reputation gives aggression and fear, high gives neutrality and friendliness. [Rain World wiki, Behavior](https://rainworld.miraheze.org/wiki/Behavior)
- PRESS. RimWorld's storyteller was inspired by Left 4 Dead's AI director and tries to follow a rising and falling pacing curve without over punishing a struggling player. [RimWorld wiki, About](https://rimworldwiki.com/wiki/About_RimWorld)
- DEV via PRESS. Tynan Sylvester on apophenia: "the notion that players extract more story meaning from a game event than is really represented in the mechanics." The cheap triggers named are that every character has a name and the cast is held to around 12. [Stanford Game Design, How Do You Create Apophenia](https://gdt.stanford.edu/how-do-you-create-apophenia/); talk listed at [Game Developer](https://gamedeveloper.com/design/video-how-i-rimworld-i-found-success-through-ridiculous-contrarian-design)
- FAN. RimWorld raid size is a points budget scaled by an adaptation factor, a difficulty threat scale and a days passed curve, then clamped. This is a forum user's reading of the code, constants not verified. [Ludeon forum](https://ludeon.com/forums/index.php?msg=427444)
- DEV. Tarn Adams defines a personality trait as generally an adjective such as "Wise" or "Cruel" that affects AI decisions or stats. [Adams, Characterization and Emergent Narrative in Dwarf Fortress (PDF)](https://wileywiggins.com//documents/dorf/Adams%20-%20Characterization%20and%20Emergent%20Narrative%20in%20Dwarf%20F.pdf)
- DEV via PRESS. Adams describes dwarves as agents "driven by the top need that has to be checked off right now". [PC Gamer](https://www.pcgamer.com/games/sim/dwarf-fortress-dwarves-are-more-human-than-human-creator-says-theyre-allowed-to-embody-bigger-emotions-theyre-allowed-to-make-bigger-mistakes-theyre-allowed-to-do-anything)
- FAN. Kenshi characters run "goals" handed out by squad AI packages, the race, the character or contracts, sorted by urgency. [Kenshi wiki, AI Goals](https://kenshi.wiki/w/AI_Goals) (page returned 404 on direct fetch; this is from the search snippet only)
- DEV. Caves of Qud simulates every creature like the player (levels, skills, gear, faction ties, body parts). Brian Bucklew gave an AI talk at the 2016 roguelike developer conference, listed in the press kit. [Caves of Qud press kit](https://cavesofqud.com/press-kit/)

### Inferences
- Rain World's depth is mostly data, not clever code: a relationship type, a 0 to 1 strength, six personality numbers and a handful of trackers. All of it converts cleanly to integers (0 to 100) for a deterministic sim.
- The personality thresholds are the "iceberg" part that shows. A hidden number becomes a visible tell at a fixed cut (eyes boggle under 30, never flees over 50). That is a direct model for "every decision shows in the animation".
- Relationships that move during play (Eats rises after being attacked) give grudges and fear without any learning system. A fighter who was hurt by one enemy could carry a raised "Attacks" value toward it for the rest of the fight.
- Sylvester's apophenia point cuts against building deep hidden logic: names, a small cast (five fighters is ideal) and visible events may buy more perceived depth than a smarter scorer.
- Jakobsson's warning applies to an auto battler: an AI tuned to look after itself can be unfun. Rain World fixed it in the level, not the brain.

### Gaps
- No developer source found for how Rain World arbitrates between modules (which tracker wins). The wiki gives only one scoring step (Ignores halves the look score). No source found for creature learning or memory of unseen creatures.
- No developer source found for Kenshi's AI internals. The fan wiki page did not load.
- No source found for how Dwarf Fortress decides fight or flee, or for Caves of Qud's goal stack. The Bucklew 2016 talk exists but no transcript was found.
- No official post from Tynan Sylvester on the current storyteller maths was found.

## Tactics, squad and auto battler games

### Takeaway
The two best developer sources are Battle Brothers (utility scores 1 to 10, drop anything at 2 or under, weighted random pick from the rest, with the behaviour list itself cut per creature type) and the Into the Breach AI write up (a plus 5, plus 5, minus 2 scorer with no lookahead, where fairness comes from maps and spawns). Both teams say the trick is what each enemy is not allowed to consider, and that players credit the AI with more than it does.

### Cited Findings
- DEV. Battle Brothers uses utility AI with relative utility. Each behaviour gets a score from 1 to 10 from factors like hit chance, likely damage, terrain, being outnumbered and shield quality. "Everything with a utility of 2 or lower is thrown out." The rest go into a pool and one is drawn at random weighted by score: "the higher the utility, the more likely it is to be picked." [Battle Brothers Dev Blog 27](https://battlebrothersgame.com/?p=1150)
- DEV. Their worked example: a skeleton with axe and shield facing a man with his shield up scores Chop about 3, Split Shield about 9, Shieldwall about 5. [Battle Brothers Dev Blog 27](https://battlebrothersgame.com/?p=1150)
- DEV. Character comes from what each type ignores. Zombies: "They don't care if they're stuck in swamp, have a height disadvantage or are outnumbered", they charge the nearest enemy and use no special skills. Skeletons read terrain and gear, form shieldwalls against archers or when outnumbered, split shields when they cannot hit, hold good ground. Ambushes and long flanks are withheld from undead because they "don't really fit the undead", and given to goblins. [Battle Brothers Dev Blog 27](https://battlebrothersgame.com/?p=1150)
- DEV. They keep randomness on purpose: "we always have an element of unpredictability." A promised deeper follow up was only "a possible later article". [Battle Brothers Dev Blog 27](https://battlebrothersgame.com/?p=1150)
- DEV. Morale was changed from a hidden number to five visible states: Confident (bonus to hit and defence), Steady, Wavering (small penalty, worse future checks), Breaking (big penalty), Fleeing (cannot act, runs, may rally to Wavering once clear). Checks are dice rolls against Bravery. Triggers: an ally cut down, heavy wounds (negative), an enemy slain (positive). A stronger or closer death makes the check harder, a chieftain's death counts more than a grunt's. Undead are immune. The stated reason is that players can read it at a glance and understand why someone ran. [Battle Brothers Dev Blog 20](https://battlebrothersgame.com/?p=883)
- DEV. Enemy retreat was added because chasing the last survivor was not fun. Only the living retreat, when they see no chance of winning, judged from allies left, allies fallen and the player's men left. They must reach the map edge and can be cut down on the way. [Battle Brothers Dev Blog 44](https://battlebrothersgame.com/dev-blog-44-retreating-enemies)
- DEV (unsigned document; the author says they were the game's only programmer, which fits Matthew Davis of Subset Games, attribution not confirmed). Into the Breach enemy loop: list every reachable tile (about 30), check four attack directions from each, score plus 5 per enemy unit hit, plus 5 per building hit, minus 2 per friendly hit, drop low scores, pick at random among the best. [Into the Breach AI write up](https://docs.google.com/document/d/1OPRNzAVQNgPtP35HzhV2ZzEqE1eCdXbWA7lEZXDdMHQ/mobilebasic)
- DEV (same document). The AI may take second best options, and harder difficulties take the best one more often. Repeating the same position and target is scored down so enemies do not simply undo the player's move. Fire, pits and tiles already targeted by earlier enemies are scored down. Map edges are scored down hard because edge enemies are hard to push. [Into the Breach AI write up](https://docs.google.com/document/d/1OPRNzAVQNgPtP35HzhV2ZzEqE1eCdXbWA7lEZXDdMHQ/mobilebasic)
- DEV (same document). What it does not do: no coordination between enemies, no difference between hitting buildings and mechs, no reading of the player's weapons, no planning of future turns. The author notes players "often assume the AI is doing many things more than it actually does." The game is called "more of a puzzle game than a strategy game", and the biggest balance lever was spawn count, not AI. [Into the Breach AI write up](https://docs.google.com/document/d/1OPRNzAVQNgPtP35HzhV2ZzEqE1eCdXbWA7lEZXDdMHQ/mobilebasic)
- DEV via PRESS. Into the Breach was built so "players always know where the enemy are attacking next". More enemies made the board "needlessly complex and hard to parse", so difficulty moved to defending buildings. Maps are hand made because "You need to always be able to manipulate them", or an enemy becomes "an unstoppable force". [Game Developer](https://gamedeveloper.com/design/reimagining-failure-in-strategy-game-design-in-i-into-the-breach-i-)
- DEV (talk summary of Anthony Giovannetti, "Slay the Spire: Metrics Driven Design and Balance"). Enemies first rolled a random move each turn. "Players too felt that it was too random" and "I have no idea how much this enemy was attacking for." A click to read "next move" sentence was tried and dropped. The team "literally went through probably ten different versions of our intent system". [Talk notes](https://readray.ai/notes/76667) (machine summary of the talk, treat wording as approximate)
- DEV. Cliff Harris says the Gratuitous Space Battles orders system "was a mess": orders sat on the ship, not the weapon, and players could not tell how orders such as Attack Fighters and Co-operate combined. [Cliff's blog, 2025](https://www.positech.co.uk/cliffsblog/2025/08/15/designing-the-orders-system-for-ridiculous-space-battles/)
- DEV. His redesign for Ridiculous Space Battles splits targeting into two visible kinds. Target criteria are filters in a priority order (size class, or defence state: shields up, armour only, hull only). Then exactly one discriminator breaks ties: Co-operate, Vulture, Retaliate, Breakthrough (only ships straight ahead in my row) or Last Defense (enemies furthest through our lines). Steps: list enemies in range and arc that match any filter, narrow by filters top to bottom, apply the tie breaker. Colour coding and visible priority numbers are his readability tools, and he rejects conditional logic because it would feel "like work". [Cliff's blog, 2025](https://www.positech.co.uk/cliffsblog/2025/08/15/designing-the-orders-system-for-ridiculous-space-battles/)
- DEV. An old GSB fix: scanning about 300 ships twice per target call was slow and ships stalled with preferred targets out of range. Each fleet now tracks which classes it still has, and when that changes ships are told to drop class based orders. [Cliff's blog, 2009](https://positech.co.uk/cliffsblog/2009/09/22/fixing-the-ai-orders-again)
- DEV. GSB battles are deterministic, and Harris planned AI fleets from hand design plus testing against thousands of randomly mutated fleets. [Cliff's blog, 2009](https://positech.co.uk/cliffsblog/2009/04/13/initial-thoughts-on-game-ai) (search snippet only, page not fetched)
- FAN. Mechabellum units generally target or move toward the closest enemy, with facing mattering somewhat. Upgrades bend the rule instead of replacing it: Aerial Specialization "will treat airborne units as 30m closer", Wraiths spread guns across as many enemies as are in range, one Melting Point upgrade tries to hit 5 different enemies. [Mechabellum wiki, Targeting](https://mbwiki.dreamhaven.com/mechanics/targeting)
- FAN. Darkest Dungeon's monster AI lives in one data file, base.monster_brains.json, which mods replace or extend. [Steam Workshop comments](https://steamcommunity.com/sharedfiles/filedetails/comments/1419429961)
- FAN (player speculation). Darkest Dungeon spiders seem to keep hitting the same hero because a mark raises the chance of being targeted again and marked heroes take bonus damage. [Steam discussion](https://steamcommunity.com/app/262060/discussions/0/135509314044957178)
- DEV. Darkest Dungeon's stress and affliction system was built to model the human response to stress, shown as a meter on each hero. [Game Developer deep dive](https://gamedeveloper.com/design/game-design-deep-dive-i-darkest-dungeon-s-i-affliction-system) (search snippet only)

### Inferences
- Battle Brothers answers the "utility scorer is basic" complaint without leaving utility. The depth is in three cheap moves: a per type behaviour list (what this fighter is blind to), a cut off that throws out bad options, and a weighted draw so two fights never play the same. A seeded integer draw keeps that deterministic.
- "What it ignores" is the cheapest personality there is. A zombie that does not care about swamp reads as a zombie. For Kindred Siege, a class or tribe could be defined by the two or three considerations it never weighs.
- Five named morale states that the player can see beat a hidden morale number. This maps straight onto a nerve bar with stance clips per state.
- Into the Breach shows a five line scorer can pass as smart when the stage, the spawn count and the telegraph do the work. "Do not repeat last turn's choice" is a single rule that stops the AI looking stuck.
- Harris's filter list plus one tie breaker is a strong shape for class targeting in an auto battler: it is ordered, has no dictionary iteration, and each tie breaker is a nameable personality (Vulture finishes the weak, Retaliate hits whoever hit me, Co-operate piles on). Mechabellum's "treat as 30 m closer" shows upgrades can be small offsets on a nearest target rule.
- Slay the Spire's ten versions of intent says the display of the decision took more work than the decision.

### Gaps
- No Battle Brothers "AI Part 2" was found. No exact morale formulas are published in the blog.
- No developer or modder documentation of the fields inside Darkest Dungeon's monster brain file was found, so targeting rules there are unverified.
- Nothing usable was found on Wartales, Stoneshard, Mount and Blade, Backpack Battles, Super Auto Pets or The Bazaar AI within the search budget. They were not searched in depth.
- The Into the Breach location score formula is in an image in the document and was not readable.
- The GDC talk for Into the Breach was not read directly, only press summaries.

## Action indies

### Takeaway
Developer sourced detail here is thin. The firm points are Hyper Light Drifter's rule that single enemies must be dumb and the horde supplies the smarts, Hollow Knight bosses choosing attacks from the player's position, and the attack token pattern (a small budget of who may attack at once), which is documented for DOOM 2016 and The Last of Us Part II, not for Sifu.

### Cited Findings
- DEV. Alex Preston (Hyper Light Drifter): "unless it's a Boss, we can't have an enemy be smart one on one." And: "We have to dumb it down, because as we add more enemies, that situation becomes exponentially more difficult." [Game Developer](https://gamedeveloper.com/design/how-i-hyper-light-drifter-i-balances-difficulty-with-fairness)
- DEV. Preston: "Combat scenarios are puzzles because you have to figure out the best approach". A cut enemy that deflected bullets and parried became unmanageable when paired with even simple enemies. Combat is read through silhouettes and windup animations. [Game Developer](https://gamedeveloper.com/design/how-i-hyper-light-drifter-i-balances-difficulty-with-fairness)
- PRESS (reporting Team Cherry's interview with ACMI). Hollow Knight bosses chose their next attack from where the player stood: far away gives a ranged attack, close gives a swing. Silksong moved to choosing an attack and a movement together so enemies can reposition. [GamesRadar](https://www.gamesradar.com/games/platformer/compared-to-hollow-knight-silksong-reworked-how-the-bosses-pick-their-attacks-and-suddenly-i-realize-why-i-got-crushed-so-often) (headline and search summary only, body not fetched)
- DEV via PRESS. Ari Gibson: "Everything was squishier and squatter and slow because everything reflects and works against the protagonist." William Pellen: "Hornet's enemies had to have more ways of catching her as she tries to move away." [Console and PC Gaming, citing the ACMI guide](https://consolepcgaming.com/how-team-cherry-evolved-hollow-knights-bosses-for-silksong)
- COMMENT (a tool seller's tutorial, not a shipped indie). Attack tokens: an enemy must claim a token from the target before attacking and hand it back after. Suggested pools per target: one melee, two ranged, one heavy, each its own budget. One 0 to 1 dial scales the pools from 0.5 to 1.5 times for difficulty. Enemies without a token must strafe, advance or reposition, because standing still looks like "politely queuing". It credits id Software (DOOM 2016) and Naughty Dog (The Last of Us Part II) with the pattern. [StraySpark](https://www.strayspark.studio/blog/attack-token-system-ue5-group-combat-that-feels-fair)
- COMMENT (indie devlogs, via search summary). One team got "take turns" attacks with a single timer of random length 0.3 to 1.2 seconds that fires one enemy action. Another gives tokens to the best scoring asker (distance, line of sight, time waited), and a much higher score can steal a token. [Iron Reclamation devlog](https://lghts.itch.io/iron-reclamation/devlog/1092177/making-combat-feel-good-fair-readable-and-responsive-ai); [Unreal RPG devlog](https://pavankrao-qlp.github.io/portfolio/blog/unreal-rpg-devlog-part-1---state-tree-based-ai--unreal) (pages not fetched)
- PRESS. Sifu has a Structure gauge on both the player and enemies that models breaking a stance. [PlayStation Blog](https://blog.playstation.com/2021/11/18/how-sifus-kung-fu-combat-works/) (search snippet only)
- DEV via PRESS. Dead Cells: Sébastien Bénard says the game has about 30 to 40 small hidden assists so it never feels unfair. The two described are player side (a late jump after leaving a ledge, a climb assist). [80.lv](https://80.lv/articles/dead-cells-hidden-tricks-that-make-the-game-feel-fair)
- COMMENT. A critic argues Hades II has a legibility problem, with one boss using five different animations for waves of damage. Players say attack radius is not clear from enemy animation and that this was less of a problem in Hades. [nex-3 blog](https://nex-3.com/blog/hades-ii-has-a-legibility-problem/); [Steam discussion](https://steamcommunity.com/app/1145350/discussions/0/591769824782233841)

### Inferences
- Preston's rule transfers directly: five fighters a side is already a horde, so each one should run a very small brain and the interplay (marks laid and cashed) supplies the intelligence.
- A token budget solves the "everyone swings at once, nobody can read it" problem and fits the project's existing one blow at a time direction. Tokens are integers and ordered claims, so they are deterministic. The key craft note is that fighters without a token must visibly do something (shift, feint, guard), or the fight looks like a queue.
- Hollow Knight's position based pick is the smallest possible readable brain: the player learns "far means X, close means Y". An auto battler version is "state of the target means move": marked target means cash, shielded target means break.

### Gaps
- No source was found that Sifu uses attack tokens. Two searches found no Sloclap talk or interview on it. Treat "Sifu attack tokens" as unconfirmed.
- No Supergiant source was found on Hades enemy telegraph rules. Only third party commentary turned up.
- No developer source on Dead Cells enemy behaviour rules was found. The GDC talk "Dead Cells: What the F*n!?" was not read.
- Nothing was found on Absolver, Nidhogg, Katana Zero or Blasphemous enemy AI.
- The Team Cherry statements come through press reports of an ACMI guide book that was not read directly.

## What made each AI feel smart for little cost, and what developers said about readability

### Takeaway
Across the sourced cases the cheap wins are the same: keep each unit's brain small, make character out of what a unit ignores, show the decision before or as it happens, and let the level, spawn count and unit mix carry the difficulty. Several developers say outright that players imagine more intelligence than exists.

### Cited Findings
- DEV. Players "often assume the AI is doing many things more than it actually does." (Into the Breach.) [Into the Breach AI write up](https://docs.google.com/document/d/1OPRNzAVQNgPtP35HzhV2ZzEqE1eCdXbWA7lEZXDdMHQ/mobilebasic)
- DEV via PRESS. Players "extract more story meaning from a game event than is really represented in the mechanics." (Tynan Sylvester.) [Stanford Game Design](https://gdt.stanford.edu/how-do-you-create-apophenia/)
- DEV. Enemies should be "smart enough to be outwitted". (Joar Jakobsson.) [Game Developer](https://gamedeveloper.com/design/crafting-the-complex-chaotic-ecosystem-of-i-rain-world-i-)
- DEV. Per type blind spots make types feel distinct (zombies ignore terrain and numbers, undead never ambush). [Battle Brothers Dev Blog 27](https://battlebrothersgame.com/?p=1150)
- DEV. Morale was made five visible states so the player sees a break coming and knows why it happened. [Battle Brothers Dev Blog 20](https://battlebrothersgame.com/?p=883)
- DEV. Hidden random enemy moves read as "too random". Showing the next action fixed it, after about ten versions of the display. [Slay the Spire talk notes](https://readray.ai/notes/76667)
- DEV via PRESS. Telegraph every enemy attack and cap enemy count, because more enemies made the board "hard to parse". [Game Developer](https://gamedeveloper.com/design/reimagining-failure-in-strategy-game-design-in-i-into-the-breach-i-)
- DEV. Single enemies stay dumb, the mix makes the puzzle, and windups plus silhouettes carry the read. [Game Developer, Hyper Light Drifter](https://gamedeveloper.com/design/how-i-hyper-light-drifter-i-balances-difficulty-with-fairness)
- DEV. Orders the player cannot see combining are a failure. Fix with a visible priority list, one tie breaker, colour coding, no conditionals. [Cliff's blog, 2025](https://www.positech.co.uk/cliffsblog/2025/08/15/designing-the-orders-system-for-ridiculous-space-battles/)
- DEV. Tune the place, not the brain: wall heights, room attractiveness, hand made maps, spawn counts. [Game Developer, Rain World](https://gamedeveloper.com/design/crafting-the-complex-chaotic-ecosystem-of-i-rain-world-i-); [Into the Breach AI write up](https://docs.google.com/document/d/1OPRNzAVQNgPtP35HzhV2ZzEqE1eCdXbWA7lEZXDdMHQ/mobilebasic)
- FAN. Hidden personality numbers surface as fixed visible tells at thresholds (Rain World Scavenger eyes, gaze time, speed). [Rain World wiki, Behavior](https://rainworld.miraheze.org/wiki/Behavior)

### Inferences
- A pattern list for Kindred Siege, each one integer friendly and cheap:
  1. Blind spots per class or tribe (Battle Brothers).
  2. Score, cut the bottom, seeded weighted draw from the rest (Battle Brothers), plus "do not repeat last choice" (Into the Breach).
  3. Ordered filters then one named tie breaker per fighter (Harris). The tie breaker is the fighter's visible personality.
  4. Relationship strength per pairing that moves in the fight, for grudges and fear (Rain World).
  5. Personality numbers with fixed thresholds that each trigger a clip or pose (Rain World).
  6. Five visible nerve states with a clip each (Battle Brothers).
  7. An attack token budget so one blow lands at a time, with idle fighters visibly busy (DOOM pattern, via commentary).
  8. Intent shown before the act (Slay the Spire, Into the Breach).
- The sources agree the display of a decision costs more effort than the decision. With 20 frame per second clips, the practical rule is one clip or pose per decision type, and no decision type without one.
- None of these sources describes a system deeper than a scorer plus rules. The "iceberg" in the best cases is data (relationships, traits, per type lists) on top of simple selection, not a more advanced algorithm.

### Gaps
- No source directly ties AI decisions to sprite animation budgets or frame counts. That link is inference.
- No auto battler developer (Mechabellum, Backpack Battles, Super Auto Pets, The Bazaar) was found explaining unit AI in their own words. Mechabellum's rules come from a wiki of unknown status.
- AI and Games episodes on these titles were not found or read within the budget.
