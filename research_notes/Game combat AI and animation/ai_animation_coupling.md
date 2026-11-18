# How games make AI decisions line up with animation

Scope note: about 25 tool calls. Several primary pages (PC Gamer, TechRadar, GDC Vault) came back cut off or behind a login, so some findings rest on secondary write ups. Each one is flagged. Frame counts from other games are at 60 fps unless stated. Kindred Siege runs 20 fps, so 1 tick = 50 ms = 3 frames at 60 fps.

## 1. Animation driven versus logic driven: who owns timing? Frame data, root motion, hit stop

### Takeaway
Shipped games split it cleanly: the logic owns the outcome and the schedule, the animation is authored (or retimed) to fit that schedule, and hit stop is a presentation freeze laid on top at the moment of contact. Dead Cells is the clearest sprite example: designers move the keys of an attack to change its gameplay timing, and the art follows.

### Cited Findings
- Dead Cells animations are keyed pose to pose first. Interpolation frames are added only before or after the keys, never between them, so attacks stay snappy and the VFX carry the sense of motion and impact. [Game Developer, Thomas Vasseur deep dive](https://www.gamedeveloper.com/production/art-design-deep-dive-using-a-3d-pipeline-for-2d-animation-in-i-dead-cells-i-)
- Dead Cells gameplay tuning is done on the animation timeline: to nerf a weapon that was too strong, the team slowed its attack by moving the keys. Vasseur: "we can adjust the timing of the animations dozens of times in minutes." The article says changing speed in code alone was not enough. [Game Developer](https://www.gamedeveloper.com/production/art-design-deep-dive-using-a-3d-pipeline-for-2d-animation-in-i-dead-cells-i-)
- Total War: Warhammer II runs battle logic ahead of display. Scott Pitkethly: "the logic generates the 'future' whilst the display renders the 'now'." So logic owns the result and the animation layer gets lead time to stage it. [Game Developer](https://www.gamedeveloper.com/design/designing-i-total-war-warhammer-ii-i-to-handle-tons-of-units-and-massive-battles)
- Enemy attacks are commonly split into three phases: anticipation, attack, recovery. Timing data a designer tunes includes attack frequency, cooldowns, anticipation frames, frames between hits and resolution frames. [Signals and Light, Isaiah Everin Cooper](https://signalsandlight.substack.com/p/how-does-enemy-attack-timing-work)
- Cooper's rule: attacks whose timing is randomised need clearer anticipation poses, because the player cannot predict them. Fixed timing attacks can get less polish. [Signals and Light](https://signalsandlight.substack.com/p/how-does-enemy-attack-timing-work)
- Hit stop in Street Fighter 2 froze both characters for roughly 10 frames. It lets the viewer confirm the hit landed, makes it feel stronger, and cancels resolve when the freeze ends so combo timing is constant. Without hit stop (the article names Dark Souls 2) hits feel weak and are harder to see. Hit sparks mark the contact point. [Critpoints](https://critpoints.net/2017/05/17/hitstophitfreezehitlaghitpausehitshit/)
- Smash Bros scales hit stop with damage. Melee: floor(d/3 + 3), cap 20 frames. Brawl and Smash 4: floor(d x 0.3846 + 5), cap 30. Ultimate: floor(d x 0.65 + 6), cap 30. A 15 percent hit gives 8, 10 and 15 frames in those three games. Electric hits multiply by 1.5. [SmashWiki, Hitlag](https://www.ssbwiki.com/Hitlag)
- Sakurai himself uses the word "hitstop" for this in his Famitsu column. [Source Gaming](https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/)
- A general game feel guide gives the range as about 2 frames for a light jab, 12 for a perfect parry, and says past 20 it starts to read as a bug. LOW CONFIDENCE: this came from a search summary and I did not open the page. Likely source: [gamejuice](https://gamejuice.pages.dev/articles/hit-stop-frame-trick)
- For Honor (motion matching) still gave attacks a separate route: movement transitions needed almost no hand authoring, but attacks used a game logic first transition with hand placed event data allowing interrupts. Secondary source, Japanese press write up of the GDC 2016 talk. [CGWorld](https://cgworld.jp/feature/1604-gdc2016-03-2.html)

### Inferences
- For a deterministic sim at one tick per clip frame, the Dead Cells model fits directly: every move is a frame data row (startup ticks, the contact tick, recovery ticks), the sim schedules by that row, and the sprite clip is drawn with its contact pose on the contact tick. The sim owns timing, the clip is cut to it.
- Converted to 20 fps ticks: SF2's 10 frames is about 3 ticks (167 ms). Smash's 8 to 15 frames for a mid hit is 3 to 5 ticks. The 30 frame cap is 10 ticks (500 ms). So a hit stop ladder of roughly 1 tick light, 3 normal, 5 heavy, 10 for a rare big moment sits inside shipped ranges.
- Hit stop must live in presentation or be a scheduled part of the sim. Critpoints' note that cancels resolve at the end of the freeze is the same point: if the freeze shifts the schedule for one unit and not others, order of events changes. (This matches the project's own earlier finding that pauses inside the sim broke balance.)
- "Tapping each other with knives" is what a pose to pose attack looks like when startup and recovery are missing and there is no freeze, spark or knockback at contact. Dead Cells gets away with few frames only because VFX and impact effects carry the motion.
- Root motion as used in 3D (the clip moves the character) needs a rig and blended clips. The sprite version is a per frame offset table baked into the move's data (lunge forward 6 px on tick 3, slide back on recovery). I found no source that describes this for sprites, so this is inference.

### Gaps
- No primary source found on root motion in 2D sprite games.
- No source found giving Dead Cells per attack frame counts. The article gives no startup or recovery numbers.
- The For Honor detail is from a press write up, not the talk itself. The GDC Vault page showed metadata only.

## 2. Combat directors and paired or synced animations

### Takeaway
Two families of method. A central manager decides who may attack whom and when (Kingdoms of Amalur's stage manager, attack tokens), and matched animation sets make both fighters play two halves of one authored exchange (Dawn of War, Total War). Both exist to make a crowd fight read as a set of clear one on one exchanges.

### Cited Findings
- Dawn of War 4 has a dedicated "combat director". Animation director Thomas Derksen calls the result "synced combat", an expansion of the old sync kills: "Every action that you see in the game really has a counterpart, so you see guys fighting it out between themselves." [FRVR, citing the IGN interview](https://frvr.com/blog/news/warhammer-40000-dawn-of-war-4-massively-expands-the-originals-sync-kill-system/)
- Dawn of War 4 action sets are keyed by matchup and size class. Derksen: "There's a range of actions that only a Terminator can use against other Terminators", and gretchins have their own set matched against other small units. [FRVR](https://frvr.com/blog/news/warhammer-40000-dawn-of-war-4-massively-expands-the-originals-sync-kill-system/)
- Dawn of War 1 and 2 each had dozens of unique kill animations. Dawn of War 4 claims over 1,000 permutations. [FRVR](https://frvr.com/blog/news/warhammer-40000-dawn-of-war-4-massively-expands-the-originals-sync-kill-system/)
- The creative director's stated worry about the system was "is this not responsive enough?" (headline only, body not readable). [TechRadar](https://techradar.com/gaming/warhammer-40-000-dawn-of-war-4-creative-director-says-the-games-advanced-animation-system-is-an-evolution-of-the-melee-system-that-initially-sounded-pretty-much-impossible-our-big-concern-was-is-this-not-responsive-enough)
- Playtesters asked for fights to last longer so they could watch the synced combat (headline only). [PC Gamer](https://www.pcgamer.com/games/rts/dawn-of-war-4-playtesters-want-the-combat-to-take-longer-because-of-how-much-they-enjoy-watching-it/)
- Total War authors matched fighting animations between specific entities. Rare special exchanges (a dragon grabs a Carnosaur, gets dragged down and head butted) are kept rare on purpose. Al Bickham: "These occurrences are rare enough to make them feel special and epic when they do happen." Most of the time both units just play their normal attack clips. [Game Developer](https://www.gamedeveloper.com/design/designing-i-total-war-warhammer-ii-i-to-handle-tons-of-units-and-massive-battles)
- Total War picks or culls whole animation sets by situation (for example units fighting on a wall) so wide moves that look fine in the open are not used where they look wrong. [Game Developer](https://www.gamedeveloper.com/design/designing-i-total-war-warhammer-ii-i-to-handle-tons-of-units-and-massive-battles)
- Total War's matched combat is data: files that name which animation files to use, what conditions must hold, and how units transition. Modder documentation, not Creative Assembly. [RPFM docs](https://frodo45127.github.io/rpfm/api/rpfm_lib/files/matched_combat/index.html)
- Kingdoms of Amalur: Reckoning ("Belgian AI", Michael Dawe). Each character carries a grid of 8 slots around it, plus two budgets: grid capacity (who may stand in) and attack capacity (what attacks may run at once). Worked example: player grid capacity 12, soldier weight 4, troll weight 8, so a second soldier must wait outside. Player attack capacity 10: troll charge costs 6, troll club 4, soldier lunge 5, soldier swing 3, so after the troll charges the soldier can only swing. [Game AI Pro, chapter 28](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter28_Beyond_the_Kung-Fu_Circle_A_Flexible_System_for_Managing_NPC_Attacks.pdf)
- Amalur uses two rings: an outer "approach" circle and an inner "attack" circle. A creature with a slot stands on the outer ring. It steps into the inner ring only when granted a specific attack. Creatures with no slot wait outside both. Dawe: this "helps the player to determine which creatures are immediate melee threats." [Game AI Pro, chapter 28](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter28_Beyond_the_Kung-Fu_Circle_A_Flexible_System_for_Managing_NPC_Attacks.pdf)
- Amalur behaviour rules that make the decision visible: any creature inside the outer ring without permission must leave at once ("made it clear to the player which creatures were attacking"). Waiting creatures stand in front of an empty slot, which gives flanking for free. A creature gives its slot back right after attacking, so attackers rotate in and out. An attacker "locks" its slot for the length of the attack. [Game AI Pro, chapter 28](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter28_Beyond_the_Kung-Fu_Circle_A_Flexible_System_for_Managing_NPC_Attacks.pdf)
- Amalur keeps all spatial reasoning out of the individual creatures. One central "stage manager" assigns slots each frame and may steal or shift them. Cooldowns per creature, per creature type and global stop the same attack being spammed. Difficulty is scaled only by changing the two capacities. [Game AI Pro, chapter 28](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter28_Beyond_the_Kung-Fu_Circle_A_Flexible_System_for_Managing_NPC_Attacks.pdf)
- The Last of Us Part II melee (Min-Lun Chou, GDC 2021): 16 radial lines around the player define "strafe slots" that NPCs fill. Melee attacks and melee moves are data entries with start and end conditions. A "melee behaviors" layer above picks from attack lists by enemy type and situation, and also picks walk or run, close in or hold, engage or evade. Secondary source, Japanese press. [4Gamer](https://www.4gamer.net/games/364/G036471/20210722007/)
- God of War (2018) uses aggression tokens to decide which enemies are actively threatening, and off screen indicators. LOW CONFIDENCE: two auto generated summaries of Mihir Sheth's GDC 2019 talk disagree on whether tokens are dynamic or fixed per difficulty and enemy type. Primary: [GDC Vault, Evolving Combat in God of War](https://gdcvault.com/play/1026423/Evolving-Combat-in-God-of)
- Batman Arkham: guides describe freeflow as rhythm based and animation driven, with each strike making Batman leap to the nearest enemy he is facing. No Rocksteady source on tokens found. [Game Design Skills](https://gamedesignskills.com/game-design/batman-arkham-combat/)

### Inferences
- The Amalur pattern is the most portable to a 5 v 5 deterministic sim: it is integer budgets, ordered slots and a central manager. No rig needed. It answers "playing tag" directly, because units without permission are given a visible job (stand at a ring, face a slot) and attackers step in, strike, step out.
- Synced combat in a sprite game means authoring clips in pairs with a shared contact tick: attacker's strike frame and defender's parry, block, dodge or hit frame land on the same tick, at a fixed pixel spacing. Dawn of War 4's 1,000 permutations rely on 3D rigs and procedural adjustment (one summary says it adapts to relative height and weight; unverified). A sprite game cannot blend, so it needs a small matrix instead: N attack types x a handful of responses, keyed by size class as Dawn of War keys by unit class.
- Total War's lesson on budget: most exchanges are plain attack clips, matched specials are rare by design. Rarity is what makes them read as events.
- Because the sim already knows the roll result (dodge, parry, block, crit, hit) before the contact tick, it can do what Total War does: logic computes the future, presentation picks the paired response clip in time for it to start early enough.

### Gaps
- Could not read the body of the PC Gamer or TechRadar Dawn of War 4 pieces, so no detail on how its director picks a pair at runtime or how it kept orders responsive.
- No primary source found for Batman Arkham or Assassin's Creed attack scheduling. Token talk there is community knowledge, not cited here.
- No Creative Assembly source on exactly how matched combat is triggered and how the damage result is tied to the matched clip.
- God of War token details unresolved (conflicting summaries).

## 3. Motion matching and AI intent; animation tags and events driving AI

### Takeaway
Motion matching is a 3D technique that needs large motion capture sets and per frame search, so it does not transfer to sprite clips. What does transfer is its split: locomotion is picked by the animation system from intent, but attacks stay logic first with hand placed events, and AI moves are data entries with start and end conditions.

### Cited Findings
- Motion matching searches all motion capture data each frame for the best matching frame and jumps to it. Simon Clavet presented it for For Honor at GDC 2016 with the goals of precise gameplay and convincing animation, described as two pillars that usually conflict. [GDC Vault](https://www.gdcvault.com/play/1022985/Motion-Matching-and-The-Road); [Game Watch](https://game.watch.impress.co.jp/docs/news/749136.html)
- For Honor's team ran recurring motion capture sessions, about weekly, to fill gaps in coverage. The write up says this may limit the technique to large studios. [CGWorld](https://cgworld.jp/feature/1604-gdc2016-03-2.html)
- For Honor attacks used a game logic first transition with hand placed event data that allows interrupts, while movement needed almost no hand authoring. [CGWorld](https://cgworld.jp/feature/1604-gdc2016-03-2.html)
- Naughty Dog switched to motion matching for The Last of Us Part II, described as taking a huge set of animations and breaking them into small pieces. [Naughty Dog at GDC 2021](https://www.naughtydog.com/blog/naughty_dog_at_gdc_2021); [GamesBeat](https://gamesbeat.com/the-last-of-us-part-ii-how-naughty-dog-plans-to-surpass-our-expectations/2)
- In The Last of Us Part II, a melee action only fires when its start conditions hold (no other action running, target within reach) and it has end conditions. Enemy "attack tells" (the wind up before damage) were made longer than in Uncharted 4 so players can read them. No frame counts given. [4Gamer](https://www.4gamer.net/games/364/G036471/20210722007/)
- The melee talk's own abstract says melee is more intimate than gunplay, so small glitches in behaviour, visuals or controls get magnified. [GDC Vault, Melee AI in The Last of Us Part II](https://gdcvault.com/play/1027115/Melee-AI-in-The-Last)

### Inferences
- Needs 3D rigs: motion matching itself, procedural pair adjustment, blending. Works at 20 fps sprites: moves as data with start and end conditions, events placed on named frames (contact, cancel open, vulnerable, step), and the AI reading those tags.
- The sprite equivalent of "intent drives locomotion" is choosing the run, strafe, back step or guard walk clip from the AI's current intent rather than one generic run. Naughty Dog's behaviour layer choosing walk versus run and engage versus evade is exactly an intent to clip mapping.
- Tags can drive AI the other way too: an enemy in a tagged recovery window is a punish target, an enemy in a tagged wind up is a parry target. This makes AI decisions a reaction to something the viewer just saw.

### Gaps
- Did not get the Clavet slides or the Naughty Dog motion matching talk content, only abstracts and press.
- No shipped 2D sprite game found that documents an animation tag system feeding its AI.

## 4. Readability: telegraph poses, the illusion of intelligence, barks, body language, animation budgets

### Takeaway
Bungie's rule is that a behaviour the player cannot read is wasted, and state changes must be announced through three channels: speech, posture and one time gestures. Sprite games cover this with few frames per move plus VFX and non animation cues, not with more clips.

### Cited Findings
- Halo (Butcher and Griesemer, GDC 2002, "The Illusion of Intelligence"): the talk is about making enemies "appear to be intelligent". [halo.bungie.org](https://halo.bungie.org/misc/gdc.2002.haloai/talk.html?page=2)
- Halo's first design goal was to make the AI intelligible so the player understands what it is doing. Players had more fun when they could see which state the AI was in. Secondary summary of the talk. [Blog summary](https://henryfinalproject.blogspot.com/2019/04/the-illusion-of-intelligence-talk-by.html)
- Halo's three ways to signal state: dialogue (tone reads even in an alien language), postures (sneak, run, walk stances), and gestures (one time animations such as throwing a grenade or diving for cover). Example: an AI shouts "grenade" when one is thrown. [Blog summary](https://henryfinalproject.blogspot.com/2019/04/the-illusion-of-intelligence-talk-by.html)
- Halo gives each enemy type a personality so reactions make sense: Grunts are cowards, hide under fire and may flee when a nearby Elite dies. Elites get angry and stand their ground. [Blog summary](https://henryfinalproject.blogspot.com/2019/04/the-illusion-of-intelligence-talk-by.html)
- Halo's rule on abilities: an AI may have an ability the player lacks if it is recognisable and its limits are predictable. [Blog summary](https://henryfinalproject.blogspot.com/2019/04/the-illusion-of-intelligence-talk-by.html)
- Telegraph length rule of thumb: assume a 0.25 second reaction time (about 8 frames at 30 fps, 15 at 60 fps) and add it to the time the answer takes to become effective. Worked example gives an 18 frame minimum wind up. Each wind up should look different from the others and hint at the follow up. LOW CONFIDENCE on attribution: from a search summary, likely [this combat design blog](https://silvasessood.blogspot.com/2021/11/combat-design-in-video-games.html)
- Cheap telegraphs: some developers use particles, squash and stretch or flashing indicators as a "shortcut for avoiding animating a ton of 'anticipation' frames for every attack." Forum post, single developer opinion, surfaced in search and not opened. Search result host: [itch.io post](https://itch.io/post/11673609)
- Dead Cells characters are about 50 px tall. The 3D model is basic because the sprite is small. A custom tool renders the mesh small with no antialiasing, exports PNG plus normal map per frame, and a toon shader adds volume. [Game Developer](https://www.gamedeveloper.com/production/art-design-deep-dive-using-a-3d-pipeline-for-2d-animation-in-i-dead-cells-i-)
- Dead Cells accepted lower detail and uncleaned flickering pixels in exchange for fast iteration on animation. Reusing rigs and old models for new monsters saved "hundred of hours". Vasseur was the only artist for the first year. [Game Developer](https://www.gamedeveloper.com/production/art-design-deep-dive-using-a-3d-pipeline-for-2d-animation-in-i-dead-cells-i-)
- Unicorn Overlord (Vanillaware). Producer Akiyasu Yamamoto: because battles run automatically, the team put particular effort into animation that fast action games cannot show, and "intentionally made the characters larger to make the battles feel more powerful". [Inverse interview](https://www.inverse.com/gaming/unicorn-overlord-interview-vanillaware)
- Unicorn Overlord criticism: a review says battles can drag because of slow unit movement and a relaxed pace, while also saying the sprites are the best reason not to skip fights. Single reviewer. [NMIA Gaming](https://nmiagaming.com/?p=176586)
- Darkest Dungeon: a 2015 player mod removes the zoom on ordinary melee, ranged and skill hits but keeps it for crits, because the author felt crits should be the impactful ones. Player made, not Red Hook. [Steam discussion](https://steamcommunity.com/app/262060/discussions/4/618453594749180956/)
- Darkest Dungeon II: Red Hook's Galen Ryder gave "Critical! Combat Presentation Tools for Darkest Dungeon II" at GDC 2024 (content behind login). Chris Bourassa said 3D characters allow more animations, effects and presentation while combat functions the same way. [GDC Vault](https://gdcvault.com/play/1034466/Independent-Games-Summit-Critical-Combat); [PC Gamer interview](https://pcgamer.com/interview-darkest-dungeon-2s-new-3d-look-monsters-the-narrator-and-its-surprising-aspirational-spirit)
- Hollow Knight frames were each drawn by hand in Photoshop and exported as PNGs. General article, no counts. [Cyberpost](https://cyberpost.co/how-did-team-cherry-make-the-art-for-hollow-knight/)

### Inferences
- Halo's three channels map onto a sprite auto battler as: a bark or word over the head (dialogue), a distinct stance clip or held pose per state (posture), and a one shot clip on each state change (gesture). The state change is the thing to animate, not the state.
- A minimum set per fighter that makes decisions visible, built from the sources above: idle that differs by state (ready, guarding, hurt, afraid), move clips by intent (advance, back off, circle), a wind up pose per attack that is unlike the others, the strike, a recovery, paired responses (block, parry, dodge, hit, heavy hit), and a few one shot gestures (taunt, flinch at ally death, call out a target). This count is my synthesis; no source gives a number per enemy.
- At 20 fps, a 0.25 second reaction floor is 5 ticks. A wind up shorter than about 5 ticks cannot be read by a viewer who is not already watching that unit. For an auto battler where the viewer watches ten units, held telegraph poses likely need to be longer than in an action game, and Naughty Dog's choice to lengthen tells supports the direction.
- Vanillaware and the Darkest Dungeon mod point the same way: when the player is watching rather than pressing, larger characters and a camera or zoom emphasis reserved for the important hits do the reading work. Zoom on every hit was the thing a player removed.
- Dead Cells' pipeline is the cheapest proven way to get many moves per character at sprite size, but it needs a 3D rig and a custom renderer. For a team generating sprite clips directly, the transferable part is the method: nail key poses and timing with the fewest frames, add in between frames only outside the keys, let VFX carry motion.

### Gaps
- No per enemy animation counts found for Hollow Knight, Owlboy, Dead Cells or Darkest Dungeon. Owlboy returned nothing usable.
- Did not reach the Halo talk pages that cover speech and enemy lifetime directly; the readable points come from a student blog summary. The talk has 27 pages at halo.bungie.org.
- No Red Hook source on how the first game's zoom and single pose attacks were designed. The GDC 2024 talk is the lead.
- The 18 frame telegraph rule is one author's rule of thumb and its source is not confirmed.

## 5. Auto battler presentation: TFT, Dota Underlords, Mechabellum, Super Auto Pets, Backpack Battles

### Takeaway
I found almost no developer writing on how auto battlers present autonomous decisions. The usable material is Riot's League clarity rule (visual noise scales with impact) and Vanillaware's comments on Unicorn Overlord. Treat this question as mostly unanswered.

### Cited Findings
- Riot's League of Legends clarity post: the more damage or teamfight impact an ability has, the more visually strong its animation should be. Silhouettes and abilities are named as the two main clarity concerns for champions. This is League, not TFT, and comes from a search summary; the page redirected and I did not open it. [Riot, Clarity in League](https://na.leagueoflegends.com/en-us/news/dev/clarity-in-league/)
- Riot said it was looking at changes "intended to slow the pace of combat" in League; average teamfights were reported at about 14 seconds. League, not TFT. [PC Gamer](https://www.pcgamer.com/riot-wants-to-slow-the-pace-of-combat-in-league-of-legends)
- TFT patch notes tune base attack speed per unit (for example Gangplank 0.75 to 0.8 in patch 15.5) but say nothing about animation readability. [TFT patch 15.5 notes](https://teamfighttactics.leagueoflegends.com/en-au/news/game-updates/teamfight-tactics-patch-15-5-notes/)
- Mechabellum is described by its publisher's CEO as looking like a real time strategy game where you can pause to think after each turn. A forum player calls its fights real time but slow paced with good readability. Not developer design notes. [GamesBeat](https://gamesbeat.com/mechabellum-gets-a-second-chance-as-dreamhaven-relaunches-the-mech-autobattler/); [Quarter to Three forum](https://forum.quartertothree.com/t/little-indie-games-worth-knowing-about-probably/73462?page=339)
- Unicorn Overlord is the best documented automatic battle case: animation effort and larger characters were chosen because the battles are automatic. [Inverse](https://www.inverse.com/gaming/unicorn-overlord-interview-vanillaware)
- Dawn of War 4 playtesters asked for longer fights so they could watch the synced melee (headline). Relevant because it is the same watch the fight situation. [PC Gamer](https://www.pcgamer.com/games/rts/dawn-of-war-4-playtesters-want-the-combat-to-take-longer-because-of-how-much-they-enjoy-watching-it/)

### Inferences
- Riot's rule gives a budget for a ten unit fight: basic exchanges stay small and quiet, the visual weight goes to the events that change the fight. This is the same shape as Total War's rare matched specials and the Darkest Dungeon mod keeping zoom only for crits.
- Slower pace is the common thread where watching is the game (Mechabellum, Unicorn Overlord, Dawn of War 4 testers), with Unicorn Overlord also showing the cost: reviewers call it slow when movement, not action, fills the time.
- Super Auto Pets and Backpack Battles show little or no fighter animation and rely on order of events and UI. I have no source for that, so it is my own observation and not cited.

### Gaps
- No Riot TFT dev article found on slowing animations or combat clarity. The brief's "Riot's notes on slowing animations for readability" could not be confirmed for TFT. Only the League clarity post and the League pace comment turned up.
- No developer source found for Dota Underlords, Mechabellum (the mechabellum.ru interview was not reached), Super Auto Pets or Backpack Battles on combat presentation.
- Worth a follow up: Riot's TFT /dev pages searched directly, the Valve Underlords team's talks, and Furcifer's Backpack Battles devlogs.
