"""Cut the 6 Nov PixelLab set_* clips (all ten units, seven moves each) into the game's own sheet format, the one
Assets/Resources/ClarthStudy and CampaignArt use: a PNG of 8 columns plus a JSON frame list with per frame durations,
anchorX/anchorY at the feet (centre and bottom of frame 0's body) and, for attacks and power moves, hitFrame (the frame
the weapon reaches furthest toward the enemy).
Usage: python -I make_sheets.py <folder of PixelLab character zips> <out dir>"""
import sys, os, io, json, zipfile
from PIL import Image

ZIPS, OUT = sys.argv[1], sys.argv[2]
os.makedirs(OUT, exist_ok=True)
UNITS = [  # (class, facing, tribe prefix, character id)
    ("warden", "se", "clarth", "d917de88-8dfc-4de2-97f0-4dc46e06cb7d"), ("warden", "sw", "blood_rot", "b3fef59f-4ba2-4f95-a526-1228e1f8b49d"),
    ("skirmisher", "se", "clarth", "a3aa47e7-da8b-47f5-a04d-dadb14721a65"), ("skirmisher", "sw", "blood_rot", "a1195c8f-03c0-42f8-8f8f-135b8e98d09c"),
    ("trapper", "se", "clarth", "44547cca-99d8-432c-bc41-ab629db97136"), ("trapper", "sw", "blood_rot", "720d540c-6f12-49b8-b409-3f84e8102bf2"),
    ("marksman", "se", "clarth", "9e0233b1-4a8a-4d81-99d8-53351b1cf28d"), ("marksman", "sw", "blood_rot", "19b7cc53-fb54-45dd-95ee-433ed538dd10"),
    ("medic", "se", "clarth", "20b62125-093d-41ca-a5d2-eb2f15d3fd25"), ("medic", "sw", "blood_rot", "e7aceef9-d12c-4c53-8817-318de3265c03"),
]
# game move name -> (PixelLab clip prefix after set_<class>_, ms a frame, has a contact frame)
MOVES = {"idle": ("idle", 100, False), "attack": ("attack1", 50, True), "attack2": ("attack2", 50, True), "block": ("block", 50, False),
         "hit": ("hit", 40, False), "power": ("power", 60, True), "death": ("death", 80, False)}

# The 7 Nov audit, every frame looked at: where the blow really lands when the furthest reach is the wind up or the
# follow through (a smear frame, the muzzle flash, the slam).
HIT = {"clarth_warden_se_power": 5, "blood_rot_warden_sw_attack2": 5, "blood_rot_warden_sw_power": 5,
       "clarth_skirmisher_se_attack2": 5, "blood_rot_skirmisher_sw_attack": 6, "blood_rot_skirmisher_sw_attack2": 5,
       "clarth_trapper_se_attack2": 5, "blood_rot_trapper_sw_attack": 4, "blood_rot_trapper_sw_attack2": 3,
       "clarth_marksman_se_attack2": 5, "blood_rot_marksman_sw_attack": 7, "blood_rot_marksman_sw_attack2": 9,
       "blood_rot_marksman_sw_power": 11, "clarth_medic_se_attack": 5, "clarth_medic_se_power": 8,
       "blood_rot_medic_sw_attack": 8, "blood_rot_medic_sw_power": 8}
# Their Marksman's south-west clips hold the rifle pointing right and fire away from our side: mirrored so he aims at us.
MIRROR = {("marksman", "sw")}

def body(im):
    return im.split()[3].point(lambda v: 255 if v > 20 else 0).getbbox()

report = []
for cls, d, tribe, cid in UNITS:
    z = zipfile.ZipFile(os.path.join(ZIPS, cid + ".zip"))
    names = z.namelist(); dd = "south-east" if d == "se" else "south-west"
    for move, (src, ms, contact) in MOVES.items():
        pre = "set_%s_%s" % (cls, src)
        if cls == "trapper" and d == "se" and move == "block": pre = "set_trapper_block2"  # the first one invented a shield
        anims = sorted({n.split("/animations/")[1].split("/")[0] for n in names if "/animations/" in n
                        and n.split("/animations/")[1].startswith(pre) and ("_%s_" % d) in n})
        if move == "attack2" and cls == "medic": move = "heal"
        assert len(anims) == 1, (cls, d, move, anims)
        files = sorted(n for n in names if "/animations/%s/%s/" % (anims[0], dd) in n and n.endswith(".png"))
        frames = [Image.open(io.BytesIO(z.read(n))).convert("RGBA") for n in files]
        if (cls, d) in MIRROR: frames = [f.transpose(Image.Transpose.FLIP_LEFT_RIGHT) for f in frames]
        w, h = frames[0].size
        assert all(f.size == (w, h) for f in frames), (anims[0], "frame sizes differ")
        soft = sum(1 for f in frames for a in f.getchannel("A").getdata() if 0 < a < 255)
        cols = 8; rows = (len(frames) + cols - 1) // cols
        sheet = Image.new("RGBA", (w * cols, h * rows), (0, 0, 0, 0))
        for i, f in enumerate(frames): sheet.paste(f, ((i % cols) * w, (i // cols) * h))
        b = body(frames[0])
        key = "%s_%s_%s_%s" % (tribe, cls, d, move)
        data = {"frames": [{"filename": "%s_%02d" % (key, i), "frame": {"x": (i % cols) * w, "y": (i // cols) * h, "w": w, "h": h},
                            "rotated": False, "trimmed": False, "spriteSourceSize": {"x": 0, "y": 0, "w": w, "h": h},
                            "sourceSize": {"w": w, "h": h}, "duration": ms} for i in range(len(frames))],
                "anchorX": (b[0] + b[2] - 1) / 2, "anchorY": b[3]}
        if contact:
            face = 1 if d == "se" else -1; best, hit = None, len(frames) // 2
            for i, f in enumerate(frames[1:-2], 1):
                fb = body(f)
                if not fb: continue
                reach = fb[2] if face > 0 else -fb[0]
                if best is None or reach > best: best, hit = reach, i
            data["hitFrame"] = HIT.get(key, hit)
        sheet.save(os.path.join(OUT, key + ".png"))
        json.dump(data, open(os.path.join(OUT, key + ".json"), "w"))
        report.append("%s %s, %d frames %dx%d, %d ms, anchor (%.1f, %d)%s%s" % (key, anims[0], len(frames), w, h, ms * len(frames),
                      data["anchorX"], data["anchorY"], ", hitFrame %d" % data["hitFrame"] if contact else "",
                      ", %d soft alpha px" % soft if soft else ""))
open(os.path.join(OUT, "sheets.txt"), "w").write("\n".join(report) + "\n")
print("\n".join(report)); print(len(report), "sheets")
