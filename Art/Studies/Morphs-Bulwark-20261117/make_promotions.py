"""The Bulwark, round 2 (Jacob, 17 Nov 2026: the FE8 Ewan promotion chart, "this is how gba does"). A GBA promotion redraws the
whole outfit at once, keeps the colours, and grows the outline. Three whole promoted outfits for the same Clarth Warden, each from
his real idle frame scaled up a little (the outline grows), painted by local Ideogram 4 with more freedom than round 1.
Placeholder art: a hired artist hand draws the finals later.
  python -I make_promotions.py STEP      the three options at that start step
  python -I make_promotions.py page STEP the numbered picture
"""
import json, sys
from pathlib import Path
from PIL import Image, ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
import make_ideas as M

HERE = M.HERE
GROW = 1.12  # the promoted outline is bigger, as every GBA promotion is

OPTIONS = [
    ("Heavy armour", "full heavy armour from head to toe, layered bone plates over dark leather, a closed helm built around his bone mask, "
                     "and a huge tower shield ringed with teeth; a square, heavy outline"),
    ("Long mantle", "a long sweeping red mantle and tabard falling to the ground, edged with rows of small teeth, a high bone collar, "
                    "standing taller, his round shield grown larger with a second ring of teeth; a tall triangle outline"),
    ("Layered hides", "a massive layered robe of thick dark hides and bone plates piled over his shoulders and back, a heavy tooth trimmed hood, "
                      "his body bigger and hunched, his round shield grown into a gaping toothed maw; a round, massive outline"),
]


def start():
    fr = Image.open(M.SPRITE).convert("RGBA").crop((0, 0, 188, 180)); fr = fr.crop(fr.getbbox())
    fr = fr.resize((int(fr.width * GROW), int(fr.height * GROW)), Image.NEAREST)
    big = fr.resize((fr.width * 5, fr.height * 5), Image.NEAREST)
    canvas = Image.new("RGB", (M.SIZE, M.SIZE), M.GROUND)
    canvas.paste(big, ((M.SIZE - big.width) // 2, M.SIZE - big.height - 60), big)
    M.INPUT.mkdir(parents=True, exist_ok=True)
    canvas.save(M.INPUT / "warden_grown.png"); canvas.save(HERE / "warden_grown.png")
    return "ks-morphs-20261117/warden_grown.png"


def caption(outfit):
    pal = M.palette()
    return json.dumps({
        "high_level_description": "The Clarth warden after his class promotion to Bulwark, the same masked man in a whole new outfit: " + outfit + ".",
        "style_description": dict(M.STYLE, color_palette=pal),
        "compositional_deconstruction": {
            "background": "a plain flat dark warm grey backdrop with nothing else in it",
            "elements": [{"type": "obj", "bbox": [40, 120, 970, 880],
                          "desc": "The promoted Clarth warden in his guarding crouch, facing left, the same bone mask and red hood: " + outfit + ".",
                          "color_palette": pal[:5]}],
        }})


if __name__ == "__main__":
    if sys.argv[1] == "page":
        step = int(sys.argv[2])
        tile, pad, lab = 420, 12, 30
        cells = [("Today: the Clarth Warden", HERE / "warden_start.png")] + [
            ("%d  %s" % (i + 1, name), HERE / ("promo-%d-step%02d.png" % (i + 1, step))) for i, (name, _) in enumerate(OPTIONS)]
        W = len(cells) * (tile + pad) + pad
        sheet = Image.new("RGB", (W, tile + lab + 2 * pad), (20, 17, 15)); d = ImageDraw.Draw(sheet)
        for k, (label, f) in enumerate(cells):
            x = pad + k * (tile + pad)
            sheet.paste(Image.open(f).convert("RGB").resize((tile, tile), Image.LANCZOS), (x, pad))
            d.text((x + 6, pad + tile + 8), label, fill=(239, 223, 190))
        sheet.save(HERE / ("bulwark-promotions-step%02d.png" % step))
        print("page", sheet.size)
    else:
        step = int(sys.argv[1])
        img = start()
        M.caption = caption  # the graph reads its caption from make_ideas
        for i, (name, outfit) in enumerate(OPTIONS):
            out = HERE / ("promo-%d-step%02d.png" % (i + 1, step))
            if not out.exists(): M.render(outfit, step, out, img)
