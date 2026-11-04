# How units in Kindred Siege should fight

Research done 4 Nov 2026, from the web. Question: how should units visibly fight so a player always sees who hits whom, what the Warden blocks, and exactly when and what to parry, with the parry at the centre of every fight?

## Short answer

Build a fourth direction, **Lit lanes**. Fighters keep their tiles. Every blow is a short lunge (melee) or a visible shot (ranged), and its contact frame lands at the exact sim time of the hit. During the wind-up, the target's tile glows in the attacker's colour. Every enemy heavy blow puts a fuse bar on the target's tile with a gold zone drawn from the start, plus 3 ticks of sound. Hold gives a clang, a short freeze, a reel and a counter.

Two small sim rules keep the telegraph honest: targets lock when the wind-up starts, and only one enemy heavy blow can be winding up at a time.

## What is wrong on screen today

1. **No contact moment.** The 40 frame attack is stretched over the whole wait between attacks, so no frame says "the hit lands now". Every parry game below times the press against a visible contact.
2. **No link from attacker to target.** Fighters swing from 240 px away with nothing joining them to the one they hit.
3. **The parry cue starts too late.** The gold tint appears when the 0.2 s window opens. A simple visual reaction takes most people about 0.25 s, so a player who waits for the tint presses after the window has closed. The cue has to count down to the window so the player can predict it.
4. **Guard has no art**, so the Warden looks idle.
5. **Damage has no visible source.** Text tags and no target link mean a big hit can't be traced to anyone.

One open question: fights run at half speed. If the 0.2 s window is counted in sim time, the player gets 0.4 s of real time. If it is counted in real time, 0.2 s. That doubles or halves the difficulty, so it should be a deliberate choice.

## The games

| Game | How hits read | How an answerable attack is shown | Numbers from sources | What transfers to us |
|---|---|---|---|---|
| Pokémon (main series) | One move at a time: move name in text, animation, target blinks, HP bar drains, then effectiveness text. | No timed answer. | Gen IV HP bar drains 1 HP per 2 frames at 60 fps. Gen I has a dedicated "target blinks quickly" hit animation. | Target flash on every hit and a readout after the strike. Strict one-at-a-time order is clear but too slow for 10 fighters. |
| Fire Emblem GBA | Close-up scene per fight: attacker hits, defender counters, faster unit can hit twice. Separate dodge and miss animations. Crits get their own flashier animation. | No timed answer. | One loop: 24 drawn frames shown over about 60 frames (1 s). Frames 248 x 160 px, 16 colours. | A crit has a longer, different wind-up than a normal hit, the same idea as our heavy blow. Few drawn frames held longer still read well and are cheap. |
| Mario & Luigi | Enemies attack one at a time. | Every enemy attack has a tell, and the tell shows which brother is the target. The attack decides the answer (jump or hammer). | Some attacks are too fast to answer by reaction; the player learns the tell. | The tell names the target. Our attacker should face and lean toward the fighter he is about to hit. |
| Paper Mario: TTYD | One actor at a time on a stage. | Guard and Superguard, pressed just before contact. | Guard: 8 frames (0.13 s) before contact. Superguard: 3 frames (0.05 s). Each badge moves the window by 1 frame. | Our 0.2 s (12 frames) is wider than TTYD's normal guard. Two reward tiers on one button work. |
| Sea of Stars | Turn based; timed hits and timed blocks are the core of combat. | Block timed to the enemy's contact. A relic adds a star flare on a successful press. | Windows differ per skill. A timed block cuts damage by about half (community guide, low confidence). | Reviewers call multi-hit timings random. Every answerable hit needs one clear contact frame. A success flash teaches the timing. |
| Clair Obscur: Expedition 33 | Turn based. On enemy turns you parry or dodge each hit in real time. Director chose fixed patterns over random chance so deaths feel fair. | Enemy wind-up animation plus sound. Parrying a full chain triggers a counter. | Community reports: about 0.15 s on Expert, wider on easier settings (claims range 0.12 to 0.25 s). Not confirmed by the studio. | Closest match to our design: a parry inside a non-action fight, taken from Sekiro. Parry opens a counter. Sound carries much of the timing. |
| Sekiro | One opponent at close range. A deflect makes a clang and sparks and builds the enemy's posture until it breaks. | Enemy wind-up; a red warning symbol and sound for attacks you can't deflect. | Wiki and mod data: 30 frame (0.5 s) default input window, cut to 7 frames if you spam guard. One animation analysis says 12 frames (0.2 s). Sources conflict. | Clang, sparks and a posture break make a parry feel decisive. A spam penalty stops mashing. |
| Mega Man Battle Network | 6 x 3 grid, 3 x 3 per side. Fighters stay on panels. | Panels about to be hit flash red. A charging enemy's row lights up before it lunges. | 3 rows per side, the same as our 3 lanes. | The grid is the warning layer. Tiles never overlap, so 10 fighters stay readable. |
| Into the Breach | Every enemy attack is shown before the player acts. | Red tiles, arrows and a numbered attack order. | Full information every turn. | Show the target tile during the wind-up. Number blows that land close together. The developers say they drop cool ideas for clarity every time. |
| Slay the Spire | Turn based card fights. | Icon above each enemy with next turn's damage and hit count. | Intents were added after playtests: hidden enemy actions clashed with permadeath. | Show how hard the next heavy blow hits. A telegraph must never lie. |
| Darkest Dungeon | 4 ranks per side. Each skill works only from some ranks into some ranks. | Each action zooms in: attacker and target are drawn close together and the rest fades. | One action at a time. DD2 has a GDC talk on its combat presentation tools. | Fighters keep their ranks in the rules, yet the blow is shown up close. That keeps slots and still shows contact. |
| Super Auto Pets | Front pets bump one pair at a time; the next pet steps up. | None. | Reviewers find it easy to follow; one found TFT footage confusing. | One pair in contact reads at a glance. |
| Mechabellum | Large battles; units fire visible shots. | None. Players can click units mid-battle to see stats. | Starts with a few units, ends with large armies. | Players accept watching without control when cause and effect is visible. |
| Teamfight Tactics | Many units at once. | None. | Riot recoloured damage numbers by damage type and made crit text pop. | Colour damage numbers by source. Big hits get bigger numbers. |
| Kingdom Rush | Barracks soldiers each engage and hold one enemy; towers shoot over them. | None. | 3 soldiers per barracks, each holding one enemy. | Melee in contact in pairs, ranged as visible projectiles over the line. |
| Super Smash Bros (hit stop) | Both fighters freeze on a hit; the struck one shakes. | n/a | Hit stop grows with damage: a 15% move gets 15 frames. Cap 20 frames in Melee, 30 from Brawl on. 4 frames to blend into the hurt pose. | Small freeze for normal blows, big freeze for a parry. |
| Dark Souls 3 (design analysis) | n/a | Wind-up, then attack. | Signal plus attack lasts at least 340 ms; a feint at least 240 ms. | Our 1 s heavy tell sits well above that floor. Normal blows can be short because nobody answers them. |

I found no auto battler with a timed parry. The nearest designs are turn based: Expedition 33 and Mario & Luigi.

### What every parry game here shares

- **One answerable thing at a time.** TTYD, Mario & Luigi, Sea of Stars, Expedition 33 and Sekiro all give the player one incoming attack to answer at a time. Into the Breach shows many, but numbers them.
- **The tell names the target before the timing matters.** Mario & Luigi, Into the Breach and Battle Network all do this.
- **Timing is read from something the player watches approach:** a blade travelling, a lit panel, a rhythm of sound.
- **Success is loud.** Clang, sparks, a freeze, a counter.
- **Telegraphs never lie.** Slay the Spire's intents always come true.

## The four directions

### A. Exchanges (Pokémon, Fire Emblem GBA, Darkest Dungeon)

Blows take turns. Each enemy heavy blow gets its own beat: everything else dims and stops, the attacker steps up and holds his wind-up in front of your fighter, and flashes gold at the window. Hold: weapons clang, he reels, your counter is the next exchange.

- **How the parry reads:** best of the four. One thing on screen, one target, one moment.
- **Pros:** Jacob named Pokémon himself. "Who hit whom" is never in doubt during a beat. The Warden can step in front during the beat, which shows the block plainly.
- **Cons:** to dim and stop everyone else honestly, the sim has to stop their timers too. If they kept fighting while dimmed, real blows would be hidden. Every heavy beat makes the fight longer, on top of half speed. Normal blows between beats still need fixing.
- **Cost:** sim rule change (other fighters' clocks pause during an enemy heavy beat). New art: step-up, held wind-up, reel, clang spark, counter.
- **Against Jacob's notes:** fixes the parry cue and "what happened" for heavies. Leaves normal blows as they are unless also fixed.

### B. Scrum (auto battlers, Kingdom Rush)

Front rows charge and meet in the middle in pairs. Back rows shoot over with visible shots. The enemy lifts a huge swing with a gold glint. Hold: your fighter catches the blade, both freeze, he reels.

- **How the parry reads:** weakest. The heavy swing happens inside a crowd, with other pairs swinging next to it.
- **Pros:** looks most like fighting, which is Jacob's loudest complaint.
- **Cons:** fighters leave their slots on screen, so rows and lanes stop meaning anything to the eye. The Warden covering an ally in another lane, the Skirmisher reaching the back row and Fall Back all get hard to show. 10 sprites meeting in the middle overlap. If slots stay in the rules but sprites stand in the middle, the screen no longer matches the rules.
- **Cost:** most art: run cycles for every class and enemy, pairing and spacing code so sprites don't stack, ranged arcs. Possibly a sim change if position starts to matter.
- **Against Jacob's notes:** fixes "they don't fight". Makes "I don't know what I'm parrying" and "what is the Warden blocking" worse.

### C. Grid strikes (Mega Man Battle Network, Into the Breach)

Fighters keep their tiles. Tiles about to be hit light up in the attacker's colour. The tile under your fighter fills like a fuse and turns gold at the window. Hold: the strike hits a gold shield on the tile and bounces back along the lit tiles.

- **How the parry reads:** strong. The fuse counts down, so the player can predict the window instead of reacting to it.
- **Pros:** fits every lock (slots, rows, lanes). Tiles don't overlap, so it stays readable with 10 fighters. Cheap. Warden cover can be a tether from his tile to the ally's tile.
- **Cons:** fighters still swing from 240 px away, so "they just wave around" stays. The fight is read on the floor, away from the fighters.
- **Cost:** mostly screen: tile overlay, fuse, travel effects. Small sim change: expose each blow's target and land time early, and lock targets at wind-up start. Almost no character art.
- **Against Jacob's notes:** fixes the cue, the target and the damage source. Does not fix "they don't fight".

### D. Lit lanes (recommended): C's floor, A's focus, Darkest Dungeon's lunge

**Every blow.** The attacker plays a short wind-up in his slot while the target's tile glows in the attacker's colour. Melee lunges to weapon reach of the target (about 200 of the 240 px), lands the contact frame on the exact sim hit time, and snaps back. Ranged fires a visible shot along the lane. The target flashes white for 2 frames and gets a small freeze (about 3 frames, both sprites only). The damage number is coloured by the attacker's side, and heavy hits get large numbers.

**Enemy heavy blows.** The attacker holds a distinct raised pose and faces his target (the Mario & Luigi tell). A fuse bar fills on the target's tile over the wind-up (1 s, or the chief's 3 s tell). The last 0.2 s of the bar is gold from the start, so the player watches the fill approach the gold. 3 ticks spaced 0.2 s apart end as the window opens. Everyone else keeps fighting at lower saturation and the camera eases about 5 to 10% toward the pair. Nothing freezes during the window, since that would stretch it.

**Hold lands.** Clang, sparks, a freeze of about 12 frames after the press resolves, the attacker reels back to his tile, and your counter lunges in. A whole-screen freeze that pauses the sim clock for everyone changes no outcome.

**Warden.** A tether joins the Warden to the ally he guards. When a blow targets that ally, the tile glow jumps to the Warden, he steps half a tile in front with his shield up for the contact frame, and the smaller damage number carries a shield icon. If a heavy targets the guarded ally, the fuse moves to the Warden's tile, so the player sees who will take it.

**Marks** become icons on the fighter, replacing the text tags.

- **How the parry reads:** close to A, because the pair is focused and the fuse counts down, without stopping the fight.
- **Pros:** answers all five problems above. Keeps every lock. No run cycles: a lunge is a sprite move. Its art (wind-up, hold pose, reel, clang, block) is the same art A needs, so if playtests still find it busy, A's full beat can be added later by adding the clock-stop rule.
- **Cons:** more on screen than A. The lunges of several fighters can cross in the gap; low saturation for non-heavy blows during a heavy keeps that in check. Needs 3 small sim rules.
- **Cost:**
  - Screen: tile overlay, colour glow, fuse bar with gold zone, ticks, tether, mark icons, coloured numbers, camera ease, saturation.
  - Sim: (1) publish each blow's attacker, target, heavy flag and land time at least one wind-up ahead (the parry window already needs the land time); (2) lock the target when the wind-up starts; (3) only one enemy heavy wind-up at a time, the next waits until the first resolves. Rule 3 changes outcomes, so it is a rule change. It removes the case where two heavies land inside one 0.2 s window and one Hold has to cover both.
  - Art per class: the current 40 frame attack split into wind-up, strike and recover, played at its own speed. New: heavy hold pose, reel, Warden block pose, parry spark. ComfyUI can make poses now; PixelLab after 6 Dec.

### Side by side

| | A. Exchanges | B. Scrum | C. Grid strikes | D. Lit lanes |
|---|---|---|---|---|
| Who hits whom | Clear for heavies | Muddled | Clear | Clear |
| What to parry, when | Best | Weak | Strong | Strong |
| Units look like they fight | Yes, in beats | Most | No | Yes |
| What the Warden blocks | Clear in beats | Muddled | Tether only | Tether and step-in |
| Where big damage came from | Heavies only | Muddled | Clear | Clear |
| Keeps slots, rows, lanes | Yes | On screen, no | Yes | Yes |
| Sim change | Clock stop per beat | Maybe positions | Target lock, early schedule | Target lock, early schedule, one heavy at a time |
| New art | Medium | Most (run cycles) | Least (effects) | Medium (poses, no run cycles) |

## Recommendation

Go with **D, Lit lanes**. It is the only direction that answers every line of Jacob's notes: units visibly strike one target, the target is lit before the hit, the Warden steps in, damage is traceable, and the parry has a countdown the player can read in time. It keeps every locked choice and needs no run cycles. Its sim changes are small, and they are the same ones that keep the telegraph truthful.

Build order, smallest first, so each step can be playtested:

1. Split the attack into wind-up, strike and recover; play it at its own speed so the contact frame lands on the sim hit. Fighters idle between blows.
2. Target tile glow and melee lunge, ranged shots.
3. Heavy blow fuse with the gold zone, 3 ticks, clang, freeze, reel, counter.
4. Warden tether, step-in and block pose.
5. Coloured damage numbers and mark icons.

After step 3, one playtest question for Jacob: "Who hit whom, and what did you parry?" If he can answer, the spine works.

The timing values above (3 frame and 12 frame freezes, 3 ticks, 200 px lunge, 5 to 10% camera) are starting points I chose from the sources, not numbers any game publishes for this case. Tune them in play.

## Sources

1. Sekiro deflection, Fextralife wiki: https://sekiroshadowsdietwice.wiki.fextralife.com/Deflection
2. Sekiro deflect timing mod and param notes, Nexus Mods: https://www.nexusmods.com/sekiro/mods/1374
3. Sekiro combat guide, Push Square: https://www.pushsquare.com/news/2019/03/guide_sekiro_shadows_die_twice_combat_-_posture_deflections_shinobi_prosthetic_and_stealth
4. Expedition 33 director on taking the parry from Sekiro, GamesRadar: https://gamesradar.com/games/rpg/clair-obscur-expedition-33-creative-director-put-parrying-in-his-turn-based-rpg-after-playing-sekiro-and-thinking-why-cant-i-parry-in-my-game
5. Expedition 33 director on patterns over random chance, Spilled: https://spilled.gg/clair-obscur-expedition-33-director-combat-system-frustrating-deaths-patterns-random-chance
6. Expedition 33 parry window reports, Steam discussions: https://steamcommunity.com/app/1903340/discussions/0/592896658667209825
7. Expedition 33 parry timing guide, Push Square: https://pushsquare.com/guides/clair-obscur-expedition-33-how-to-improve-parry-timing-and-make-parrying-easier
8. Paper Mario Guard, Super Mario Wiki: https://www.mariowiki.com/Guard
9. Paper Mario Superguard, Super Mario Wiki: https://mariowiki.com/Superguard
10. Mario & Luigi counterattacks and tells, Super Mario Wiki: https://mariowiki.com/Counterattack
11. Sea of Stars creator interview, RPG Site: https://rpgsite.net/interview/14807-developer-debrief-chatting-with-the-mind-behind-sea-of-stars
12. Sea of Stars, TV Tropes (timed hit relic and success flare): https://www.tvtropes.org/pmwiki/pmwiki.php/VideoGame/SeaofStars
13. Mega Man Battle Network battle system analysis: https://saschb2b.github.io/game-mechanics/games/mega-man-battle-network/battle-system
14. Mega Man Battle Network combat basics, Wikibooks: https://en.wikibooks.org/wiki/Mega_Man_Battle_Network/Combat_Basics
15. Into the Breach review, GameSpot: https://gamespot.com/reviews/into-the-breach-review-a-mechanized-masterpiece/1900-6416865/
16. Into the Breach developers on UI clarity, Game Developer: https://gamedeveloper.com/design/-i-into-the-breach-i-dev-on-ui-design-sacrifice-cool-ideas-for-the-sake-of-clarity-every-time-
17. Into the Breach combat guide (attack order, red marks), Gamepressure: https://www.gamepressure.com/into-the-breach/combat/z1aa78
18. Slay the Spire, Wikipedia (origin of intents): https://en.wikipedia.org/wiki/Slay_the_Spire
19. Darkest Dungeon, Wikipedia: https://en.wikipedia.org/wiki/Darkest_Dungeon
20. Darkest Dungeon II combat presentation talk, GDC Vault: https://gdcvault.com/play/1034466/Independent-Games-Summit-Critical-Combat
21. GBA Fire Emblem battle animation guide: https://fe-battle-animations.neocities.org
22. Pokémon Gen I battle animations, Bulbapedia: https://bulbapedia.bulbagarden.net/wiki/List_of_battle_animations_by_index_number_(Generation_I)
23. Pokémon HP and HP bar, Bulbapedia: https://bulbapedia.bulbagarden.net/wiki/HP
24. Smash hitlag numbers, SmashWiki: https://www.ssbwiki.com/Hitlag
25. Sakurai's hitstop column (fan translation), Source Gaming: https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/
26. Anatomy of an enemy attack in Dark Souls 3, Game Developer: https://www.gamedeveloper.com/game-platforms/anatomy-of-an-enemy-attack-in-dark-souls-3
27. Super Auto Pets battles, GamingOnLinux: https://gamingonlinux.com/2022/02/send-help-as-i-have-discovered-super-auto-pets/page=1
28. Super Auto Pets readability, Indie Hell Zone: https://indiehellzone.com/2021/10/26/super-auto-pets/
29. Mechabellum cause and effect, PC Gamer: https://www.pcgamer.com/games/strategy/mechabellum-is-a-conversation-you-should-be-having
30. TFT damage text changes (PBE notes), Surrender at 20: https://www.surrenderat20.net/2019/06/618-pbe-update-arcade-caitlyn-pe-tft-up.html
31. Kingdom Rush basics, Pocket Gamer: https://www.pocketgamer.com/kingdom-rush/basic-strategies

Reliability: Sekiro and Expedition 33 window numbers come from wikis, mods and players, and they disagree. Paper Mario and Smash numbers come from community wikis built on game data. The 0.25 s reaction figure is a common average for simple visual reaction, not from these sources.
