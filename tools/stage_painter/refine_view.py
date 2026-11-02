import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import refine_assets as RA

# refine_view.py sky | drift | stop K SKY DRIFT FLOOR
# Composites a stop the way the refined page draws it (far, world, floor), for looking at options side by side.
PAGE = RA.PAGE
_cache = {}


def img(name):
    if name not in _cache:
        _cache[name] = Image.open(PAGE / name).convert("RGBA")
    return _cache[name]


def far_dx(cam_x, drift):
    cx = int(round(cam_x))
    return 0 if drift == 1 else (cx // 20 if drift == 2 else cx // 5)


def compose(k, sky=1, drift=2, floor=0, cam_x=None, cam_y=None):
    cx = RA.STOPS[k] if cam_x is None else int(round(cam_x))
    cy = RA.TILT[k] if cam_y is None else int(round(cam_y))
    v = Image.new("RGBA", (RA.VW, RA.VH), (29, 23, 28, 255))
    v.alpha_composite(img("l1-far.png"), (-far_dx(cx, drift), 0))
    world = img("l2-world-s%d.png" % sky) if sky else img("l2-world.png")
    v.alpha_composite(world.crop((cx, cy, cx + RA.VW, cy + RA.VH)))
    v.alpha_composite(img("l3-floor.png").crop((cx, cy, cx + RA.VW, cy + RA.VH)))
    if floor:
        p = img("l3-crypt-f%d.png" % floor)
        v.alpha_composite(p, (RA.STOPS[2] - cx, -cy))
    return v


def label(im, text, xy, size=2):
    d = ImageDraw.Draw(im)
    f = ImageFont.load_default()
    x, y = xy
    tw = int(d.textlength(text, font=f))
    d.rectangle((x - 4, y - 3, x + tw * size // 1 + 4, y + 11 * size), fill=(18, 14, 20, 255))
    t = Image.new("RGBA", (tw + 2, 12), (0, 0, 0, 0))
    ImageDraw.Draw(t).text((1, 0), text, font=f, fill=(239, 223, 190, 255))
    im.alpha_composite(t.resize((t.width * size, t.height * size), Image.NEAREST), (x, y))


def sky_stacks(rows=440):
    for k in range(3):
        out = Image.new("RGBA", (RA.VW, rows * 3 + 16), (18, 14, 20, 255))
        for n in (1, 2, 3):
            v = compose(k, sky=n, drift=1).crop((0, 0, RA.VW, rows))
            out.alpha_composite(v, (0, (n - 1) * (rows + 8)))
            label(out, "SKY %d" % n, (10, (n - 1) * (rows + 8) + 8))
        out.convert("RGB").save(HERE / ("look-stop%d-sky.png" % (k + 1)))
        print("saved", HERE / ("look-stop%d-sky.png" % (k + 1)))


import json

_M = []


def manifest():
    if not _M:
        _M.append(json.loads((PAGE / "manifest.js").read_text().split("=", 1)[1].rsplit(";", 1)[0]))
    return _M[0]


def shadow(v, sx, sy, w, h, col):
    from PIL import ImageDraw
    d = ImageDraw.Draw(v)
    x, y = int(round(sx)) + 2, int(round(sy)) - int(round(h / 2)) + 1
    for r in range(h):
        u = (r + 0.5) / h * 2 - 1
        half = int(round(w / 2 * (1 - u * u) ** 0.5))
        if half > 0:
            d.rectangle((x - half, y + r, x + half - 1, y + r), fill=col)


def fighter(v, s, fx_canvas, fy_canvas, sheets):
    M = manifest()
    sx, sy = fx_canvas - M["stage"][0], fy_canvas - M["stage"][1]
    if s["sheet"] not in sheets:
        sheets[s["sheet"]] = Image.open(PAGE / s["sheet"]).convert("RGBA")
    x, y, w, h = s["frames"][0][:4]
    fr = sheets[s["sheet"]].crop((x, y, x + w, y + h))
    ax, ay = s["anchor"]
    if s.get("flip"):
        fr = fr.transpose(Image.FLIP_LEFT_RIGHT)
        ax = w - ax
    v.alpha_composite(fr, (int(round(sx - ax)), int(round(sy - ay))))


def compose_full(k, sky=1, drift=2, floor=1):
    """The stop as the fight opens: layers plus both armies on their first idle frame, with contact shadows."""
    M = manifest()
    v = compose(k, sky, drift, floor)
    col = tuple(int(M["colours"]["shadow"][i:i + 2], 16) for i in (1, 3, 5)) + (255,)
    F = M["fighters"]
    st = M["stage"]
    people = [(o["idle"], o["x"], o["y"], o["shadow"]) for o in F["ours"]]
    people.append((F["leader"]["idle"], F["leader"]["x"], F["leader"]["y"], F["leader"]["shadow"]))
    people += [(e["idle"], e["x"], e["y"], e["shadow"]) for e in F["theirs"][k]]
    people.sort(key=lambda p: p[2])
    for s, x, y, (w, h) in people:
        shadow(v, x - st[0], y - st[1], w, h, col)
    sheets = {}
    for s, x, y, _ in people:
        fighter(v, s, x, y, sheets)
    return v


def grid(cells, cols, labels, rowlabels, gap=8, top=34):
    cw, ch = cells[0].size
    rows = len(cells) // cols
    W = cols * cw + (cols - 1) * gap
    out = Image.new("RGBA", (W, top + rows * ch + (rows - 1) * gap), (18, 14, 20, 255))
    for i, c in enumerate(cells):
        r, q = divmod(i, cols)
        out.alpha_composite(c, (q * (cw + gap), top + r * (ch + gap)))
    for q, t in enumerate(labels):
        label(out, t, (q * (cw + gap) + 8, 6))
    for r, t in enumerate(rowlabels):
        label(out, t, (8, top + r * (ch + gap) + 8), 1)
    return out


def picks():
    # SKY: columns are the three layouts, rows the three stops, each view at half size with the armies in it
    cells = []
    for k in range(3):
        for n in (1, 2, 3):
            v = compose_full(k, sky=n, drift=1, floor=1).crop((28, 0, 1348, 768))
            cells.append(v.resize((v.width // 2, v.height // 2), Image.NEAREST))
    g = grid(cells, 3, ["SKY 1  LOW WALLS", "SKY 2  FRAMED", "SKY 3  ONE LANDMARK"], ["STOP 1", "STOP 2", "STOP 3"])
    g.convert("RGB").save(RA.OUT / "pick-1-sky.png")
    # DRIFT: where the far layer and its court sit at each stop, top 320 rows at half size, sky 1
    cells = []
    for k in range(3):
        for d in (1, 2, 3):
            v = compose_full(k, sky=1, drift=d, floor=1).crop((28, 0, 1348, 320))
            cells.append(v.resize((v.width // 2, v.height // 2), Image.NEAREST))
    g = grid(cells, 3, ["DRIFT 1  STILL", "DRIFT 2  SLOW (1 PX PER 20)", "DRIFT 3  FASTER (1 PX PER 5)"], ["STOP 1", "STOP 2", "STOP 3"])
    g.convert("RGB").save(RA.OUT / "pick-2-drift.png")
    # FLOOR: the crypt where the ranks stand, at full size (1x), armies in place
    cells = []
    for f in (1, 2, 3):
        cells.append(compose_full(2, sky=1, drift=2, floor=f).crop((120, 250, 760, 700)))
    g = grid(cells, 3, ["FLOOR 1  FLAGSTONES", "FLOOR 2  SOFT COBBLES", "FLOOR 3  PACKED EARTH"], [])
    g.convert("RGB").save(RA.OUT / "pick-3-floor.png")
    for name in ("pick-1-sky", "pick-2-drift", "pick-3-floor"):
        im = Image.open(RA.OUT / (name + ".png"))
        s = min(1.0, 1400 / im.width)
        im.resize((int(im.width * s), int(im.height * s)), Image.NEAREST).save(HERE / ("look-" + name + ".png"))
        print(name, im.size)


if __name__ == "__main__":
    what = sys.argv[1]
    if what == "picks":
        picks()
    if what == "sky":
        sky_stacks()
    elif what == "stop":
        k, s, d, f = (int(v) for v in sys.argv[2:6])
        p = HERE / ("look-stop%d-s%d-d%d-f%d.png" % (k + 1, s, d, f))
        compose(k, s, d, f).convert("RGB").save(p)
        print("saved", p)
