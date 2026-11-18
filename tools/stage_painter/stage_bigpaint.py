import json
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import stage_life as SL

SP, SR, RG = SL.SP, SL.SR, SL.RG

# stage_bigpaint.py [paint|cut|look] [CROP ...]
# Jacob, 4 Nov 2026: "re do all props the way you did ... painted them high and toned them down", then "go". The
# route he liked that morning on the pilgrims and traders ("these are great"): the stage's own crop round the props,
# blown up 4x, real shape starts drawn in, painted from step 3 on his GPU, each prop cut by BiRefNet at the big size,
# then shrunk to 1x with stage_life.shrink. This does the rest of the moving layer's props: the vultures, sailors,
# flying scarabs, sun moths, ember bats, bone birds and the sky whale. The old 1x cuts go to
# life-backup-20261104-props first; the new ones keep their names in the life folder, so stage_life.py plays them as
# they are. Every pose of a kind is shrunk in one go, so its wing beats share their 12 colours.
#   paint  paints the crops, cuts them, shrinks them into the life folder, then the look
#   cut    cuts and shrinks the painted crops again        look   look-props-4x.png only
S = 4
WORK = SL.LIFE / "props4x"
BAK = SL.OUT / "life-backup-20261104-props"
NEG = ", cartoon face, eyebrows, cute, chibi, mascot, smiling, angry cartoon eyes, comic, emoji, sticker"
FLY = "each with a clear head, body and wings"
WALK = "each with a clear head, body, arms and clothes"
# rect: the 1x stage crop painted at 4x. items: (kind, index) from stage_life.PATCHES, with a box moved where the
# old one sat on scenery (the flyers fly anywhere, so where they are painted only sets their light).
CROPS = {
    "1v": dict(k="1", rect=(32, 0, 376, 192), seed=2026110431, items=[("vulture", 0), ("vulture", 1), ("vulture", 2)],
               adds="three ash vultures flying in the sky", clear=FLY + " and tail"),
    "1s": dict(k="1", rect=(104, 110, 360, 270), seed=2026110432, items=[("sailor", 0), ("sailor", 1), ("sailor", 2)],
               adds="three travellers of the Thundercoast walking to the right beside the caravan", clear=WALK),
    "2": dict(k="2", rect=(624, 0, 880, 160), seed=2026110433, items=[("scarab", 0), ("scarab", 1), ("scarab", 2)],
              boxes={("scarab", 0): (650, 12, 690, 38), ("scarab", 1): (720, 44, 760, 70),
                     ("scarab", 2): (800, 12, 840, 38)},  # clear of the obelisk tops and the smoke
              adds="three flying scarabs in the sky", clear=FLY),
    "3": dict(k="3", rect=(40, 0, 296, 160), seed=2026110434, items=[("moth", 0), ("moth", 1), ("moth", 2)],
              adds="three sun moths in the sky", clear=FLY),
    "4a": dict(k="4", rect=(320, 0, 576, 160), seed=2026110435, items=[("bat", 0), ("bat", 2), ("bonebird", 0)],
               adds="two ember bats and a bone bird flying in the night sky", clear=FLY),
    "4b": dict(k="4", rect=(576, 0, 832, 160), seed=2026110436, items=[("bat", 1), ("bonebird", 1), ("bonebird", 2)],
               adds="an ember bat and two bone birds flying in the night sky", clear=FLY),
    "5": dict(k="5", rect=(944, 0, 1264, 180), seed=2026110437, items=[("whale", 0)],
              adds="one sky whale far off in the sky", clear="with a clear head, body, fins and tail", huge=True),
}


def items(cid):
    """(kind, i, 1x box, desc) for each prop in a crop."""
    C = CROPS[cid]
    out = []
    for kind, i in C["items"]:
        _, box, desc = [e for e in SL.PATCHES[C["k"]]["els"] if e[0] == kind][i]
        out.append((kind, i, C.get("boxes", {}).get((kind, i), box), desc))
    return out


# ------------------------------------------------------------------ the shape starts, drawn at 4x

def vulture(d, X, Y, desc):
    """stage_life.pose_blob's vulture, drawn at 4x: flying left, broad fingered wings in the box's beat."""
    dark, lit, head, neck = (46, 40, 44), (118, 102, 98), (206, 182, 160), (176, 156, 128)
    tip = 0.5 + SL.pose(desc) * 0.42
    for side in (-1, 1):
        sx, ex = 0.5 + side * 0.08, 0.5 + side * 0.5
        d.polygon([(X(sx), Y(0.38)), (X(ex), Y(tip - 0.08)), (X(ex - side * 0.06), Y(tip + 0.12)), (X(sx), Y(0.64))],
                  fill=dark)
        d.line([(X(sx), Y(0.38)), (X(ex), Y(tip - 0.08))], fill=lit, width=2 * S)
        for f in range(4):
            fx = ex - side * 0.045 * f
            d.line([(X(fx), Y(tip - 0.02 + 0.03 * f)), (X(fx + side * 0.05), Y(tip + 0.12 + 0.03 * f))], fill=dark,
                   width=S)
    d.ellipse((X(0.38), Y(0.38), X(0.64), Y(0.7)), fill=dark)
    d.line([(X(0.4), Y(0.4)), (X(0.6), Y(0.38))], fill=lit, width=S)
    d.line([(X(0.4), Y(0.5)), (X(0.3), Y(0.44))], fill=neck, width=2 * S)  # the bandaged neck
    d.ellipse((X(0.24), Y(0.36), X(0.33), Y(0.5)), fill=head)
    d.polygon([(X(0.62), Y(0.58)), (X(0.74), Y(0.52)), (X(0.74), Y(0.7))], fill=dark)


def bonebird(d, X, Y, desc):
    """stage_life.pose_blob's bone bird, drawn at 4x: flying left, wing bones with rag strips."""
    bone, shade, rag = (222, 210, 188), (150, 138, 120), (122, 92, 62)
    tip = 0.5 + SL.pose(desc) * 0.42
    d.line([(X(0.2), Y(0.5)), (X(0.78), Y(0.54))], fill=bone, width=2 * S)
    for r in range(5):
        rx = 0.36 + r * 0.05
        d.line([(X(rx), Y(0.5)), (X(rx) + S, Y(0.68))], fill=shade, width=S)
    d.ellipse((X(0.08), Y(0.34), X(0.22), Y(0.64)), fill=bone)
    d.ellipse((X(0.13) - S, Y(0.48) - S, X(0.13) + S, Y(0.48) + S), fill=(30, 26, 30))
    d.polygon([(X(0.08), Y(0.5)), (X(0.0), Y(0.56)), (X(0.08), Y(0.58))], fill=shade)
    for side in (-1, 1):
        sx, ex = 0.46 + side * 0.04, 0.5 + side * 0.36
        d.line([(X(sx), Y(0.46)), (X(ex), Y(tip))], fill=bone, width=2 * S)
        for s in range(3):
            t = 0.35 + s * 0.22
            px, py = X(sx) + (X(ex) - X(sx)) * t, Y(0.46) + (Y(tip) - Y(0.46)) * t
            hh = Y(1) - Y(0)
            d.polygon([(px - S, py + S), (px + 2 * S, py + S), (px + S, py + hh * 0.3), (px - S, py + hh * 0.26)],
                      fill=rag)
    for s in range(3):
        d.line([(X(0.78), Y(0.54)), (X(0.98), Y(0.5 + 0.14 * s))], fill=rag, width=S)


def bat(d, X, Y, desc):
    """Flying left: leathery wings with arm bones and a scalloped edge, ears, two ember eyes."""
    black, skin, eye = (24, 20, 26), (58, 44, 52), (255, 150, 60)
    tip = 0.5 + SL.pose(desc) * 0.45
    for side in (-1, 1):
        sx, mx, ex = 0.5 + side * 0.06, 0.5 + side * 0.28, 0.5 + side * 0.5
        d.polygon([(X(sx), Y(0.42)), (X(mx), Y(tip - 0.12)), (X(ex), Y(tip)), (X(0.5 + side * 0.42), Y(tip + 0.18)),
                   (X(0.5 + side * 0.34), Y(tip + 0.1)), (X(0.5 + side * 0.26), Y(tip + 0.22)),
                   (X(0.5 + side * 0.18), Y((tip + 0.66) / 2 + 0.06)), (X(sx), Y(0.66))], fill=skin)
        d.line([(X(sx), Y(0.42)), (X(mx), Y(tip - 0.12)), (X(ex), Y(tip))], fill=black, width=S)
    d.ellipse((X(0.4), Y(0.36), X(0.6), Y(0.74)), fill=black)
    d.ellipse((X(0.34), Y(0.3), X(0.5), Y(0.56)), fill=black)
    d.polygon([(X(0.35), Y(0.36)), (X(0.36), Y(0.1)), (X(0.41), Y(0.32))], fill=black)
    d.polygon([(X(0.42), Y(0.32)), (X(0.46), Y(0.1)), (X(0.49), Y(0.36))], fill=black)
    for ex_ in (0.38, 0.44):
        d.ellipse((X(ex_) - 0.7 * S, Y(0.42) - 0.7 * S, X(ex_) + 0.7 * S, Y(0.42) + 0.7 * S), fill=eye)


def moth(d, X, Y, desc):
    """Seen from above, head up: fore and hind wings with a dark red sun disc, the span set by the beat."""
    gold, lit, hind, disc, body = (210, 180, 110), (240, 214, 150), (184, 146, 88), (150, 40, 36), (90, 64, 40)
    span = 1.0 if "spread" in desc else 0.72 if "raised" in desc else 0.45
    for side in (-1, 1):
        o = 0.5 + side * 0.5 * span
        d.polygon([(X(0.5 + side * 0.04), Y(0.5)), (X(0.5 + side * 0.4 * span), Y(0.58)),
                   (X(0.5 + side * 0.3 * span), Y(0.94)), (X(0.5 + side * 0.04), Y(0.8))], fill=hind)
        d.polygon([(X(0.5 + side * 0.04), Y(0.28)), (X(o), Y(0.08)), (X(o - side * 0.06 * span), Y(0.52)),
                   (X(0.5 + side * 0.04), Y(0.56))], fill=gold)
        d.line([(X(0.5 + side * 0.04), Y(0.28)), (X(o), Y(0.08))], fill=lit, width=S)
        cx, cy, r = X(0.5 + side * 0.3 * span), Y(0.32), 0.09 * span * (X(1) - X(0))
        d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=disc)
    d.ellipse((X(0.45), Y(0.2), X(0.55), Y(0.88)), fill=body)
    d.ellipse((X(0.44), Y(0.12), X(0.56), Y(0.26)), fill=body)
    d.line([(X(0.47), Y(0.14)), (X(0.4), Y(0.0))], fill=body, width=S)
    d.line([(X(0.53), Y(0.14)), (X(0.6), Y(0.0))], fill=body, width=S)


def scarab(d, X, Y, desc):
    """Seen from above, head up: wing cases split open and thin amber wings out to the sides, span set by the beat."""
    shell, lit, wing, leg = (92, 64, 42), (176, 126, 64), (214, 170, 90), (40, 30, 26)
    span = 1.0 if "wide" in desc else 0.75 if "raised" in desc else 0.5
    for side in (-1, 1):
        for j in range(3):
            y = 0.5 + 0.12 * j
            d.line([(X(0.5 + side * 0.08), Y(y)), (X(0.5 + side * 0.2), Y(y + 0.08))], fill=leg, width=S)
        d.polygon([(X(0.5 + side * 0.06), Y(0.42)), (X(0.5 + side * 0.5 * span), Y(0.28)),
                   (X(0.5 + side * 0.48 * span), Y(0.6)), (X(0.5 + side * 0.08), Y(0.62))], fill=wing)
        d.polygon([(X(0.5), Y(0.4)), (X(0.5 + side * 0.22), Y(0.36)), (X(0.5 + side * 0.2), Y(0.88)),
                   (X(0.5 + side * 0.04), Y(0.82))], fill=shell)
        d.line([(X(0.5 + side * 0.02), Y(0.42)), (X(0.5 + side * 0.2), Y(0.38))], fill=lit, width=S)
    d.ellipse((X(0.4), Y(0.3), X(0.6), Y(0.54)), fill=shell)
    d.line([(X(0.42), Y(0.34)), (X(0.56), Y(0.32))], fill=lit, width=S)
    d.polygon([(X(0.42), Y(0.32)), (X(0.5), Y(0.12)), (X(0.58), Y(0.32))], fill=shell)
    d.line([(X(0.5), Y(0.2)), (X(0.5), Y(0.04))], fill=lit, width=S)


def whale(d, X, Y, desc):
    """Swimming left: a long grey body with a pale grooved belly, a long trailing fin, flukes, bronze barnacles."""
    grey, light, belly, dark, bronze = (150, 150, 158), (206, 206, 212), (196, 196, 200), (96, 96, 106), (150, 110, 60)

    def P(pts):
        return [(X(a), Y(b)) for a, b in pts]
    d.polygon(P([(0.78, 0.4), (0.9, 0.44), (0.9, 0.5), (0.78, 0.56)]), fill=grey)
    d.polygon(P([(0.88, 0.47), (1.0, 0.2), (0.95, 0.47), (1.0, 0.76)]), fill=grey)
    d.polygon(P([(0.02, 0.5), (0.06, 0.3), (0.16, 0.18), (0.36, 0.14), (0.6, 0.2), (0.78, 0.36), (0.86, 0.46),
                 (0.78, 0.58), (0.6, 0.72), (0.36, 0.82), (0.14, 0.78), (0.04, 0.66)]), fill=grey)
    d.polygon(P([(0.04, 0.62), (0.14, 0.74), (0.36, 0.78), (0.6, 0.68), (0.7, 0.62), (0.5, 0.64), (0.3, 0.66),
                 (0.12, 0.62)]), fill=belly)
    for i in range(5):
        d.line(P([(0.08 + 0.05 * i, 0.66), (0.2 + 0.06 * i, 0.74)]), fill=dark, width=S)
    d.line(P([(0.06, 0.3), (0.16, 0.18), (0.36, 0.14), (0.6, 0.2)]), fill=light, width=S)
    d.polygon(P([(0.26, 0.64), (0.34, 0.66), (0.52, 0.98), (0.44, 0.96)]), fill=grey)
    d.line(P([(0.34, 0.66), (0.52, 0.98)]), fill=dark, width=S)
    for bx, by in ((0.3, 0.22), (0.42, 0.19), (0.5, 0.25), (0.36, 0.29), (0.58, 0.28)):
        d.ellipse((X(bx - 0.02), Y(by - 0.04), X(bx + 0.02), Y(by + 0.04)), fill=bronze)
    d.ellipse((X(0.13) - S, Y(0.52) - S, X(0.13) + S, Y(0.52) + S), fill=(30, 28, 34))
    d.line(P([(0.03, 0.56), (0.16, 0.6)]), fill=dark, width=S)


def sailor(d, X, Y, desc):
    """This morning's standing Thundercoast figure, no staff: a long sea coat, a brass helmet, a brass belt."""
    robe, lit, acc, shade = (38, 50, 88), (80, 96, 140), (184, 142, 70), (34, 28, 32)
    d.polygon([(X(0.3), Y(0.24)), (X(0.7), Y(0.24)), (X(0.9), Y(0.95)), (X(0.1), Y(0.95))], fill=robe)
    d.line([(X(0.3), Y(0.24)), (X(0.1), Y(0.95))], fill=lit, width=S)
    d.line([(X(0.3), Y(0.24)), (X(0.7), Y(0.24))], fill=lit, width=S)
    d.rectangle((X(0.22), Y(0.95), X(0.42), Y(1.0)), fill=shade)
    d.rectangle((X(0.56), Y(0.95), X(0.76), Y(1.0)), fill=shade)
    hx0, hy0, hx1, hy1 = X(0.32), Y(0.0), X(0.72), Y(0.24)
    hw, hh = hx1 - hx0, hy1 - hy0
    d.ellipse((hx0, hy0 + hh * 0.25, hx1, hy1), fill=shade)
    d.chord((hx0 - S, hy0, hx1 + S, hy0 + hh * 0.9), 180, 360, fill=acc)
    d.line([(hx0, hy0 + hh * 0.2), (hx0 + hw * 0.5, hy0 + hh * 0.08)], fill=(236, 206, 128), width=S)
    d.line([(X(0.2), Y(0.55)), (X(0.8), Y(0.55))], fill=acc, width=S)
    d.ellipse((X(0.74), Y(0.42), X(0.94), Y(0.52)), fill=lit)


SHAPES = dict(vulture=vulture, bonebird=bonebird, bat=bat, moth=moth, scarab=scarab, whale=whale, sailor=sailor)


# ------------------------------------------------------------------ paint, cut, shrink

def make_init(cid):
    C = CROPS[cid]
    ox, oy = C["rect"][:2]
    base = Image.open(SL.OUT / ("stage-%s.png" % C["k"])).convert("RGB").crop(C["rect"])
    init = base.resize((base.width * S, base.height * S), Image.LANCZOS)
    d = ImageDraw.Draw(init)
    for kind, _, box, desc in items(cid):
        x0, y0, w, h = (box[0] - ox) * S, (box[1] - oy) * S, (box[2] - box[0]) * S, (box[3] - box[1]) * S
        SHAPES[kind](d, (lambda f, x0=x0, w=w: x0 + w * f), (lambda f, y0=y0, h=h: y0 + h * f), desc)
    return init


def prompt(cid):
    C = CROPS[cid]
    P, o = SL.PATCHES[C["k"]], SR.STAGES[C["k"]]
    return json.dumps({
        "high_level_description": "A close piece of a pixel art battle stage background: " + P["scene"] + ". In it: " +
                                  C["adds"] + ", " + C["clear"] + ", in the same pixel art, colours and light as "
                                  "everything around them. " + SP.STYLE,
        "style_description": {"medium": "pixel_art", "art_style": RG.L3.ART,
                              "aesthetics": "one coherent pixel art scene, coherent pixel clusters, clear readable "
                                            "figures, everything lit by the same light",
                              "lighting": o["light"], "color_palette": o["pal"]},
        "compositional_deconstruction": {"background": P["scene"],
                                         "elements": [{"type": "obj", "bbox": SL.pbb(b, C["rect"]), "desc": desc}
                                                      for _, _, b, desc in items(cid)]}})


def neg(cid):
    n = SL.neg_patch().replace(" close up, cropped,", "")
    if CROPS[cid].get("huge"):
        n = n.replace(" giant, colossal, huge figures,", "")
    return n + NEG


def paint(cid):
    WORK.mkdir(parents=True, exist_ok=True)
    init = make_init(cid)
    init.save(WORK / ("big-%s-init.png" % cid))
    old, SP.OUT = SP.OUT, WORK
    try:
        raw = SP.run("bigpaint-%s" % cid, init, prompt(cid), CROPS[cid]["seed"], step=3, neg=neg(cid))
    finally:
        SP.OUT = old
    if raw.size != init.size:
        print("painter gave", raw.size, "for", init.size, flush=True)
        raw = raw.resize(init.size, Image.LANCZOS)
    raw.save(WORK / ("big-%s-raw.png" % cid))


def cut(cid):
    """Each prop cut at the big size the morning's way (BiRefNet on its box grown 6 px, shown 2x; the border diff if
    BiRefNet finds little; the group nearest the box centre, big ones first), kept inside its box plus 4 px so a
    neighbour can't join it, on the 4x grid. Saved big, with its 1x spot, for shrink()."""
    C = CROPS[cid]
    raw = Image.open(WORK / ("big-%s-raw.png" % cid)).convert("RGB")
    a = np.asarray(raw).astype(np.float64)
    ox, oy = C["rect"][:2]
    spots = json.loads((WORK / "spots.json").read_text()) if (WORK / "spots.json").exists() else {}
    for kind, i, (x0, y0, x1, y1), _ in items(cid):
        X0, Y0, X1, Y1 = (x0 - ox) * S, (y0 - oy) * S, (x1 - ox) * S, (y1 - oy) * S
        g = 6 * S
        bx0, by0 = max(0, X0 - g), max(0, Y0 - g)
        bx1, by1 = min(raw.width, X1 + g), min(raw.height, Y1 + g)
        reg = a[by0:by1, bx0:bx1]
        crop = raw.crop((bx0, by0, bx1, by1))
        big = SL.cutout("bigpaint-%s-%s-%d" % (cid, kind, i), crop.resize((crop.width * 2, crop.height * 2),
                                                                           Image.LANCZOS))
        m = np.asarray(Image.fromarray((big * 255).astype(np.uint8)).resize(crop.size, Image.BOX)) > 127
        if m.sum() < 0.08 * m.size:
            print("BiRefNet found little for", kind, i, "so the border diff is used", flush=True)
            edge = np.concatenate([reg[:3].reshape(-1, 3), reg[-3:].reshape(-1, 3), reg[:, :3].reshape(-1, 3),
                                   reg[:, -3:].reshape(-1, 3)])
            m = np.abs(reg - np.median(edge, 0)).sum(2) > 60
        m[:, :max(0, X0 - 4 * S - bx0)] = False
        m[:, max(0, X1 + 4 * S - bx0):] = False
        m[:max(0, Y0 - 4 * S - by0)] = False
        m[max(0, Y1 + 4 * S - by0):] = False
        lab, n = SL.components(m)
        if n == 0:
            print("nothing cut for", kind, i, flush=True)
            continue
        cy, cx = (by1 - by0) / 2, (bx1 - bx0) / 2
        best = min(range(1, n + 1), key=lambda L: np.hypot(*(np.argwhere(lab == L).mean(0) - (cy, cx))) -
                   0.05 / S * (lab == L).sum())
        keep = lab == best
        ys, xs = np.nonzero(keep)
        ya, xa = (by0 + ys.min()) // S * S, (bx0 + xs.min()) // S * S  # on the 4x grid, so 1x lands on whole pixels
        yb, xb = -(-(by0 + ys.max() + 1) // S) * S, -(-(bx0 + xs.max() + 1) // S) * S
        full = np.zeros(a.shape[:2], bool)
        full[by0:by1, bx0:bx1] = keep
        rgba = np.dstack([a[ya:yb, xa:xb], full[ya:yb, xa:xb] * 255.0])
        Image.fromarray(rgba.astype(np.uint8), "RGBA").save(WORK / ("%s-%d-big.png" % (kind, i)))
        spots["%s-%d" % (kind, i)] = [int(ox + xa // S), int(oy + ya // S)]
    (WORK / "spots.json").write_text(json.dumps(spots, indent=1))


def backup():
    """The old 1x cuts and cuts.json, once, before anything is replaced."""
    if BAK.exists():
        return
    BAK.mkdir(parents=True)
    shutil.copy2(SL.LIFE / "cuts.json", BAK / "cuts.json")
    for cid in CROPS:
        for kind, i, _, _ in items(cid):
            p = SL.LIFE / ("%s-%d.png" % (kind, i))
            if p.exists():
                shutil.copy2(p, BAK / p.name)
    print("old cuts backed up in", BAK, flush=True)


def shrink(kinds):
    """Every big cut of a kind shrunk in one go (one 12 colour palette for all its poses) into the life folder, and its
    spot into cuts.json."""
    backup()
    spots = json.loads((WORK / "spots.json").read_text())
    cuts = json.loads((SL.LIFE / "cuts.json").read_text())
    for kind in sorted(kinds):
        names = sorted([n for n in spots if n.rsplit("-", 1)[0] == kind and (WORK / (n + "-big.png")).exists()],
                       key=lambda n: int(n.rsplit("-", 1)[1]))
        parts = [np.asarray(Image.open(WORK / (n + "-big.png")).convert("RGBA")).astype(np.float64) for n in names]
        if not parts:
            continue
        hh, gap = max(p.shape[0] for p in parts), 2 * S
        canvas = np.zeros((hh, sum(p.shape[1] for p in parts) + gap * (len(parts) - 1), 4))
        x, spans = 0, []
        for n, p in zip(names, parts):
            canvas[:p.shape[0], x:x + p.shape[1]] = p
            spans.append((n, x // S, p.shape[1] // S, p.shape[0] // S))
            x += p.shape[1] + gap
        col, mm = SL.shrink(canvas, scale=1.0 / S, ncol=12, outline=False)
        for n, sx, sw, sh in spans:
            rgba = np.dstack([col[:sh, sx:sx + sw], mm[:sh, sx:sx + sw] * 255.0]).clip(0, 255).astype(np.uint8)
            Image.fromarray(rgba, "RGBA").save(SL.LIFE / (n + ".png"))
            cuts[n] = spots[n]
        print("shrunk", kind, [s[0] for s in spans], flush=True)
    (SL.LIFE / "cuts.json").write_text(json.dumps(cuts, indent=1))


def look():
    """Each kind, the old 1x paint (the backup) beside the new one, scaled up together: look-props-4x.png."""
    rows = []
    for kind in ("vulture", "sailor", "scarab", "moth", "bat", "bonebird", "whale"):
        n = sum(1 for cid in CROPS for kd, _ in CROPS[cid]["items"] if kd == kind)
        old = [Image.open(BAK / ("%s-%d.png" % (kind, i))).convert("RGBA") for i in range(n)
               if (BAK / ("%s-%d.png" % (kind, i))).exists()]
        new = [Image.open(SL.LIFE / ("%s-%d.png" % (kind, i))).convert("RGBA") for i in range(n)]
        wid = sum(t.width + 4 for t in old + new) + 30
        sc = max(1, min(5, 1800 // max(1, wid)))
        tiles = [t.resize((t.width * sc, t.height * sc), Image.NEAREST) for t in old + new]
        rows.append((kind, sc, len(old), tiles))
    hgt = sum(max(t.height for t in tiles) + 34 for _, _, _, tiles in rows)
    wid = max(sum(t.width + 12 for t in tiles) + 60 for _, _, _, tiles in rows)
    s = Image.new("RGB", (wid, hgt), (64, 58, 66))
    d = ImageDraw.Draw(s)
    y = 0
    for kind, sc, nold, tiles in rows:
        d.text((8, y + 6), "%s   old (painted at 1x)  |  new (painted 4x, shrunk)   shown %dx" % (kind, sc),
               fill=(239, 223, 190), font=SL.font(16))
        x = 8
        for j, t in enumerate(tiles):
            if j == nold:
                x += 40
            s.paste(t, (x, y + 28), t)
            x += t.width + 12
        y += max(t.height for t in tiles) + 34
    s.save(SL.OUT / "look-props-4x.png")
    print("look-props-4x.png", s.size, flush=True)


if __name__ == "__main__":
    args = sys.argv[1:]
    mode = args[0] if args and args[0] in ("paint", "cut", "look") else "look"
    cids = [a for a in args if a in CROPS] or list(CROPS)
    if mode == "paint":
        for cid in cids:
            paint(cid)
    if mode in ("paint", "cut"):
        for cid in cids:
            cut(cid)
        shrink({kd for cid in cids for kd, _ in CROPS[cid]["items"]})
    look()
