import json
import math
import subprocess
import sys
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import stage_region as SR

SP, RG = SR.SP, SR.RG

# stage_life.py [repaint] [STAGE ...]
# Jacob, 1 Nov 2026, on the five Bloodridge stills (stage_region.py region): "Move on to the moving layer now (flyers,
# other tribes, catapult shots, sun boats) and fix the stills after." Earlier the same night he asked for stages "like
# camp scene ... with layers cool things happening in the back like camp animated stuff and much more and fucking draw
# new species of flying things and add more variety unseen tribes or other designed tribes can also be in the
# background", and for the hybrid of Claude's three region plans. What moves in each stage is that plan's "moves".
# Built the way camp 5's war loop was (town_anim.py, town_fix.py): the tribes' own fighter sheets shrunk to the
# distance, the camp's PixelLab bursts, flames and smoke, the camp's sun boats; nothing paid. New here: six flying
# species and three foreign tribes, painted on Jacob's GPU into each stage's own 1x patch (PATCHES below; the first
# try painted them big on blank sheets and shrank them, and they looked like another game, 1 Nov) and cut out with
# BiRefNet. Everything that moves stays above row 285, the game's atmosphere clip
# (CampaignArtRegionalStage.cs). Positions were read off the five stills Jacob pasted on 1 Nov (half size), so they are
# good to a few pixels. A loop is 12 s at 12 fps and every schedule repeats inside it, so it loops without a seam.
W, H = SP.W, SP.H
FPS, SEC = 12, 12
N = FPS * SEC
TICK = 1000.0 / FPS
TOP = 285
OUT = SR.OUT
LIFE = OUT / "life"
SHEETS = HERE.parents[1] / "Assets/Resources/CampaignArt"  # the fighters' sheets, in this checkout
CAMP = RG.REPO / "Art/Studies/Camp-War-20261028"  # camp 5's effects and sun boats, in Jacob's main folder
FF = getattr(SR.BL, "FF", "ffmpeg")
# What gets painted in place, the camp's way (camp_v4_paint.py: props painted on a 1x patch of the camp itself, so they
# share its style, light and pixel size). Jacob, 1 Nov, after the sheet creatures: "why dont you use something else camp
# we used same artsyle for both props and layers", then "go". Each stage gets one 512 x 320 patch of its own still;
# the painter adds the creatures and people into the boxes (stage pixels), BiRefNet on his GPU cuts each one out, and
# they move at the size they were painted. One box is one pose, in order; creatures face left unless FACE says.
VULTURE = ("an ash vulture: a big carrion bird with ragged ash grey and black feathers, a bald pale head and a long neck "
           "wrapped in burial bandages, flying to the left")
SCARAB = ("a flying scarab: a big beetle seen from above, its corroded bronze shell split open and thin amber wings, "
          "flying")
MOTH = "a sun moth: a big moth seen from above with broad pale gold wings, each marked with a dark red sun disc"
BAT = "an ember bat: a black bat with ragged leathery wings and two glowing ember eyes, flying to the left"
BONE = ("a bone bird: a long thin bird skeleton of pale bones bound with strips of sepia rag, rag strips trailing from "
        "its wings and tail, flying to the left")
WHALE = ("a sky whale: a huge whale floating in the air, swimming to the left, pale grey skin, long trailing fins and "
         "patches of corroded bronze barnacles on its back")
SAILOR = ("a traveller of the Thundercoast, a foreign tribe: a long oiled dark blue sea coat and a brass helmet, "
          "walking to the right beside the caravan")
PILGRIMS = [("a pilgrim of Stoneveil, a foreign tribe: a dark violet hooded robe with red crystal shards on the "
             "shoulders"), "a pilgrim of the Echo Reed, a foreign tribe: a woven reed mask and a cloak of long green reeds",
            "a pilgrim of the Thundercoast, a foreign tribe: a long oiled dark blue sea coat and a brass helmet"]
KNEEL, STAND = ", kneeling in prayer facing right, head bowed", ", standing facing right"
TRADER = "a trader wrapped in burial bandages under a hood, standing at the stall"
BLOB = {"vulture": (52, 46, 50), "scarab": (120, 90, 50), "moth": (210, 180, 110), "bat": (24, 20, 26),
        "bonebird": (210, 200, 180), "whale": (170, 170, 176), "sailor": (40, 52, 90), "pilgrim": (90, 70, 80),
        "trader": (150, 130, 110)}
FACE = {"sailor": 1, "pilgrim": 1, "trader": 1, "moth": 0, "scarab": 0}  # 1 painted facing right, 0 seen from above
PATCHES = {
    "1": dict(rect=(0, 0, 512, 320), seed=2026110111,
              scene="the dawn sky over blood red hills, a caravan of covered wagons and shaggy pack beasts on a rise, "
                    "a low dark stone wall in front",
              adds="three ash vultures flying in the sky and three travellers walking beside the caravan",
              els=[("vulture", (40, 18, 104, 58), VULTURE + ", its wings raised high above its back"),
                   ("vulture", (180, 48, 244, 88), VULTURE + ", its wings spread out level"),
                   ("vulture", (300, 14, 364, 54), VULTURE + ", its wings pushed down below its body"),
                   ("sailor", (118, 214, 136, 246), SAILOR),
                   ("sailor", (208, 216, 226, 246), SAILOR),
                   ("sailor", (330, 214, 348, 246), SAILOR)]),
    "2": dict(rect=(560, 0, 1072, 320), seed=2026110112,
              scene="a bright morning sky over a grey stone fortress far away, black smoke rising, a big bronze "
                    "catapult and dark obelisks in front",
              adds="three flying scarabs in the sky",
              els=[("scarab", (660, 10, 700, 36), SCARAB + ", its wings spread wide"),
                   ("scarab", (760, 60, 800, 86), SCARAB + ", its wings raised"),
                   ("scarab", (860, 25, 900, 51), SCARAB + ", its wings half folded")]),
    "3": dict(rect=(0, 0, 512, 320), seed=2026110113,
              scene="a warm afternoon sky over jagged red mountains, a camp of patched tents on a terrace, black "
                    "obelisks and pillars with ram skulls in front",
              adds="three sun moths in the sky and seven pilgrims of foreign tribes on the terrace",
              els=[("moth", (60, 20, 88, 42), MOTH + ", its wings spread wide open"),
                   ("moth", (150, 30, 178, 52), MOTH + ", its wings raised up"),
                   ("moth", (250, 15, 278, 37), MOTH + ", its wings folded up"),
                   ("pilgrim", (86, 230, 102, 249), PILGRIMS[0] + KNEEL),
                   ("pilgrim", (110, 230, 126, 249), PILGRIMS[1] + KNEEL),
                   ("pilgrim", (134, 230, 150, 249), PILGRIMS[2] + KNEEL),
                   ("pilgrim", (158, 222, 172, 249), PILGRIMS[0] + STAND),
                   ("pilgrim", (270, 230, 286, 249), PILGRIMS[1] + KNEEL),
                   ("pilgrim", (292, 230, 308, 249), PILGRIMS[2] + KNEEL),
                   ("pilgrim", (314, 222, 328, 249), PILGRIMS[1] + STAND)]),
    "4": dict(rect=(300, 0, 812, 320), seed=2026110114,
              scene="a night sky with a big pale moon and stars over a red cliff city of tombs, a market stall with "
                    "caged mummified animals between two hooded statues",
              adds="four ember bats and three bone birds in the sky and two traders at the stall",
              els=[("bat", (330, 20, 356, 38), BAT + ", its wings raised"),
                   ("bat", (600, 25, 626, 43), BAT + ", its wings spread out"),
                   ("bat", (420, 110, 446, 128), BAT + ", its wings pushed down"),
                   ("bonebird", (330, 50, 390, 76), BONE + ", its wings raised"),
                   ("bonebird", (640, 30, 700, 56), BONE + ", its wings spread out level"),
                   ("bonebird", (700, 95, 760, 121), BONE + ", its wings pushed down"),
                   ("trader", (612, 234, 632, 264), TRADER),
                   ("trader", (744, 234, 764, 264), TRADER)]),
    "5": dict(rect=(864, 0, 1376, 320), seed=2026110115,
              scene="a crimson eclipse sky with red light beams over a dark cliff with ragged banners and ram skulls",
              adds="one sky whale far off in the sky",
              els=[("whale", (960, 15, 1240, 105), WHALE)]),
}
FLAP = {1: [0], 2: [0, 1], 3: [0, 1, 2, 1]}

# What moves in each stage. Flyers fly an orbit (cx, cy, rx, ry), a closed path or a drift; boats cross, orbit or
# follow keys (t, x, y); fighters stand at (x, feet) and fight, or walk keys over a span of frames from an offset;
# painted people stay where they were painted (kneeling ones bow) or walk; shots fly from a catapult to a fort and
# burst; glow boxes flicker their ember pixels; eyes blink.
STAGE_FX = {
    "1": dict(
        tint=(1.0, 0.92, 0.92),
        flyers=[dict(kind="vulture", orbit=(700, 92, 150, 22), laps=1, phase=0.0),
                dict(kind="vulture", orbit=(700, 92, 150, 22), laps=1, phase=2.8),
                dict(kind="vulture", orbit=(300, 66, 90, 14), laps=-1, phase=1.0)],
        boats=[dict(sprite="barque_mid", cross=(78, -1)),
               dict(sprite="barque_far", orbit=(1050, 116, 200, 8), laps=1, phase=1.0)],
        people=[dict(name="sailor-%d" % i, walk=[(0, -20, 246), (N, 400, 246)], span=N, offset=o)
                for i, o in ((0, 0), (1, 48), (2, 96))],
        fighters=[dict(base="clarth_skirmisher_sw", at=(1238, 171), h=15, face=1),
                  dict(base="clarth_trapper_sw", at=(1256, 170), h=15, face=1),
                  dict(base="blood_rot_warden_se", at=(1278, 169), h=16, face=-1),
                  dict(base="blood_rot_skirmisher_se", at=(1296, 170), h=15, face=-1),
                  dict(base="blood_rot_trapper_se", at=(1314, 172), h=15, face=-1)],
        fires=[(450, 284, 0.55), (700, 266, 0.55), (918, 284, 0.55)],
        glow=[(540, 60, 625, 290), (775, 45, 860, 290), (400, 90, 445, 290), (930, 170, 985, 290),
              (1115, 190, 1165, 290)]),
    "2": dict(
        tint=(1.0, 1.0, 1.0),
        shots=[dict(at=0, src=(522, 72), dst=(500, 150), peak=10, dur=30),  # on the forts, clear of the barrels
               dict(at=36, src=(1042, 104), dst=(1062, 150), peak=24, dur=30),
               dict(at=72, src=(522, 72), dst=(496, 168), peak=6, dur=30),
               dict(at=108, src=(1042, 104), dst=(1056, 164), peak=20, dur=30)],
        flashes=[(485, 140, 610, 200), (905, 135, 1075, 195)],
        burning=[(88, 214), (150, 228), (335, 212)],
        flyers=[dict(kind="scarab", path=[(380, 60), (520, 30), (680, 80), (840, 40), (1100, 70), (880, 110),
                                          (640, 60), (450, 105)], laps=1, phase=p) for p in (0.0, 0.33, 0.66)],
        flies=[(505, 236, 16, 10), (980, 240, 14, 8)],
        fighters=[dict(base="clarth_warden_sw", h=26, walk=[(0, 1400, 268), (60, 1236, 268)], span=60, offset=o,
                       face=-1) for o in (0, 18)] +
                 [dict(base="clarth_medic_sw", h=24, walk=[(0, 1400, 268), (60, 1236, 268)], span=60, offset=o,
                       face=-1) for o in (72, 90)] +
                 [dict(base="blood_rot_warden_se", h=28, walk=[(0, 1400, 268), (60, 1236, 268)], span=60, offset=o,
                       face=-1) for o in (36, 108)],
        glow=[(260, 80, 310, 280), (440, 80, 480, 280), (600, 80, 640, 280), (770, 70, 805, 280),
              (815, 90, 850, 280), (1095, 95, 1130, 280), (395, 250, 440, 285), (865, 245, 905, 285)]),
    "3": dict(
        tint=(1.05, 0.95, 0.85),
        boats=[dict(sprite="barque_far", orbit=(850, 110, 260, 8), laps=-1, phase=0.5),
               dict(sprite="barque_mid", keys=[(0, W + 80, 100), (40, 1255, 176), (90, 1255, 176), (130, 600, 30),
                                               (143, 420, -60), (N, W + 80, 100)])],
        people=[dict(name="pilgrim-%d" % i, bow=None if i in (3, 6) else 9 * i) for i in range(7)],
        fighters=[dict(base="clarth_warden_sw", h=34, walk=[(0, 590, 284), (24, 688, 284), (48, 690, 248)], span=48,
                       offset=o, face=1) for o in (0, 72)] +
                 [dict(base="clarth_skirmisher_sw", h=32, walk=[(0, 590, 284), (24, 688, 284), (48, 690, 248)],
                       span=48, offset=o, face=1) for o in (16, 88)] +
                 [dict(base="blood_rot_warden_se", h=36, walk=[(0, 590, 284), (24, 688, 284), (48, 690, 248)],
                       span=48, offset=o, face=1) for o in (32, 104)],
        fires=[(312, 268, 0.55), (563, 226, 0.5), (690, 220, 0.6), (820, 224, 0.5), (1062, 268, 0.55)],
        flyers=[dict(kind="moth", orbit=(312, 250, 18, 9), laps=3, phase=0.0, jitter=3),
                dict(kind="moth", orbit=(1062, 250, 20, 10), laps=-3, phase=1.0, jitter=3),
                dict(kind="moth", orbit=(690, 200, 26, 12), laps=2, phase=2.0, jitter=3),
                dict(kind="moth", orbit=(563, 208, 16, 8), laps=-4, phase=3.0, jitter=2)],
        glow=[(600, 60, 790, 235), (470, 60, 510, 290), (560, 60, 600, 230), (780, 60, 820, 230),
              (870, 60, 910, 290), (920, 100, 970, 290), (1015, 140, 1055, 290)]),
    "4": dict(
        tint=(0.62, 0.68, 0.9),
        eyes=[(525, 150, 575, 205), (815, 150, 870, 205)],
        fighters=[dict(base="blood_rot_warden_se", at=(1040, 238), h=14, face=1),
                  dict(base="blood_rot_skirmisher_se", at=(1056, 239), h=13, face=1),
                  dict(base="skull_warden_sw", at=(1076, 238), h=14, face=-1),
                  dict(base="skull_marksman_sw", at=(1094, 239), h=13, face=-1)],
        boats=[dict(sprite="barque_mid", cross=(104, 1), lanterns=True)],
        lanterns=(0, 130, W, 285),
        glow=[(0, 215, W, 262)],
        flyers=[dict(kind="bat", path=[(420, 40), (520, 15), (640, 60), (560, 95), (460, 80)], laps=2, phase=p,
                     jitter=2) for p in (0.0, 0.25, 0.5, 0.75)] +
               [dict(kind="bat", path=[(900, 40), (1050, 70), (1200, 30), (1100, 90)], laps=-2, phase=p, jitter=2)
                for p in (0.1, 0.6)] +
               [dict(kind="bonebird", orbit=(1000, 70, 200, 18), laps=1, phase=0.0),
                dict(kind="bonebird", orbit=(250, 60, 150, 14), laps=-1, phase=2.0)],
        people=[dict(name="trader-0"), dict(name="trader-1")]),
    "5": dict(
        tint=(1.0, 0.62, 0.55),
        ring=(295, 10, 400, 112),
        embers=70,
        fighters=[dict(base=b, at=(x, 279), h=22, face=1) for b, x in
                  (("blood_rot_warden_se", 470), ("blood_rot_skirmisher_se", 492), ("blood_rot_trapper_se", 514),
                   ("blood_rot_medic_se", 536))] +
                 [dict(base=b, at=(x, 279), h=22, face=-1) for b, x in
                  (("blood_rot_marksman_se", 846), ("blood_rot_warden_se", 868), ("blood_rot_skirmisher_se", 890),
                   ("blood_rot_trapper_se", 912))] +
                 [dict(base=b, at=(x, 133), h=16, face=-1) for b, x in
                  (("hunter_warden_sw", 1070), ("hunter_captain_sw", 1094), ("hunter_skirmisher_sw", 1120))],
        flyers=[dict(kind="whale", drift=(1100, 60, 30, 3))],
        fires=[(335, 248, 0.55), (583, 250, 0.55), (808, 252, 0.55), (1055, 248, 0.55)],
        glow=[(670, 170, 730, 255), (430, 160, 470, 285), (570, 120, 610, 285), (790, 120, 830, 285),
              (930, 160, 970, 285)]),
}


# ------------------------------------------------------------------ painting in place and cutting out

def neg_patch():
    base = json.loads(RG.L3.BASE.read_text())["5"]["inputs"]["text"]
    for w in (" creatures,", " letters,", " people,", " guards,", " human figures,"):
        base = base.replace(w, "")
    return base + (", vector art, flat cartoon, smooth shapes, blurry, text, letters, user interface, HUD, frames, "
                   "borders, watermark, signature, giant, colossal, huge figures, close up, cropped, split screen")


def pbb(b, rect):
    x0, y0, x1, y1 = b
    px, py, qx, qy = rect
    w, h = qx - px, qy - py
    return [int((y0 - py) / h * 1000), int((x0 - px) / w * 1000), int((y1 - py) / h * 1000), int((x1 - px) / w * 1000)]


def paint_patch(k):
    """The stage's own 1x patch, soft blobs where each new thing goes, painted from step 3 like camp_v4_paint's
    patches, with the stage's light and palette."""
    P, o = PATCHES[k], SR.STAGES[k]
    px, py, qx, qy = P["rect"]
    init = Image.open(OUT / ("stage-%s.png" % k)).convert("RGB").crop(P["rect"])
    d = ImageDraw.Draw(init)
    for kind, (x0, y0, x1, y1), _ in P["els"]:
        d.ellipse((x0 - px + 2, y0 - py + 2, x1 - px - 2, y1 - py - 2), fill=BLOB[kind])
    pos = json.dumps({
        "high_level_description": "A piece of a pixel art battle stage background at its own pixel size: " +
                                  P["scene"] + ". In it: " + P["adds"] + ", small, in the same pixel art, colours and "
                                  "light as everything around them. " + SP.STYLE,
        "style_description": {"medium": "pixel_art", "art_style": RG.L3.ART,
                              "aesthetics": "one coherent pixel art scene, coherent pixel clusters, small readable "
                                            "silhouettes, everything lit by the same light",
                              "lighting": o["light"], "color_palette": o["pal"]},
        "compositional_deconstruction": {"background": P["scene"],
                                         "elements": [{"type": "obj", "bbox": pbb(b, P["rect"]), "desc": desc}
                                                      for _, b, desc in P["els"]]}})
    SP.OUT = LIFE
    raw = SP.run("life-patch-%s" % k, init, pos, P["seed"], step=3, neg=neg_patch())
    SP.OUT = OUT
    raw.save(LIFE / ("patch-%s-raw.png" % k))


def cutout(tag, im):
    """BiRefNet on Jacob's GPU (the core RemoveBackground node, as town_fix.cutout), sent and fetched over the API."""
    name = SP.upload(im, tag + ".png", sub="ks-bloodridge-life")
    g = {"1": {"class_type": "LoadImage", "inputs": {"image": name}},
         "2": {"class_type": "LoadBackgroundRemovalModel", "inputs": {"bg_removal_name": "birefnet-general.safetensors"}},
         "3": {"class_type": "RemoveBackground", "inputs": {"bg_removal_model": ["2", 0], "image": ["1", 0]}},
         "4": {"class_type": "MaskToImage", "inputs": {"mask": ["3", 0]}},
         "5": {"class_type": "SaveImage", "inputs": {"images": ["4", 0], "filename_prefix": "KS-Bloodridge-Life/" + tag}}}
    try:
        pid = SP.G.api("/prompt", {"prompt": g, "front": True})["prompt_id"]
    except SP.urllib.error.HTTPError as e:
        raise SystemExit("The painter refused %s: %s" % (tag, e.read().decode("utf-8", "replace")[:4000]))
    return np.asarray(SP.result(pid).convert("L").resize(im.size, Image.BOX)).astype(np.float64) / 255


def components(m):
    """8 connected groups of a mask (no scipy needed)."""
    lab = np.zeros(m.shape, np.int32)
    h, w = m.shape
    n = 0
    for y, x in zip(*np.nonzero(m)):
        if lab[y, x]:
            continue
        n += 1
        lab[y, x] = n
        q = deque([(y, x)])
        while q:
            cy, cx = q.popleft()
            for ny in (cy - 1, cy, cy + 1):
                for nx in (cx - 1, cx, cx + 1):
                    if 0 <= ny < h and 0 <= nx < w and m[ny, nx] and not lab[ny, nx]:
                        lab[ny, nx] = n
                        q.append((ny, nx))
    return lab, n


def cut_patch(k):
    """Each new thing cut out of the painted patch at the size it was painted: BiRefNet's mask of its box (grown 6 px,
    shown to BiRefNet 4x big), the group nearest the box centre; if BiRefNet finds nothing there, the pixels that
    differ from the box's border. Saved as kind-i.png with where it was painted in cuts.json."""
    P = PATCHES[k]
    px, py, qx, qy = P["rect"]
    raw = Image.open(LIFE / ("patch-%s-raw.png" % k)).convert("RGB")
    a = np.asarray(raw).astype(np.float64)
    cuts = json.loads((LIFE / "cuts.json").read_text()) if (LIFE / "cuts.json").exists() else {}
    count = {}
    for j, (kind, (x0, y0, x1, y1), _) in enumerate(P["els"]):
        i = count.get(kind, 0)
        count[kind] = i + 1
        bx0, by0 = max(0, x0 - px - 6), max(0, y0 - py - 6)
        bx1, by1 = min(qx - px, x1 - px + 6), min(qy - py, y1 - py + 6)
        reg = a[by0:by1, bx0:bx1]
        crop = raw.crop((bx0, by0, bx1, by1))
        big = cutout("life-%s-%d" % (k, j), crop.resize((crop.width * 4, crop.height * 4), Image.NEAREST))
        m = np.asarray(Image.fromarray((big * 255).astype(np.uint8)).resize(crop.size, Image.BOX)) > 127
        if m.sum() < 0.08 * m.size:
            edge = np.concatenate([reg[:3].reshape(-1, 3), reg[-3:].reshape(-1, 3), reg[:, :3].reshape(-1, 3),
                                   reg[:, -3:].reshape(-1, 3)])
            m = np.abs(reg - np.median(edge, 0)).sum(2) > 60
        (LIFE / ("%s-%d.png" % (kind, i))).unlink(missing_ok=True)
        cuts.pop("%s-%d" % (kind, i), None)
        lab, n = components(m)
        if n == 0:
            print("nothing cut for", kind, i, flush=True)
            continue
        cy, cx = (by1 - by0) / 2, (bx1 - bx0) / 2
        best = min(range(1, n + 1), key=lambda L: np.hypot(*(np.argwhere(lab == L).mean(0) - (cy, cx))) -
                   0.05 * (lab == L).sum())
        keep = lab == best
        ys, xs = np.nonzero(keep)
        sl = (slice(ys.min(), ys.max() + 1), slice(xs.min(), xs.max() + 1))
        Image.fromarray(np.dstack([reg[sl], keep[sl] * 255.0]).astype(np.uint8), "RGBA").save(
            LIFE / ("%s-%d.png" % (kind, i)))
        cuts["%s-%d" % (kind, i)] = [int(px + bx0 + xs.min()), int(py + by0 + ys.min())]
    (LIFE / "cuts.json").write_text(json.dumps(cuts, indent=1))


def creatures(ks, repaint=False):
    LIFE.mkdir(parents=True, exist_ok=True)
    for k in ks:
        if repaint or not (LIFE / ("patch-%s-raw.png" % k)).exists():
            paint_patch(k)
        cut_patch(k)
    look_creatures()


def look_creatures():
    """Every cut at its own size and at 4x, one row a stage's patch: look-creatures.png."""
    rows = []
    for k, P in sorted(PATCHES.items()):
        seen, row = {}, []
        for kind, _, _ in P["els"]:
            i = seen.get(kind, 0)
            seen[kind] = i + 1
            p = LIFE / ("%s-%d.png" % (kind, i))
            if p.exists():
                im = Image.open(p).convert("RGBA")
                row.append(im.resize((im.width * 4, im.height * 4), Image.NEAREST))
        rows.append(("%s  %s" % (k, ", ".join(sorted(set(e[0] for e in P["els"])))), row))
    hgt = sum(max([t.height for t in r] + [20]) + 34 for _, r in rows)
    s = Image.new("RGB", (1376, hgt), (40, 36, 44))
    d = ImageDraw.Draw(s)
    y = 0
    for name, row in rows:
        d.text((8, y + 6), name, fill=(239, 223, 190), font=font(18))
        x = 8
        for t in row:
            if x + t.width > 1376:
                break
            s.paste(t, (x, y + 30), t)
            x += t.width + 16
        y += max([t.height for t in row] + [20]) + 34
    s.save(OUT / "look-creatures.png")


def font(size):
    try:
        return ImageFont.truetype("arial.ttf", size)
    except OSError:
        try:
            return ImageFont.load_default(size)
        except TypeError:
            return ImageFont.load_default()


# ------------------------------------------------------------------ shrinking sprites to the distance

def shrink(rgba, width=None, scale=None, ncol=12, outline=True, ember=False):
    """Box shrink with the colours cut to a few, ember and bone accents kept, and a dark one pixel outline, the way
    town_anim.shrink_fixed shrinks the camp's fighters."""
    a = rgba.astype(np.float64)
    al = a[..., 3] > 127
    s = scale if scale is not None else width / a.shape[1]
    w, h = max(1, round(a.shape[1] * s)), max(1, round(a.shape[0] * s))

    def down(x):
        return np.asarray(Image.fromarray(x.astype(np.float32)).resize((w, h), Image.BOX)).astype(np.float64)

    den = down(al)
    src = a[..., :3]
    col = np.dstack([down(src[..., c] * al) for c in range(3)]) / np.maximum(den[..., None], 1e-6)
    m = den >= 0.45
    if al.sum() > 4:
        px = np.ascontiguousarray(src[al]).astype(np.uint8)
        q = Image.fromarray(px[None]).quantize(colors=ncol, method=Image.Quantize.MEDIANCUT)
        qp = np.array(q.getpalette()[:3 * ncol], np.float64).reshape(-1, 3)
        col = qp[((col[:, :, None, :] - qp[None, None]) ** 2).sum(3).argmin(2)]
    else:
        qp = np.array([[30.0, 24, 28]])
    r, g, b = src[..., 0], src[..., 1], src[..., 2]
    lu = r * 0.299 + g * 0.587 + b * 0.114
    accents = [(al & (lu > 190) & (np.abs(r - b) < 40), 0.3)]
    if ember:  # only Bloodridge sprites: on anything else it painted every orange-ish patch bright orange (1 Nov)
        accents.append((al & (r > 180) & (g > 50) & (g < 175) & (b < 100) & (lu > 105), 0.07))
    for acc, need in accents:
        frac = down(acc)
        mean = np.dstack([down(src[..., c] * acc) for c in range(3)]) / np.maximum(frac[..., None], 1e-6)
        use = (frac >= need) & m
        col[use] = mean[use]
    if not outline:
        return col, m
    out = np.zeros((h + 2, w + 2, 3))
    out[1:-1, 1:-1] = col
    mm = np.zeros((h + 2, w + 2), bool)
    mm[1:-1, 1:-1] = m
    ring = (np.asarray(Image.fromarray((mm * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3))) > 0) & ~mm
    out[ring] = qp[qp.sum(1).argmin()] * 0.7
    return out, mm | ring


CACHE = {}


def species(kind):
    """A species' painted poses at the size they were painted, centred on one canvas so it does not jump between
    wing beats."""
    key = ("sp", kind)
    if key not in CACHE:
        files = sorted(LIFE.glob(kind + "-*.png"), key=lambda q: int(q.stem.split("-")[-1]))
        raw = [np.asarray(Image.open(q).convert("RGBA")).astype(np.float64) for q in files]
        if not raw:
            print("no", kind, "cut yet", flush=True)
            CACHE[key] = None
            return None
        hh, ww = max(r.shape[0] for r in raw), max(r.shape[1] for r in raw)
        out = []
        for r in raw:
            c, m = np.zeros((hh, ww, 3)), np.zeros((hh, ww), bool)
            y0, x0 = (hh - r.shape[0]) // 2, (ww - r.shape[1]) // 2
            c[y0:y0 + r.shape[0], x0:x0 + r.shape[1]] = r[..., :3]
            m[y0:y0 + r.shape[0], x0:x0 + r.shape[1]] = r[..., 3] > 127
            out.append((c, m))
        CACHE[key] = out
    return CACHE[key]


def person(name):
    """One painted person, with the spot it was painted at."""
    key = ("person", name)
    if key not in CACHE:
        p = LIFE / (name + ".png")
        cuts = json.loads((LIFE / "cuts.json").read_text()) if (LIFE / "cuts.json").exists() else {}
        if not p.exists() or name not in cuts:
            print("no", name, "cut yet", flush=True)
            CACHE[key] = None
            return None
        r = np.asarray(Image.open(p).convert("RGBA")).astype(np.float64)
        CACHE[key] = (r[..., :3], r[..., 3] > 127, cuts[name])
    return CACHE[key]


def sheet_frames(base, anim):
    meta = json.loads((SHEETS / ("%s_%s.json" % (base, anim))).read_text())["frames"]
    meta = meta if isinstance(meta, list) else list(meta.values())
    sheet = np.asarray(Image.open(SHEETS / ("%s_%s.png" % (base, anim))).convert("RGBA"))
    return [sheet[q["y"]:q["y"] + q["h"], q["x"]:q["x"] + q["w"]] for q in (f["frame"] for f in meta)], \
        np.array([f["duration"] for f in meta], float)


def fighter(base, anim, h):
    """town_anim.clip: one sheet shrunk so the idle stance is h tall, every frame cut by the union box so the feet
    stay put."""
    key = ("ft", base, anim, h)
    if key not in CACHE:
        idle, _ = sheet_frames(base, "idle")
        ys = np.nonzero(idle[0][..., 3] > 127)[0]
        s = h / (ys.max() - ys.min() + 1)
        frames, durs = sheet_frames(base, anim)
        fy, fx = np.nonzero(frames[0][..., 3] > 127)
        uy, ux = np.nonzero(np.any([f[..., 3] > 127 for f in frames], axis=0))
        y0, y1, x0, x1 = uy.min(), uy.max() + 1, ux.min(), ux.max() + 1
        CACHE[key] = dict(frames=[shrink(f[y0:y1, x0:x1], scale=s, ncol=10, ember=base.startswith("blood"))
                                  for f in frames], cum=np.cumsum(durs),
                          total=float(durs.sum()), ground=round((fy.max() + 1 - y0) * s) + 1,
                          cx=round(((fx.min() + fx.max()) / 2 - x0) * s) + 1)
    return CACHE[key]


SMOKE = np.array([[62.0, 56, 60], [96, 88, 92], [132, 126, 130]])  # town_fix.SMOKE: the camp's pale puffs, darkened


def fxload(name, scale=1.0, ramp=None):
    """A camp effect, shrunk; with a ramp its colours are replaced by the ramp's three by brightness."""
    key = ("fx", name, scale, ramp is None)
    if key not in CACHE:
        meta = json.loads((CAMP / "fx" / (name + ".json")).read_text())["frames"]
        meta = meta if isinstance(meta, list) else list(meta.values())
        im = np.asarray(Image.open(CAMP / "fx" / (name + ".png")).convert("RGBA"))
        fr = [shrink(im[q["y"]:q["y"] + q["h"], q["x"]:q["x"] + q["w"]], scale=scale, ncol=10, outline=False)
              for q in (f["frame"] for f in meta)]
        if ramp is not None:
            px = np.vstack([rgb[al] for rgb, al in fr if al.any()])
            qs = np.percentile(px @ np.array([0.299, 0.587, 0.114]), [33, 66])
            fr = [(ramp[np.digitize(rgb @ np.array([0.299, 0.587, 0.114]), qs)], al) for rgb, al in fr]
        pv = meta[0].get("pivot", {"x": 0.5, "y": 0.5})
        h, w = fr[0][1].shape
        CACHE[key] = dict(frames=fr, ox=pv["x"] * w, oy=pv["y"] * h)
    return CACHE[key]


def boat(name):
    key = ("boat", name)
    if key not in CACHE:
        p = CAMP / ("layer-2-ship-%s.png" % name.replace("_", "-"))
        a = np.asarray(Image.open(p).convert("RGBA")).astype(np.float64) if p.exists() else None
        CACHE[key] = None if a is None else (a[..., :3], a[..., 3] > 127)
    return CACHE[key]


# ------------------------------------------------------------------ drawing

def blit(cv, rgb, al, x0, y0, flip=False, tint=None):
    if flip:
        rgb, al = rgb[:, ::-1], al[:, ::-1]
    h, w = al.shape
    x0, y0 = int(round(x0)), int(round(y0))
    xa, ya, xb, yb = max(0, x0), max(0, y0), min(W, x0 + w), min(TOP, y0 + h)
    if xa >= xb or ya >= yb:
        return
    sub = al[ya - y0:yb - y0, xa - x0:xb - x0]
    src = rgb[ya - y0:yb - y0, xa - x0:xb - x0]
    if tint is not None:
        src = src * np.array(tint)
    cv[ya:yb, xa:xb][sub] = src[sub]


def dot(cv, x, y, col):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < TOP:
        cv[y, x] = col


def ember_mask(base, box, yellow=False, dark=125):
    """Ember (or lantern yellow) pixels inside a box, only where the area around them is dark: cracks in stone,
    eyes in a hood, lights in a doorway. Bright sky of the same colour never flickers."""
    x0, y0, x1, y1 = box
    r, g, b = base[..., 0], base[..., 1], base[..., 2]
    lu = r * 0.299 + g * 0.587 + b * 0.114
    if yellow:
        e = (r > 215) & (g > 165) & (b < 150)
    else:
        e = (r > 190) & (g > 50) & (g < 200) & (b < 120) & (lu > 100)
    if dark:
        e &= np.asarray(Image.fromarray(lu.clip(0, 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(6))) < dark
    m = np.zeros((H, W), bool)
    m[y0:min(y1, TOP), x0:x1] = e[y0:min(y1, TOP), x0:x1]
    return m


def ring_mask(base, box):
    """The burning ring round the black sun: ember pixels in a band just outside the dark disc."""
    x0, y0, x1, y1 = box
    lu = base[y0:y1, x0:x1].mean(2)
    ys, xs = np.nonzero(lu < 40)
    m = np.zeros((H, W), bool)
    if len(ys) < 20:
        return m, None
    cy, cx = ys.mean(), xs.mean()
    rad = math.sqrt(len(ys) / math.pi)
    yy, xx = np.mgrid[y0:y1, x0:x1]
    dist = np.hypot(yy - (y0 + cy), xx - (x0 + cx))
    e = ember_mask(base, box, dark=0)[y0:y1, x0:x1]
    m[y0:y1, x0:x1] = e & (dist > rad - 2) & (dist < rad + 12)
    return m, (x0 + cx, y0 + cy, rad)


def path_at(pts, u):
    """A point u (0 to 1) of the way round a closed path, and which way it is heading."""
    seg = [math.dist(pts[i], pts[(i + 1) % len(pts)]) for i in range(len(pts))]
    d = (u % 1.0) * sum(seg)
    for i, s in enumerate(seg):
        if d <= s or i == len(seg) - 1:
            a, b = pts[i], pts[(i + 1) % len(pts)]
            f = d / s if s else 0
            return a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f, (1 if b[0] > a[0] else -1)
        d -= s


def keys_at(keys, t):
    for (t0, x0, y0), (t1, x1, y1) in zip(keys, keys[1:]):
        if t0 <= t <= t1:
            f = (t - t0) / max(1, t1 - t0)
            return x0 + (x1 - x0) * f, y0 + (y1 - y0) * f, (1 if x1 > x0 else -1)
    return keys[-1][1], keys[-1][2], 1


def schedule(rng, base, h):
    """A fighter's loop: idle, attack and hit in turns, filling the loop exactly."""
    segs, t = [], 0
    while t < N:
        anim = str(rng.choice(["idle", "attack", "hit"], p=[0.45, 0.4, 0.15]))
        if anim == "idle":
            segs.append((t, "idle", int(rng.integers(0, 4000))))
            t += int(rng.integers(8, 30))
        else:
            segs.append((t, anim, 0))
            t += int(math.ceil(fighter(base, anim, h)["total"] / TICK))
    return segs


def frame_of(c, anim, ms):
    ms = ms % c["total"] if anim == "idle" else min(ms, c["total"] - 1)
    return min(int(np.searchsorted(c["cum"], ms, side="right")), len(c["frames"]) - 1)


def draw_fighter(cv, f, t, tint):
    side = f["base"].split("_")[-1]
    flip = f["face"] != (1 if side == "se" else -1)
    if "walk" in f:
        u = (t - f["offset"]) % N
        if u >= f["span"]:
            return
        x, y, _ = keys_at(f["walk"], u)
        c = fighter(f["base"], "idle", f["h"])
        rgb, al = c["frames"][frame_of(c, "idle", u * TICK)]
        y -= (u // 3) % 2  # walking is a slide with a 1 px bob (camp, 28 Oct)
    else:
        x, y = f["at"]
        t0, anim, phase = [s for s in f["segs"] if s[0] <= t][-1]
        c = fighter(f["base"], anim, f["h"])
        rgb, al = c["frames"][frame_of(c, anim, (t - t0) * TICK + phase)]
    cx = al.shape[1] - 1 - c["cx"] if flip else c["cx"]
    blit(cv, rgb, al, x - cx, y - c["ground"], flip, tint)


def flyer_at(f, t):
    if "orbit" in f:
        cx, cy, rx, ry = f["orbit"]
        ang = 2 * math.pi * f["laps"] * t / N + f["phase"]
        x, y = cx + rx * math.cos(ang), cy + ry * math.sin(ang)
        face = 1 if -math.sin(ang) * f["laps"] > 0 else -1
    elif "path" in f:
        x, y, face = path_at(f["path"], f["laps"] * t / N + f["phase"])
        if f["laps"] < 0:
            face = -face
    else:
        cx, cy, ax, ay = f["drift"]
        x = cx + ax * math.sin(2 * math.pi * t / N)
        y = cy + ay * math.sin(4 * math.pi * t / N)
        face = 1 if math.cos(2 * math.pi * t / N) > 0 else -1
    j = f.get("jitter", 0)
    if j:
        x += j * math.sin(2 * math.pi * 7 * t / N + f["phase"] * 3)
        y += j * math.cos(2 * math.pi * 11 * t / N + f["phase"] * 5)
    return x, y, face


def boat_at(b, t, w):
    if "cross" in b:
        y, d = b["cross"]
        span = W + w + 60
        x = (W + w / 2 + 30 - span * t / N) if d < 0 else (-w / 2 - 30 + span * t / N)
        return x, y + 2 * math.sin(2 * math.pi * 6 * t / N), d
    if "orbit" in b:
        cx, cy, rx, ry = b["orbit"]
        ang = 2 * math.pi * b["laps"] * t / N + b["phase"]
        return cx + rx * math.cos(ang), cy + ry * math.sin(ang), (1 if -math.sin(ang) * b["laps"] > 0 else -1)
    x, y, d = keys_at(b["keys"], t)
    return x, y + 2 * math.sin(2 * math.pi * 6 * t / N), d


BOULDER = np.array([[64.0, 48, 40], [112, 88, 70], [150, 124, 100]])
TURQ = np.array([[120.0, 255, 240], [60, 210, 200]])


def render(k):
    """The stage's living loop over its still: stage-k-life.mp4 (2x, three loops) and the frames for the look."""
    o = STAGE_FX[k]
    base = np.asarray(Image.open(OUT / ("stage-%s.png" % k)).convert("RGB")).astype(np.float32)
    rng = np.random.default_rng(int(SR.STAGES[k]["seed"]))
    tint = o.get("tint")
    for f in o.get("fighters", ()):
        if "at" in f:
            f["segs"] = schedule(rng, f["base"], f["h"])
    glow = np.zeros((H, W), bool)
    for box in o.get("glow", ()):
        glow |= ember_mask(base, box)
    gy, gx = np.nonzero(glow)
    gph = rng.uniform(0, 2 * math.pi, (H // 8 + 1, W // 8 + 1))[gy // 8, gx // 8]
    lan = ember_mask(base, o["lanterns"], yellow=True) if "lanterns" in o else np.zeros((H, W), bool)
    ly, lx = np.nonzero(lan)
    lph = rng.uniform(0, 2 * math.pi, (H // 6 + 1, W // 6 + 1))[ly // 6, lx // 6]
    eyes = np.zeros((H, W), bool)
    for box in o.get("eyes", ()):
        eyes |= ember_mask(base, box, dark=110)
    ring, sun = ring_mask(base, o["ring"]) if "ring" in o else (np.zeros((H, W), bool), None)
    flame = fxload("ks-camp-town-flame", 1.0)
    burst = fxload("ks-camp-war-explosion-20", 1.2)
    puff = fxload("ks-camp-smoke-puff", 0.6, SMOKE)
    fire = fxload("ks-camp-building-fire", 0.5)
    big = fxload("ks-camp-smoke-puff-big", 0.5, SMOKE)
    emb = [dict(x=float(rng.uniform(0, W)), y0=float(rng.uniform(0, TOP)), k=int(rng.integers(1, 3)),
                sw=float(rng.uniform(2, 6)), ph=float(rng.uniform(0, 6.3)),
                c=[(255, 170, 60), (255, 220, 120), (230, 90, 40)][int(rng.integers(0, 3))])
           for _ in range(o.get("embers", 0))]
    flies = [(cx, cy, r, float(rng.uniform(0, 6.3)), int(rng.integers(3, 7))) for cx, cy, r, n in o.get("flies", ())
             for _ in range(n)]
    sparks = [(float(rng.uniform(0, 6.3)), int(rng.integers(0, N))) for _ in range(14)] if "ring" in o else []
    frames = []
    for t in range(N):
        cv = base.copy()
        if len(gy):
            cv[gy, gx] = np.clip(base[gy, gx] * (1 + 0.16 * np.sin(2 * math.pi * 3 * t / N + gph))[:, None], 0, 255)
        if len(ly):
            cv[ly, lx] = np.clip(base[ly, lx] * (1 + 0.12 * np.sin(2 * math.pi * 5 * t / N + lph))[:, None], 0, 255)
        if eyes.any():
            shut = (t % 48) in (40, 41, 42) or (t % 72) in (20, 21)
            cv[eyes] = base[eyes] * (0.3 if shut else 1.0 + 0.15 * math.sin(2 * math.pi * 4 * t / N))
        if ring.any():
            cv[ring] = np.clip(base[ring] * (1.05 + 0.2 * math.sin(2 * math.pi * 6 * t / N)), 0, 255)
            cx, cy, rad = sun
            for ang, t0 in sparks:
                u = (t - t0) % N
                if u < 16:
                    rr = rad + 6 + u * 1.5
                    dot(cv, cx + rr * math.cos(ang), cy + rr * math.sin(ang) + u * 0.4,
                        (255, 220, 120) if u < 6 else (230, 90, 40))
        for b in o.get("boats", ()):
            sp = boat(b["sprite"])
            if sp is None:
                continue
            rgb, al = sp
            x, y, d = boat_at(b, t, al.shape[1])
            blit(cv, rgb, al, x - al.shape[1] / 2, y - al.shape[0] / 2, d > 0, tint)
            if b.get("lanterns") and (t // 3) % 4:
                dot(cv, x - al.shape[1] * 0.3, y, (255, 214, 120))
                dot(cv, x + al.shape[1] * 0.3, y - 1, (255, 190, 90))
        for f in o.get("flyers", ()):
            fr = species(f["kind"])
            if fr is None:
                continue
            x, y, face = flyer_at(f, t)
            seq = FLAP[min(3, len(fr))]
            i = seq[(t // (2 if fr[0][1].shape[1] < 30 else 3) + int(f.get("phase", 0) * 5)) % len(seq)]
            rgb, al = fr[i]
            native = FACE.get(f["kind"], -1)
            blit(cv, rgb, al, x - al.shape[1] / 2, y - al.shape[0] / 2, native != 0 and face != native)
        for s in o.get("shots", ()):
            u = (t - s["at"]) % N
            (x0, y0), (x1, y1) = s["src"], s["dst"]
            if u < s["dur"]:
                f = u / s["dur"]
                x = x0 + (x1 - x0) * f
                y = y0 + (y1 - y0) * f - (4 * f * (1 - f)) * (min(y0, y1) - s["peak"])
                r = 5 - 3 * f
                for q in range(3, 0, -1):
                    f2 = max(0, (u - q * 2) / s["dur"])
                    dot(cv, x0 + (x1 - x0) * f2, y0 + (y1 - y0) * f2 - (4 * f2 * (1 - f2)) * (min(y0, y1) - s["peak"]),
                        (150, 124, 100))
                yy, xx = np.mgrid[-4:5, -4:5]
                for dy, dx in zip(yy[(xx ** 2 + yy ** 2) <= r * r], xx[(xx ** 2 + yy ** 2) <= r * r]):
                    dot(cv, x + dx, y + dy, BOULDER[0] if dy > 0 else (BOULDER[2] if dy < -r / 2 else BOULDER[1]))
            elif u < s["dur"] + len(burst["frames"]):
                rgb, al = burst["frames"][u - s["dur"]]
                blit(cv, rgb, al, x1 - burst["ox"], y1 - burst["oy"])
            elif u < s["dur"] + len(burst["frames"]) + 3 * len(puff["frames"]):
                v = u - s["dur"] - len(burst["frames"])
                rgb, al = puff["frames"][(v // 3) % len(puff["frames"])]
                blit(cv, rgb, al, x1 - puff["ox"], y1 - puff["oy"] - v * 0.5)
        for i, box in enumerate(o.get("flashes", ())):
            x0, y0, x1, y1 = box
            for j in range(3):
                u = (t + 5 * i + 3 * j) % 9
                if u < 3:
                    rr = np.random.default_rng(((t - u) % N) * 31 + i * 7 + j).random(2)
                    x, y = x0 + rr[0] * (x1 - x0), y0 + rr[1] * (y1 - y0)
                    for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
                        dot(cv, x + dx, y + dy, TURQ[0] if (dx, dy) == (0, 0) or u == 0 else TURQ[1])
                    for q in range(2, 5 + 3 * u):
                        dot(cv, x - q * 0.6, y + q * 1.2, TURQ[1])
        for x, y, sc in o.get("fires", ()):
            fl = fxload("ks-camp-town-flame", sc)
            rgb, al = fl["frames"][(t // 2 + int(x)) % len(fl["frames"])]
            blit(cv, rgb, al, x - fl["ox"], y - fl["oy"])
        for x, y in o.get("burning", ()):
            for j in range(4):  # town_fix: smoke breaks up instead of fading
                v = (t + j * N // 4 + int(x)) % (N // 4 * 4)
                if v < 4 * len(big["frames"]):
                    rgb, al = big["frames"][v // 4]
                    blit(cv, rgb, al, x - big["ox"] + 3 * math.sin(v / 7 + j), y - 14 - big["oy"] - v * 0.8)
            rgb, al = fire["frames"][(t // 2 + int(x)) % len(fire["frames"])]
            blit(cv, rgb, al, x - fire["ox"], y - fire["oy"])
        for cx, cy, r, ph, sp in flies:
            dot(cv, cx + r * math.cos(2 * math.pi * sp * t / N + ph), cy + r * 0.5 * math.sin(2 * math.pi * sp * 2 * t / N + ph),
                (30, 26, 28))
        for e in emb:
            y = (e["y0"] + TOP * e["k"] * t / N) % TOP
            dot(cv, e["x"] + e["sw"] * math.sin(2 * math.pi * 2 * t / N + e["ph"]), y, e["c"])
        for f in o.get("people", ()):
            sp = person(f["name"])
            if sp is None:
                continue
            rgb, al, (px, py) = sp
            if "walk" in f:
                u = (t - f["offset"]) % N
                if u >= f["span"]:
                    continue
                x, y, d = keys_at(f["walk"], u)
                native = FACE.get(f["name"].split("-")[0], -1)
                blit(cv, rgb, al, x - al.shape[1] / 2, y - al.shape[0] - (u // 3) % 2, native != 0 and d != native)
            else:  # where it was painted; kneeling ones bow a pixel now and then
                bow = f.get("bow") is not None and (t + f["bow"]) % 45 < 8
                blit(cv, rgb, al, px, py + (1 if bow else 0))
        for f in o.get("fighters", ()):
            draw_fighter(cv, f, t, tint)
        frames.append(cv.clip(0, 255).astype(np.uint8))
    dst = OUT / ("stage-%s-life.mp4" % k)
    p = subprocess.Popen([FF, "-v", "error", "-y", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", "%dx%d" % (W, H),
                          "-r", str(FPS), "-i", "-", "-vf", "scale=%d:%d:flags=neighbor" % (W * 2, H * 2),
                          "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "16", str(dst)], stdin=subprocess.PIPE)
    for _ in range(3):
        for f in frames:
            p.stdin.write(f.tobytes())
    p.stdin.close()
    p.wait()
    print("life", k, dst.name, flush=True)
    return frames[0], frames[N // 2]


def look(shots):
    """The top of every stage (where things move) at two moments, half size, one row a stage: look-life.png."""
    th, bar = 150, 26
    s = Image.new("RGB", (2 * (W // 2) + 8, len(shots) * (th + bar)), (18, 14, 20))
    d = ImageDraw.Draw(s)
    for r, (k, fr) in enumerate(shots):
        y = r * (th + bar)
        d.text((8, y + 4), "%s   %s" % (k, SR.STAGES[k]["name"]), fill=(239, 223, 190), font=font(16))
        for c, f in enumerate(fr):
            s.paste(Image.fromarray(f[:300]).resize((W // 2, th), Image.BOX), (c * (W // 2 + 8), y + bar))
    s.save(OUT / "look-life.png")


if __name__ == "__main__":
    args = sys.argv[1:]
    repaint = args[:1] == ["repaint"]
    ks = [a for a in args if a in STAGE_FX] or sorted(STAGE_FX)
    creatures(ks, repaint)
    look([(k, render(k)) for k in ks])
    print("look-life.png and look-creatures.png", flush=True)
