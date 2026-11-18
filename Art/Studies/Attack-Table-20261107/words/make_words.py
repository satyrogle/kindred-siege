"""Render words the way CampaignArtTitle.PixelFont does (font_logo atlas, 1 px dark ring plus drop shadow, caps rows).
Usage: python -I words.py <font_logo.bytes> <out dir> <check png>"""
import sys
from PIL import Image
GLYPHS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.,!?:;'\"-+()0123456789/%*=_$"
NIGHT = (28, 18, 14, 255)
atlas = Image.open(sys.argv[1]).convert("RGBA"); W, H = atlas.size; px = atlas.load()
cell = W // 8; space = max(3, cell // 3)
ink = lambda x, y: px[x, y][3] > 127
glyphs = {}; capTop, capBottom = cell, -1
for i, ch in enumerate(GLYPHS):
    cx, cy = i % 8 * cell, i // 8 * cell
    xs = [x for x in range(cell) for y in range(cell) if ink(cx + x, cy + y)]
    ys = [y for x in range(cell) for y in range(cell) if ink(cx + x, cy + y)]
    if not xs: continue
    glyphs[ch] = (cx + min(xs), cy, max(xs) - min(xs) + 1)
    if i < 26: capTop, capBottom = min(capTop, min(ys)), max(capBottom, max(ys))
rows = capBottom - capTop + 1

def render(text, fill, spacing=2, shadow=False):
    width = sum(space if c == ' ' else glyphs[c][2] + spacing for c in text) - spacing
    w, h = width + 2, rows + 3
    inked = [[False] * w for _ in range(h)]
    pen = 1
    for c in text:
        if c == ' ': pen += space; continue
        gx, gy, gw = glyphs[c]
        for y in range(rows):
            for x in range(gw):
                if pen + x < w - 1 and ink(gx + x, gy + capTop + y): inked[y + 1][pen + x] = True
        pen += gw + spacing
    near = lambda x, y: any(0 <= x + dx < w and 0 <= y + dy < h and inked[y + dy][x + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1))
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0)); o = out.load()
    for y in range(h):
        for x in range(w):
            if inked[y][x]: o[x, y] = fill
            elif near(x, y) or (shadow and near(x, y - 1)): o[x, y] = NIGHT
    return out

if __name__ == "__main__":
    check = Image.open(sys.argv[3]).convert("RGBA")
    for sp in (1, 2):
        mine = render("PINNED", (153, 190, 172, 255), sp)
        same = mine.size == check.size and list(mine.getdata()) == list(check.getdata())
        print("spacing", sp, mine.size, check.size, "IDENTICAL" if same else "differs")
