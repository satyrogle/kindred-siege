import sys, zipfile, io, os
from PIL import Image
S = sys.argv[1]; A = S + "/allin"; Z1 = A + "/zips"; Z2 = A + "/zips2"
R = "C:/Users/jacob/KindredSiege-Clean/Assets/StreamingAssets/CampaignArt/Bloodridge/"
bg = Image.open(R + "ground-underpaint.png").convert("RGBA")
for l in ["rear-burial-architecture.png", "rooted-entrance.png"]:
    bg.alpha_composite(Image.open(R + l).convert("RGBA"))

def zclip(zdir, cid, name, d):
    z = zipfile.ZipFile(f"{zdir}/{cid}.zip")
    names = sorted(x for x in z.namelist() if f"/animations/{name}/{d}/" in x)
    return [Image.open(io.BytesIO(z.read(n))).convert("RGBA") for n in names]

def local(path):
    return [Image.open(f"{path}/{i}.png").convert("RGBA") for i in range(17)]

def anchor(im):
    a = im.split()[3].point(lambda v: 255 if v > 20 else 0).getbbox()
    return ((a[0] + a[2]) // 2, a[3])

# id, fight clip source, run dir, feet x, feet y, fight phase, start offset x, arrive frame
W = "d917de88-8dfc-4de2-97f0-4dc46e06cb7d"
units = [
    (W, ("local", S + "/tank3/t3"), "se", 655, 565, 0, -300, 14),
    ("a1195c8f-03c0-42f8-8f8f-135b8e98d09c", ("z", "allin_skirmisher_hack_sw_20261106"), "sw", 745, 565, 2, 300, 14),
    ("a3aa47e7-da8b-47f5-a04d-dadb14721a65", ("z", "allin_skirmisher_combo_se_20261106"), "se", 665, 445, 5, -300, 12),
    ("b3fef59f-4ba2-4f95-a526-1228e1f8b49d", ("z", "allin_warden_brace_strike_sw_20261106"), "sw", 755, 445, 4, 300, 12),
    ("44547cca-99d8-432c-bc41-ab629db97136", ("z", "allin_trapper_jab_se_20261106"), "se", 665, 690, 9, -300, 15),
    ("720d540c-6f12-49b8-b409-3f84e8102bf2", ("z", "allin_trapper_slash_sw_20261106"), "sw", 745, 690, 11, 300, 15),
    ("9e0233b1-4a8a-4d81-99d8-53351b1cf28d", ("z", "allin_marksman_shot_se_20261106"), "se", 480, 500, 3, -160, 9),
    ("20b62125-093d-41ca-a5d2-eb2f15d3fd25", ("z", "allin_medic_heal_se_20261106"), "se", 470, 630, 7, -160, 10),
    ("19b7cc53-fb54-45dd-95ee-433ed538dd10", ("z", "allin_marksman_shot_sw_20261106"), "sw", 945, 500, 12, 160, 9),
    ("e7aceef9-d12c-4c53-8817-318de3265c03", ("z", "allin_medic_heal_sw_20261106"), "sw", 955, 630, 14, 160, 10),
]
DIR = {"se": "south-east", "sw": "south-west"}
built = []
for cid, src, d, x, y, ph, off, arrive in units:
    fight = local(src[1]) if src[0] == "local" else zclip(Z1, cid, src[1], DIR[d])
    run = zclip(Z2, cid, f"allin_run_{d}_20261106", DIR[d])
    built.append((fight, anchor(fight[0]), run, anchor(run[0]), x, y, ph, off, arrive))

order = sorted(range(len(built)), key=lambda k: built[k][5])
box = (300, 300, 1120, 760)
N = 16 + 40
out = []
for f in range(N):
    c = bg.copy()
    for k in order:
        fight, fa, run, ra, x, y, ph, off, arrive = built[k]
        if f < arrive:
            t = f / arrive
            ease = 1 - (1 - t) ** 2  # fast start, slows into the plant
            px = round(x + off * (1 - ease))
            im = run[f % len(run)]; ax, ay = ra
        else:
            px = x
            im = fight[(f - arrive + ph) % len(fight)] if f - arrive > 0 else fight[0]
            ax, ay = fa
        c.alpha_composite(im, (px - ax, y - ay))
    cc = c.crop(box)
    out.append(cc.resize((cc.width * 2, cc.height * 2), Image.NEAREST))
os.makedirs(A + "/out2", exist_ok=True)
for i in (3, 10, 20):
    out[i].convert("RGB").save(f"{A}/out2/still{i}.png")
pal = [o.convert("P", palette=Image.ADAPTIVE, colors=255) for o in out]
dur = [90] * N
pal[0].save(A + "/out2/all-in-charge.gif", save_all=True, append_images=pal[1:], duration=dur, loop=0, disposal=2)
print("ok", [len(b[2]) for b in built])
