import json
import sys
import time
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
import grove3 as G
import stops_grade as SG
import refine_assets as RA
import refine_view as V

# looks3.py blockouts | generate | screens | all
# Jacob, 22 Oct: "80's grunge aint hitting with backgrounds need to try something different", then "go bro do it all 3
# do your best and reason more please". Three genuinely different looks for the crypt, judged side by side, with
# everything but the look held the same:
#   one game stage layout (framing pieces at both edges, the crypt left of centre, a sky gap with the far court,
#   a big tree on the right, an oval arena for the ranks, dark framers in the bottom corners), one seed, one strength;
#   a coloured blockout per look, so Ideogram starts from that look's light and palette and repaints the detail.
# The world is clean and colourful and the floor calm and mid value, so the grimy fighters are what pops.
OUT = Path("C:/Users/jacob/KindredSiege-Clean/Art/Studies/Bloodridge-Crypt-Looks-20261022")
INP = Path("C:/Users/jacob/ComfyUI-Installs/ComfyUI/ComfyUI-ArtLab/input/ks-crypt-looks-20261022")
BASE = Path("C:/Users/jacob/KindredSiege-Clean/Art/Studies/Stage-Pixel-Match-20261018/skullpeak/workflow-api.json")
W, H, FL = 1376, 768, 300
SEED, STEP = 2026102401, 3

LOOKS = {
    "1": {
        "name": "BRIGHT STORYBOOK",
        "c": {"sky0": (86, 166, 228), "sky1": (176, 218, 240), "far": (126, 176, 140), "court": (150, 172, 168),
              "lit": (232, 204, 156), "sh": (176, 140, 108), "door": (54, 40, 52), "wall": (220, 190, 140),
              "can": (86, 156, 64), "can2": (52, 112, 48), "trunk": (112, 82, 60), "base": (96, 150, 66),
              "arena": (122, 168, 82), "fg": (44, 96, 48), "extra": (255, 255, 255)},
        "high": "Bright storybook fantasy strategy game look in the spirit of Kingdom Rush: cheerful saturated colours, bold clean readable shapes, chunky pixel clusters, soft cartoon shading, sunny daylight under a clear blue sky with a few white clouds, spooky but cheerful.",
        "aes": "bright storybook fantasy strategy game stage, cheerful, saturated, bold readable shapes",
        "light": "Sunny midday light from the upper left as discrete flat pixel colour planes, soft cartoon shadows, the grass arena evenly lit and calm.",
        "pal": ["#2c5a30", "#4f9a3e", "#7ab84e", "#a8d46a", "#e8cc96", "#b48a62", "#6e5240", "#5aa6e4", "#b0daf0", "#ffffff", "#3a2834"],
        "els": ["a tall broken warm sandstone pillar wrapped in ivy", "a cheerful little ruined crypt temple of warm sandstone with a round dark doorway and two lit torches", "low broken sandstone walls with flowers", "a distant soft green ridge with a small stepped pyramid court", "a huge round leafy green oak tree", "a mossy sandstone arch", "a sunny green meadow arena with a few pale flagstones, calm and even", "bright green bushes and flowers"],
        "neg": "dark, gloomy, night, fog",
    },
    "2": {
        "name": "COSMIC HORROR",
        "c": {"sky0": (22, 16, 58), "sky1": (42, 94, 118), "far": (48, 40, 92), "court": (70, 60, 120),
              "lit": (156, 146, 200), "sh": (70, 60, 112), "door": (24, 10, 40), "wall": (120, 110, 170),
              "can": (70, 200, 190), "can2": (40, 120, 140), "trunk": (60, 50, 100), "base": (52, 56, 92),
              "arena": (78, 86, 124), "fg": (22, 18, 42), "extra": (214, 196, 232)},
        "high": "Cosmic horror under an alien sky: a deep indigo night with a huge cracked pale moon, ribbons of teal and magenta aurora and strange stars; ancient pale violet stone built with impossible wrong geometry, stairs that bend the wrong way and floating broken blocks; faint glowing cyan cracks and runes. Eerie, vast and beautiful, colourful, never muddy.",
        "aes": "cosmic horror battle stage, alien sky, impossible geometry, eerie glow, colourful not muddy",
        "light": "Cold moonlight and teal aurora glow as discrete flat pixel colour planes, faint cyan rim light on the stone, the arena in calm even dim blue light, still readable.",
        "pal": ["#16103a", "#2a5e76", "#30285c", "#4c3c78", "#9c92c8", "#d6c4e8", "#5ad8c8", "#c85ad2", "#4e566e", "#1a1430"],
        "els": ["a tall black monolith floating a little above the ground with glowing cyan cracks", "an ancient crypt of pale violet stone with impossible geometry, its doorway a swirling dark void full of stars", "low walls of floating broken blocks with faint glowing runes", "a far ridge under a huge cracked pale moon with a small stepped pyramid court", "a huge dead tree of pale crystal glowing teal, its branches bending the wrong way", "an arch that loops back into itself", "a smooth dark slate blue stone arena with faint cyan runes, calm and even", "dark violet crystal shards and black tendrils"],
        "neg": "sunlight, daylight, blue sky, green grass, cheerful",
    },
    "3": {
        "name": "WARM MODERN PIXEL",
        "c": {"sky0": (98, 140, 210), "sky1": (250, 206, 150), "far": (150, 146, 196), "court": (176, 162, 196),
              "lit": (246, 228, 196), "sh": (124, 120, 170), "door": (40, 40, 72), "wall": (230, 212, 186),
              "can": (236, 176, 80), "can2": (180, 110, 60), "trunk": (110, 80, 70), "base": (86, 146, 92),
              "arena": (150, 158, 188), "fg": (28, 66, 70), "extra": (255, 236, 190)},
        "high": "Modern high quality pixel art like Sea of Stars: golden hour, a warm low sun behind the ruins, rays of light through the leaves, lush vines and flowers overgrowing white marble ruins, cool blue violet shadows, fireflies, rich harmonious colour and lighting.",
        "aes": "modern high quality pixel art battle stage, golden hour light, lush, harmonious colour",
        "light": "Warm golden sunlight from behind and above the ruins, cool blue violet shadows as discrete flat pixel colour planes, the arena in calm even cool shade, a few soft light shafts.",
        "pal": ["#6290d2", "#fad096", "#9692c4", "#f6e4c4", "#7c78aa", "#ecb050", "#b46e3c", "#56925c", "#96a0bc", "#1c4246", "#282848"],
        "els": ["a tall white marble pillar wrapped in flowering vines", "a white marble crypt temple overgrown with lush vines, warm sunlight on its face, a dark doorway", "low white walls with ferns and flowers", "a far violet mountain ridge with a small stepped pyramid court in golden haze", "a huge lush tree with golden autumn leaves and light shafts through its canopy", "a ruined white arch with hanging moss", "a pale stone arena in cool blue shade, calm and even, grass along its edges", "dark teal foliage with fireflies"],
        "neg": "night, gloomy, fog, muddy",
    },
}

ART = ("Authentic hand pixelled 2D game battle stage background. Visible square pixel clusters and stair stepped contours "
       "everywhere. Flat colour clusters, two to four shades per material. Crisp pixel edges without antialiasing. "
       "Clean readable shapes. No paint brushwork, no gradients except a dithered sky, no noise texture.")
LAYOUT = ("One continuous 2.5D battle stage seen from a fixed side oblique camera like a classic arcade fighting game "
          "stage, no figures, no text. The top third is sky above a distant ridge, seen through a gap right of centre. "
          "The back of the stage is a row of ruins framed by a tall piece at each edge, the crypt left of centre and a "
          "huge tree on the right. The lower two thirds is one wide open flat arena floor where two armies will stand, "
          "calm and even, clearly separated from the back. The bottom corners hold dark foreground framing pieces in shadow.")
BOXES = [[0, 0, 400, 120], [80, 190, 410, 470], [260, 465, 400, 650], [150, 470, 330, 760], [0, 640, 410, 920],
         [30, 910, 400, 1000], [400, 70, 980, 930], [820, 0, 1000, 200]]


def blockout(c):
    im = Image.new("RGB", (W, H))
    a = np.zeros((H, W, 3))
    t = np.clip(np.arange(H) / 230.0, 0, 1)[:, None, None]
    a[:] = np.array(c["sky0"]) * (1 - t) + np.array(c["sky1"]) * t
    im = Image.fromarray(a.astype(np.uint8))
    d = ImageDraw.Draw(im)
    if c["extra"] == (255, 255, 255):                               # storybook clouds
        for cx, cy, r in ((220, 70, 34), (262, 62, 42), (304, 72, 30), (1010, 52, 30), (1046, 44, 38)):
            d.ellipse((cx - r * 1.6, cy - r, cx + r * 1.6, cy + r), fill=c["extra"])
    elif c["extra"] == (214, 196, 232):                             # the cracked moon and aurora
        d.ellipse((690, 10, 860, 180), fill=c["extra"])
        d.line((720, 60, 780, 120, 760, 160), fill=(150, 130, 180), width=3)
        for k, col in ((0, (90, 220, 200)), (1, (200, 90, 210))):
            pts = [(x, 40 + k * 34 + int(18 * np.sin(x / 90.0 + k))) for x in range(0, W, 16)]
            d.line(pts, fill=col, width=6)
    else:                                                           # the low sun's glow behind the ruins
        d.ellipse((780, 120, 920, 260), fill=c["extra"])
    d.polygon([(0, 250)] + [(x, int(216 + 10 * np.sin(x / 70.0))) for x in range(0, W + 1, 32)] + [(W, 250), (W, 320), (0, 320)], fill=c["far"])
    d.polygon([(730, 222), (770, 170), (810, 222)], fill=c["court"])
    d.rectangle((0, 250, W, FL), fill=c["far"])
    # back row
    d.rectangle((20, 20, 150, FL), fill=c["lit"]); d.rectangle((110, 20, 150, FL), fill=c["sh"])
    d.rectangle((10, 10, 160, 40), fill=c["lit"])
    d.polygon([(640, FL), (640, 210), (690, 196), (740, 214), (800, 200), (890, 222), (890, FL)], fill=c["wall"])
    d.rectangle((270, 70, 640, FL), fill=c["lit"]); d.rectangle((560, 70, 640, FL), fill=c["sh"])
    d.rectangle((250, 60, 660, 110), fill=c["lit"])
    d.rectangle((390, 150, 520, FL), fill=c["door"])
    d.rectangle((360, FL - 20, 550, FL + 8), fill=c["sh"])
    d.rectangle((1000, 120, 1080, FL + 6), fill=c["trunk"])
    d.ellipse((870, -60, 1310, 210), fill=c["can"]); d.ellipse((1010, 40, 1300, 210), fill=c["can2"])
    d.rectangle((1262, 30, 1376, FL), fill=c["lit"]); d.rectangle((1330, 30, 1376, FL), fill=c["sh"])
    # floor, arena, foreground
    d.rectangle((0, FL, W, H), fill=c["base"])
    ys, xs = np.mgrid[0:H, 0:W]
    ar = (np.abs((xs - 688) / 600.0) ** 4 + np.abs((ys - 545) / 232.0) ** 4 < 1) & (ys > FL + 8)
    a = np.asarray(im).copy()
    a[ar] = c["arena"]
    im = Image.fromarray(a)
    d = ImageDraw.Draw(im)
    d.polygon([(0, H), (0, 636), (120, 616), (210, 650), (262, 700), (300, H)], fill=c["fg"])
    d.polygon([(W, H), (W, 612), (1270, 600), (1190, 646), (1140, 700), (1110, H)], fill=c["fg"])
    return im


def prompt(L):
    els = [{"type": "obj", "bbox": b, "desc": e} for b, e in zip(BOXES, L["els"])]
    return json.dumps({"high_level_description": "Empty Bloodridge sunken crypt battle stage, rendered entirely in crisp, deliberately clustered pixel art. " + L["high"],
                       "style_description": {"medium": "pixel_art", "art_style": ART, "aesthetics": L["aes"],
                                             "lighting": L["light"], "color_palette": L["pal"]},
                       "compositional_deconstruction": {"background": LAYOUT, "elements": els}})


def generate():
    base = json.loads(BASE.read_text())
    neg0 = base["5"]["inputs"]["text"]
    for k, L in LOOKS.items():
        if (OUT / ("look-%s-raw.png" % k)).exists():
            print("skip", k)
            continue
        g = json.loads(json.dumps({n: v for n, v in base.items() if n != "8"}))
        g["4"]["inputs"]["text"] = prompt(L)
        g["5"]["inputs"]["text"] = neg0 + ", sepia, muddy brown, grunge, dull, desaturated, text, letters, " + L["neg"]
        g["11"]["inputs"]["noise_seed"] = SEED
        g["15"] = {"class_type": "LoadImage", "inputs": {"image": "ks-crypt-looks-20261022/blockout-%s.png" % k}}
        g["16"] = {"class_type": "VAEEncode", "inputs": {"pixels": ["15", 0], "vae": ["3", 0]}}
        g["18"] = {"class_type": "SplitSigmas", "inputs": {"sigmas": ["10", 0], "step": STEP}}
        g["12"]["inputs"]["sigmas"] = ["18", 1]
        g["12"]["inputs"]["latent_image"] = ["16", 0]
        g["14"]["inputs"]["filename_prefix"] = "KS-Crypt-Looks-20261022/look-" + k
        (OUT / ("look-%s-workflow.json" % k)).write_text(json.dumps(g, indent=1))
        t0 = time.time()
        raw = G.wait_image(G.api("/prompt", {"prompt": g})["prompt_id"]).convert("RGB")
        raw.save(OUT / ("look-%s-raw.png" % k))
        print("look", k, raw.size, "%.0fs" % (time.time() - t0), flush=True)


def clean(im, n=64):
    q = im.quantize(colors=n, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    pal = np.array(q.getpalette()[:3 * n], np.int32).reshape(-1, 3)
    idx = RA.adopt(np.asarray(q).astype(np.int64), np.ones(q.size[::-1], bool), thresh=1, passes=2)
    return Image.fromarray(pal[idx].astype(np.uint8)), pal, idx


def screen(scene, pal, idx, k=2):
    M = V.manifest()
    F, st = M["fighters"], M["stage"]
    lab = SG.srgb_to_lab(pal.astype(np.float64)[None])[0]
    fl = idx[420:700, 200:1200].reshape(-1)
    Lf = np.median(lab[fl, 0])
    h = np.degrees(np.arctan2(lab[:, 2], lab[:, 1]))
    hf = np.median(h[fl])
    sh = pal[int(np.argmin(np.abs(lab[:, 0] - Lf * 0.6) + 0.15 * np.abs(((h - hf + 180) % 360) - 180)))]
    v = scene.convert("RGBA")
    people = [(o["idle"], o["x"], o["y"], o["shadow"]) for o in F["ours"]]
    people.append((F["leader"]["idle"], F["leader"]["x"], F["leader"]["y"], F["leader"]["shadow"]))
    people += [(e["idle"], e["x"], e["y"], e["shadow"]) for e in F["theirs"][k]]
    people.sort(key=lambda p: p[2])
    for s, x, y, (w, hh) in people:
        V.shadow(v, x - st[0], y - st[1], w, hh, tuple(int(c) for c in sh) + (255,))
    sheets = {}
    for s, x, y, _ in people:
        V.fighter(v, s, x, y, sheets)
    c = Image.new("RGBA", (1440, 900), (0, 0, 0, 255))
    c.alpha_composite(v, tuple(st))
    PAGE = V.PAGE
    c.alpha_composite(Image.open(PAGE / "frame-fight.png").convert("RGBA"))
    c.alpha_composite(Image.open(PAGE / "t-title.png").convert("RGBA"), (36, 14))
    c.alpha_composite(Image.open(PAGE / "t-name2.png").convert("RGBA"), (36, 42))
    c.alpha_composite(Image.open(PAGE / "count-0.png").convert("RGBA"), (st[0] + 12, st[1] + 20))
    return c, float(Lf)


def separation(scene):
    """How far the floor where the ranks stand sits from each army's own colours (median Lab distance, lightness gap)."""
    M = V.manifest()
    F = M["fighters"]
    a = np.asarray(scene.convert("RGB")).astype(np.float64)[420 - 66:720 - 66, 350 - 32:1250 - 32]
    fmed = np.median(SG.srgb_to_lab(a.reshape(1, -1, 3))[0], 0)
    out = {}
    for side, sheets in (("theirs", [e["idle"]["sheet"] for e in F["theirs"][2]]), ("ours", [o["idle"]["sheet"] for o in F["ours"]])):
        px = []
        for s in sheets:
            b = np.asarray(Image.open(V.PAGE / s).convert("RGBA")).reshape(-1, 4)
            px.append(b[b[:, 3] > 0][:, :3])
        tm = np.median(SG.srgb_to_lab(np.vstack(px).astype(np.float64)[None])[0], 0)
        out[side] = [round(float(np.linalg.norm(fmed - tm)), 1), round(float(abs(fmed[0] - tm[0])), 1)]
    return out


def screens():
    shots, rep = [], {}
    for k, L in LOOKS.items():
        scene, pal, idx = clean(Image.open(OUT / ("look-%s-raw.png" % k)).convert("RGB"))
        scene.save(OUT / ("look-%s.png" % k))
        shot, Lf = screen(scene, pal, idx)
        shot.convert("RGB").save(OUT / ("look-%s-game-screen.png" % k))
        rep[k] = {"floor_L": round(Lf, 1), "separation": separation(scene)}
        shots.append((k, L["name"], shot))
    pic = Image.new("RGBA", (1440, 3 * 900 + 2 * 12), (18, 14, 20, 255))
    for i, (k, name, shot) in enumerate(shots):
        pic.alpha_composite(shot, (0, i * 912))
        V.label(pic, "%s  %s" % (k, name), (1180 - 9 * len(name), i * 912 + 96), 2)
    pic.convert("RGB").save(OUT / "pick-crypt-looks-3.png")
    print(json.dumps(rep))


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    INP.mkdir(parents=True, exist_ok=True)
    what = sys.argv[1]
    if what in ("blockouts", "all"):
        for k, L in LOOKS.items():
            b = blockout(L["c"])
            b.save(OUT / ("blockout-%s.png" % k))
            b.save(INP / ("blockout-%s.png" % k))
        print("blockouts saved")
    if what in ("generate", "all"):
        generate()
    if what in ("screens", "all"):
        screens()
