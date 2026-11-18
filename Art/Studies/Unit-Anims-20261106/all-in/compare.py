"""Old clips (redrawn every frame) next to new skeleton clips, one row per unit, plus a drift number.
Usage: python -I compare.py <allin dir>"""
import sys, zipfile, io
from PIL import Image, ImageDraw
A = sys.argv[1]
U = [  # id, dir, old fight clip, unit
    ("d917de88-8dfc-4de2-97f0-4dc46e06cb7d", "se", "tank_take3_block_bash_20261106", "warden"),
    ("b3fef59f-4ba2-4f95-a526-1228e1f8b49d", "sw", "allin_warden_brace_strike_sw_20261106", "warden"),
    ("a3aa47e7-da8b-47f5-a04d-dadb14721a65", "se", "allin_skirmisher_combo_se_20261106", "skirmisher"),
    ("a1195c8f-03c0-42f8-8f8f-135b8e98d09c", "sw", "allin_skirmisher_hack_sw_20261106", "skirmisher"),
    ("44547cca-99d8-432c-bc41-ab629db97136", "se", "allin_trapper_jab_se_20261106", "trapper"),
    ("720d540c-6f12-49b8-b409-3f84e8102bf2", "sw", "allin_trapper_slash_sw_20261106", "trapper"),
    ("9e0233b1-4a8a-4d81-99d8-53351b1cf28d", "se", "allin_marksman_shot_se_20261106", "marksman"),
    ("19b7cc53-fb54-45dd-95ee-433ed538dd10", "sw", "allin_marksman_shot_sw_20261106", "marksman"),
    ("20b62125-093d-41ca-a5d2-eb2f15d3fd25", "se", "allin_medic_heal_se_20261106", "medic"),
    ("e7aceef9-d12c-4c53-8817-318de3265c03", "sw", "allin_medic_heal_sw_20261106", "medic"),
]
D = {"se": "south-east", "sw": "south-west"}

def clip(zf, name, d):
    n = sorted(x for x in zf.namelist() if f"/animations/{name}/{d}/" in x)
    return [Image.open(io.BytesIO(zf.read(x))).convert("RGBA") for x in n]

def still(zf, d):
    n = [x for x in zf.namelist() if x.endswith(f"rotations/{d}.png")][0]
    return Image.open(io.BytesIO(zf.read(n))).convert("RGBA")

def keys(im):
    return {(r >> 3, g >> 3, b >> 3) for r, g, b, a in im.getdata() if a > 128}

def off(frames, ref):
    """average and worst percent of opaque pixels whose colour (5 bit) is not within 2 steps of the still sprite's"""
    near = set()
    for r, g, b in ref:
        for i in (-2, -1, 0, 1, 2):
            for j in (-2, -1, 0, 1, 2):
                for k in (-2, -1, 0, 1, 2):
                    near.add((r + i, g + j, b + k))
    t = []
    for f in frames:
        px = [(r >> 3, g >> 3, b >> 3) for r, g, b, a in f.getdata() if a > 128]
        t.append(100 * sum(p not in near for p in px) / max(1, len(px)))
    return sum(t) / len(t), max(t)

def crop(f):
    return f.crop(f.getbbox() or (0, 0, 1, 1))

def pick(fr, n):
    return [fr[round(i * (len(fr) - 1) / (n - 1))] for i in range(n)]

rows, report = [], []
for cid, d, oldf, name in U:
    z3 = zipfile.ZipFile(f"{A}/zips3/{cid}.zip")
    zo = z3 if oldf.startswith("tank") else zipfile.ZipFile(f"{A}/zips/{cid}.zip")
    z2 = zipfile.ZipFile(f"{A}/zips2/{cid}.zip")
    s = still(z3, D[d]); ref = keys(s)
    of = clip(zo, oldf, D[d]); nf = clip(z3, f"lock_{name}_fight_{d}_20261106", D[d])
    orun = clip(z2, f"allin_run_{d}_20261106", D[d]); nrun = clip(z3, f"lock_run_{d}_20261106", D[d])
    sc = [off(x, ref) for x in (orun, nrun, of, nf)]
    report.append(f"{cid[:8]} {name:10} {d}  run old {sc[0][0]:4.1f}% (worst {sc[0][1]:4.1f})  new {sc[1][0]:4.1f}% (worst {sc[1][1]:4.1f})"
                  f"   fight old {sc[2][0]:4.1f}% (worst {sc[2][1]:4.1f})  new {sc[3][0]:4.1f}% (worst {sc[3][1]:4.1f})   frames old {len(of)} new {len(nf)}")
    rows.append([crop(s)] + [crop(f) for f in pick(orun, 4)] + [None] + [crop(f) for f in pick(nrun, 4)] + [None]
                + [crop(f) for f in pick(of, 5)] + [None] + [crop(f) for f in pick(nf, 5)])
H = max(max(i.height for i in r if i) for r in rows)
cw = [max((r[c].width if r[c] else 14) for r in rows) for c in range(len(rows[0]))]
W = sum(cw) + 4 * len(cw)
sheet = Image.new("RGBA", (W, (H + 6) * len(rows) + 20), (34, 30, 32, 255))
dr = ImageDraw.Draw(sheet)
x = 0; heads = {0: "still", 1: "OLD run", 6: "NEW run", 11: "OLD fight", 17: "NEW fight"}
for c, w in enumerate(cw):
    if c in heads: dr.text((x + 2, 4), heads[c], fill=(240, 220, 160))
    if rows[0][c] is None: dr.rectangle((x + 4, 20, x + 8, sheet.height), fill=(120, 30, 30))
    x += w + 4
for ri, r in enumerate(rows):
    x = 0; y = 20 + ri * (H + 6)
    for c, im in enumerate(r):
        if im: sheet.alpha_composite(im, (x + (cw[c] - im.width) // 2, y + H - im.height))
        x += cw[c] + 4
sheet.save(f"{A}/compare.png")
open(f"{A}/drift.txt", "w").write("\n".join(report) + "\n")
print("\n".join(report)); print(sheet.size)
