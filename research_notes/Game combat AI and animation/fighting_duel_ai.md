# Fighting game and duel AI: how CPU opponents decide, learn and stay fair

Source quality key used below:
- (read) = I opened the page and the fact is on it.
- (search summary) = the fact comes from a search result summary of that page. I did not open the page. Treat as a lead and check before quoting.
- "Developer confirmed" means the developer said it (talk, paper, blog). "Community" means wiki, forum or dataminer.

## 1. How do classic and modern CPU opponents decide (Street Fighter, Tekken, Mortal Kombat, Smash Bros level 9 CPUs, Guilty Gear)? Input reading, difficulty through reaction delays, randomness.

### Takeaway
The standard commercial pattern is a reactive rule set ("macros") whose difficulty is a few dials: how often it blocks, how accurate its block and counter are, how fast it reacts, and how likely it is to follow through on a decision. Turned up to the top, those dials give one frame reactions that players call input reading and hate. I found no developer source for the insides of Street Fighter, Tekken, Mortal Kombat or Guilty Gear arcade CPUs.

### Cited Findings
- Traditional fighting game AI is reactive macros tuned by parameters: block frequency, block and counter accuracy, and action timing. Low values give an AI that fails to respond to the player. High values give near perfect blocks and counters, which the article ties to frustrating bosses such as Gill (Street Fighter III), Shao Kahn (Mortal Kombat) and Eyedol (first Killer Instinct). Source is an AI and Games article built on the Killer Instinct developers' talks, so this is a secondhand description, not a statement from Capcom or NetherRealm. (read) [Game Developer, The Killer Groove](https://gamedeveloper.com/programming/the-killer-groove-the-shadow-ai-of-killer-instinct)
- Smash Bros: CPU level sets how likely the CPU is to act on a decision and how fast it reacts. Level 1 often waits a long time before acting. Level 9 almost always acts at once. Community wiki. (read) [SmashWiki, AI](https://www.ssbwiki.com/AI)
- Smash Bros: low levels rarely shield or dodge and mostly stand next to the opponent using weak attacks. High levels defend almost constantly and use aerials, smash attacks and grabs more. So level changes the move mix as well as the speed. Community wiki. (read) [SmashWiki, AI](https://www.ssbwiki.com/AI)
- Smash Bros from Brawl onward stores level as a number from 0 to 100: level 1 = 0, 2 = 15, 3 = 21, 4 = 31, 5 = 42, 6 = 48, 7 = 60, 8 = 75, 9 = 100. Events can use values in between. Community wiki from game data. (read) [SmashWiki, AI](https://www.ssbwiki.com/AI)
- Smash Bros Brawl: a 2013 community analysis (by Toomai) found level 9 CPUs have one frame reaction times but do not read controller inputs. They react to the game state one frame later. The earlier belief that they read inputs was not supported. Community. (read) [SmashWiki, AI](https://www.ssbwiki.com/AI)
- Smash Bros Brawl: the rumour that CPUs learn from players was disproved by hacking research. Save files of different ages behaved the same, replays store only human inputs (so the CPU must be deterministic for replays to sync), and the AI code holds no learning logic. Community. (read) [SmashWiki, AI](https://www.ssbwiki.com/AI)
- Smash 4 and Ultimate: level 7 to 9 CPUs air dodge almost frame perfectly, but only when an attack or projectile comes close. The wiki lists this as a flaw because it makes aerial combos hard to practise. It also says the AI "cannot mind game" and does not learn from mistakes, and that its predictable habits teach players bad habits. Community. (read) [SmashWiki, AI](https://www.ssbwiki.com/AI)
- Smash Bros recovery by level: low levels use a simple, predictable recovery. High levels mix techniques. Example: Smash 4 Luigi uses only Super Jump Punch at levels 1 to 5 and adds Green Missile and Luigi Cyclone at levels 6 to 9. So difficulty also unlocks options, it does not only speed things up. Community. (read) [SmashWiki, AI](https://www.ssbwiki.com/AI)
- SNK Neo Geo fighters such as Art of Fighting 2 are described by players as input reading: the CPU reads the player's input, then counters with set moves, and players win by baiting the wrong reaction. Forum guide, not a developer. (search summary) [GOG forum](https://www.gog.com/forum/snk/art_of_fighting_2_cheesing_the_nonboss_cpu_characters_and_mr_big)
- Street Fighter 6's hardest CPU is described by press as input reading that detects your attack and answers with an optimal counter. Press opinion, not Capcom. (search summary) [Push Square](https://www.pushsquare.com/news/2023/04/even-fighting-game-legends-cant-beat-street-fighter-6s-hardest-difficulty)
- Human reaction numbers used by researchers: a Blade and Soul pro level AI paper applied about 230 ms of delay to match professional players. A Smash Melee paper treats 12 frames (at 60 fps) as the lowest human plausible reaction and about 15 frames as an average human. (search summary) [arXiv 1904.03821](https://arxiv.org/pdf/1904.03821), [arXiv 1810.07286](https://www.arxiv.org/pdf/1810.07286)
- The Killer Instinct article gives competitive human reaction as roughly 12 to 19 frames at 60 fps. (read) [Game Developer, The Killer Groove](https://gamedeveloper.com/programming/the-killer-groove-the-shadow-ai-of-killer-instinct)

### Inferences
- "Input reading" in practice is almost never reading the controller. It is reacting to the opponent's move state with zero delay. The fix is the same either way: a reaction delay measured in frames, and a chance to fail.
- The four dials (reaction frames, follow through chance, defence accuracy, option set) all work as integers and a seeded roll. They fit a deterministic sim with no change.
- For Kindred Siege the reaction delay maps straight onto animation: at 20 fps art, a 12 to 19 frame (60 fps) human reaction is about 4 to 6 sprite frames. A fighter that needs 4 to 6 sprite frames of the enemy's wind up before it may answer will look like it saw the move, and a fighter that answers on frame 1 will look like a cheat.
- A unit stat can be the dial. A veteran reads in 4 frames, a recruit in 7, and a recruit sometimes does not follow through. That shows skill in the animation instead of in a number.
- "Ping pong" is what the macro model gives when each side has one reactive rule and no memory. The games above escape it only by adding option sets and follow through rolls, which is thin. The richer answers are in sections 2 to 4.

### Gaps
- No developer source found for how Street Fighter, Tekken, Mortal Kombat or Guilty Gear arcade CPUs choose moves. The claims about them above are press and player descriptions.
- I did not find exact per level reaction frame tables for Smash Ultimate.
- I did not open the two arXiv papers for the delay numbers. The figures come from search summaries.

## 2. Learning AIs: Killer Instinct's Shadow AI (2014) built from player data, Tekken's Ghost, Virtua Fighter 4's AI Kumite, Forza Drivatars as a parallel. How did they model players (n grams, case based reasoning, machine learning)?

### Takeaway
Killer Instinct's Shadows are the best documented: case based reasoning. Record everything a player does with the situation it happened in, parse it into named patterns, then at run time find the nearest stored situation and replay what the player did there. Tekken's Ghost and Virtua Fighter 4's Kumite have no public technical description that I could find. Drivatar began as a Bayesian neural network at Microsoft Research.

### Cited Findings
Killer Instinct Shadow AI (developer talks by Bruce Hayles and Derek Neal of Iron Galaxy, reported by AI and Games):
- The technique is case based reasoning. Hayles gave a nucl.ai 2015 talk titled "Case-based Reasoning for Player Behaviour Cloning in Killer Instinct", and Neal and Hayles presented at GDC 2016 ("Designing AI for Competitive Games"). (search summary) [GDC Vault](https://gdcvault.com/play/1022992/Designing-AI-for-Competitive), [Game Developer video note](https://gamedeveloper.com/design/video-designing-competitive-game-ai-the-i-killer-instinct-i-way)
- Every match is recorded in full: movement, jumps, blocks, counters, combos. Standard attacks are logged at fixed durations, movement and combos as continuous streams. Combos are stored down to the animation frame count. (read) [Game Developer, The Killer Groove](https://gamedeveloper.com/programming/the-killer-groove-the-shadow-ai-of-killer-instinct)
- Match state is stored with each action: health, shadow meter, distance, time left. Distance and health are stored relative to the opponent, not as absolute values, so one case applies in many situations. (read) [same](https://gamedeveloper.com/programming/the-killer-groove-the-shadow-ai-of-killer-instinct)
- A parser turns low level actions into patterns, usually 400 to 700 per match. Pattern types include special move setups, zoning, traps, combos, combo breakers, counters, shadow counters and taunts. (read) [same](https://gamedeveloper.com/programming/the-killer-groove-the-shadow-ai-of-killer-instinct)
- Three matches give a Shadow base competence. Up to 40 matches are kept per Shadow per opposing character, so up to about 28,000 patterns per matchup. (read) [same](https://gamedeveloper.com/programming/the-killer-groove-the-shadow-ai-of-killer-instinct)
- Retrieval is nearest neighbour matching over more than 40 metrics, such as whether each fighter is grounded or airborne, whether the player is defensive or set to anti air, and character specific state (for example where Maya's thrown knives are). The preceding action sequence is also part of the match, such as the opponent's setup or the current combo stage. (read) [same](https://gamedeveloper.com/programming/the-killer-groove-the-shadow-ai-of-killer-instinct)
- Matched actions are ranked by perceived value. With a small probability the Shadow picks a lower ranked action, to bait the opponent. (read) [same](https://gamedeveloper.com/programming/the-killer-groove-the-shadow-ai-of-killer-instinct)
- Breakers and reversals are timed from the recorded animation frame counts. Without that they "would almost never work". (read) [same](https://gamedeveloper.com/programming/the-killer-groove-the-shadow-ai-of-killer-instinct)
- If a combo's conditions fail (for example the opponent blocks), an overlapping valid pattern takes over as the fallback. (read) [same](https://gamedeveloper.com/programming/the-killer-groove-the-shadow-ai-of-killer-instinct)
- The system follows the four case based reasoning steps: retrieve, reuse, revise, retain. Revise tracks how actions do during the match. If the Shadow keeps getting punished for one action it shifts to another strategy mid match. (read) [same](https://gamedeveloper.com/programming/the-killer-groove-the-shadow-ai-of-killer-instinct)
- Stated limits: the retrieval metrics need a designer with deep competitive knowledge of the game, a Shadow must see a behaviour several times before it enters the model, and Hayles accepts that 40 matches can still miss behaviours. (read) [same](https://gamedeveloper.com/programming/the-killer-groove-the-shadow-ai-of-killer-instinct)

Tekken 8 Super Ghost Battle:
- Bandai Namco's statement: the more a player fights, the more accurate their Ghost becomes. Guides say a Ghost needs at least 3 fights against the CPU, tracks how you move, which attacks you use and how, also copies your mistakes, and takes time to unlearn them. No technical method is given. (search summary) [Push Square](https://www.pushsquare.com/news/2023/10/tekken-8s-upcoming-beta-will-boast-impressive-ai-ghost-battles), [GGRecon guide](https://www.ggrecon.com/guides/tekken-8-super-ghost-battle/), [esports.gg guide](https://esports.gg/news/tekken-8/tekken-8-super-ghost-battle/)

Virtua Fighter 4 AI training and Kumite:
- Community reports only. A fan training guide says the AI learns in three layers: single moves, then strings, floats and multi part moves, then simple flowcharts and strategy. Players report it learns blocking and getting up on its own with fight count, and mostly learns from sparring with its owner. (search summary) [virtuafighter.com thread](https://host.virtuafighter.com/threads/vf4-evo-ai-training.14691), [virtuafighter.com thread](https://virtuafighter.com/threads/a-i.3298)
- The Killer Instinct article classes Virtua Fighter 4's trained AI and Smash amiibo as still using the parameter template, where learning adjusts dials, as opposed to full behaviour cloning. Secondhand claim. (read) [Game Developer, The Killer Groove](https://gamedeveloper.com/programming/the-killer-groove-the-shadow-ai-of-killer-instinct)

Smash amiibo (Figure Players):
- An amiibo gains experience up to level 50 and adapts to repeat opponents. Example: if the opponent shields often, it shields more itself and answers with grabs. Community wiki. (read) [SmashWiki, AI](https://www.ssbwiki.com/AI)
- A forum claim says amiibo pick moves by a changing priority per move, starting from random values, not from a fixed list. Unverified. (search summary) [Smashboards](https://smashboards.com/threads/training-your-amiibo-don%E2%80%99t-waste-your-time.386544)

Forza Drivatar:
- The first Drivatar was developed at Microsoft Research Cambridge. Creative director Dan Greenawalt described it as a Bayesian neural network that recorded racing lines and the characteristics of how someone drove. (search summary) [Dexerto](https://www.dexerto.com/racing/forza-developers-reveal-how-they-make-super-realistic-ai-drivers-1419779/), [Game Developer, How Forza's Drivatar actually works](https://gamedeveloper.com/design/how-forza-s-drivatar-actually-works)
- When learning moved to servers, the flood of player data produced "weird inaccuracies" and the developers had to classify and filter what Drivatars learned from. (search summary) [Dexerto](https://www.dexerto.com/racing/forza-developers-reveal-how-they-make-super-realistic-ai-drivers-1419779/)

N grams:
- N gram prediction (count which action followed the last N actions, predict the most common) is taught as a standard game AI method for guessing a player's next move, with the fighting game as the stock example. I found a university lecture and a student Unity demo, not a shipped fighting game that names it. (search summary) [Georgia Tech lecture, Prediction: N gram and CBR](https://faculty.cc.gatech.edu/~surban6/2016-cs4731/lectures/2016_07_19-Prediction_NgramAndCBR.pdf), [student project](https://sites.google.com/view/thompsonben/other-projects/n-gram-research-project)

### Inferences
- Case based reasoning is a table lookup. Every piece (integer features, relative distance bands, nearest neighbour by an integer distance sum, ranked choice with a seeded "pick second best" roll) is deterministic. It needs no floats and no training. It is the learning method that fits Kindred Siege's sim rules best.
- The useful twist for an auto battler: the "player" being cloned can be authored. A designer writes the case table per class or per tribe (situation in, pattern out), which gives each fighter a style the way a Shadow has its owner's style. Kindred Siege already has "learning tribes" in its locks, so the same table could also be filled from what the player's squad did in past fights.
- The "revise" step is the cheapest form of reading the opponent: keep a small integer score per pattern for this fight, lower it each time the pattern gets punished, and stop using it when it drops. That alone breaks ping pong, because the second exchange cannot be the same as the first if the first one failed.
- The n gram is the cheapest form of predicting the opponent: a small integer count table of "after A then B, he did C". With ordered lists it is deterministic. A fighter who has seen the enemy open with the same hit twice can parry the third. That is a decision the animation can show (he waits, then parries early).
- Storing relative state (distance band, who has more health, whose turn it was) instead of absolute state is what makes a small table cover many fights. Worth copying.
- Drivatar's lesson: learned behaviour from real players includes junk. If tribes learn from the player, filter what they are allowed to learn.

### Gaps
- No technical source found for Tekken's Ghost (Tekken 5, 6 or 8). Guides describe what it does, not how. Harada's role was not confirmed by any source I found.
- No developer source found for Virtua Fighter 4's AI system. Everything is fan reports.
- I did not open the GDC Vault talk itself. The Killer Instinct detail comes from the AI and Games write up of the talks.
- The Microsoft Research Drivatar theory page was not opened.

## 3. Academic fighting game AI: the FightingICE competition, Monte Carlo tree search fighters, opponent modeling papers, rolling horizon evolution.

### Takeaway
The research line is lookahead with a forward model: simulate a few actions ahead for both fighters, score by health difference, do the first action of the best line. Monte Carlo tree search led the FightingICE competition from 2016, and rolling horizon evolution with a small learned opponent model matched or beat it by 2019 to 2020. A one layer opponent model beat deeper ones.

### Cited Findings
- FightingICE is a 2D fighting game built by the Ritsumeikan University lab for AI research. It gives bots 56 possible actions per character, a 16.67 ms decision budget per frame, and a 15 frame delay on what the bot observes, meant to simulate human reaction time. Rounds are 60 seconds, 400 HP each. (read) [Tang et al., arXiv 2003.13949](https://ar5iv.labs.arxiv.org/html/2003.13949)
- History of winners per that paper: rule based controllers picked by UCB, and dynamic scripting, won early (dynamic scripting won 2015). These are limited by fixed cases and can be exploited. Monte Carlo tree search bots have led since 2016 because the built in simulator makes search workable. Deep reinforcement learning bots of that period were still beaten by MCTS bots, including 2017 champion GigaThunder. (read) [Tang et al.](https://ar5iv.labs.arxiv.org/html/2003.13949)
- The first MCTS paper on the platform (Ishihara, Miyazaki, Chu, Harada, Thawonmas, 2016) picks the next action through stochastic simulations and expands all of the AI's available actions at once instead of one node at a time. From slides, paper not opened. (search summary) [slides](https://pt.slideshare.net/ftgaic/application-of-monte-carlo-tree-search-in-a-fighting-game-ai-gcce2016)
- Rolling horizon evolution as used by Tang, Zhu, Zhao and Lucas: the genome is a sequence of 4 actions. Population is 7 with 1 elite kept. Uniform crossover, mutation probability 0.85 replacing one gene. Fitness mixes a score (health difference, or plus or minus 1 for win or loss) with a diversity term. Each frame it runs within the budget, then plays the first action of the best sequence. (read) [Tang et al.](https://ar5iv.labs.arxiv.org/html/2003.13949)
- Their opponent model: 18 inputs (both fighters' HP and energy, positions, one hot state of Stand, Crouch, Air or Down for each, relative distance) into 56 outputs, one per action, with a single linear layer and no hidden layer. Deeper networks and LSTM versions did worse. During the lookahead it predicts the opponent's action at each step. (read) [Tang et al.](https://ar5iv.labs.arxiv.org/html/2003.13949)
- The opponent model is trained between rounds on that round's recorded opponent state and action pairs, then the data is cleared. It starts from scratch, so the gain shows from round two on. (read) [Tang et al.](https://ar5iv.labs.arxiv.org/html/2003.13949)
- Results: plain rolling horizon beat all five 2018 bots with mean win rates of about 74 to 85 percent. With the policy gradient opponent model it reached about 87 to 93 percent. It placed 2nd in the 2019 competition (122 points against 133 for ReiwaThunder). (read) [Tang et al.](https://ar5iv.labs.arxiv.org/html/2003.13949)
- An enhanced version won the 2020 competition. (search summary) [same paper listing](https://arxiv.org/abs/2003.13949v1)
- An earlier MCTS bot with a simple action table as its opponent model did not improve significantly, per Tang et al. (read) [Tang et al.](https://ar5iv.labs.arxiv.org/html/2003.13949)
- A separate study (simple RTS, not a fighting game) found rolling horizon evolution is much more sensitive to opponent model accuracy than MCTS, and that with a low budget and an unknown opponent, no model may be better. (search summary) [arXiv 2006.08659 summary](https://fugumt.com/fugumt/paper_check/2006.08659v1_enmode)
- A 2019 IEEE CIG paper tunes MCTS toward "highlight" moments to make the fight more entertaining to watch, and a user study found its fights more entertaining than a typical MCTS bot's. Details not opened. (search summary) [DOI 10.1109/CIG.2019.8848069](https://unpaywall.org/10.1109%2FCIG.2019.8848069)
- Blade and Soul pro level AI (NCSoft): deep reinforcement learning with a self play curriculum. Three agent styles were made through reward shaping and trained against each other. The agents won 62 percent against five professional players. (read, abstract only) [arXiv 1904.03821](https://arxiv.org/abs/1904.03821)
- Ubisoft La Forge trained For Honor player bots with deep reinforcement learning and gave the agent a bonus reward for defending more, to shape its style. Presented at CEDEC by Joshua Romoff. (search summary) [80.lv](https://80.lv/articles/ubisoft-on-building-efficient-player-bots-using-deep-reinforcement-learning)

### Inferences
- A deterministic integer sim is the ideal forward model. Kindred Siege can copy its own fight state, step both fighters forward a few moves, and score the result exactly. Research bots only approximate this, the sim gets it for free.
- For a one on one exchange the search can be tiny and exhaustive. With, say, 5 options each and 3 moves ahead, that is small enough to score every line with integers, with no random sampling at all. Same input, same answer, so replays match. If sampling is used, it must draw from `DeterministicRng` and run a fixed number of iterations, never a time budget (a time budget is what makes the research bots non deterministic).
- This is what "frame data reasoning" looks like in code: the lookahead finds "if I block this, I recover 3 frames before he does, so my fast hit lands". The fighter did not have a rule for punishing. It found the punish. That is the iceberg under a visible blocked hit then counter.
- The opponent model is the mixup engine. Lookahead needs a guess of what the other fighter does. If the guess is "his most common answer so far" (an integer count table), then a fighter who varies beats a fighter who repeats, and both will be seen adapting inside one fight. A count table is deterministic. A neural net with floats is not safe for the sim.
- The finding that a one layer model beat deep ones, and that models learned inside a single match start weak, argues for the simplest table and for seeding it with a class default (what a Warden usually does) so the first exchange is already sensible.
- The "highlight" MCTS work matters for an auto battler: the score function can pay for watchable outcomes (a clash, a narrow parry, a finisher) and not only for health difference. The spectator is the customer.
- Reward shaping for style (defensive bonus, aggressive bonus) translates to per class or per tribe weights in the lookahead's score. Same search, different temperament. This is where a utility scorer still lives, but as the leaf score of a search, not as the whole brain.
- Full deep reinforcement learning is a poor fit: float networks, training cost, and behaviour nobody can explain or tune by hand.

### Gaps
- I did not open the original 2016 MCTS paper, the 2019 highlight paper or the For Honor reinforcement learning talk. Their details above are from summaries.
- I found no published n gram opponent modelling paper specific to FightingICE in this pass. One may exist.
- No source found on the cost of exhaustive short lookahead in a five against five brawl. The sizing above is my own estimate, not checked.

## 4. Duel AI in action games: For Honor bots, Sekiro and Elden Ring enemies (input reading, combo strings, punish windows), Absolver, Sifu, Mount and Blade melee.

### Takeaway
FromSoftware's enemies run on a very plain system: a stack of goals, a weighted random pick of the next attack by distance band with cooldowns, combo strings pushed as a queue of attack steps, and "interrupt" events that roll a chance to drop everything and punish what the player just did. Mount and Blade's fighters look at the opponent's guard direction and attack somewhere else. I found no developer source for For Honor's shipped bots, Sifu or Absolver.

### Cited Findings
Elden Ring and FromSoftware (community reverse engineering, not developer confirmed):
- The AI is written in Havok Script (a Lua variant). The article is based on scripts that others decompiled, and its code samples are the author's own illustration. (read) [nega.tv, The low tech AI of Elden Ring](https://nega.tv/posts/low-tech-ai-of-elden-ring.html)
- Each actor has a stack of goals. Each frame the top goal updates and can push sub goals. A goal returns continue, success or failure. Each goal has a lifetime in seconds that acts as a stuck timeout. (read) [same](https://nega.tv/posts/low-tech-ai-of-elden-ring.html)
- When sub goals run out, the battle goal makes a weighted random choice among the enemy's attacks. The weights change with distance to the target. The author's illustrative table: far away favours the leap attack (65) and the ranged attack (15), mid range favours the light combo (60) and heavy combo (35), close range adds the ground slam (20). Numbers are illustrative, not real values. (read) [same](https://nega.tv/posts/low-tech-ai-of-elden-ring.html)
- Cooldowns zero a move's weight for some seconds after use, and HP thresholds also change weights. (read) [same](https://nega.tv/posts/low-tech-ai-of-elden-ring.html)
- A combo is a queue: approach if too far, then attack step "initial", then "repeat", an extra "repeat" on a roll under 0.2, then "finisher". So string length varies by a small roll. (read) [same](https://nega.tv/posts/low-tech-ai-of-elden-ring.html)
- Interrupts are events that bubble up the goal stack: the player used an item (roll under 0.5: clear goals and run the light combo), the enemy took damage at close range (roll under 0.8: ground slam). The Bell Bearing Hunter reacts to spell casting or item use with an 85 percent chance to abort and attack. Watch regions (for example behind or under a boss) also fire interrupts. (read) [same](https://nega.tv/posts/low-tech-ai-of-elden-ring.html)
- Each actor has its own random number stream for weighted picks and interrupt rolls. (read) [same](https://nega.tv/posts/low-tech-ai-of-elden-ring.html)
- Attacks are animation driven. Animation events carry hitboxes, timings and effects, and combo events choose different event sets so attacks chain faster. (read) [same](https://nega.tv/posts/low-tech-ai-of-elden-ring.html)
- Dataminer Zullie the Witch showed that enemies punish healing by reacting to the player's animation, not the button press, and that a boss will dodge a spell even when it is aimed away. Zullie notes some conjecture. Press report of community work, page body not readable. (search summary) [PC Gamer](https://www.pcgamer.com/elden-ring-dataminer-explains-why-enemies-smack-you-when-you-reach-for-a-potion), [GamesRadar](https://gamesradar.com/heres-why-elden-ring-enemies-stab-you-every-time-you-try-to-heal)
- Sekiro: players report enemies react to character actions, not buttons, and can cancel their own attacks to block or deflect. Forum claims only. (search summary) [Steam discussion](https://steamcommunity.com/app/814380/discussions/0/1651043320660726155)

Mount and Blade II Bannerlord (developer blog, via coverage):
- After the 2020 melee AI overhaul, an AI fighter considers its opponent's blocking direction when it chooses its own attack direction, at every skill level. If it sees the target blocking the right way it switches direction, unless the target holds a shield. Higher melee skill gives more intense bot fights. (search summary) [GameBanshee copy of TaleWorlds dev blog](https://gamebanshee.com/3ua66), [Altchar](https://www.altchar.com/game-news/mount-blade-2-bannerlord-dev-shows-off-ai-melee-combat-overhaul-aFJSl3w4fuLj)
- Player complaint (2024): the AI changes its block direction as if it had eyes in the back of its head. One user's opinion. (search summary) [Steam discussion](https://steamcommunity.com/app/261550/discussions/0/4852154959747347068/?ctp=3)

For Honor:
- Ubisoft's GDC 2017 talk "Deterministic vs. Replicated AI: Building the Battlefield of For Honor" (Frederic Doll, Xavier Guilbeault) covers running the game's AI on a peer to peer deterministic simulation. It is about the battlefield architecture. I did not open it and cannot say what it reveals about duel decisions. (search summary) [GDC Vault](https://gdcvault.com/play/1024035/contactUs), [Game Developer](https://gamedeveloper.com/design/video-deterministic-vs-replicated-ai-building-i-for-honor-i-s-battlefield)
- Player reports: mind games do not work on the bots, level 2 bots never parry, and a bot falls for a feint only about the third time. Anecdotes. (search summary) [Steam discussion](https://steamcommunity.com/app/304390/discussions/0/1777136225035620309/?l=koreana)

Sifu and Absolver (Sloclap):
- A design analysis notes weak enemies have their attack strings interrupted when the player dodges them, and describes enemy aggression as controlled so the fight is about dodge and riposte. A review says enemies are just predictable enough to learn. Analyst and press opinion, not developer. (search summary) [Steventus, Deep Design Dive: Sifu](https://steventus.substack.com/p/deep-design-dive-sifu), [PlayStation Blog, How Sifu's kung fu combat works](https://blog.playstation.com/2021/11/18/how-sifus-kung-fu-combat-works/)

### Inferences
- The FromSoftware model is deterministic friendly as is: integer weights, integer distance bands, cooldown counters in ticks, a seeded stream per actor. It is also exactly "a plain scorer with a random pick", which is the thing the owner called basic. Its depth comes from three things layered on top: authored strings with a variable length, interrupts that punish specific enemy actions, and move sets that change by phase.
- The interrupt is the piece Kindred Siege's ping pong lacked. Ping pong means each fighter only decides when its own move ends. Interrupts let a fighter decide during the other fighter's move: "he started a slow heavy, 80 percent I cut my string and sidestep". Every interrupt is visible by nature, because it cancels one animation into another.
- Variable string length (the extra repeat on a small roll) is the simplest mixup there is. The defender cannot know if the string is 2 or 3 hits, so the punish window is a guess. Two fighters both using it will naturally produce early punishes that get hit, and late punishes that miss the window. That is a read, shown in the animation.
- Mount and Blade's guard reading maps directly onto Kindred Siege's hit kinds meeting a stopping stat: the attacker looks at which stop the defender has ready (dodge, parry, block) and picks the hit kind or timing that stop handles worst. Limiting how fast a fighter may switch its ready stop gives the attacker something to exploit and avoids the "eyes in the back of his head" complaint.
- The For Honor anecdote (falls for a feint about the third time) suggests a counter based design: the bot tracks how many feints it has seen and changes its response. Whether intended or not, a small integer "times fooled" counter per fighter is a cheap, visible adaptation.
- Goal stack plus queue of attack steps is a good fit for sprite clips: one queued step equals one clip. The plan the fighter holds is the list of clips it intends to play, and the drama is when that list gets cut.

### Gaps
- No developer source on how For Honor's shipped bots choose parries, feints and guard switches.
- No developer source on Sifu or Absolver enemy AI. "Attack tokens" in Sifu could not be confirmed.
- No developer source on Sekiro's AI. The Elden Ring detail is community reverse engineering with illustrative numbers.
- The PC Gamer and GamesRadar pages on Zullie's finding would not load, so the animation versus input claim rests on search summaries.
- I did not open the TaleWorlds blog itself, only coverage of it.

## 5. How do they keep AI beatable, fair and readable (telegraphs, no input reading complaints)?

### Takeaway
Every source points at the same few tools: a real reaction delay, a chance to fail, a visible wind up before anything dangerous, punishing the opponent's visible action (never its hidden intent), dropping a tactic that keeps failing, and now and then picking a worse option on purpose. Perfect defence is what players call cheating, and predictable habits are what they call dumb.

### Cited Findings
- FightingICE forces a 15 frame observation delay on every bot to simulate human reaction time. Fairness is built into the platform, not left to each bot. (read) [Tang et al., arXiv 2003.13949](https://ar5iv.labs.arxiv.org/html/2003.13949)
- The Blade and Soul pro level AI was given about 230 ms of delay, matched to professional players' average reaction, so it would not have an unfair edge. (search summary) [arXiv 1904.03821](https://arxiv.org/pdf/1904.03821)
- Killer Instinct's Shadow sometimes picks a lower ranked action on purpose, to bait. It changes strategy mid match when one action keeps getting punished. And it only reproduces what a human actually did in a similar spot, so its mistakes are human mistakes. (read) [Game Developer, The Killer Groove](https://gamedeveloper.com/programming/the-killer-groove-the-shadow-ai-of-killer-instinct)
- Parameter AI at high settings gives near perfect blocks and counters, which is the source of the classic unfair boss complaint. (read) [Game Developer, The Killer Groove](https://gamedeveloper.com/programming/the-killer-groove-the-shadow-ai-of-killer-instinct)
- Smash level 9's one frame defence and frame perfect air dodges are listed by the community as flaws, alongside its lack of mind games and its exploitable habits (falling for the same counter). Both too perfect and too predictable get criticised in the same AI. (read) [SmashWiki, AI](https://www.ssbwiki.com/AI)
- Elden Ring's punish reactions are probability rolls (for example 50 percent on item use, 85 percent for the Bell Bearing Hunter), not certainties, and they key off the player's visible action. Community reverse engineering. (read) [nega.tv](https://nega.tv/posts/low-tech-ai-of-elden-ring.html)
- Elden Ring's enemies dodging spells that were aimed away from them was shared as a funny flaw: the reaction keyed on the cast animation, not on real danger. (search summary) [PC Gamer](https://www.pcgamer.com/elden-ring-dataminer-explains-why-enemies-smack-you-when-you-reach-for-a-potion)
- One player description of Sekiro: every boss attack is telegraphed and has an input the player can make in answer. Forum opinion. (search summary) [Reddit mirror](https://lr.in.psf.lt/r/Games/comments/1dotcok/elden_ring_calibration_update_1122/lae866e/?context=3)
- Mount and Blade's AI reading guard direction drew a complaint that it blocks as if it can see behind itself. Awareness with no limits reads as cheating even when the rule is sensible. (search summary) [Steam discussion](https://steamcommunity.com/app/261550/discussions/0/4852154959747347068/?ctp=3)
- The MCTS highlight work found fights from a bot tuned for highlight moments were rated more entertaining than fights from a bot tuned only to win. (search summary) [DOI 10.1109/CIG.2019.8848069](https://unpaywall.org/10.1109%2FCIG.2019.8848069)

### Inferences
- In an auto battler both sides are AI, so "fair" changes meaning. Nobody is being cheated. The viewer must be able to see why each fighter did what it did. The same tools still apply, because a reaction with no delay cannot be read by a viewer either.
- Rule for readable decisions, drawn from all five sections: a fighter may only react to something that has been on screen for its reaction time, and every decision must change what clip plays. If a decision does not change the clip, it is under the waterline and the viewer gets nothing from it. The iceberg should show its tip each time.
- Perception limits make reads visible. Give each fighter a facing and a reaction delay in ticks, and only let it react to threats in front of it after the delay. Flanks then matter and the "eyes in the back" complaint cannot happen.
- React to real danger, not to the tag. The Elden Ring spell dodge flaw says: check that the incoming hit would actually reach this fighter before spending a dodge. The sim can check that exactly.
- Put together, the mechanisms that fit a deterministic integer sim, in order of cost:
  1. Reaction delay and follow through as stats (section 1). Cheap. Shows skill.
  2. Goal stack with queued strings of variable length, and interrupts with a seeded chance (section 4). Kills ping pong, because fighters now decide in the middle of each other's moves.
  3. A per fight memory: punished counts per own move, and an n gram count table of the enemy's habits (section 2). Gives visible adaptation inside one fight.
  4. A case table per class or tribe (section 2). Gives style, and is where learning tribes could store what they copied.
  5. Short exact lookahead on the sim itself, with the memory from 3 as the opponent model and a class flavoured score (section 3). Gives punishes, baits and frame traps that nobody scripted. A utility score survives here only as the leaf score.
- Baiting on purpose (Killer Instinct's lower ranked pick) is a good use of the seeded roll: it is the one place where randomness reads as intent, as long as the animation shows the bait (a short step back, a held guard).
- Each layer can be tested on its own with the mechanic ablation approach: switch one layer off, run the same seeds, and see if exchange length and variety change.

### Gaps
- No controlled source found on what viewers of AI against AI fights find readable, apart from the one MCTS highlight study, which I did not open.
- No developer statement found from Capcom, Bandai Namco, NetherRealm or Arc System Works on how they keep their CPUs fair.
- The ordering and sizing of the five layers is my inference. None of it has been built or measured in Kindred Siege.
