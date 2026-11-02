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
import camp_v4_paint as P
import town_anim as TA
import town_fix as TF

# camp_v4_parts.py [takes]
# Jacob, 28 Oct 2026: "go do 1-9 pass". The pieces town_v4.py draws, built from camp_v4_paint's paintings and the
# sprites already in the repo (no new character drawing): the new ships, the Butcher's three forge machines baked over
# his anvil (his hammer, arm and sparks stay in front), the strike effects of his locked verbs (purple black hole, blue
# portal, gold frozen time), the Shade's three route takes and the Whisperer's three regalia takes in astral outlines,
# the landing ring with the trailer's own wipeout weapons and ash, the dome hits, the anchor trying to restart, the
# supply case glyphs pulsing, and the fighters cut so their poses read. Effects are drawn here, never generated: flat
# ramps of four, snap and hold, and they end by breaking up. "takes" renders the pick video and sheet.
OUT = TF.OUT
V4 = OUT / "v4"
CA = P.RG.REPO / "Assets/Resources/CampaignArt"
REM = P.RG.REPO / "Art/PixelLab/Cult/2026-10-18/remains"
W, H, FPS = TF.W, TF.H, TF.FPS
N = FPS * 30
AT = {"butcher": (350, 142), "glutton": (1016, 138), "shade": (140, 448), "whisperer": (834, 508)}
CYC = {"butcher": 8, "glutton": 5, "shade": 6, "whisperer": 7}  # whole loops in the 30 s preview, 3 to 7% off
CLIP = {"butcher": 80, "shade": 80, "whisperer": 40}  # effect clip lengths in body frames, both divide the preview
OUTLINE = (30.0, 24, 34)
HOLE = np.array([[24.0, 14, 36], [90, 40, 150], [170, 110, 240], [240, 225, 255]])
PORTAL = np.array([[16.0, 34, 96], [40, 110, 230], [120, 200, 255], [235, 250, 255]])
TIME = np.array([[110.0, 70, 16], [214, 154, 40], [255, 214, 100], [255, 248, 214]])
ASTRAL = np.array([[60.0, 40, 110], [120, 110, 220], [190, 215, 255], [245, 250, 255]])
DOME = np.array([[140.0, 64, 104], [226, 116, 156], [255, 186, 208], [255, 244, 250]])
SPARK = np.array([[255.0, 170, 70], [180, 240, 255], [255, 255, 255]])
CASE = np.array([[120.0, 64, 34], [242, 162, 74], [255, 214, 140]])
RAMP = {"hole": HOLE, "portal": PORTAL, "time": TIME}
VERBS = ["hole", "portal", "time", "hole"]  # the four strikes of the Butcher's two loop clip
PEAKS = (8, 25)  # his hammer lands on frames 8 and 25 of 40
SBOX = (40, 400, 380, 690)
WBOX = (700, 470, 1140, 720)
HITS = [(40, (300, 128)), (128, (690, 150)), (214, (880, 128)), (300, (1290, 160))]  # stray shots on the dome
ASH = [("ash_1", (640, 466)), ("small_ash_1", (556, 474)), ("small_ash_2", (772, 414))]
RIM = {"C": np.array([178.0, 74, 62]), "B": np.array([96.0, 86, 92])}
TAKES = {"butcher": ["m1", "m2", "m3"], "shade": ["ghost step", "rift mine", "mirror"],
         "whisperer": ["pod grows a fighter", "dresses a fighter", "ghost copies"]}


def lum(a):
    return a[..., 0] * 0.299 + a[..., 1] * 0.587 + a[..., 2] * 0.114


def erode(m):
    return np.asarray(Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3))) > 127


def grow(m, px):
    return C5L.grow(m, px)


def comp(seed, m):
    """The 4 connected region of m holding seed (y, x)."""
    out = np.zeros_like(m)
    stack = [seed]
    while stack:
        y, x = stack.pop()
        if 0 <= y < m.shape[0] and 0 <= x < m.shape[1] and m[y, x] and not out[y, x]:
            out[y, x] = True
            stack += [(y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)]
    return out


def rgba_frames(nm):
    meta = json.loads((CA / ("CampLoops/%s_camp.json" % nm)).read_text())["frames"]
    sheet = np.asarray(Image.open(CA / ("CampLoops/%s_camp.png" % nm)).convert("RGBA"))
    return [sheet[q["y"]:q["y"] + q["h"], q["x"]:q["x"] + q["w"]].copy() for q in (f["frame"] for f in meta)], meta


def sprite_frame(base, anim="idle", i=0):
    meta = json.loads((CA / ("%s_%s.json" % (base, anim))).read_text())["frames"]
    meta = meta if isinstance(meta, list) else list(meta.values())
    q = meta[i]["frame"]
    im = np.asarray(Image.open(CA / ("%s_%s.png" % (base, anim))).convert("RGBA"))
    return im[q["y"]:q["y"] + q["h"], q["x"]:q["x"] + q["w"]].copy()


def astral(f, line=1, inner=2):
    """A sprite as an astral ghost: its silhouette in one flat light tone inside an astral outline. A shape, not a
    texture (inner detail lines turned to scribble on these sprites, 28 Oct)."""
    al = f[..., 3] > 127
    core = erode(al)
    out = np.zeros(f.shape, np.uint8)
    out[core, :3] = ASTRAL[inner]
    out[al & ~core, :3] = ASTRAL[line]
    out[al, 3] = 255
    return out


def behind(f, x0, y0, shape):
    """Where the Pal's own frame is not, in an effect box: ghosts drawn with it sit behind her."""
    m = np.zeros(shape, bool)
    al = f[..., 3] > 127
    h, w = al.shape
    xa, ya = max(0, x0), max(0, y0)
    xb, yb = min(shape[1], x0 + w), min(shape[0], y0 + h)
    m[ya:yb, xa:xb] = al[ya - y0:yb - y0, xa - x0:xb - x0]
    return ~m


def over(dst, src, x0, y0, keep=None):
    """src RGBA drawn over dst RGBA at (x0, y0); keep is an optional mask in dst space."""
    h, w = src.shape[:2]
    x0, y0 = int(round(x0)), int(round(y0))
    xa, ya, xb, yb = max(0, x0), max(0, y0), min(dst.shape[1], x0 + w), min(dst.shape[0], y0 + h)
    if xa >= xb or ya >= yb:
        return
    s = src[ya - y0:yb - y0, xa - x0:xb - x0]
    m = s[..., 3] > 127
    if keep is not None:
        m = m & keep[ya:yb, xa:xb]
    dst[ya:yb, xa:xb][m] = s[m]


def thin(src, frac, seed, rise=0, drift=0):
    """Break up: keep a fixed random share of the pixels, nudged up and out."""
    m = src[..., 3] > 127
    r = np.random.default_rng(seed).random(m.shape)
    out = np.zeros_like(src)
    ys, xs = np.nonzero(m & (r < frac))
    ys2 = np.clip(ys - rise, 0, src.shape[0] - 1)
    xs2 = np.clip(xs + (np.sign(xs - src.shape[1] / 2) * drift).astype(int), 0, src.shape[1] - 1)
    out[ys2, xs2] = src[ys, xs]
    return out


def C(r, i):
    return tuple(int(v) for v in r[i]) + (255,)


# ships -----------------------------------------------------------------------------------------------------------
SHIPW = {"b_barge": 84, "b_dart": 64, "b_drum": 58, "b_fins": 70, "c_fin": 60, "c_sled": 62, "c_jaw": 52,
         "b_barge_far": 36, "b_dart_far": 32, "c_jaw_far": 30, "c_fin_far": 30}


def ships(haze):
    raw = np.asarray(Image.open(OUT / "v4-ships-raw.png").convert("RGB")).astype(np.int64)
    bg = np.median(raw[4:24, 4:60].reshape(-1, 3), axis=0)
    V4.mkdir(parents=True, exist_ok=True)
    out = {}
    for name, wpx in SHIPW.items():
        x0, y0, x1, y1 = P.SHIP_BOXES[name.replace("_far", "")]
        reg = raw[y0:y1, x0:x1]
        m = C5H.mfilter(np.abs(reg - bg).sum(2) > 45, ImageFilter.MedianFilter(3))
        ys, xs = np.nonzero(m)
        reg, m = reg[ys.min():ys.max() + 1, xs.min():xs.max() + 1], m[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
        rgb, al = TA.shrink_fixed(np.dstack([reg, m * 255]).astype(np.uint8), wpx / m.shape[1], haze, 0.1)
        out[name] = (rgb, al)
        Image.fromarray(np.dstack([rgb, al * 255]).clip(0, 255).astype(np.uint8), "RGBA").save(
            V4 / ("ship-%s.png" % name.replace("_", "-")))
    return out


def ship_face(name):
    return -1 if name.startswith("b") else 1


# the Butcher -----------------------------------------------------------------------------------------------------
def butcher():
    """Each machine look baked over the anvil in all 40 frames. A pixel of his is kept in front only where that frame
    differs from his usual look there (the hammer, his arm, the sparks), so the anvil never shows again."""
    fr, meta = rgba_frames("butcher")
    st = np.stack(fr).astype(np.int64)
    key = ((st[..., 0] >> 3) << 15) | ((st[..., 1] >> 3) << 10) | ((st[..., 2] >> 3) << 5) | (st[..., 3] > 127)
    cnt = np.stack([(key == key[j]).sum(0) for j in range(len(fr))])
    best = cnt.argmax(0)
    tkey = np.take_along_axis(key, best[None], 0)[0]
    tcnt = cnt.max(0)
    moving = [(key[i] != tkey) & (st[i, ..., 3] > 127) for i in range(len(fr))]
    bx, by = AT["butcher"]
    x0, y0, x1, y1 = P.BLOCK
    yy, xx = np.mgrid[:192, :192]
    inblock = (xx >= x0 - bx) & (xx <= x1 - bx) & (yy >= y0 - by + 2) & (yy <= y1 - by)
    anvil = inblock & ((tkey & 1) == 1) & (tcnt >= 30)
    sparks = {i: moving[i] & (lum(st[i]) > 140) & (st[i, ..., 0] > 170) & (st[i, ..., 2] < 170) & (yy >= 50)
              & (yy <= 150) for i in range(len(fr))}
    ys, xs = np.nonzero(sparks[PEAKS[0]])
    pt = (int(round(xs.mean())), int(round(ys.mean()))) if len(xs) else (88, 108)
    # his sprite's dither flickers every frame; over the machine only real moving shapes (hammer, arm) stay in front
    zone = grow(inblock, 14)
    for i in range(len(fr)):
        m, keep = moving[i] & zone, np.zeros((192, 192), bool)
        seen = np.zeros_like(m)
        for y, x in zip(*np.nonzero(m)):
            if seen[y, x]:
                continue
            part, stack = [], [(y, x)]
            seen[y, x] = True
            while stack:
                cy, cx = stack.pop()
                part.append((cy, cx))
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        ny, nx = cy + dy, cx + dx
                        if 0 <= ny < 192 and 0 <= nx < 192 and m[ny, nx] and not seen[ny, nx]:
                            seen[ny, nx] = True
                            stack.append((ny, nx))
            if len(part) >= 10:
                py_, px_ = zip(*part)
                keep[list(py_), list(px_)] = True
        moving[i] = (moving[i] & ~zone) | keep
    takes = {}
    for k in TAKES["butcher"]:
        plan = np.asarray(Image.open(OUT / ("v4-%s-plan-mask.png" % k)).convert("L"))[by:by + 192, bx:bx + 192] > 127
        sil = plan | grow(anvil, 2)
        raw = np.asarray(Image.open(OUT / ("v4-%s-raw.png" % k)).convert("RGB")).astype(np.float64)
        mrgb = raw[by - P.MCROP[1]:by - P.MCROP[1] + 192, bx - P.MCROP[0]:bx - P.MCROP[0] + 192].copy()
        pal = TF.median_cut(mrgb[sil], 24)
        mrgb = TF.nearest(mrgb.reshape(-1, 3), pal).reshape(192, 192, 3)
        mrgb[sil & ~erode(sil)] = OUTLINE
        frames = []
        for i in range(len(fr)):
            cover = sil & ~moving[i]
            rgb = st[i, ..., :3].astype(np.float64)
            rgb[cover] = mrgb[cover]
            frames.append((rgb, (st[i, ..., 3] > 127) | cover))
        takes[k] = dict(frames=frames, pal=np.vstack([pal, [OUTLINE]]))
        print("butcher %s: plan misses %d anvil edge px (filled from the painting), hammer px over the machine per "
              "loop %d" % (k, int((grow(anvil, 2) & ~plan).sum()), int(sum((sil & moving[i]).sum() for i in range(40)))),
              flush=True)
    print("butcher anvil px", int(anvil.sum()), "strike point", pt, "spark px at the peaks",
          [int(sparks[p].sum()) for p in PEAKS], flush=True)
    return dict(takes=takes, pt=pt, sparks=sparks, st=st, meta=meta)


def verb_shape(d, verb, a, pt):
    px, py = pt
    R = RAMP[verb]
    if verb == "hole":
        if 0 <= a <= 4:
            rc, rr = [2, 3, 4, 4, 4][a], [4, 6, 7, 7, 7][a]
            d.ellipse((px - rr - 1, py - rr - 1, px + rr + 1, py + rr + 1), outline=C(R, 1))
            d.ellipse((px - rr, py - rr, px + rr, py + rr), outline=C(R, 2))
            d.ellipse((px - rc, py - rc, px + rc, py + rc), fill=C(R, 0))
            rs = [17, 14, 11, 8, 6][a]
            for k in range(8):
                th = k * math.pi / 4 + a * 0.35
                x, y = px + rs * math.cos(th), py + rs * 0.8 * math.sin(th)
                d.point((x, y), fill=C(R, 3 if k % 2 else 2))
                d.point((x + 1, y), fill=C(R, 2))
        elif a == 5:
            d.ellipse((px - 2, py - 2, px + 2, py + 2), fill=C(R, 0))
            d.point((px, py), fill=C(R, 3))
        elif a == 6:
            for k in range(4):
                th = k * math.pi / 2 + 0.4
                d.point((px + 4 * math.cos(th), py + 4 * math.sin(th)), fill=C(R, 2))
    elif verb == "portal":
        cx, cy = px, py - 2
        if 0 <= a <= 6:
            rx, ry = [(3, 6), (9, 15), (8, 13), (8, 13), (8, 13), (8, 13), (8, 13)][a]
            d.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=C(R, 0), outline=C(R, 2), width=2)
            d.ellipse((cx - rx + 2, cy - ry + 2, cx + rx - 2, cy + ry - 2), outline=C(R, 1))
            if a >= 2:
                d.arc((cx - rx + 1, cy - ry + 1, cx + rx - 1, cy + ry - 1), a * 60, a * 60 + 100, fill=C(R, 3))
            if 2 <= a <= 5:
                for k in range(3):
                    u = min(1.0, (a - 2 + k / 3) / 4)
                    d.point((cx - 5 + 5 * k, cy - 20 + 20 * u), fill=C(R, 3))
        elif a == 7:
            d.line((cx - 8, cy, cx + 8, cy), fill=C(R, 3))
    else:
        if 0 <= a <= 5:
            r = 13 if a == 0 else 11
            d.ellipse((px - r, py - r, px + r, py + r), outline=C(R, 2))
            d.ellipse((px - r + 2, py - r + 2, px + r - 2, py + r - 2), outline=C(R, 1))
            for th in range(0, 360, 90):
                c_, s_ = math.cos(math.radians(th)), math.sin(math.radians(th))
                d.line((px + (r - 3) * c_, py + (r - 3) * s_, px + r * c_, py + r * s_), fill=C(R, 3))
            d.line((px, py, px, py - r + 4), fill=C(R, 3))
            d.line((px, py, px + 4, py + 2), fill=C(R, 3))
        elif a in (6, 7):
            n = 12 if a == 6 else 6
            for k in range(n):
                th = k * 2 * math.pi / n + 0.2
                r = 10 + 5 * (a - 6) + (k % 3)
                d.point((px + r * math.cos(th), py + r * math.sin(th)), fill=C(R, 2 + (k % 2)))


def butcher_fx(B):
    """80 frames, two of his loops, in his own cell. The sparks his hammer makes take the verb's colours; gold time
    holds the sparks of the strike frozen while he lifts the hammer again."""
    st, sp, pt = B["st"], B["sparks"], B["pt"]
    out = []
    for f in range(80):
        b, L = f % 40, f // 40
        arr = np.zeros((192, 192, 4), np.uint8)
        for s, pk in enumerate(PEAKS):
            a, verb = b - pk, VERBS[2 * L + s]
            R = RAMP[verb]
            if -1 <= a <= 4:
                m = sp[b]
                lu = lum(st[b].astype(np.float64))
                cols = np.where(lu[..., None] < 170, R[1], np.where(lu[..., None] < 215, R[2], R[3]))
                arr[m, :3] = cols[m]
                arr[m, 3] = 255
            if verb == "time" and 0 <= a <= 5:
                m = sp[pk]
                lu = lum(st[pk].astype(np.float64))
                cols = np.where(lu[..., None] < 170, R[1], np.where(lu[..., None] < 215, R[2], R[3]))
                arr[m, :3] = cols[m]
                arr[m, 3] = 255
        im = Image.fromarray(arr, "RGBA")
        d = ImageDraw.Draw(im)
        for s, pk in enumerate(PEAKS):
            verb_shape(d, VERBS[2 * L + s], b - pk, pt)
        out.append(np.asarray(im).copy())
    return out


# the Shade -------------------------------------------------------------------------------------------------------
def shade_fx():
    """Three takes of 80 frames (two of her loops) in the box SBOX, each with a front and a ground layer."""
    fr, _ = rgba_frames("shade")
    sx, sy = AT["shade"]
    bw, bh = SBOX[2] - SBOX[0], SBOX[3] - SBOX[1]
    takes = {}
    # 1 ghost step: an astral copy of her steps out to the left from behind her, holds, breaks up
    k0 = 18
    ghost = astral(fr[k0 % 40])
    flash = ghost.copy()
    flash[flash[..., 3] > 0, :3] = ASTRAL[3]
    front, ground = [], []
    for f in range(80):
        a = f - k0
        cv = np.zeros((bh, bw, 4), np.uint8)
        keep = behind(fr[f % 40], sx - SBOX[0], sy - SBOX[1], (bh, bw))
        if a == 0:
            over(cv, flash, sx - 4 - SBOX[0], sy - SBOX[1], keep)
        elif 1 <= a <= 12:
            dx = {1: -14, 2: -34}.get(a, -30)
            over(cv, ghost, sx + dx - SBOX[0], sy - SBOX[1], keep)
        elif 13 <= a <= 16:
            over(cv, thin(ghost, [0.6, 0.35, 0.16, 0.05][a - 13], 7 + a, a - 12, 0), sx - 30 - SBOX[0],
                 sy - SBOX[1], keep)
        front.append(cv)
        ground.append(np.zeros((bh, bw, 4), np.uint8))
    takes["ghost step"] = (front, ground)
    # 2 rift mine: a rune ring blooms on the ground under her, ticks turn, the core blinks and spits, it snaps out
    k0 = 16
    cx, cy = 237 - SBOX[0], 637 - SBOX[1]
    front, ground = [], []
    for f in range(80):
        a = f - k0
        im = Image.new("RGBA", (bw, bh), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        if a == 0:
            d.line((cx - 2, cy, cx + 2, cy), fill=C(ASTRAL, 3))
        elif a == 1:
            d.ellipse((cx - 16, cy - 5, cx + 16, cy + 5), outline=C(ASTRAL, 2), width=2)
        elif a == 2:
            d.ellipse((cx - 46, cy - 14, cx + 46, cy + 14), outline=C(ASTRAL, 3), width=2)
        elif 3 <= a <= 15:
            hot = a == 15
            d.ellipse((cx - 40, cy - 12, cx + 40, cy + 12), outline=C(ASTRAL, 3 if hot else 2), width=2)
            d.ellipse((cx - 26, cy - 8, cx + 26, cy + 8), outline=C(ASTRAL, 3 if hot else 1))
            d.ellipse((cx - 12, cy - 4, cx + 12, cy + 4), outline=C(ASTRAL, 3 if hot else 0))
            for k in range(8):
                th = math.radians(k * 45 + (a // 2) * 15)
                d.line((cx + 30 * math.cos(th), cy + 9 * math.sin(th), cx + 40 * math.cos(th),
                        cy + 12 * math.sin(th)), fill=C(ASTRAL, 3))
            d.rectangle((cx - 1, cy - 1, cx + 1, cy + 1), fill=C(ASTRAL, 3 if (a % 4 < 2 or hot) else 2))
            if a % 4 == 0:
                d.point((cx, cy - 4 - (a % 8)), fill=C(ASTRAL, 3))
        elif 16 <= a <= 18:
            for k in range(8):
                th = math.radians(k * 45 + 7 * 15)
                d.point((cx + 40 * math.cos(th), cy + 12 * math.sin(th) - 2 * (a - 15)), fill=C(ASTRAL, 2))
        ground.append(np.asarray(im).copy())
        front.append(np.zeros((bh, bw, 4), np.uint8))
    takes["rift mine"] = (front, ground)
    # 3 mirror: a dark glass pane opens beside her with her astral reflection in it, a glint runs down, it flashes
    # and shatters
    k0 = 22
    px, py, HW, HH = 150 - SBOX[0], 548 - SBOX[1], 20, 34
    pane = np.zeros((bh, bw), bool)
    pane[py - HH + 2:py + HH - 1, px - HW + 2:px + HW - 1] = True
    rng = np.random.default_rng(2026102851)
    shards = [(px + rng.uniform(-16, 16), py + rng.uniform(-32, 32), rng.uniform(-3, 3), rng.uniform(-2.5, 0.5))
              for _ in range(16)]

    def astral_lines(f):
        """Only her edge, as light lines: a reflection on dark glass (a filled one hid the pane)."""
        al_ = f[..., 3] > 127
        edge = al_ & ~erode(al_)
        out = np.zeros(f.shape, np.uint8)
        out[edge, :3] = ASTRAL[2]
        out[edge, 3] = 255
        return out
    front, ground = [], []
    for f in range(80):
        a = f - k0
        im = Image.new("RGBA", (bw, bh), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        if a == 0:
            d.line((px, py - HH, px, py + HH), fill=C(ASTRAL, 3))
        elif a in (1, 2):
            hw = 6 if a == 1 else HW + 3
            d.rectangle((px - hw, py - HH, px + hw, py + HH), outline=C(ASTRAL, 2 if a == 1 else 3),
                        width=1 if a == 1 else 2)
        elif 3 <= a <= 12:
            d.rectangle((px - HW, py - HH, px + HW, py + HH), fill=C(ASTRAL, 0), outline=C(ASTRAL, 2), width=2)
        elif a == 13:
            d.rectangle((px - HW, py - HH, px + HW, py + HH), fill=C(ASTRAL, 3))
        elif 14 <= a <= 18:
            u = a - 13
            for j, (x, y, vx, vy) in enumerate(shards):
                X, Y = x + vx * u, y + vy * u + 0.5 * u * u
                d.line((X, Y, X + 1, Y + (1 if j % 2 else -1)), fill=C(ASTRAL, 2 if j % 2 else 1))
        arr = np.asarray(im).copy()
        if 3 <= a <= 12:
            refl = astral_lines(fr[f % 40][:, ::-1].copy())
            over(arr, refl, 150 - 96.5 - SBOX[0], sy - SBOX[1], pane)
            im = Image.fromarray(arr, "RGBA")
            d = ImageDraw.Draw(im)
            gy = py - HH + (a - 3) * 7
            d.line((px - HW + 2, gy + 6, px + HW - 2, gy - 4), fill=C(ASTRAL, 3))
            arr = np.asarray(im).copy()
        front.append(arr)
        ground.append(np.zeros((bh, bw, 4), np.uint8))
    takes["mirror"] = (front, ground)
    return takes


# the Whisperer ---------------------------------------------------------------------------------------------------
def pod_window(l3rgb):
    x0, y0, x1, y1 = 990, 470, 1130, 710
    a = l3rgb[y0:y1, x0:x1]
    lu = lum(a)
    yy, xx = np.mgrid[y0:y1, x0:x1]
    lens = ((xx - 1042) / 34.0) ** 2 + ((yy - 622) / 70.0) ** 2 <= 1  # the open front of the pod, measured 28 Oct
    m = lens & (lu < 200) & (((a[..., 2] > a[..., 1] + 8) & (lu < 175)) |
                             ((a[..., 0] > a[..., 1] + 40) & (a[..., 2] > a[..., 1] + 10)))
    ys, xs = np.nonzero(m)
    cyx = (int(np.median(ys)), int(np.median(xs)))
    if not m[cyx]:
        k = np.argmin((ys - cyx[0]) ** 2 + (xs - cyx[1]) ** 2)
        cyx = (ys[k], xs[k])
    reg = comp(cyx, m)
    reg = erode(~erode(~reg))  # close small holes
    full = np.zeros((H, W), bool)
    full[y0:y1, x0:x1] = reg
    return full


def crescent(f):
    """Her own crescent, cut from her frame: the loose piece, not joined to her body, most in her top left."""
    al = f[..., 3] > 127
    seen, comps = np.zeros_like(al), []
    for y, x in zip(*np.nonzero(al)):
        if not seen[y, x]:
            c_ = comp((y, x), al)
            seen |= c_
            comps.append(c_)
    comps.sort(key=lambda c_: -int(c_.sum()))
    cands = [c_ for c_ in comps[1:] if 30 <= c_.sum() <= 600]

    def topleft(c_):
        ys_, xs_ = np.nonzero(c_)
        return int(((ys_ < 96) & (xs_ < 96)).sum())

    m = max(cands, key=topleft)
    ys, xs = np.nonzero(m)
    piece = f[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy()
    piece[~m[ys.min():ys.max() + 1, xs.min():xs.max() + 1]] = 0
    return piece, ((xs.min() + xs.max()) / 2, (ys.min() + ys.max()) / 2)


def whisperer_fx(l3rgb):
    fr, _ = rgba_frames("whisperer")
    wx, wy = AT["whisperer"]
    bw, bh = WBOX[2] - WBOX[0], WBOX[3] - WBOX[1]
    cres, (ccx, ccy) = crescent(fr[0])
    start = (wx + ccx - WBOX[0], wy + ccy - WBOX[1])
    win = pod_window(l3rgb)
    ys, xs = np.nonzero(win)
    winfo = (int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max()), int(win.sum()))
    winbox = win[WBOX[1]:WBOX[3], WBOX[0]:WBOX[2]]
    takes = {}

    def dress(fig, feet, clip_mask, k0, bloom):
        """A fighter's astral outline; three crescents fly onto its head and shoulders, it flashes (a rank up), holds,
        breaks up."""
        fh, fw = fig.shape[:2]
        fx0, fy0 = feet[0] - fw / 2, feet[1] - fh
        fa = fig[..., 3] > 0
        ys_, xs_ = np.nonzero(fa)
        top, cx = fy0 + ys_.min(), fx0 + (xs_.min() + xs_.max()) / 2
        tgts = [(cx, top - 10), (cx - 20, top + 46), (cx + 20, top + 46)]  # a crown and both hands, not over it
        out = []
        for f in range(40):
            a = f - k0
            cv = np.zeros((bh, bw, 4), np.uint8)
            if a == 0 and bloom is not None:
                cv[bloom] = np.append(ASTRAL[2], 255).astype(np.uint8)
            if 1 <= a <= 17:
                g = fig.copy()
                if a == 13:
                    g[g[..., 3] > 0, :3] = ASTRAL[3]
                over(cv, g, fx0, fy0, clip_mask)
            if 18 <= a <= 21:
                over(cv, thin(fig, [0.6, 0.35, 0.16, 0.05][a - 18], 30 + a, a - 17, 0), fx0, fy0, clip_mask)
            for j, (tx, ty) in enumerate(tgts):
                s = 2 + 2 * j
                u = (a - s) / 5
                if 0 <= u < 1:
                    mx_, my_ = (start[0] + tx) / 2, min(start[1], ty) - 40
                    x = (1 - u) ** 2 * start[0] + 2 * u * (1 - u) * mx_ + u * u * tx
                    y = (1 - u) ** 2 * start[1] + 2 * u * (1 - u) * my_ + u * u * ty
                    over(cv, cres, x - cres.shape[1] / 2, y - cres.shape[0] / 2)
                elif u >= 1 and a <= 17:
                    c_ = cres.copy()
                    if a == 13 or a - s == 5:
                        c_[c_[..., 3] > 0, :3] = 255
                    over(cv, c_, tx - cres.shape[1] / 2, ty - cres.shape[0] / 2)
                elif 18 <= a <= 21:
                    over(cv, thin(cres, [0.6, 0.35, 0.16, 0.05][a - 18], 40 + a + j, a - 17, 0),
                         tx - cres.shape[1] / 2, ty - cres.shape[0] / 2)
            out.append(cv)
        return out

    med = astral(sprite_frame("clarth_medic_sw"))
    ys_, xs_ = np.nonzero(winbox)
    feet = ((xs_.min() + xs_.max()) / 2, ys_.max() - 6)
    takes["pod grows a fighter"] = dress(med, feet, None, 6, winbox)  # it stands in the open pod, in front
    skir = astral(sprite_frame("clarth_skirmisher_sw"))[:, ::-1].copy()
    sk_al = skir[..., 3] > 0
    gy, gx = np.nonzero(sk_al)
    skir = skir[gy.min():gy.max() + 1, gx.min():gx.max() + 1]
    takes["dresses a fighter"] = dress(skir, (790 - WBOX[0], 694 - WBOX[1]), None, 6, None)
    # 3 ghost copies: two astral copies of her slide out left and right, hold, snap back with a flash
    out = []
    for f in range(40):
        a = f - 8
        cv = np.zeros((bh, bw, 4), np.uint8)
        if 0 <= a <= 9:
            g = astral(fr[f % 40])
            if a in (0, 9):
                g[g[..., 3] > 0, :3] = ASTRAL[3]
            dx = {0: 3, 1: 8, 2: 22, 9: 3}.get(a, 18)
            keep = behind(fr[f % 40], wx - WBOX[0], wy - WBOX[1], (bh, bw))
            for sgn in (-1, 1):
                over(cv, g, wx + sgn * dx - WBOX[0], wy - WBOX[1], keep)
        out.append(cv)
    takes["ghost copies"] = out
    print("pod window x %d to %d, y %d to %d, %d px; crescent %dx%d" % (winfo + cres.shape[1::-1]), flush=True)
    return takes


# the floor: the landing ring and the wipeout's debris ------------------------------------------------------------
def layer3_v4():
    l3i = np.asarray(Image.open(OUT / "layer-3-floor.png").convert("RGBA"))
    rgb, al = l3i[..., :3].astype(np.float64), l3i[..., 3] > 127
    before = rgb.copy()
    ring = np.asarray(Image.open(OUT / "v4-ring-plan-mask.png").convert("L")) > 127
    raw = np.asarray(Image.open(OUT / "v4-ring-raw.png").convert("RGB")).astype(np.float64)
    sub = np.zeros_like(rgb)
    sub[P.RCROP[1]:P.RCROP[1] + 320, P.RCROP[0]:P.RCROP[0] + 512] = raw
    rgb[ring] = TF.nearest(sub[ring], TF.median_cut(sub[ring], 28))
    al = al | ring
    bright = ring & (lum(rgb) > 165)
    ax, ay = P.ANCHOR
    taken = np.zeros((H, W), bool)
    taken[ay - 46:ay + 26, ax - 30:ax + 30] = True
    ok_zone = grow(ring, 26)
    pals = np.zeros((H, W), bool)
    for nm, (x, y) in AT.items():
        fr, _ = rgba_frames(nm)
        u = np.any([f[..., 3] > 127 for f in fr], axis=0)
        pals[y:y + 192, x:x + 192] |= u[:max(0, min(192, H - y)), :max(0, min(192, W - x))]
    pals = grow(pals, 12)
    placed, shadow = {}, np.zeros((H, W), bool)
    spr_all = []
    for name, (cx, cy) in list(P.WEAPONS.items()) + ASH:
        im = np.asarray(Image.open(REM / (name + ".png")).convert("RGBA"))
        ys, xs = np.nonzero(im[..., 3] > 127)
        im = im[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
        sa = im[..., 3] > 127
        h, w = sa.shape
        best = None
        for _, dx, dy in sorted((dx * dx + dy * dy, dx, dy) for dx in range(-40, 41, 2) for dy in range(-24, 25, 2)):
            x0, y0 = cx + dx - w // 2, cy + dy - h // 2
            if x0 < 0 or y0 < 0 or x0 + w > W or y0 + h > H:
                continue
            reg = (bright | taken | pals)[y0:y0 + h, x0:x0 + w]
            if not ok_zone[y0:y0 + h, x0:x0 + w][sa].all():
                continue
            bad = int((reg & sa).sum())
            if best is None or bad < best[0]:
                best = (bad, x0, y0)
            if bad == 0:
                break
        bad, x0, y0 = best
        sh = np.zeros((H, W), bool)
        sh[y0 + 2:y0 + 2 + h, x0 + 3:x0 + 3 + w] = sa[:H - y0 - 2, :W - x0 - 3] if y0 + 2 + h > H or x0 + 3 + w > W else sa
        shadow |= sh
        taken[y0:y0 + h, x0:x0 + w] |= sa
        placed[name] = (x0, y0, w, h, bad)
        spr_all.append((im, x0, y0))
    rgb[shadow & ~taken] *= 0.7
    rem_px = np.vstack([im[im[..., 3] > 127][:, :3] for im, _, _ in spr_all]).astype(np.float64)
    rem_pal = TF.median_cut(rem_px, 20)
    for im, x0, y0 in spr_all:
        sa = im[..., 3] > 127
        h, w = sa.shape
        reg = rgb[y0:y0 + h, x0:x0 + w]
        reg[sa] = TF.nearest(im[sa][:, :3].astype(np.float64), rem_pal)
        al[y0:y0 + h, x0:x0 + w] |= sa
    sm = np.asarray(Image.open(OUT / "station-masks.png").convert("L")) > 127
    print("ring px", int(ring.sum()), "station px changed", int((np.abs(rgb - before).sum(2)[sm] > 0).sum()),
          "placed", {k: v for k, v in placed.items()}, flush=True)
    Image.fromarray(np.dstack([rgb, al * 255]).clip(0, 255).astype(np.uint8), "RGBA").save(OUT / "layer-3-floor-v4.png")
    return rgb, al, ring, rem_pal, placed


# things that move on the camp, drawn straight into the frame -----------------------------------------------------
HEX = [(-3, 0), (-2, -1), (-2, 1), (-1, -2), (0, -2), (1, -2), (-1, 2), (0, 2), (1, 2), (2, -1), (2, 1), (3, 0)]


def dome(cv, t, cols):
    """Stray shots from the raid stop dead on the camp's shield dome: a streak, a flash, a ring of glyph hexes
    spreading, held, then breaking up cell by cell."""
    for hi, (t0, (ix, iy)) in enumerate(HITS):
        a = (t - t0) % N
        if a >= N - 6:
            u = (a - (N - 6) + 1) / 6
            sx, sy = ix + (-36 if ix < 688 else 36), iy + 64
            x, y = sx + (ix - sx) * u, sy + (iy - sy) * u
            ln = math.hypot(ix - sx, iy - sy)
            for q in range(5):
                TF.dot(cv, x - (ix - sx) / ln * q, y - (iy - sy) / ln * q, cols["tracer"])
            continue
        if a > 13:
            continue
        if a == 0:
            TF.dot(cv, ix, iy, DOME[3], 1)
            for k in range(8):
                th = k * math.pi / 4
                TF.dot(cv, ix + 3 * math.cos(th), iy + 3 * 0.62 * math.sin(th), DOME[2])
            continue
        r = [6, 11, 16, 20, 23][a - 1] if a <= 5 else 23
        rnd = np.random.default_rng(900 + hi)
        for j in range(-6, 7):
            for i in range(-5, 6):
                hx, hy = ix + i * 6 + (j % 2) * 3, iy + j * 4
                dd = math.hypot((hx - ix), (hy - iy) / 0.62)
                if dd > r or (a <= 5 and dd < r - 6):
                    continue
                if a >= 10 and rnd.random() < 0.25 * (a - 9):
                    continue
                c = DOME[2] if dd > r - 5 else DOME[1]
                for ox, oy in HEX:
                    TF.dot(cv, hx + ox, hy + oy, c)


def anchor_parts(l3rgb):
    """The painted hoop, measured on layer-3-floor-v4 (28 Oct): centre (686, 413), its line about 3 px wide. The
    field fills an ellipse just inside the line; the hoop for sparks is the band round it. (A colour test caught only
    the hoop's lit top arc, and a row span caught the bronze weapons beside it.)"""
    cx, cy = 686, 413
    yy, xx = np.mgrid[:H, :W]
    hole = ((xx - cx) / 17.0) ** 2 + ((yy - cy) / 22.0) ** 2 <= 1
    hoop = (((xx - cx) / 22.0) ** 2 + ((yy - cy) / 27.0) ** 2 <= 1) & ~hole
    print("anchor hoop px", int(hoop.sum()), "hole px", int(hole.sum()), flush=True)
    return hoop, hole


def anchor(cv, t, hoop, hole):
    """The anchor keeps trying to restart: its hoop fills with a violet field from the bottom up under a bright edge,
    holds, fizzles in a flash and throws sparks. Small sparks crawl on the hoop between tries. One try every 6 s."""
    a = t % 72
    hy, hx = np.nonzero(hole)
    py, px = np.nonzero(hoop)
    if 3 <= a <= 13:
        top, bot = hy.min(), hy.max()
        lev = bot - (bot - top + 1) * min(1.0, (a - 2) / 8)
        m = hy >= lev
        cv[hy[m], hx[m]] = ASTRAL[2] if a >= 11 else ASTRAL[1]
        if a < 11:
            e = m & (hy <= lev + 1)
            cv[hy[e], hx[e]] = ASTRAL[3]
        else:
            TF.dot(cv, int(hx.mean()), int(hy.mean()), ASTRAL[3], 1)
    elif a == 14:
        cv[hy, hx] = ASTRAL[3]
    elif 15 <= a <= 18:
        rnd = np.random.default_rng(77)
        cx, cy = hx.mean(), hy.mean()
        for k in range(10):
            th = rnd.uniform(0, 2 * math.pi)
            r = 24 + 5 * (a - 15)
            TF.dot(cv, cx + r * math.cos(th), cy + 1.2 * r * math.sin(th) + (a - 15), SPARK[k % 3])
    elif a % 9 == 0 and len(px):
        rnd = np.random.default_rng(a)
        for k in rnd.integers(0, len(px), 2):
            TF.dot(cv, px[k], py[k], SPARK[1 + k % 2])


def case_glyphs(l3rgb):
    x0, y0, x1, y1 = 1196, 616, W, H
    a = l3rgb[y0:y1, x0:x1]
    m = (a[..., 0] > 200) & (a[..., 1] > 100) & (a[..., 1] < 200) & (a[..., 2] < 120)
    full = np.zeros((H, W), bool)
    full[y0:y1, x0:x1] = m
    print("case glyph px", int(m.sum()), flush=True)
    return np.nonzero(full)


def cases(cv, t, gl):
    ys, xs = gl
    lev = 0.5 + 0.5 * np.sin(2 * math.pi * t / 36 - (xs - 1196) / 30.0)
    idx = np.where(lev < 0.3, 0, np.where(lev < 0.78, 1, 2))
    cv[ys, xs] = CASE[idx]


# the fighters, cut so their poses read ---------------------------------------------------------------------------
SIL4 = {}


def sil_clip_v4(base, anim, hpx, team):
    """town_fix's silhouette, with thin parts kept (30% cover, not 45%), lone specks and pin holes cleaned, and a
    1 px lit rim on the top and left edges (the sun is upper left)."""
    key = (base, anim, hpx)
    if key in SIL4:
        return SIL4[key]
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
    pal = TF.TEAMC[team]
    rim = RIM[team] * (1 - KF[0]) + TF.HAZE * KF[0]

    def down(m):
        return np.asarray(Image.fromarray(m.astype(np.float32)).resize((w, h), Image.BOX))

    out = []
    for f in frames:
        a = f[y0:y1, x0:x1].astype(np.float64)
        al = a[..., 3] > 127
        r, g, b = a[..., 0], a[..., 1], a[..., 2]
        lu = r * 0.299 + g * 0.587 + b * 0.114
        body = down(al) >= 0.3
        pad = np.pad(body, 1)
        n4 = pad[:-2, 1:-1].astype(int) + pad[2:, 1:-1] + pad[1:-1, :-2] + pad[1:-1, 2:]
        body = (body & (n4 > 0)) | (~body & (n4 == 4))
        col = np.zeros((h, w, 3)) + pal["body"]
        plain = np.ones((h, w), bool)
        if team == "C":
            m1 = down(al & (r > g + 60) & (r > b + 40)) >= 0.35
            m2 = down(al & (lu > 190) & (np.abs(r - b) < 40)) >= 0.3
            col[m1], col[m2] = pal["cloak"], pal["bone"]
            plain &= ~m1 & ~m2
        else:
            m1 = down(al & (r > 180) & (g > 50) & (g < 175) & (b < 100) & (lu > 105)) >= 0.07
            col[m1] = pal["ember"]
            plain &= ~m1
        pad = np.pad(body, 1)
        lit = body & plain & (~pad[:-2, 1:-1] | ~pad[1:-1, :-2])
        col[lit] = rim
        out.append((col, body))
    ys, xs = np.nonzero(frames[0][..., 3] > 127)
    SIL4[key] = dict(frames=out, cum=np.cumsum(durs), total=float(durs.sum()), durs=durs.tolist(), w=w, h=h,
                     ground=max(0, round((ys.max() + 1 - y0) * s) - 1), cx=round(((xs.min() + xs.max()) / 2 - x0) * s))
    return SIL4[key]


KF = [0.08]  # town_v4 sets the fighters' haze before the first cut


def rim_colours(k_f):
    return np.array([c * (1 - k_f) + TF.HAZE * k_f for c in RIM.values()])


def effect_palette():
    return np.vstack([HOLE, PORTAL, TIME, ASTRAL, DOME, SPARK, CASE])


# the pick video and sheet ----------------------------------------------------------------------------------------
CROPS = {"butcher": (290, 120, 710, 360), "shade": (40, 420, 460, 660), "whisperer": (720, 470, 1140, 710)}


def takes_video():
    lay1 = np.asarray(Image.open(OUT / "layer-1-far.png").convert("RGB")).astype(np.float64)
    l3rgb, l3al, _, _, _ = layer3_v4()
    shi = np.asarray(Image.open(OUT / "pal-shadows.png").convert("RGBA"))
    bg = lay1.copy()
    bg[l3al] = l3rgb[l3al]
    bg[shi[..., 3] > 127] = shi[..., :3][shi[..., 3] > 127]
    B = butcher()
    bfx = butcher_fx(B)
    S = shade_fx()
    Wfx = whisperer_fx(l3rgb)
    base = {nm: rgba_frames(nm)[0] for nm in ("shade", "whisperer")}
    durs = {"butcher": 100, "shade": 130, "whisperer": 110}

    def cell(nm, take, ms):
        cv = bg.copy()
        b = int(ms // durs[nm])
        fi = b % 40
        x, y = AT[nm]
        if nm == "butcher":
            rgb, al = B["takes"][take]["frames"][fi]
            reg = cv[y:y + 192, x:x + 192]
            reg[al] = rgb[al]
            e = bfx[b % 80]
            m = e[..., 3] > 0
            reg[m] = e[..., :3][m]
        elif nm == "shade":
            front, ground = S[take]
            e = ground[b % 80]
            m = e[..., 3] > 0
            cv[SBOX[1]:SBOX[3], SBOX[0]:SBOX[2]][m] = e[..., :3][m]
            f = base[nm][fi]
            reg = cv[y:y + 192, x:x + 192]
            reg[f[..., 3] > 127] = f[..., :3][f[..., 3] > 127]
            e = front[b % 80]
            m = e[..., 3] > 0
            cv[SBOX[1]:SBOX[3], SBOX[0]:SBOX[2]][m] = e[..., :3][m]
        else:
            f = base[nm][fi]
            reg = cv[y:y + 192, x:x + 192]
            reg[f[..., 3] > 127] = f[..., :3][f[..., 3] > 127]
            e = Wfx[take][b % 40]
            m = e[..., 3] > 0
            cv[WBOX[1]:WBOX[3], WBOX[0]:WBOX[2]][m] = e[..., :3][m]
        x0, y0, x1, y1 = CROPS[nm]
        return cv[y0:y1, x0:x1]

    rows = ["butcher", "shade", "whisperer"]
    cw, ch, lab = 840, 480, 22
    dst = OUT / "camp-v4-takes.mp4"
    proc = subprocess.Popen([BL.FF, "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s",
                             "%dx%d" % (3 * cw + 16, 3 * (ch + lab)), "-r", str(FPS), "-i", "-", "-c:v", "libx264",
                             "-preset", "slow", "-crf", "18", "-pix_fmt", "yuv420p", "-movflags", "+faststart",
                             str(dst)], stdin=subprocess.PIPE)
    labels = Image.new("RGB", (3 * cw + 16, 3 * (ch + lab)), (18, 14, 20))
    d = ImageDraw.Draw(labels)
    for r, nm in enumerate(rows):
        for c, take in enumerate(TAKES[nm]):
            d.text((c * (cw + 8) + 6, r * (ch + lab) + 5), "%s %d: %s" % (nm, c + 1, take), fill=(239, 223, 190))
    lab_a = np.asarray(labels)
    keyf = {}
    for t in range(125):
        ms = t * 1000.0 / FPS
        fr = lab_a.copy()
        for r, nm in enumerate(rows):
            for c, take in enumerate(TAKES[nm]):
                im = cell(nm, take, ms).clip(0, 255).astype(np.uint8)
                big = np.repeat(np.repeat(im, 2, 0), 2, 1)
                fr[r * (ch + lab) + lab:r * (ch + lab) + lab + ch, c * (cw + 8):c * (cw + 8) + cw] = big
        proc.stdin.write(np.ascontiguousarray(fr).tobytes())
        if t in (12, 30, 45, 66, 80):
            keyf[t] = fr
    proc.stdin.close()
    proc.wait()
    Image.fromarray(keyf[30]).save(OUT / "camp-v4-takes-30.png")
    # the sheet: for each take the strongest moment, picked by the effect clocks above
    pick = {"butcher": [(8, "m1"), (25, "m2"), (48, "m3")], "shade": [(22, "ghost step"), (24, "rift mine"),
                                                                   (30, "mirror")],
            "whisperer": [(19, "pod grows a fighter"), (19, "dresses a fighter"), (12, "ghost copies")]}
    sheet = Image.new("RGB", (3 * 428, 3 * 262), (18, 14, 20))
    d = ImageDraw.Draw(sheet)
    for r, nm in enumerate(rows):
        for c, (b, take) in enumerate(pick[nm]):
            im = cell(nm, take, b * durs[nm] + 1).clip(0, 255).astype(np.uint8)
            sheet.paste(Image.fromarray(im), (c * 428, r * 262 + 20))
            d.text((c * 428 + 4, r * 262 + 4), "%s %d: %s" % (nm, c + 1, take), fill=(239, 223, 190))
    sheet.save(OUT / "camp-v4-takes-sheet.png")
    print("takes video MB %.1f" % (dst.stat().st_size / 1e6), flush=True)


if __name__ == "__main__":
    if "takes" in sys.argv[1:]:
        takes_video()
    else:
        sk = json.loads((OUT / "split.json").read_text())
        hz = np.array([200.0, 228, 246])
        ships(hz)
        l3rgb, l3al, ring, rem_pal, placed = layer3_v4()
        B = butcher()
        butcher_fx(B)
        shade_fx()
        whisperer_fx(l3rgb)
        anchor_parts(l3rgb)
        case_glyphs(l3rgb)
