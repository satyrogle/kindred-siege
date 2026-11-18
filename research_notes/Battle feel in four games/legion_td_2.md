# Legion TD 2 (AutoAttack Games): battle feel and unit play in the fight phase

Source key used below:
- DEV = developer source (official update page on beta.legiontd2.com, or a developer written article).
- WIKI = community wiki (legiontd2.wiki.gg). Not developer sourced. The pathing page says pages made before October 2022 are adapted from the old Fandom wiki and it cites no developers.
- SNIPPET = text seen only in a search result summary; the page itself was not opened (SteamDB returned 403). Treat as unconfirmed wording.
- INFERENCE = my own reasoning, not from any source.

The single richest developer source found is the Season 6 update (v6.00), which is almost entirely a readability pass. Its date was not shown on the fetched page.

## 1. Weight scaled to power: how big or expensive units read as heavier

### Takeaway
I found no developer numbers for model scale, attack timing or sound per tier. The one hard technique is Lifebars 2.0: the bar is cut into 1,000 HP chunks so a tank visibly carries more segments than a squishy unit. Weight otherwise comes from the model brief (a heavy melee fighter is designed "hefty" from day one) and from animators being given timing and feel notes per animation.

### Cited Findings
- Lifebars 2.0: "Now shows chunks of 1k hp to distinguish tanks from squishy units." Damaged units also show orange burn graphics on the bar. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- Bar segments can be switched on or off in Options > Interface. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- The tier 4 Atlantean fighter Grarl was designed "to fill a certain gameplay niche as a heavy-hitting melee fighter, so we knew off-the-bat that he was going to be hefty." No scale numbers are given. (DEV: [Sketchfab, Game Studio Spotlight: AutoAttack Games, 20 Aug 2018](https://sketchfab.com/blogs/community/game-studio-spotlight-autoattack-games))
- Rigging and animation are outsourced. The studio sends "assets, animation descriptions, and references" and tries to be "as detailed as possible about each animation's timing, feel, and use case." (DEV: [Sketchfab spotlight](https://sketchfab.com/blogs/community/game-studio-spotlight-autoattack-games))
- Pipeline: a quick high resolution ZBrush sculpt, then retopology to a low resolution in game model; "the hi-res model is never used in the game." Over 100 unique units with custom models. (DEV: [Sketchfab spotlight](https://sketchfab.com/blogs/community/game-studio-spotlight-autoattack-games))
- Boss and King get "extra-wide lifebars": King, Granddaddy (wave 10) and Maccabeus (wave 20). So bar width, not only bar length, is used as a power signal for the biggest bodies. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- Some upgraded units got "small persistent VFX" (Chained Fist, Oathbreaker, Golden Buckler, Royal Guard), and Elite Archer and Trinity Archer got improved VFX. (SNIPPET from v6.01: [v6.01 Game Balance & Improvements](https://beta.legiontd2.com/updates/v601-game-balance-improvements/))
- Golden Buckler and Royal Guard (an upgrade line) got a texture update in v6.00. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))

### Inferences
- INFERENCE: The persistent small VFX on upgraded units reads as a "this one costs more" marker that lives on the body all fight, separate from attack effects. For Kindred Siege sprites that is a one or two pixel glow, aura flicker or trim colour on higher tier fighters.
- INFERENCE: Chunked bars are the cheapest weight signal for tiny sprites. A 5 v 5 fight could chunk at a fixed HP value so a tank shows, say, five ticks and a skirmisher two, readable even when the sprites are near the same size.
- INFERENCE: Extra wide bars for chiefs and bosses translate directly; a chief or boss in Kindred Siege could get a wider bar or a bar on the HUD edge.

### Gaps
- No developer figures found for model scale per tier, attack wind up or recovery timings, impact sound per tier, or screen shake. Searched patch notes for "model size", "unit size" and "scale"; only the Warcraft III Legion TD changelog mentions model and collision sizes ([legiontd.com old changelog](http://legiontd.com/map/oldchangelog.php)), not this game.
- No source found on splash visuals (size of splash rings, cleave arcs).

## 2. Hits that travel and land on a body: movement, targeting, melee contact, projectiles, spells

### Takeaway
Movement is Boids flocking tuned for smoothness, so units are allowed to walk through each other. Each unit has an acquisition range a bit larger than its attack range, and targeting prefers the enemy missing the most flat HP, which makes focus fire emerge. Projectiles are kept short lived to cut clutter.

### Cited Findings
- Units take the shortest route; paths are "usually fairly straightforward" because there are generally no blocking walls. (WIKI: [Pathing & Targeting](https://legiontd2.wiki.gg/wiki/Pathing_%26_Targeting))
- Movement uses Boids: steering, alignment, cohesion, goal seeking, obstacle avoidance. "Legion TD 2 is configured to prioritize smoothness," even though units sometimes walk through each other. (WIKI: [Pathing & Targeting](https://legiontd2.wiki.gg/wiki/Pathing_%26_Targeting))
- Each unit has an acquisition range and an attack range; acquisition range is "typically a bit higher than the attack range," especially for melee units. No numbers are given. (WIKI: [Pathing & Targeting](https://legiontd2.wiki.gg/wiki/Pathing_%26_Targeting))
- Target logic, in order: attack a target in attack range; else approach a target in acquisition range (it may switch to a better one on the way); else walk to the next waypoint. (WIKI: [Pathing & Targeting](https://legiontd2.wiki.gg/wiki/Pathing_%26_Targeting))
- Tie breaks inside attack range: (1) most flat health missing wins (flat, not percent, so tanks are not skipped; example Golem 100 damage taken beats Doppelganger 90); (2) closer target, ranged units only; (3) when a wave or mercenary attacks a fighter, the fighter whose tower spot is further forward in the lane. Ties inside acquisition range only: closest wins. (WIKI: [Pathing & Targeting](https://legiontd2.wiki.gg/wiki/Pathing_%26_Targeting))
- "No run around" rule: a unit will not run around a valid target to reach a preferred one further away; it switches to the valid target. Wiki advice from this: DPS on the lane side, tank at least one column toward the centre. (WIKI: [Pathing & Targeting](https://legiontd2.wiki.gg/wiki/Pathing_%26_Targeting))
- Special target weights: power mercenaries (Brute, Safety Mole) tend to be targeted later; Sea Serpent and Deepcoiler are tuned not to be targeted too fast; Great Boar, Red Eyes and units that revive or split on death are tuned to be targeted fast. (WIKI: [Pathing & Targeting](https://legiontd2.wiki.gg/wiki/Pathing_%26_Targeting))
- The King targets by the target's threat level and the hits needed to kill it. No numbers. (WIKI: [Pathing & Targeting](https://legiontd2.wiki.gg/wiki/Pathing_%26_Targeting))
- Mercenary spawn layout: tanks spawn in front of the wave, melee inside it, ranged behind it. Side to side order is by mythium cost with a fixed odd or even pattern. (WIKI: [Mercenaries](https://legiontd2.wiki.gg/wiki/Mercenaries))
- Mesh projectiles (Lizard, Cannoneer, Quill Shooter, Ranger, Tempest Machine Gunner, Leviathan Bombardier) "now vanish faster." Harpy missile VFX fixed. Some ability VFX launched from the wrong point under lag, fixed mostly for Tempest and Leviathan passives. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- Deploying a fighter: "It should feel more responsive now." (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- VFX are built in PopcornFX, called "fairly technical but powerful" and needing "a combination of art and math skills." (DEV: [Sketchfab spotlight](https://sketchfab.com/blogs/community/game-studio-spotlight-autoattack-games))
- King spells (no numbers on the page): Chain Frost, a frost orb that jumps between enemies, Pure damage, slows attack speed 15%; Judgement, a staff slam hitting all nearby enemies; Lightning Hammer, bouncing lightning bolts using mana; Immolation, damage per second around the King. (WIKI: [King](https://legiontd2.wiki.gg/wiki/King))
- A patch note says the developers "upgraded our game engine's visual effects renderer, which was a huge task and involved recreating every visual effect in the game from scratch." Version and date unknown. (SNIPPET: [Steam news feed, Legion TD 2](https://store.steampowered.com/oldnews/?appids=469600&appgroupname=Legion+TD+2+-+Multiplayer+Tower+Defense&feed=steam_community_announcements))

### Inferences
- INFERENCE: Acquisition range bigger than attack range is what makes melee units visibly walk into contact instead of snapping. For Kindred Siege, a fighter that "sees" an enemy a little before it can hit gives a short approach beat, which reads as intent on small sprites.
- INFERENCE: "Most flat HP missing" targeting makes everyone pile onto the same wounded body. Focus fire is readable because one sprite takes many hits at once. If Kindred Siege wants a readable 5 v 5, a similar sticky rule (finish the wounded) makes kills arrive in clear steps instead of spread chip damage.
- INFERENCE: "No run around" keeps fights local and lets placement decide who gets hit first. That is the link between a build choice (where you stand a fighter) and what the player sees in the fight.
- INFERENCE: Allowing overlap for smoothness is a deliberate trade. At sprite size, overlap hides who is hitting whom; Kindred Siege may want the opposite choice (spacing) for a 2.5D screen with only ten bodies.
- INFERENCE: Fast vanishing projectiles: the hit should land and the projectile should leave. On pixel sprites that means a projectile that dies on impact frame with a short spark, no lingering trail.

### Gaps
- No numeric acquisition or attack ranges were on the wiki page. Unit pages may hold attack range values; not fetched.
- No source on melee contact feel: hit stop, flash on hit, knockback, damage numbers. I found nothing saying Legion TD 2 uses hit stop or screen shake.
- No timings for spell telegraphs or King spell cooldowns.

## 3. Readability: health bars, silhouettes, colour, reading a crowded lane

### Takeaway
Legion TD 2 reads the crowd mostly through the HUD layer, not the models. Season 6 cut clutter hard: hide full HP bars, drop team colour textures (the bar carries team), drop aura circles, cap repeated sounds at 5 per player per second, and make the 3D fonts match the UI.

### Cited Findings
- Full health units that do not use mana in the battle phase hide their lifebars. Holding CTRL shows all bars; HOME cycles display modes. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- "Removed red/yellow lifebar option." (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- Team colour textures removed from units because "they looked out of place, and lifebars serve the same role." (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- Aura range circles removed because "they weren't useful and just took attention away from the yellow squares, which are more accurate." (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- Sounds of the same type capped at 5 per player within about one second, so crowded moments are less noisy. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- 3D in game fonts (bounty texts, lane markers) changed to match the UI font. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- Improved icons for Safety Aura, Leadership Aura, Deflection, Giant Slayer, Death Stare and Cleave; improved graphics for Magician, Sorcery, Vampirism, Protector and Leadership (Pack Leader Aura). (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- The Tab scoreboard got a light background on your own lane so you can find it. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- A Steam patch note line: "VFX: Many visuals should be more visible and distinct." (SNIPPET: [Steam news feed](https://store.steampowered.com/oldnews/?appids=469600&appgroupname=Legion+TD+2+-+Multiplayer+Tower+Defense&feed=steam_community_announcements))
- v5.04 announced "New icons and VFX" alongside new units. (DEV, tweet seen only as snippet: [Legion TD 2 on X](https://x.com/LegionTD2/status/1234543532871966720))
- The developers use Sketchfab so players can view units close up and learn their "animations and personalities." (DEV: [Sketchfab spotlight](https://sketchfab.com/blogs/community/game-studio-spotlight-autoattack-games))
- In game Codex idle animations play every 5 seconds and players can cycle between Breathe/Idle and Attack, so a unit's attack can be learned outside the fight. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))

### Inferences
- INFERENCE: The pattern is "remove what is not information." Every removal in Season 6 is justified by a more accurate signal already on screen. For a pixel 5 v 5, the same test can be applied to every overlay: if the bar or the ring already says it, drop the extra.
- INFERENCE: Hiding full HP bars means a bar appearing is itself a hit signal. In Kindred Siege, a bar that pops in on first damage marks the first contact without any extra effect.
- INFERENCE: The sound cap (5 same type per second per player) is a direct number Kindred Siege can copy for a crowded fight so a parry or chief blow is not buried under repeated swing sounds.
- INFERENCE: Moving team identity off the model and onto the bar frees the sprite palette. That fits shared 32 colour palettes, where tinting sprites per team would fight the palette lock.

### Gaps
- No developer statement found on silhouette rules, camera angle or how many units the art team plans to fit on screen. The Sketchfab article does not cover readability from the top down camera.
- No data on damage number popups during the fight (whether they exist, size, colour).

## 4. Every hit changes the state: deaths, leaks, King damage, a wave held or broken

### Takeaway
The lane fight has a hard visible break point: when your last fighter on a lane dies, the rest of the wave runs to the King. Leak size is tracked as a percentage and the developers tune King damage so small leaks hurt relatively more and big leaks relatively less. The King itself fights back and later got damage reduction that grows with damage taken in the wave.

### Cited Findings
- Once all fighters on a lane die, remaining creatures run toward the King. The King defends itself. The match ends when the King dies. (WIKI: [King](https://legiontd2.wiki.gg/wiki/King))
- Clearing means killing every enemy before they reach the end and earning maximum gold; leaking means fewer gold for you, bonus gold for the opponent and damage to the shared King. (Community guide: [Switchblade Gaming beginner's guide](https://www.switchbladegaming.com/strategy-games/legion-td-2/beginners-guide-12/), seen as snippet)
- Patch notes: "The king now takes more damage against small leaks and less damage against large leaks on wave 6." and later "The king is now more vulnerable to small leaks and relatively less vulnerable to large leaks." Versions not captured. (SNIPPET: [SteamDB patch notes](https://steamdb.info/patchnotes/16645715))
- v10.00 reportedly raised King damage by 100% and gave the King Tenacity: 1.5% damage reduction per 1% damage taken in the current wave. (Secondary news article: [player.one on v10.00](https://www.player.one/legion-td-2-update-v1000-implements-changes-gold-gain-classic-mode-153455), seen as snippet)
- "Leaked percentage on boss waves is now accurate." (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- "Pro-leak!" achievement: leak 17% or less on a wave. (Official store achievement list, seen as snippet: [Epic Games Store achievements](https://store.epicgames.com/achievements/legion-td-2-a244b9))
- Damage reduced by damage reduction and evasion now counts in the Damage Taken tracker. (SNIPPET: [SteamDB patch notes](https://steamdb.info/patchnotes/16645715))
- Damaged lifebars show orange burn graphics. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- Bounty texts are 3D world text in the lane (font changed to match UI), so kills pop a number in the world. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))

### Inferences
- INFERENCE: The "last fighter dies, wave runs" rule is the visible moment a held lane becomes a broken one. Kindred Siege's equivalent could be the moment the leader falls or the front row breaks, shown as the enemy surging forward.
- INFERENCE: Making small leaks hurt relatively more keeps every single enemy that gets through meaningful. In a 5 v 5, the matching idea is that every death or every landed big hit should move a visible bar.
- INFERENCE: Tenacity (King resists more as it takes more) is a comeback brake. It stops one wave from ending the game, so the King's bar drops in big steps early in a wave and slows later. That shape could suit a leader's nerve bar.
- INFERENCE: Leak as a percentage is a single number the player can read after each fight. Kindred Siege could report "how much got through" or "how much of the enemy you broke" as one number per fight.

### Gaps
- No exact King HP or damage per leaked unit found; the wiki King page has no HP numbers.
- No source on death animations, corpse time, or a "wave cleared" visual or sound cue.

## 5. Rare big moments: King, mercenaries, boss waves, big spells

### Takeaway
Big moments are marked by HUD and economy signals, not by pausing the fight. Boss waves and the King get extra wide bars, mercenaries arrive as a formed group at the start of the battle phase, hiring them pops "+ income", and King spells are big area effects. I found no evidence of slow motion, camera cuts or freeze frames.

### Cited Findings
- Extra wide lifebars for the King, Granddaddy (wave 10) and Maccabeus (wave 20). (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- Mercs hired in the build phase attack as soon as the battle phase starts; mercs hired in the battle phase attack when the next battle phase starts. Each player sends to one fixed opponent. Mythium can be saved over several waves for a bigger push. (WIKI: [Mercenaries](https://legiontd2.wiki.gg/wiki/Mercenaries))
- Merc costs in mythium (examples): Snail 20, Brute 60, Imp 100, Safety Mole 120, Mimic 160, Kraken 400. (WIKI: [Mercenaries](https://legiontd2.wiki.gg/wiki/Mercenaries))
- A "+ income" text popup appears when hiring a mercenary; it "adds visual clarity and feedback for income." A "+1 mythium rate" or "+2 mythium rate" popup appears when training a worker. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- Chat announces when someone finds a card. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- Hero style Legion Spell arrow graphics fixed; Legion Spell window graphics made consistent. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- A Christmas patch made Lizard Army "harder to fake" with "increased clarity and a buff." (SNIPPET: [Steam workshop changelog](https://steamcommunity.com/sharedfiles/filedetails/changelog/541930328))
- King spells: Chain Frost (bouncing orb, 15% attack speed slow), Judgement (staff slam on all nearby), Lightning Hammer (bouncing bolts using mana), Immolation (damage aura). King upgrades only in the build phase: 20 mythium each, 10 levels max; Attack +8% damage per level, Regen +0.005% of missing HP plus 0.005% of max HP per level, Spell +9% AOE damage per level. (WIKI: [King](https://legiontd2.wiki.gg/wiki/King))
- King Upgrades on the wave timer bar use different colours. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))

### Inferences
- INFERENCE: Mercenaries arrive in a fixed formation (tanks front, melee in, ranged back), so a big send reads as a shaped army walking in, not a scatter. A Kindred Siege raid boss or reinforcement could enter in a fixed shape that tells the player what is coming.
- INFERENCE: The King is a rare actor that only fights when the lane breaks, so its spells are the "big moment" of a bad wave. That maps to a Kindred Siege leader whose big parry only matters when the line is in danger.
- INFERENCE: "Harder to fake" plus "increased clarity" on Lizard Army suggests the developers treat a big spell's tell as competitive information that must be honest. For a parry game, the telegraph of a big hit has to be reliable or the parry stops feeling fair.

### Gaps
- No source found on any freeze, slow motion, camera move or cut in for boss arrival, King spells or merc arrival. If they exist I could not confirm them.
- No source on arrival sounds or announcer lines for mercs or bosses.
- No timings for wave 10 and wave 20 boss mechanics.

## 6. The battle as the build paying off: build phase, then watching it hold or fail

### Takeaway
Legion TD 2 splits play into a build phase and a hands off battle phase, then explains the result with numbers: a value bar against a recommended value, leak percentage, MVP score, a Builds tab, and a view of what the enemy fielded last wave. Placement matters because targeting rules decide who gets hit first.

### Cited Findings
- Value Bar 2.0: value shown near gold, mythium, income and workers; it turns yellow within 20% of recommended value and red beyond 20% (wave 1 has its own thresholds). The recommended value "is a heuristic that assumes you have an average build with average typing against the wave." (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- New icons for income and fighter value. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- Holding TAB and clicking "Enemy Team" (or Space) shows enemy fighters from the previous wave; it "makes it easier to keep track of which lane you are sending to." (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- Builds tab shows the specific King Upgrades received, not only their total value. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- MVP score now "rewards causing the opponent to leak more and punishes feeding the opponent more." (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- Victory/Defeat screen: the snapshot pauses once the game ends. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- Tooltips: attack and defense update live when attack speed or mana regen changes; target frame shows flat damage reduction; Tab scoreboard tooltips show current mana; mana regen shown as a number. (DEV: [v6.00 Season 6](https://beta.legiontd2.com/updates/v600-season-6/))
- Placement advice from targeting: put the DPS on the lane side and the tank at least one column toward the centre, because units will not run around a valid target. (WIKI: [Pathing & Targeting](https://legiontd2.wiki.gg/wiki/Pathing_%26_Targeting))
- API added a valuePerWave property (player value per wave, including stacks). (SNIPPET: [SteamDB patch notes](https://steamdb.info/patchnotes/16645715))

### Inferences
- INFERENCE: The value bar tells the player before the fight whether the build is on pace, so a loss can be read as "I was red" rather than "the game cheated." Kindred Siege could show a squad strength against the fight's expected strength before the fight starts.
- INFERENCE: Showing the enemy's previous wave lineup lets the player connect what they saw in the fight to what they build next. A Kindred Siege post fight screen could replay or freeze the moment the line broke.
- INFERENCE: Because the fight is hands off, every tool for understanding it lives outside it (scoreboard, tooltips, value). Kindred Siege's leader parry is the one live input, so the other readouts can stay outside the fight just as here.

### Gaps
- No source found on a per wave damage dealt breakdown or post wave summary screen beyond the Tab scoreboard and leak percentage.
- No developer interview found on the design reasoning for the build then watch loop; the 2016 Reddit AMA by designer Jules Gari was found but not read.
- Build and battle phase durations not found (the wiki Waves page returned 404).
