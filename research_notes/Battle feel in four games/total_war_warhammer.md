# Total War: Warhammer I, II and III: how battle feel and unit play are built

Reading key for every line below.
DEV = Creative Assembly (CA) staff or an official CA post or patch note.
PRESS = a journalist reporting or paraphrasing CA staff.
COMMUNITY = forum post, player test, guide, fan wiki or mod. Treat its numbers as unverified.
SUMMARY ONLY = the page refused my fetcher (totalwar.com gave 429, pcgamer.com served an empty page, fandom gave 402, wiki.gg gave 401, Bell of Lost Souls gave 403), so the fact comes from the search engine's summary of that page, not from my own reading of it.
MY INFERENCE = mine, not from any source. These lines carry the translation to Kindred Siege's 5 v 5 pixel sprites.

## 1. Weight scaled to power: mass, charge, collision and knockback

### Takeaway
In Total War: Warhammer (TWW) weight is built from three separate levers: a hidden per entity mass stat that decides who gets shoved or knocked down, a damage burst on contact (impact damage, and in WH3 a capped "collision attack"), and a short charge bonus buff after contact. CA's main public tuning of this (WH2 patch 1.12.1, November 2021) took the charge buff out of the collision maths so that braced spears could visibly stop horses, and WH3 makes big single bodies lose stats as they lose health.

### Cited Findings
- WH1 dropped the old unit versus unit combat model and tracks every entity on its own, because the old model did not suit heroes and giants. In a preview demo a giant could grab one soldier out of a unit and throw him. DEV via PRESS (Scott Pitkethly, lead programmer), SUMMARY ONLY. ([GamesBeat](https://gamesbeat.com/?p=107473); [PC Gamer, six new things](https://www.pcgamer.com/six-new-things-we-noticed-while-playing-total-war-warhammer))
- WH2 patch 1.12.1 cavalry update went live on 3 Nov 2021. Charge bonus no longer feeds collision damage. The collision damage normaliser went from 0.7 to 0.8. Bracing multipliers went up to widen the gap between bracing and counter charging. DEV, SUMMARY ONLY (the CA blog returned 429 three times). ([totalwar.com, Patch 1.12.1 and Cavalry Update](https://totalwar.com/?p=15220))
- Same update: an entity no longer makes a charge attack if a friendly unit blocks its charge path. This mainly stops infantry back ranks charging while the front rank is already engaged. CA's stated intent, as paraphrased: a charging cavalry unit should be favoured to win even when the infantry reacts with its own counter charge. Before the change, charging cavalry took more hits than intended because infantry formations have more bodies, and dealt too little back. PRESS paraphrasing CA. ([PCGamesN](https://www.pcgamesn.com/total-war-warhammer-2/cavalry-overhaul-beta))
- Beta preview: charging into braced infantry now does much lower impact damage than charging unbraced infantry. PRESS, SUMMARY ONLY. ([Bell of Lost Souls](https://belloflostsouls.net/?p=388319))
- Players describe post patch collision damage as driven only by mass and speed, with charge bonus applied afterwards as a temporary melee attack and damage boost once the bodies stop flying. COMMUNITY, SUMMARY ONLY. ([Steam WH3 discussion](https://steamcommunity.com/app/1142710/discussions/0/4357872738329493783))
- Entity mass is a hidden stat that CA tunes in patch notes. Example: WH3 update 3.1 moved some Bretonnian mounts from 2,200 to 2,300 entity mass. DEV patch note via PRESS, SUMMARY ONLY. ([SI, update 3.1 patch notes](https://videogames.si.com/news/total-war-warhammer-3-patch-notes-update-3-1))
- WH1 era knockback chance: roughly 10% times the attacker to target weight ratio, and no knockback at all below a 35% ratio. COMMUNITY (2016 Steam guide), SUMMARY ONLY. ([Steam guide, Some notes](https://steamcommunity.com/sharedfiles/filedetails/?id=1114878285))
- Stacked mass bonuses cannot exceed 10 times the base mass. A braced unit's mass is fixed at 600 when a small unit collides with it. Knockback and knockdown need the attack animation to carry motion; without motion the target only staggers ("stiffness"). COMMUNITY (NamuWiki), SUMMARY ONLY. ([NamuWiki, Total War: Warhammer basic information](https://en.namu.wiki/w/%ED%86%A0%ED%83%88%20%EC%9B%8C:%20%EC%9B%8C%ED%95%B4%EB%A8%B8/%EA%B8%B0%EB%B3%B8%20%EC%A0%95%EB%B3%B4))
- A fan wiki says collision size priority can matter more than the raw mass difference. COMMUNITY, SUMMARY ONLY. ([Fandom, Units](https://totalwarwarhammer.fandom.com/wiki/Units))
- A WH3 Steam discussion says braced mass scales with rank depth and cites a 4x bracing multiplier and 2x charge defence multiplier from the 1.12.1 change. Another user says WH3 now shows mass in game. COMMUNITY, SUMMARY ONLY, unverified. ([Steam WH3, Entity Mass](https://steamcommunity.com/app/1142710/discussions/0/591781130180569696/))
- WH3 (2024, after update 5.1) gives chariots and some monsters "collision attacks": a capped number of damage hits per charge written X/X, with about 100 weapon strength per hit for chariots and most multi entity chariots set to 1/1. Once the cap is used, every further collision deals plain impact damage. Overkill on a collision hit is wasted: 100 damage and 10,000 damage do the same to a 90 hp model. Killing a model lets impact damage pass to the next one. Player tests: Chaos chariots did about 50% more damage than same price shock cavalry over a 5 second frontal charge (about 3k against 2k). COMMUNITY (player tests on CA's forum, no CA staff replies). ([CA forum, new collision attack paradigm](https://community.creative-assembly.com/total-war/total-war-warhammer/forums/10-battles/threads/5938-thoughts-on-the-new-collision-attack-paradigm?page=1))
- A community poster quotes the 5.1 notes as adding collision attacks to several large monsters and reducing the max splash attacks of most monsters. They think the Dread Saurian and Khemrian Warsphinx got them. The Elemental Bear still has max splash 12. Their test: a Dread Saurian got 260 kills into six Empire Swordsmen units; a YouTuber got over 900 into Clanrats. A commenter says collision attacks use the model's weapon strength. COMMUNITY. ([CA forum, Dread Saurian thread](https://community.creative-assembly.com/total-war/total-war-warhammer/forums/10-battles/threads/6092-the-changes-to-the-dread-saurian-should-be-expanded-to-the-other-colossal-monsters?page=1))
- WH3 Hotfix 5.0.5 (13 June 2024) cut "Max Entities Hit by Splash Attack" from 6 to 5, to concentrate damage on fewer targets. Which units it covers was not clear in the summary. DEV patch note via PRESS, SUMMARY ONLY. ([hard-drive.net, patch notes June 13](https://hard-drive.net/author/grandemil-reyes/page/92/))
- WH3 "wounded state": single entity units (lords, heroes, monsters) get stat debuffs as their health drops and can be healed out of it. CA also lowered single entity and tower damage at small unit sizes. Battle designer James Martin's example: a tower doing 500 damage to a 2,000 hp unit on ultra size; on small size that unit has 500 hp and would be one shot. Scaling for small and medium sizes had always been a little off, with one cannon on small doing the work of four. DEV via PRESS (22 June 2021). ([GameWatcher](https://gamewatcher.com/news/total-war-warhammer-3-lords-monsters-debuff-small-unit-size-scaling))

### Inferences
- MY INFERENCE: TWW's weight is three levers, not one number. For Kindred Siege that maps cleanly onto integer sim state: (a) a mass class per fighter, with a ratio gate deciding shove, knockdown or only stagger (TWW's "below 35% ratio, no knockback" is a usable shape); (b) one capped damage burst on contact, with overkill wasted so a beast cannot delete a whole line in one hit; (c) a short timed buff after contact.
- MY INFERENCE: the "knockdown needs motion" rule is a sprite rule. A big hitter's attack clip must visibly travel forward (a lunge, a stomp step) for the knockdown to read; a swing in place should only cause a hit stagger frame on the target.
- MY INFERENCE: TWW's brace (posture multiplies the defender's mass and blocks the charge buff) is the closest TWW cousin to Kindred Siege's leader parry. A parried big hit could be shown the TWW way: no knockback on the defender, the attacker's own momentum stops dead.
- MY INFERENCE: the wounded state gives big single bodies visible decline. On a 32 px beast this could be a second idle (lower head, slower breath) at 50% and 25% health, matching WH3's 75/50/25 health ticks.
- MY INFERENCE: the splash cap (6 cut to 5 targets per swing) is how CA keeps monsters heavy but not absurd. At 5 v 5 the equivalent is a cap of one or two victims per big swing.

### Gaps
- No official mass table. I could not find typical mass values for infantry, cavalry or monsters from a CA source; the only hard number is the 2,200 to 2,300 mount change.
- A "70 damage cap" on collision attacks is claimed in a forum post that the search engine summarised, but I could not trace it to a page, so it is unverified.
- No official WH3 knockback formula; the 10% times weight ratio rule is a WH1 era player guide.
- The CA 1.12.1 blog itself could not be opened, so the 0.7 to 0.8 normaliser and the bracing change are from the search summary of that page.

## 2. Hits that travel and land on a body: matched combat, melee contact, projectiles

### Takeaway
TWW resolves melee one entity at a time "on an animation basis": most fighting is single body attack and hit react clips, while true two body matched kills are authored for specific pairings (lord or hero against monstrous infantry, monster against monster) and kept for major moments. Every projectile is physically simulated, and the main readability risk CA names is line of sight that looks clear on screen but is blocked in the engine.

### Cited Findings
- Battle designer Simon Mann defined a matched animation as one animation shared by two entities. PC Gamer reported that WH1 still has matched animations but keeps them for major moments or kills. DEV via PRESS, SUMMARY ONLY. ([PC Gamer, six new things](https://www.pcgamer.com/six-new-things-we-noticed-while-playing-total-war-warhammer))
- Pitkethly said the team moved to individual combat resolution on an animation basis, which he called a snappier system with more action; each entity animates and reacts on its own, so fights look more visceral. DEV via PRESS, SUMMARY ONLY. ([PC Gamer, six new things](https://www.pcgamer.com/six-new-things-we-noticed-while-playing-total-war-warhammer))
- A preview separates matched animations (two characters played together) from synced animations (one character reacts to the other's action), with synced layered on top of matched. PRESS, SUMMARY ONLY; I could not confirm which PC Gamer article this came from. ([PC Gamer, heroes, quests, mounts and magic](https://www.pcgamer.com/total-war-warhammer-heroes-quests-mounts-and-magic/2))
- KitGuru described paired two person animations with exchanges of blows and killing strikes. PRESS, SUMMARY ONLY. ([KitGuru](https://www.kitguru.net/?p=252671))
- WH2: animation sets are picked per situation. For fights on castle walls the team authors or culls specific sets so motion does not look wrong. The team also authors matched fighting animations between specific entities to capitalise on particular situations. DEV (Pitkethly). ([Game Developer, designing WH2 for massive battles](https://www.gamedeveloper.com/design/designing-i-total-war-warhammer-ii-i-to-handle-tons-of-units-and-massive-battles))
- WH2 example of a monster on monster interaction: in Carnasaur against a grounded dragon, the dragon sometimes tries to fly off with the Carnasaur, gets pulled down and is headbutted. DEV (Al Bickham, CA; the fetch summary printed "Mark Bickham"). ([Game Developer](https://www.gamedeveloper.com/design/designing-i-total-war-warhammer-ii-i-to-handle-tons-of-units-and-massive-battles))
- Battle logic and animation run separately from display, and the logic computes future states over several frames so complex interactions do not hurt frame rate. DEV (Pitkethly). ([Game Developer](https://www.gamedeveloper.com/design/designing-i-total-war-warhammer-ii-i-to-handle-tons-of-units-and-massive-battles))
- Every projectile is physically simulated. Accuracy depends on the shooter's quality and the target's situation. The checks are expensive at scale, so the team rations how often they request them. DEV (Pitkethly). ([Game Developer](https://www.gamedeveloper.com/design/designing-i-total-war-warhammer-ii-i-to-handle-tons-of-units-and-massive-battles))
- The biggest line of sight problem is the gap between what the player sees and what the engine computes: a low hill about the height of a bus stop can block riflemen who look like they have a clear shot. DEV (Pitkethly). ([Game Developer](https://www.gamedeveloper.com/design/designing-i-total-war-warhammer-ii-i-to-handle-tons-of-units-and-massive-battles))
- Players report special animations for a lord or hero impaling monstrous infantry, and for flying monsters grabbing and dropping units, and say matched animations are limited overall. COMMUNITY, SUMMARY ONLY. ([Steam WH1 discussion](https://steamcommunity.com/app/364360/discussions/0/1290690669214676400))
- A WH3 mod describes sync kills as special cinematic animations that play when certain units fight or kill each other, says some had been broken since WH1, and restores them for Rotting Prometheans, Rotting Leviathans and Arachnarok mounted lords. MOD, not CA. ([Sync Kills and broken animations FIXED](https://catalogue.smods.ru/archives/426418))
- Hit resolution: CA's Feature Focus gives a base melee hit chance of 35%, plus the attacker's melee attack, minus the defender's melee defence, clamped between 8% and 90%, then a roll of 1 to 100. DEV, SUMMARY ONLY; this post may be written for Total War: Pharaoh rather than TWW. A fan guide gives the same 35 + MA minus MD formula with 8 to 90 bounds for WH3, and overhaul mods change the base to 15 or 20. ([CA Feature Focus #2: Damage, part 1](https://totalwar.com/?p=19917); [Screen Rant WH3 unit stats guide](https://screenrant.com/total-war-warhammer-3-unit-stats-guide/))

### Inferences
- MY INFERENCE: TWW has two tiers of melee presentation. Tier one is cheap and constant: one body plays an attack clip, the other plays a hit react or death clip. Tier two is rare and authored: one shared two body clip for a specific pairing. At 5 v 5 with pixel sprites, tier two is affordable as one combined sprite clip that replaces both fighters for its length (the same trick as a fighting game throw).
- MY INFERENCE: good candidates for Kindred Siege's tier two: a leader's parry counter on a beast's big hit, a chief kill, and beast against beast grapples. Everything else stays tier one.
- MY INFERENCE: TWW's "logic runs ahead of display" design suits Kindred Siege's deterministic sim. Presentation can read a few ticks ahead and start a kill clip before the hp reaches zero, so the blow visibly lands at the moment the sim kills.
- MY INFERENCE: the line of sight warning applies directly. If the sim blocks an arrow or a parry, the screen must show why (a body in the way, a raised shield); never let a block happen that looks clear on screen.
- MY INFERENCE: for projectiles on small sprites, a visible arc plus a landing puff or flash on the target's body sells "travel and land"; TWW's physics is not needed, only the read.

### Gaps
- No CA source found on how a matched kill is chosen at run time (probability, health thresholds, which pairs qualify), or how many matched clips exist per unit.
- No source found on frame timings, hit stop or freeze frames in TWW melee. I found nothing saying TWW uses hit stop.
- The Elliot Maren (lead animator) BAFTA workshop on TWW animation exists (posted 13 Nov 2021), but its content is not public in what I could read. ([GameAnim](https://www.gameanim.com/?p=66939))

## 3. Readability: unit cards, banners, health bars, who is winning

### Takeaway
TWW reads a melee through the floating unit banner over each unit (health bar plus a leadership or morale state that flashes when wavering), tooltips that name the cause, and in WH3 two health bar upgrades: ticks at 75/50/25% and "lazy" bars that leave a draining trail after a big hit. I found no CA source on unit card layout or a "who is winning" indicator.

### Cited Findings
- WH3: unit health bars in the battle space have ticks at 75%, 50% and 25% so the player can see when health dependent abilities are about to trigger. DEV (CA quality of life blog, Nov 2021), SUMMARY ONLY. ([totalwar.com, QoL updates in WH3 battles](https://totalwar.com/?p=16273))
- Same period: WH3 added "lazy" health bars to show incoming damage. A guide describes them as highlighting the new value and draining to it over time, so sudden big bursts of damage stand out, without changing how damage is applied; a review describes a visible trace left as the bar falls. PRESS and COMMUNITY, partly SUMMARY ONLY. ([GamingOnLinux](https://gamingonlinux.com/2021/11/total-war-warhammer-iii-gets-lots-of-improvements-for-battles); [Gamepressure WH3 review](https://www.gamepressure.com/editorials/reviews/total-war-warhammer-3-review-quality-of-war/z9522))
- The same WH3 batch: healing became a percentage of max health per second instead of a flat value; hotkeys for idle units; Three Kingdoms style formation options; a full unit browser page. PRESS. ([GamingOnLinux](https://gamingonlinux.com/2021/11/total-war-warhammer-iii-gets-lots-of-improvements-for-battles))
- CA also said unit abilities and stats would be easier to read than in earlier titles. PRESS, SUMMARY ONLY. ([Pro Game Guides](https://progameguides.com/total-war-warhammer-iii/total-war-warhammer-3-will-include-a-host-of-quality-of-life-improvements/))
- WH1: the grey bar under the unit banner is morale; it drops faster or slower depending on the unit's leadership, and the unit flees when it hits zero. COMMUNITY, SUMMARY ONLY; the exact thread among two WH1 results is uncertain. ([Steam WH1 discussion](https://steamcommunity.com/app/364360/discussions/0/133256689833181836))
- A wavering unit's flag flashes. COMMUNITY guide, SUMMARY ONLY. ([Gamepressure, leadership and fatigue](https://www.gamepressure.com/totalwarwarhammer/leadership-and-unit-fatigue/z38a4f))
- WH3 tooltips can name the cause, for example "Wavering, General Wounded" after the army leader is taken out, though some players say they do not see it. COMMUNITY, SUMMARY ONLY. ([Steam WH3 discussion](https://steamcommunity.com/app/1142710/discussions/0/3832045766778568866))
- Player complaints: in WH3 all unit info can be hidden unless Space is held; a unit's total hp is tied to each model's health rather than the model count, so judging how big a unit still is from the bar is unintuitive. COMMUNITY, SUMMARY ONLY. ([Steam WH3 discussion](https://steamcommunity.com/app/1142710/discussions/0/3365901931426620282); [Reddit via redlib](https://redlib.belloworld.it/r/Volound/comments/1fne3ks/health_potentially_misunderstoodthrown_under_the))
- Readability at the faction level: battle designer Simon Mann said the team treats each race as its own Total War game and wants to show off that diversity. DEV via PRESS, SUMMARY ONLY; exact PC Gamer article uncertain. ([PC Gamer, battle designer on blood and magic](https://pcgamer.com/total-war-warhammers-battle-designer-on-blood-and-badass-magic))

### Inferences
- MY INFERENCE: for a 5 v 5 sprite fight, one small bar per fighter with a lazy trail (a lighter chunk that drains over about half a second) is the single cheapest TWW trick to make a big hit read as big. A parried hit could show no trail at all.
- MY INFERENCE: put ticks only where Kindred Siege state changes (for example nerve break or a wounded idle), so a tick crossing means "something just changed", as WH3 does for ability triggers.
- MY INFERENCE: in TWW you judge who is winning a melee by comparing how fast two bars drain and whether a banner is flashing. At 5 v 5 the same read works per fighter; a flashing name plate or bar for a fighter about to break is the direct port.
- MY INFERENCE: name the cause in a short pop ("Leader down", "Braced") the way WH3 tooltips name "General Wounded", but on screen rather than in a hover, because nobody hovers in an auto battler.

### Gaps
- No CA source on unit card layout in battle, or on a balance of power bar. I believe the TWW battle HUD shows one, but I did not find a source this session.
- No CA source on how leadership is shown in WH3 banners beyond the player reports above.
- The CA QoL blog itself could not be opened, so I could not check whether CA uses the word "lazy".

## 4. Every hit changes the state: deaths, wavering, breaking, routing, leadership

### Takeaway
Every casualty feeds leadership, and units move down a visible ladder: steady, wavering (drops its order, flag flashes), broken and routing (after a short grace below zero), possibly rallied by a nearby lord, and shattered for good after repeated breaks. Faction rules swap the outcome (undead crumble, WH3 daemons take damage instead of fleeing), fear and terror attack leadership directly, and lords lift nearby units while their wounding drags them down.

### Cited Findings
- Leadership falls with casualties in the battle so far and in the last minute; nearby generals and heroes raise it. PRESS guide. ([Screen Rant](https://screenrant.com/total-war-warhammer-3-unit-stats-guide/))
- A wavering unit drops its current command; a unit routs once its leadership has stayed below zero for 2.5 seconds. COMMUNITY guide, SUMMARY ONLY. ([Gamepressure WH1](https://www.gamepressure.com/totalwarwarhammer/leadership-and-unit-fatigue/z38a4f); [Gamepressure WH2](https://www.gamepressure.com/total-war-warhammer-2/leadership-and-exhaustion/zba33d))
- Broken units can rally; the lord's Encourage ability raises the leadership of nearby routing units; a unit will not rally without a lord within range. COMMUNITY, SUMMARY ONLY. ([ludo.guide, morale and leadership](https://www.ludo.guide/guide/total-war-warhammer-3/morale-leadership))
- Shattered units cannot be rallied. A unit can rout three times and shatters the next time it breaks. COMMUNITY wiki, SUMMARY ONLY. ([Gamepedia, Break](https://totalwarwarhammer.gamepedia.com/Break))
- WH3 daemonic units have Daemonic Instability: at low leadership they take damage over time instead of routing. Undead units do not rally but may stop crumbling. COMMUNITY, SUMMARY ONLY. ([ludo.guide](https://www.ludo.guide/guide/total-war-warhammer-3/morale-leadership))
- Fear: a constant, slight leadership penalty on nearby enemies, given on the WH1 wiki as minus 8 leadership within 30 m, not stacking. Terror: when the unit charges it has a chance to cause a large temporary leadership drop on enemies in an area; units that cause terror are immune to fear and terror. A player says a unit must be at 12 leadership or lower to be terror routed. COMMUNITY, SUMMARY ONLY. ([Gamepedia, Break](https://totalwarwarhammer.gamepedia.com/Break); [Steam WH3 discussion](https://steamcommunity.com/app/1142710/discussions/0/6861841362664331684))
- Lord death: WH2 players say units waver but hold at low leadership after a lord dies; WH3 shows a "General Wounded" wavering state. COMMUNITY, inconsistent reports. ([Steam WH3 discussion](https://steamcommunity.com/app/1142710/discussions/0/3832045766778568866))
- WH3 wounded state for single entities (stat debuffs as health falls, healable). DEV via PRESS. ([GameWatcher](https://gamewatcher.com/news/total-war-warhammer-3-lords-monsters-debuff-small-unit-size-scaling))
- WH3 survival battles: to keep a horde feel without more bodies, enemy infantry entity counts per unit were halved and the number of infantry units the AI sends was doubled, keeping entities on screen about the same. Enemy health was cut but enemy damage was not, which the designer admits breaks consistency with normal battles. DEV (James Martin, battle gameplay designer, 15 Sep 2021). ([Game Developer deep dive, survival battles](https://www.gamedeveloper.com/design/deep-dive-how-survival-battles-balance-the-demonic-chaos-of-i-total-war-warhammer-iii-i-))
- Bickham: small moments are what make a battle feel like a real, dynamic fight rather than complex maths. DEV. ([Game Developer](https://www.gamedeveloper.com/design/designing-i-total-war-warhammer-ii-i-to-handle-tons-of-units-and-massive-battles))

### Inferences
- MY INFERENCE: the 2.5 second grace below zero before a rout is a readable "about to break" window. In Kindred Siege's nerve break, that window is where the leader could step in, and a flashing state on the fighter tells the player it is happening.
- MY INFERENCE: each ladder step wants its own sprite read: wavering as a flinch or a glance backward in the idle, broken as a turn and run clip, shattered as fleeing off the stage edge. Rallied could reuse the idle with a short flash.
- MY INFERENCE: faction swaps of the break outcome (crumble, take damage instead of fleeing) are a cheap way to make tribes feel different in the same fight, since the trigger is shared and only the result changes.
- MY INFERENCE: the survival battle trick (half the bodies per unit, twice the units, lower health, same damage) shows CA will bend consistency for feel. At 5 v 5 the lesson is the opposite direction: fewer bodies means each death must be shown in full.

### Gaps
- No official CA leadership formula or modifier table for any of the three games; all thresholds above are community figures.
- I found no source on whether dead models stay on the field or how long corpses persist, though I believe they do. Not sourced.
- No source on how a line visibly breaks (formation loosening, models turning) beyond the routing state itself.

## 5. Rare big moments: lord and monster kills, big spells, camera, sound

### Takeaway
CA says outright that showpiece interactions are kept rare so they feel special and epic. The big moments are authored matched kills and monster on monster grapples, plus vortex and bombardment spells that cost the most magic and, when overcast, carry a random path risk. I found no CA source on camera or sound techniques for these moments.

### Cited Findings
- On monster on monster moments like the dragon trying to carry off a Carnasaur, Al Bickham said they are kept rare so they "feel special and epic when they do happen". DEV. ([Game Developer](https://www.gamedeveloper.com/design/designing-i-total-war-warhammer-ii-i-to-handle-tons-of-units-and-massive-battles))
- Matched animations are kept for major moments or kills. PRESS, SUMMARY ONLY. ([PC Gamer, six new things](https://www.pcgamer.com/six-new-things-we-noticed-while-playing-total-war-warhammer))
- Vortex spells are the group able to deal the highest damage and they have the highest Winds of Magic cost. COMMUNITY guide, SUMMARY ONLY. ([Gamepressure, types of spells](https://www.gamepressure.com/totalwarwarhammer/types-of-spells-and-how-to-cast-them/z88ef5))
- A changelog says moving vortexes no longer wander in a fully random direction and instead follow a roughly straight path, while overcast versions spawn three vortexes that move randomly, making them high risk, high reward. SUMMARY ONLY; I could not confirm whether this is a CA or a mod changelog. ([Steam changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/2149749957?p=2))
- Comet of Casandora (Lore of Heavens): power cost 13, cooldown 49 per a fan source; an early WH1 tester saw under 1,000 damage on a skeleton unit, while later players call it the closest thing to a nuke. Final Transmutation: power cost 18 per a fan wiki, lore assignment disputed. COMMUNITY, SUMMARY ONLY. ([Alcast, Lore of Heavens](https://alcasthq.com/warhammer-lore-of-heavens-magic-type/); [Alcast, Lore of Death](https://alcasthq.com/warhammer-lore-of-death-magic-type))
- WH3 survival battles build to a boss fight against a greater daemon, with the map unlocking in stages that point the player at where the finale will happen. DEV (James Martin). ([Game Developer deep dive](https://www.gamedeveloper.com/design/deep-dive-how-survival-battles-balance-the-demonic-chaos-of-i-total-war-warhammer-iii-i-))
- WH3 reduced single entity and tower damage at small unit sizes so one hero or tower does not one shot whole units. DEV via PRESS. ([GameWatcher](https://gamewatcher.com/news/total-war-warhammer-3-lords-monsters-debuff-small-unit-size-scaling))

### Inferences
- MY INFERENCE: TWW rations spectacle by authoring and by cost, not by a cooldown on the presentation. For Kindred Siege: at most one authored two body clip per fight (the parry counter or a chief kill), and big spells gated by a resource so they cannot chain.
- MY INFERENCE: the vortex rule pairs readability with risk. A straight path is a telegraph the eye can follow; the overcast random path is a gamble the player chose. A Kindred Siege big cast could show its path as a floor line first, matching the floor ring language already used for spell effects.
- MY INFERENCE: survival battle staging (stages unlock, then a boss) is the campaign scale version of the same rarity rule: the big moment is placed at the end, signposted in advance.

### Gaps
- No CA source found on camera behaviour for big moments (TWW is a free RTS camera; I found nothing on cinematic cuts, shake or slow motion).
- No CA source found on sound design for lord kills, monster hits or spells.
- No developer statement found specifically about keeping spells rare.

## 6. The battle as the player's build paying off: composition and counters

### Takeaway
TWW counters come from stat tags rather than hard rock paper scissors: bonus vs large, bonus vs infantry, armour piercing, charge defence vs large, brace, and leadership immunities. CA's cavalry patch tuned these so the right choice visibly pays (braced spears stop a charge, unbraced ones get flattened), and each race is built as its own toolset so composition itself is the plan.

### Cited Findings
- Anti large units have an edge against targets at least as big as a horse; bonus vs large raises both melee attack and damage against large targets. COMMUNITY fan wiki. ([Gamepedia, Spearmen (Empire)](https://totalwarwarhammer.gamepedia.com/Spearmen_(Empire)))
- Spear units get a higher bonus vs large but are much worse against non large targets; halberds have armour piercing and a usually smaller bonus vs large. COMMUNITY (mod author's pinned note), SUMMARY ONLY. ([Steam mod discussion](https://steamcommunity.com/workshop/filedetails/discussion/1334469565/1745605598715710897))
- Charge defence vs large negates the charge bonus of large attackers (example: Bretonnian Spearmen at Arms). COMMUNITY fan wiki. ([Gamepedia, Spearmen at Arms (Shields)](https://totalwarwarhammer.gamepedia.com/Spearmen-at-Arms_(Shields)))
- Bonus vs large is not armour piercing and is usually larger than armour piercing bonuses; Spearmen at Arms have 15 bonus vs large, which an overhaul mod cut to 13 after players called it too high. COMMUNITY and MOD. ([Steam WH2 discussion](https://steamcommunity.com/app/594570/discussions/0/2280456883447324314); [Steam mod changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/2858308116?p=28))
- What counts as "large" can be contested: players argue Orc Big'uns are normal infantry despite their size, so anti infantry is better against them. COMMUNITY. ([Steam WH3 discussion](https://steamcommunity.com/app/1142710/discussions/0/600766950433402985))
- CA's cavalry patch intent: charging cavalry should beat infantry that merely counter charges, and bracing should become much more effective than counter charging. PRESS paraphrasing CA. ([PCGamesN](https://www.pcgamesn.com/total-war-warhammer-2/cavalry-overhaul-beta))
- PC Gamer's review gives a counter example: Longbeards are immune to psychology, which makes them strong against fear causing Giants. PRESS, SUMMARY ONLY. ([PC Gamer review](https://pcgamer.com/total-war-warhammer-review))
- Ian Roxburgh (CA) said there is more unit variety within any one race than within the whole of any previous Total War game. DEV via PRESS, SUMMARY ONLY. ([GamesRadar](https://www.gamesradar.com/total-warwarhammer-delivers-huge-thrilling-battles))
- WH3 survival battles: internal testers across the studio attacked the mode with ranged, artillery, towers or spells and got widely different results, which created new balancing needs; the goal was to not punish players who micro less while still challenging experts. A shared battle currency lets each player choose how much micromanagement to do. DEV (James Martin). ([Game Developer deep dive](https://www.gamedeveloper.com/design/deep-dive-how-survival-battles-balance-the-demonic-chaos-of-i-total-war-warhammer-iii-i-))
- Outside commentators frame TWW unit balance as rock paper scissors at its core. COMMUNITY and designer blog, not CA. ([80.lv](https://80.lv/articles/how-strategy-games-apply-the-rock-paper-scissors-mechanic/?amp=1); [Game Developer blog](https://www.gamedeveloper.com/design/rock-paper-scissors-design-in-strategy-games))

### Inferences
- MY INFERENCE: in an auto battler where fighters act alone, the build pays off at the moment two tags meet. TWW's clearest version is brace against charge. For Kindred Siege, a braced stop could be its own short clip: the beast's charge halts, no knockback on the defender, plus a small icon or word pop, so the player sees that the pick they made before the fight caused it.
- MY INFERENCE: the wrong build should be just as visible. In TWW an unbraced line gets knocked flying; at 5 v 5 a knockdown chain on the fighters who lacked the counter is the mirror read.
- MY INFERENCE: TWW counters are percentage shifts (bonus vs large adds to hit chance and damage), not hard immunities, except for psychology immunity. That keeps fights probabilistic but legible; hard immunities are rare and memorable (Longbeards against fear).

### Gaps
- No CA developer statement found on their counter philosophy for TWW battles (the rock paper scissors framing is from outsiders).
- No source found on post battle feedback (per unit kill and loss stats) showing the player whether their army choices were right. I believe TWW shows a post battle unit breakdown, but did not source it this session.
- I did not read Al Bickham's MCV interview on spiky balance; it may hold CA's view on lopsided matchups. ([MCV/Develop](https://mcvuk.com/development-news/total-war-warhammer-interview-can-things-ever-be-too-spiky/))
