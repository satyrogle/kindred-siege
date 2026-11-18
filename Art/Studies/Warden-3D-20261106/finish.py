"""Pixel finish for the Warden 3D renders: hard alpha, his sprite's own palette, 1 px dark outline.
Writes finished frames, an audit sheet (every frame of every move, his sprite first) and a move list GIF.
Usage: python -I finish.py <w3d dir> <warden zip>"""
import sys, os, glob, zipfile, io
from PIL import Image, ImageDraw, ImageFilter

W, ZIP = sys.argv[1], sys.argv[2]
z = zipfile.ZipFile(ZIP)
still = Image.open(io.BytesIO(z.read([n for n in z.namelist() if n.endswith("rotations/south-east.png")][0]))).convert("RGBA")
pal = still.convert("RGB").quantize(48, method=Image.Quantize.MEDIANCUT)
OUT = (24, 12, 8, 255)

def finish(im):
    a = im.split()[3].point(lambda v: 255 if v > 110 else 0)
    q = im.convert("RGB").quantize(palette=pal, dither=Image.Dither.NONE).convert("RGBA"); q.putalpha(a)
    edge = Image.new("RGBA", im.size, OUT); edge.putalpha(a.filter(ImageFilter.MaxFilter(3)))
    edge.alpha_composite(q)
    return edge

moves = ["stance", "shieldpush", "slash", "block", "parry", "hit", "slam", "walkfight", "runfast", "death"]
done = {}
for m in moves:
    fr = [finish(Image.open(p).convert("RGBA")) for p in sorted(glob.glob(f"{W}/frames/{m}/*.png"))]
    os.makedirs(f"{W}/final/{m}", exist_ok=True)
    for i, f in enumerate(fr): f.save(f"{W}/final/{m}/{i:03d}.png")
    done[m] = fr

box = None
for fr in done.values():
    for f in fr:
        b = f.getbbox()
        if b: box = b if box is None else (min(box[0], b[0]), min(box[1], b[1]), max(box[2], b[2]), max(box[3], b[3]))
cw, ch = box[2] - box[0], box[3] - box[1]

# audit sheets: every frame, one row per move (wrapping at 14 frames), his sprite at the top
for part, group in enumerate([moves[:5], moves[5:]]):
    rows = []
    for m in group:
        fr = done[m]
        for s in range(0, len(fr), 14): rows.append((m if s == 0 else "", s, fr[s:s + 14]))
    sheet = Image.new("RGBA", (80 + cw * 14, (ch + 4) * (len(rows) + 1)), (70, 62, 58, 255)); d = ImageDraw.Draw(sheet)
    d.text((4, 4), "sprite", fill=(255, 255, 0)); sheet.alpha_composite(still, (80, ch - still.height))
    for r, (m, s, fr) in enumerate(rows, 1):
        y = r * (ch + 4); d.text((4, y + 4), m, fill=(255, 255, 0)); d.text((4, y + 18), str(s), fill=(200, 200, 200))
        for c, f in enumerate(fr): sheet.alpha_composite(f.crop(box), (80 + c * cw, y))
    sheet.save(f"{W}/audit3d_{part}.png")

# move list GIF, 2x, on the court floor colour, labelled
frames = []
for m in ["stance", "shieldpush", "slash", "block", "hit", "slam", "death"]:
    for f in done[m]:
        bgc = Image.new("RGBA", (cw + 20, ch + 24), (92, 80, 74, 255)); bgc.alpha_composite(f.crop(box), (10, 18))
        ImageDraw.Draw(bgc).text((4, 2), m, fill=(255, 235, 170))
        frames.append(bgc.resize((bgc.width * 2, bgc.height * 2), Image.NEAREST).convert("RGB"))
    frames += [frames[-1]] * 6
frames[0].save(f"{W}/warden_3d_moves.gif", save_all=True, append_images=frames[1:], duration=83, loop=0)
print(box, {m: len(f) for m, f in done.items()}, len(frames))
