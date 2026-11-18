# How 2D and pixel art auto battlers and watched battle games make fights feel weighty and readable at small sprite sizes

Scope note: six target traits are (1) weight scaled to power, (2) hits that travel and land on a body, (3) every unit readable at a glance, (4) every hit visibly changes the state, (5) rare big moments, (6) the battle as the build paying off. Target game: Kindred Siege, 2.5D pixel art 5 v 5 auto battler, 128 px AI generated sprites, units fight alone, a leader parries big hits.

Source labels used below: DEV = developer's own words (devlog, patch note, interview quote, GDC talk). PRESS = journalist write up of dev words. COMMUNITY = wiki, fan analysis, forum, review. SNIPPET = seen only in a search result summary, page not opened in full. Everything under "Inferences" is my own reasoning, not sourced.

## Which 2D and pixel games do this well, and what concrete techniques do they use per trait?

### Takeaway
The strongest primary evidence is from Fire Emblem GBA (frame timing, via a community frame count), Smash Bros (Sakurai's own hit stop rules), Unicorn Overlord (devs say auto battle is why they spent on animation and made characters bigger), Hero's Hour (devlog: shadows sized to unit, projectile shadows, corpse marks, spell target highlights) and Backpack Battles (patch notes: per item activation animations plus an activation log). For Super Auto Pets, The Bazaar, Despot's Game, Dicey Dungeons and Mechabellum I found no developer writing on presentation; only mechanics from wikis.

### Cited Findings

**Unicorn Overlord (Vanillaware, auto resolving squad battles, 2D)**
- Because battles play out automatically, the team "paid particular attention to captivating animation." (DEV, Inverse interview: [Inverse](https://www.inverse.com/gaming/unicorn-overlord-interview-vanillaware)). Attribution conflict: the full page fetch attributes this to director Takafumi Noma; a search summary of the same page attributed it to producer Akiyasu Yamamoto ([Inverse](https://www.inverse.com/gaming/unicorn-overlord-interview-vanillaware)).
- Characters were enlarged in battle to "make the battles feel more powerful and realistic." (DEV, Noma: [Inverse](https://www.inverse.com/gaming/unicorn-overlord-interview-vanillaware))
- Auto combat was fixed at the planning stage so "the importance of preparing for battle" stays central; giving direct orders to every unit would be "overwhelming." (DEV, Wataru Nakanishi: [Inverse](https://www.inverse.com/gaming/unicorn-overlord-interview-vanillaware))
- The battle system came from "combining strategy and ease of understanding." (DEV, Nakanishi: [Inverse](https://www.inverse.com/gaming/unicorn-overlord-interview-vanillaware))
- Players praised the detail of the 2D battle animation; the report calls it the studio's most ambitious project. (PRESS, SNIPPET: [Automaton](https://automaton-media.com/en/news/unicorn-overlords-2d-animation-shocks-with-obsessive-attention-to-detail-and-sexiness/))
- The interview says nothing about camera or sprite technique specifically. ([Inverse](https://www.inverse.com/gaming/unicorn-overlord-interview-vanillaware))

**Fire Emblem GBA (watched battle cut ins, pixel art)**
- Battle animation frames are 248 x 160 px, one image per frame; each melee class has separate 1 range and ranged attacks, each with its own critical version, plus dodge; miss usually reuses the normal attack. (COMMUNITY, hacking guide: [FE battle animations guide](https://fe-battle-animations.neocities.org))
- Full frame analysis in the next section. Key point for trait 2 and 4: the contact pose is held at least 8 frames and the hold length depends on how long the HP bar takes to drop. (COMMUNITY analysis: [Lost Worlds blog](https://lost-worlds.neocities.org/blog/2024/10/20/fire-emblem-animation/))
- The author says the 2003 game's opening chapter rigged its numbers to stage a critical hit for the player. (COMMUNITY: [Lost Worlds blog](https://lost-worlds.neocities.org/blog/2024/10/20/fire-emblem-animation/))
- A GBA critical (Hero class) is described as throwing the shield up, letting it hover, somersaulting to grab it, ending in a light burst: the rare moment is a whole different choreography, not just a bigger number. (COMMUNITY, SNIPPET: [Fire Emblem fandom file page](https://fireemblem.fandom.com/wiki/File:FE6_Mage_Critical.gif) and related results; exact page for the Hero description not confirmed)
- A bright flash plus a distinct sound announcing an impending critical arrived later, in Radiant Dawn and from New Mystery of the Emblem on, not in the GBA games. (COMMUNITY wiki, SNIPPET: [Fire Emblem Wiki, Critical hit](https://fireemblemwiki.org/wiki/Critical_hit))

**Darkest Dungeon (2D side on, Spine animation, pose based attacks)**
- Art drawn in Photoshop and animated in Spine; style aims for "old and worn" with medieval woodcut influence, pooling blacks, flat colouring, and a touch of "cute" to make characters iconic. (DEV, Chris Bourassa: [Game Developer, Road to the IGF](https://www.gamedeveloper.com/audio/road-to-the-igf-red-hook-studios-i-darkest-dungeon-i-))
- A GameSpot piece contains the line "our pose-to-pose combat presentation needed a modern comic-book sensibility to really shine." The first person wording suggests a developer quote, but one search summary said it was the writer's; the page returned 403 so the speaker is not confirmed. (PRESS, SNIPPET: [GameSpot](https://www.gamespot.com/articles/the-gothic-sensibilities-of-darkest-dungeon/1100-6424880/))
- Players complained about the in combat camera zoom; a community mod removes the zoom on ranged hits, melee hits and skills but keeps it for critical hits. This shows the base game zooms on hits and that players accept zoom when it is rare. (COMMUNITY: [Steam discussion](https://steamcommunity.com/app/262060/discussions/4/618453594749180956/))
- Darkest Dungeon II: Bourassa described "really amazing preparatory animations in combat": clicking a skill makes the Highwayman whip his knife out and get ready; clicking a target makes him follow through. (PRESS, SNIPPET: [PC Gamer interview](https://pcgamer.com/interview-darkest-dungeon-2s-new-3d-look-monsters-the-narrator-and-its-surprising-aspirational-spirit))
- Darkest Dungeon II uses animation "as a vehicle" to express each character's attitude, fighting style and personality. (PRESS, SNIPPET: [Game Developer](https://gamedeveloper.com/design/how-a-theme-of-a-road-trip-from-hell-would-shape-darkest-dungeon-ii))

**Hero's Hour (pixel art, auto resolving mass battles)**
- Developer devlog v0.33 lists: "Big units get larger shadows"; "Added shadows to projectiles"; "Battlefield unit corpses draw a shrinking circle with their colour"; "Units in battle now use defending animation"; "Spells now highlight what units will be affected by casting them"; "Magic effect animations are a bit easier to see"; area spells got "elliptical and slightly larger areas"; unknown enemy counts show a range like "5-10" instead of "several". (DEV: [Hero's Hour v0.33 devlog](https://thingonitsown.itch.io/heros-hour/devlog/222862/combat-update-heros-hour-v033))
- v0.6.1 "tuned down number of groups for large armies"; the reason (readability or performance) is not stated. (DEV, SNIPPET: [Hero's Hour v0.6 devlog](https://thingonitsown.itch.io/heros-hour/devlog/228841/sieges-fear-update-heros-hour-v06))
- A reviewer says armies can grow so large that reading the battlefield is hard and it is easier to watch a meter to see who is winning. (COMMUNITY review, SNIPPET: [Tech Gaming review](https://tech-gaming.com/heros-hour))

**Backpack Battles (2D, items fire on timers inside a backpack grid)**
- A Steam patch "Added activation animations for Mr. Struggles, Ruby Chonk, Cauldron", added hover particles for Cauldron, and added a panel to show, minimise or hide activation entries in the combat log. (DEV patch notes, SNIPPET: [SteamDB patch notes](https://steamdb.info/patchnotes/14869193))
- Demo patch 0.3.1 added scaled damage in tooltips and fixed Spiked Shield's 1 Block gain being counted as an activation (for Goobert, which counts activations). (DEV: [Backpack Battles itch devlog 0.3.1](https://playwithfurcifer.itch.io/backpack-battles/devlog/551677/patch-031))

**The Bazaar (2D cards on a board, items fire on cooldowns)**
- Combat is automatic; items activate on their own cooldowns through the fight. (PRESS: [Game8](https://game8.co/articles/reviews/the-bazaar-gameplay-and-story-info))
- Triggers are throttled by a hidden internal cooldown; a few items bypass it. (COMMUNITY guide, SNIPPET: [Mobalytics keywords](https://mobalytics.gg/the-bazaar/guides/keywords-and-terms))
- Reynad described ongoing work to move passive items toward active ones (haste, cooldown reduction, crit). (COMMUNITY summary of a Reynad stream interview: [Bazaar Builds](https://bazaar-builds.net/reynad-interview-insights-on-the-future-of-the-game/))

**Super Auto Pets (2D, 5 v 5 lines of pets)**
- Each pet has two stats, Attack and Health; caps are 50 each (Behemoth 100); the highest attack pet acts first; teams are 5 animals. (COMMUNITY wiki: [Super Auto Pets wiki, The Basics](https://superautopets.wiki.gg/wiki/The_Basics))

**Teamfight Tactics (3D models, 2D UI and effects)**
- Riot's labs and surveys found newer players "found it very difficult to identify the in-game models based on their splash art"; small details like Aurelion Sol's star fighters lose prominence "especially when scaled down." (DEV: [TFT dev post, Splash Art Returns](https://teamfighttactics.leagueoflegends.com/en-gb/news/dev/splash-art-returns-to-teamfight-tactics/))
- Riot's TFT VFX job postings describe balancing visual appeal against gameplay clarity so effects accurately show their gameplay action or state. (DEV, SNIPPET: [Riot job posting](https://riotgames.com/en/j/6430922))
- League's VFX style guide lists clarity for gameplay as a core goal. (DEV, SNIPPET: [League VFX style guide](https://nexus.leagueoflegends.com/en-us/2017/10/dev-leagues-vfx-style-guide/))

**Mechabellum (3D, mass auto battle)**
- Matches scale from a handful of cheap units to huge battles of titans and swarms. (PRESS: [PC Gamer](https://www.pcgamer.com/i-cant-stop-playing-this-autobattling-strategy-masterpiece))

**Slay the Spire and Into the Breach (not auto battlers, but the reference for telegraphed big hits)**
- A transcript summary of a Mega Crit talk says the game only came together once enemy intent was revealed. (SECONDARY transcript summary: [Lilys notes](https://lilys.ai/notes/1046045))
- Attack intents show the exact incoming damage, so players can block exactly enough; in StS2 multi intents sit side by side rather than stacked. (COMMUNITY guide: [Untapped StS2 intent guide](https://sts2.untapped.gg/guides/how-to-read-enemy-intent))
- Analysis: "Telegraphs that lie or omit are worse than no telegraph." (COMMUNITY analysis: [Enemy intent telegraph](https://saschb2b.github.io/game-mechanics/concepts/enemy-intent-telegraph))
- Into the Breach shows the enemy's next action outright. (PRESS: [Kicktraq making of](https://kicktraq.com/articles/redirect/35147)); the GDC 2019 design postmortem exists at ([GDC Vault](https://www.gdcvault.com/play/1025772/)), content not read.

### Inferences
- Trait coverage by game (my mapping): weight scaled to power = Sakurai's damage scaled hit stop plus Hero's Hour's shadow size by unit size plus Unicorn Overlord enlarging fighters; hit lands on a body = Fire Emblem contact hold, Hero's Hour projectile shadows; readable at a glance = TFT model recognition, Super Auto Pets two numbers per pet; every hit changes state = Fire Emblem holding contact until the HP bar settles, Hero's Hour corpse circles; rare big moments = Fire Emblem critical choreography, Darkest Dungeon zoom kept for crits; build paying off = Backpack Battles activation animations and log, The Bazaar cooldown items, Reynad's push from passive to active.
- Unicorn Overlord is the closest reference to Kindred Siege's premise: auto battle, so the animation itself is the reward, and fighters are scaled up in the battle view.
- Darkest Dungeon and Darkest Dungeon II together suggest "pose, then follow through" is enough for weight without full animation, as long as the camera and sound carry the impact.

### Gaps
- No developer writing found on how Super Auto Pets, The Bazaar, Despot's Game, Dicey Dungeons or Mechabellum stage hits, show damage or show item triggers. Their store pages, Steam news and dev Discords are the next place to look.
- No primary source found for Darkest Dungeon's camera zoom rules (when it zooms, how far, how long). A quote ("We knew we wanted to get down and close to the characters and show them off a little bit") appeared in a search summary attributed to a Rock Paper Shotgun interview with Bourassa, but I did not open the original.
- Unicorn Overlord's battle camera and how it frames a 2D squad were not covered in any source I opened.

## How do 2D games show weight with few frames (anticipation, squash, impact frames, hit stop, knockback, scaling by power)?

### Takeaway
Weight in 2D comes from timing more than frame count: a held wind up, a fast smear, a long held contact, then slow secondary motion. Sakurai's rules make the freeze scale with damage, apply equally to both fighters, cap it, shrink projectile freeze, and shake the victim in a decaying, direction aware way.

### Cited Findings

**Fire Emblem GBA timing (community frame count)**
- The General's sword loop has 24 unique drawings shown over about 60 game frames; the attack from idle is timed 3-3-3-4, 1-1-1-3-10, 1-1-2, 2-8, totalling 43 displayed frames from 14 unique drawings. (COMMUNITY: [Lost Worlds blog](https://lost-worlds.neocities.org/blog/2024/10/20/fire-emblem-animation/))
- Eliwood's attack is timed 1-2-3-6-1-16; recovery to idle 1-2-1-2-1-2-1-2-1-2-2-1-2-1-3. A popular GIF with even 10 frame holds looks "floaty". (COMMUNITY: [Lost Worlds blog](https://lost-worlds.neocities.org/blog/2024/10/20/fire-emblem-animation/))
- Rule of thumb from the author: most frames on 3s, smears on 1 to 2 frames, contact held 8 or more frames, other poses 1 to 3 frames. (COMMUNITY: [Lost Worlds blog](https://lost-worlds.neocities.org/blog/2024/10/20/fire-emblem-animation/))
- Anticipation: the ready pose is held 7 extra frames (about 10 total) before the fast swing. (COMMUNITY: [Lost Worlds blog](https://lost-worlds.neocities.org/blog/2024/10/20/fire-emblem-animation/))
- Spacing: close spacing reads heavy and slow, wide spacing light and fast; the in betweens into the ready pose are closely spaced for weight. (COMMUNITY: [Lost Worlds blog](https://lost-worlds.neocities.org/blog/2024/10/20/fire-emblem-animation/))
- Secondary motion: pauldrons rise 1 px for 2 frames then sink for 8 frames on contact; cloth lags the body. (COMMUNITY: [Lost Worlds blog](https://lost-worlds.neocities.org/blog/2024/10/20/fire-emblem-animation/))
- The heavy General gets no smear on recovery because he is too heavy for one; overusing smears weakens them. (COMMUNITY: [Lost Worlds blog](https://lost-worlds.neocities.org/blog/2024/10/20/fire-emblem-animation/))
- Overshoot: the last recovery frame drops the sword 1 px below idle then snaps back; contact frames skip overshoot because in game screen shake would hide it. (COMMUNITY: [Lost Worlds blog](https://lost-worlds.neocities.org/blog/2024/10/20/fire-emblem-animation/))

**Sakurai on hit stop (Famitsu column, translated)**
- Attacker and victim freeze for the same time; more damage means longer hit stop; each attack has its own modifier (Marth's sword tip more than the edge); projectiles get less; electric attacks get more; there is a hard cap the freeze never exceeds, because of the Hammer and Final Smashes. (DEV via translation: [Source Gaming, Famitsu vol. 490](https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/))
- The attacker also vibrates slightly; hurtboxes stay put while the model vibrates; grounded characters shake side to side, airborne ones up and down; amplitude adjusts for camera distance; shake starts large and shrinks, precalculated. (DEV via translation: [Source Gaming](https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/))
- After the first flinch frame, the victim blends into the hurt pose over 4 frames while still frozen; the attacker drifts at a tiny speed during the stop and snaps back to its position after. (DEV via translation: [Source Gaming](https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/))
- He keeps hit stop shorter than he would like because in a four player game a frozen pair gives a third player a free hit. (DEV via translation: [Source Gaming](https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/))
- The column gives no frame counts for the freeze or the cap. (DEV via translation: [Source Gaming](https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/))
- His YouTube video lists eight hit stop techniques: shake the victim more; keep the hitbox still; horizontal shake on ground, vertical in air; decay the shake; control the length; interpolate into the damage pose; let the attacker keep moving slightly; scale shake by camera distance. (PRESS summary of DEV video: [NintendoWire](https://nintendowire.com/news/2022/12/12/this-week-in-sakurai-12-5-12-11-fine-tuning-hit-stop-and-cheating-the-system/))
- In another video he compares a Shoryuken hit with and without the stop (the stopped one reads stronger) and treats "Boss Stop", a freeze on a boss defeat, as a cousin of hit stop for highlighting important moments. (PRESS, SNIPPET: [Kotaku Australia](https://www.kotaku.com.au/2022/08/masahiro-sakurais-new-youtube-channel-is-a-masterclass-in-making-video-games/))

**Vlambeer screen shake**
- "The Art of Screenshake" (2013) takes a dull shooter and applies about 30 small tweaks. (COMMUNITY, SNIPPET: [Game Developer](https://gamedeveloper.com/design/vlambeer-co-founder-shares-advice-on-building-better-action-games); [Kenney list](https://kenney.nl/learn/must-see-videos-for-indie-developers))
- From a Nijman interview (not the talk): the camera is kicked back 6 px from the aim direction and the shake decays fast. (COMMUNITY write up, SNIPPET: [Infovore](https://infovore.org/?p=5275))

**Shadows and size as weight**
- Hero's Hour gives big units larger shadows and gives projectiles shadows. (DEV: [Hero's Hour v0.33](https://thingonitsown.itch.io/heros-hour/devlog/222862/combat-update-heros-hour-v033))
- Unicorn Overlord enlarged characters to make battles "feel more powerful." (DEV: [Inverse](https://www.inverse.com/gaming/unicorn-overlord-interview-vanillaware))

### Inferences
- At 60 fps, Fire Emblem's 8 frame contact hold is about 133 ms and the 10 frame ready pose about 167 ms (my arithmetic). A held wind up of that size is also a natural parry cue for Kindred Siege's leader.
- Sakurai's "projectiles get less freeze" fits trait 2: ranged hits feel weighty through travel and landing, not through a long stop.
- The cap matters for an auto battler with five fighters a side: if every heavy hit froze the whole screen at full length, five overlapping hits would stall the fight. A per pair freeze or a global cap is the likely answer (my inference, not tested).
- Fire Emblem's overshoot rule (no overshoot on contact because shake hides it) is a direct lesson for 128 px pixel art: shake must move in whole pixels, so tiny sprite detail on the contact frame is wasted.
- Shadow size scaled to unit power, as in Hero's Hour, is the cheapest weight cue for a 2.5D floor view.

### Gaps
- No source gives real hit stop frame counts from Smash or any pixel auto battler. Blog figures found in search (for example 2 to 3 frames light, 4 to 6 heavy) were from unvetted sites and disagreed, so I left them out.
- No source on squash and stretch or knockback distances in pixel auto battlers specifically.
- No primary source for Fire Emblem GBA screen shake or flash on crits; the frame analysis explicitly excludes effects.

## How do they show health and state on small units (bars, numbers, card stats under sprites, damage states)?

### Takeaway
The sourced patterns are: hold the hit until the HP bar finishes moving (Fire Emblem), show two plain numbers per unit (Super Auto Pets), colour code damage types (League's damage text), leave a mark where a unit died (Hero's Hour corpse circles), and make sure the unit itself is recognisable at board size (TFT's lab finding). Large mass battles lose readability and fall back to a single "who is winning" meter (Hero's Hour review).

### Cited Findings
- Fire Emblem GBA's contact pose is held at least 8 frames and its length depends on how long the HP bar takes to drop. (COMMUNITY: [Lost Worlds blog](https://lost-worlds.neocities.org/blog/2024/10/20/fire-emblem-animation/))
- Super Auto Pets pets carry exactly two stats, Attack and Health, with Health being what an attack removes. (COMMUNITY wiki: [Super Auto Pets wiki](https://superautopets.wiki.gg/wiki/The_Basics))
- Riot changed League's damage text appearance for physical, magic and true damage in patch 9.13 so damage type reads at a glance. Correction: an earlier search summary said this was a TFT change; the page shows it was League. (DEV quote on fan mirror: [Surrender at 20](https://www.surrenderat20.net/2019/06/618-pbe-update-arcade-caitlyn-pe-tft-up.html))
- Riot found newer TFT players could not match in game models to splash art, and fine details lose prominence when scaled down. (DEV: [TFT dev post](https://teamfighttactics.leagueoflegends.com/en-gb/news/dev/splash-art-returns-to-teamfight-tactics/))
- Hero's Hour corpses become a shrinking circle in the unit's colour; units use a defend animation; unknown enemy counts show as a numeric range. (DEV: [Hero's Hour v0.33](https://thingonitsown.itch.io/heros-hour/devlog/222862/combat-update-heros-hour-v033))
- Hero's Hour review: at large army sizes it is easier to watch a meter than read the battlefield. (COMMUNITY: [Tech Gaming](https://tech-gaming.com/heros-hour))
- Darkest Dungeon's art direction used flat colour and pooling blacks plus a touch of "cute" so characters read as iconic. (DEV: [Game Developer, Road to the IGF](https://www.gamedeveloper.com/audio/road-to-the-igf-red-hook-studios-i-darkest-dungeon-i-))
- A Mechabellum player asked for unit cards to show how many models a unit has before picking it. (COMMUNITY, SNIPPET: [Steam discussion](https://steamcommunity.com/app/669330/discussions/1/4431066216144676829)); exact thread URL not confirmed among two Steam results.

### Inferences
- For 128 px detailed AI sprites, TFT's finding is a warning: detail does not equal identity. Each class needs one big silhouette or colour cue that survives at board size.
- Fire Emblem ties trait 2 and trait 4 together in one rule: the body stays in the hit pose until the state change (bar drop) has finished. For an auto battler with several hits at once, a short "bar settles, then next blow" rhythm per pair is the equivalent (Kindred Siege's own rules 40 already holds 0.5 s per blow, per the project memory, not a sourced claim).
- Five per side is small enough to avoid Hero's Hour's mass battle problem, so per unit bars or numbers remain viable.

### Gaps
- No developer source found for how Super Auto Pets, The Bazaar or Backpack Battles lay out numbers, bars and damage popups, or for TFT health bar and star level design.
- No source on visible damage states (cracked armour, wounded idle) in pixel auto battlers.

## How do they show the build paying off (synergy activation, items triggering with a visible pulse, buff lines between units)?

### Takeaway
The sourced evidence is thin but consistent: Backpack Battles gives individual items their own activation animations and a combat log of activations; The Bazaar's items fire on visible cooldowns and Reynad pushed passive items toward active ones; Unicorn Overlord's devs built the whole auto battle around preparation being the point. Spell target highlighting (Hero's Hour) is the closest sourced "line between units" technique.

### Cited Findings
- Backpack Battles added activation animations for specific items (Mr. Struggles, Ruby Chonk, Cauldron), hover particles, and a combat log panel listing activations that players can show, minimise or hide. (DEV patch notes, SNIPPET: [SteamDB](https://steamdb.info/patchnotes/14869193))
- Backpack Battles counts activations as a stat other items read (Goobert); a fix stopped a 1 Block gain counting as one. (DEV: [itch devlog 0.3.1](https://playwithfurcifer.itch.io/backpack-battles/devlog/551677/patch-031))
- The Bazaar's items activate automatically on their own cooldowns; haste and slow change other items' timers. (PRESS: [Game8](https://game8.co/articles/reviews/the-bazaar-gameplay-and-story-info); COMMUNITY: [Mobalytics](https://mobalytics.gg/the-bazaar/guides/keywords-and-terms))
- Reynad described moving passive items to active ones (haste, cooldown, crit). (COMMUNITY summary: [Bazaar Builds](https://bazaar-builds.net/reynad-interview-insights-on-the-future-of-the-game/))
- Unicorn Overlord's auto combat was chosen so preparation stays central. (DEV: [Inverse](https://www.inverse.com/gaming/unicorn-overlord-interview-vanillaware))
- Hero's Hour highlights which units a spell will affect before it is cast. (DEV: [Hero's Hour v0.33](https://thingonitsown.itch.io/heros-hour/devlog/222862/combat-update-heros-hour-v033))
- TFT models let Riot show champion specific mechanics, and backgrounds were used to show how champions relate by origin and class. (DEV: [TFT dev post](https://teamfighttactics.leagueoflegends.com/en-gb/news/dev/splash-art-returns-to-teamfight-tactics/))

### Inferences
- Reynad's passive to active shift reads as a presentation lesson as much as a balance one: a passive bonus has no moment on screen, an active trigger does. For Kindred Siege, gear and Pal effects should each get a visible firing moment tied to the fighter who carries them.
- A Backpack Battles style activation log is cheap and helps the "did my build do that?" question after the fight; it complements, not replaces, the on screen pulse.

### Gaps
- No developer source found on The Bazaar's or Super Auto Pets' trigger pulse, buff transfer animation, or how they sequence simultaneous triggers.
- No source on TFT trait activation VFX design.

## Developer sources: postmortems, GDC talks, devlogs

### Takeaway
Usable primary material: Sakurai's Famitsu column and YouTube videos (hit stop), the Hero's Hour itch devlogs, Backpack Battles patch notes, the Unicorn Overlord Inverse interview, Riot's TFT splash art post, and Red Hook's Road to the IGF interview. Talks that exist but were not read: Subset's Into the Breach GDC 2019 postmortem, Vlambeer's Art of Screenshake, Mega Crit's talk on intents.

### Cited Findings
- Sakurai, Famitsu column vol. 490 on hit stop (translated). ([Source Gaming](https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/))
- Sakurai YouTube hit stop techniques, summarised. ([NintendoWire](https://nintendowire.com/news/2022/12/12/this-week-in-sakurai-12-5-12-11-fine-tuning-hit-stop-and-cheating-the-system/); [Kotaku Australia](https://www.kotaku.com.au/2022/08/masahiro-sakurais-new-youtube-channel-is-a-masterclass-in-making-video-games/))
- Hero's Hour combat devlog v0.33. ([itch.io](https://thingonitsown.itch.io/heros-hour/devlog/222862/combat-update-heros-hour-v033))
- Unicorn Overlord dev interview. ([Inverse](https://www.inverse.com/gaming/unicorn-overlord-interview-vanillaware))
- TFT readability lab finding. ([TFT dev post](https://teamfighttactics.leagueoflegends.com/en-gb/news/dev/splash-art-returns-to-teamfight-tactics/))
- Darkest Dungeon art pipeline. ([Game Developer](https://www.gamedeveloper.com/audio/road-to-the-igf-red-hook-studios-i-darkest-dungeon-i-))
- Into the Breach GDC 2019 design postmortem by Matthew Davis. ([GDC Vault](https://www.gdcvault.com/play/1025772/); [GDC news](https://www.gdconf.com/news/get-inside-look-making-breach-gdc-2019))
- Fire Emblem GBA frame analysis (community, not dev). ([Lost Worlds blog](https://lost-worlds.neocities.org/blog/2024/10/20/fire-emblem-animation/))

### Inferences
- The best single next read for Kindred Siege is Sakurai's hit stop video itself, since his rules (scale by damage, cap, equal freeze, decaying direction aware shake, less on projectiles) map almost one to one onto an auto battler with a parrying leader.

### Gaps
- No postmortem or GDC talk found from Super Auto Pets (Team Wood), The Bazaar (Tempo), Backpack Battles (PlayWithFurcifer), Despot's Game, Dicey Dungeons or Mechabellum on combat presentation.
- The Bazaar Q&A blog page lists only question headings, no answers. ([Bazaar shop blog](https://shop.playthebazaar.com/blogs/news/the-bazaar-q-a-with-reynad))
- The GameSpot Darkest Dungeon article returned 403, so its combat presentation quotes are unverified.
- Hearthstone, Legends of Runeterra, Legion TD 2 and Total War were out of this note's scope.
