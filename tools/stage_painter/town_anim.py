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
import town_paint as TP

# town_anim.py split | video | all
# Jacob, 28 Oct 2026: settlements on the two hills, the whole town alive, more than flying dinos (ships, bursts),
# more depth. split: the two hills leave layer 3 for layer 1 (they are the hills behind), the painted settlements
# (town_paint.py) are cut in over them, and far silhouettes of both tribes' lands go on the cloud sea. video: layer 2
# gets the fighters on both hills (existing sprites shrunk, idle, attack, hit and death clips on a seeded schedule),
# the tribes' ships crossing and circling, bombs and catapult shots with the PixelLab bursts, flames and smoke on
# burning tents, waving flags, plus the sky clouds, gap banks and pterodactyls as before. 15 s at 12 fps, so it loops.
C5, OUT = TP.C5, TP.OUT
FX = OUT / "fx"
W, H, FPS, SEC = TP.W, TP.H, BL.FPS, BL.SEC
N = FPS * SEC
TICK = 1000.0 / FPS
ZONES = [(236, 150, 444, 345), (952, 170, 1196, 345)]  # the Butcher's forge and blades, the Glutton's plates
HAZE = TP.HAZE
SKY = np.array([200.0, 228, 246])
NATIVE = {"sw": -1, "se": 1}  # which way each tribe's sheets face: Clarth sw = left, Bloodridge se = right
CLASS_H = {"warden": 1.0, "skirmisher": 0.92, "trapper": 0.92, "marksman": 0.86, "medic": 0.86, "chief": 1.2}
# (sheet, x, feet y, facing). The clash by the gap on each hill, and life in each town.
LEFT = [("clarth_marksman_sw", 506, 214, 1), ("clarth_trapper_sw", 518, 226, 1), ("clarth_warden_sw", 514, 246, 1),
        ("clarth_skirmisher_sw", 536, 240, 1), ("clarth_medic_sw", 520, 214, 1),
        ("blood_rot_warden_se", 590, 246, -1), ("blood_rot_skirmisher_se", 570, 242, -1),
        ("blood_rot_trapper_se", 598, 228, -1), ("blood_rot_skirmisher_se", 552, 246, -1),
        ("blood_rot_marksman_se", 606, 216, -1)]
RIGHT = [("clarth_skirmisher_sw", 868, 258, 1), ("clarth_warden_sw", 848, 244, 1), ("clarth_trapper_sw", 884, 228, 1),
         ("clarth_marksman_sw", 838, 216, 1),
         ("blood_rot_warden_se", 934, 258, -1), ("blood_rot_skirmisher_se", 946, 238, -1),
         ("blood_rot_trapper_se", 918, 222, -1), ("blood_rot_marksman_se", 950, 214, -1),
         ("blood_rot_medic_se", 904, 214, -1)]
TOWN = [("clarth_chief_sw", 128, 268, 1), ("clarth_medic_sw", 64, 294, 1), ("clarth_marksman_sw", 196, 296, 1),
        ("blood_rot_medic_se", 1232, 292, -1), ("blood_rot_warden_se", 1342, 264, -1),
        ("blood_rot_marksman_se", 1210, 260, -1)]
FLEET = [dict(name="barque_mid", y=150, dir=-1, pad=48, bombs=[(103, "big"), (150, "small")], trail=True),
         dict(name="skiff_mid", y=178, dir=1, pad=48, bombs=[(115, "big"), (156, "small")], trail=False),
         dict(name="barque_far", orbit=(690, 110, 180, 12), laps=1, phase=0.0, hz=0.45),
         dict(name="skiff_far", orbit=(700, 130, 150, 10), laps=-1, phase=2.2, hz=0.45)]
SHIP_FACE = {"barque": -1, "skiff": 1}
PICKS = {"barque_mid": (0, 72), "barque_far": (2, 36), "skiff_mid": (4, 64), "skiff_far": (5, 32)}
VOLLEYS = [(8, (852, 226)), (50, (900, 194)), (96, (852, 226)), (140, (900, 194))]


def profiles(a3, tan):
    """Per column: the hill's top row and the first sand row under it, read outside the station columns and
    interpolated across them (blade tips and plates would fake them)."""
    xs = np.arange(W)
    top = np.array([int(np.argmax(a3[:, x])) if a3[:, x].any() else H for x in range(W)])
    bot = top.copy()
    for x in range(W):  # the sand is the first run of 8 tan rows, not the first tan pixel (the rim's warm edge line)
        col = tan[top[x] + 4:345, x]
        run, found = 0, None
        for i, v in enumerate(col):
            run = run + 1 if v else 0
            if run >= 8:
                found = i - 7
                break
        bot[x] = top[x] + 4 + found if found is not None else top[x]
    ok = np.ones(W, bool)
    for x0, _, x1, _ in ZONES:
        ok[x0:x1 + 1] = False
    return (np.interp(xs, xs[ok], top[ok]).round().astype(int), np.interp(xs, xs[ok], bot[ok]).round().astype(int))


def flood(seed, allow):
    m = seed & allow
    while True:
        g = C5L.grow(m, 1) & allow
        if (g == m).all():
            return m
        m = g


def silhouettes(lay1):
    """Far landmarks of both tribes' lands on the cloud sea, flat shapes in the far peaks' own blues: the Clarth's
    giant shark skeleton and a bone spire on the left, the Bloodridge's pyramids, obelisks and a wall on the right."""
    base = 166
    ov = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(ov)
    fill, shade, lit = (138, 192, 226, 255), (120, 178, 220, 255), (160, 206, 230, 255)
    d.ellipse((70, 132, 176, 190), fill=fill)
    d.ellipse((100, 148, 118, 160), fill=shade)
    d.polygon([(150, 150), (202, 146), (176, 168)], fill=fill)
    for x, hgt in ((206, 58), (230, 50), (256, 54), (284, 44), (310, 36)):
        d.arc((x - 11, base - hgt, x + 11, base + hgt), 185, 355, fill=fill, width=3)
    d.polygon([(330, base), (336, 118), (342, base)], fill=shade)
    d.polygon([(1040, base), (1110, 116), (1180, base)], fill=shade)
    d.polygon([(1040, base), (1110, 116), (1110, base)], fill=fill)
    d.polygon([(1300, base), (1336, 142), (1372, base)], fill=shade)
    d.polygon([(1300, base), (1336, 142), (1336, base)], fill=fill)
    d.rectangle((1196, 158, 1296, base), fill=shade)
    d.rectangle((1196, 158, 1296, 160), fill=lit)
    for x, t in ((1222, 124), (1266, 132)):
        d.polygon([(x - 4, base), (x - 3, t + 6), (x, t), (x + 3, t + 6), (x + 4, base)], fill=shade)
    a = np.asarray(ov).astype(np.float64)
    m = a[..., 3] > 0
    m[base:] = False
    lay1[m] = a[..., :3][m] * 0.8 + HAZE * 0.2
    return m


def split():
    l3i = np.asarray(Image.open(C5 / "layer-3-floor.png").convert("RGBA")).astype(np.int64)
    a3 = l3i[..., 3] > 127
    r, g, b = l3i[..., 0], l3i[..., 1], l3i[..., 2]
    lu = C5L.lum(l3i)
    tan = a3 & (r > b + 25)
    top, bot = profiles(a3, tan)
    yy = np.arange(H)[:, None]
    band = a3 & (yy >= top[None, :]) & (yy < bot[None, :])
    hillcol = ((r >= g) & (b - g >= 2) & (lu > 140)) | ((b > r + 30) & (lu < 205))
    zone = C5L.boxes(ZONES)
    hill = flood(band & ~zone, band & (hillcol | ~zone))
    lay1 = np.asarray(Image.open(C5 / "layer-1-far.png").convert("RGB")).astype(np.float64)
    lay1[hill] = l3i[..., :3][hill]
    struct = np.zeros((H, W), bool)
    for side, (xa, xb) in (("L", (0, 660)), ("R", (758, W))):
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
        use = (hill | (zone & band)) & inside  # behind the stations layer 1 takes the painter's hill too
        st = flood(C5L.grow(use, 1) & solid, solid)
        lay1[use | st] = p[use | st]
        struct |= st
        print(side, "hill px", int(use.sum()), "structure px", int(st.sum()), flush=True)
    sil = silhouettes(lay1)
    Image.fromarray(lay1.clip(0, 255).round().astype(np.uint8)).save(OUT / "layer-1-far.png")
    l3n = l3i.copy()
    l3n[hill, 3] = 0
    Image.fromarray(l3n.astype(np.uint8), "RGBA").save(OUT / "layer-3-floor.png")
    Image.fromarray(((hill | struct) * 255).astype(np.uint8)).save(OUT / "layer-2-town-mask.png")
    (OUT / "hill-profile.json").write_text(json.dumps({"top": top.tolist(), "bottom": bot.tolist()}))
    print("hill px", int(hill.sum()), "silhouette px", int(sil.sum()), flush=True)


def shrink_fixed(a, s, haze, hk=0.08):
    """C5H.shrink without its crop: the whole frame canvas is shrunk, so every frame of a clip keeps one anchor."""
    al = a[..., 3] > 127
    w, h = max(1, round(a.shape[1] * s)), max(1, round(a.shape[0] * s))
    src = a[..., :3].astype(np.float64)

    def down(x):
        return np.asarray(Image.fromarray(x.astype(np.float32)).resize((w, h), Image.BOX)).astype(np.float64)

    den = down(al)
    col = np.dstack([down(src[..., c] * al) for c in range(3)]) / np.maximum(den[..., None], 1e-6)
    px = np.ascontiguousarray(a[al][:, :3]).astype(np.uint8)
    qp = np.array(Image.fromarray(px[None]).quantize(colors=10, method=Image.Quantize.MEDIANCUT).getpalette()[:30],
                  np.float64).reshape(-1, 3)
    col = qp[((col[:, :, None, :] - qp[None, None]) ** 2).sum(3).argmin(2)]
    r, g, b = src[..., 0], src[..., 1], src[..., 2]
    slum = r * 0.299 + g * 0.587 + b * 0.114
    ember = al & (r > 180) & (g > 50) & (g < 175) & (b < 100) & (slum > 105)
    bone = al & (slum > 190) & (np.abs(r - b) < 40)
    for acc, need in ((bone, 0.3), (ember, 0.07)):
        frac = down(acc)
        mean = np.dstack([down(src[..., c] * acc) for c in range(3)]) / np.maximum(frac[..., None], 1e-6)
        use = (frac >= need) & (den >= 0.45)
        col[use] = mean[use]
    out = np.zeros((h + 2, w + 2, 3))
    out[1:-1, 1:-1] = col
    m = np.zeros((h + 2, w + 2), bool)
    m[1:-1, 1:-1] = den >= 0.45
    ring = (np.asarray(Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3))) > 0) & ~m
    out[ring] = qp[qp.sum(1).argmin()] * 0.7
    m |= ring
    if haze is not None:
        out = out * (1 - hk) + haze * hk
    return out, m


STAND, CLIPS = {}, {}


def stand_h(base):
    if base not in STAND:
        ys = np.nonzero(C5H.sprite(base, "idle", 0)[..., 3] > 127)[0]
        STAND[base] = int(ys.max() - ys.min() + 1)
    return STAND[base]


def clip(base, anim, hpx, haze=None):
    """One sheet shrunk to a standing height, all frames cut by the union box so the feet and centre stay put."""
    key = (base, anim, hpx)
    if key in CLIPS:
        return CLIPS[key]
    meta = json.loads((C5H.SHEETS / ("%s_%s.json" % (base, anim))).read_text())["frames"]
    meta = meta if isinstance(meta, list) else list(meta.values())
    sheet = np.asarray(Image.open(C5H.SHEETS / ("%s_%s.png" % (base, anim))).convert("RGBA"))
    frames = [sheet[q["y"]:q["y"] + q["h"], q["x"]:q["x"] + q["w"]] for q in (f["frame"] for f in meta)]
    durs = np.array([f["duration"] for f in meta], float)
    ys, xs = np.nonzero(frames[0][..., 3] > 127)
    uy, ux = np.nonzero(np.any([f[..., 3] > 127 for f in frames], axis=0))
    y0, y1, x0, x1 = uy.min(), uy.max() + 1, ux.min(), ux.max() + 1
    s = hpx / stand_h(base)
    CLIPS[key] = dict(frames=[shrink_fixed(f[y0:y1, x0:x1], s, haze) for f in frames], cum=np.cumsum(durs),
                      total=float(durs.sum()), durs=durs.tolist(), ground=round((ys.max() + 1 - y0) * s) + 1,
                      cx=round(((xs.min() + xs.max()) / 2 - x0) * s) + 1)
    return CLIPS[key]


def frame_of(c, anim, local_ms, phase_ms):
    ms = (local_ms + phase_ms) % c["total"] if anim == "idle" else min(local_ms, c["total"] - 1)
    return min(int(np.searchsorted(c["cum"], ms, side="right")), len(c["frames"]) - 1)


def blit(cv, rgb, al, x0, y0, vis=None, hz=0.0, hzc=None, tint=None):
    h, w = al.shape
    x0, y0 = int(round(x0)), int(round(y0))
    xa, ya, xb, yb = max(0, x0), max(0, y0), min(W, x0 + w), min(H, y0 + h)
    if xa >= xb or ya >= yb:
        return
    sub = al[ya - y0:yb - y0, xa - x0:xb - x0]
    if vis is not None:
        sub = sub & vis[ya:yb, xa:xb]
    src = rgb[ya - y0:yb - y0, xa - x0:xb - x0]
    if hz > 0:
        src = src * (1 - hz) + hzc * hz
    if tint is not None:
        src = src * 0.3 + np.array(tint, float) * 0.7
    reg = cv[ya:yb, xa:xb]
    reg[sub] = src[sub]


def dot(cv, x, y, col, r=1):
    x, y = int(round(x)), int(round(y))
    cv[max(0, y - r):y + r + 1, max(0, x - r):x + r + 1] = col


def spark(cv, x, y, k):
    if k == 0:
        dot(cv, x, y, [255, 255, 255], 1)
    elif k == 1:
        dot(cv, x, y, [255, 170, 60], 0)
        for dx, dy in ((2, 0), (-2, 0), (0, 2), (0, -2)):
            dot(cv, x + dx, y + dy, [255, 210, 120], 0)
    else:
        dot(cv, x - 2, y - 1, [160, 50, 30], 0)
        dot(cv, x + 2, y + 1, [160, 50, 30], 0)


def cast(rng, haze):
    out = []
    for group, mortal in ((LEFT, True), (RIGHT, True), (TOWN, False)):
        for base, x, feet, face in group:
            cls, side = base.split("_")[-2], base.split("_")[-1]
            hpx = int(round((32 + (feet - 212) * 0.24) * CLASS_H[cls]))
            death_at = int(rng.integers(70, 150)) if mortal and rng.random() < 0.3 else None
            segs, t = [], 0
            while t < N:
                if death_at is not None and t >= death_at:
                    segs.append([t, "death", 0])
                    break
                anim = str(rng.choice(["idle", "attack", "hit"], p=[0.45, 0.4, 0.15]))
                if anim == "idle":
                    segs.append([t, "idle", int(rng.integers(0, 4000))])
                    t += int(rng.integers(8, 30))
                else:
                    segs.append([t, anim, 0])
                    t += int(math.ceil(clip(base, anim, hpx, haze)["total"] / TICK))
            for anim in ("idle", "attack", "hit", "death"):
                clip(base, anim, hpx, haze)
            out.append(dict(base=base, x=x, feet=feet, h=hpx, face=face, flip=face != NATIVE[side], segs=segs))
    return out


def draw_fighter(cv, f, t):
    t0, anim, phase = [s for s in f["segs"] if s[0] <= t][-1]
    c = clip(f["base"], anim, f["h"])
    rgb, al = c["frames"][frame_of(c, anim, (t - t0) * TICK, phase)]
    cx = c["cx"]
    if f["flip"]:
        rgb, al, cx = rgb[:, ::-1], al[:, ::-1], al.shape[1] - 1 - cx
    tint = (255, 255, 255) if (anim == "hit" and t == t0) else ((255, 90, 70) if (anim == "hit" and t == t0 + 2) else None)
    blit(cv, rgb, al, f["x"] - cx, f["feet"] - c["ground"], tint=tint)
    if anim == "attack" and 12 <= t - t0 < 15:
        spark(cv, f["x"] + f["face"] * int(f["h"] * 0.35), f["feet"] - f["h"] // 2, t - t0 - 12)


def cut_ships(haze):
    raw = np.asarray(Image.open(OUT / "ships-raw.png").convert("RGB")).astype(np.int64)
    bg = np.median(raw[4:24, 4:60].reshape(-1, 3), axis=0)
    out = {}
    for name, (i, wpx) in PICKS.items():
        (x0, y0, x1, y1), _ = TP.SHIPS[i]
        reg = raw[y0:y1, x0:x1]
        m = C5H.mfilter(np.abs(reg - bg).sum(2) > 45, ImageFilter.MedianFilter(3))
        ys, xs = np.nonzero(m)
        reg, m = reg[ys.min():ys.max() + 1, xs.min():xs.max() + 1], m[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
        rgb, al = shrink_fixed(np.dstack([reg, m * 255]).astype(np.uint8), wpx / m.shape[1], haze, 0.1)
        out[name] = (rgb, al)
        Image.fromarray(np.dstack([rgb, al * 255]).clip(0, 255).astype(np.uint8), "RGBA").save(
            OUT / ("layer-2-ship-%s.png" % name.replace("_", "-")))
    return out


def ship_pos(sh, w, t):
    if "orbit" in sh:
        cx, cy, rx, ry = sh["orbit"]
        ang = 2 * math.pi * (t / N) * sh["laps"] + sh["phase"]
        return cx + rx * math.cos(ang), cy + ry * math.sin(ang), (1 if -math.sin(ang) * sh["laps"] > 0 else -1)
    span = W + w + sh["pad"]
    s = span / SEC * t / FPS
    x = (W + w / 2 + sh["pad"] / 2 - s) if sh["dir"] < 0 else (-w / 2 - sh["pad"] / 2 + s)
    return x, sh["y"] + 2 * math.sin(2 * math.pi * t / 24), sh["dir"]


def ground(x):
    """Where a bomb lands: the hill top under that column (rough rows, read from the plans)."""
    if x < 230:
        return 296
    if x < 440:
        return 290
    if x < 660:
        return 244
    if x < 950:
        return 258
    if x < 1200:
        return 240
    return 288


def bombs(ships):
    out = []
    for sh in FLEET:
        if "orbit" in sh:
            continue
        w = ships[sh["name"]][1].shape[1]
        for tr, size in sh["bombs"]:
            x0, y0, d = ship_pos(sh, w, tr)
            gy = ground(x0)
            fall = math.sqrt((gy - y0) / 150.0)
            ti = tr + int(math.ceil(fall * FPS))
            out.append(dict(t=tr, x0=x0, y0=y0, dir=d, gy=gy, ti=ti, size=size, xi=x0 + d * 30 * fall))
    return out


def volleys(rng):
    return [dict(t=tr, src=[sx, sy], dst=[int(rng.integers(500, 586)), int(rng.integers(222, 247))], flight=18)
            for tr, (sx, sy) in VOLLEYS]


def fxload(name, scale=1.0):
    meta = json.loads((FX / (name + ".json")).read_text())["frames"]
    meta = meta if isinstance(meta, list) else list(meta.values())
    im = np.asarray(Image.open(FX / (name + ".png")).convert("RGBA")).astype(np.float64)
    fr = []
    for f in meta:
        q = f["frame"]
        a = im[q["y"]:q["y"] + q["h"], q["x"]:q["x"] + q["w"]]
        rgb, al = a[..., :3], a[..., 3] > 127
        if scale != 1.0:
            rgb, den = BL.box_shrink(rgb, al.astype(np.float64), max(1, round(al.shape[1] * scale)))
            al = den >= 0.5
        fr.append((rgb, al))
    pv = meta[0].get("pivot", {"x": 0.5, "y": 0.5})
    h, w = fr[0][1].shape
    return dict(frames=fr, ox=pv["x"] * w, oy=pv["y"] * h)


def fx_at(cv, fx, i, x, y, hz=0.0):
    if 0 <= i < len(fx["frames"]):
        rgb, al = fx["frames"][i]
        blit(cv, rgb, al, x - fx["ox"], y - fx["oy"], hz=hz, hzc=SKY)


def flag_frames(col, col2, mark):
    out = []
    for k in range(4):
        rgb, al = np.zeros((9, 11, 3)), np.zeros((9, 11), bool)
        for c in range(10):
            dy = int(round(1.2 * math.sin(2 * math.pi * (c / 10 + k / 4))))
            for rr in range(6):
                y = 1 + rr + dy
                rgb[y, c] = col2 if rr == 5 or c == 9 else col
                al[y, c] = True
        for c in (4, 5):
            for rr in (2, 3):
                dy = int(round(1.2 * math.sin(2 * math.pi * (c / 10 + k / 4))))
                rgb[1 + rr + dy, c] = mark
        out.append((rgb, al))
    return out


def video():
    lay1 = np.asarray(Image.open(OUT / "layer-1-far.png").convert("RGB")).astype(np.float64)
    l3i = np.asarray(Image.open(OUT / "layer-3-floor.png").convert("RGBA"))
    l3rgb, l3 = l3i[..., :3].astype(np.float64), l3i[..., 3] > 127
    shi = np.asarray(Image.open(C5 / "pal-shadows.png").convert("RGBA"))
    shrgb, shal = shi[..., :3].astype(np.float64), shi[..., 3] > 127
    gap = np.asarray(Image.open(C5 / "layer-2-gap-mask.png").convert("L")) > 127
    struct = np.asarray(Image.open(OUT / "layer-2-town-mask.png").convert("L")) > 127
    clean = np.asarray(Image.open(C5 / "camp-5-clean.png").convert("RGB")).astype(np.float64)
    _, sky, _ = BL.maps("5")
    plan = json.loads((C5 / "layer-2-plan.json").read_text())
    skyv = sky & ~l3 & ~struct
    clouds = BL.cloud_sprites(clean, sky & ~l3)
    banks = C5L.gap_banks(lay1, gap)
    pt = {s: BL.ptero_frames(s) for s in ("near", "mid", "far")}
    haze = clean[sky].mean(0)
    pals = C5L.pal_sheets()
    rng = np.random.default_rng(20261028)
    fighters = cast(rng, haze)
    ships = cut_ships(haze)
    drops = bombs(ships)
    shots = volleys(rng)
    big, small = fxload("ks-camp-war-explosion-32"), fxload("ks-camp-war-explosion-20")
    flame, ember, puff = fxload("ks-camp-town-flame"), fxload("ks-camp-town-flame", 0.5), fxload("ks-camp-smoke-puff", 0.6)
    fires = [tuple(p) for p in TP.CLARTH["fires"] + TP.BLOOD["fires"]]
    braziers = [tuple(p) for p in TP.BLOOD["braziers"]]
    flags = [(x + 1, top, "C") for x, _, top in TP.CLARTH["poles"]] + [(x + 1, top, "B") for x, _, top in TP.BLOOD["poles"]]
    flagf = {"C": flag_frames([176, 58, 48], [122, 38, 36], [236, 226, 206]),
             "B": flag_frames([124, 100, 80], [86, 68, 56], [140, 30, 28])}
    for k, fr in flagf.items():
        Image.fromarray(np.hstack([np.dstack([rgb, al * 255]) for rgb, al in fr]).astype(np.uint8), "RGBA").save(
            OUT / ("layer-2-flag-%s.png" % ("clarth" if k == "C" else "bloodridge")))
    (OUT / "fighters").mkdir(exist_ok=True)
    for (base, anim, hpx), c in CLIPS.items():
        fr = c["frames"]
        Image.fromarray(np.hstack([np.dstack([rgb, al * 255]) for rgb, al in fr]).clip(0, 255).astype(np.uint8),
                        "RGBA").save(OUT / "fighters" / ("%s_%s_h%d.png" % (base, anim, hpx)))
        (OUT / "fighters" / ("%s_%s_h%d.json" % (base, anim, hpx))).write_text(json.dumps(
            {"w": fr[0][1].shape[1], "h": fr[0][1].shape[0], "durations": c["durs"], "ground": c["ground"], "cx": c["cx"]}))
    (OUT / "layer-2-plan.json").write_text(json.dumps(
        {"fps": FPS, "seconds": SEC, "sky_clouds": plan["sky_clouds"], "gap_banks": plan["gap_banks"],
         "flyers": plan["flyers"], "shadows": plan["shadows"], "fighters": fighters, "ships": FLEET, "bombs": drops,
         "shots": shots, "fires": fires, "braziers": braziers, "flags": flags,
         "smoke": {"every": 5, "life": 16, "rise": 2.2, "drift": 0.7},
         "order": ["sky clouds (sky only)", "gap banks (gap only)", "far and mid pterodactyls", "far ships", "smoke",
                   "flags", "flames", "fighters by feet", "shots, bombs, bursts", "mid ships", "layer 3", "pal shadows",
                   "pals", "near pterodactyl"]}, indent=1))
    allv = np.ones((H, W), bool)

    def frame(t, only2=False):
        cv = np.zeros_like(lay1) if only2 else lay1.copy()
        for (rgb, al), (cx0, cy, v) in zip(clouds, plan["sky_clouds"]):
            span = W + al.shape[1]
            BL.paste(cv, rgb, al, (cx0 - v * t / FPS) % span - al.shape[1] / 2, cy, skyv)
        for bk in banks:
            for k in range(bk["n"]):
                s = (bk["v"] * t / FPS + k * bk["span"] / bk["n"]) % bk["span"]
                BL.paste(cv, bk["rgb"], bk["al"], 880 - s, bk["cy"], gap)
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
        for sh in FLEET:
            if "orbit" in sh:
                rgb, al = ships[sh["name"]]
                x, y, d = ship_pos(sh, al.shape[1], t)
                BL.paste(cv, rgb, al, x, y, allv, SKY, sh["hz"], d != SHIP_FACE[sh["name"].split("_")[0]])
        for i, (fx, fy) in enumerate(fires):
            for a in range(16):
                s = t - a
                if (s - 2 * i) % 5:
                    continue
                fx_at(cv, puff, min(a // 2, 7), fx + 0.7 * a + 2 * math.sin(0.5 * a + s), fy - 8 - 2.2 * a,
                      min(0.7, 0.15 + 0.035 * a))
        for i, (x, top, k) in enumerate(flags):
            rgb, al = flagf[k][(t // 3 + i) % 4]
            blit(cv, rgb, al, x, top - 1)
        for i, (fx, fy) in enumerate(fires):
            fx_at(cv, flame, (t + 3 * i) % 8, fx, fy)
        for i, (bx, by) in enumerate(braziers):
            fx_at(cv, ember, (t + 5 * i + 2) % 8, bx, by - 10)
        for f in sorted(fighters, key=lambda f: f["feet"]):
            draw_fighter(cv, f, t)
        for sh in shots:
            u = (t - sh["t"]) / sh["flight"]
            (sx, sy), (tx, ty) = sh["src"], sh["dst"]
            for k in range(3):
                uu = u - 0.04 * k
                if 0 <= uu < 1:
                    dot(cv, sx + (tx - sx) * uu, sy + (ty - sy) * uu - 70 * math.sin(math.pi * uu),
                        [[255, 200, 90], [230, 120, 40], [150, 60, 30]][k], 1 if k == 0 else 0)
            fx_at(cv, big, t - sh["t"] - sh["flight"], tx, ty - 6)
        for bm in drops:
            if bm["t"] <= t < bm["ti"]:
                tau = (t - bm["t"]) / FPS
                dot(cv, bm["x0"] + bm["dir"] * 30 * tau, bm["y0"] + 150 * tau * tau, [40, 30, 32], 1)
                dot(cv, bm["x0"] + bm["dir"] * 30 * tau, bm["y0"] + 150 * tau * tau - 2, [255, 150, 60], 0)
            fx_at(cv, big if bm["size"] == "big" else small, t - bm["ti"], bm["xi"], bm["gy"] - 6)
        for sh in FLEET:
            if "orbit" not in sh:
                rgb, al = ships[sh["name"]]
                x, y, d = ship_pos(sh, al.shape[1], t)
                if sh["trail"]:
                    for a in range(8):
                        tt = t - a
                        px, py, _ = ship_pos(sh, al.shape[1], tt)
                        dot(cv, px - d * (al.shape[1] * 0.42) + 0.6 * a, py + 2 + 0.5 * a + math.sin(tt) * 1.5,
                            [[255, 200, 90], [240, 130, 40], [160, 50, 30]][min(2, a // 3)], 0)
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

    dst = OUT / "camp-5-war.mp4"
    proc = subprocess.Popen([BL.FF, "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", "%dx%d" % (W, H),
                             "-r", str(FPS), "-i", "-", "-vf", "scale=%d:%d:flags=neighbor" % (W * 2, H * 2),
                             "-c:v", "libx264", "-preset", "slow", "-crf", "18", "-pix_fmt", "yuv420p",
                             "-movflags", "+faststart", str(dst)], stdin=subprocess.PIPE)
    keep = {}
    for t in range(N):
        out = frame(t).clip(0, 255).astype(np.uint8)
        proc.stdin.write(out.tobytes())
        if t in (40, 75, 120, 160):
            keep[t] = out
        if t % 60 == 0:
            print("frame", t, flush=True)
    proc.stdin.close()
    proc.wait()
    Image.fromarray(keep[75]).save(OUT / "camp-5-war-still.png")
    lk = Image.new("RGB", (2 * 704 * 2 + 8, 2 * (240 * 2 + 18)), (18, 14, 20))
    d = ImageDraw.Draw(lk)
    for j, t in enumerate((40, 120)):
        for i, box in enumerate(((0, 90, 704, 330), (672, 90, 1376, 330))):
            lk.paste(Image.fromarray(keep[t]).crop(box).resize((1408, 480), Image.NEAREST), (i * 1416, j * 498 + 18))
            d.text((i * 1416 + 4, j * 498 + 3), "frame %d  %s hill at 2x" % (t, "left" if i == 0 else "right"),
                   fill=(239, 223, 190))
    lk.save(OUT / "look-town-2x.png")
    chk = ((np.indices((H, W)).sum(0) // 16) % 2)[..., None] * 30 + 40
    parts = [("1  static far, hills and towns in it now", lay1), ("2  living, frame 75", frame(75, True)),
             ("3  floor and stations", np.where(l3[..., None], l3rgb, chk)), ("all, frame 75", keep[75].astype(np.float64))]
    ls = Image.new("RGB", (2 * 688 + 8, 2 * (384 + 18) + 8), (18, 14, 20))
    d = ImageDraw.Draw(ls)
    for j, (name, im) in enumerate(parts):
        x, y = (j % 2) * 696, (j // 2) * 410
        ls.paste(Image.fromarray(im.clip(0, 255).astype(np.uint8)).resize((688, 384), Image.BOX), (x, y + 18))
        d.text((x + 4, y + 3), name, fill=(239, 223, 190))
    ls.save(OUT / "camp-5-war-layers.png")
    print("video MB %.1f" % (dst.stat().st_size / 1e6), "fighters", len(fighters), "clips", len(CLIPS),
          "bombs", [(b["t"], b["ti"], round(b["xi"]), b["gy"]) for b in drops], flush=True)


if __name__ == "__main__":
    step = sys.argv[1] if len(sys.argv) > 1 else "all"
    if step in ("split", "all"):
        split()
    if step in ("video", "all"):
        video()
