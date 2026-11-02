import json
import sys
import time
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import regions as RG

G = RG.G

# barren_paint.py [camp numbers]
# Jacob, 27 Oct 2026, on the six camps: "when Sovereign wipes us out, we just take our shit and we go away. So at this
# point of time, the cam should actually look like a desolate piece of stretch of land ... barren mostly ... the first
# layer, should be coherent with their civilization ... You need to fix your prompts". Then "yes go make them" to the
# new rules: the prompt opens with who they are (DESIGN_LOCKS: a cult of cyborg flesh beings, his Type 4), the land is
# bare and natural, the only things on it are what the five carried, made from the leader's locked materials (pearly
# opalescent scaled shell, plum astral cloth, rose gold trims) and the Pals' own gear (dark metal, orange glyph seams),
# no joke props or signs, layer 2 stays animated. Same painter as before: Ideogram 4 local from a colour plan.
OUT = RG.REPO / "Art/Studies/Camp-Barren-20261027"
W, H = 1376, 768

WHO = ("The camp of the last five survivors of a cult of cyborg flesh beings, an intergalactic species from a Type 4 "
       "civilization: their leader and his four first officers, all heavily damaged, who fled with only what they "
       "could carry after the Sovereign destroyed their army in one second. They have stopped on a barren, desolate, "
       "empty stretch of bare natural land where nothing grows and nobody ever built anything. The only objects in "
       "the whole picture are a few small pieces of their own advanced alien technology that they carried with them, "
       "made of pearly opalescent shell with iridescent hexagonal scale cells and rose gold trims, deep plum astral "
       "cloth with a starfield inside it, and dark gunmetal with glowing orange glyph seams.")
STYLE = ("Rendered entirely in crisp, deliberately clustered pixel art. Modern high quality pixel art like Sea of Stars: "
         "bright, colourful and readable, never muddy.")
LAY = ("One game camp screen seen from a fixed side oblique camera like a classic arcade game stage, no people. The top "
       "part is sky over a distant horizon. Behind the camp the bare land stands in overlapping rows at different "
       "depths, hazier and paler the further away. The lower part is one wide open flat bare ground, calm and empty in "
       "the middle, with four small work stations of alien technology spread to its corners and nothing else on it.")
AES = ("a bright readable barren desolate land, vast and empty, with a few small pieces of advanced alien technology, "
       "coherent pixel clusters, a real place with depth and scale")
TECH = "made of their own alien technology"
STATIONS = [((200, 150, 660, 350), "the small forge of the Butcher for weapons and gear, " + TECH + ": a squat dark "
             "gunmetal heat core with glowing orange glyph seams and a glowing open mouth, capped with pearly opalescent "
             "shell, three finished curved alien blades standing upright in the ground beside it"),
            ((960, 150, 1376, 350), "the small corner of the Glutton for armour and body augments, " + TECH + ": a low "
             "stack of pearly opalescent armour plates with rose gold trims and a dark gunmetal augment cradle with "
             "glowing orange glyph seams"),
            ((60, 450, 470, 720), "the small floating shrine of the Shade to their old gods for war orders, " + TECH +
             ": their white three pronged sigil of pearly opalescent shell hovering above a low dark gunmetal disc, a "
             "few glowing orange glyph tablets floating around it"),
            ((930, 470, 1340, 750), "the pod of the Whisperer for skins and regalia, " + TECH + ": a tall pearly "
             "opalescent chrysalis pod standing half open, glowing violet inside, small violet crystal shards floating "
             "around it")]
FRAMEBOX = [(0, 640, 170, 768), (1220, 640, 1376, 768)]
CASES = "two sealed supply cases of their alien technology, dark gunmetal with glowing orange glyph seams"
# The 27 Oct six had all of these, from the old prompt or filled in by the painter for "camp".
RG.NEG = RG.NEG + (", wood, wooden furniture, planks, logs, campfire, fire pit, tents, tarps, canvas, sails, rope, "
                   "laundry, clothes, clothes racks, wardrobes, bookshelves, shelves, books, candles, banners, flags, "
                   "signs, text, letters, crates, barrels, chests, ladders, hammocks, benches, stools, boats, "
                   "telescopes, statues, ruins, buildings, walls, wrecks, crashed spaceship, skeletons, bones, trees, "
                   "palm trees, plants, grass, water, pools, oasis, medieval, fantasy village")
T7 = ["#ece4ee", "#a898b8", "#4a2350", "#a050e0", "#f2a24a", "#2e2c38", "#e0a070"]
LANDS = {
    "1": dict(name="SALT PAN", seed=2026102721,
              land="a vast flat salt pan, cracked white and empty all the way to the horizon",
              sky="a bright pale blue midday sky, almost white near the horizon",
              far="the endless flat white horizon of the salt pan, a thin line of low pale blue hills very far away",
              big=[(0, 175, 1376, 300), "a few low cracked ridges of raised salt crust far behind the camp, pale and hazy"],
              floor="a vast flat crust of white salt cracked into big polygon plates, bare and empty, calm and empty in the middle",
              light="Bright hard midday sun from the upper left, short cool blue shadows, as discrete flat pixel colour "
                    "planes, the ground in calm even light.",
              pal=["#5a9ae0", "#cfe4f6", "#9ab4d0", "#f4f2ec", "#dcd8d2", "#b8b6b6", "#8c98b4"] + T7,
              plan=dict(sky0=[90, 154, 224], sky1=[214, 232, 246], far=[176, 196, 216], floor0=[240, 238, 232],
                        floor1=[214, 210, 204], piece=[70, 62, 84]),
              rock="a raised slab of cracked salt crust"),
    "2": dict(name="ASH DUNES", seed=2026102722,
              land="a sea of pale ash dunes, soft and empty, under a dusk sky",
              sky="a huge dusk sky, deep violet above fading through pink to orange at the horizon",
              far="long rolling pale ash dunes far away, violet and hazy",
              big=[(0, 170, 1376, 310), "one long low dune crest of pale ash behind the camp, its wind blown edge "
                                        "catching the dusk light"],
              floor="flat packed pale warm grey ash with soft wind ripples, bare and empty, calm and empty in the middle",
              light="Warm dusk light from the upper left, long violet shadows, as discrete flat pixel colour planes, the "
                    "ground in calm even light, bright and readable.",
              pal=["#3a2a6a", "#e07a8a", "#f4a86a", "#8a7aa0", "#e6dace", "#c4b4ac", "#94849a"] + T7,
              plan=dict(sky0=[58, 42, 106], sky1=[244, 168, 106], far=[160, 140, 170], floor0=[226, 214, 202],
                        floor1=[196, 182, 176], piece=[70, 56, 86]),
              rock="a dark drift of ash with a sharp wind cut edge"),
    "3": dict(name="RED FLATS", seed=2026102723,
              land="a flat red stone desert, bare and endless, with low mesas on the horizon",
              sky="a hot late afternoon sky, deep blue above fading to warm gold at the horizon",
              far="a row of low flat topped red mesas far away on the horizon, pale and hazy",
              big=[(0, 180, 1376, 310), "a low wind worn shelf of red rock behind the camp"],
              floor="flat bare red sandstone with long dry cracks and a little drifted sand, calm and empty in the middle",
              light="Warm late afternoon sun from the upper left, long violet shadows, as discrete flat pixel colour "
                    "planes, the ground in calm even light.",
              pal=["#3d7fc6", "#f6d6a0", "#e8a070", "#e08a5a", "#c86a44", "#9a4a34", "#6a3a3a"] + T7,
              plan=dict(sky0=[60, 128, 200], sky1=[246, 214, 160], far=[226, 160, 120], floor0=[214, 132, 92],
                        floor1=[184, 104, 72], piece=[64, 50, 70]),
              rock="a dark red boulder"),
    "4": dict(name="DRY SEA", seed=2026102724,
              land="the bed of a long dead sea, pale cracked clay as flat as a table",
              sky="a soft dawn sky, pale lavender blue above fading to pink and cream at the horizon",
              far="a far line of tall pale cliffs, the old shore of the dead sea, pink and hazy",
              big=[(0, 160, 1376, 300), "the far line of tall pale cliffs of the old shore"],
              floor="flat pale cream clay of the dry sea bed cracked into big plates, bare and empty, calm and empty in "
                    "the middle",
              light="Soft pink dawn light from the upper left, cool lilac shadows, as discrete flat pixel colour planes, "
                    "the ground in calm even light.",
              pal=["#7a8ad0", "#f8d0c8", "#d8b8c0", "#f0e4d4", "#d6c6b6", "#ac9c90", "#8a7c94"] + T7,
              plan=dict(sky0=[130, 142, 210], sky1=[248, 214, 204], far=[214, 186, 192], floor0=[236, 226, 210],
                        floor1=[212, 198, 184], piece=[72, 60, 88]),
              rock="a dark cracked clay ledge"),
    "5": dict(name="HIGH PLATEAU", seed=2026102725,
              land="a bare windswept stone plateau high above a sea of white clouds",
              sky="a clear cold morning sky, deep blue above fading to pale blue at the horizon",
              far="a sea of white clouds far below the plateau with tall blue mountain peaks poking through it far away",
              big=[(0, 190, 1376, 310), "the sheer edge of the plateau behind the camp dropping away to the clouds"],
              floor="flat bare warm tan stone of the plateau, wind scoured and cracked, calm and empty in the middle",
              light="Clear cold morning sun from the upper left, cool blue shadows, as discrete flat pixel colour planes, "
                    "the ground in calm even light.",
              pal=["#2a6ab8", "#bfe0f4", "#ffffff", "#6a88b0", "#d4c4a4", "#b0a084", "#7c7480"] + T7,
              plan=dict(sky0=[42, 106, 184], sky1=[200, 228, 246], far=[236, 242, 248], floor0=[208, 192, 160],
                        floor1=[180, 164, 134], piece=[66, 58, 80]),
              rock="a dark broken stone edge"),
    "6": dict(name="CRATER", seed=2026102726,
              land="the floor of a vast shallow crater, pale ochre dust ringed by its low rim far away",
              sky="a warm afternoon sky, clear blue above fading to pale gold at the horizon",
              far="the low ring of the crater rim far away all round, ochre cliffs pale and hazy",
              big=[(0, 165, 1376, 300), "the low curved cliffs of the crater rim far behind the camp"],
              floor="flat pale ochre dust of the crater floor, bare and empty, calm and empty in the middle",
              light="Warm afternoon sun from the upper left, soft violet shadows, as discrete flat pixel colour planes, "
                    "the ground in calm even light.",
              pal=["#4a8ad0", "#f0dcb0", "#c09060", "#ecd09c", "#d0b07c", "#a8885c", "#80688a"] + T7,
              plan=dict(sky0=[74, 138, 208], sky1=[240, 220, 176], far=[200, 156, 110], floor0=[232, 206, 156],
                        floor1=[206, 178, 126], piece=[66, 54, 76]),
              rock="a dark ochre boulder"),
}


def bb(b):
    x0, y0, x1, y1 = b
    return [int(y0 / H * 1000), int(x0 / W * 1000), int(y1 / H * 1000), int(x1 / W * 1000)]


def prompt(o):
    els = [{"type": "obj", "bbox": bb((0, 0, W, 190)), "desc": o["sky"]},
           {"type": "obj", "bbox": bb((0, 150, W, 290)), "desc": o["far"]},
           {"type": "obj", "bbox": bb(o["big"][0]), "desc": o["big"][1]}]
    els += [{"type": "obj", "bbox": bb(b), "desc": d} for b, d in STATIONS]
    els += [{"type": "obj", "bbox": bb((0, 290, W, H)), "desc": o["floor"]},
            {"type": "obj", "bbox": bb(FRAMEBOX[0]), "desc": o["rock"] + " in the near foreground, in shadow"},
            {"type": "obj", "bbox": bb(FRAMEBOX[1]), "desc": CASES + " in the near foreground, in shadow"}]
    return json.dumps({"high_level_description": WHO + " " + STYLE + " No people anywhere, the officers are added "
                                                 "later. This land, " + o["land"] + ".",
                       "style_description": {"medium": "pixel_art", "art_style": RG.L3.ART, "aesthetics": AES,
                                             "lighting": o["light"], "color_palette": o["pal"]},
                       "compositional_deconstruction": {"background": LAY, "elements": els}})


def shrink(b, f):
    x0, y0, x1, y1 = b
    cx, cy, hw, hh = (x0 + x1) / 2, (y0 + y1) / 2, (x1 - x0) * f / 2, (y1 - y0) * f / 2
    return (cx - hw, cy - hh, cx + hw, cy + hh)


def plan(o):
    """The colour plan the painter starts from: sky, far band and floor, a low pale mass for the land's feature, small
    dark spots for the four stations and dark corners for the near frame. Small spots so the ground stays bare."""
    rng = np.random.default_rng(o["seed"])
    P = {a: np.array(v, float) for a, v in o["plan"].items()}
    a = np.zeros((H, W, 3))
    for y in range(H):
        if y < 200:
            a[y] = P["sky0"] + (P["sky1"] - P["sky0"]) * (y / 200)
        elif y < 290:
            a[y] = P["far"]
        else:
            a[y] = P["floor0"] + (P["floor1"] - P["floor0"]) * ((y - 290) / (H - 290))
    blobs = [(o["big"][0], 110, P["far"] * 0.8)] + [(shrink(b, 0.55), 150, P["piece"]) for b, _ in STATIONS]
    blobs += [((x0, y0, x1, y1 + 160), 200, P["piece"]) for x0, y0, x1, y1 in FRAMEBOX]
    for box, fill, col in blobs:
        m = Image.new("L", (W, H), 0)
        ImageDraw.Draw(m).ellipse(tuple(box), fill=fill)
        al = np.asarray(m.filter(ImageFilter.GaussianBlur(20))).astype(np.float64)[..., None] / 255
        a = a * (1 - al) + col * al
    small = rng.normal(0, 12, (H // 16 + 2, W // 16 + 2, 3)).astype(np.float32)
    a += np.dstack([np.asarray(Image.fromarray(small[..., c]).resize((W, H), Image.BICUBIC)) for c in range(3)])
    return Image.fromarray(np.clip(a, 0, 255).round().astype(np.uint8))


def run(tag, init, pos, seed, step=2):
    """RG.run with the queue jump: another session shares the GPU."""
    RG.INP.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    init.save(RG.INP / (tag + "-init.png"))
    g = {n: v for n, v in json.loads(RG.L3.BASE.read_text()).items() if n != "8"}
    g["4"]["inputs"]["text"] = pos
    g["5"]["inputs"]["text"] = RG.negative()
    g["10"]["inputs"].update(width=W, height=H)
    g["11"]["inputs"]["noise_seed"] = seed
    g["15"] = {"class_type": "LoadImage", "inputs": {"image": "ks-regions-20261024/" + tag + "-init.png"}}
    g["16"] = {"class_type": "VAEEncode", "inputs": {"pixels": ["15", 0], "vae": ["3", 0]}}
    g["18"] = {"class_type": "SplitSigmas", "inputs": {"sigmas": ["10", 0], "step": step}}
    g["12"]["inputs"]["sigmas"] = ["18", 1]
    g["12"]["inputs"]["latent_image"] = ["16", 0]
    g["14"]["inputs"]["filename_prefix"] = "KS-Camp-Barren-20261027/" + tag
    (OUT / (tag + "-workflow.json")).write_text(json.dumps(g, indent=1))
    t0 = time.time()
    out = G.wait_image(G.api("/prompt", {"prompt": g, "front": True})["prompt_id"]).convert("RGB")
    print(tag, out.size, "%.0fs" % (time.time() - t0), flush=True)
    return out


CA = RG.REPO / "Assets/Resources/CampaignArt"
FEET = {"butcher": (478, 400), "glutton": (1144, 396), "shade": (268, 706), "whisperer": (962, 766)}


def with_pals(k):
    v = Image.open(OUT / ("camp-%s.png" % k)).convert("RGBA")
    for nm, (fx, fy) in FEET.items():
        f = json.loads((CA / ("CampLoops/%s_camp.json" % nm)).read_text())["frames"][0]["frame"]
        pal = Image.open(CA / ("CampLoops/%s_camp.png" % nm)).convert("RGBA").crop(
            (f["x"], f["y"], f["x"] + f["w"], f["y"] + f["h"]))
        v.alpha_composite(pal, (fx - 32 - 96, fy - 66 - 192))
    v.convert("RGB").save(OUT / ("camp-%s-with-pals.png" % k))


def sheet():
    tw, th = 688, 384
    s = Image.new("RGB", (2 * tw + 8, 3 * (th + 20)), (18, 14, 20))
    d = ImageDraw.Draw(s)
    for i, k in enumerate(sorted(LANDS)):
        p = OUT / ("camp-%s-with-pals.png" % k)
        if not p.exists():
            continue
        x, y = (i % 2) * (tw + 8), (i // 2) * (th + 20)
        s.paste(Image.open(p).resize((tw, th), Image.BOX), (x, y + 18))
        d.text((x + 4, y + 3), "%s  %s" % (k, LANDS[k]["name"]), fill=(239, 223, 190))
    s.save(HERE / "look-barren-six.png")


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "camp-options.json").write_text(json.dumps({"who": WHO, "stations": STATIONS, "lands": LANDS}, indent=1))
    for k in (sys.argv[1:] or sorted(LANDS)):
        o = LANDS[k]
        init = plan(o)
        init.save(OUT / ("camp-%s-init.png" % k))
        raw = run("barren-%s" % k, init, prompt(o), o["seed"])
        raw.save(OUT / ("camp-%s-raw.png" % k))
        RG.clean(raw).save(OUT / ("camp-%s.png" % k))
        with_pals(k)
        sheet()
    print("all painted", flush=True)
