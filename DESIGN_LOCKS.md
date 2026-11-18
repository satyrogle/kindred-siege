# Kindred Siege - Final owner design constraints

Status: LOCKED by Jacob on 2026-09-30.

Owner instruction: "these are my final corrections and will not be changed ever".

These conditions govern subsequent design, datasets, art planning and implementation. They supersede conflicting earlier proposals and older design documents. Existing code is implementation history, not permission to change these conditions. Do not reopen, reduce, expand or silently reinterpret the locked counts, tier structure, genre, priorities or battle presentation.

## The ten locked conditions

1. **25 tribes in total:** 23 tribes, the Sovereign's tribe as **Alpha**, and the hunters, the rival tribe. Jacob changed this on 18 Oct 2026; it was 24, with the hunters counted as hers. See "The rival and the Sovereign's army, owner decisions, 18 Oct 2026" at the end.
2. **7 tiers** divide the tribe rosters.
3. **Tiers 1-3:** starting rosters; they provide most of the narrative.
4. **Tiers 4-5:** midgame rosters; they pave the path to endgame.
5. **Tier 6:** pre-endgame; unlocks most of the stuff in the game.
6. **Tier 7:** Sovereign.
7. **Genre and pace:** roguelike, fast-paced action.
8. **17 classes:** unlocked through progression. How class unlocks connect to tiers remains to be decided.
9. **Top priority:** "Items, Pals, upgrades and tribes codex" (owner wording, spacing normalised).
10. **Battle screen:** 2.5D.

The Sovereign's Alpha tribe and the hunters are both inside the total of 25 (18 Oct 2026; before that the Alpha tribe was inside a total of 24). Tier 7 is the Sovereign tier; allocation of the other 23 tribes across tiers 1-6 is not yet specified, and neither is where the hunters sit.

## Owner clarification: Pals and the science-fiction premise

Clarified by Jacob on 2026-09-30: Pals are the main character's trusted inner circle. After the opening fight wipes out their force, the main character and a few trusted friends survive. Those surviving people are the Pals, and they serve as upgraders.

The game's tribal roster must be developed in connection with the group's advanced science-fiction technology. Pals are the people through whom upgrades are developed/applied; their specific identities, specialties, costs and upgrade rules remain to be designed. Do not assume that being an upgrader also defines whether a Pal fights, resides at a hub, or occupies a recruitable combat-class slot.

The earlier assistant interpretation of Pals as collectible creatures, pets or handler-controlled combat companions was incorrect. The proposed Beastwarden class depended on that mistake and its definition was withdrawn. In the final catalogue, its reserved C15 slot is Disruptor. Tribal identity does not imply a generic medieval or primitive setting. The class catalogue grounds combat equipment in future technology adapted to tribal capabilities.

## Owner clarification: Rivalry operates alongside progression

On 2026-09-30 Jacob rejected the revised class-tier proposal and reminded Codex: "we also need to make sure to run rivalry engine parallelly". The Rivalry Engine must remain part of the design alongside tribe recruitment, class unlocks, items and Pal upgrades. Class progression must account for its interaction with rival encounters and adaptation; it cannot be finalized in isolation.

The two preceding class-tier distributions are unapproved and must not be implemented as owner decisions. This clarification does not change the 24 tribes, seven tiers, 17 classes or other ten locked conditions. Detailed rivalry rules and their mapping to the current classes still require review; existing prototype code is not proof of correct runtime integration or approval of every historical mechanic.

## Final class catalogue

At Jacob's instruction "lets finalise the classes", the complete class definitions were finalized on 2026-09-30 in `CLASS_ROSTER.json`. This is the current design catalogue; `CLASS_ROSTER_DRAFT.json` is superseded history.

The 17 classes are Warden, Vanguard, Duelist, Berserker, Marksman, Skirmisher, Artillerist, Infiltrator, Trapper, Recon, Medic, Herald, Chemist, Field Engineer, Disruptor, Binder and Conduit.

Each has a distinct combat role, a tribal skill foundation, technological equipment, a signature action and a weakness. Pals remain the surviving inner-circle upgraders. Field Engineer's battlefield deployment role does not replace that Pal function. Tier assignments, progression requirements, numerical balance, tribe availability and detailed equipment/upgrade recipes remain open. Finalized design definitions do not assert implementation, playtest results or approval of a visual asset.

## Decisions still open within those constraints

- The complete tribe roster and the number of tribes in each of tiers 1-6.
- The 17 classes' progression requirements and their connection to tiers; names and core roles are defined in `CLASS_ROSTER.json`.
- Which class/tribe combinations exist. Do not assume every tribe fields every class.
- Detailed rules, content and presentation for Items, Pals, upgrades and the tribes codex, while retaining their top priority.
- Which unlocks belong to which stage, including what Tier 6's "most of the stuff" specifically covers.
- Camera, rendering and asset choices within the locked 2.5D battle presentation. 2.5D alone does not select an isometric camera or require a particular sprite/mesh pipeline.

Item 3 in the owner's message ends with an unfinished "and". Only the supplied complete statement about starting rosters and most of the narrative is recorded as a condition; no missing requirement has been invented.

## How this changes the preceding discussion

- The eight classes found in the existing code are not the final class catalogue. Use the 17 definitions in `CLASS_ROSTER.json`; runtime enum, data, save and behaviour migration remains implementation work.
- The earlier seven-tribe sketch and three-record/guard-scout examples were proposals or examples. They do not define the final roster size.
- The earlier request for a small, grounded dataset remains relevant to implementation: use bounded authored records and shared systems to realise the fixed scope. Smaller development tests do not change the final totals.
- Preserve "roguelike, fast-paced action" as the genre direction. Do not substitute a different genre based on the older GDD or prototype.

This record locks the owner's design decisions. It does not claim that the game, 24-tribe dataset, 17-class roster, codex or 2.5D battle screen has already been implemented or validated.

## Owner calls, 1 Oct 2026 (Jacob's answers, recorded from HANDOFF_LOG.md)

1. Fight model: "Auto battle + directives". Units fight on their own; the player spends directive points (Hold, Fall Back, Focus Fire, Unleash, Sacrifice), with pause and speed. On 2 Oct the orders became refilling buttons (below).
2. Innovation claim: units and tribes that learn from real fights, including the wars between tribes, with a replay of why. Rivalry stays ("there will be no narrative without this"), running on the learning system.
3. The 8 Dec visa slice: Fallen Star; three tribes (the Clarth, a raider tribe whose chief is the rival, a third tribe at war with the raiders); five classes (Warden, Marksman, Skirmisher, Trapper, Medic); about 20 to 30 minutes.
4. Counts: "yes lock it". Tribes and classes are hand made and unchanging (24 and 17 remain the full game); everything inside them is procedural: individual fighters, traits, leaders, what they learned, and the wars between tribes.
5. Art: PixelLab for sprites and animation.

## Owner calls, 2 Oct 2026 (Jacob in chat with Claude)

These add to the ten locked conditions; none of the ten changes. Items marked "Claude's detail" were proposed by Claude and not objected to, but not confirmed word for word.

**Premise and tone**
- The player's people are a cult of cyborg flesh beings ("cybernetic bio organisms"; not synths, not ethereal) who worship old gods and follow their teachings. Cunning and vile, sure they are the top predator.
- Prologue, part 1 (Jacob, 7 Oct; this replaces the 2 Oct landing, where the army was "wiped out by the Sovereign in one second", and the older "crash landed" wherever the wording differs). Reference for the wipe: Clair Obscur: Expedition 33. The planet is still a rogue death planet where everything alive fights (references: Genna in Predator: Badlands, the hubris of Outriders). The cult is overconfident: the ship's scanners track the most powerful being on the planet, the ship comes into orbit and the army raids the Sovereign's place directly. The Sovereign is a she. She comes out of nowhere and destroys half the army in a second. While everyone is baffled, the leader's right hand man is cut in half. She lands on top of the leader, says "boo hoo you suck" and prepares to annihilate the rest. When the dust settles and the explosions die down, every officer is dead except the leader's four first officers, scattered. The leader takes them and retreats. They regroup, all heavily damaged, some missing limbs. Claude's reading, not objected to: the four first officers are the four Pals. Not decided yet: why she leaves the leader alive, and part 2.
- Tone: funny everywhere, in the spirit of Ironhide's Kingdom Rush; only the Sovereign is truly scary.
- The "three tit rule": every character and region needs one look a gamer gets in one second from a screenshot. Lore can be clever; the look must be instantly appealing. An absurd, addictive loop that players can beat in many ways comes before niche artistry.

**Loop**
- Fight: auto battle. Order buttons refill during the fight so the player stays busy (Kingdom Rush). Orders come from the leading Pal's god (Claude's detail) plus the signature moves taken from broken tribes (Mega Man Battle Network).
- Break: each tribe has a nerve bar. Hits, scary orders and gear drain it; at zero the tribe gives up and everyone still alive joins the cult (kill fewer, keep more). This replaces "breaks below half strength".
- The chief of a broken tribe runs away with what he learned and comes back later leading a splinter group.
- Upgrades: Pals forge gear from what you won, and the Pal who broke a chief learns that chief's gear recipe (Summon Night: Swordcraft Story). Gear enhances a unit's look, like He Man's Snake Armor. No mutations and no extra limbs.
- The answer to the "focus their Medic and Hold" exploit: holding drains your own nerve over time, and chiefs can also learn to flank a holding line.
- Death: army, gear and converts reset; chief memories, the codex and Pal stories stay. Our own units keep learning.
- Modes: a campaign big enough to feel endless, plus an endless mode (the same world with the ending switched off). Battles are generated by the rivalry engine inside the 24 tribe and 17 class locks. Tribes are still collected in the codex.

**Pals**
- Five survivors: the leader (the player, kept quiet) and four Pals. Pals are still the upgraders, and a Pal leads each fight as the hero. Claude's detail: one Pal per fight, the fight is lost if the Pal falls, and only Pals transform mid fight (the Cringer into Battle Cat moment).
- The four Pals take Warhammer Chaos god traits only, never Games Workshop's looks: the Butcher (war, rage), the Glutton (rot, endurance), the Shade (change, tricks), the Whisperer (excess, charm). Names are placeholders. They do not hate each other.
- Personalities are cheeky, in the spirit of the Dave the Diver cast. Each has a backstory of what the Sovereign's one second did to them, and their final chapters land at the Sovereign rematch (Octopath Traveler style arcs).

**Tribes and planet**
- Character look (Jacob, 3 Oct; this replaces the earlier "one native species" and "small horns" calls): every native is lean and toned, never bulky, "hot and menacing", in light battle gear with one signature piece of identity. Each tribe is its own race, mixing several tribal or advanced races from different films and games, never a copy of one source (Jacob rejected drafts that read as Dek's species, an old god and a World of Warcraft character). Realistic heroic proportions, not chibi. The three tit rule always applies.
- Each tribe also has the monster it hunts and the wreck it scavenged. No riding monsters (Jacob, 9 Oct): no chief rides or surfs a monster. Converts keep their look and add the cult sigil and their Pal's gear.
- The Clarth (Broken Wastes), approved 3 Oct: war hunters with crested bone war masks, red cloaks, toned bodies, bronze bracers and braided hair; the slice squad is three women and two men. Their signature pieces come from the sand shark they hunt: jaw shield (Warden), spine rifle (Marksman), fin blades (Skirmisher), tooth net (Trapper), tooth totem staff (Medic).
- The Bloodridge raiders (the rival's tribe), locked by Jacob on 3 Oct: Egyptian desert guardians after the Medjai (the black robed guardians of The Mummy and the real Medjay), finished in the mood of Zdzisław Beksiński's paintings. Rotted sepia and ash brown rags, burial bandages on the arms and legs, corroded bronze collars and armbands, ashen skin, lava glowing through cracks. Two kinds of head: faceless hoods with ember eyes for the Warden and Trapper, lava tattooed faces under hoods and headwraps for the Skirmisher, Marksman and Medic. Signature pieces: tall Egyptian war shield and khopesh (Warden), bronze fitted rifle (Marksman), twin khopesh (Skirmisher), hooked bronze flail (Trapper), sceptre with an ember lantern (Medic). By Jacob's choice this tribe is truly scary, an exception to "only the Sovereign is scary". Rejected on the way: Beksiński style shrouds with crab part weapons, the same shrouds with forged weapons, and a clean black, indigo and gold Medjai that lost the Beksiński mood.
- Skullpeak (at war with the raiders: sharpshooters), approved by Jacob on 3 Oct: humanoid beings who grew machine parts into their own bodies, with H. R. Giger as the reference for scope, not for his actual designs, never alien looking and never a copy of the xenomorph. Warm human skin with polished steel bones showing through the chest and ribs, a steel crest that stops at the hairline so the face shows, dark slate wraps and a turquoise glow in the seams. Each unit's signature piece is one bionic arm, after Bionic Commando: grappling claw arm and tower shield (Warden), arm ending in a curved blade (Skirmisher), grapple arm with a hook on a chain (Trapper), rifle built into the arm (Marksman), injector needles and turquoise vials (Medic). Rejected on the way: armoured biomech brutes, black biomech creatures, a human in a Giger suit, other peoples with a Giger finish, a scope eye and piston muscles.
- Chiefs (Jacob, 3 Oct): Jacob made a placeholder design for each chief and had them drawn in PixelLab from those, as 192 px fronts that stand a head taller than their Wardens. Approved: the Clarth chief, a lean war chief with red ochre war paint and white stripes, a crested bone crown, long black braids, a white fur mantle, a red cloak with white sigils, dark armour with bronze trim and a long spear with a glowing red blade ("clarth guy is set"; the placeholder's Predator style face was dropped for a human one). The Bloodridge chief, a faceless ember hood with antlers, ram skulls on one shoulder, a corroded bronze collar, rotted layered rags and bandages, small skulls and charms, a lava sun tabard and a staff holding a burning ring ("option 1 for bloodridge is fine"; the first, cleaner round was too simple and lost the rot). The Skullpeak chief, a woman with a white undercut and a scarred blind eye, three breasts in a fitted black top framed by steel ribs with a turquoise glow, a torn black cape, bone gauntlets, a turquoise lit sword and a steel bionic leg (the three breasts are Jacob's call; in the approved drawing they read only faintly). Their mounts stay as above.
- Pals (Jacob, 3 Oct): Jacob draws the four Pals himself. Agents do not draw them.
- Colour (Jacob, 3 Oct): every battle sprite is cut to 32 colours in Aseprite, with one palette shared by all eight of its directions and its idle frames.
- Size ladder (Jacob, 3 Oct): tanks are drawn biggest and most brutal, melee damage dealers medium, ranged and support small, so every squad reads big to small. In the slice: Warden 128 px; Skirmisher and Trapper 112 px; Marksman and Medic 96 px.
- Bloodridge champion clarification (Jacob, 15 Oct): the champion is the war chief's right hand, capable of beating a Warden. His art should be nearly the Warden's size, with a clearly distinctive champion trait and the presence of an elite officer. Use the 128 px Warden class as his visual scale reference, rather than the ordinary 112 px melee class. This is an art-direction clarification, not a change to combat rules or statistics.
- Bloodridge champion appearance selected (Jacob, 15 Oct, "1"): option 1 from `Art/Studies/Bloodridge-Champion-Weapon-20261015/weapon-review-2x.png`. Reference master `champion-1.aseprite`, native image `champion-1-1x.png` in the same folder. Keep the two-handed great khopesh, braced grip, ragged layered shoulder mantle, burial hood and ember face. This selects the static champion design; rotations, animation and game integration are not yet checked or approved.
- Regions keep the concept map names (Broken Wastes, Bloodridge, Stoneveil, Thundercoast, Skullpeak, Silvertree, Echo Reed), each with one screenshot hook. The planet is littered with the wrecks of earlier armies the Sovereign beat; that joke lives in the codex and the Pals' lines, not in the look.

**Slice scope and art**
- The 8 Dec slice is the core loop plus one transform: fight, nerve break, convert, one Pal (the Butcher), one forged gear piece, and a chief who runs and comes back smarter.
- Characters are drawn in PixelLab, not the local pipeline (the local drafts of 2 Oct were rejected). Battle sprites follow the size ladder (128, 112 and 96 px) and fight in the three quarter views. Each tribe's approved units are the style reference for the rest of that tribe. Briefs and asset IDs live in `PIXELLAB_BRIEFS_SLICE.md`. Every generation must count.

## Leader appearance and four camp Pals - owner direction, 10 Oct 2026

This dated owner direction supersedes earlier conflicting Pal combat/art-reservation statements above. Jacob asked Codex to make all four Pal concepts, inheriting the leader's shared skin/species design, and confirmed that all four stay at camp as upgraders. Earlier instructions reserving all Pal drawings to Jacob no longer prohibit this requested concept pass. These new Pal appearances still require his review before sprite production.

- Leader: Jacob locked the body, face, three eyes (vertical forehead eye), fully transparent brain, split-arch crown, aged dreads, opalescent skin, augmentations, clothing, broken chest sigil, feet and structured layered astral cloak shown in `Art/Concepts/InnerCircle/2026-10-10/leader_locked_body_v16.png`. Its old sceptre is explicitly superseded.
- Leader weapon direction: an integrated bracer uses his nerve impulses to steer a small physical relic over his palm. No sceptre or holographic toy soldiers. The new rendering is `Art/Concepts/InnerCircle/2026-10-10/leader_neural_relic_v17.png`; this is the requested implementation of the direction, not a separate owner verdict on that new rendering. Do not reopen the locked body/cloak while reviewing the relic.
- Pals: all four remain at camp and each is responsible for an upgrade area. CORRECTED by Jacob later on 10 Oct: only the underlying skin material is shared; colour, corruption, anatomy, faces and regalia must differ substantially. The leader's three eyes, transparent brain, dreads and crown are exclusive to him. His anatomical/material principles may be hybridised into substantially transformed Pal bodies. The first four Pal sheets were rejected as leader clones. Exact individual injuries remain open; see the owner's detailed correction below.
- Butcher: war/rage; forging weapons and gear. Shade: change/tricks; the priest-shaman woman whose upgrades unlock orders. Both follow the established shared-log directions.
- Glutton: rot/endurance; armour and body augments. Whisperer: excess/charm; skins and regalia. Jacob explicitly selected this pair of responsibilities on 10 Oct in response to the design question.
- The four Chaos gods AND their armies provide references for distinct visual identities, transformed forms and traits, as clarified by Jacob later on 10 Oct. Create original hybrids rather than literal Warhammer characters. The Pals are trusted comrades; do not import the gods' mutual rivalry. The four working names remain placeholders.
- This locks role ownership and shared visual constraints, not costs, statistics, recipes, additional passive-upgrade systems or new combat powers. Concept approval does not imply PixelLab/sprite/animation/in-game integration approval.

## Pal visual reset - owner's detailed answers, later on 10 Oct 2026

This correction supersedes the first four generated Pal concepts and their inherited leader anatomy. Those sheets are REJECTED, not approved references. The leader's locked appearance is unchanged. Work one Pal at a time, starting with Glutton, and obtain a visual verdict before extending a new direction to the others.

- Shared origin: the same underlying opalescent material with different colouring and corruption for each Pal. Each may become a different astral form through their god's influence; heavily transformed, almost completely alien bodies are explicitly allowed for these Pals. This is character design, not a new item-driven mutation mechanic.
- Leader-exclusive: transparent brain, three-eye arrangement, dreads and crown. Hybridise anatomical/material principles where useful without replicating his identity.
- Butcher: hybrid of a battle-scarred general and furious war priest; sheer aggression is the principal Khorne reference.
- Butcher v2 (`Art/Concepts/InnerCircle/2026-10-10/butcher_general_v2.png`) is REJECTED IN FULL: Jacob said "nope", then "nothing" when asked which parts to keep. Carry forward none of its face, body, clothing or tool design. The original general/war-priest brief remains; the replacement appearance is undecided. Preserve the rejected image for history only.
- Butcher replacement direction, selected by Jacob on 10 Oct: "Towering and inhuman: long proportions, massive hands, visibly transformed anatomy" and "Cold command: rigid authority, anger expressed through absolute control". Express his underlying aggression through controlled authority at rest. These answers guide a fresh concept; they do not approve its resulting image, assign a specific injury or change his camp weapons/gear role.
- Butcher v4 (`Art/Concepts/InnerCircle/2026-10-10/butcher_v4_commanding_mass.png`) is parked as a PROVISIONAL working candidate. Jacob said "yea whatever"; Codex proposed provisional status and moving to Shade, and Jacob said "go". Do not describe Butcher as having the same explicit appearance lock as Glutton.
- Glutton: enormous, swollen and disturbingly different; beautiful skin with subtle underlying corruption together with overt grotesque rot. Armour/body-augmentation responsibility stays locked. First new concept only; specific appearance remains for review.
- Shade: transformed astral scholar; layered ceremonial robes and shifting silhouettes. The priest-shaman woman and new-orders role remain.
- Shade regalia clarification, later on 10 Oct: Jacob rejected the hood/belt/tailored robe/spellbook direction as too much like Harry Potter. He authorised the proposed replacement with layered black membranes anchored to grown obsidian, exposed astral anatomy and markings beneath the skin; scholarly identity should come through expression and working gesture. Shade v4 (`Art/Concepts/InnerCircle/2026-10-10/shade_v4_embodied_regalia.png`) is parked as a PROVISIONAL working candidate: Jacob said "yeah whatever", Codex proposed provisional status and moving to Whisperer, and Jacob said "lets go". This is not the explicit appearance lock given to Glutton. Her retained face is not separately locked.
- Whisperer: deliberately difficult to place, beautiful and threatening; a key character needing deeper Slaanesh research. The owner refers to her as she while requesting ambiguous presentation. Skins/regalia responsibility remains; do not reduce her identity to a generic attractive tailor.
- Whisperer clarification, later on 10 Oct: Jacob rejected v2 (`Art/Concepts/InnerCircle/2026-10-10/whisperer_v2_dark_astral.png`) and selected "something dangerously inhuman beneath an inviting manner" together with "mesmerising beauty" as the intended presence. Convey the welcome first and the inhuman danger on closer inspection. This clarifies direction, not approval of a new rendering. Preserve rejected v2 for history only.
- Clothing/material: black fabric combined with polished volcanic-glass armour. Around 55 percent exposed skin was a tentative visual balance, not a strict quota. Strong void black, obsidian, deep purple and ancient gold; the owner hates neon.
- Aura: restrained dark mist leaking from their bodies and moving with intention; avoid a large obscuring effects cloud.
- Injuries: offer the owner options; specific Pal injury/amputation assignments have not been selected.

### Glutton personality clarification - 10 Oct 2026

- Jacob rejected `glutton_v2_dark_astral.png`. Its appearance is not an approved reference.
- Owner's chosen personality: "a patient, unsettling patriarch with heavy funny uncle personality carefree". Keep his enormous/swollen dark astral character, but make the affectionate, teasing, carefree uncle temperament visible in expression and body language.
- Armour/body-augmentation responsibility and the shared Pal material constraints remain. Specific Sovereign injury/amputation is still unselected.
- Jacob subsequently approved Glutton v3 with "haha yea thats it". `Art/Concepts/InnerCircle/2026-10-10/glutton_v3_carefree_patriarch.png` is now the locked concept reference: enormous upright swollen violet-nacre body, articulated alien face with amused/laughing expressions, obsidian-black and ancient-gold regalia, localised flank decay and restrained dark haze. Preserve his patient, unsettling, affectionate and carefree funny-uncle personality. Earlier Glutton versions remain rejected. This records concept approval, not completed sprite, animation or game integration.

### Inner-circle sprite implementation - owner instruction, 10 Oct 2026

- Jacob then instructed: "Whatever generate 192x pixel for those using pixel lab and let's put them in game". This authorises the five current concepts for this implementation pass: leader v17, Butcher v4, Glutton v3, Shade v4 and Whisperer v3. It supersedes the earlier wait for sprite-production permission; it does not retroactively give every provisional concept Glutton's explicit appearance lock.
- Five 192 x 192 transparent PixelLab fronts are exported through Aseprite to at most 32 colours. The leader is bound in CampaignArtLoop battles; the four Pals are presented at camp with their existing roles. They do not join combat. These are static fronts; directional views, animations and camp upgrade transactions are outside this pass.
- Whisperer's first request failed PixelLab's content check without charge. One successful retry uses full opaque black regalia and retains her face, hair and palette; it covers more of her anatomy than concept v3. This translation difference is not a new owner appearance lock.
- Assets and generation receipts: `Art/PixelLab/InnerCircle/2026-10-10/`; runtime fronts: `Assets/Resources/CampaignArt/InnerCircle/`.

### Owner rejection and corrected scope - later 10 Oct 2026

- Jacob rejected the five first sprite translations and the premature integration. Correct the sprites for review before locking appearances or making camp animations. Current correction files are review-only under `Art/PixelLab/InnerCircle/2026-10-10/corrections_v2/`; do not silently install them in Unity.
- Leader garments and cloak must read black, retaining his established skin and anatomy. Reduce Glutton's exaggerated belly. Give Butcher a proportionate larger head. Fully cover both of Shade's breasts. Remove Whisperer's nun-like floor-length costume while retaining her inviting, dangerously inhuman identity.
- Camp presentation: joke intro (Kindred Freeze to Kindred Siege), town main menu, then a physical camp containing all four Pals and their upgrade stations. Four character cards are not the intended camp.
- Highlighting a Pal plays a short repeating work/task loop, with activity such as hammering at an anvil or reading. This is the owner's old-school Game Boy / Final Fantasy Tactics-like interaction reference, not a demand to reproduce another game's assets. Corrected sprite approval comes first; specific loops and props remain to be agreed. No camp implementation or animation production in the current correction-only pass.

### Pal presence corrections - later 10 Oct 2026

- Jacob: Shade should always levitate. Glutton is always asleep, with Jabba the Hutt as a posture/presence reference, not a species or appearance to copy. Butcher needs visible gear. These supersede the standing review poses as final camp appearances; the corresponding new sprite revisions remain pending.
- Jacob rejected the corrected Whisperer as still a goth human woman. When offered two replacement directions he selected option 2: "Living apparition: a beautiful face within a shifting, partly translucent astral body; her silhouette never fully settles." Retain the inviting manner and concealed inhuman danger. This selects a direction, not approval of a generated result.
- Next bounded pass is one Whisperer apparition sprite for review and phone delivery through Drive. Other Pal poses/gear, task loops and game integration are not silently bundled into this single-character review.

- Subsequent Whisperer correction: Jacob rejected the apparition take and explicitly requested a "wild beautiful feral astral being like n'aaru" and an intense goth woman. Her goth identity is now intentional: fierce beauty, wild presence and convincing goth facial styling, combined with original astral anatomy inspired by floating energy/fragment forms. Do not keep the earlier demure face and merely replace her legs. This supersedes treating all humanoid goth cues as forbidden; the rejected sprite itself remains rejected. One fresh 192px candidate for review, not automatic locking or integration.

## Game loop, owner decisions, 10 Oct 2026

Jacob played the campaign art build on 10 Oct and rejected the respect campaign built from the 8 Oct answers. He could not see the loop on screen, and it "didn't make sense". The calls below replace the 8 Oct answers they conflict with. Anything not listed here is still open, including everything Claude proposed in chat that day.

- Order of work: fix the game loop first.
- The loop stays a roguelike and stays simple, "like easy Pokémon": go in, fight a few times, beat the chief.
- Each tribe is a set path: a few fights, then its chief ("neat and easy"). The respect bar is dropped; this replaces 8 Oct answers 5b and 6.
- Taboos are dropped; this replaces 8 Oct answer 9b.
- Sacrifice is left out of the orders (it was in the 1 Oct order list).
- Each chief gives you one main order, like the Eclipse. This fits 8 Oct answers 12c and 13a.
- Every run starts and ends at camp, even when you lose. This replaces the 8 Oct rejection of "a hub you return to on every death".
- Camp shows visible attributes.
- Losing a run works like Hades and Absolum: the gear and items picked up during the run are lost, while the materials the Pals use always come home and pay for upgrades at camp. This adds to the locked death rule above (army, gear and converts reset; chief memories, the codex and Pal stories stay).

Still open, raised by Jacob on 10 Oct: whether one run covers one tribe (8 Oct answer 1a) or crosses several tribes (the "march" option he liked that day); parry (he wants it; its timing, cost and reward are not designed); the Alpha tribe hunting you and how that scales; how the loop scales to 23 tribes, 7 tiers and 17 classes; what each Pal's upgrade does; what happens to a player who keeps losing; and how the unique mechanic (tribes that learn from real fights, with a replay of why) shows on screen.

## Run structure and progression, owner decisions, 10 Oct 2026

Jacob said yes to this later on 10 Oct ("yes okay i can understand all this but lets see how it plays"), then "go add it to the locks and start engineering". It settles the open run question above: a run crosses several tribes. It replaces 8 Oct answer 1a (one run is one tribe).

- The campaign is the runs, like Hades. Every run leaves camp and pushes out tier by tier, one tribe per tier, toward the Sovereign. Each tribe is a few fights and then its chief.
- A run never visits all 23 tribes. Each tier holds about four of them and a run meets one per tier, so a full run is at most six tribes and then the Sovereign. Most runs end in tiers 1 to 3, where the locks put most of the story. Other runs meet other tribes.
- The 8 Dec slice is one run: camp, Bloodridge, Skullpeak, then the first Alpha hunters.
- Nobody joins until the chief fight (8 Oct answer 3b). The more of the chief's fighters still standing when his nerve breaks, the more of them join.
- Each chief gives you his one main order for the rest of the run.
- When a run ends, win or lose, the tribes that joined during it, their gear and the chief orders taken during it reset. Pal materials, the upgrades they bought at camp, the codex and what the chiefs learned about you stay.
- The story moves forward every time you come home, through the Pals at camp.
- The chiefs fight differently on later runs because they learned from your earlier ones. This is the unique mechanic. Whether it sits too close to the Nemesis patent is still the solicitor question from 8 Oct.
- The Alpha hunt grows within a run: the more tribes join you during it, the harder the Sovereign's hunters come. It resets with the run.
- Every tribe you win over goes into the codex for good. Filling the codex over many runs is the long goal.
- The run and the camp are saved separately.

Still open: what each Pal upgrade does; parry; the Alpha hunt's exact rules; making one button spam lose. Numbers such as two fights before each chief and a run of 30 to 40 minutes are starting values.

## Presentation, owner decisions, 11 Oct 2026

Jacob played the art build after the UI kit pass and the sprint that followed it.

- The UI is fine. The tutorial walkthrough and the fight choice cards (icon, name, short tags, full text on pointing) are okay.
- The world is drawn at 1x: battle stages, fighters and the camp at their own pixel size. Menus, buttons and text stay on the 2x kit. Jacob: "yes go 1x, do it". This replaces the morning's 2x for the world, which made every fighter 60% bigger than the 1.25x before it; at 2x "everybody is cooped up in a singular place and we can't able to point out the map because every unit is so big".
- Battle stages are redesigned: "stop using the same thing. Use coherent consistent images that can be actually used as stages for battling." One stage per region instead of the Clarth canyon for every fight.
- Intro: the joke names run Kindred Freeze, Kindred Cheese, Kindred Bees, then Kindred Siege, then the game's logo. Then the premise is shown like a trailer, after proper research on intros: the alien ship locking onto the Sovereign, the ship on the horizon and the place she lives, then the opening fight, then the camp. Claude's reading, said in chat with no objection: a short skippable comic in Kingdom Rush's way, using the prologue part 1 wording, the Sovereign as a silhouette until she is designed, ending on the retreat because part 2 is not written.
- Camp: "an open land ... where there is enough space for all the guys to have all their tools", each Pal with a visual identity, "not all clamped together with screenshots or wallpapers".
- Rejected on 11 Oct: the three PixelLab town options (jaw gate hill town, crater town, moon walled town: generic towns, not this world) and the three camp options (canyon camp, temple hall, cave: cramped wallpapers with the Pals in a row).
- Rejected in full later on 11 Oct, the whole 1x art pass: "the whole pass is shit. I don't want anything from what you made in the last pass." That covers the open camp ("single most worst thing I've ever seen in a video game"), the three menu looks, the three region stages (Broken Wastes, Bloodridge, Skullpeak), their PixelLab top down tilesets and the props placed on them. Nothing from that pass carries forward. His order before any more art: "Do some proper research, bro how these are actually made. Just don't do shit and waste credit." The research is in Desk 42/reports/Pixel art stages hubs and menus.md; no route from it is approved yet.

## Fight and recruit calls, owner decisions, 11 Oct 2026

Jacob said "yes to both, go" after the spam test (bots on the same 100 fights per kind: Unleash spam won most war parties, trials and Bloodridge's Hold the line; healer focus was the weakest bot almost everywhere).

- Unleash drains your own nerve while it runs, the way Hold already does (call J3), at half Hold's rate: one nerve every two ticks. Jacob chose "halve the cost" after the full rate also hurt the reading bot (Skullpeak trials 97% to 46%) while war party spam still won. A starting value.
- No floor on recruits (call J4): exactly the chief's fighters still standing when he breaks join you. Killing them all brings nobody, though his order still comes. This replaces the old top up to three.

## Forge and hunt calls, owner decisions, 11 Oct 2026

Jacob said "yes to both, go" to calls J1 and J2, then answered four questions about them (all four answers were Claude's recommendation).

- The Butcher's first gear is the Lava Sun Tabard (call J1). Breaking the Bloodridge chief teaches the Butcher the recipe. At camp, 50 materials forge it, once and for good. Every run after that one fighter wears it, and their hits drain enemy nerve 40% harder. The tabard comes off when the run ends; the forge stays.
- Until you pick someone, the first Warden in your squad wears the tabard. You can hand it to anyone at camp or between fights. If its wearer falls, the first Warden left picks it up.
- After the run's last chief comes one last fight against the Sovereign's hunters (call J2): 4 fighters and a captain, plus 1 fighter for every tribe that joined you this run. The captain winds up a big hit you can see coming. Breaking their nerve wins the run; they never join. A full slice run is now camp, Bloodridge, Skullpeak, then the hunters.
- The hunters arrive knowing what the tribes on the path know: they Hold when you Unleash and cover whoever you focus. Without that, Unleash spam won 90% of full run hunts in the bots; with it, 9%, while the plain sensible bot wins 84% and the reading bot 40%.
- The hunters do not learn between runs. They come back the same every run.
- Breaking the hunters pays 30 materials, like a chief.

Numbers (50 materials, 40%, 4 hunters plus 1 per tribe, 30 materials) are starting values. Not decided: how the hunt scales past the slice, and the other Pals' recipes.

## Tribe war and rematch calls, owner decisions, 11 Oct 2026

- No tribe war fight in the 8 Dec slice (call J5). Jacob first chose a war at camp between runs, then cut it ("yes cut it, go"): it adds no play time, it could wear down the habits that stop spam, and it muddies "they learned from you". The habits tribes arrive with stay set by hand.
- "A chief who runs and comes back smarter" is shown on the fight card instead (C7): before a fight, the card says what that tribe learned from one of your fights and points at the fight that taught it. Claude's rule for which lesson: the newest one it still uses.

## Balance calls, owner decisions, 11 Oct 2026

Jacob said "tune it now", then answered three questions after the bots showed that every enemy squad was picked by how many fights had been played, so the same fight swung from about 1% to 98% for a sensible bot depending on what came before it.

- Fixed lineups: each tribe sends the same squad to the same kind of fight, every time, so each fight is one test you can learn. Claude picked each lineup with the bots: doing nothing loses where a lineup allows it, then the best one for a sensible player. The lineups are in CampaignRules.cs and AlphaHunt.cs.
- Difficulty is Medium: a sensible player wins about one first run in three, and most players win by run 3.
- Medium is measured on the reading bot, which reacts only to what the screen shows, including the habits the squad page lists. The plain bot ignores those habits, so it cannot pass Skullpeak's precision trial (the 10 Oct test of focusing their Wardens first).
- Result, a starting value: the hunt runs at 98% strength. The reading bot wins 31% of first runs and 87% by run 3. Doing nothing, spam, random and the cheat bot win no runs.

Still open, for Jacob's calls: Bloodridge's hold the line is lost by holding (your own Hold drains your nerve faster than their attacks); doing nothing still wins a third to a half of Skullpeak war parties; Unleash spam still wins most war parties and Skullpeak's trial.

## Name calls, owner decisions, 12 Oct 2026

Jacob answered Codex on 11 Oct (answer 27): "We need to do a random name generator", with the chiefs named by it. On 12 Oct he took Claude's recommendation on all four calls.

- Each people has its own sound. Bloodridge get real ancient Egyptian names, after the Medjay they are based on (Nakht, Meryt, Ahmose). The Clarth get short, hard, made up hunter names (Karru, Teshka, Vahn). Skullpeak get clipped, metal sounding made up names (Vessa, Orsk, Dagna).
- Humour: every chief has a deadpan title, for example "Sekhemre, Twice Buried" or "Mirka, Mostly Original Parts". About one fighter in 20 gets a cheeky name (Rivet, Bait, Still Warm). A name given to a second fighter of yours makes them "the Second". Bloodridge stay scary, so their titles and cheeky names stay grim.
- Enemies have names too, and a fighter who joins you keeps the name they fought under. A chief is the same person all campaign. Nobody you meet on one tribe's path in a run shares a name.
- The Sovereign's hunters stay faceless: no names, and their captain stays "the hunt captain".

The name lists are Claude's, in CampaignNames.cs, and Jacob can change any of them. The Pals' names are untouched (still placeholders).

## Game design, owner decisions, 12 and 13 Oct 2026

Jacob called the slice "click and play" on 12 Oct. He and Claude brainstormed a new design on 12 and 13 Oct. Every point below is his answer to a question or his own correction, and he said "go" on 13 Oct. It replaces the calls listed at the end of this section. Numbers are starting values.

The heart is the catch, and the catch is capturing the chief.
- Nobody joins in a tribe's path fights (8 Oct answer 3b stands).
- Capture the chief, by breaking his nerve while he is still alive, and the whole tribe joins you, yours to command, with his order. Jacob: "its not easy".
- Kill the chief instead and the tribe does not join, but you take his Princess gear (confirmed 13 Oct).
- A won path fight: strip one piece of their gear, or bank materials that go home.

Losing: your roster is your lives.
- Lose a fight and your fallen stay down, and the winners capture one of yours, by name. The run goes on.
- The run ends when you cannot field a squad of 3.

Gear comes in three tiers: Pauper, Mid and Princess. War parties drop Pauper, trials and duels drop Mid, elites and chiefs drop Princess. One gear slot per fighter. Princess gear has a special effect. Run gear is lost when the run ends and materials come home (the 10 Oct Hades rule stands).

Every fighter has one quirk, for example "Grudge: hits his old tribe harder". Two or three fighters of one tribe in the squad unlock that tribe's set bonus.

Orders cost points you earn, Jacob's idea to kill spam: a parry, dodging a cast with Fall Back, interrupting a cast with Focus Fire, a kill, breaking an enemy's nerve, a combo. A small trickle over time so nobody is stuck at zero. Points are spent on Focus Fire, Unleash and the chief's order.

Parry is Perfect Hold. Hold in the last half second before a big hit lands and you take no damage and no nerve cost, the attacker staggers, and an Unleash straight after is a combo. Hold at any other time works as before. No new button. This settles the parry left open on 10 Oct.

Attacks can miss. Every fighter has Hit and Avoid, and tribes differ. No chance is shown anywhere (Jacob, 13 Oct: "Just show damage number"): the player sees damage numbers, and MISS when a blow misses. Focus Fire raises Hit and Hold raises Avoid. One roll from the run's seed decides each blow, so replays stay exact.

Tribes act differently under threat, because repetition is a chore (Jacob). Each tribe gets three or four reactions, for example Bloodridge rallying when one of theirs falls and guarding the shaman, Skullpeak hunting your most hurt fighter, and the Clarth kiting and trapping your back row.

Nerve snaps: when a fighter's nerve breaks, they either panic (flee, ignore orders) or rise (berserk). Both sides.

The Sovereign's hunters become a mercenary company. They stay faceless (12 Oct stands), maul you every run, and inside a run they counter your sets and favourite tricks. They do not learn between runs (11 Oct stands). Nobody can recruit them until the late endgame, so not in the slice.

Also in the 8 Dec slice, by Jacob's picks: a path map in place of the three fight cards, events between fights, formation (front and back rows), chief taunts, fight objectives, omens (a harder run for better drops), arena hazards, and camp upgrades if there is room.

Build first, optimise later (Jacob, 12 Oct). Each feature is built until it plays and he has played it. No bot sweeps or balance tuning before that; one quick test per feature, only to prove it runs.

Order of work. Each gate is a moment Jacob has to feel in a real run. Dates are starting values.
- Sprint 1, 13 to 19 Oct, the fight: earned points, Perfect Hold, Hit and Avoid, tribe reactions, nerve snaps, combos on screen. Gate 1, The Read.
- Sprint 2, 20 Oct to 1 Nov, the run: chief capture, roster as lives, gear tiers with strip or bank, quirks and sets, formation. Gate 2, The Catch and The Comeback.
- Sprint 3, 2 to 8 Nov, the road: path map, events, objectives, omens. Gate 3, The Route.
- Sprint 4, 9 to 15 Nov, the rival: the mercenary company and chief taunts. Gate 4, The Rival.
- Sprint 5, 16 to 22 Nov: arena hazards (they need the stage art route) and camp upgrades if there is room.
- 23 to 29 Nov: tuning with bots. Gate 5, 30 Nov: the player test. Gate 6, 7 Dec: freeze.
- If late, cut arena hazards first, then camp upgrades, then omens.

This replaces: the chief's standing fighters joining (11 Oct, J4), now the whole tribe joins on capture; one lost fight ending the run, now the roster is your lives; the 8 Dec slice as one forged gear piece, now it grows to the list above. Still open: the quirk list, each tribe's set bonus and reactions, point values, the path map's shape, the other Pals' upgrades, and the art route.

Sprint 2 calls, 13 Oct 2026. Jacob picked Claude's call on each of these with his "go start sprint 2". They close the quirk list and the set bonuses above. Numbers are starting values.
- Losing: a lost fight does not move the run on. You get fresh fights at the same step, and a chief who beats you calls you out again. Your fallen stay down and the winners capture your most hurt survivor.
- Quirks, one per fighter. Grudge: 25% more damage on a tribe that captured one of yours this run (Jacob, later on 13 Oct: "yes go grudge on the tribe that captured"; this replaces "the tribe he left", which never came up in the slice, and the "old tribe" example above). Any fighter can roll it, the Clarth too (Claude's call, then Jacob's yes on 13 Oct: "yes anyone can roll grudge"). Brute: 15% more damage. Tough: 15% more health. Sharp eye: +10 Hit. Quick: +10 Avoid. Steady: far likelier to rise than panic when his nerve snaps.
- Sets: 2 Clarth give +8 Avoid each, 2 Bloodridge 10% more damage each, 2 Skullpeak 10% more health each. 3 of a tribe also bring its doctrine to your side: the Clarth pick off the back row, Bloodridge rally when one falls, Skullpeak hunt the enemy's most hurt.
- Gear: each piece raises one stat, damage, health, Hit or Avoid. Pauper +5, Mid +10, Princess +15 (percent for damage and health, points for Hit and Avoid). A Princess piece also carries its chief's special: the Bloodridge chief's makes the wearer's kills break more nerve, the Skullpeak chief's halves big hits on the wearer.
- Claude's reading, said before the questions and not changed by Jacob: capture means their nerve breaks while the chief is still alive, and then his whole band from that fight joins, fallen ones too, with his order; the chief does not fight for you himself. Kill means he dies: no tribe, no order, his Princess piece. A won path fight banks its materials as before, and you can swap them for one piece of their gear before the next fight. Formation: on the squad page you set each fighter front or back.

Sprint 3 calls, 13 Oct 2026. Jacob picked Claude's call on each of these with his "go start sprint 3". They close the path map's shape. Numbers are starting values.
- Path map: each tribe's road is a small map you see whole before you start: 3 lanes and 3 rows of nodes, then the chief. Lanes cross. Every node shows what it is and what it drops. You plan a route, and the chief waits at the end. A lost fight leaves you where you stood, so you choose again from the same forks.
- Events: some map nodes are events, a short scene with 2 or 3 choices that use what already exists: a fighter, a piece of gear, materials. Taking an event means one fight less on that road. The first row is always fights. The event list and its text are Claude's placeholders; Jacob writes the story.
- Objectives: some fights carry one optional goal, shown on the node and on a banner in the fight: nobody of yours falls, win inside 40 seconds, or break them with 4 still standing. Win and meet it, and the drop goes up a tier (Pauper to Mid to Princess). Miss it and you only lose the bonus.
- Omens: at camp, before a run, you may pick omens. Each makes the run harder in one plain way: Blood Moon, their fighters hit 20% harder; Hungry Hunters, the hunters bring 2 more; Long Road, one more row on every road; Thin Ranks, the run starts with 4 fighters, not 5. Each omen picked adds 25% to the materials the run earns, and two or more raise every gear drop on the road a tier. The four omens and these numbers are Claude's placeholders.

Sprint 3 screen calls, 13 Oct 2026. Jacob kept Claude's calls on the road page with his "yes keep them": the nodes you can take next have gold corners; an event node shows "?" with the event's name on the map; the shrine's Pray says "price unknown", because what answers sets its price.

Fix all 9, 13 Oct 2026. Claude played four fights in review and reported nine problems; Jacob said "fix all 9", then "go". The calls inside the fixes:
- The tutorial is practice. Bloodridge bring their doctrine but none of their learned habits, so each order the coach asks for works, Unleash included. Nobody learns from it, them or you.
- Unleash keeps its rule: a tribe that learned to Hold when you Unleash still stops a plain Unleash (the 11 Oct anti spam calls). Letting the scare land first broke that in the tests, so it was dropped. The fight says it instead: the Unleash plate warns "They will Hold" or "Blocked", a blocked Unleash is called out, and the banner gives the way through: parry their cast, then Unleash for a Riposte.
- Every road node forks: to its own lane and one beside it.
- The fight shows both nerves as bars and names each big swing (5% or more) with its cause, marks both leaders, times their cast with the parry moment marked, and tips fallen placeholder tokens over. The why box and the result's WHY list the swings that decided the fight. These screens are interim and kit only, for the cult HUD to restyle or replace. Numbers are starting values.

14 Oct 2026, after Claude's playtest for Jacob:
- A won fight never offers a piece you already hold, the same tribe, stat and tier; it moves on to the next stat (Jacob's "go" on Claude's four fixes).
- Sprints 4 and 5 are parked. The art placeholders get filled first (Jacob: "lets park sprints 4 and 5 for now and let populate all the placeholders"). The shaman and the duel champions get their own designs in a separate art pass, not borrowed class sprites (Jacob to Codex).

14 Oct 2026, who does what (Jacob: "acknowledge lock these constraints"). Jacob locked the split Codex set out in HANDOFF_LOG.md at 7914 and 7934:
- Claude owns the gameplay systems: authoritative combat, difficulty and balance, progression, rewards, persistence, the Sim and its tests.
- Codex owns the frontend: Slice UI, HUD, input and readability; environments, including Skullpeak's replacement stage; squad, equipment, camp, reward and results screens; binding approved character art and animation; scene wiring; the combined player build.
- The character options Claude made on 14 Oct (the shaman, both duel champions, the hunters and their captain) are vision, not final art. They go to Codex (Jacob: "handover the sprites yo made to codex and tell her that these are vision not final art"). Jacob picks one number per character; Codex makes the final art from his picks and binds it. Paid generation still needs his yes.
- This repair and review round finishes before Sprints 4 and 5 restart. It is finished when one normal player build shows representative Bloodridge and Skullpeak fights with real command choices and passes a look and readability review plus save and replay checks. Test counts alone do not settle feel or art.
- Claude's next gameplay job (7914): explain why parry then Riposte and Eclipse dominate and why fights feel easy, then propose the smallest fix that keeps the Perfect Hold and anti spam locks. Diagnosis and a proposal only: no silent number changes, no bot sweeps. Tuning still waits for Jacob's own play.
- Each agent takes requests in its own lane and settles hand overs in HANDOFF_LOG.md, so Jacob does not route work between them.

14 Oct 2026, the loose end (Jacob: "fix the loose end", then "go"): an event never gives a piece you already hold either, the same tribe, stat and tier; it moves on to the next stat, like a won fight's spoils.

14 Oct 2026, tribes learn your parry (Jacob: "yes go build the parry learning", after Claude's diagnosis that the parry was the one answer no tribe ever learned): parry a tribe's cast and it learns to wait out your Hold. Its caster holds the cast until your Hold is older than the parry window, so a perfect Hold only soaks: half the hit, no stagger, no Riposte. Fall Back still dodges and focus can still break the cast. The habit eases off a step after each fight where they cast and you never tried to parry. Perfect Hold still works against any tribe that has not learned it.

14 Oct 2026, the intro and title (Jacob: "lets fix the intro"): the four joke names (Kindred Freeze, Cheese, Bees, then Siege) are cut, too cringy for some players, Anu included; the logo comes up straight. The town title is replaced by the Sovereign's place seen from our ship above the planet, and Play dives from orbit into her place, then the game starts. This replaces the joke names in the 11 Oct intro call. Her place is "a camp of some sort where she lives" (Jacob, 11 Oct), on a planet covered in the wrecks of armies she beat.

15 Oct 2026, the hunters' look (Jacob): the Sovereign's hunters are goth looking reptilian beings like the dragonkin of World of Warcraft, with body armour and scales, a goatee that is really skin hanging down on either side of the chin, very good looking, smallish horns. One class always carries a war banner: the Warden. "Faceless" (12 Oct) means no names, not hidden faces; the masked hunter looks Claude made on 14 Oct misread it and are dropped.

15 Oct 2026, the title picture (Jacob picked B, then "go"): our leader, three quarters at the open bay of our ship, lit red by the Sovereign's planet below, his relic over his palm. The bay and the planet are drawn 10% bigger so he blocks less of them; he is not moved, shrunk or redrawn (moving him off the door and redrawing him smaller were both rejected). One still picture, no strip. The main screen he liked: the logo top left, COMMENCE as the play item, and the ship's deadpan readouts in green under the list: THREAT: LOW (EST.), ARMY: 2000, CONFIDENCE: 100%, PREVIOUS ARMIES: 0 (SCANNING). The dive from orbit into her place is still open: its storyboard frames were rejected ("rest all suck").

16 Oct 2026, the hunters' look, replacing the 15 Oct one (Jacob, over the 15 and 16 Oct rounds, then "go"): reptilians from another planet, never a copy of WoW's dragonkin or Dracthyr ("copyright violation waiting to happen"). A broad, flat, hooded manta ray head with manta fins at the mouth for the goatee; astral skin with glowing purple neon veins and a purple core in the chest; obsidian body augmentations; a top heavy build on solid legs; menacing, from a war torn, lawless space wasteland. The Warden carries a real, cosmic war banner. His picks, all PixelLab placeholders an artist redraws later (Art/PixelLab/Hunters/2026-10-15/): the Warden with obsidian crystals; the Skirmisher with a double ended obsidian blade staff and a slit visor; the Trapper with a harpoon launcher on a chain and tracking eyes across his hood; the Marksman with an energy rifle and a targeting lens; the Medic with vials, a healing drone and surgical arms; the hunt captain with a manta wing cape and a long glaive, drawn at chief height. All six are drawn in the hunt in the SW facing with idle, attack, hit and death.

## The rival and the Sovereign's army, owner decisions, 18 Oct 2026

Jacob after teaser take 7 (HANDOFF_LOG.md 8865 and 8923), then "go with 2 and update the locks". These replace every earlier line that puts the hunters on her side.

- The hunters are not the Sovereign's army. They are their own tribe, the 25th tribe in the game, and they are the rival. Jacob: "they are 25 th tribe in game which are procedurally increasing in difficulty as the game goes by thats the rival and rivalry engine surrounding it". The rivalry engine (30 Sep, above) is built around them.
- Every "Sovereign's hunters", "Alpha hunters" and "Alpha hunt" above now means the hunters, the rival tribe. Only their allegiance changes: the hunt after a run's last chief (11 Oct, J2), how they arrive knowing your tricks, the mercenary company (12 and 13 Oct), Sprint 4's rival gate and their look (16 Oct) all stand. Their names changed later the same day (next section). The 1 and 3 Oct wording that made the Bloodridge chief the rival is replaced.
- The hunters get harder as the game goes on. Their strength carries over between runs and keeps climbing (Jacob's "yes" to Claude's call on 18 Oct). This replaces "It resets with the run" (10 Oct) and "They come back the same every run" (11 Oct). Later the same day Jacob also said they learn across runs, and how they climb was designed (next section); its numbers wait for his play.
- The tribe count is 25 (condition 1, updated above). Older lines that say 24 tribes now read 25.
- The Sovereign's own tribe, the Alpha tribe of tier 7, is the 57. Jacob: fierce light warriors whose planet an interdimensional dark surge imploded. Only the ones in pods far away survived, exactly 57. The dark energy took over these light beings, turned them rogue and corrupts them from inside. Lord Boros of One Punch Man is the reference for how they feel, a reference and not a design to copy, as with her on 25 Sep. She saved the 57 by teleporting them to this war torn planet and made them strong through suffering, so they are loyal to her to the core. Claude reads his "to be this my porting" as teleporting; that word is not confirmed.
- Their look, picked by Jacob on 18 Oct ("go with 2"): option 2 of round 1, `Art/PixelLab/Sovereign/2026-10-18/round1/option-2-glass.png`. A pale white, fierce alien face with a high crest and no hair; a dark storm inside the body that shows through the skin as branching white light veins; spiked gold conqueror armour; torn white cloth; weapons of solid white light. Jacob left the skin to Claude. This picks the race's look only; each class, the turns, animation and binding are separate approvals, and paid generation needs his yes.
- Still open: what her place looks like beyond "a camp of some sort where she lives" (11 Oct); the 57's classes and how they fight; renaming the hunt in code and on screen (AlphaHunt and the rest).

## The hunters' names, learning and ambushes, owner decisions, 18 Oct 2026, later

Jacob said "go, build the rivals with 16 traits in the three stages" to Claude's plan for the hunters as mercenaries like those of Assassin's Creed Odyssey (HANDOFF_LOG.md 8981), which Claude had said it would take as yes to the plan's three calls, then "yes, update DESIGN_LOCKS so the hunters have names, learn across runs and ambush on the road". These replace the 12 Oct "The Sovereign's hunters stay faceless: no names", the 13 Oct "They stay faceless" and "They do not learn between runs", and the same points in the section above.

- Names: every hunter has a name and a title, like Odyssey's mercenaries, for example "Vessk, Twice Paid". The company's other fighters stay nameless. The lists are Claude's placeholders in HunterCompany.cs.
- They learn across runs: a hunter who beats you climbs the ladder, gets stronger and learns a trait against the order you gave most in that fight, and keeps it from run to run until you kill them. The whole company also gets stronger with every run.
- They ambush you on the road: from bounty 2, some rows of a tribe's road hold a hunter on one fight node, more rows the higher the bounty, never more than one a row, so you can go round them. The hunt after the run's last chief stays as the finale.
- They come with the rest of that plan: a bounty from 1 to 5 each run, raised by beating chiefs and stripping gear; a ladder of 10 named hunters with you on it, where beating one moves you up and a hunter you kill is replaced; and 16 traits, four for each of your four buttons, plus one weakness each.
- Their habits (Hold when you Unleash, cover whoever you focus, wait out your parry) still do not change; what they learn comes as traits.
- Every number is a starting value that waits for Jacob's play. Still open: renaming the hunt in code and on screen.

## The Clarth chief spell, owner decision, 18 Oct 2026

Jacob, 18 Oct: "since clarth doesnt have any spell and moreover till eclipse is gained there is no chief spell make a clarth spell and make them inherit this".

- The Clarth get a chief spell of their own. Until now they had none, so your 5th button stayed empty until you captured the Bloodridge chief and his Eclipse.
- Claude's reading, not confirmed word for word: the Clarth you command from the tutorial bring their spell with them, so your 5th button holds it from the first fight. When you capture a chief, his spell takes the 5th button, as it does today. Like every chief spell it is a question that one of the four order buttons answers, and it gets a cut in like the Eclipse and Steel Storm.
- Which spell: three options went to Jacob on 18 Oct. His pick is pending.
- Picked later on 18 Oct: Dust Shark. Jacob liked the Sand Shark option and asked for a hybrid ("do a hybrid i like the shark one"), then picked "2 Dust Shark". The tell (3 s, the Eclipse's windup slot): red sand whirls round the caster's fighters while the shark circles under it, and the enemy's blows mostly miss. Then the sand drops and the shark bites the enemy's back row, one big hit on their healers and shooters. Answered by Fall Back, which dodges the bite, so its cut in carries Fall Back's colour and icon. How much the sand makes them miss and how hard the bite lands are starting values for Claude to set. Not built yet.
- Built in the sim later on 18 Oct (HANDOFF_LOG.md has the details). Starting values: during the 3 s tell the enemy's blows on your side land 40 points less often (never below the 40% floor), then the bite hits each fighter in their back row for 15% of their health (grade 1, a captured chief's order is grade 2) with the Eclipse's nerve cost. Not felt in a build yet.
- Tuned later on 18 Oct (Jacob: "think big", then "tune the existing ones not rewrite the whole thing", then "go, tune the chief spells to those numbers"): chief spells must be a real threat, so every row hitting spell doubles, 30/40/50% of each target's health by grade instead of 15/20/25, with a 350 nerve hit instead of 200. The Dust Shark's bite is now 30% at grade 1. Steel Storm's wall lets 10% of blows through instead of 30, stands 8 s instead of 6, and smashing it costs 300 nerve instead of 120. The answers do not change. Starting values.

## The two new tribes, owner decisions, 18 Oct 2026, later

Jacob turned down Claude's six tribe options ("no bro") and gave his own two, then picked how each fights ("Cave -2", "Sea - 6"), then said "go, build both tribes as planned and put them on the road now with placeholder figures". Each of the first two tiers now holds two tribes, and a run meets one of each at random (his "random tribe per tier", the same day).

- Tier 1, beside Bloodridge: a cave species. Jacob: "ones that look like shaak Ti and plo koon fused together they are species with blood rituals as a main agenda they are cave dwellers and are like blood trolls characteristics from WOW". Shaak Ti, Plo Koon and World of Warcraft's blood trolls are references to mix into an original race, never to copy (the 3 Oct rule above, and his 16 Oct "copyright violation waiting to happen").
- How the cave tribe fights, his pick: bloodletting. Their blades leave wounds that keep bleeding. Their chief's blood call rips the blood out of every bleeding fighter of yours at once, unless you Fall Back.
- Tier 2, beside Skullpeak: an ancient Nautolan tribe that lives underwater. Jacob: "they are very clever and made use of underwater machinery and dna alternations to make them a hybrid a fishmen to be precise but however they are very inferior to non underwater lviving beings and see them all as a eradication". Claude's reading, said in chat and not corrected: they see everyone who does not live underwater as beneath them and want them all wiped out. Nautolans are a reference to mix into an original race, like the others.
- How the sea tribe fights, his pick: the flood. They flood the fight, so your fighters slow down and lose nerve while theirs get stronger. Their chief's tide drowns your back row unless you Fall Back. Claude's addition, offered with the options and kept in the plan he said go to: they never take prisoners, so when they beat you, your most hurt fighter dies instead of being captured.
- Both chiefs' spells are answered by Fall Back; Claude said so before the go.
- Claude's placeholders, his to change: the names Stoneveil for the cave tribe and Thundercoast for the sea tribe, after two free map regions; their chiefs, name lists, squads, set bonuses, reactions, chief's gear pieces and every number.
- They are on the road now with labelled placeholder figures until their art exists. Their art is a separate approval: his references first, and paid generation needs his yes.

This replaces the 10 Oct line that the slice run is camp, Bloodridge, Skullpeak, then the hunters: a run now meets Bloodridge or the cave tribe, then Skullpeak or the sea tribe, then the hunters.

## The squad's spells, marks and class pieces, owner decisions, 20 Oct 2026

Jacob over 19 and 20 Oct, after Claude's research on combos, builds, drops and stages (`reports/Roguelike item combos drops and stages.md`): "All are okay a hybrid of all 3 wuld be super cool", "put all ten into the hybrid", then his class rule, then "yea okay to all" and "go" to the sprint plan. These replace the 13 Oct lines that any fighter can wear any piece and that a won fight offers one piece.

- The class rule, his words: "classes have auto attacks then have specific spells only they can use tank cant heal and heal cant dps". Items and upgrades grow a class's own role, as the class catalogue says. Nothing ever hands a class another class's spell.
- Each of the five has its auto attack plus one spell only it has. Warden: Guard. Marksman: Aimed Shot. Skirmisher: a melee strike. Trapper: barbed Snare. Medic: Treatment; she never attacks.
- Two points of the class catalogue (`CLASS_ROSTER.json`) change. The Skirmisher fights in melee and is the main damage, not a light ranged fighter. The Trapper's snare holds and also leaves a wound that bleeds.
- The marks. Each class's spell leaves a mark and another class's spell hits harder on it. Four marks: Snare, Bleed, Opening and Guard. Only the Trapper can snare. The Marksman's Aimed Shot crits a snared enemy. Claude's reading of the rest, his to change: the snare also leaves Bleed, which the Skirmisher's strike rips open so all the bleeding left lands at once; the Aimed Shot leaves an Opening, which the Skirmisher's strike hits twice as hard; the Warden's Guard covers the Medic while she treats, and a treatment under his Guard heals more (the 19 Oct pitch's Covered Treatment). Covering the ally he guards was tried first and never fired: he takes that ally's blows, so it is never the one she treats.
- Gear. Every piece belongs to one class and bends only that class's own spell: which mark it leaves, or how hard it cashes one in. After a won fight you strip one of three pieces they wore, or bank. A chief you kill still gives his own piece.
- Pieces are not drawn on the sprite. Each one shows as an effect in the fight and an icon on the fighter's card.
- The hybrid, all ten, as he had them explained:
  1. Status web: the marks above.
  2. Trophy pieces: a beast drops one part for each part you break, and the Butcher forges one part into one class piece. No fusing two parts.
  3. Tribe stacking: inside one run only, never grinding. Pieces come from what you strip and who joins you; two or three from one tribe switch on its bonus.
  4. Kin bonds: two fighters who land a combo on the same enemy three times in one fight are bonded for the run. A bonded Marksman fires at whatever his Trapper snares without aiming.
  5. Pal duos: once you hold favours from two Pals, their duo favour can show up, about once or twice a run. Higher risk, higher reward: a favour still costs that fight's materials.
  6. Formation: some pieces help whoever stands next to the wearer. Fall Back splits the lines and the bonus drops until they are back together.
  7. Road scars: a fighter earns a trait by what they do on the road, and it powers their own spell. The Warden who breaks the shark's jaw on his shield becomes Jawbreaker, and his Guard holds better against beasts.
  8. Blood pacts: the strongest pieces carry a cost written in words.
  9. Order sockets: an item slots into one of your four orders and changes what it does.
  10. Road jobs: an errand, never a class change. You send one fighter away for a node or two and fight without them; they come back with a beast part, a look at the next node, or the chief starting with less nerve.
- The stages read what you carry. A beast. An envoy: a war party stands aside or pays tribute if you carry the right tribe or trophy. An event with one extra choice for a mark, tribe or trophy you carry. The hunters learn your most used mark and shrug it off, and the fight card warns you first.
- Beasts: each region's road gets a beast boss node you choose to take. The chief is still the boss at the road's end. The Broken Wastes' beast is the sand shark. In Claude's scene its jaw, fin, spine and tail are targets with their own health, and the Clarth's shark pieces (3 Oct) are the class pieces they turn into.
- The order. Sprint 1 is the fight: these spells, the marks, class pieces and strip one of three. Sprint 2 is the shark: the beast node, its parts and pieces, road scars and its art. Sprint 3 is the road: envoys, road jobs, events, Pal duos, blood pacts, order sockets, formation, bonds, the hunters learning your favourite mark and codex pages with blanks and hints. He plays after each sprint before the next one starts. Codex builds each sprint's screens.
- Every number is a starting value that waits for his play.

Later on 20 Oct, after Sprint 1's bad news (Jacob: "fix em", then his answers to Claude's questions, then "go"):
- Enemy fighters really wear the pieces a win strips: up to three, one for each class among them, each on the first of theirs of that class, with its stat and its bend. A duel's champion wears one, so a duel offers one. A fight whose win drops no gear (a favour fight, a chief, an ambush, the hunt, the tutorial) dresses nobody.
- An event's piece is always for a class you have.
- A broken aim comes back after 2 seconds, not 5 (his pick). Tier 2 stays as it is until he plays, and the Warden keeps covering the Medic (his picks).
- Chiefs: he called yield, bodyguards and a second phase the basics that should already be in the game, and said the game needs more than them. (The kneel part of this is replaced by the next section.) Built as the base: at half health a chief's second phase brings two more of his tribe into the fight and he casts his spell again; at a quarter health he kneels, and while your side holds your fighters spare him, so his nerve breaks and you capture him, or you keep hitting him and he dies; while his band stands his Wardens guard him, his Medic treats him first, and snares and Openings do not take on him. Deeper chief fights are still to design.

## Chief fights, owner decision, 20 Oct 2026, later

Jacob over one long talk on 20 Oct, after Claude's boss research (`reports/Boss fight depth for chiefs.md`). He turned down a 3 second kneel window ("very punishing"), chiefs who use your own four orders back at you ("copy paste of the same formula"), and bosses written as one key for one lock ("you can only kill a boss linearly"). His rule: "Boss fights, everything should be different. And how you tackle that is all the gameplay." The kit you were given (the orders, the class spells, the marks, the 5th button) is how you deal with what each boss does. He called Claude's Bloodridge chief, written like a raid boss page, "perfect", asked for it "more flashy and addictive", then for a lean on tactical games, then said the flashy page is the core, the tactical part is a separate "tactical value giver", and "lock this direction".

- A chief fight is the chief's own moveset: several moves, each on its own timer, each with its own look, sound and cue, each with more than one answer that pays differently. The moves overlap, so you choose what to answer and what to take. Every chief gets a different kit, never the same moves renamed.
- The model is the Bloodridge chief. Cleave every 6 s: his blade glows for 1 s, then a heavy hit on whoever stands across from him; a Hold as it falls parries it (2 points and a stagger), his target's Warden Guard takes it, or you take it. Brand every 15 s: a burning sigil over one of your fighters with a 4 s ring, then it bursts on that fighter and those beside them; Fall Back, a Warden covering them, or the Medic healing through. Eclipse every 25 s: his cut in, the sky darkens, 3 s, your front row glows; a perfect Hold parries it (no damage, he staggers, 2 points, Unleash after it is the Riposte), Fall Back dodges it (1 point), Focus Fire on him breaks the cast (1 point, your next order doubled), and dodged or not he is worn out for 4 s after it and takes double damage. At half health, Blood Sun: the sky stays red, his Cleave makes fighters bleed, Brand marks two, and every Eclipse after it hits 20% harder than the last, so stalling loses.
- Flash: every move has its own look and sound. A parry freezes the screen for a split second with a white flash, sparks and a PARRIED stamp. The Riposte plays half a second of slow motion. At half health the fight stops for 2 s while he roars. The kneel slows everything down, his weapon drops, and your Spare and Execute buttons pulse. One big moment at a time.
- Glory, the hook, like Devil May Cry's style rank: a big letter from D to S. Answers raise it, a perfect parry more than a dodge, mixing answers faster than repeating one, and eating a big move drops a rank. Higher Glory pays more points per answer, an empowered 5th button at S, and a grade card at the end with better spoils for a better grade.
- Each chief hides one perfect chain. Bloodridge's is Sunbreaker: parry two Cleaves then the Eclipse, all perfect, and your Warden throws the Eclipse back at him. His codex page keeps a blank until you find it.
- He talks at the big moments, and his kneel line changes if you have met him before. Each run he takes 3 of his 5 moves. His tribe remembers your last fight. Your best grade against him is kept on his codex page.
- Breaking him: every move you answer takes a chunk of his nerve. When it runs out he kneels and stays down, with no timer, and you choose. Hold spares him: he and his tribe join you with his spell. Focus Fire executes him: you take his piece, the strongest gear in the game. His Wardens step back while he kneels, so Execute is real. Kill him without breaking him and you take his piece with no choice. An executed chief is dead for that run only; the same chief leads his tribe next run.
- This replaces the 20 Oct base where a chief kneels at a quarter health and Hold spares him while his side bleeds 50 nerve a second. The second phase and the bodyguards stay as the base under every chief's own kit.
- The tactical part is its own system, added as the tiers climb, never all in one fight. Its pieces: seeing his next move, its target and how hard, 6 s early (Into the Breach, Slay the Spire); moves that hit places, not people, so your formation matters (Darkest Dungeon's ranks); a band with jobs, so kill order matters (a Drummer who speeds his timers, a Standard Bearer who holds his nerve up, a Brander who casts Brand; each kill costs something); a war bar his band's hits fill, which casts Eclipse when full (Advance Wars); a tax on order spam (Slay the Spire's Time Eater); a half health victim mark (Darkest Dungeon's Heart of Darkness); and a rolled strength and weakness on his card each run (XCOM 2's Chosen). The first chief gets one piece, later chiefs more.
- Only the Bloodridge chief's kit is written. Every other chief gets his own before he is built: Stoneveil the blood rite, Skullpeak a slow grind behind walls, Thundercoast the tide's clock. Every number is a starting value.
- Built in the sim on 21 Oct 2026 (Jacob: "go do the chief build"; HANDOFF_LOG.md has the details), as campaign rules version 3, so a fight saved before plays on as it began. Built: the kneel for every chief with Spare and Execute and his Wardens stepping back; Bloodridge's Cleave, Brand, Eclipse every 25 s and Blood Sun; Glory D to S with its points, the empowered 5th button at S and more materials for a better grade; Sunbreaker; his lines; his codex page (met him, best grade, the chain's blank); the move pool. Not built: the screens and the flash (Codex's side), any tactical piece (which one the first chief gets is Jacob's pick), and two more Bloodridge moves, so he takes all three written ones every run. His first kneel line, "Choose.", is Claude's placeholder for Jacob to write. Nobody has played it.

## The sand shark, owner decisions, 21 Oct 2026

Jacob: "yes, queue the shark API ... with your tail call", then "go, build the shark API with your six calls" to Claude's six calls. These fill in the 20 Oct lines on beasts, trophy pieces (point 2) and road scars (point 7) for Sprint 2.
- The shark has no nerve. Unleash and anything else that drains nerve do nothing to it, so you beat it by breaking parts.
- Four moves, one per part, each on its own timer with more than one answer, and breaking a part ends its move. Jaw, Bite: a heavy bite on whoever stands in front; Hold parries it; a Warden guarding them takes it and the jaw takes the hit back, so he can break the jaw on his shield. Tail, Sweep: the whole front row; Fall Back dodges it, a covering Warden halves it, the Medic heals through. Fin, Dive: it sinks, your blows mostly miss for 3 s, then it bursts up under your back row; Fall Back dodges it, a Trapper's snare holds it on top. Spine, Volley: spines rain on your back row; Fall Back dodges it, a covering Warden halves it.
- Break all four and it dies. If the fight clock runs out first, it dives away and you keep what you broke. Lose, and you keep nothing; your fallen stay down, and it captures nobody.
- Trophies come home with the materials, even if the run is lost. At camp the Butcher forges one into its piece once and for good, like the tabard, and you have it every run after.
- The tail: the jaw trophy forges into the jaw shield, the tooth net or the tooth totem staff (the teeth are in the jaw), the fin into the fin blades, the spine into the spine rifle, and the tail, the hardest part, into any one of the five.
- Whoever lands the blow that breaks a part earns its scar for the run, and it powers their own spell against beasts. Jawbreaker is Jacob's; Finripper, Spinesnapper and Tailcutter are placeholder names.
- No road in the run goes through the Broken Wastes, so the shark waits beside the first road of every run, which you can take or leave, until each tribe's land has its own beast.
- Claude's calls while building, his to change: it waits beside the road, not on a road node, so taking it costs no step and it is not one of the road's offers; you can hunt it any time before that road's chief, and after a loss you can try again; scars are earned win or lose; a forged piece costs the trophy only and goes on the first fighter of its class each run; the pieces are Princess class pieces with set bends (Jaw Shield Bastion, Spine Rifle Deadeye, Fin Blades Bloodrush, Tooth Net Barbs, Tooth Totem Staff Surgeon); a scar makes its fighter's spell work a quarter better against a beast; a won hunt sends home a road fight's materials. Every number is a starting value.
- Built in the sim on 21 Oct 2026 as campaign rules version 4 (HANDOFF_LOG.md has the details). Not built: the screens, the shark's art (it needs Jacob's references) and any other region's beast. Nobody has played it. At default strength the bot kills it in under 50 s with nobody falling, because its Warden guards every Bite and its Trapper's trap catches every Dive; tuning waits for his play.
- Its phases, Jacob's "go, fix the audit findings and add both phases" on 22 Oct to Claude's calls: Blood frenzy when its second part breaks (it roars, the one big moment, like a chief at half health; its remaining moves come a third faster, and every hit it lands leaves its victim bleeding); Last stand when its third part breaks (its last part moves twice as fast and it starts digging down: break that part within 15 s and it dies with all four trophies, or it dives away and you keep three, the greed choice).
- Fixed with them: its moves never fall on your leader while any other fighter stands, and on him once nobody else does, so a lone leader can lose; the Sweep has the three answers above and no Hold soak; a Hold given while paused never parries the Bite. Built in the sim on 22 Oct 2026; nobody has played it, and it is still far too easy at default strength.

## The Bloodridge chief finished and the Skullpeak chief's kit, owner decision, 22 Oct 2026

Jacob: "okay fix chief we go do the one for skullpeak", then "yes, lock both as written" to Claude's calls. This settles the Bloodridge chief's open bits in "Chief fights" above (his two more moves, his tactical piece, his first kneel line) and writes the Skullpeak chief's kit. Every number is a starting value. Built in the sim on 22 Oct 2026 as campaign rules version 5 (Jacob: "go, build the Skullpeak chief and the Bloodridge extras in the sim and hand the API to Codex"); HANDOFF_LOG.md has the details and Claude's calls while building. Not built: the screens and the art. Nobody has played it.

Bloodridge, finished:
- Move 4, Burning Ring, every 12 s: he spins his ring for 1.5 s, then throws it down one lane, through both your rows. Focus Fire him while it spins and your Marksman's shot knocks it out of his hand; Fall Back gets everyone clear; or you eat it.
- Move 5, Sun Mantle, every 18 s: fire wraps him for 5 s and every blow on him burns whoever struck it. Switch your Focus Fire to his band, Fall Back so your blades stop, or you eat it.
- With these his pool is five, and each run he takes three.
- His tactical piece: you see his next move, its target and how hard, 6 s early. He is the first chief, so it teaches you to read moves.
- His first kneel line: "Finish it." (Jacob's to rewrite). Met before, it stays "You again."

The Skullpeak chief, a slow grind behind walls. She fights from behind a steel shield line, her rifles pick off your hurt fighters, and her band works like a machine; you win by cracking her walls at the right moment, not by hitting harder. Each run she takes 3 of these 5:
1. Steel Storm, every 30 s: her Wardens slam their shields together over 2 s; the wall stands 8 s and your blows do a tenth. Unleash smashes it, a big chunk of her nerve; your Trapper snares a Warden while they lock, so it only goes up half; or you wait it out.
2. Hook, every 9 s: a chain glints at one of your back row for 1 s, then yanks them in front of her shields for 3 s, where every blow on them lands. Hold as it bites and they dig in, the chain snaps and her Trapper staggers; your Warden's Guard on them anchors them; or they get dragged.
3. Rifle Line, every 15 s: three aim lines lock onto your most hurt fighter for 2 s, then a volley. Focus Fire a shooter and each one hit loses his shot; Fall Back takes everyone out of sight; or your Medic tops them up first.
4. Iron Advance, every 20 s: the shield line marches for 4 s and crushes your front row each second. Hold braces against it but costs your own nerve; Unleash into the march staggers the line; Fall Back gives ground, so no damage, but her side gets nerve back.
5. Overclock, every 25 s: her Medic jabs her band for 1.5 s, then they act twice as fast for 6 s. Focus Fire her Medic during the jab; Hold through the flurry; or your Trapper snares one of them.
- At half health, Lockdown: she steps behind a wall that never drops. Your blows only land in the 6 s after an Unleash smashes it, and her Wardens rebuild it every 12 s. The last half of the fight is your points against her walls.
- Where it gets hard: at 0:30 Rifle Line locks onto your Medic while her line advances; Fall Back saves the Medic but gives ground, Hold braces the front but the volley lands. After Lockdown every Unleash buys a window, but Hook still wants a Hold, so spending your points wrong leaves you outside her wall with none left.
- Your squad changes the calls: Trappers keep her walls half built, Marksmen break her aims, Wardens anchor the hooks.
- Her hidden chain, Breach: smash two walls with Unleash in their first second, then Hold as a Hook bites. Your Skirmisher runs the chain back and takes out her steel leg; she is down 4 s and cannot Hook again that fight. Her codex page keeps a blank until you find it.
- Her tactical piece, since she is tier 2: her band has jobs. Kill her Shieldbearer and her walls come up slower; kill her Spotter and Rifle Line takes longer to aim. Kill order matters.
- Already built for her: the kneel, Spare and Execute, Glory, her second phase and her bodyguards. She talks at the same big moments as Bloodridge; the words are Jacob's.

## The cage bear, Skullpeak's beast, owner decision, 22 Oct 2026

Jacob: "nice this is fine now do for skullpeak", then "no bro they are cyborg creatures right they should feel the same ?" (Skullpeak's beast shares their cyborg look: warm flesh, polished steel bones grown through, a steel crest, turquoise seams, one bionic limb, never scrap bolted on), then "cage bear bro looks and feels awesome", then "go, build the Cage Bear as planned" to Claude's plan. Every number is a starting value.
- It waits beside Skullpeak's road the way the shark waits beside the first road: not one of the road's offers, it costs no step, you can hunt it before Skullpeak's chief or never, and after a loss you can try again.
- A huge cave bear with warm fur, a polished steel ribcage grown through its chest and one giant bionic claw arm. Four parts: its claw, maw and ribcage stand in front, its heart behind them. It has no nerve.
- Ribcage: no move of its own. While it stands every hit on the bear lands at half and the heart cannot be hit at all; while your Unleash runs, hits on the ribcage land twice as hard as that.
- Heart: break it and the bear drops dead on the spot, and only the parts you broke come home as trophies. Crack the cage and go straight for the heart for a fast kill, or break the claw and the maw first for all four.
- Claw, Grab, every 9 s: it reaches for whoever stands across from it. Fall Back gets clear, a Warden covering them blocks it on his shield, or it grabs them and holds them out of the fight, crushing them, until you break the claw or they fall.
- Maw, Roar, every 15 s: a big hit to your nerve. A Hold blocks it; Focus Fire on the maw while it roars cuts it off.
- Heart, Pulse, every 12 s: while the cage stands it mends the bear's other parts; once the cage is broken it hits your whole side instead. A Hold soaks it, and the Medic heals through it.
- Overdrive when its second part breaks: its moves come a third faster and its grip crushes twice as hard. Last stand when only its heart is left: it pulses every 4 s until you break the heart or fall. It never runs, so it is a race.
- Its trophies, which the Butcher forges into Skullpeak's bionic pieces: the claw into the claw arm, the blade arm or the hook chain; the ribcage into the tower shield; the maw into the rifle arm; the heart into the injector vials, or any one of the six.
- Claude's calls while building, his to change: when the fight's clock runs out it holds its ground and you lose; a Warden it reaches for is grabbed himself unless another Warden covers him; a fighter in the claw does nothing, nothing of the bear's but the crush reaches them, and nobody heals or covers them; the claw holds one at a time; a Hold that blocks the Roar earns no point, cutting the Roar off earns one like breaking a cast; nothing dodges the Pulse; the pieces are Skullpeak Princess class pieces with the other bend of each class from the shark's (Claw Arm Retaliator, Tower Shield Bastion, Blade Arm Opportunist, Hook Chain Long Snare, Rifle Arm Quickdraw, Injector Vials Field Kit); its scars are Clawcutter, Roarbreaker, Cagecracker and Heartstopper, placeholder names.
- Built in the sim on 22 Oct 2026 under the shark's campaign rules version 4, and the shark plays exactly as before (HANDOFF_LOG.md has the details). Not built: the screens, the bear's art (it needs Jacob's references) and the beasts of the other tribes' lands. Nobody has played it. In one scratch fight a default squad giving no orders lost to it at 47 s: the claw grabbed the Warden at 9 s and crushed him over 24 s, the heart mended what was hit, and its third Roar broke their nerve with nothing broken. Tuning waits for his play.

## The hunters' boss, the hunt, owner decision, 22 Oct 2026

Jacob: "build hunter boss now paper only go ill see if we need to make changes". Claude's first page made the hunt captain a rival duelist who read your answers, with mirror copies of himself, a three cut glaive combo and an escape to come back. Jacob: "you made him exactly like dmc 3 boss" (Claude read it as Vergil). It is dropped, and none of it comes back. Claude's second page, the hunt below, is built from real hunting (tracking, driving the game, nets, the kill), and Claude found no game boss built this way. Jacob: "yea go i like it". Built in the sim on 22 Oct 2026 as campaign rules version 6 (Jacob: "yes go"); HANDOFF_LOG.md has the details and Claude's calls while building. Not built: the screens and the art. Nobody has played it. Every number is a starting value.
- The fight at the end of a run against the hunters is the hunt they run on one of your fighters, step by step, and you break it before it reaches the kill. A bar of four steps shows over the prey, close to the war bar piece in "Chief fights". Each step runs on unless you break it.
- Track, 5 s: his Trapper with the tracking eyes marks your most hurt fighter. Break it: your Medic heals them over half, so the scent goes cold, or your Warden covers them.
- Drive, 4 s: his front row pushes in, and his blows on the prey's row land a third harder. Break it: Hold the line, or Focus Fire one of his front row.
- Corner: a purple line glows for 2 s, then his Trapper nets the prey. They are held, cannot dodge, and his Marksmen's shots crit them. Break it: Focus Fire his Trapper while the line glows, your Medic cuts them free, or Unleash tears the net.
- The Kill, 1.5 s: the captain's glaive comes down on the prey, a huge hit. Break it: a timed Hold parries it, or your Warden steps in and takes it.
- Break any step and the hunt starts again at Track, on a new prey. Every break is an answer: Glory, and a chunk of his nerve. Miss all four and the prey probably dies.
- At half health, the Pack: two hunts at once, on two of your fighters.
- His hidden chain, Turned Hunt: break all four steps of one hunt with four different answers, and for 6 s you hunt him: his band cannot cover him, and your blows on him land double.
- The end: break their nerve and the run is won. They never join. Kill the captain in the fight and he is gone from the ladder for good, as the ladder already works.
- Kept from what is built: his card (the named hunter's traits and weakness from the ladder), the hunters' habits, and Glory.

## The Stoneveil and Thundercoast chiefs, owner decision, 22 Oct 2026

Jacob: "bro in that case we have 2 more bosses for you to do api for", then "yes, lock both kits as written" to Claude's two pages and two calls. This writes the last two chief kits named in "Chief fights" above: Stoneveil the blood rite, Thundercoast the tide's clock. Claude searched for famous look-alikes first and kept clear of the two closest it found: Darkest Dungeon's blood ritual, where one of your fighters gets turned against you, and the rising water chase levels. Built in the sim on 22 Oct 2026 as campaign rules version 7 (Jacob: "go, build the Stoneveil and Thundercoast chief kits in the sim and hand the API to Codex"); HANDOFF_LOG.md has the details and Claude's calls while building. Not built: the screens and the art. Nobody has played them. Every number is a starting value.

The Stoneveil chief, the blood rite. The blood priest of the cave tribe, a Medic. His band's blades leave wounds that keep bleeding, and his rite turns your blood into his power. You win by keeping your fighters from bleeding, or by breaking the rite, not by trading hits. Each run he takes 3 of these 5:
1. Blood Call, every 20 s, his spell: after a 3 s chant it rips the blood out of every bleeding fighter of yours at once. Fall Back gets clear; your Medic stitches wounds during the chant, and each treatment ends one fighter's bleeding, so there is less to rip; or you eat it.
2. The Basin, every 15 s: his acolyte carries a stone basin forward for 4 s, and it fills from every bleeding fighter of yours. If it fills, he drinks it: he heals a fifth, and his band hits a quarter harder for 6 s. Focus Fire the acolyte and it spills, Unleash kicks it over, or have nobody bleeding so it stays empty.
3. Leeches, every 12 s: cave leeches land on one of your back row. They drink 3% of that fighter's health a second for 6 s and feed it to his most hurt. Your Medic plucks them off, Fall Back shakes them loose, or you eat it.
4. Open Veins, every 18 s: for 5 s every wound of yours bleeds twice as fast. A Hold presses the wounds and halves the bleeding while it runs; your Medic stitches; or you eat it.
5. Transfusion, every 16 s: he channels for 3 s, drawing blood out of your most hurt fighter into his band. Focus Fire on him breaks the channel, your Warden covering them blocks the draw, or you eat it.
- His tactical piece, Blood Price, the tax on order spam (Slay the Spire's Time Eater) from the list in "Chief fights": every 6th order you give feeds his rite, and his next move comes at once. His card shows six drops. It is the piece that makes spamming one button lose.
- At half health, the Red Rite: the cave runs red. Your wounds bleed twice as long, and every Blood Call heals him for what it rips out.
- His hidden chain, Clean Hands: have nobody of yours bleeding when three of his moves land in a row. The rite fails: he is stunned 4 s, and his band's blades stop leaving wounds for the rest of the fight.
- Where it gets hard: Open Veins doubles your bleeding just as the Basin comes forward. Your Medic stitches one at a time, and the Basin fills from whoever she misses. Unleash the Basin and Open Veins runs on. Every order you spend brings his next move sooner.
- Your squad changes the calls: Medics are the heroes here, Wardens block the Transfusion, Skirmishers reach the acolyte, and a squad that bleeds less starves the rite.

The Thundercoast chief, the tide's clock. The sea tribe's chief, a Warden, and their machinery. The water already rises in their fights (the flood in "The two new tribes"); with him it rises and falls on a clock you can see, like a tide table. At high water his fishmen are at their strongest. At low water they are stranded: they act slower and hit lighter, and that is your window. His moves bend the clock his way, and your answers bend it back. Each run he takes 3 of these 5:
1. Tide, every 25 s, his spell: it drowns your back row, and it hits harder the higher the water. Fall Back gets clear. At low water it barely lands.
2. Sluice Gate, every 15 s: his engineer, his band's Trapper, cranks a gate for 2 s, then the water jumps a level and the next low water comes late. Focus Fire the engineer while it grinds and it jams, Unleash slams it shut, or the water rises.
3. Riptide, every 18 s: for 5 s the current runs out, and anyone of yours who falls back is pulled into the deep, hurt and doing nothing for 2 s. Hold and you stand fast, or just do not Fall Back while it runs. It is the trap for the button that answers his Tide.
4. Tidal Bore, every 20 s: a wall of water runs across your formation from one side to the other over 2 s, both rows, lane by lane, hitting and knocking down each fighter it reaches. A Warden on the side it comes from breaks it there, so those past him are spared; Fall Back gets everyone clear; a Hold soaks it.
5. Storm Surge, every 30 s: he sounds a horn for 3 s, then the water jumps to its highest for 8 s. Focus Fire him during the horn and the call breaks, Hold through it to halve the nerve you lose, or you eat it.
- His tactical piece, moves that hit places, not people (Darkest Dungeon's ranks) from the list in "Chief fights": the Tide hits your back row, the Bore runs lane by lane, and the water hits everyone, so where each fighter stands decides who gets hit.
- At half health, Spring Tide: the clock runs faster and higher. The water never drops below the first level, and his Tide drowns both rows.
- His hidden chain, Turn of the Tide: answer three of his moves in a row while the water stands at its highest. The tide turns: it drains to nothing for 10 s and his fishmen are stranded.
- Where it gets hard: at high water a Tide comes for your back row while a Riptide runs. Fall Back saves your back row from the Tide, but the Riptide drags the ones who ran. Hold keeps them, but the Tide lands.
- Your squad changes the calls: Wardens break the Bore on the side it comes from, Marksmen and Trappers reach his engineer, and a squad built to push hard wins the low water windows.

Both talk at the same big moments as Bloodridge; the words are Jacob's. Already built for both: the kneel, Spare and Execute, Glory, the second phase and the bodyguards.

## Beast concept briefs, owner decision, 23 Oct 2026

Jacob: "go, briefs approved, make the reference and skeleton pages", to Claude's two briefs, written after his 22 Oct call that concept art goes in rounds (the concept-art skill). These are the briefs the art is judged against. The fights stay as built.

Sand shark (Broken Wastes):
- Words: hunger under the sand. Theme: the Clarth's oldest prey, scarred by generations of hunts; their whole kit is made from it.
- Job: the first beast of every run, beside the first road; fast and easy to read; the player should feel the ground is not safe.
- Every power on a body part: Bite, an oversized jaw with rows of teeth (the jaw shield, tooth net and tooth staff come from it); Dive, a flat wedge head and smooth scales that shed sand, with only the fin and spine row above the sand as the warning, and it feels footsteps through the sand; Volley, hollow spines along its back that it flexes and fires (the spine rifle); Sweep, a heavy tail with a sickle fin.
- Built from a shark skeleton, the sandfish skink, the spiny dogfish's back spines and the thresher shark's tail. Long, low and flat; triangles moving in curves. A dusty red back darker than the sand floor, a pale belly, the one hot colour on the maw and spine tips. About 2.5 Wardens long.
- Steer clear of Monster Hunter's Cephadrome and Balahara, Breath of the Wild's Molduga, the Tremors worms and the Dune sandworms.

Cage bear (Skullpeak):
- Words: a great beast its own steel is killing. Theme: what Skullpeak's steel does to an animal that never chose it; sorrow, not cool armour.
- Job: Skullpeak's beast beside their road; slow and heavy; dread, then pity when it falls.
- Every power on a body part: Grab, one foreleg's bones grown into a giant steel grappling claw like the Skullpeak Warden's claw arm, the other paws still animal; Cage, its own ribs and breastbone pushed out through the fur as steel bars, which halve your hits and hide the heart; Roar, the cage rings round its chest like a drum, so the roar shakes your nerve; Pulse, the heart beats turquoise like Skullpeak's seams, mending it while caged and lashing out once the cage breaks.
- Built from a cave bear skeleton (huge skull and hump) and a grizzly's stance, with steel only where bone already sits (ribs, breastbone, forearm, claws, skull crest); the one huge arm makes it lopsided, so it leans on its other side. One heavy block, triangle claws and one circle, the heart, the only glowing round shape and the weak point. Warm brown fur, polished steel mirroring the cold light, turquoise only on the heart and seams. About two Wardens tall.
- Steer clear of Horizon's Fireclaw, Doom's Cyber Mancubus, robot bears, bolted on armour plates and a metal bear suit look when it stands.

## The parry at the center, owner decision, 24 Oct 2026

Jacob over 23 and 24 Oct, after Codex's audit warned that the learning rules counter whatever works for the player and set the pass as "players recognise what changed and can describe a new opportunity it creates": "yes, redo the Gate 4 paper so every lesson opens a new play, and rework the hunter traits the same way"; on showing the rivalry, "the beauty of nemesis or mgs is you can actually see the visual feedback ... dont mimic but something new like a hybrid"; then "parry is the ultimate move it negates anything and everything like sekiro you cant make it a side mechanic bro everything should be intertwined between them", "yes, put the parry at the center of every boss and redo the Gate 4 paper around it", "yes, add the duel for nerve and the miss penalty to the paper", "yes, add the combo cues to the paper as part 4", "yes to all five calls, lock them", and "go, lock all five calls into DESIGN_LOCKS and the log". Every number is a starting value. Nothing here is built yet.

**1. The parry, for every boss**
- Every move with a tell has a beat, the moment it lands. Hold in the last half second before the beat and you parry.
- Every beat is a duel for nerve:
  - Win it, with a parry: his move does nothing at all, whoever did it staggers for 2 s, and his side's nerve cracks harder than from anything else. Parries in a row crack it harder each time, and Glory no longer halves a second parry in a row.
  - Lose it, too early: your Hold was up but not in the last half second. You only soak, and your side's nerve cracks as hard as the parry would have cracked his. This covers a Hold put up early to brace or soak (the march, the Riptide, the Bore), the way blocking costs posture in Sekiro.
  - Lose it, too late or with no Hold: his move lands in full. Your Glory drops a rank and your parry chain resets.
  - Skip it, with a safe answer: you are safe, but his nerve cracks only a quarter as much as from a parry.
- Risk: the Hold's point and the hit itself; miss a few beats and your fighters start to snap. Reward: points come back, Glory climbs, and a chain of parries breaks his nerve while he stands, which is the catch. Your punish window is the 2 s stagger: an Unleash is the Riposte, and Focus Fire on the staggered one lands in full because nobody can cover him while he reels.
- Every answer each kit has today stays, as the safe choice. Fall Back is the dodge when you cannot time it.
- Nothing is "cannot be parried" any more: fire, Unstoppable and a berserk band all become parryable.
- A Hold given while paused never parries, now in every fight.

**2. Every boss move's beat** ("new" marks a moment the sim does not have yet)
- Bloodridge: Eclipse, the blast. Cleave, the fall. Brand, the burst. Burning Ring, the landing. Sun Mantle, the flare as it lights (new); a parry snuffs it.
- Skullpeak: Steel Storm, the shields locking; a parry drops the wall before it stands. Hook, as today. Rifle Line, the volley. Iron Advance, any crush; one parry breaks the march. Overclock, the needle; a parry and no flurry comes.
- Stoneveil: Blood Call, the rip. Basin, the drink; a parry spills it. Leeches, the leap onto your fighter (new). Open Veins, the cut (new). Transfusion, the latch (new).
- Thundercoast: Tide, the crash. Sluice Gate, the slam; a parry jams it. Riptide, the yank 2 s in (new). Tidal Bore, the first lane it hits; a parry breaks it there. Storm Surge, the horn's last blast; a parry and the surge never comes.
- The hunt: Track, the mark (new). Drive, the shove (new). Corner, the net throw. Kill, as today. The captain's cast, the release.
- Sand shark: Bite, as today. Dive, the eruption. Volley, the spines. Sweep, the tail.
- Cage bear: Grab, the snatch. Crush, any squeeze; one parry breaks the grip. Roar, its peak. Pulse, the beat.
- Everyone else: heavy swings as today; Firebomb, War Cry and Dust Shark, the release.
- Hidden chains and second phases stay; parries count as answers in them.

**3. Gate 4: what rivals learn, around the parry**

The rule: whatever a rival learned to answer, the same button of yours now baits him into an opening. What rivals mostly learn is your timing.

Tribes:
- Waits out your parry (learned when you parried him): he holds his beat until your Hold goes stale, once per beat in every fight. The play is the feint: fake a Hold, then parry the real beat.
- Feints (new; learned when you parried 3 of his moves in one fight): he throws a fake beat first; the tell flashes and nothing comes. He is mid motion and open during the fake, so Focus Fire or Unleash him then.
- Covers whoever you focus (learned when your Focus Fire killed one of theirs): his Wardens jump to your target and stay there for the 5 s; a second Focus Fire in that time finds them stuck. Parry first, since a staggered fighter cannot be covered, or focus a decoy and then switch.
- Holds when you Unleash (learned when your Unleash did 60 or more damage): a Riposte lands before his Hold, so he cannot stop one. Every reflex Hold costs him a point and his cast needs 3, so an Unleash just before his cast makes it come late. The screen shows his points and when his cast is due.
- Unleashes when you Fall Back (learned when you fell back in a fight they still lost): fall back behind a Hold and he wastes 2 points and his own nerve on nothing.
- Shoots past your Warden: cut. Tribes stop learning it, and any tribe that has it drops it.
- They still remember which class hurt them most; story only.

Hunters learn one trait when they beat you, against your favourite button:
- Parried most: a rhythm, one more each time he beats you. Delay: his beat comes late; fake a Hold, then parry the late one. Double swing: light then heavy; parry both and the second cracks twice; Fall Back clears only the first. Feint: a fake beat first; hit him during it.
- Fell back most: Long Reach. They reach through your Fall Back, but nobody covers them while they do.
- Focused most: Thick Hide. The one you focus braces: your blows do half, but braced he cannot swing.
- Unleashed most: Unshakable. Your Unleash cannot scare them, and they stop Holding against it.
- Unstoppable becomes Delay and Quickstrike becomes Double swing; Feint is new, so the hunters have 17 traits, not 16. Shieldbreaker and Evasive stay as traits hunters roll for themselves. Firebomb and Berserk lose their "cannot be parried".

The showcase, a hybrid: Shadow of War's gang you can see, MGS V's adaptation you can see, ECHO's learning you can shape, and our own, his memory as a replay of your real fight (the 1 Oct "replay of why").
- Camp, the Hunt Banner (placeholder name): the hunters' ladder drawn as their own war banner, ten names stitched down it and yours among them. A hunter who beats you climbs it and hangs a trophy off his line, a piece of the fighter he beat. Tap him for what he learned, the play it opens, and his memory. The chiefs who remember you get the same on their codex pages.
- In the fight, marks on the body: each lesson shows on whoever carries it, and flashes when it fires, so you know where the bait is.
- Before the rematch, his memory: three seconds of your real fight replayed from the archive, the moment he learned it, with his one line on top in Jacob's words.
- On the road: the hunters the bounty sends show on their nodes with their faces and trophies.
- On the pause screen and the results, what they are learning: "You've parried 7 of his swings. Beat you, and he learns Delay." You can teach a lesson on purpose, or hide it.
- The sim gives the screens every lesson a rival holds against you with its fight and its play, the moment each one fires, his points and next cast, what he is learning now, the lesson behind an order's miss (Gate 2's order results), and what Spare or Execute gives before you choose. Hunters keep where they learned each trait (the fight, the button, how many times), which needs a save version bump.

**4. How you see the combos**
- Colour is whoever finishes it. Each class and each order button gets one colour. A mark glows on the enemy in the colour of the fighter who cashes it, with that fighter's icon.
- A finisher lights up when it is ready, with a ring that runs down: after a parry, Unleash glows (the Riposte); after a Fall Back, Unleash glows (the Ambush); after Focus Fire then Unleash, the 5th button glows (the Pyre); a snared enemy glows in the Marksman's colour, and Focus Fire on him sends the Aimed Shot there.
- A parried enemy gets an Opening (new), so the Skirmisher's next strike on him hits twice as hard, on top of the Riposte.
- The threat read shows your units: when a boss move comes, the fighter who can answer it glows, beside the Hold button's beat ring.
- Every combo that lands gets its name stamped big with freeze and flash (names are placeholders). Each combo is taught once, by one line the first time it is ready; after that only the glow.
- The glows show what is possible, not orders: several light up at once and each costs points.
- The sim gives the screens every open combo window (what finishes it, how long it has) and every mark on every enemy with who cashes it.

**What this replaces, and how it is built**
- 13 Oct "Hold at any other time works as before" becomes: a Hold that is not a parry soaks and cracks your nerve.
- 14 Oct: the wait out of your parry happens once per beat in every fight; before, without heavy swings, every fresh Hold made him wait again, at a point each.
- 18 Oct: "Hold cannot stop fire" goes, and the hunters' traits change as above, to 17.
- 20 Oct: the answers each chief and beast kit was locked with become that move's safe answers, and the Glory rule that pays mixing answers faster than repeating one no longer slows a second parry.
- Every rule change goes behind a new campaign rules version 8, so fights and saves from before play back exactly as they began.
- Not built: all of it. The looks (the banner, the marks, the glows, the stamps) come as three free mocks before any art, with no PixelLab. Codex builds the screens.
- Open: the banner, the trophies and the memories sit closest to the Nemesis patent (US 10,926,179); the solicitor should see them before they are built.

## The cage bear's look and boss scene, owner decision, 24 Oct 2026

Jacob on 23 and 24 Oct, after the concept rounds were rejected: "bro why dont you use pixel lab bro ??", then "bear 1 and refine shark 2", "beasts have to be size of combined canvas size of all the fighters", "he should be doing party wide damage not to 1 unit he should be where 3 tit is and there should be no other sprite apart the beast", the Battle Cat moment "when tiger becomes powered by greyskull", then "bear is super" and "go, rebuild the shark's idle with those five and lock the bear in DESIGN_LOCKS". Jacob calls the Skullpeak chief Arina. This look replaces the 23 Oct brief's look where they differ: the brief kept the steel to its bones and ruled out armour plates, and the approved bear wears a steel skull mask and, powered, a crested helm and shoulder plates. The fight rules stay as built.
- Look, drawn in PixelLab in the Skullpeak Warden's style: a giant war bear of the frozen Skullpeak mountains, hunched low, a polished steel skull mask with turquoise eyes, one foreleg a massive black steel claw like the Skullpeak claw arms, its chest caged in black steel ribs round a glowing turquoise core, snow caked in shaggy dark brown fur, old scars and steaming breath.
- Powered form, the Battle Cat beat: the great steel skull of the Skullpeak arena flares and sends a bolt into it; it rises in a crested steel skull helm and steel shoulder plates with turquoise lightning over it, and roars.
- Size: drawn at 256 px and shown at 2x, so it is as big as your whole squad. It stands alone where Arina stands, with no other enemy sprite on its side.
- What it does on screen: it paces and snarls, pawing the snow, its heart pulsing; the heart falters and goes dark; the skull powers it up; it rears and roars, and the roar shakes the nerve of your whole squad; frost cracks race across the floor at your squad as the tell; it slams down and a wave of ice spikes rolls through all five of your fighters.
- Files: Art/Studies/Beast-Concepts-20261022/directions/anim2 (bear-256.png, bear-powered-256.png, and the pace, idle_dim, transform and rage_slam clips of 40 frames), the scene in make/boss_scenes.py, the video bear-boss-v2.mp4.
- Open: in the sim the Grab still takes one fighter; the party wide slam lives only in this scene, and the sim's party wide moves are the Roar (nerve) and the Pulse. Whether the slam becomes a sim move is not decided.
- Not built: anything in Unity. Its pixels are twice the size of your fighters' pixels.

## The squad first (rules 9), owner decision, 24 Oct 2026, later

Jacob played the parry at the center in Codex's builds later on 24 Oct and rejected the experience: parrying crowded out the other orders and rivals learned too fast (Codex's brief, HANDOFF_LOG line 10863). Claude's proposal at line 10945 ended with three calls, Claude's picks first. Jacob: "go, build rules 9 with your three picks as written", then "go, write my three rules 9 picks into DESIGN_LOCKS". Every number is a starting value.

1. A band's heavy swings are small beats: still parryable, no duel. A parried swing still staggers the swinger, leaves him open and gives the Riposte its window, but it cracks no nerve and adds nothing to your parry chain, and one that lands drops no Glory and does not reset your chain. Band swings come every 5 s, not every 2.5 s. Big beats stay duels for nerve: a chief's kit moves and spell, every beast move, the hunt's steps.
2. No block cost. A Hold is a stance again: one that is not a parry soaks the front row and costs only its point and its drain.
3. A chief learns only from fights against him, one lesson per fight, on one move. Road fights never arm him. A lesson sits on the move you beat most with that button ("his Cleave waits out your Hold"), not on every move, and a feint and a wait out never land on the same beat.

This replaces, in the parry at the center above:
- "Every beat is a duel for nerve" now holds for big beats only.
- "Lose it, too early": your nerve no longer cracks, the Hold only soaks. The 13 Oct line as rewritten there ("a Hold that is not a parry soaks and cracks your nerve") goes back to a plain soak.
- Gate 4: a tribe's wait out and feint no longer act on every beat, and its chief no longer learns from your road fights.

Built in the sim behind campaign rules version 9, and rules 8 fights and saves play back as they began (hand back at HANDOFF_LOG line 11014, with Claude's calls on how each pick is built, still open). The rest of rules 9, points and nerve from the squad and the honest reads, is Claude's proposal at line 10945, built with these but not locked. Not played by Jacob, not on screen.

## The camp, owner decisions, 27 Oct 2026

Jacob on 27 Oct, on the first six camp takes: "when Sovereign wipes us out, we just take our shit and we go away. So at this point of time, the cam should actually look like a desolate piece of stretch of land or something", "barren mostly", "If you are an intergalactic species, from a type 4 civilization, you would literally have the setting, anything you can take. But the first layer, should be coherent with their civilization, right? You just can't make shit up. Just because it's a campfire." Then "yes go make them" to Claude's new prompt rules, "5 but no where near the final", "yes go" to the pass on camp 5 and "camp should be locked for slice since i spent already computing power on it and dont want to come back ot it". Later the same day: "after the wipeout the leader used a teleportation device and teleported the four guys, the balz and himself, to a safe place. So we need to make the safe place as a camp where we start each run from, and since Clarith was the one who came looking for us, so they're tribe and their encampment should be visible nearby and bloodrich is attacking them, so it should also showcase that", then "Yes go" to Claude's plan for it.

- The cult is an intergalactic species from a Type 4 civilization.
- After the wipeout the leader used a teleportation device to take himself and the four Pals to a safe place. That safe place is the camp, where every run starts. This adds to prologue part 1 (7 Oct): it is how the leader "takes them and retreats".
- The camp is a bare, desolate stretch of natural land. Nothing grows there and nobody ever built anything there.
- The only things in the camp are what the five carried, made by their own civilization. Their look comes from the leader's locked materials (pearly opalescent scaled shell, plum astral cloth with a starfield, rose gold trims) and the Pals' own gear (dark metal with orange glyph seams). Claude's call, Jacob's yes.
- Never in the camp: campfires, wood, tents, cloth on the ground, laundry, racks, shelves, books, candles, banners, signs, crates, joke props, oases, ruins, wrecks or a crashed ship.
- The Clarth came looking for the cult, so their encampment is in sight of the camp, and Bloodridge is raiding it. The camp shows that raid, alive, in layer 2. Claude's plan, Jacob's "Yes go": a lone hill rising out of the cloud sea in the gap of the rim, the Clarth encampment on its left side (shark bone frames and red hides, from the sand shark they hunt), Bloodridge raiders climbing its right side and the two lines clashing on top, drawn with the existing Clarth and Bloodridge sprites shrunk down. Three stills come first for Jacob's pick, then the animation.
- Layer 2 always moves (Jacob, 27 Oct: "always make the 2nd layer animated bro like pterodactyls flying clouds moving or something").
- Locked for the slice: camp 5, the bare high plateau over a sea of clouds, after the pass on it. The two cloths are painted out, the three layers are split, clouds drift through the gap in the rim, and the Pals play their 40 frame camp loops with ground shadows. The raid hill is the one addition still to come. It is placeholder art; the final camp is the paid artist's (22 Oct). Files and how they stack: HANDOFF_LOG, 27 Oct, "Camp 5 locked for the slice".
- Left as they are by this lock: the station props are the painter's guesses at their tech, the edges are soft, and a faint copy seam sits right of the Whisperer's pod.
- The camp is the first screen after the trailer and the main menu, so it carries the game's first impression (Jacob, 27 Oct).

## The camp's debris and tech, and the Pals' camp effects, owner decisions, 28 Oct 2026

Jacob on 28 Oct, on the war camp: "our camp looks desolate no debris from the wipeout no advance tech of any sort butcher is still wiht an anvil shade and whisperer are not ding anything cool". Claude gave a call on each, then Jacob: "go do 1-9 pass". On the result: "nice pass bro not there yet", "I really like the pass", and on the Pals' effects: "just leave it alone ... Actually, Whisperer alone too. Just write it in design logs and leave it later. Just park it."

- The teleport dragged a ring of the battlefield with it: a scorched landing ring in the middle of the camp floor, with the trailer's own dropped weapons and ash from the wipeout, cracked pearly shell plates, and the burnt out teleport anchor (a rose gold hoop) in its centre. This changes two 27 Oct rules for the ring only: the camp is no longer all bare land, and the ring's debris is allowed although wrecks are not. The rest of the 27 Oct list stays banned.
- Their tech moves: the anchor keeps trying to restart, the supply cases' glyphs pulse, and stray shots from the raid stop on a shield dome over the camp, which is why the camp is safe next to the raid.
- Parked, the Butcher: his anvil is covered by a forge machine of their tech, and his strikes feed his locked verbs (purple black hole, blue portal, gold frozen time). The pick of the three machine looks and a new loop without the hammer wait. The new loop needs PixelLab: about 4 generations by the last 40 frame loop's quote; 32.5 were left on 28 Oct, refilling 2 Nov. Machine 2 stays as the placeholder.
- Parked, the Whisperer: her three takes wait. Take 1 (her pod grows a fighter and her crescents lock on as regalia) stays as the placeholder.
- The Shade: Jacob asked to join two takes into one: "the circular thing on the bottom, which is number 2, and the first one which he leaves the shadow behind, can you club both of them together". He said "the whisperer", but the circle and the shadow left behind are the Shade's Rift Mine and Ghost Step. Not built yet.
- Placeholder art, like all of camp 5 (22 Oct). Files: Art/Studies/Camp-War-20261028/, the v4 files.

## The camp in PixelLab's style, owner decisions, 6 and 7 Nov 2026

Jacob on 6 Nov reopened the camp 5 lock to try PixelLab's own style for the whole scene: "we lock camp first then we move onto the next regions since we are going for new approach". On 7 Nov, on the v16 build: "need better models and more depth to the world bro there is no vegetation nothing just plain ass brown land". Codex drew three takes of the whole camp (meadow, grove, spring), and Jacob picked: "hybrid of 2 and 3 go bro this is final lock get props right add more debris and a little makeshift". After Claude's audit of that build (v17), "yes go" to Claude's call: fix its motion bugs, put the Pals back as their own sprites with their approved loops, and rewrite these camp lines. Where this differs from the two camp sections above, this wins. On 7 Nov, after Claude's self audit of v18, "yes go" again to Claude's call: the camp is not locked yet, and the lines below now say which calls were Claude's and not Jacob's.

- The camp is the grove of take 2 with the spring, stream and waterfall of take 3, on the plateau over the sea of clouds: alien trees with glowing pods and hanging moss, plants and moss, and small grey critters at the water. This replaces "a bare, desolate stretch of natural land. Nothing grows there" (27 Oct). Claude's call, not Jacob's words: it also takes oases off the banned list, and the glowing pods stay although Claude's v17 audit said they read as lanterns.
- Debris from the wipeout lies across the camp: cracked pearly shell plates and broken shell pieces with glowing orange cracks. This replaces the 28 Oct rule that debris lies only in the landing ring. Claude's call, not Jacob's: "broken shell pieces" is Claude's name for what its v17 audit called wrecks, and wrecks are still banned.
- A little makeshift: a table leg bound with cable, and a cable from the lounger to the mirror.
- Not in this camp, Claude's call and not Jacob's: the landing ring, the rose gold teleport anchor and the supply cases. The anchor and the cases were Jacob's 28 Oct answer to "no advance tech of any sort", so dropping them waits for his word. The mirror stands on a rock by the spring in the middle.
- The stations follow Jacob's page 11 mock: the Butcher behind a long bench with a jagged blade on it, the Glutton asleep in a round lounger whose curved arm holds the talent tree hologram, the Shade by a round orb basin she feeds with pink ghost hands, and the Whisperer by a table of small figures and domes, fed by an orange beam from her hand. No cylinders on the bench (the 29 Oct tech ban). Claude's call, not Jacob's: the flame under the Shade in the mock is not used, because her approved loop has none.
- The Pals are their own sprites: the four 40 frame camp loops Jacob called okay on 10 Oct, at 1x, on their locked looks (10 Oct). The Butcher works his bowl behind the bench, which hides the bowl. The Glutton sleeps in the lounger, behind its front rim. The Shade and the Whisperer float at their stations over their ground shadows. Each loop runs a whole number of times in the camp's 30 s loop.
- Still banned (27 Oct): campfires, wood, tents, cloth on the ground, laundry, racks, shelves, books, candles, banners, signs, crates, joke props, ruins, wrecks and a crashed ship.
- Unchanged: the Clarth camp and the Bloodridge raid move on the far isles with the v14 sky behind the camp, stray shots stop on the shield dome, and layer 2 always moves (27 Oct).
- How it is made: Codex draws the whole scene, then a copy with the sky keyed green; PixelLab image_to_pixelart turns it into pixel art in six 512x448 tiles, stitched on the cheapest seams; motion is added in code; Pals and figures stay their own sprites. Claude's call, not Jacob's: using this as the kit for the regions.
- Placeholder art like all camp art (22 Oct); the final art is the paid artist's. Files: KS-Painter/Art/Camp/PixelLab-20261106/v18/ (camp-v18.mp4, the layers, and camp-v18-plan.json with the stack order, where each Pal sits and how its loop is cut), made by campx_v18.py in Claude's session scratchpad, not yet in a repo.

## The four Pals' verbs and costs, owner decisions, 28 Oct 2026

Jacob on 27 Oct set the four Pals' jobs: the Butcher "augments to their weapons", the Glutton "talent tree", the Shade "enhance buttons", the Whisperer "unit upgrades", "thats it". The Butcher's tech should "enhance the gunshot from the marksman or the medic's heals, the tank's defensive cooldowns". The talent tree is universal across tribes and grows, and "glutton will only enhance talent trees he manages them". He approved the Butcher's 15 combos with "go" the same night. On 28 Oct, after Claude's audit found the four Pals giving the same effects, he said "yes, stop the mockups and give each Pal its own verbs first", then picked "Yes, costs next" on Claude's verb list and "Yes, lock it in" on Claude's costs (buttons under each list). Every number is a starting value.

**The rule**
- Classes are what fighters do. Each of the 17 keeps its own signature move from `CLASS_ROSTER.json`, and the 20 Oct class rule stands: nothing ever hands a class another class's spell. Pals change what a fighter already does.
- Each Pal changes one thing, and no verb belongs to two Pals:
  - **Butcher:** the fighters' own moves (shots, strikes, blocks, heals), through augments on their weapons. Verbs: pull (Gravity Well), portal (Fold Door) and stop time (Time Stop: freeze, slow or rewind). His forge also breaks nerve (the Lava Sun Tabard). On screen: purple black holes, blue portals, gold frozen time.
  - **Shade:** your four buttons. One verb per route: return (Mirror), shield (Sanctuary), decoy (Ghost Step), mine (Rift Mine), mark (Rupture), chain (Death Warrant), push (Breach), echo (Echo). On screen: astral outlines.
  - **Whisperer:** the fighter. Grow (a rank up inside the fighter's own class), carry (a second slot) and habit (how the fighter acts alone, like Mobile Aim). Her rank ups show as new regalia on the fighter.
  - **Glutton:** the talent tree, universal by role and growing with the campaign. A talent only boosts what the other three Pals give (bigger, longer, faster or cheaper) and never adds an effect of its own. Talents keep their Type IV names. He feeds one talent past its max once a run.
- The only thing left that stops a killing blow is the Shade's Sanctuary, and only off a full parry.

**The Butcher's 15 combos.** A combo is one fighter's own move plus one augment, making one bigger move you can see (Jacob, 27 Oct). Three of the 27 Oct list changed on 28 Oct. New names are working names.
- Gravity Well (purple): Marksman, Black Hole Round (the shot drags nearby enemies into one spot and crushes them). Skirmisher, Collapse (the second strike drops a black hole that pins the target flat for 2 s). Warden, Event Horizon (the shield bash pulls everyone around in, then the cleave hits them all). Trapper, Singularity Net (the snared enemy drags its neighbours in until all are stuck). Medic, Mercy Well (the heal pulls spilt blood back into every nearby ally), in place of Event Shell.
- Fold Door (blue): Marksman, Around the Corner (the shot exits a portal behind their line and hits their healer). Skirmisher, Execution Corridor (on a landed parry, the Skirmisher steps through a portal and hits the attacker from behind). Warden, Wrong Door (hits on any ally come out of a portal onto the Warden's shield), in place of Return to Sender. Trapper, Doorway (a charging enemy falls through a portal into the snare), in place of Reel In. Medic, Evac (the serpent pulls a dying fighter through a portal to the back and heals them).
- Time Stop (gold): Marksman, Stopped Bullets (three shots hang frozen, then all hit at once). Skirmisher, Frozen Flurry (the target freezes, then every hit lands at once). Warden, Still Wall (whoever hits the shield freezes for 2 s while your squad hits it). Trapper, Time Snare (the snared enemy's big move winds up twice as slowly). Medic, Rewind (rewinds your fighter 3 s, undoing the damage).
- Why three changed: Event Shell was a shield, the Shade's verb, and the Warden's Guard on a Medic. Return to Sender sent hits back, the Shade's Mirror, and was the Duelist's Riposte on a Warden. Reel In pulled one enemy into your line, the Binder's Tether.

**What changes in the other papers and drafts**
- Shade (Codex's `reports/Shade command specialisations.md`): Rift Mine keeps its mine, and its tether and pull become a push, because pull is the Butcher's. Mirror's last form, Fold, and Ghost Step's last form, Collapse, get new names, because both are Butcher names. The 20 Oct order sockets (point 9) become the Shade's routes.
- Whisperer: Claude's 27 Oct examples of a Skirmisher growing into a Duelist, a Marksman into a Sniper and a Skirmisher into an Assassin are withdrawn. Duelist is another of the 17 classes, and Sniper and Assassin are not in the 17.
- Glutton's tree (Claude's 27 Oct table, seven limbs of five talents): Second Plate moves to the Whisperer as carry. Eighteen talents become boosts. Copies of the Butcher: Fold Shield, Fold Step, Answer the Parry, Fold Trap, Slow Light Rounds, Light Stake, Crawl Field, Stasis Wind Up, Rewind Wound. Copies of the Shade: Echo Strike, Nothing Wasted, Branch Anchor, Recompile, Synth Armour, Command Relay. Against the three parry bars: Deep Node. Part of a dropped system: Strip (its armour bank went with Strip and Reprint). An effect no Pal owns: Entropy Purge. Entropy Sink also drops its slow. Six of the seven keystones are among the eighteen.
- Favours (18 Oct, in `CampaignFavours.cs`): each Pal's favour bent one button. Buttons are the Shade's now, so each favour becomes a one run taste of its own Pal: the Butcher's lends an augment, the Shade's a route form, the Whisperer's a rank up, the Glutton's a talent. Pal duos stay.

**Costs.** Today a road fight pays 10 materials, a chief 30, and the tabard costs 50 (`CampaignRules.cs`, `CampaignForge.cs`). A slice run brings home about 150 if you win all of it.
- Butcher: each augment is forged once at camp for 50 materials, like the tabard. All three are known from the start. An augment takes the fighter's one gear slot, and each carries a pact, a cost written in words (the 20 Oct blood pacts). Gravity Well, Heavy: the move takes twice as long to wind up. Fold Door, Unnerving: every door costs the fighter a tenth of their nerve. Time Stop, Borrowed time: every freeze costs the fighter a tenth of their health. The forge stays when the run ends, and who carries what is picked at camp or between fights, like the tabard.
- Shade: each route form costs 30, then 60, then 100 materials at camp, once you have first reached its tier (Codex's tiers). Routes open free by tier, and switching is free at camp. Each route keeps its own weakness from Codex's paper. All of it stays when the run ends. This follows the 10 Oct rule that materials pay for Pal upgrades at camp, in place of Codex's free milestone forms.
- Whisperer: grow is bought between fights from the run's materials. Rank 2 costs 10, rank 3 costs 30, and rank 3 opens the second slot. Those materials never go home, the rank is lost if that fighter is captured, and every rank goes with the army when the run ends. Habit costs 30 materials per class at camp. Each class has two habits and uses one (the Marksman's are Mobile Aim or a faster standing aim). Habits stay, because our own units keep learning.
- Glutton: talent points, one for each new thing the codex records (a tribe won over, a beast killed, a chief's hidden chain found), never from repeating fights. A talent costs 1, 2 or 3 points by its tier and a keystone 5, so all seven limbs cost 77. A talent has no drawback. The price is that the tree costs more than the codex will ever give, so you never own all of it. Reset is free at the Glutton. Feed is free once a run: one talent goes past its max for that run, and he eats another talent in the same limb for that run (you pick which).
- Favours cost that fight's materials (20 Oct) and go when the run ends.
- Risk and reward: the safe pick pays less everywhere. A plain gear piece is steadier than an augment, banking is safer than a rank up, and a base button is safer than a route. The tree is the one fully safe layer, so it is capped.

**This replaces**
- From the 10 Oct Pal jobs above: the Glutton's armour and body augments (now the talent tree) and the Whisperer's skins and regalia (now unit upgrades, which show as regalia). The Butcher keeps his forge and adds the weapon augments. The Shade's upgrades that unlock orders become enhancing all four buttons.
- The 18 Oct favour buttons, Codex's free Shade forms, Claude's 27 Oct Whisperer examples and the eighteen tree talents, as above.

Still open: the wording of the eighteen boosts and the six new keystones, new names for Mirror's and Ghost Step's last forms, Rift Mine's new forms, each class's two habits, which campaign tier opens each tree tier, and how many points the whole codex gives (it must stay under 77). Nothing here is built or played. Codex's Shade paper and the favour code are not changed yet.

## The Butcher, Glutton and Shade locked, owner decisions, 28 Oct 2026, later

Jacob asked Claude to finish reading Codex's papers "so we can lock 3 pals real quick and move onto the next one". Claude read her whole Shade paper, her Shade audit and revision 3 of the Four Pals paper, then gave a call on each open point. Jacob: "Yes, lock all three". This closes the open points above for these three Pals. The Whisperer is next. Every number is a starting value.

**Butcher**
- Held parry bars make combos stronger: +10% per held bar, so +30% at three. Casting the chief ultimate spends the bars. This is how the two parry rewards Codex recorded (filling the three bars, and stronger chains) work together.
- A chief's gear recipe is learned when you spare him, because Jacob said "delete execute" on 27 Oct.
- The 15 one fighter combos replace Codex's revision 3 Folded Volley and its Spread or Concentrate branch, which needed two fighters.
- A rank 3 fighter carrying two augments fires both combos and pays both pacts. There is no third combo.

**Glutton**
- Each tree tier opens the first time you reach that campaign tier, and keystones open at tier 4.
- The whole tree, seven limbs. Each limb is tier 1, tier 2, a tier 3 pair you pick one of, and a keystone:
  - Front line: Dark Matter Plating (every hit on the front line loses some of its weight). Entropy Sink (front line cooldowns refill faster). Fold Shield (front line portals cost half the nerve) or Neutron Mass (front line black holes pull from twice as far). Keystone Branch Anchor (Sanctuary's shields on the front line are twice as thick).
  - Strikers: Nucleus Edge (strikes hit harder). Strip (strikes on an Opening hit even harder). Fold Step (striker portals open instantly) or Echo Strike (Echo replays striker hits at full strength). Keystone Answer the Parry (strikers hit twice as hard on a parried enemy for 2 s).
  - Shooters: Slow Light Rounds (shooter freezes last a second longer). Dark Matter Rounds (shots pass through armour). Light Stake (shooter black holes crush twice as hard) or Probability Lock (shots on a snared enemy can't miss). Keystone Nothing Wasted (Death Warrant carries all the overkill, not part of it).
  - Control: Gravity Mesh (a snared enemy is too heavy to dodge). Time Lock (snares hold longer). Crawl Field (Time Snare makes big moves wind up three times as slowly) or Fold Trap (Doorway catches two chargers, not one). Keystone Stasis Wind Up (Singularity Net drags in twice as many).
  - Healers: Matter Printer (heals land bigger). Rewind Wound (Rewind undoes 5 s, not 3). Synth Armour (heals on a Guarded ally land twice as big) or Entropy Purge (healer portals cost half the nerve). Keystone Recompile (heals are twice as big on anyone under a quarter health).
  - Command (you): Precognition (the parry window is wider). Glancing Fold (a near miss folds part of the hit away). Deep Node (Shade forms cost a third less) or Charged Branches (held bars power the combos harder). Keystone Command Relay (one enhanced button gets each form a tier early).
  - War chest: Rogue Planet Prospecting (more materials). Branch Hedge (your streak survives one loss, because you take the branch where you didn't lose). Synthesis Discount (Butcher augments cost less) or Gene Library (Whisperer upgrades cost less). Keystone Node Cache (each run starts with one free augment, pulled from another branch).
- Nineteen of these were rewritten on 28 Oct (the eighteen above plus Neutron Mass in Second Plate's slot). The other sixteen keep their 27 Oct meaning in plainer words, with Entropy Sink's slow removed. Each limb costs 1, 2, 3 and 5 points, so 11, and the tree 77.

**Shade.** A call on each of the nine findings in Codex's `reports/Shade routes audit and re-audit.md`. Most take her own recommended repair.
- F1, Sanctuary's Relay: it draws from one small shared pool made with the field, never from other fighters' shields.
- F2, Rupture's finish: fighters about to attack hit now, then wait that much longer after.
- F3, follow up presses: a follow up press spends what that use stored, once, and never fires the normal button again.
- F4, space: no movement system. Rift Mine goes off on the first enemy who attacks after your Fall Back (her event option), and a push means that enemy's next attack comes later.
- F5, Echo: each fighter's echo stores a share of every hit in the window, still one echo each.
- F6, Ghost Step: only a manual Recall bursts the decoy, and a normal return leaves it catching hits.
- F7, a lone chief: before a chief fight you can set an enhanced button back to its base form for that fight.
- F8, a route unlocked mid run: unlocking it gives one free swap at the next safe screen.
- F9, depth: both enhanced buttons share the same gates, second forms from tier 3 and final forms from tier 5. The 30, 60 and 100 material prices decide which one you deepen first. This replaces Codex's fixed order of one button at tier 3, the other at tier 4, one final form at tier 5 and the other at tier 6. The Glutton's Command Relay moves one button a tier earlier.
- Names and forms: Mirror's last form is Pierce (the returned hit punches through to one enemy behind). Ghost Step's last form is Shatter. Rift Mine's three forms are Mine, Shock (the blast pushes) and Wide Blast (it also hits two nearby enemies).
- Codex's Gate D (a prototype and Jacob's play) still stands.

Still open: the Whisperer (each class's two habits, and what rank 2 and rank 3 give besides the second slot), and how many points the whole codex gives (it must stay under 77). Nothing here is built or played. Codex's Shade paper and audit and the favour code are not changed yet.

## The Whisperer locked, owner decisions, 28 Oct 2026, later still

Claude's first Whisperer draft had only habits and ranks. Jacob: "I thought units behaviours and their traits are being tackled here ?", then "Yes, redo her" to adding traits and the paired actions he gave her on 27 Oct (HANDOFF_LOG line 11887: unit behaviour and paired actions belong to the Whisperer, not the Shade), then "Yes, lock her". This closes the Whisperer's open points above. Every number is a starting value.

The Whisperer changes the fighter in four ways: habits (how it acts), pairs (who it works with), traits (what it was born with and what it earned) and ranks (how far it has grown). Focus Fire still comes first. Nothing she gives changes a number or adds an effect, and every part uses the class's own spell (the 20 Oct class rule).

**Habits.** Each class has two and uses one, or stays on its plain behaviour. A class's pair of habits costs 30 materials at camp and is kept for good. Switching is free at camp.
- Warden: Bodyguard (Guards the leader) or Rescuer (Guards whoever has the least health). Plain: he Guards the Medic so she heals more, and both habits give that up.
- Skirmisher: Patient Blade (strikes only a marked enemy, and waits otherwise) or Avenger (strikes whoever last hit one of yours). Plain: a marked enemy first, else her normal target.
- Marksman: Patient Eye (shoots only a snared enemy, so every shot crits, and waits otherwise) or Spotter (shoots whoever is winding up a big move, leaving it an Opening). Plain: a snared enemy first, else his normal target. These replace Mobile Aim and "a faster standing aim" in the costs above: Mobile Aim is half of the Quickdraw piece, and a faster aim is a number.
- Trapper: Ambusher (her trap ignores light hits and springs on the first heavy blow) or Gatekeeper (her trap springs only on someone attacking your back row). Plain: it springs on the first melee attacker.
- Medic: Triage (waits until someone is under a quarter health) or Front First (treats the front row first). Plain: the most hurt ally under 90% health.
- What each gives up: both Warden habits lose the Medic's bigger heals under Guard. The two Patient habits can leave the fighter idle. Avenger and Spotter cash in fewer marks. Ambusher snares less often, and Gatekeeper leaves the front row untrapped. Triage lets small damage pile up, and Front First leaves the back row waiting.

**Pairs.** A pair is taught to two classes at camp for 30 materials and kept for good. You run one pair at a time and switch free at camp. When the first fighter's spell lands, the second uses its own spell on the same enemy at once if its spell is nearly ready, then waits that much longer after (the borrowed time rule of the Shade's Rupture finish).
- Pin and Shoot (Trapper, Marksman): her trap springs, and he aims at the snared enemy at once, so the shot crits.
- Open and Cut (Marksman, Skirmisher): his shot lands, and she strikes that enemy at once, so the Opening doubles her hit.
- Snare and Rip (Trapper, Skirmisher): her trap springs, and she strikes the snared enemy at once and rips the bleed open.
- Mend and Hold (Medic, Warden): she treats someone, and he Guards them right after.
- The catch: the second fighter's spell is spent on the first one's timing, so it may still be waiting when a bigger threat comes. Mend and Hold also leaves the Medic without the Warden's Guard.
- These are not the parked kin bonds (20 Oct, point 4). A pair is taught to two classes, while a bond was earned by two named fighters in a fight. Bonds stay parked.

**Traits**
- Born, the quirks (13 Oct, same list): still rolled, one per fighter. Between fights she can reroll one fighter's quirk for 10 of the run's materials, and it can come out worse.
- Earned, the road scars (21 Oct, like Jawbreaker): at camp she can keep one scar for its whole class for good, for 30 materials, so every fighter of that class starts each run with it. One kept scar per class. This gives the death rule's "our own units keep learning" a real system.

**Ranks.** Bought between fights from the run's materials: rank 2 costs 10 and rank 3 costs 30. A rank is lost if that fighter is captured, and every rank goes when the run ends. Each rank adds a piece of the Whisperer's regalia to the fighter.
- Rank 2, the class spell happens one more time: the Warden Guards two allies at once, the Skirmisher strikes twice, the Marksman fires two shots per aim, the Trapper keeps two traps armed, and the Medic keeps one treatment in hand so she can treat twice in a row.
- Rank 3: a second slot.

**Also fixed:** the Glutton's Strip copied the Opportunist gear piece, so Strip now makes Openings last twice as long.

The other 12 classes get their habits, pairs and rank 2 by the same rules when they are built.

Still open: how many points the whole codex gives (it must stay under 77). Nothing here is built or played.

## The four Pals audited and fixed, owner decisions, 28 Oct 2026, last

Jacob: "audit these 4 synergies make them have no holes identify bottlenecks run sim using bots and see if we can do something". The Pals are not in the sim yet, so Claude ran bots in a throwaway model built from the sim's own numbers, and a second agent audited the three sections above against the code. They found 27 holes and 3 bottlenecks. Jacob: "Yes, lock the fixes". Where this section and the three above differ, this one wins. Every number is a starting value.

**What the code already does, which the design now follows** (rules 11 to 14): a lost fight costs one of the run's 3 lives and nobody is captured; each fight won in a row multiplies its materials, up to three times; chains are built, so the first ready fighter cashes a mark at once, and three cashes bond two fighters for the run; the leader cannot be targeted while any ally stands; a snared enemy cannot be missed; only melee attackers spring the trap, and each side has one trap.

**Prices, times 2.5,** because a run now banks 154 to 399 materials and the old shop was bought in one or two good runs: an augment 125, Shade forms 75, 150 and 250, a class's habits 75, a pair 75, a kept scar 75, rank 2 is 25 and rank 3 is 75, and a quirk reroll 25. The tabard stays 50.

**Talent points:** the codex also counts every tribe met, every beast part broken and every chief move seen, still never from repeating fights. In the 8 Dec slice only, tree tier 3 and Shade second forms open at campaign tier 2, and keystones at campaign tier 3.

**Butcher**
- Pacts are flat and the same for every fighter. Heavy: the spell's own clock runs half again as long. Unnerving: a quarter less nerve every fight, taken from the side's bar where fighters have no nerve of their own. Borrowed time: a quarter less health.
- Rank 2's extra hit is plain: it fires no combo and cashes no mark.
- Event Horizon: every fourth swing is the bash that pulls.
- Time Snare: the slow outlasts the hold and hits the enemy's next big move.
- Space: "nearby" means the same row and the next lane, and a pull means everyone pulled takes the crush. Neutron Mass reaches two lanes. Doorway also lets shooters spring her trap. Evac takes the fighter out of reach for a few seconds.
- Singularity Net holds the snared enemy and its two lane neighbours, and the Stasis Wind Up keystone makes it four.
- Wrong Door takes only hits on the allies next to the one he Guards.
- Execution Corridor fires when her strike cashes an Opening, not on a parry, so it is no longer the Duelist's Riposte.
- Trophy pieces and augments that want the same slot are picked at camp.

**Shade**
- Only the parry bars and the Shade's own routes pay off a parry.
- One pull per spell cycle, from any source (chains, pairs or Rupture's finish), and a pulled spell pulls nothing.
- Black hole pins and time freezes hold the enemy but leave no Snare mark. A Sanctuary parry still counts as parried.
- Command Relay: it halves one enhanced button's weakness.

**Glutton**
- The tree's rule now reads: talents make what you have bigger, longer, faster or cheaper.
- Feed: it doubles one talent you own, for that run.
- New wording: Gravity Mesh, Singularity Net crushes harder. Probability Lock, an Aimed Shot at a snared enemy aims at once. Synth Armour, heals on the Warden land twice as big while he Guards. Slow Light Rounds, Stopped Bullets holds one more bullet. Answer the Parry, Mirror's return hits twice as hard. Time Lock, Time Snare's slow lasts twice as long. Matter Printer, Mercy Well heals a third more. Glancing Fold is renamed Glancing Blow.
- Reset happens only when you arrive at camp, before buying anything. Synthesis Discount also covers the tabard and trophies. Node Cache lends a second copy of an augment you own, for the run.

**Whisperer**
- Pairs: Pin and Shoot, Open and Cut and Snare and Rip now mean the two classes start every run already bonded. Mend and Hold stays as it was. "Bonds stay parked" above is withdrawn, because chains and bonds are built.
- A fighter who falls in a lost fight loses his top rank.
- Habits: Bodyguard now Guards the Marksman while he aims. Patient Blade becomes Breaker (strikes whoever is winding up a big move). Patient Eye becomes Counter Shot (shoots whoever last hit your back row). Spotter keeps only its target choice.
- The Trapper's rank 2: her trap springs twice before she rearms it.
- Target order: Hook drag, Focus Fire, the combo's target, the habit, the pair, then plain. In the Guard order, habits and Mend and Hold replace only the "Guard the Medic" step.
- A tribe that learned trap caution never waits on a trap its melee cannot spring.

Still open: what a chief kill pays now that execute is gone and sparing always wins (Jacob's call), what counts as a heavy blow for Ambusher, and how many points the whole codex gives (under 77). Nothing here is built or played.

## The Pals on the road, owner decisions, 28 Oct 2026, after the audit

Jacob, after the audit fixes were locked: "bro i think we need ot make it like roguelike bro". Claude's read: the 28 Oct sections above sell almost every Pal thing at camp for good (augments, Shade forms, habits, pairs, kept scars), so within one to three runs you own every combo and every run after plays the same build. A roguelike hands you the build during the run and takes it away at the end, as his 26 Oct words say: "evrey roguelike gives player a thing to choose that makes the combos and build". Claude proposed flipping it and Jacob said "Yes, that's it". Claude then showed the flip as one run, and Jacob said "Lock it". Where this differs from the four 28 Oct Pal sections above, this wins. Every number is a starting value.

**The rule.** The build comes from the road. Everything a Pal gives that changes a fight is picked on the road and lasts one run. Camp only decides what can show up, plus the forge, the kept scars and the Glutton's tree. Materials still come home and pay at camp (10 Oct), now mostly for unlocks.

**The road, in the code since 18 Oct.** One fight in every row of a tribe's road carries a Pal. Win it and pick one of three of that Pal's things in place of that fight's materials. A pick raises no bounty and ends with the run. The other fights still drop gear. The flip changes what is in the offer, not the road. The twelve placeholder favours that each bent one button are dropped.

**What each Pal's fight offers, for this run only**
- Butcher: three combos for fighters in your squad, from all 15, because all three augments are known from the start. A combo takes that fighter's one gear slot and keeps its pact. His fights never offer a combo you already hold, so making things bigger stays the Glutton's job.
- Shade: three button forms from the routes open at your tier. Picking a route you already hold gives its next form. Tier gates stay (in the slice, second forms open at campaign tier 2).
- Whisperer: a habit for one of your classes, a pair, or a rank up for one fighter. Ranks are no longer bought between fights. A pair bonds its two classes from the moment you take it, for the rest of the run. Claude's reading, his to change: a class still uses one habit and the squad one pair, so a new pick replaces the old one.
- Glutton: he lends one talent from his tree for the run, one you don't own yet that boosts something you hold. Feed stays.
- Example from the chat: Black Hole Round from the Butcher, then Pin and Shoot from the Whisperer so the snare sets up the black hole shot, then Light Stake lent by the Glutton so it crushes twice as hard.

**What run 1 can offer.** All 15 combos, every open route's first form, one habit per class, every rank up and the Glutton's talents.

**Camp, for good.**
- The forge stays as it is: the tabard, trophies and chief gear are forged once and kept.
- Kept scars stay as they are, one per class, because our own units keep learning.
- The Glutton's tree stays, paid by codex firsts.
- Unlocks: pay once and the thing can show up on the road from then on. The Shade's second and third forms (150, 250), each class's second habit (75) and each pair (75).
- Unchanged prices: the kept scar (75), the quirk reroll (25), the tabard (50) and the trophies.
- Prices that go: augments, because all 15 combos can show up from run 1; first forms, because a first form comes free with its route; ranks, because ranks are picks.

**The chief.** Execute is gone (27 Oct), so a kill only happens when he falls before he kneels. A kill pays the big pick in place of his gear: one of three, from any Pal, at once. A spare pays his tribe, his order and his recipe, as now. So it is his allies or his power, both for this run. This answers what a chief kill pays.

**Checked against his earlier calls.** On 26 Oct Jacob turned down stacking Pal favours because they were number tweaks on buttons ("these are not combos"). Here the picks are the combos themselves.

Still open: which habit of each class run 1 offers; new words for Node Cache (it lent a second copy of an augment you own) and Synthesis Discount (augments cost less), because nobody owns or buys augments now; what counts as a heavy blow for Ambusher; how many points the whole codex gives (under 77). Already in the code: the road's Pal fights, the pick in place of materials, and picks ending with the run. Not built: the real Pal things in the offer, a random three from the bigger pool drawn from the node's own seed (today the offer lists that Pal's favours in order), the camp unlocks and the chief's big pick. Nothing here is played.

## Four Pal fixes from the model, owner decisions, 29 Oct 2026

Jacob asked for bots and a regression on the Pals ("now bro run some combos and playsyle do some regression how all 4 are in synergy"), then for six fixes to be tested ("go, test the six fixes in the model and show me what changes"), then for the blockers in fixes 2 and 3 to be cleared ("there are still real blockers in 2 3 how can we lock it in"). Claude corrected the model's snare to match the code and tested new versions of 2 and 3. Jacob: "Lock all four". Where this differs from the Pal sections above, this wins. Every number is a starting value. Every result below comes from Claude's throwaway model with a guessed band, not from the sim, and "points" means the change in the chance to win a fight, out of 100.

**Fix 1, the pacts are halved.** Heavy: the spell's own clock runs a quarter longer. Unnerving: an eighth less nerve every fight, taken from the side's bar where fighters have no nerve of their own. Borrowed time: an eighth less health. Why: at full price most damage combos cost more than they gave. Halved, 12 of the 15 combos help, up from 10. Black Hole Round goes from 2 points to 17, and Mercy Well from a 23 point loss to a 12 point gain.

**Fix 2, today's bonds stay and a pair's answer is free.** Bonds keep forming as the code has them (three cashes bond two fighters for the run), and a pair still bonds its two classes the moment you take it.
- A pair's answer is free: the second fighter answers the first one's move without spending their own spell, so their own clock keeps running. They still answer at most once per cycle of their own spell (one pull per spell cycle, from the audit section), and at least 1.5 s apart.
- Mend and Hold: the Warden Guards the fighter the Medic treats, and the Medic too, so she keeps her bigger heals under Guard.
- This drops the two catches written for the pairs in the Whisperer section: the spell spent on the other fighter's timing, and the Medic left without the Warden's Guard. A pair still costs the Whisperer's pick, so no habit or rank up from that fight.
- Why: the first fix 2, natural bonds lasting the whole run, made the early fights of every run about 20 points harder for everyone. This version keeps them as hard as now, and all four pairs pay: Pin and Shoot 17 to 22 points, Snare and Rip 8 to 23, Mend and Hold from an 18 point loss to a 26 point gain, Open and Cut 2 to 9.

**Fix 3, Ambusher and Gatekeeper.**
- The snare stays as the code has it: it springs before the blow lands, the blow never lands, and it holds the attacker for 2 s. That already gives Ambusher its job. Her trap waits for a heavy blow, so it stops the heavies you fail to parry. Ambusher is the pick for players who miss parries: a 42 point gain for weak parriers, a 23 point loss for average ones and a 97 point loss for strong ones, whose trap waits for heavies they always parry.
- The first fix 3's extra trigger (a parried heavy springs it too) is dropped, because it made Ambusher beat the plain trap for almost everyone.
- Claude's reading, his to change: a heavy blow is the enemy's telegraphed big hit, the one you parry.
- Gatekeeper is out of the slice's road picks. It still loses by 31 to 40 points, because its job needs an enemy that reaches your back row in melee, and melee can't reach it while your front row stands. Letting shooters spring it would copy the Butcher's Doorway. It comes back when the slice has an enemy it answers.

**Fix 5, a different Pal on every fight.** Every fight in a row carries a Pal, and no two in a row carry the same one, so a run meets each Pal more often and you can chase one, like a Hades god. Events stay where they are. A pick is still taken in place of the materials the fight banked, so you can still bank. Why: in the model every run got about 1.5 more picks, and runs that chase one Pal started to work.
- The catch the model missed, open for Jacob: a Pal fight drops no gear (18 Oct), and in most rows the bonus goal fight is the only gear fight. With a Pal on it too, the road drops no gear, nobody can strip, and stripping is what raises the bounty between chiefs. The model had no gear and no bounty, so this cost is not in its numbers.

**Dropped.** Fix 4 (rank 2 costs two picks) made the Whisperer the Pal you skip. Fix 6 (the Glutton's road picks only boost what you hold) left his fights mostly empty. Both are out: rank 2 stays one pick, and the Glutton's road offer stays as written above, with his plain talents in it.

Still open: Wrong Door, Around the Corner and Doorway still lose with half pacts (7, 12 and 13 points), so their effects need a rethink, not a lower price. Every other habit still loses against a plain band: Bodyguard, Rescuer, Triage and Front First by 33 to 40 points, Breaker, Avenger and Counter Shot by 6 to 12 (Spotter never fired in the model, so it is not measured). Rank 2 is still the strongest pick, and Dark Matter Plating and Recompile still win the Glutton's offer. The Pals still add up but barely multiply. The gear on the road under fix 5. The open items in the sections above still stand. In the model the trap springs about 15 times a fight, which may make it stronger there than in the game. Nothing here is built or played.

## The four losing combos rethought, owner decisions, 29 Oct 2026, later

All 15 combos were built into the sim on 29 Oct (rules 15) and then measured on the real sim in about 330,000 fights (HANDOFF_LOG, "The 15 combos measured on the real sim"). That measure replaces the model's numbers above: in the real sim Doorway and Wrong Door do not lose, and four other combos did. Jacob picked "Rethink losers". Claude counted why each one lost and tested a rethought version of each in a scratch copy of the sim, and Jacob said "Lock and build". Where this differs from the sections above, this wins. Every number is a starting value. Win points are against a band the plain squad beats about half the time, first for the reference pilot, then for one giving no orders.

**Stopped Bullets** (Marksman, Time Stop). Why it lost: two shots in three sat frozen doing nothing, so it landed about one shot a fight instead of three and a half, its frozen shots left no Openings, and bullets still hanging when the fight ended were wasted. Now each bullet that hangs frozen in its mark freezes the mark for 2 s and leaves its Opening, and the third shot fires all three at once and cannot miss or be guarded, because time stopped around them. Win points went from 7 below the plain squad to 1 below, and from 7 below to 3 above.

**Around the Corner** (Marksman, Fold Door). Why it lost: going for their healer pulled him off snared enemies, so he lost his snare crits, and off your Focus Fire. Now his Aimed Shot comes through a portal from behind, so no Guard can take it. He keeps to your Focus Fire and to snared enemies first, and only then goes for their healer, and that shot from behind crits. Win points went from 3 below to 3 above, and from 2 below to 5 above.

**Mercy Well** (Medic, Gravity Well). Why it lost: Heavy cost her a fifth of her treatments, and two thirds of the allies beside her patient were already full. Now each treatment still heals the allies beside her patient for half, and whatever they do not need flows back into the patient. Win points went from 7 below to 5 above, and from 5 below to 7 above. A bigger version, the patient healed half again too, scored 7 and 9 above, close to Evac and Rewind, so it was not picked.

**Time Snare** (Trapper, Time Stop). Why it lost: only one slow in five ever met a big move. Now, for 6 s after her trap lets go, whoever sprang it moves at half speed (its turns come half as often), and every big move it starts in that time winds up twice as slowly. This replaces "the slow outlasts the hold and hits the enemy's next big move" from the 28 Oct audit. Win points went from 6 below to 2 above, and from 3 below to 4 above.

The pacts stay as they were (halved, fix 1). At the game's own enemy strength the squad already wins 85 to 88% of these fights and none of the four moves wins, but each one leaves the squad healthier than before: Stopped Bullets about even, the other three ahead. The other eleven combos came out exactly the same. Not measured: the bots never parry road heavies on the beat, so a player who parries should get more from Time Snare's slower wind ups than these numbers show.

Still open: Evac and Rewind lead every other combo by about 6 to 10 points against the hard band, and whether that is too much waits for Jacob's play. Nothing here is played.

## The combos' hooks, owner decisions, 29 Oct 2026, last

After the real sim measure Jacob asked "so combos dont work and there is no synergy ?": pairs of combos did barely more together than apart, only Collapse with Singularity Net, because each combo paid out on its own. Claude proposed one hook per combo, a mark it leaves or a mark it cashes, from the 20 Oct gear rule ("which mark it leaves, or how hard it cashes one in"). Jacob: "Yes, design hooks", then "Lock and build". Where this differs from the sections above, this wins. Every number is a starting value.

**Why marks.** The chains are built: a mark makes its casher hit at once, out of turn (a Marksman for a Snare, a Skirmisher for an Opening or a Bleed), every cashed mark pays an order point and nerve, and three cashes of one fighter's marks bond the two for the run. A hook puts the combo into that.

**The three engines.**
- Snare, still only the Trapper's: Singularity Net and Doorway lay more; Black Hole Round and Around the Corner cash them.
- Opening: Stopped Bullets, Still Wall, Evac and Rewind lay more; Execution Corridor and Frozen Flurry cash them.
- Bleed: Event Horizon, Wrong Door and Time Snare lay more; Collapse and Mercy Well cash them.

**The hooks.**
- Black Hole Round: at a snared enemy the shot's crit carries into the crush, so everyone dragged in takes half the crit.
- Around the Corner: a snared enemy shot from behind takes triple, not double.
- Stopped Bullets: each frozen bullet leaves its Opening, as the rethink built it.
- Collapse: a strike of hers that rips a bleeding wound open drops the black hole at once and pins that enemy 2 s.
- Execution Corridor: cashes Openings, as built.
- Frozen Flurry: an Opening on her frozen mark when it thaws is cashed by the thaw, and the whole kept hit lands twice as hard. The thaw cashes it because her own strike cannot come again inside the 1.5 s freeze.
- Event Horizon: everyone his cleave hits is left bleeding, as a snare's wound bleeds.
- Wrong Door: whoever's blow comes through his door is cut by its edge and left bleeding.
- Still Wall: whoever freezes on his shield is left with an Opening.
- Singularity Net: everyone it drags in is snared too and carries the Snare mark, held as long, with no extra bleed. For this one combo this replaces the 28 Oct audit line that black hole pins leave no Snare mark. Only the Trapper still snares.
- Doorway: shooters spring her trap, so they are snared, as built.
- Time Snare: the snare's wound bleeds for the whole slow, the hold plus 6 s, instead of 5 s.
- Mercy Well: each treatment heals a fifth more for every bleeding enemy, up to three, and the half heals beside grow with it. It never stops a bleed and never deals damage.
- Evac: whoever's blow would have killed him is left with an Opening.
- Rewind: whoever last struck the ally she rewinds is left with an Opening, rewound mid swing. A bleed or a burn is no strike.

**Risk, reward, punish.** Reward: a squad whose combos link cashes far more marks, so more out of turn hits, order points, nerve and bonds, and the Warden and the Medic can now bond with the Skirmisher. Risk: a hook with no partner combo gets only the plain spell's cash, and the Marksman and the Skirmisher each serve one engine. Punish: a chief shrugs Snares and Openings while his band stands (built); the hunters learning your most used mark (20 Oct, not built) is what answers a squad on one engine.

Guard stays the Warden and Medic link it already is: a guarded ally takes no blows, so a Guard hook on patients would never fire. Still open: whether this makes real synergy waits for the same pair measure on the built hooks, if Jacob asks for it, and for his play.

## The art direction, owner decision, 30 Oct 2026

Jacob on three takes of the war camp's frame (29 Oct: v10's colours with just the cut, his option 3 as previewed, option 3 at half strength), after v12 came out greyer than the option 3 preview he had picked ("wtf why is everyting dark and brown and lifelesss"): "i like 1 lock this art direction in and make the rest of stages", then "for stages you need to do for atlest 5 matches so 1 region wiht 5 battle stages check how you did earlier and use the locked camp to make all of those", then "go". This replaces his 29 Oct pick of option 3 (the far layer softened, the war layer one darker, cooler band).

- The look is take 1: camp 5's painting in its own full colour and contrast on every layer. Blue sky, white clouds, lit snow and bone, red hides, and fires and blasts that glow. No darkening, haze or tint band on the far layer or on the war behind the camp.
- The busyness cut stays (29 Oct): one fire per town plus the tower, no small smokes, fewer ships, flak and far bursts, ash 46 to 16, and at most 6 deaths a loop with nobody hopping. Replacements slide down the barge's rope, climb back onto the tower, or run in from behind cover.
- This is the template for every battle stage. First: one region with five battle stages, one per match, made the way camp 5 was made, from the locked camp.
- Placeholder art like all of camp 5 (22 Oct); the final art is the paid artist's. Files: Art/Studies/Camp-War-20261028/camp-5-war-v13.mp4, the locked loop, and look-war-v13-take1.png.

## Duels, owner decisions, 30 Oct 2026

Whole runs measured on 29 and 30 Oct (HANDOFF_LOG, the entries after line 12346) found that duels decide runs and that the fighter you send decides duels: sent plain, the Trapper won 100% of them, the Skirmisher 95 to 100%, the Warden 3 to 22%, the Marksman 5 to 40%, and the Medic none. Two things in the code made it so. Rules 9 made every heavy swing a small beat, meant for bands, and that caught the lone champion too, so parrying him cracked no nerve. And a melee fighter in a road fight never breaks on nerve, so only damage could end a duel. Claude proposed a design; Jacob: "Yes, design it", then "Yes, build it". Campaign rules 16. Where this differs from the sections above, this wins for duels. Every number is a starting value.

- In a duel the champion's heavy swing is a big beat, a duel for nerve as the 24 Oct parry lock says. Parried, he staggers 2 s, is left with an Opening, and his nerve cracks, harder with each parry in a row.
- He swings every 2.5 s, as before rules 9, not every 5 s like a band.
- A champion whose nerve runs out yields: he leaves the field and the duel is won. Your duelist never runs (25 Oct: the player's fighters only die), so his nerve runs out as in any road fight.
- A Hold too early cracks your duelist's nerve as hard as a parry would have cracked his, the 24 Oct rule that rules 9 had turned off.
- The Trapper's snare catches him once a duel; after that he steps around her armed trap.
- You pick who duels; Campaign.StartEncounter already takes the pick. The duel card should ask, with nobody sent by default (Codex's screen).
- Your duelist's class decides how you cash his 2 s stagger, not whether you can win: the Skirmisher's strike doubles on the Opening, the Warden bashes, the Marksman shoots, the Trapper lays her trap, and the Medic can win on nerve alone.
- Risk, reward, punish: a parry staggers, opens and cracks him, and a chain makes him yield; a missed beat lands in full; a Hold too early cracks your nerve; Fall Back dodges for a smaller crack than a parry.
- The test: bots that pick the duelist and parry the beat at three skill levels. It works if the parry rate decides duels more than the class does, and if at half the parries no class wins more than about 80% or less than about 20%.
- Tuned, 30 Oct 2026, to the target Jacob took ("Yes, go tune", then "Go, write 1500"): half the parries wins about half the duels, and a Skirmisher who never parries loses most. In a duel the champion's heavy hits 15 times his ordinary blow, not 2 times; one that lands takes about 28% of a Skirmisher's health at the game's own enemy scale. The parry crack stays at 40, and every other heavy is unchanged. The test's bots with orders: half the parries wins 47% of duels (31 to 76% by class), and a Skirmisher who never parries wins 0.2%. Still off: Frozen Flurry's wearer wins 84% at half, over the test's 80, and without orders half the parries wins 67% of duels, the Skirmisher 93%. HANDOFF_LOG line 12503 has the numbers.

## Builds: one theme per Pal, owner decisions, 30 Oct 2026, later

Jacob: the game needs builds like Hades has, "lightning build fire build dual booing heavy dots", "depending on 4 pals". A read only survey of the whole sim that day (72 files, eight layers) found a solid floor (exact dice, replays that prove themselves, no floats in game logic) but none of the middle that builds need: no status system and no stacking (only burn stacks, bleed keeps the biggest), every bonus adding into one pile, about 218 percent steps that each round down, no positions, no scheduler, one Pal pick a row with every offer equally likely, and only the Butcher in code. Claude planned three build systems. Jacob: "Yes, plan builds", then "Yes, theme per Pal", then "Lock it". Where this differs from the sections above, this wins. Every number is a starting value. Nothing here is built or played.

**The decision.** A build is one Pal's theme you lean into over a run. Duos are how two Pals mix. The Butcher keeps pull, portal and time as his three flavours. Claude's reading, his to change: each other Pal's theme grows from its 2 Oct Chaos trait (the Glutton's rot and endurance, the Shade's change and tricks, the Whisperer's excess and charm). What each theme does is still to design.

**1. Statuses with stacks.** One status list on every fighter. Each status says how many stacks it has and its cap, how long it lasts, how often it ticks, how big each stack is, who put it there, and how a new one refreshes it: add a stack, reset the clock, or keep the bigger. Snare, pin, stun, panic, flurry, bleed, burn, slow and the marks all move onto it, and a Pal's theme adds new rows. Every status has a cap, and a chief's band already shrugs off marks.

**2. Multiplier layers.** One damage pipeline with named layers. Bonuses of the same kind add together; different kinds multiply each other. A hit rounds once at the end and keeps the leftover, as nerve already does. To start: base power, then the increased pile (gear, quirks, traits), then separate more layers for status payoffs, combos, mark cashes and pacts. The held parry bars (+10% each, 28 Oct) are one of the more layers. Heals and damage over time ticks run through the same pipeline. A fifth copy of one bonus adds a little, while a bonus from a new layer multiplies everything, so builds snowball by hunting the next layer.

**3. Pal offers.** The Pal shows on the road, so the route steers toward the Pal you are building. Offers lean toward what you hold: after your first picks from two Pals, those two come up more, as Hades caps a run at four gods. A duo unlocks once you hold a pick from both Pals of a pair. A pick offered again ranks up. A Pal on every fight (fix 5) still waits on Jacob's gear and bounty call; this works with one Pal fight a row and better with more.

**Build order, each step its own yes.**
1. The status list, with no change to how fights play: the old against new proof must show identical fights.
2. The damage pipeline, behind a new rules version, so old fights and saves stay exact.
3. The Butcher's theme end to end, with the offer weights, then the whole run sweep: themed builds against the same number of random picks.
4. The other three Pals, then duos.

**Risk, reward, punish.** Leaning into one theme pays the most, and spreading your picks pays less but gives a counter less to aim at. Every theme must have something that counters it, and the hunters learning your most used mark (20 Oct, not built) is the punish for a run on one theme.

**The test.** In the whole run sweep, a run that leans into one Pal, or two Pals plus their duo, wins clearly more runs than the same number of random picks, and its theme still has a counter.

Still open: what each of the other three themes does, the duo pairs and their effects, the offer weights, and fix 5.

## The Butcher's theme (builds step 3), owner decisions, 30 Oct 2026, last

Step 3 of the builds lock above, designed on the status list and the damage layers built that day (steps 1 and 2, HANDOFF_LOG lines 12483 and 12539, in the sim only, not in Unity yet). A run meets the Butcher about once or twice, and his combos add up but barely multiply (the 29 Oct model and the whole run sweeps), so nothing rewards leaning into him yet. Claude designed his theme; Jacob: "Yes, design step 3", then "Lock it". Where this differs from the sections above, this wins. The names are working names and every number is a starting value. Nothing here is built or played.

**One mark per flavour.** Each is a new row on the status list. It stacks to 3 and lasts 6 s, and a new stack resets the clock. At 3 stacks it bursts and clears.
- Pull (purple), Crush: laid on everyone Black Hole Round or Event Horizon pulls, on the enemy Collapse pins, on everyone Singularity Net holds, and by Mercy Well on each bleeding enemy her heal reads. Its burst, Crushed: a hit for the power of whoever laid the third stack, in the Combo layer so the damage layers apply, and a 1 s pin.
- Portal (blue), Exposed: laid on Around the Corner's target, Execution Corridor's target, the striker whose blow comes through Wrong Door, whoever Doorway's trap catches, and the enemy Evac pulled an ally away from. Its burst, Wide open: your side's next blow on it can't miss, can't be Guarded, and crits double, in the Cash layer.
- Time (gold), Stasis: laid on Stopped Bullets' target, Frozen Flurry's mark, whoever freezes on Still Wall, Time Snare's slowed enemy, and the enemy Rewind undoes. Its burst, Stopped: frozen 1.5 s, and a big move it was winding up starts over.
- Why a burst and not a small bonus per stack: one combo of a flavour rarely lands 3 stacks inside 6 s, but four or five combos of it often do. So leaning in multiplies, and spreading across flavours almost never bursts. Every combo still does what it does today.

**Rank ups.** A combo you already hold, offered again and taken, ranks up: rank 2 lays 2 stacks and rank 3 lays 3, so it bursts on its own. His ranks add stacks, never bigger numbers; bigger, longer, faster and cheaper stay the Glutton's. This replaces the 28 Oct road line that his fights never offer a combo you already hold (the builds lock: a pick offered again ranks up).

**Offer weights.**
- Which Pal a road fight carries: weight 1 each, times 3 for a Pal you have taken a pick from this run. Once you hold picks from two Pals, a Pal you hold nothing from drops to a third. It applies from the next road on, so the route steers toward your Pal.
- Inside the Butcher's three: a combo weighs 1, plus 1 for each combo you hold of its flavour, and a combo you hold below rank 3 weighs 2. The three are drawn from the fight's own seed, so the same fight always offers the same three.

**Risk, reward, punish.**
- Leaning into one flavour bursts the most, but one answer to that mark blunts everything, and the flavour's pact lands on every fighter: a pull squad casts slowly (Heavy), a portal squad has less nerve (Unnerving), a time squad has less health (Borrowed time).
- Spreading is safer, with mixed pacts and no single counter, but it rarely bursts.

**Counters that already exist.** A chief whose band stands shrugs the marks off, as he does pins and snares. A duel has one enemy, so pulls drag nobody and portals have no back line. The Stoneveil chief cleanses. The run level punish stays the hunters learning your most used mark (20 Oct, not built).

**The test.** The whole run sweep from the builds lock: 300 seeds, scale 110 with orders and 70 without, a themed pilot (it takes its leading flavour, else a rank up) against a random pilot with the same number of picks. It passes if the themed pilot wins at least 10 more runs in 100 at both settings, and its edge shrinks against banded chiefs.

**What gets built, behind campaign rules 18, on its own yes.** The three marks and their bursts, each combo laying its mark, ranks on combo pieces (save version 24), both offer weights, one quick test per piece, the old against new proof below rules 18, then the sweep.

Still open: the names, every number, fix 5 (a Pal on every fight, which waits on Jacob's gear and bounty call; this works without it and better with it), and the chief's big pick (locked 28 Oct, not built), which Claude would leave for later.

## Fix 5: a Pal on every fight, owner decisions, 30 Oct 2026, after step 3

The step 3 sweep (HANDOFF_LOG line 12638) found that a run takes 0.6 of the Butcher's picks on average, 172 runs of 300 none, so leaning into his theme had nothing to lean on. Fix 5 (29 Oct, above) puts a Pal on every fight, and its catch was left open: a Pal fight drops no gear, and in most rows the bonus goal fight is the only gear fight, so road gear, stripping and the bounty rising between chiefs would all go. Claude proposed that a won fight offer both; Jacob: "Yes, fix 5 next", then "Lock it". Where this differs from the sections above, this wins. Every number is a starting value. Nothing here is built or played.

**The decision.**
- Every fight on the road carries a Pal, and no two fights in one row carry the same Pal. Events stay where they are. The road's Pal weights from step 3 decide which Pals go on a row's fights, drawn without repeats.
- A won fight offers both on one card: its Pal's three picks and the tribe's gear it drops today. Take one thing, or bank the materials the fight banked. Taking either clears the rest.
- Stripping still raises the bounty (1 heat a piece, 2 a chief), and a pick still raises none.
- This answers fix 5's open catch: road gear, stripping and the bounty between chiefs all stay.

**Why.** Picks were the wall. With a Pal on every fight you can route to the Pal you are building in most rows.

**Risk, reward, punish.** A pick builds the run. A strip gives you a piece but raises the bounty, so the hunters come sooner. Banking pays at camp, not in the run, so the safe answer pays least in the run.

**What gets built, on its own yes.** A new rules version: roads made under it carry a Pal on every fight, and a won fight's offer holds the picks and the gear together; the save keeps both on offer; one quick test per piece; the old against new proof below it; then the step 3 sweep again. Codex's card shows the picks and the gear together.

Still open: how many picks a run gets (the 29 Oct model said about 1.5 more, with no gear or bounty in it).

## The Glutton's theme: one limb (builds step 4), owner decisions, 31 Oct 2026

Step 4 of the builds lock above starts with the Glutton. Claude first proposed a rot rule laid over every status; Jacob: "man go look at his talent tree", and the theme was redone from the locked tree screen (round 22, Art/Studies/Talent-Tree-20261027/round22-star-lattice.png) and the tree locked on 28 Oct. Jacob: "Lock it". Where this differs from the sections above, this wins. Every number is a starting value. Nothing here is built or played.

**The decision.** The Glutton's theme is one limb of his own tree. His seven limbs are roles (Front line, Strikers, Shooters, Control, Healers, Command, War chest): a build leans into one role, and he makes that role huge. In one line: three Shooter picks are the Shooter keystone.
- His road pick lends a talent you don't own yet, for the run (28 Oct). A pick from a limb you have already picked from this run lends that limb's next talent, so the run climbs it: tier 1, tier 2, the tier 3 pick of two, then the keystone.
- The keystone is his payoff for leaning: Nothing Wasted for Shooters, Answer the Parry for Strikers, Recompile for Healers, and the rest as locked.
- Spread picks give only tier 1s, which boost a little and never pay off big.
- Feed still doubles one talent you hold, for the run.
- His rule stands: every talent makes what you have bigger, longer, faster or cheaper, and never adds an effect of its own. That makes him the natural partner for a duo: Shooter talents like Light Stake make the Butcher's Black Hole Round crush twice as hard.
- The rot proposal is dropped.

**Risk, reward, punish.** Reward: a keystone late in the run. Risk: you only get there if the road keeps offering him, which fix 5 and the step 3 weights help. Punish: picks spread over three limbs are three weak tier 1s.

Still open:
- Three picks or four: a straight climb from nothing is four picks (tier 1, tier 2, tier 3, keystone), but the locked line says three. Claude's reading, his to change: a limb's third pick lends its tier 3 talent and its keystone together.
- The screen says keystones open at campaign tier 3, the 28 Oct section says tier 4.
- Whether a climb on the road can reach a keystone before its campaign tier opens it.
- What a talent does in code while the thing it boosts is not built (the Shade's forms, the Whisperer's picks).

## Jacob's playtest fixes 3, 7 and 9, owner decisions, 31 Oct 2026

Jacob played rules 19 and found no challenge, no use for Fall Back and a boss that was too easy (HANDOFF_LOG 12785, 12797). Claude proposed fixes 3, 7 and 9; Jacob: "Go, lock and build". Where this differs from the sections above, this wins, including one line of the parry at the center (24 Oct) and of rules 9, for blasts only. Every number is a starting value.

**Fix 7, blasts.** Big moves are swings or blasts.
- Swings: Cleave and the heavy swings. A Hold soaks them, as today.
- Blasts: the Eclipse, Brand and Burning Ring. A Hold that is not a parry soaks nothing: only a parry in the last half second or a Fall Back saves you. This replaces "Lose it, too early: you only soak" for blasts. Sun Mantle's flare already lands unless parried, and its burn is answered as today; a carried Burning Ring already ignores a Hold.
- Risk, reward, punish: a parry pays the most (the stagger, the Riposte, a nerve crack) but needs the timing; Fall Back is safe but stops your attacks for 2.5 s; a Hold no longer works on a blast.

**Fix 3, threat on the road.** Bloodridge's road bands carry their chief's moves from the first road fight, not only from the fourth row. A carried move you don't answer takes a third of the health of each fighter it hits: a carried Cleave a third of its target's health, not twice a blow; a carried Brand and Burning Ring a third, not 10% and 15%. Heavy swings stay small beats (rules 9).

**Fix 9, the boss.** His swings and blasts take turns: after a Cleave, his next Cleave waits until one of his blasts has gone, so holding Hold all fight stops working. An unanswered Brand takes 35% of a fighter's health, not 20%, and a Burning Ring 35%, not 25%. How he reads on screen is the art and screen side of fixes 2 and 9.

**What gets built, behind campaign rules 21, on this yes.** The blast rule, the carriers from the first fight and their size, the turn taking and the two sizes, one quick test per piece, and the old against new proof below rules 21.

Still open: every number, and the other tribes' chiefs, whose moves are not sorted into swings and blasts yet.

## The Shade's core: two buttons become tricks (builds step 4), owner decisions, 31 Oct 2026

Step 4 of the builds lock, the second Pal. Jacob: "lets design the pals and their core then move onto mapping systems into them", then "yes go shade". Claude read the Shade sections above (28 Oct), Codex's reports/Shade command specialisations.md and her audit, and found her core already locked in all but name. Jacob: "Lock it". Where this differs from the sections above, this wins. Every number is a starting value. Nothing here is built or played.

**The decision.** The Shade's core is two of your four buttons becoming tricks. In one line: press the button and it does her trick too, and three picks of one route are its final form.
- Each button has two routes, and you enhance at most two buttons, one route each: Hold, Mirror (send the hit back) or Sanctuary (a clean parry shields the squad); Fall Back, Ghost Step (a decoy takes a hit) or Rift Mine (blasts whoever follows you); Focus Fire, Rupture (build a weak point, press again to pop it) or Death Warrant (a kill passes Focus on); Unleash, Breach (shove their front line) or Echo (store hits, release them later).
- Each route has three forms, as in Codex's paper and the 28 Oct names. A road pick of a route you hold gives its next form, so leaning into her climbs your two buttons to their final forms. The final form is her payoff, as a keystone is the Glutton's.
- Every route keeps its price from the paper, for example Sanctuary gives up the parry's stagger and Echo halves Unleash's instant bonus.
- Her two buttons do not link: one clear trick per button, as x + y = z. A link between them can come later, as a duo would.
- She is the answer to two of the playtest's complaints: her Fall Back routes give Fall Back a job, and Rupture makes Focus Fire's effect visible. Her Fall Back routes sit on top of fix 7 above (a Fall Back dodges a blast and stops your attacks for 2.5 s).

**Risk, reward, punish.** Reward: two buttons at their final forms late in the run. Risk: the road must keep offering her, and each route carries its price the whole time it is equipped. Punish: a pick into a third route you can't equip, or a chief who answers your trick (F7 lets you park a button before a chief fight).

Still open:
- Command Relay: the 28 Oct lock moves one button a tier early, while Codex's paper says it now halves one button's weakness instead. Jacob's call.
- Nothing of hers is in code; her road offer is still the old placeholder favour on Fall Back. Rift Mine and Breach were written for space and the sim has no positions, so both stay the timing versions of finding F4.

## The Whisperer's core: one star fighter (builds step 4), owner decisions, 31 Oct 2026

Step 4 of the builds lock, the third of the other three Pals. Claude read the Whisperer sections above (28 Oct, the audit's fixes, the road flip, fixes 2 and 3) and her locked screen (pod round 6, Art/Studies/Talent-Tree-20261027/whisperer-round6-pod.png), and proposed her core. Jacob: "Go, lock it". Where this differs from the sections above, this wins. Every number is a starting value. Nothing here is built or played.

**The decision.** The Whisperer's core is making one fighter your star. In one line: two picks on one fighter are his spell twice, then a second slot.
- Her road pick is a habit for one of your classes, a pair, or a rank up for one fighter (28 Oct flip).
- Leaning into her ranks one fighter. Rank 2: his class spell happens one more time (the Warden Guards two allies, the Skirmisher strikes twice, the Marksman fires two shots per aim, the Trapper's trap springs twice, the Medic treats twice in a row). Rank 3: a second slot, so he carries two pieces, for example two Butcher combos. That slot is her payoff, as the keystone is the Glutton's and the final form the Shade's.
- His habit and the squad's pair shape how the star plays and who backs him up: Pin and Shoot sets up the Marksman's crit.
- Her Chaos trait: excess is one fighter getting far more than he should, and charm is her pairs bonding classes.
- With the Butcher: a rank 3 fighter can carry two combos of one flavour, so he lays twice the stacks and bursts sooner.

**Risk, reward, punish.** Reward: one fighter doing double, then carrying two pieces. Risk: everything rides on one fighter. Punish: a fighter who falls in a lost fight loses his top rank (28 Oct audit), and a captured one loses his rank.

Still open:
- The price lines above (rank 2 for 25, rank 3 for 75 materials) are stale: since the 28 Oct flip ranks are road picks, not camp buys.
- Habits, pairs and fighter ranks are not in code; her road offer is still the old placeholder favour. Bonds and chains are built (rules 11), and her pairs bond on them.

## The parry meter, owner decisions, 31 Oct 2026

Jacob asked for it on 26 Oct (HANDOFF_LOG 11823: each parry fills a segment, three let you cast the chief ultimate, and parrying makes the squad's chains stronger), and the Butcher's lock of 28 Oct counted on it ("Held parry bars make combos stronger: +10% per held bar ... Casting the chief ultimate spends the bars"), but it was never locked on its own or built. After his rules 19 playtest ("i mean where is break meter you parry 3 times to full gauge and then use chief spell ???????") Claude wrote it up from his words and that lock; Jacob: "Go, lock and build". Where this differs from the sections above, this wins. Every number is a starting value.

- Your side has a meter of 3 bars. Every parry you land fills one bar, up to 3: a big beat, a small beat or a cast, and each beat of a move with several. A move of three beats parried three times fills it in one go.
- With 3 bars you can cast your chief spell, and casting it spends all three. It no longer costs order points. Below 3 bars it can't be cast.
- Each bar you hold makes your combos and cash ins 10% stronger, up to 30% at three (the Parry damage layer, on any of your hits with a Combo or a Cash bonus). So you choose: hold the bars for stronger payoffs, or spend them on the big spell.
- A miss never takes bars away, and a Fall Back earns none.
- The meter is your side's only; the enemy's spells keep their own rules.

**Risk, reward, punish.** A parry is the risky answer, and now it pays twice: the stagger and Riposte it already gave, and a bar. Fall Back and a soak stay safe but build nothing.

**What gets built, behind campaign rules 22, on this yes.** The meter on your side, its fill on every parry, the chief spell cast with the bars, the 10% a held bar, one quick test per piece, and the old against new proof below rules 22. Codex draws the meter and the spell's button.

Still open: every number, and whether the meter carries from fight to fight (it starts empty every fight for now).

## The floor under the three Pals, owner decisions, 31 Oct 2026

After the four Pal cores were locked (the sections above from line 1049) and the sim was mapped onto them, four holes stood under the Glutton, the Shade and the Whisperer. Claude gave three options for each; Jacob picked: "1.B", then on the second slot "i dnt know what to do here what adds more build variety ?" and "2.A", "3.B", the shield ("i hate them all except shield"), then for the two he rejected, from three new ideas each, "ghoststep backstab" and "death warrant fear", then "Go, sprint it". Where this differs from the sections above, this wins. Every number is a starting value.

**1. A store for each Pal's picks.** The run keeps each Pal's picks in its own place, each with its own save lines: the Glutton's lent talents (Fed or not), the Shade's routes on her two buttons with each one's form, the Whisperer's habit for each class, the squad's pair and each fighter's rank. They go when the run does. The Butcher keeps his combos as gear pieces with ranks, as built.

**2. A full second slot.** At rank 3 a fighter carries two pieces, two combos of his class included, for example a Marksman with Black Hole Round and Stopped Bullets, pull and time on one fighter. Both fire, both pacts apply, and each lays its own flavour's mark at its own rank.

**3. Faster and cheaper, one hook in each central spot.** A fighter's clocks (his spell's turn, his aim, his cooldown) all pass one percent, and your side's order costs pass one percent, so a talent that makes something faster or cheaper sets that percent. Bigger stays in the damage layers.

**4. The Shade's three tricks.**
- Sanctuary's shield: a shield on a fighter soaks hits before his health, then breaks.
- Ghost Step is now Backstab: on a Fall Back your squad vanishes and steps out behind them, and each fighter's next hit lands double, in the Cash layer. This replaces the false formation that caught a hit.
- Death Warrant is now Fear: killing the enemy under your Focus Fire cracks the nerve of the enemies beside him, and the one with the least nerve left panics. This replaces passing Focus on and carrying the overkill.

**What gets built, behind campaign rules 23, on this go.** The four stores and their save lines, the second slot through the sim and the save, the two hooks, the shield, Backstab and Fear as each route's first form, one quick test per piece, and the old against new proof below rules 23. This is the floor only: the Glutton's 35 talents, the Shade's other routes and forms, the habits and pairs, and every Pal's road offer are built on it later.

Still open: every number; the Shade's second and third forms of Sanctuary, Backstab and Fear; what the Glutton's Nothing Wasted keystone becomes, now that Death Warrant carries no overkill.

## Fear on a break too, owner decision, 31 Oct 2026

The rules 23 build found that in a road fight enemies mostly break on nerve and flee before anyone kills them, so Fear, which went off only on a kill, would rarely fire on the road (HANDOFF_LOG line 12983). Jacob: "Yes, on break too", then "Go, do it". Where this differs from the section above, this wins: Fear goes off when the enemy under your Focus Fire falls, killed or broken and fleeing. A melee enemy whose nerve runs out makes a last stand instead of fleeing, so for him it still takes the kill.

## Habits come with the game, the Whisperer upgrades them, owner decision, 31 Oct 2026

Claude's Whisperer proposal kept the 28 Oct road flip's habit card. Jacob: "arent habits come with base game not an upgrade and then these habits can be upgraded that will be on point". Claude redrew it that way, and Jacob said "Lock it". Where this differs from the sections above (the Whisperer's habits, the road flip's Whisperer offer and camp unlocks, and "The Whisperer's core"), this wins. Every number is a starting value.

- **Habits are part of the game.** Every class has its two habits from the start. At camp you set one per class, free to switch. A class's second habit is no longer a camp unlock. This closes the open point of which habit run 1 offers.
- **The Whisperer's road card** is a rank up for one fighter, a pair, or a habit upgrade. A habit upgrade lasts the run. Claude's reading, his to change: her fights offer upgrades only for the habit each class has set.
- **What each upgrade does:**
  - Bodyguard (Guards the Marksman while he aims): Guards every back row fighter while they aim or cast.
  - Rescuer (Guards whoever has the least health): the Guard jumps the moment someone else drops lower, with no waiting.
  - Breaker (strikes whoever is winding up a big move): her strike sends his wind up back to the start.
  - Avenger (strikes whoever last hit one of yours): she goes at once even if her spell is not ready, once per cycle of her spell.
  - Counter Shot (shoots whoever last hit your back row): no aim, he shoots back at once.
  - Spotter (shoots whoever is winding up a big move, leaving an Opening): the Opening stays until someone cashes it.
  - Ambusher (her trap waits for a heavy blow): the heavy blow that springs it is stopped.
  - Triage (waits until someone is under a quarter health): that saved treatment lands twice as big.
  - Front First (treats the front row first): her treatment reaches the whole front row.
  - Gatekeeper stays out of the slice.
- The Whisperer section's rule that nothing she gives changes a number or adds an effect no longer holds for habit upgrades.

Still open: whether a habit can be upgraded more than once; what counts as a heavy blow for Ambusher; the rest of Claude's 31 Oct Whisperer proposal (a rank up for a fighter already ranked weighing 2 in her offer, and building her behind campaign rules 24) waits for his go. Nothing here is built or played.

## The sand shark's look and land boss scene, owner decision, 1 Nov 2026

Jacob on 24 Oct picked PixelLab's shark 2 ("bear 1 and refine shark 2"), then asked "if i told you the shark can breathe out of water", so it hauls out and fights on land. Later he asked for the bite to be redone and to "put half fish in ground half on top". On 31 Oct he rejected it walking on land ("its walking is weird as fuck") and said "make it behave the same way it would underwater", then "sweet" to the result, and on 1 Nov picked "Lock the shark". This look replaces the 23 Oct brief's look where they differ. The fight rules stay as built.
- Look, drawn in PixelLab: a huge dusty red shark with cracked, scaly hide, a row of bone spines down its back, old harpoons and spears with red rags stuck in it from generations of Clarth hunts, a cream belly and jaw, and rows of yellow teeth.
- Size: drawn at 256 px and shown at 2x, so it is as big as your whole squad. It stands alone on its side.
- On land it sits half in the sand. The sand line cuts it just under the jaw, so its jaws, back and spines show and its belly, front fins and lower tail are under. A rim of sand moves along the cut all the time: a rippling lip, a wave of sand piled ahead of whatever part pushes forward, a furrow behind that fills back in, and sand pouring off its back.
- It never walks. Anywhere it moves, it moves the way it does under the sand.
- What it does on screen: its fin and shadow patrol under the sand, gliding and kicking; it bursts out, flies, and lands with a splash along its whole length, sinking to half depth; it breathes, gills working; it dives head first, swims under the sand to your squad as a fin and shadow, and bursts up half in the sand in front of them; it bites, its jaws clamping shut on your front row, then thrashes; it spins and its tail sweeps a blast of sand spikes through all five of your fighters; it dives head first and its fin patrols again.
- Files: Art/Studies/Beast-Concepts-20261022/directions/anim2 (shark-land-256.png and the shark_haul, shark_breathe, shark_bite2 and shark_sweep clips of 40 frames), the scene in make/shark_land2.py and the video shark-land2.mp4, on the branch art/beast-bosses. The video is not on GitHub yet.
- Open: in the sim the Bite hits whoever stands in front and the Sweep hits the front row; in this scene the bite takes the whole front row and the tail blast all five. The scene has no Volley. Whether the scene's hits become sim moves is not decided.
- Not built: anything in Unity. Its pixels are twice the size of your fighters' pixels.

## The beasts hit as their scenes show (rules 25), owner decision, 1 Nov 2026

Jacob picked "Sim follows scenes" for the open lines in "The cage bear's look and boss scene" and "The sand shark's look and land boss scene", then "Go as written" to Claude's three rules. Built in the sim as campaign rules version 25 (HANDOFF_LOG has the details). This answers both open lines. Every number is a starting value.
- The sand shark's Bite takes everyone in the marked fighter's row (your front row, your back row once the front has fallen), 18% of each one's health, instead of one fighter. The answers stay: a Hold as it lands parries the whole Bite; a Warden takes the share of whoever he covers on his shield, his own too, and the jaw takes half of each share back; any other Hold soaks the front row.
- Its tail's Sweep hits all five, both rows, 10% each, instead of your front row. Fall Back dodges it, a covering Warden halves a share, and the Medic heals through it.
- The cage bear's Grab becomes Slam and Grab: an ice wave on all five, 10% each, then the claw grabs the one it marked, as before. Fall Back dodges the whole move; a covering Warden halves the wave and blocks the grab.
- Dive, Volley, Roar, Crush and Pulse do not change. Older fights, saves and replays keep the old rules.
- Not done: the fight screen's move guide still describes the old hits, and nobody has played it.

## The parry: tight window, big payoff, a gold flash on the attacker, owner decisions, 1 Nov 2026

After his rules 19 playtest ("parry idk what im parrying or why or when"), Jacob set the bar: "it should be risky with a lot of reward and parry window should be very less", like Expedition 33, Sekiro, Ghost of Tsushima and Nioh 2, "parry is very imporatnt but its not everywhere"; the cue must work "irrespective of units and act like a universal visual cue"; and every fight keeps parry chances ("then by your proposal all other fights will be boring since no parry"). Claude mocked three cues on the attacker as clips on his fight with the chief's own sprites and the game's sounds (a glint on the weapon, a gold flash of the whole body, a floor ring closing to the feet). Jacob picked "2", then "Go, lock it". Where this differs from the sections above, this wins. Every number is a starting value.

- **The window is 0.2 s.** Hold in the last 0.2 s before a parryable blow lands and it is parried. This replaces Perfect Hold's last half second.
- **Parry chances in every fight, but not on every blow.** Band heavies, chief moves and carried moves are parryable; normal blows never are. The 24 Oct line stays: nothing big is unparryable.
- **Reward.** A parry cancels the move, staggers the attacker for 2 s, fills one bar of the parry meter, and counters for the move's damage.
- **Risk.** A Hold that misses the window lets the blow land in full, and your squad takes 50% more for 1 s.
- **The cue: the attacker flashes gold.** During the 0.2 s window the whole body of whoever is swinging flashes gold, twice, with a normal frame between. The same flash on every unit, so it reads the same on a band heavy, a carrier or a chief. Nothing on the HUD carries the cue. The landed parry gets a flat white frame on the attacker, a clash spark where the blow is stopped, a short freeze and shake, the game's parry sound, then he reels on his hit frames and a meter bar lights.

**What gets built, when Jacob says go (this lock is not a build order).** Claude: the window, the parryable blows, the reward and the risk behind a new campaign rules version, with the sim telling the screen when each window opens and closes and on whom, one quick test per piece, and the old against new proof. The final flash, spark and white frame are drawn with the PixelLab workbench, not taken from the ComfyUI mock. Codex: the flash on the attacker, the landed parry stack and the meter on screen.

Still open: every number; whether tribes that learned your parry (14 Oct) keep holding their casts against a 0.2 s window; the colour of the flash for a blow you must not parry, if one is ever added.

## The beasts as raid bosses (rules 27), owner decision, 1 Nov 2026

Jacob: "can we finish the bear and shark just like a wow boss", then "Full plan" to Claude's plan and "Go as written" to its rules. Built in the sim as campaign rules version 27 (built as 26, then renumbered the same day after the tight parry took 26; HANDOFF_LOG has the details). Every number is a starting value.
- Sand shark, Feeding frenzy: when its second part breaks it dives for 4 s; its moves stop and nothing reaches its parts. For the last 3 s the sand churns under all five, then it bursts up through all five for 20% of each one's health. Fall Back dodges it; an armed Trapper's trap snares it on top instead, nobody is hit and its parts take double damage for 4 s; a covering Warden halves a share; the Medic heals through it. Then its Blood frenzy, as built.
- Sand pups: as it bursts, 2 small sand sharks come up and latch onto your back row, one each, biting whoever they hold for 2% of their health every second with a bleed until you kill them. Each has a quarter of its parts' average health; they can be targeted and Focus Fired, give no trophies, and die with the shark.
- Cage bear, Overload: when its ribcage breaks the skull bolts it and it roars (its Roar's nerve hit, which nothing blocks or cuts off), then frost cracks race across the floor for 4 s. Anyone of yours not in Fall Back as they land is frozen for 3 s and does nothing.
- Its Last stand: each Pulse hits 20% harder than the last.
- The screens: the sim gives each intermission's name and time left, the warning lines ("THE SAND CHURNS: FALL BACK", "FROST CRACKS: FALL BACK"), the pups and whom they hold, and who is frozen, for the boss bar, cast bars, warnings and phase banners.
- Not built: those screens and the pups' art (a PixelLab clip). Nobody has played it.

## The Shade's forms and road card, owner decisions, 31 Oct 2026 (cloud chat)

After research on kit upgrade systems (Outriders, World of Warcraft, Ori, Hollow Knight, Vampire Survivors, Hades), Jacob in a cloud chat picked "Finish her routes" over replacing them, said "go" on Claude's forms for the three routes built in rules 23, and on 1 Nov said "lock it". Where this differs from the sections above, this wins. Every number is a starting value. Built behind campaign rules 25 (HANDOFF_LOG, "Hand back: the Shade's forms").

- Sanctuary (Hold): 1 Shelter, a clean parry shields every fighter; 2 Relay, the field also makes one shared pool that soaks what a broken shield could not; 3 Last Refuge, once a fight a killing blow on one of yours under the field leaves him at 1 health and shatters the field. Price: your parries no longer stagger.
- Backstab (Fall Back): 1 Backstab, each fighter's next hit lands double; 2 Twin Knives, the next 2 hits; 3 Shadow Strike, Fall Back no longer stops your attacks (this sets aside fix 7's pause for her side). Price: the next Fall Back waits 2 s longer.
- Fear (Focus Fire): 1 Fear; 2 Spreading Dread, an enemy Fear breaks sets Fear off again around him, up to 2 more times; 3 Turncoat, the enemy Fear panics hits the enemy beside him once. Price: Focus Fire lasts a quarter shorter.
- Her road card: three picks from the node's seed, a new route on a plain button or the next form of a route you hold; held routes weigh 2, new ones 1; with both buttons taken only next forms show. No tier gates or camp unlocks for now (the paper's 150 and 250 can come back later).

## The Shade finished: her other five routes, owner decisions, 1 Nov 2026

Jacob: "finish shade", then "lock it". Built to the forms already locked (31 Oct, "as in Codex's paper and the 28 Oct names") in the sim's timing versions (finding F4: a push is a delay, nearby is the one beside him). Where this differs from the sections above, this wins. Every number is a starting value inside the paper's ranges. Built behind campaign rules 25 (HANDOFF_LOG, "Hand back: the Shade finished").

- Mirror (Hold): Return, a clean parry sends a share of the parried one's health back at him; Catch, times the parries in a row, up to 3; Pierce, half of it also goes through him into the first of his side behind him. Price: only a clean parry returns.
- Rift Mine (Fall Back): Mine, the first enemy who strikes one of yours within 5 s is blasted; Shock, his next turn waits 1 s; Wide Blast, the ones beside him too. Price: your attacks wait 150% of the Fall Back.
- Rupture (Focus Fire): Weak point, your hits on the target open up to 3 steps, and a second Focus Fire on him ruptures it; Crack, a full one thins his armour; Finish, your fighters about to act act now and wait that much longer after. Price: changing target, or the Focus running out, loses the steps.
- Breach (Unleash): Impact, it hits up to 3 of their front row and makes them wait; Gap, those it hit cannot cover anyone; Ram, a second Unleash ends it and rams them, harder the more is left. Price: your side takes 20% more damage while it runs and 1.5 s after.
- Echo (Unleash): Record, your fighters keep 35% of their hits and replay them when it ends; Release, a second Unleash lets them go at once; Converge, that release sends them all at your Focus Fire target. Price: Unleash's own bonus is halved.
- The second presses (rupture, ram, release) are free, never fire the plain order (finding F3), and are refused with nothing to cash.
- Her road card offers all eight routes.
- Claude's calls, his to change: Mirror returns on every clean parry, not only after a move's last beat; Rift Mine is set off by blows only, not a chief's moves; Finish skips Medics; Echo aims at the first one each fighter recorded.

## The Glutton built: his tree, road card, Feed and talents, owner decisions, 1 Nov 2026

Jacob: "lets move onto glutton", "go" on Claude's plan and nine calls for the open points of "The Glutton's theme: one limb" and the tree locked on 28 Oct, then "lock it". Where this differs from the sections above, this wins. Every number is a starting value. Built behind campaign rules 26 (HANDOFF_LOG, "Hand back: the Glutton").

- His calls: 1, a limb's third road pick lends its tier 3 talent and its keystone together, so three picks climb a limb. 2, the road can lend a keystone before its campaign tier; only the camp tree keeps the gate. 3, Nothing Wasted: Marksmen hit your Focus Fire target twice as hard. 4, Command Relay: the Shade's route prices are halved. 5, Deep Node: the Shade's tricks hit a third harder. 6, Synthesis Discount: the Butcher's pacts cost a third less; Gene Library: pairs answer twice as often. 7, Node Cache: every Pal card shows 4 picks. 8, Glancing Fold: a Hold that soaks instead of parrying soaks half as much again. 9, talent points: one for each codex first (a tribe met, a tribe won over, a chief met, a chief's hidden chain, a beast slain).
- The camp tree: talents cost 1, 2 or 3 points by tier and a keystone 5, bought at camp in each limb's order, one of the tier 3 pair, tier 3 from campaign tier 2 and keystones from campaign tier 3 in the slice; reset is free; owned talents stay between runs and ride into every run. The slice pays about 22 points, about two limbs.
- His road card lends the next talent of each limb you don't hold, for the run; a limb he lent from this run weighs 2. Feed, once a run, doubles one talent's bonus and eats another of its limb for the run (nothing, when you hold no other there).
- The limbs are roles: front line your front row, strikers your Skirmishers, shooters your Marksmen, control your Trappers, healers your Medics, command your side, war chest the run. Each talent makes something you have bigger, longer, faster or cheaper, as written in GluttonRules.Describe.
- Claude's calls, his to change: Probability Lock adds 25% damage on a snared enemy (its old "can't miss" already held); Fold Step makes Execution Corridor hit 50% harder (no wait was found to cut); Gravity Mesh is all or nothing; a talent you own is never lent again.
- Still open: beast parts broken and chief moves seen are not codex entries yet, so they pay no points; how many points the whole codex gives past the slice.

## Renumbered on the merge with game-one, 2 Nov 2026

Jacob: "merge first, then build the duos on top. Go". game-one took campaign rules 25 (the beasts as their scenes show them), 26 (the tight parry) and 27 (the beasts as raid bosses) while the cloud chat built its own 25 to 30. The cloud chat's move up by three, in the same order, with nothing else changed: the Shade's forms and her other routes play from 28, the Glutton from 29, the Pals' duos from 30, the sizing pass from 31, the Butcher's flavours from 32, and the Whisperer's vows with the Butcher's flavour fights from 33. The sections above keep the numbers they were locked with.

## The parry meter's look: three brass suns, owner decision, 2 Nov 2026

From three looks for the parry meter on the fight HUD (gems, a molten bar, sun discs), Jacob picked "Look 3, suns", then "Go, free cut" (PixelLab's subscription had run out). Where this differs from the sections above, this wins.

- The meter is three small brass sun discs on one dark iron bar. A filled bar is a lit gold sun with rays; an empty one is a dull bronze socket. They light left to right, one per parry.
- The art is 144 x 32 art px (the meter's own size on the HUD), four states: none, one, two and three lit. Cut from the approved ComfyUI mock, one shared palette for all four.

Still open: whether the empty discs should be darker; final PixelLab art once the subscription is back.

## A warning before the parry flash, owner decision, 2 Nov 2026

Jacob played the Pals30 build: "there was no cue to parry". His road fight opened seven parry windows on raiders' heavy swings, and the gold flash does draw (Codex's capture 26-attacker-gold shows it on the chief), but half a second of gold on one small raider out of six, with nothing before it, can't be caught. Claude proposed a warning step; Jacob: "Go, add the warning". Where this differs from "The parry: tight window, big payoff, a gold flash on the attacker", this wins.

- **Rim means coming.** From the moment a parryable move starts winding up, at most 1 s before its beat, the attacker wears a steady gold outline (the flash's own rim, no fill). Every unit, the same.
- **Fill means now.** In the 0.2 s window the existing double gold flash plays as locked.
- A move you can't parry gets neither. The sim's rules and numbers don't change: the screen reads both steps from Threat (ActorId, BeatTicks, WindowTicks, Answers).

Still open: whether 1 s is the right lead; whether a fake or a wait out keeps its rim.

## Marks you can read and one metal for the parry, owner decisions, 1 Nov 2026

Jacob's playtest point 5 ("combos firing and synergies cant see") and the parry's sound. From the reference board of 31 Oct (one row each for the parry tell, the marks and the parry sound), Jacob picked marks 3 and sound 1 ("1. 3 1"), then "Go, lock and make". Where this differs from the sections above, this wins. Every word, colour and number is a starting value.

**Marks 3: name the cash in.** The chain that does most of the damage is the brightest, slowest thing on screen, and its payoff is named where it lands.
- The shot or blow that lays a mark travels slowly enough for the eye to follow it from the setter to the target.
- When another fighter spends the mark, one short word pops at the target in that mark's colour, with the hit's number bigger than a normal hit's. One word, never a sentence (Jacob's playtest point 4: too much text). The words, in the game's 32 px pixel font: a snare cashed, PINNED; an Opening cashed, OPENED; a bleed ripped, RIPPED; the Butcher's Crush bursting, CRUSHED; a blow that spends wide open (Exposed), EXPOSED; a Stasis burst, FROZEN.
- The colours, from the fight screen's own palette: PINNED mint, OPENED orange, RIPPED red, CRUSHED lilac, EXPOSED blue, FROZEN yellow. The parry's gold stays the parry's alone.
- Copy risk: floating element glyphs read as Genshin, so these are our own words in our own font, no icons.

**Sound 1: one metal, two tones.** A perfect parry is a bright, ringing clang. A Hold that misses the window is a dull clunk from the same metal. You hear which one you got without looking. From rules 26 a Hold out of the window is a whiff, not a soak, so the clunk is the whiff's sound.
- Copy risk: Sekiro's clang is famous, so ours must not sound like it.

Still open: every word, colour and length; whether the counter Hit gets its own sound apart from the riposte.

## Focus Fire and Fear you can read, owner decision, 2 Nov 2026

Jacob's playtest point 6 ("idk if focus fire does somehting or no"), and his Pals30 play, where his Focus ran out 0.8 s before its target died so Fear never fired and nothing said so (Codex's review, HANDOFF_LOG 13423). Claude proposed four steps; Jacob: "Go, lock it". Screen only: no rule or number changes. Where this differs from the sections above, this wins.

1. **While it runs.** The red Focus crosshair stays over the focused enemy's head, with a ring round it that drains as its time runs out.
2. **Proof it works.** Your hits on the focused enemy pop bigger, in red, and a running total of them sits under the crosshair.
3. **How it ends.** He falls in time: the crosshair snaps shut with KILLED. He breaks and flees in time: BROKE. Time runs out first: it fades grey with RAN OUT.
4. **With Fear.** With Fear equipped the crosshair has a purple edge, so a kill will spread panic. When Fear fires, the panic shows on the enemies beside him. When the Focus runs out first, it ends NO FEAR, in grey.

At most one short word, and only when a Focus ends.

Still open: every colour and length; whether the running total should count only hits that landed during the Focus.

## Beasts as rolls, tier 1 to 50, owner decision, 2 Nov 2026

Jacob: "integrate beasts as rolls they appear in 1 in 5 run pure randomness every time it comes back after defeat it will be even more powerful tier 1 to tier 50 do what you want with it". Design only, nothing built. Where this differs from the beast sections above, this wins. The details below the first two points are Claude's calls, his to change. Every number is a starting value.

- **A roll, 1 in 5 runs.** When a run starts it rolls once: a 1 in 5 chance that a beast shows up on that run's road. Pure chance for the player; in the sim the roll comes from the run's own seed (a stream nothing else draws from), so a saved run always keeps its beast or its lack of one.
- **It comes back stronger.** Each beast has its own tier, 1 to 50, kept at camp between runs. It starts at tier 1. Every time you beat it, its tier goes up one, and the next time it rolls in it comes back at that tier. Losing to it leaves its tier as it was. At tier 50 it stays at 50.
- Claude's calls: which beast is a second roll, even between the beasts built (the cage bear and the sand shark). It takes one fight node in a random row of the road, never the chief's, marked on the map so you can go round it.
- Claude's calls on power: each tier above 1 adds 8% to its parts' health and 5% to their damage (tier 50 is about 5 times the health and 3.5 times the damage of tier 1). Every 10 tiers (10, 20, 30, 40, 50) it adds one step of its raid kit: its intermission comes at a higher health, its last stand hits harder, and from tier 30 the shark brings one more pup and the bear's frost lasts a second longer. The parry window and the warnings never change, so it stays readable.
- Claude's call on reward (risk, reward, punish): its trophies and materials grow with its tier, so the higher it climbs, the more beating it pays. Punish: it is a real wall at high tiers, and going round it pays nothing.

Still open: every number; whether the tier shows on the map before you commit; whether a run can roll two beasts later in the campaign.

## The leaders, the champion and the Warden in the middle, owner decisions, 8 Nov 2026

Jacob on the big moments clip (rules 42, seed 5100 road fight 9): "it is okay" and the lunges are coherent, then "the leader of the other tribe shouldn't be attacked, we should always go for the units. Once the units are done, the leader automatically retreats", "Warden should always be in the middle, guarding the party", "if the warden has low stats on defense and all, he is more prone for making the other units come through" and "The leaders cannot be touched until he is alone in the last stage where he has three phase or four phase fights". Then, on Claude's route: "the sim side is actually fine", "in stage 4, instead of leader, the champion steps in and takes control of all the units and we have to kill the champion to go to the tribe's leader", "the 3 phases, I think you already designed them in another chat", "our warden holds the middle and his defense decides how many enemies blows get past him. Yes", "every unit should get their own art and you have full permissions to use Pixel Lab", and for the three older pieces that hit a chief while his band stands, "you can move them to a leader solo phase or you can actually attach them to the champion".

- A tribe's leader can't be hit while any of his fighters stand. Units go for units. When the last one falls, he retreats and the fight is won.
- Stage 4, the last fight of a tribe's road: the tribe's champion steps in instead of the leader and leads the band. Kill him and they break, and you go on to the leader.
- The last stage is the leader himself, in the three phases built for Bloodridge's chief as rules 10 (25 Oct): learn him, his transformation at half health, his desperation under 30%.
- Your Warden holds the middle of your squad and guards everyone. His Defence decides how many enemy blows get past him.
- Every unit gets its own move art, made in PixelLab.
- Built on 8 Nov as campaign rules 43 (HANDOFF_LOG, Claude's entry that day). Claude's calls, his to change:
  - A leader is any chief role on the far side: a tribe's chief, the hunt captain, a trial's shaman. He still fights and casts while he can't be hit. He steps around traps, nothing chains at him, and he covers nobody.
  - In a chief's own fight he stays when his band falls and fights on alone, so his half health phase, his kneel and his worn out turn after his spell all come once he is alone: the three older pieces moved to his solo phase. Turned Hunt now turns on the hunters: for its 6 s nobody covers them and your blows on them land double.
  - The champion is his tribe's duel champion (its first Warden or Skirmisher) with a chief's power and a lieutenant's health, in the front row. He takes one fighter's place in the band (a trial's shaman makes way for him instead), winds up his tribe's spell at a duel's grade for your leader to parry, and can be hit from the start. Stage 4 is the last row of the road, however long it is (the fourth on Bloodridge's staged road).
  - Both sides' Wardens hold the middle. Only melee blows meet him, his chance is half his Defence (50% for a Warden), and a snared, stunned, panicking or kneeling Warden holds nothing. A blow he holds lands on his shield as a Guard's does.

Still open: the other leaders' three or four phases (only Bloodridge's are built); the champions' own looks and moves (they borrow the duel champion); every number.

## Grid rules and kit move swaps, owner decisions, 9 Nov 2026

Jacob, 9 Nov, after the champion fight films: "since they are behaving like grid based battle ground.. why not use megaman battle network rules not they jsut teleport where they want then wahts the point of having traits when unis can behave how ever thy want". Claude's call: each band fights on its own 3 by 3 panels (back, middle and front rows across three lanes) and every class gets a panel shape, built as a new rules version, with old saves on their old rules. He picked "Page first", then said "i wanna see them in action", then asked "cant we alter the way they do their attacks like warden puts up ghost shield etc ? some one buffing up the group etc ?", picked "Five first", said "i kinda like them all" on the kit moves page, and "Yes, Whisperer" to the call below.

- The five built classes' defence and boost moves each have three versions on the Grid Kit Moves page (https://claude.ai/artifact/2uVXB4jiJgUmKnEotPE8f4). Option 1 is the move as the sim plays it today, put on the grid. Options 2 and 3 are new: Ghost Shield and Ghost Wall for Brace, Bash and shove and Shield Line for Shield Bash, Roll aside and Behind cover for Step Back, Ricochet and Spotter for Steady, Afterimage and Duck back for Sidestep, Scissor and Open season for Twin Cut, Front snare and Spiked guard for Snap Trap, Double set and Caltrops for Rearm, Patch and slip and Shared patch for Self Patch, Stim lane and Stim panel for Stim.
- Every class starts each run with option 1. Options 2 and 3 are the Whisperer's road picks, since unit upgrades are his job (27 Oct) and every Pal pick is on the road for one run while camp only unlocks (28 Oct): one swap per move per run, and camp decides which swaps can appear.

Still open: the grid itself is Claude's proposal and not built; the Grid Class Shapes page (https://claude.ai/artifact/TAmC7bXYBJ9Re3q6CbhbgK, three shapes for each of the 17 classes) got its picks the same day, below; the other twelve classes' kits; how a swap is offered and priced next to the Whisperer's habits, pairs, traits and ranks; every number.

## Grid class shapes, owner decision, 9 Nov 2026

Jacob on the Grid Class Shapes page (https://claude.ai/artifact/TAmC7bXYBJ9Re3q6CbhbgK): "they all are good again", then "Yes, lock it" to Claude's call.

- Every class's base shape is option 1, except the Warden's, which is option 2, Guard the cross: he stands in the centre panel and takes the blows aimed at the four panels touching his, because the 8 Nov lock puts him in the middle guarding everyone.
- Each class's other two shapes are the Whisperer's road picks, one swap per run, the same rule as the kit moves above.
- The bases: Warden 2, Guard the cross. Vanguard 1, Ram the lane. Duelist 1, Across. Berserker 1, Wide swing. Skirmisher 1, Lane dash. Marksman 1, Lane shot. Artillerist 1, Square. Infiltrator 1, Flank lane. Trapper 1, Their front. Recon 1, Lane scan. Medic 1, Touch. Herald 1, Ring. Chemist 1, Square cloud. Field Engineer 1, Lane sentry. Disruptor 1, Lane pulse. Binder 1, Reel in. Conduit 1, Lane beam.

Still open: building the grid as a new rules version (each band on its own 3 by 3 panels, Claude's proposal the same day, not built); how swaps are offered and priced next to the Whisperer's habits, pairs, traits and ranks; the other twelve classes' kits; every number.

## Locked pairs, owner decision, 9 Nov 2026

Jacob on Codex's grid plus contact mock (Builds/KindredSiege-Grid-Contact-Mock-20261109, the rules 44 fight with the reactive fighting animations): "even tho its fine i still cant see the coherence of the combat and the units theya re doing whatever they want". Claude's read of that fight's sim dump (camp 2, the Stoneveil war party): every blow of our band already stays in its lane, but about 5 fighters swing every second on their own clocks, there are 22 different who hits whom pairs in 30 seconds, and nobody answers a blow. Claude's call was locked pairs. He picked "Yes, page first" (Locked Pairs page, https://claude.ai/artifact/NG2xvzbSf3Rw92s8hREYKv), then "Lock all six".

- One opponent each. At the start, and whenever he has none, a fighter takes the nearest enemy his class shape reaches who has no opponent yet. The fastest picks first. He keeps him until one of them falls or an order breaks the pair.
- They trade. The faster one (Speed, one of the six stats) swings first. The other answers with the attack table's dodge, parry, block or hit, then swings back. Nobody swings twice before the other has answered. Speed sets how quickly each answer comes.
- The back row joins the beat. A shooter whose lane already has a fight fires on his partner's swing, so the lane keeps one rhythm (Jacob, 25 Oct: "back row is constant combo doers"). With no fight in his lane, he takes his own opponent.
- The Warden and the Medic stay out of pairs. The Warden guards his cross and steps into blows aimed next to him, as rules 44 built it. The Medic treats whoever touches her and steps toward a hurt ally between beats.
- Every pair fights at once. Nothing waits for another pair; the rules 41 call stands (units "just head on fight", no "1 after other").
- Orders break pairs on purpose. Focus Fire: everyone who can reach the mark drops his opponent and gangs up on him. Fall Back: every pair breaks, the band steps back a row, and pairs form again after. Hold stays the parry: a big swing in any pair still goes through the leader's parry circle.

Still open: building it, as a new rules version on top of the grid (rules 44); whether a trade's rhythm changes the leader's parry timing; chief moves still pick targets by row; the gap between a swing and its answer, and every other number; how the screen draws a pair (Codex).

## Phase, the orbit lock animation, owner decision, 12 Nov 2026

Jacob on the first Phase loop (cyan holograms, scan lines, tears): "bro so much noise", then "need soemthing solid bro that is very easy to spot and has very advance type 4 civ vibes to the animation". Claude sent three solid looks (void cut, orbit lock, prism). He picked "2", the orbit lock, and on the full loop in that look: "yeah fine bro lock this animation".

- Phase lands: the enemy flashes white for one frame and a thin cyan ring lies flat at his feet. That ring is the phased mark.
- Each delayed swing is caught where it would land: one white frame, then a cyan ring snaps round him and holds for about half a second. He recovers with the ring still turning round him. A white light runs along each ring.
- One ring per delayed attack, up to three, each at its own tilt, like a gyroscope. You count the delays by counting the rings.
- At three the rings spin and crush into him and he flashes white. Then he winds up the merged tank buster and flashes gold twice. The leader parries on the second flash.
- Phase itself is cyan and white only. Gold stays the parry's alone. No scan lines, tears or glitch. This replaces the "broken hologram" look written on 12 Nov.
- The art is PixelLab VFX workbench renders, saved in My effects as "KS Phase orbit lock" (mark, 1 ring, 2 rings, 3 rings, lands, catch 1 to 3, tank buster). The loop clip and its builder sit outside the repo in the side session notes folder, phase-20261112 (phase-orbit-loop.gif, build_orbit.py).

Still open: who Phase hits and why; what a missed pile does; whether the pile parry is one hit; every number. Not in the sim, not in Unity, not with Codex.

## The cult's tech is Type 4 in every animation, standing rule, owner decision, 12 Nov 2026

Jacob on the mind link clip, where a gold line ran from the enemy to the leader's hand: "instead of a lodged tracking thing can you make it more suited for an advance 25th century type 4 civilisation species i dont think a lodged ray will suffice bro haha and can you lock it every time you make animations you make with this theme always nothing generic nothing shit".

- Every animation of the leader's powers, and of anything else the cult's own tech does, is made in this theme: a 25th century species from a Type 4 civilisation (see "The camp", 27 Oct: "The cult is an intergalactic species from a Type 4 civilization").
- Nothing generic. A ray or line lodged between two units is out. The gold link clip (phase-link-loop.gif) is rejected.
- Claude's reading, not his words: their tech moves space and time, it does not shoot something across the room. New effects build on what is already locked: the relic over the leader's palm (10 Oct), the orbit lock rings, cyan and white, with gold kept for the parry alone.

Still open: the look that replaces the gold link, for how a phased pile reaches the leader's parry.

## The leader's species is argon based life; Phase is stasis plus a rift, owner decision, 13 Nov 2026

He rejected all five species options in the 12 Nov research (type4-research.md, side notes, not in the repo), then gave references (Warframe Limbo's Banish, Borderlands Phaselock, Mass Effect Stasis, Apex void, Destiny Suspend) and picked "stasis with limbos rift".

The lore, his words, after quoting that argon life is implausible because argon is an inert noble gas: "this statement is super true because it is what it is, but the leaders species worked a way around it that is their potential bro making statis and 2 dimensions clash is what is called a piss for them".

- The leader's species is argon based life. They made the impossible work, and that is where their power comes from.
- Freezing a thing in stasis and clashing two dimensions is easy for them.
- Phase looks like stasis plus Limbo's rift: the effect is on the enemy's own body (sealed, frozen, half in another dimension), not a ring or line beside him.
- Claude's reading, not his words: their light is argon's real glow (violet in a discharge, blue and green in an argon laser), which sits on the locked colours; the phased enemy turns argon coloured and see through; the merged blow comes out of a rift in front of the leader, so nothing crosses the room. "Noble gas, noble species" is a joke offered, not locked.

Still open: which take (bubble, vortex) or neither; who Phase hits and why; every number. Not in the sim, not in Unity, not with Codex.

## Phase on a unit and on a boss, owner decision, 14 Nov 2026

Jacob on the Warden clip (phase-warden.gif, three PixelLab takes of Phase on the Bloodridge Warden's shield bash): "Take 2 But where is Parry?", then "leader has to face a unit, not his attack. When it comes to 5-on-5 battles, but when it comes to the champion fight and the chief fight, the attacks should face rather than the boss", "if we use attacks as a phasing option, we will never get there" and "does the enemy get the advantage or we get the advantage? When they are phased". Claude's call below. He said "Yes, lock it".

- At the champion and the chief, Phase takes their attack. His 12 Nov rule stands (GDD_CURRENT, Tool 1): it delays a boss attack, delays pile up and land together, tank busters and casts can never be phased.
- On the road, Phase takes an enemy unit himself, not his swing.
- A phased unit is held in place, half out of this dimension. While held he can't hit anyone and nobody can hit him.
- That breaks his pair. Our fighter who was trading with him is free and joins the nearest pair, two on one. Phase is the leader's way to break a pair, as Focus Fire and Fall Back are the orders' way (locked pairs, 9 Nov).
- When the hold breaks he slams back in with one big stored hit on whoever his shape reaches. It flashes gold and goes through the leader's parry circle (rules 41, the mind link).
- Both sides gain. We get time two on one and a parry chance we made ourselves. He gets a hit bigger than his normal swing. Miss the parry and it lands hard. A held man can't be killed, so holding one who is nearly dead saves him. The safe use (save a fighter who is losing his pair) pays less than the risky one (set up the parry).
- One line for both fights: Phase buys time now and makes one big parry later. On a boss it holds the attack; on a unit it holds the man.
- This answers "who Phase hits and why", left open on 12 and 13 Nov.
- He picked take 2 of the Warden clip (side notes, phase-20261112: the violet bands grip him while he leans into them, hold, shatter, then he bashes). A pick, not a locked animation. The leader and his parry were not in that clip.

Still open: the tiers (Claude's proposal with Jacob, 14 Nov); how long a hold lasts and how big the stored hit is; what a parried stored hit pays; what a missed pile does; every number. Not in the sim, not in Unity, not with Codex.

## The build up, owner decision, 14 Nov 2026

Jacob after Claude's Phase tiers: "what if the phased attacks and other buttons add up to something that gives advantage to enemies and gives us a heavy super duper big attack like all these buttons add up to something then the chief will be like here bro take this big time build up we all have in expedition 33 style multiple parry attack that will melt the group". On Claude's call: "A perfect string pays the biggest tag in the game. Yes, love it" and "Champion gets a short version. Yes". He asked for a road version and for a cap of two marks ("you have four, right? So you can only put two. In like combos"), then asked whether the string depends on which marks the boss holds. He said "yea okay" to the four halves and the whole build up below.

- Every button the leader presses puts one mark on the enemy who will throw the string: the chief, the champion, or on the road their leader. Each attack you phase counts as a press.
- He holds two marks at most: your last two buttons. A third press pushes out the oldest. Marks never lock a button; you can press any of the four at any time.
- Each of the four buttons has one half string. His string is the older mark's half, then the newer mark's half. Four halves make sixteen strings, counting the same button twice. One mark throws a half string; no marks, no string. After a string both marks clear.
- Each half is your own button turned against you and takes its feel from that button. The void half (Phase): hits that hang in the air and drop late. Each chief throws the halves with his own weapon; the rhythm stays the button's.
- Every hit in a string flashes gold, is parry only and can never be phased, like tank busters and casts.
- Every hit you miss lands on our whole group. Parry every hit and the squad throws one huge counter back. Only a clean string pays it. (Expedition 33's rule as Claude read it: the guides disagree on whether every hit or only the last needs the parry; Claude went with every hit.)
- The chief throws his at his two locked turns, half health and under 30%. A clean string at the chief pays the biggest attack in the game.
- The champion throws a short version.
- On the road their leader throws the shortest version when half his band is down. He can't be hit, so a clean string's counter lands on his band.
- Both sides gain: he gets a string that can melt the group, we get the counter. What you press last before his turn decides what you parry. No new meter: you read the two marks on him.

Still open: the halves of the other three buttons (call 1); the void half's real hits; when the champion throws his; the Phase tiers (Jacob said "I like it" on 14 Nov, not locked); every number (hits in a half, the gap between hits, the counter, what a missed hit does). Not in the sim, not in Unity, not with Codex.

## Phase tiers, owner decision, 14 Nov 2026

Claude's proposal after the unit and boss split above. Jacob: "I like it", then "Yes, lock tiers". This settles the tiers left open in the two sections above.

- The same three tiers in both fights. Each tier adds a thing you can do, not only a bigger number. What grows: how long the hold lasts, how many blows it stores (the rings, up to three, the 12 Nov look), and what the parry does with them.
- On the road (a unit):
  - Tier 1: a short hold. He stores one blow and slams back with it. The parry pays as a normal parry.
  - Tier 2: a longer hold. He stores a blow for every swing he would have thrown while held, up to three rings, and slams back with all of them as one hit. The parry pays for each ring; a miss lands all of it.
  - Tier 3: the hold is as long as tier 2. A parried slam goes back into him: he eats his own stored hit, and with three rings it can kill him.
- At the champion and the chief (an attack):
  - Tier 1: delays one attack a short time. Phase a second while the first is held and they pile, two at most. They land together and one parry handles both.
  - Tier 2: a longer delay; the pile goes up to three.
  - Tier 3: the negate. Parry the pile and it goes back into the boss instead. Tank busters and casts can still never be phased.
- The negate happens through the parry, so his 12 Nov words ("higher tiers negate them") and "only the parry cancels a boss attack" both hold.
- Bigger piles are bigger risk: at tiers 2 and 3 a missed parry hurts more. The player who waits for three rings is paid more than the one who lets one go.
- Every attack phased is still a press for the build up.

Still open: how a tier is raised (the leader is the button enhancer, 12 Nov); every number (hold and delay lengths, the stored hit, what each ring pays). Not in the sim, not in Unity, not with Codex.

## Every class is a fighter: the Warden is a bruiser, the Medic is a mage, owner decision, 14 Nov 2026

Jacob after Codex's duel and support mocks (Builds/KindredSiege-Duel120-Mock-20261113 through Builds/KindredSiege-Support-Brains-Mock-20261114, where he said "WTF is happening here ?"): "we don't need tank and healer. We just need all out blown out units fighting each other. I mean, Warden and the medic still should exist, but they be doing their own thing in a different way, not like healing or DPS", "Medic is a healer in any language, so I don't know what to do about it" and "what if they're just damage doers instead of tank and healers?". He said "Change it" to Claude's first call (a Warden whose blocks load a slam, a Medic who finishes the most hurt enemy), then "yea bro" to the call below.

- Every class is a fighter. No tank and no healer. Each one kills in its own way, and they fight until someone drops. The leader already protects the squad: big hits come to him through the mind link and he parries them (rules 41).
- The Warden is the bruiser. He has no blocking job. He walks in and swings. His hits are heavy bashes: each one staggers the man it hits, so he loses his next swing, and leaves him with an Opening for the Skirmisher. He is slow, so fast enemies hit him first. A bash that misses leaves him wide open.
- The Medic is the mage. Her blast bursts on her target and splashes the men next to him. She is fragile and slow to cast. Each tribe's piece is already a caster's weapon: the tooth totem staff (Clarth), the sceptre with its ember lantern (Bloodridge), the injector needles and turquoise vials (Skullpeak).
- She no longer heals, so she gets a new name. Names are a taste call: three options next.
- Claude's readings, his to change: the bash's stagger and Opening are the Warden's Shield Bash boost (rules 12) made into his main attack; the blast is the first thing in the sim to read Spirit against Will, the way a blow reads Attack against Defence (rules 42); the size ladder holds, since the bruiser stays the biggest and the mage stays small.

This replaces the class rule "tank cant heal and heal cant dps" (20 Oct, line 411); Guard and Treatment as the Warden's and the Medic's spells, and "she never attacks" (line 412); the Warden holding the middle and guarding everyone (8 Nov, line 1343); Guard the cross as his base shape (9 Nov, line 1366); and the Warden and the Medic staying out of pairs (9 Nov, line 1379). Both now take an opponent and trade blows like everyone else. Codex's proposal the same day (a Warden whose shield is a committed action, a Medic with a finite supply, HANDOFF_LOG) is not taken.

Parked, each its own call later, because each was built on guarding others or on healing: the Guard mark; the Medic's three Butcher combos (Mercy Well, Evac, Rewind) and the Warden's Wrong Door; the Glutton's Healers limb; the habits Bodyguard, Rescuer, Triage and Front First; the Mend and Hold pair; Covered Treatment; the Medic's Self Patch and Stim. Event Horizon and Still Wall stay, since both work off his bash and his shield.

Still open: her name; both classes' shapes and their slot 2 and slot 3 moves (Shield Bash moves out of his slot 3 into his attack); whether the Artillerist, the roster's splash class, changes now that the mage splashes; every number. Not in the sim (rules 43 to 45 still play the old Warden and Medic), not in Unity. Codex is told in HANDOFF_LOG, with no job.

## The nine cult soldiers are class templates, owner decision, 15 Nov 2026

Jacob asked whether the trailer's unit art could be shared across the 17 classes. Claude's call: not as the classes, because the nine cult soldiers (18 Oct; fronts in Art/PixelLab/Cult/2026-10-17, sheets in Assets/Resources/CampaignArt/cult_*) are the leader's own species, whose army the prologue wipes out, and every class is fielded by a tribe in that tribe's own look; but as shape templates. Jacob: "Yes, as templates".

- The nine stay the prologue and trailer army. They never join a squad.
- Each is the starting shape for one class when that class's tribe versions are made: the heavy tank for the Warden, shield and spear for the Vanguard, the skull raider for the Berserker, the hooded zealot for the Duelist, the sharpshooter for the Marksman, the preacher for the mage, the hook hunter for the Binder, the blade dancer for the Infiltrator, the standard bearer for the Herald. A tribe's version keeps the shape and the weapon's read and takes the tribe's own race, colours and signature piece.
- The Artillerist, Recon, Field Engineer, Disruptor, Chemist and Conduit have no template.

Still open: the 17 class rewrite itself (Claude's proposal of 15 Nov, not locked); whether PixelLab keeps a template's pose when it redraws it in a tribe's look; every tribe's look for the twelve new classes. No art made.

## The 17 classes rewritten: who beats who, two new per tier, the tier sets strength, owner decision, 15 Nov 2026

Jacob on 15 Nov, after the 14 Nov "every class is a fighter" lock: "You made a class with no spread", "the classes should have advantage and disadvantage over each other, like Pokemon types, but in stat variation", "special attack is always negating against speed ... spells need to travel and he can dash and negate the spell", "the bruiser ... archer arrows won't do shit to him because of his huge shield" and "If you give player whatever they want, they will not play it". Then "do fo all 17 rewrite classes to to match the vibe of the war torn planet". He rejected Claude's first placement, which used only the six regions with art ("in tier 2, Thunder Coast, and tier 3, whatever it is, you get most of the units"), asked "the guys from tier 1 can beat tier 5 guys. Then what's the point of having them?", and said "Lock it" to the chart, two new classes per tier, and the tier setting strength.

**Every class is a damage dealer with a kind of hit, and each kind meets one stopping stat.**
- Blade: Defence cuts it, Speed dodges some of it, and the Duelist's parry turns it.
- Shot (arrows, rifles, harpoons): too fast to dodge. Defence cuts it and a shield blocks most of it.
- Shell (the Artillerist): comes late and from above. No shield blocks it, and Speed gets out of it.
- Spell: it travels, so Speed dodges it. Will softens it.
- Gas (the Chemist): a cloud. Speed walks out of it and Will softens it.
- Sound (the Herald): can't be dodged. Only Will softens it.
- Hold (the Trapper's snare, the Binder's tether): takes the man's Speed while it holds, so he can't dodge. The Binder can't pull a heavy man (the Warden, the Vanguard).
- Claude's working rule, a starting value: a weak stop doubles a hit, a strong stop halves it, as Pokemon's super effective and resisted.

**The chart in four lines.** Armour stops blades and shots. Spells beat the slow and the armoured. Speed dodges spells. Shots and holds catch the fast.

**The spreads**, on the 7 Nov ladder (Health, Attack, Defence, Speed, Spirit, Will):

| Class | Hit | Health | Attack | Defence | Speed | Spirit | Will | Its twist |
|---|---|---|---|---|---|---|---|---|
| Warden | blade | high | mid | very high | very low | very low | low | Shield; heavy bash staggers and leaves an Opening (14 Nov) |
| Marksman | shot | low | very high | very low | mid | low | mid | Aimed Shot |
| Skirmisher | blade | mid | very high | very low | very high | low | low | Dashes in |
| Trapper | blade, hold | mid | mid | low | high | high | mid | Snare |
| Mage (name open) | spell | low | very low | low | mid | very high | very high | Small splash (14 Nov) |
| Vanguard | blade | high | high | mid | low | very low | very low | Charge staggers the first man; heavy |
| Duelist | blade | low | high | low | high | very low | low | Parries blades |
| Berserker | blade | high | very high | very low | mid | very low | very low | Hits harder as he bleeds |
| Conduit | spell | mid | very low | low | low | very high | high | A beam paid with his own health |
| Artillerist | shell | mid | high | mid | very low | very low | mid | Shells |
| Recon | shot | low | mid | low | very high | low | mid | Marks a man |
| Field Engineer | shot | mid | high | high | very low | low | low | Sets down a sentry gun |
| Disruptor | spell | mid | very low | low | high | high | high | Breaks a unit's wind up, on the road only |
| Infiltrator | blade | mid | high | very low | very high | very low | low | Flanks to the back row |
| Binder | blade, hold | high | mid | mid | mid | high | mid | Tether pulls a man out of their line |
| Chemist | gas | mid | very low | low | low | very high | high | Gas cloud |
| Herald | sound | mid | very low | low | mid | high | high | War horn |

In Claude's paper model (one on one, not the sim) every class beats between 4 and 10 of the other 16. The Warden beats every blade and every shot and loses to every spell, gas, sound and shell. The two slice lines that flipped from Claude's first chart of five: the Warden beats the Skirmisher, and the Marksman beats the mage. Jacob's own two examples hold: the Skirmisher beats the mage, and the Warden beats the Marksman.

**Two new classes per tier.** The five from the start (Warden, Marksman, Skirmisher, Trapper, mage) are fielded by every tribe in its own gear. Each tier adds two, and every tribe of that tier fields them too. Beat any tribe of a tier and that tier's two unlock at camp for good.

| Tier | New classes | Beaten by what you already have |
|---|---|---|
| 1 | Vanguard, Berserker | The Warden beats both; the Skirmisher beats the Berserker |
| 2 | Artillerist, Field Engineer | The mage beats both |
| 3 | Duelist, Infiltrator | The Warden and the Field Engineer beat both |
| 4 | Binder, Recon | The Warden, the Vanguard and the Field Engineer beat both |
| 5 | Chemist, Disruptor | The Marksman, Skirmisher, Recon and Infiltrator beat both |
| 6 | Conduit, Herald | The Marksman and the mage beat both |

**Class is the type, the tier is the strength.** The chart holds between equal fighters. A tribe's tier raises its fighters' spreads in every class (Jacob, 12 Nov: "this what it makes to go up the tiers to get better pokemon logic"), so a tier 1 Marksman beats a tier 5 Chemist only once he is grown (Whisperer ranks) or is a higher tier tribe's Marksman himself. New classes are new tools, not bigger numbers.

Claude's examples of one tribe's version each, his to change: Bloodridge's Vanguard rams behind a bronze tomb door; Stoneveil's Berserker drinks from the blood pools; Skullpeak's Artillerist has a mortar grafted onto his arm; Thundercoast's Field Engineer bolts a harpoon gun from a wreck to the floor. The nine cult soldiers are the shape templates (the section above).

This replaces: line 18's "how class unlocks connect to tiers remains to be decided"; the roles and signatures in `CLASS_ROSTER.json` where they differ (no class heals, restores sanity or only supports: the Medic, Herald, Chemist, Disruptor and Recon fight); the 7 Nov spreads of the Warden (Health, Attack, Spirit, Will) and the Skirmisher (Attack); rules 42's dodge from Speed against every blow and block against every blow, which become per kind of hit. The two 30 Sep tier tables stay rejected.

Still open: the mage's name; every number (the ladder's values, the doubling and halving, how much a tier raises a spread); the tribes of tiers 3 to 6 and their looks for the new classes; whether a rival who learns your squad brings what beats it (Claude's proposal); `CLASS_ROSTER.json` still holds the old text. Not in the sim (the table in BattleSimulation.Table.cs still has the 7 Nov spreads and one dodge rule), not in Unity.

## The mage is the Hexer, owner decision, 15 Nov 2026

Jacob picked option 2 of Claude's three names (1 Mage, 2 Hexer, 3 Quack): "The name is Hexer. Lock it."

- The class that was the Medic, and the mage since 14 Nov, is the Hexer. Dark folk magic: she throws curses. Her blast bursts on her target and splashes the men next to him, as locked on 14 Nov, with the spread in the 15 Nov table above.
- Every lock that says the Medic or the mage for this class now means the Hexer. Her tribe pieces stay: the tooth totem staff (Clarth), the sceptre with its ember lantern (Bloodridge), the injector needles and turquoise vials (Skullpeak).

Still open: the code still calls her Medic (UnitClass.Medic) and `CLASS_ROSTER.json` still has the Medic.

## The Hex replaces the Guard mark, owner decision, 15 Nov 2026

Jacob answered "tes" (his yes) to Claude's call: "Replace the Guard mark with the Hex? Yes, or change it?"

- The Guard mark is gone. Since 14 Nov nobody guards or heals, so nothing could lay it or cash it.
- The Hex takes its place. The Hexer's blast curses her target and the men it splashes.
- The Warden's heavy bash on a cursed man lands double and uses up the curse.
- The curse wears off after a few seconds. That length is a starting value.
- The four marks are now Snare, Bleed, Opening and Hex. One class lays a mark and another cashes it, as locked on 20 Oct.
- Speed dodges spells (15 Nov), so a fast man often escapes the curse.
- Why, in Claude's words: the Hexer was the only class with no mark, and the Warden laid an Opening but cashed nothing. The Hex links the two classes that changed on 14 Nov. On the chart the Hexer beats the slow and armoured, and the Warden is the slow and armoured man, so pairing them covers his weak spot and spends her curse where his bash is strongest.

Risk, reward, punish:
- Reward: curse plus bash is a double hit from your slowest, heaviest swing.
- Risk: the bash is slow and the cursed man may be across the field. Bashing whoever is near is the safe play, and it pays less.
- Punish for you: fast enemies dodge the curse. Their Skirmisher goes for your fragile Hexer, and if she dies nothing gets cursed.
- Their side: their Hexer and Warden can crush your men the same way. Your answer is your Skirmisher diving their Hexer.

This replaces Guard as the fourth mark (20 Oct, line 414) and the Guard mark in the 14 Nov parked list (line 1485). Covered Treatment, the old way the Guard mark paid out, stays parked with the habits.

Still open: how long the curse lasts; whether double stays double; what gear bends on it (gear bends only its own class's spell: which mark it leaves, or how hard it cashes one). Not in the sim, not in Unity.

## The Warden's slot 3 and the Hexer's slots 2 and 3, owner decision, 15 Nov 2026

Jacob said "Yes, lock them" to Claude's call: Shield Pin, Evil Eye and Bad Blood.

- Warden, slot 3 (his boost), Shield Pin: about every 15 s, on its own, he slams his shield on the man in front of him. That man is pinned for 2 s and can't dodge. It uses the pin status the sim already has (the Butcher combos). It takes the empty slot 3 left when Shield Bash became his attack on 14 Nov.
- Hexer, slot 2 (her defence), Evil Eye: when a melee man's blow lands on her, he gets the Hex. The same shape as the Trapper's Snap Trap. It replaces Self Patch, which healed her.
- Hexer, slot 3 (her boost), Bad Blood: about every 15 s, on its own, she points at a bleeding man and his bleed jumps to the men next to him, so the Skirmisher's strike rips them all open. It replaces Stim, which sped up an ally's spell.
- Why, in Claude's words: every slot 2 and slot 3 move feeds another class. Pin, then curse, then bash kills one man fast; Evil Eye punishes whoever dives her; Bad Blood turns the Trapper's and the Skirmisher's bleeds into more for the Skirmisher to rip. The curse is a spell and Speed dodges spells, so the pin takes away the dodge the curse would miss on.

Risk, reward, punish:
- Reward: one man dies fast, the diver eats a double bash, one bleed becomes three.
- Risk: Bad Blood fires only on a bleeding man, so it needs the Trapper's snare or the Skirmisher's Twin Cut. Evil Eye pays only while your Warden lives to bash.
- Punish for you: Evil Eye fires after the blow lands, so she still takes the hit, and a Skirmisher crit can kill her first.
- Their side: your melee man who hits their Hexer gets cursed, so the answer to her is your Marksman, whose shots can't be dodged and who beats her on the chart.

This replaces the Medic's Self Patch (rules 11) and Stim (rules 12), and the Warden's slot 3 left open on 14 Nov. Both are taken off the 14 Nov parked list (line 1485).

Still open: every number (the 15 s, the 2 s, how far the bleed jumps); the Warden's slot 2 stays Brace as the sim plays it; Claude's 12 Nov route mapping (Last Refuge for the Medic) is untouched. Not in the sim, not in Unity.

## The Warden's and the Hexer's grid shapes, PARKED, 15 Nov 2026

Jacob said "Yes, lock them" to Claude's call below, then the same day asked "wait what for youre doing grids ? since codex is doing combat for units ???" and said "Yes, park it". Not locked. Codex owns the unit combat, and his duel and exchange mocks since 14 Nov have no grid, so these shapes wait until his combat settles. The real sim still has the grid (rules 44), and Codex's pairs (rules 45, 13 Nov) pick opponents on it.

Claude's proposal, kept for then:
- Warden, front row: 1 Bash across (base), he bashes the enemy straight across. 2 Shield swing, he can bash any of the three enemy front panels and picks a cursed man first. 3 Bowl over, he knocks the man across into the man behind him and both stagger.
- Hexer, back row, casting on her lane partner's swing as a shooter does: 1 Lane hex (base), the curse flies down her lane to the first enemy and splashes the panels touching his. 2 Over the top, the curse arcs over their front row to the first enemy behind it. 3 Crowd hex, the curse goes to whichever enemy has the most men touching him, in any lane.

The 9 Nov bases, Guard the cross and Touch (line 1366), guard and heal, which the 14 Nov lock took away, so the two classes' shapes are open.

## The four parked Butcher combos, owner decision, 15 Nov 2026

Jacob said "Yes, lock them" to Claude's call: Curse Well, Split Hex, Old Wounds and the new Wrong Door. A combo is a fighter's own attack plus the Butcher's augment, one bigger attack you can see (27 Oct). Three were heals and one a guard, so all four became attacks. The verbs stay: pull, portal, stop time. The names are working names.

- Hexer with Gravity Well, Curse Well (was Mercy Well): before her blast bursts it drags the men around her target in on him, so the splash hits them all and every one is cursed. It lays the Crush stack, as every pull does.
- Hexer with Fold Door, Split Hex (was Evac): a portal carries half her burst to the enemy furthest from her target. He is cursed too and left Exposed, as everyone a portal hits is.
- Hexer with Time Stop, Old Wounds (was Rewind): her curse rewinds the man 3 s, so every hit he took in those 3 s lands again. It lays the Stasis stack, as every time combo does.
- Warden with Fold Door, Wrong Door (keeps the name): his bash also comes out of a portal onto a man in their back row. The door's edge cuts him and leaves him bleeding, the 30 Oct hook, kept.
- Why, in Claude's words: each one is her blast or his bash made bigger, and each feeds the marks, curses for the Warden and bleeds for Bad Blood and the Skirmisher.

Risk, reward, punish:
- Reward: a cursed crowd for the Warden; a curse that reaches far; a man the squad has focused takes his last hits again; a bash that reaches their back row.
- Risk: Curse Well pays only when they stand close together; Old Wounds pays nothing on a man nobody has just hit; Wrong Door needs a man in their back row.
- Punish: the pacts stay as they are (halved, 29 Oct), so each combo still costs time, nerve or health, and Old Wounds on the wrong man pays that cost for nothing.

This replaces Mercy Well, Evac and Rewind (27 and 28 Oct, line 725 to 727), the old Wrong Door (line 726, and line 840), their 29 Oct rethinks (line 930) and their 30 Oct mark hooks (lines 962 to 964), and takes all four off the 14 Nov parked list (line 1485). Claude's correction before the lock: Wrong Door first said "the back row man in his lane", and lanes belong to the parked grid.

Still open: every number (3 s, half the burst, how far the pull reaches); the Glutton's talents that grew the old four (Rewind Wound, Matter Printer, the Healers limb) wait for that call. Evac and Rewind led every other combo by 6 to 10 points in the 29 Oct sim measure; the new four are not measured, and wait for Jacob's play (build first, 12 Oct). Not in the sim (rules 15 still plays the old four), not in Unity.

## The habits and the Mend and Hold pair are parked, Covered Treatment is dropped, owner decision, 15 Nov 2026

Jacob said "Yes, park them" to Claude's call, after he asked why Claude was doing grids while Codex does the unit combat.

- Parked until Codex's unit combat settles, next to the grid shapes: the habits Bodyguard, Rescuer, Triage and Front First, and the Mend and Hold pair. Habits are how a unit picks who to go for, and a pair decides who the second fighter goes for, and Codex's brains (Builds/KindredSiege-Support-Brains-Mock-20261114) make that choice per unit. Designing them now would build on top of his work.
- Covered Treatment is dropped for good. It paid out the Guard mark on a heal, and both are gone. The Hex pays out on the Warden's bash instead.

Still open: all five parked pieces, once Codex's combat settles. Nothing here is in the sim's new rules; rules 15 to 45 still play the old habits and pair.

## The Glutton's Healers limb becomes Casters, owner decision, 15 Nov 2026

Jacob said "Yes, lock it" to Claude's call. Every Healers talent made heals bigger, and nobody heals since 14 Nov.

- The limb is renamed Casters. It covers every class whose hit is a spell, gas or sound: the Hexer now, and the Conduit, Disruptor, Chemist and Herald when they unlock.
- Same shape and cost as every limb (1, 2, 3 and 5 points):
  - Tier 1, Matter Printer: caster blasts land bigger.
  - Tier 2, Rewind Wound: Old Wounds replays 5 s, not 3.
  - Tier 3, pick one: Half Life (the Hex lasts twice as long), a new name in place of Synth Armour; or Entropy Purge (caster portals cost half the nerve, so Split Hex is cheaper).
  - Keystone Recompile: caster blasts are twice as big on anyone under a quarter health.
- The Glutton's rule holds: each talent only makes what another Pal gives bigger, longer, faster or cheaper, and adds no effect of its own.
- Risk, reward, punish: the tier 3 pick is the choice. Half Life gives the slow Warden time to reach the cursed man; Entropy Purge saves nerve but does nothing without Split Hex; Recompile is wasted on fresh men and finishes hurt ones.

This replaces the Healers limb (28 Oct, line 767) and takes it off the 14 Nov parked list (line 1485).

Still open: every number. Not in the sim, not in Unity.

## Ranks for the Warden and the Hexer, and the rank 3 promotion, owner decision, 15 Nov 2026

Jacob said "Yes, lock it" to Claude's call. He had asked twice whether a fighter changes class at rank 3, as a mage becomes an archmage.

- Rank 2 keeps the rule every class has: the class spell happens one more time. The Warden bashes twice in a row; the Hexer casts twice in a row. The extra hit is plain, as locked on 28 Oct (line 835): it fires no combo and cashes no mark, so the second bash does not use up the Hex.
- Rank 3 is a promotion, by name and look only. The fighter keeps his class, his moves and the second slot rank 3 already gives, and takes a promoted name and the Whisperer's full regalia, worn on top of his own sprite. No bigger move, because bigger, longer, faster and cheaper are the Glutton's. No new sprite per tribe: one regalia set per class.
- This holds for every class, so every one of the 17 gets a promoted name. The names are a taste call, three options per class.
- Changing into a different class stays withdrawn (27 Oct): the 17 stay 17.
- Risk, reward, punish: you pour road picks into one fighter for a second slot and a title, and every rank goes when the run ends, so the promotion is earned again each run. (Claude first said a captured fighter loses it, but nobody is captured since rules 11.)

This replaces rank 2's "the Warden Guards two allies at once" and "the Medic keeps one treatment in hand so she can treat twice in a row" (28 Oct, lines 814 and 1124), and takes ranks off the 14 Nov parked list. Every piece the 14 Nov class change parked is now settled or parked behind Codex's unit combat.

Still open: the 17 promoted names; the regalia sets; every number. Not in the sim, not in Unity.

## The 17 promoted names, owner decision, 15 Nov 2026

Claude offered three directions for the rank 3 names: 1 Arch in front of the class name, 2 a new grim war word, 3 a cult church office. Jacob: "mix of 2 and 3 no 1 i hate 1". Claude made one mix by a rule (the cult office where the church job is the class's own job, the new word where the office is a stretch or obscure, or where the word ties to approved art), and Jacob said "yes mlock".

| Class | Rank 3 name |
|---|---|
| Warden | Bulwark |
| Marksman | Canon |
| Skirmisher | Zealot |
| Trapper | Jawsetter |
| Hexer | Crone |
| Vanguard | Templar |
| Berserker | Flagellant |
| Artillerist | Bombardier |
| Field Engineer | Siegewright |
| Duelist | Bladesaint |
| Infiltrator | Confessor |
| Binder | Exorcist |
| Recon | Pathfinder |
| Chemist | Plaguewright |
| Disruptor | Heretic |
| Conduit | Martyr |
| Herald | Cantor |

- Rejected: Arch in front of the class name, for every class.
- The fighter keeps his class; the name and the Whisperer's full regalia are the promotion, as locked above.

Still open: the regalia sets. Not in the sim, not in Unity.

## The 12 new classes' defence and boost moves, owner decision, 15 Nov 2026

Jacob asked "what is left in class design how do they behave", picked item 1 of Claude's list ("Let's do 1": the 12 new classes' defence and boost moves), and said "Yea okay" to Claude's call. Each move is built from the class's roster signature and its 15 Nov spread. None uses a Pal's verb (pull, portal, time stop, shield, mirror, decoy, mine, push, echo), and none takes away the class's weakness on the chart. The defence fires when he is hit; the boost fires on its own about every 15 s, only with a real target.

| Tier | Class | Defence (slot 2) | Boost (slot 3) |
|---|---|---|---|
| 1 | Vanguard | Headbutt: the melee man who hits him staggers and loses his next swing | Crack Armour: his heavy blow halves the man's Defence for 4 s, so blades and shots get through |
| 1 | Berserker | Blood Rage: a hit makes him bleed, and his next swing hits harder | Bloodrush: for 4 s he hits half again as hard, and double on a staggered man, but his Defence is gone |
| 2 | Artillerist | Point Blank: a melee man who reaches him eats a shell at their feet, and both take the blast | Concussion Shell: every man the shell hits staggers |
| 2 | Field Engineer | Cover Fire: his sentry turns and shoots the melee man who hits him | Redeploy: a destroyed sentry is set again at once |
| 3 | Duelist | Riposte: a blade he parries is answered at once with a cut that lands double | Disarm: the man in front of him can't block or parry for 2 s |
| 3 | Infiltrator | Smoke Bomb: smoke bursts, and every man next to him misses his next swing | Ambush: his strike crits on a man who is busy fighting one of yours |
| 4 | Binder | Lash: whoever hits him is tethered and held 2 s, so he can't dodge | Gauntlet: the man he pulls takes a free swing from each of your fighters next to him |
| 4 | Recon | Scramble: a blow at him misses, and he fires a shot as he runs | Range Finder: for 3 s the man he marked can't dodge or escape spells, shells or gas |
| 5 | Chemist | Cracked Vial: the man who hits him is gassed | Accelerant: his cloud turns flammable, and the next spell or shell into it sets everyone inside on fire |
| 5 | Disruptor | Static: the man who hits him restarts any big move he is winding up (road only, as his attack) | Jam: the man he hits loses his defence move for 5 s |
| 6 | Conduit | Ground Out: a hit cuts his beam, and the stored charge bursts on the men next to him | Overchannel: for 3 s his beam hits twice as hard and costs twice the health |
| 6 | Herald | Bellow: the man who hits him lands his next swing at half | Dread Horn: everyone who hears it loses Will for 4 s, so spells, gas and sound hit them harder |

- How they link, in Claude's words: stagger feeds the Berserker (from the Vanguard, the Artillerist and the Warden's bash). Lash, Range Finder, Accelerant and Dread Horn feed the casters, since Speed is what stops spells, shells and gas. Crack Armour and Disarm help everyone. Ambush pays when two of yours gang up on one man.

Risk, reward, punish:
- Every defence fires after the blow lands, so he still takes the hit.
- Four moves cost their own man: Blood Rage (their Skirmisher can rip his bleed), Bloodrush (no Defence for 4 s), Point Blank (he takes the blast too), Overchannel (double the health cost).
- The feeding moves pay only when the partner class is in your squad.

This settles "the other twelve classes' kits" left open on 9 Nov (lines 1360 and 1370) and replaces the roster signatures in `CLASS_ROSTER.json` where they differ (Rally, Suppressant and Field Treatment style support no longer apply).

Still open: which marks the new classes lay or cash with their attacks; their Butcher combos; their rank 3 regalia; every number (15 s, 2 s, 3 s, 4 s, 5 s, half, double are starting values); whether stagger becomes a mark. Not in the sim, not in Unity.

## The 12 new classes' Butcher combos, owner decision, 15 Nov 2026

Jacob said "Butcher combos now", then "Yes, lock them" to Claude's 36. Three per class, one per flavour: Gravity Well (pull), Fold Door (portal), Time Stop (time). Each is the class's own attack made into one bigger attack you can see, as the 27 Oct rule. None uses a Shade verb (shield, mirror, decoy, mine, chain, push, echo), copies another class's identity, or repeats one of the first 15 (line 711) or the four for the Warden and the Hexer (line 1618). The names are working names.

| Class (attack) | Gravity Well (pull) | Fold Door (portal) | Time Stop (time) |
|---|---|---|---|
| Vanguard (charge) | Wrecking Ball: a black hole rides his charge, drags the men near his path along and crushes them at the end | Through Charge: he charges into a portal and comes out behind their front, staggering their back row man | Standstill Charge: time freezes as he charges, so the first man can't dodge or block |
| Berserker (rush) | Red Giant: his rush ends in a black hole that grows with how much he is bleeding | Scent Door: his rush comes out of a portal at their most hurt man, anywhere | Overtime: for 3 s time runs double for him, twice the swings |
| Artillerist (shell) | Gravity Shell: the shell lands as a black hole, so nobody steps out before it blows | Trapdoor Shell: the shell drops through a portal with no delay and no warning | Frozen Crater: everyone the blast hits freezes for 1.5 s |
| Field Engineer (sentry) | Lodestone Sentry: the sentry drags men in front of its barrel, then fires a burst | Muzzle Door: the sentry's shots come out of a door right beside the man, so a shield can't block them | Stopwatch Sentry: men the sentry hits move at half speed for 3 s |
| Duelist (parry) | Dead Weight: a blade he parries turns heavy and the man can't swing for 2 s | Private Door: a portal takes him and the man in front of him out of the crowd for 3 s, one on one | Stolen Second: time stops when he parries, and he gets two free cuts |
| Infiltrator (flank) | Undertow: the black hole he leaves in their back row drags them together, and his strike hits them all | Shortcut: he steps through a portal straight to their back row and strikes at once | Between Seconds: he reaches the back row while time is stopped, strikes, and is gone |
| Binder (tether) | Heavy Hook: his tether can pull a heavy man too, and he is crushed when he lands | Long Hook: his tether goes through a portal and pulls a man from their back row | Still Hook: the man he pulls is frozen for 2 s when he lands |
| Recon (marking shot) | Lens: shots fired near the marked man bend into him | Peephole: his shot and his mark go through a portal, so they reach anyone | Rerun: his shot hits, then rewinds and hits again |
| Chemist (gas) | Choke Well: the cloud is sucked into a tight knot that hits twice as hard but covers less | Vent: the cloud comes out of a portal in their back row | Thick Air: men in the cloud move at half speed, so they can't walk out in time |
| Disruptor (breaks a wind up) | Implosion: the broken move's power collapses on its maker and drags the men next to him in | Short Circuit: his pulse goes through a portal to whoever is winding up the biggest move | Lost Time: the man he breaks waits twice as long for his next big move |
| Conduit (beam) | Accretion: the beam drags the men around his target into its path | Bent Beam: the beam goes in one door and out another, hitting two men | Standing Beam: the beam keeps burning for 3 s after he stops paying for it |
| Herald (horn) | Low Note: the deepest note drags everyone who hears it into a knot and crushes them | Earshot: the horn sounds from a portal in the middle of their band, full strength on all | Hush: everyone who hears it freezes for 1 s |

- Each combo lays its flavour's stack (line 1018): Crush for Gravity Well, Exposed for Fold Door, Stasis for Time Stop.
- The pacts stay as they are, halved (line 897, fix 1): Heavy, Unnerving, Borrowed time.
- The Disruptor's combos work on the road only, as his attack.

Risk, reward, punish:
- Four trade the class's weakness on the chart (line 1499) for the pact: Heavy Hook pulls a heavy man, Trapdoor Shell has no delay to step out of, Private Door keeps the Duelist from being ganged, Thick Air keeps men from walking out of the gas.
- You pay the pact on every fight for it, and an enemy with the same combo does the same to you.

This settles "their Butcher combos" left open in the section above.

Still open: how these 36 hook into the Snare, Opening, Bleed and Hex marks, as line 938 did for the first 15; the marks the new classes lay or cash with their attacks; their rank 3 regalia; every number (1 s, 1.5 s, 2 s, 3 s, half, double, twice are starting values); the art. Not in the sim, not in Unity.

## The leader's relic parries the Phase blow, owner decision, 15 Nov 2026

Jacob over a day of takes, then on the last one: "Yes looks good" and "Yes, lock it". His words that set it: "keep the relic as it is just use it as a shield and it blocks the attack and leader parry's using thunder fist not hammer", then "can't you just use the one in the first hand", then "let the relic hover during the strike, and he just punches the relic back".

- The relic is the one already on his battle sprite: the small spiky crystal he holds in his first hand at idle (his look, L120). It never opens, grows or changes shape. There is only ever one relic on screen.
- It floats out of his hand and over his palm in his own cast frames (leader_se_attack 6, 8, 16, 20).
- The Phase blow comes to him and stops on the relic. The relic hovers in front of him and holds the blow back.
- Violet lightning charges on his fist while he winds up (argon violet, L1417).
- He punches the relic. It bursts into the blow. That burst is the parry cue: gold, at the relic, and the only gold the leader shows (gold is the parry's alone). His body never turns gold.
- On that same frame the chief flashes white and one sun on the parry meter (L1277) lights where it sits, with one white pop. Nothing flies to the meter.
- The blow is knocked back and breaks into violet pieces that fly off and are gone. Nothing crosses the room.
- The relic floats back over his palm, then back into his hand in his own cast frames 22 to 32.
- Rejected on the way, do not bring back: a sun or star inside rings, layered crescent ribs, any relic that opens or changes shape, the concept sheet relic pasted on (two relics showed), the meter disc flying up, a gold screen, pieces thrown back at the chief, the hammer fist, the backhand and the blink.
- The art is in `Art/Studies/Relic-Parry-20261115/`: `phase-punch-relic.gif` (the whole parry at 12 fps), `pr_5/` (the PixelLab clip, 17 frames, made from his cast frame 20 cut at 40,24; frame 8 is not used because it drew a beam along his arm), `c20.png` and `c20_hit.png` (its inputs). In the punch frames his body is PixelLab's redraw of his sprite. The frames before and after are his real sprite.

Still open: the parry look for the other three buttons; whether this same parry is used for every blow that reaches him or only Phase's; every number. Not in the sim, not in Unity.

## The 12 new classes' marks, owner decision, 15 Nov 2026

Jacob said "delegate the work for codex and next on the list", then "Yes, lock them" to Claude's call: each new class's attack does one mark job, and each tier has one class that lays and one that cashes. Only attacks lay or cash marks; the defence and boost moves (line 1706) do not. A cash lands double and spends the mark, as the five do (line 414, line 1567). The cash pops its mark's word (line 1296).

| Tier | Class | Mark | How |
|---|---|---|---|
| 1 | Vanguard | lays Opening | His charge staggers the first man and leaves him open |
| 1 | Berserker | cashes Bleed | His swing on a bleeding man lands double |
| 2 | Artillerist | cashes Snare | A snared man can't step out, so the shell lands double |
| 2 | Field Engineer | lays Bleed | His sentry's rounds leave the man bleeding |
| 3 | Duelist | cashes Opening | His cut on an opened man lands double |
| 3 | Infiltrator | lays Bleed | His strike from behind leaves a deep bleed in their back row |
| 4 | Binder | cashes Opening | His tether on an opened man lands double |
| 4 | Recon | lays Opening | His marking shot leaves an Opening (the old roster's Expose) |
| 5 | Chemist | cashes Snare | A snared man can't walk out of the gas, so it hits double |
| 5 | Disruptor | lays Hex | The man whose wind up he breaks is cursed (road only, as his attack) |
| 6 | Conduit | cashes Hex | His beam on a cursed man burns double |
| 6 | Herald | lays Hex | Everyone who hears the horn is cursed |

The whole web with the five:
- Snare: only the Trapper lays it (20 Oct). The Marksman, the Artillerist and the Chemist cash it.
- Bleed: the Trapper, the Field Engineer and the Infiltrator lay it. The Skirmisher and the Berserker cash it.
- Opening: the Warden, the Marksman, the Vanguard and the Recon lay it. The Skirmisher, the Duelist and the Binder cash it.
- Hex: the Hexer, the Disruptor and the Herald lay it. The Warden and the Conduit cash it.

Why, in Claude's words:
- Every new class links to at least one of the five you start with, so a new unlock works in your squad the day you get it.
- Each cash follows the chart (line 1499). Speed escapes shells and gas, and a snare takes Speed away. A spell beats the cursed man.
- Snare stays the Trapper's alone, so it is the scarce mark and its payoffs are the prize. Opening is the common one.

Risk, reward, punish:
- Reward: a cash lands double and pops its word.
- Risk: a casher with no layer in your squad never doubles. A layer does little damage on its own. Three classes want one Trapper.
- Punish: the enemy has the same web. Kill their layer and their casher hits plain. Their Herald curses your whole band and sound can't be dodged, so only Will is cover.

This settles "which marks the new classes lay or cash with their attacks", left open at lines 1734 and 1765.

Still open: how the 36 combos hook into these marks (as line 938 did for the first 15); whether double stays double; how long each mark lasts; the Hex's cash word; whether stagger becomes a mark. Not in the sim, not in Unity. Codex holds the class build (HANDOFF_LOG, 15 Nov).

## Which classes each tribe fields, rolled once, owner decision, 15 Nov 2026

Jacob said tribes are shuffled and never field every class ("all the tribes dont have all units they are shuffled right so a artlier cant be in stoneveil since they are nocturnal and cave dwellers"), then "need to write a randomizer for it". He picked "Roll once" over a roll every run, then said "Lock it" to the table below. The roll ran once with `python tools/class_roll/class_roll.py --seed kindred-siege-class-roll-2`, and the table is now fixed. It does not roll again in the game.

The rules the roll kept, Claude's calls, which Jacob saw before "Lock it":
- Each tribe fields 5 classes, picked from the classes unlocked up to its tier (the five starters plus the two of every tier up to its own).
- Every tribe fields at least one of its own tier's two, so beating it unlocks a class you have fought.
- Four musts from Claude's examples at line 1552: Bloodridge the Vanguard, Stoneveil the Berserker, Skullpeak the Artillerist, Thundercoast the Field Engineer.
- Stoneveil bans the Artillerist (his words). As a tier 1 tribe it could not roll one anyway.
- No two tribes of a tier field the same five.
- Clarth (yours) and the hunters are not in the roll. The 19 tribes with no name yet are slots: nothing was ruled out for them because they have no lore.

| Tier | Tribe | Classes it fields |
|---|---|---|
| 1 | Bloodridge | Skirmisher, Trapper, Hexer, Vanguard, Berserker |
| 1 | Stoneveil | Warden, Skirmisher, Trapper, Hexer, Berserker |
| 1 | Tier 1 tribe C | Warden, Marksman, Skirmisher, Hexer, Vanguard |
| 1 | Tier 1 tribe D | Marksman, Trapper, Hexer, Vanguard, Berserker |
| 2 | Skullpeak | Skirmisher, Trapper, Hexer, Vanguard, Artillerist |
| 2 | Thundercoast | Skirmisher, Vanguard, Berserker, Artillerist, Field Engineer |
| 2 | Tier 2 tribe C | Warden, Skirmisher, Hexer, Berserker, Artillerist |
| 2 | Tier 2 tribe D | Marksman, Skirmisher, Hexer, Vanguard, Field Engineer |
| 3 | Tier 3 tribe A | Warden, Marksman, Trapper, Berserker, Infiltrator |
| 3 | Tier 3 tribe B | Skirmisher, Hexer, Vanguard, Berserker, Duelist |
| 3 | Tier 3 tribe C | Hexer, Vanguard, Artillerist, Field Engineer, Duelist |
| 3 | Tier 3 tribe D | Trapper, Hexer, Artillerist, Field Engineer, Duelist |
| 4 | Tier 4 tribe A | Skirmisher, Trapper, Vanguard, Infiltrator, Binder |
| 4 | Tier 4 tribe B | Skirmisher, Trapper, Field Engineer, Infiltrator, Binder |
| 4 | Tier 4 tribe C | Warden, Marksman, Trapper, Infiltrator, Recon |
| 4 | Tier 4 tribe D | Trapper, Berserker, Artillerist, Field Engineer, Binder |
| 5 | Tier 5 tribe A | Trapper, Hexer, Vanguard, Binder, Chemist |
| 5 | Tier 5 tribe B | Marksman, Hexer, Vanguard, Artillerist, Disruptor |
| 5 | Tier 5 tribe C | Marksman, Skirmisher, Hexer, Field Engineer, Disruptor |
| 5 | Tier 5 tribe D | Vanguard, Berserker, Artillerist, Duelist, Disruptor |
| 6 | Tier 6 tribe A | Marksman, Vanguard, Duelist, Disruptor, Conduit |
| 6 | Tier 6 tribe B | Trapper, Field Engineer, Chemist, Disruptor, Conduit |
| 6 | Tier 6 tribe C | Hexer, Vanguard, Duelist, Disruptor, Herald |

That is 115 tribe and class pairs. Jacob on rank 3, the same evening: "1 upgrade for 1 class in 1 tribe", a unique morph sprite, so one morph per pair. If Clarth fields all 17, that adds 17.

This replaces line 1539's "The five from the start (Warden, Marksman, Skirmisher, Trapper, mage) are fielded by every tribe in its own gear. Each tier adds two, and every tribe of that tier fields them too." The unlock stands: beat any tribe of a tier and that tier's two unlock at camp for good. The 19 slots take their names, lore and looks later (line 1556 keeps the tribes of tiers 3 to 6 open; tiers 1 and 2 each have two slots too). A slot keeps its row when it gets a name.

Still open:
- Two chiefs no longer match their tribe. In the code (`CampaignRules.cs`) Bloodridge's chief is a Marksman and Thundercoast's chief is a Warden, and their rows field neither.
- The 8 Dec slice (line 70) is Clarth against Bloodridge with five classes. Bloodridge's row brings the Vanguard and the Berserker into it.
- Art: sheets that no tribe now uses are Bloodridge's Warden and Marksman, Skullpeak's Warden and Marksman, Stoneveil's Marksman, and Thundercoast's Warden, Marksman, Trapper and Medic. Missing for the four named tribes: Bloodridge's Vanguard and Berserker, Stoneveil's Berserker, Skullpeak's Vanguard and Artillerist, Thundercoast's Vanguard, Berserker, Artillerist and Field Engineer.
- The top tier classes come out rare: one tribe each fields the Recon and the Herald, two each the Chemist and the Conduit.
- The rank 3 morph: line 1663 still says regalia over his own sprite and no new sprite per tribe.
- The script's default seed is 1, which gives two tribes of one tier the same five. The locked table is seed 2.
- Not in the sim (`CampaignRules.cs` still holds the hand written rosters), not in Unity.

## A chief stands apart from his tribe's five, owner decision, 15 Nov 2026

Jacob said "Stand apart" to Claude's question: do chiefs stand apart from their tribe's five, or must each chief's class be one of them? A chief's class does not have to be one of the five his tribe fields (line 1823). Bloodridge's chief stays a Marksman and Thundercoast's chief stays a Warden, as in `CampaignRules.cs`, though their bands field no Marksman and no Warden. The chief is his own unit with his own sprite and kit.

This settles the first open item of line 1823. Not in the sim, not in Unity.

## Rank 3 morphs the fighter into a new sprite, owner decision, 15 Nov 2026

Claude's three regalia looks (crown, halo, orbit of the Whisperer's crescents over the Clarth Warden) were rejected: "there are marks then there are debuffs a lot of stuff on screen these will get lost in background". Jacob instead: "it is less tax to morph them into something else unique sprite so it adds variety and cool factor rather than feathers", and "1 upgrade for 1 class in 1 tribe". Then "Lock the morph" to Claude's question.

- At rank 3 the fighter morphs into a new, unique sprite. One morph per tribe and class pair, only for the classes that tribe fields (line 1823): 115 for the 23 tribes, and 17 more if Clarth fields all 17.
- The rest of rank 3 stands (line 1663): he keeps his class and his moves, takes the second slot and his promoted name (line 1677). The 17 stay 17.
- Nothing floats on or around the fighter for rank 3.

This replaces line 1668's "the Whisperer's full regalia, worn on top of his own sprite ... No new sprite per tribe: one regalia set per class", line 1702's "the name and the Whisperer's full regalia are the promotion", and, for rank 3, line 720's "Her rank ups show as new regalia on the fighter". The regalia sets left open at lines 1675, 1704, 1734 and 1765 are now the morphs, and the rank 3 item left open at line 1870 is settled.

Still open: what each morph looks like (a taste call, three options); whether rank 2 still adds a regalia piece (line 813); the morph moment on screen; the art. Not in the sim, not in Unity.

## Rank 2 shows nothing on the fighter, owner decision, 15 Nov 2026

Jacob said "Show nothing" to Claude's question: does rank 2 still add a regalia piece, or show nothing? Rank 2 changes nothing on the fighter's sprite: no regalia piece, no new sprite. Its effect stands (line 1667: the class spell happens one more time, as a plain hit). Only rank 3 changes the look, with the morph (line 1880).

This replaces line 813's "Each rank adds a piece of the Whisperer's regalia to the fighter" and the rest of line 720's "Her rank ups show as new regalia on the fighter". No rank wears regalia now. Not in the sim, not in Unity.

## The Butcher's combos cut to 12, one per effect, owner decision, 16 Nov 2026

Jacob asked Claude to check the 36 new combo hooks again for overlaps "across all 4 pals". Claude and a separate read of the locks found that almost every hook, and 16 of the 36 combos under them (line 1740), copied another Pal's verb, a class's own move or a Butcher burst. Jacob picked "Combos first". Claude's 16 replacements failed a second separate check: only Falling Door passed. Claude then proposed one theme per flavour repeated across classes, as Hades repeats a god's effect across slots; Jacob: "Theme is overlap". Claude counted about 13 different effects in the Butcher's three verbs; Jacob: "Yes, cut to 13". It came out at 12, because Dead Weight's heavy blade is the stagger the Warden and the Vanguard already have (lines 1478, 1712). Jacob: "Lock these 12". Where this differs from the sections above, this wins.

**The rule.** Each combo has an effect no other combo has, and a theme repeated across classes counts as overlap. Each effect sits on one class, a different class where it can be, using only combos that passed both checks or are locked and built. The Binder carries two, because both of his effects only work with his tether.

**The 12**

| Effect | Combo | Class | Flavour | What it does |
|---|---|---|---|---|
| Drag men into one spot and crush them | Wrecking Ball | Vanguard | pull | A black hole rides his charge, drags the men near his path along and crushes them at the end (line 1742) |
| Pin one man flat | Collapse | Skirmisher | pull | Her second strike drops a black hole that pins the man flat for 2 s (line 725) |
| Pull in a heavy man | Heavy Hook | Binder | pull | His tether can pull a heavy man too, and he is crushed when he lands (line 1748) |
| Reach their back row | Vent | Chemist | portal | The cloud comes out of a door in their back row (line 1750) |
| Split the hit onto a second man | Split Hex | Hexer | portal | A door carries half her burst to the enemy furthest from her target (line 1623) |
| Drop a charger into a trap | Doorway | Trapper | portal | A charging enemy falls through a door into her snare, and shooters spring her trap too (lines 726, 838) |
| Take one man out, one on one | Private Door | Duelist | portal | A door takes him and the man in front of him out of the crowd for 3 s (line 1746) |
| Drop down from above | Falling Door | Berserker | portal | His rush goes into a door, and he drops out of one above the man, landing on him and every man around him. New, in place of Scent Door |
| Drag a back row man through a door | Long Hook | Binder | portal | His tether goes through a door and pulls a man out of their back row (line 1748) |
| Freeze a man | Still Wall | Warden | time | Whoever hits his shield freezes for 2 s while your squad hits him (line 727) |
| Hang a hit and land it later | Stopped Bullets | Marksman | time | Three shots hang frozen, then all hit at once (line 727) |
| Slow a man | Stopwatch Sentry | Field Engineer | time | Men the sentry hits move at half speed for 3 s (line 1745) |

- Stopped Bullets goes back to its first form. Its 29 Oct rethink (line 926) is dropped: "cannot miss or be guarded" is the Exposed burst, and each hanging bullet freezing its mark is Still Wall's freeze.
- The pacts (line 897, fix 1), the flavour stacks (line 1018) and the rank ups (line 1028) stay. Pull and time have three combos each and portal six, so a pull or time build leans on rank ups to burst.

**Cut, 40 combos.** 51 were locked; 11 stay and Falling Door is new.
- From the first 15 as amended on 15 Nov: Black Hole Round, Around the Corner, Execution Corridor, Frozen Flurry, Event Horizon, Singularity Net, Time Snare, Curse Well, Old Wounds and Wrong Door.
- From the 36 at line 1740: Through Charge, Standstill Charge, Red Giant, Scent Door, Overtime, Gravity Shell, Trapdoor Shell, Frozen Crater, Lodestone Sentry, Muzzle Door, Dead Weight, Stolen Second, Undertow, Shortcut, Between Seconds, Still Hook, Lens, Peephole, Rerun, Choke Well, Thick Air, Implosion, Short Circuit, Lost Time, Accretion, Bent Beam, Standing Beam, Low Note, Earshot and Hush.

**What else this changes**
- Six classes have no Butcher combo: the Artillerist, Infiltrator, Recon, Disruptor, Conduit and Herald. The Butcher's road offer shows only combos for classes in your squad (line 875).
- Six Glutton talents boosted a cut combo and have nothing to boost until they are reworded: Light Stake (Black Hole Round), Gravity Mesh and the Stasis Wind Up keystone (Singularity Net), Time Lock and Crawl Field (Time Snare), and Rewind Wound (Old Wounds). Three of them are the Control limb.
- Shield Pin, the Warden's boost (line 1593), uses the same 2 s pin as Collapse.
- The mark hooks of the cut combos go with them (lines 950 to 964, and the 36 hooks Claude proposed on 15 and 16 Nov, never locked).

This replaces: line 724's 15 combos and lines 725 to 727 where they differ; the four at line 1618; line 1738's three per class, one per flavour, and the 36 at line 1740; the 29 Oct rethinks of Around the Corner, Stopped Bullets and Time Snare (lines 926 to 932).

Still open: the mark hook of each of the 12; new targets for the six Glutton talents; every number. Not in the sim (rules 15 still plays the first 15), not in Unity.

## Four hooks, six talents, stagger and the slice's classes, owner decisions, 16 Nov 2026

After the cut to 12 (the section above), Claude ran four jobs at once and brought back five calls. Jacob: "Yes to all four", to the four yes or no calls: hooks on only four combos, the six Glutton talents reworded, stagger stays a plain status, and the 8 Dec slice keeps the old five classes. The fifth, the Hex's cash word, is still his pick. Where this differs from the sections above, this wins.

**Hooks on four of the 12.** A hook is one mark a combo lays or cashes (line 940), and a cash lands double. None of the 12 lays a Hex and only the Trapper lays a Snare, so only about six hook jobs are free. A separate check found the other hooks pair up: three lay an Opening, two cash a Hex, two cash a Bleed, two cash an Opening and two lay a Bleed. By "Theme is overlap" a pair is one hook, so hooks go only where a job is free and the picture is good.

| Combo | Class | Hook | What you see |
|---|---|---|---|
| Wrecking Ball | Vanguard | cashes the Snare | A snared man in his path is torn out of the trap, and the crush lands double and spends the Snare |
| Doorway | Trapper | lays an Opening | The charger lands face down in her snare and is left with an Opening |
| Private Door | Duelist | lays a Bleed | The man comes back out of the door cut and bleeding |
| Stopped Bullets | Marksman | cashes the Bleed | On a bleeding man the three bullets land in one wound, double, and spend the Bleed |

- Each hook is a different job: one Snare cash, one Opening lay, one Bleed lay, one Bleed cash.
- The other eight have no hook: Collapse, Heavy Hook, Long Hook, Vent, Split Hex, Falling Door, Still Wall and Stopwatch Sentry. No combo cashes a Hex or an Opening.
- This replaces the 29 Oct hooks of the combos that survived (lines 951, 952, 957 and 959): Stopped Bullets' Opening per frozen bullet went with its rethink, Still Wall no longer leaves an Opening, Collapse no longer cashes a Bleed, and Doorway's shooters springing her trap is now part of the combo itself (the 12 table above).

**Six Glutton talents get new targets.** Same names, limbs and costs. Each still only makes another Pal's piece bigger, longer, faster or cheaper (line 1073).

| Talent | Limb and tier | Was | Now |
|---|---|---|---|
| Gravity Mesh | Control 1 | Singularity Net crushes harder (line 853) | Spells, shells, gas and sound can spring Doorway's trap too, not only shots |
| Time Lock | Control 2 | Time Snare's slow lasts twice as long (line 853) | A Snare takes two cashes before it is spent |
| Crawl Field | Control 3, the other pick to Fold Trap | Time Snare makes big moves wind up three times as slowly (line 766) | Heavy Hook's crush hits twice as hard |
| Stasis Wind Up | Control keystone | Singularity Net drags in twice as many (line 766) | On a snared or tethered man, the Butcher's flavour stacks (line 1018) burst at 2, not 3 |
| Light Stake | Shooters 3, the other pick to Probability Lock | Black Hole Round crushes twice as hard (line 765) | Stopwatch Sentry slows men to a quarter speed, not half |
| Rewind Wound | Casters 2 | Old Wounds replays 5 s, not 3 (line 1653) | Vent's cloud is twice as wide |

- The Control limb is now the two classes that hold men: the Trapper (Snare, Doorway) and the Binder (tether, Heavy Hook). In the 8 Dec slice only the Trapper exists, so only Gravity Mesh and Time Lock work there.
- These six had no separate overlap check before Jacob's yes. The second pass on the rest of the tree checks them against every other talent.
- Still broken, for that second pass: Neutron Mass, Fold Step, Fold Shield and Glancing Blow boost nothing now, and three pairs do the same thing: Fold Shield and Entropy Purge, Strip and Half Life, Nucleus Edge and Matter Printer.

**Stagger stays a plain status.** It has no cash word, no cash, and it is not a mark. The Warden's bash, the Vanguard's charge and Headbutt, and the Artillerist's Concussion Shell stagger (lines 1478, 1712, 1714, 1790). The bash, the charge and a parried duel champion (line 983) already leave an Opening on the same blow, so a stagger mark would be the same moment twice. The sim needs one plain timer, staggered until a tick, that Bloodrush (line 1713) can read; today a stagger only pushes back the man's next swing. This settles line 1821's "whether stagger becomes a mark".

**The 8 Dec slice keeps the old five classes.** For the slice only, Bloodridge and Skullpeak field the Warden, Marksman, Skirmisher, Trapper and Hexer (the old Medic), as line 70 says. All five have battle art in both facings. The rolled rows (lines 1837 and 1841) start after 8 Dec, with the new classes. Why: the rolled rows bring three classes into the slice, Bloodridge's Vanguard and Berserker and Skullpeak's Artillerist, with no art and none in the sim, 22 days before the endorsement. This answers line 1867.

Still open: the Hex's cash word (HEXED, DOOMED or BLIGHTED, his pick); the second pass on the rest of the tree; who draws the new classes' battle sheets after 8 Dec (CLAUDE.md gives sprite animation to Codex, HANDOFF_LOG 15608 moved unit animation to Claude); every number. Not in the sim, not in Unity.

## The Glutton's whole tree retargeted, owner decisions, 16 Nov 2026

The second pass on the tree (the section above left four talents boosting nothing and three overlapping pairs) failed its separate check, 2 of 11 clean. Claude asked whether overlap means the same thing boosted or the same kind of boost. Jacob: "Yes, same thing". Claude ran a third pass with three more separate checks. Jacob said "Yes, lock it" to all 35, with Fold Trap on Gauntlet. Where this differs from the sections above, this wins.

**What overlap means for a talent.** Two talents overlap when they boost the same property of the same piece. Two talents that give the same kind of boost (two "hits harder", two "lasts longer") to different pieces do not. A talent also fails when it copies, onto another piece:
- the Whisperer's rank 2, the class spell one more time;
- a Whisperer habit upgrade, such as Spotter's Opening that stays until someone cashes it (line 1186);
- the Butcher's rank up (more stacks) or his bursts;
- a Shade verb, such as Mirror's return (line 1253);
- a class move, a mark's cash, or a gear piece (ClassBend in ClassSpells.cs, such as Barbs, the snare's bleed twice as hard).

The Glutton's rule stands (line 1073): a talent only makes what another Pal, a class, or the parry and its meter give bigger, longer, faster or cheaper. It adds no effect of its own and is not a plain stat boost.

**The parry stays risky** (lines 1218 to 1223). No talent may soften the parry's risk, the 0.2 s window and the whiff, or pay the safe answer more: Brace, Sidestep, Fall Back and its routes. Brace and Sidestep only answer a band heavy while Hold is down, so boosting them makes skipping the parry cheaper. The parry's reward (line 1222: it cancels the move, staggers 2 s, fills a bar and counters) may be boosted.

**The 18 that change.** Same names, limbs and costs.

| Talent | Limb and tier | Was | Now | Why it moved |
|---|---|---|---|---|
| Dark Matter Plating | Front line 1 | Every hit on the front line loses some of its weight | Shield Pin (line 1593) comes round every 10 s, not 15 | A plain stat boost; "front line" is row wording |
| Entropy Sink | Front line 2 | Front line cooldowns refill faster | The Warden's bash on a cursed man (line 1573) lands triple, not double | A plain stat boost |
| Fold Shield | Front line 3, the other pick to Neutron Mass | Front line portals cost half the nerve | Still Wall (line 1917) freezes 3 s, not 2 | No front line portal since Wrong Door was cut; it also matched Entropy Purge |
| Neutron Mass | Front line 3, the other pick to Fold Shield | Event Horizon pulls from twice as far | Wrecking Ball (line 1908) drags men from twice as far | Event Horizon was cut |
| Branch Anchor | Front line keystone | Sanctuary's shields on the front line are twice as thick | Sanctuary's shields (line 1244) are twice as thick | Wording only: drops "front line" |
| Nucleus Edge | Strikers 1 | Strikes hit harder | Twin Cut's two cuts hit twice as hard | A plain stat boost; it matched Matter Printer |
| Strip | Strikers 2 | Openings last twice as long | Her strike rips a bleed open half again as hard (Twin Cut's bleed, which her next strike rips open) | A longer Opening is the Whisperer's Spotter upgrade (line 1186) |
| Fold Step | Strikers 3, the other pick to Echo Strike | Execution Corridor hits 50% harder | Collapse's own pin (line 1909) lasts 3 s, not 2. Shield Pin stays 2 s | Execution Corridor was cut |
| Answer the Parry | Strikers keystone | Strikers hit twice as hard on a parried enemy for 2 s | The Duelist's Riposte (line 1716) lands triple, not double | A parry staggers for 2 s, so it was Bloodrush's double on a staggered man |
| Slow Light Rounds | Shooters 1 | Stopped Bullets' freeze lasts 1 s longer | On a bleeding man, Stopped Bullets (its hook, line 1949) lands triple, not double | The freeze was dropped from Stopped Bullets |
| Dark Matter Rounds | Shooters 2 | Marksmen's shots go through half of armour | Steady's shot goes through at full power, not half | Through armour copied Crack Armour |
| Matter Printer | Casters 1 | Caster blasts land bigger | Bad Blood's bleed (line 1595) jumps twice as far | A plain stat boost; it matched Recompile and Nucleus Edge |
| Precognition | Command 1 | The parry window is wider | A man you parry is staggered 3 s, not 2 (line 1222) | A wider window makes the parry easier |
| Glancing Blow (Glancing Fold, line 768) | Command 2 | A Hold that soaks soaks half as much again | Mirror (line 1253) sends back twice as much | Since rules 26 a missed Hold is a whiff, not a soak |
| Deep Node | Command 3, the other pick to Charged Branches | The Shade's tricks hit a third harder | Rupture's Crack (line 1255) thins armour twice as much | A Rift Mine version paid Fall Back, the safe answer |
| Command Relay | Command keystone | The Shade's route prices are halved | Your chief spell (line 1140) hits twice as hard | Halved prices made Fall Back's routes cheaper too |
| Synthesis Discount | War chest 3, the other pick to Gene Library | The Butcher's pacts cost a third less | The forge (line 884: tabard, trophies, chief gear) costs a third less | Cheaper pacts copied Entropy Purge's cheaper portal |
| Fold Trap | Control 3, the other pick to Crawl Field | Doorway catches two chargers, not one | Gauntlet (line 1718): the free swings on the man the Binder pulls hit twice as hard | A second charger is the Trapper's rank 2; every other Trapper piece is taken (Barbs, Long Snare, Time Lock, Gravity Mesh, instant Rearm) |

**The 17 that keep their meaning.**
- Strikers: Echo Strike, Echo replays Skirmishers' hits at full strength.
- Shooters: Light Stake, Stopwatch Sentry slows men to a quarter speed, not half (16 Nov). Probability Lock, Marksmen hit a snared enemy 25% harder. Nothing Wasted (keystone), Marksmen hit your Focus Fire target twice as hard.
- Control: Gravity Mesh, Time Lock, Crawl Field and Stasis Wind Up, as the section above words them.
- Casters: Rewind Wound, Vent's cloud is twice as wide (16 Nov). Half Life, the Hex lasts twice as long. Entropy Purge, caster portals cost half the nerve. Recompile (keystone), caster blasts are twice as big on anyone under a quarter health.
- Command: Charged Branches, each held parry bar adds more to combos.
- War chest: Rogue Planet Prospecting, won fights send home more. Branch Hedge, your streak survives one lost fight a run. Gene Library, pairs answer twice as often. Node Cache (keystone), every Pal card shows 4 picks.

**Numbers to watch when it is built.** Every number is a starting value.
- Precognition: the 30 Oct duel tune was set with a 2 s stagger, so the duels need a rerun.
- Glancing Blow: with Mirror's Catch, every parry sends back 30% of the man's health.
- Command Relay: a grade 3 chief spell would take 100% of health.

**At risk.**
- Branch Anchor, Echo Strike, Glancing Blow and Deep Node sit on the Shade's routes. GDD_CURRENT says (SAID 12 Nov, not locked) the routes become unit moves handed out by the Whisperer and Echo is dropped. If that is locked, Echo Strike needs a new target; the other three follow their route to its unit.
- Sanctuary's price (your parries no longer stagger, line 1244) switches Precognition off.
- Fold Trap and Crawl Field, the Control tier 3 pair, both make the man the Binder holds take more, so the pick is close.
- Gene Library rests on pairs, parked on 15 Nov.

**Dead in the 8 Dec slice,** which fields only the old five classes: Neutron Mass, Answer the Parry, Light Stake, Crawl Field, Rewind Wound and Fold Trap. The Control tier 3 pair has no live pick there, and Rewind Wound, Casters tier 2, costs 2 points for nothing.

**Not in the sim.** GluttonRules.Describe in PalKit.cs still has the 1 Nov words, and the limb names still say Healers. Line 1269 made that text the meaning; from now on this section is the meaning, and the code follows it when it is built. The enum keeps its order and numbers, so saves keep working (SynthArmour is Half Life).

This replaces: every older wording of the 18 above, including the 28 Oct tree (lines 764 to 769), Command Relay at lines 781, 848 and 1115, Jacob's calls 4, 5, 6 (the Synthesis Discount half) and 8 at line 1266, Claude's Fold Step call at line 1270, and the "still broken" note in the section above.

Still open: which classes each limb covers (each talent above names its own piece); the Hex's cash word (HEXED, DOOMED or BLIGHTED); who draws the new classes' battle sheets after 8 Dec; every number. Not built, not in Unity. The sim work waits for Jacob's go.

## The Hex's cash word, owner decision, 16 Nov 2026

Claude gave three words for the moment a Hex is cashed: HEXED, DOOMED or BLIGHTED. Jacob picked 1, HEXED. The Hex is cashed when the Warden's bash on a cursed man lands double and spends the curse (line 1573), and when the Conduit's beam on a cursed man burns double (line 1800).

- HEXED pops over the man when his Hex is cashed, the way PINNED, OPENED, RIPPED and EXPOSED pop for the other marks.
- It is made like the other cash words: the game's 32 px logo font (Menu/font_logo), filled with the mark's colour, with a 1 px dark edge (28, 18, 14), by make_words.py in Art/Studies/Attack-Table-20261107/words. The file will be cash_hexed.png next to the others in CampaignArt/UI/Marks.
- At 5 letters it is narrower than RIPPED, so it crowds the brain plates less.

This settles "the Hex's cash word" at line 1821 and in the two sections above.

Still open: the Hex's colour (Codex suggested green, HANDOFF_LOG 15620; the nearest colour in use is the Snare's mint on PINNED); which classes each Glutton limb covers; who draws the new classes' battle sheets after 8 Dec; every number. cash_hexed.png is not made yet. Not built, not in Unity.

## The cash words at pixel size 2, and the Hex's colour, owner decisions, 16 Nov 2026

Claude showed HEXED in three colours beside the six cash words. Jacob: "they need to be more pixelated". Claude showed two ways; Jacob picked 1, "obviously 1": the same letters on a grid twice as coarse. Then the colour: "blak", the black curse.

- Every cash word is drawn at pixel size 2, the pixel size the rest of the UI uses (Px=2 in CampaignArtPixelUI.cs). The 1 Nov words were 1x, finer than everything else on screen. Same font (Menu/font_logo), same letters, same colours and the same one cell dark edge. Each letter is shrunk to the coarser grid, and the R is drawn by hand so it does not read as an A.
- HEXED is near black (46, 26, 54) with a bone edge (239, 223, 190), the reverse of the other six. It is the only cash word with a light edge.
- The seven files are in Art/Studies/Marks-CashIn-20261116: cash_pinned, cash_opened, cash_ripped, cash_crushed, cash_exposed, cash_frozen and cash_hexed, made by make_cash_px2.py there. They sit on the same 34 px tall canvas as the 1 Nov files, so they swap in by file name. Art/Studies is git ignored.

This replaces the 1 Nov cash word art (HANDOFF_LOG 13594) and settles the Hex's colour, left open in the section above.

Not changed: the table words (DODGE, PARRY, BLOCK, CRIT) and the status words (BLEED, SNARED and the rest) were made the same way and are still 1x. Not in Unity: the new files are not in Assets yet.

Still open: which classes each Glutton limb covers; who draws the new classes' battle sheets after 8 Dec; every number.

## Which classes each Glutton limb covers, owner decision, 16 Nov 2026

Jacob: "group classes in one tree like mdps in one limb that spits and rdps in one curse guys in one big guys in one limb etc". Claude grouped the 17 classes by role, and Jacob said "Yes, lock it". Where this differs from the sections above, this wins.

| Limb | Role | Classes |
|---|---|---|
| Front line | the big guys | Warden, Vanguard |
| Strikers | melee damage | Skirmisher, Duelist, Berserker, Infiltrator |
| Shooters | ranged damage | Marksman, Recon, Field Engineer, Artillerist |
| Control | the holders | Trapper, Binder |
| Casters | the curse guys | Hexer, Conduit, Disruptor, Chemist, Herald (as locked on 15 Nov) |

- Command and War chest cover no class: Command is your side and War chest is the run (line 1269).
- The Berserker has high health, but his kit is all damage, so he is a Striker, not a big guy.
- Each limb holds one of the 8 Dec slice's five classes: Warden, Skirmisher, Marksman, Trapper and Hexer.
- The talents already point at these classes' pieces (the whole tree section above): Front line boosts Shield Pin and Wrecking Ball, Strikers boost Twin Cut and Riposte, Shooters boost Steady and the sentry, Control boosts the Snare and Gauntlet.

This replaces line 1269's limbs as rows and roles ("front line your front row, strikers your Skirmishers, shooters your Marksmen, control your Trappers, healers your Medics") and settles "which classes each limb covers", left open in the sections above.

Still open: who draws the new classes' battle sheets after 8 Dec; every number. Not built: PalKit.cs still names the limbs with Healers.

## Every pop word at pixel size 2, owner decision, 16 Nov 2026

Jacob: "k do it" to the table words and status words still being 1x, then "Go, both". The 19 status words (CampaignArt/UI/Words43) and the 4 attack table words (Table42Review/Words) are redrawn at pixel size 2 the same way as the cash words in the section above. Where that section says they are still 1x, this wins.

- The files are in Art/Studies/Marks-CashIn-20261116: the cash words in the folder itself, words43/ and table42/, each with its old name and canvas height, made by make_cash_px2.py, which first rebuilds every 1x file pixel for pixel to prove its text and colours.
- The font draws Q to Z a pixel lower and shorter than A to P. At pixel size 2 that put R, S, U, X and Z 2 px out of line, so every letter is now top aligned, and the seven cash words were remade with this fix. K and G are drawn by hand like the R.

Not in Unity: none of these files are in Assets yet.

## Tectonic Shift, the leader's gravity tool, owner decision, 16 Nov 2026

Jacob's seed, 15 Nov: "shifts gravity to create more damage bro more heavy hits argon is encapsulated into crystals that are left behind on succesful parrys break these". On 16 Nov he answered Claude's paper with "animate it", and to the fifth cut: "Yes, lock it".

Locked: Tectonic Shift as cut 5 plays it.
- Every parry leaves an argon crystal on the floor under the man parried. It stays there when he moves on.
- The press is the leader's relic punch (L1767) with the burst in argon violet, since gold is the parry's alone. Every crystal flashes white and breaks on that frame.
- The floor under each crystal shudders, then tears loose in slabs and heaves up. Whoever stands there goes up, our own men too: his duel breaks, he can't swing or dodge, and hits on him land heavy. Then the slabs slam down and he lands on his back and gets up.
- Two crystals close together break the floor as one bigger break, and the men on it go higher and hang longer.
- No crystals, no shift.

His calls on the way:
- The crystal grows inside a ring of light: take 2 of 3, his pick after "crystal spawn like aids bro need more good animation".
- The floor never breaks in a drawn shape ("how will grounf break in a perfect circle omg"). It splits into slabs cut from the court's own floor along cracks. He picked "2 Slabs" over a tent and a shatter.
- No lines ("bro those lines are the problem omg"). The cracks are never drawn, they only set where the floor cuts. The gaps under and between the slabs are the floor's own colour in shadow, and the violet light comes up out of them onto what is above.

Claude's calls, not his words:
- The slabs shudder a pixel or two on the punch, snap up past their height and settle, and stay off the floor until the slam frame. After the slam the floor is whole.
- A small argon core lights where each crystal burst, only as it bursts. The lifted men take the violet light from below in three steps, outlines dark, less the higher they go. Slab faces near a crystal take a little of it.
- Grit of the court's floor sprays out as the slabs snap up.
- A lifted man plays his own death clip, knocked back then tumbling, with violet motes rising under him, and gets up with his get up clip from the combat study. Our Trapper's launch is PixMiniMax from his hurt frame, mask turned away.
- HEAVY shows on each man's first heavy hit.
- In the cut, the normal blows before the press are parried with the relic punch too. That is the open question at L1782, not settled here.

The files sit outside the repo in the side session notes folder, tectonic-20261116: tectonic-shift-5.mp4 with its timeline, build_ts.py and breaks.py (run build_ts.py --take slabs with that folder on the Python path), and dl/ with the PixelLab pieces: the crystal (object 995d972a), its grow, the motes pulled out of a parried man, the cracks that guide the cut, the lift motes and the grit. The workbench effects are in his My effects named "ts ...". The mock's screen shakes fire 9 to 19 frames before their hits, because they run on the screen's frame count; they belong on the hit frames.

Still open: how it works at the champion and the chief, where a button never stops a boss move; its half string in the build up; its tiers; whether crystals fade; sound; every number. Not in the sim, not in Unity, not with Codex.

## Tool 3, the relic's ground attack, owner decision, 18 Nov 2026

This replaces the 17 Nov section "Tool 3, the relic turns the ground" (the grave Hex in cut 2). Jacob on that version, seen again: "i see some skulls and ground going black wtf how is that a unique game changing thing omfg". Then: "arent we using terrrain to create an attack by sending relic into the bnattle based on it it will launch an attack"; that it must work on bosses ("It wouldn't technically be a unique button in a kit for a leader if it is not usable against bosses"); and "okay bro scrap everything listen keep it an attack it interupts units and boss cleaves/aoe attacks from hitting ... changes terrain for 4-5 sec depenind on terrain units get buff/debuff but whatever attack does the terrain change should feel like a residue of it". He said "yes" to Claude's play back below, then "Yes, swap both".

Locked, his words:
- The leader sends his relic into the fight and that stage's ground launches an attack on their men there. The same on the road, at the champion and at the chief.
- The hit interrupts their units: whatever they were winding up or swinging doesn't land.
- At bosses it interrupts cleaves and AoE attacks, so they don't hit. This is his exception to the 12 Nov rule that only the parry cancels a boss attack. Tank busters, casts and the build up string stay the parry's alone.
- The ground changes for 4 to 5 s, and the change is what the attack left behind: a buff for our men on it, a debuff for theirs.

Claude's calls, in the play back he said yes to (every number a starting value):
- Press and pick a spot. The relic flies there and stops; the floor under it takes his violet, about 4 fighters across, which is the zone. It comes back about 1.5 s after the press.
- If the zone covers a boss winding up a cleave or AoE, it breaks even while he can't be hit (L1340); only the damage misses him. The champion can be hit from the start (L1348).
- The five grounds (the attack / what it leaves / ours on it / theirs on it):
  - Bloodridge, the root bound burial court: roots tear up in straight lines out from the relic / tangled roots, 5 s / they take less damage / they can't step out of their duel.
  - Stoneveil, the cave: stone spikes drop from the roof, one on each of their men, shadows first / rubble, 4 s / they hit harder / they hit lighter.
  - Skullpeak, the frozen ground: frost spreads, then the ice splits under all of them at once / an ice sheet, 5 s / they act faster / they can't dodge.
  - Thundercoast, their flooded floor: coral grows round their biggest man and crushes him / the reef, 5 s / up out of the water, so the flood can't touch them, though the water still rises on its own clock / the coral cuts them.
  - Clarth, the Broken Wastes: the sand blasts out from under the relic, hardest in the middle / dust in the air, 4 s / they dodge more / they miss more. Met in the sand shark's fight.
- The hunters have no home ground; the relic uses whatever ground the ambush is on. The 57's ground is open. Each of the 19 unnamed tribes gets an attack, what it leaves and one buff and one debuff when its stage is drawn, in a shape the five don't use.
- While the relic is out he can't parry, if every parry is his punch on the relic (open, L1782).
- Dropped with the 17 Nov version: the grave Hex, ground hazards that fire on their own, live patches, and marks from the ground.

The rules sit outside the repo in the side session notes folder: tool3-20261117/tool3-ground-rules.md (draft 5). The 17 Nov mock (tool3-relic-2.mp4, build_t3b.py) shows the old Hex version; only its relic flying in and stopping still matches.

Still open: its name, tiers and sound; every number (the zone, 1.5 s, the 4 or 5 s, each buff and debuff, the hit); whether the attack can kill or leaves the kill to the units (GDD: "The units keep the credit"); its half string in the build up (L1437); whether the relic punch is every parry (L1782); the 57's and the unnamed tribes' grounds. Not in the sim, not in Unity, not with Codex.

## The Pal and leader screens: FORGE, COMMANDS and WAR TABLE, owner decision, 18 Nov 2026

Jacob, 17 Nov, on the clickable camp: "use this link up each pals and leader ui screen next". Claude gave three ideas for each missing screen; Jacob: "2 2 3" (FORGE idea 2, the pour; COMMANDS idea 2, the four hands; WAR TABLE idea 3, the table), then "Go" to the route: rebuild the UI kit from the two locked screens, paint each scene with GPT images through the Codex app, put the real sprites and words back on a 2x grid. Round 1 and its fixes, 18 Nov. Jacob: "Lock these bro ask codex to hook em up in unity and let's go".

Locked: the look and layout of the three screens, as in Art/Studies/Pal-Screens-20261118 (forge-locked.png, commands-locked.png, wartable-locked.png, 1440x900). They make one set with the Glutton's TALENTS (round 22, line 1066) and the Whisperer's SQUAD (pod round 6, line 1120).
- FORGE (the Butcher): his forge floats in the cult's astral camp. The Butcher stands on the left, a fighter on the right, and between them a bowl of molten metal with three moulds sunk in it, blue, purple and gold. A crucible on a chain pours into the picked mould. The picked mould's label sits under the bowl, the other two above it.
- COMMANDS (the Shade): she floats over her pink basin. Four arms of shadow grow from her back and hold your four buttons (Hold, Fall Back, Focus Fire, Unleash) as gold rings, each with its two routes as cards beside it. The picked card gets corner brackets. A strip bottom right shows your four buttons and what each one holds.
- WAR TABLE (the leader): a long table of astral light runs away from him, with the run's five banners on it (Bloodridge or Stoneveil, Skullpeak or Thundercoast, then the hunters, line 405) and roads of light between them. The picked road is gold, the others pale blue. Your squad sits in the tabs strip bottom right.
- Every screen: the title in the logo font with the gold rule under it, a number box and a note top right, the other Pals' portraits down the right edge (each opens that Pal's screen), and the picked thing's panel bottom left with its cost plate and button. The kit and the words are at pixel size 2; the painted scene is cut to 48 colours on the same 2x grid.

Not locked: the words and numbers on them. They are mock, and where they differ from these sections, these win:
- Combos are road picks that last one run, and augments have no price (line 866). The camp forge makes the tabard, trophies and chief gear. So the FORGE's augment for "60 MATERIALS", a price Claude made up, sells nothing that exists.
- Black Hole Round and Around the Corner were cut on 16 Nov (line 1898). Of the Marksman's three, only Stopped Bullets is left.
- The Shade's forms come on the road; camp pays to unlock second and third forms for 150 and 250 (line 866). The COMMANDS "30 MATERIALS", "2 OF 2 BUTTONS ENHANCED" and "SECOND FORMS AT TIER 3" come from the 28 Oct sections before that (line 781).
- Panel words were cut to fit four lines: the FORGE dropped "Gravity Well on the Marksman's shot", COMMANDS dropped "Price:" and "Forms:", and the WAR TABLE ends "Break him, take the Eclipse."

Claude's calls, not his words: the mould colours (the augments' colours, lines 725 to 727); the HEXER tab in place of MEDIC (line 1558); the WAR TABLE's four portraits at a step of 126 px so the fourth clears the strip; the mould labels above and under the bowl.

Known faults, left for the build: "YOUR SQUAD" touches the table's corner on the WAR TABLE; the tiny font's comma reads as a full stop; the FORGE panel's icon is a plain purple disc; the big numbers are smaller than on the two locked screens.

Still open: what the FORGE's three moulds hold at camp; animation (the pour, the arms, the banners); sound; every number. Not in Unity yet; with Codex from 18 Nov (HANDOFF_LOG.md, 18 Nov entry).
