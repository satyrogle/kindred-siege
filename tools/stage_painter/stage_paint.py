import io
import json
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import camp_v4_parts as CP
import regions as RG

G = RG.G

# stage_paint.py [stage numbers]
# Jacob, 30 Oct 2026: "i like 1 lock this art direction in and make the rest of stages", then "for stages you need to
# do for atlest 5 matches so 1 region wiht 5 battle stages check how you did earlier and use the locked camp to make
# all of those got it ?", then "go". Bloodridge's five battle stages, made the way camp 5 was (barren_paint.py): the
# same painter (Ideogram 4 local from a colour plan, step 2 of 20), the camp's sky, sea of clouds, blue peaks, morning
# light, palette and floor stone, and Bloodridge as the locked camp shows it on its hill (town_paint.py: desert
# guardians in the manner of ancient Egypt). Painted at the game's stage size, 1376 x 768 at 1x. Layout from the only
# stage Jacob ever accepted (the court, 13 to 14 Oct) and the 22 Oct template: the landmarks stand on the floor line at
# row 287, and the floor below it stays calm, because the back row's feet are on row 420 and a Warden's head reaches
# row 292 (CampaignArtBattle.cs: feet on rows 420 to 700, squads at x 420 to 600 and 840 to 1020, leaders at 250 and
# 1160). Five places, bare to the chief's court ("it starts with bare minimum and goes to maximum", HANDOFF_LOG 8679).
# Not imported: barren_paint, which adds the camp's bans (tents, banners, buildings) to the shared negative.
# The road order and the five places are Claude's calls; layers and life come after Jacob's look.
# Fixes from the looks, 30 Oct. First stage 1: every Bloodridge building stood on the bare border and the floor was a
# blank lavender fog, so this text names no buildings and the floor is the camp's own tan stone. Then all five: ember
# glow spilt onto the floor like lava (1), a row of UI boxes and cannons with cannonballs in place of catapults (2),
# soft dust fog bands behind the fighters (all, worst on 4), pyramids floating on the clouds (4), fires along the
# bottom edge (4) and a see through ghost pyramid (5).
OUT = RG.REPO / "Art/Studies/Bloodridge-Stages-20261030"
W, H = 1376, 768
FLOOR_Y = 287
WHO = ("A battle stage in the land of the Bloodridge, desert guardians in the manner of ancient Egypt, on the flat top "
       "of a high windswept hill above a sea of white clouds. Ash brown, sepia and corroded bronze with ember light, "
       "under a bright cold morning sky. Ember light glows only in doorways, in the cracks of the obelisks and in "
       "braziers, never on the ground. Only the few things named for this place stand here, along the far edge of the "
       "hill top; nothing else is built anywhere in the picture.")
STYLE = ("Rendered entirely in crisp, deliberately clustered pixel art. Modern high quality pixel art like Sea of Stars: "
         "bright, colourful and readable, never muddy.")
LAY = ("One battle stage seen from a fixed side oblique camera like a classic arcade fighting game stage, no people. The "
       "top third is sky over a sea of clouds, and the landmarks stand along the far edge of the hill top with their feet "
       "on one line a third of the way down. Everything below that line is one wide open flat ground where two armies "
       "will fight, calm and empty from the left edge to the right edge, with nothing standing on it.")
AES = ("a bright readable battle stage in a real lived in desert temple land on a hill above the clouds, coherent pixel "
       "clusters, a real place with depth and scale, readable silhouettes, the open ground calm and even so fighters read "
       "on it")
SKY = "a clear cold morning sky, deep blue above fading to pale blue at the horizon"
FAR = "a sea of white clouds far below the hill with tall blue mountain peaks poking through it far away"
LIGHT = ("Clear cold morning sun from the upper left, cool blue shadows, as discrete flat pixel colour planes, the ground "
         "in calm even light.")
PAL = ["#2a6ab8", "#bfe0f4", "#ffffff", "#6a88b0", "#ece4ee", "#a898b8", "#7c7480", "#5a443a", "#3e302c", "#7a6250",
       "#96703e", "#f07828", "#8c201c", "#d4c4a4", "#b0a084"]  # camp 5's, plus Bloodridge's from town_paint
FLOOR = ("flat bare warm tan stone of the hill top, the same stone as the camp's plateau, wind scoured and cracked into "
         "big plates with a few long thin dark cracks, crisp and clear, calm and even, no dust and no fog")
NEG_STAGE = (", user interface, HUD, health bar, inventory slots, icons, buttons, game UI, cannon, cannonballs, gun "
             "barrel, gunpowder, lava, molten rock, glowing ground, glowing cracks in the floor, fire on the ground, "
             "fog, mist, haze over the ground, dust clouds, sand drifts, dunes, floating pyramids, buildings on the "
             "clouds, translucent buildings, ghost buildings, see through stone, giant stone head, colossal face, "
             "giant sword stuck in the ground")  # the last three: stage 1's head and khopesh (Jacob, 31 Oct)
FRAMEBOX = [(0, 650, 170, H), (1220, 650, W, H)]
PLAN = dict(sky0=[42, 106, 184], sky1=[200, 228, 246], far=[236, 242, 248], back=[206, 200, 214],
            floor0=[208, 192, 160], floor1=[180, 164, 134], piece=[62, 48, 46])  # sky to floor: camp 5's plan colours
STAGES = {
    "1": dict(name="THE HILL ROAD", seed=2026103001, note="bare: the border of their land",
              back=[((0, 215, W, 290), "the far edge of the hill top, low dark rocks, the sea of clouds beyond"),
                    ((980, 50, 1070, 290), "a lone tall dark stone obelisk with thin glowing ember cracks, the border "
                                           "of the Bloodridge"),
                    ((160, 160, 260, 290), "a ragged banner with a dark red sun disc on a leaning pole")],
              near=["a dark broken stone edge", "a dark broken stone edge"]),
    "2": dict(name="THE SIEGE LINE", seed=2026103002, note="their army's catapults",
              back=[((0, 220, W, 290), "a row of low tents of sepia and ash brown rags along the far edge"),
                    ((110, 90, 480, 290), "a big ancient siege catapult of corroded bronze: a squat frame with a tall "
                                          "throwing arm and a sling at its tip, a heap of rough boulders beside it"),
                    ((880, 100, 1250, 290), "a second ancient bronze siege catapult with a tall throwing arm and a "
                                            "sling, and a bronze brazier with ember light")],
              near=["a heap of rough boulders", "a dark broken stone edge"]),
    "3": dict(name="THE EMBER WELL", seed=2026103003, note="the heart of their camp",
              back=[((0, 200, W, 290), "low tents of sepia and ash brown rags in rows along the far edge, ragged "
                                       "banners with a dark red sun disc on poles"),
                    ((480, 180, 900, 290), "a wide round well of dark stone blocks with ember light glowing deep "
                                           "inside it, a bronze rim"),
                    ((150, 160, 280, 290), "a bronze brazier with ember light"),
                    ((1090, 160, 1220, 290), "a bronze brazier with ember light")],
              near=["a dark broken stone edge", "a heap of sepia rags over a bronze frame"]),
    "4": dict(name="THE OBELISK WALK", seed=2026103004, note="the road to the temple",
              back=[((470, 60, 910, 290), "one stepped pyramid of solid dark stone standing on the far end of the hill "
                                          "top behind the obelisks, a little hazy"),
                    ((30, 40, 420, 290), "a row of tall dark stone obelisks with thin glowing ember cracks receding "
                                         "into the distance, bronze braziers between them"),
                    ((960, 40, 1350, 290), "a second row of tall dark stone obelisks with thin glowing ember cracks "
                                           "receding into the distance, bronze braziers between them")],
              near=["a toppled dark obelisk", "a dark broken stone edge"]),
    "5": dict(name="THE PYLON GATE", seed=2026103005, note="the chief's court",
              back=[((300, 20, 1080, 290), "a great stepped pyramid of solid dark stone, fully opaque, standing on the "
                                           "hill top right behind the gate"),
                    ((390, 70, 990, 290), "the dark stone temple gate of two sloped pylon towers, ember light glowing "
                                          "in its tall doorway, the court of the Bloodridge chief"),
                    ((120, 140, 290, 290), "a tall bronze brazier with ember light and a ragged banner with a dark red "
                                           "sun disc"),
                    ((1090, 140, 1260, 290), "a tall bronze brazier with ember light and a ragged banner with a dark "
                                             "red sun disc")],
              near=["a dark broken stone edge", "a dark broken stone edge"]),
}
# The game's fight lanes (CampaignArtBattle.cs, screen px at 1x): feet of a squad of five and its leader per side.
LINEUP = {0: [(250, 640, "clarth_chief_sw")] + [(600, y, b) for y, b in ((420, "clarth_warden_sw"),
          (560, "clarth_skirmisher_sw"), (700, "clarth_trapper_sw"))] + [(420, 420, "clarth_marksman_sw"),
          (420, 700, "clarth_medic_sw")],
          1: [(1160, 620, "blood_rot_warden_se")] + [(840, y, b) for y, b in ((420, "blood_rot_warden_se"),
          (560, "blood_rot_skirmisher_se"), (700, "blood_rot_trapper_se"))] + [(1020, 420, "blood_rot_marksman_se"),
          (1020, 700, "blood_rot_medic_se")]}


def bb(b):
    x0, y0, x1, y1 = b
    return [int(y0 / H * 1000), int(x0 / W * 1000), int(y1 / H * 1000), int(x1 / W * 1000)]


def negative():
    """regions.py's negative without the camp's bans (Bloodridge's stages are full of their tents and banners), plus
    what the first five showed. RG.negative() would carry the camp's bans (tents, banners, statues, buildings, ruins,
    bones, trees), which barren_paint.py adds to RG.NEG when camp_v4_parts loads it, so this builds from NEG_BASE."""
    base = json.loads(RG.L3.BASE.read_text())["5"]["inputs"]["text"]
    return base.replace(" creatures,", "").replace(" letters,", "") + RG.NEG_BASE + NEG_STAGE


def prompt(o):
    els = [{"type": "obj", "bbox": bb((0, 0, W, 170)), "desc": SKY},
           {"type": "obj", "bbox": bb((0, 120, W, 250)), "desc": FAR}]
    els += [{"type": "obj", "bbox": bb(b), "desc": d} for b, d in o["back"]]
    els += [{"type": "obj", "bbox": bb((0, FLOOR_Y, W, H)), "desc": o.get("floor", FLOOR)},
            {"type": "obj", "bbox": bb(FRAMEBOX[0]), "desc": o["near"][0] + " in the near foreground, in shadow"},
            {"type": "obj", "bbox": bb(FRAMEBOX[1]), "desc": o["near"][1] + " in the near foreground, in shadow"}]
    return json.dumps({"high_level_description": WHO + " " + STYLE + " No people anywhere, the fighters are added "
                                                 "later. This place: " + o["name"].lower() + ", " + o["note"] + ".",
                       "style_description": {"medium": "pixel_art", "art_style": RG.L3.ART, "aesthetics": AES,
                                             "lighting": LIGHT, "color_palette": PAL},
                       "compositional_deconstruction": {"background": LAY, "elements": els}})


def shrink(b, f):
    x0, y0, x1, y1 = b
    cx, cy, hw, hh = (x0 + x1) / 2, (y0 + y1) / 2, (x1 - x0) * f / 2, (y1 - y0) * f / 2
    return (cx - hw, cy - hh, cx + hw, cy + hh)


def plan(o):
    """barren_paint's colour plan with the stage's bands: sky, the sea of clouds, the pale far edge of the hill top
    and the floor from row 287 down, dark masses for the landmarks standing on the floor line and dark near corners.
    Nothing on the floor."""
    rng = np.random.default_rng(o["seed"])
    P = {a: np.array(v, float) for a, v in PLAN.items()}
    a = np.zeros((H, W, 3))
    for y in range(H):
        if y < 170:
            a[y] = P["sky0"] + (P["sky1"] - P["sky0"]) * (y / 170)
        elif y < 245:
            a[y] = P["far"]
        elif y < FLOOR_Y:
            a[y] = P["back"]
        else:
            a[y] = P["floor0"] + (P["floor1"] - P["floor0"]) * ((y - FLOOR_Y) / (H - FLOOR_Y))
    blobs = [(o["back"][0][0], 90, P["far"] * 0.85)] + [(shrink(b, 0.75), 160, P["piece"]) for b, _ in o["back"][1:]]
    blobs += [((x0, y0, x1, y1 + 160), 200, P["piece"]) for x0, y0, x1, y1 in FRAMEBOX]
    for box, fill, col in blobs:
        m = Image.new("L", (W, H), 0)
        ImageDraw.Draw(m).ellipse(tuple(box), fill=fill)
        al = np.asarray(m.filter(ImageFilter.GaussianBlur(16))).astype(np.float64)[..., None] / 255
        al[FLOOR_Y + 6:] *= 0 if box[3] <= FLOOR_Y + 10 else 1  # landmarks never spill onto the floor
        a = a * (1 - al) + col * al
    small = rng.normal(0, 12, (H // 16 + 2, W // 16 + 2, 3)).astype(np.float32)
    a += np.dstack([np.asarray(Image.fromarray(small[..., c]).resize((W, H), Image.BICUBIC)) for c in range(3)])
    return Image.fromarray(np.clip(a, 0, 255).round().astype(np.uint8))


API = getattr(G, "API", "http://127.0.0.1:8191")


def upload(img, name, sub="ks-bloodridge-stages"):
    """Hands the colour plan to the painter over its own API (/upload/image), so it lands in whatever input folder the
    running ArtLab reads. 31 Oct: saved by path into ComfyUI-ArtLab/input, the painter answered "Invalid image file",
    it reads another folder. Returns the name LoadImage takes."""
    buf = io.BytesIO()
    img.save(buf, "PNG")
    b = "----ks" + uuid.uuid4().hex
    body = (("--%s\r\nContent-Disposition: form-data; name=\"image\"; filename=\"%s\"\r\nContent-Type: image/png"
             "\r\n\r\n" % (b, name)).encode() + buf.getvalue() + b"\r\n")
    for k, v in (("subfolder", sub), ("type", "input"), ("overwrite", "true")):
        body += ("--%s\r\nContent-Disposition: form-data; name=\"%s\"\r\n\r\n%s\r\n" % (b, k, v)).encode()
    body += ("--%s--\r\n" % b).encode()
    req = urllib.request.Request(API + "/upload/image", data=body,
                                 headers={"Content-Type": "multipart/form-data; boundary=" + b})
    with urllib.request.urlopen(req, timeout=60) as r:
        j = json.load(r)
    return (j["subfolder"] + "/" if j.get("subfolder") else "") + j["name"]


def result(pid):
    """Waits for the job and fetches its image over the painter's API (/view), whatever its output folder is."""
    while True:
        try:
            h = G.api("/history/" + pid).get(pid)
        except OSError:
            time.sleep(10)
            continue
        if h:
            if h.get("status", {}).get("status_str") == "error":
                raise RuntimeError(json.dumps(h["status"])[:2000])
            imgs = [i for o in h.get("outputs", {}).values() for i in o.get("images", [])]
            if imgs:
                q = urllib.parse.urlencode({"filename": imgs[0]["filename"], "subfolder": imgs[0].get("subfolder", ""),
                                            "type": imgs[0].get("type", "output")})
                with urllib.request.urlopen(API + "/view?" + q, timeout=120) as r:
                    return Image.open(io.BytesIO(r.read()))
        time.sleep(4)


def run(tag, init, pos, seed, step=2, neg=None):
    """barren_paint.run into this stage folder, with the plan and the result sent over the painter's API. neg replaces
    the stage negative (stage_life.py's sprite sheets have their own)."""
    OUT.mkdir(parents=True, exist_ok=True)
    name = upload(init, tag + "-init.png")
    g = {n: v for n, v in json.loads(RG.L3.BASE.read_text()).items() if n != "8"}
    g["4"]["inputs"]["text"] = pos
    g["5"]["inputs"]["text"] = negative() if neg is None else neg
    g["10"]["inputs"].update(width=init.size[0], height=init.size[1])  # a patch paints at its own size
    g["11"]["inputs"]["noise_seed"] = seed
    g["15"] = {"class_type": "LoadImage", "inputs": {"image": name}}
    g["16"] = {"class_type": "VAEEncode", "inputs": {"pixels": ["15", 0], "vae": ["3", 0]}}
    g["18"] = {"class_type": "SplitSigmas", "inputs": {"sigmas": ["10", 0], "step": step}}
    g["12"]["inputs"]["sigmas"] = ["18", 1]
    g["12"]["inputs"]["latent_image"] = ["16", 0]
    g["14"]["inputs"]["filename_prefix"] = "KS-Bloodridge-Stages-20261030/" + tag
    (OUT / (tag + "-workflow.json")).write_text(json.dumps(g, indent=1))
    t0 = time.time()
    try:
        pid = G.api("/prompt", {"prompt": g, "front": True})["prompt_id"]
    except urllib.error.HTTPError as e:  # 31 Oct: a bare "HTTP Error 400"; the painter's reply says why
        raise SystemExit("The painter refused %s: %s" % (tag, e.read().decode("utf-8", "replace")[:4000]))
    out = result(pid).convert("RGB")
    print(tag, out.size, "%.0fs" % (time.time() - t0), flush=True)
    return out


def sprite(base, flip):
    a = CP.C5H.sprite(base, "idle", 0)
    if flip:
        a = a[:, ::-1]
    ys, xs = np.nonzero(a[..., 3] > 127)
    return Image.fromarray(np.ascontiguousarray(a)), xs.min(), xs.max(), ys.max()


def test_fight(k):
    """The stage with a test lineup standing on the game's lanes: Clarth on the left, Bloodridge on the right, idle
    frames at 1x. Only for judging how fighters read; not the slice's real squads."""
    v = Image.open(OUT / ("stage-%s.png" % k)).convert("RGBA")
    figs = [(fy, fx, base, side) for side, row in LINEUP.items() for fx, fy, base in row]
    for fy, fx, base, side in sorted(figs):
        im, x0, x1, y1 = sprite(base, flip=(side == 0) == base.startswith("clarth"))
        v.alpha_composite(im, (int(fx - (x0 + x1) / 2), int(fy - y1)))
    v.convert("RGB").save(OUT / ("stage-%s-test-fight.png" % k))


def sheet():
    tw, th = 688, 384
    s = Image.new("RGB", (2 * tw + 8, 3 * (th + 20)), (18, 14, 20))
    d = ImageDraw.Draw(s)
    for i, k in enumerate(sorted(STAGES)):
        p = OUT / ("stage-%s-test-fight.png" % k)
        if not p.exists():
            continue
        x, y = (i % 2) * (tw + 8), (i // 2) * (th + 20)
        s.paste(Image.open(p).resize((tw, th), Image.BOX), (x, y + 18))
        d.text((x + 4, y + 3), "%s  %s, %s" % (k, STAGES[k]["name"], STAGES[k]["note"]), fill=(239, 223, 190))
    d.text((tw + 12, 2 * (th + 20) + 24), "Test lineup on the game's lanes: Clarth left, Bloodridge right.",
           fill=(239, 223, 190))
    s.save(OUT / "look-bloodridge-stages.png")


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "stages.json").write_text(json.dumps({"who": WHO, "stages": STAGES, "lineup": LINEUP}, indent=1))
    for k in (sys.argv[1:] or sorted(STAGES)):
        o = STAGES[k]
        init = plan(o)
        init.save(OUT / ("stage-%s-init.png" % k))
        raw = run("stage-%s" % k, init, prompt(o), o["seed"])
        raw.save(OUT / ("stage-%s-raw.png" % k))
        RG.clean(raw).save(OUT / ("stage-%s.png" % k))
        test_fight(k)
        sheet()
    print("all painted", flush=True)
