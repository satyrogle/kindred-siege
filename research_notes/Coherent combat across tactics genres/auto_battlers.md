# How auto battlers make watched combat coherent and readable

Scope note: about 23 searches and fetches. Mostly secondary sources (fan wikis, guides, forum posts). Primary sources found: Riot's TFT Galaxies learnings post, Vanillaware staff interview (Inverse), Square Enix staff via Game Developer/Polygon, Atlus press info (via RPG Site). Player measurements and fan wiki claims are marked [community]. Not covered for lack of time or sources: Auto Chess, Storybook Brawl, Legion TD 2, Despot's Game, Vampire Survivors, Hero Wars style idle battlers, Dragon Age tactics (see Gaps).

## 1. Targeting rules, and how predictable targeting makes fights readable

### Takeaway
Every readable auto battler uses a small, fixed, learnable targeting rule per unit (nearest, front to front, leftmost attacks first, building only, ground only, same column) plus one or two named exceptions (taunt, assassin jump, leader). The genres that read best (Super Auto Pets, Hearthstone Battlegrounds, Ogre Battle 64) put attackers in a fixed order and lock who can hit whom by lane or slot; free roaming real time targeting (TFT, Mechabellum) relies on hard counters and big shapes instead.

### Cited Findings
**Super Auto Pets (strictest, most readable)**
- Only the two front pets fight; they hit each other at the same time; when one dies the pet behind moves up — [note.com player write up](https://note.com/massan_gabagaba/n/n9520a392a633?hl=en) [community]
- Start of battle abilities resolve in order of attack, highest first — [SAP fandom wiki glossary](https://superautopets.fandom.com/wiki/Glossary) [community]; ties go to higher health — [Two Average Gamers guide](https://www.twoaveragegamers.com/ultimate-guide-to-super-auto-pets-game-mechanics/) [community]
- Players on Steam still report faint and on death triggers resolving in an order they cannot explain, i.e. the edge cases are where readability breaks — [Steam thread](https://steamcommunity.com/app/1714040/discussions/0/3198118348349750330) [community]

**Hearthstone Battlegrounds**
- Minions attack in sequence from leftmost to rightmost, the two boards alternating, then repeat — [Hearthstone wiki](https://hearthstone.fandom.com/wiki/Battlegrounds); [Icy Veins mechanics guide](https://www.icy-veins.com/hearthstone/hearthstone-battlegrounds-mechanics-guide)
- Target is random unless the defender has Taunt, then Taunts are attacked first — [Hearthstone wiki](https://hearthstone.fandom.com/wiki/Battlegrounds)
- First attacker: the side with more minions (e.g. 7 vs 6) goes first; equal counts pick randomly — [Hearthstone wiki](https://hearthstone.fandom.com/wiki/Battlegrounds); [PCGamesN guide](https://www.pcgamesn.com/hearthstone/battlegrounds-guide-how-to-play)
- Players have proposed systems to reduce attack order RNG, i.e. the random target is a known frustration — [Blizzard forums](https://us.forums.blizzard.com/en/hearthstone/t/improving-attack-order-rng-with-a-tipping-system-in-bg/155660) [community]

**Teamfight Tactics**
- Units default to the nearest target; assassins (older sets) leap to the enemy backline after a short delay, in a straight line — [Mobalytics positioning guide](https://mobalytics.gg/blog/tft/tft-positioning-guide-how-to-get-the-most-from-your-units/) [community]
- Sources disagree on the exact assassin target: "farthest non assassin, non berserker unit" (2019 Reddit) vs "farthest enemy or nearest available tile" — [r/CompetitiveTFT 2019](https://redlib.hbubli.cc/r/CompetitiveTFT/comments/e6hwcm/weekly_questions_and_help_thread/fdo2ute); [Inven Global assassin guide](https://www.invenglobal.com/articles/8672/tft-guide-understanding-assassins-and-counter-strategies) [community]. The ambiguity itself shows players reverse engineer targeting.
- Counterplay is spatial and readable: clear the hex in front of your carry, use a bait tank, shift one hex off the corner to break the jump — [Inven Global](https://www.invenglobal.com/articles/8672/tft-guide-understanding-assassins-and-counter-strategies) [community]

**Dota Underlords**
- Melee units prefer targets on straight (vertical or horizontal) lines and only take diagonals when none are available — [Underlords wiki, Attacks](https://dotaunderlords.fandom.com/wiki/Attacks) [community]
- Abilities have preferred targets; the caster walks to the nearest valid target if none is in range — [Underlords wiki, Abilities](https://dotaunderlords.fandom.com/wiki/Abilities) [community]
- Valve shipped an update headlined as fixing "AI targeting issues"; details not confirmed — [Player.One](https://www.player.one/dota-underlords-update-fixes-ai-targeting-issues-and-changes-some-heroes-134554)

**Mechabellum (counter triangle)**
- Rule of thumb: splash beats chaff, chaff beats single target damage, single target beats splash — [DualShockers counters](https://www.dualshockers.com/mechabellum-all-counters-explained/) [community]
- Rounds are often won by whoever clears the other side's chaff first; cheap bodies make Marksmen waste shots — [DualShockers](https://www.dualshockers.com/mechabellum-all-counters-explained/); [Mechabellum wiki, Marksman](https://mechabellumwiki.wiki/counters/mechabellum-marksman-counter) [community]
- Hard target type locks: Hacker cannot target air at all and does 30% of listed damage to barriers; Typhoon debuffs air in a 100 m radius and cuts their range by 20 — [DualShockers](https://www.dualshockers.com/mechabellum-all-counters-explained/) [community]

**Clash Royale (unit identity through target type)**
- Each troop has an explicit target type: ground, air, buildings, or any. Giant and Hog Rider target buildings only and ignore troops; only buildings can distract building targeters — [Clash Royale wiki, X-Bow](https://clashroyale.fandom.com/wiki/X-Bow) [community]
- Stun (e.g. Zap) forces a retarget to the nearest entity — [Clash Royale wiki, X-Bow](https://clashroyale.fandom.com/wiki/X-Bow) [community]

### Inferences
- Readability rises with how few targets a unit may legally pick. SAP (one legal target), Ogre Battle 64 (own column plus neighbours), Hearthstone (taunt or random) and Clash Royale (target type lock) let a viewer predict the next blow. TFT and Mechabellum allow many legal targets and compensate with counters and big spell shapes.
- Random target choice (Hearthstone) is the most complained about rule. Random order or target reads as "doing whatever they want".
- For Kindred Siege: locked pairs that trade blows are the SAP model (front pet vs front pet) generalised to five lanes. It is the most legible rule in the genre. The coherence risk is every exception (leader parry, class moves, retargets after a death). Each exception needs a fixed, nameable rule ("when your partner dies, you step to the nearest living enemy in your row") and should be rare.

### Gaps
- No official Riot page with current TFT targeting rules (Riot does not publish one; assassins no longer exist as a trait per Mobalytics).
- No Game River (Mechabellum) interview found on targeting design.
- Super Auto Pets: no Team Wood primary source on the order rules.

## 2. Unicorn Overlord and Ogre Battle 64: squad auto battles with tactics (closest to Kindred Siege)

### Takeaway
Both games resolve a fight as a fixed budget of actions, taken one at a time in a stat based order, by units on a small grid whose row and column limit who they can hit. Ogre Battle 64 gives each class a fixed number of attacks per row (e.g. 3 front, 2 middle, 1 back) and ends the fight when every unit has spent its actions, winner by total damage. Unicorn Overlord replaces the per row counts with AP (active) and PP (passive) gems and ordered condition rules, and Vanillaware said the auto battle was chosen at planning stage to keep the focus on preparation and "ease of understanding".

### Cited Findings
**Unicorn Overlord (Vanillaware, 2024)**
- Combat was auto from the planning stage: "It was decided at the planning stage that the game would be auto-combat." "We wanted the focus of the game to be on troop building and the importance of preparing for battle." "It would probably be overwhelming to have to give direct commands to all the units marching at the same time." "The battle system itself was a result of our pursuit of combining strategy and ease of understanding." (Wataru Nakanishi, lead game designer) — [Inverse interview](https://www.inverse.com/gaming/unicorn-overlord-interview-vanillaware)
- Because battles run automatically, the team "paid particular attention to captivating animation" and "intentionally made the characters larger to make the battles feel more powerful" (Takafumi Noma, director) — [Inverse](https://www.inverse.com/gaming/unicorn-overlord-interview-vanillaware)
- George Kamitani was not director; Noma was director, programmer and lead artist — [Inverse](https://www.inverse.com/gaming/unicorn-overlord-interview-vanillaware). I found no Kamitani quote on the battle system.
- Turn order is set by each combatant's Initiative; on its turn a combatant uses one Active Skill, which costs AP (red gems). Passive Skills cost PP (blue gems) and fire automatically on a condition such as being attacked. The battle ends when all AP is spent or one side is defeated — [RPG Site, Atlus info](https://www.rpgsite.net/news/15481-unicorn-overlord-highlights-tactics-priority-and-battle-stages-along-with-several-new-non-human-allies)
- Tactics: each skill can have up to two conditions (e.g. an ally below an HP threshold); tactics are ranked and the highest entry whose conditions hold is used. Put heals high; give narrower conditions higher priority. With no tactics set, units attack the enemy directly in front of them. An Optimize button sets recommended tactics — [RPG Site](https://www.rpgsite.net/news/15481-unicorn-overlord-highlights-tactics-priority-and-battle-stages-along-with-several-new-non-human-allies); [Prima Games](https://primagames.com/gaming/unicorn-overlord-tactics-and-conditions-explained)
- First Strike: a unit that loses a battle is knocked back into a waiting state; attacking it grants First Strike, and that unit always acts first regardless of Initiative — [RPG Site](https://www.rpgsite.net/news/15481-unicorn-overlord-highlights-tactics-priority-and-battle-stages-along-with-several-new-non-human-allies)
- Battle preview: when you direct a unit at an enemy you see a preview of the outcome before committing — [GameSpot tips](https://www.gamespot.com/gallery/unicorn-overlord-tips-12-things-to-know-about-the-anime-rpg/2900-5162/)
- Formation: 6 blocks (two rows of three); front row for Hoplites, Gladiators, Warriors, Paladins; back for Hunters, Knights, Clerics, Wizards — [The Game Crater](https://www.thegamecrater.com/unicorn-overlord-the-best-unit-formations/); [Prima Games formations](https://primagames.com/gaming/unicorn-overlord-best-unit-formations) [community]
- Unit size starts at 2, max 5; costs 10 honors for size 3, 50 for size 4 (Renown C), 200 for size 5 (Renown A). Up to 10 squads, starting with 3 — [Game8](https://game8.co/games/Unicorn-Overlord/archives/447607); [UO fandom wiki](https://unicornoverlord.fandom.com/wiki/Expanding_Units) [community]
- High Initiative knights may not land killing blows, so players pair them with faster units, i.e. players reason about order — [gleetron guide](https://gleetron.pages.dev/posts/combat-tips-for-unicorn-overlord) [community, weak source]

**Ogre Battle 64 (Quest, 2000)**
- Each squad is a 3x3 grid: front, middle, back rows by left, centre, right columns, one character per square; large monsters need an empty buffer square — [LP Archive mechanics megadump](https://lparchive.org/Ogre-Battle-64/Update%2002/) [community]
- Attacks per class depend on row: Paladin, Golem, Ogre, Hellhound 3 front / 2 middle / 1 back; Fighter, Berserker, Hawkman 2 front / 1 elsewhere; Wizard, Sorceress, Amazon 2 back / 1 elsewhere; Archer 1 front / 2 middle; Diana 3 back — [LP Archive](https://lparchive.org/Ogre-Battle-64/Update%2002/) [community]
- Order: by Agility, front row characters first; ties go to the leader or leftmost — [LP Archive](https://lparchive.org/Ogre-Battle-64/Update%2002/); [RPGClassics tips](https://discourse.rpgclassics.com/t/ogre-battle-64-tips/20010) [community]
- No fixed round count: the battle ends when every character on both sides has used all its actions; the side that dealt more total damage wins (wipe also wins); loser gives ground. Boss battles are won only by killing the leader — [LP Archive](https://lparchive.org/Ogre-Battle-64/Update%2002/) [community]
- Reach: a unit can hit its own column or one column left or right; centre column can hit any column but can be hit by any. Melee must go through the first character in the column; ranged must clear everyone behind the target. Physical goes front to back, ranged and magic back to front — [LP Archive](https://lparchive.org/Ogre-Battle-64/Update%2002/); [RPGClassics](https://discourse.rpgclassics.com/t/ogre-battle-64-tips/20010) [community]
- Four squad strategies: Autonomous (random valid target), Attack Weakest (lowest HP), Attack Strongest (highest HP), Attack Leader (leader if reachable, else Autonomous) — [LP Archive](https://lparchive.org/Ogre-Battle-64/Update%2002/) [community]
- Players complain the strategies barely work because column reach overrides them; "Leader is the only one that seems to work" — [GameFAQs thread](https://gamefaqs.gamespot.com/boards/198230-ogre-battle-64-person-of-lordly-caliber/77464760); [GameFAQs "why don't units follow strategies"](https://gamefaqs.gamespot.com/boards/198230-ogre-battle-64-person-of-lordly-caliber/79825344) [community]
- Crit pushes the target back one row if the square is free; magic cannot crit and always does some damage — [LP Archive](https://lparchive.org/Ogre-Battle-64/Update%2002/) [community]
- Leader death: the squad ignores orders and flees — [LP Archive](https://lparchive.org/Ogre-Battle-64/Update%2002/) [community]. Players advise killing the leader last because the rest then scatter onto whoever killed him — [RPGClassics](https://discourse.rpgclassics.com/t/ogre-battle-64-tips/20010) [community]
- The only player input mid fight is an Interrupt meter that fills over time and with damage: change strategy, retreat, or fire an Elem Pedra; using one empties it — [LP Archive](https://lparchive.org/Ogre-Battle-64/Update%2002/) [community]

### Inferences
- The coherence engine in both games is a countable action budget: you can see each unit's remaining attacks (OB64 row counts, UO gems), so the fight has a visible beginning, middle and end, and you can predict who acts next. Kindred Siege could show remaining blows per unit the same way.
- OB64's column rule (own column, plus or minus one; melee must go through the front of the column) maps almost directly to a 3x3 band facing a 3x3 band. It gives "who hits whom" a spatial reason a viewer can see. Locked pairs are a stricter version of it.
- OB64 shows a warning: when a stated strategy is overridden by a hidden reach rule, players feel units "don't follow orders". Same symptom Jacob describes. Any target rule Kindred Siege shows must be the rule that actually decides.
- UO's default ("attack the enemy directly in front of them") plus the outcome preview before committing are both cheap readability wins.
- Kindred Siege's leader parry and Jacob's 8 Nov leader rules (leader off limits until his band falls, then he retreats) echo OB64's leader rules (boss wins only by killing leader, squad flees on leader death).

### Gaps
- No primary source found for UO battle scene length in seconds, the fast forward / skip animation option, or exact initiative formula. (From memory, not verified: UO has a battle speed up and a skip option; do not cite.)
- No George Kamitani statement on the battle system or on Ogre Battle as an influence was found; Inverse says only that 1990s SRPGs were the inspiration.
- OB64 battle scene duration and camera presentation not documented in sources found.

## 3. Rule based AI the player sets (FF XII gambits, UO tactics, Dragon Age)

### Takeaway
Gambits and tactics exist so a player can control many units without micromanaging; they read as coherent because each action can be traced to a visible, ordered rule ("condition then action, top rule wins").

### Cited Findings
- Square Enix chose gambits because the team feared command based real time play would be too fast and hard, and "we didn't want it to be overwhelming, so we went with the Gambit system"; it "allowed us to manage the party without micromanaging the characters" (Takashi Katano) — [Game Developer, from Polygon](https://www.gamedeveloper.com/design/why-i-final-fantasy-iv-i-was-key-to-i-ffxii-i-s-ai-driven-gambit-system)
- The precedent was Hiroyuki Ito's monster AI in Final Fantasy IV — [Game Developer](https://www.gamedeveloper.com/design/why-i-final-fantasy-iv-i-was-key-to-i-ffxii-i-s-ai-driven-gambit-system)
- Gambits are conditional statements, e.g. use a potion when HP below 30% — [Game Developer](https://www.gamedeveloper.com/design/why-i-final-fantasy-iv-i-was-key-to-i-ffxii-i-s-ai-driven-gambit-system); [PlayStation Blog](https://blog.playstation.com/2017/07/07/extended-play-how-final-fantasy-xiis-gambit-created-one-of-the-most-distinct-rpgs-ever/)
- UO uses the same shape: up to two conditions per skill, ranked, top valid rule fires — [RPG Site](https://www.rpgsite.net/news/15481-unicorn-overlord-highlights-tactics-priority-and-battle-stages-along-with-several-new-non-human-allies)
- OB64 offers four coarse strategies only, and players say they mostly don't work — [GameFAQs](https://gamefaqs.gamespot.com/boards/198230-ogre-battle-64-person-of-lordly-caliber/66159368) [community]

### Inferences
- A rule list the player set turns "the AI did something weird" into "my rule 2 fired". For Kindred Siege's directives, showing which rule fired (a short label over the unit) is the cheapest coherence fix.

### Gaps
- Dragon Age tactics and FF XII gambit slot counts not researched.

## 4. Presentation: round length, pacing, damage numbers, deaths, replays

### Takeaway
Fights are short and capped: TFT about 30 s before overtime, Backpack Battles 10 to 20 s with fatigue at 17 s. Riot's own fix for unreadable fights was to slow spells down, add travel time, and make key casters easier to track.

### Cited Findings
- TFT PvP fights end on a wipe or after a timer; overtime starts 30 s into combat and lasts 15 s (about 45 s total in that source); another wiki states 40 s — [TFTips battle specs](https://tftips.app/en/specs/battle); [League fandom TFT](https://leagueoflegends.fandom.com/wiki/Teamfight_Tactics_(game)) [community; numbers vary by set]
- Overtime effects disagree by source: about 3x attack speed, +100% ability power, minus 66% CC and healing (fandom) vs per second stacking 1.1x damage and attack speed, healing and CC down 10% a second to 70%, CC immunity from 38 s (TFTips) — [League fandom](https://leagueoflegends.fandom.com/wiki/Teamfight_Tactics_(game)); [TFTips](https://tftips.app/en/specs/battle) [community]
- Riot on TFT Set 2 (Rise of the Elements): "overfilled boards and a lot of invisible power", "It was often quite hard to track what was going on." Set 3 fix: many spells slowed down, given travel time and made clearer; Caitlyn, Ziggs, Zoe, Rakan made easier to track. Still unclear: Graves smoke (who is blinded), Yasuo's fast movement, Ezreal's spell fading before impact. Riot also added damage blocked and healing done meters — [Riot, TFT Galaxies learnings](https://teamfighttactics.leagueoflegends.com/en-us/news/dev/dev-teamfight-tactics-galaxies-learnings)
- Riot's general VFX rule (League): gameplay readability always wins over thematic resonance; silhouettes are the single most important thing for champion recognition — [Riot VFX updates dev blog](https://www.leagueoflegends.com/en-us/news/dev/dev-behind-the-scenes-of-vfx-updates/); [Sportskeeda on Riot clarity blog](https://www.sportskeeda.com/esports/riot-dev-talks-clarity-league-legends)
- Backpack Battles: each item fills a highlight at its cooldown rate and fires when full; Heat +2% speed per stack, Cold 2% slower, Stun pauses all cooldowns. Fights last about 10 to 20 s; fatigue starts at 17 s — [Backpack Battles wiki, Cooldown](https://backpackbattles.wiki.gg/wiki/Cooldown); [fandom, Game Mechanics](https://backpack-battles.fandom.com/wiki/Game_Mechanics) [community]
- Players asked Backpack Battles to preview cooldown changes before battle, not only show them in combat — [Steam thread](https://steamcommunity.com/app/2427700/discussions/0/4290313152625938174) [community]
- The Bazaar: haste doubles cooldown speed, slow counters it, charge instantly fills seconds of cooldown; minimum cooldown 1 s — [TV Tropes](https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/TheBazaar) [community]

### Inferences
- The readable pattern in cooldown battlers is a visible fill (bar or highlight) that predicts the next action. Kindred Siege's pair trades could show a small timer per pair so the viewer knows which pair swings next.
- Riot's fix list is the direct answer to "can't follow it": fewer simultaneous events, travel time on big moves, and specific named units made trackable.

### Gaps
- No primary data on TFT attack animation speeds, damage number rules, or death effects.
- Replay and spectator features (Mechabellum, TFT) not found in sources.
- OB64 and UO battle scene durations not found.

## 5. Unit identity: shapes, silhouettes, mana bars, counters

### Takeaway
Identity comes from one strong channel per unit: target type (Clash Royale), counter role (Mechabellum), silhouette (Riot rule), or a distinct spell shape.

### Cited Findings
- Clash Royale's target type locks (buildings only, ground only) are what make a unit's job readable from its behaviour — [Clash Royale wiki](https://clashroyale.fandom.com/wiki/X-Bow) [community]
- Mechabellum identity is the counter role (chaff, splash, single target, anti air) — [DualShockers](https://www.dualshockers.com/mechabellum-all-counters-explained/) [community]
- Riot: silhouettes are the single most important thing for champion recognition; showing damage radius lets players read range — [Sportskeeda](https://www.sportskeeda.com/esports/riot-dev-talks-clarity-league-legends); [Surrender at 20, VFX blog](https://surrenderat20.net/2015/06/red-post-collection-dev-blog-on-vfx.html)
- Riot named "overly subtle effects or targeting" as a remaining TFT problem — [Riot](https://teamfighttactics.leagueoflegends.com/en-us/news/dev/dev-teamfight-tactics-galaxies-learnings)

### Inferences
- Kindred Siege's per class panel shape is the same idea as TFT spell shapes. It reads only if the shape appears on the grid at the moment it matters, and if each class's target rule is part of its identity (e.g. Warden only hits the unit attacking its row).

### Gaps
- No TFT dev source on line, cone and circle spell shapes or mana bar design.

## 6. Readability complaints and fixes

### Takeaway
Complaints cluster on four things: too many simultaneous effects, units that look alike, hidden timers, and random or unexplained target choice. Fixes: slower and fewer effects, travel time, visible timers, previews before battle, deterministic order.

### Cited Findings
- TFT Set 2: overfilled boards, invisible power, hard to track — fixed with slower, clearer spells — [Riot](https://teamfighttactics.leagueoflegends.com/en-us/news/dev/dev-teamfight-tactics-galaxies-learnings)
- Magicbook AutoBattler: late game effect clutter makes the screen "mostly colors"; similar units hard to tell apart; developer working on reducing clutter — [Steam thread](https://steamcommunity.com/app/2800260/discussions/0/4696783492317517397) [community]
- Littleblade: player wished cooldowns were easier to see — [itch.io](https://porkrind.itch.io/littleblade/comments) [community]
- Hearthstone BG: players want less attack order RNG — [Blizzard forums](https://us.forums.blizzard.com/en/hearthstone/t/improving-attack-order-rng-with-a-tipping-system-in-bg/155660) [community]
- OB64: units seen as ignoring their strategy because column reach overrides it — [GameFAQs](https://gamefaqs.gamespot.com/boards/198230-ogre-battle-64-person-of-lordly-caliber/79825344) [community]
- Backpack Battles: players want changes previewed before the fight — [Steam](https://steamcommunity.com/app/2427700/discussions/0/4290313152625938174) [community]

### Inferences (Kindred Siege fixes for "units do whatever they want")
1. One legal target per unit at a time (SAP and OB64). Locked pairs already do this; never let a unit swing at anyone but its pair partner unless a named rule says so.
2. Fixed visible order (Hearthstone left to right, OB64 agility then front row, UO initiative). Show the next actor; one action resolves at a time, like UO and OB64.
3. Countable action budget per unit (OB64 attacks per row, UO AP gems). The fight ends when blows run out. This gives a clear arc.
4. Every exception is a named rule shown on screen when it fires (taunt, First Strike, leader parry, retarget on partner death). Rule label over the unit, like a gambit.
5. Spatial reach the viewer can see (OB64 same column plus one; melee through the front of the column). A 3x3 band vs 3x3 band maps directly.
6. Fewer, slower, travelling big moves (Riot fix). Avoid simultaneous effects.
7. Outcome or target preview before the fight (UO preview, Backpack Battles request).
8. Don't show a strategy the sim then overrides (OB64 lesson).

### Gaps
- No Reddit threads for TFT or Underlords readability complaints were retrieved.
- Auto Chess, Storybook Brawl, Legion TD 2, Despot's Game, Vampire Survivors style and Hero Wars style idle battlers were not researched in this pass.
