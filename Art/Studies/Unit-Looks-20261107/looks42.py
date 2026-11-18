"""Three looks for the marks, on the rules 42 fight. 7 Nov 2026, after Jacob's "Yes, remake them" and "Go".

The same real fight as all in v3 (seed 5100, road fight 9), replayed with only that fight at rules 42 (FightDump3.cs,
dump42.txt): the attack table's dodges, parries, blocks and crits with the five table effects and the take 1 words
(Art/Studies/Attack-Table-20261107), the 6 Nov clips cut to game sheets, Guard's twin rings and the snare's bone jaws
(picked 6 Nov), the parry cue (gold body flash, rules 26) and the hit stack from the Codex job at HANDOFF_LOG 14434.
Who is guarded, aimed at and marked comes from the sim's own state on every tick (the S lines in dump42.txt).

The fight is drawn three times, frame for frame. Only the marks layer changes:
  1 FLOOR     a ring at the feet split by the marks' colours, the snare's spike ring, Guard's tether and a Marksman's
              aim as lines on the ground, Focus Fire chevrons
  2 OVERHEAD  a row of icons over the head
  3 BODY      on the fighter's own sprite: outlines (Guard mint, aim red blinking, Focus Fire red), orange cracks for
              Open, a blue rim for Exposed, a lilac net for the snare, blood drips, frost for Stasis, a tremble for Panic
Usage: python -I looks42.py <PixelLab zips dir> <out dir> [render only the first N ms]"""
import sys, os, io, json, math, zipfile, zlib, random
from PIL import Image, ImageDraw, ImageFont, ImageChops, ImageFilter

ZIPS, OUT = sys.argv[1], sys.argv[2]
LIMIT = int(sys.argv[3]) if len(sys.argv) > 3 else 0
os.makedirs(OUT, exist_ok=True)
KS = "C:/Users/jacob/KindredSiege-Clean/"
HERE = KS + "Art/Studies/Unit-Looks-20261107/"
CA = KS + "Assets/Resources/CampaignArt/"
GS = KS + "Art/Studies/Unit-Anims-20261106/game-sheets/"
CM = KS + "Art/Studies/Class-Moves-20261106/"
AT = KS + "Art/Studies/Attack-Table-20261107/"
EC = KS + "Art/Studies/Chief-Cut-Ins-20261018/spell-language/game-topdown/eclipse/"
FLIP = Image.Transpose.FLIP_LEFT_RIGHT

# ------------------------------------------------------------------ stage, camera, colours
R = KS + "Assets/StreamingAssets/CampaignArt/Bloodridge/"
STAGE = Image.open(R + "ground-underpaint.png").convert("RGBA")
for l in ["rear-burial-architecture.png", "rooted-entrance.png"]:
    STAGE.alpha_composite(Image.open(R + l).convert("RGBA"))
BOX = (278, 230, 1062, 714)            # one fixed camera for all three looks, 784 x 484 at 1:1
MG = 4                                  # spare pixels round the crop for the 2 px shake
OX, OY = BOX[0] - MG, BOX[1] - MG
BG = STAGE.crop((OX, OY, BOX[2] + MG, BOX[3] + MG))
PW, PH = BOX[2] - BOX[0], BOX[3] - BOX[1]
HDR, FTR = 24, 20

NIGHT = (28, 18, 14)
PX = dict(bone=(239, 223, 190), muted=(173, 154, 134), mint=(153, 190, 172), red=(218, 107, 81), gold=(214, 170, 92),
          blue=(141, 182, 224), orange=(238, 154, 77), lilac=(194, 176, 214), yellow=(242, 210, 124))
MCOL = dict(guard=PX["mint"], aim=PX["red"], focus=PX["red"], snare=PX["lilac"], bleed=PX["red"], open=PX["orange"],
            exposed=PX["blue"], stasis=PX["bone"], panic=PX["yellow"])
STATUS = {"Bleed": "bleed", "Snare": "snare", "Opening": "open", "Exposed": "exposed", "Stasis": "stasis", "Panic": "panic"}

def ramp(c):
    return tuple(int(v * 0.5) for v in c), c, tuple(int(v + (255 - v) * 0.5) for v in c)

def solid(im):
    return im.getchannel("A").point(lambda v: 255 if v > 20 else 0)

def body_box(im):
    return solid(im).getbbox()

def flat(im, col):
    out = Image.new("RGBA", im.size, col + (255,)); out.putalpha(im.getchannel("A")); return out

def tint(im, col, a):
    al = im.getchannel("A"); out = Image.blend(im, Image.new("RGBA", im.size, col + (255,)), max(0.0, min(1.0, a)))
    out.putalpha(al); return out

def font(n):
    try: return ImageFont.truetype("C:/Windows/Fonts/impact.ttf", n)
    except Exception: return ImageFont.load_default()
NUMF, BIGF, SMALL, TITLE, LABEL = font(17), font(22), font(13), font(34), font(17)

# ------------------------------------------------------------------ art
def sheet(path, scale=1.0):
    d = json.load(open(path + ".json")); im = Image.open(path + ".png").convert("RGBA")
    fr = [im.crop((f["frame"]["x"], f["frame"]["y"], f["frame"]["x"] + f["frame"]["w"], f["frame"]["y"] + f["frame"]["h"])) for f in d["frames"]]
    ms_ = [int(f.get("duration", d.get("frameMs", 100))) for f in d["frames"]]
    if scale != 1.0: fr = [f.resize((round(f.width * scale), round(f.height * scale)), Image.NEAREST) for f in fr]
    if "anchorX" in d: ax, ay = d["anchorX"] * scale, d["anchorY"] * scale
    else: b = body_box(fr[0]); ax, ay = (b[0] + b[2] - 1) / 2, b[3]
    return dict(frames=fr, ms=ms_, ax=int(round(ax)), ay=int(round(ay)), hit=d.get("hitFrame"))

ZIPCACHE = {}
def zip_frames(cid, prefix, d, mirror):
    if cid not in ZIPCACHE: ZIPCACHE[cid] = zipfile.ZipFile(f"{ZIPS}/{cid}.zip")
    z = ZIPCACHE[cid]; dd = "south-east" if d == "se" else "south-west"
    names = sorted({n.split("/animations/")[1].split("/")[0] for n in z.namelist() if "/animations/" in n and n.split("/animations/")[1].startswith(prefix)})
    names = [n for n in names if f"_{d}_" in n]
    assert names, (cid, prefix, d)
    files = sorted(n for n in z.namelist() if f"/animations/{names[0]}/{dd}/" in n and n.endswith(".png"))
    fr = [Image.open(io.BytesIO(z.read(x))).convert("RGBA") for x in files]
    return [f.transpose(FLIP) for f in fr] if mirror else fr

CIDS = {("warden", 0): "d917de88-8dfc-4de2-97f0-4dc46e06cb7d", ("warden", 1): "b3fef59f-4ba2-4f95-a526-1228e1f8b49d",
        ("skirmisher", 0): "a3aa47e7-da8b-47f5-a04d-dadb14721a65", ("skirmisher", 1): "a1195c8f-03c0-42f8-8f8f-135b8e98d09c",
        ("trapper", 0): "44547cca-99d8-432c-bc41-ab629db97136", ("trapper", 1): "720d540c-6f12-49b8-b409-3f84e8102bf2",
        ("marksman", 0): "9e0233b1-4a8a-4d81-99d8-53351b1cf28d", ("marksman", 1): "19b7cc53-fb54-45dd-95ee-433ed538dd10",
        ("medic", 0): "20b62125-093d-41ca-a5d2-eb2f15d3fd25", ("medic", 1): "e7aceef9-d12c-4c53-8817-318de3265c03"}

def kit(cls, side):
    tribe, d = ("clarth", "se") if side == 0 else ("blood_rot", "sw")
    k = {}
    for move in ["idle", "attack", "attack2", "block", "hit", "power", "death"]:
        name = "heal" if (move == "attack2" and cls == "medic") else move
        k[move] = sheet(GS + f"{tribe}_{cls}_{d}_{name}")
    run = "lock_run" if (cls == "warden" and side == 1) else "allin_run"  # their Warden's old run lost his tall shield
    fr = zip_frames(CIDS[(cls, side)], f"{run}_{d}", d, (cls, d) == ("marksman", "sw"))
    idle = k["idle"]
    if fr[0].size == idle["frames"][0].size: ax, ay = idle["ax"], idle["ay"]
    else: b = body_box(fr[0]); ax, ay = (b[0] + b[2] - 1) // 2, b[3]
    k["run"] = dict(frames=fr, ms=[90] * len(fr), ax=ax, ay=ay, hit=None)
    return k

CHIEF = {k: sheet(CA + f"bloodridge_chief_sw_{k}", 0.72) for k in ["idle", "attack", "hit", "death"]}
_n3 = len(CHIEF["death"]["frames"]) // 3          # his death clip opens with a last swing; start at the fall
CHIEF["death"] = dict(CHIEF["death"], frames=CHIEF["death"]["frames"][_n3:], ms=CHIEF["death"]["ms"][_n3:])
LEADER = {k: sheet(CA + f"Leader/leader_se_{k}") for k in ["idle", "attack", "block", "hit", "victory"]}

def effect(name):
    m = json.load(open(f"{AT}effects/{name}/game/manifest.json"))
    c = [x for x in m["clips"] if x["name"] == "whole"][0]
    s = Image.open(f"{AT}effects/{name}/game/{c['sheets'][0]['file']}").convert("RGBA")
    w, h = c["frame"]["w"], c["frame"]["h"]; cols = c["sheets"][0]["columns"]
    fr = [s.crop(((i % cols) * w, (i // cols) * h, (i % cols) * w + w, (i // cols) * h + h)) for i in range(c["frames"])]
    return dict(frames=fr, ms=m["frame_ms"], off=(c["offset"]["x"], c["offset"]["y"]), w=w, h=h, dur=m["frame_ms"] * len(fr))
EFF = {k: effect(k) for k in ["hit", "crit", "block", "parry", "dodge"]}
SUBJ = dict(hit=125, crit=125, block=125, parry=102, dodge=102)   # body height of the sprite each effect was built on
WORD = {k: Image.open(f"{AT}words/take1_{k}.png").convert("RGBA") for k in ["dodge", "parry", "block", "crit"]}

RISE = [Image.open(f"{CM}guard/parts/rise_anim/{i}.png").convert("RGBA") for i in range(9)]
JAWS = [Image.open(f"{CM}snare/v2-props/parts/jaws_anim/{i}.png").convert("RGBA").resize((96, 96), Image.NEAREST) for i in range(9)]
SNARE_RING = Image.open(f"{CM}moves/parts/snare_ring.png").convert("RGBA")
E = {k: sheet(EC + "eclipse_" + k) for k in ["tell_under", "break_under"]}
FOCUS_ICON = Image.open(CA + "UI/icon_focus.bytes").convert("RGBA")   # the game's own Focus Fire icon, ringed in NIGHT
_fa = FOCUS_ICON.getchannel("A").point(lambda v: 255 if v > 20 else 0)
_fr = Image.new("RGBA", FOCUS_ICON.size, NIGHT + (255,)); _fr.putalpha(_fa.filter(ImageFilter.MaxFilter(5))); _fr.alpha_composite(FOCUS_ICON)
FOCUS_ICON = _fr

# ------------------------------------------------------------------ icons for look 2 (11 x 11, a NIGHT ring, shown 2x)
GLYPH = {
    "guard": [".MMMMMMMMM.", "MLLLMMMMMMD", "MLMMMMMMMMD", "MLMMMMMMMMD", "MLMMMMMMMDD", ".MLMMMMMMD.", ".MLMMMMMMD.",
              "..MLMMMMD..", "..MLMMMMD..", "...MMMMD...", "....MDD...."],
    "bleed": [".....M.....", "....MLM....", "....MLM....", "...MLMMD...", "...MLMMD...", "..MLLMMMD..", "..MLMMMMD..",
              ".MLMMMMMMD.", ".MMMMMMMMD.", "..DMMMMMD..", "...DDDDD..."],
    "open": [".....L.....", "....LLM....", "...LLDMM...", "..LLMDMMM..", ".LLMMDDMMD.", "LLMMMMDMMDD", ".LMMMDDMMD.",
             "..MMMDMMD..", "...MMDMD...", "....MDD....", ".....D....."],
    "exposed": ["...........", "....DDD....", "..DDLLLDD..", ".DLLMMMLLD.", "DLLMMDMMLLD", "DLLMDDDMLLD", "DLLMMDMMLLD",
                ".DLLMMMLLD.", "..DDLLLDD..", "....DDD....", "..........."],
    "stasis": ["DDDDDDDDDDD", ".DLLLLLLLD.", "..DLLLLLD..", "...DLLLD...", "....DLD....", ".....M.....", "....DMD....",
               "...DMMMD...", "..DMMMMMD..", ".DMMMMMMMD.", "DDDDDDDDDDD"],
    "panic": ["..MM...MM..", "..ML...ML..", "..ML...ML..", "..ML...ML..", "..MM...MM..", "..MM...MM..", "...........",
              "..MM...MM..", "..MM...MM..", "...........", "..........."],
    "snare": ["...MMMMM...", "..M.....M..", "..M.....M..", "..M.....M..", ".MMMMMMMMM.", ".MLLLLLLLD.", ".MLLLDLLLD.",
              ".MLLLDLLLD.", ".MLLLLLLLD.", ".MMMMMMMMD.", "..........."],
}

def glyph_image(rows, col):
    dark, mid, light = ramp(col)
    g = Image.new("RGBA", (13, 13), (0, 0, 0, 0)); px = g.load()
    for yy, row in enumerate(rows):
        for xx, ch in enumerate(row):
            if ch in "DML": px[xx + 1, yy + 1] = {"D": dark, "M": mid, "L": light}[ch] + (255,)
    a = g.getchannel("A"); ring = ImageChops.subtract(a.filter(ImageFilter.MaxFilter(3)), a)
    out = Image.new("RGBA", g.size, NIGHT + (255,)); out.putalpha(ring); out.alpha_composite(g)
    return out.resize((26, 26), Image.NEAREST)
ICON = {k: glyph_image(v, MCOL[k]) for k, v in GLYPH.items()}

AIMCACHE = {}
def aim_icon(p):
    """A crosshair whose ring lights up clockwise as the aim runs out."""
    q = min(12, int(p * 12))
    if q in AIMCACHE: return AIMCACHE[q]
    dark, mid, light = ramp(PX["red"])
    rows = []
    for yy in range(11):
        row = ""
        for xx in range(11):
            dx, dy = xx - 5, yy - 5; dist = math.hypot(dx, dy)
            if 3.4 <= dist <= 4.6:
                ang = (math.degrees(math.atan2(dx, -dy)) + 360) % 360
                row += "L" if ang < q * 30 else "M"
            elif (dx == 0 and abs(dy) == 5) or (dy == 0 and abs(dx) == 5): row += "L"
            elif dx == 0 and dy == 0: row += "L" if q >= 11 else "M"
            else: row += "."
        rows.append(row)
    AIMCACHE[q] = glyph_image(rows, PX["red"]); return AIMCACHE[q]

# ------------------------------------------------------------------ the fight
units, events, orders, S = {}, [], [], {}
for line in open(os.environ.get("KS_DUMP", HERE + "dump42.txt")):   # Leaders-20261108/stage46.py points it at dump43.txt
    line = line.rstrip("\n")
    if line.startswith("U "):
        p = line.split(); units[p[2]] = dict(side=int(p[1]), cls=p[3].lower(), role=p[4])
    elif line.startswith(("E ", "D ")):
        left, right = line.split(" > ", 1); lp = left.split(); rp = right.split()
        if line[0] == "E": tick, kind, side, actor = int(lp[1]), lp[2], int(lp[3]), (lp[4] if len(lp) > 4 else "")
        else: tick, kind, side, actor = int(lp[1]), "Directive", int(lp[2]), (lp[3] if len(lp) > 3 else "")
        target = ""
        if rp and not rp[0].lstrip("-").isdigit(): target, rp = rp[0], rp[1:]
        ev = dict(tick=tick, kind=kind, side=side, actor=actor, target=target, amount=int(rp[0]) if rp else 0, detail=" ".join(rp[1:]))
        (events if line[0] == "E" else orders).append(ev)
    elif line.startswith("S "):
        p = line.split(); aim = None
        if p[4][2:] != "-": tid, lft = p[4][2:].rsplit(":", 1); aim = (tid, int(lft))
        marks = {}
        if p[5][2:] != "-":
            for kv in p[5][2:].split(","): n_, v_ = kv.split("="); marks[n_] = int(v_)
        S[(int(p[1]), p[2])] = dict(guard=[x for x in p[3][2:].split("/") if x != "-"], aim=aim, marks=marks)

TPS, CHARGE, STEP = 30, 1600, 40
def ms(tick): return CHARGE + tick * 1000 // TPS
def tick_at(t): return (t - CHARGE) * TPS // 1000
LAST = max(e["tick"] for e in events)

T = "camp.11.alpha.alphahunt.alpha."
HOME = {"clarth.warden.1": (672, 448), "clarth.skirmisher.3": (664, 568), "clarth.trapper.4": (672, 690),
        "clarth.marksman.2": (482, 478), "clarth.medic.5": (470, 662), "leader": (338, 578),
        T + "1": (768, 448), T + "captain": (790, 572), T + "4": (836, 505), T + "2": (768, 690),
        T + "3": (985, 478), T + "0": (1000, 662)}
LANE = {"clarth.warden.1": T + "1", T + "1": "clarth.warden.1", "clarth.skirmisher.3": T + "captain", T + "captain": "clarth.skirmisher.3",
        "clarth.trapper.4": T + "2", T + "2": "clarth.trapper.4", T + "4": "clarth.skirmisher.3"}
if set(HOME) != set(units):     # another fight's dump (Leaders-20261108/stage47.py, which places everyone itself): free spots
    _spare = {0: [(560, 600), (600, 520)], 1: [(768, 448), (790, 572), (836, 505), (768, 690), (985, 478), (1000, 662), (900, 600), (930, 520)]}
    HOME = {k: v for k, v in HOME.items() if k in units}
    for _uid, _u0 in units.items():
        if _uid not in HOME: HOME[_uid] = _spare[_u0["side"]].pop(0)
    LANE = {k: v for k, v in LANE.items() if k in units and v in units}

U = {}
for uid, h in HOME.items():
    u0 = units[uid]; side = u0["side"]
    if uid == "leader":
        clips = {"idle": LEADER["idle"], "attack": LEADER["attack"], "attack2": LEADER["attack"], "block": LEADER["block"],
                 "hit": LEADER["hit"], "power": LEADER["attack"], "death": LEADER["hit"], "run": LEADER["idle"], "victory": LEADER["victory"]}
    elif u0["role"] == "Chief":
        clips = {"idle": CHIEF["idle"], "attack": CHIEF["attack"], "attack2": CHIEF["attack"], "block": CHIEF["hit"],
                 "hit": CHIEF["hit"], "power": CHIEF["attack"], "death": CHIEF["death"], "run": CHIEF["idle"]}
    else:
        clips = kit(u0["cls"], side)
    idle = clips["idle"]; b = body_box(idle["frames"][0])
    U[uid] = dict(id=uid, side=side, cls=u0["cls"], chief=u0["role"] == "Chief", leader=uid == "leader", face=1 if side == 0 else -1,
                  clips=clips, home=h, x=float(h[0]), y=float(h[1]), acts=[], dead_at=None, alt=0, targets=[],
                  melee=u0["cls"] in ("warden", "skirmisher", "trapper") and uid != "leader",
                  height=idle["ay"] - b[1], width=b[2] - b[0])

FX = []
def fx(kind, start, dur, data): FX.append((kind, int(start), int(dur), data))
def num(uid, t, text, col, big=False): fx("num", t, 950, (uid, text, col, big))

def clip_hit(clip, face):
    if clip.get("hit") is not None: return clip["hit"]
    best, bi = None, len(clip["frames"]) // 2
    for i, f in enumerate(clip["frames"][1:-2], 1):
        b = body_box(f)
        if not b: continue
        reach = b[2] if face > 0 else -b[0]
        if best is None or reach > best: best, bi = reach, i
    return bi

def act(u, key, at, kind="attack", freeze=0, hold_from=None):
    clip = u["clips"][key]; d = list(clip["ms"])
    if kind == "attack":
        c = clip_hit(clip, u["face"]); d[c] += freeze; start = at - sum(d[:c])
        if hold_from is not None and c > 0 and hold_from < start: d[c - 1] += start - hold_from; start = hold_from
    elif kind == "react":
        d[0] += freeze; start = at
    elif kind == "death":
        d[-1] = 10 ** 7; start = at
    else:
        start = at
    u["acts"].append((int(start), clip, d, key))

def busy(u, at):
    return any(key in ("attack", "attack2", "power", "windup") and s <= at < s + sum(d) for s, clip, d, key in u["acts"])

POWER = ("strike", "aimed", "flurry", "twincut", "rip")
def swing(a, t, det, v, freeze=0):
    ranged = a["cls"] == "marksman" or a["leader"]
    at = t - 120 if ranged else t
    if busy(a, at): return
    if det.startswith(POWER): key = "power"
    else:
        key = "attack" if (a["alt"] % 2 == 0 or a["cls"] == "medic") else "attack2"; a["alt"] += 1
    act(a, key, at, "attack", freeze)
    if v is not None:
        a["targets"].append((t - 600, v["id"]))
        if ranged: fx("bullet", t - 120, 120, (a["id"], v["id"]))

# pass 1: every landed blow, its table result and its size, then the hit stop rule (3 within 100 ms: only the biggest)
struck = {}
for e in events:
    if e["kind"] == "Struck": struck.setdefault((e["tick"], e["actor"]), []).append(e["detail"])
HITS = []
for e in events:
    if e["kind"] != "Hit" or e["target"] not in U: continue
    q = struck.get((e["tick"], e["actor"])) if e["actor"] else None
    outcome = q.pop(0) if q else None
    a = U.get(e["actor"])
    if e["detail"] == "bleed": size = "dot"
    elif outcome == "crit": size = "crit"
    elif e["amount"] >= 40 or e["detail"].startswith(POWER) or (a is not None and a["chief"]): size = "heavy"
    else: size = "light"
    HITS.append(dict(e=e, t=ms(e["tick"]), outcome=outcome, size=size))
for i, h in enumerate(HITS):
    near = [(x["e"]["amount"], -j, j) for j, x in enumerate(HITS) if x["size"] != "dot" and abs(x["t"] - h["t"]) <= 100]
    h["lead"] = h["size"] != "dot" and (len(near) < 3 or max(near)[2] == i)
    h["freeze"] = dict(crit=200, heavy=120, light=60, dot=0)[h["size"]] if h["lead"] else 0
HIT_OF = {id(h["e"]): h for h in HITS}

def land(v, a, t, h, det):
    fr = h["freeze"]
    if h["outcome"] == "block":
        if not busy(v, t): act(v, "block", t, "react", fr)
        fx("eff", t, EFF["block"]["dur"], ("block", v["id"]))
    elif h["size"] != "dot":
        if det != "trap" and not busy(v, t): act(v, "hit", t, "react", fr)
        name = "crit" if h["outcome"] == "crit" else "hit"; fx("eff", t, EFF[name]["dur"], (name, v["id"]))
    if h["lead"]:
        fx("white", t, 40, v["id"])
        if fr: fx("jitter", t, fr, v["id"])
    fx("tint", t + 40, 320, v["id"])
    kb = dict(crit=8, heavy=8, light=4, dot=0)[h["size"]]
    if kb and h["outcome"] != "block": fx("shove", t + fr, 180, (v["id"], -v["face"] * kb))
    if h["outcome"] == "crit": fx("shake", t, 160, 2)
    if h["outcome"] in ("block", "crit"): fx("word", t + 40, 700, (v["id"], h["outcome"]))
    big = h["size"] in ("crit", "heavy")
    col = (255, 200, 60) if big else (255, 80, 60) if h["size"] != "dot" else (205, 72, 52)
    num(v["id"], t, str(h["e"]["amount"]), col, big)
    if det == "guarded": fx("wall", t - 120, 4 * 80 + 300, v["id"])

# pass 2: the screen events
eclipse, heavy, end_t = None, {}, None
for e in events:
    t = ms(e["tick"]); a = U.get(e["actor"]); v = U.get(e["target"]); k, det = e["kind"], e["detail"]
    if k == "Hit" and v:
        h = HIT_OF[id(e)]
        if a and det != "trap": swing(a, t, det, v, h["freeze"])
        land(v, a, t, h, det)
    elif k == "Miss" and v:
        if a: swing(a, t, det, v, 120 if det == "parry" else 0)
        if det == "dodge":
            fx("dodge", t - 80, 600, (v["id"], -v["face"] * 14)); fx("eff", t, EFF["dodge"]["dur"], ("dodge", v["id"]))
        elif det == "parry":
            if not busy(v, t): act(v, "block", t, "react", 120)
            fx("eff", t, EFF["parry"]["dur"], ("parry", v["id"]))
            if a: fx("shove", t + 120, 220, (a["id"], -a["face"] * 6))
        fx("word", t + 40, 700, (v["id"], det))
    elif k == "Heal" and a and v:
        if not busy(a, t): act(a, "attack2", t, "attack")
        num(v["id"], t, "+" + str(e["amount"]), (120, 240, 130)); fx("glow", t, 280, v["id"])
    elif k == "TrapArmed" and a:
        if not busy(a, t): act(a, "power", t, "attack")
    elif k == "TrapSprung" and v:
        fx("jaws", t - 80, 9 * 90 + 700, v["id"])
    elif k == "Guard" and v:
        fx("ring", t - 200, 2200, v["id"])
    elif k == "Defence" and det == "sidestep" and a:
        fx("dodge", t - 80, 600, (a["id"], -a["face"] * 14)); fx("eff", t, EFF["dodge"]["dur"], ("dodge", a["id"]))
    elif k == "Windup" and a:
        dur = e["amount"] * 1000 // TPS
        if a["chief"]:
            targets = [x for x in ("clarth.warden.1", "clarth.skirmisher.3", "clarth.trapper.4") if U[x]["dead_at"] is None]
            eclipse = dict(start=t, targets=targets, release=None)
            att = a["clips"]["attack"]
            a["acts"].append((t, dict(att, frames=att["frames"][:2]), [120, dur], "windup"))
            fx("parry", t, dur + 300, "leader"); fx("title", t, 1200, "ECLIPSE")
        else:
            heavy[e["actor"]] = dict(start=t, target=e["target"])
    elif k == "Release" and a:
        fx("gold", t - 200, 200, a["id"])                       # rules 26, cue 2: the body flashes gold in the window
        if a["chief"] and eclipse:
            eclipse["release"] = t; fx("title", t, 900, det.upper())
        elif e["actor"] in heavy:
            hv = heavy.pop(e["actor"]); vv = U.get(hv["target"]); ranged = a["cls"] == "marksman"
            act(a, "power", t - 120 if ranged else t, "attack", 120, hold_from=hv["start"])
            if ranged and vv: fx("bullet", t - 120, 120, (a["id"], vv["id"]))
    elif k == "Tell" and a and a["chief"]:
        fx("parry", t, 1100, "leader")
    elif k == "Kill" and v:
        v["dead_at"] = t; act(v, "death", t, "death")
        fx("white", t, 60, v["id"]); fx("shake", t, 240, 2)
        if any(x["kind"] == "End" for x in events if x["tick"] == e["tick"]):
            end_t = t; fx("title", t + 400, 2600, "BREAK")

if end_t:
    for u in U.values():
        if u["side"] == 1 and u["dead_at"] is None: u["flee"] = end_t + 1500
    vic = LEADER["victory"]; U["leader"]["acts"].append((end_t + 1400, vic, [100] * (len(vic["frames"]) - 1) + [10 ** 7], "victory"))
END_MS = (end_t or ms(LAST)) + 3000
if LIMIT: END_MS = min(END_MS, LIMIT)

# ------------------------------------------------------------------ marks from the sim's state, tick by tick
WARD, AIMS, STAT = {}, {}, {}
by_shooter = {}
for (k, uid), st in S.items():
    for w in st["guard"]: WARD[(k, w)] = uid
    if st["aim"]: by_shooter.setdefault(uid, []).append((k, st["aim"][0], st["aim"][1]))
    m = {STATUS[n]: left for n, left in st["marks"].items() if n in STATUS}
    if m: STAT[(k, uid)] = m
aimed_hits = sorted((e["tick"], e["actor"], e["target"]) for e in events if e["kind"] == "Hit" and e["detail"].startswith("aimed"))
for sh, lst in by_shooter.items():
    lst.sort(); runs = []
    for k, tg, left in lst:
        if runs and runs[-1][-1][0] == k - 1 and runs[-1][-1][1] == tg: runs[-1].append((k, tg, left))
        else: runs.append([(k, tg, left)])
    for run in runs:
        total = run[0][2] + 1
        for k, tg, left in run:
            shown = tg
            if U[tg]["dead_at"] is not None and ms(k) >= U[tg]["dead_at"]:   # his mark fell: the shot goes to the next man
                nxt = [x for x in aimed_hits if x[1] == sh and x[0] >= k]
                shown = nxt[0][2] if nxt else None
            if shown: AIMS[(k, shown)] = (sh, 1 - left / total)
FOCUS = []
for o in orders:
    if o["detail"] == "FocusFire" and o["side"] == 0 and o["target"] in U:
        FOCUS.append((o["target"], ms(o["tick"]), U[o["target"]]["dead_at"] or END_MS))

def marks_at(t):
    k = tick_at(t); out = {}
    if k < 1 or k > LAST: return out
    for uid, u in U.items():
        if u["dead_at"] is not None and t >= u["dead_at"]: continue
        m = dict(STAT.get((k, uid), {}))
        g = WARD.get((k, uid))
        if g and (U[g]["dead_at"] is None or t < U[g]["dead_at"]): m["guard"] = g
        if (k, uid) in AIMS: m["aim"] = AIMS[(k, uid)]
        if any(fu == uid and s0 <= t < s1 for fu, s0, s1 in FOCUS): m["focus"] = True
        if m: out[uid] = m
    return out

# ------------------------------------------------------------------ movement (all in v3, plus a slot each per target)
def target_of(u, t):
    if not u["melee"] or (u["dead_at"] is not None and t >= u["dead_at"]): return None
    tg = None
    for gt, tid in u["targets"]:
        if gt <= t and (U[tid]["dead_at"] is None or t < U[tid]["dead_at"]): tg = tid
    return None if tg == LANE.get(u["id"]) else tg

def goal_xy(u, t, tg, slot):
    if u["dead_at"] is not None and t >= u["dead_at"]: return None
    if u.get("flee") and t >= u["flee"]: return (u["home"][0] + 420, u["home"][1])
    if tg is None or (end_t and t >= end_t): return u["home"]          # once they break, ours hold their ground
    v = U[tg]; s = 1 if u["home"][1] >= v["y"] else -1
    if slot % 2 == 1: s = -s                      # a second man on the same target takes the other side of him
    off = (100 if v["chief"] else 88) + (40 if slot >= 2 else 0)
    return (v["x"] - u["face"] * off, v["y"] + s * 46)

frames_t = list(range(0, END_MS, STEP))
positions = []
for u in U.values():
    u["x"] = u["home"][0] - u["face"] * (320 if u["melee"] else 180)
for t in frames_t:
    snap = {}
    tgs = {u["id"]: target_of(u, t) for u in U.values()}
    slots = {}
    for tid in sorted(set(x for x in tgs.values() if x)):
        for n, uid in enumerate(sorted(x for x, y_ in tgs.items() if y_ == tid)): slots[uid] = n
    for u in U.values():
        moving = False
        if t < CHARGE:
            p = min(1, t / CHARGE); ease = 1 - (1 - p) ** 2
            sx = u["home"][0] - u["face"] * (320 if u["melee"] else 180)
            u["x"] = sx + (u["home"][0] - sx) * ease; moving = p < 1
        else:
            g = goal_xy(u, t, tgs[u["id"]], slots.get(u["id"], 0))
            if g:
                fast = u.get("flee") and t >= u["flee"]
                dx, dy = g[0] - u["x"], g[1] - u["y"]; dist = math.hypot(dx, dy); sp = (320 if fast else 190) * STEP / 1000
                if dist > 5:
                    u["x"] += dx / dist * min(sp, dist); u["y"] += dy / dist * min(sp, dist); moving = dist > 12
        snap[u["id"]] = (u["x"], u["y"], moving)
    positions.append(snap)

# ------------------------------------------------------------------ drawing
PRI = {"death": 3, "attack": 2, "attack2": 2, "power": 2, "windup": 2, "victory": 2}
def frame_of(u, t):
    best = None
    for s, clip, d, key in u["acts"]:
        if s <= t < s + sum(d):
            score = (PRI.get(key, 1), s)
            if best is None or score > best[0]: best = (score, s, clip, d, key)
    if best:
        _, s, clip, d, key = best; lt = t - s; acc = 0
        for j, dd in enumerate(d):
            acc += dd
            if lt < acc: return clip, min(j, len(clip["frames"]) - 1), key
    if u["dead_at"] is not None and t >= u["dead_at"]:
        c = u["clips"]["death"]; return c, len(c["frames"]) - 1, "dead"
    return None, None, None

def paste_center(c, im, x, y):
    c.alpha_composite(im, (int(x - im.width / 2) - OX, int(y - im.height / 2) - OY))

def draw_eff(c, name, uid, k_frame, pos):
    ef = EFF[name]; u = U[uid]; x, y, _ = pos[uid]
    im = ef["frames"][min(k_frame, len(ef["frames"]) - 1)]
    cx, cy = ef["off"][0] + ef["w"] / 2, ef["off"][1] + ef["h"] / 2
    if name != "dodge": cy = cy * u["height"] / SUBJ[name]
    if u["side"] == 1: im = im.transpose(FLIP); cx = -cx
    c.alpha_composite(im, (int(x + cx - ef["w"] / 2) - OX, int(y + cy - ef["h"] / 2) - OY))

RINGCACHE = {}
def status_ring(cols, rot, rx=28, ry=9):
    key = (tuple(cols), rot % 24, rx, ry)
    if key in RINGCACHE: return RINGCACHE[key]
    W, H = 2 * rx + 9, 2 * ry + 9; cx, cy = 4 + rx, 4 + ry
    m = Image.new("L", (W, H), 0); ImageDraw.Draw(m).ellipse((4, 4, 4 + 2 * rx, 4 + 2 * ry), outline=255, width=2)
    back = m.filter(ImageFilter.MaxFilter(3))
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0)); po, pm, pb = out.load(), m.load(), back.load(); n = len(cols)
    for yy in range(H):
        for xx in range(W):
            if pm[xx, yy]:
                ang = (math.atan2((yy - cy) * rx / ry, xx - cx) + rot * math.pi / 12) % (2 * math.pi)
                dark, mid, light = ramp(cols[int(ang / (2 * math.pi) * n) % n])
                po[xx, yy] = (light if yy > cy else mid) + (255,)
            elif pb[xx, yy]:
                po[xx, yy] = NIGHT + (190,)
    RINGCACHE[key] = (out, cx, cy); return RINGCACHE[key]

def dashed(d, p0, p1, col, phase=0, upto=1.0, dim=None, on=7, off=4):
    (x0, y0), (x1, y1) = p0, p1; L = math.hypot(x1 - x0, y1 - y0)
    if L < 1: return
    ux, uy = (x1 - x0) / L, (y1 - y0) / L; s = -(phase % (on + off))
    while s < L:
        a_, b_ = max(0.0, s), min(L, s + on)
        if b_ > a_:
            c_ = col if (a_ + b_) / 2 / L <= upto else (dim or col)
            d.line((x0 + ux * a_, y0 + uy * a_, x0 + ux * b_, y0 + uy * b_), fill=NIGHT + (210,), width=4)
            d.line((x0 + ux * a_, y0 + uy * a_, x0 + ux * b_, y0 + uy * b_), fill=c_ + (255,), width=2)
        s += on + off

def chevron(d, cx, cy, dx, dy, col):
    px_, py_ = -dy, dx
    pts = [(cx + dx * 6, cy + dy * 6), (cx + px_ * 6 - dx * 3, cy + py_ * 6 - dy * 3), (cx - px_ * 6 - dx * 3, cy - py_ * 6 - dy * 3)]
    d.polygon(pts, fill=col + (255,), outline=NIGHT + (255,))

def floor_look(c, t, pos, mk):
    """Look 1: everything on the ground."""
    d = ImageDraw.Draw(c, "RGBA")
    for uid, m in mk.items():
        if uid not in pos: continue
        x, y, _ = pos[uid]; X, Y = x - OX, y - OY
        if "guard" in m and m["guard"] in pos:
            gx, gy, _ = pos[m["guard"]]; dashed(d, (gx - OX, gy - OY + 2), (X, Y + 2), ramp(PX["mint"])[2], phase=t // 60)
        if "aim" in m and m["aim"][0] in pos:
            sx, sy, _ = pos[m["aim"][0]]; p = m["aim"][1]
            dashed(d, (sx - OX, sy - OY + 2), (X, Y + 2), ramp(PX["red"])[2], phase=0, upto=p, dim=ramp(PX["red"])[0])
    for uid, m in mk.items():
        if uid not in pos: continue
        x, y, _ = pos[uid]; X, Y = x - OX, y - OY
        if "snare" in m:
            c.alpha_composite(SNARE_RING, (int(X) - 65, int(Y) - 96))
        cols = [MCOL[k] for k in ("guard", "bleed", "open", "exposed", "stasis", "panic") if k in m]
        if cols:
            ring, cx, cy = status_ring(cols, int(t / 1000 * 24 * 0.2), 34, 11)
            c.alpha_composite(ring, (int(X) - cx, int(Y) - cy + 1))
        if "aim" in m:
            p = m["aim"][1]; r = 46 - 12 * p; light = ramp(PX["red"])[2]
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                ex, ey = X + dx * r, Y + 1 + dy * r / 3
                d.line((ex, ey, ex - dx * 9, ey - dy * 3), fill=NIGHT + (230,), width=4)
                d.line((ex, ey, ex - dx * 9, ey - dy * 3), fill=light + (255,), width=2)
        if "focus" in m:
            inset = 3 if (t // 200) % 2 else 0
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                rx, ry = 56 - inset, 19 - inset / 3
                chevron(d, X - dx * rx, Y + 1 - dy * ry, dx, dy, ramp(PX["red"])[2])

def overhead_look(c, t, pos, mk):
    """Look 2: a row of icons over the head."""
    for uid, m in mk.items():
        if uid not in pos: continue
        u = U[uid]; x, y, _ = pos[uid]
        icons = []
        for k in ("guard", "focus", "aim", "snare", "bleed", "open", "exposed", "stasis", "panic"):
            if k not in m: continue
            icons.append(FOCUS_ICON if k == "focus" else aim_icon(m["aim"][1]) if k == "aim" else ICON[k])
        if not icons: continue
        total = sum(i.width for i in icons) + 2 * (len(icons) - 1)
        x0 = int(x - total / 2); base = int(y - u["height"] - 4)
        for im in icons:
            bob = -2 if (im is FOCUS_ICON and (t // 200) % 2) else 0
            c.alpha_composite(im, (x0 - OX, base - im.height + bob - OY)); x0 += im.width + 2

CRACK, NET = {}, {}
def crack_layer(u, size):
    key = (u["id"], size)
    if key not in CRACK:
        idle = u["clips"]["idle"]; b = body_box(idle["frames"][0]); ay = idle["ay"]
        rnd = random.Random(zlib.crc32(u["id"].encode()))
        lay = Image.new("RGBA", size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay); dark, mid, light = ramp(PX["orange"])
        for n in range(3):
            x = b[0] + 1; y = ay - u["height"] * (0.40 + 0.13 * n) + rnd.randint(-3, 3); pts = [(x, y)]
            while x < b[2] - 1:
                x += rnd.randint(3, 6); y += rnd.choice((-3, -2, 2, 3)); pts.append((x, y))
            d.line(pts, fill=mid + (255,), width=3); d.line(pts, fill=light + (255,), width=1)
        CRACK[key] = lay
    return CRACK[key]

def net_layer(u, size):
    key = (u["id"], size)
    if key not in NET:
        idle = u["clips"]["idle"]; ay = idle["ay"]; cut = ay - u["height"] * 0.45
        lay = Image.new("RGBA", size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay); dark, mid, light = ramp(PX["lilac"])
        for s in range(-size[1], size[0] + size[1], 7):
            d.line((s, 0, s + size[1], size[1]), fill=light + (255,), width=1)
            d.line((s, size[1], s + size[1], 0), fill=light + (255,), width=1)
        mask = Image.new("L", size, 0); ImageDraw.Draw(mask).rectangle((0, int(cut), size[0], ay), fill=255)
        lay.putalpha(ImageChops.multiply(lay.getchannel("A"), mask)); NET[key] = lay
    return NET[key]

def over_mask(im, layer, mask, amount=1.0):
    a = ImageChops.multiply(layer.getchannel("A"), mask)
    if amount < 1: a = a.point(lambda v: int(v * amount))
    lay = layer.copy(); lay.putalpha(a); out = im.copy(); out.alpha_composite(lay); return out

def body_look(u, im, t, m):
    """Look 3: the marks on the fighter's own sprite."""
    if not m: return im, 0
    mask = solid(im); out = im
    if "stasis" in m:                               # frozen: grey stone, a cold cast
        g = im.convert("L").convert("RGBA"); g.putalpha(im.getchannel("A")); out = tint(g, (190, 205, 228), 0.35)
    if "exposed" in m:
        edge = ImageChops.subtract(mask, mask.filter(ImageFilter.MinFilter(3)))
        lay = Image.new("RGBA", im.size, ramp(PX["blue"])[2] + (255,)); lay.putalpha(edge.point(lambda v: v * 4 // 5)); out = out.copy(); out.alpha_composite(lay)
    if "open" in m: out = over_mask(out, crack_layer(u, im.size), mask, 0.7 + 0.3 * math.sin(t / 160))
    if "snare" in m: out = over_mask(out, net_layer(u, im.size), mask, 0.9)
    rings = []
    if "focus" in m: rings.append((PX["red"] if (t // 250) % 2 == 0 else ramp(PX["red"])[2], 2))
    if "aim" in m:
        period = int(300 - 220 * m["aim"][1])
        if (t // max(60, period)) % 2 == 0: rings.append((ramp(PX["red"])[2], 2))
    if "guard" in m: rings.append((PX["mint"], 2))
    if rings:
        base = Image.new("RGBA", im.size, (0, 0, 0, 0)); prev = mask
        for col, wd in rings[:2]:
            dil = prev.filter(ImageFilter.MaxFilter(2 * wd + 1)); ring = ImageChops.subtract(dil, prev)
            lay = Image.new("RGBA", im.size, col + (255,)); lay.putalpha(ring); base.alpha_composite(lay); prev = dil
        base.alpha_composite(out); out = base
    dx = (1 if (t // 60) % 2 else -1) if "panic" in m else 0
    return out, dx

def drips(c, t, pos, mk):
    """Look 3: blood running off a bleeding fighter."""
    d = ImageDraw.Draw(c); dark, mid, light = ramp(PX["red"])
    for uid, m in mk.items():
        if "bleed" not in m or uid not in pos: continue
        u = U[uid]; x, y, _ = pos[uid]
        for s in range(t // 160 - 4, t // 160 + 1):
            r = random.Random(zlib.crc32(f"{uid}:{s}".encode())); t0 = s * 160 + r.randint(0, 80); age = t - t0
            if age < 0: continue
            sx = int(x + (r.random() - 0.5) * u["width"] * 0.55) - OX; sy = y - u["height"] * (0.28 + 0.22 * r.random())
            fall = 0.0009 * age * age; land_at = math.sqrt(max(0.0, (y - sy) / 0.0009))
            if sy + fall < y - 1:
                yy = int(sy + fall) - OY; d.rectangle((sx, yy, sx + 1, yy + 2), fill=mid); d.point((sx, yy), fill=light); d.point((sx + 1, yy + 2), fill=dark)
            elif age < land_at + 260:
                d.rectangle((sx - 1, int(y) - OY, sx + 2, int(y) - OY), fill=dark)

def draw_units(c, t, act_fx, pos, mk):
    for u in sorted(U.values(), key=lambda u_: pos[u_["id"]][1]):
        x, y, moving = pos[u["id"]]
        clip, j, key = frame_of(u, t)
        fleeing = u.get("flee") is not None and t >= u["flee"]
        if clip is None or fleeing:
            clip = u["clips"]["run"] if (moving or fleeing) else u["clips"]["idle"]
            j = int(t // (90 if (moving or fleeing) else 100)) % len(clip["frames"])
        im = clip["frames"][j]; ax, ay = clip["ax"], clip["ay"]
        if fleeing: im = im.transpose(FLIP); ax = im.width - 1 - ax
        dx = 0; white = red = gold = False; glow = 0.0; jit = None
        for f in act_fx:
            if f[0] == "white" and f[3] == u["id"]: white = True
            elif f[0] == "tint" and f[3] == u["id"] and ((t - f[1]) // 80) % 2 == 0: red = True
            elif f[0] == "glow" and f[3] == u["id"]: glow = max(glow, 0.35 * (1 - (t - f[1]) / f[2]))
            elif f[0] == "gold" and f[3] == u["id"]: gold = True
            elif f[0] == "jitter" and f[3] == u["id"]: jit = 1 if ((t - f[1]) // 40) % 2 == 0 else -1
            elif f[0] == "shove" and f[3][0] == u["id"]:
                p = (t - f[1]) / f[2]; dx += f[3][1] * (p / 0.25 if p < 0.25 else 1 - (p - 0.25) / 0.75)
            elif f[0] == "dodge" and f[3][0] == u["id"]:
                p = (t - f[1]) / f[2]; dx += f[3][1] * (min(1, p / 0.15) if p < 0.7 else (1 - p) / 0.3)
        # each flash once, however many blows are behind it
        if jit: dx += jit
        if white: im = flat(im, (255, 255, 255))
        elif gold: im = tint(im, PX["gold"], 0.65)
        else:
            if red: im = tint(im, (210, 30, 20), 0.25)
            if glow > 0: im = tint(im, (120, 255, 140), glow)
            if eclipse and u["chief"] and eclipse["start"] <= t < (eclipse["release"] or 10 ** 9):
                im = tint(im, (255, 70, 30), 0.18 + 0.14 * math.sin(t / 70))
        if mk is not None:
            im, ddx = body_look(u, im, t, mk.get(u["id"])); dx += ddx
        c.alpha_composite(im, (int(round(x + dx)) - ax - OX, int(round(y)) - ay - OY))

def ground_common(c, t, act_fx, pos):
    if eclipse:
        ef = (t - eclipse["start"]) // 80; rel = eclipse["release"]
        for xid in eclipse["targets"]:
            fx_, fy_ = U[xid]["home"]
            if 0 <= ef < len(E["tell_under"]["frames"]) and (rel is None or t < rel):
                paste_center(c, E["tell_under"]["frames"][ef], fx_, fy_)
            if rel is not None and rel <= t < rel + len(E["break_under"]["frames"]) * 80:
                paste_center(c, E["break_under"]["frames"][(t - rel) // 80], fx_, fy_)
    d0 = ImageDraw.Draw(c)
    for f in act_fx:
        if f[0] in ("ring", "wall") and f[3] in pos:
            x, y, _ = pos[f[3]]
            k_ = min(4, (t - f[1]) // 90) if f[0] == "ring" else 5 + min(3, (t - f[1]) // 80)
            c.alpha_composite(RISE[k_], (int(x) - 64 - OX, int(y) - 108 - OY))
        elif f[0] == "parry" and f[3] in pos:
            x, y, _ = pos[f[3]]; X, Y = x - OX, y - OY; p = (t - f[1]) / f[2]; r_out = 74 - 40 * p
            d0.ellipse((X - 46, Y - 15, X + 46, Y + 15), outline=(255, 205, 90, 255), width=3)
            d0.ellipse((X - r_out, Y - r_out * 0.33, X + r_out, Y + r_out * 0.33), outline=(255, 235, 160, 255), width=2)

def over_common(c, t, act_fx, pos):
    d = ImageDraw.Draw(c)
    for f in act_fx:
        if f[0] == "jaws" and f[3] in pos:
            x, y, _ = pos[f[3]]; c.alpha_composite(JAWS[min(8, (t - f[1]) // 90)], (int(x) - 48 - OX, int(y) - 84 - OY))
        elif f[0] == "bullet":
            a_id, v_id = f[3]
            if a_id in pos and v_id in pos:
                ax_, ay_, _ = pos[a_id]; vx, vy, _ = pos[v_id]; fa = U[a_id]["face"]
                sx, sy = ax_ + fa * 40 - OX, ay_ - U[a_id]["height"] * 0.6 - OY; ex, ey = vx - OX, vy - U[v_id]["height"] * 0.55 - OY
                p = (t - f[1]) / f[2]; hx, hy = sx + (ex - sx) * p, sy + (ey - sy) * p; L = math.hypot(ex - sx, ey - sy) or 1
                tx, ty = hx - (ex - sx) / L * 26, hy - (ey - sy) / L * 26
                d.line((tx, ty, hx, hy), fill=(255, 225, 140), width=3); d.line((tx, ty, hx, hy), fill=(255, 255, 240), width=1)
        elif f[0] == "eff" and f[3][1] in pos:
            draw_eff(c, f[3][0], f[3][1], (t - f[1]) // EFF[f[3][0]]["ms"], pos)

def texts(c, t, act_fx, pos, L, mk):
    d = ImageDraw.Draw(c)
    words = {}
    for f in act_fx:
        if f[0] == "word" and f[3][0] in pos and (f[3][0] not in words or f[1] >= words[f[3][0]][1]): words[f[3][0]] = f
    placed = []
    for uid, f in sorted(words.items(), key=lambda kv: kv[1][1]):
        u = U[uid]; x, y, _ = pos[uid]; im = WORD[f[3][1]]; p = (t - f[1]) / f[2]; rise = int(6 * min(1, p * 2))
        lift = 4                                   # just over the head; in look 2 over his icons
        if L == 1 and mk.get(uid): lift += (32 if "focus" in mk[uid] else 26) + 2
        bx, by = int(x - im.width / 2), int(y - u["height"] - lift - im.height - rise)
        while any(bx < q[2] and q[0] < bx + im.width and by < q[3] and q[1] < by + im.height for q in placed):
            by -= 4                                # two words at once on neighbours: the newer one goes over the older
        placed.append((bx, by, bx + im.width, by + im.height))
        c.alpha_composite(im, (bx - OX, by - OY))
    stacks = {}
    for f in sorted([f for f in act_fx if f[0] == "num"], key=lambda f: f[1]): stacks.setdefault(f[3][0], []).append(f)
    for uid, lst in stacks.items():
        if uid not in pos: continue
        u = U[uid]; x, y, _ = pos[uid]
        for j, f in enumerate(reversed(lst[-3:])):
            p = (t - f[1]) / f[2]; rise = int(12 * min(1, p * 3))
            tx = x + u["width"] / 2 + 2 - OX; ty = y - u["height"] * 0.62 - rise - j * 18 - OY
            d.text((tx, ty), f[3][1], font=BIGF if f[3][3] else NUMF, fill=f[3][2], stroke_width=2, stroke_fill=NIGHT)
    titles = [f for f in act_fx if f[0] == "title"]
    if titles:                                     # one title at a time: the newest
        f = max(titles, key=lambda f: f[1])
        tw = d.textlength(f[3], font=TITLE); d.text((MG + (PW - tw) / 2, MG + 6), f[3], font=TITLE, fill=(255, 228, 170), stroke_width=3, stroke_fill=(30, 8, 6))

NAMES = ["1  FLOOR   rings and lines on the ground", "2  OVERHEAD   icons over the head", "3  BODY   outlines and marks on the fighter"]
def compose(scenes, t, shake):
    frame = Image.new("RGB", (PW, 3 * (HDR + PH) + FTR), (14, 9, 7)); d = ImageDraw.Draw(frame)
    for L in range(3):
        y0 = L * (HDR + PH)
        frame.paste(scenes[L].crop((MG + shake, MG, MG + shake + PW, MG + PH)).convert("RGB"), (0, y0 + HDR))
        d.text((8, y0 + 3), NAMES[L], font=LABEL, fill=(240, 222, 186))
    tick = max(0, min(LAST, tick_at(t)))
    d.text((8, 3 * (HDR + PH) + 2), f"the same real fight three times, rules 42, seed 5100 road fight 9   tick {tick}", font=SMALL, fill=(200, 186, 160))
    return frame

def render(t, i):
    pos = positions[i]; act_fx = [f for f in FX if f[1] <= t < f[1] + f[2]]; mk = marks_at(t)
    base = BG.copy(); ground_common(base, t, act_fx, pos)
    sc = [base.copy(), base.copy(), base]
    floor_look(sc[0], t, pos, mk)
    for L in range(3): draw_units(sc[L], t, act_fx, pos, mk if L == 2 else None)
    for L in range(3): over_common(sc[L], t, act_fx, pos)
    overhead_look(sc[1], t, pos, mk)
    drips(sc[2], t, pos, mk)
    for L in range(3): texts(sc[L], t, act_fx, pos, L, mk)
    shake = 0
    for f in act_fx:
        if f[0] == "shake": shake = f[3] if ((t - f[1]) // 40) % 2 == 0 else -f[3]
    return compose(sc, t, shake)

# ------------------------------------------------------------------ outputs
if __name__ == "__main__":
    # the icons, at 4x, to check before anything else
    ic = Image.new("RGBA", (10 * 120, 140), (60, 48, 40, 255)); di = ImageDraw.Draw(ic)
    allicons = [(k, ICON[k]) for k in GLYPH] + [("aim 0.3", aim_icon(0.3)), ("aim 1.0", aim_icon(1.0)), ("focus", FOCUS_ICON)]
    for n, (k, im) in enumerate(allicons):
        big = im.resize((im.width * 4, im.height * 4), Image.NEAREST); ic.alpha_composite(big, (n * 120 + 4, 24)); di.text((n * 120 + 4, 4), k, fill=(255, 255, 0))
    ic.save(OUT + "/icons_check.png")
    out_frames = []
    for i, t in enumerate(frames_t):
        out_frames.append(render(t, i))
        if i % 50 == 0: print("frame", i, "of", len(frames_t), flush=True)
    for tk in [int(x) for x in os.environ.get("KS_STILLS", "").split(",") if x]:
        if ms(tk) // STEP < len(out_frames): out_frames[ms(tk) // STEP].save(f"{OUT}/still_tick{tk}.png")
    # the file: from the last steps of the charge to the rout, 80 ms frames, every white flash frame kept
    first = 900 // STEP; last = min(len(out_frames), ((end_t or END_MS) + 2400) // STEP) if not LIMIT else len(out_frames)
    whites = {i for i, t in enumerate(frames_t) if any(f[0] == "white" and f[1] <= t < f[1] + f[2] for f in FX)}
    keep = [i for i in range(first, last) if (i - first) % 2 == 0 or i in whites]
    durs = [((keep[n + 1] if n + 1 < len(keep) else last) - i) * STEP for n, i in enumerate(keep)]
    name = "unit-looks-three-v1" + ("-test" if LIMIT else "")
    clip_ = [out_frames[i] for i in keep]
    clip_[0].save(f"{OUT}/{name}.webp", save_all=True, append_images=clip_[1:], duration=durs, loop=0,
                  quality=80, method=6, minimize_size=True, allow_mixed=True)
    # audit sheets: every 200 ms, the three looks side by side at half size
    os.makedirs(OUT + "/audit", exist_ok=True)
    steps = list(range(0, len(out_frames), 5)); per = 6; cw, ch = PW // 2, PH // 2
    for s0 in range(0, len(steps), per):
        sh = Image.new("RGB", (3 * cw + 70, per * (ch + 4)), (0, 0, 0)); dd = ImageDraw.Draw(sh)
        for r, i in enumerate(steps[s0:s0 + per]):
            f = out_frames[i]
            for L in range(3):
                panel = f.crop((0, L * (HDR + PH) + HDR, PW, L * (HDR + PH) + HDR + PH)).resize((cw, ch), Image.LANCZOS)
                sh.paste(panel, (70 + L * cw, r * (ch + 4)))
            dd.text((4, r * (ch + 4) + 4), f"{i * STEP} ms", fill=(255, 255, 0)); dd.text((4, r * (ch + 4) + 20), f"tick {max(0, tick_at(i * STEP))}", fill=(255, 255, 0))
        sh.save(f"{OUT}/audit/audit_{s0 // per:02d}.png")
    # key moments for Jacob: rows are moments, columns the three looks
    if not LIMIT:
        moments = [40, 100, 176, 205]
        ks = Image.new("RGB", (3 * PW + 6, len(moments) * (PH + 26) + 30), (14, 9, 7)); dk = ImageDraw.Draw(ks)
        for L in range(3): dk.text((L * (PW + 3) + 8, 6), NAMES[L], font=LABEL, fill=(240, 222, 186))
        for r, tk in enumerate(moments):
            i = ms(tk) // STEP; f = out_frames[i]; y0 = 30 + r * (PH + 26)
            dk.text((8, y0 + 4), f"tick {tk}", font=SMALL, fill=(200, 186, 160))
            for L in range(3):
                ks.paste(f.crop((0, L * (HDR + PH) + HDR, PW, L * (HDR + PH) + HDR + PH)), (L * (PW + 3), y0 + 22))
        ks.save(f"{OUT}/unit-looks-key-moments.png")
        for tk in moments: out_frames[ms(tk) // STEP].save(f"{OUT}/frame_tick{tk}.png")
    print("frames", len(out_frames), "ms", END_MS, "webp MB", round(os.path.getsize(f"{OUT}/{name}.webp") / 1e6, 2))
