# Kindred Siege: the game as it stands

Written 11 Nov 2026 by Claude, at Jacob's request, from every line of `DESIGN_LOCKS.md` and from Jacob's words on 10 and 11 Nov.

This is one page that says what the game is today. It does not replace `DESIGN_LOCKS.md`, which stays the record. Where the two disagree, the locks win and this file is wrong.

## How to read the tags

- **LOCKED**: Jacob decided it. The number is the line in `DESIGN_LOCKS.md`.
- **BUILT**: it is in the sim. Almost nothing here has been checked in Unity.
- **SAID**: Jacob said it on 10 or 11 Nov. Not written into the locks yet.
- **OPEN**: undecided, or only somebody's proposal.

Every number in the game is a starting value.

---

## 1. The game in one page

- A funny cult of cyborg flesh beings raids the Sovereign and gets wiped out in a second. The leader and four officers survive. **LOCKED** (L79, L674)
- You take over tribes, one per tier, on the way back to her. **LOCKED** (L186, L196)
- It is a roguelike, fast paced, and simple: "go in, fight a few times, beat the chief". **LOCKED** (L17, L181)
- "ours is parry driven auto chess period". **LOCKED in the log** (HANDOFF_LOG 11835)
- The game is "staying in the fight commanding a small war party". (HANDOFF_LOG 11787)

**The fight, one picture. SAID 10 to 12 Nov, confirmed by Jacob 12 Nov ("yes write it in the gdd")**
- **Units** fight on their own in every fight, road and boss. Pairs trading blows, "like a real combat like how codex made".
- **The parry** is universal. It is always there, in every fight, timing only. It stays the big move: "risky with a lot of reward".
- **The four buttons** are redesigned and belong to your leader. Each one is his own move, with "insane cool animations".
- **Their leader causes problems. Your leader is the answer.** "chief's leader causes problems and darkseid is the answer for the problems."
- **On the road** (stages 1 to 3), the leader's buttons help the squad win.
- **At the champion and the chief** (stages 4 and 5), the buttons are how the leader answers the boss. That is where they are tested hardest. "Boss fights, everything should be different. And how you tackle that is all the gameplay." (L441)
- The leader supports. The units keep the credit.
- This joins two of Jacob's own lines into one game: "parry driven auto chess" (27 Oct) and the boss as the game (20 Oct, 11 Nov). It is one kit everywhere, not two modes.
**The leader's kit, the point. Confirmed by Jacob 12 Nov ("yes")**
- You are the advanced species. The tribes have tech too, but yours is far ahead.
- The Pals take your tech and put it into the tribes' fighters. That is the enhancing.
- The leader uses the same tech, raw, live in the fight. Time and gravity: "them having manipulation and tactics over time and gravity that actually makes sense".
- Same civilisation and same direction as the Pals' tech, but **not a clone of the Butcher**. Pull, portal and stop time stay the Butcher's (L718). The leader bends other forces: "do something else for leader same direction but not the clone" (12 Nov).
- His tools are general, not made for one boss move. So there is a use for them against anything a chief throws: "it is about answer for everything kind of way". How you use them is the skill.
- **Only the parry cancels a boss attack.** A button never stops, steals or switches off a boss move: "thats not handling thts cheating". The attack still lands, the button helps you handle it. Exception, 18 Nov: tool 3 interrupts cleaves and AoE attacks.
- The tools are the same in every fight, against every tribe, and they get bigger as you go up the tiers, the way Kingdom Rush powers do (his example, not to be copied).
**Tool 1: Phase. Jacob's own design, 12 Nov ("so that's that")**
- His words: "He delays the boss attack higher tiers negate them, attacks like tank busters and casts can only be parried they cant be phased ever .. future tech cyberpunk style".
- Tier 1 delays a boss attack. Higher tiers delay it longer. The top tier negates it.
- Tank busters and casts can never be phased. Only the parry handles those. A gold flash means parry only (gold is the parry's alone, L1303).
- Delayed attacks pile up. Phase two and both land together when the delay ends, so you build your own tank buster. "i like the tank buster idea which gives more build variety".
- No new meter. The parry meter is the only meter in the fight: "We already have a meter for parry how many do you think we need to track". You track the delays by counting the rings on him.
- **The look: the orbit lock. LOCKED 12 Nov** ("yeah fine bro lock this animation", DESIGN_LOCKS "Phase, the orbit lock animation"). A thin cyan ring at his feet means phased. Each caught swing snaps one cyan ring round him, up to three, like a gyroscope. At three they crush into him and he flashes gold for the parry. The first look, a glitching hologram, was rejected: "bro so much noise".
- **Who Phase hits. LOCKED 14 Nov** ("Yes, lock it", DESIGN_LOCKS "Phase on a unit and on a boss"). At the champion and the chief it takes their attack, by the rules above. On the road it takes the enemy unit himself: he is held, can't hit or be hit, his pair breaks and our fighter goes two on one, then he slams back with one big stored hit for the leader to parry. "Phase buys time now and makes one big parry later." Take 2 of the Warden clip is his pick for the grip, not a locked animation.
- **The build up. LOCKED 14 Nov** ("yea okay", DESIGN_LOCKS "The build up"). Every button press marks the enemy who throws the string; he keeps your last two. Each button has one half string, so his string is the older half then the newer: four halves, sixteen strings. Every hit is gold and parry only; each miss hits the whole group; a clean string pays a huge squad counter. The chief throws at half health and under 30% (the biggest attack in the game), the champion a short version, and on the road their leader the shortest when half his band is down, its counter landing on his band. Expedition 33's multi parry, his reference.
- **Tiers. LOCKED 14 Nov** ("Yes, lock tiers", DESIGN_LOCKS "Phase tiers"). The same three in both fights. 1 holds one blow (a boss pile of two). 2 holds longer and stacks up to three rings. 3 sends a parried pile back into the unit or the boss: the negate goes through the parry.
- **The relic parry. LOCKED 15 Nov** ("Yes, lock it", DESIGN_LOCKS "The leader's relic parries the Phase blow"). His own relic, the one on his sprite, floats over his palm and holds the blow. He punches the relic, it bursts gold into the blow, the chief goes white and one meter sun lights in place. The relic never changes shape and there is only one.
- **OPEN:** how a tier is raised, the other three halves, every number.
- Not built.
**Tool 2: Tectonic Shift. Jacob's own seed, 15 Nov**
- His words: "shifts gravity to create more damage bro more heavy hits argon is encapsulated into crystals that are left behind on succesful parrys break these".
- Every parry leaves an argon crystal on the floor under the man parried. The press breaks them all and the floor under each heaves up in slabs. Whoever stands there goes up, ours too: his duel breaks, he can't swing or dodge, hits on him land heavy, then he slams down. Two crystals close together make one bigger break. No crystals, no shift.
- **LOCKED 16 Nov** ("Yes, lock it" to cut 5, DESIGN_LOCKS "Tectonic Shift, the leader's gravity tool"). His relic punch with a violet burst; the slabs shudder, then tear loose over dark gaps; violet light from below on the lifted men; the floor is whole after the slam. Rejected on the way: slabs cut as ovals ("how will grounf break in a perfect circle omg") and glowing crack lines ("bro those lines are the problem omg").
- **OPEN:** how it works at the champion and the chief, its half string, its tiers, every number.
- Not built.
**Tool 3: the relic's ground attack. Jacob's own design, 16 to 18 Nov**
- His words: "keep it an attack it interupts units and boss cleaves/aoe attacks from hitting ... changes terrain for 4-5 sec depenind on terrain units get buff/debuff but whatever attack does the terrain change should feel like a residue of it".
- The relic flies into the fight and that stage's ground attacks their men there. The hit interrupts their units, and at bosses it breaks cleaves and AoE attacks (his exception to the parry rule above; tank busters, casts and the build up string stay the parry's). What the attack leaves stays 4 to 5 s: a buff for ours on it, a debuff for theirs.
- **LOCKED 18 Nov** ("yes" to the play back, then "Yes, swap both", DESIGN_LOCKS "Tool 3, the relic's ground attack"). It replaces the 17 Nov grave Hex version: "i see some skulls and ground going black wtf how is that a unique game changing thing".
- The five grounds are Claude's calls: court roots (tangle; ours take less damage, theirs can't leave their duel), cave stone (rubble; ours hit harder, theirs lighter), Skullpeak ice (a sheet; ours faster, theirs can't dodge), Thundercoast coral (a reef; ours above the flood, theirs cut), Clarth sand (dust; ours dodge, theirs miss).
- **OPEN:** its name, its half string, its tiers, every number, whether it can kill, the 57's and the unnamed tribes' grounds.
- Not built.
- **OPEN, call 1:** tool 4. On the table, none picked: size, momentum, decay, sight, mind link.

- Your build comes from the road. Four Pals each change one thing. **LOCKED** (L870, L717)
- Tribes and rivals learn from your real fights. **LOCKED** (L69)
- Everything the player has to learn must read as "x + y = z". (HANDOFF_LOG 12819)

---

## 2. The world

- 25 tribes: 23 tribes, the Sovereign's own, and the hunters. **LOCKED** (L11)
- 7 tiers. About four tribes per tier. A run meets one per tier. **LOCKED** (L197)
- 17 classes. The slice has five: Warden, Marksman, Skirmisher, Trapper, Medic. **LOCKED** (L42, L70). Bloodridge and Skullpeak field these five in the slice too, an exception for the slice only; their rolled rows and the new classes start after 8 Dec: **LOCKED 16 Nov** (DESIGN_LOCKS "Four hooks, six talents, stagger and the slice's classes")
- Every class is a fighter. No tank, no healer: "we don't need tank and healer. We just need all out blown out units fighting each other". The Warden is a bruiser, the Medic is a mage named the Hexer (LOCKED 15 Nov). **LOCKED 14 Nov** (DESIGN_LOCKS "Every class is a fighter"). This replaces the 20 Oct rule "tank cant heal and heal cant dps" (L411).
- Nothing hands a class another class's spell. **LOCKED** (L411)
- Tribes designed so far: Clarth (yours from the tutorial), Bloodridge, Skullpeak (chief Arina), Stoneveil, Thundercoast. **LOCKED** (L101 to L103, L397)
- The Sovereign is a she, tier 7. **LOCKED** (L80)
- The hunters are the 25th tribe and the rival. They get harder as the game goes on. **LOCKED** (L363)
- Tone: funny everywhere. Only the Sovereign is scary, plus Bloodridge. **LOCKED** (L81)
- The 8 Dec slice is Fallen Star: one run, about 20 to 30 minutes. **LOCKED** (L70, L405)

- **The 17 rewritten. LOCKED 15 Nov** (DESIGN_LOCKS "The 17 classes rewritten"). Every class has a kind of hit and a six stat spread, so each beats some and loses to others, like Pokémon types. Armour stops blades and shots, spells beat the slow and armoured, Speed dodges spells, shots and holds catch the fast. Two new classes per tier: 1 Vanguard, Berserker; 2 Artillerist, Field Engineer; 3 Duelist, Infiltrator; 4 Binder, Recon; 5 Chemist, Disruptor; 6 Conduit, Herald. Beat any tribe of a tier and its two unlock at camp. Class is the type, the tribe's tier is the strength. Not built.
- **Which classes each tribe fields. LOCKED 15 Nov** (DESIGN_LOCKS "Which classes each tribe fields, rolled once"). Tribes are shuffled, so no tribe fields every class. Rolled once, not every run: each of the 23 tribes fields 5 classes from those unlocked up to its tier, always one of its own tier's two, never one its lore bans, and no two tribes of a tier field the same five. Bloodridge: Skirmisher, Trapper, Hexer, Vanguard, Berserker. Stoneveil: Warden, Skirmisher, Trapper, Hexer, Berserker. Skullpeak: Skirmisher, Trapper, Hexer, Vanguard, Artillerist. Thundercoast: Skirmisher, Vanguard, Berserker, Artillerist, Field Engineer. The other 19 are slots. 115 tribe and class pairs, one rank 3 morph each. This replaces "every tribe fields the five plus its tier's two". A chief stands apart from his tribe's five: **LOCKED 15 Nov** (DESIGN_LOCKS "A chief stands apart"), so Bloodridge's chief stays a Marksman and Thundercoast's a Warden. Not built.

**OPEN**: which tribe sits in which tier, the platform.

---

## 3. Your side

**The leader**
- He is the player. Codename Darkseid, a working name only. **SAID**
- His look is locked: three eyes, clear brain, a relic over his palm. **LOCKED** (L120)
- He is on the field in every fight. **BUILT**
- He cannot be hit while any of your fighters stands. **BUILT**, rules 11 to 14 (L827)
- He does the parry for the whole squad. Big hits are pulled through a mind link onto him. **BUILT**, rules 41, Jacob's "Go" (HANDOFF_LOG 14407)
- He gets "an awesome parry animation". One parry, not several kinds. **SAID**
- He supports. The fighters keep the credit. **SAID**
- He is the button enhancer. Making his four buttons stronger is his own job, not a Pal's. **SAID 12 Nov** (this was the Shade's job)
- Driving his attacks or movement directly was withdrawn as a genre change. (HANDOFF_LOG 11787)
- Every animation of his powers and of the cult's tech is Type 4, 25th century: "nothing generic nothing shit". No ray or line lodged between two units. **LOCKED 12 Nov** (DESIGN_LOCKS "The cult's tech is Type 4 in every animation")

**The four Pals**
- They are the surviving officers. All four stay at camp as upgraders and never fight. **LOCKED** (L118, HANDOFF_LOG 5137)
- "Each Pal changes one thing, and no verb belongs to two Pals." **LOCKED** (L717)

| Pal | Changes | Core | State |
|---|---|---|---|
| Butcher | A fighter's weapon | 12 combos, one per effect, in three flavours: pull, portal, stop time: **LOCKED 16 Nov** (DESIGN_LOCKS "The Butcher's combos cut to 12"). Six classes have none. Four of the 12 carry a mark hook: **LOCKED 16 Nov** | The first 15 are BUILT, rules 15; the 12 are not built |
| Glutton | A role of fighters | A talent tree of seven limbs. Three picks climb one limb to its keystone | BUILT, rules 29 |
| Shade | Moves from spared chiefs | **SAID 12 Nov, replaces her button routes.** She holds every move taken from a chief you spare. Each move has three tiers: 1 basic, 2 mid, 3 a heavy hit plus a buff. You pay materials at her camp station to raise a tier, and it stays for good. | Her old routes are BUILT as rules 28. The new job is not built |
| Whisperer | One fighter | Your star: his spell twice, then a second gear slot. **SAID 12 Nov:** she also hands out the old Shade routes as versions of a unit's defence and boost slots | Star is LOCKED. Routes on units not built |

**The camp**: a grove on a plateau over clouds, four stations. Not fully locked. (L698)

---

## 4. The run

- Every run starts and ends at camp. **LOCKED** (L186)
- A tribe's road is a map you see whole: 3 lanes, 3 rows of nodes, then the end. **LOCKED** (L322)
- Nodes are fights, events and hunter ambushes. **LOCKED** (L323)
- **The stages of a road. SAID**, and it matches the 8 Nov lock (L1341, L1342):
  - Stages 1 to 3 are road fights. All of theirs die, you go on.
  - Stage 4 is the champion, "like wow dungeon boss".
  - Stage 5 is the chief himself.
- Road fighters show off small versions of their chief's moves, so you learn him on the way. **BUILT**, rules 12 and 21 (L1093)
- The champion uses about 40 to 45% of the chief's kit. The rest is new in the last fight. **SAID**
- A lost fight costs one of 3 lives. Nobody is captured. **BUILT** (L827)
- Wins in a row multiply materials, up to three times. **BUILT** (L827)
- Every road fight carries a Pal. A win offers that Pal's three picks and the tribe's gear on one card. Take one thing or bank. **LOCKED** (L1051)
- Gear: one slot per fighter. A piece belongs to one class and bends that class's own spell. Never drawn on the sprite. **LOCKED** (L415, L416)
- Nobody joins from road fights. Spare the kneeling chief and his tribe joins, with his spell. **LOCKED** (L449)
- Execute is gone. **LOCKED** (L756)
- Beasts are rolls, 1 in 5 runs, stronger each time you beat them. **LOCKED** (L1326)
- The run's last fight is the hunt. **BUILT**, rules 6
- Kept between runs: materials, the forge, scars, the Glutton's tree, the codex, what rivals learned. Lost: the army, gear, chief spells, every Pal pick. **LOCKED** (L201, L870)

---

## 5. The fight

### Layer 1: the units fight on their own

- Five fighters and your leader against their band and their leader. **LOCKED**, **SAID**
- They fight on their own: "these are great as long as they re code and what player has not to worry" (HANDOFF_LOG 11533). Every class is a fighter, no tank and no healer. **LOCKED 14 Nov**
- Each class has its auto attack and one spell only it has: Warden heavy bash (the man staggers, loses his next swing and is left with an Opening), Marksman Aimed Shot, Skirmisher melee strike, Trapper Snare, the mage's blast (bursts on her target and splashes the men next to him). **LOCKED** (L412, the Warden and the mage changed 14 Nov)
- Parked 14 Nov because each was built on guarding or healing, then settled on 15 Nov: Self Patch and Stim were replaced. The Glutton's Healers limb became **Casters: LOCKED 15 Nov** (Matter Printer, Rewind Wound, Half Life or Entropy Purge, Recompile; DESIGN_LOCKS "The Glutton's Healers limb becomes Casters"), not built. Bodyguard, Rescuer, Triage, Front First and Mend and Hold are **PARKED 15 Nov** until Codex's unit combat settles, since habits and pairs pick targets and his brains do that now; Covered Treatment is dropped (DESIGN_LOCKS "The habits and the Mend and Hold pair are parked"). Mercy Well, Evac, Rewind and Wrong Door became Curse Well, Split Hex, Old Wounds and a new Wrong Door: **LOCKED 15 Nov** (DESIGN_LOCKS "The four parked Butcher combos"). Not built. Only Split Hex survives the 16 Nov cut to 12 (DESIGN_LOCKS "The Butcher's combos cut to 12").
- Four marks: Snare, Bleed, Opening, Hex. One class lays a mark and another cashes it. **LOCKED** (L414). The Hex replaced the Guard mark: **LOCKED 15 Nov** (DESIGN_LOCKS "The Hex replaces the Guard mark"). The Hexer's blast curses her target and the splashed men, and the Warden's bash on a cursed man lands double and uses up the curse. Not built.
- The 12 new classes' marks: **LOCKED 15 Nov** (DESIGN_LOCKS "The 12 new classes' marks"). Each new class's attack lays or cashes one mark, one of each per tier, and a cash lands double. Snare: only the Trapper lays it; the Marksman, Artillerist and Chemist cash it. Bleed: the Trapper, Field Engineer and Infiltrator lay it; the Skirmisher and Berserker cash it. Opening: the Warden, Marksman, Vanguard and Recon lay it; the Skirmisher, Duelist and Binder cash it. Hex: the Hexer, Disruptor and Herald lay it; the Warden and Conduit cash it. Not built. Stagger stays a plain status, not a mark, with no cash word: **LOCKED 16 Nov**; the sim needs one timer Bloodrush can read. The Hex's cash word is HEXED: **LOCKED 16 Nov** (DESIGN_LOCKS "The Hex's cash word"). HEXED is black with a bone edge, and every cash word is drawn at pixel size 2: **LOCKED 16 Nov**. Not in Unity yet.
- A blow rolls once: dodge, parry, block, crit or hit. Six stats. No chance is ever shown, only damage and MISS. **BUILT**, rules 42. **LOCKED** (L291)
- Wardens hold the middle and stop melee blows by their Defence. **BUILT**, rules 43 (L1343). Replaced 14 Nov: the bruiser has no blocking job. The sim still plays it.
- Each side stands on its own 3 by 3 panels, and each class reaches a set shape. **BUILT in the sim**, rules 44 (HANDOFF_LOG 14757). Shapes **LOCKED** (L1366). The Warden's and the Hexer's are open: their 9 Nov bases guard and heal, and Claude's 15 Nov call was **PARKED** the same day, because Codex owns the unit combat and his duel mocks have no grid (DESIGN_LOCKS "The Warden's and the Hexer's grid shapes, PARKED"). The sim still plays Guard the cross and Touch.
- **Locked pairs. LOCKED 9 Nov, not built** (L1376 to L1383):
  - One opponent each, kept until one falls or an order breaks the pair.
  - They trade: the faster swings, the other answers, then swings back.
  - A shooter fires on his lane partner's swing.
  - Everyone pairs, the Warden and the mage too. **LOCKED 14 Nov**. The 9 Nov line kept them out (L1379).
  - Every pair fights at once.
- This is the fight Jacob means by "like a real combat like how codex made". **SAID**. It exists only in Codex's browser mock, not in the sim.
- Your fighters never run. They only die. **LOCKED**

**Three slots per unit. SAID 12 Nov, confirmed by Jacob ("yes write it in the gdd")**
- Every unit carries exactly three slots and nothing outside them. This cuts the noise of too many layers on every fighter.
- It is Jacob's own idea from 25 Oct: "3 attacks/defensekit/magic boost etc to each unit that they use in proper intervals of time" (HANDOFF_LOG 11151).

| Slot | What it is | Warden example | Which Pal changes it |
|---|---|---|---|
| 1. Attack | His class spell. It lays or cashes a mark | His heavy bash (was Guard until 14 Nov) | Butcher, through his weapon augment |
| 2. Defence | What he does when he is hit | Brace, or the Mirror route | Whisperer |
| 3. Boost | Fires on its own every so often | Shield Pin: pins the man in front of him for 2 s, so he can't dodge (**LOCKED 15 Nov**) | Whisperer |

- The Hexer's three: her blast, which lays the Hex; Evil Eye, the melee man whose blow lands on her gets the Hex; Bad Blood, a bleeding man's bleed jumps to the men next to him. **LOCKED 15 Nov** (DESIGN_LOCKS "The Warden's slot 3 and the Hexer's slots 2 and 3"). Not built.
- The 12 new classes' defence and boost moves: **LOCKED 15 Nov** (DESIGN_LOCKS "The 12 new classes' defence and boost moves"). Vanguard Headbutt, Crack Armour. Berserker Blood Rage, Bloodrush. Artillerist Point Blank, Concussion Shell. Field Engineer Cover Fire, Redeploy. Duelist Riposte, Disarm. Infiltrator Smoke Bomb, Ambush. Binder Lash, Gauntlet. Recon Scramble, Range Finder. Chemist Cracked Vial, Accelerant. Disruptor Static, Jam. Conduit Ground Out, Overchannel. Herald Bellow, Dread Horn. Not built.
- The 12 new classes' Butcher combos: three each were **LOCKED 15 Nov** (DESIGN_LOCKS "The 12 new classes' Butcher combos"), then every Butcher combo was cut to one per effect: **LOCKED 16 Nov** (DESIGN_LOCKS "The Butcher's combos cut to 12"). The 12 in all: Vanguard Wrecking Ball, Skirmisher Collapse, Binder Heavy Hook and Long Hook, Chemist Vent, Hexer Split Hex, Trapper Doorway, Duelist Private Door, Berserker Falling Door (new, in place of Scent Door), Warden Still Wall, Marksman Stopped Bullets (its first form), Field Engineer Stopwatch Sentry. The Artillerist, Infiltrator, Recon, Disruptor, Conduit and Herald have none. Hooks on four: Wrecking Ball cashes the Snare, Doorway lays an Opening, Private Door lays a Bleed, Stopped Bullets cashes the Bleed; the other eight have none: **LOCKED 16 Nov**. The six Glutton talents that lost their target have new ones: Gravity Mesh (more hits spring Doorway's trap), Time Lock (a Snare takes two cashes), Crawl Field (Heavy Hook crushes twice as hard), Stasis Wind Up (the Butcher's stacks burst at 2 on a held man), Light Stake (Stopwatch Sentry slows to a quarter), Rewind Wound (Vent's cloud twice as wide): **LOCKED 16 Nov** (DESIGN_LOCKS "Four hooks, six talents, stagger and the slice's classes"). Then the whole tree: 18 talents moved to new pieces, overlap meaning the same property of the same piece, and Fold Trap on the Binder's Gauntlet: **LOCKED 16 Nov** (DESIGN_LOCKS "The Glutton's whole tree retargeted"). Six talents are dead in the 8 Dec slice. **BUILT** as campaign rules 46 where the piece is in the sim; the talents on Shield Pin, the Hex, Bad Blood and the new classes wait for the class build (HANDOFF_LOG 15848).

- The **Glutton** makes any slot bigger, longer or faster. He never adds a fourth thing (his locked rule, L1064).
- The **Shade's old routes** stop being their own layer. Each becomes a version of a unit's slot 2 or 3, handed out by the Whisperer. Echo is dropped, it does not fit a unit.
- Claude's first mapping of the routes, not approved yet:
  - Warden: Sanctuary, Mirror
  - Medic: Last Refuge (once a fight, stops one fighter dying)
  - Skirmisher: Backstab, Breach
  - Trapper: Rift Mine
  - Marksman: Rupture, Fear
- **Marks and chains** ride inside slot 1. They are not separate layers.
- **Habits** stay as how a unit picks targets. They are never shown.
- The sim has slots 2 and 3 today (rules 11 defence moves, rules 12 boosts), but not a hard limit of three. Not built.

**Unit stats. SAID 12 Nov**
- Six stats, like Pokémon: Health, Attack, Defence, Speed, Spirit, Will. **BUILT**, rules 42, but fixed per class only, and nothing reads Spirit or Will yet.
- **The tribe sets the base.** Each tribe's fighters have their own spread, and higher tiers have better ones. "this what it makes to go up the tiers to get better pokemon logic." **LOCKED 15 Nov** as part of the 17 rewrite: class is the type, the tier is the strength. Not built.
- **Each kind of hit meets one stopping stat. LOCKED 15 Nov.** Blades: Defence. Shots: can't be dodged, Defence and a shield. Shells: no shield, Speed escapes. Spells: Speed dodges, Will softens. Gas: Speed and Will. Sound: Will only. Holds take Speed. Not built; the sim still rolls one dodge from Speed against every blow.
- **The Whisperer raises them.** When she ranks a fighter up, his stats go up with it. Not built.
- **Ranks. LOCKED 15 Nov** (DESIGN_LOCKS "Ranks for the Warden and the Hexer"). Rank 2: the class spell happens one more time, a plain hit, so the Warden bashes twice and the Hexer casts twice. Rank 3: the second slot plus a promotion by name, same class and moves. At rank 3 the fighter morphs into a new unique sprite, one per tribe and class pair, 115 in all: **LOCKED 15 Nov** (DESIGN_LOCKS "Rank 3 morphs the fighter"), replacing the Whisperer's regalia over his own sprite. Rank 2 shows nothing on the sprite: **LOCKED 15 Nov** (DESIGN_LOCKS "Rank 2 shows nothing"), so no rank wears regalia. Every rank goes when the run ends. The 17 names: **LOCKED 15 Nov**, Bulwark, Canon, Zealot, Jawsetter, Crone, Templar, Flagellant, Bombardier, Siegewright, Bladesaint, Confessor, Exorcist, Pathfinder, Plaguewright, Heretic, Martyr, Cantor (DESIGN_LOCKS "The 17 promoted names"). Not built.
- Talent trees are not stat boosters: "talent trees are unique they are not stat boosters bro". The Glutton stays out of stats.

### Layer 2: their leader causes problems

- Their leader cannot be hit while any of his fighters stands. He still fights and casts. **BUILT**, rules 43 (L1340)
- Chiefs do party wide attacks, and "busters that require 100% parry". **SAID**
- Jacob's own list of chief moves, 27 Oct (HANDOFF_LOG 11837):
  - a tank buster: parry it fully or the whole party takes it
  - a cast that strikes only your leader
  - a second cast that puts a damage over time on you
  - a Cleave now and then
  - a main attack like the Eclipse
- Each chief has five moves, and every move has more than one answer. **BUILT**, rules 3, 5 and 7 (L443)

| Chief | His five moves |
|---|---|
| Bloodridge | Cleave, Brand, Eclipse, Burning Ring, Sun Mantle |
| Skullpeak | Steel Storm, Hook, Rifle Line, Iron Advance, Overclock |
| Stoneveil | Blood Call, Basin, Leeches, Open Veins, Transfusion |
| Thundercoast | Tide, Sluice Gate, Riptide, Tidal Bore, Storm Surge |

- You see a chief's next move 6 seconds early. **BUILT**, rules 5
- "One big moment at a time." **LOCKED** (L445)

### Layer 3: your leader answers

**The parry. LOCKED and BUILT**, rules 26 (L1220 to L1224)
- The window is the last 0.2 s before a big blow lands.
- The cue is on the attacker: a gold rim while it winds up, then his whole body flashes gold. Nothing on the HUD. (L1290)
- A parry cancels the move, staggers him 2 s, fills one bar, and hits him back.
- A missed Hold is a whiff: the blow lands in full and your squad takes 50% more for 1 s.
- Normal blows are never parried. "Nothing big is unparryable."
- "parry is the ultimate move ... you cant make it a side mechanic bro". (L573)

**The parry meter. LOCKED and BUILT**, rules 22 (L1139 to L1141)
- Three bars. Every parry fills one. A miss takes none.
- Each bar you hold makes combos and cash ins 10% stronger.
- Three bars cast your chief spell, and that spends all three.

**The buttons today. LOCKED**
- Hold (the parry), Fall Back (the safe dodge), Focus Fire (all on one enemy), Unleash (the nerve attack), and a 5th button for a chief's spell. (L68, L386)
- In Jacob's saved fights he pressed Unleash and Focus Fire, and never Hold or Fall Back. (HANDOFF_LOG 13535)

**The buttons are being redrawn. SAID**
- "I don't want these four buttons to be exactly what they are right now." "These four actually make no sense."
- The four buttons become the leader's own kit. He counters the other leader's moves while the units fight.
- Jacob said the same on 26 Oct: the buttons are "not a ceiling". (HANDOFF_LOG 11789)
- His first rule for the buttons, 2 Oct: they refill so the player stays busy, and they are "signature moves taken from broken tribes". **LOCKED** (L85)

**What the new buttons are. OPEN. This is call 1, where we are now.**
- One proposal is on the table, Claude's, not approved. Jacob said "go deeper", not "lock it":
  - A parried move fills a bar with that move's own icon.
  - Pressing the bar spends the move, thrown by one of your fighters.
  - A held move can answer a different problem.
  - Three bars still buy the chief's big move.
- Jacob's test for any button: it must not be a stat with a timer. It has to solve a real problem in our own fights.

---

## 6. Champion and chief

- **The champion, stage 4.** The tribe's duel champion with a chief's power. He stands in the front row and can be hit from the start. He winds up the tribe's spell for your leader to parry. Kill him and the band breaks. **BUILT**, rules 43 (L1341)
- **The chief, stage 5.** Fought alone, in three phases: learn him, his change at half health, his desperation under 30%. Only Bloodridge's phases are built. **LOCKED** (L1342)
- "Boss fights, everything should be different. And how you tackle that is all the gameplay." **LOCKED** (L441)
- When his nerve runs out he kneels, with no timer. Hold spares him. **LOCKED** (L449)

**What rivals learn**
- A chief learns only from fights against him, one lesson per fight, on one move. **BUILT**, rules 9 (L661)
- Whatever he learned to answer, the same button now baits him into an opening. **LOCKED** (L600)
- A hunter who beats you learns one trait against your most used button. **LOCKED** (L376)

---

## 7. Presentation locks

- 2.5D battle screen. Three quarter view fighters. **LOCKED** (L20, L114)
- Parry gold belongs to the parry alone. **LOCKED** (L1303)
- One short word per cash, never a sentence. **LOCKED** (L1302)
- Gear is never drawn on the fighter. **LOCKED** (L416)
- The "three tit rule": every character and region has one look you get in one second. **LOCKED** (L82)
- No riding monsters, no mutations, never a copy of one source. **LOCKED**
- The nine cult soldiers (18 Oct) are the prologue army and the shape templates for nine classes. They never join a squad. **LOCKED 15 Nov** (DESIGN_LOCKS "The nine cult soldiers are class templates")

---

## 8. What the sim has, by rules number

| Rules | What | Jacob's verdict |
|---|---|---|
| 26 | The 0.2 s parry | Locked |
| 38 | The Pit | "they are just lunging forward thats it" |
| 40 | One blow at a time | "an eyesore" |
| 41 | Units brawl at once, leader is the parry circle | His "Go" |
| 42 | The attack table and six stats | Built on his "Go, build it" |
| 43 | Leaders, champion, Wardens in the middle | Locked |
| 44 | Grid and class shapes | Built on "Build it". Not picked over the mocks |
| not built | Locked pairs | Locked 9 Nov |

Rules 44 is not committed and not in Unity.

---

## 9. The open calls, in order

One at a time. On 14 Nov Jacob opened the classes, so we are on that one now.

0. **The classes.** Every class is a fighter, the Warden a bruiser and the Medic a mage: **LOCKED 14 Nov**. The 17 rewritten with a who beats who chart and two new per tier: **LOCKED 15 Nov**. The mage is the **Hexer: LOCKED 15 Nov**. The Guard mark became the **Hex: LOCKED 15 Nov**. Their other slots, Shield Pin, Evil Eye and Bad Blood: **LOCKED 15 Nov**. Their grid shapes: **PARKED 15 Nov** until Codex's unit combat settles. Their Butcher combos, Curse Well, Split Hex, Old Wounds and Wrong Door: **LOCKED 15 Nov**. Their habits and pair: **PARKED 15 Nov**, Covered Treatment dropped. The Healers limb became **Casters: LOCKED 15 Nov**. Ranks, rank 2 hits twice and rank 3 promotes by name: **LOCKED 15 Nov**. The 17 promoted names: **LOCKED 15 Nov**. Every piece the class change parked is settled, or parked behind Codex's unit combat. The 12 new classes' defence and boost moves: **LOCKED 15 Nov**. Their 36 Butcher combos: **LOCKED 15 Nov**, then every Butcher combo cut to 12, one per effect: **LOCKED 16 Nov**. Their marks: **LOCKED 15 Nov**. Which classes each tribe fields, rolled once: **LOCKED 15 Nov**. Chiefs stand apart from their tribe's five: **LOCKED 15 Nov**. Rank 3 morphs the fighter into a new sprite per tribe and class, and rank 2 shows nothing, so no rank wears regalia: **LOCKED 15 Nov**. Hooks on four of the 12, new targets for six Glutton talents, stagger as a plain status, and the slice keeping the old five: **LOCKED 16 Nov**. The whole Glutton tree retargeted: **LOCKED 16 Nov**. The Hex's cash word, HEXED, black with a bone edge, and every cash word at pixel size 2: **LOCKED 16 Nov**. Which classes each Glutton limb covers, by role (big guys, melee, ranged, holders, curse guys): **LOCKED 16 Nov**. Left: the morphs' looks, every number, and building it, which is Codex's job (HANDOFF_LOG 15562). *We are here.*
1. **The leader's buttons.** What the four are. Phase, Tectonic Shift and tool 3 are locked; tool 4 is open.
2. **Where the Shade goes.** Her tricks sit on the old four buttons.
3. **How a road fight ends.** "All die, next stage" (**SAID**) against nerve breaks (the locks).
4. **The kit split.** Which chief moves the road and the champion show, and which are saved for the chief.
5. **The other tribes' roads.** Only Bloodridge's fighters carry their chief's moves.
6. **The look of the unit fight.** Pairs are locked but not in the sim. Codex's mocks: blocks read but dead, free fighters became one ball, pairs lost the bands. Grouping pairs by band was ordered on 10 Nov (HANDOFF_LOG 15077).
7. **Bands of bodies and tribe against tribe.** Jacob called tribe against tribe "actually great". Not locked.
8. **The leader's clips.** The parry and each button.

---

## 10. Things in the locks that disagree

These need a call each. They are not settled by this file.

- **A missed Hold.** The 1 Nov whiff says the blow lands in full. Older lines still let a Hold soak a swing. (L1223 against L1089)
- **Hitting the chief.** He cannot be hit while his band stands, but many locked answers are "Focus Fire on him". (L1340)
- **Captures.** Nobody is captured since rules 11, but Grudge and a rank rule still mention captures. (L827 against L316, L1129)
- **Execute.** Deleted, but older chief sections still show it. (L756 against L449)
- **Nerve.** No one line says how a road fight ends on nerve now.
- **Road length.** Three rows then the chief, but also a fourth row. (L322 against L1348)
- **Grid.** One line says not built, a later one says built. The sim has it. (L1360 against L1379)
- **Not written in the locks at all:** The Pit, one blow at a time, the attack table and its stats, rules 41, the Pal duos, Jacob's 25 Oct calls.

---

## 11. Rejected. Do not pitch these again

**Standing, from the locks and the log**
- A game centred on studying a learning enemy and pressing Hold over and over. (HANDOFF_LOG 10868)
- Chiefs using your own buttons back at you: "copy paste of the same formula". (L441)
- A boss you can only kill in one fixed order. (L441)
- Four buttons that are pure benefits: they "make the game easier". (HANDOFF_LOG 11893)
- Number rank tables: "generic". (HANDOFF_LOG 11939)
- Gear slots like an RPG: "we already lost". (HANDOFF_LOG 11766)
- Three layers of combos at once.
- Pal picks that are number tweaks: "these are not combos". (L893)
- Too much text: "its like a encyclopaedia". (HANDOFF_LOG 12819)
- Free targeting: "wahts the point of having traits when unis can behave how ever thy want". (L1355)
- Deck builders, a collector loop, hit chances on screen.

**From 10 and 11 Nov**
- A stripped five against five: "what is player doing then??"
- Five against waves.
- The leader as Batman: "he's taking all the credit and the units are again left".
- The leader as a Hades character: "by far the worst".
- Four kinds of parry: too heavy.
- Ward, Cleanse, Call, Burst: "generic as shit".
- Copying Kingdom Rush's three buttons. It was an example of a kit that fits its own game.
- Convert: "speed runners will run convert runs and kill my game".
- Tug of war, a shield wall parry, one spotlight duel, a gang as a health bar, three small fights.
- Formation calls, retiring buttons, the mind link carrying burns: "dogshit".

**From 12 Nov**
- Any button that counters one kind of boss move. Gaze, Seize, Graft, Decree, then Silence, Devour, Invert, Ordain: "they counter a problem again which gives radical advantage over the chief speedrunners will know wht to use and break my game".
- A kit that reads like a boss's small kit. It has to look like a species that runs the universe.
- Buttons designed with no chief fights under them. The kit has to hold across every tribe's boss fight.
- Talent trees as stat boosters.

**From 14 Nov**
- Tank and healer classes: "we don't need tank and healer". Codex's duel and support mocks: "WTF is happening here ?"
- Claude's first fix, a Warden whose blocks load a slam and a Medic who finishes the most hurt enemy: "Change it".
- Adding Fire Emblem classes on top of the 17: "Not actually integrating fire random classes into it".
