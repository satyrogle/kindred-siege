import json
import math
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import barren_life as BL
import camp5_hill as C5H
import camp5_layers as C5L
import regions as RG
import town_anim as TA
import town_paint as TP

G = RG.G

# town_fix.py masks | landmarks | split | render
# Jacob, 28 Oct 2026, after the audit of the war pass: "what do you need to fix this ?", then "I agree with your calls
# and go" to Claude's two calls (the fighters become small tribe silhouettes about half a tent tall; walking is a slide
# with a 1 px bob) and the fix list: the stations stay whole in layer 3 (BiRefNet cutouts on his GPU), the right edge
# column bug goes, the hills sit back in the haze, the Pals keep a clear gap, one palette, smoke breaks up instead of
# fading, painted far landmarks, far bursts on the horizon, bursts hit people, every schedule repeats every 15 s with
# arrivals replacing the fallen. render ends with the pixel scenes audit and prints every check.
C5, OUT = TP.C5, TP.OUT
W, H, FPS, SEC = TP.W, TP.H, BL.FPS, BL.SEC
N = FPS * SEC
TICK = 1000.0 / FPS
HAZE = TP.HAZE
ZONES = TA.ZONES
CUT = {"forge": (224, 130, 456, 400), "plates": (944, 228, 1200, 404)}
GAPCOLS = (656, 762)
FAR_BLUES = np.array([[122.0, 179, 218], [159, 205, 229], [181, 216, 233]])
# Clarth blood red with bone, Bloodridge near black with ember eyes: the two sides must part by hue and value at 15 px
TEAM = {"C": {"body": (120, 28, 32), "cloak": (168, 44, 38), "bone": (226, 214, 186)},
        "B": {"body": (20, 16, 18), "ember": (255, 138, 48)}}
NATIVE = {"C": -1, "B": 1}  # Clarth sheets face south west (left), Bloodridge south east (right)
SMOKE = np.array([[62.0, 56, 60], [96, 88, 92], [132, 126, 130]])
DUST = np.array([[150.0, 140, 136], [186, 178, 172], [214, 206, 200]])
CLASS_F = {"warden": 1.0, "skirmisher": 0.94, "trapper": 0.94, "marksman": 0.9, "medic": 0.9, "chief": 1.12}
SPEED = 14.0 / FPS
# Sheet box, what the painter draws there, and where it stands on the far horizon: centre x, base y, width, tallest.
# The Clarth skeleton keeps off the near skull hall and the spire off the Butcher's head.
LANDMARKS = [((40, 60, 700, 380), "the giant skeleton of an ancient sand shark lying on its side, bleached bones: a long "
                                   "curved spine with tall curved ribs and a huge skull with open jaws",
              (285, 186, 130, 40)),
             ((760, 40, 900, 380), "a tall thin spire of giant bones stacked on each other", (372, 186, 14, 40)),
             ((40, 420, 640, 710), "a great stepped pyramid of dark stone with two tall thin obelisks beside it",
              (1110, 170, 140, 64)),
             ((700, 440, 1336, 710), "a long low wall of dark stone with two sloped temple gate towers in it",
              (1250, 170, 104, 30))]
# Where everyone stands: id, sheet, target spot (moved to the nearest open hill ground that is clear of the Pals, the
# stations, the others and anything standing in front), facing, where a replacement walks in from. Perched ones stand
# on the Clarth tower's gallery and are not moved.
SLOTS = [
    dict(id="tower1", base="clarth_marksman_sw", at=(536, 143), face=1, spawn=(524, 143), perch=11),
    dict(id="tower2", base="clarth_marksman_sw", at=(562, 143), face=1, spawn=(524, 143), perch=11),
    dict(id="def1", base="clarth_warden_sw", at=(590, 240), face=1, spawn=(560, 246)),
    dict(id="def2", base="clarth_skirmisher_sw", at=(602, 238), face=1, spawn=(560, 246)),
    dict(id="def3", base="clarth_trapper_sw", at=(582, 214), face=1, spawn=(560, 246)),
    dict(id="raid1", base="blood_rot_warden_se", at=(632, 210), face=-1, spawn=(652, 202)),
    dict(id="raid2", base="blood_rot_skirmisher_se", at=(642, 204), face=-1, spawn=(652, 202)),
    dict(id="raid3", base="blood_rot_skirmisher_se", at=(616, 240), face=-1, spawn=(652, 202)),
    dict(id="chief", base="clarth_chief_sw", at=(118, 268), face=1, spawn=(118, 268), immortal=True),
    dict(id="guard1", base="clarth_warden_sw", at=(196, 290), face=1, spawn=(150, 286)),
    dict(id="walker1", base="clarth_medic_sw", at=(200, 276), face=1, spawn=(200, 276), walk_to=(232, 291),
         immortal=True),
    dict(id="crew1", base="blood_rot_trapper_se", at=(946, 262), face=-1, spawn=(982, 240)),
    dict(id="crew2", base="blood_rot_warden_se", at=(896, 273), face=-1, spawn=(982, 240)),
    dict(id="crew3", base="blood_rot_marksman_se", at=(926, 270), face=-1, spawn=(982, 240)),
    dict(id="guard2", base="blood_rot_skirmisher_se", at=(964, 256), face=-1, spawn=(982, 240)),
    dict(id="guard3", base="blood_rot_medic_se", at=(996, 260), face=-1, spawn=(982, 240)),
    dict(id="gate1", base="blood_rot_warden_se", at=(1246, 296), face=-1, spawn=(1270, 290)),
    dict(id="gate2", base="blood_rot_skirmisher_se", at=(1268, 298), face=-1, spawn=(1270, 290)),
    dict(id="walker2", base="blood_rot_marksman_se", at=(1232, 298), face=-1, spawn=(1270, 290), walk_to=(1350, 298),
         immortal=True),
]
CATAPULTS = {"C1": (836, 224), "C2": (884, 190)}
SHOTS = [(0, "C2", (548, 142), 22), (48, "C1", (596, 238), 24), (103, "C2", (586, 214), 24), (150, "C1", (606, 238), 24)]
TRACERS = [(10, "tower1", "crew1"), (36, "tower2", "crew2"), (62, "tower1", "crew3"), (88, "tower2", "crew1"),
           (116, "tower1", "crew2"), (142, "tower2", "crew3"), (166, "tower1", "guard2")]
LETHAL_TRACERS = {36, 142}
CROSSING = [dict(name="barque_mid", y=104, dir=-1, pad=48, targets=[(604, 238), (196, 290)]),
            dict(name="skiff_mid", y=120, dir=1, pad=48, targets=[(896, 273), (1268, 296)])]  # lanes above the Pals' heads
FAR_FLEET = [dict(name="barque_far", orbit=(690, 110, 180, 12), laps=1, phase=0.0),
             dict(name="skiff_far", orbit=(700, 130, 150, 10), laps=-1, phase=2.2)]
SHIP_FACE = {"barque": -1, "skiff": 1}
DOCK = (700, 189)
MELEE = [(84, "def1", "raid3"), (132, "raid1", "def3")]
FAR_BURSTS = [(6, 420, 150), (31, 1180, 142), (55, 260, 160), (83, 960, 136), (109, 70, 146), (134, 700, 132),
              (160, 1330, 150)]
# The tower top burns with the full flame; a burning tent gets the half size one, so it reads as fire on a tent and
# not as a torch as tall as the tent
FIRES = [(548, 150, "big"), (598, 236, "small"), (45, 252, "small"), (175, 268, "small"), (985, 216, "small"),
         (1322, 268, "small")]
BRAZIERS = [(942, 252)]


def lab_chroma(px):
    c = np.asarray(px, float) / 255.0
    c = np.where(c > 0.04045, ((c + 0.055) / 1.055) ** 2.4, c / 12.92)
    m = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]])
    xyz = c @ m.T / np.array([0.9505, 1.0, 1.089])
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116)
    return np.hypot(500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2]))


def median_cut(px, n):
    im = Image.fromarray(np.ascontiguousarray(np.asarray(px).clip(0, 255).astype(np.uint8))[None])
    q = im.quantize(colors=n, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    pal = np.array(q.getpalette()[:3 * n], float).reshape(-1, 3)
    return pal[np.unique(np.asarray(q))]


def nearest(px, pal):
    px = np.asarray(px, float).reshape(-1, 3)
    out = np.empty_like(px)
    for i in range(0, len(px), 20000):
        d = ((px[i:i + 20000, None, :] - pal[None]) ** 2).sum(2)
        out[i:i + 20000] = pal[d.argmin(1)]
    return out


def cutout(tag, im):
    """BiRefNet on his GPU (the core RemoveBackground node): a soft foreground mask of one station."""
    RG.INP.mkdir(parents=True, exist_ok=True)
    im.save(RG.INP / (tag + ".png"))
    g = {"1": {"class_type": "LoadImage", "inputs": {"image": "ks-regions-20261024/" + tag + ".png"}},
         "2": {"class_type": "LoadBackgroundRemovalModel", "inputs": {"bg_removal_name": "birefnet-general.safetensors"}},
         "3": {"class_type": "RemoveBackground", "inputs": {"bg_removal_model": ["2", 0], "image": ["1", 0]}},
         "4": {"class_type": "MaskToImage", "inputs": {"mask": ["3", 0]}},
         "5": {"class_type": "SaveImage", "inputs": {"images": ["4", 0], "filename_prefix": "KS-Camp-War-20261028/" + tag}}}
    out = G.wait_image(G.api("/prompt", {"prompt": g, "front": True})["prompt_id"])
    return np.asarray(out.convert("L")).astype(np.float64) / 255


def masks():
    clean = Image.open(C5 / "camp-5-clean.png").convert("RGB")
    l3i = np.asarray(Image.open(C5 / "layer-3-floor.png").convert("RGBA"))
    sm = np.zeros((H, W), bool)
    for name, (x0, y0, x1, y1) in CUT.items():
        big = clean.crop((x0, y0, x1, y1)).resize(((x1 - x0) * 4, (y1 - y0) * 4), Image.NEAREST)
        m = cutout("station-" + name, big)
        small = np.asarray(Image.fromarray((m * 255).astype(np.uint8)).resize((x1 - x0, y1 - y0), Image.BOX))
        sm[y0:y1, x0:x1] |= small.astype(np.float64) / 255 >= 0.35
        print(name, "mask px", int((small >= 0.35 * 255).sum()), flush=True)
    sm = C5L.grow(sm, 1) & (l3i[..., 3] > 127)
    Image.fromarray((sm * 255).astype(np.uint8)).save(OUT / "station-masks.png")
    a = l3i[..., :3].copy()
    a[l3i[..., 3] <= 127] = [40, 40, 40]
    a[C5L.grow(sm, 1) & ~sm] = [255, 0, 255]
    lk = Image.new("RGB", (2 * 3 * 256 + 8, 3 * 270), (18, 14, 20))
    for j, (x0, y0, x1, y1) in enumerate(CUT.values()):
        lk.paste(Image.fromarray(a).crop((x0, y0, x1, y1)).resize(((x1 - x0) * 3, (y1 - y0) * 3), Image.NEAREST),
                 (j * (3 * 256 + 8), 0))
    lk.save(HERE / "town" / "fix-station-masks.png")


def neg_base():
    return json.loads(RG.L3.BASE.read_text())["5"]["inputs"]["text"].replace(" creatures,", "").replace(" letters,", "")


def landmarks():
    im = Image.new("RGB", (W, H), TP.GREY)
    d = ImageDraw.Draw(im)
    for (x0, y0, x1, y1), _, _ in LANDMARKS:
        d.ellipse((x0 + 20, y0 + 20, x1 - 20, y1 - 20), fill=(150, 140, 130))
    pos = json.dumps({
        "high_level_description": "A pixel art sheet of four landmarks of two desert tribes, each one whole and "
                                  "separate with empty space around it, on a plain flat pale grey background, seen "
                                  "from the side. " + TP.STYLE,
        "style_description": {"medium": "pixel_art", "art_style": RG.L3.ART,
                              "aesthetics": "clean readable silhouettes, coherent pixel clusters",
                              "lighting": "Clear morning sun from the upper left, flat pixel colour planes.",
                              "color_palette": ["#e2d6ba", "#a89880", "#5a443a", "#3e302c", "#7a6250", "#e2e6ec"]},
        "compositional_deconstruction": {
            "background": "A plain flat pale grey background and nothing else: no sky, no clouds, no ground.",
            "elements": [{"type": "obj", "bbox": [int(y0 / H * 1000), int(x0 / W * 1000), int(y1 / H * 1000),
                                                  int(x1 / W * 1000)], "desc": desc}
                         for (x0, y0, x1, y1), desc, _ in LANDMARKS]}})
    neg = neg_base() + (", people, figures, text, letters, signs, labels, frames, borders, grid, ground, grass, sky, "
                        "clouds, water, scenery")
    TP.run("landmarks", im, pos, neg, 2026102805)


def cut_landmarks():
    """The four far landmarks cut off the painter's sheet with BiRefNet (it painted a brown ground patch under each),
    shrunk to their width but never taller than their own limit, recoloured into the far peaks' three blues by
    brightness."""
    raw = Image.open(OUT / "landmarks-raw.png").convert("RGB")
    out = []
    for i, ((x0, y0, x1, y1), _, (cx, by, wpx, hmax)) in enumerate(LANDMARKS):
        mp = OUT / ("landmark-mask-%d.png" % i)
        if not mp.exists():
            Image.fromarray((cutout("landmark-%d" % i, raw.crop((x0, y0, x1, y1))) * 255).astype(np.uint8)).save(mp)
        m = np.asarray(Image.open(mp).convert("L")) > 127
        reg = np.asarray(raw.crop((x0, y0, x1, y1))).astype(np.float64)
        ys, xs = np.nonzero(m)
        reg = reg[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
        m = m[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
        rgb, den = BL.box_shrink(reg, m.astype(np.float64), int(min(wpx, hmax * m.shape[1] / m.shape[0])))
        al = den >= 0.5
        lu = C5L.lum(rgb)
        qs = np.percentile(lu[al], [35, 75])
        out.append((FAR_BLUES[np.digitize(lu, qs)], al, cx, by))
    return out


def split():
    l3i = np.asarray(Image.open(C5 / "layer-3-floor.png").convert("RGBA")).astype(np.int64)
    a3 = l3i[..., 3] > 127
    r, g, b = l3i[..., 0], l3i[..., 1], l3i[..., 2]
    yy = np.arange(H)[:, None] * np.ones((1, W), int)
    xx = np.arange(W)[None, :] * np.ones((H, 1), int)
    tan = a3 & (r > b + 25)
    floor = TA.flood(tan & (yy >= 345), tan)
    run8 = floor.copy()
    for k in range(1, 8):
        run8[:-k] &= floor[k:]
    station = np.asarray(Image.open(OUT / "station-masks.png").convert("L")) > 127
    top = np.array([int(np.argmax(a3[:, x])) if a3[:, x].any() else H for x in range(W)])
    bot = np.full(W, H)
    for x in range(W):  # the sand body is the first 8 rows of floor in a row, not a thin warm line above it
        ys = np.nonzero(run8[top[x] + 2:, x])[0]
        bot[x] = top[x] + 2 + int(ys[0]) if len(ys) else H
    ok = np.ones(W, bool)
    for x0, _, x1, _ in ZONES:
        ok[x0:x1 + 1] = False
    xs = np.arange(W)
    top = np.interp(xs, xs[ok], top[ok]).round().astype(int)
    bot = np.interp(xs, xs[ok], bot[ok]).round().astype(int)
    band = a3 & (yy >= top[None, :]) & (yy < bot[None, :])
    move = band & ~station & ~(tan & (yy >= bot[None, :] - 6)) & ~((xx >= GAPCOLS[0]) & (xx < GAPCOLS[1]))
    lay1 = np.asarray(Image.open(C5 / "layer-1-far.png").convert("RGB")).astype(np.float64)
    lay1[move] = l3i[..., :3][move]
    use, struct, P = np.zeros((H, W), bool), np.zeros((H, W), bool), np.zeros((H, W, 3))
    for side, (xa, xb) in (("L", (0, GAPCOLS[0])), ("R", (GAPCOLS[1], W))):
        cx0, cy0, cx1, cy1 = TP.CROPS[side]
        raw = Image.open(OUT / ("town-%s-raw.png" % side)).convert("RGB")
        p = np.zeros((H, W, 3))
        p[cy0:cy1, cx0:cx1] = np.asarray(RG.clean(raw.resize((cx1 - cx0, cy1 - cy0), Image.BOX))).astype(np.float64)
        inside = np.zeros((H, W), bool)
        inside[cy0:cy1, max(cx0, xa):min(cx1, xb)] = True
        pr, pg, pb = p[..., 0], p[..., 1], p[..., 2]
        plu = C5L.lum(p)
        lav = (pr >= pg) & (pb - pg >= 4) & (plu > 150)
        ptan, pgrey = pr > pb + 25, (np.abs(pb - pr) < 10) & (plu < 200)
        cloud = ((pr >= 115) & (pb >= pr - 2) & ~lav & ~ptan & ~pgrey) | (plu > 224) | (lav & (plu > 200))
        above = inside & (yy < top[None, :] + 2) & (yy >= top[None, :] - 130)
        solid = C5H.mfilter(above & ~cloud & ~(pb > pr + 30), ImageFilter.MedianFilter(5))
        solid = C5H.mfilter(C5H.mfilter(solid, ImageFilter.MaxFilter(5)), ImageFilter.MinFilter(5)) & above
        u = move & inside
        st = TA.flood(C5L.grow(u, 1) & solid, solid)
        use |= u
        struct |= st
        P[inside] = p[inside]
    town = use | struct
    lay1[town] = P[town]
    pr, pg, pb = P[..., 0], P[..., 1], P[..., 2]
    plu = C5L.lum(P)
    pale = (plu > 135) & (np.abs(pr - pb) < 30) & (pb >= pg - 4)
    sand = (plu > 150) & (pr > pb + 25) & (xx >= GAPCOLS[1])
    ground = C5H.mfilter(use & ~struct & (pale | sand), ImageFilter.MedianFilter(3)) & use
    flat = ((pb > pr + 20) & (plu < 215)) | ((np.abs(pr - pb) < 30) & (pb >= pg - 4) & (plu > 95)) | (
        (pr - pb > 0.45 * plu) & (plu > 80) & (xx >= GAPCOLS[1]))  # blue faces, shadowed hill and sand lie flat
    solidm = town & ~ground & ~flat
    hills = move | struct
    stc = lab_chroma(l3i[..., :3][station])
    target = 0.85 * np.percentile(stc, 90)
    px = lay1[hills].copy()
    k = 0.0
    for k in np.arange(0.0, 0.72, 0.02):
        if np.percentile(lab_chroma(px * (1 - k) + HAZE * k), 90) <= target:
            break
    camp = np.unique(np.asarray(Image.open(C5 / "camp-5-clean.png").convert("RGB")).reshape(-1, 3), axis=0).astype(float)
    hazed = px * (1 - k) + HAZE * k
    tribe = median_cut(hazed, 28)
    lay1[hills] = nearest(hazed, np.unique(np.vstack([camp, tribe]), axis=0))
    far = np.zeros((H, W), bool)
    l1lum = C5L.lum(lay1)
    for col, al, cx, by in cut_landmarks():
        h_, w_ = al.shape
        x0, y0 = int(cx - w_ / 2), by + 6 - h_
        for (i, j) in zip(*np.nonzero(al)):
            y, x = y0 + i, x0 + j
            if 0 <= y < H and 0 <= x < W and not hills[y, x] and (y < by - 4 or l1lum[y, x] < 232):
                lay1[y, x] = col[i, j]
                far[y, x] = True
    l3n = l3i.copy()
    l3n[move, 3] = 0
    shi = np.asarray(Image.open(C5 / "pal-shadows.png").convert("RGBA")).astype(np.float64)
    over = (shi[..., 3] > 127) & move
    if over.any():  # Pal shadow pixels that fell on the old hill take the new hill under them, darkened the same
        fac = shi[..., :3][over] / np.maximum(l3i[..., :3][over].astype(np.float64), 1)
        rgb = shi[..., :3]
        rgb[over] = nearest(np.clip(lay1[over] * fac, 0, 255), np.unique(np.vstack([camp, tribe]), axis=0))
    Image.fromarray(lay1.clip(0, 255).round().astype(np.uint8)).save(OUT / "layer-1-far.png")
    Image.fromarray(l3n.astype(np.uint8), "RGBA").save(OUT / "layer-3-floor.png")
    Image.fromarray(shi.clip(0, 255).round().astype(np.uint8), "RGBA").save(OUT / "pal-shadows.png")
    for name, m in (("town", town), ("ground", ground), ("solid", solidm), ("hills", hills), ("far", far)):
        Image.fromarray((m * 255).astype(np.uint8)).save(OUT / ("mask-%s.png" % name))
    (OUT / "hill-profile.json").write_text(json.dumps({"top": top.tolist(), "bottom": bot.tolist()}))
    (OUT / "split.json").write_text(json.dumps({"haze": round(float(k), 2), "tribe_palette": tribe.round().tolist(),
                                                "station_chroma_p90": round(float(np.percentile(stc, 90)), 1)}))
    print("moved", int(move.sum()), "painted", int(use.sum()), "structures", int(struct.sum()), "haze",
          round(float(k), 2), "shadow px redone", int(over.sum()), "landmark px", int(far.sum()), flush=True)


SIL = {}
TEAMC = {}


def sil_clip(base, anim, hpx, team):
    key = (base, anim, hpx)
    if key in SIL:
        return SIL[key]
    meta = json.loads((C5H.SHEETS / ("%s_%s.json" % (base, anim))).read_text())["frames"]
    meta = meta if isinstance(meta, list) else list(meta.values())
    sheet = np.asarray(Image.open(C5H.SHEETS / ("%s_%s.png" % (base, anim))).convert("RGBA"))
    frames = [sheet[q["y"]:q["y"] + q["h"], q["x"]:q["x"] + q["w"]] for q in (f["frame"] for f in meta)]
    durs = np.array([f["duration"] for f in meta], float)
    sy = np.nonzero((C5H.sprite(base, "idle", 0)[..., 3] > 127).any(1))[0]
    s = hpx / (sy.max() - sy.min() + 1)
    uy, ux = np.nonzero(np.any([f[..., 3] > 127 for f in frames], axis=0))
    y0, y1, x0, x1 = uy.min(), uy.max() + 1, ux.min(), ux.max() + 1
    w, h = max(1, round((x1 - x0) * s)), max(1, round((y1 - y0) * s))
    pal = TEAMC[team]

    def down(m):
        return np.asarray(Image.fromarray(m.astype(np.float32)).resize((w, h), Image.BOX))

    out = []
    for f in frames:
        a = f[y0:y1, x0:x1].astype(np.float64)
        al = a[..., 3] > 127
        r, g, b = a[..., 0], a[..., 1], a[..., 2]
        lu = r * 0.299 + g * 0.587 + b * 0.114
        body = down(al) >= 0.45
        col = np.zeros((h, w, 3)) + pal["body"]
        if team == "C":
            col[down(al & (r > g + 60) & (r > b + 40)) >= 0.35] = pal["cloak"]
            col[down(al & (lu > 190) & (np.abs(r - b) < 40)) >= 0.3] = pal["bone"]
        else:
            col[down(al & (r > 180) & (g > 50) & (g < 175) & (b < 100) & (lu > 105)) >= 0.07] = pal["ember"]
        out.append((col, body))
    ys, xs = np.nonzero(frames[0][..., 3] > 127)
    SIL[key] = dict(frames=out, cum=np.cumsum(durs), total=float(durs.sum()), durs=durs.tolist(), w=w, h=h,
                    ground=max(0, round((ys.max() + 1 - y0) * s) - 1), cx=round(((xs.min() + xs.max()) / 2 - x0) * s))
    return SIL[key]


def height(f, cls):
    return int(np.clip(round((12 + (f - 205) * 0.07) * CLASS_F[cls]), 10, 19))


def put(cv, rgb, al, x0, y0, vis=None, flat=None):
    h, w = al.shape
    x0, y0 = int(round(x0)), int(round(y0))
    xa, ya, xb, yb = max(0, x0), max(0, y0), min(W, x0 + w), min(H, y0 + h)
    if xa >= xb or ya >= yb:
        return
    sub = al[ya - y0:yb - y0, xa - x0:xb - x0]
    if vis is not None:
        sub = sub & vis[ya:yb, xa:xb]
    reg = cv[ya:yb, xa:xb]
    reg[sub] = flat if flat is not None else rgb[ya - y0:yb - y0, xa - x0:xb - x0][sub]


def fx_draw(cv, fx, i, x, y, vis=None):
    if 0 <= i < len(fx["frames"]):
        rgb, al = fx["frames"][i]
        put(cv, rgb, al, x - fx["ox"], y - fx["oy"], vis)


def dot(cv, x, y, col, r=0):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H:
        cv[max(0, y - r):y + r + 1, max(0, x - r):x + r + 1] = col


def render():
    S = json.loads((OUT / "split.json").read_text())
    k_h = S["haze"]
    k_f = 0.2 * k_h
    lay1 = np.asarray(Image.open(OUT / "layer-1-far.png").convert("RGB")).astype(np.float64)
    l3i = np.asarray(Image.open(OUT / "layer-3-floor.png").convert("RGBA"))
    l3rgb, l3 = l3i[..., :3].astype(np.float64), l3i[..., 3] > 127
    shi = np.asarray(Image.open(OUT / "pal-shadows.png").convert("RGBA"))
    shrgb, shal = shi[..., :3].astype(np.float64), shi[..., 3] > 127
    M = {n: np.asarray(Image.open(OUT / ("mask-%s.png" % n)).convert("L")) > 127
         for n in ("town", "ground", "solid", "hills", "far")}
    gap = np.asarray(Image.open(C5 / "layer-2-gap-mask.png").convert("L")) > 127
    clean = np.asarray(Image.open(C5 / "camp-5-clean.png").convert("RGB")).astype(np.float64)
    _, sky, _ = BL.maps("5")
    plan = json.loads((C5 / "layer-2-plan.json").read_text())
    yy = np.arange(H)[:, None] * np.ones((1, W), int)
    skyv = sky & ~l3 & ~M["town"]
    vis_far = ~M["hills"]
    allv = np.ones((H, W), bool)
    clouds = BL.cloud_sprites(clean, sky & ~l3)
    banks = C5L.gap_banks(lay1, gap)
    pt = {s: BL.ptero_frames(s) for s in ("near", "mid", "far")}
    haze_sky = clean[sky].mean(0)
    pals = C5L.pal_sheets()
    # one master palette: camp 5's own colours, the hills' tribe colours, the effects, the two tribes' silhouettes
    camp = np.unique(clean.reshape(-1, 3), axis=0)
    for t_, d in TEAM.items():
        TEAMC[t_] = {n: np.array(c, float) * (1 - k_f) + HAZE * k_f for n, c in d.items()}
    figc = np.array([c for d in TEAMC.values() for c in d.values()])
    fxs = {n: TA.fxload(n) for n in ("ks-camp-war-explosion-32", "ks-camp-war-explosion-20", "ks-camp-town-flame")}
    fxpx = np.vstack([rgb[al] for fx in fxs.values() for rgb, al in fx["frames"] if al.any()])
    smoke_c = SMOKE * (1 - 0.7 * k_h) + HAZE * 0.7 * k_h
    dust_c = DUST * (1 - k_f) + HAZE * k_f
    MASTER = np.unique(np.vstack([camp, np.array(S["tribe_palette"]), median_cut(fxpx, 12), figc, smoke_c, dust_c]),
                       axis=0)

    def mc(c):
        return nearest(np.array([c], float), MASTER)[0]

    def variant(fx, hz, scale=1.0, ramp=None):
        src = TA.fxload(fx, scale) if isinstance(fx, str) else fx
        frames = []
        allpx = np.vstack([rgb[al] for rgb, al in src["frames"] if al.any()])
        qs = np.percentile(C5L.lum(allpx), [33, 66])
        for rgb, al in src["frames"]:
            c = ramp[np.digitize(C5L.lum(rgb), qs)] if ramp is not None else rgb * (1 - hz) + HAZE * hz
            c = c.copy()
            if al.any():
                c[al] = nearest(c[al], MASTER)
            frames.append((c, al))
        return dict(frames=frames, ox=src["ox"], oy=src["oy"])

    big = variant("ks-camp-war-explosion-32", 0.5 * k_h)
    farb = variant("ks-camp-war-explosion-20", 0.5, 0.6)
    flame = variant("ks-camp-town-flame", 0.4 * k_h)
    ember = variant("ks-camp-town-flame", 0.4 * k_h, 0.5)
    smoke = variant("ks-camp-smoke-puff", 0, 0.6, smoke_c)
    dust = variant("ks-camp-smoke-puff", 0, 0.4, dust_c)
    ships = TA.cut_ships(haze_sky)
    SH = {}
    for name, (rgb, al) in ships.items():
        hz = 0.45 if name.endswith("far") else 0.0
        c = (rgb * (1 - hz) + HAZE * hz).copy()
        c[al] = nearest(c[al], MASTER)
        SH[name] = (c, al)
    rgb, al = ships["barque_mid"]
    c = (rgb * (1 - k_f) + HAZE * k_f).copy()
    c[al] = nearest(c[al], MASTER)
    SH["docked"] = (c, al)
    for name, (c, al) in SH.items():
        Image.fromarray(np.dstack([c, al * 255]).clip(0, 255).astype(np.uint8), "RGBA").save(
            OUT / ("layer-2-ship-%s.png" % name.replace("_", "-")))
    COLS = {k: mc(v) for k, v in dict(core=(255, 220, 120), trail1=(240, 140, 50), trail2=(160, 60, 30),
                                      tracer=(255, 236, 170), bomb=(40, 30, 32), fuse=(255, 150, 60),
                                      plank=(60, 46, 40), flash=(250, 246, 236), red=(220, 70, 50)).items()}
    # where everyone stands
    palm = np.zeros((H, W), bool)
    for p in pals.values():
        x, y = p["at"]
        for _, al in p["frames"]:
            palm[y:y + al.shape[0], x:x + al.shape[1]] |= al
    pal_clear = C5L.grow(palm, 10)
    ground, solid = M["ground"], M["solid"]

    def spot_ok(X, F, w, h):
        x0, x1 = X - w // 2, X + w // 2 + 1
        return (ground[F, max(0, X - 1):X + 2].all() and not solid[F + 1:F + 15, x0:x1].any()
                and not pal_clear[max(0, F - h):F + 1, x0:x1].any() and not l3[max(0, F - h):F + 1, x0:x1].any())

    rng = np.random.default_rng(2026102804)
    slots = {}
    moved, taken = [], []
    for spec in SLOTS:
        s = dict(spec)
        s["team"] = "C" if s["base"].startswith("clarth") else "B"
        cls = s["base"].split("_")[-2]
        x, f = s["at"]
        if "perch" in s:
            s["h"] = s["perch"]
        else:
            for _ in range(2):
                h = height(f, cls)
                w = int(sil_clip(s["base"], "idle", h, s["team"])["frames"][0][1].any(0).sum())
                best = None
                for _, dx, dy in sorted((dx * dx + dy * dy, dx, dy) for dx in range(-32, 33) for dy in range(-20, 21)):
                    X, F = s["at"][0] + dx, s["at"][1] + dy
                    if (0 <= F < H - 16 and 0 <= X - w // 2 and X + w // 2 + 1 < W and spot_ok(X, F, w, h)
                            and all(abs(X - tx) >= 7 or abs(F - tf) >= 5 for tx, tf in taken)):
                        best = (X, F)
                        break
                if best is None:
                    moved.append((s["id"], "no open spot"))
                    break
                x, f = best
            s["h"] = height(f, cls)
            if (x, f) != tuple(s["at"]):
                moved.append((s["id"], s["at"], (x, f)))
        s["x"], s["f"] = x, f
        taken.append((x, f))
        s["phase"] = int(rng.integers(0, 40))
        for anim in ("idle", "attack", "hit", "death"):
            sil_clip(s["base"], anim, s["h"], s["team"])
        slots[s["id"]] = s
    print("moved to open ground:", moved, flush=True)

    def ticks(s, anim):
        return int(math.ceil(SIL[(s["base"], anim, s["h"])]["total"] / TICK))

    def walk_seq(sx, sy, ex, ey):
        L = max(1, int(math.ceil(math.hypot(ex - sx, ey - sy) / max(SPEED, math.hypot(ex - sx, ey - sy) / 40))))
        return [("walk", k, (sx, sy, ex, ey, L)) for k in range(L)]

    def lay(tape, t0, seq):
        for k, e in enumerate(seq):
            tape[(t0 + k) % N] = e

    for s in slots.values():
        tape = [None] * N
        if "walk_to" in s:
            (ax, af), (bx, bf) = (s["x"], s["f"]), s["walk_to"]
            L = (N - 60) // 2
            seq = [("walk", k, (ax, af, bx, bf, L)) for k in range(L)] + [("idle", k, None) for k in range(30)]
            seq += [("walk", k, (bx, bf, ax, af, L)) for k in range(L)]
            seq += [("idle", k, None) for k in range(N - len(seq))]
            lay(tape, 0, seq)
        else:
            j, start = 0, int(rng.integers(0, 40))
            fighter = s["id"] not in ("chief",)
            while j < N:
                rem = N - j
                at = ticks(s, "attack")
                if rem <= 30 or not fighter and rng.random() < 0.75:
                    L = rem if rem <= 30 else int(rng.integers(12, 30))
                    lay(tape, start + j, [("idle", k, None) for k in range(L)])
                elif rng.random() < 0.5 and at <= rem - 8:
                    L = at
                    lay(tape, start + j, [("attack", k, None) for k in range(L)])
                else:
                    L = int(rng.integers(8, 26))
                    lay(tape, start + j, [("idle", k, None) for k in range(min(L, rem))])
                j += L
        s["tape"] = tape
        s["died"] = False

    alive = ("idle", "attack", "hit")
    dusts = []

    def kill(s, t):
        seq = [("death", k, None) for k in range(ticks(s, "death"))] + [("lie", k, None) for k in range(20)]
        dusts.append(((t + len(seq)) % N, s["x"], s["f"]))
        seq += [("gone", k, None) for k in range(12)]
        seq += walk_seq(s["spawn"][0], s["spawn"][1], s["x"], s["f"])
        lay(s["tape"], t, seq)
        s["died"] = True

    def hit(s, t):
        L = ticks(s, "hit")
        if all(s["tape"][(t + k) % N][0] in alive for k in range(L)):
            lay(s["tape"], t, [("hit", k, None) for k in range(L)])

    impacts = []
    for t0, cat, (tx, ty), fl in SHOTS:
        impacts.append(((t0 + fl) % N, tx, ty, 14, True, None))
    bombs = []
    for sh in CROSSING:
        w = SH[sh["name"]][1].shape[1]
        for tx, ty in sh["targets"]:
            best = None
            for tr in range(N):
                x0, y0, d = TA.ship_pos(sh, w, tr)
                y0 += 12
                if ty <= y0:
                    continue
                fall = math.sqrt((ty - y0) / 150.0)
                err = abs(x0 + d * 30 * fall - tx)
                if best is None or err < best[0]:
                    best = (err, tr, x0, y0, d, fall)
            _, tr, x0, y0, d, fall = best
            b = dict(t=tr, x0=x0, y0=y0, dir=d, fall=fall, n=int(math.ceil(fall * FPS)), xi=x0 + d * 30 * fall, yi=ty)
            bombs.append(b)
            impacts.append(((tr + b["n"]) % N, b["xi"], ty, 14, True, None))
    tr_events = []
    for t0, who, tgt in TRACERS:
        tr_events.append(dict(t=t0, who=who, tgt=tgt, lethal=t0 in LETHAL_TRACERS))
    for t, a, v in MELEE:
        impacts.append((t, None, None, 0, True, (a, v)))
    events = sorted([(e[0], 0, e) for e in impacts] + [((e["t"] + 6) % N, 1, e) for e in tr_events],
                    key=lambda q: (q[0], q[1]))
    for tick, kind, e in events:
        if kind == 1:
            shooter = slots[e["who"]]
            if shooter["tape"][e["t"]][0] not in alive:
                other = "tower2" if e["who"] == "tower1" else "tower1"
                if slots[other]["tape"][e["t"]][0] not in alive:
                    e["skip"] = True
                    continue
                e["who"] = other
            v = slots[e["tgt"]]
            if v["tape"][tick][0] in alive:
                if e["lethal"] and not v["died"]:
                    kill(v, tick)
                else:
                    hit(v, tick)
            continue
        _, x, y, r, lethal, melee = e
        if melee:
            a, v = slots[melee[0]], slots[melee[1]]
            if v["tape"][tick][0] in alive and not v["died"]:
                kill(v, tick)
                if all(a["tape"][(tick - 12 + k) % N][0] in alive for k in range(ticks(a, "attack"))):
                    lay(a["tape"], tick - 12, [("attack", k, None) for k in range(ticks(a, "attack"))])
            continue
        near = [s for s in slots.values() if s["tape"][tick][0] in alive and abs(s["x"] - x) <= r and abs(s["f"] - y) <= r]
        near.sort(key=lambda s: abs(s["x"] - x) + abs(s["f"] - y))
        for i, s in enumerate(near):
            if i == 0 and lethal and not s.get("immortal") and not s["died"]:
                kill(s, tick)
            else:
                hit(s, tick)
    for s in slots.values():  # an attack or hit cut short by an arrival or a hit starts over as idle
        tp = s["tape"]
        for t in range(N):
            st, k, ex = tp[t]
            if st in ("attack", "hit") and k > 0 and tp[(t - 1) % N][:2] != (st, k - 1):
                u = t
                while tp[u % N][0] == st and tp[u % N][1] >= k:
                    tp[u % N] = ("idle", 0, None)
                    u += 1

    def draw_fig(cv, s, t, sparks=None):
        st, k, ex = s["tape"][t]
        if st == "gone":
            return
        x, f, face = s["x"], s["f"], s["face"]
        flat = None
        if st == "walk":
            sx, sy, ex_, ey, L = ex
            u = (k + 1) / L
            x, f = sx + (ex_ - sx) * u, sy + (ey - sy) * u - ((t // 3) % 2)
            face = 1 if ex_ > sx else (-1 if ex_ < sx else face)
            c, i = sil_clip(s["base"], "idle", s["h"], s["team"]), 0
        elif st == "idle":
            c = sil_clip(s["base"], "idle", s["h"], s["team"])
            n = len(c["frames"])
            i = int(t * max(1, round(SEC * 1000 / c["total"])) * n / N + s["phase"]) % n
        elif st == "lie":
            c = sil_clip(s["base"], "death", s["h"], s["team"])
            i = len(c["frames"]) - 1
        else:
            c = sil_clip(s["base"], st, s["h"], s["team"])
            i = min(int(np.searchsorted(c["cum"], k * TICK, side="right")), len(c["frames"]) - 1)
            if st == "hit":
                flat = COLS["flash"] if k == 0 else (COLS["red"] if k == 2 else None)
            if st == "attack" and 12 <= k < 15 and sparks is not None:
                sparks.append((x + face * s["h"] * 0.45, f - s["h"] * 0.5, k - 12))
        rgb, al = c["frames"][i]
        cx = c["cx"]
        if face != NATIVE[s["team"]]:
            rgb, al, cx = rgb[:, ::-1], al[:, ::-1], al.shape[1] - 1 - cx
        put(cv, rgb, al, x - cx, f - c["ground"], flat=flat)

    def feet_now(s, t):
        st, k, ex = s["tape"][t]
        if st == "walk":
            sx, sy, ex_, ey, L = ex
            return sy + (ey - sy) * (k + 1) / L
        return s["f"]

    def frame(t, only2=False, sky_clouds=True):
        cv = np.zeros_like(lay1) if only2 else lay1.copy()
        if sky_clouds:
            for (rgb, al), (cx0, cy, v) in zip(clouds, plan["sky_clouds"]):
                span = W + al.shape[1]
                BL.paste(cv, rgb, al, (cx0 - v * t / FPS) % span - al.shape[1] / 2, cy, skyv)
        for t0, x, y in FAR_BURSTS:
            a = (t - t0) % N
            if a == 0:
                dot(cv, x, y, COLS["flash"], 1)
            fx_draw(cv, farb, a, x, y, vis_far)
        for bk in banks:
            for k in range(bk["n"]):
                s_ = (bk["v"] * t / FPS + k * bk["span"] / bk["n"]) % bk["span"]
                BL.paste(cv, bk["rgb"], bk["al"], 880 - s_, bk["cy"], gap)
        near = None
        for f in plan["flyers"]:
            if f["kind"] == "circle":
                ang = 2 * math.pi * (t / N) * f["laps"] + f["phase"]
                cx, cy = f["cx"] + f["rx"] * math.cos(ang), f["cy"] + f["ry"] * math.sin(ang)
                flip = math.sin(ang) * f["laps"] > 0
                i = BL.flap_index(t, int(f["phase"] * 10), 1, 22)
            else:
                s_ = (f["x0"] + f["v"] * t / FPS) % (f["v"] * SEC)
                cx = s_ - 140 if f["dir"] > 0 else W + 140 - s_
                cy = f["cy"] + f["amp"] * math.sin(2 * math.pi * s_ / f["wave"])
                flip = f["dir"] < 0
                i = BL.flap_index(t, f.get("ph", 0))
            bob = f.get("bob", 0) * (-1 if i in (4, 5, 6) else (1 if i in (1, 2) else 0))
            rgb, al = pt[f["size"]][i]
            shot = (rgb, al, cx, cy + bob, vis_far if f["layer"] == "far" else allv, haze_sky, f.get("haze", 0), flip)
            if f["layer"] == "near":
                near = shot
            else:
                BL.paste(cv, *shot)
        for sh in FAR_FLEET:
            rgb, al = SH[sh["name"]]
            x, y, d = TA.ship_pos(sh, al.shape[1], t)
            BL.paste(cv, rgb, al, x, y, vis_far, None, 0, d != SHIP_FACE[sh["name"].split("_")[0]])
        for i, (fx, fy, _) in enumerate(FIRES):
            for a in range(16):
                if (t - a - i) % 4:
                    continue
                fx_draw(cv, smoke, a // 2, fx - 0.5 * a + 1.5 * math.sin(0.7 * a + i), fy - 6 - 1.6 * a)
        for i, (fx, fy, size) in enumerate(FIRES):
            fx_draw(cv, flame if size == "big" else ember, int(t * 23 * 8 / N + 3 * i) % 8, fx, fy)
        for i, (bx, by) in enumerate(BRAZIERS):
            fx_draw(cv, ember, int(t * 23 * 8 / N + 5 * i + 2) % 8, bx, by)
        rgb, al = SH["docked"]
        bob = int(round(math.sin(2 * math.pi * t / 45)))
        BL.paste(cv, rgb, al, DOCK[0], DOCK[1] + bob, allv)
        for k in range(14):
            dot(cv, DOCK[0] - 37 + 1 - k, DOCK[1] + 10 + bob + (k * (3 - bob)) / 14, COLS["plank"])
        sparks = []
        for s in sorted(slots.values(), key=lambda s: feet_now(s, t)):
            draw_fig(cv, s, t, sparks)
        for x, y, k in sparks:
            dot(cv, x, y, [COLS["flash"], COLS["core"], COLS["trail2"]][k], 0)
        for t0, cat, (tx, ty), fl in SHOTS:
            a = (t - t0) % N
            lx, ly = CATAPULTS[cat]
            if a < 8:
                fx_draw(cv, dust, a, lx, ly + 4)
            if a < fl:
                for j, ck in enumerate(("core", "trail1", "trail2")):
                    u = (a - 0.5 * j) / fl
                    if u >= 0:
                        A = 0.35 * math.hypot(tx - lx, ty - ly)
                        dot(cv, lx + (tx - lx) * u, ly + (ty - ly) * u - A * math.sin(math.pi * u), COLS[ck],
                            1 if j == 0 else 0)
            fx_draw(cv, big, a - fl, tx, ty - 6)
        for e in tr_events:
            if e.get("skip"):
                continue
            a = (t - e["t"]) % N
            sh_, tg = slots[e["who"]], slots[e["tgt"]]
            sx, sy = sh_["x"] + 5, sh_["f"] - sh_["h"] * 0.6
            tx, ty = tg["x"], tg["f"] - tg["h"] * 0.5
            if a < 6:
                u = (a + 1) / 6
                dx, dy = (tx - sx), (ty - sy)
                ln = math.hypot(dx, dy)
                for q in range(5):
                    dot(cv, sx + dx * u - dx / ln * q, sy + dy * u - dy / ln * q, COLS["tracer"])
            elif a < 9:
                dot(cv, tx, ty, [COLS["flash"], COLS["core"], COLS["trail2"]][a - 6], 1 if a == 6 else 0)
        for b in bombs:
            a = (t - b["t"]) % N
            if a < b["n"]:
                tau = a / FPS
                x, y = b["x0"] + b["dir"] * 30 * tau, b["y0"] + 150 * tau * tau
                dot(cv, x, y, COLS["bomb"], 1)
                dot(cv, x, y - 2, COLS["fuse"])
            fx_draw(cv, big, a - b["n"], b["xi"], b["yi"] - 6)
        for t0, x, f in dusts:
            fx_draw(cv, dust, (t - t0) % N, x, f - 4)
        for sh in CROSSING:
            rgb, al = SH[sh["name"]]
            x, y, d = TA.ship_pos(sh, al.shape[1], t)
            BL.paste(cv, rgb, al, x, y, allv, None, 0, d != SHIP_FACE[sh["name"].split("_")[0]])
        if only2:
            if near:
                BL.paste(cv, *near)
            return cv
        cv[l3] = l3rgb[l3]
        cv[shal] = shrgb[shal]
        ms = t * 1000.0 / FPS
        for nm, p in pals.items():
            rgb, al = p["frames"][int(ms // p["dur"]) % len(p["frames"])]
            x, y = p["at"]
            reg = cv[y:y + al.shape[0], x:x + al.shape[1]]
            reg[al] = rgb[al]
        if near:
            BL.paste(cv, *near)
        return cv

    dst = OUT / "camp-5-war-v2.mp4"
    proc = subprocess.Popen([BL.FF, "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", "%dx%d" % (W, H),
                             "-r", str(FPS), "-i", "-", "-vf", "scale=%d:%d:flags=neighbor" % (W * 2, H * 2),
                             "-c:v", "libx264", "-preset", "slow", "-crf", "18", "-pix_fmt", "yuv420p",
                             "-movflags", "+faststart", str(dst)], stdin=subprocess.PIPE)
    keep = {}
    for t in range(N):
        out = frame(t).clip(0, 255).astype(np.uint8)
        proc.stdin.write(out.tobytes())
        if t in (0, 1, 40, 60, 61, 75, 120, 121, 178, 179):
            keep[t] = out
    proc.stdin.close()
    proc.wait()
    Image.fromarray(keep[75]).save(OUT / "camp-5-war-v2-still.png")
    lk = Image.new("RGB", (2 * 1408 + 8, 2 * (480 + 18)), (18, 14, 20))
    d = ImageDraw.Draw(lk)
    for j, t in enumerate((40, 120)):
        for i, box in enumerate(((0, 90, 704, 330), (672, 90, 1376, 330))):
            lk.paste(Image.fromarray(keep[t]).crop(box).resize((1408, 480), Image.NEAREST), (i * 1416, j * 498 + 18))
            d.text((i * 1416 + 4, j * 498 + 3), "frame %d  %s hill at 2x" % (t, "left" if i == 0 else "right"),
                   fill=(239, 223, 190))
    lk.save(OUT / "look-war-v2-2x.png")
    chk = ((np.indices((H, W)).sum(0) // 16) % 2)[..., None] * 30 + 40
    parts = [("1  static far: hills, towns, far landmarks", lay1), ("2  living, frame 75", frame(75, True)),
             ("3  floor and stations", np.where(l3[..., None], l3rgb, chk)), ("all, frame 75", keep[75].astype(np.float64))]
    ls = Image.new("RGB", (2 * 688 + 8, 2 * (384 + 18) + 8), (18, 14, 20))
    d = ImageDraw.Draw(ls)
    for j, (name, im) in enumerate(parts):
        x, y = (j % 2) * 696, (j // 2) * 410
        ls.paste(Image.fromarray(im.clip(0, 255).astype(np.uint8)).resize((688, 384), Image.BOX), (x, y + 18))
        d.text((x + 4, y + 3), name, fill=(239, 223, 190))
    ls.save(OUT / "camp-5-war-v2-layers.png")

    def runs(tape):
        out, t = [], 0
        while t < N:
            st, _, ex = tape[t]
            u = t
            while u < N and tape[u][0] == st and (st != "walk" or tape[u][2] == ex):
                u += 1
            out.append([t, u, st] + ([list(map(float, ex[:4]))] if st == "walk" else []))
            t = u
        return out

    (OUT / "layer-2-plan.json").write_text(json.dumps({
        "fps": FPS, "seconds": SEC, "haze": k_h, "palette": MASTER.round().astype(int).tolist(),
        "sky_clouds": plan["sky_clouds"], "gap_banks": plan["gap_banks"], "flyers": plan["flyers"],
        "fighters": [{"id": s["id"], "sheet": s["base"], "x": s["x"], "feet": s["f"], "height": s["h"],
                      "face": s["face"], "spawn": s["spawn"], "runs": runs(s["tape"])} for s in slots.values()],
        "catapult_shots": SHOTS, "catapults": CATAPULTS, "tracers": [e for e in tr_events if not e.get("skip")],
        "bombs": bombs, "far_bursts": FAR_BURSTS, "fires": FIRES, "braziers": BRAZIERS, "dust": dusts,
        "docked": {"ship": "docked", "at": DOCK, "bob_period": 45}, "crossing": CROSSING, "far_ships": FAR_FLEET,
        "smoke": {"every": 4, "life": 16, "rise": 1.6, "drift": -0.5},
        "order": ["layer 1", "sky clouds (sky only)", "far bursts, far pterodactyls, far ships (not over the hills)",
                  "gap banks (gap only)", "mid pterodactyl", "smoke", "flames, braziers", "docked barque",
                  "fighters by feet", "sparks, shots, tracers, bombs, bursts, dust", "crossing ships", "layer 3",
                  "pal shadows", "pals", "near pterodactyl"]}, indent=1, default=float))
    for (base, anim, hpx), c in SIL.items():
        (OUT / "silhouettes").mkdir(exist_ok=True)
        Image.fromarray(np.hstack([np.dstack([rgb, al * 255]) for rgb, al in c["frames"]]).clip(0, 255).astype(np.uint8),
                        "RGBA").save(OUT / "silhouettes" / ("%s_%s_h%d.png" % (base, anim, hpx)))
        (OUT / "silhouettes" / ("%s_%s_h%d.json" % (base, anim, hpx))).write_text(json.dumps(
            {"w": c["w"], "h": c["h"], "durations": c["durs"], "ground": c["ground"], "cx": c["cx"]}))

    # the pixel scenes audit, on this render
    print("AUDIT", flush=True)
    old3 = np.asarray(Image.open(C5 / "layer-3-floor.png").convert("RGBA")).astype(int)
    station = np.asarray(Image.open(OUT / "station-masks.png").convert("L")) > 127
    bad = (np.abs(old3[station] - l3i[station].astype(int)).sum(1) > 0).sum()
    print(" stations unchanged in layer 3: %s (%d px differ)" % ("PASS" if bad == 0 else "FAIL", bad))
    o = old3.astype(int)
    hc = ((o[..., 0] >= o[..., 1]) & (o[..., 2] - o[..., 1] >= 2) & (C5L.lum(o) > 140)) | (
        (o[..., 2] > o[..., 0] + 30) & (C5L.lum(o) < 205))
    prof = json.loads((OUT / "hill-profile.json").read_text())
    tb, bb = np.array(prof["top"]), np.array(prof["bottom"])
    left = l3 & hc & ~station & (yy >= tb[None, :]) & (yy < bb[None, :] - 6)
    print(" old hill left in layer 3: %s (%d px)" % ("PASS" if left.sum() < 30 else "FAIL", left.sum()))
    edge = int(l3[190:min(bb[1340:].min(), 330), 1340:].sum())
    print(" right edge clear: %s (%d px)" % ("PASS" if edge == 0 else "FAIL", edge))
    feet_bad = [s["id"] for s in slots.values() if "perch" not in s and not ground[s["f"], s["x"]]]
    front_bad = [s["id"] for s in slots.values() if "perch" not in s and solid[s["f"] + 1:s["f"] + 15,
                                                                          s["x"] - 3:s["x"] + 4].any()]
    print(" feet on open hill ground: %s %s | nothing standing in front: %s %s" % (
        "PASS" if not feet_bad else "FAIL", feet_bad, "PASS" if not front_bad else "FAIL", front_bad))
    hs = sorted(s["h"] for s in slots.values())
    print(" heights %d to %d px against tents 20 to 35 px: %s" % (hs[0], hs[-1], "PASS" if hs[-1] <= 19 else "FAIL"))
    clash, team_px = 0, {"C": [], "B": []}
    for t in range(0, N, 6):
        for s in slots.values():
            m = np.full((H, W, 3), -1.0)
            draw_fig(m, s, t)
            mm = m[..., 0] >= 0
            clash += int((mm & pal_clear).sum())
            if t == 72:
                team_px[s["team"]].append(m[mm])
    print(" fighters inside the Pals' clear gap, any frame: %s (%d px)" % ("PASS" if clash == 0 else "FAIL", clash))
    cm, bm = np.vstack(team_px["C"]).mean(0), np.vstack(team_px["B"]).mean(0)
    dist = float(np.linalg.norm(cm - bm))
    print(" tribes apart: Clarth %s Bloodridge %s distance %.0f: %s" % (cm.round(), bm.round(), dist,
                                                                        "PASS" if dist >= 60 else "FAIL"))
    st75 = keep[75].astype(np.float64)
    hz = M["town"] & (yy < 300)
    cool = ((st75[..., 0] >= st75[..., 1]) & (st75[..., 2] - st75[..., 1] >= 2)) | (st75[..., 2] > st75[..., 0] + 30)
    c_t = np.percentile(lab_chroma(st75[hz & ~cool]), 90)
    print(" hills paler than the stations: chroma p90 %.0f against %.0f: %s" % (
        c_t, S["station_chroma_p90"], "PASS" if c_t < S["station_chroma_p90"] else "FAIL"))
    before = np.asarray(Image.open(C5 / "camp-5-v2-still.png").convert("RGB")).astype(np.float64)
    for nm in ("butcher", "glutton"):
        p = pals[nm]
        rgb, al = p["frames"][int(75 * 1000.0 / FPS // p["dur"]) % len(p["frames"])]
        x, y = p["at"]
        m = np.zeros((H, W), bool)
        m[y:y + al.shape[0], x:x + al.shape[1]] = al
        ring = C5L.grow(m, 4) & ~m
        db, da = (C5L.lum(before[ring]) < 100).mean(), (C5L.lum(st75[ring]) < 100).mean()
        print(" %s outline: dark ring share before %.2f after %.2f: %s" % (nm, db, da, "PASS" if da <= db + 0.05 else "FAIL"))
    nc = len(np.unique(keep[75].reshape(-1, 3), axis=0))
    print(" colours in frame 75: %d (before 236): %s" % (nc, "PASS" if nc <= 320 else "FAIL"))
    seam = []
    f2 = {t: frame(t, True, False) for t in (0, 1, 60, 61, 120, 121, 178, 179)}
    for a_, b_ in ((0, 1), (60, 61), (120, 121), (178, 179), (179, 0)):
        seam.append(int((np.abs(f2[a_] - f2[b_]).sum(2) > 30).sum()))
    print(" loop, layer 2 without the sky clouds, px changed: normal %s, seam %d: %s" % (
        seam[:4], seam[4], "PASS" if seam[4] <= 1.5 * max(seam[:4]) else "FAIL"))
    print("video MB %.1f" % (dst.stat().st_size / 1e6), "bombs", [(b["t"], round(b["xi"]), b["yi"]) for b in bombs],
          "deaths", [s["id"] for s in slots.values() if s["died"]], flush=True)


if __name__ == "__main__":
    for step in sys.argv[1:]:
        {"masks": masks, "landmarks": landmarks, "split": split, "render": render}[step]()
