# Legends of Runeterra (Riot Games): battle feel and unit play

Scope note for the report writer. Riot has published very little about the craft of LoR combat feel (strike animation, hit timing, sound). I found no GDC talk on LoR art, animation or VFX, no sound design interview for the card game, and no developer figures in milliseconds. The strongest developer sources are: the 0.9.1 and 0.9.2 patch notes, Andrew Yip's open beta blog (reproduced by Out of Games), an engineering tweet from Exis, a Reddit reply from Riot's TinyEric on level up production, the patch 3.4 UX notes, and Riot's official keyword glossary. Community wiki claims are marked [community]. Search summaries of pages I could not open are marked [search summary, page not opened]. Anything from my own memory of playing or watching the game is marked [my memory, unsourced] and sits only under Inferences. Translation ideas for Kindred Siege (2.5D pixel art, 5 v 5 auto battler, units fight on their own, a leader parries big hits) are my own inference.

## 1. Weight scaled to power: how strikes look for small and big units, impact, board reactions, sound

### Takeaway
Riot's documented lever was not "make big hits heavier", it was "make common actions faster and stop single cards from chaining long animations". Basics (draw, summon, attack drag, spells, round start) were sped up and input lock out cut, so the rare heavy moments stand out against a quick baseline. I found no developer source on power scaled impact (shake, hit stop, sound by power).

### Cited Findings
- Patch 0.9.1 (March 2020) raised animation speed for: summoning units, drawing cards, dragging units out to attack, playing Fast and Slow spells, playing Burst spells, and the initiative token flip at the start of rounds. It also removed effect buffers and shortened action lock out windows to improve responsiveness. Riot said it knew of a few card specific animations and interactions that were especially slow and would address them over time. [Out of Games, 0.9.1 patch notes](https://outof.games/news/868-legends-of-runeterra-patch-091-notes-new-cosmetics-friend-challenge-xp-reverted-animation-speed-increases/)
- Riot engineer Exis (tweet quoted in coverage): the team would speed things up over several updates starting with 0.9.1, beginning with the basics: drawing cards, playing units, passing the turn, starting a new round. No numbers were given. [Out of Games, animation speeds article](https://outof.games/news/819-animation-speeds-are-increasing-with-the-next-legends-of-runeterra-patch/)
- Design Director Andrew Yip, open beta blog: 0.9.1 began cutting both animation time and input block time for common actions (playing units, drawing, start of round). He named Draven's chained axes lasting several seconds as the kind of single card chain they wanted gone, and said internal guidelines were being updated so new content follows the same timing lessons. He framed the goal as the game being as fun to interact with as it is to look at. He also said FPS options were added in 0.9.0 and admitted Riot had misjudged how much players wanted them. Only timing reference given is "multi second"; no durations. [Out of Games, Riot discusses the state of the beta](https://outof.games/legends-of-runeterra/921-riot-discusses-the-state-of-the-legends-of-runeterra-beta-mobile-balance-card-backs-card-styles-more)
- Patch 0.9.2 (17 March 2020) continued the speed up (wiki highlight: animation speed up, continued). [community] [LoL Wiki, V0.9.2](https://wiki.leagueoflegends.com/en-us/V0.9.2_(Legends_of_Runeterra)). Coverage lists: Draven's Spinning Axe animation sped up and its VFX cleaned up; level 2 Hecarim visuals sped up; Puffcap animations sped up; discard animation made faster and clearer, with opponent discards now showing the card before it disintegrates. Riot reportedly called the old slow interactions hard on both the turn timer and the player's sanity. [search summary, page not opened] [Shacknews, 0.9.2 notes](https://shacknews.com/article/116943/legends-of-runeterra-patch-notes-092-slows-hecarims-charge); [Dot Esports](https://dotesports.com/?p=231991)
- Coverage of 0.9.1 says card animations such as Tryndamere's level up and multiple Draven axes were not changed in that patch but were being monitored. [search summary, page not opened] [Gfinity, 0.9.1 notes](https://www.gfinityesports.com/article/legends-of-runeterra-patch-notes-0-9-1-update-3rd-march); [Upcomer](https://upcomer.com/legends-of-runeterra-patch-0-9-1-speeds-up-gameplay-and-fixes-bugs)
- Sound: LoR was a 2021 Webby honoree for Best Music/Sound Design, with Jeff Seamster credited as Audio Lead. [Webby Awards 2021](https://winners.webbyawards.com/2021/games/game-features/best-musicsound-design/172466/legends-of-runeterra). Riot sound designer Sandy Zhou lists LoR among her credits, but the interview found is general, not about LoR combat sound. [search summary, page not opened] [The Magic Rain](https://themagicrain.com/?p=36999)

### Inferences
- [my memory, unsourced] On the board, units are small framed portraits. Attackers slide forward into the combat row; each one in turn lunges at its blocker or the Nexus, the target jolts, and a damage number appears. Very high cost units (dragons, big champions) have heavier summon arrivals than cheap followers. Verify by watching match footage before quoting.
- Translation: the documented lesson is a pacing budget. Keep the cost of a small unit's basic hit very low (fast, minimal hold) so a big unit's hit can spend more time and still feel rare. Kindred Siege's "one blow at a time" screen hold (0.5 s per blow, rules 40) is the opposite risk to LoR's beta: if every blow holds, the whole fight feels like the slow 0.9.0 client. Scale the hold with the blow's weight instead of holding every blow equally.
- Translation: the Draven chain lesson applies directly to auto battler procs. One unit's repeated effect (multi hit, bleed ticks, chain procs) should never serialise into seconds of solo animation. Batch repeats or overlap them.
- Translation: Riot cut "input block time" separately from "animation time". For an auto battler the equivalent is the parry window: a heavy wind up can be long and readable, but the leader's input must never be locked out by an unrelated animation still finishing.

### Gaps
- No developer numbers (ms or frames) for any LoR animation, before or after 0.9.1.
- No developer source on screen shake, hit stop or sound that scales with a unit's Power.
- No LoR sound design talk or interview found; Riot's published SFX articles found are about League of Legends.
- No GDC talk on LoR art, animation or VFX found.

## 2. Hits that travel and land on a body: the combat lane, strike order, how a strike lands, Overwhelm

### Takeaway
LoR combat is a set of attacker and blocker pairs resolved one pair at a time, left to right. The rules give each strike a clear beat structure (Quick Attack strikes alone first, Double Attack strikes twice, normal pairs trade at once), and Overwhelm sends surplus Power past the body into the Nexus. The keyword is also worn on the unit's frame and changes when the unit is ready to attack.

### Cited Findings
- Official glossary: a unit strikes when it deals damage to an enemy unit or Nexus using its Power; 0 Power units cannot strike. Quick Attack: when attacking, strikes first; if the blocker dies before striking back, the attacker takes no damage (attack only). Double Attack: strikes both before and at the same time as its blocker. Overwhelm: Power beyond the blocker's Health goes to the defending Nexus, Barrier does not stop that excess (attack only). Challenger: attacker chooses which enemy blocks it. Elusive: only blockable by Elusive. Fearsome: only blockable by units with 3 or more Power. Impact: on striking while attacking, deal 1 to the enemy Nexus, stacks. Support: when attacking, buffs the ally to its right. Lifesteal: when it strikes, its Nexus heals by its Power. Tough: takes 1 less damage from any source. Barrier: negates the next damage, lasts one round. [Riot Support, Keywords and Game Vocab](https://support.riotgames.com/legends-of-runeterra/gameplay/keywords-and-game-vocab-legends-of-runeterra)
- Official worked example (per search summary of the same page): a 3|1 Overwhelm attacker blocked by a 1|1 Barrier unit: the blocker's share is negated, the Nexus still takes the other 2. [search summary] [Riot Support glossary](https://support.riotgames.com/en-us/legends-of-runeterra/gameplay/keywords-and-game-vocab-legends-of-runeterra)
- [community] Blocking: after an attack is committed, the defender drags units into blocking position, pairing each attacker with one blocker; not every attacker must be blocked. After blocks or spells, the attacker may respond; once both players pass, striking begins. Battling units strike each other left to right. If a committed blocker is removed before resolution, the attacker does not strike unless it has Overwhelm; an unblocked attacker strikes the Nexus. [LoL Wiki, Block](https://wiki.leagueoflegends.com/en-us/LoR:Keywords/Block)
- [community] A strike and the effects it triggers (Nexus Strike, Overwhelm, Lifesteal) resolve before the resulting effects such as death and Last Breath. A Formidable unit strikes with its Health instead of its Power. [LoL Wiki, Strike](https://wiki.leagueoflegends.com/en-us/LoR:Vocabs/Strike)
- [community] Overwhelm on the body: while its owner holds the attack token, an Overwhelm unit wears a rocky protrusion on top of its frame with glowing magma cracks. When it attacks, the protrusion moves to the top corners and glows molten red. If the attack token is lost, the protrusion detaches, flips to the bottom of the card and turns into the Overwhelm icon. The icon is a shield shattered through the middle (breaking through defences). When the blocker is gone, the Overwhelm unit strikes the Nexus as if there were no blocker. The page does not describe a separate effect for the excess damage itself. Tough reduces Overwhelm damage by 1. [LoL Wiki, Overwhelm](https://wiki.leagueoflegends.com/en-us/LoR:Keywords/Overwhelm)

### Inferences
- [my memory, unsourced] Each attacker and its blocker stand in the same column, so the pair is a short vertical lane on screen. The attacker travels the short gap and hits; for an unblocked attacker the travel continues to the enemy Nexus. Check footage for exact travel and timing.
- Translation: LoR's left to right, one pair at a time order is the same idea as Kindred Siege's one blow at a time. LoR proves players accept strict serial resolution when each beat is short and the order is predictable (always left to right). An auto battler can borrow a fixed, visible order so the eye knows where to look next.
- Translation: the three strike shapes are three readable beat patterns for sprites: Quick Attack = attacker hits, pause, survivor answers (two beats); normal trade = both collide in one beat; Double Attack = hit, then trade. Each can be a distinct animation rhythm without any text.
- Translation: the defender's "respond before striking" window is the closest LoR analogue to a parry. It is a deliberate pause between commit and impact where the defending player gets the last word. For Kindred Siege, the leader's parry belongs in that same slot: after the wind up is shown, before the strike lands.
- Translation: Overwhelm as worn frame decoration that changes when armed is a strong idea for small sprites: show a unit's "breaks through" trait as a body or base mark that lights up when the unit commits to a big hit, then show the overflow continuing past the blocker to the back target.

### Gaps
- No developer source on the strike animation (lunge distance, hit frame, hit stop, recoil).
- No developer source on how Overwhelm excess damage travels to the Nexus visually.
- The official glossary does not state left to right order; that comes from the community wiki.

## 3. Readability: power and health, keyword icons, damage shown, attacker against blocker

### Takeaway
LoR's best documented readability tools are previews of outcomes before they happen: patch 3.4 added a Nexus damage preview (louder when lethal) and cracks on any unit that is about to die. Keywords are icons, and at least Overwhelm is also worn on the unit frame. Champion progress was made visible to both players.

### Cited Findings
- Patch 3.4 (30 March 2022), Gameplay UX Updates: Nexus Health Previsualization shows exactly how much damage you are dealing or your Nexus is taking, and the indicator becomes more emphatic if incoming damage is lethal. Unit Destruction Previsualization makes units display cracks when they face lethal damage during an interaction. [LoL Wiki, V3.4 patch notes](https://wiki.leagueoflegends.com/en-us/V3.4_(Legends_of_Runeterra))
- Coverage framed the 3.4 change as fixing players missing lethal or passing on blocks because they forgot an effect (The Dreadway, or a champion levelling after an attack was committed), and said Riot called these UX updates "Premonitions", with more planned. [search summary, page not opened, link now 404] [Dot Esports](https://dotesports.com/?p=563060); [GINX, 3.4.0](https://www.ginx.tv/en/legends-of-runeterra-patch-3-4-0)
- Open beta patch: champions that can make level up progress from the deck (e.g. Ezreal, Hecarim) now pop up to show both players when progress is made. [search summary, page not opened] [PCGamesN, open beta patch](https://www.pcgamesn.com/legends-of-runeterra/open-beta-patch)
- Andrew Yip: a recent update displays champions at match start and when their level up progresses, making them more visible during games. [Out of Games, state of the beta](https://outof.games/legends-of-runeterra/921-riot-discusses-the-state-of-the-legends-of-runeterra-beta-mobile-balance-card-backs-card-styles-more)
- Patch 0.9.2 gave a keyword ("Skill") a new icon, showing keywords carry iterated icons. [community] [LoL Wiki, V0.9.2](https://wiki.leagueoflegends.com/en-us/V0.9.2_(Legends_of_Runeterra))
- A press piece describes attack and defence shown on each player's half of the board with a sword icon for attack and a shield for defence. [search summary, page not opened, exact source page uncertain] [Fandom article](https://www.fandom.com/articles/riot-legends-of-runeterra)
- [community] Overwhelm protrusion on the frame shows readiness to attack (see section 2). [LoL Wiki, Overwhelm](https://wiki.leagueoflegends.com/en-us/LoR:Keywords/Overwhelm)
- Official: once one copy of a champion levels up, all copies level up. [Riot Support glossary](https://support.riotgames.com/legends-of-runeterra/gameplay/keywords-and-game-vocab-legends-of-runeterra)

### Inferences
- [my memory, unsourced] Board units show Power bottom left and Health bottom right; damaged Health turns red and buffed stats turn green; keyword icons sit along the card. Verify before quoting.
- Translation: the crack overlay is the single most portable idea here. In an auto battler the player cannot act on every unit, but showing cracks on a sprite that the next blow will kill tells the player where the fight is about to turn. It also tells the leader which incoming big hit is worth parrying.
- Translation: the lethal Nexus preview maps to the leader or squad health bar: before a heavy blow lands, show a ghost chunk of the bar it will remove, louder if it would end the fight. That makes the parry decision readable in a glance.
- Translation: wear keywords on the body. At 32 px an icon row is hard to read; a base ring, glow or silhouette add that changes state when the trait is armed (as Overwhelm's frame piece does) reads better than a small icon.
- Translation: LoR's 3.4 fix came because players forgot hidden modifiers. In Kindred Siege, any hidden modifier (Pal upgrade, mark, combo) that changes a blow's outcome should show up in the preview, or the outcome will feel unfair.

### Gaps
- No developer talk on card frame or board unit layout, stat colours, or icon design rules.
- No developer source on how damage numbers are styled or timed.

## 4. Every hit changes the state: health dropping, deaths, Nexus damage, visible resolution

### Takeaway
LoR resolves each strike fully (damage, Overwhelm, Lifesteal, Nexus Strike) before deaths and death triggers, so each beat has a clean "hit, then consequence" order. Riot added previews of destruction (cracks) and of Nexus loss, and fixed the discard animation so the player sees what is lost before it vanishes.

### Cited Findings
- [community] Strike effects resolve before resulting effects such as death and Last Breath; strike effects do not trigger if the unit is obliterated after striking. [LoL Wiki, Strike](https://wiki.leagueoflegends.com/en-us/LoR:Vocabs/Strike)
- Official state rules that show damage persists and changes are timed by round: Regeneration heals to max Health at the start of each round; Barrier lasts one round; Frostbite sets Power to 0 until round end; Stun removes a unit from combat for the round. [Riot Support glossary](https://support.riotgames.com/legends-of-runeterra/gameplay/keywords-and-game-vocab-legends-of-runeterra)
- Patch 3.4: cracks on units facing lethal damage; Nexus preview more emphatic when lethal. [LoL Wiki, V3.4](https://wiki.leagueoflegends.com/en-us/V3.4_(Legends_of_Runeterra))
- Patch 0.9.2: discard made faster and clearer; opponent discards now show the card before it disintegrates. [search summary, page not opened] [Shacknews](https://shacknews.com/article/116943/legends-of-runeterra-patch-notes-092-slows-hecarims-charge)
- Nexus has 20 max Health outside special modes. [search summary, page not opened] [community] [Fandom wiki, Nexus](https://leagueoflegends.fandom.com/wiki/Nexus_(Legends_of_Runeterra))

### Inferences
- Inference: the existence of Regeneration (heal to full each round) implies damage on units persists between rounds by default, which keeps each hit's state change on screen until the unit dies or heals.
- Translation: "show the thing before it is destroyed" (the discard fix) is a rule for deaths in Kindred Siege: a dying sprite should hold its last readable pose for a beat, then break up, so the player registers who died.
- Translation: keep LoR's strict order inside one beat: impact, number, health drop, then death. Do not let the death start before the number has been seen.

### Gaps
- No developer source on unit death animations, Nexus hit animation, or the Nexus destruction sequence.
- No timings for any of these.

## 5. Rare big moments: champion level ups, champion spells, board effects, pacing statements, Path of Champions

### Takeaway
Level ups are LoR's set piece: a short, champion specific cinematic when a play condition completes. Early ones ran live in Unity; later ones are prebuilt videos made by outside vendors, because live scenes cost too much memory on low end devices and needed engineering per champion. Riot cut long chains elsewhere so these moments stay special. Path of Champions turned the same game into a roguelike with relics, champion levels and star powers.

### Cited Findings
- Riot's TinyEric (Reddit reply): older level ups, including the core set, ran in engine (rendered live); newer ones are premade videos. Running the level up scene live alongside the gameplay state uses too much memory for low end devices, and the memory cost of an idea is not known in advance, so each live level up took many iteration cycles. Live level ups needed a Unity art team plus dedicated engineering and tech art per champion; video lets an outsourced vendor work in parallel and keeps engineers and artists free. Retrofitting the old ones would have cost "millions of dollars and hundreds of hours of dev time." [devtrackers.gg, TinyEric reply](https://devtrackers.gg/legends-of-runeterra/p/b130da82-if-there-are-budgetary-issues-why-not-come-back-to-simple-level-up-animations-like-in-the-core-set)
- Andrew Yip: champion level ups are slick and champions often feel like their League counterparts. [Out of Games, state of the beta](https://outof.games/legends-of-runeterra/921-riot-discusses-the-state-of-the-legends-of-runeterra-beta-mobile-balance-card-backs-card-styles-more)
- 0.9.1 bug fix: champions who meet their level up condition while Detained now level up when they return. [Out of Games, 0.9.1 notes](https://outof.games/news/868-legends-of-runeterra-patch-091-notes-new-cosmetics-friend-challenge-xp-reverted-animation-speed-increases/)
- 0.9.2: level 2 Hecarim visuals sped up; Draven Spinning Axe sped up. [search summary, page not opened] [Shacknews](https://shacknews.com/article/116943/legends-of-runeterra-patch-notes-092-slows-hecarims-charge)
- Shurima expansion (3 March 2021): Renekton, Nasus and Azir can Ascend to level 3, the first time champions levelled more than once; design lead Mark Sassenrath said future level 3 or other levelling models were on the table. The Sun Disc counts down from 25. [Inven Global, Shurima](https://www.invenglobal.com/datalab/articles/13420/time-glory-brutality-how-the-legends-of-the-runeterra-team-brought-shurima-to-life)
- Skins (2021) can change a champion's summoned units and can grant entirely new level up animations; 3.4 lists Infernal Shen with a level up animation and new effects. [search summary, page not opened] [The Game Haus](https://thegamehaus.com/legends-of-runeterra/champion-skins-are-coming-to-legends-of-runeterra/2021/05/18/); [LoL Wiki, V3.4](https://wiki.leagueoflegends.com/en-us/V3.4_(Legends_of_Runeterra))
- Outside credits: Virtuos (Sparx and Virtuos Xi'an) credited for modelling, rigging, 2D assets, previz layout, animation, VFX and lighting on some 2023 skin level up animations; freelance VFX artist FangYu Chung shows Evelynn level up and spell VFX; Matt Schwartz's LoR VFX reel describes in game FX and leading artists across gameplay and cinematics, in collaboration with BeyondFX. [search summary via a third party bookmark list, credits not verified on ArtStation] [Raindrop bookmark list](https://raindrop.io/simon81/fx-3209787/embed/sort=-sort&perpage=30&page=0&search=%23runeterra)
- Path of Champions (official guide): hub World Map; Champion Campaigns per champion; Champion XP raises champion level, granting deck upgrades, items, relic slots; Champion Fragments buy Stars, which unlock Star Powers; Relics are equippable items with upgraded stats and passive abilities. [Riot Support, Guide to The Path of Champions](https://support.riotgames.com/legends-of-runeterra/gameplay/guide-to-the-path-of-champions)
- Path of Champions principal designer Shawn Main said the mode was built on earlier Labs work (Lab of Legends, The Saltwater Scourge) and kept LoR's back and forth play. [search summary, page 403 when opened] [Prima Games interview](https://primagames.com/gaming/legends-of-runeterra-path-of-champions-interview)
- [community] Guides describe four ways a champion gets stronger in PoC: Champion XP, Relics, global Legend Level, Stars; stars later became Constellations (5.5). [Mastering Runeterra](https://masteringruneterra.com/lor-path-of-champions-tips-and-tricks/); [Spellmana](https://spellmana.com/path-of-champions-constellations-new-features-legends-of-runeterra/)

### Inferences
- [my memory, unsourced] A level up interrupts the board: the champion's card is pulled forward, a short champion specific animation plays with a music sting and voice line, and the card returns with new art and stats. Length roughly a few seconds. Verify on footage before quoting any duration.
- Translation: Riot's split is a budget rule Kindred Siege can copy: baseline hits are cheap and fast; set pieces (a Pal's big move, a chief kill, a combo finisher) are rare, authored clips that can be prebuilt. TinyEric's point is that live, one off spectacle per character is the expensive path; a fixed, prerendered or pre baked clip per moment is cheaper and safer on memory.
- Translation: the Detained fix is a rule worth copying: if a payoff condition completes while the unit cannot show it, queue the moment and play it when the unit is back on screen. Never drop it silently.
- Translation: Path of Champions is the closest LoR relative to Kindred Siege (roguelike run, relics, per run powers). Its lesson for battle feel is that run upgrades show up as the champion's own card and stats changing, not as menus.

### Gaps
- No developer statement on level up duration, or on how often a set piece may interrupt play.
- No developer talk on Path of Champions presentation or animation pacing.
- No developer source on champion spell VFX design.

## 6. The battle as the player's build paying off

### Takeaway
Every champion has a visible level up condition built from things the player's deck does in play (draw 8 cards, kill 2 enemies, survive damage). Riot made progress toward it visible to both players, even from the deck, and Yip framed the game as watching plans develop back and forth and pairing champions. The level up is the payoff moment for the deck plan.

### Cited Findings
- Official: champions level up when they meet their unique condition; one levelling means all copies level. [Riot Support glossary](https://support.riotgames.com/legends-of-runeterra/gameplay/keywords-and-game-vocab-legends-of-runeterra)
- Examples: Twisted Fate levels after drawing 8 or more cards. [search summary, page not opened] [Inven Global, Twisted Fate](https://www.invenglobal.com/legendsofruneterra/articles/11077/twisted-fate-comes-to-legends-of-runeterra-holding-all-the-cards). Fiora levels at 2 kills; Braum's condition is surviving damage. [search summary, page not opened] [PC Gamer, champions list](https://pcgamer.com/all-legends-of-runeterra-champions-spells)
- Open beta: deck progress pop ups shown to both players (Ezreal, Hecarim). [search summary, page not opened] [PCGamesN](https://www.pcgamesn.com/legends-of-runeterra/open-beta-patch). Yip: champions shown at match start and when their level up progresses. [Out of Games](https://outof.games/legends-of-runeterra/921-riot-discusses-the-state-of-the-legends-of-runeterra-beta-mobile-balance-card-backs-card-styles-more)
- Andrew Yip interview: he wanted conversational play that goes back and forth, where players watch the opponent's plan develop over several actions while adjusting their own; players should find which cards pair well; players should be able to put favourite champions together, with the pairing both mechanical and thematic through voice over interactions. [Ungeek, Andrew Yip](https://ungeek.ph/2020/05/design-director-andrew-yip-shares-how-legends-of-runeterra-challenges-the-norm-of-digital-card-games)
- Patch 1.14 Q&A summary: the team likes champions that level up often. [search summary, page not opened] [outof.cards](https://outof.cards/comments/127892)
- Patch 3.4 reasoning included players forgetting a champion levelling after an attack was committed, which is why the Nexus preview accounts for it. [search summary, page not opened] [GINX, 3.4.0](https://www.ginx.tv/en/legends-of-runeterra-patch-3-4-0)

### Inferences
- [my memory, unsourced] The champion card in play shows its level up progress as a counter (for example x of 8), and champion pairs trade voice lines when played together. Verify before quoting.
- Translation: for an auto battler where the player does not act every second, the build payoff has to be announced. A visible counter on the unit or Pal toward its payoff, a pop up when progress happens off screen, and a voice or bark when two synergy units both act are the LoR toolkit.
- Translation: LoR's conditions are things the player can watch happen (draws, kills, damage survived). Kindred Siege payoffs should likewise trigger from events the fight already shows (parries, marks landing, kills), so the player can see why the payoff fired.
- Translation: Yip's "watch the opponent's plan develop" suggests showing enemy payoff counters too, so a big enemy moment is foreseen, not a surprise. This pairs with the leader's parry decision.

### Gaps
- No developer source on the in play progress counter UI.
- No developer source on how synergy triggers (not champion level ups) are visually called out.
