import json
import os
import shutil
import subprocess
import sys
import time
import urllib.request
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
OLD = Path("C:/Users/jacob/AppData/Local/Temp/claude/C--Users-jacob-Desk-42/6569067f-8aa7-490e-b59a-c16d4469b0b5/scratchpad")
sys.path.insert(0, str(OLD / "refine"))
sys.path.insert(0, str(OLD))
import grove3 as G
import looks3 as L3
import refine_assets as RA
import refine_view as V
import stops_grade as SG

# regions.py check | onepass ID | refine ID | layerize ID | split ID | bloodridge | roadmap
# Jacob, 24 Oct 2026: "you gotto do what you have to do bro just go make all the 6 regions with clear roadmap and such
# i love you man go". After the Everything bites strip he liked ("you drew beautiful bgs with my gpu") and the Layerize
# split ("kinda good"). Route: paint each region whole on his GPU (Ideogram 4 local, one pass at 2048x384 from a colour
# plan, set pieces placed by JSON boxes), refine at 4128x768 in four overlapping patches, cut colours, split into his
# three layers with fal's Layerize (one call a region), mark the stops. Bloodridge is the Everything bites strip as it is.
# A road today is 2 fight rows and the chief (CampaignTuning.PathFights is 2), so every region is 3 stops on 3 screens:
# stop 1 bare, stop 2 more built up, stop 3 the chief's court. Run order from DESIGN_LOCKS: camp, tier 1 (Bloodridge or
# Stoneveil), tier 2 (Skullpeak or Thundercoast), the hunt; the sand shark of the Broken Wastes waits beside the first
# road. Region looks come from the locks' tribes; stop names, signs and gags are placeholders for the vibe.
REPO = Path("C:/Users/jacob/KindredSiege-Clean")
OUT = REPO / "Art/Studies/Regions-Roadmap-20261024"
INP = Path("C:/Users/jacob/ComfyUI-Installs/ComfyUI/ComfyUI-ArtLab/input/ks-regions-20261024")
BITES = REPO / "Art/Studies/Bloodridge-WarTorn-Regions-20261023/onepass"
W, H, VW = 4128, 768, 1376
SW, SH = 2048, 384
INTENT = "you gotto do what you have to do bro just go make all the 6 regions with clear roadmap and such i love you man go"
STYLE = ("Rendered entirely in crisp, deliberately clustered pixel art. Modern high quality pixel art like Sea of Stars: "
         "bright, colourful and readable, never muddy. A war torn brutal planet where everything wants to kill everything, "
         "told with absurd deadpan humour.")
NEG = (", vector art, flat cartoon, smooth shapes, muddy brown, grunge, dull, desaturated, gore, tiny figures, characters, "
       "soldiers, warriors, units, sprites, people standing on the floor, square floor tiles, repeated buildings, "
       "duplicated landmarks, seams, panels, split screen, collage, borders, watermark, signature")
NEG_BASE = NEG  # NEG as written here: barren_paint.py adds the camp's bans to NEG when the camp helpers load
BOX = {"sky": (0, 0, W, 200), "far": (0, 110, W, 340), "back": (0, 280, W, 430),
       "p1": (380, 150, 1000, 470), "p2": (1650, 30, 2480, 480), "p3": (2950, 0, 3960, 480), "floor": (0, 410, W, H),
       "f0": (0, 520, 300, H), "f1": (1250, 640, 1500, H), "f2": (2630, 640, 2880, H), "f3": (3860, 470, W, H)}
ORDER = ["broken-wastes", "bloodridge", "stoneveil", "skullpeak", "thundercoast", "hunters"]

REGIONS = {
    "broken-wastes": {
        "name": "THE BROKEN WASTES", "tribe": "THE CLARTH", "seed": 2026102401,
        "where": "WHERE THE RUN STARTS. THE SAND SHARK WAITS BESIDE THE FIRST ROAD",
        "stops": ["THE OUTER DUNES", "THE SHARK GRAVEYARD", "THE CLARTH WAR CAMP"],
        "high": ("The Broken Wastes, a scorching desert of golden dunes where sand sharks hunt under the sand and the "
                 "Clarth war hunters make their camp."),
        "sky": "a hot clear deep blue sky, paler and golden near the horizon, a white sun high at the upper left",
        "far": ("a long line of golden dunes and red rock mesas on the horizon, and far away at the right end the "
                "colossal sun bleached jawbone of a giant sand shark arching over the dunes, the region's landmark"),
        "back": ("behind the arena: rippled dunes, half buried shark ribs, rusted wrecked siege machines of earlier "
                 "armies, sparse at the left and denser toward the war camp"),
        "p1": ("a wooden lifeguard tower standing alone in the desert with a skeleton lifeguard still sitting in it, a "
               "wooden sign that says NO SWIMMING, and a shark fin cutting through the sand beside it"),
        "p2": ("the giant skeleton of a sand shark rising out of the dunes, its huge ribcage arching high like a gate, "
               "its open jaw full of teeth"),
        "p3": ("the Clarth war camp at the right end: a towering gate made from a colossal shark jaw, red hide tents, "
               "tall bone totems with red war banners, bronze braziers burning, the chief's raised bone throne"),
        "f": ["a half buried rusted shield, spears and bleached bones", "a low pile of shark teeth and a broken drum",
              "a low rack of drying shark fins and a bundle of spears", "a red war banner on a bone pole and a bronze pot"],
        "floor": ("one calm, empty, cracked pale salt flat running the whole width as the arena floor, cool pale grey "
                  "with faint violet shadows, a worn trail along its back edge through all three places"),
        "light": ("Hard bright noon sun from the upper left, short cool violet shadows, as discrete flat pixel colour "
                  "planes, the salt flat arena in calm even light."),
        "pal": ["#3d8fd6", "#8cc8ee", "#f2e2b8", "#e8b45a", "#d0903c", "#a8682c", "#efe6cf", "#c9b994", "#8a4a2c",
                "#c43a2e", "#c08a3a", "#4a3a5c", "#d8d4dc", "#b4aec0"],
        "plan": {"sky0": (61, 143, 214), "sky1": (242, 226, 184), "far": (208, 144, 60), "back": (168, 104, 44),
                 "floor0": (216, 212, 220), "floor1": (180, 174, 192), "piece": (110, 72, 46)},
        "lz": ["the sky and the distant dunes, mesas and the giant shark jawbone on the horizon",
               ("the dunes, shark ribs, wrecked siege machines, the lifeguard tower, the sign, the shark fin, the giant "
                "shark skeleton, the war camp with its jaw gate, tents, totems, banners, braziers and throne"),
               "the flat pale salt flat arena floor"],
    },
    "bloodridge": {
        "name": "BLOODRIDGE", "tribe": "THE RAIDERS", "where": "TIER 1, ONE OF TWO",
        "stops": ["THE WILD OUTSKIRTS", "THE BURIAL GROUNDS", "THE CHIEF'S CRYPT"],
    },
    "stoneveil": {
        "name": "STONEVEIL", "tribe": "THE CAVE TRIBE", "seed": 2026102402, "where": "TIER 1, THE OTHER ONE",
        "stops": ["THE CAVE MOUTH", "THE BLOOD POOLS", "THE BLOOD PRIEST'S ALTAR"],
        "high": ("Stoneveil, a vast cave where a tribe of blood priests holds its rites, lit by glowing red crystals and "
                 "torches, where everything down here wants your blood."),
        "sky": "the high dark ceiling of a vast cavern, hung with long red crystal stalactites glowing softly",
        "far": ("the far wall of the vast cavern in deep violet rock with thin red waterfalls falling into the dark, and "
                "high on it at the right end a colossal red handprint painted on the rock, glowing faintly, the "
                "region's landmark"),
        "back": ("behind the arena: dark basalt pillars, clusters of red glowing crystals, bone altars, stalagmites with "
                 "little fanged mouths, chains hanging from the ceiling, sparse at the left and denser toward the altar"),
        "p1": ("the jagged cave mouth with pale daylight spilling in, a crooked wooden sign that says BLOOD BANK, and a "
               "leech the size of a dog asleep on a rock"),
        "p2": ("a wide pool of glowing red blood fed by a carved stone fountain shaped like a screaming face, giant "
               "leeches lounging at its edge like bathers"),
        "p3": ("the blood priest's altar at the right end: a towering stepped altar of black stone and bone crowned by a "
               "huge stone bowl overflowing with glowing red, red banners with a handprint sigil, torches, rows of skulls"),
        "f": ["red crystal shards and a dark rock", "a low cluster of fanged stalagmites",
              "a low pile of bones and a cracked bowl", "a dark stone pillar wrapped in chains"],
        "floor": ("one calm, empty, smooth mid grey blue stone cave floor running the whole width, lit evenly, a worn "
                  "path along its back edge through all three places"),
        "light": ("Warm torchlight and glowing red crystals from the upper left, cool teal bounce light, as discrete "
                  "flat pixel colour planes, the cave floor in calm even light, bright and readable, never murky."),
        "pal": ["#1c1426", "#3a2240", "#5a2a4a", "#8a2a3a", "#d23a4a", "#ff6a6a", "#f2a24a", "#ffd27a", "#2a6a6a",
                "#4ab0a0", "#7a6a8a", "#a89ab0", "#e8dcd0", "#8a94a8", "#6e7890"],
        "plan": {"sky0": (40, 24, 52), "sky1": (90, 42, 74), "far": (70, 40, 80), "back": (58, 34, 64),
                 "floor0": (138, 148, 168), "floor1": (110, 120, 144), "piece": (40, 26, 48)},
        "lz": ["the cavern ceiling with its red crystal stalactites, and the far cavern wall with the blood waterfalls "
               "and the giant red handprint",
               ("the pillars, crystals, bone altars, fanged stalagmites, chains, the cave mouth, the sign, the leeches, "
                "the blood pool and its fountain, the altar with its bowl, banners, torches and skulls"),
               "the flat grey blue stone cave floor"],
    },
    "skullpeak": {
        "name": "SKULLPEAK", "tribe": "THE STEEL BONED", "seed": 2026102403, "where": "TIER 2, ONE OF TWO",
        "stops": ["THE FROZEN FOOTHILLS", "THE SNIPER TERRACES", "THE WALLED HOLD"],
        "high": ("Skullpeak, a frozen mountain range of humans with polished steel bones grown through their bodies, "
                 "sharpshooters who wait behind walls, and every rock here aims at you."),
        "sky": "a crisp pale blue cold sky with thin high clouds",
        "far": ("jagged snowy mountains, and towering over them at the right end the Skullpeak itself: a colossal "
                "mountain peak shaped like a skull, polished steel bones showing through its snow and rock, turquoise "
                "light in its eye sockets, the region's landmark"),
        "back": ("behind the arena: snowy slate rocks with polished steel bone spikes growing out of them, turquoise "
                 "glowing seams in the rock, sniper nests of slate and steel, wine red cloth banners, sparse at the left "
                 "and denser toward the hold"),
        "p1": ("a snowman with a steel cyborg arm holding a rifle, beside a wooden sign riddled with bullet holes that "
               "says WELCOME"),
        "p2": ("a tall slate and steel watchtower full of sniper slits, a row of practice targets shaped like raiders "
               "in front of it, wine red banners"),
        "p3": ("the walled hold of the Skullpeak chief at the right end: thick layered walls of stone and steel, a great "
               "iron gate, turquoise glowing seams, wine red banners, rifle barrels poking from every slit"),
        "f": ["a snowy boulder with a steel spike", "a low pile of spent brass shells and a broken rifle in the snow",
              "a low stack of steel crates and a wine red flag in the snow", "a snowy slate pillar with steel bones"],
        "floor": ("one calm, empty floor of packed white snow over grey flagstones running the whole width, a worn "
                  "trail of trodden snow along its back edge through all three places"),
        "light": ("Clear cold morning light from the upper left, soft blue shadows on the snow, as discrete flat pixel "
                  "colour planes, the snowy floor in calm even light."),
        "pal": ["#6aa8e0", "#c4e2f6", "#f4f8fc", "#d8e4ee", "#9aaabc", "#4a5468", "#2e3444", "#b8c2cc", "#e8eef2",
                "#2ad0c0", "#8ef2e6", "#7a2a3a", "#a83a4a", "#8a96a6", "#6e7a8a"],
        "plan": {"sky0": (106, 168, 224), "sky1": (196, 226, 246), "far": (216, 228, 238), "back": (74, 84, 104),
                 "floor0": (232, 238, 244), "floor1": (200, 210, 222), "piece": (46, 52, 68)},
        "lz": ["the sky, the snowy mountains and the giant skull shaped peak",
               ("the snowy rocks, steel spikes, sniper nests, banners, the snowman, the sign, the watchtower and its "
                "targets, the walled hold with its gate"),
               "the flat packed snow arena floor"],
    },
    "thundercoast": {
        "name": "THUNDERCOAST", "tribe": "THE SEA TRIBE", "seed": 2026102404, "where": "TIER 2, THE OTHER ONE",
        "stops": ["THE STORM BEACH", "THE SHIPWRECK SHALLOWS", "THE TIDE GATE"],
        "high": ("Thundercoast, a stormy coast ruled by a sea tribe of fishmen who build machines under the water and "
                 "look down on everyone who breathes air, where everything in the sea is electric."),
        "sky": ("a dramatic storm sky of dark slate clouds with bright breaks of sunlight, one jagged lightning bolt "
                "striking far away"),
        "far": ("a churning teal sea with tall black sea stacks, and at the right end a colossal brass tide engine "
                "tower rising from the waves with lightning crackling around its top, the region's landmark"),
        "back": ("behind the arena: wet black rocks, coral, rusted brass pipes and valves, glass tanks of glowing "
                 "water, shipwreck timbers, kelp, sparse at the left and denser toward the tide gate"),
        "p1": ("a lifeguard chair on the storm beach with an electric eel coiled in its seat, a sign that says BEACH "
               "CLOSED FOREVER, and a lifebuoy with teeth"),
        "p2": ("the wreck of a ship broken in half on the rocks, giant brass pump pipes bursting through its hull, "
               "glowing jellyfish drifting around it"),
        "p3": ("the tide gate of the Thundercoast chief at the right end: a huge brass and stone sea gate with a giant "
               "tide clock dial, pipes, glass domes of glowing water, harpoon cannons, a sign that says NO AIR BREATHERS"),
        "f": ["a wet black rock with barnacles and kelp", "a low coil of rope and a dented brass diving helmet",
              "a low tangle of pipes and a broken glass tank", "a barnacled mooring post with chains"],
        "floor": ("one calm, empty floor of wet grey sand and flat stone slabs running the whole width, a few shallow "
                  "puddles reflecting the sky, a worn trail along its back edge through all three places"),
        "light": ("Bright sun breaking through storm clouds from the upper left, wet shine on rocks and brass, cool teal "
                  "shadows, as discrete flat pixel colour planes, the sand floor in calm even light."),
        "pal": ["#2a3a52", "#4a6a8a", "#9ac4d8", "#e8f8ff", "#1a6a7a", "#2a9aa8", "#7ad0d0", "#1e2630", "#3a4652",
                "#b87a3a", "#e0a84a", "#ff7a3a", "#4a8a6a", "#9aa6a2", "#c2c8bc"],
        "plan": {"sky0": (42, 58, 82), "sky1": (154, 196, 216), "far": (26, 106, 122), "back": (30, 38, 48),
                 "floor0": (180, 186, 176), "floor1": (150, 160, 156), "piece": (24, 30, 40)},
        "lz": ["the storm sky, the sea, the sea stacks and the giant brass tide engine tower",
               ("the rocks, coral, pipes, glass tanks, shipwreck timbers, kelp, the lifeguard chair and its eel, the "
                "signs, the lifebuoy, the broken ship, the jellyfish, the tide gate with its clock, domes and cannons"),
               "the flat wet sand and stone arena floor"],
    },
    "hunters": {
        "name": "THE HUNTERS' GROUNDS", "tribe": "THE HUNTERS", "seed": 2026102405, "where": "THE FINALE: THE HUNT",
        "stops": ["THE ASH FLATS", "THE BOUNTY BOARD", "THE HUNT LODGE"],
        "high": ("The hunters' grounds, the ash and obsidian land where the Sovereign's hunters keep the trophies of "
                 "every beast and every army they have caught."),
        "sky": "a pale lavender dusk sky with long violet clouds",
        "far": ("a long line of black obsidian cliffs streaked with glowing violet veins, and at the right end a "
                "colossal obsidian wall hung with the giant skulls of beasts from every land, the region's landmark"),
        "back": ("behind the arena: drifts of grey ash, black obsidian shards, iron bear traps, hanging rope nets, racks "
                 "of spears, trophy poles with beast skulls, sparse at the left and denser toward the lodge"),
        "p1": ("a wooden sign that says HUNTING SEASON ALWAYS OPEN, a field of iron bear traps half buried in the ash, "
               "a single snapped spear"),
        "p2": ("a big wooden bounty board covered in wanted posters, beside a towering rack of trophies: a sand shark "
               "jaw, a bear skull in a cage, antlers and horns"),
        "p3": ("the hunters' lodge at the right end: a huge hall of dark timber and obsidian with a roof of giant ribs, "
               "violet banners, nets and hooks hanging everywhere, cages, a great gong for the hunt"),
        "f": ["a black obsidian shard and a sprung bear trap", "a low pile of empty cages and rope",
              "a low heap of antlers and a broken net", "a tall trophy pole with a horned skull"],
        "floor": ("one calm, empty floor of flat pale grey ash and stone running the whole width, a worn trail along "
                  "its back edge through all three places"),
        "light": ("Soft violet dusk light from the upper left, deep blue shadows, violet glow from the obsidian veins, "
                  "as discrete flat pixel colour planes, the ash floor in calm even light, bright and readable."),
        "pal": ["#7a6aa8", "#b8a8d8", "#e8dcf0", "#3a3448", "#1e1a26", "#8a4ad0", "#c08af0", "#9a9a9e", "#c8c6c4",
                "#5a5660", "#e8e0d0", "#8a5a3a", "#d8b45a", "#a8a6b0", "#8a8896"],
        "plan": {"sky0": (122, 106, 168), "sky1": (232, 220, 240), "far": (58, 52, 72), "back": (40, 36, 52),
                 "floor0": (176, 174, 182), "floor1": (146, 144, 156), "piece": (30, 26, 38)},
        "lz": ["the dusk sky and the distant obsidian cliffs and the trophy wall with the giant skulls",
               ("the ash drifts, obsidian shards, bear traps, nets, spear racks, trophy poles, the sign, the bounty "
                "board, the trophy rack, the hunters' lodge with its banners, cages and gong"),
               "the flat pale ash and stone arena floor"],
    },
}


def bb(box):
    x0, y0, x1, y1 = box
    return [int(y0 / H * 1000), int(x0 / W * 1000), int(y1 / H * 1000), int(x1 / W * 1000)]


def prompt(R, refine=False):
    if refine:
        els = [{"type": "obj", "bbox": [0, 0, 300, 1000], "desc": R["sky"]},
               {"type": "obj", "bbox": [540, 0, 1000, 1000], "desc": R["floor"]}]
        high = R["high"] + " " + STYLE + " A section of one wide connected region panorama, refined with crisp pixel clusters."
        lay = ("A section of a continuous 2.5D battle stage panorama seen from a fixed side oblique camera, no people: "
               "sky above a distant horizon, scenery behind the arena in overlapping rows, a calm empty floor.")
    else:
        els = [{"type": "obj", "bbox": bb(BOX["sky"]), "desc": R["sky"]},
               {"type": "obj", "bbox": bb(BOX["far"]), "desc": R["far"]},
               {"type": "obj", "bbox": bb(BOX["back"]), "desc": R["back"]},
               {"type": "obj", "bbox": bb(BOX["p1"]), "desc": "the first place, the bare outskirts: " + R["p1"]},
               {"type": "obj", "bbox": bb(BOX["p2"]), "desc": "the second place, one big set piece: " + R["p2"]},
               {"type": "obj", "bbox": bb(BOX["p3"]), "desc": R["p3"] + ", the biggest and most built up place"},
               {"type": "obj", "bbox": bb(BOX["floor"]), "desc": R["floor"]}]
        els += [{"type": "obj", "bbox": bb(BOX["f%d" % i]), "desc": R["f"][i] + " in the near foreground, in shadow"} for i in range(4)]
        high = (R["high"] + " " + STYLE + " One whole region shown as one wide connected panorama, read left to right "
                "in three places: " + ", then ".join(s.lower() for s in R["stops"]) + ". Bare at the left, more and more "
                "built up toward the chief's place at the right end. Every part continues into the next with the same "
                "light, the same horizon and the same floor; nothing is repeated.")
        lay = ("One continuous 2.5D battle stage panorama seen from a fixed side oblique camera like a classic arcade "
               "game stage, no people. The top part is sky above a distant horizon with the region's landmark. Behind "
               "the arena the scenery stands in overlapping rows at different depths, with one big set piece for each "
               "of the three places and low framing pieces in the near foreground. The lower part is one flat calm "
               "arena floor running the whole width.")
    return json.dumps({"high_level_description": high,
                       "style_description": {"medium": "pixel_art", "art_style": L3.ART,
                                             "aesthetics": "bright readable war torn battle stage with absurd deadpan humour, coherent pixel clusters, a real place with depth",
                                             "lighting": R["light"], "color_palette": R["pal"]},
                       "compositional_deconstruction": {"background": lay, "elements": els}})


def negative():
    base = json.loads(L3.BASE.read_text())["5"]["inputs"]["text"]
    return base.replace(" creatures,", "").replace(" letters,", "") + NEG


def init_plan(R):
    """The region's colour plan at full size: sky, far band with a ridge line rising to the landmark at the right,
    back band, floor, soft dark blobs where the set pieces and frames go, smooth blotches (flat colour gives stripes,
    speckle gives dither, found on 23 Oct); then shrunk to the one pass size."""
    rng = np.random.default_rng(R["seed"])
    P = {k: np.array(v, float) for k, v in R["plan"].items()}
    a = np.zeros((H, W, 3))
    for y in range(H):
        if y < 210:
            a[y] = P["sky0"] + (P["sky1"] - P["sky0"]) * (y / 210)
        elif y < 330:
            a[y] = P["far"]
        elif y < 420:
            a[y] = P["back"]
        else:
            a[y] = P["floor0"] + (P["floor1"] - P["floor0"]) * ((y - 420) / (H - 420))
    xs = np.arange(W)
    kx = np.arange(0, W + 120, 120)
    prof = np.interp(xs, kx, 180 + 28 * rng.uniform(-1, 1, len(kx)))
    rise = np.clip((xs - 2900) / 500.0, 0, 1)
    prof = (prof - 110 * rise * rise * (3 - 2 * rise)).astype(int)
    for x in range(W):
        a[max(0, prof[x]):210, x] = P["far"]
    blob = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(blob)
    for k in ("p1", "p2", "p3"):
        d.ellipse(BOX[k], fill=170)
    for k in ("f0", "f1", "f2", "f3"):
        x0, y0, x1, y1 = BOX[k]
        d.ellipse((x0, y0, x1, y1 + 200), fill=200)
    al = np.asarray(blob.filter(ImageFilter.GaussianBlur(24))).astype(np.float64)[..., None] / 255
    a = a * (1 - al) + P["piece"] * al
    small = rng.normal(0, 12, (H // 16 + 2, W // 16 + 2, 3)).astype(np.float32)
    a += np.dstack([np.asarray(Image.fromarray(small[..., c]).resize((W, H), Image.BICUBIC)) for c in range(3)])
    return Image.fromarray(np.clip(a, 0, 255).round().astype(np.uint8)).resize((SW, SH), Image.LANCZOS)


def run(tag, init, pos, seed, step, size):
    INP.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    init.save(INP / (tag + "-init.png"))
    base = json.loads(L3.BASE.read_text())
    g = json.loads(json.dumps({n: v for n, v in base.items() if n != "8"}))
    g["4"]["inputs"]["text"] = pos
    g["5"]["inputs"]["text"] = negative()
    g["10"]["inputs"].update(width=size[0], height=size[1])
    g["11"]["inputs"]["noise_seed"] = seed
    g["15"] = {"class_type": "LoadImage", "inputs": {"image": "ks-regions-20261024/" + tag + "-init.png"}}
    g["16"] = {"class_type": "VAEEncode", "inputs": {"pixels": ["15", 0], "vae": ["3", 0]}}
    g["18"] = {"class_type": "SplitSigmas", "inputs": {"sigmas": ["10", 0], "step": step}}
    g["12"]["inputs"]["sigmas"] = ["18", 1]
    g["12"]["inputs"]["latent_image"] = ["16", 0]
    g["14"]["inputs"]["filename_prefix"] = "KS-Regions-20261024/" + tag
    (OUT / (tag + "-workflow.json")).write_text(json.dumps(g, indent=1))
    t0 = time.time()
    out = G.wait_image(G.api("/prompt", {"prompt": g})["prompt_id"]).convert("RGB")
    print(tag, out.size, "%.0fs" % (time.time() - t0), flush=True)
    return out


def onepass(rid):
    R = REGIONS[rid]
    init = init_plan(R)
    init.save(OUT / (rid + "-init.png"))
    out = run(rid + "-onepass", init, prompt(R), R["seed"], 2, (SW, SH))
    out.save(OUT / (rid + "-onepass.png"))
    pic = Image.new("RGB", (1376, 2 * 258 + 8), (18, 14, 20))
    pic.paste(init.resize((1376, 258), Image.BOX), (0, 0))
    pic.paste(out.resize((1376, 258), Image.BOX), (0, 266))
    pic.save(HERE / ("look-%s-onepass.png" % rid))


def clean(im, n=80, acc_n=10):
    """The colour cut, but bright high chroma accents (fire, crystals, glows) keep colours of their own; the despeckle
    skips them (a plain cut averages them away, found on the crypt braziers 23 Oct)."""
    a = np.asarray(im).astype(np.float64)
    lab = SG.srgb_to_lab(a)
    acc = (np.hypot(lab[..., 1], lab[..., 2]) > 55) & (lab[..., 0] > 45)
    q = im.quantize(colors=n, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    pal = np.array(q.getpalette()[:3 * n], np.int32).reshape(-1, 3)
    idx = np.asarray(q).astype(np.int64)
    if acc.sum() > 50:
        fq = Image.fromarray(a[acc].astype(np.uint8)[None]).quantize(colors=acc_n, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
        fpal = np.array(fq.getpalette()[:3 * acc_n], np.int32).reshape(-1, 3)
        idx[acc] = len(pal) + np.asarray(fq)[0].astype(np.int64)
        pal = np.vstack([pal, fpal])
    idx = RA.adopt(idx, ~acc, thresh=1, passes=2)
    return Image.fromarray(pal[idx].astype(np.uint8))


def refine(rid):
    R = REGIONS[rid]
    up = np.asarray(Image.open(OUT / (rid + "-onepass.png")).convert("RGB").resize((W, H), Image.LANCZOS)).astype(np.float64)
    acc, wsum = np.zeros((H, W, 3)), np.zeros((H, W))
    for i, x0 in enumerate((0, 917, 1835, 2752)):
        out = np.asarray(run("%s-refine-%d" % (rid, i), Image.fromarray(np.clip(up[:, x0:x0 + VW], 0, 255).round().astype(np.uint8)),
                             prompt(R, refine=True), R["seed"] + 10 + i, 9, (VW, H))).astype(np.float64)
        Image.fromarray(out.astype(np.uint8)).save(OUT / ("%s-refine-%d.png" % (rid, i)))
        wgt = np.ones(VW)
        if x0 > 0:
            wgt[:300] = np.linspace(0.01, 1, 300)
        if x0 + VW < W:
            wgt[-300:] = np.linspace(1, 0.01, 300)
        acc[:, x0:x0 + VW] += out * wgt[None, :, None]
        wsum[:, x0:x0 + VW] += wgt[None, :]
    raw = Image.fromarray(np.clip(acc / np.maximum(wsum, 1e-6)[..., None], 0, 255).round().astype(np.uint8))
    raw.save(OUT / (rid + "-raw.png"))
    c = clean(raw)
    c.save(OUT / ("region-%s.png" % rid))
    c.resize((W // 3, H // 3), Image.BOX).save(HERE / ("look-%s-third.png" % rid))
    print(rid, "colours", len(np.unique(np.asarray(c).reshape(-1, 3), axis=0)))


class Fal:
    URL = "https://mcp.fal.ai/mcp"

    def __init__(self):
        cfg = json.loads((Path(os.path.expanduser("~")) / ".claude.json").read_text(encoding="utf-8"))
        self.auth, self.sid, self.n = cfg["mcpServers"]["fal"]["headers"]["Authorization"], None, 0
        self.rpc("initialize", {"protocolVersion": "2025-06-18", "capabilities": {}, "clientInfo": {"name": "ks-regions", "version": "0"}})
        self.rpc("notifications/initialized", notify=True)

    def rpc(self, method, params=None, notify=False):
        self.n += 1
        msg = {"jsonrpc": "2.0", "method": method}
        if params is not None:
            msg["params"] = params
        if not notify:
            msg["id"] = self.n
        h = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream", "Authorization": self.auth}
        if self.sid:
            h["Mcp-Session-Id"] = self.sid
        with urllib.request.urlopen(urllib.request.Request(self.URL, data=json.dumps(msg).encode(), method="POST", headers=h), timeout=400) as r:
            self.sid = r.headers.get("Mcp-Session-Id") or self.sid
            body = r.read().decode("utf-8", "replace")
        if notify:
            return None
        for line in body.splitlines():
            if line.startswith("data:"):
                return json.loads(line[5:])
        return json.loads(body)

    def call(self, tool, **args):
        res = self.rpc("tools/call", {"name": tool, "arguments": {"user_intent": INTENT, **args}})
        if "error" in res:
            return {"error": res["error"]}
        txt = "".join(c.get("text", "") for c in res["result"].get("content", []) if c.get("type") == "text")
        try:
            return json.loads(txt)
        except Exception:
            return txt

    def upload(self, path):
        size = path.stat().st_size
        up = self.call("upload_file", prepare_upload=True, file_name=path.name, file_size=size)
        hdrs = []
        for k, v in (up.get("headers") or {"Content-Type": "image/png"}).items():
            hdrs += ["-H", "%s: %s" % (k, v)]
        p = subprocess.run(["C:/Windows/System32/curl.exe", "-s", "-S", "-o", "NUL", "-w", "%{http_code}", "-X", up.get("method", "PUT"),
                            *hdrs, "--data-binary", "@" + str(path), up["upload_url"]], capture_output=True, text=True, timeout=300)
        ver = self.call("verify_upload", file_url=up["file_url"], file_size=size)
        print("upload", p.stdout, ver.get("status") if isinstance(ver, dict) else ver, flush=True)
        return up["file_url"]

    def run(self, endpoint, inp):
        res = self.call("run_model", endpoint_id=endpoint, input=inp)
        rid = res.get("request_id") if isinstance(res, dict) else None
        for _ in range(60):
            if isinstance(res, dict) and (res.get("status") == "COMPLETED" or (res.get("result") or {}).get("layers")):
                break
            if not rid:
                break
            time.sleep(5)
            res = self.call("check_job", endpoint_id=endpoint, request_id=rid, wait_seconds=30)
        return res


def layerize(rid):
    R = REGIONS[rid]
    d = OUT / (rid + "-layerize")
    d.mkdir(parents=True, exist_ok=True)
    fal = Fal()
    url = fal.upload(OUT / ("region-%s.png" % rid))
    p = ("Separate exactly three layers. Layer 1: " + R["lz"][0] + ", the far background. Layer 2: everything standing "
         "behind and around the arena: " + R["lz"][1] + ", and the near foreground framing pieces at the bottom. "
         "Layer 3: " + R["lz"][2] + ".")
    t0 = time.time()
    res = fal.run("bytedance/seedream/v5/pro/layerize", {"image_url": url, "prompt": p, "image_size": "auto"})
    (d / "layerize-result.json").write_text(json.dumps(res, indent=1))
    layers = ((res.get("result") or {}).get("layers") or []) if isinstance(res, dict) else []
    print(rid, "layerize %.0fs" % (time.time() - t0), res.get("status") if isinstance(res, dict) else "?", "layers", len(layers))
    for i, L in enumerate(layers):
        q = subprocess.run(["C:/Windows/System32/curl.exe", "-s", "-S", "-L", "-o", str(d / ("layer-%02d.png" % i)), "-w", "%{http_code}",
                            L["image"]["url"]], capture_output=True, text=True, timeout=300)
        print(" ", i, q.stdout, (L.get("bounding_box") or {}).get("absolute"), L.get("name"))


def grow(m, r):
    p = np.pad(m, r)
    out = np.zeros_like(m)
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            out |= p[r + dy:r + dy + m.shape[0], r + dx:r + dx + m.shape[1]]
    return out


def split(rid):
    """The three layers keep every visible pixel of the region; Layerize decides which layer each pixel is in and
    gives only what is hidden (floor under props, the far land behind them), snapped to the region's own colours.
    Same rules as the Everything bites split (24 Oct, HANDOFF_LOG 10724)."""
    d = OUT / (rid + "-layerize")
    O = np.asarray(Image.open(OUT / ("region-%s.png" % rid)).convert("RGB"))
    res = json.loads((d / "layerize-result.json").read_text())["result"]["layers"]

    def placed(i):
        im = np.asarray(Image.open(d / ("layer-%02d.png" % i)).convert("RGBA"))
        bx = (res[i].get("bounding_box") or {}).get("absolute") or [0, 0, W, H]
        if im.shape[1] != bx[2] - bx[0] or im.shape[0] != bx[3] - bx[1]:
            im = np.asarray(Image.fromarray(im).resize((bx[2] - bx[0], bx[3] - bx[1]), Image.NEAREST))
        out = np.zeros((H, W, 4), np.uint8)
        out[bx[1]:bx[3], bx[0]:bx[2]] = im[:H - bx[1], :W - bx[0]]
        return out, bx
    far_l = world_l = floor_l = None
    far_box = None
    for i in range(1, len(res)):
        nm = (res[i].get("name") or "").lower()
        arr, bx = placed(i)
        if "floor" in nm:
            floor_l = arr if floor_l is None else np.maximum(floor_l, arr)
        elif any(k in nm for k in ("far", "sky", "distant", "background", "ceiling", "horizon")) and far_l is None:
            far_l, far_box = arr, bx
        else:
            world_l = arr if world_l is None else np.maximum(world_l, arr)
    wA, flA, fA = world_l[..., 3] >= 128, floor_l[..., 3] >= 128, far_l[..., 3] >= 128
    far_bot = int(far_box[3])
    rows = np.arange(H)[:, None]
    ftop = np.where(flA.any(0), flA.argmax(0), H)[None, :]
    top = O[:max(8, far_bot // 4)].reshape(-1, 3)
    cols, cnt = np.unique(top, axis=0, return_counts=True)
    skyset = cols[cnt >= 0.002 * len(top)].astype(np.int64)
    key = O[..., 0].astype(np.int64) * 65536 + O[..., 1].astype(np.int64) * 256 + O[..., 2].astype(np.int64)
    skyO = np.isin(key, skyset[:, 0] * 65536 + skyset[:, 1] * 256 + skyset[:, 2])
    # things standing on the floor that Layerize left out of the world (Skullpeak's corner rocks, 24 Oct): where the
    # region differs strongly from Layerize's rebuilt floor, in solid blobs, they go to the world
    dE = np.linalg.norm(SG.srgb_to_lab(O.astype(np.float64)) - SG.srgb_to_lab(floor_l[..., :3].astype(np.float64)), axis=2)
    obj = flA & ~wA & (dE > 28)
    dens = np.asarray(Image.fromarray((obj * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(6))) > 128
    obj = grow(dens & obj, 2) & flA & ~wA
    print(rid, "pieces on the floor moved to the world:", int(obj.sum()))
    wA = wA | obj
    left = ~wA & ~flA
    visF = left & (rows < far_bot) & (rows < ftop)      # far boxes that reach into the floor would give it floor bits
    fringe = visF & ~skyO & grow(wA, 2)
    visF &= ~fringe
    to_floor = left & (rows >= ftop)
    to_world = (left & (rows >= far_bot) & (rows < ftop)) | fringe
    worldM, floorM = wA | to_world, flA | to_floor
    pal, inv = np.unique(O.reshape(-1, 3), axis=0, return_inverse=True)
    pal = pal.astype(np.int32)

    def snap(rgb, region):
        idx = inv.reshape(H, W).copy()
        px = rgb[region].astype(np.int32)
        best = np.zeros(len(px), np.int64)
        for i in range(0, len(px), 200000):
            best[i:i + 200000] = ((px[i:i + 200000, None, :] - pal[None]) ** 2).sum(2).argmin(1)
        idx[region] = best
        idx = RA.adopt(idx, region, thresh=1, passes=2)
        return pal[idx].astype(np.uint8)
    hidF = ~visF & fA & (rows < far_bot)
    far_rgb = O.copy()
    far_rgb[hidF] = snap(far_l[..., :3], hidF)[hidF]
    xs = np.arange(W)
    for y in range(far_bot):
        v = visF[y]
        if not v.any():
            continue
        li = np.maximum.accumulate(np.where(v, xs, -1))
        ri = np.minimum.accumulate(np.where(v, xs, W)[::-1])[::-1]
        hole = ~v
        has = hole & (li >= 0) & (ri < W)
        both = np.zeros(W, bool)
        both[has] = skyO[y, li[has]] & skyO[y, ri[has]]
        far_rgb[y, both] = O[y, li[both]]
        rest = hole & ~fA[y] & ~both
        near = np.where(li >= 0, li, ri)
        ok = rest & (near >= 0) & (near < W)
        far_rgb[y, ok] = O[y, near[ok]]
    far_rgb[far_bot:] = np.median(far_rgb[far_bot - 1], 0).round().astype(np.uint8)
    far = np.dstack([far_rgb, np.full((H, W), 255, np.uint8)])
    hid_floor = flA & wA
    floor_rgb = O.copy()
    floor_rgb[hid_floor] = snap(floor_l[..., :3], hid_floor)[hid_floor]
    floor = np.dstack([np.where(floorM[..., None], floor_rgb, 0), np.where(floorM, 255, 0)]).astype(np.uint8)
    world = np.dstack([np.where(worldM[..., None], O, 0), np.where(worldM, 255, 0)]).astype(np.uint8)
    save_layers(rid, far, world, floor, O)


def save_layers(rid, far, world, floor, O):
    for nm, arr in (("l1-far", far), ("l2-world", world), ("l3-floor", floor)):
        Image.fromarray(arr).save(OUT / ("region-%s-%s.png" % (rid, nm)))
    st = Image.fromarray(far)
    st.alpha_composite(Image.fromarray(floor))
    st.alpha_composite(Image.fromarray(world))
    print(rid, "restacked vs the region, pixels that differ:", int((np.asarray(st.convert("RGB")) != O).any(2).sum()))
    roadmap_2x(rid, O)


def roadmap_2x(rid, O):
    R = REGIONS[rid]
    big = Image.fromarray(O).resize((W * 2, H * 2), Image.NEAREST).convert("RGBA")
    bar = 92
    pic = Image.new("RGBA", (W * 2, H * 2 + bar), (18, 14, 20, 255))
    pic.alpha_composite(big, (0, bar))
    d = ImageDraw.Draw(pic)
    V.label(pic, "%s (%s): %s" % (R["name"], R["tribe"], R["where"]), (12, 8), 2)
    y = 66
    d.line((40, y, W * 2 - 40, y), fill=(214, 170, 90, 255), width=4)
    for i, nm in enumerate(R["stops"]):
        cx = (i * VW + VW // 2) * 2
        d.ellipse((cx - 11, y - 11, cx + 11, y + 11), fill=(214, 170, 90, 255))
        V.label(pic, "STOP %d: %s%s" % (i + 1, nm, ", THE CHIEF" if i == 2 else ""), (cx + 22, y - 12), 2)
        if i:
            d.line((i * VW * 2, 40, i * VW * 2, bar), fill=(214, 170, 90, 255), width=3)
    pic.convert("RGB").save(OUT / ("region-%s-2x.png" % rid))


def bloodridge():
    O = np.asarray(Image.open(BITES / "region-2.png").convert("RGB"))
    Image.fromarray(O).save(OUT / "region-bloodridge.png")
    lay = [np.asarray(Image.open(BITES / "layerize" / ("region-2-%s.png" % n)).convert("RGBA")) for n in ("l1-far", "l2-world", "l3-floor")]
    save_layers("bloodridge", *lay, O)


def roadmap():
    """The world roadmap: every region in run order at a third of its size, its stops on a road under it; and every
    region's three layers apart."""
    ROW = H // 3
    groups = [("START", ["broken-wastes"]), ("TIER 1: A RUN MEETS ONE OF THESE TWO", ["bloodridge", "stoneveil"]),
              ("TIER 2: A RUN MEETS ONE OF THESE TWO", ["skullpeak", "thundercoast"]), ("THE FINALE", ["hunters"])]
    blocks = []
    for gname, ids in groups:
        g = Image.new("RGBA", (W // 3, 30), (18, 14, 20, 255))
        V.label(g, gname, (8, 6), 2)
        blocks.append(g)
        for rid in ids:
            R = REGIONS[rid]
            b = Image.new("RGBA", (W // 3, 22 + ROW + 34), (30, 25, 33, 255))
            V.label(b, "%s (%s): %s" % (R["name"], R["tribe"], R["where"]), (6, 4), 1)
            b.alpha_composite(Image.open(OUT / ("region-%s.png" % rid)).convert("RGBA").resize((W // 3, ROW), Image.BOX), (0, 22))
            d = ImageDraw.Draw(b)
            y = 22 + ROW + 12
            d.line((10, y, W // 3 - 10, y), fill=(214, 170, 90, 255), width=2)
            for i, nm in enumerate(R["stops"]):
                cx = (i * VW + VW // 2) // 3
                d.ellipse((cx - 5, y - 5, cx + 5, y + 5), fill=(214, 170, 90, 255))
                V.label(b, "%d %s%s" % (i + 1, nm, " (CHIEF)" if i == 2 else ""), (cx + 9, y + 3), 1)
            blocks.append(b)
    pic = Image.new("RGBA", (W // 3, sum(b.height + 6 for b in blocks)), (18, 14, 20, 255))
    y = 0
    for b in blocks:
        pic.alpha_composite(b, (0, y))
        y += b.height + 6
    pic.convert("RGB").save(OUT / "regions-roadmap.png")
    chk = np.indices((H, W)).sum(0) // 16 % 2
    chk = Image.fromarray(np.dstack([np.where(chk, 60, 40)] * 3 + [np.full((H, W), 255)]).astype(np.uint8))
    tw, th = W // 7, H // 7
    sheet = Image.new("RGBA", (3 * tw + 2 * 8, len(ORDER) * (th + 18)), (18, 14, 20, 255))
    for r, rid in enumerate(ORDER):
        for c, nm in enumerate(("l1-far", "l2-world", "l3-floor")):
            t = chk.copy()
            t.alpha_composite(Image.open(OUT / ("region-%s-%s.png" % (rid, nm))).convert("RGBA"))
            sheet.alpha_composite(t.resize((tw, th), Image.BOX), (c * (tw + 8), r * (th + 18) + 16))
        V.label(sheet, "%s: 1 FAR, 2 WORLD, 3 FLOOR" % REGIONS[rid]["name"], (4, r * (th + 18) + 2), 1)
    sheet.convert("RGB").save(OUT / "regions-layers.png")
    print("roadmap", pic.size, "layers", sheet.size)


if __name__ == "__main__":
    what = sys.argv[1]
    OUT.mkdir(parents=True, exist_ok=True)
    if what == "check":
        for rid in ORDER:
            if rid == "bloodridge":
                continue
            p = prompt(REGIONS[rid])
            json.loads(p)
            init_plan(REGIONS[rid]).resize((1024, 192), Image.BOX).save(HERE / ("look-init-%s.png" % rid))
            print(rid, "prompt chars", len(p), "refine chars", len(prompt(REGIONS[rid], refine=True)))
        print("negative:", negative()[:160])
    elif what == "onepass":
        for rid in sys.argv[2:]:
            onepass(rid)
    elif what == "refine":
        for rid in sys.argv[2:]:
            refine(rid)
    elif what == "layerize":
        for rid in sys.argv[2:]:
            layerize(rid)
    elif what == "split":
        for rid in sys.argv[2:]:
            split(rid)
    elif what == "bloodridge":
        bloodridge()
    elif what == "roadmap":
        roadmap()
