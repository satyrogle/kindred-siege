# Grid action and JRPG battle feel: how actions are made readable and exciting

Scope: (A) real time grid combat (Mega Man Battle Network, One Step From Eden) and (B) turn based JRPG battle systems (Pokemon, Octopath Traveler, Sea of Stars, Paper Mario, Mario and Luigi, Persona 5, Final Fantasy X, Child of Light). Written for Kindred Siege: 2.5D pixel 5v5 auto battler, each band on its own 3 by 3 grid (from Battle Network), units locked in pairs trading blows, each class has a panel shape, leader parries big hits, player watches more than commands.

Source quality key: [P] primary (developer words, official), [W] fan wiki or guide, [F] fan measurement or community research, [R] press review or preview. Fan translated interviews are marked [W/P].

Research budget note: about 20 tool calls. Rocket Science style panel games, Star Force, Chained Echoes, Pokemon move animation and 3D staging, and Persona 5 specifics got no usable sources in this pass. They are listed under Gaps, not filled from memory.

## Q1. Mega Man Battle Network: grid, chip shapes, highlighting, custom gauge, telegraphs, why movement matters

### Takeaway
Battle Network splits one 6 by 3 field into two 3 by 3 halves, gives every chip a fixed panel footprint (one panel ahead, a column, a row, a lob 3 ahead), and paces the whole fight with a custom gauge of about 8 to 10 seconds that freezes the action when opened. Telegraphs are panel based: wide attacks flash their tiles before firing, charging enemies light their row, and counter hit openings flash on the enemy body. This is the closest direct ancestor to Kindred Siege's 3 by 3 band grid and class panel shapes.

### Cited Findings
Grid and panels
- The battle screen is a six by three grid; Mega Man stands on one half, opponents on the other. [Wikipedia, Mega Man Battle Network](https://en.wikipedia.org/wiki/Mega_Man_Battle_Network) [W]
- Two 3 by 3 halves, 18 tiles total, player left, enemies right; movement is one panel per D pad press with a real time slide between discrete tiles. [Game mechanics wiki, MMBN battle system](https://saschb2b.github.io/game-mechanics/games/mega-man-battle-network/battle-system) [W]
- Panel types change the field: Cracked becomes Broken after one step off and cannot be stood on; Ice slides you to the next valid panel; Grass heals Wood actors and doubles Fire damage taken; Stolen panels (AreaGrab, Steal chips) take enemy columns and shrink the opponent's space. [Game mechanics wiki](https://saschb2b.github.io/game-mechanics/games/mega-man-battle-network/battle-system) [W]; [Rockman EXE Zone, Panels](https://therockmanexezone.com/wiki/Panels_in_Mega_Man_Battle_Network) [W]
- Battles run at 60 frames per second. [Rockman EXE Zone wiki](https://therockmanexezone.com/wiki/Mod_Tools) [W]

Chip shapes (footprints)
- WideSword hits the column one ahead; it covers the whole column if the user stands in the middle row. [Mega Man Fandom, Sword chip](https://megaman.fandom.com/wiki/Sword_(Battle_Chip)) [W]
- In BN4, WideSword range 3 panels, 80 damage; LongSword range 2 panels, 80 damage. [Mega Man Wiki, BN4 Battle Chips](https://megamanwiki.com/wiki/Battle_Chip_(Battle_Network_4)) [W]
- Cannon: 40 damage, hits one enemy (straight down the row, first target). [Almar's Guides, BN6 chips](https://almarsguides.com/retro/walkthroughs/gba/games/megamanbattlenetwork6cybeastgregar/misc/lists/battlechips/) [W]
- MiniBomb: 50 damage, thrown 3 squares ahead. [Almar's Guides, BN6 chips](https://almarsguides.com/retro/walkthroughs/gba/games/megamanbattlenetwork6cybeastgregar/misc/lists/battlechips/) [W]
- Sources disagree on exact ranges across games; no single source covers every chip in every game. [search synthesis of the above] [W]

Custom gauge (pacing)
- Fills in about 8 to 10 seconds. [Mega Man Fandom, Custom Gauge](https://megaman.fandom.com/wiki/Custom_Gauge) [W]
- About ten seconds. [StrategyWiki, MMBN NetBattles](https://strategywiki.org/wiki/Mega_Man_Battle_Network/NetBattles) [W]
- 8.5 to 9 seconds to fill depending on the game; exactly half with FastGauge. [LP Archive, MMBN Operate Star Force, Update 24](https://lparchive.org/MegaMan-Battle-Network-Operate-Star-Force/Update%2024) [F]
- At 60 fps an 8 to 10 second fill is roughly 480 to 600 frames; no source states the frame count directly. [derived from the two lines above]
- Opening Custom freezes combat and opens chip select. Hitting enemies fills the gauge faster, taking damage slows it (this per page; treat as unconfirmed, single wiki source). Chip cost per chip in later systems: Standard 1/3, Mega 2/3, Giga full bar (per this page; may describe a fan variant, unconfirmed). [Game mechanics wiki](https://saschb2b.github.io/game-mechanics/games/mega-man-battle-network/battle-system) [W, low confidence]

Telegraphs and counters
- Wide area chips flash affected tiles red before firing; a charging enemy's row highlights before the lunge; hazard panels mark danger in advance; cracked panels warn a tile will become unstandable. [Game mechanics wiki](https://saschb2b.github.io/game-mechanics/games/mega-man-battle-network/battle-system) [W]
- Counter hit: hit an enemy just as it begins its attack; timing varies by enemy. Feedback is a special sound and the enemy freezes in place (or blows up). [mmhp.net EXE tips](https://mmhp.net/EXE/Tips.html) [W]
- With the Emotion Window state active, enemy counter openings flash on screen: red in BN4, blue in BN5 and BN6. A counter hit with a non dimming chip gives Full Synchro (BN4 onward). [Mega Man Fandom, Emotion Window](https://megaman.fandom.com/wiki/Emotion_Window); [Rockman EXE Zone, Emotion Window](https://therockmanexezone.com/wiki/Emotion_Window) [W]
- Sources conflict on whether Full Synchro can also be reached by filling an emotion value without a counter in BN5 and later. [Mega Man Fandom, Synchronization](https://breezewiki.discard.no/megaman/wiki/Synchronization) vs [Rockman EXE Zone](https://therockmanexezone.com/wiki/Emotion_Window) [W, conflicting]

Developer words (Masahiro Yasuma, battle system lead)
- Yasuma was mainly in charge of the battle system. [Mega Man Fandom, Masahiro Yasuma](https://megaman.fandom.com/wiki/Masahiro_Yasuma) [W]
- On the EXE3 rules: "Coming up with the rules was a process of trial and error"; an early version had a tug of war gauge at the top of the screen in the first screenshots. [Rockman EXE Zone translated interview](https://therockmanexezone.com/?p=3047) [W/P]
- In an EXE6 interview he talks about designing around a weakness cycle (a character "should be a design that looks weak to Wind"), i.e. element weakness read from the look. [Rockman EXE Zone, EXE6 Ultimate Navigation COMP interview](https://www.therockmanexezone.com/?p=4227) [W/P]
- Critic Jeremy Parish (1UP) called the battle system the "sole reason for the series' success" (quoted secondhand). [Game mechanics wiki](https://saschb2b.github.io/game-mechanics/games/mega-man-battle-network/battle-system) [R, secondhand]

### Inferences
- KS APPLIES: Battle Network makes every chip a footprint on a known grid (cell ahead, column, row, lob 3 ahead). Kindred Siege's class panel shapes are the same idea. The readable part is the footprint flashing on the target band's 3 by 3 before the hit, so the watcher reads "column", "row", "cell" before damage lands.
- KS APPLIES: Counter hits in BN give a distinct sound, a freeze on the enemy, and (in BN4+) a coloured flash on the enemy at the opening. That is a ready template for the leader parry: flash at the opening, unique sound, freeze on success.
- The custom gauge (about 8 to 10 s) is the series' pulse: a burst of action, a freeze to pick, repeat. For a watcher heavy auto battler, a similar fixed beat (a decision or Pal pick window every N seconds) would give the exchange a rhythm.
- Movement matters in BN because footprints are fixed and the field is small: one panel step changes which attacks can reach you. In KS units are locked, so the meaning has to come from which cells a footprint covers, not from dodging.

### Gaps
- No primary source on exact enemy windup frames per virus, invincibility frames after a hit, or movement frames per panel step.
- No source confirmed panel highlight colour and duration per chip in the original games (the red tile flash claim is from one wiki page).
- Star Force (3 by 5 field seen from behind, lock on swords) got no sources this pass.
- Rocket Science style panel games got no sources this pass.

## Q2. One Step From Eden: telegraph tile highlighting

### Takeaway
One Step From Eden copies the Battle Network split grid (a 4 by 8 field in halves) and telegraphs nearly every enemy attack by colouring target tiles: yellow for most attacks, a red square with "!" for some impacts. Reviewers say the warnings are fair in principle but the dodge window can be a fraction of a second. No developer interview on the telegraph design was found.

### Cited Findings
- Battle map is a 4 by 8 grid split into two sides, real time. [Checkpoint Gaming review](https://checkpointgaming.net/reviews/2020/03/one-step-from-eden-review-punishing-paradise/) [R]
- Described as Battle Network combat plus Slay the Spire deckbuilding and area progression. [GameGrin review](https://cdn.gamegrin.com/reviews/one-step-from-eden-review) [R]
- Most enemy attacks highlight target tiles in yellow; some mark impact with a red square and "!". [Nintendo World Report review](https://www.nintendoworldreport.com/review/53290/one-step-from-eden-switch-review) [R]
- Another review describes the warning as an exclamation mark on the ground under your character. [Parallax Media review](https://parallaxmedia.one/2020/04/03/one-step-from-eden-review) [R]
- "Attacks are telegraphed, but the window to escape can be a fraction of a second." [Checkpoint Gaming](https://checkpointgaming.net/reviews/2020/03/one-step-from-eden-review-punishing-paradise/) [R]
- Thomas Moon Kang said mapping out hundreds of spells and artifacts was the hardest part; per spell Unity GameObjects did not scale, and it took many trashed prototypes. [AB Games developer highlight](https://blog.abgames.io/thomas-moon-kang-one-step-from-eden-developer-highlight) [P]

### Inferences
- KS APPLIES: Two telegraph levels (yellow area = "this footprint will be hit", red plus "!" = "impact here") is a cheap, readable grammar for a 3 by 3 band. A big hit the leader can parry could use the stronger mark.
- A fraction of a second warning works for a player who moves; KS's player watches, so the warning must last long enough to be read and to parry, which argues for longer telegraphs than OSFE.

### Gaps
- No developer source on telegraph timings, colour choices, or the reasoning behind yellow vs red. No frame data.

## Q3. Pokemon: move animation, text pacing, HP bar drain, super effective feedback, Speed order, 3D staging

### Takeaway
Only HP bar data was found. Bulbapedia (via search snippet) gives Gen IV HP drain as 1 HP per 2 frames at 60 fps for Pokemon with more HP than bar pixels; Smogon research says Gens 2 to 4 animate per HP point for 49+ HP at about 5 ms per HP. The two conflict. Before Gen VI, drain speed depended on max HP.

### Cited Findings
- Before Generation VI, HP bar depletion speed depends on max HP and differs by generation. Gen IV depletes 1 HP every 2 frames at 60 fps (first HP lost takes 1 frame) for Pokemon with more HP than bar pixels. [Bulbapedia, HP (search snippet; page returned 403 on fetch)](https://bulbapedia.bulbagarden.net/wiki/HP) [W]
- Gens 2, 3, 4 animate the bar per pixel for 48 HP or less and per HP point for 49+, at roughly 5 ms per HP; not in Gens 1, 5, 6, 7, 8. [Smogon forum research thread](https://www.smogon.com/forums/threads/hp-bar-animations-and-their-effect-on-hp-precision.3667321/) [F]
- These two conflict (5 ms vs 33 ms per HP). Unresolved.
- Decompilation community docs exist for making Emerald's HP drain faster, meaning the stock drain was considered slow by hackers. [pokeemerald wiki, Faster HP Drain](https://code.heni.lol/decomp/emerald/wiki/Faster-HP-Drain) [F]

### Inferences
- KS APPLIES: An HP bar that drains over time (not snaps) lets a watcher see how big a hit was. Drain time tied to damage size makes big hits feel big.

### Gaps
- No source fetched on: type identity in move animations, battle text box timings, "It's super effective!" timing, Speed based turn order rules, Stadium or Colosseum camera staging. These are well known in general but were not sourced in this pass and should not be cited from these notes.

## Q4. Timed hits and blocks: Sea of Stars, Paper Mario, Mario and Luigi, Super Mario RPG

### Takeaway
Timed inputs keep the player watching each animation because the press is tied to a moment inside it. Super Mario RPG set the numbers (close timing +50% physical damage, perfect doubles; timed block halves or negates). Paper Mario TTYD guard is 8 frames, superguard 3 frames before contact. Sea of Stars adds locks: an enemy readying a big skill shows lock icons, each lock is a damage type, breaking some weakens the move proportionally, breaking all cancels it.

### Cited Findings
Sea of Stars (Sabotage Studio, director Thierry Boulanger)
- All combat is entirely turn based, no ATB gauge. Each character has four skills with different damage types. Some enemy skills show "locks" when used; each lock shows a weakness icon; hitting that weakness breaks the lock; breaking one weakens the move, breaking all staggers the enemy. [Nintendo Life, July 2022](https://www.nintendolife.com/news/2022/07/sea-of-stars-shares-in-depth-look-at-chrono-trigger-inspired-combat) [R quoting P]
- Timed hits: press at the right moment during an attack to make it stronger; the same works for defending, and timing also helps healing items. [Nintendo Life](https://www.nintendolife.com/news/2022/07/sea-of-stars-shares-in-depth-look-at-chrono-trigger-inspired-combat) [R quoting P]
- Boulanger on locks: the number of locks is relative to the potency of the ability; e.g. a fireball with four locks, remove two and damage is cut in half; remove all and the spell is cancelled. Breaking locks "isn't a pass or fail scenario", resources spent are never wasted. He said the system works because it is purely turn based. [Game Rant interview via mirror, unverified original](https://lutecddep.pages.dev/posts/sea-of-stars-director-talks-making-an-engaging-turn-based-combat-system) [P via mirror, check original]
- The goal of timed hits is to make combat more engaging and to offer skill based options instead of grinding. [same mirror](https://lutecddep.pages.dev/posts/sea-of-stars-director-talks-making-an-engaging-turn-based-combat-system) [P via mirror]
- Combo points build dual attacks and do not carry between fights; live mana boosts attacks. [Nintendo Life](https://www.nintendolife.com/news/2022/07/sea-of-stars-shares-in-depth-look-at-chrono-trigger-inspired-combat) [R]
- The Dawn of Equinox update added Mystery Locks (lock types revealed as you meet them) and co op timed hits. [Sabotage Studio press release](https://sabotagestudio.com/press-release/sea-of-stars-gains-a-constellation-of-new-features-in-free-dawn-of-equinox-update/) [P]

Super Mario RPG origin (numbers)
- Physical timed hit: close timing +50% damage, perfect timing doubles. Single press special: close +25%, perfect +50% (healing too). Timed block: close halves damage, perfect negates. Against a mortal blow a normal block leaves 1 HP, a perfect block avoids it. Timed hits modify damage after calculation, not stats. The 1996 Japanese booklet calls it the "Timing System". [Super Mario Wiki, Action Command](https://www.mariowiki.com/Action_Command) [W]

Paper Mario
- TTYD Superguard needs input 3 frames (3/60 s) before contact; a normal Guard has 8 frames. [Super Mario Wiki, Superguard](https://mariowiki.com/Superguard) [W]
- Some enemies bait early or late guards because the window is so small. [search snippet, Super Mario Wiki](https://mariowiki.com/Superguard) [W]
- Power Bounce (GameCube): first bounce 8 frame window, second 7, shrinking until single frame inputs. [GamesRadar on speedrunners](https://gamesradar.com/games/super-mario/its-not-just-you-speedrunners-confirm-an-essential-paper-mario-the-thousand-year-door-move-is-way-harder-in-the-rpg-remake) [F]
- The Switch remake adjusted Action Command and Superguard timing; exact remake numbers not found. [GamesRadar](https://gamesradar.com/games/super-mario/its-not-just-you-speedrunners-confirm-an-essential-paper-mario-the-thousand-year-door-move-is-way-harder-in-the-rpg-remake) [R]
- Stylish moves recharge Star Power faster; an audience increases the effect. Color Splash hammer shows a charge meter indicating when to press. Origami King "Excellent" adds 10 coins to the clear bonus. [Super Mario Wiki, Action Command](https://www.mariowiki.com/Action_Command) [W]

Mario and Luigi
- Jump and hammer commands require a press as the brother lands or the hammer shakes; successful counters avoid damage entirely. No window sizes given. [Super Mario Wiki, Action Command](https://www.mariowiki.com/Action_Command) [W]

### Inferences
- KS APPLIES (parry): TTYD's two tier defence (8 frame guard reduces, 3 frame superguard counters) is a direct model for a parry with a wide safe window and a tight reward window. Kindred Siege's locked 0.2 s window is 12 frames, wider than TTYD's 8 frame guard.
- KS APPLIES (locks): Sea of Stars locks are a telegraph that is also a puzzle: icon count shows threat size, partial success scales the result. For KS, a big enemy hit could show its strength as pips the band can chip away before the leader's parry, so the watcher sees the threat coming and sees it shrink.
- Partial credit (Boulanger) keeps failure from feeling binary. SMRPG's close/perfect tiers do the same. A KS parry that grades (near miss reduces, perfect counters) fits this.
- Enemies baiting guard timing (TTYD) is how a single input stays interesting: vary the windup rhythm, not the button.

### Gaps
- No frame data for Sea of Stars timed hit windows or for Mario and Luigi counters.
- No source on how many turns a Sea of Stars lock stays before the cast resolves.

## Q5. Turn order displays and break systems: Octopath, FFX, Child of Light, Persona 5, Chained Echoes

### Takeaway
All three studied games put the future on screen so players plan: FFX's CTB list on the right updates live when you hover an action; Octopath shows turn order on top and a shield count on each enemy that says how many weakness hits until Break; Child of Light's timeline has a long Wait zone and a short Cast zone where a hit interrupts and knocks the unit back. Octopath's devs said shield points were shown so players could "set off their own combos".

### Cited Findings
Octopath Traveler
- Producer Masashi Takahashi (Square Enix) and director Keisuke Miyauchi (Acquire) said a plain turn based system would not stand out, so "after trial and error the Command Boost and Break systems were added"; guard points are displayed "showing how many attacks under a Break occurs", "in order to add to the fun of the game", letting players "set off their own combos". [Siliconera, Famitsu interview coverage](https://siliconera.com/?p=683405) [P paraphrased by press]
- Shield points count hits needed to Break; at zero the enemy is stunned, takes more damage and loses its next turn. [Switchaboo](https://www.switchaboo.com/octopath-traveler-basics-boost-points) [W]; [iMore](https://www.imore.com/how-master-octopath-traveler-battle-system) [W]
- Each character gains 1 BP per turn; boosting skips the next turn's gain. [Twinfinite](https://twinfinite.net/2018/07/5-things-i-wish-i-knew-before-playing-octopath-traveler/2/) [W]
- BP cap: 5 stored per Destructoid; 3 can be spent on one action per Gamer Guides. These describe stored vs spent and may both be right. [Destructoid](https://vip-develop.destructoid.com/?p=364364) [R]; [Gamer Guides](https://gamerguides.com/octopath-traveler/guide/walkthrough/chapter-1/subterranean-study) [W]
- Turn order shows at the top of the screen, loosely based on Speed; Defend moves a character to the front for the next turn. [Gamer Guides](https://www.gamerguides.com/octopath-traveler/guide/the-basics/game-mechanics/battle-system) [W]; [iMore](https://www.imore.com/how-master-octopath-traveler-battle-system) [W]

Final Fantasy X (CTB)
- Battle director Toshiro Tsuchida replaced ATB with Conditional Turn Based battle to make fights more strategic, with Final Fantasy IV in mind. [Wikipedia, Toshiro Tsuchida](https://en.wikipedia.org/wiki/Toshiro_Tsuchida) [W]
- The battle pauses during each player turn, so action choice has no time pressure; not round based; higher Agility gives more turns. [Wikipedia, Final Fantasy X](https://en.wikipedia.org/wiki/Final_Fantasy_X) [W]
- Turn order shows on the right; highlighting an action in the menu shows how it changes the order before you commit. [Giant Bomb, Conditional Turn Based Battle](https://giantbomb.com/wiki/Concepts/Conditional_Turn_Based_Battle) [W]; [Jegged CTB Window](https://jegged.com/Games/Final-Fantasy-X/Tips-and-Tricks/CTB-Window.html) [W]

Child of Light
- Timeline has a long Wait section and a short Cast section; cast time ranges from none to several seconds depending on the action. Hitting an enemy in the red Cast zone interrupts its action and sends it back on the timeline; the same applies to the player. Stronger moves have longer cast times and so more interrupt risk. Igniculus can blind an enemy to slow it on the timeline, limited by his energy. [Kotaku](https://kotaku.com/1570005649) [R]; [Expert Reviews](https://www.expertreviews.co.uk/technology/gaming/child-of-light-review) [R]
- A preview called it a hybrid of 2D Final Fantasy ATB and Grandia's battle system. Director Patrick Plourde, writer Jeffrey Yohalem (Ubisoft Montreal). [God is a Geek preview](https://godisageek.com/2013/09/child-light-preview/) [R]

Persona 5
- Katsura Hashino (2023, Denfamicogamer via Gameranx): turn based battles halt the tempo, so the team worked to keep battles from breaking immersion. [Gameranx](https://gameranx.com/updates/id/464731/article/persona-director-katsura-hashino-shares-his-thoughts-on-turn-based-gameplay/) [P via press]
- Hashino on Metaphor: the balance of fast and slow tempo was refined in their earlier games' combat. [GamesRadar](https://gamesradar.com/games/jrpg/metaphor-refantazio-combines-real-time-and-turn-based-battles-because-its-director-wanted-to-integrate-the-idea-of-manga-and-movie-style-combat) [P via press]
- Baton Pass lets the member who landed a critical hand their bonus turn to an ally. [Everyeye preview](https://www.everyeye.it/articoli/provato-persona-5-32435.html) [R]
- Persona 5 Director interview on combat systems and turn based defence. [TechRadar](https://www.techradar.com/news/persona-5-director-talks-combat-systems-defends-turn-based-games) [P via press, not fetched]

### Inferences
- KS APPLIES: Kindred Siege's player watches. A visible "who strikes next" strip (FFX, Octopath, Child of Light) lets a watcher anticipate, which is the main source of tension when you are not choosing every action. It also gives the parry a countdown.
- KS APPLIES: Octopath's shield number on an enemy is a "hits left until something big" counter. In KS a pair trading blows could show a small count toward a stagger or break, so each traded blow visibly moves something.
- Child of Light's Cast zone is a telegraph with a risk built in: big moves wait longer and can be interrupted. For KS, a big hit that the leader can parry could sit in a visible cast zone; the bigger the hit, the longer the zone, the more chance to see it and answer.
- FFX's preview of order changes on hover is the strongest planning tool found. It suits KS's order or directive inputs if they change timing.

### Gaps
- Chained Echoes Overdrive bar got no source in this pass.
- Persona 5 One More and All Out Attack rules and pacing got no primary source. Known in general but not cited here.
- No numbers found on average action length in seconds for any of these games.

## Q6. Concrete timings: action length, damage numbers, HP drain, exchange pacing

### Takeaway
Hard numbers found: Battle Network custom gauge 8 to 10 s (fan measure 8.5 to 9 s), 60 fps battle logic; TTYD guard 8 frames, superguard 3 frames; Power Bounce windows 8, 7, then shrinking; Pokemon Gen IV HP drain 1 HP per 2 frames (conflicting fan figure 5 ms per HP in Gens 2 to 4); SMRPG timed hit +50%/x2, block x0.5/x0. No sourced numbers for average JRPG action length or damage number display time.

### Cited Findings
- Battle Network custom gauge 8 to 10 s. [Mega Man Fandom](https://megaman.fandom.com/wiki/Custom_Gauge) [W]; 8.5 to 9 s, half with FastGauge. [LP Archive](https://lparchive.org/MegaMan-Battle-Network-Operate-Star-Force/Update%2024) [F]
- Battle Network 60 fps. [Rockman EXE Zone](https://therockmanexezone.com/wiki/Mod_Tools) [W]
- TTYD guard 8 frames, superguard 3 frames. [Super Mario Wiki](https://mariowiki.com/Superguard) [W]
- TTYD Power Bounce 8 then 7 frame windows, shrinking. [GamesRadar](https://gamesradar.com/games/super-mario/its-not-just-you-speedrunners-confirm-an-essential-paper-mario-the-thousand-year-door-move-is-way-harder-in-the-rpg-remake) [F]
- Pokemon Gen IV HP drain 1 HP per 2 frames. [Bulbapedia snippet](https://bulbapedia.bulbagarden.net/wiki/HP) [W]; about 5 ms per HP in Gens 2 to 4. [Smogon](https://www.smogon.com/forums/threads/hp-bar-animations-and-their-effect-on-hp-precision.3667321/) [F]; conflicting.
- SMRPG timed hit and block multipliers. [Super Mario Wiki](https://www.mariowiki.com/Action_Command) [W]
- Sea of Stars lock damage scales with locks broken (2 of 4 = half damage, all = cancel). [Game Rant via mirror](https://lutecddep.pages.dev/posts/sea-of-stars-director-talks-making-an-engaging-turn-based-combat-system) [P via mirror]
- Child of Light cast time from none to several seconds. [Kotaku](https://kotaku.com/1570005649) [R]
- One Step From Eden dodge window "a fraction of a second". [Checkpoint Gaming](https://checkpointgaming.net/reviews/2020/03/one-step-from-eden-review-punishing-paradise/) [R]

### Inferences
- KS APPLIES: KS's 0.2 s parry window (12 frames at 60 fps) sits between TTYD guard (8 frames) and wider "close" bands. A two tier parry (wide reduce, tight counter) matches the most praised timed defence systems.
- A Battle Network style beat of about 8 to 10 s between decision windows is a candidate rhythm for KS rounds.

### Gaps
- No frame accurate source for: Battle Network chip animation length, enemy windups, Pokemon text box speed, damage number on screen time in any listed game, average seconds per turn in Octopath, FFX or Persona 5. Measuring these from capture footage would close the gap.
- Spriters Resource frame counts for MMBN chip animations were not checked.
