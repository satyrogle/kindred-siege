# How combined unit and mass combat games make battles coherent and readable

Scope note: 23 tool calls. Several primary pages (Liquipedia WC3, Hive Workshop, HoMM shoutwiki, Total War fandom wiki, Total War Academy) refused automated fetches (403, 402, bot wall, redirect). Where a fact comes only from a search result snippet, it is marked "(snippet)". Player or community measurements are marked "(community)". Facts I recall but could not source this session are in Gaps, never in Cited Findings.

## 1. Total War: melee lock, matched combat, morale and routing

### Takeaway
Total War sells "two blocks of men are fighting" mainly through morale state shown on the banner and a clear route (units turn and run), not through every man duelling. Matched (paired) kill animations began in Medieval 2 from Spartan: Total Warrior animators to improve readability, but in Warhammer CA kept them only for "major moments or kills" because a single shared two body animation is slow and breaks collision; the routine fight became snappier, freer one body animations plus "synced" reactions.

### Cited Findings
- Matched combat arrived in Medieval 2. Rome 1 had none; its animations were motion capture "with a few tweaks". Animators from the cancelled console project Spartan: Total Warrior moved to the Medieval 2 team and brought the know how (source: [DBLTAP, citing CA's Medieval 2 retrospective video](https://www.dbltap.com/news/total-war-medieval-2-matched-combat-animations-spartan-total-warrior-history))
- Greg Alston, head of the Total War art team: that was where the team "started to understand more about the readability of animations"; Spartan taught them to make animations "a bit more theatrical". Medieval 2 also added skeleton bones (Rome's soldiers reportedly lacked shoulder bones, making attacks stiff) (source: [DBLTAP](https://www.dbltap.com/news/total-war-medieval-2-matched-combat-animations-spartan-total-warrior-history))
- Warhammer 1 (2015), battle designer Simon Mann: "Matched combat is a single animation with two entities." CA described the new system as snappier with more visible action; matched animations "are preserved for major moments or kills" (snippet) (source: [PC Gamer, Six new things we noticed](https://pcgamer.com/six-new-things-we-noticed-while-playing-total-war-warhammer))
- Warhammer 1 alpha coverage: CA is "adding synced animations for combat on top of matched animations". "Whereas a matched animation animates two characters together", "synced animations sees one character react to their opponent's action" (source: [Gameranx](https://gameranx.com/updates/id/28437/article/total-war-warhammer-first-impressions-demonstrate-the-games-huge-promise/))
- Warhammer II (2018), CA's Al Bickham and Scott Pitkethly: they author matched fighting animations between specific entity pairs for special moments; Bickham's Carnasaur versus dragon example (dragon tries to fly off with the opponent, then gets countered) is kept "rare enough to make them feel special and epic". Animation sets can also be authored or culled per situation, for example "units fighting atop, say, a castle wall" (source: [Game Developer](https://gamedeveloper.com/design/designing-i-total-war-warhammer-ii-i-to-handle-tons-of-units-and-massive-battles))
- Pitkethly on the display: "In a battle scene, the logic generates the 'future' whilst the display renders the 'now'." Every projectile from every entity is physically simulated (source: [Game Developer](https://gamedeveloper.com/design/designing-i-total-war-warhammer-ii-i-to-handle-tons-of-units-and-massive-battles))
- Pitkethly names a readability failure: from the top down camera "the riflemen appear to have line of sight", but at soldier level a small rise blocks fire, so units reposition and look like they ignore orders (source: [Game Developer](https://gamedeveloper.com/design/designing-i-total-war-warhammer-ii-i-to-handle-tons-of-units-and-massive-battles))
- Rome 2 Patch 9: routing units no longer turn around or play matched combat animations while being killed (snippet) (source: [PC Gamer, Patch 9](https://www.pcgamer.com/total-war-rome-2s-giant-patch-9-improves-ai-performance-and-balance))
- Community criticism of matched combat: units "animating into each other" broke unit cohesion and turned infantry melee into "absurd blobs even when there were only two units involved"; matched combat "breaks actual collision" (community, snippet) (source: [Steam, Warhammer II discussion](https://steamcommunity.com/app/594570/discussions/0/3076496088114787038); [Steam, Attila discussion](https://steamcommunity.com/app/325610/discussions/0/617328415062918251))
- Community claims, not official: Rome 2 kill moves could not be interrupted, which hurt collision; Attila's kill rate values were halved after complaints that Rome 2 fights were too slow; Three Kingdoms keeps matched combat only for heroes and large monsters (community, snippet) (source: [Steam, Attila discussion](https://steamcommunity.com/app/325610/discussions/0/617328415062918251); [Steam, Warhammer II discussion](https://steamcommunity.com/app/594570/discussions/0/1520386297687453601/?ctp=2))
- Rome 2 official manual, morale bar on the unit banner: green is fine; "when it turns yellow a morale penalty is in place"; "when it turns red the unit is wavering and about to break and rout". Broken units "will rout" and leave the field (source: [Rome 2 encyclopedia, morale](https://r2enc.totalwar.com/en/manual/single-player/0087_enc_page_battle_play_phase_conflict_morale))
- Rome 2 morale losses: "lengthy, protracted melee", casualties, fatigue, facing overwhelming numbers. Gains: the general's presence, nearby heroes; elite units "have much higher morale from the outset". Losing the general while units rout makes them shattered; an army with no general that routs is shattered; "Units whose morale is shattered will not respond to the command to rally" (source: [Rome 2 encyclopedia, morale](https://r2enc.totalwar.com/en/manual/single-player/0087_enc_page_battle_play_phase_conflict_morale))
- Warhammer: flank or rear attacks cause more casualties and leadership loss (official Academy, snippet); a unit attacked from more than one side loses resolve much faster (community wiki, snippet); Warhammer III leadership penalties come from total HP lost in the battle, HP lost in the last 60 seconds, and HP lost in the last 4 seconds, each separately (guide, snippet); the banner flashes when wavering and turns white when routing; daemons take damage over time instead of routing; unbreakable units never rout (source: [Total War Academy](https://academy.totalwar.com/battle-basic-tactics/); [Screen Rant, WH3 unit stats](https://screenrant.com/total-war-warhammer-3-unit-stats-guide/); [Gamepressure, WH leadership](https://www.gamepressure.com/totalwarwarhammer/leadership-and-unit-fatigue/z38a4f))
- Lord loss penalty reported as minus 16 leadership (community, unconfirmed) (source: [Steam, WH discussions](https://steamcommunity.com/app/594570/discussions/0/1771511442687123095))

### Inferences
- For Kindred Siege: Total War itself backed away from "every fighter in a paired animation". Paired animations cost speed and collision and become noise when they are the default. CA's answer is a split: most blows are one body swings plus a reaction (synced), and true paired set pieces are rare kills or big moments. This maps onto the locked pairs rule: the pairing (who fights whom) is a sim fact that should always be visible, but the paired animation should be reserved for the finisher or the leader's parry of a big hit.
- Morale is the readable "end" of a fight in Total War: a three colour bar on the banner, then a body level change (men turn and run). A small auto battler can copy the colour ladder on each pair (steady, wavering, broken) cheaply, and the run away is the payoff that tells the eye "this side lost here".
- The HP lost in the last 4 s and last 60 s rule means burst damage breaks units, which turns a big hit into a visible morale event, not only a number.

### Gaps
- No official numbers found for Total War charge bonus decay, flank and rear leadership values, or the wavering and broken thresholds; wiki pages blocked. Recall (unverified): Warhammer charge bonus applies for a short window after contact and needs a run up; rear attacks give a large leadership hit. Needs data mining or the fandom wiki read by hand.
- No primary CA statement found that says matched combat was "removed" from Rome 2; only community claims and a patch note about routers.
- Formation depth effects (rank count, line width) not sourced this session.

## 2. Heroes of Might and Magic 3 and Songs of Conquest

### Takeaway
HoMM3 is readable because only one stack acts at a time, in a fixed speed order, on a small 15 by 11 hex field, and every exchange is attack then one counterattack. Songs of Conquest keeps the formula but shrinks stacks (caps around 10 to 50 by tier) and adds elevation.

### Cited Findings
- Battlefield is 15 hexes wide by 11 tall; most units take 1 hex, some 2 hexes wide; Speed is both hexes moved per turn and turn order, higher first (this replaced HoMM2's separate initiative). Wait moves the stack's turn to the end of the round; Defend spends the turn for a big Defense boost (source: [Gamepressure, Heroes 3 HD combat](https://www.gamepressure.com/heroes3hd/combat/z97369))
- Round phases: creatures act from highest speed down; in the wait phase they act in reverse, slowest first (snippet) (source: [GameSpy archive HoMM training](https://gamespy-archives.quaddicted.com/sites/www.strategyplanet.com/homm/mac/training/combat.html))
- Retaliation: a stack normally retaliates once per round; a second attack on the same stack in the same round gets no retaliation; a few units are exceptions with more retaliations (snippet) (source: [GameSpy archive](https://gamespy-archives.quaddicted.com/sites/www.strategyplanet.com/homm/mac/training/combat.html); [GOG forum](https://gog.com/forum/heroes_of_might_and_magic_series/counter_attack_question))
- Songs of Conquest: stack caps by tier, "more like 50, or 10, depending on a creature's tier"; caps can be raised by research and artifacts; hex elevation gives high ground and longer range for shooters; troops generate essence for the commander's spells (source: [Gameskinny review](https://www.gameskinny.com/culture/songs-of-conquest-early-access-review-a-breath-of-fresh-air-for-strategy-games/); [Songs of Conquest changelogs](https://songsofconquest.com/changelogs))

### Inferences
- The once per round retaliation is a pairing rule: every hit has an answer from the one it hit, so two stacks "trade blows" readably. It is the closest classic analogue to Kindred Siege's locked pairs and the leader parry. It also creates the gang up tactic (first attacker eats the counter, second one is free), which is a clear, legible decision.
- Strict one at a time acting is HoMM's main readability lever. Kindred Siege's own "one blow at a time" rule (rules 40) is the same lever.

### Gaps
- Not sourced this session (recall, unverified): no retaliation creatures (e.g. Vampire Lords), unlimited retaliation (Griffins get 2, Royal Griffins unlimited), shooters do half damage in melee and half at long range unless they have no penalty, morale gives a 1 in 24 per point extra turn, luck doubles damage. Need a working HoMM3 wiki.
- No Lavapotion dev talk on combat readability found.

## 3. Battle Brothers

### Takeaway
Overhype replaced a hidden morale number with five named states shown on each unit, explicitly so players can read "how everyone is doing" at a glance and act before a man breaks. Morale checks fire only on visible events (an ally dies, a heavy wound, an enemy dies), so cause and effect is on screen.

### Cited Findings
- Five morale states: Confident (+1 to hit and defense), Steady (0, start), Wavering (slightly worse, penalty on later checks), Breaking (much worse, next failure more likely to flee), Fleeing (cannot act, runs from all visible enemies; once at distance it can roll each turn to rally, success returns it to Wavering) (source: [Battle Brothers Dev Blog #20](https://battlebrothersgame.com/?p=883))
- Checks are a roll against Bravery, difficulty set by event severity. Negative triggers: seeing an ally cut down, taking heavy wounds, the death of a stronger character or one closer by (harder check). Positive trigger: an enemy slain, scaled by its power and distance (a Goblin Chieftain's death helps more than a random goblin's). Undead are immune (source: [Dev Blog #20](https://battlebrothersgame.com/?p=883))
- Design intent: morale should be "easy to read" with a clear continuum so you can see at a glance how everyone is doing; every weighty action gets a morale reaction; players and AI should use it (hit low morale flanks, kill leaders to cause disarray); veterans hold the front as losses mount. The old system was a big abstract number changing each turn and was replaced (source: [Dev Blog #20](https://battlebrothersgame.com/?p=883))
- Spearwall and zone of control read as movement denial: smart enemies try to go around a spearwall; spears hold a zone for a turn or two rather than doing much damage (community) (source: [Steam discussions](https://steamcommunity.com/app/365360/discussions/0/2673382217196990722))
- A lone low level brother went from steady to fleeing in a few turns against several thugs; steady to wavering after only two enemies engaged (community) (source: [Steam discussions](https://steamcommunity.com/app/365360/discussions/0/3158630999998210776))

### Inferences
- Directly usable for Kindred Siege: discrete named morale states tied to visible events (my partner fell, I took a big hit, I killed mine) give every pair a readable story. "Kill the leader to cause disarray" lines up with the leader rules (rules 43): the leader's fall can be the big negative check for his band.
- Fleeing as "cannot act, moves away" is a body level change the eye catches instantly, the same lesson as Total War routing.

### Gaps
- Zone of control rule (free attack when leaving an engaged tile), shield wall and spearwall numbers, and the injury system were not sourced this session. Recall (unverified): leaving a zone of control provokes a free attack; spearwall attacks anyone entering the zone and stops them on a hit; injuries come from damage above a threshold and are named per body part. Need the official wiki.

## 4. Company of Heroes

### Takeaway
Suppression and cover make positions readable because the state is shown at both the UI (squad icon flashing) and body level (men drop prone, crawl). No primary Relic source on suppression readability was found.

### Cited Findings
- In vanilla CoH2 the whole squad icon flashed yellow when suppressed or pinned, "leaving no doubt"; the poster argues later changes made it harder to read (community) (source: [coh2.org visibility thread](https://coh2.org/topic/2323/visibility))
- Hidden suppression meter thresholds: 0.2 to be suppressed, 0.6 to be pinned (community testing) (source: [coh2.org suppression thread](https://coh2.org/topic/24312/why-some-units-stay-suppressed-so-long/post/218047))
- Cover gives "courage" so squads shed suppression faster; squads in green (heavy) cover recover quicker; one or two members standing in the open can soak enough suppression to pin the whole squad (community) (source: [coh2.org suppression thread](https://www.coh2.org/topic/24312/title/post/218026))
- Relic on CoH2: the game is about building squads "that you can utilise to solve problems on the battlefield" (source: [The Average Gamer interview](https://www.theaveragegamer.com/?p=10854))

### Inferences
- Squad state as a single shared condition (suppressed, pinned) is easier to read than each man's HP. For a 5v5, one state word or pose per pair or band beats many small numbers.

### Gaps
- No Relic primary source on suppression animations, cover markers (green, yellow, red cover pips), or retreat. Recall (unverified): cover is shown as green, yellow, red shields at the cursor; retreat is a global order that sends squads to base.

## 5. Warcraft 3 and StarCraft: attack timing and impact

### Takeaway
Blizzard RTS units read because every attack has a windup (damage point), an impact moment, and a cancellable backswing; damage happens at a fixed frame of the swing, so the hit lines up with the pose.

### Cited Findings
- WC3 attack sequence: the unit plays its attack animation; after the Damage Point the attack instance is created; the backswing after it can be cancelled to shorten the animation (snippet) (source: [Liquipedia Warcraft, Attack](https://liquipedia.net/warcraft/Attack))
- Footman: animation damage point 0.50 s and backswing point 0.50 s, so the hit lands 0.50 s into the swing and the animation runs another 0.50 s (community, snippet) (source: [Hive Workshop](https://www.hiveworkshop.com/threads/movement-speed-attack-speed-cast-animation.319479/))
- Attack speed bonuses scale damage point and backswing along with cooldown, which caps how fast a unit can attack (community, snippet) (source: [Hive Workshop](https://www.hiveworkshop.com/threads/increase-attack-speed.246885))
- Dota 2 (which inherits WC3's model) documents attack point and backswing per hero (source: [Liquipedia Dota 2, Attack Animation](https://liquipedia.net/dota2/Attack_backswing))

### Inferences
- Kindred Siege's one blow at a time hold (0.5 s per blow on screen) is close to the WC3 footman's 0.5 s windup. The lesson is to put the damage number and hit flash on the exact impact frame, and keep a short visible follow through.

### Gaps
- Blizzard art direction on silhouettes and team colour, StarCraft projectile speeds, and Age of Empires data were not reached in the budget. Field of Glory, Hard West, Wartales, Mount and Blade, King's Bounty and Kingdom Wars were not researched; no findings.

## 6. What makes the eye understand "the line broke" or "this unit is losing"

### Takeaway
Across sources the same pattern recurs: a coarse, few step state shown on the unit (colour bar, named state, flashing icon), changed only by visible events, ending in a body level change (turn and run, go prone). Paired animations help drama but hurt clarity and flow when they are the default.

### Cited Findings
- Total War: three colour banner bar, then rout, white banner, flashing when wavering (source: [Rome 2 encyclopedia](https://r2enc.totalwar.com/en/manual/single-player/0087_enc_page_battle_play_phase_conflict_morale); [Screen Rant](https://screenrant.com/total-war-warhammer-3-unit-stats-guide/))
- Battle Brothers: five discrete states replacing a hidden number, for at a glance reading (source: [Dev Blog #20](https://battlebrothersgame.com/?p=883))
- CoH2: squad icon flash for suppressed or pinned (community) (source: [coh2.org](https://coh2.org/topic/2323/visibility))
- Total War Warhammer: matched animations kept rare so they feel "special and epic" (source: [Game Developer](https://gamedeveloper.com/design/designing-i-total-war-warhammer-ii-i-to-handle-tons-of-units-and-massive-battles))

### Inferences
- For "units do whatever they want": every reference game makes who fights whom a hard rule the player can see. HoMM3 has one attacker, one target, one counter. Battle Brothers locks engaged units with zone of control. Total War locks units in melee and shows it as a block against a block. Kindred Siege's locked pairs are the right lever; the screen should always show the pair link, and morale should be per pair or per band in 3 to 5 named steps with a visible run away at the bottom.
- Reserve paired kill animations for the finisher and the leader's parry. Everything else should be one body swings with a reaction on the impact frame.

### Gaps
- No dev talk found on camera framing for mass battles (Total War's camera, HoMM's fixed side view). Not researched further.
