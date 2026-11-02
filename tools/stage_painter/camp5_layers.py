import json
import math
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import barren_paint as BP
import barren_life as BL

RG, G = BP.RG, BP.G

# camp5_layers.py paint [step] [seed] | split | video
# Jacob, 27 Oct 2026, picked camp 5 ("5 but no where near the final"), then "yes go" to the next pass on it: paint out
# the two cloths (they read as rugs) and the halos, split it into his 3 layers (1 static far: sky, peaks, cloud sea;
# 2 living: sky clouds, pterodactyls, cloud banks drifting past the gap in the rim; 3 the plateau with the stations),
# give each Pal its 40 frame camp loop from the repo and a ground shadow. Station designs and the pixel finish are
# left for the paid artist (his 22 Oct rule).
D = BP.OUT
O = D / "camp5"
W, H = BP.W, BP.H
FPS, SEC = BL.FPS, BL.SEC
N = FPS * SEC

# The cloth patches: crop box for the painter, the cloth outline, and what inside it must survive (floating glyph
# tablets, crystals, the supply cases, the pod and its base ring).
PATCHES = {"A": ((0, 512, 512, 768),
                 [(140, 640), (170, 606), (215, 598), (262, 612), (305, 614), (340, 636), (342, 668), (325, 700),
                  (270, 706), (212, 700), (170, 682)],
                 [(236, 570, 262, 646), (279, 586, 311, 628)]),
           "B": ((864, 448, 1376, 768),
                 [(905, 680), (960, 668), (1010, 690), (1040, 700), (1080, 690), (1110, 640), (1150, 600), (1190, 560),
                  (1225, 505), (1262, 498), (1270, 560), (1330, 600), (1345, 640), (1300, 660), (1240, 662), (1222, 700),
                  (1200, 752), (1100, 758), (1020, 760), (960, 740), (915, 716)],
                 [(1094, 606, 1122, 648), (1136, 566, 1164, 608), (1082, 646, 1108, 672), (1222, 640, 1376, 768),
                  (1290, 628, 1376, 768)])}
POD = [(990, 470), (1062, 466), (1102, 520), (1104, 664), (1100, 700), (1080, 712), (1000, 712), (975, 698), (980, 640)]
PATCH_PROMPT = json.dumps({
    "high_level_description": "A close patch of bare flat warm tan stone ground on a high windswept plateau, wind "
                              "scoured and cracked, with nothing lying on it. " + BP.STYLE,
    "style_description": {"medium": "pixel_art", "art_style": RG.L3.ART,
                          "aesthetics": "bare natural stone ground, coherent pixel clusters",
                          "lighting": BP.LANDS["5"]["light"], "color_palette": BP.LANDS["5"]["pal"]},
    "compositional_deconstruction": {"background": "A patch of the flat bare plateau floor seen from a fixed side "
                                                   "oblique camera like a classic arcade game stage.",
                                     "elements": [{"type": "obj", "bbox": [0, 0, 1000, 1000],
                                                   "desc": "flat bare warm tan stone of the plateau, wind scoured and "
                                                           "cracked"}]}})
CLOTH_BANS = ", cloth, fabric, rug, blanket, carpet, towel, cape, cloak, drapery, glow, halo, bloom, objects, props"


def poly(pts):
    m = Image.new("L", (W, H), 0)
    ImageDraw.Draw(m).polygon(pts, fill=255)
    return np.asarray(m) > 0


def boxes(bs):
    m = np.zeros((H, W), bool)
    for x0, y0, x1, y1 in bs:
        m[y0:y1, x0:x1] = True
    return m


def grow(m, px):
    return np.asarray(Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(2 * px + 1))) > 127


def lum(a):
    return a[..., 0] * 0.299 + a[..., 1] * 0.587 + a[..., 2] * 0.114


def cloth_masks(a):
    """Everything inside each cloth outline goes, except the pixels of the things on or over it: the white sigil, the
    dark ring, the glyph tablets, the crystals, the supply cases and the pod. Inside the keep boxes only the object's
    own colours stay, so the cloth round them goes too (the first try kept whole boxes and the fill spread them)."""
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    lu = lum(a)
    white = (r > 200) & (g > 200) & (b > 200)
    pod = poly(POD)
    yy = np.arange(H)[:, None] * np.ones((1, W), int)
    magenta = (r > g + 30) & (b > g + 20) & (lu > 60)
    glyph = (r > 200) & (g > 100) & (b < 110) & (r - g < 110)
    case = (r - g < 12) & (lu < 80) | glyph
    crystal = (lu > 120) & (r > 150)
    out = {}
    for k, (box, outline, keep) in PATCHES.items():
        kb = boxes(keep)
        if k == "A":
            obj = white | (lu < 70) | kb
        else:
            obj = pod & ~((yy > 684) & magenta) | grow(kb & crystal, 1) & kb | kb & case
        out[k] = grow(poly(outline), 2) & ~obj
    return out


def fill(a, m):
    """Push the stone round the hole into it, ring by ring, so the painter starts from stone, not cloth. Only stone
    feeds the fill; the objects kept inside the outline never bleed into it."""
    a = a.astype(np.float64).copy()
    stone = ((a[..., 0] > a[..., 2] + 15) & (lum(a) > 90) & (a[..., 1] > 0.72 * a[..., 0])
             & (a[..., 0] - a[..., 2] < 100))
    src = stone & ~m
    todo = m.copy()
    for _ in range(600):
        if not todo.any():
            break
        acc = np.zeros_like(a)
        cnt = np.zeros(m.shape)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            k = np.roll(src, (dy, dx), (0, 1))
            v = np.roll(a, (dy, dx), (0, 1))
            acc[k] += v[k]
            cnt[k] += 1
        new = todo & (cnt > 0)
        a[new] = acc[new] / cnt[new][:, None]
        src = src | new
        todo = todo & ~new
    a[todo] = a[stone & ~m].mean(0)
    return a


def snap(px, pal):
    d = ((px[:, None, :].astype(np.int64) - pal[None, :, :]) ** 2).sum(2)
    return pal[d.argmin(1)]


def inpaint(tag, box, src, m, seed, step):
    x0, y0, x1, y1 = box
    RG.INP.mkdir(parents=True, exist_ok=True)
    Image.fromarray(fill(src, m).clip(0, 255).astype(np.uint8)).save(RG.INP / (tag + ".png"))
    Image.fromarray((m * 255).astype(np.uint8)).save(RG.INP / (tag + "-mask.png"))
    g = {n: v for n, v in json.loads(RG.L3.BASE.read_text()).items() if n != "8"}
    g["4"]["inputs"]["text"] = PATCH_PROMPT
    g["5"]["inputs"]["text"] = RG.negative() + CLOTH_BANS
    g["10"]["inputs"].update(width=x1 - x0, height=y1 - y0)
    g["11"]["inputs"]["noise_seed"] = seed
    g["15"] = {"class_type": "LoadImage", "inputs": {"image": "ks-regions-20261024/" + tag + ".png"}}
    g["16"] = {"class_type": "VAEEncode", "inputs": {"pixels": ["15", 0], "vae": ["3", 0]}}
    g["19"] = {"class_type": "LoadImageMask", "inputs": {"image": "ks-regions-20261024/" + tag + "-mask.png",
                                                          "channel": "red"}}
    g["20"] = {"class_type": "SetLatentNoiseMask", "inputs": {"samples": ["16", 0], "mask": ["19", 0]}}
    g["18"] = {"class_type": "SplitSigmas", "inputs": {"sigmas": ["10", 0], "step": step}}
    g["12"]["inputs"]["sigmas"] = ["18", 1]
    g["12"]["inputs"]["latent_image"] = ["20", 0]
    g["14"]["inputs"]["filename_prefix"] = "KS-Camp-Barren-20261027/" + tag
    (O / (tag + "-workflow.json")).write_text(json.dumps(g, indent=1))
    out = G.wait_image(G.api("/prompt", {"prompt": g, "front": True})["prompt_id"]).convert("RGB")
    return np.asarray(out.resize((x1 - x0, y1 - y0), Image.NEAREST))


def paint():
    """The cloths go by copying clean floor from elsewhere on the same plateau into each hole: the offset whose edge
    matches best, a gradient tone fit from the stone round the hole (cracks dropped), and a wavy seam inside a stone
    margin so the edge reads as floor, not a cut. Exact pixels, the camp's own palette. Masked painting on the local
    painter (inpaint above) left colour blocks in the holes at every strength tried, 27 Oct."""
    O.mkdir(parents=True, exist_ok=True)
    PATCHES["A"][2][0] = (236, 570, 262, 632)  # the tablet box held a sliver of cloth under the tablet
    PATCHES["B"][2][3] = (1226, 654, 1376, 768)  # the case boxes held cloth shadow above the cases
    PATCHES["B"][2][4] = (1292, 630, 1376, 768)
    a = np.asarray(Image.open(D / "camp-5.png").convert("RGB")).astype(np.int64)
    pal = np.unique(a.reshape(-1, 3), axis=0)
    ms = cloth_masks(a)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    lu = lum(a)
    tanish = (r > b + 10) & (g > 0.66 * r) & (r - b < 120)
    stone = (r > b + 15) & (lu > 90) & (g > 0.72 * r) & (r - b < 100)
    core = {"A": ms["A"] | boxes([(236, 630, 262, 646)]) & (r > g + 40) & (g > b),
            "B": ms["B"] | boxes([(1240, 476, 1268, 512)]) & (lu < 170)}
    band = {"A": 8, "B": 12}
    grown = {k: core[k] | grow(core[k], band[k]) & tanish & ~grow(~tanish & ~core[k] & grow(core[k], band[k] + 2), 1)
             for k in core}
    clean = stone & ~grow(grown["A"] | grown["B"], 3)
    rng = np.random.default_rng(5)
    noise = np.asarray(Image.fromarray((rng.random((H // 10 + 2, W // 10 + 2)) * 255).astype(np.uint8)).resize(
        (W, H), Image.BICUBIC)).astype(np.float64) / 255
    res = a.copy()
    for k, m in grown.items():
        ys, xs = np.nonzero(m)
        ry, rx = np.nonzero(grow(m, 2) & ~m & stone)
        best = (1e18, 0, 0)
        for dy in range(-200, 121, 4):
            if ys.min() + dy < 0 or ys.max() + dy >= H:
                continue
            for dx in range(-760, 761, 4):
                if (dx == 0 and dy == 0) or xs.min() + dx < 0 or xs.max() + dx >= W:
                    continue
                if clean[ys[::3] + dy, xs[::3] + dx].mean() < 0.99:
                    continue
                d = ((a[ry + dy, rx + dx] - a[ry, rx]) ** 2).sum(1).mean() + 6 * abs(dy)
                if d < best[0]:
                    best = (d, dx, dy)
        _, dx, dy = best
        wy, wx = np.nonzero(grow(m, 6) & ~m & stone)
        diff = (a[wy, wx] - a[wy + dy, wx + dx]).astype(np.float64)
        fit = np.c_[np.ones(len(wy)), (wx - wx.mean()) / 100.0, (wy - wy.mean()) / 100.0]
        keep = np.ones(len(wy), bool)
        for _ in range(3):
            coef = np.linalg.lstsq(fit[keep], diff[keep], rcond=None)[0]
            e = np.abs(diff - fit @ coef).sum(1)
            keep = e <= np.percentile(e, 70)
        shift = np.c_[np.ones(len(ys)), (xs - wx.mean()) / 100.0, (ys - wy.mean()) / 100.0] @ coef
        copy = np.zeros_like(a)
        copy[ys, xs] = snap((a[ys + dy, xs + dx] + shift).round().astype(np.int64), pal)
        dist = np.zeros((H, W))
        cur = m.copy()
        i = 0
        while cur.any() and i < 40:
            i += 1
            dist[cur] = i
            cur = cur & ~grow(~cur, 1)
        use = m & (grow(core[k], 2) | (dist > 2 + (band[k] - 3) * noise))
        res[use] = copy[use]
        print(k, "offset", dx, dy, "copied", int(use.sum()), "px", flush=True)
    Image.fromarray(res.astype(np.uint8)).save(O / "camp-5-clean.png")
    old = Image.open(D / "camp-5.png").convert("RGB")
    new = Image.fromarray(res.astype(np.uint8))
    look = Image.new("RGB", (2 * 688 + 8, 2 * 160 * 2 + 8), (18, 14, 20))
    for i, (x0, y0, x1, y1) in enumerate([(0, 520, 480, 768), (880, 470, 1376, 768)]):
        for j, im in enumerate((old, new)):
            c = im.crop((x0, y0, x1, y1))
            c = c.resize((688, round(688 * c.height / c.width)), Image.NEAREST)
            look.paste(c.crop((0, 0, 688, 320)), (j * 696, i * 328))
    look.save(HERE / "look-c5-paint.png")


def split():
    """Layer 3 is all the land: the rim blocks, the floor and everything standing on it. Layer 1 is what is beyond
    the rim: sky, peaks and the cloud sea, seen over the rim and through the gap. Per column, the land starts at the
    first run of rim colours below the cloud tops (cliff blues, rim lavender, floor tan); stations come from depth."""
    O.mkdir(parents=True, exist_ok=True)
    a = np.asarray(Image.open(O / "camp-5-clean.png").convert("RGB")).astype(np.int64)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    lu = lum(a)
    lav = (r >= g) & (b - g >= 4) & (lu > 150)
    tan = r > b + 25
    grey = (np.abs(b - r) < 10) & (lu < 200)
    cloud = (r >= 115) & (b >= r - 2) & ~lav & ~tan & ~grey
    dep, sky, _ = BL.maps("5")
    land_px = ~cloud & ~sky
    top = np.full(W, 300)
    for x in range(W):
        col = land_px[180:310, x]
        run = np.convolve(col.astype(int), np.ones(7, int), "valid") == 7
        hit = np.nonzero(run)[0]
        if len(hit):
            top[x] = 180 + hit[0]
    yy = np.arange(H)[:, None]
    xx = np.arange(W)[None, :]
    l3 = (yy >= top[None, :]) | ((dep >= 150) & ~sky)
    # the gap widens downward, so its walls slant and clouds sit under cliff in the same column: split per pixel there
    box = (yy >= 186) & (yy < 300) & (xx >= 600) & (xx <= 820)
    l3 = np.where(box, ~cloud & ~sky | (dep >= 150), l3)
    l3 = np.asarray(Image.fromarray((l3 * 255).astype(np.uint8)).filter(ImageFilter.MedianFilter(3))) > 127
    lay1 = a.copy()
    for x in range(W):
        t = int(top[x])
        for y in range(t, H):
            lay1[y, x] = a[t - 1 - (y - t) % 40, x]  # under the plateau: the low cloud band again, for parallax
    lay1[~l3] = a[~l3]
    Image.fromarray(lay1.astype(np.uint8)).save(O / "layer-1-far.png")
    rgba = np.dstack([a, l3 * 255]).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(O / "layer-3-floor.png")
    gap = ~l3 & (yy >= 196) & (np.arange(W)[None, :] >= 600) & (np.arange(W)[None, :] <= 820)
    Image.fromarray((gap * 255).astype(np.uint8)).save(O / "layer-2-gap-mask.png")
    # look: layer 1 tinted blue, the gap in green, layer 3 as it is
    v = a.astype(np.float64)
    v[~l3] = v[~l3] * 0.5 + np.array([40, 80, 255]) * 0.5
    v[gap] = v[gap] * 0.4 + np.array([40, 255, 80]) * 0.6
    vi = Image.fromarray(v.clip(0, 255).astype(np.uint8))
    look = Image.new("RGB", (1376, 768 // 2 + 8 + 360), (18, 14, 20))
    look.paste(vi.resize((688, 384), Image.BOX), (0, 0))
    look.paste(vi.crop((440, 120, 960, 300)).resize((1040, 360), Image.NEAREST).crop((0, 0, 1376, 360)), (0, 392))
    look.paste(Image.fromarray(lay1.astype(np.uint8)).resize((688, 384), Image.BOX), (688, 0))
    look.save(HERE / "look-c5-split.png")
    print("layer 3 px", int(l3.sum()), "gap px", int(gap.sum()), "top median", int(np.median(top)), flush=True)


def gap_banks(lay1, gap):
    """Cloud banks drifting left behind the rim, seen only through the gap. All banks in a row share one shape and
    move one spacing per loop, so the clip closes clean. The far row is slower than the near one."""
    px = lay1[gap].astype(np.float64)
    order = np.argsort(lum(px))
    ramp = np.array([px[order[int(len(order) * q)]] for q in (0.15, 0.45, 0.75, 0.97)])
    cr = np.asarray(Image.open(BL.PIECES / "clouds-raw.png").convert("RGB")).astype(np.float64)
    cm = np.asarray(Image.open(BL.PIECES / "clouds-mask.png").convert("L")).astype(np.float64) / 255
    rows = []
    for src, w, cy, n in ((0, 100, 224, 3), (1, 120, 264, 2)):
        x0, y0, x1, y1 = BL.CLOUD_BOX[src]
        rgb, al = BL.box_shrink(cr[y0:y1, x0:x1], cm[y0:y1, x0:x1], w, 0.8)
        al = al >= 0.5
        qs = np.percentile(lum(rgb)[al], [30, 60, 85])
        spr = ramp[np.digitize(lum(rgb), qs)].clip(0, 255)
        span = 220 + 120
        rows.append({"rgb": spr, "al": al, "cy": cy, "n": n, "span": span, "v": span / (n * SEC)})
    return rows


def pal_sheets():
    out = {}
    for nm, (fx, fy) in BP.FEET.items():
        meta = json.loads((BP.CA / ("CampLoops/%s_camp.json" % nm)).read_text())["frames"]
        sheet = np.asarray(Image.open(BP.CA / ("CampLoops/%s_camp.png" % nm)).convert("RGBA"))
        frames = []
        for f in meta:
            q = f["frame"]
            c = sheet[q["y"]:q["y"] + q["h"], q["x"]:q["x"] + q["w"]]
            frames.append((c[..., :3].astype(np.float64), c[..., 3] > 127))
        out[nm] = {"frames": frames, "dur": meta[0]["duration"], "at": (fx - 32 - 96, fy - 66 - 192)}
    return out


def shadows(l3rgb, l3, pals, pal):
    """A two step contact shadow on the ground under each Pal, a little to the right (the sun is upper left).
    Floating Pals get a small one on the ground under them."""
    a = l3rgb.astype(np.float64).copy()
    spec = {}
    for nm, p in pals.items():
        al = p["frames"][0][1]
        rows = np.nonzero(al.any(1))[0]
        bot = int(rows.max())
        if nm in ("shade", "whisperer"):
            cols = np.nonzero(al.any(0))[0]
            cx, cy, w, h = (cols.min() + cols.max()) / 2 + 3, 189, 46, 9
        else:
            cols = np.nonzero(al[bot - 8:bot + 1].any(0))[0]
            w = (cols.max() - cols.min()) * 1.08 + 10
            cx, cy, h = (cols.min() + cols.max()) / 2 + 5, bot - 1, max(10, w * 0.16)
        X, Y = p["at"][0] + cx, p["at"][1] + cy
        spec[nm] = [round(X), round(Y), round(w), round(h)]
        for f, k in ((1.0, 0.82), (0.68, 0.64)):
            m = Image.new("L", (W, H), 0)
            ImageDraw.Draw(m).ellipse((X - w * f / 2, Y - h * f / 2, X + w * f / 2, Y + h * f / 2), fill=255)
            m = (np.asarray(m) > 0) & l3
            a[m] = a[m] * k
    changed = np.abs(a - l3rgb).sum(2) > 0
    a[changed] = snap(a[changed].round().astype(np.int64), pal)
    return a, changed, spec


def video():
    lay1 = np.asarray(Image.open(O / "layer-1-far.png").convert("RGB")).astype(np.float64)
    l3img = np.asarray(Image.open(O / "layer-3-floor.png").convert("RGBA"))
    l3rgb, l3 = l3img[..., :3].astype(np.float64), l3img[..., 3] > 127
    gap = np.asarray(Image.open(O / "layer-2-gap-mask.png").convert("L")) > 127
    clean = np.asarray(Image.open(O / "camp-5-clean.png").convert("RGB")).astype(np.int64)
    pal = np.unique(clean.reshape(-1, 3), axis=0)
    pals = pal_sheets()
    l3s, sh_mask, spec = shadows(l3rgb, l3, pals, pal)
    sh_rgba = np.dstack([l3s, sh_mask * 255]).astype(np.uint8)
    Image.fromarray(sh_rgba, "RGBA").save(O / "pal-shadows.png")
    _, sky, _ = BL.maps("5")
    plan = json.loads(BL.PLANS.read_text())["5"]
    skyv = sky & ~l3
    clouds = BL.cloud_sprites(clean.astype(np.float64), skyv)
    banks = gap_banks(lay1, gap)
    pt = {s: BL.ptero_frames(s) for s in ("near", "mid", "far")}
    haze = clean[sky].mean(0)
    allv = np.ones((H, W), bool)
    O.joinpath("layer-2-plan.json").write_text(json.dumps(
        {"sky_clouds": plan["clouds"], "flyers": plan["flyers"], "shadows": spec,
         "gap_banks": [{k: v for k, v in b.items() if k in ("cy", "n", "span", "v")} for b in banks]}, indent=1))

    def frame(t, only2=False):
        cv = np.zeros_like(lay1) if only2 else lay1.copy()
        for (rgb, al), (cx0, cy, v) in zip(clouds, plan["clouds"]):
            span = W + al.shape[1]
            BL.paste(cv, rgb, al, (cx0 - v * t / FPS) % span - al.shape[1] / 2, cy, skyv)
        for b in banks:
            for k in range(b["n"]):
                s = (b["v"] * t / FPS + k * b["span"] / b["n"]) % b["span"]
                BL.paste(cv, b["rgb"], b["al"], 820 + 60 - s, b["cy"], allv)
        near = None
        for f in plan["flyers"]:
            if f["kind"] == "circle":
                ang = 2 * math.pi * (t / N) * f["laps"] + f["phase"]
                cx, cy = f["cx"] + f["rx"] * math.cos(ang), f["cy"] + f["ry"] * math.sin(ang)
                flip = math.sin(ang) * f["laps"] > 0
                i = BL.flap_index(t, int(f["phase"] * 10), 1, 22)
            else:
                s = (f["x0"] + f["v"] * t / FPS) % (f["v"] * SEC)
                cx = s - 140 if f["dir"] > 0 else W + 140 - s
                cy = f["cy"] + f["amp"] * math.sin(2 * math.pi * s / f["wave"])
                flip = f["dir"] < 0
                i = BL.flap_index(t, f.get("ph", 0))
            bob = f.get("bob", 0) * (-1 if i in (4, 5, 6) else (1 if i in (1, 2) else 0))
            rgb, al = pt[f["size"]][i]
            shot = (rgb, al, cx, cy + bob, allv, haze, f.get("haze", 0), flip)
            if f["layer"] == "near":
                near = shot
            else:
                BL.paste(cv, *shot)
        if only2:
            if near:
                BL.paste(cv, *near)
            return cv
        cv[l3] = l3s[l3]
        ms = t * 1000.0 / FPS
        for nm, p in pals.items():
            rgb, al = p["frames"][int(ms // p["dur"]) % len(p["frames"])]
            x, y = p["at"]
            reg = cv[y:y + al.shape[0], x:x + al.shape[1]]
            reg[al] = rgb[al]
        if near:
            BL.paste(cv, *near)
        return cv

    dst = O / "camp-5-v2.mp4"
    proc = subprocess.Popen([BL.FF, "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", "%dx%d" % (W, H),
                             "-r", str(FPS), "-i", "-", "-vf", "scale=%d:%d:flags=neighbor" % (W * 2, H * 2),
                             "-c:v", "libx264", "-preset", "slow", "-crf", "18", "-pix_fmt", "yuv420p",
                             "-movflags", "+faststart", str(dst)], stdin=subprocess.PIPE)
    keep = {}
    for t in range(N):
        out = frame(t).clip(0, 255).astype(np.uint8)
        proc.stdin.write(out.tobytes())
        if t in (30, 75, 120, 165):
            keep[t] = out
    proc.stdin.close()
    proc.wait()
    Image.fromarray(keep[75]).save(O / "camp-5-v2-still.png")
    sh = Image.new("RGB", (2 * 688 + 12, 2 * 384 + 12), (20, 20, 20))
    for j, t in enumerate(sorted(keep)):
        sh.paste(Image.fromarray(keep[t]).resize((688, 384), Image.BOX), ((j % 2) * 700, (j // 2) * 396))
    sh.save(HERE / "look-c5-v2-life.png")
    # the layers, apart: 1 far, 2 living (frame 75 on black), 3 floor with the shadows, and the whole frame
    chk = ((np.indices((H, W)).sum(0) // 16) % 2)[..., None] * 30 + 40
    l3v = np.where(l3[..., None], l3s, chk)
    parts = [("1  static far", lay1), ("2  living, frame 75", frame(75, True)), ("3  floor + Pal shadows", l3v),
             ("all three + Pals", keep[75].astype(np.float64))]
    ls = Image.new("RGB", (2 * 688 + 8, 2 * (384 + 18) + 8), (18, 14, 20))
    d = ImageDraw.Draw(ls)
    for j, (name, im) in enumerate(parts):
        x, y = (j % 2) * 696, (j // 2) * 410
        ls.paste(Image.fromarray(im.clip(0, 255).astype(np.uint8)).resize((688, 384), Image.BOX), (x, y + 18))
        d.text((x + 4, y + 3), name, fill=(239, 223, 190))
    ls.save(O / "camp-5-layers.png")
    old = Image.open(D / "camp-5-life-still.png").convert("RGB")
    cmp_ = Image.new("RGB", (2 * 688 + 8, 384 + 18), (18, 14, 20))
    d = ImageDraw.Draw(cmp_)
    for j, (name, im) in enumerate((("before", old), ("after", Image.fromarray(keep[75])))):
        cmp_.paste(im.resize((688, 384), Image.BOX), (j * 696, 18))
        d.text((j * 696 + 4, 3), name, fill=(239, 223, 190))
    cmp_.save(O / "camp-5-before-after.png")
    print("video MB %.1f" % (dst.stat().st_size / 1e6), "shadows", spec, flush=True)


if __name__ == "__main__":
    step = sys.argv[1]
    if step == "paint":
        paint(*(int(v) for v in sys.argv[2:4]))
    elif step == "split":
        split()
    elif step == "video":
        video()
