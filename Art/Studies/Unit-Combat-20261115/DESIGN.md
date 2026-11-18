# Unit combat study: the eight layers (Claude, 16 Nov 2026)

Jacob, 15 Nov night: "can you build all the layers ?? while im away", after the research report
`reports/Game combat AI and animation.md`. This file is what the build follows. Every number is a starting value.

The contract (from the research): no decision type without a clip, pose or word, and no reaction to anything that has not
been on screen for the fighter's reaction time. Decide, announce, act. The sim owns timing; clips are cut to its rows.

## Layer 1. Frame data rows with tags
Every move is a row: the clip frame per tick, contact ticks with hit kind and share, and tags per tick:
TELL (wind up the viewer reads, at least 5 ticks before any big contact), CANCEL (the move can still be dropped:
feints live here), ACTIVE (contact), VULNERABLE (recovery after the last contact: a punish target), STEP (root motion).
Moves per fighter: quick (a short cut of the combo), heavy (the class move, whole combo), feint (tell then cancel back),
boost moves (pin, twin, bad blood), guard (hold), dodge (clip), block (clip), roar, get up, run (both facings).
Facing is part of the row: each tribe has a sheet per facing with its own frame timing.

## Layer 2. Senses and belief
Each fighter keeps a belief per other fighter: what he was doing ReactTicks ago (Speed and personality set it, 4 to 7
ticks). He reacts to what he believes, never the truth. Attention: his target and the one man swinging at him are seen
at ReactTicks; everyone else at double. A feint is believed to be a real attack until its cancel shows.

## Layer 3. The attack referee (Kingdoms of Amalur, Game AI Pro ch. 28)
One referee per target. Grid capacity 12 (class grid weights: Warden 6, Skirmisher 4, Trapper 4, Marksman 0, Hexer 0),
attack capacity 10 (move weights: heavy 6, quick 3, shot 3, curse 4, pin 2). A fighter with a slot stands on the outer
ring; he steps into the inner ring only with a granted attack, and gives the grant back the moment the attack launches.
Refused fighters must visibly wait: circle, guard, feint, roar. Requests are served in a fixed order (priority, then id).
Mark cash contracts: when a mark lands, the referee names the one best casher; he gets first claim on the next grant.

## Layer 4. Strict priority list per class, with blind spots
The first relevant behaviour wins (Halo 2). Scoring is only for who and where. Each class ignores something:
- Warden: never dodges, never reads feints (he braces instead). Priorities: pin, cash a hex, punish, cover a diven ally,
  strike if granted, brace and wait.
- Skirmisher: ignores her own health until Wavering (reckless). Priorities: cash, punish a whiff, dive the shooter,
  strike if granted, circle and feint.
- Trapper: snares only melee men. Priorities: snare the fastest threat, punish, strike if granted, wait.
- Marksman: tunnel vision, ignores melee men until within 120 px. Priorities: cash a snare, shoot his target, back off.
- Hexer: always curses the biggest crowd or the man her Warden is on. Priorities: curse, bad blood, flee a diver.

## Layer 5. Stimulus interrupts (Halo 2)
Events open short windows (1 to 2 s) where special reactions are legal and come first:
ally fell (vengeful: roar and charge the killer; cautious: step back; Hexer: curse the killer), enemy whiffed a heavy
(nearest punishes), mark cashed (allies pile on if granted), a heavy wind up seen aimed at me (dodge, block or brace).

## Layer 6. Memory inside one fight (Killer Instinct, F.E.A.R.)
Per fighter: outcome counts per own move against each enemy (landed, dodged, blocked, punished); habit counts per enemy
(what he did after my wind up); a grudge (who hurt me most, who killed my friend). A move punished twice against a man
drops below its fallback against him (the Warden pins before he bashes a dodger). A known dodger gets a feint first.

## Layer 7. Nerve ladder with personal breaks (Battle Brothers, Darkest Dungeon, Halo 3)
States: Confident, Steady, Wavering, Breaking, Broken. One step per event, checked against Will. Down: ally killed near
(two steps if he was stronger), heavy wound, a cash on me, outnumbered. Up: a kill, an ally's cash near me.
Rally (a kill by our side near him) returns him to Wavering, never Steady. Broken branches by personality:
flee (turn and run), freeze (cower), rage (roar, then attack the nearest with no referee), shield (stand by our weakest).
Each state has a stance; each break has its own act and word.

## Layer 8. Tribe lessons across fights (Alien: Isolation)
A list of locked counters per rival tribe. After a fight the log is read for what the player's side did that worked;
the matching counter unlocks (one per fight). Counters: guard the shooter, spread out vs the curse, kill the Hexer first,
hold the line vs dives, bait the bash. The next fight starts with the lesson visible (formation and one line).

## Left out
Lookahead search (the research: last, if at all). Motion matching and blending (need 3D rigs).
