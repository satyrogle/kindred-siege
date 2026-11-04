# Shape carries class, trim carries tier

Draw each gear piece as a 32 x 32 px icon whose outline shows the class that wears it and whose trim shows its tier. Keep it off the fighter's body unless Jacob reopens his 20 Oct lock. The clearest gear in the games studied reads at a glance the same way: a single fixed pose per item type, a bold silhouette, a 1 px dark outline, 7 to 15 colours, and tier in an accent colour or a frame. Kindred Siege's 2 health "mails" for the Skirmisher and the Warden are the hard case, and they need different silhouettes. Games that reuse a picture across tiers (Diablo 2) or only swap its colours (Slay the Spire 1's potions) leave players reading text to tell items apart. A full paper doll on these 240-frame fighters would need 240 layer frames per gear look per body, 480 for a cloak, and no tool the team has makes layers that line up from frame to frame.

The free local route that fits is a reference edit from an approved Bloodridge sheet on FLUX.2 klein 4B or Qwen Image Edit 2511, both Apache 2.0. A feature-keeping downscale, a snap to the tribe palette, a 1 px outline and a hand clean in Aseprite follow. The report offers 3 directions to mock with it. A puts a distinct object icon on the card only and fits the lock as written. B builds pieces from a shared base and a bend emblem, also worn as a small badge at the health bar, and needs Jacob's yes on the badge. C wears a single signature piece on the fighter and needs Jacob to reopen the lock. Before anything ships, 2 licence questions need answers: the local Ideogram 4 weights behind the approved chief portraits sit under a non-commercial agreement, and any FLUX [dev] model needs a paid licence for a commercial game. Codex's exact keys and sizes could not be read, so the sizes here come from the game's existing 32 and 48 px icons. Most web evidence came from search summaries, because this session blocked most sites outside GitHub.

## Good gear icons hold the pose fixed and change the shape

The study covered 7 pixel games and 10 painted roguelikes, for craft only. Each row below is a size, a rule or a number. None of it is a look to copy, since Jacob rejects anything that reads as a famous game's look. Figures marked measured come from game files or decompiled code on GitHub. Cells marked (summary) come from search summaries of pages this session could not open.

### Pixel games draw small, at 1x, with a single pose per item type

| Game | Icon size | Silhouette and colour | How tier shows | How 2 pieces of one slot differ |
|---|---|---|---|---|
| Fire Emblem GBA, FE8 (measured) | 16 x 16, art about 14 x 14 inside a 1 px empty border, shown 1:1 on a 240 x 160 screen ([icons](https://github.com/FireEmblemUniverse/fireemblem8u/tree/HEAD/graphics/item_icon); [screen](https://github.com/FireEmblemUniverse/fireemblem8u/blob/HEAD/include/gba/defines.h)) | A single 16-colour palette shared by all 25 icons sampled, 7 to 15 colours each. Dark tinted outline (#293920, #523941), no pure black. Edges are lighter on the top and left, so light comes from the top left. Every blade sits on the same diagonal | Drawn into each weapon's own picture. The E to S letter rank sits in the data only ([data_items.c](https://github.com/FireEmblemUniverse/fireemblem8u/blob/HEAD/src/data_items.c)) | Width and mass: iron sword 51 opaque pixels, steel 61, silver 72. Silver adds white and cool accents. Killer weapons add red, orange and yellow |
| Shattered Pixel Dungeon (measured) | 16 x 16 cells, median art 13 x 14; 8 x 8 overlay icons ([ItemSpriteSheet.java](https://github.com/00-Evan/shattered-pixel-dungeon/blob/master/core/src/main/java/com/shatteredpixel/shatteredpixeldungeon/sprites/ItemSpriteSheet.java)) | Pure black 1 px outline all round, median 8 colours per item. All armour in the same front torso pose ([items.png](https://github.com/00-Evan/shattered-pixel-dungeon/blob/master/core/src/main/assets/sprites/items.png)) | Text on the slot: a coloured "+N" and a strength number ([ItemSlot.java](https://github.com/00-Evan/shattered-pixel-dungeon/blob/master/core/src/main/java/com/shatteredpixel/shatteredpixeldungeon/ui/ItemSlot.java)). Weapon tier is a new drawing that grows from 13 to 16 px | Each armour type has its own outline: cloth 15 x 12 with a wide collar, mail 14 x 12 with a chain check, plate 12 x 12 with stepped shoulders. 2 copies of the same armour differ only in the slot text |
| Dungeon Crawl Stone Soup (measured) | 32 x 32, art 26 x 29 to 30 x 31 ([armour tiles](https://github.com/crawl/crawl/tree/HEAD/crawl-ref/source/rltiles/item/armour)) | Pure black outline, drawn by hand. Cloak, leather and plate use 7 to 12 colours; chain mail and robe about 100 to 125, and look noisy | 3 tiles per armour: plain, magic and artefact ([dc-item.txt](https://github.com/crawl/crawl/blob/HEAD/crawl-ref/source/rltiles/dc-item.txt)) | Plain to magic plate: the same outline, with 79 of about 552 pixels recoloured in red and gold trim. Artefact: gold leads and the outline changes by 34 pixels |
| Diablo 2 (game data) | 29 x 29 per inventory cell; every body armour fills 2 x 3 cells ([inventory.json](https://github.com/blizzhackers/d2data/blob/master/json/inventory.json); [armor.json](https://github.com/blizzhackers/d2data/blob/master/json/armor.json)) | Not measured | Colour of the item's name: white, blue, yellow, with set items green and uniques gold (summary, [Diablo Wiki](https://diablo2.diablowiki.net/Item_quality)) | All 15 body armour families reuse a single picture across their 3 tiers. Uniques often recolour the base: 234 of 433 carry a colour code ([uniqueitems.json](https://github.com/blizzhackers/d2data/blob/master/json/uniqueitems.json)) |
| Into the Breach (community mod files, measured) | 35 of 40 weapon pictures are 60 x 40; pilot portraits 61 x 61. Sizes in the shipped game unconfirmed ([mod pack](https://github.com/tos-x/ITB-LemonymousMods)) | Each weapon picture carries its own backdrop and fills its whole canvas | Not found | Not found |
| Dead Cells (summary) | Not found | Icon coloured by stat family: red, purple or green, split when tied, white when colourless ([Dead Cells Wiki](https://deadcells.wiki.gg/wiki/Weapon)) | Legendary items get a gold border; quality is a text suffix | Stat colour and suffix. Guides say to read the listed stats, since the icon alone misleads ([itechguides](https://www.itechguides.com/how-dead-cells-weapon-scaling-and-affixes-work/)) |
| Octopath Traveler (summary) | Not found | Not found | Not found | Likely a single icon per weapon type; players compare numbers on hover ([Game8](https://game8.co/games/Octopath-Traveler-2/archives/406341)) |

The sets that read well share 3 habits. Each holds a single pose per item type, so a row of icons scans and only the parts that matter change. Each wraps every object in a 1 px dark outline: black in Shattered Pixel Dungeon and Dungeon Crawl, 2 dark tinted colours in Fire Emblem. And the 2 clearest sets, Fire Emblem and Shattered Pixel Dungeon, draw at 16 px with 1 to 3 px of empty space round the shape.

Diablo 2 is the warning. Its normal, exceptional and elite versions of an armour share the same picture, so the player has to hover and read the name. Justin Ma of Into the Breach put the priority plainly: "we would sacrifice cool ideas for the sake of clarity every time" ([Game Developer](https://gamedeveloper.com/design/-i-into-the-breach-i-dev-on-ui-design-sacrifice-cool-ideas-for-the-sake-of-clarity-every-time-)).

### Painted roguelikes keep tier in a layer beside the art

| Game | Stored size | Readability rule | How tier shows | Owner or same-type rule |
|---|---|---|---|---|
| Slay the Spire 1 (modding docs, fan files) | 128 x 128 canvas, art in the centre 48 to 64 px ([BaseMod wiki](https://github.com/daviscook477/BaseMod/wiki/Custom-Relics)) | A single object with a soft grey halo on a transparent canvas | A rarity line under the name on the large popup | Every relic has unique art. Potions often share colour-swapped icons, which coverage of the sequel names as a weak spot (summary, [PCGamesN](https://www.pcgamesn.com/slay-the-spire-2/slay-the-spire-2-makes-keeping-track-of-its-new-relics-and-potions-much-easier)) |
| Slay the Spire 2 (decompiled code) | 256 x 256 large icons; small icons from an atlas | A thick dark outline kept as a separate layer ([NRelic.cs](https://github.com/Aper-mesa/Slay-the-Spire-2-Plus/blob/HEAD/src/MegaCrit.Sts2.Core.Nodes.Relics/NRelic.cs)) | Label colour and a hue-shifted frame on the inspect screen; no mark on the icon in the code read ([NInspectRelicScreen.cs](https://github.com/Aper-mesa/Slay-the-Spire-2-Plus/blob/HEAD/src/MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens/NInspectRelicScreen.cs)) | The owner's colour tints the outline at 66% alpha in the collection ([NRelicCollectionEntry.cs](https://github.com/Aper-mesa/Slay-the-Spire-2-Plus/blob/HEAD/src/MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection/NRelicCollectionEntry.cs)) |
| Darkest Dungeon 1 (fan and mod files, measured) | Trinkets 72 x 144; equipment 72 x 144 opaque tiles ([trinkets](https://github.com/WizardMeow/darkest-dungeon-trinkets/tree/HEAD/public/icons)) | Heavy black ink outline, muted fill | Equipment level by saturation: near grey at level 0, full colour at level 4 ([class mod](https://github.com/Ellisande/candlemaker-dd-mod/tree/HEAD/candlemaker/heroes/candlemaker/icons_equip)) | Separate weapon and armour art for each class |
| Darkest Dungeon 2 (summary) | Not found | Not found | Name prefix: Minor X, X, Greater X ([DD2 Wiki](https://darkestdungeon2.wiki.fextralife.com/Trinkets)) | Some trinkets are limited to a hero |
| Hades (game scripts) | Fan crops of 146 to 150 px; diamond shaped | Inked art, black plus 1 or 2 god colours | The frame is swapped per rarity, and a colour patch appears only above Common ([UIScripts.lua](https://github.com/AndrewZZJ/scripts_mods_of_hades/blob/HEAD/Scripts/UIScripts.lua); [UpgradeChoice.lua](https://github.com/AndrewZZJ/scripts_mods_of_hades/blob/HEAD/Scripts/UpgradeChoice.lua)) | God shown by palette and symbol; Duo boons get their own frame and colour |
| Backpack Battles (decompiled code) | 80 px grid cell; sprites stored at about 100 to 480 px ([Item.gd](https://github.com/cttt-des/AI-BackpackBattles/blob/HEAD/decompiled_full/Items/Item.gd)) | Thick dark brown outline and a single saturated accent | 6 rarities; a coloured light under the item in the shop and the rarity word in the tooltip ([ShopOffer.gd](https://github.com/cttt-des/AI-BackpackBattles/blob/HEAD/decompiled_full/Interface/ShopOffer.gd)) | Class badges in the tooltip ([ItemTooltip.gd](https://github.com/cttt-des/AI-BackpackBattles/blob/HEAD/decompiled_full/Interface/Tooltips/ItemTooltip.gd)) |
| The Bazaar (summary) | Not found | Not found | Bronze, Silver, Gold and Diamond, plus Legendary as its own category ([BazaarDB](https://www.bazaardb.gg/card/4px4c0rrqpq1nm8dqsabeg924/Quicksilver)) | Size shown by card width of 1, 2 or 3 slots ([Mobalytics](https://mobalytics.gg/?p=83976)) |
| Wildfrost (mod template) | 200 x 200 canvas, art 86 x 167 ([template](https://github.com/Hopeless403/Hopefuls-Wildfrost-Mods/tree/HEAD/WildfrostModMiya-master/CardPortraits)) | Thick dark outline, 2 tones and a highlight | Not found | Some charms are limited to card types ([Wildfrost Wiki](https://wildfrostwiki.com/Charms)) |
| Teamfight Tactics (summary) | Not found | Item art redrawn "to be more readable" ([Riot](https://teamfighttactics.leagueoflegends.com/en-gb/news/game-updates/items-regadgetized)) | Not found | Emblems share a single base and show the trait's icon, which the developers said "helps a lot with initial readability" ([AMA](https://devtrackers.gg/teamfight-tactics/p/aba28ea1-reckoning-ama-with-the-devs-and-friends)) |
| Super Auto Pets (summary) | Not found | Not found | Not found | The held perk's icon is drawn on the pet ([Super Auto Pets Wiki](https://superautopets.wiki.gg/wiki/Perk)) |

These games use painted art, at 2 to 10 times the size of Kindred Siege's icons, so their sizes stay out of scope. Their framing rules apply. Rarity almost never changes the item picture. It sits in a swapped frame (Hades), a light behind the item (Backpack Battles), a label (Slay the Spire 2) or a name prefix (Darkest Dungeon 2). The lowest tier often gets no mark at all, as with Hades' Common boons.

Owner and rarity travel on separate channels. Hades puts rarity in the frame and the god in the art's palette and symbol. Slay the Spire 2 tints the outline by owner. With 5 classes and 3 tiers, Kindred Siege can split the same way and avoid 15 colour combinations.

Colour alone fails for some players. Xbox Accessibility Guideline 103 says no essential information should be carried by a fixed colour alone ([Microsoft Learn](https://learn.microsoft.com/en-gb/gaming/accessibility/xbox-accessibility-guidelines/103)). A colour-blind Warframe player could pick out only the middle of 3 relic tiers (summary, [forum](https://forums.warframe.com/topic/992482-colour-blind-people-relics/)). So class or tier has to show by shape as well.

### What these rules mean for a 32 px Bloodridge icon

Use a 32 x 32 card icon, the size of the game's order icons, until Codex's request can be read. It gives twice Fire Emblem's linear room: a shape of about 26 to 28 px inside a 1 to 2 px empty border, enough for a silhouette and a small mark. If Codex asks for 48 px, the encounter icon size, rebuild from the same render at 40 to 44 px. Scaling a 32 px icon up by 1.5 would leave pixels of uneven size.

Draw the outline 1 px wide in the tribe's darkest brown, #120b08. The approved chief portraits used the same rule (HANDOFF_LOG.md line 9201), and it matches Fire Emblem's dark tinted outline. Keep 12 to 16 colours per icon, taken from a single gear palette picked from the Bloodridge family. Light every icon from the same side as the fighters' own baked light, which needs checking on an approved sheet. Fix a single pose per noun: every mail front-on as a torso, every cloak front-on hanging from the shoulders.

Give the class the silhouette and give the tier the trim. The Warden's mail should be the widest, most closed and squarest shape. The Skirmisher's mail should be lighter, open and cut away. Tier changes only colour depth or an accent, and the top tier may add a small shape change, as Dungeon Crawl's artefacts do. Princess gear carries a special effect (DESIGN_LOCKS.md line 283), so it earns its own mark. Leave Pauper unmarked.

**The test is simple: shrink every icon to 16 px in greyscale.** If the 2 mails still read apart, the set works. Tribe sets switch on with 2 or 3 pieces from a tribe, so the tribe should be the element all its pieces share: its palette and materials.

The game's own nouns raise 2 cautions. The piece names in code are placeholders (CampaignGear.cs line 219), so the art should carry class and bend without help from the name. And "eye charm" is the Hit noun, so the Deadeye cloak should avoid an eye motif.

## Gear on the body costs 240 frames per look

A full paper doll is out of reach for these fighters. Each class look has 240 frames: idle 40, attack 40, hit 16 and death 24, in 2 facings (game_context.md, from the game branch). A gear layer has to match every frame, keep the right draw order when an arm crosses the body, and stay inside a sheet that already uses about 32 colours.

### Shipped paper dolls removed either the animation or the hand drawing

The 2 shipped paper dolls on small sprites each cut the costly part. Battle Brothers layers head, helmet, armour and weapon on busts that do not animate. Its developers said fully animated figures "would have multiplied the workload immensely" ([Dev Blog 5](https://battlebrothersgame.com/?p=414), summary). Diablo II rendered almost all its art, armour components included, from 3D Studio Max, on characters 75 px tall, with 3 armour weights per body part and a colour shift per item ([postmortem](https://www.gamedeveloper.com/design/postmortem-blizzard-s-i-diablo-ii-i-); [colour shifts](https://gamespy-archives.quaddicted.com/sites/www.planetdiablo.com/modding/guides/colorshifts/index.html), summaries). A re-implementation shows 16 layer types and a draw order stored per direction and per frame ([OpenDiablo2 cof.go](https://github.com/OpenDiablo2/OpenDiablo2/blob/master/d2common/d2fileformats/d2cof/cof.go)).

Hand-made pixel games mostly chose cheaper routes. Shattered Pixel Dungeon redraws the whole hero per armour tier: 7 rows of 21 frames at 12 x 15 px, with an identical silhouette for tiers 1 to 5 ([HeroSprite.java](https://github.com/00-Evan/shattered-pixel-dungeon/blob/master/core/src/main/java/com/shatteredpixel/shatteredpixeldungeon/sprites/HeroSprite.java); [sheets](https://github.com/00-Evan/shattered-pixel-dungeon/tree/master/core/src/main/assets/sprites)). Wargroove paints each unit in a single palette with reserved colour ranges and swaps team colour at runtime ([Wargroove](https://wargroove.com/palette-swapping/), summary). Soulbound, a live pixel game, hand-drew each gear piece frame by frame over a base skeleton and says the system has to handle "thousands of combinations" ([Soulbound](https://soulbound.game/news/how-we-built-a-pixel-art-character-and-cosmetic-system/), summary).

Auto battlers tie item identity to a marker on the unit. Teamfight Tactics shows items with the health bar, and Riot kept fixing their contrast and sharpness at small sizes after launch ([Follow the Fix](https://followthefix.teamfighttactics.leagueoflegends.com/en-us/), summary). Super Auto Pets draws the held perk's icon on the pet. Darkest Dungeon players report that upgraded gear shows on the character sheet and leaves the hero unchanged ([Steam](https://steamcommunity.com/app/262060/discussions/0/490121928351931621)). Battle Brothers does draw gear on the body, yet at least 1 reviewer said its units "tend to blend in to one" (summary; the outlet among 4 reviews was not identified, [TheSixthAxis](https://www.thesixthaxis.com/?p=391300) is one).

### Generated frames make layers harder still

A third-party research note on PixelLab found its public tools return composited frames with no equipment layer, and it rated animated equipment layers "promising but high risk" ([pixellab-pip](https://github.com/Shilo/pixellab-pip/blob/main/docs/pixellab/pixellab-paperdolling-research-spike.md)). PixelLab's outfit transfer handles up to 3 frames per call at 81 to 128 px ([PixelLab docs](https://www.pixellab.ai/docs/tools/transfer-outfit-pro), summary), and Kindred Siege's cells are 136 and 180 px. The subscription is off until 6 Dec 2026 anyway. Local models drift from frame to frame, which breaks the pixel registration a layer depends on.

### What each approach costs

These counts are estimates built from the game's frame counts. No game studied published hours or money.

| Approach | New art per gear look | Touches the body | Works with generated frames |
|---|---|---|---|
| Full paper doll | 240 layer frames per body, 480 for a cloak, plus draw order per frame in attack and death | Yes | Poorly |
| Rigid piece on anchors (helmet, pauldron, hood) | 2 small sprites (1 per facing), 4 to 6 with turned variants; plus a 240-point anchor track per class body, made once | Yes | Partly. Breaks where the body turns (death, some hits) unless hidden |
| Tier body swap | 240 frames per extra look, per class, per tribe | Yes | Moderately. Motion and face drift between looks |
| Palette swap of a gear colour range | 1 palette row per look; an index map and gear mask per sheet, made once | Yes | For colour only. Shows tier, and leaves the piece itself unnamed |
| Badge at the health bar | 1 small icon per look, 8 to 16 px | No | Yes |
| Card icon plus trigger effect (current lock) | 1 card icon per piece; 1 effect per bend, about 10 bends | No | Yes. Nothing shows who wears what until a piece fires |

Across 12 looks per class (4 nouns x 3 tiers) and 5 classes, a full paper doll comes to 14,400 to 28,800 layer frames per tribe body. Stripped pieces can move between tribes, which multiplies that again. Enemies really wear up to 3 pieces (DESIGN_LOCKS.md line 434), so every body option must fit enemy bodies too. A badge shows on enemies at no extra cost and tells the player which enemy carries the piece a win can strip.

Body art also strains the palette. The Bloodridge blood reds and rust oranges are likely shared by mail, hair, skin shadow and blood. A colour index cannot tell those regions apart, so a palette swap needs a gear mask ([itch.io thread](https://itch.io/t/2629327/more-dynamic-palette-indexation), summary). Shattered Pixel Dungeon's rows rise from 21 colours with no armour to 38 with it, at a far smaller size. Whether a shader recolour counts as "drawn in code" under Jacob's rules is his call.

### The lock and the playtest complaint

The 20 Oct lock says: "Pieces are not drawn on the sprite. Each one shows as an effect in the fight and an icon on the fighter's card" (DESIGN_LOCKS.md line 416). **Every option above that touches the body needs Jacob to reopen that lock.** A badge at the health bar leaves the body alone, but the lock names only an effect and a card icon, so the badge needs his yes as well. The notes record a 4 Nov playtest complaint that players could not tell who wears what. The card-and-effect route leaves that open until a piece fires.

My call is to keep the body untouched and add a marker tied to the fighter, if Jacob agrees. If he wants gear on the body, a single rigid piece at the shoulder or head is the only body option with a bounded cost. Confidence: likely on the costs, a guess on how well each reads until it is mocked.

## Klein 4B or Qwen Edit plus a palette snap is the clean free route

The team's free kit is local ComfyUI 0.31.0 on an RTX 3060 12 GB with 32 GB of RAM (HANDOFF_LOG.md line 4038), Aseprite 1.3.18.6 (line 7275), and possibly PixelLab's 2 free workbenches. PixelLab's paid tools are off until 6 Dec 2026. Jacob's standing rules shape every choice: references first, no lookalike of a famous game, visuals generated and never drawn in code, free local mocks first, paid PixelLab only on his yes (line 6991).

### Licences decide which local models can make shipped art

| Model | Licence | Commercial game use |
|---|---|---|
| SDXL 1.0 | CreativeML Open RAIL++-M; the licensor claims no rights in outputs ([licence](https://github.com/Stability-AI/generative-models/blob/main/model_licenses/LICENSE-SDXL1.0)) | Yes, within its list of banned uses |
| FLUX.2 klein 4B and 4B Base | Apache 2.0; fits about 8 GB of VRAM ([flux2 README](https://github.com/black-forest-labs/flux2)) | Yes |
| Qwen-Image | Apache 2.0 ([Qwen-Image](https://github.com/QwenLM/Qwen-Image)) | Yes. The README does not name a licence for the Edit releases separately |
| FLUX.1 schnell | Apache 2.0 ([flux README](https://github.com/black-forest-labs/flux)) | Yes |
| Z-Image | Apache 2.0 per secondary sources only ([invideo](https://invideo.io/blog/z-image-ai-image-generator/)) | Probably; check its own card |
| FLUX.1 dev, Kontext, Redux, Krea, Fill; FLUX.2 dev; klein 9B | FLUX Non-Commercial licence. Outputs may be used "for any purpose", but using the model for "revenue-generating activity" falls outside it ([licence v2.0](https://github.com/black-forest-labs/flux2/blob/main/model_licenses/LICENSE-FLUX-DEV)) | Only with a paid BFL licence |
| Ideogram 4 open weights | Non-commercial model agreement, per the model pages ([model page](https://huggingface.co/ideogram-ai/ideogram-4-nf4), summary) | Disputed; check before shipping |

The approved chief portraits ran on local Ideogram 4 weights, `ideogram4_int8_convrot` (HANDOFF_LOG.md lines 8729, 9044, 9201). Users on the model's own page dispute whether locally made outputs can ship. One quotes a grant for non-commercial purposes only, and another cites an Ideogram FAQ giving full rights ([discussion](https://huggingface.co/ideogram-ai/ideogram-4-fp8/discussions/14), summary). Nobody in this research read the agreement itself. **Before those portraits ship, or before the route is reused for gear, someone should read Ideogram's agreement or swap the bust model for klein 4B or Qwen-Image.** Confidence: the non-commercial label is likely; what it means for outputs is unknown.

The FLUX [dev] licence text was read in full from GitHub. Its outputs clause sounds open, and readers disagree on what it allows ([HF discussion](https://huggingface.co/black-forest-labs/FLUX.1-Kontext-dev/discussions/6); [guide](https://localaimaster.com/blog/local-image-model-licenses-commercial-use), summaries). A shipped game is plainly revenue-generating. The safe reading keeps every [dev] model, and every LoRA built on one, out of shipped art.

### Pixel LoRAs give blocky art at the wrong size

No open model makes a clean 32 px icon by itself. Pixel LoRAs render blocky "fake pixels" at 512 to 1024 px, which then have to be snapped to a true grid and a palette.

| Model or service | Licence | Good for 32 px icons | Bad for 32 px icons |
|---|---|---|---|
| SDXL with nerijs pixel-art-xl ([card](https://huggingface.co/nerijs/pixel-art-xl), summary) | SDXL licence above; the LoRA is tagged CreativeML OpenRAIL-M, full text unread | Collapses cleanly by an 8x nearest downscale to 128 px. Worked locally with IP-Adapter on scenes Jacob picked (line 4170) | 128 px is 4 times the target. Made "mush" of 34 px characters locally (line 4343) |
| FLUX.2 klein 4B with the Limbicnation pixel LoRA ([card](https://huggingface.co/limbicnation/pixel-art-lora), summary) | Apache 2.0, checked locally by SHA256 (line 4344) | About 25 s per image at 512 px. Lands on a 64 px grid with 15 colours | Speckle and "meme bodybuilder proportions" on characters (line 4555). Halving 64 to 32 loses detail |
| FLUX.2 klein 4B edit, no LoRA ([README](https://github.com/black-forest-labs/flux2)) | Apache 2.0 | Edits from several references, up to 8 with a community node ([node](https://github.com/capitan01R/ComfyUI-Flux2Klein-Enhancer)) | Needs the full downscale chain |
| Qwen Image Edit 2511 ([README](https://github.com/QwenLM/Qwen-Image)) | Apache 2.0 for Qwen-Image | Keeps identity across edits. Already runs on this machine | Turned 2 of Tide's busts back into his standing sprite (line 9575) |
| Z-Image Turbo pixel LoRA ([Comfy](https://comfy.org/p/supported-models/pixel-art-style-z-image-turbo), summary) | Apache 2.0 per secondary sources | The default LoRA in ComfyUI's own template | One test found image-to-image fails on Turbo ([test](https://lilting.ch/en/articles/z-image-pixel-art-conversion), summary) |
| lizozom pixel-icon-lora-sdxl ([card](https://huggingface.co/lizozom/pixel-icon-lora-sdxl), summary) | Training data "derived from a third party's artwork" and unpublished | The only LoRA found trained on true 16 x 16 icons, 522 of them | Provenance risk. Do not use |
| Dever Flux game assets and UmeAiRT Flux pixel art ([Dever](https://civitai.com/models/945266/pixel-game-assets-flux-by-dever); [UmeAiRT](https://huggingface.co/UmeAiRT/FLUX.1-dev-LoRA-Modern_Pixel_art), summaries) | Built on FLUX.1 [dev], non-commercial | Tagged for game assets and icons | Need a BFL licence. UmeAiRT's author says it is based on games he enjoyed, a lookalike risk |
| Retro Diffusion website and API ([API examples](https://github.com/Retro-Diffusion/api-examples)) | Paid per image; its FAQ says you own outputs | Native icon and item styles from 12 to 256 px, a palette lock, up to 9 style references | Costs money; training consent unaudited; no independent icon review |
| Retro Diffusion Aseprite extension ([itch.io](https://astropulse.itch.io/retrodiffusion/purchase), summary) | $65 one-time | Runs locally on a 4 GB or larger NVIDIA card, so on the 3060 | Older models than the website; its future is unclear |

Retro Diffusion is the most purpose-built service found. It charges about $0.03 to $0.18 an image by tier, with $0.50 of starting credit ([pricing](https://retrodiffusion.ai/pricing), summary). That credit buys about 8 RD Plus images, enough to look at a few icons and short of a set. A third-party listing says commercial use needs a paid tier, which conflicts with the official FAQ ([aiindigo](https://aiindigo.com/tool/retro-diffusion), summary). It says it trained on licensed art from consenting pixel artists. An anonymous commenter disputes that, and nobody has audited it ([profile](https://medium.com/curiouserinstitute/ai-art-profiles-cody-claus-retro-diffusion-8cb9f45e939a); [comments](https://astropulse.itch.io/retrodiffusion/comments), summaries). Any use is a spending decision for Jacob, under the same rule as paid PixelLab.

From 6 Dec, PixelLab returns as a paid option on Jacob's yes. The game's 5 order icons were PixFlux jobs at 1 generation each, made on a palette strip of the game's colours (HANDOFF_LOG.md line 6103).

### Reference the game's own sprite without training on it

Style match comes from references. IP-Adapter on SDXL has a style transfer mode; its code is Apache 2.0 and its weights' licence is unverified ([IP-Adapter](https://github.com/tencent-ailab/IP-Adapter); [IPAdapter plus nodes](https://www.runcomfy.com/comfyui-nodes/ComfyUI_IPAdapter_plus), summary). Klein 4B edits and Qwen Image Edit 2511 both take the game's own pixels as a reference, and Qwen already corrected the chief busts against a sprite shown at 5x (line 9575).

Use a crop of real armour or cloth pixels, 64 to 96 px, upscaled 4x to 5x by nearest neighbour. Keep the whole fighter out of the reference, or the output tends to turn back into the fighter, as Tide's busts did.

Do not train a LoRA on the fighter sheets without PixelLab's written permission. They were made in PixelLab, and its terms forbid training your own model on its outputs without it ([PixelLab ToS](https://www.pixellab.ai/termsofservice), summary). A reference image is not training, so the routes here avoid that question.

### The cleanup chain does most of the work

Practitioners agree on an order: generate large on a flat background, cut out, find the true grid, downscale with a method that keeps features, lock the palette, harden the alpha, remove stray pixels, add the outline, then hand clean in Aseprite ([Summer Engine guide](https://docs.summerengine.com/skills/pixel-art), summary). Every tool below is free.

| Tool | Licence | Use it for | Watch for |
|---|---|---|---|
| Aseprite indexed mode and FX Outline | Owned | Final lock to the gear palette; a 1 px outline in a chosen colour ([FX docs](https://aseprite.org/docs/fx/), summary) | Matches colours by plain RGB distance ([forum](https://community.aseprite.org/t/octree-color-indexed-conversion-testing/9682)); cannot dither into a fixed multi-colour palette ([forum](https://community.aseprite.org/t/is-there-a-way-to-use-gradient-smoothly/12442)) |
| Pixel Snapper ([GitHub](https://github.com/Hugo-Dz/spritefusion-pixel-snapper)) | MIT | Finds the grid in fake pixel art; forces a fixed palette with `--palette`; runs folders as a batch | Keeps dither by design, noise included |
| unfake.js ([GitHub](https://github.com/jenissimo/unfake.js)) | MIT | Grid detection, including uneven grids; fixed palettes; fills holes, removes noise, hardens alpha | Untested at 32 px |
| PixelOE ([GitHub](https://github.com/KohakuBlueleaf/PixelOE)) | Apache 2.0 | Widens outlines and bright details before downscaling; contrast and k-centroid modes; ComfyUI nodes | No fixed palette, so pair it with a snap |
| K-Centroid for Aseprite ([GitHub](https://github.com/Astropulse/K-Centroid-Aseprite)) | Free script, licence not checked | A downscale that keeps each tile's dominant colour | |
| ComfyUI PixelArt Detector ([GitHub](https://github.com/dimtoneff/ComfyUI-PixelArt-Detector)) | MIT | Palette conversion inside ComfyUI | Its cleanup drops rare colours and leaves stray pixels |
| `aseprite_reduce.lua` | The team's own (line 4584) | Batch colour reduction, already in the kit | |
| `pixelart_workbench` lint and inspect | PixelLab, may need a subscription | `orphan_pixel` and `outline_gap` checks; proof every pixel is in the palette ([mcp.so mirror](https://mcp.so/servers/pixellab-mcp), summary) | Access unconfirmed (see below) |

Box downscaling is the step to change. The approved portrait route used a box downscale to 230 rows, probably a 4x to 7x reduction (lines 9044, 9201). A 32 px icon from a 1024 render is a 32x reduction. At that ratio, box averaging blends every thin strap and rivet into in-between colours, and the palette snap scatters them as speckle, the flaw Jacob rejected on local characters (line 4555). PixelOE's contrast mode or K-Centroid keep dark and bright details through the reduction ([PixelOE](https://github.com/KohakuBlueleaf/PixelOE)).

Turn dithering off. A dither pattern at 32 px reads as noise. Then check the snap by eye at 1x. The Bloodridge browns sit close together (#261611, #534a3f, #736d6b), so plain RGB matching can pull a warm brown onto a grey one. That risk is an inference from the method and has not been tested.

The portrait route fits icons with 2 changes: a feature-keeping downscale in place of box, and a real hand clean. The portraits were not hand cleaned. A 32 px icon has about 1,000 pixels, so the hand pass is probably minutes per icon, but no source gives a figure. Time the first 3. The snap, outline and checks are post-processing, as in the approved portraits. The "never drawn in code" rule does not cover a person cleaning by hand in Aseprite. Somebody still has to do that pass.

### PixelLab's free workbenches may be locked until 6 Dec

`pixelart_workbench` checks and fixes an existing drawing. `lint` flags defects such as orphan pixels and outline gaps, `inspect` returns a zoomed crop with exact colours, and its outlining returns a new drawing (summary, [mcp.so mirror](https://mcp.so/servers/pixellab-mcp)). `vfx_workbench` renders effects from presets and about 90 parts and spends no generations ([pixelkiln PR 259](https://github.com/gfargo/pixelkiln/pull/259)). Jacob's rule is that effects are drawn in it and never by an image model (line 9747), so it makes the in-fight effect for every direction below.

The same mirror says the workbench is "free for subscribers", and the subscription is off until 6 Dec. The team's own log points the same way: it calls the VFX workbench "free on his plan" (HANDOFF_LOG.md line 9747). Nothing found confirms whether it works without the plan. A single free `describe` call on each workbench from Jacob's MCP session settles it. Using its drawing commands to build an icon from scratch would break the "never drawn in code" rule. Lint, inspect and fixes on a generated icon are post-processing.

## Jacob's 3 options: card only, badge, or worn piece

Each direction answers the 3 questions differently: what the piece is, who wears it, and how 2 pieces differ. They differ in where gear shows and in how the icon is built. All 3 use only free local tools and the setup below, and each needs a trigger effect from `vfx_workbench` for the fight. The build routes can be swapped between directions; each pairing is my pick for the best fit.

| | A. A distinct object per piece | B. Shared base and bend emblem | C. A signature piece worn |
|---|---|---|---|
| What the piece is | Its own object; the noun shows in the pose | The noun shows in the shared base; the bend in a large emblem | The worn piece itself |
| Who wears it | Class shown by silhouette | Class glyph in a fixed corner; badge on the fighter in the fight | Seen on the fighter |
| How the 2 mails differ | Silhouette | Emblem and glyph only | A different piece on a different body |
| Tier | Colour depth; the top tier adds a shape | Frame weight: none, thin, ornate | Colour depth on the piece |
| In the fight | Trigger effect only | 16 px badge at the health bar, plus the effect | Piece on the body, plus the effect |
| 20 Oct lock | Fits as written | Body untouched; the badge needs Jacob's yes | **Needs Jacob to reopen the lock** |
| Build route | Reference edit on klein 4B or Qwen Edit 2511 | Pixel LoRA on SDXL with IP-Adapter, parts layered in Aseprite | Reference edit at fighter scale, placed in Aseprite |
| Cost to scale | 1 icon per piece | 1 base per noun per tribe, 1 emblem per bend, 5 glyphs, 3 frames | 2 to 6 small sprites per look, plus 240 anchors per class body |

### Setup shared by all 3

Build a gear palette of 16 to 24 colours from the Bloodridge family: the near-black browns (#120b08, #261611), blood reds (#541003 to #c23105), rust and fire oranges (#d63c03 to #fc9e18), golds (#fdc32c, #fed745), bone (#c6b7a6 to #ece2d4) and grey browns (#534a3f, #736d6b). Use it for every icon, the way Fire Emblem shares a single palette across its icon set. Reserve the golds for the Princess tier. The palette size and the gold reserve are my calls.

Every icon is 32 x 32, with the shape inside about 26 to 28 px, a 1 px outside outline in #120b08, and light from the fighters' own light side. Run the same graph on every output: BiRefNet cut-out, hardened alpha, crop and pad to square, PixelOE contrast mode or K-Centroid down to 28 px, a snap to the gear palette with dithering off, Aseprite Edit > FX > Outline, then a hand clean at 1x and 4x. Fix the seed, the reference crop and the prompt skeleton, and change only the noun and the motif. Generate 3 seeds per piece and keep the best.

Until Codex's request can be read, make all 3 pieces at Pauper, plus Bastion mail at Mid and Princess so Jacob sees the tier rule. Show each direction at 1x and 4x on the real gear card, next to a 16 px greyscale strip of the 2 mails. For B and C, add a 1440 x 900 battle capture with the Bloodridge Skirmisher, Warden and Marksman in place. Time each step. Put the 3 directions side by side and numbered, and name none of them after a game.

### Direction A: a distinct object per piece, shaped by class

Each piece is its own object. Class shows in the silhouette, the stat in the pose, the bend in a single motif, and the tier in colour depth. This follows the strongest rule in the study: a different silhouette per sub-type, with tier carried by trim, as in Fire Emblem's blade widths, Shattered Pixel Dungeon's armour outlines and Dungeon Crawl's trim.

Tier works by colour depth. Pauper uses only bone and grey browns. Mid adds the blood reds. Princess adds gold trim and a small shape change, such as a crest or an extra plate, made by a region edit and run through the same graph. Value changes with hue at each step, which should hold up better for colour-blind players than a hue step alone (an untested inference).

Bloodrush mail (Skirmisher). Reference: a crop of torso armour pixels from the approved Bloodridge Skirmisher sheet. Prompt: "a single short open mail shirt icon, one shoulder bare, ragged hem, blood red binding, in the same pixel style and materials as image 1, front view, plain grey background". Its silhouette should be the narrowest and most uneven of the 3. A streak of blood across the links carries the bend: strikes come faster under a health threshold.

Bastion mail (Warden). Reference: the Warden sheet's armour pixels. Prompt: "a single heavy closed mail coat icon, broad square shoulder plates, a round iron boss on the chest, rust and bone, in the same pixel style and materials as image 1, front view, plain grey background". Its silhouette should be the widest and most even. The boss carries the bend: a guarded blow takes less damage.

Deadeye cloak (Marksman). Reference: the Marksman sheet's cloth pixels. Prompt: "a single long narrow hooded cloak icon, one straight seam from hood to hem, a fletching clasp at the throat, in the same pixel style and materials as image 1, front view, plain grey background". Its tall, upright shape cannot be mistaken for either mail. The long seam carries the bend: a longer aim hits harder.

This direction fits the lock as written. In the fight each piece shows only as its trigger effect, so the playtest complaint stays open until a piece fires. The main build risk is style drift across 3 separate generations. The fixed seed, reference crop and graph guard against it.

### Direction B: a shared base with a bend emblem, also worn as a badge

Every mail in a tribe shares a single base picture, and so does every cloak. The bend shows as a large emblem, 12 to 14 px, on the chest. A 6 to 7 px class glyph sits in the same top-left corner of every icon. Tier is frame weight: no frame for Pauper, a 1 px bone frame for Mid, a 2 px frame with gold corners for Princess. In the fight, the emblem in its tier frame sits as a 16 px badge at the fighter's health bar.

The precedents are craft rules from several games. Teamfight Tactics builds emblems on a shared base with the trait's icon and shows items with the health bar. Hades puts rarity in the frame. Backpack Battles puts class badges in the tooltip, and Shattered Pixel Dungeon uses fixed corners for slot marks. Frame weight is a shape cue, so tier stays readable without colour.

Build the parts once and layer them. Generate 2 bases, a Bloodridge mail torso and a Bloodridge cloak, front view, on SDXL with pixel-art-xl and IP-Adapter style transfer from a crop of the approved sheets. Collapse each to the LoRA's 128 px grid with unfake.js or Pixel Snapper, then K-Centroid it to size. Generate the 3 emblems, the 3 class glyphs and a set of 3 frames the same way, as separate small icons on plain grey. Then build each piece in Aseprite from those generated layers and run the shared snap and outline.

Bloodrush mail: the mail base, a blood drop split by a slash as the emblem, the Skirmisher glyph. Bastion mail: the same mail base, a round shield boss as the emblem, the Warden glyph. Deadeye cloak: the cloak base, a drawn bowstring line or a narrow sighting slit as the emblem, the Marksman glyph.

The body stays untouched, so the core of the lock holds. The lock names only an effect and a card icon, so the badge needs Jacob's yes. B scales most cheaply: a base per noun per tribe, an emblem per bend (10 bends are listed), 5 glyphs and 3 frames. Badges also show on enemies, so the player sees which enemy carries each strippable piece.

The risk is real. The 2 mails share a picture, the pattern behind Diablo 2's hover-to-read problem, so the emblem has to carry the whole difference. The 16 px greyscale test decides this direction. A 32 px icon holding a base, an emblem, a glyph and a frame is also crowded. Pixel-art-xl made mush of 34 px characters here, and flat emblems are simpler shapes, but nobody has tested them.

### Direction C: a signature piece worn on the fighter

**This direction draws the piece on the sprite, so it needs Jacob to reopen the 20 Oct lock.** Each piece adds a single rigid part to the fighter at the shoulder or head, where a fixed sprite holds up best. The card icon is the same part drawn at 32 px, so card and fighter match. Tier shows in the part's own colour depth, as in A. It follows Jacob's own early references: gear that adds to a unit's look with no extra limbs (DESIGN_LOCKS.md line 88), and natives in light battle gear with a signature piece of identity (line 99).

Bloodrush mail: a light mail sleeve and shoulder guard on the near arm, bound in blood red. Bastion mail: a heavy square pauldron with a gorget plate, the widest shape on the body. Deadeye cloak: a short, stiff hood and mantle with no long tail. A full cloak would need its own cloth motion across 240 frames and a front and back split, the costliest case in the study.

To build the mock, crop the shoulder or head region of the approved sheet at 64 to 96 px, upscale it 4x to 5x, and add the part with a klein 4B or Qwen Edit 2511 edit. Cut the part out and take it through the shared graph down to fighter scale, roughly 10 to 20 px. Snap it to that sheet's own colours, so the sheet stays near 32. Make 1 sprite per facing, SW and SE. In Aseprite, place it on a new layer over the 40 idle frames of a facing per class, plus the attack's hit frame and 2 hit frames, and hide it during death. Make the 32 px card icon from the same render through the same graph.

A production set needs 2 to 6 small sprites per look plus a 240-point anchor track per class body, made once per tribe. Anchors would be placed by hand, or taken from PixelLab's skeleton keypoints after 6 Dec, which nobody has tested at 48 x 94. Attack frames where an arm crosses the part need a draw order or a hide. A 10 to 20 px part may not read on a busy 1x battle view, and its tier colours will read less, so the battle capture is the test.

## Most web evidence came from search summaries

GitHub was the only host reliably open in this session. The figures for Fire Emblem, Shattered Pixel Dungeon, Dungeon Crawl, the Into the Breach mod files and Diablo 2's data were measured from files there, as were the Slay the Spire 2, Hades and Backpack Battles code reads. Fan wikis, spriters-resource, Lospec, itch.io, Steam, Hugging Face, Civitai, pixellab.ai, retrodiffusion.ai, aseprite.org, mcp.so and the Battle Brothers, Wargroove and Soulbound sites were blocked.

Facts from blocked sites came from search summaries and should be treated as second-hand. That covers Dead Cells, Octopath, Diablo 2's name colours, Teamfight Tactics, Super Auto Pets, Battle Brothers, Wargroove, Soulbound, PixelLab's docs, Retro Diffusion's prices and claims, every Hugging Face model card and the Ideogram licence. The Into the Breach sizes come from mod files, and several roguelike sizes come from fan copies that may be rescaled.

Other gaps matter. No on-screen icon size was measured for any game. No route here has been tested on a 32 or 48 px icon, so the route ranking is reasoned. No source gives a cleanup time per icon. **Codex's request, in the HANDOFF_LOG entry headed "Damage attributed; five screen proposals reviewed", is not in the pushed HANDOFF_LOG.md on game-one or any of the 20 remote branches searched**, so the keys and sizes he wants for the 3 ART PENDING pieces are unknown.

These licence points should be checked, at the confidence the notes support.

| Item | What the notes found | How sure | Check |
|---|---|---|---|
| Ideogram 4 local weights, used for the approved chief portraits | Non-commercial agreement; rights to local outputs disputed | Label likely; meaning unknown | Read the agreement, or swap to klein 4B or Qwen-Image |
| FLUX [dev] family and LoRAs built on it | Model use for revenue falls outside the licence | Text read in full | Keep out of shipped art, or buy a BFL licence |
| Qwen Image Edit 2511 | Apache 2.0 for Qwen-Image; no separate line for Edit | Likely clean | Read the Edit model card |
| IP-Adapter SDXL weights | Code Apache 2.0; weights unverified | Unknown | Read the h94/IP-Adapter card |
| nerijs pixel-art-xl | Tagged CreativeML OpenRAIL-M; full text unread | Likely clean | Read the card |
| Z-Image | Apache 2.0 per secondary sources | Unconfirmed | Read its card |
| lizozom icon LoRA and game-style Civitai LoRAs | Undisclosed or game-derived training data | High risk | Do not use |
| Retro Diffusion | FAQ says you own outputs; a listing disagrees; consent claim unaudited | Vendor's word | Read its terms before any paid use |
| PixelLab | No training on its outputs without written permission; output ownership clause not found | Summary only | Read the full terms; ask before any LoRA on the fighter sheets |
| OpenGameArt CC0 icon packs, if used to train | Licences vary by pack ([OpenGameArt](https://opengameart.org/content/consolidated), summary) | Varies | Check each page |

## Conclusion

The art question turns on a design question: should the fight show who wears what before a piece fires? Direction A says no and keeps the lock as written. B and C say yes. Teamfight Tactics and Super Auto Pets, the auto battlers studied that show items mid-fight, both use a marker tied to the unit. The icon rules are settled by the evidence whichever way Jacob goes. The in-fight read is the taste call, and the 3 mocks exist so he can make it with pictures in front of him.

The 2 mails are the cheapest proof of any direction. If a Skirmisher mail and a Warden mail still read apart at 16 px in greyscale, the set will hold as it grows to 10 bends, 5 classes and 3 tiers. If they fail that test, more colour or a heavier frame will not rescue them. The Ideogram check matters beyond gear, because it touches chief portraits Jacob has already approved.
