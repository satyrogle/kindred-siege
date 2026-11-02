import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
import stops_grade as SG

# refine_assets.py far | cuts | crypt | shadows | all
# Jacob, 22 Oct: "with this new insights can you do the new pass ?" then "go". The report's passes 1 to 3 on a copy of the
# road page (Art/Studies/Bloodridge-Road-Refine-20261022/page/):
#   far:  a new far layer drawn as flat bands (sky, two ridges, one court, haze) from a nine colour ramp added to the
#         master palette, wide enough for the fastest drift option, no mirror.
#   cuts: three sky window layouts cut into the world layer along the stonework's own dark mortar lines (a least cost
#         seam), stops 1 to 3 only; the court is untouched this pass.
#   crypt: the crypt cleaned end to end (head band and floor despeckled), with three floor textures to pick from.
#   shadows: flat contact shadow ellipses in one dark floor colour, hard edged.
REPO = Path("C:/Users/jacob/KindredSiege-Clean")
OLD = REPO / "Art/Studies/Bloodridge-Road-Layers-20261021/page"
OUT = REPO / "Art/Studies/Bloodridge-Road-Refine-20261022"
PAGE = OUT / "page"
STRIP = REPO / "Art/Studies/Bloodridge-Stops-20261020/c3sky3-strip.png"
STOPS = [0, 1376, 2376, 3728]
TILT = [0, 40, 75, 0]           # the crypt moves from 30 to 75 so its floor line sits where the other stops' do
VW, VH = 1376, 768
FAR_W, FAR_H = 2122, 300         # view width plus the 1/5 drift over the 3728 px road
COURT_FAR_X = 1150               # the far court's centre in far layer pixels


def lab_rgb(L, C, h):
    a, b = C * np.cos(np.radians(h)), C * np.sin(np.radians(h))
    rgb = SG.lab_to_srgb(np.array([[[L, a, b]]], np.float64))[0, 0]
    return tuple(int(v) for v in np.clip(np.round(rgb), 0, 255))


# The far ramp: dusty rose, pulled toward the sky, hue shifted (cooler in shade), ridge values inside 8 L*.
FAR = {
    "sky0": lab_rgb(77.5, 14.0, 38), "sky1": lab_rgb(80.5, 13.0, 41), "sky2": lab_rgb(83.5, 11.0, 44),
    "haze": lab_rgb(78.0, 10.0, 36), "city": lab_rgb(74.5, 10.0, 32), "far": lab_rgb(72.0, 9.0, 28),
    "lit": lab_rgb(68.5, 9.0, 34), "ridge": lab_rgb(65.0, 10.0, 26), "dark": lab_rgb(60.5, 10.0, 18),
}


def strip_palette():
    s = np.asarray(Image.open(STRIP).convert("RGB")).reshape(-1, 3)
    return np.vstack([np.unique(s, axis=0).astype(np.int32), np.array([[24, 18, 24]], np.int32)])


def master_palette():
    return np.vstack([strip_palette(), np.array(list(FAR.values()), np.int32)])


# ---------------------------------------------------------------- far layer
def far():
    rng = np.random.default_rng(2210)
    H, W = FAR_H, FAR_W
    a = np.zeros((H, W, 3), np.uint8)
    c = {k: np.array(v, np.uint8) for k, v in FAR.items()}
    xs = np.arange(W)

    def checker(y, top, bot):
        a[y, (xs + y) % 2 == 0] = c[top]
        a[y, (xs + y) % 2 == 1] = c[bot]

    # sky: three flat bands, one checker row at each join (the piece's only dither)
    a[0:38] = c["sky0"]
    a[38:76] = c["sky1"]
    a[76:] = c["sky2"]
    checker(37, "sky0", "sky1")
    checker(75, "sky1", "sky2")

    # back ridge: low, smooth, lighter (closest to the haze)
    yb = np.round(106 + 5 * np.sin(xs / 173.0) + 3 * np.sin(xs / 59.0 + 1.3) + 2 * np.sin(xs / 23.0 + 0.4)).astype(int)
    for x in range(W):
        a[yb[x]:, x] = c["far"]

    # near ridge: terraced plateaus with short stepped slopes over a broad rise that peaks under the court
    def lvl(x):
        hill = 18 * max(0.0, 1 - abs(x - COURT_FAR_X) / 340.0)
        return 128 + 6 * np.sin(x / 211.0) + 4 * np.sin(x / 89.0 + 0.7) - hill

    yn = np.zeros(W + 400, int)
    x, level = 0, 128
    while x < W:
        run = int(rng.integers(40, 150))
        if abs(x + run // 2 - COURT_FAR_X) < 90:
            target, run = int(round(lvl(COURT_FAR_X))), 150
        else:
            target = int(round(lvl(x + run // 2))) + int(rng.integers(-3, 4))
        steps = abs(target - level)
        for i in range(steps):           # one pixel up or down every 2 to 4 columns
            w = int(rng.integers(2, 5))
            level += 1 if target > level else -1
            yn[x:x + w] = level
            x += w
        yn[x:x + run] = level
        x += run
    # eroded buttes standing off the crest, away from the court
    for bx in (420, 905, 1490, 1760):
        bw, bh = int(rng.integers(18, 36)), int(rng.integers(7, 13))
        yn[bx:bx + bw] = np.minimum(yn[bx:bx + bw], yn[bx:bx + bw].min() - bh)
    yn = yn[:W]
    for x in range(W):
        a[yn[x]:, x] = c["ridge"]
        a[yn[x], x] = c["lit"]                      # lit plateau edge (light from the upper left)
        if x > 0 and yn[x] < yn[x - 1]:             # a rise: its left face catches the light
            a[yn[x]:yn[x - 1] + 1, x] = c["lit"]
        if x > 0 and yn[x] > yn[x - 1]:             # a drop: its face is in shade
            a[yn[x - 1]:yn[x] + 1, x - 1] = c["dark"]

    # burial terraces: broken ledge lines (lit over shade) and a few tomb doors under them
    for k in (12, 25, 37):
        x = int(rng.integers(0, 40))
        while x < W:
            seg = int(rng.integers(30, 130))
            for xx in range(x, min(W, x + seg)):
                y = yn[xx] + k
                if y + 1 < 168:
                    a[y, xx] = c["lit"]
                    a[y + 1, xx] = c["dark"]
            d = x + int(rng.integers(8, max(9, seg - 8)))
            if d < W - 4 and yn[d] + k + 6 < 168 and rng.random() < 0.7:
                a[yn[d] + k + 2:yn[d] + k + 6, d:d + 3] = c["dark"]
            x += seg + int(rng.integers(12, 50))

    # haze band where the ridge meets the plain, with faint far city blocks
    yh = np.round(168 + 3 * np.sin(xs / 97.0 + 0.7)).astype(int)
    for x in range(W):
        a[yh[x]:, x] = c["haze"]
        if (x + yh[x]) % 2 == 0:
            a[yh[x] - 1, x] = c["haze"]
    x = int(rng.integers(0, 20))
    while x < W:
        w, h = int(rng.integers(3, 12)), int(rng.integers(3, 13))
        a[205 - h:205, x:x + w] = c["city"]
        x += w + int(rng.integers(2, 26))
    a[205:207] = c["city"]
    checker(207, "city", "haze")

    # the chief's court: a stepped pyramid with a centre stair and the gate on top, lit left, shaded right
    base = int(yn[COURT_FAR_X - 60:COURT_FAR_X + 60].min())
    half, y = 62, base
    for step in range(7):
        y0 = y - 4
        for xx in range(COURT_FAR_X - half, COURT_FAR_X + half):
            a[y0:y, xx] = c["lit"] if xx < COURT_FAR_X else c["ridge"]
        a[y0, COURT_FAR_X - half:COURT_FAR_X + half] = c["lit"]
        a[y0:y, COURT_FAR_X + half - 1] = c["dark"]
        half -= 8
        y = y0
    a[y:base, COURT_FAR_X - 3:COURT_FAR_X + 3] = c["lit"]
    a[y:base, COURT_FAR_X + 2] = c["dark"]
    gx0, gx1, gtop = COURT_FAR_X - 8, COURT_FAR_X + 8, y - 13
    a[gtop:y, gx0:gx0 + 2] = c["dark"]
    a[gtop:y, gx1 - 2:gx1] = c["dark"]
    a[gtop:gtop + 2, gx0 - 1:gx1 + 1] = c["dark"]
    # small gate markers along the crest
    for mx in (240, 610, 1530, 1880):
        my = yn[mx]
        a[my - 5:my, mx - 3] = c["dark"]
        a[my - 5:my, mx + 3] = c["dark"]
        a[my - 6, mx - 4:mx + 5] = c["dark"]

    im = Image.fromarray(a)
    im.save(PAGE / "l1-far.png")
    cols = len(np.unique(a.reshape(-1, 3), axis=0))
    print("far layer", im.size, "colours", cols, "court top", gtop, "ridge base", base)
    return {"far_size": [W, H], "court_far_x": COURT_FAR_X, "court_top": int(gtop), "colours": cols}


# ---------------------------------------------------------------- sky window cuts
# Targets in strip pixels: (x0, x1, target_y). Target 0 keeps the world at full height. KEEP: (x0, x1, max_y) that the
# seam may not pass below, so the secrets' anchors stay (the skeleton's niche, the ghoul's blocks, the crows' slab,
# the spider's slab).
KEEP = [(530, 640, 80), (2120, 2250, 144), (2290, 2420, 174)]
LAYOUTS = {
    1: [(0, 1756, 130), (1756, 1936, 0), (1936, 2600, 160), (2600, 2800, 185), (2800, 3270, 0), (3270, 3700, 185)],
    2: [(0, 230, 0), (230, 1240, 165), (1240, 1376, 0), (1376, 1936, 0), (1936, 2376, 190), (2376, 3270, 0),
        (3270, 3526, 205), (3526, 3728, 0)],
    3: [(0, 430, 140), (430, 780, 0), (780, 1376, 140), (1376, 1756, 180), (1756, 1936, 0), (1936, 2600, 180),
        (2600, 2800, 175), (2800, 3270, 0), (3270, 3700, 175)],
}
SEAM_W = 3728
SEAM_H = 300


def seam(E, yt, band, keep_max, D=6, wt=0.06, lam=0.01):
    """Least cost top edge, one y per column. E: (H, W) cost of a pixel being the new top edge (dark mortar cheap)."""
    H, W = E.shape
    cs = np.vstack([np.zeros((1, W)), np.cumsum(E, 0)])          # cs[y] = sum of E rows 0..y-1
    ys = np.arange(H)
    INF = 1e18
    dp = np.full((H, W), INF)
    back = np.zeros((H, W), np.int32)

    def local(x):
        t = wt * np.maximum(0, np.abs(ys - yt[x]) - band[x])
        v = E[:, x] + t
        v[ys > keep_max[x]] = INF
        return v

    dp[:, 0] = local(0)
    for x in range(1, W):
        best = np.full(H, INF)
        arg = np.zeros(H, np.int32)
        for dy in range(-D, D + 1):
            yp = ys - dy
            ok = (yp >= 0) & (yp < H)
            prev = np.full(H, INF)
            prev[ok] = dp[yp[ok], x - 1]
            if dy > 0:      # moving down: column x rows yp..y-1 become the cut's side
                side = np.where(ok, cs[np.clip(ys, 0, H), x] - cs[np.clip(yp, 0, H), x], 0)
            elif dy < 0:    # moving up: column x-1 rows y..yp-1 become the cut's side
                side = np.where(ok, cs[np.clip(yp, 0, H), x - 1] - cs[np.clip(ys, 0, H), x - 1], 0)
            else:
                side = 0
            cand = prev + side + lam * abs(dy)
            m = cand < best
            best[m] = cand[m]
            arg[m] = yp[m]
        dp[:, x] = best + local(x)
        back[:, x] = arg
    y = int(np.argmin(dp[:, W - 1]))
    out = np.zeros(W, np.int32)
    for x in range(W - 1, -1, -1):
        out[x] = y
        y = back[y, x]
    return out


def mortar_colour(strip, l2):
    """The commonest dark colour in the world layer's top rows: the stonework's own mortar."""
    top = strip[:SEAM_H, :SEAM_W].reshape(-1, 3).astype(np.int32)
    on = l2[:SEAM_H, :SEAM_W, 3].reshape(-1) > 0
    L = SG.srgb_to_lab(top.astype(np.float64)[None])[0][:, 0]
    pick = top[on & (L > 16) & (L < 27)]
    u, n = np.unique(pick, axis=0, return_counts=True)
    return u[np.argmax(n)]


def cuts():
    strip = np.asarray(Image.open(STRIP).convert("RGB")).astype(np.float64)
    l2 = np.asarray(Image.open(OLD / "l2-world.png")).copy()
    L = SG.srgb_to_lab(strip[:SEAM_H, :SEAM_W])[..., 0] / 100.0
    E = np.where(l2[:SEAM_H, :SEAM_W, 3] > 0, L, 0.0)
    edge = mortar_colour(strip.astype(np.int32), l2)
    report = {"edge_colour": [int(v) for v in edge]}
    for n, segs in LAYOUTS.items():
        rng = np.random.default_rng(700 + n)
        yt = np.zeros(SEAM_W)
        band = np.full(SEAM_W, 8.0)
        keep_max = np.full(SEAM_W, SEAM_H - 1)
        for x0, x1, ty in segs:
            if ty == 0:
                keep_max[x0:x1] = 0
                continue
            x = x0                      # broken top: blocks 16 to 56 px wide at their own heights
            while x < x1:
                w = int(rng.integers(16, 57))
                yt[x:min(x1, x + w)] = ty + int(rng.integers(-22, 12))
                x += w
        # next to a piece kept tall, the cut comes down in three block steps instead of one slope
        for i, (x0, x1, ty) in enumerate(segs):
            if ty == 0:
                continue
            if i > 0 and segs[i - 1][2] == 0:
                x = x0
                for f in (0.3, 0.6, 0.85):
                    w = int(rng.integers(18, 31))
                    yt[x:x + w] = int(ty * f)
                    x += w
            if i + 1 < len(segs) and segs[i + 1][2] == 0:
                x = x1
                for f in (0.3, 0.6, 0.85):
                    w = int(rng.integers(18, 31))
                    yt[x - w:x] = int(ty * f)
                    x -= w
        for x0, x1, my in KEEP:
            keep_max[x0:x1] = np.minimum(keep_max[x0:x1], my)
        s = seam(E, yt, band, keep_max, D=40)
        out = l2.copy()
        rows = np.arange(l2.shape[0])[:, None]
        cut = np.zeros(l2.shape[:2], bool)
        cut[:, :SEAM_W] = rows < s[None, :]
        out[cut, 3] = 0
        # a one pixel mortar edge on every new top and side, where the world is still opaque
        op = out[..., 3] > 0
        for x in range(SEAM_W):
            y = s[x]
            if y > 0 and op[y, x]:
                out[y, x, :3] = edge
            if x > 0 and s[x] != s[x - 1]:
                lo, hi = sorted((s[x], s[x - 1]))
                col = x - 1 if s[x] > s[x - 1] else x
                ys = np.arange(lo, hi)
                ys = ys[op[ys, col]]
                out[ys, col, :3] = edge
        Image.fromarray(out).save(PAGE / ("l2-world-s%d.png" % n))
        np.save(HERE / ("seam-s%d.npy" % n), s)
        report[n] = {"removed_px": int((cut & (l2[..., 3] > 0)).sum()), "max_seam": int(s.max())}
        print("sky layout", n, report[n], flush=True)
    return report


# ---------------------------------------------------------------- the crypt, cleaned end to end
CRYPT_X0, CRYPT_X1 = 2376, 3728      # the crypt's floor (the court's own floor starts at 3728)
HEAD = (2694, 3594, 259, 409)         # strip box of the band behind the back rank's heads at the crypt (tilt 75)
FIGHT_Y = 409                         # strip row where the fight band starts at the crypt


def pal_index(rgb, pal):
    key = (rgb[..., 0].astype(np.int64) << 16) | (rgb[..., 1].astype(np.int64) << 8) | rgb[..., 2]
    pk = (pal[:, 0].astype(np.int64) << 16) | (pal[:, 1].astype(np.int64) << 8) | pal[:, 2]
    order = np.argsort(pk)
    pos = np.clip(np.searchsorted(pk[order], key), 0, len(pk) - 1)
    idx = order[pos]
    miss = pk[idx] != key
    if miss.any():                    # anything off palette snaps to the nearest entry
        px = rgb[miss].astype(np.int32)
        idx[miss] = ((px[:, None, :] - pal[None, :, :]) ** 2).sum(2).argmin(1)
    return idx


def adopt(idx, mask, thresh=2, passes=4):
    """The Adopt Pixels rule: a pixel with fewer than `thresh` same colour neighbours (of 8) takes its neighbours'
    commonest colour. It only reuses colours already there, so nothing new enters the palette."""
    idx = idx.copy()
    H, W = idx.shape
    for _ in range(passes):
        p = np.pad(idx, 1, mode="edge")
        nb = np.stack([p[1 + dy:1 + dy + H, 1 + dx:1 + dx + W] for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dy or dx], -1)
        same = (nb == idx[..., None]).sum(-1)
        counts = np.stack([(nb == nb[..., j:j + 1]).sum(-1) for j in range(8)], -1)
        mode = np.take_along_axis(nb, counts.argmax(-1)[..., None], -1)[..., 0]
        change = mask & (same < thresh) & (mode != idx)
        if not change.any():
            break
        idx[change] = mode[change]
    return idx


def lone_share(idx, mask):
    p = np.pad(idx, 1, mode="edge")
    H, W = idx.shape
    same = np.zeros((H, W), bool)
    for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
        same |= p[1 + dy:1 + dy + H, 1 + dx:1 + dx + W] == idx
    return float((~same & mask).sum() / max(1, mask.sum()))


def tones(pal, idx, mask):
    """Light, mid, dark, gap and edge tones of a floor area, as palette indices already used there."""
    Ls = SG.srgb_to_lab(pal.astype(np.float64)[None])[0][:, 0]
    v = Ls[idx[mask]]
    used = np.isin(np.arange(len(pal)), np.unique(idx[mask]))
    pick = lambda q: int(np.argmin(np.abs(Ls - np.percentile(v, q)) + 1000 * ~used))
    return {"hi": pick(94), "light": pick(80), "mid": pick(50), "dark": pick(22), "gap": pick(5)}


def smooth_noise(H, W, cy, cx, rng):
    """Value noise: a coarse random grid blown up with bilinear interpolation (cells cy by cx pixels)."""
    gh, gw = H // cy + 2, W // cx + 2
    g = rng.random((gh, gw))
    yy = np.arange(H) / cy
    xx = np.arange(W) / cx
    y0, x0 = yy.astype(int), xx.astype(int)
    fy, fx = (yy - y0)[:, None], (xx - x0)[None, :]
    a = g[y0][:, x0]
    b = g[y0][:, x0 + 1]
    c = g[y0 + 1][:, x0]
    d = g[y0 + 1][:, x0 + 1]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def flagstones(H, W, y_top, t, rng):
    """Irregular flagstones in perspective: jittered seeds in courses, nearest seed under a squashed metric,
    dark joints where two stones meet, a lit top edge and a shaded bottom edge on each stone."""
    seeds, y = [], float(y_top)
    while y < H + 40:
        h = 15 + (y - y_top) * 0.055
        x = -rng.uniform(0, 2 * h)
        while x < W + 3 * h:
            sw = h * rng.uniform(1.8, 2.8)
            seeds.append((x + sw / 2 + rng.uniform(-0.2, 0.2) * sw, y + h / 2 + rng.uniform(-0.2, 0.2) * h, h))
            x += sw
        y += h
    seeds = np.array(seeds)
    tone_of = rng.choice([t["dark"], t["mid"], t["mid"], t["light"]], len(seeds))
    near = np.full((H, W), -1, np.int32)
    joint = np.zeros((H, W), bool)
    for y0 in range(y_top, H, 24):
        y1 = min(H, y0 + 24)
        cand = np.nonzero(np.abs(seeds[:, 1] - (y0 + y1) / 2) < 3 * seeds[:, 2] + 24)[0]
        yy, xx = np.mgrid[y0:y1, 0:W]
        dx = xx[..., None] - seeds[cand, 0]
        dy = (yy[..., None] - seeds[cand, 1]) * 2.3
        d = np.sqrt(dx * dx + dy * dy)
        o = np.argsort(d, -1)[..., :2]
        d1 = np.take_along_axis(d, o[..., :1], -1)[..., 0]
        d2 = np.take_along_axis(d, o[..., 1:2], -1)[..., 0]
        near[y0:y1] = cand[o[..., 0]]
        joint[y0:y1] = (d2 - d1) < 2.4
    out = np.where(near >= 0, tone_of[np.clip(near, 0, None)], t["mid"])
    up = np.roll(near, 1, 0)
    down = np.roll(near, -1, 0)
    out[(up != near) & ~joint] = t["hi"]
    out[(down != near) & ~joint] = t["dark"]
    out[joint] = t["gap"]
    cracked = rng.random(len(seeds)) < 0.08     # a few cracked stones
    for i in np.nonzero(cracked)[0]:
        cx, cy, h = seeds[i]
        for s in range(int(h * 0.8)):
            px, py = int(cx - h * 0.4 + s), int(cy - h * 0.2 + s * 0.35)
            if 0 <= py < H and 0 <= px < W and near[py, px] == i and not joint[py, px]:
                out[py, px] = t["gap"]
    return out, near, seeds


def crypt():
    pal = master_palette()
    l3 = np.asarray(Image.open(OLD / "l3-floor.png")).copy()
    sub = l3[:, CRYPT_X0:CRYPT_X1]
    H, W = sub.shape[:2]
    fl = sub[..., 3] > 0
    idx0 = pal_index(sub[..., :3], pal)
    rep = {}
    # 1. the band behind the heads: despeckle the floor part (all options) and the world base (all sky variants)
    hx0, hx1, hy0, hy1 = HEAD[0] - CRYPT_X0, HEAD[1] - CRYPT_X0, HEAD[2], HEAD[3]
    head = np.zeros((H, W), bool)
    head[hy0:hy1, hx0:hx1] = True
    rep["head_floor_lone_before"] = lone_share(idx0, head & fl)
    idx_h = adopt(idx0, head & fl, thresh=1, passes=2)
    rep["head_floor_lone_after"] = lone_share(idx_h, head & fl)
    for n in (1, 2, 3):
        w = np.asarray(Image.open(PAGE / ("l2-world-s%d.png" % n))).copy()
        ws = w[:, CRYPT_X0:CRYPT_X1]
        wi = pal_index(ws[..., :3], pal)
        on = head & (ws[..., 3] > 0)
        before = lone_share(wi, on)
        wi = adopt(wi, on, thresh=1, passes=2)
        ws[..., :3] = np.where(on[..., None], pal[wi], ws[..., :3])
        w[:, CRYPT_X0:CRYPT_X1] = ws
        Image.fromarray(w).save(PAGE / ("l2-world-s%d.png" % n))
        rep["head_world_lone_s%d" % n] = [round(before, 3), round(lone_share(wi, on), 3)]
    # 2. three textures where the ranks stand (fight band and below), each finished by the same despeckle
    rank = fl.copy()
    rank[:FIGHT_Y] = False
    t = tones(pal, idx_h, rank)
    rng = np.random.default_rng(2230)
    ys, xs = np.mgrid[0:H, 0:W]
    opts = {}
    # option 1: big flat flagstones in perspective where the ranks stand
    o1 = idx_h.copy()
    fs, near, seeds = flagstones(H, W, FIGHT_Y - 30, t, rng)
    inside = np.abs((seeds[:, 0] - 690) / 650.0) ** 4 + np.abs((seeds[:, 1] - 600) / 265.0) ** 4 < 1.0
    laid = rank & (near >= 0) & inside[np.clip(near, 0, None)]
    o1[laid] = fs[laid]
    opts[1] = o1
    # option 2: the small cobbles kept, their dark outlines and bright edges pulled to the stone around them
    Lp = SG.srgb_to_lab(pal.astype(np.float64)[None])[0][:, 0]
    Lm = Lp[idx_h]
    k = 4
    pad = np.pad(Lm, k, mode="edge")
    loc = np.zeros_like(Lm)
    for dy in range(-k, k + 1):
        for dx in range(-k, k + 1):
            loc += pad[k + dy:k + dy + H, k + dx:k + dx + W]
    loc /= (2 * k + 1) ** 2
    off = rank & ((Lm < loc - 6) | (Lm > loc + 8))
    used = np.isin(np.arange(len(pal)), np.unique(idx_h[rank]))
    o2 = idx_h.copy()
    near = np.abs(Lp[None, :] - loc[off][:, None]) + 1000 * ~used[None, :]
    o2[off] = np.argmin(near, -1)
    opts[2] = o2
    # option 3: packed earth in big soft patches where the ranks stand, cobbles only around the edges
    zone = rank & (np.abs((xs - 690) / 610.0) ** 4 + np.abs((ys - 600) / 245.0) ** 4
                   < 1.0 + 0.12 * (smooth_noise(H, W, 9, 9, rng) - 0.5))
    n3 = 0.75 * smooth_noise(H, W, 14, 40, rng) + 0.25 * smooth_noise(H, W, 5, 12, rng)
    earth = np.where(n3 < 0.36, t["dark"], np.where(n3 > 0.62, t["light"], t["mid"]))
    o3 = idx_h.copy()
    o3[zone] = earth[zone]
    peb = zone & (rng.random((H, W)) < 1 / 700.0)
    for py, px in zip(*np.nonzero(peb)):
        o3[py, px:px + 2] = t["hi"]
        if py + 1 < H:
            o3[py + 1, px:px + 2] = t["gap"]
    opts[3] = o3
    for n, o in opts.items():
        o = adopt(o, rank, thresh=1, passes=2)
        out = sub.copy()
        out[..., :3] = np.where(fl[..., None], pal[o], sub[..., :3])
        Image.fromarray(out).save(PAGE / ("l3-crypt-f%d.png" % n))
        fight = rank.copy()
        fight[FIGHT_Y + 320:] = False
        rep["fight_lone_f%d" % n] = round(lone_share(o, fight), 3)
    rep["fight_lone_before"] = round(lone_share(idx0, rank & (ys < FIGHT_Y + 320)), 3)
    rep["tones"] = {k2: "#%02x%02x%02x" % tuple(pal[v]) for k2, v in t.items()}
    print(json.dumps(rep, indent=1))
    return rep


# ---------------------------------------------------------------- shadows, palette colours for the secrets, manifest
def hexc(c):
    return "#%02x%02x%02x" % tuple(int(v) for v in c)


def feet_width(sheet, frame, anchor_y):
    im = np.asarray(Image.open(PAGE / sheet).convert("RGBA"))
    x, y, w, h = frame[:4]
    f = im[y:y + h, x:x + w, 3] > 0
    rows = np.nonzero(f.any(1))[0]
    if not len(rows):
        return 40
    y1 = min(rows.max() + 1, int(round(anchor_y)) + 2)
    band = f[max(0, y1 - max(6, (y1 - rows.min()) // 8)):y1]
    cols = np.nonzero(band.any(0))[0]
    return int(cols.max() - cols.min() + 1) if len(cols) else 40


def nearest(pal, rgb, used=None):
    d = ((pal.astype(np.int32) - np.array(rgb, np.int32)) ** 2).sum(1)
    return pal[int(np.argmin(d))]


def extras():
    M = json.loads((OLD / "manifest.js").read_text().split("=", 1)[1].rsplit(";", 1)[0])
    pal = master_palette()
    lab = SG.srgb_to_lab(pal.astype(np.float64)[None])[0]
    L, C, h = lab[:, 0], np.hypot(lab[:, 1], lab[:, 2]), np.degrees(np.arctan2(lab[:, 2], lab[:, 1])) % 360
    # the contact shadow: one opaque dark floor colour from the master palette
    floor = np.asarray(Image.open(OLD / "l3-floor.png"))
    fl = floor[..., 3] > 0
    fidx = pal_index(floor[..., :3][fl][::7], pal)
    u, n = np.unique(fidx, return_counts=True)
    cand = [i for i in u if 24 <= L[i] <= 31 and C[i] < 18]
    shadow = pal[max(cand, key=lambda i: n[list(u).index(i)])] if cand else pal[np.argmin(np.abs(L - 28) + 100 * (C > 18))]
    # glow colours come from the fighters' own sheets (the master palette includes the sprites' approved colours)
    sp = []
    for o in M["fighters"]["ours"]:
        a = np.asarray(Image.open(PAGE / o["idle"]["sheet"]).convert("RGBA")).reshape(-1, 4)
        sp.append(a[a[:, 3] > 0][:, :3])
    sp = np.unique(np.vstack(sp), axis=0)
    sl = SG.srgb_to_lab(sp.astype(np.float64)[None])[0]
    sL, sC, sh = sl[:, 0], np.hypot(sl[:, 1], sl[:, 2]), np.degrees(np.arctan2(sl[:, 2], sl[:, 1])) % 360
    red = (sh < 45) | (sh > 345)
    gold = (sh > 60) & (sh < 95)

    def pick(mask, Lt):
        i = np.nonzero(mask & (sC > 25))[0]
        return hexc(sp[i[np.argmin(np.abs(sL[i] - Lt))]]) if len(i) else None
    glow = {"red": [pick(red, 28), pick(red, 42), pick(red, 58)], "gold": [pick(gold, 45), pick(gold, 65), pick(gold, 82)]}
    strip_pal = strip_palette()
    colours = {
        "shadow": hexc(shadow), "glow": glow,
        "ash": [hexc(nearest(strip_pal, c)) for c in ((222, 206, 184), (178, 155, 134), (92, 78, 80))],
        "thread": hexc(nearest(strip_pal, (186, 174, 166))), "bone": hexc(nearest(strip_pal, (232, 220, 198))),
        "dirt": [hexc(nearest(strip_pal, c)) for c in ((106, 78, 60), (138, 106, 82))],
        "dust": [hexc(nearest(strip_pal, c)) for c in ((176, 158, 142), (150, 138, 140))],
        "outline": "#181218",
    }
    # each fighter's shadow: 1.25 times the width of its feet, a third as tall
    def sw(s):
        w = feet_width(s["sheet"], s["frames"][0], s["anchor"][1])
        w = int(np.clip(round(w * 1.25 / 2) * 2, 26, 92))
        return [w, max(6, int(round(w * 0.3)))]
    for o in M["fighters"]["ours"]:
        o["shadow"] = sw(o["idle"])
    M["fighters"]["leader"]["shadow"] = sw(M["fighters"]["leader"]["idle"])
    for party in M["fighters"]["theirs"]:
        for e in party:
            e["shadow"] = sw(e["idle"])
    M["tilt"] = TILT
    M.pop("backdrop_y", None)
    M.pop("court_haze_box", None)
    M["far"] = {"w": FAR_W, "h": FAR_H, "court_x": COURT_FAR_X}
    M["colours"] = colours
    M["crypt_patch_x"] = CRYPT_X0
    M["seams"] = {str(n): [int(v) for v in np.load(HERE / ("seam-s%d.npy" % n))] for n in (1, 2, 3)}
    M["template"] = {"floor_line": 353, "feet": [415, 565, 700], "head": [350, 250, 1250, 420], "fight": [350, 400, 1250, 720]}
    (PAGE / "manifest.js").write_text("window.MANIFEST = " + json.dumps(M) + ";\n")
    print(json.dumps(colours, indent=1))
    print("shadows ours", [o["shadow"] for o in M["fighters"]["ours"]], "leader", M["fighters"]["leader"]["shadow"])
    return {"colours": colours}


if __name__ == "__main__":
    PAGE.mkdir(parents=True, exist_ok=True)
    what = sys.argv[1]
    rep = {}
    if what in ("extras", "all"):
        rep["extras"] = None
    if what in ("far", "all"):
        rep["far"] = far()
    if what in ("cuts", "all"):
        rep["cuts"] = cuts()
    if what in ("crypt", "all"):
        rep["crypt"] = crypt()
    if what in ("extras", "all"):
        rep["extras"] = extras()
    (HERE / ("report-%s.json" % what)).write_text(json.dumps(rep, indent=1))
