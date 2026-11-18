"""The Bulwark, round 3: part studies of option 3, Layered hides (Jacob, 17 Nov 2026: "3 Layered hides but dont tell me its the
final design", then "Go"). Each option repaints option 3 with one part described differently (local Ideogram 4 img2img), then only
that part is pasted back onto the untouched option 3, so the body stays the same pixels and each row differs in one part only.
Placeholder art: a hired artist hand draws the finals later.
  python -I make_parts.py run      the 12 repaints
  python -I make_parts.py page     the four rows, numbered
"""
import json, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
sys.path.insert(0, str(Path(__file__).resolve().parent))
import make_ideas as M

HERE = M.HERE
BASE = HERE / "promo-3-step03.png"
STEP = 4
BODY = ("a massive layered robe of thick dark hides and bone plates piled over his shoulders and back, a heavy tooth trimmed red hood, "
        "his body bigger and hunched")
# where each part sits on option 3 (x0, y0, x1, y1, on the 1024 picture)
BOX = {"mask": (380, 240, 580, 505), "shield": (495, 545, 775, 825), "hides": (280, 170, 900, 680), "club": (180, 600, 420, 750)}
# how much each part may change: the hides are a big shape and needed more freedom (step 4 left them as picked, 17 Nov)
STEP_OF = {"mask": 3, "shield": 4, "hides": 2, "club": 4}
ROWS = [
    ("mask", "Mask", [
        ("His old mask", "his old long pale bone skull mask with a pointed snout and two short swept back horns, under the red hood"),
        ("Ram horns", "his long pale bone skull mask with a pointed snout, and two thick ram horns curling back and down past his hood to his jaw"),
        ("Jaw hung below", "his long pale bone skull mask with a pointed snout and a hinged lower jaw of fangs hanging below it, a full skull"),
    ]),
    ("shield", "Shield", [
        ("Two rows of fangs", "his round shield is a maw of bone with two rows of long fangs, its rim ringed with small teeth like his old shield"),
        ("Red gullet", "his round shield is a gaping maw with a dark red gullet inside and red stripes on its bone rim, like his old shield's red marks"),
        ("Shark jaw", "his round shield is a huge shark jaw set on dark leather, wide open, with rows of serrated teeth"),
    ]),
    ("hides", "Hides", [
        ("Ragged pile", "thick dark brown hides piled over his shoulders and back, their edges ragged and torn"),
        ("Hide wall", "a tall mantle of hides rising high behind his shoulders and head like a hunched wall, edged with teeth"),
        ("Bone plated", "hides layered with large bone plates, rows of teeth along every edge"),
    ]),
    ("club", "Club", [
        ("His old club", "in his right hand his old short wooden club"),
        ("Bone club", "in his right hand a heavy bone club as long as his forearm"),
        ("Toothed club", "in his right hand a short wooden club studded with teeth"),
    ]),
]


def caption(part_text):
    pal = M.palette()
    whole = BODY + "; " + part_text
    return json.dumps({
        "high_level_description": "The promoted Clarth warden, the Bulwark, in his guarding crouch facing left: " + whole + ".",
        "style_description": dict(M.STYLE, color_palette=pal),
        "compositional_deconstruction": {
            "background": "a plain flat dark brown backdrop with nothing else in it",
            "elements": [{"type": "obj", "bbox": [200, 150, 980, 880], "desc": "The promoted Clarth warden: " + whole + ".",
                          "color_palette": pal[:5]}],
        }})


def figure(im, thresh=60):
    """Where the figure is: pixels clearly off the picture's own flat backdrop (its corner colour)."""
    g = im.getpixel((8, 8))
    return im.point(lambda v: v).convert("RGB"), Image.eval(
        Image.merge("RGB", [Image.eval(c, lambda v, gg=gg: abs(v - gg)) for c, gg in zip(im.split(), g)]).convert("L"),
        lambda v: 255 if v * 3 > thresh else 0)


def paste(base, variant, part):
    """Only this part of the variant onto the base: inside its box, and only where either picture has figure, never the
    backdrop (the backdrops differ a little, which left a darker box on 17 Nov). For the hides the base's mask, shield and club
    go back on top, cut to the figure too."""
    out = base.copy()
    x0, y0, x1, y1 = BOX[part]
    box = Image.new("L", base.size, 0); ImageDraw.Draw(box).rounded_rectangle((x0, y0, x1, y1), radius=40, fill=255)
    _, fv = figure(variant); _, fb = figure(base)
    both = Image.eval(Image.merge("RGB", [fv, fb, fb]).convert("L"), lambda v: 255 if v > 0 else 0)
    m = Image.composite(both, Image.new("L", base.size, 0), box).filter(ImageFilter.MaxFilter(5))
    if part == "hides":
        # the repaint moves his head, so nothing of the variant near the head comes over (a second head grew there on 17 Nov,
        # and a moved head sat on his belly), and only new pixels touching his body come over (loose chips floated otherwise)
        ImageDraw.Draw(m).ellipse((320, 220, 640, 660), fill=0)
        near = fb.filter(ImageFilter.MaxFilter(61))
        m = Image.composite(m, Image.new("L", base.size, 0), near)
    m = m.filter(ImageFilter.GaussianBlur(3))
    out.paste(variant, (0, 0), m)
    if part == "hides":
        for keep in ("mask", "shield", "club"):
            a, b, c, d = BOX[keep]
            k = Image.new("L", base.size, 0); ImageDraw.Draw(k).rounded_rectangle((a + 10, b + 10, c - 10, d - 10), radius=30, fill=255)
            k = Image.composite(fb, Image.new("L", base.size, 0), k).filter(ImageFilter.GaussianBlur(3))
            out.paste(base, (0, 0), k)
    return out


if __name__ == "__main__":
    if sys.argv[1] == "run":
        base_name = "ks-morphs-20261117/promo3_base.png"
        Image.open(BASE).convert("RGB").save(M.INPUT / "promo3_base.png")
        M.caption = caption
        base = Image.open(BASE).convert("RGB")
        for part, _, opts in ROWS:
            for i, (name, text) in enumerate(opts):
                raw = HERE / ("part-%s-%d-raw.png" % (part, i + 1))
                if not raw.exists(): M.render(text, STEP_OF[part], raw, base_name)
                paste(base, Image.open(raw).convert("RGB"), part).save(HERE / ("part-%s-%d.png" % (part, i + 1)))
    elif sys.argv[1] == "page":
        tile, pad, lab, head = 300, 10, 26, 30
        W = 4 * (tile + pad) + pad
        H = len(ROWS) * (head + tile + lab + pad) + pad
        sheet = Image.new("RGB", (W, H), (20, 17, 15)); d = ImageDraw.Draw(sheet)
        y = pad
        for part, title, opts in ROWS:
            d.text((pad, y + 8), title + ": option 3 as picked, then 1, 2, 3 (only the " + title.lower() + " changes)", fill=(239, 223, 190))
            y += head
            x0, y0, x1, y1 = BOX[part]
            cx, cy, r = (x0 + x1) // 2, (y0 + y1) // 2, max(x1 - x0, y1 - y0) // 2 + 60
            crop = (max(0, cx - r), max(0, cy - r), min(1024, cx + r), min(1024, cy + r))
            cells = [("Picked", BASE)] + [("%d  %s" % (i + 1, n), HERE / ("part-%s-%d.png" % (part, i + 1))) for i, (n, _) in enumerate(opts)]
            for k, (label, f) in enumerate(cells):
                x = pad + k * (tile + pad)
                if f.exists(): sheet.paste(Image.open(f).convert("RGB").crop(crop).resize((tile, tile), Image.LANCZOS), (x, y))
                d.text((x + 4, y + tile + 6), label, fill=(239, 223, 190))
            y += tile + lab + pad
        sheet.save(HERE / "bulwark-parts.png")
        print("page", sheet.size)
