"""One row per unit: the still, then for each of the 7 set clips its frame at one third, two thirds, and its last frame.
Usage: python -I check.py <work dir>"""
import sys, zipfile, io, glob, os
from PIL import Image, ImageDraw
A = sys.argv[1]
rows = []
for z in sorted(glob.glob(A + "/zips/*.zip")):
    zf = zipfile.ZipFile(z); names = zf.namelist()
    sets = sorted({n.split("/animations/")[1].split("/")[0] for n in names if "/animations/set_" in n})
    for d in ("south-east", "south-west"):
        mine = [s for s in sets if f"_{d[6:8] if False else ('se' if d == 'south-east' else 'sw')}_" in s]
        if not mine: continue
        still = [n for n in names if n.endswith(f"rotations/{d}.png")]
        row = [("still", Image.open(io.BytesIO(zf.read(still[0]))).convert("RGBA"))]
        order = ["idle", "attack1", "attack2", "block", "hit", "power", "death"]
        for key in order:
            s = [x for x in mine if x.split("_")[2].startswith(key)]
            if not s: row.append((key + "?", None)); continue
            fr = sorted(n for n in names if f"/animations/{s[0]}/{d}/" in n)
            for j in (len(fr) // 3, 2 * len(fr) // 3, len(fr) - 1):
                row.append((key if j == len(fr) // 3 else "", Image.open(io.BytesIO(zf.read(fr[j]))).convert("RGBA")))
        rows.append((os.path.basename(z)[:8] + " " + d, row))
CW, CH = 120, 130
sheet = Image.new("RGB", (CW * 22 + 110, CH * len(rows)), (34, 30, 32))
dr = ImageDraw.Draw(sheet)
for r, (name, row) in enumerate(rows):
    dr.text((4, r * CH + 4), name, fill=(240, 220, 160))
    for c, (lab, im) in enumerate(row):
        x = 110 + c * CW; y = r * CH
        if im is not None:
            b = im.getbbox() or (0, 0, 1, 1); im = im.crop(b); im.thumbnail((CW - 4, CH - 16), Image.NEAREST)
            sheet.paste(im, (x + (CW - im.width) // 2, y + CH - im.height - 2), im)
        if lab: dr.text((x + 2, y + 2), lab, fill=(200, 200, 255))
sheet.save(A + "/out/check.png"); print(sheet.size, len(rows))
