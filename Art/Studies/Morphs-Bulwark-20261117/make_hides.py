"""The Bulwark's hides, round 4 (Jacob, 17 Nov 2026: mask 2, shield 2, club 2, then "Hides first"). A masked repaint: local Ideogram 4
paints only inside a mask over his shoulders and back (SetLatentNoiseMask), so his head, horns, shield, club and legs cannot move
or double, which the whole picture repaint of round 3 did. Placeholder art: a hired artist hand draws the finals later.
  python -I make_hides.py mask      draw the mask and a check picture
  python -I make_hides.py run       the three options
  python -I make_hides.py page      the row, numbered
"""
import json, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
sys.path.insert(0, str(Path(__file__).resolve().parent))
import make_ideas as M
import make_parts as P

HERE = M.HERE
BASE = HERE / "bulwark-picks-combined.png"
STEP = 2
OPTIONS = [
    ("Beast humps", "thick dark brown hides piled over his shoulders and back in big rounded humps, like the back of a hunched beast, ragged at the edges"),
    ("Hide wall", "a tall stiff mantle of dark shaggy hides standing up behind his shoulders like a high collar wall, plain hide with a ragged top edge"),
    ("Bone shell", "dark hides covered by big curved bone plates over his shoulders and back like a shell, rows of teeth along every plate edge"),
]


def make_mask():
    base = Image.open(BASE).convert("RGB")
    _, fb = P.figure(base)
    m = Image.new("L", base.size, 0); d = ImageDraw.Draw(m)
    # his shoulders and back, and room above and behind them to grow into
    d.polygon([(330, 420), (420, 300), (560, 250), (720, 250), (880, 330), (930, 470), (900, 640), (790, 600), (640, 560), (450, 600), (330, 560)], fill=255)
    # keep out: the head and horns, the shield, the club arm, the legs
    d.ellipse((410, 300, 620, 520), fill=0)
    # and the space left of and above his head, where a second skull grew on 17 Nov
    d.rectangle((300, 240, 470, 520), fill=0)
    d.rectangle((410, 240, 600, 330), fill=0)
    d.rectangle((560, 545, 800, 830), fill=0)
    d.rectangle((150, 560, 430, 760), fill=0)
    d.rectangle((300, 760, 950, 1024), fill=0)
    m = m.filter(ImageFilter.GaussianBlur(6))
    M.INPUT.mkdir(parents=True, exist_ok=True)
    m.save(M.INPUT / "hides_mask.png"); m.save(HERE / "hides_mask.png")
    base.save(M.INPUT / "bulwark_combined.png")
    check = base.copy(); red = Image.new("RGB", base.size, (255, 40, 40))
    check.paste(red, (0, 0), Image.eval(m, lambda v: v // 2))
    check.resize((512, 512)).save(HERE / "check-hides-mask.png")


def graph(text, step, prefix):
    g = M.graph(text, step, prefix, "ks-morphs-20261117/bulwark_combined.png")
    g["20"] = {"class_type": "LoadImageMask", "inputs": {"image": "ks-morphs-20261117/hides_mask.png", "channel": "red"}}
    g["21"] = {"class_type": "SetLatentNoiseMask", "inputs": {"samples": ["16", 0], "mask": ["20", 0]}}
    g["12"]["inputs"]["latent_image"] = ["21", 0]
    return g


def caption(text):
    pal = M.palette()
    whole = "a ram horned long bone skull mask under a red hood, a round bone shield shaped like a gaping maw with a red gullet, a bone club; " + text
    return json.dumps({
        "high_level_description": "The promoted Clarth warden, the Bulwark, in his guarding crouch facing left: " + whole + ".",
        "style_description": dict(M.STYLE, color_palette=pal),
        "compositional_deconstruction": {
            "background": "a plain flat dark brown backdrop with nothing else in it",
            "elements": [{"type": "obj", "bbox": [200, 280, 700, 920], "desc": "His shoulders and back: " + text + ".", "color_palette": pal[:5]}],
        }})


if __name__ == "__main__":
    if sys.argv[1] == "mask":
        make_mask()
    elif sys.argv[1] == "run":
        M.caption = caption
        M.NEG = M.NEG + ", second head, extra skull, extra face, skull on the shoulder"
        M.graph_masked = graph
        base = Image.open(BASE).convert("RGB")
        mask = Image.open(HERE / "hides_mask.png").convert("L")
        orig_graph = M.graph
        M.graph = lambda idea, step, prefix, image: graph(idea, step, prefix) if image == "masked" else orig_graph(idea, step, prefix, image)
        for i, (name, text) in enumerate(OPTIONS):
            raw = HERE / ("hides-%d-raw.png" % (i + 1))
            if not raw.exists(): M.render(text, STEP, raw, "masked")
            # outside the mask the picture stays exactly the picked one (the VAE round trip shifts pixels a little)
            Image.composite(Image.open(raw).convert("RGB"), base, mask).save(HERE / ("hides-%d.png" % (i + 1)))
    elif sys.argv[1] == "page":
        tile, pad, lab = 380, 10, 26
        cells = [("Picked so far", BASE)] + [("%d  %s" % (i + 1, n), HERE / ("hides-%d.png" % (i + 1))) for i, (n, _) in enumerate(OPTIONS)]
        W = len(cells) * (tile + pad) + pad
        sheet = Image.new("RGB", (W, tile + lab + 3 * pad + 20), (20, 17, 15)); d = ImageDraw.Draw(sheet)
        d.text((pad, 8), "Hides: your picks so far, then 1, 2, 3 (only the hides change)", fill=(239, 223, 190))
        for k, (label, f) in enumerate(cells):
            x = pad + k * (tile + pad)
            sheet.paste(Image.open(f).convert("RGB").crop((150, 120, 1000, 970)).resize((tile, tile), Image.LANCZOS), (x, 30))
            d.text((x + 4, 30 + tile + 6), label, fill=(239, 223, 190))
        sheet.save(HERE / "bulwark-hides.png")
        print("page", sheet.size)
