# Hearthstone and Hearthstone Battlegrounds: how battle feel and unit play are built

Source key. DEV = Blizzard staff, official blog or official patch notes. WIKI = hearthstone.wiki.gg (community wiki). COMMUNITY = forum players, TV Tropes, fan sites. INFERENCE = my own reasoning, not a source. Where a line came only from a search result summary and the page itself was not opened, it says so.

Big caveat up front: I found no developer source (GDC talk text, blog, patch note) that gives frame counts or millisecond timings for the minion attack itself (lift, fly, slam, return). The GDC 2015 UI talk exists but its content was not readable as text. The hard numbers I did find are trigger timings (0.8 s to 0.2 s), shake thresholds (community only) and Battlegrounds timer numbers.

## 1. Weight scaled to power (attack animation, shake thresholds, impact, sound, Battlegrounds scaling for huge stats)

### Takeaway
Hearthstone sells weight with a stepped screen shake keyed to the attack value of the hitter (community reports three steps at 3, 6 and 8 attack in normal play, with dust and debris at the top step), and Battlegrounds moved those steps up to 10, 20 and 30 when its stats inflated. When Blizzard sped Battlegrounds up in January 2020, players said the heavy hero impact shrank to a "tap" and the game lost its weight, which is the clearest evidence that the impact frame, not the wind up, carries the feel.

### Cited Findings
- COMMUNITY, Battlegrounds shake ladder: a player wrote on 25 Jan 2020 that the "usual shaking effects (at 3, 6, and 8 attack instead at 10, 20, and 30 attack now)" had been moved up in Battlegrounds, and asked for a toggle. Player claim, no Blizzard reply in the thread. (source: [Blizzard forums, "Battleground animtions went down in quality"](https://us.forums.blizzard.com/en/hearthstone/t/battleground-animtions-went-down-in-quality/23086))
- COMMUNITY, normal play tiers: TV Tropes says 6 or 7 attack gives a moderate shake and 8 or more gives a heavier one "accompanied by a cloud of dust and debris", that the thresholds are raised in Mercenaries and Battlegrounds, and that shake can be turned off in settings. I only saw this in a search result summary; the page returned 403 when fetched. It agrees with the forum's 3/6/8 ladder (it simply does not mention the smallest step). (source: [TV Tropes, Hearthstone tropes S to Z](https://www.tvtropes.org/pmwiki/pmwiki.php/HearthstoneHeroesOfWarcraft/TropesSToZ))
- WIKI, shake is a setting: "Screen shake toggle option added" in Patch 18.0.0.54613, 2020-07-30; listed as "Enable Screen Shake" under Other. (source: [Options, hearthstone.wiki.gg](https://hearthstone.wiki.gg/wiki/Options))
- COMMUNITY, losing the impact: the original poster (21 Jan 2020) said faster attacks were fine but hero attacks now ended in a small "tap" instead of a heavy screen shaking impact, whatever the damage, and later: "the game used to have a weighty feel behind everything, now it feels feels meh". Another player said some animations had become very fast and others very slow (dropping a minion, the pause between attacks). Another said the smashing attacks could have stayed and only the long ones needed trimming. (source: [Blizzard forums thread 23086](https://us.forums.blizzard.com/en/hearthstone/t/battleground-animtions-went-down-in-quality/23086))
- COMMUNITY, shake as information: a developer forum commenter said the shake "communicates the amount of damage dealt"; another called Blizzard's juice "deliberate and communicative". The thread starter asked how a card game has "better juice and screenshake than many indie action games". (source: [Make Games SA, "Delicious Hearthstone"](https://makegamessa.com/discussion/comment/30280/))
- DEV, sound is built from the art, not the motion: Brian Farr (Sound Supervisor): "we have the creative freedom to create sound based on the card art." Andy Brock (Principal Sound Designer) calls this "subjective sound design", aiming at the emotional impact of the card rather than physical realism. (source: [Blizzard blog, the sound team behind Hearthstone](https://hearthstone.blizzard.com/en-us/blog/23964694/))
- DEV, two beats per spell: Brock says spells have key beats, "the cast, the hit". (source: [Blizzard blog 23964694](https://hearthstone.blizzard.com/en-us/blog/23964694/))
- DEV, layering for weight: Dan Johnson (Technical Sound Designer) built the spider Blightfang from a processed alien screech, pitched hissing and pincer sounds, "to make it sound like it coming from behind clacking pincers." (source: [Blizzard blog 23964694](https://hearthstone.blizzard.com/en-us/blog/23964694/))
- DEV, sound timed to fixed gameplay pacing: Brock says they "design the sound around that particular gameplay pacing so it feels right" (for example dragging a card into play). (source: [Blizzard blog 23964694](https://hearthstone.blizzard.com/en-us/blog/23964694/))
- WIKI, Battlegrounds mutes attack sounds: "Minions attack automatically, and because their targets are random, they do not play their attack sounds." (Most likely means the per minion attack voice line; the wiki does not say.) (source: [Battlegrounds, hearthstone.wiki.gg](https://hearthstone.wiki.gg/wiki/Battlegrounds))
- DEV, spectacle versus speed: Hadidjah Chamberlin (Lead VFX Artist): "we tend to lean in more towards the ceremony and the fantasy of the moment" and "It's always a balance of what are the mechanics here, what is the gameplay surrounding this". (source: [Shacknews interview, Demon Hunters and making the game faster](https://shacknews.com/article/117568/hearthstone-interview-demon-hunters-and-making-the-game-faster))

### Inferences
- INFERENCE: the shake ladder is keyed to the attacker's attack value, a number the player can already see, so the shake confirms a number rather than adding new information. That is why it reads as "communicative".
- INFERENCE: Battlegrounds raising the steps from 3/6/8 to 10/20/30 shows the steps must scale with the stat range of the mode. A fixed raw threshold either shakes on every hit (flat noise) or never.
- INFERENCE, for Kindred Siege small sprites: use a three step weight ladder (light, heavy, crushing) keyed to the hit's share of the target's max health or to the run's damage band, not raw damage. Step 1: sprite jolt and spark only. Step 2: a few pixels of camera shake plus a dust puff at the feet. Step 3: bigger shake, dust and debris, hit stop, and the hit is a parry candidate for the leader. The shake step then tells the player which hits matter, which is exactly what the leader needs to read.
- INFERENCE: the January 2020 complaints say the cut should come from wind ups and gaps between attacks, never from the impact. Keep the impact frame and its shake; trim travel and pauses.
- INFERENCE: give a shake toggle from day one (Hearthstone added one in 2020).

### Gaps
- No developer source found for the attack motion itself (how high the card lifts, flight time, slam frames, return time). The GDC 2015 talk "Hearthstone: How to Create an Immersive User Interface" by Derek Sakamoto is on [GDC Vault](https://www.gdcvault.com/play/1022036/) but its content was not available as text. A German write up gives its principle as "Immersion vor Nützlichkeit" (immersion over usefulness), seen only in a search summary ([mein-mmo](https://mein-mmo.de/hearthstone-weg-zum-megahit063/)). Watching the talk is the best next step.
- No developer confirmation of the 3/6/8 or 10/20/30 thresholds, or of any later Battlegrounds change to them.
- No source on whether the impact sound changes with attack value.

## 2. Hits that travel and land on a body (path, impact, return, divine shield, deathrattle)

### Takeaway
In Hearthstone one attack is one exchange: attacker and defender hit each other at the same moment, triggers resolve in a fixed order, and deaths are held until all attack triggers finish. Blizzard cut each passive trigger's display time from 0.8 s to 0.2 s, and Battlegrounds was the test bed for that cut.

### Cited Findings
- WIKI, the exchange is simultaneous: "Attacker and defender deal damage simultaneously." Attacker damage triggers resolve before defender damage triggers. (source: [Combat, hearthstone.wiki.gg](https://hearthstone.wiki.gg/wiki/Combat))
- WIKI, three phases: preparation (proposed attack triggers, then other pre attack triggers, deaths from those, attack cancelled if either side left play), combat (simultaneous damage), after combat (final attack triggers, then combat deaths are handled only after these finish). (source: [Combat, hearthstone.wiki.gg](https://hearthstone.wiki.gg/wiki/Combat))
- WIKI, constructed targeting visuals: a red arrow "visible to both players" runs from attacker to pointer; a bright green frame marks characters able to attack; exhausted minions float "zzz". (source: [Attack, hearthstone.wiki.gg](https://hearthstone.wiki.gg/wiki/Attack))
- DEV, trigger timing cut: triggers ("Whenever" and "After" effects, plus Lifesteal and Poisonous) went from 0.8 s to 0.2 s, a 75 percent cut; some special cases were flagged to resolve at 0 s. Shared by Lead VFX Artist Hadidjah Chamberlin on Reddit, reported 1 Feb 2020. (source: [Hearthstone Top Decks](https://www.hearthstonetopdecks.com/minion-animations-triggers-will-be-sped-up-significantly-in-regular-game-too/))
- DEV, Battlegrounds first: the patch before had heavily sped up Battlegrounds animations, "especially Murloc triggers with Divine Shield and Poisonous", which had each taken nearly a second. (source: [Hearthstone Top Decks](https://www.hearthstonetopdecks.com/minion-animations-triggers-will-be-sped-up-significantly-in-regular-game-too/))
- DEV, Chamberlin: "That's where things like trigger bubbles speed up. It happens a lot." On Shudderwock: "that was one of those things that was causing these constant three-quarter second breaks". (source: [Shacknews](https://shacknews.com/article/117568/hearthstone-interview-demon-hunters-and-making-the-game-faster))
- WIKI, divine shield: when a shielded character takes damage "that damage is reduced to 0 and any Divine Shield effect is removed"; players call it "popping" a "bubble". The wiki does not describe the pop's look or sound. (source: [Divine Shield, hearthstone.wiki.gg](https://hearthstone.wiki.gg/wiki/Divine_Shield))
- WIKI, deathrattle timing: deathrattles resolve after combat damage and after destroyed minions leave the board; "all effects triggered from the death of a minion will play out in the order in which they were put into play"; several deaths resolve one at a time in entry order. (source: [Deathrattle, hearthstone.wiki.gg](https://hearthstone.wiki.gg/wiki/Deathrattle))
- WIKI, Battlegrounds hero hit: damage to the losing hero is shown by "Strikes", "the animation that plays when the player deals damage to the enemy hero", with two stages depending on how much damage is dealt. (source: [Battlegrounds, hearthstone.wiki.gg](https://hearthstone.wiki.gg/wiki/Battlegrounds))

### Inferences
- INFERENCE: simultaneous damage means one impact frame shows two results (both numbers change at once). For 5 v 5 sprites, show both reactions on the same frame: the hitter recoils, the target flinches, both numbers tick.
- INFERENCE: holding deaths until after the swing lets the hit land fully first, then the death plays as its own beat. Kindred Siege can queue deaths at the end of a blow and play them one at a time in a fixed order.
- INFERENCE: 0.2 s per passive trigger is a practical budget when many triggers stack in an automatic fight; 0.8 s to 1 s per trigger was judged too slow by Blizzard itself.
- INFERENCE: a shield pop is a binary state change (all damage gone, shield gone) and wants a single clear burst, not a health tick. On small sprites: a one colour ring around the unit that breaks into 4 to 6 shards on the impact frame.
- INFERENCE: the two stage Strike is a weight ladder on the last hit of the fight; small payoff and big payoff.

### Gaps
- No source found for the exact attack path (straight line or arc), the travel time, or the return animation.
- No source found describing the divine shield pop visual (memory, not checked: a golden bubble that shatters) or the deathrattle trigger visual.
- Strike stage threshold (how much damage switches stage 1 to stage 2): the wiki "Strike" and "Strikes" pages returned 404.

## 3. Readability (numbers, splats, colours, keyword icons, taunt frames)

### Takeaway
Hearthstone keeps state on the unit itself: health on a red blood drop at bottom right, white at full and red when damaged, with frames and icons for taunt, can attack, exhausted and deathrattle. Blizzard's VFX lead says the main job of effects is to communicate game information, and it deliberately tones effects down when they would hide combos.

### Cited Findings
- WIKI, health display: "Health is displayed at the bottom right corner of each character's card or portrait, set against a red drop of blood." Full health is white; "those below maximum Health have it displayed in red." (source: [Health, hearthstone.wiki.gg](https://hearthstone.wiki.gg/wiki/Health))
- WIKI, taunt frame: Taunt is shown by "a shield-like border enclosing them." (source: [Attack, hearthstone.wiki.gg](https://hearthstone.wiki.gg/wiki/Attack))
- WIKI, deathrattle icon: "Cards with Deathrattle appear with a skull and crossbones when in play." (source: [Deathrattle, hearthstone.wiki.gg](https://hearthstone.wiki.gg/wiki/Deathrattle))
- WIKI, action state: bright green frame for able to attack, "zzz" for exhausted, red arrow for targeting. (source: [Attack, hearthstone.wiki.gg](https://hearthstone.wiki.gg/wiki/Attack))
- DEV, purpose of VFX: Hadidjah Chamberlin (Senior Effects Artist, 2018): the main role of VFX is to "communicate information to the player in fun and interesting ways" and "Visual effects help the player read the board and understand what's happening more clearly." (source: [Blizzard, Developer Insights: The Art Behind THE SCIENCE](https://hearthstone.blizzard.com/en-us/blog/22552047))
- DEV, identity by colour and shape: "A lot of Class identity is defined through visual effects and the colors, materials, and shapes common to that Class"; each class has "a few very specific color palettes"; Druid space magic uses moons and bigger shapes, Mage uses "smaller, sparkly star shapes." (source: [Blizzard blog 22552047](https://hearthstone.blizzard.com/en-us/blog/22552047))
- DEV, restraint: for Magnetic they wanted clarity about targets and attachment but to "make sure that the effect wasn't too flashy"; for Floop's Glorious Gloop the persistent effect was kept light so "players could focus on playing out their combos." (source: [Blizzard blog 22552047](https://hearthstone.blizzard.com/en-us/blog/22552047))
- DEV, audio restraint: Andy Brock: "making the right decisions about what should be heard when, or even not at all." (source: [Blizzard blog 23964694](https://hearthstone.blizzard.com/en-us/blog/23964694/))
- WIKI, rarity frames: legendaries have an orange gem and an elite dragon border; golden cards add class colour ribbons because the gold frame hides the class colour. (source: [Legendary](https://hearthstone.wiki.gg/wiki/Legendary); [Golden card](https://hearthstone.wiki.gg/wiki/Golden_card))
- COMMUNITY, juice as clarity: "What is this telling the player?" should be asked of every effect (forum commenter). (source: [Make Games SA](https://makegamessa.com/discussion/comment/30280/))

### Inferences
- INFERENCE: two stat numbers per unit, fixed corners, fixed colours. On a 5 v 5 pixel board, a pixel font health number under each sprite, white at full and red when hurt, does the same job. A health bar alone does not tell the player "this unit dies to the next heavy".
- INFERENCE: keyword frames (taunt shield border, deathrattle skull) mean a player can read a unit's rules without hovering. For small sprites: one ring or frame shape per state on the floor under the unit (fits the existing floor ring language for effects), one icon slot above the head for passives.
- INFERENCE: tribe identity through a small fixed palette and a shape family (moons versus sparkles) maps straight onto Kindred Siege's 24 tribes: each tribe's hit spark gets its own shape family inside the shared palette.
- INFERENCE: "not too flashy" for persistent effects. Auras and marks that stay on a unit must be quieter than hits, or the hits stop reading.

### Gaps
- No source found for the damage splat (memory, not checked: a starburst over the portrait showing minus N) or its duration.
- No source found for green buffed stat numbers (memory, not checked: Attack and Health above base show green). The wiki Health page mentions only white and red.
- No source listing the on minion icons for Poisonous, Lifesteal, Reborn, triggers and so on.

## 4. Every hit changes the state (numbers dropping, deaths, tokens spawning, board shifting)

### Takeaway
State changes are ordered and visible: a damaged number turns red, deaths wait until the attack resolves, death effects run one at a time in play order, and in Battlegrounds new tokens slot into the left to right attack queue. After combat the board resets, and the surviving minions' tiers become the damage to the enemy hero.

### Cited Findings
- WIKI, damaged number turns red: below maximum health the number is red. (source: [Health](https://hearthstone.wiki.gg/wiki/Health))
- WIKI, death order: deaths resolve one at a time, in the order the minions entered play; death triggers have no special priority. (source: [Deathrattle](https://hearthstone.wiki.gg/wiki/Deathrattle))
- WIKI, Battlegrounds queue: minions attack automatically from the leftmost moving right for each player, then repeat; "Newly summoned minions (such as from Deathrattles) join this left-to-right sequence"; a 0 attack minion skips; "The player with the greater number of minions attacks first"; ties random; targets random unless the other side has Taunt. (source: [Battlegrounds](https://hearthstone.wiki.gg/wiki/Battlegrounds))
- WIKI, end and reset: combat runs until one or both sides have no minions; afterwards boards return to their starting positions, minions are restored, and tokens made in combat are removed. (source: [Battlegrounds](https://hearthstone.wiki.gg/wiki/Battlegrounds))
- WIKI, hero damage reads the survivors: "Damage dealt is equal to their current Tavern Tier, plus the combined tiers of all their surviving minions." Tokens count as tier 1; no survivors on either side is a draw. (source: [Battlegrounds](https://hearthstone.wiki.gg/wiki/Battlegrounds))
- COMMUNITY (search summary, Kotaku): cards can be placed in any order, "with the leftmost card always attacking first." (source: [Kotaku](https://kotaku.com/1839928805))

### Inferences
- INFERENCE: a fixed, visible attack order (leftmost first, alternating) is what makes an automatic fight readable: the player can predict who swings next and watch for it. Kindred Siege's sim has its own schedule, but a visible "next to act" cue gives the same benefit.
- INFERENCE: tokens joining the queue means summons are not decoration; each one visibly takes a turn. For Kindred Siege summons, show the newcomer stepping into a slot.
- INFERENCE: board shifting closes gaps when a unit dies. On a 2.5D pixel stage, a short slide of neighbours into the gap after a death (a few frames) keeps the formation readable.
- INFERENCE: the hero damage formula turns the board left standing into the final blow. A Kindred Siege equivalent: survivors each add a visible piece to the end of fight hit on the enemy leader.

### Gaps
- No source found for how minions die visually (break, burn, shatter) or whether death style varies by cause. Hearthstone's death visuals are not described in any page I could read.
- No source on the board slide timing when a minion dies.

## 5. Rare big moments and pacing (legendaries, golden minions, big spells, hero death, combat speed)

### Takeaway
Big moments are kept rare and unique: legendaries get their own short music stinger (mostly World of Warcraft themes) and Big Deal cards get one off effects that "celebrate the moment". Pacing has moved steadily faster: passive triggers went from 0.8 s to 0.2 s in 2020, Battlegrounds was the test bed, and later Battlegrounds patches raised the maximum combat speed, because in Battlegrounds the next shop timer runs during combat.

### Cited Findings
- WIKI, legendary music: playing a legendary triggers a short, unique track, usually from World of Warcraft music. (source: [Legendary](https://hearthstone.wiki.gg/wiki/Legendary))
- DEV, legendary stingers: Andy Brock says "Legendary stingers" reuse legacy World of Warcraft music to tie legendary characters to Hearthstone. (source: [Blizzard blog 23964694](https://hearthstone.blizzard.com/en-us/blog/23964694/))
- DEV, Big Deal cards: the goal is to "celebrate the moment and the player's accomplishment in a unique way"; Mecha'Thun "really shouldn't feel like V-07-TR-0N or Deathwing, because they all deserve to feel beautiful and special"; legendary minions "get a lot more wiggle room" and take cues from card art and lore rather than strictly the class. (Hadidjah Chamberlin, 2018.) (source: [Blizzard blog 22552047](https://hearthstone.blizzard.com/en-us/blog/22552047))
- DEV, long ceremonies exist: Chamberlin refers to "which is like a seven-second animation" (context not captured in my fetch) and says idle animations are "still a big source of slowdowns". (source: [Shacknews](https://shacknews.com/article/117568/hearthstone-interview-demon-hunters-and-making-the-game-faster))
- WIKI, golden cards: golden frame and "a unique animation", "purely aesthetic", "Golden cards often add movement and visual effects", turning "a static 2D image into a recoloured, animated, moving display"; no gameplay difference. (source: [Golden card](https://hearthstone.wiki.gg/wiki/Golden_card))
- WIKI, Battlegrounds triples: three copies "fuse the three cards into a golden copy with doubled stats and a stronger ability"; playing it gives a Triple Reward that discovers a minion from the next Tavern Tier (tier 6 if already at 6). (source: [Battlegrounds](https://hearthstone.wiki.gg/wiki/Battlegrounds); [Golden card](https://hearthstone.wiki.gg/wiki/Golden_card))
- DEV, trigger speed: 0.8 s to 0.2 s, some 0 s (see section 2). (source: [Hearthstone Top Decks](https://www.hearthstonetopdecks.com/minion-animations-triggers-will-be-sped-up-significantly-in-regular-game-too/))
- DEV, Battlegrounds as proving ground: Chamberlin: "we had more freedom to really test some of those faster animations" and "it accidentally became this proving ground for something that then goes on to positively influence the rest of the game"; "we really wanted to make sure that we sped it up, so that everything feels more fluid". (source: [Shacknews](https://shacknews.com/article/117568/hearthstone-interview-demon-hunters-and-making-the-game-faster))
- DEV, patch 33.2.1 (official notes as reproduced): "Combat speed: Maximum combat speed has been increased." "Turn 2: 10 seconds shorter." "Turn 3: 5 seconds shorter." No reason given. (source: [Hearthstone Top Decks, 33.2.1 hotfix notes](https://www.hearthstonetopdecks.com/hearthstone-33-2-1-hotfix-patch-notes/))
- WIKI, combat time eats shop time: "The Combat Phase does not actually last any time": the next Recruit Phase timer starts when combat begins, so shorter combats leave more time to recruit. First Recruit Phase 60 s, plus 15 s per round, capped at 120 s. (source: [Battlegrounds](https://hearthstone.wiki.gg/wiki/Battlegrounds))
- DEV (search summary only), long fights: patch 29.6.1 raised the maximum experience per turn "to account for the potential for extremely long combat phases." (source: [Out of Games, 29.6.1](https://outof.games/news/7382-hearthstones-2961-patch-brings-increased-battlegrounds-experience-armor-adjustments-and-more/))
- COMMUNITY (search summary only): a 31.6.2 forum commenter asked for "removal of the speed up in the middle of duo fights", which suggests combat speeds up partway through long fights; a 34.x thread says a phone player can finish combat up to 30 s after the other; a 31.4.2 thread says Undead combat "still takes forever". (source: [31.6.2 notes thread](https://us.forums.blizzard.com/en/hearthstone/t/142650); [34.4.2 notes thread](https://eu.forums.blizzard.com/en/hearthstone/t/12612))
- DEV (via news), 16.4 speedups: coverage of patch 16.4 reports animation speed increases. (source: [Out of Games, 16.4](https://outof.games/news/772-hearthstones-164-update-brings-a-new-arena-rotation-battlegrounds-fixes-and-animation-speed-increases/))

### Inferences
- INFERENCE: big moments are big because they are unique per card and rare. A shared "legendary" effect would wear out. For Kindred Siege: each chief or Pal signature gets its own sting and its own effect, and normal hits stay plain.
- INFERENCE: golden is a pure visual upgrade on a stronger unit. In Battlegrounds it marks "this unit is your build's peak". For small sprites a palette swap plus a few frames of shimmer on the sprite outline would carry it.
- INFERENCE: the 60 to 120 s shop timer running during combat creates hard pressure to keep the automatic fight short. Every slow trigger costs the player shopping time, which is why Battlegrounds kept speeding up. Kindred Siege does not share a timer, but long automatic fights will still feel like waiting.
- INFERENCE: a reported mid fight speed up (community only) is a cheap fix for very long automatic fights: normal speed for the opening, then faster. Worth checking in game before copying.

### Gaps
- No developer source on hero death or the final blow of a Battlegrounds game.
- No source on the legendary entrance animation itself (memory, not checked: legendaries land with a larger slam and board shake). The wiki Legendary page covers only music, gem and border.
- No source found for how the Battlegrounds mid fight speed up works (trigger point, multiplier). Patch notes found do not describe it.
- No source with timings for big spells.

## 6. The battle as the player's build paying off (tribes triggering, buffs flying, watching it work or fail)

### Takeaway
The developer material on this is thin. What exists says: keep persistent effects quiet so combos read, make attack triggers frequent so a class feels proactive, and use Battlegrounds as a testing ground for fast triggers because a tribal board (Murlocs with Divine Shield and Poisonous) fires many triggers in a row.

### Cited Findings
- DEV, combos must read: for Floop's Glorious Gloop the effect was kept light so "players could focus on playing out their combos"; designers track which events form a card's combo, such as minions dying and mana crystals being restored. (Hadidjah Chamberlin.) (source: [Blizzard blog 22552047](https://hearthstone.blizzard.com/en-us/blog/22552047))
- DEV, triggers after attacks make a class feel active: Stephen Chang (Game Designer) on Demon Hunter: "It makes them feel proactive because they're constantly attacking and a lot of the cards activate after they attack." (source: [Shacknews](https://shacknews.com/article/117568/hearthstone-interview-demon-hunters-and-making-the-game-faster))
- DEV, tribal boards stack triggers: the Battlegrounds speedup targeted "Murloc triggers with Divine Shield and Poisonous", which each took nearly a second. (source: [Hearthstone Top Decks](https://www.hearthstonetopdecks.com/minion-animations-triggers-will-be-sped-up-significantly-in-regular-game-too/))
- WIKI, the result is scored by what survived: hero damage = Tavern Tier plus the tiers of surviving minions, shown as a two stage Strike. (source: [Battlegrounds](https://hearthstone.wiki.gg/wiki/Battlegrounds))
- DEV, losing is expected: Dean Ayala (Game Designer): "You play in this competitive mode and the expectation isn't necessarily that you win." (source: [Game Developer](https://gamedeveloper.com/design/why-the-i-hearthstone-i-devs-wanted-to-make-an-auto-battler))
- WIKI, the build persists, the fight does not: boards reset after combat and combat tokens vanish, but permanent enchantments from the Recruit Phase stay. (source: [Battlegrounds](https://hearthstone.wiki.gg/wiki/Battlegrounds))
- COMMUNITY (search summary only), stutter breaks the read: a tweet reported minions buffed by Flametongue Totem hesitating during attack animations, making the game feel laggy. (source: [Out of Games dev tracker](https://outof.games/devtracker/hearthstone/twitter/twitter-hearthstone/39885))

### Inferences
- INFERENCE: in an automatic fight the player watches for their own pieces firing. Fast, small, consistent trigger pops (about 0.2 s each) let a chain of ten triggers read as a chain, not a slideshow.
- INFERENCE: quiet persistent effects plus loud trigger moments is the rule that lets a build read. For Kindred Siege's squad synergies and Pal combos: the passive aura stays dim, and each time it pays off it flashes once on the unit that benefits.
- INFERENCE: the end of fight score made from survivors ties the fight's result back to the build. Kindred Siege could show which pieces of the squad's build did the damage in the closing beat.
- INFERENCE: the player cannot act during Battlegrounds combat, so the only feedback channel is the show. Kindred Siege's leader parry gives the player a hand in the fight; the big hits that shake (section 1) should be the ones the parry answers.

### Gaps
- No developer source on buffs visibly flying between minions (memory, not checked: buffs travel as small glowing projectiles from source to target).
- No developer statement found on "watching your build work or fail" in Battlegrounds. The PC Gamer making of article and the Kotaku piece were not readable in full.
- No GDC talk specific to Battlegrounds combat presentation found.
