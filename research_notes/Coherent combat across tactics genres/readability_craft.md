# Readability craft: making a fight that plays itself read as cause and effect

Scope: intent systems, cause and effect perception, spectator needs, game feel and hit feedback, VFX clarity hierarchies, turn structure, concrete numbers. Written for Kindred Siege (2.5D pixel 5v5 auto battler, each band on its own 3x3 grid, units locked in pairs that trade blows, class panel shapes, leader parries big hits as the main input). Problem statement from Jacob: units "do whatever they want", combat lacks coherence and game feel.

Source quality note: several primary talks (Into the Breach postmortem, Slay the Spire interface story, Overwatch art GDC talk, Juice it or Lose it) are behind GDC Vault paywalls or video only. Where I cite a summary or transcript rather than the talk, it says so.

## 1. Intent systems: why showing what an enemy will do next makes combat feel fair and tactical

### Takeaway
Every shipped example (Slay the Spire, Into the Breach, Wildfrost) arrived at intent display after a version without it felt random or unplannable. The common pattern: one small glyph per actor, sized or shaped by threat, always visible without clicking, and a fixed countdown or fixed turn so the player knows WHEN as well as WHAT.

### Cited Findings
- Slay the Spire's original "next move" system showed each enemy's upcoming action as text; it hurt streaming (viewers had to click each enemy), was cluttered, and was inconvenient in multi enemy fights. Enemies picked moves at random, so players lacked information to plan. The icon based "intent" system replaced it and is described by the devs as the piece that made the game come together. (Secondary: AI transcript/summary of Ars Technica "War Stories" video with Casey Yano and Anthony Giovannetti, "How Slay the Spire's Original Interface Almost Killed the Game") — [readray.ai notes of Ars Technica War Stories](https://readray.ai/notes/1046045); title also listed at [nintygamer](https://nintygamer.com/tag/slay-the-spire/)
- Slay the Spire intent icon grammar: attack = sword icon that grows more menacing with damage (small dagger at 0 to 5 damage up to a scythe like icon at roughly 30 to 35+); block = shield; attack plus block = sword and shield; debuff = green swirl, strong debuff = big purple swirl; buff = blue flame; unknown = non damaging actions (summon, state change). Players read intent to choose offence or defence (enemy buffing: attack all out; big attack coming: block). Devs say this gave players control and offset randomness. Bosses often use fixed patterns; random selection has anti repeat code. (Same secondary source) — [readray.ai notes](https://readray.ai/notes/1046045)
- Wikipedia derived summary: team moved from a "Next Turn" system to an "Intents" system using icons for each enemy's next move — [Slay the Spire, Wikipedia](https://wikipedia.com/wiki/Slay_the_Spire)
- The only Mega Crit GDC talk found is Anthony Giovannetti, "Slay the Spire: Metrics Driven Design and Balance" (GDC 2019), which is about balance, not intents — [Game Developer](https://www.gamedeveloper.com/design/learn-i-slay-the-spire-i-s-metrics-driven-approach-to-game-balancing-at-gdc-2019); [GDC Vault](https://www.gdcvault.com/browse/gdc-19/play/1025731)
- Wildfrost: cards do not attack every turn; each has a "counter" counting down turns until it automatically acts. Each turn all counters drop by 1; at 0 the card triggers and the counter resets to max — [Steam store](https://store.steampowered.com/app/1811990/Wildfrost); [Wildfrost Wiki](https://wildfrostwiki.com/Getting_Started)
- Wildfrost devs (Will Lewis, Gaziter) kept returning to a "dynamic counter system, where each action counts as a turn"; restructuring battles to happen side by side with a polished counter system is what convinced them to continue. They kept the rest of the battle UI deliberately simple so the counter system stays readable, rejected a skip turn button as clutter, and fixed consistent terminology (what counts as a "hit") so cards behave predictably with counters — [MCV/Develop, "When We Made... Wildfrost"](https://mcvuk.com/?p=224506)
- Into the Breach (Subset Games, Matthew Davis GDC 2019 "Into the Breach Design Postmortem"): all enemy attacks are telegraphed; designers wanted less randomness than FTL and every death to feel like the player's fault — [GDC Vault](https://gdcvault.com/play/1025772/-Into-the-Breach-Design); [Game Developer IGF piece](https://gamedeveloper.com/game-platforms/road-to-the-igf-subset-games-i-into-the-breach-i-)
- Into the Breach: moving the threat onto cities worked because players always know where enemies will attack next and can disrupt it. The rejected alternative, to "flood the board with enemies and danger zones", became "needlessly complex and hard to parse" (Justin Ma). Davis: "You need to always be able to manipulate them"; an enemy you cannot move "becomes this unstoppable force and it's not fun at all." Ma: the most frustrating thing is "no options whatsoever to resolve." About 100 maps were hand designed instead of procedural to avoid unfun setups — [Game Developer, "Reimagining failure in strategy game design in Into the Breach"](https://gamedeveloper.com/design/reimagining-failure-in-strategy-game-design-in-i-into-the-breach-i-)

### Inferences
- Intent works because it turns a future event into a present, readable object. When the event then happens, it confirms a prediction, which is exactly what makes it read as caused rather than random (see section 2, event segmentation: boundaries and "randomness" feel come from prediction failure).
- Kindred Siege fix: every unit (or at least every pair) should show ONE intent glyph before it acts: target line to its locked partner, and a small icon whose size scales with the blow's damage (Slay the Spire dagger to scythe ladder). Big hits that the leader can parry need the loudest intent, since they are the player's decision point.
- Wildfrost's counter is the closest analogue to an auto battler: units act on visible countdowns instead of free running timers. A visible counter per unit (or per pair) tells the watcher who acts next, which directly answers "units do whatever they want".
- Into the Breach's lesson for the grid: more units and more danger zones made the board harder to parse, not more interesting. Fewer simultaneous threats, each fully legible, beats many.

### Gaps
- No primary GDC talk by Mega Crit on the intent system found; the detail comes from an AI summary of an Ars Technica video. Treat icon damage thresholds as approximate.
- Monster Train floor/intent design: no developer source found in this pass.
- No Wildfrost dev blog on counter rationale found (Chucklefish said one would follow).

## 2. Cause and effect: what makes players perceive a blow as caused by another

### Takeaway
Perceived causality is a tight timing effect: the reaction must follow contact within roughly 30 to 100 ms, and it degrades fast after that. Players also chunk action into events at goal endpoints and at prediction failures, and they can only track a handful of moving things at once (classic figure about 4, flexible with speed and spacing). So a blow reads as caused when: one attacker is clearly the focus, it winds up (prediction), it lands with an instant reaction on the target, and few other things move at the same time.

### Cited Findings
- Michotte launching effect: direct launching is reported constantly only for gaps up to about 50 ms, occasionally up to 100 ms; best causal impressions at about 30 to 40 ms delay — [Springer, Psychonomic paper on launching delays](https://link.springer.com/content/pdf/10.3758%2FBF03194815.pdf); [Frontiers in Human Neuroscience 2010](https://www.frontiersin.org/journals/human-neuroscience/articles/10.3389/fnhum.2010.00028/pdf)
- Delays of 100 to 200 ms may minimise perceived causality; Scholl and Nakayama's causal capture was confined to a 0 to 100 ms overlap, with larger delays significantly reducing judged causality — [Frontiers 2010](https://www.frontiersin.org/journals/human-neuroscience/articles/10.3389/fnhum.2010.00028/pdf); [Galway research on negative delays](https://research.universityofgalway.ie/en/publications/some-effects-of-negative-delays-upon-the-perception-of-causal-rel/) (caveat: some studies find individual differences and question how automatic causal perception is)
- Event Segmentation Theory (Zacks et al.): perceivers continuously predict the near future; event boundaries are marked where prediction fails; segmentation is automatic and tied to meaningful changes; boundary activity aligns with motion area MT/V5 — [Zacks et al. 2010, Frontiers in Human Neuroscience](https://www.frontiersin.org/journals/human-neuroscience/articles/10.3389/fnhum.2010.00168/pdf); [WUSTL "The brain's cutting room floor"](https://profiles.wustl.edu/en/publications/the-brains-cutting-room-floor-segmentation-of-narrative-cinema/)
- Intentional action endpoints (goal completions) are prime candidates for the boundaries observers detect when parsing action in real time — [Temple Infant Lab, "A goal bias in action"](https://templeinfantlab.com/wp-content/uploads/sites/2/2017/12/A-Goal-Bias-in-Action.pdf)
- Multiple object tracking: Pylyshyn and Storm (1988) showed observers track up to about 4 to 5 independently moving targets among identical distractors; the limit is commonly reported as four. Alvarez and Franconeri (2007) showed capacity can reach 8 or 9 at slow speeds and falls as speed rises, arguing for a flexible resource rather than a fixed number. Franconeri and colleagues argue object spacing (crowding) is the root constraint — [Alvarez & Franconeri 2007 PDF](https://visualcognition.psych.northwestern.edu/publications/AlvarezTracking2007.pdf); [Franconeri et al. 2010 Psych Sci](https://visualcognition.psych.northwestern.edu/publications/FranconeriPsychSci2010.pdf); [Franconeri et al. MOT review](https://visualcognition.psych.northwestern.edu/publications/Franconeri_etal_MOT_PBR.pdf)
- Anticipation as readable telegraph, Dark Souls 3 breakdown (Jerry Zhang): five phases, Opening Pose (telegraph silhouette conveying weight), Attack Signal (wind up, motion bent to feel instant), Attack (hitbox active only about 2 to 4 frames), End Pose, Return (recovery, usually the longest phase, the "window of opportunity"). Attack Signal plus Attack lasts at least 340 ms; End Pose at least 240 ms when used as bait, at least 170 ms before Return so the player can confirm the attack ended — [Game Developer, "Anatomy of an Enemy Attack in Dark Souls 3"](https://www.gamedeveloper.com/game-platforms/anatomy-of-an-enemy-attack-in-dark-souls-3)
- Same article, Model Human Processor: perceive about 100 ms (50 to 200), think about 70 ms (30 to 100), act about 70 ms (25 to 170). Input response: 50 to 100 ms feels tight, over 100 ms noticeable, about 200 ms sluggish (Mick West guideline) — [Game Developer DS3](https://www.gamedeveloper.com/game-platforms/anatomy-of-an-enemy-attack-in-dark-souls-3)
- Rule of thumb (blog level, lower quality): player reaction about 0.25 s; anticipation time = reaction time + time to trigger the answer + difficulty buffer (example: 8 frames reaction + 10 frames block = 18 frames minimum at 30 fps) — [Game Developer, "Enemy attacks and telegraphing"](https://gamedeveloper.com/design/enemy-attacks-and-telegraphing)
- Hit reaction: Sakurai (Smash) starts the hurt animation immediately on impact and blends flinch to hurt over four frames so damage reads as painful, not ambiguous — [Source Gaming translation of Sakurai's Famitsu column vol. 490](https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/)

### Inferences
- "Units do whatever they want" is, in perception terms, a prediction failure problem: the watcher cannot predict who acts next or at whom, so every action is an unexplained boundary. Fixes: visible pair links (who fights whom), visible intent and counters (when), and strict action, reaction pairs.
- Timing target for Kindred Siege: target's hit reaction (flash, flinch frame, knockback) must start within the same frame or the next couple of frames after contact (well under 50 ms at 60 fps, i.e. 0 to 3 frames). A reaction that waits for a separate damage tick 100+ ms later will not read as caused.
- Ten units on screen exceeds the about 4 object tracking budget, and close spacing on two 3x3 grids makes crowding worse. Only one or two pairs should be in their "loud" phase (wind up, hit) at a time; the others should idle or do small, low contrast motion. This matches the repo's own "one blow at a time" direction (rules 40) on the perception evidence.
- A 3 phase beat for each blow, sized from the DS3 numbers: wind up/telegraph roughly 300 to 500 ms (longer for parryable big hits so the player can react: at least 340 ms signal plus attack, and more for new players), contact with hitstop, then a held reaction and recovery the eye can confirm (170 ms+). These are LIKELY ranges derived from action games, not tested on an auto battler.

### Gaps
- No peer reviewed study found on how many simultaneous attacks a viewer can attribute correctly in a game scene; the MOT numbers are for tracking identical dots, which is a proxy.
- No source found giving canonical anticipation frame counts from Richard Williams or Disney applied to games; Disney's 12 principles (anticipation, follow through) were not fetched this pass.

## 3. Spectator UX: what watchers need

### Takeaway
Spectators need three things at any moment: who is winning, what just happened, and what is about to happen. Evidence from esports research says the information shown should differ for players and passive viewers, and that controlled information asymmetry (watcher knows something the actors do not) creates suspense.

### Cited Findings
- University of Washington 2011 study on StarCraft spectating: appeal comes from "information asymmetry", where players and spectators know different things, creating suspense; designers must decide what to show spectators and when to reveal it — [Slashdot summary of the UW paper "Why People Watch StarCraft Instead of Playing"](https://games.slashdot.org/story/11/05/16/1851224/Why-People-Watch-StarCraft-Instead-of-Playing)
- Kokkinakis et al. (IMX 2020), "Data Driven Audience Experiences in Esports" (Weavr): companion app delivering data driven insights during broadcasts, studying how presented statistics affect viewer experience; full findings not retrieved — [White Rose eprints](https://eprints.whiterose.ac.uk/160015)
- van Kempen, van der Spek and Wallner (2022): importance of dashboard elements differs across player, passive viewer and spectator player groups — [Springer chapter](https://springerlink.bibliotecabuap.elogim.com/content/pdf/10.1007/978-3-031-20212-4_23)
- Slay the Spire's text intents failed partly because they hurt streaming: viewers had to click each enemy to see moves. Always visible icons fixed this — [readray.ai notes of Ars Technica War Stories](https://readray.ai/notes/1046045)

### Inferences
- For Kindred Siege (a game the player mostly watches), the screen should always answer: band health totals or a tug of war bar (who is winning), a short log or kill feed style line for the last decisive event (what just happened), and the intents/counters plus the next parryable big hit warning (what happens next).
- Information asymmetry is usable: the player sees the enemy's big hit intent before the fighters "react", which makes the parry a moment of dramatic irony rather than a reflex test.

### Gaps
- No primary source found from Riot on TFT combat spectating or observer tools; no Valve/Dota observer UX talk retrieved. No auto battler specific spectator study found.
- No kill feed design study found.

## 4. Game feel and hit feedback for watched combat (GDC and dev talks)

### Takeaway
Hitstop (freeze both fighters on impact, scaled by damage, capped) plus a small directional vibration and an instant hurt pose is the most cited single tool for making a blow feel like it landed. Juice talks (Jonasson and Purho; Nijman) add screen shake, flashes, particles, sound, but those amplify an action only after it is legible.

### Cited Findings
- Sakurai hitstop rules (Smash Bros): both fighters freeze for the same duration; longer freeze for more damage; a hard cap regardless of damage (relevant for Hammer and Final Smashes); electric attacks get extra, projectiles less, Marth's sword tip more than the edge. Attacker also vibrates. Hurtboxes stay put while visuals shake. Grounded characters vibrate side to side, airborne up and down (vertical shake on ground clips feet). Shake strength scaled to camera distance. Shake decays over the freeze. Hitstop is capped in free for alls because a freeze gives a third party a window. Purpose: "emphasizing the power of impact"; vibration conveys shock better than a plain freeze in 2D. No frame values given in the column — [Source Gaming translation, Famitsu vol. 490](https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/)
- Sakurai's YouTube "Design Specifics" first video, "Stop for Big Moments!", is about hit stop: freezing the action for a moment whenever something impactful happens — [Automaton](https://automaton-media.com/en/news/20220824-15208/); [Destructoid](https://www.destructoid.com/smash-boss-sakurais-big-surprise-is-a-new-youtube-channel/); a later video covers 8 hit stop techniques — [GoNintendo](https://www.gonintendo.com/contents/13581-sakurai-s-latest-game-dev-video-features-8-hit-stop-techniques)
- Hitstop definition: number of frames both characters freeze when a hit lands — [Dustloop frame data glossary](https://www.dustloop.com/wiki/images/cache/9/90/BBTag%252FAkatsuki%252FFrame_Data.html)
- "Juice It or Lose It" by Martin Jonasson and Petri Purho (2012; venue reported as Nordic Game, also listed on GDC Vault under the Independent Games Summit) — [GDC Vault](https://gdcvault.com/play/1016487/Juice-It-or-Lose); a counter talk "Don't Juice It or Lose It" also exists — [GDC Vault](https://gdcvault.com/play/1020861/Don-t-Juice-It-or)
- Jan Willem Nijman (Vlambeer), "The Art of Screenshake", INDIGO Classes 2013 (Control Conference, Amsterdam) — [Game Developer, Vlambeer co founder shares advice](https://gamedeveloper.com/design/vlambeer-co-founder-shares-advice-on-building-better-action-games)

### Inferences
- For an auto battler, Sakurai's free for all rule matters: if several pairs fight at once, a global freeze for one hit stalls the others and muddles which hit caused it. Either freeze only the pair that hit (local hitstop), or sequence blows so only one lands at a time and use a global freeze for the big ones.
- Damage scaled hitstop is itself a readable channel: watchers learn that a long freeze means a big blow, without reading numbers.
- Juice amplifies whatever is on screen, including noise. Add it after the cause, effect chain reads, and only to the current focal blow.

### Gaps
- No primary Supergiant (Hades), Motion Twin (Dead Cells) or Team Cherry (Hollow Knight) combat feel talk was retrieved in this pass. No specific hitstop frame numbers from Sakurai found in text form.

## 5. The battle scene as presentation, and VFX clarity hierarchies

### Takeaway
Riot's clarity framework is the most explicit primary source: visual attention must match gameplay importance (damage, crowd control, dodgeability, impact on play), hitboxes must match effects, noise adds up fast, and silhouettes are the single most important recognition tool. Blizzard built Overwatch art around readability at distance via exaggerated silhouettes. Fire Emblem's GBA battle screen isolates one exchange at a time in a separate framed view; I found no developer statement of intent, only fan analysis.

### Cited Findings
- Riot "Clarity in League": clarity = "the ability to understand what's happening in League and respond to it". Three priorities: gameplay clearly conveyed, hierarchy preserved, noise minimal. Attention should match importance, judged on four factors: damage, crowd control, dodgeability, impact on play. Hitboxes should match effects; mismatches are a major source of frustration. Good examples: Zoe Q bright and saturated with an audio cue ahead of time; Taric stun with bright look, audio wind up and well defined hitbox. Bad example: Arcane Comet's large ground circle implies more dodgeability than real. Projectiles show direction even when still by blurring the tail. Visual and audio noise "adds up quickly"; flashiness has a cost unless the gameplay effect is large. Silhouettes are "the single most important thing for champion recognition"; test them in darkness; facing direction should read from silhouette; size signals role (bulky = tank). Reuse established shape language (scrolling down lines for spell shields). Avoid near permanent glows because they blur whether a glow means a state or an active ability — [League of Legends dev blog, Clarity in League](https://www.leagueoflegends.com/en-us/news/dev/clarity-in-league/)
- Riot process: designers provide a Visual Hierarchy document rating a spell's importance 1 to 10 so visual importance matches gameplay importance; Garen Mecha Kingdoms auto attacks were too bright and opaque and were made more transparent — (search summary of Riot clarity post; the 1 to 10 rating was not present on the page version fetched, so treat as secondary) [Ask Riot: Let's Talk Clarity](https://leagueoflegends.com/en-us/news/dev/ask-riot-let-s-talk-clarity); [Surrender at 20 red post collection](https://www.surrenderat20.net/2021/03/red-post-collection-lol-pls-client.html)
- Riot 2014 Summoner's Rift update: visual hierarchy ordered as in game UI, then champion and unit abilities and attacks, then champions and units (environment beneath) — [PCGamesN](https://www.pcgamesn.com/how-riots-summoners-rift-update-is-improving-clarity-in-league-of-legends); [Surrender at 20](https://www.surrenderat20.net/2014/06/update-to-summoners-rift-headed-to-pbe.html)
- Riot VFX style guide lists visual clarity for gameplay as a primary goal — [Nexus, League's VFX style guide](https://nexus.leagueoflegends.com/en-us/2017/10/dev-leagues-vfx-style-guide/); missile VFX communicates direction, speed, danger — [RealtimeVFX, Anatomy of a League missile part 2](https://realtimevfx.com/t/anatomy-of-a-league-of-legends-missile-part-2-what-are-we-communicating-with-our-vfx/13062)
- Overwatch: readability "is the cornerstone of their art style" (BlizzCon 2014 recap); gameplay is designed first, then the hero; exaggerated attributes give distinctive silhouettes identifiable at distance (Sam Didier, via PC Gamer); GDC 2017 talk "The Art of Overwatch: Evolving a Legacy" (Bill Petras, Arnold Tsang) — [BlizzPro BlizzCon panel](https://blizzpro.com/?p=150); [PC Gamer](https://www.pcgamer.com/how-torbjrn-became-the-art-inspiration-for-all-of-overwatch); [GDC Vault](https://gdcvault.com/play/1024268/The-Art-of-Overwatch-Evolving)
- TFT (unofficial designer case study): board readability work includes lighting and colour saturation and tweaking VFX colours; hovered units highlighted with on model VFX — [Behance TFT UI case study](https://www.behance.net/gallery/133371555/Teamfight-Tactics-UI-Design)
- Fire Emblem GBA battle animations: separate full screen 248x160 animation per exchange, with distinct normal, critical and ranged animations; a loop of 24 unique frames shown over about 60 frames; crits are flashier. Fan sources; some players turn animations off as they slow pace — [fe-battle-animations tutorial](https://fe-battle-animations.neocities.org); [lost worlds blog](https://lost-worlds.neocities.org/blog/2024/10/20/fire-emblem-animation/); [FE Universe poll](https://feuniverse.us/t/battle-animations-on-or-off-poll/9092/13)

### Inferences
- Kindred Siege should keep a written importance rating per effect (Riot's 1 to 10 idea): leader parryable big hits and kills at the top, class panel moves next, ordinary trades low contrast and small. Ordinary auto attacks being as bright as specials is exactly the Garen Mecha problem.
- The ring/panel shapes per class (already in the repo's class move art) are "established shape language" in Riot's sense; keep each shape meaning one thing.
- Fire Emblem's battle screen is a strong precedent for "one exchange at a time in focus": the camera (or a highlight) isolates the active pair, so the watcher cannot miss attacker, defender and outcome. A light version for 2.5D: dim/desaturate idle pairs, tint the focal pair.

### Gaps
- No Valve or Blizzard VFX clarity talk retrieved with specifics. No developer source on Fire Emblem, Pokemon or Octopath battle presentation intent found.

## 6. Turn structure psychology: alternating vs simultaneous action

### Takeaway
I found no direct study comparing alternating and simultaneous combat for readability. The perception evidence (MOT limit about 4, causal window under 100 ms, segmentation at prediction failures) implies alternating or sequenced actions read better because each produces one clean cause, effect boundary; simultaneous action works when it is slow, sparse, and each actor's target is pre committed and visible.

### Cited Findings
- Wildfrost moved to "each action counts as a turn" with visible counters and found it more distinctive and satisfying; side by side battles plus counters is what made the prototype work — [MCV/Develop](https://mcvuk.com/?p=224506)
- MOT capacity falls with speed and crowding — [Alvarez & Franconeri 2007](https://visualcognition.psych.northwestern.edu/publications/AlvarezTracking2007.pdf); [Franconeri et al. 2010](https://visualcognition.psych.northwestern.edu/publications/FranconeriPsychSci2010.pdf)
- Sakurai caps hitstop in free for alls because one freeze opens a window for third parties, a sign that simultaneous fighters complicate per hit emphasis — [Source Gaming](https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/)

### Inferences
- Kindred Siege's locked pairs already pre commit targets (good for simultaneous play), but if five pairs swing on independent timers the scene is five overlapping cause, effect chains, above the tracking budget. Two workable models: (a) a turn conductor, one pair's blow lands at a time on a shared beat (Wildfrost/Fire Emblem style); (b) simultaneous but with only small, low contrast trades and a single focal pair promoted for any significant blow (TFT style with a hierarchy). Model (a) matches the repo's rules 40 "one blow at a time" and the evidence.
- A beat length estimate from the numbers: telegraph about 0.3 to 0.5 s, contact plus hitstop about 0.05 to 0.2 s, reaction/recovery about 0.2 to 0.4 s, so roughly 0.6 to 1.0 s per readable blow. GUESS, derived from DS3 and causality data, not measured on an auto battler.

### Gaps
- No TFT developer source on how Riot keeps simultaneous combat readable. No academic work on turn based vs real time comprehension found.

## Summary for Kindred Siege: what fixes "units do whatever they want"
(Inferences, each tied to findings above.)
1. Show intent: a link line from each unit to its locked partner, a damage scaled intent icon (StS ladder), and a visible countdown (Wildfrost) so the next actor is predictable.
2. Sequence the loud parts: only one or two blows in wind up/impact at a time; others idle quietly (MOT about 4, crowding, Into the Breach "hard to parse").
3. Action, reaction within 0 to 3 frames: hurt pose, flash and knockback start on contact (Michotte under 50 ms; Sakurai instant hurt pose).
4. Hitstop scaled to damage, capped, local to the pair, with directional vibration decaying over the freeze (Sakurai).
5. Telegraph parryable leader hits longest and loudest: at least about 340 ms signal plus attack, more for new players (DS3), with audio wind up (Riot Zoe/Taric examples).
6. Visual hierarchy document rating every effect 1 to 10; ordinary trades dim and small, parry moments and kills top (Riot).
7. Always on spectator answers: who is winning bar, last event line, next big threat (esports spectator research, StS streaming lesson).
