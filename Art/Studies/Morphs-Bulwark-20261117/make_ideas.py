"""The Bulwark ideas page (Jacob, 17 Nov 2026: "lets do morphs", Fire Emblem promotion not Pokemon, then "Go").
Every idea starts from the real Clarth Warden idle frame (img2img on local Ideogram 4, ArtLab ComfyUI on 8191), so he stays the
same man; only the promotion idea changes. Placeholder art: a hired artist hand draws the finals later.
  python -I make_ideas.py calibrate      idea 1 at two start steps, to pick how much may change
  python -I make_ideas.py run STEP       all 12 ideas at that start step
  python -I make_ideas.py page           the numbered page from what has been made
"""
import json, sys, time, shutil, urllib.request
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]
SPRITE = REPO / "Assets/Resources/CampaignArt/clarth_warden_sw_idle.png"
INPUT = Path(r"C:\Users\jacob\ComfyUI-Installs\ComfyUI\ComfyUI-ArtLab\input\ks-morphs-20261117")
URL = "http://127.0.0.1:8191"
SEED = 2026111701
SIZE = 1024
GROUND = (42, 36, 32)

# The 12 promotion ideas. Each one changes something different on the same man (Fire Emblem promotion rules, the brief of
# 17 Nov): never floating, no new creature, same face and mask, keeps his shield, no gold, nothing that reads as a loot item.
IDEAS = [
    ("Tower shield", "his round tooth ringed shield has grown into a tall door sized tower shield ringed with two rows of teeth, reaching from his shin to his brow"),
    ("Jaw helm", "his bone mask has grown into a full shark skull helm, its open jaw framing his masked face, teeth around his head"),
    ("Gill plates", "heavy layered bone and dark leather plates cover his shoulders and chest, overlapping like shark gill plates"),
    ("War cloak", "a long torn red war cloak hangs from his shoulders to the ground behind him, its hem edged with small teeth"),
    ("Planted stake", "a thick iron spike runs down from the bottom of his shield, driven into the ground, so the shield stands like a wall"),
    ("Grown flesh", "his body has grown bigger and heavier, huge arms and shoulders and a thick neck, the same gear stretched over the new bulk"),
    ("Held the line", "broken arrows and snapped blades are stuck in his shield and shoulder plates, proof of every charge he stopped"),
    ("Second shield", "a second smaller tooth ringed shield is strapped over his left shoulder as a great pauldron"),
    ("Hide wall", "a thick stretched hide mantle rises over his back and shoulders like a hunched wall, studded with teeth"),
    ("Fin crest", "a tall dark shark fin crest rises from the top of his helm and runs down his back"),
    ("Bone greaves", "heavy bone greaves and wide armoured boots on his legs, his stance set wider and lower like a gate post"),
    ("Full Bulwark", "a tall tower shield ringed with teeth, heavy layered shoulder plates and a short dark fin crest on his helm, together"),
]

STYLE = {
    "aesthetics": "a battle sprite from a handheld tactics game of the early 2000s: the same warrior after his class promotion, heavier and more imposing, still clearly himself",
    "lighting": "warm light from the upper left, shadow on his lower right side",
    "medium": "pixel art",
    "art_style": "crisp hard edged pixel sprite with a dark outline, flat colour clusters, no anti aliasing, no gradients",
}
NEG = "gold, golden, glowing aura, halo, crown, floating objects, magic effects, text, letters, photo, 3d render, blurry, extra characters, cute, chibi"


def palette(n=12):
    """His own colours, uppercase hex, from the idle frame (the brief: no colours outside his palette)."""
    im = Image.open(SPRITE).convert("RGBA").crop((0, 0, 188, 180))
    px = [p[:3] for p in im.getdata() if p[3] > 0]
    flat = Image.new("RGB", (len(px), 1)); flat.putdata(px)
    q = flat.quantize(n, method=Image.Quantize.MEDIANCUT)
    pal = q.getpalette()[:3 * n]
    return ["#%02X%02X%02X" % tuple(pal[i:i + 3]) for i in range(0, 3 * n, 3)]


def start_image():
    """Frame 0 of his idle, scaled up 5x with hard pixels, centred low on a flat dark ground with room to grow."""
    fr = Image.open(SPRITE).convert("RGBA").crop((0, 0, 188, 180))
    fr = fr.crop(fr.getbbox())
    big = fr.resize((fr.width * 5, fr.height * 5), Image.NEAREST)
    canvas = Image.new("RGB", (SIZE, SIZE), GROUND)
    canvas.paste(big, ((SIZE - big.width) // 2, SIZE - big.height - 90), big)
    INPUT.mkdir(parents=True, exist_ok=True)
    canvas.save(INPUT / "warden_start.png")
    canvas.save(HERE / "warden_start.png")
    return "ks-morphs-20261117/warden_start.png"


def starts():
    """A rough block of each idea's new part, drawn at his own pixel size in his own colours onto his real frame (Ideogram keeps
    a start's big shapes, so the new part has to be in the start: the calibration of 17 Nov added no tower shield on its own).
    Drawn on a 1x canvas of SIZE/5, then scaled 5x with hard pixels, like the plain start."""
    S = SIZE // 5
    fr = Image.open(SPRITE).convert("RGBA").crop((0, 0, 188, 180)); fr = fr.crop(fr.getbbox())
    ox, oy = (S - fr.width) // 2, S - fr.height - 18
    pal = [tuple(int(h[i:i + 2], 16) for i in (1, 3, 5)) for h in palette(16)]
    lum = lambda c: 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]
    redness = lambda c: c[0] - (c[1] + c[2]) / 2
    OUT = min(pal, key=lum)
    BONE = max(pal, key=lum)
    RED = max(pal, key=redness)
    BROWN = sorted(pal, key=lum)[len(pal) // 3]
    LEATHER = sorted(pal, key=lum)[len(pal) // 2]
    IRON = (58, 56, 60)
    # anchors on the 1x canvas, measured on the start image (shield ring centre 673,604; mask 491,413; figure 227..796, 314..933)
    sx, sy, mx, my = 673 // 5, 604 // 5, 491 // 5, 413 // 5
    top, bottom, left, right = 314 // 5, 933 // 5, 227 // 5, 796 // 5
    shield = fr.crop((sx - ox - 23, sy - oy - 32, sx - ox + 23, sy - oy + 32))

    def teeth(d, cx, cy, rx, ry, n, inward=True, size=3):
        import math
        for k in range(n):
            a = 2 * math.pi * k / n
            x, y = cx + rx * math.cos(a), cy + ry * math.sin(a)
            tx, ty = cx + (rx - size if inward else rx + size) * math.cos(a), cy + (ry - size if inward else ry + size) * math.sin(a)
            px_, py_ = -math.sin(a) * 1.6, math.cos(a) * 1.6
            d.polygon([(x - px_, y - py_), (x + px_, y + py_), (tx, ty)], fill=BONE, outline=OUT)

    def make(k):
        back = Image.new("RGBA", (S, S), GROUND + (255,)); d = ImageDraw.Draw(back)
        body = fr
        if k == 6:  # grown flesh: his own body, wider and taller
            body = fr.resize((int(fr.width * 1.18), int(fr.height * 1.1)), Image.NEAREST)
        bx, by = (S - body.width) // 2, S - body.height - 18
        # parts behind him
        if k == 4:
            d.polygon([(mx - 16, top + 22), (mx + 26, top + 22), (right - 6, bottom - 2), (right - 16, bottom - 8), (right - 28, bottom),
                       (mx + 4, bottom - 6), (left + 18, bottom), (left + 8, bottom - 6)], fill=RED, outline=OUT)
        if k == 9:
            d.polygon([(left + 10, top + 36), (mx - 10, top - 8), (mx + 34, top - 6), (sx + 4, top + 30), (sx - 6, top + 50), (left + 20, top + 52)],
                      fill=BROWN, outline=OUT)
            for t in range(9):
                x = mx - 8 + t * 5
                d.polygon([(x, top - 6), (x + 3, top - 6), (x + 1, top - 12)], fill=BONE, outline=OUT)
        if k in (10, 12):
            h = 26 if k == 10 else 14
            d.polygon([(mx - 2, top + 6), (mx + 14, top + 4), (mx + 30, top + 40), (mx + 8, top - h)], fill=OUT, outline=OUT)
            d.polygon([(mx + 1, top + 4), (mx + 12, top + 3), (mx + 8, top - h + 4)], fill=(48, 44, 52))
        back.alpha_composite(body, (bx, by))
        d = ImageDraw.Draw(back)
        # parts in front of him
        if k in (1, 12):
            d.rounded_rectangle([sx - 26, top + 8, sx + 26, bottom - 14], radius=10, fill=BROWN, outline=OUT, width=1)
            teeth(d, sx, (top + bottom) // 2 - 3, 21, (bottom - top - 22) // 2 - 3, 22)
            d.rectangle([sx - 3, (top + bottom) // 2 - 6, sx + 3, (top + bottom) // 2], fill=OUT)
        if k == 2:
            # a shark skull's jaws closing round his face: the upper jaw over the hood, the lower under the chin
            d.arc([mx - 17, my - 14, mx + 17, my + 18], 195, 345, fill=BONE, width=5)
            d.arc([mx - 15, my - 6, mx + 15, my + 22], 25, 155, fill=BONE, width=4)
            for a0 in range(200, 345, 18):
                import math
                x, y = mx + 13 * math.cos(math.radians(a0)), my + 2 + 14 * math.sin(math.radians(a0))
                d.polygon([(x - 1, y), (x + 1, y), (mx + 9 * math.cos(math.radians(a0)), my + 2 + 9 * math.sin(math.radians(a0)))], fill=BONE, outline=OUT)
        if k in (3, 12):
            for j in range(3):
                d.chord([mx - 22 + j * 3, my + 8 + j * 5, mx + 2 + j * 3, my + 26 + j * 5], 180, 360, fill=BONE if j % 2 == 0 else BROWN, outline=OUT)
                d.chord([mx + 8 - j * 2, my + 8 + j * 5, mx + 32 - j * 2, my + 26 + j * 5], 180, 360, fill=BROWN if j % 2 == 0 else BONE, outline=OUT)
        if k == 5:
            d.polygon([(sx - 7, sy + 28), (sx + 7, sy + 28), (sx, bottom + 12)], fill=IRON, outline=OUT)
        if k == 7:
            # broken arrows and snapped blades stuck in his shield and shoulder: thick enough to read at his own size
            for (x0, y0, x1, y1) in [(sx - 4, sy - 12, sx + 26, sy - 34), (sx + 6, sy + 4, sx + 36, sy - 6), (sx - 10, sy + 14, sx + 18, sy + 34),
                                     (mx + 8, my + 16, mx - 12, my - 6), (sx + 12, sy - 22, sx + 34, sy - 44)]:
                d.line([(x0, y0), (x1, y1)], fill=OUT, width=3)
                d.line([(x0, y0), (x1, y1)], fill=BROWN, width=1)
                d.polygon([(x1 - 3, y1 - 2), (x1 + 3, y1 - 2), (x1, y1 + 3)], fill=BONE, outline=OUT)
        if k == 8:
            small = shield.resize((shield.width * 6 // 10, shield.height * 6 // 10), Image.NEAREST)
            back.alpha_composite(small, (mx - 30, my + 2))
        if k == 11:
            for gx in (mx - 22, mx + 14):
                d.rectangle([gx, bottom - 30, gx + 14, bottom - 4], fill=BONE, outline=OUT)
                d.line([(gx + 2, bottom - 20), (gx + 12, bottom - 20)], fill=OUT)
        img = back.convert("RGB").resize((SIZE, SIZE), Image.NEAREST)
        name = "start-%02d.png" % k
        img.save(INPUT / name); img.save(HERE / name)

    INPUT.mkdir(parents=True, exist_ok=True)
    for k in range(1, len(IDEAS) + 1): make(k)
    tiles = [Image.open(HERE / ("start-%02d.png" % k)).resize((256, 256)) for k in range(1, len(IDEAS) + 1)]
    sheet = Image.new("RGB", (6 * 256, 2 * 256))
    for i, t in enumerate(tiles): sheet.paste(t, ((i % 6) * 256, (i // 6) * 256))
    sheet.save(HERE / "starts-sheet.png")


def caption(idea):
    pal = palette()
    return json.dumps({
        "high_level_description": "The hulking Clarth warden from the start image, promoted to a Bulwark: " + idea +
                                  ". Same bone mask, red hood, dark leather and guarding crouch.",
        "style_description": dict(STYLE, color_palette=pal),
        "compositional_deconstruction": {
            "background": "a plain flat dark warm grey backdrop with nothing else in it",
            "elements": [{"type": "obj", "bbox": [60, 150, 960, 850],
                          "desc": "The Clarth warden in his guarding crouch, facing left, " + idea + ".",
                          "color_palette": pal[:5]}],
        }})


def graph(idea, step, prefix, image):
    return {
        "1": {"class_type": "UNETLoader", "inputs": {"unet_name": "ideogram4_int8_convrot.safetensors", "weight_dtype": "default"}},
        "2": {"class_type": "CLIPLoader", "inputs": {"clip_name": "qwen3vl_8b_fp8_scaled.safetensors", "type": "ideogram4", "device": "default"}},
        "3": {"class_type": "VAELoader", "inputs": {"vae_name": "flux2-vae.safetensors"}},
        "4": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["2", 0], "text": caption(idea)}},
        "5": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["2", 0], "text": NEG}},
        "6": {"class_type": "CFGOverride", "inputs": {"model": ["1", 0], "cfg": 3.0, "start_percent": 0.7, "end_percent": 1.0}},
        "7": {"class_type": "DualModelGuider", "inputs": {"model": ["6", 0], "positive": ["4", 0], "cfg": 7.0, "model_negative": ["6", 0], "negative": ["5", 0]}},
        "9": {"class_type": "KSamplerSelect", "inputs": {"sampler_name": "euler"}},
        "10": {"class_type": "Ideogram4Scheduler", "inputs": {"steps": 20, "width": SIZE, "height": SIZE, "mu": 0.0, "std": 1.75}},
        "11": {"class_type": "RandomNoise", "inputs": {"noise_seed": SEED}},
        "15": {"class_type": "LoadImage", "inputs": {"image": image}},
        "16": {"class_type": "VAEEncode", "inputs": {"pixels": ["15", 0], "vae": ["3", 0]}},
        "18": {"class_type": "SplitSigmas", "inputs": {"sigmas": ["10", 0], "step": step}},
        "12": {"class_type": "SamplerCustomAdvanced", "inputs": {"noise": ["11", 0], "guider": ["7", 0], "sampler": ["9", 0], "sigmas": ["18", 1], "latent_image": ["16", 0]}},
        "13": {"class_type": "VAEDecode", "inputs": {"samples": ["12", 0], "vae": ["3", 0]}},
        "14": {"class_type": "SaveImage", "inputs": {"images": ["13", 0], "filename_prefix": prefix}},
    }


def api(path, body=None):
    req = urllib.request.Request(URL + path, data=None if body is None else json.dumps(body).encode(),
                                 headers={"Content-Type": "application/json"})
    return json.load(urllib.request.urlopen(req))


def render(idea, step, out, image):
    pid = api("/prompt", {"prompt": graph(idea, step, "KS-Morphs-20261117/" + out.stem, image), "client_id": "ks-morphs"})["prompt_id"]
    t0 = time.time()
    while True:
        h = api("/history/" + pid)
        if pid in h and h[pid].get("outputs"):
            img = h[pid]["outputs"]["14"]["images"][0]
            q = "filename=%s&subfolder=%s&type=output" % (img["filename"], img["subfolder"])
            out.write_bytes(urllib.request.urlopen(URL + "/view?" + q.replace(" ", "%20")).read())
            print(out.name, "%.0fs" % (time.time() - t0), flush=True)
            return
        if pid in h and h[pid].get("status", {}).get("status_str") == "error":
            raise SystemExit("ComfyUI error: " + json.dumps(h[pid]["status"])[:800])
        time.sleep(3)


def page():
    """Today's frame, then the 12 numbered ideas, each with its one line; and a strip of all 12 at his real sprite height."""
    from PIL import ImageFont
    files = [HERE / ("idea-%02d.png" % (i + 1)) for i in range(len(IDEAS))]
    tile, pad, lab = 240, 10, 34
    cols = 5
    cells = [("0  Today: the Clarth Warden", HERE / "warden_start.png")] + [
        ("%d  %s" % (i + 1, IDEAS[i][0]), f) for i, f in enumerate(files) if f.exists()]
    rows = (len(cells) + cols - 1) // cols
    W = cols * (tile + pad) + pad
    strip_h = 150
    H = rows * (tile + lab + pad) + pad + strip_h + 40
    sheet = Image.new("RGB", (W, H), (20, 17, 15)); d = ImageDraw.Draw(sheet)
    for k, (label, f) in enumerate(cells):
        x, y = pad + (k % cols) * (tile + pad), pad + (k // cols) * (tile + lab + pad)
        sheet.paste(Image.open(f).convert("RGB").resize((tile, tile), Image.LANCZOS), (x, y))
        d.text((x + 4, y + tile + 6), label, fill=(239, 223, 190))
    # the strip: each idea shrunk to his real sprite height on the dark ground, beside today's sprite
    y0 = rows * (tile + lab + pad) + pad + 10
    d.text((pad, y0), "At his real sprite height (about 125 px):", fill=(173, 154, 134))
    fr = Image.open(SPRITE).convert("RGBA").crop((0, 0, 188, 180)); fr = fr.crop(fr.getbbox())
    x = pad
    sheet.paste(fr, (x, y0 + 18), fr); x += fr.width + 8
    for f in files:
        if not f.exists(): continue
        im = Image.open(f).convert("RGB").crop((150, 30, 874, 1004))
        h = 125 * 974 // 640
        im = im.resize((im.width * h // im.height, h), Image.LANCZOS)
        if x + im.width > W: break
        sheet.paste(im, (x, y0 + 18 - (h - 125)))
        x += im.width + 4
    sheet.save(HERE / "bulwark-ideas-page.png")
    print("page", sheet.size)


if __name__ == "__main__":
    img = start_image()
    if sys.argv[1] == "calibrate":
        for step in (5, 9):
            render(IDEAS[0][1], step, HERE / ("calib-step%02d.png" % step), img)
    elif sys.argv[1] == "starts":
        starts()
    elif sys.argv[1] == "run":
        step = int(sys.argv[2])
        only = [int(a) for a in sys.argv[3:]]
        for i, (name, idea) in enumerate(IDEAS):
            if only and i + 1 not in only: continue
            out = HERE / ("idea-%02d.png" % (i + 1))
            if not out.exists(): render(idea, step, out, "ks-morphs-20261117/start-%02d.png" % (i + 1))
    elif sys.argv[1] == "page":
        page()
