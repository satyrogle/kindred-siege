# Squad tactics and party combat: how XCOM, Gears Tactics, Darkest Dungeon, Banner Saga, Phoenix Point, Mutant Year Zero and Midnight Suns make combat coherent, readable and exciting

Scope note: about 30 web lookups. Several primary pages were blocked (Unreal Engine developer interviews 403, Fandom wiki 402, PC Gamer article bodies truncated). Where a fact comes only from a search engine summary of a page, it is marked "(via search summary)". Player measurements and wiki data are marked as such. Items I remember but could not source are in Gaps, not in Findings.

Kindred Siege context used for the flags: 2.5D pixel art, 5v5 auto battler, each band on its own 3 by 3 grid, units locked in pairs that trade blows, each class has a panel shape, a leader parries big hits, the player mostly watches. Findings that matter most for a game that plays itself are tagged **[AUTO]**.

## 1. Action economy: XCOM two actions and overwatch, Gears Tactics AP and executions, Banner Saga alternation, Darkest Dungeon speed and ranks, and why

### Takeaway
Every one of these games replaced fine grained time accounting with a small, countable budget (2 actions, 3 AP, 1 turn per round, 3 card plays) because the granular version was "too cumbersome" and slowed pace; the budget is then bent by one signature rule (attacking ends the turn, executions refund AP, one unit per side alternates, speed plus a d8 roll) that gives each game its rhythm.

### Cited Findings
**XCOM: Enemy Unknown / XCOM 2 (Firaxis)**
- Firaxis prototyped with the original game's Time Units first, then switched: Jake Solomon said they "found the action/move-action system to be far more engaging." — [GameBanshee, XCOM EU lead designer responds](https://gamebanshee.com/ddq) (via search summary)
- Playtesting found Time Units, and the many unit attributes that went with them, "just too cumbersome"; the team moved to a tabletop style design with simpler stats, class skills and two actions per turn. Defence moved from dodge and armour reduction to avoidance through cover; armour now adds HP. — [Game Developer, A Deep Dive Into XCOM and XCOM 2](https://www.gamedeveloper.com/design/a-deep-dive-into-xcom-and-xcom-2)
- Each soldier gets two actions per turn; attacking always ends that soldier's turn (Skirmisher class is the exception in XCOM 2). — [Game Developer deep dive](https://www.gamedeveloper.com/design/a-deep-dive-into-xcom-and-xcom-2)
- Cover numbers: partial cover reduces the chance to be hit by 25 points, full cover by 50; Hunker Down reduces it by a further 50% and is the only universal special option. — [Game Developer deep dive](https://www.gamedeveloper.com/design/a-deep-dive-into-xcom-and-xcom-2)
- Solomon framed cover as the way "to emphasize positioning as part of the combat system", and the move/action system as a more direct way of carrying out a plan, with Heavy, Assault and Support each taking a different role in a turn. — [Gamingbolt, Aftermath interview with Jake Solomon](https://gamingbolt.com/aftermath-xcom-enemy-unknown-interview-with-jake-solomon) (via search summary)
- Overwatch is described by one analyst as "the only way to lock down enemies and get a shot in when they move"; War of the Chosen's Chosen can become immune to it. — [Game Developer deep dive](https://www.gamedeveloper.com/design/a-deep-dive-into-xcom-and-xcom-2)
- Enemies sit in pods at set map points; revealing a pod activates it at once and it scrambles into cover; pulling two pods can leave the squad outnumbered "two or three to one." — [Game Developer deep dive](https://www.gamedeveloper.com/design/a-deep-dive-into-xcom-and-xcom-2)
- The postmortem credits dropping Time Units and capping the squad at six with making "each decision matter" and pushing group tactics. — [Game Developer, Classic Postmortem: XCOM Enemy Unknown](https://www.gamedeveloper.com/design/classic-postmortem-i-xcom-enemy-unknown-i-which-turns-5-today)
- Critique: because attacking ends the turn you cannot fire then retreat. — [Game Developer deep dive](https://www.gamedeveloper.com/design/a-deep-dive-into-xcom-and-xcom-2)

**Gears Tactics (Splash Damage / The Coalition)**
- Three AP per unit per turn, spent on any mix of move, shoot, abilities. — [Game Informer preview](https://www.gameinformer.com/preview/2020/03/27/gears-tactics-might-have-what-it-takes-to-compete-with-xcom)
- Why three: Tyler Bielman said one move plus one shoot action per activation is "very restrictive." — [Unreal Engine developer interview](https://www.unrealengine.com/en-US/developer-interviews/gears-tactics-uses-the-gears-formula-brilliantly-to-offer-a-fresh-tactical-experience) (page 403, via search summary); Wikipedia summarises the same decision — [Wikipedia, Gears Tactics](https://en.wikipedia.org/wiki/Gears_Tactics) (via search summary)
- Executions: a heavily damaged enemy enters a downed state (crawls, can be revived by allies); a melee execution removes it and gives every other squad member one extra AP that turn, so chained executions extend a turn. — [Game Informer preview](https://www.gameinformer.com/preview/2020/03/27/gears-tactics-might-have-what-it-takes-to-compete-with-xcom)
- Bielman called the squad wide bonus AP for an execution "a big breakthrough", and said the goal was to let you "play more aggressively" than other turn based games; executions let you push forward through the map without spending much on movement. — [Unreal Engine developer interview](https://www.unrealengine.com/en-US/developer-interviews/gears-tactics-uses-the-gears-formula-brilliantly-to-offer-a-fresh-tactical-experience) (via search summary)
- Bielman: "We wanted it to feel a lot more intense than traditional tactics games." Movement is free, not gridded. — [Game Informer preview](https://www.gameinformer.com/preview/2020/03/27/gears-tactics-might-have-what-it-takes-to-compete-with-xcom)
- Overwatch: a unit that enters overwatch with three AP can fire up to three times at enemies entering its field of view (AP becomes reaction shots). — [Game Informer preview](https://www.gameinformer.com/preview/2020/03/27/gears-tactics-might-have-what-it-takes-to-compete-with-xcom)
- Heavy class: each successive attack does more damage as long as the unit does not move between shots. — [Game Informer preview](https://www.gameinformer.com/preview/2020/03/27/gears-tactics-might-have-what-it-takes-to-compete-with-xcom)

**The Banner Saga (Stoic)**
- Turns strictly alternate, one player unit then one enemy unit, whatever the numbers on each side, "vaguely chess like." — [GameBanshee review](https://gamebanshee.com/4w9h) (via search summary)
- Exception: Pillage mode, when one side is down to one unit, the other side acts with all its units. — [GameBanshee review](https://gamebanshee.com/4w9h) (via search summary); [Matchstick Eyes analysis](https://www.matchstickeyes.com/2014/02/02/the-banner-saga-turn-based-tactics-with-a-twist/)
- Consequence noted by players: killing weak "chaff" first is a mistake because it hands the strong survivors more turns; so the optimal play is to wound many rather than kill. One reviewer called it "a bizarre design decision." — [Matchstick Eyes](https://www.matchstickeyes.com/2014/02/02/the-banner-saga-turn-based-tactics-with-a-twist/); [GameBanshee review](https://gamebanshee.com/4w9h) (via search summary)
- Stats: Strength is both HP and damage (a wounded unit hits weaker), Armor is a buffer you must break, Break is how much armour a hit removes, Willpower is spent on abilities and extra movement, Exertion caps how much Willpower can be spent at once. — [Banner Saga Wiki, Stats](https://bannersaga.fandom.com/wiki/Stats) (via search summary); [Stoic press release on combat](https://pressreleases.triplepointpr.com/2013/12/05/stoic-reveals-more-detail-on-combat-in-the-banner-saga/)
- Stoic's stated aim: "we've made every stat equally important even though they all do wildly different things"; later the founders admitted players felt overwhelmed by strength/armor/willpower plus two abilities per character. — 2012 Stoic dev blog via [azerothtozork repost](https://azerothtozork.blogspot.com/2012/11) (secondary repost, via search summary)
- I found no developer statement giving the reason for strict alternation. Alex Thomas said combat and usability changed "over a dozen times." — [GameBanshee interview](https://gamebanshee.com/9r3k) (via search summary)

**Darkest Dungeon (Red Hook)**
- Turn order each round: every unit's round speed = SPD + random roll 1 to 8; highest acts first; ties go to heroes, then to the unit nearer the enemy. Most units act once per round; some bosses act several times. — [Darkest Dungeon wiki.gg, Combat Mechanics](https://darkestdungeon.wiki.gg/wiki/Combat_Mechanics_(Darkest_Dungeon)) (community wiki, data mined)
- Wiki table of how likely the faster unit acts first: equal SPD 56.25%, +1 67%, +2 77%, +3 84%, +4 91%, +5 95%, +6 98%, +7 or more 100%. — [wiki.gg, Combat Mechanics](https://darkestdungeon.wiki.gg/wiki/Combat_Mechanics_(Darkest_Dungeon))
- Ranks run 1 (nearest the enemy) to 4 (farthest). Large monsters fill 2, 3 or 4 ranks; an attack that touches any of their ranks hits them once. — [wiki.gg, Combat Mechanics](https://darkestdungeon.wiki.gg/wiki/Combat_Mechanics_(Darkest_Dungeon))
- Origin of ranks: the four on four line up came from imagining "a spear reaching deep into enemy ranks"; Chris Bourassa: "You couldn't hit somebody all the way behind three other guys with a sword." Bard's Tale was the start point (front melee, back ranged or magic); Tyler Sigman said four on four gave more design room than Bard's Tale. — [GameBanshee summary of RPS interview](http://www.gamebanshee.com/kucrr)

**Phoenix Point (Snapshot Games)**
- Action Points pay for movement and standard attacks; Will Points pay for class abilities; a unit's Willpower stat sets its starting and maximum WP; pushing WP below zero can cause panic. — [RPG Codex interview with Julian Gollop](https://rpgcodex.net/content.php?id=10618) and [Facebook Live Q&A](https://phoenixpoint.info/blog/2018/1/22/julians-live-question-and-answer-session-on-facebook-live) (via search summary)
- Gollop called it "an implicit action points system" that they kept testing and retuning; equipment changes movement costs. — [Phoenix Point Facebook Q&A](https://phoenixpoint.info/blog/2018/1/22/julians-live-question-and-answer-session-on-facebook-live) (via search summary)
- A 2017 backer vote on AP versus Time Units showed "a slight preference towards Action Points" with the biggest vote in the middle. — [RPG Codex interview](https://rpgcodex.net/content.php?id=10618) (via search summary)

**Marvel's Midnight Suns (Firaxis)**
- Main resource is "card plays", an explicit count of cards you can play from hand each turn; Heroism is a second resource earned by playing Attack and Skill cards and spent on Heroic cards and on Environment Attacks (which cost no card play). — [PlayStation Blog explainer](https://blog.playstation.com/?p=371251) and [Gameranx](https://gameranx.com/updates/id/388378/article/firaxis-explains-combat-system-of-marvels-midnight-suns/) (via search summary)
- Joe Weinhoffer's GDC 2024 talk "From Soldiers to Superheroes": heroes use no defensive cover, no movement restrictions and no chance to hit rolls. — [GDC Vault session page](https://gdcvault.com/play/1034622/From-Soldiers-to-Superheroes-Designing)
- Firaxis started from XCOM 2's combat and stripped it; Weinhoffer: heroes flip the dynamic, "All the enemies are scared of them." Cover was dropped because it did not fit heroes being the superior force. — [Digital Trends interview](https://digitaltrends.com/gaming/marvels-midnight-suns-director-interview); [Den of Geek](https://www.denofgeek.com/?p=767470) (via search summary)
- On randomness: "it's a random pull but you know exactly what your card does" (Weinhoffer). — [Digital Trends](https://digitaltrends.com/gaming/marvels-midnight-suns-director-interview) (via search summary); also [Stevivor, "XCOM's randomness doesn't work in Midnight Suns"](https://stevivor.com/features/interviews/xcoms-randomness-doesnt-work-in-marvel-midnight-suns-says-firaxis/)
- Free hero movement is what made knockback and environmental attacks the positioning layer. — [Unreal Engine developer interview](https://www.unrealengine.com/en-US/developer-interviews/firaxis-games-talks-about-designing-and-developing-the-superhero-tactical-experience-of-marvel-s-midnight-suns) (403, via search summary)

**Mutant Year Zero (The Bearded Ladies)**
- Two phases: real time exploration and stealth, then turn based combat meant to emulate the pen and paper game's dice. An open world plan was dropped because it did not blend with turn based combat. — [Wikipedia, Mutant Year Zero](https://en.wikipedia.org/wiki/Mutant_Year_Zero) (via search summary)

### Inferences
- The shared lesson: the budget per unit is tiny (1 to 3) and readable at a glance. **[AUTO]** For a fight that plays itself, the viewer can only follow a budget they can count. Kindred Siege pairs trading blows is close to Banner Saga's strict alternation inside each pair; that makes each exchange a readable "A then B" beat.
- Banner Saga shows the risk of strict alternation: it changes who you want to kill (wound, don't kill). If pairs re-lock after a death, Kindred Siege should check the same perverse incentive in its bot sweeps.
- Gears Tactics' execution refund is a momentum rule: a kill speeds up the rest of the squad. **[AUTO]** In an auto battle a visible "kill pays the team" rule produces cascades the viewer can see build, which is exciting to watch.
- Darkest Dungeon's SPD + d8 means order is mostly predictable but not certain; the ordering itself is a small drama each round. A pure auto battler can show the order bar so the viewer knows who is next.

### Gaps
- No primary source found for why Stoic chose strict one for one alternation, or why Pillage exists.
- Could not read Bielman's full Unreal interview (403); his statements come from search summaries.
- From memory, not sourced: Midnight Suns gives 3 card plays, 2 redraws and 1 move per turn; Gears Tactics squads are 4 units; XCOM 2 squads are 4 to 6. Treat as unverified.

## 2. Darkest Dungeon in depth: fixed ranks as class identity, camera zoom, hit stop, narrator, timing, the zoom mods, Red Hook talks

### Takeaway
Darkest Dungeon's 1D, four rank line makes identity visible because each skill has a printed "usable from" and "hits" rank set, so where a hero stands tells you what it can do; presentation is a side on view that punches in on each action with a gravel voiced narrator; but a very large share of players paid to cut that presentation down (zoom removal and 2x combat speed mods), which is the single most important warning for a watch heavy game.

### Cited Findings
**Ranks and identity (community wiki data, read from the in game rank icons)**
- Crusader (base SPD 1, the slowest hero): Smite, Zealous Accusation and Stunning Blow usable only from ranks 1 to 2, hitting enemy ranks 1 to 2; Holy Lance usable from ranks 3 to 4, hits ranks 2 to 4 and moves him forward 1 rank; Battle Heal and Inspiring Cry usable from any rank. — [wiki.gg, Crusader](https://darkestdungeon.wiki.gg/wiki/Crusader). Bulwark of Faith (from ranks 1 to 2) gives +20 to 30% PROT and marks himself, once per battle. Caution: the fetch tool printed every Crusader launch rank mirrored (Smite "3 to 4", Holy Lance "1 to 2"), which contradicts Holy Lance's "move forward" effect; I mirrored all launch ranks consistently, as for the Highwayman below. Verify on the page before quoting.
- Highwayman (base SPD 5, rising to 7): Wicked Slice from ranks 1 to 3 hitting 1 to 2; Pistol Shot from 2 to 4 hitting 2 to 4; Point Blank Shot only from rank 1, hits rank 1 with +50% damage, knocks the target back 1 and pushes the Highwayman back 1; Grapeshot Blast from 2 to 3, hits several enemy ranks at -50% damage; Duelist's Advance from 2 to 4 hitting 1 to 3, moves him forward 1 and sets Riposte for 3 rounds; Open Vein (from 1 to 3, hits 1 to 2) bleeds 2 per round for 3 rounds and slows. — [wiki.gg, Highwayman](https://darkestdungeon.wiki.gg/wiki/Highwayman). Caution: the fetch tool read the hero launch rank icons mirrored (it printed Point Blank Shot as "rank 4 only", Wicked Slice as 2 to 4, Pistol Shot as 1 to 3). The wiki draws hero ranks right to left (4 3 2 1), so I mirrored every launch rank consistently; target ranks were not mirrored. Verify on the page before quoting exact ranks.
- So a single hero carries moves that only work from specific ranks, and some moves reposition the user (Holy Lance forward, Point Blank Shot back, Duelist's Advance forward). Formation shuffles (enemy knockback, pulls) can strand a hero in a rank where most of its kit is unusable. The RPS interview summary names exactly this tension: an enemy "ability can disrupt positioning and leave heroes useless." — [GameBanshee summary of RPS interview](http://www.gamebanshee.com/kucrr)
- Accuracy: hit chance = skill ACC minus target DODGE plus a hidden +5; shown 95% or more is a sure hit; floor 5%; each consecutive miss after the first adds a hidden +4 ACC (a bad luck cushion). Crit = 1.5 times the top of the damage range. PROT caps at 80%. Heroes at 0 HP enter Death's Door rather than die. — [wiki.gg, Combat Mechanics](https://darkestdungeon.wiki.gg/wiki/Combat_Mechanics_(Darkest_Dungeon)) (data mined community figures)

**Camera and view**
- One side on view is used for both exploring and combat, with no fade to a separate battle screen; Bourassa disliked that mode switch. Part reason was budget, one artist for much of development. — [GameBanshee summary of RPS interview](http://www.gamebanshee.com/kucrr)
- Bourassa: "We knew we wanted to get down and close to the characters and show them off a little bit." Big, detailed figures make heroes feel vulnerable and worth caring about. — [GameBanshee summary of RPS interview](http://www.gamebanshee.com/kucrr)
- In Darkest Dungeon II each attack gets a cutaway as the blade or bullet lands; Bourassa described the Highwayman's Wicked Slice: he whips the knife out and readies, and on target click follows through; the camera pulls back to catch the heavy follow through. — [PC Gamer DD2 interview](https://pcgamer.com/interview-darkest-dungeon-2s-new-3d-look-monsters-the-narrator-and-its-surprising-aspirational-spirit) and [PressReader PC Gamer excerpt](https://www.pressreader.com/usa/pc-gamer-us/20210615/281672552879563) (via search summary; article body was truncated when fetched)

**The zoom and speed mods (player behaviour data)**
- "Less Combat Zoom Tweak" (Steam Workshop, 2015) removes the zoom on ranged hits, melee hits and skills but keeps it on critical hits, so that crits feel stronger; the author wrote that "some people have an issue with the way the camera zooms in during combat." — [Steam Workshop discussion / mod](https://steamcommunity.com/app/262060/discussions/4/618453594749180956/) (via search summary)
- "x2 Combat Speed" by o'nite (Workshop, 18 Aug 2018) doubles battle speed including animations by enabling the game's own acceleration during battles; about 266,795 subscribers, 9,752 favourites; the page lists a separate "No Camera Zoom" mod as compatible, and points to a milder "+50% Combat Speed." The page now shows a removal notice. — [Steam Workshop, x2 Combat Speed](https://steamcommunity.com/sharedfiles/filedetails/?id=1483754805)
- A Nexus mod page for Darkest Dungeon camera behaviour also exists. — [Nexus Mods, Darkest Dungeon mod 371](https://www.nexusmods.com/darkestdungeon/mods/371) (not opened; content unverified)

**Narrator**
- Voiced by Wayne June (first heard in the original trailer); a disembodied voice that comments sardonically on wins and nods knowingly at failures; he gets excited only on big hits or multi kills; the team recorded multiple takes of many lines to vary delivery. — [Game Developer, Designing for despair](https://gamedeveloper.com/business/-i-darkest-dungeon-i-designing-for-despair-and-kicking-you-when-you-re-down)
- Bourassa on tone: enemy crit chance is tuned to tell the player "Yes you're underpowered, yes you're vulnerable, focus up." — [Game Developer, Designing for despair](https://gamedeveloper.com/business/-i-darkest-dungeon-i-designing-for-despair-and-kicking-you-when-you-re-down)
- Bourassa: "a game is a lot like a band, and if you try to make every feature the lead guitarist the whole thing is going to fall apart." Sigman: stress and affliction are "a cherry on top" of combat that had to be fun on its own first. — [Game Developer, Designing for despair](https://gamedeveloper.com/business/-i-darkest-dungeon-i-designing-for-despair-and-kicking-you-when-you-re-down)

**Red Hook talks**
- Sigman gave a GDC postmortem focused mostly on the Affliction and stress systems, not combat staging. — [GamesBeat](https://gamesbeat.com/darkest-dungeon-designers-really-arent-sadists-were-realists/) (via search summary)
- Sigman and Bourassa's IGS talk "Partnership on 'Darkest Dungeon': The Double-Edged Sword" (GDC 2018) is about the two founder partnership, not combat. — [GDC Vault](https://gdcvault.com/play/1024985/contactUs)
- Game Developer also ran "Game Design Deep Dive: Darkest Dungeon's Affliction system." — [Game Developer](https://gamedeveloper.com/design/game-design-deep-dive-i-darkest-dungeon-s-i-affliction-system) (page showed a different headline when searched; not read)

### Inferences
- **[AUTO]** The rank system is the closest match to Kindred Siege's class panel shapes: a printed "from these cells, hits these cells" diagram per move is what made Darkest Dungeon classes legible in one look. Kindred Siege can show the panel shape the moment a move fires, the way Darkest Dungeon's dots show launch and target ranks.
- **[AUTO]** The mods are the clearest data point in this whole brief: about 267 thousand players subscribed to a mod that halves the time of every action, and the zoom mod kept zoom only for crits. In a game where the player watches every action, the per action cinematic is a cost paid every few seconds. Reserve camera punch ins for rare beats (crits, kills, the leader's parry) and keep normal exchanges short.
- The hidden +4 ACC per consecutive miss is a quiet anti frustration rule; useful where the player cannot intervene after a miss streak.
- The narrator is a cheap layer that turns random outcomes into story ("successes surprising, defeats inevitable"), which suits fights the player does not control.

### Gaps
- No Red Hook talk on combat staging, zoom timing or hit stop found. No official per action duration (seconds) found; the speed mods imply the default felt slow to many, but no measured default exists in my sources.
- No hit stop frame counts for Darkest Dungeon found.
- Rank tables were read by a fetch tool from icon grids and partly contradict known play; recheck on the wiki pages before quoting exact ranks.
- Party size: four heroes per side is in the Bard's Tale comparison ("four on four") but monster party sizes and their rank sizes vary by encounter; no count table found.

## 3. XCOM presentation: action camera, kill cams, hit chance display, cover icons, shot staging, Jake Solomon's talks

### Takeaway
XCOM keeps a fixed tactical camera with 90 degree steps and swoops in only for the aiming view and for visceral moments; readability came from cutting clutter (sightline lines were removed after months of work) and from a single uniform Shot HUD, while the cinematic camera is a reward layer on top.

### Cited Findings
- Solomon: "we have a fixed camera with 90-degree rotations on the D-pad"; "When you go into aiming view over the shoulder, that's when the camera will swoop down"; "free camera has very limited value"; pulling in is meant to make moments "feel a little more visceral at times"; you can zoom out for a tactical overview. — [GamesBeat interview](https://gamesbeat.com/xcom-enemy-unknown-interview/)
- Camera disorientation was named a major concern for this kind of game, which is why free camera was limited. — [GamesBeat interview](https://gamesbeat.com/xcom-enemy-unknown-interview/) (via search summary)
- Players and press singled out the swoop in during aiming and the soldiers' slide into cover at the end of a move as "little things" that set it apart. — [Yahoo, How well does XCOM Enemy Within](https://www.yahoo.com/entertainment/well-does-xcom-enemy-within-214329382.html) (via search summary)
- Readability cuts: sightlines from every unit became a "plethora of multicolored lines" and were removed after months of effort; 3D fog of war was a "nightmare" for graphics and performance. — [Game Developer, Classic Postmortem](https://www.gamedeveloper.com/design/classic-postmortem-i-xcom-enemy-unknown-i-which-turns-5-today)
- Gamepad support needed a uniform "Shot HUD" for all contextual abilities. — [Game Developer, Classic Postmortem](https://www.gamedeveloper.com/design/classic-postmortem-i-xcom-enemy-unknown-i-which-turns-5-today)
- "Find the Fun" was the guiding rule; aim was to be accessible without "dumbing down"; a 90 minute optional tutorial taught one layer at a time. — [Game Developer, Classic Postmortem](https://www.gamedeveloper.com/design/classic-postmortem-i-xcom-enemy-unknown-i-which-turns-5-today)
- Art direction: interview with Greg Foertsch on artistic inspirations exists. — [GamesRadar](https://www.gamesradar.com/uk/xcom-enemy-unknown-interview-we-talk-artistic-inspirations-greg-foertsch/) (not read)

### Inferences
- XCOM's ladder: the tactical view is the default, the cinematic cut is the exception. **[AUTO]** For Kindred Siege the "tactical view" is the 2.5D battle screen; cuts or punch ins should be the exception, the same lesson as the Darkest Dungeon mods.
- The sightline removal is a direct warning: too many simultaneous lines (targets, threat links) read as noise. In a 5v5 with locked pairs, five pair links on screen is the maximum before it turns into the "multicolored lines" problem.

### Gaps
- No primary Firaxis source found that describes the shot stages (aim, fire, impact, reaction) with timings, or names a "kill cam." The postmortem does not cover the action camera.
- No source found for the hit chance display numbers (aim 65 base, etc.) or the shield icon semantics (half shield, full shield, flank yellow). I recall these from play but did not source them.
- No Jake Solomon GDC talk transcript found; the RPS post GDC interview could not be read.

## 4. Telegraphing enemy intent and threat

### Takeaway
These games telegraph in three forms: a marked area that resolves next round (XCOM 2 Sectopod Wrath Cannon), a preview of the consequence of your own move (XCOM 2 concealment red eye tiles, pod activation warnings), and certainty by design (Midnight Suns removed hit rolls so cards do exactly what they say); none of the sources showed Slay the Spire style per enemy intent icons for these games.

### Cited Findings
- XCOM 2 Sectopod Wrath Cannon: the enemy marks an area, then a round later fires on every unit inside it. — [Fextralife XCOM 2 wiki, Sectopod](https://xcom2.wiki.fextralife.com/Sectopod); [StrategyWiki, XCOM 2 Alien Forces](https://strategywiki.org/wiki/XCOM_2/Alien_Forces) (via search summary)
- XCOM 2 concealment: tiles that would reveal you are marked with a red eye, so the game shows in advance that a destination will alert enemies. — [Twinfinite](https://twinfinite.net/?p=165386) (via search summary)
- XCOM 2 pod activation warning (yellow) is not always accurate; Steam users report a pod activating with no warning and a warned tile not triggering one. — [Steam discussion](https://steamcommunity.com/app/268500/discussions/0/1798529872648045787) (anecdotal player reports)
- Gears Tactics enemy roles are distinct by job: defensive front line units, snipers, and rushers that push heroes out of cover. — [Game Informer preview](https://www.gameinformer.com/preview/2020/03/27/gears-tactics-might-have-what-it-takes-to-compete-with-xcom)
- Midnight Suns: no chance to hit rolls; "you know exactly what your card does." — [GDC Vault](https://gdcvault.com/play/1034622/From-Soldiers-to-Superheroes-Designing); [Digital Trends](https://digitaltrends.com/gaming/marvels-midnight-suns-director-interview) (via search summary)
- Darkest Dungeon: tension comes from mechanics rather than jump scares; a plan fails on an unlucky roll, or an enemy move scrambles your formation. — [GameBanshee summary of RPS interview](http://www.gamebanshee.com/kucrr)

### Inferences
- **[AUTO]** The Wrath Cannon pattern (mark now, resolve next beat) is the right telegraph shape for a watched fight: the viewer gets a beat of dread and then the payoff. Kindred Siege's leader parry of big hits already fits: show the big hit marked first, then the parry or the hit.
- A wrong warning (XCOM 2 pod flag) erodes trust fast; in an auto battler where the player cannot test the warning themselves, a telegraph must be exact every time.

### Gaps
- No source found for Gears Tactics boss telegraphs, Midnight Suns enemy intent display, or Phoenix Point and Mutant Year Zero enemy intent UI. From memory (unsourced): Midnight Suns shows enemy targets with lines and marks; Phoenix Point shows enemy body parts as targetable; MYZ shows enemy vision cones in stealth. Unverified.
- No Chosen telegraph source found.

## 5. Unit identity: silhouettes, signature abilities, animation sets

### Takeaway
Identity comes from one rule per class that changes how it is positioned or sequenced (Gears Heavy gets stronger the longer it stays still, Darkest Dungeon heroes are defined by which ranks they fire from and hit, Midnight Suns heroes by their card sets, Phoenix Point by targetable body parts), with art large enough to read and an attack that has a wind up and follow through.

### Cited Findings
- Gears Tactics: five classes with over 30 skills each; the Heavy ramps damage on successive attacks if it does not move. — [Game Informer preview](https://www.gameinformer.com/preview/2020/03/27/gears-tactics-might-have-what-it-takes-to-compete-with-xcom)
- XCOM EU: squad roles (Heavy, Assault, Support) each take a different job in a turn. — [Gamingbolt, Solomon interview](https://gamingbolt.com/aftermath-xcom-enemy-unknown-interview-with-jake-solomon) (via search summary)
- Darkest Dungeon: Crusader is a slow (SPD 1), high HP generalist with self heal and stress relief; his weakness is speed so he pairs with fast stunners. Highwayman is fast (SPD 5 to 7) with moves that shuffle his own rank (Duelist's Advance forward, Point Blank Shot back). — [wiki.gg, Crusader](https://darkestdungeon.wiki.gg/wiki/Crusader); [wiki.gg, Highwayman](https://darkestdungeon.wiki.gg/wiki/Highwayman)
- Darkest Dungeon art: big detailed figures, "get down and close to the characters." — [GameBanshee summary of RPS interview](http://www.gamebanshee.com/kucrr)
- DD2: each attack animates a wind up (knife out, ready) and a follow through on target click, with the camera pulling back to catch it. — [PC Gamer DD2 interview](https://pcgamer.com/interview-darkest-dungeon-2s-new-3d-look-monsters-the-narrator-and-its-surprising-aspirational-spirit) (via search summary)
- Phoenix Point: free aim at individual body parts that disable abilities (shoot the arm to stop the weapon). — [GamesBeat](https://gamesbeat.com/how-phoenix-point-takes-turn-based-tactics-where-the-genre-hasnt-gone/) (via search summary); [Daily Illini Q&A](https://dailyillini.com/buzz-stories/video-games/game-reviews/2021/04/16/qa-julian-gollop-of-phoenix-point-gives-gameplay-advice-2) (via search summary)
- Midnight Suns: hero abilities are cards; the superhero fantasy (enemies fear you) drove cutting cover and dice. — [GDC Vault](https://gdcvault.com/play/1034622/From-Soldiers-to-Superheroes-Designing); [Digital Trends](https://digitaltrends.com/gaming/marvels-midnight-suns-director-interview) (via search summary)
- Banner Saga: Strength is HP and damage at once, so a hurt unit visibly hits weaker; Willpower lets a unit spend for extra movement or damage. — [Banner Saga Wiki, Stats](https://bannersaga.fandom.com/wiki/Stats) (via search summary)

### Inferences
- **[AUTO]** Darkest Dungeon's "from these ranks, hits these ranks" is the identity rule that reads with no player input: you see where the unit stands and what it can reach. Kindred Siege's panel shapes are the same idea on a 3 by 3, so showing the shape on each blow is the identity carrier.
- A class rule tied to time or place (Heavy stays still and ramps) is easy for a spectator to follow because it shows on screen as a build up.
- Banner Saga's Strength = damage means a wounded unit's threat drops visibly; that keeps the board state legible from health alone.

### Gaps
- No primary sources on class silhouette rules in any of these games (no art direction talk found for Gears Tactics or Darkest Dungeon silhouettes specifically).
- No animation frame counts or move set sizes found.

## 6. Concrete numbers: action durations, units per side, turn counts per fight

### Takeaway
Hard numbers are thin in primary sources; the solid ones are XCOM EU's six soldier cap and 20 minute average battle (50 minute max), Darkest Dungeon's four on four with SPD + 1 to 8 initiative, Gears Tactics' 3 AP and plus one AP per execution, XCOM's 25 and 50 point cover, and the Darkest Dungeon 2x speed mod's roughly 267 thousand subscribers.

### Cited Findings
- XCOM EU: squad cap 6; battles tuned to average about 20 minutes, longest under 50; more than 70 unique maps; 90 minute optional tutorial. — [Game Developer, Classic Postmortem](https://www.gamedeveloper.com/design/classic-postmortem-i-xcom-enemy-unknown-i-which-turns-5-today)
- XCOM 2 example: a first mission with 4 soldiers against 10 enemies; larger squads need an upgrade. — [Game Developer deep dive](https://www.gamedeveloper.com/design/a-deep-dive-into-xcom-and-xcom-2)
- XCOM cover: partial 25, full 50; Hunker Down halves hit chance again. — [Game Developer deep dive](https://www.gamedeveloper.com/design/a-deep-dive-into-xcom-and-xcom-2)
- Darkest Dungeon: 4 heroes on 4 ranks; SPD + 1 to 8 per round; crit = 1.5 times max damage; PROT cap 80%; hit floor 5%, shown 95% is a sure hit; +4 hidden ACC per consecutive miss. — [wiki.gg, Combat Mechanics](https://darkestdungeon.wiki.gg/wiki/Combat_Mechanics_(Darkest_Dungeon)); [GameBanshee summary of RPS interview](http://www.gamebanshee.com/kucrr)
- Darkest Dungeon speed mods: x2 Combat Speed about 266,795 subscribers (and a +50% variant exists). — [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=1483754805)
- Gears Tactics: 3 AP per unit, plus 1 AP to every other squad member per execution, overwatch up to 3 shots. — [Game Informer preview](https://www.gameinformer.com/preview/2020/03/27/gears-tactics-might-have-what-it-takes-to-compete-with-xcom)
- Banner Saga: 1 for 1 alternation until one side has 1 unit left. — [GameBanshee review](https://gamebanshee.com/4w9h) (via search summary)

### Inferences
- **[AUTO]** If Darkest Dungeon players chose 2x speed in large numbers, a watched 5v5 with 5 pairs trading blows needs per blow beats short enough that a whole fight stays well under XCOM's 20 minute average; the evidence points to keeping ordinary blows quick and saving holds for rare beats.

### Gaps
- No source gives per action durations in seconds for any of these games. Measuring them would mean timing gameplay video frame by frame (a player measurement, not done here).
- No turn count per fight data found for any title.
- No Gears Tactics, Midnight Suns, Phoenix Point or Mutant Year Zero squad size numbers sourced (my recall: Gears 4, Midnight Suns 3 heroes, MYZ 3 mutants, Phoenix Point up to 8; all unverified).
