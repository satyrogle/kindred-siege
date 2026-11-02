import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import barren_paint as BP
import camp5_layers as C5

RG = BP.RG

# camp5_hill.py [variants to repaint]
# Jacob, 27 Oct 2026: "after the wipeout the leader used a teleportation device and teleported the four guys ... to a
# safe place ... since Clarith was the one who came looking for us, so they're tribe and their encampment should be
# visible nearby and bloodrich is attacking them, so it should also showcase that ... make one animation layer where
# the two tribes are fighting each other on either side of the hill or something", then "Yes go" to Claude's plan: a
# lone hill out of the cloud sea in the gap of the rim, the Clarth encampment on its left side, Bloodridge raiders
# climbing its right side and the lines clashing on top, drawn with the existing sprites shrunk down. Three stills
# first, for his pick; the animation comes after. Variants named on the command line are repainted; the others reuse
# their saved painting. Only the hill joined to its foot is kept (the painter once floated a camp in the sky), and
# each hill has its own battle line, with feet on the hill's outline and, in the camp, on the ground under it.
O = C5.O / "hill"
W, H = BP.W, BP.H
SHEETS = BP.CA
RIM = 200  # the hill only shows above the rim; its foot stays in the clouds behind it
CAMP_DESC = ("the war camp of the Clarth desert hunters: a few small frames of giant sand shark bones covered with red "
             "hides, and red banners, tiny with distance")
HILLS = {
    "1": dict(name="MESA", seed=2026102811, camp=(590, 52, 700, 78),
              shape=[(500, 196), (522, 160), (545, 120), (572, 90), (596, 72), (850, 70), (872, 88), (898, 120),
                     (918, 160), (936, 196)],
              line=[568, 596, 628, 664, 700, 728, 744, 752, 770, 790, 812, 830, 858, 884, 906],
              desc="a big lone flat topped mesa of warm tan rock rising high out of the sea of white clouds in the "
                   "middle distance, filling the middle of the view behind the rim, pale and hazy with distance, its "
                   "flat top bare on the right"),
    "2": dict(name="CRAG", seed=2026102812, camp=(575, 86, 660, 112),
              shape=[(500, 196), (530, 160), (560, 128), (600, 112), (650, 106), (690, 84), (735, 56), (775, 80),
                     (820, 118), (870, 160), (905, 196)],
              line=[588, 610, 632, 652, 670, 690, 704, 712, 726, 740, 756, 772, 790, 812, 836],
              desc="a big lone steep crag of warm tan rock rising high out of the sea of white clouds in the middle "
                   "distance, filling the middle of the view behind the rim, a pointed top on the right and a broad "
                   "shoulder on its left, pale and hazy with distance"),
    "3": dict(name="TWIN", seed=2026102823, camp=(580, 80, 660, 104),
              shape=[(495, 196), (520, 156), (550, 122), (585, 100), (625, 92), (660, 98), (700, 112), (740, 96),
                     (780, 84), (820, 92), (858, 116), (890, 154), (915, 196)],
              line=[590, 612, 634, 656, 676, 694, 706, 712, 726, 742, 760, 778, 800, 824, 850],
              desc="a big lone hill of warm tan rock with two rounded tops and a saddle between them, rising high out "
                   "of the sea of white clouds in the middle distance, filling the middle of the view behind the rim, "
                   "pale and hazy with distance"),
}
# The battle for the stills, in the order of each hill's line: (sprite, animation, frame, standing height in px).
# Clarth hold the left by their camp, Bloodridge come from the right; the two skirmishers meet at the clash. Heights
# keep the size ladder: Warden 20, melee 18, ranged and support 16. Weapons out wherever a frame has them.
FIGHTERS = [("clarth_medic_sw", "idle", 0, 16), ("clarth_marksman_sw", "attack", 22, 16),
            ("clarth_trapper_sw", "attack", 12, 18), ("clarth_warden_sw", "attack", 16, 20),
            ("clarth_warden_sw", "idle", 0, 20), ("clarth_skirmisher_sw", "attack", 20, 18),
            ("clarth_skirmisher_sw", "death", 99, 18),
            ("blood_rot_skirmisher_se", "attack", 20, 18), ("blood_rot_warden_se", "death", 99, 20),
            ("blood_rot_trapper_se", "attack", 12, 18), ("blood_rot_skirmisher_se", "idle", 10, 18),
            ("blood_rot_warden_se", "attack", 16, 20), ("blood_rot_marksman_se", "attack", 22, 16),
            ("blood_rot_warden_se", "idle", 6, 20), ("blood_rot_skirmisher_se", "idle", 0, 18)]


def prompt(v):
    p = json.loads(BP.prompt(BP.LANDS["5"]))
    xs = [x for x, _ in v["shape"]]
    ys = [y for _, y in v["shape"]]
    p["high_level_description"] += (" In the middle distance, framed by the gap in the rim, " + v["desc"] + ", with " +
                                    CAMP_DESC + " on its left side.")
    p["compositional_deconstruction"]["elements"] += [
        {"type": "obj", "bbox": BP.bb((min(xs), min(ys), max(xs), RIM)), "desc": v["desc"]},
        {"type": "obj", "bbox": BP.bb(v["camp"]), "desc": CAMP_DESC}]
    return json.dumps(p)


def poly(pts):
    m = Image.new("L", (W, H), 0)
    ImageDraw.Draw(m).polygon(pts, fill=255)
    return np.asarray(m) > 0


def mfilter(m, f):
    return np.asarray(Image.fromarray((m * 255).astype(np.uint8)).filter(f)) > 127


def init(v, base, l3):
    """The camp as painted, with the hill blocked in behind the rim: lit tan on the left, hazy violet on the right,
    a red spot where the encampment goes."""
    a = base.astype(np.float64).copy()
    hill = poly(v["shape"]) & ~l3
    xx = np.ones((H, 1)) * np.arange(W)[None, :]
    cx = np.mean([x for x, _ in v["shape"]])
    t = np.clip((xx - (cx - 150)) / 300, 0, 1)[..., None]
    haze = np.array([214.0, 226, 238])
    col = (np.array([214.0, 196, 160]) * (1 - t) + np.array([150.0, 140, 150]) * t) * 0.7 + haze * 0.3
    a[hill] = col[hill]
    m = Image.new("L", (W, H), 0)
    ImageDraw.Draw(m).ellipse(v["camp"], fill=255)
    camp = (np.asarray(m) > 0) & ~l3
    a[camp] = np.array([170.0, 70, 60]) * 0.75 + haze * 0.25
    rng = np.random.default_rng(v["seed"])
    small = rng.normal(0, 10, (H // 16 + 2, W // 16 + 2, 3)).astype(np.float32)
    a += np.dstack([np.asarray(Image.fromarray(small[..., c]).resize((W, H), Image.BICUBIC)) for c in range(3)])
    return Image.fromarray(a.clip(0, 255).round().astype(np.uint8))


def cut(out, v, l3):
    """The hill out of the painter's whole new picture: solid, not cloud and not blue (sky, peaks and cloud shadow
    are all blue), near the blocked in shape and above the rim; small holes closed (white bones, banners); and only
    what is joined to the hill's foot at the rim, so nothing floats. The ground line is the hill's outline, except in
    the camp, where it is the first rock at the bottom of the camp box, under the bone frames and hides."""
    a = np.asarray(out).astype(np.int64)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    lu = C5.lum(a)
    lav = (r >= g) & (b - g >= 4) & (lu > 150)
    tan = r > b + 25
    grey = (np.abs(b - r) < 10) & (lu < 200)
    cloud = (r >= 115) & (b >= r - 2) & ~lav & ~tan & ~grey
    blue = b > r + 30
    above = (np.arange(H)[:, None] < RIM) & np.ones((1, W), bool)
    zone = C5.grow(poly(v["shape"]), 24) & ~l3 & above
    solid = mfilter(zone & ~cloud & ~blue, ImageFilter.MedianFilter(5))
    solid = mfilter(mfilter(solid, ImageFilter.MaxFilter(5)), ImageFilter.MinFilter(5)) & zone
    hill = np.zeros_like(solid)
    hill[RIM - 4:RIM] = solid[RIM - 4:RIM]
    while True:
        grown = C5.grow(hill, 1) & solid
        if (grown == hill).all():
            break
        hill = grown
    cx0, _, cx1, cy1 = v["camp"]
    gt = np.full(W, -1)
    for x in range(W):
        ys = np.nonzero(hill[:RIM, x])[0]
        if cx0 <= x <= cx1:
            ys = ys[ys >= cy1 - 3]
        if len(ys):
            gt[x] = ys.min()
    return hill, gt


def sprite(base, anim, i):
    meta = json.loads((SHEETS / ("%s_%s.json" % (base, anim))).read_text())["frames"]
    meta = meta if isinstance(meta, list) else list(meta.values())
    q = meta[min(i, len(meta) - 1)]["frame"]
    a = np.asarray(Image.open(SHEETS / ("%s_%s.png" % (base, anim))).convert("RGBA"))
    return a[q["y"]:q["y"] + q["h"], q["x"]:q["x"] + q["w"]]


def shrink(a, s, haze):
    """Box shrink with the alpha as weight, colours snapped back to the frame's own 10, the small accents kept (a
    Bloodridge ember or a Clarth bone mask is a few pixels and would average away), a thin dark outline so the figure
    reads on the rock, then a little haze for the distance."""
    al = a[..., 3] > 127
    ys, xs = np.nonzero(al)
    a, al = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1], al[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    w, h = max(1, round(a.shape[1] * s)), max(1, round(a.shape[0] * s))
    src = a[..., :3].astype(np.float64)

    def down(x):
        return np.asarray(Image.fromarray(x.astype(np.float32)).resize((w, h), Image.BOX)).astype(np.float64)

    rgb = src * al[..., None]
    den = down(al)
    col = np.dstack([down(rgb[..., c]) for c in range(3)]) / np.maximum(den[..., None], 1e-6)
    qp = np.array(Image.fromarray(np.ascontiguousarray(a[..., :3])).quantize(colors=10, method=Image.Quantize.MEDIANCUT)
                  .getpalette()[:30], np.float64).reshape(-1, 3)
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
    out = np.zeros((h + 2, w + 2, 4))
    out[1:-1, 1:-1, :3] = col
    out[1:-1, 1:-1, 3] = den >= 0.45
    m = out[..., 3] > 0
    ring = (np.asarray(Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3))) > 0) & ~m
    out[ring, :3] = qp[qp.sum(1).argmin()] * 0.7
    out[ring, 3] = 1
    out[..., :3] = out[..., :3] * 0.88 + haze * 0.12
    return out


def battle(v, gt, haze):
    """The still's fighters as an RGBA layer, both sides turned to face each other. Feet go on the lowest ground under
    the middle of the figure, so on a slope it leans into the hill instead of hanging off it."""
    lay = np.zeros((H, W, 4))
    placed = []
    for (base, anim, i, height), x in zip(FIGHTERS, v["line"]):
        under = [int(gt[c]) for c in range(x - 3, x + 4) if 0 <= c < W and gt[c] >= 0]
        if not under:
            continue
        stand = sprite(base, "idle", 0)
        ys = np.nonzero(stand[..., 3] > 127)[0]
        s = height / (ys.max() - ys.min() + 1)
        spr = shrink(sprite(base, anim, i), s, haze)[:, ::-1]
        placed.append((max(under) + 1, x, spr))
    for feet, x, spr in sorted(placed, key=lambda p: p[0]):
        h, w = spr.shape[:2]
        x0, y0 = x - w // 2, feet - h + 1
        reg = lay[max(0, y0):y0 + h, max(0, x0):x0 + w]
        sub = spr[max(0, -y0):max(0, -y0) + reg.shape[0], max(0, -x0):max(0, -x0) + reg.shape[1]]
        m = sub[..., 3] > 0
        reg[m] = sub[m]
    return lay, len(placed)


def make(k, repaint):
    v = HILLS[k]
    O.mkdir(parents=True, exist_ok=True)
    clean = np.asarray(Image.open(C5.O / "camp-5-clean.png").convert("RGB")).astype(np.int64)
    lay1 = np.asarray(Image.open(C5.O / "layer-1-far.png").convert("RGB")).astype(np.int64)
    l3 = np.asarray(Image.open(C5.O / "layer-3-floor.png").convert("RGBA"))[..., 3] > 127
    still = np.asarray(Image.open(C5.O / "camp-5-v2-still.png").convert("RGB")).astype(np.int64)
    rawp = O / ("hill-%s-raw.png" % k)
    if repaint or not rawp.exists():
        neg = RG.NEG
        for word in ("tents", "banners", "flags", "skeletons", "bones"):
            RG.NEG = RG.NEG.replace(", " + word, "")
        try:
            raw = BP.run("hill-%s" % k, init(v, clean, l3), prompt(v), v["seed"])
        finally:
            RG.NEG = neg
        raw.save(rawp)
    raw = Image.open(rawp).convert("RGB")
    hill, gt = cut(raw, v, l3)
    q = np.asarray(RG.clean(raw)).astype(np.int64)
    lay1v = lay1.copy()
    lay1v[hill] = q[hill]
    Image.fromarray(np.dstack([q, hill * 255]).astype(np.uint8), "RGBA").save(O / ("hill-%s-layer1.png" % k))
    haze = clean[~l3 & (np.arange(H)[:, None] < 140)].mean(0)
    bat, n = battle(v, gt, haze)
    Image.fromarray((np.dstack([bat[..., :3], bat[..., 3]]) * [1, 1, 1, 255]).clip(0, 255).astype(np.uint8),
                    "RGBA").save(O / ("hill-%s-battle.png" % k))
    back = lay1v.astype(np.float64)
    m = bat[..., 3] > 0
    back[m] = bat[..., :3][m]
    open_ = ~l3 & (np.abs(still - lay1).sum(2) == 0)
    new = still.copy()
    new[open_] = back[open_].round().astype(np.int64)
    Image.fromarray(new.astype(np.uint8)).save(O / ("camp-5-hill-%s.png" % k))
    print(k, v["name"], "hill px", int(hill.sum()), "fighters placed", n, "of", len(FIGHTERS), flush=True)


def sheet():
    tiles = []
    for k in sorted(HILLS):
        p = O / ("camp-5-hill-%s.png" % k)
        if p.exists():
            tiles.append((k, Image.open(p).convert("RGB")))
    zw, zh = 1040, 520
    s = Image.new("RGB", (zw, len(tiles) * (zh + 20)), (18, 14, 20))
    d = ImageDraw.Draw(s)
    for j, (k, im) in enumerate(tiles):
        s.paste(im.crop((450, 20, 970, 280)).resize((zw, zh), Image.NEAREST), (0, j * (zh + 20) + 18))
        d.text((4, j * (zh + 20) + 3), "%s  %s  (hill and battle at 2x)" % (k, HILLS[k]["name"]), fill=(239, 223, 190))
    s.save(O / "camp-5-hill-pick.png")


if __name__ == "__main__":
    for k in sorted(HILLS):
        make(k, k in sys.argv[1:])
    sheet()
    print("all made", flush=True)
