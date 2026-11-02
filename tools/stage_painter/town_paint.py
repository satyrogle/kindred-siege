import json
import sys
import time
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import regions as RG

G = RG.G

# town_paint.py [L] [R] [S]
# Jacob, 28 Oct 2026: the two pale flat topped hills left and right of the gap behind camp 5 get the two tribes'
# settlements, painted on his GPU (Ideogram 4 local, a 2x crop of each hill from a colour plan drawn here). The
# painter only draws the buildings. Everything that moves (fighters, ships, flags, fires, smoke, bursts) is layer 2,
# in town_anim.py, which also reads the plans below for where things stand. S paints a sheet of six small flying
# ships, three a tribe. Measured 28 Oct: hill tops at y 195; sand from y 300 at the left end, from y 250 by the gap
# (a cliff face under it), from y 268 right of the gap, from y 290 at the right end. The Butcher and his forge
# (x 238 to 510) and the Glutton and her plates (x 955 to 1210) stand in front, so nothing tall is planned there.
REPO = RG.REPO
C5 = REPO / "Art/Studies/Camp-Barren-20261027/camp5"
OUT = REPO / "Art/Studies/Camp-War-20261028"
W, H = 1376, 768
CROPS = {"L": (0, 64, 704, 336), "R": (672, 64, 1376, 336)}
SEEDS = {"L": 2026102801, "R": 2026102802, "S": 2026102803}
HAZE = np.array([214.0, 226, 238])
STYLE = ("Rendered entirely in crisp, deliberately clustered pixel art. Modern high quality pixel art like Sea of Stars: "
         "bright, colourful and readable, never muddy.")
LIGHT = "Clear cold morning sun from the upper left, cool blue shadows, as discrete flat pixel colour planes."
PAL = ["#2a6ab8", "#bfe0f4", "#ffffff", "#6a88b0", "#d4c4a4", "#b0a084", "#7c7480", "#ece4ee", "#a898b8"]
FAR = ("Everything on the hill is small with distance, seen from far across the gap, with clear readable silhouettes "
       "against the white clouds behind it.")

# The two settlement plans, canvas pixels. Tents are (x, bottom, width, height); poles (x, bottom, top); obelisks
# (x, bottom, height). fires and fight are for town_anim: where the flames and smoke go, and the x span and feet y
# span of each clash by the gap.
CLARTH = dict(
    hall=(36, 166, 204, 266), tower=(524, 118, 574, 250),
    tents=[(20, 296, 40, 34), (72, 298, 36, 30), (124, 290, 32, 28), (170, 298, 30, 26), (28, 228, 24, 20),
           (218, 236, 26, 22), (454, 232, 22, 18), (508, 246, 30, 24), (590, 240, 22, 18), (112, 236, 20, 16)],
    poles=[(62, 296, 238), (206, 242, 186), (472, 244, 192), (598, 238, 190)],
    fence=(0, 600), fires=[(516, 240), (598, 236), (86, 232)], fight=((470, 600), (212, 246)),
)
BLOOD = dict(
    catapults=[(852, 262), (900, 230)], obelisks=[(930, 240, 84), (1216, 236, 70), (1356, 240, 94)],
    pylon=(1228, 192, 1306, 288), pyramid=[(1300, 284), (1348, 150), (1396, 284)],
    tents=[(846, 264, 36, 24), (890, 258, 30, 20), (964, 228, 26, 18), (1014, 224, 28, 20), (1072, 232, 24, 16),
           (1132, 226, 28, 18), (1186, 234, 24, 16), (1214, 290, 36, 26), (1372, 262, 30, 22)],
    poles=[(914, 262, 212), (1208, 292, 240), (1338, 262, 208)], braziers=[(878, 254), (1322, 268)],
    fires=[(866, 256), (1232, 284)], fight=((826, 952), (212, 262)),
)


def mix(c, k=0.15):
    return tuple(int(v) for v in np.array(c, float) * (1 - k) + HAZE * k)


def bb(box, crop):
    x0, y0, x1, y1 = box
    cx0, cy0, cx1, cy1 = crop
    f = lambda v, a, n: int(np.clip((v - a) / n * 1000, 0, 1000))
    return [f(y0, cy0, cy1 - cy0), f(x0, cx0, cx1 - cx0), f(y1, cy0, cy1 - cy0), f(x1, cx0, cx1 - cx0)]


def base_canvas():
    """camp-5-clean with the two stations wiped off the hills: inside each box every row takes the colour of the
    column just outside it (cloud, hill top, face and sand all run sideways there). The stations come back in
    front from layer 3, so whatever the painter puts there is covered."""
    a = np.asarray(Image.open(C5 / "camp-5-clean.png").convert("RGB")).astype(np.float64).copy()
    for (x0, y0, x1, y1), src in (((236, 150, 428, 340), 234), ((952, 240, 1196, 340), 1198)):
        a[y0:y1, x0:x1] = a[y0:y1, src:src + 1]
    return a


def tent(d, cx, by, w, h, col, pole):
    d.polygon([(cx - w // 2, by), (cx, by - h), (cx + w // 2, by)], fill=col)
    d.line([(cx, by - h - 4), (cx, by - h + 3)], fill=pole, width=1)


def plan_side(side):
    im = Image.fromarray(base_canvas().clip(0, 255).astype(np.uint8))
    d = ImageDraw.Draw(im)
    if side == "L":
        P = CLARTH
        bone, bone_d, red, red_d, dark = mix((226, 214, 186)), mix((170, 154, 136)), mix((176, 58, 48)), \
            mix((118, 38, 36)), mix((62, 42, 46))
        for x in range(4, P["fence"][1], 8):
            if not 236 <= x <= 512:
                d.polygon([(x - 2, 200), (x, 191 + (x // 8) % 3), (x + 2, 200)], fill=bone_d)
        for i, (cx, by, w, h) in enumerate(P["tents"]):
            tent(d, cx, by, w, h, red if i % 2 else red_d, bone)
        x0, y0, x1, y1 = P["hall"]
        d.ellipse((x0, y0, x1, y1 - 8), fill=bone)
        d.ellipse((x0 + 38, y0 + 36, x0 + 66, y0 + 60), fill=dark)
        d.ellipse((x1 - 66, y0 + 36, x1 - 38, y0 + 60), fill=dark)
        d.rectangle((x0 + 52, y1 - 34, x1 - 52, y1), fill=dark)
        for x in range(x0 + 56, x1 - 52, 10):
            d.polygon([(x, y1 - 34), (x + 4, y1 - 22), (x + 8, y1 - 34)], fill=bone)
        d.polygon([(x0 + 10, y0 + 40), (x0 - 16, y0 + 2), (x0 + 30, y0 + 24)], fill=bone)
        d.polygon([(x1 - 10, y0 + 40), (x1 + 16, y0 + 2), (x1 - 30, y0 + 24)], fill=bone)
        x0, y0, x1, y1 = P["tower"]
        d.polygon([(x0, y1), (x0 + 8, y0 + 16), (x1 - 8, y0 + 16), (x1, y1)], fill=bone_d)
        d.polygon([(x0 - 6, y0 + 18), (x0 + 25, y0 - 4), (x1 + 6, y0 + 18)], fill=red)
        for x, by, top in P["poles"]:
            d.line([(x, by), (x, top)], fill=bone, width=2)
            d.rectangle((x + 1, top, x + 10, top + 12), fill=red)
    else:
        P = BLOOD
        stone, stone_d, rag, bronze, dark, ember = mix((98, 76, 64)), mix((64, 50, 46)), mix((124, 100, 80)), \
            mix((152, 114, 64)), mix((46, 36, 36)), mix((236, 118, 40), 0.05)
        for cx, by in P["catapults"]:
            d.polygon([(cx - 16, by), (cx - 10, by - 12), (cx + 10, by - 12), (cx + 16, by)], fill=bronze)
            d.line([(cx + 6, by - 12), (cx - 18, by - 40)], fill=stone_d, width=3)
            d.rectangle((cx + 2, by - 24, cx + 12, by - 13), fill=dark)
        for cx, by, h in P["obelisks"]:
            d.polygon([(cx - 6, by), (cx - 4, by - h), (cx, by - h - 6), (cx + 4, by - h), (cx + 6, by)], fill=stone_d)
            d.line([(cx, by - 6), (cx - 1, by - h + 8)], fill=ember, width=1)
        for cx, by, w, h in P["tents"]:
            d.polygon([(cx - w // 2, by), (cx - w // 4, by - h), (cx + w // 4, by - h), (cx + w // 2, by)], fill=rag)
        x0, y0, x1, y1 = P["pylon"]
        d.polygon([(x0, y1), (x0 + 6, y0), (x0 + 30, y0), (x0 + 30, y1)], fill=stone)
        d.polygon([(x1 - 30, y1), (x1 - 30, y0), (x1 - 6, y0), (x1, y1)], fill=stone)
        d.rectangle((x0 + 30, y0 + 30, x1 - 30, y1), fill=dark)
        d.rectangle((x0 + 34, y1 - 36, x1 - 34, y1), fill=ember)
        (ax, ay), (bx, by), (cx, cy) = P["pyramid"]
        for i in range(5):
            t = i / 5
            d.rectangle((ax + (bx - ax) * t, by + (ay - by) * (1 - t) - 2, cx - (cx - bx) * t, cy), fill=stone)
        for cx, by in P["braziers"]:
            d.polygon([(cx - 7, by - 10), (cx + 7, by - 10), (cx + 4, by), (cx - 4, by)], fill=bronze)
            d.ellipse((cx - 3, by - 14, cx + 3, by - 9), fill=ember)
        for x, by, top in P["poles"]:
            d.line([(x, by), (x, top)], fill=stone_d, width=2)
            d.rectangle((x + 1, top, x + 10, top + 12), fill=rag)
            d.ellipse((x + 3, top + 3, x + 8, top + 8), fill=mix((140, 30, 28)))
    cx0, cy0, cx1, cy1 = CROPS[side]
    plan = np.asarray(im.crop((cx0, cy0, cx1, cy1)).resize(((cx1 - cx0) * 2, (cy1 - cy0) * 2), Image.LANCZOS))
    rng = np.random.default_rng(SEEDS[side])
    h2, w2 = plan.shape[:2]
    small = rng.normal(0, 8, (h2 // 16 + 2, w2 // 16 + 2, 3)).astype(np.float32)
    plan = plan + np.dstack([np.asarray(Image.fromarray(small[..., c]).resize((w2, h2), Image.BICUBIC)) for c in range(3)])
    return Image.fromarray(plan.clip(0, 255).round().astype(np.uint8))


def prompt_side(side):
    crop = CROPS[side]
    if side == "L":
        who = ("the settlement of the Clarth, a tribe of desert hunters who hunt the giant sand shark. Everything they "
               "build is made from the bones and hide of that shark: a great hall built inside a giant bleached shark "
               "skull, its open jaws the doorway, red hides hung on it; a tall watchtower of lashed bone with a red "
               "hide roof at the hill's corner over the gap; tents of red hide stretched over bone frames; red banners "
               "with a white horn sign on bone poles; a low fence of curved ribs along the far edge.")
        els = [((0, 64, 704, 192), "a sea of white clouds far behind and below, with a few pale blue mountain peaks"),
               ((0, 190, 660, 300), "the pale lavender flat top of the hill, bare wind scoured stone"),
               ((410, 250, 662, 300), "the blue shadowed stone face of the hill's edge"),
               ((20, 150, 220, 268), "the great hall of the Clarth inside a giant bleached sand shark skull, its open "
                                     "jaws the doorway, red hides hanging on it, the biggest building"),
               ((0, 256, 190, 300), "small tents of red hide over shark bone frames, a red banner on a bone pole"),
               ((440, 210, 540, 250), "small tents of red hide over shark bone frames"),
               ((516, 110, 582, 252), "a tall watchtower of lashed shark bones with a red hide roof"),
               ((0, 300, 704, 336), "the near edge of the warm tan stone plateau in the foreground, in shadow")]
        pal = PAL + ["#b03a30", "#701f20", "#e2d6ba", "#a89880"]
    else:
        who = ("the camp of the Bloodridge, desert guardians in the manner of ancient Egypt: a dark stone temple gate "
               "of two sloped pylon towers with ember light in its doorway, a stepped pyramid of dark stone, tall dark "
               "obelisks with thin glowing ember cracks, low tents of sepia and ash brown rags, bronze braziers, two "
               "bronze catapults standing at the hill's corner over the gap, ragged banners with a dark red sun disc "
               "on poles. The mood is ash brown, sepia and corroded bronze with ember light.")
        els = [((672, 64, 1376, 194), "a sea of white clouds far behind and below, with a few pale blue mountain peaks"),
               ((760, 190, 1376, 300), "the pale lavender flat top of the hill, bare wind scoured stone"),
               ((760, 196, 832, 290), "the blue shadowed stone face of the hill's edge by the gap"),
               ((830, 186, 924, 266), "two bronze catapults standing at the hill's corner, low tents of sepia rags, a "
                                      "bronze brazier"),
               ((916, 150, 944, 244), "a tall dark stone obelisk with thin glowing ember cracks"),
               ((950, 200, 1200, 240), "low tents of rotted sepia rags in a row"),
               ((1220, 186, 1312, 292), "the dark stone temple gate of two sloped pylon towers, ember light in the "
                                        "doorway, a ragged banner on a pole beside it"),
               ((1296, 144, 1376, 288), "a stepped pyramid of dark stone and a tall dark obelisk"),
               ((672, 300, 1376, 336), "the near edge of the warm tan stone plateau in the foreground, in shadow")]
        pal = PAL + ["#5a443a", "#3e302c", "#7a6250", "#96703e", "#f07828", "#8c201c"]
    return json.dumps({
        "high_level_description": "A wide strip of a pixel art game background: a high plateau above a sea of white "
                                  "clouds. Across a deep gap stands a pale lavender flat topped hill, and on it, seen "
                                  "from far away, is " + who + " " + FAR + " " + STYLE,
        "style_description": {"medium": "pixel_art", "art_style": RG.L3.ART,
                              "aesthetics": "a real lived in settlement seen from afar, coherent pixel clusters, depth "
                                            "and scale, readable silhouettes",
                              "lighting": LIGHT, "color_palette": pal},
        "compositional_deconstruction": {
            "background": "A distant hill top with its settlement, seen from a fixed side oblique camera like a "
                          "classic arcade game stage, clouds behind it, no people.",
            "elements": [{"type": "obj", "bbox": bb(b, crop), "desc": t} for b, t in els]}})


SHIPS = [((40, 100, 440, 340), "B"), ((488, 100, 888, 340), "B"), ((936, 100, 1336, 340), "B"),
         ((40, 430, 440, 670), "C"), ((488, 430, 888, 670), "C"), ((936, 430, 1336, 670), "C")]
BARQUE = ("a small flying sky barque of the Bloodridge: an ancient Egyptian sun boat with a long crescent hull curling "
          "up into a tall papyrus prow and stern, dark ash brown wood and corroded bronze plates with thin ember "
          "cracks, a ragged sepia sail with a dark red sun disc, ember lanterns hanging from it, side view, facing left")
SKIFF = ("a small fast flying sand skiff of the Clarth hunters: a sleek hull built from the ribs and jaw of a giant "
         "sand shark lashed with red hide, one big curved red hide sail with a white horn sign, a bone outrigger, "
         "side view, facing right")
GREY = (226, 230, 236)


def plan_ships():
    im = Image.new("RGB", (W, H), GREY)
    d = ImageDraw.Draw(im)
    for (x0, y0, x1, y1), kind in SHIPS:
        mx, my = (x0 + x1) // 2, (y0 + y1) // 2
        hull = (88, 66, 54) if kind == "B" else (190, 172, 150)
        sail = (140, 112, 86) if kind == "B" else (176, 56, 46)
        d.polygon([(x0 + 24, my - 10), (x0 + 70, y1 - 40), (x1 - 70, y1 - 40), (x1 - 24, my - 10)], fill=hull)
        if kind == "B":
            d.polygon([(x0 + 24, my - 10), (x0 + 10, y0 + 40), (x0 + 50, my - 20)], fill=hull)
            d.polygon([(x1 - 24, my - 10), (x1 - 10, y0 + 40), (x1 - 50, my - 20)], fill=hull)
            d.rectangle((mx - 60, y0 + 30, mx + 60, my - 12), fill=sail)
        else:
            d.polygon([(mx - 80, my - 12), (mx + 10, y0 + 20), (mx + 90, my - 12)], fill=sail)
    return im


def prompt_ships():
    return json.dumps({
        "high_level_description": "A pixel art sprite sheet of six small flying ships of two desert tribes, each one "
                                  "whole and separate with empty space around it, on a plain flat pale grey "
                                  "background. Top row: three different sky barques of the Bloodridge, ancient "
                                  "Egyptian sun boats that sail the sky. Bottom row: three different sand skiffs of "
                                  "the Clarth hunters. " + STYLE,
        "style_description": {"medium": "pixel_art", "art_style": RG.L3.ART,
                              "aesthetics": "clean game sprites, readable silhouettes, coherent pixel clusters",
                              "lighting": "Clear morning sun from the upper left, flat pixel colour planes.",
                              "color_palette": ["#5a443a", "#3e302c", "#7a6250", "#96703e", "#f07828", "#8c201c",
                                                "#b03a30", "#701f20", "#e2d6ba", "#a89880", "#e2e6ec"]},
        "compositional_deconstruction": {
            "background": "A plain flat pale grey background and nothing else: no sky, no clouds, no water, no ground.",
            "elements": [{"type": "obj", "bbox": [int(y0 / H * 1000), int(x0 / W * 1000), int(y1 / H * 1000),
                                                  int(x1 / W * 1000)], "desc": BARQUE if k == "B" else SKIFF}
                         for (x0, y0, x1, y1), k in SHIPS]}})


def run(tag, init, pos, neg, seed, step=2):
    RG.INP.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    init.save(RG.INP / (tag + "-init.png"))
    init.save(OUT / (tag + "-init.png"))
    g = {n: v for n, v in json.loads(RG.L3.BASE.read_text()).items() if n != "8"}
    g["4"]["inputs"]["text"] = pos
    g["5"]["inputs"]["text"] = neg
    g["10"]["inputs"].update(width=init.size[0], height=init.size[1])
    g["11"]["inputs"]["noise_seed"] = seed
    g["15"] = {"class_type": "LoadImage", "inputs": {"image": "ks-regions-20261024/" + tag + "-init.png"}}
    g["16"] = {"class_type": "VAEEncode", "inputs": {"pixels": ["15", 0], "vae": ["3", 0]}}
    g["18"] = {"class_type": "SplitSigmas", "inputs": {"sigmas": ["10", 0], "step": step}}
    g["12"]["inputs"]["sigmas"] = ["18", 1]
    g["12"]["inputs"]["latent_image"] = ["16", 0]
    g["14"]["inputs"]["filename_prefix"] = "KS-Camp-War-20261028/" + tag
    (OUT / (tag + "-workflow.json")).write_text(json.dumps(g, indent=1))
    t0 = time.time()
    out = G.wait_image(G.api("/prompt", {"prompt": g, "front": True})["prompt_id"]).convert("RGB")
    print(tag, out.size, "%.0fs" % (time.time() - t0), flush=True)
    out.save(OUT / (tag + "-raw.png"))
    return out


if __name__ == "__main__":
    neg = RG.negative().replace(", repeated buildings, duplicated landmarks", "") + (
        ", people, figures, crowds, fire, flames, smoke, text, letters, signs, trees, plants, water, modern "
        "buildings, castles, European houses, frames, borders")
    for k in (sys.argv[1:] or ["L", "R", "S"]):
        if k == "S":
            run("ships", plan_ships(), prompt_ships(), neg + ", sea, waves, sky, clouds, scenery, ground, grid, "
                "labels, crew", SEEDS["S"])
        else:
            run("town-" + k, plan_side(k), prompt_side(k), neg, SEEDS[k])
    print("all painted", flush=True)
