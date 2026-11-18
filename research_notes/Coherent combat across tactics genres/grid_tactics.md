# Grid tactics RPGs: how combat is made coherent, readable and exciting

Scope: Fire Emblem (GBA, Three Houses, Engage), Final Fantasy Tactics, Tactics Ogre, Triangle Strategy, Disgaea, Advance Wars, Into the Breach, Mario + Rabbids (Kingdom Battle, Sparks of Hope), Wargroove. Mechanisms and presentation only. Notes flagged **[AUTO]** apply especially to Kindred Siege, where the fight plays itself (5v5, each band on its own 3x3 grid, units locked in pairs that trade blows, class panel shapes, a leader who parries big hits, the player watches more than commands).

Source quality key: (dev) = developer statement, (official) = publisher page, (fan) = wiki, guide or forum, (review) = press review or preview.

## Turn order and initiative: phase based vs speed or charge time, and how order is shown

### Takeaway
Two families. Phase games (Fire Emblem, Advance Wars, Wargroove, Into the Breach, Mario + Rabbids) move a whole side, then the other, and need no turn list. Speed games (FFT, Tactics Ogre, Triangle Strategy) interleave units by a hidden clock, and the recent trend is to put the whole future order on screen (Triangle Strategy's AT bar, the 2025 FFT remaster's turn list with a preview of where an action will land you). Into the Breach adds a third idea that matters most for a watched fight: enemies act in a fixed, numbered order that the player can see before it happens.

### Cited Findings
- FFT Charge Time: every unit starts at 0 CT; each clock tick adds the unit's Speed to its CT; a unit acts at 100 (fan sources disagree slightly on "reaches 100" vs "exceeds 100") (fan) — [Game8](https://game8.co/games/Final-Fantasy-Tactics/archives/543470); [FFHacktics forum](https://ffhacktics.com/smf/index.php?msg=146984)
- FFT: charged actions (spells, Jump) resolve before units act on the same tick; ties are broken by roster order; Haste, Slow and speed buffs change how fast CT fills (fan, code level) — [FFHacktics forum](https://ffhacktics.com/smf/index.php?msg=146965)
- FFT The Ivalice Chronicles (released 30 Sep 2025) added a turn order list on the left of the screen showing all upcoming turns including pending spells and skills, and highlighting an action previews where it would put the unit in the order (review); Square Enix lists "turn order display, tactical view, speed controls" among the new features (official) — [RPG Site](https://www.rpgsite.net/news/17622-final-fantasy-tactics-the-ivalice-chronicles-release-date-platforms); [Nova Crystallis](https://novacrystallis.com/2025/08/final-fantasy-tactics-the-ivalice-chronicles-details-its-new-and-enhanced-features/); [RPG Site review](https://www.rpgsite.net/review/18508-final-fantasy-tactics-the-ivalice-chronicles-review)
- Tactics Ogre: turn order follows Wait Turn (WT) / Recovery Time, which changes with the kind and number of actions taken and with equipment weight (fan/press) — [Wikipedia](https://www.wikipedia.org/wiki/Tactics_Ogre_Reborn); [Square Enix news](https://square-enix-games.com/news/tactics-ogre-reborn-new-features)
- Triangle Strategy: order by Speed; upcoming order shown on the "AT Bar" at the bottom of the screen; pressing Y on an empty panel shows each unit's turn number on the field; a post demo update added enemy turn order on hover and on the AT bar (official) — [Nintendo (demo survey changes)](https://www.nintendo.com/en-ca/whatsnew/triangle-strategy-debut-demo-survey-results-nintendo-switch/); [Nintendo tips](https://www.nintendo.com/en-ca/whatsnew/tips-for-getting-started-with-triangle-strategy/)
- Triangle Strategy TP: reviews say each unit starts with 3 TP and gains 1 per turn; a demo preview said 1 TP, likely an earlier build (review, conflicting) — [Turn Based Lovers](https://turnbasedlovers.com/review/triangle-strategy-tbl); [RPGFan demo impressions](https://www.rpgfan.com/2022/02/19/triangle-strategy-prologue-demo-impressions/)
- Advance Wars: two to four armies take turns on grid maps; turn based by army (press) — [Wikipedia](https://en.wikipedia.org/wiki/Advance_Wars)
- Into the Breach: Vek move and attack after each player turn, then new Vek emerge (press) — [Wikipedia](https://en.wikipedia.org/wiki/Into_the_Breach). Enemy attacks resolve in a numbered order the player can display (Alt/Tab on PC per fan guides; key bindings unconfirmed officially) (fan) — [Gamepressure interface guide](https://www.gamepressure.com/into-the-breach/interface-battle-map/zcaa73); [Game Informer tips](https://www.gameinformer.com/b/features/archive/2018/02/27/23-tips-to-help-you-dominate-into-the-breach.aspx?amp)
- Disgaea: combo attacks happen when several characters are scheduled to hit the same target consecutively; actions are queued then executed together; in Disgaea 5 each later hit in a combo is stronger, up to x3 at Combo Count 20 (fan) — [GamerGuides Disgaea 5 battle basics](https://gamerguides.com/disgaea-5-alliance-of-vengeance/guide/basics/gameplay/battle-basics); [Steam discussion](https://steamcommunity.com/app/803600/discussions/0/1732090362062536554)

### Inferences
- **[AUTO]** A watched fight has the same problem FFT's remaster solved: hidden timing makes events feel random. A visible queue of "who strikes next" (like FFT's list or Into the Breach's numbered order) turns a stream of blows into a sequence the viewer can anticipate. For locked pairs, a per pair tick bar or a single shared "next 3 blows" strip would do this.
- **[AUTO]** Disgaea's "queue, then execute" and combo count is the nearest genre model to a self playing exchange: the plan is shown, then it plays out with escalating payoff per consecutive hit.
- Phase based games get readability free (only one side moves at a time). A simultaneous 5v5 auto fight loses that, so the presentation must recreate it, for example by staggering blows so only one pair resolves at a time (consistent with the project's "one blow at a time" direction).

### Gaps
- No primary dev statement found on why FFT, Tactics Ogre or Triangle Strategy chose speed timelines over phases.
- Exact Tactics Ogre WT formula and Triangle Strategy's speed formula not sourced.

## Targeting, range, zones of control, weapon triangle, counters, threat, terrain, facing

### Takeaway
The genre makes every blow readable before it lands: a forecast or preview states the exact numbers (Fire Emblem's combat forecast), attacks are framed by rock paper scissors matchups (weapon triangle, Wargroove's damage matrix), position changes outcomes in fixed, learnable ways (FFT facing, Mario + Rabbids cover tiers, Wargroove's conditional crits), and counters make every exchange a two way trade. The modern direction is less randomness and more visible certainty.

### Cited Findings
- Fire Emblem combat forecast: a window shown before you commit, listing Might, projected damage, hit rate, crit rate and both combatants' remaining HP (fan) — [Fire Emblem Wiki: Combat forecast](https://www.fireemblemwiki.org/wiki/Combat_forecast); [SuperCheats Three Houses](https://www.supercheats.com/fire-emblem-three-houses/walkthrough/what-is-combat-forecast)
- FE GBA (Sacred Stones) formulas: Attack Speed = Speed minus max(0, weapon weight minus Constitution); a follow up (double) attack happens when your Attack Speed exceeds the enemy's by 4 or more; triangle advantage gives +15 hit and +1 Might (disadvantage the reverse); Reaver weapons double the triangle; a critical multiplies damage by 3 (fan, data mined) — [Serenes Forest calculations](https://serenesforest.net/the-sacred-stones/miscellaneous/calculations/)
- Weapon triangle: Sword > Axe > Lance > Sword; Magic: Anima > Light > Dark > Anima (fan) — [Serenes Forest](https://serenesforest.net/the-sacred-stones/miscellaneous/calculations/)
- FE Engage "Break": attacking with triangle advantage breaks the target, which then cannot counterattack (until after its next turn, per most sources; one source says only the current exchange) (press/fan, conflicting on duration). RPGFan: in past games the triangle only gave accuracy and damage bonuses, in Engage "it fuels the new break system" — [TechRaptor](https://techraptor.net/gaming/news/new-fire-emblem-engage-break-mechanic-stops-counter-attacks); [Game8](https://game8.co/games/Fire-Emblem-Engage/archives/401299); [RPGFan review](https://www.rpgfan.com/review/fire-emblem-engage/)
- Advance Wars: a defending unit counterattacks direct attackers; indirect (ranged) units must stay still to attack; damage depends on HP, weapon, unit types and terrain; a unit at 10 HP has full strength, at 1 HP about 10% (press/fan) — [Wikipedia](https://en.wikipedia.org/wiki/Advance_Wars); [Advance Wars Wiki: Damage formula](https://advancewars.fandom.com/wiki/Damage_Formula)
- Wargroove critical hits are never random; each unit has its own crit condition. Finn "Tiy" Brice (Chucklefish CEO, lead designer): "Every single unit in the game can inflict a critical hit, but it's never random. Instead, that unit has to fulfill their critical hit condition." Examples he gives: Spearmen crit next to other Spearmen; Knights crit after running a certain distance; "There are many of these to learn and it makes precise unit placement really rewarding." He compares chained crits to "watching all of the Puyos burst in a chain reaction in Puyo Puyo" (dev) — [Siliconera interview](https://www.siliconera.com/wargroove-creator-talks-about-commanders-critical-hits-and-pitch-sessions/)
- Further Wargroove crit conditions (fan): Turtles crit in deep sea, Trebuchets at max range, Dragons when the target is on a road, Golems at or below 40% HP; Commanders use "Grooves" instead of crits. Note Brice says Archers crit if they attack without moving, while the wiki lists a different condition (conflicting) — [HyperX blog](https://blog.hyperx.com/article/7051/unit-strengths-weaknesses-and-critical-hits-in-wargroove); [Wargroove Wiki units](https://wargroovewiki.com/Unit)
- FFT facing: attacks from the side or back strip evasion. Code level wiki: from the side Class Evade is nullified; from the back Class Evade plus both hands' weapon/shield evade are nullified. A fan walkthrough instead gives flat +20% back and +10% side bonuses (fan, conflicting) — [FFHacktics wiki: Facing Evade Calculation](https://ffhacktics.com/wiki/Facing_Evade_Calculation)
- Mario + Rabbids Kingdom Battle cover: no cover 100% to hit, half cover 50%, full cover 0%; a missed shot damages the cover; flanking gives 100% (review/fan). Goomba Stomp: hit and evade "aren't tied to skills. They're based purely on location on the battlefield." — [Gameranx walkthrough](https://gameranx.com/features/id/118324/article/mario-rabbids-kingdom-battle-world-1-walkthrough-ancient-gardens/); [Goomba Stomp accessibility](https://goombastomp.com/kingdom-battle-accessibility/); [GameSpot tips](https://Gamespot.com/gallery/mario-rabbids-kingdom-battle-guides-9-tips-to-get-/2900-1424/)
- Disgaea team attacks: a normal attack can pull in adjacent allies for extra hits (chance based); up to 4 participants in Disgaea 2 and 5 (T shape in D2), up to 10 in Disgaea 6 (fan/press) — [GamerGuides](https://gamerguides.com/disgaea-5-alliance-of-vengeance/guide/basics/gameplay/battle-basics); [GoNintendo on Disgaea 6](https://gonintendo.com/stories/374672-disgaea-6-defiance-of-destiny-details-disgaea-1-characters-item)

### Inferences
- **[AUTO]** Wargroove's rule is the strongest borrow for Kindred Siege: crits that fire from a visible condition (a class's panel shape filled, an ally in a set panel, the partner's HP threshold) make big hits feel earned and explainable even when the player did not order them. A watcher can learn "the Spearman crits when flanked by a Spearman" by seeing it happen twice.
- **[AUTO]** Engage's Break is a direct fit for locked pairs that trade blows: a matchup win can visibly cancel the opponent's return blow. One clear consequence ("no counter") reads better than a hidden +15 hit.
- **[AUTO]** FE's forecast is a decision tool, but its numbers can double as a pre fight "matchup card" for each pair in an auto battler, so the viewer knows who is favoured before blows start.
- Mario + Rabbids' 0/50/100 tiers show how to collapse probability into three readable states; a 3x3 panel can carry similar fixed modifiers by row (front, middle, back).

### Gaps
- Fire Emblem threat range display (all enemy reach tiles in red) and blue move tiles: well known in the series, but no source fetched here; treat as unverified in these notes.
- No dev quote found on why Intelligent Systems uses the weapon triangle.
- FE "displayed hit vs true hit" (two random numbers averaged in GBA era) not confirmed by a fetched source.

## How each class shows its identity through its attack; durations, frames, skip options

### Takeaway
Fire Emblem's identity comes from per class (and in Engage per character, per level) battle animations with a fixed set of modes: attack, critical, ranged, dodge, miss, standing. Crits are a separate, showier animation, and that contrast is what makes a crit feel like an event. GBA animations run on 16 colours, with movement frames held 2 to 7 game frames and wind ups held 10 to 30 frames at 60 fps. Engage's developers pushed for "flashy" per unit actions and even change a character's motions as they level.

### Cited Findings
- GBA FE battle animation modes: 1 melee attack, 3 melee crit, 5 ranged attack, 6 ranged crit, 7/8 dodge, 9 standing (melee), 10 standing, 11 standing (ranged), 12 miss; modes 2 and 4 automatic (fan, community tooling) — [How to make GBA Fire Emblem battle animations](https://fe-battle-animations.neocities.org)
- GBA frame canvas 248x160 px; standing sprite feet about 102 px from top; standard body height 32 px (women) and 34 px (men); max 16 colours per animation including transparent and outline (#282828), so 14 usable (fan) — [same](https://fe-battle-animations.neocities.org)
- GBA frame timing: durations in 60 fps game frames; movement frames typically 2 to 7 frames; pauses such as raising a weapon typically 10 to 30 frames; bulkier units hold frames slightly longer; Eirika's attack uses 19 images, 47 in her full set; the template's impact comes about 10 frames (about 1/6 s) after the swing starts (derived from an example, not a rule) (fan) — [same](https://fe-battle-animations.neocities.org)
- Crits: same script structure but a different hit command and their own frames; animators add spins, twirls or jumps; mages and archers reuse one animation for normal and crit (fan) — [same](https://fe-battle-animations.neocities.org)
- FE Shadow Dragon (DS): "most of the vanilla animations seem to have every frame of animation have a duration of 3 frames" (fan) — [FEUniverse FE11 documentation](https://feuniverse.us/t/fire-emblem-shadow-dragon-fe11-documentation/27666/5)
- Engage Ask the Developer (dev): Nakanishi says in the previous title (Three Houses), battalion movement limits meant they "didn't have much choice but to tone down the actions"; in Engage battle scenes were planned per individual unit and they asked Intelligent Systems to make actions "as flashy as possible", e.g. a dash effect when soldiers attack and arrows shot from a fort. Higuchi credits art director Takafumi Teraoka, honouring past titles "characterized by their flashy battle animation despite the pixel art." Nakanishi: "the battling motions will change as the characters level up"; early a character "will just simply dodge", later they "will counterattack while parrying, or slash at flying arrows with a sword." Monks are "good at hand-to-hand combat" so healers can fight back. Tei: "All scenes are expressed with 3D models and camerawork" — [Ask the Developer Vol. 8, Part 2](https://www.nintendo.com/us/whatsnew/ask-the-developer-vol-8-fire-emblem-engage-part-2/)
- Engage Part 1 (dev): Tei on Emblem attacks: "The effects used in cutscenes showing their attacks are colorful and flashy" and "we felt the need to communicate to players that it was so powerful that it could crush enemies." Higuchi: in tactics "the fun comes a little later", so they wanted "the unleashing of overwhelming power and special attacks" as early as possible — [Ask the Developer Vol. 8, Part 1](https://www.nintendo.com/us/whatsnew/ask-the-developer-vol-8-fire-emblem-engage-part-1/)
- Engage's animations are described by players as lengthy, making battles much longer with them on; a secondhand Reddit claim says a May 2023 interview stated the aim was for people not to want to turn animations off (fan, unverified, untranslated source) — [Reddit thread](https://lr.eu.psf.lt/r/fireemblem/comments/1bkbpxo/combat_animations_in_fe_engage_are_the_spiritual/kvzfoec/?context=3)
- Advance Wars battle animations are short; a review estimates "couldn't be more than 20 frames per battle"; Days of Ruin later used a 2 frame gun blaze (review/fan) — search summary of [Nintendo World Report review](https://nintendoworldreport.com/review/3803/advance-wars-game-boy-advance) (snippet only, not fetched in full)
- Mario + Rabbids weapons were designed around a visible effect first: producer Xavier Manzanares: "with weapons we first had the 'push' effect, and then we thought 'hey we can do bounce'"; "Super" weapon effects "were created with 'craziness' in mind" (dev) — [Nintendo Life](https://www.nintendolife.com/news/2017/08/feature_mario_plus_rabbids_kingdom_battles_producer_on_the_wtf_blend_of_iconic_franchises)

### Inferences
- **[AUTO]** The FE mode list is a ready made minimum clip set per class for a watched fight: idle, attack, crit (distinct and bigger), dodge, miss, hit react. The crit being its own animation, not a faster normal, is the key to "this blow mattered."
- **[AUTO]** Engage's "motions change as they level" (dodge early, parry and counter later) maps directly onto a leader who parries big hits: the parry can be the visible proof of rank.
- Wind up holds of 10 to 30 frames (1/6 to 1/2 s) against 2 to 7 frame motion frames give a concrete timing pattern: long anticipation, snap action, short recovery. Useful as a starting rule for pixel attack clips.
- Three Houses toned down its battle actions because of battalions; Engage reversed this. Evidence that per unit action clarity was judged worth the cost by the developer.

### Gaps
- No primary source on total seconds per FE exchange (on vs off). TASVideos has with/without animation encodes of GBA FE but timings were not extracted.
- No dev statement found on the production cost of FE battle animations (asked in brief; nothing found in Engage, Awakening or Three Houses interviews checked).
- No frame data found for FFT, Tactics Ogre, Triangle Strategy, Disgaea or Wargroove attack animations.

## Battle scenes and cut ins: when and what is shown, how attacker and defender alternate, doubling

### Takeaway
Fire Emblem and Advance Wars cut from the map to a side view duel for each attack. Fire Emblem's duel shows both units' HP, hit, damage and crit, then plays attacker strike, defender counter, then a follow up if one side is 4+ attack speed faster. The structure is fixed and learnable, so the viewer reads every exchange the same way. Advance Wars keeps the cut short. Engage adds camera close ups for character expression. Every game in the set lets players switch the cut off or speed it up, which shows the genre treats the duel as a reward, not a requirement.

### Cited Findings
- FE exchange rules: follow up attack when Attack Speed lead is 4+ (GBA) (fan) — [Serenes Forest](https://serenesforest.net/the-sacred-stones/miscellaneous/calculations/); forecast lists damage, hit, crit and remaining HP for both sides (fan) — [Fire Emblem Wiki](https://www.fireemblemwiki.org/wiki/Combat_forecast)
- Hit and crit roll separately; forecast numbers are chances, not guarantees (fan) — [Fire Emblem Wiki](https://www.fireemblemwiki.org/wiki/Combat_forecast)
- Engage: "All scenes are expressed with 3D models and camerawork", letting the camera zoom in on serious expressions; same face models used in in battle conversation windows (dev) — [Ask the Developer Part 2](https://www.nintendo.com/us/whatsnew/ask-the-developer-vol-8-fire-emblem-engage-part-2/)
- Advance Wars: CO Power meter fills by defeating enemies or taking heavy damage; when full the CO's power buffs friends or debuffs foes (e.g. Olaf's snow cuts enemy movement). Director Makoto Shimojo described adding "waves of excitement" to pace the game, like calm stretches in films (dev quoted in press) — [Wikipedia](https://en.wikipedia.org/wiki/Advance_Wars)
- FFT remaster lists "speed controls" as a headline feature (official) — [Nova Crystallis](https://novacrystallis.com/2025/08/final-fantasy-tactics-the-ivalice-chronicles-details-its-new-and-enhanced-features/)
- Into the Breach kept fights short on purpose: unlike XCOM, where a battle can take up to an hour, a turn limit and telegraphing keep battles brief (press, attributed to devs) — [Wikipedia](https://en.wikipedia.org/wiki/Into_the_Breach)

### Inferences
- **[AUTO]** The FE duel order (strike, counter, follow up) is exactly the shape of Kindred Siege's locked pairs trading blows. Borrow the fixed order and a fixed rule for the extra hit (FE's 4 speed gap) so viewers can predict and then confirm it.
- **[AUTO]** Advance Wars' power meter that fills from kills and from heavy damage taken is a good model for a leader's parry meter or a big moment trigger: it guarantees a peak arrives even in a losing fight ("waves of excitement").
- **[AUTO]** Since the player watches, a duel cut in for every blow would be too long (Engage players already complain). Cut in only for crits, kills and leader parries; play normal trades on the board. This matches FE's split between map and battle view, but chosen per blow rather than globally.

### Gaps
- Exact seconds of an FE GBA, Three Houses or Engage exchange not found in a citable source.
- Whether Advance Wars battle scenes show HP as the number of soldier figures: commonly claimed, not confirmed by any fetched source.
- No dev commentary found on the value vs cost of battle animations in Fire Emblem.

## Into the Breach: telegraphs, determinism, push, exact tiles

### Takeaway
Into the Breach is built on full information: every enemy shows its next attack tiles, movement range and HP during your turn, attacks resolve in a visible order, and your own weapons have almost no randomness. Subset cut weapons that could not be made readable and replaced text with tiny looping animations of each weapon's effect, which Justin Ma calls the most important decision they made. Push (moving units off their attack line or into each other) carries much of the play.

### Cited Findings
- 8x8 board, three mechs per battle, one weapon fired per mech per turn, fixed turn limit per map; Vek show intended attack direction, movement range and remaining HP on screen; they move and attack after each player turn, then new Vek emerge (press) — [Wikipedia](https://en.wikipedia.org/wiki/Into_the_Breach)
- Subset: "When every enemy attack is telegraphed and there's no random chance in your attack options, the game starts to feel like a puzzle"; "We wanted to make something where every death felt like your own fault"; "it's very clear how or why you failed so that you can feel like you can improve"; "We prefer games with clear rulesets, and were interested in pursuing something with less randomness than FTL" (dev) — [Game Developer: Road to the IGF](https://gamedeveloper.com/game-platforms/road-to-the-igf-subset-games-i-into-the-breach-i-)
- Subset on push: "The first artillery attack can damage enemies, but its side effect of pushing adjacent tiles is usually more impactful"; "Considering the many ways weapons can interact to create original solutions is at the heart of the game"; difficulty is knife edge: "A single additional enemy turns a battle from 'a fun challenge' to 'completely impossible'" (dev) — [same](https://gamedeveloper.com/game-platforms/road-to-the-igf-subset-games-i-into-the-breach-i-)
- Justin Ma: "As a game design principle, we would sacrifice cool ideas for the sake of clarity every time"; "Our requirement that the player has to understand what's going on in any situation restricted our game design options"; early on "You could just see all these damage indicators all over the map" which was fine for the devs, "but it was very clear, as soon as we handed it to someone else, that we needed to make it very clear who was doing what"; on weapon tooltips: playtesters read three sentences and said "What"; the animated tooltip: "I still think it's the most important decision we made about this. You could type out a hundred times, 'Damages a tile and pushes adjacent tiles,' but showing that little animation of them moving is a thousand times more effective." Weapons and firing patterns were cut because playtesters could not understand them (dev, via RPS) — [Game Developer](https://gamedeveloper.com/design/-i-into-the-breach-i-dev-on-ui-design-sacrifice-cool-ideas-for-the-sake-of-clarity-every-time-)
- Origin: the idea grew from a single enemy that telegraphed its attacks, which suited the theme of collateral damage; Ma cited films like Man of Steel and Pacific Rim where "the whole city gets demolished, but no one cares because the good guys won" (dev, via press) — [Wikipedia](https://en.wikipedia.org/wiki/Into_the_Breach)
- GDC 2019 "Into the Breach Design Postmortem" by Matthew Davis: covers deciding what to cut, "stealing" design from other games and choosing difficulty (official listing; content paywalled) — [GDC Vault](https://www.gdcvault.com/play/1025772/); [GDC news](https://www.gdconf.com/news/get-inside-look-making-breach-gdc-2019)
- Every map fits on one screen so players see the whole board at once (review) — summarised from [Game Developer](https://gamedeveloper.com/design/-i-into-the-breach-i-dev-on-ui-design-sacrifice-cool-ideas-for-the-sake-of-clarity-every-time-) search snippet

### Inferences
- **[AUTO]** The biggest lesson for a watched fight: show the next blow's target panels a beat before it lands. A viewer who sees the telegraph and then sees it resolve feels the fight is coherent even with no input. This is the "telegraph, then payoff" loop without the decision.
- **[AUTO]** "Every death felt like your own fault" translates in an auto battler to "every loss is explained by the build": determinism plus visible causes lets the player trace a loss to a squad choice.
- **[AUTO]** Animated tooltips (a looping micro clip of the move on a tiny grid) are an excellent fit for teaching each class's panel shape: a 3x3 micro grid showing which panels the attack hits, playing on loop in the squad screen.
- Clarity beats variety: Subset cut cool weapons. For Kindred Siege, any class move whose panel pattern cannot be read at sprite size is a cut candidate.
- Small boards (8x8, 3 units) plus fixed turn limits keep fights short; a 3x3 per band with 5 units is denser, so overlapping telegraphs are a real risk (Ma's "damage indicators all over the map").

### Gaps
- GDC 2019 talk content itself (paywalled; no transcript found).
- No dev statement fetched on the exact enemy ordering rules (only fan guides confirm numbered order).
- Into the Breach does have small random elements (e.g. building resist chance per fan knowledge), not confirmed by a fetched source.

## Mario + Rabbids: team jump, movement as attack, 3D readability on a grid

### Takeaway
Kingdom Battle made movement itself an attack: the dash damages enemies you pass through, and team jump launches off an ally for extra reach, so a turn is "move through things, then shoot." Hit chance is tied purely to position (0/50/100 by cover), not to stats, which makes odds readable at a glance. Sparks of Hope dropped the grid for bounded free movement to feel more like Mario, adding Sparks to keep tactics.

### Cited Findings
- Dash was in the first Unity prototype in 2014; then "Team Jump, Triple Team Jump, Triple Dash" were added; "initially the emphasis is a lot on the actions like the dash"; "It was important to mix the accessibility – which Nintendo required – along with depth" (dev, Manzanares) — [Nintendo Life](https://www.nintendolife.com/news/2017/08/feature_mario_plus_rabbids_kingdom_battles_producer_on_the_wtf_blend_of_iconic_franchises)
- Soliani (E3 2017): later in the game Rabbid Peach can triple dash enemies, run to a teammate, team jump and land behind cover, all in the movement phase (dev) — [Nintendo World Report](https://www.nintendoworldreport.com/interview/44949/mario--rabbids-kingdom-battle-interview)
- Cover tiers 100/50/0 hit; flanking 100%; misses damage the cover; odds based purely on location, not skills (review/fan) — [Gameranx](https://gameranx.com/features/id/118324/article/mario-rabbids-kingdom-battle-world-1-walkthrough-ancient-gardens/); [Goomba Stomp](https://goombastomp.com/kingdom-battle-accessibility/)
- Sparks of Hope: Manzanares to Edge: "We still had the grid in 2018... the grid system was still there, the cursor was still there"; Soliani: free movement "adds freedom" and is core to Mario; heroes still have a bounded movement area; Sparks pushed further partly to keep the tactical side after free movement (dev via press) — [Edge via PressReader](https://www.pressreader.com/australia/edge/20220811/281646783902649); [GameSpot](https://www.gamespot.com/articles/mario-rabbids-sparks-of-hope-plays-more-like-a-mario-game/1100-6504980/); [Nintendo Life](https://www.nintendolife.com/features/mario-plus-rabbids-producer-talks-free-movement-and-fusing-lumas-rabbids-and-composers)
- Weapon effects (push, bounce) were designed as visible verbs first (dev) — [Nintendo Life](https://www.nintendolife.com/news/2017/08/feature_mario_plus_rabbids_kingdom_battles_producer_on_the_wtf_blend_of_iconic_franchises)

### Inferences
- **[AUTO]** "Movement as attack" suggests the way a unit enters its pair (dash in, leap over an ally) can itself be a class signature and a hit, giving the opening of each duel an identity beat.
- **[AUTO]** Position only odds (no hidden stat roll) are ideal when the player cannot act: the viewer can see why a blow missed (it was behind cover / in the back row).
- 3 heroes per side is the series' squad size (Sparks: "each of the three heroes"); confirms small casts keep 3D grid fights readable.

### Gaps
- No dev statement on camera choices for reading 3D grids.
- Kingdom Battle map sizes and enemy counts not sourced.

## Concrete numbers (durations, turn lengths, map sizes, unit counts)

### Takeaway
Hard numbers are scarce in primary sources. The solid ones: Into the Breach 8x8 with 3 mechs and a fixed turn limit; FE GBA frame timings (2 to 7 frames per motion, 10 to 30 frames wind up, 60 fps, 16 colours); FE doubling at 4+ attack speed and crit x3; Mario + Rabbids 3 heroes and 0/50/100 cover; Disgaea team attacks up to 4 (D2, D5) or 10 (D6); Advance Wars units at 10 HP.

### Cited Findings
- Into the Breach: 8x8, 3 mechs, fixed turn limit — [Wikipedia](https://en.wikipedia.org/wiki/Into_the_Breach)
- FE GBA: 60 fps, motion frames 2 to 7, holds 10 to 30, 16 colours, 248x160 canvas, ~32 to 34 px body — [fe-battle-animations](https://fe-battle-animations.neocities.org)
- FE GBA: double at +4 AS, triangle +15 hit/+1 Might, crit x3 — [Serenes Forest](https://serenesforest.net/the-sacred-stones/miscellaneous/calculations/)
- Advance Wars: 10 HP units, strength scales with HP, 18 unit types — [Wikipedia](https://en.wikipedia.org/wiki/Advance_Wars); [Advance Wars Wiki](https://advancewars.fandom.com/wiki/Damage_Formula)
- FFT: CT threshold 100, CT += Speed per tick — [Game8](https://game8.co/games/Final-Fantasy-Tactics/archives/543470)
- Disgaea 5 combo: up to x3 damage at Combo Count 20 — [GamerGuides](https://gamerguides.com/disgaea-5-alliance-of-vengeance/guide/basics/gameplay/battle-basics)
- Triangle Strategy: 3 TP start, +1 per turn (review, conflicting with demo) — [Turn Based Lovers](https://turnbasedlovers.com/review/triangle-strategy-tbl)

### Inferences
- **[AUTO]** A practical starting timing for a Kindred Siege blow, derived from FE GBA: wind up 10 to 30 frames (0.17 to 0.5 s), strike 2 to 7 frames per drawing, impact roughly 1/6 s after the swing begins. A full trade (strike plus counter) would then sit near 1 to 2 s, in line with the project's 0.5 s per blow hold. This is an inference, not a measured FE figure.

### Gaps
- Seconds per exchange for any game, with animations on vs off, not found.
- Map sizes and units per side for Fire Emblem, FFT, Tactics Ogre, Triangle Strategy, Wargroove not fetched.
- Turn lengths in real time for any title not found.
