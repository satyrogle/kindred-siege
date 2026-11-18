"""Every pop word at pixel size 2 (Jacob, 16 Nov 2026: "more pixelated", row 1; HEXED "blak"; then "Go, both" for the rest).
Groups: the 7 cash words (CampaignArt/UI/Marks), the 19 status words (CampaignArt/UI/Words43), the 4 attack table words
(Table42Review/Words). Each 1x file is first rebuilt pixel for pixel from Menu/font_logo to prove its text and colours.
Then each letter is shrunk on its own to a grid twice as coarse (strict vote, 3 of 4 cells, so holes stay open), top
aligned (the font draws Q to Z a pixel lower and shorter than A to P), R, K and G drawn by hand, a one cell edge, every
cell drawn 2x2, on the old canvas height, centred where the old word was. Output keeps each file's name.
Usage: python -I make_cash_px2.py <repo root> <out dir>"""
import sys, os
from collections import Counter
from PIL import Image, ImageDraw
REPO, OUT = sys.argv[1].rstrip("/\\") + "/", sys.argv[2].rstrip("/\\") + "/"
GLYPHS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.,!?:;'\"-+()0123456789/%*=_$"
NIGHT = (28, 18, 14, 255); BONE = (239, 223, 190, 255); CLEAR = (0, 0, 0, 0)
UI = REPO + "Assets/Resources/CampaignArt/UI/"
GROUPS = [  # folder in Assets, out subfolder, 1x canvas pad (rows above the 1x word), {file: text}
    (UI + "Marks/", "", 5, {"cash_pinned": "PINNED", "cash_opened": "OPENED", "cash_ripped": "RIPPED",
                            "cash_crushed": "CRUSHED", "cash_exposed": "EXPOSED", "cash_frozen": "FROZEN"}),
    (UI + "Words43/", "words43/", 0, {"bleed": "BLEED", "burn": "BURN", "exposed": "EXPOSED", "guarded": "GUARDED",
        "heal": "HEAL", "holds": "HOLDS", "opening": "OPEN", "panic": "PANIC", "pin": "PINNED", "rearm": "REARM",
        "rise": "RISE", "selfpatch": "SELF PATCH", "sidestep": "SIDESTEP", "snaptrap": "SNAP TRAP", "snare": "SNARED",
        "stasis": "FROZEN", "stim": "STIM", "treat": "TREAT", "twincut": "TWIN CUT"}),
    (REPO + "Assets/Resources/Table42Review/Words/", "table42/", 5,
        {"block": "BLOCK", "crit": "CRIT", "dodge": "DODGE", "parry": "PARRY"}),
]
NEW = [("cash_hexed", "HEXED", (46, 26, 54, 255), BONE, 34)]  # Jacob's pick: near black with a bone edge

atlas = Image.open(REPO + "Assets/Resources/CampaignArt/Menu/font_logo.bytes").convert("RGBA")
apx = atlas.load(); cell = atlas.size[0] // 8; SPACE = max(3, cell // 3)
glyph = {}; capTop, capBot = cell, -1
for i, ch in enumerate(GLYPHS):
    cx, cy = i % 8 * cell, i // 8 * cell
    pts = [(x, y) for x in range(cell) for y in range(cell) if apx[cx + x, cy + y][3] > 127]
    if not pts: continue
    xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
    glyph[ch] = (cx + min(xs), cy, max(xs) - min(xs) + 1, min(ys), max(ys))
    if i < 26: capTop, capBot = min(capTop, min(ys)), max(capBot, max(ys))
ROWS = capBot - capTop + 1

def edged(m, fill, edge):
    H, W = len(m) + 2, len(m[0]) + 2
    ink = lambda x, y: 1 <= x <= W - 2 and 1 <= y <= H - 2 and m[y - 1][x - 1]
    im = Image.new("RGBA", (W, H), CLEAR); o = im.load()
    for y in range(H):
        for x in range(W):
            if ink(x, y): o[x, y] = fill
            elif any(ink(x + i, y + j) for i in (-1, 0, 1) for j in (-1, 0, 1)): o[x, y] = edge
    return im

def word_1x(text, fill, edge, spacing=2):
    """The 1 Nov make_words.py layout: cap rows, 2 px between letters, a space of SPACE px, one row under."""
    w = sum(SPACE if c == " " else glyph[c][2] + spacing for c in text) - spacing
    m = [[False] * w for _ in range(ROWS + 1)]; pen = 0
    for c in text:
        if c == " ": pen += SPACE; continue
        gx, gy, gw = glyph[c][:3]
        for y in range(ROWS):
            for x in range(gw):
                if apx[gx + x, gy + capTop + y][3] > 127: m[y][pen + x] = True
        pen += gw + spacing
    return edged(m, fill, edge)

def shrink(m):
    H, W = len(m), len(m[0])
    return [[sum(m[y * 2 + j][x * 2 + i] for j in (0, 1) for i in (0, 1)
                 if y * 2 + j < H and x * 2 + i < W) >= 3 for x in range((W + 1) // 2)] for y in range((H + 1) // 2)]

GRID_ROWS = 12  # A to P shrink to 12 rows; every letter is made that tall
HAND = {  # letters the shrink breaks, drawn by hand on the coarse grid
    "R": ["#########..", "..##..###..", "..##..###..", "..##..###..", "..#######..", "..######...",
          "..##.###...", "..##..###..", "..##..###..", ".###..####.", "####..#####", "####..#####"],
    "K": ["####..#####", ".###..####.", "..##.####..", "..##.###...", "..######...", "..#####....",
          "..######...", "..##.###...", "..##..###..", "..##..###..", ".###..####.", "####..#####"],
    "G": ["..#####..", "########.", "########.", "###..###.", "###......", "###......",
          "###.#####", "###.####.", "###..###.", "########.", "########.", ".#######."],
}

def letter_grid(c):
    if c in HAND: g = [[ch == "#" for ch in r] for r in HAND[c]]
    else:
        gx, gy, gw, top, bot = glyph[c]
        g = shrink([[apx[gx + x, gy + y][3] > 127 for x in range(gw)] for y in range(top, bot + 1)])
        while g and not any(g[0]): g.pop(0)
        while g and not any(g[-1]): g.pop()
        used = [x for x in range(len(g[0])) if any(r[x] for r in g)]
        g = [r[used[0]:used[-1] + 1] for r in g]
        while len(g) < GRID_ROWS: g.append(list(g[-1]))  # a letter the font draws shorter gets its foot doubled
    assert len(g) == GRID_ROWS, (c, len(g))
    return g

def word_px2(text, fill, edge, canvas_h, old_centre):
    out = None
    for c in text:
        if c == " ":
            for r in out: r.extend([False] * (SPACE // 2))
            continue
        g = letter_grid(c)
        if out is None: out = [list(r) for r in g]
        else:
            for r, gr in zip(out, g): r.extend([False] + list(gr))
    im = edged(out, fill, edge); im = im.resize((im.width * 2, im.height * 2), Image.NEAREST)
    canvas = Image.new("RGBA", (im.width, canvas_h), CLEAR)
    y0 = int(round(old_centre - im.height / 2)); y0 -= y0 % 2
    assert 0 <= y0 and y0 + im.height <= canvas_h, (text, y0, im.height, canvas_h)
    canvas.paste(im, (0, y0))
    return canvas

made = []
for folder, sub, pad, files in GROUPS:
    os.makedirs(OUT + sub, exist_ok=True)
    for name, text in files.items():
        old = Image.open(folder + name + ".png").convert("RGBA")
        counts = Counter(p for p in old.getdata() if p[3] > 0)
        fill = next(c for c, _ in counts.most_common() if c != NIGHT); edge = NIGHT
        mine = word_1x(text, fill, edge)
        if pad:
            padded = Image.new("RGBA", (mine.width, old.height), CLEAR); padded.paste(mine, (0, pad)); mine = padded
        assert mine.size == old.size and list(mine.getdata()) == list(old.getdata()), ("1x rebuild differs", name, text)
        box = old.getbbox(); centre = (box[1] + box[3]) / 2
        new = word_px2(text, fill, edge, old.height, centre)
        assert set(new.getdata()) <= {fill, edge, CLEAR}
        new.save(OUT + sub + name + ".png"); made.append((sub, name, text, old, new))
        print(f"{sub or 'marks/'}{name}: {text!r} 1x rebuild identical; {old.size} -> {new.size}")
for name, text, fill, edge, h in NEW:
    new = word_px2(text, fill, edge, h, 18.5); new.save(OUT + name + ".png"); made.append(("", name, text, None, new))
    print(f"marks/{name}: {text!r} new; {new.size}")

# preview: every new word on the Bloodridge floor the fight films use, shown 2x
court = REPO + "Assets/StreamingAssets/CampaignArt/Bloodridge/"
stage = Image.open(court + "ground-underpaint.png").convert("RGBA")
for layer in ("rear-burial-architecture.png", "rooted-entrance.png"):
    stage.alpha_composite(Image.open(court + layer).convert("RGBA"))
def strip(images, y):
    s = stage.crop((0, y, stage.size[0], y + 46)).copy(); x = 12
    for im in images:
        assert x + im.width <= s.width - 8
        s.alpha_composite(im, (x, (46 - im.height) // 2)); x += im.width + 12
    return s.crop((0, 0, x, 46))
rows = [strip([m[4] for m in made[i:i + 7]], 500) for i in range(0, len(made), 7)]
W = max(r.width for r in rows) * 2
sheet = Image.new("RGBA", (W, sum(r.height * 2 + 10 for r in rows) + 30), NIGHT); y = 6
for r in rows:
    sheet.alpha_composite(r.resize((r.width * 2, r.height * 2), Image.NEAREST), (0, y)); y += r.height * 2 + 10
ImageDraw.Draw(sheet).text((8, y + 4), "Every pop word at pixel size 2, shown 2x: cash words, status words, attack table words.", fill=BONE)
sheet.save(OUT + "preview_all_px2.png")
print("preview", sheet.size, "words", len(made))
