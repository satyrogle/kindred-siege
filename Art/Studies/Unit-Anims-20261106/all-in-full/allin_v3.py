"""All in fight, take 3, from the same real rules 41 fight (seed 5100, fight 9, dump55.txt). Fixes from the 6 Nov audit:
lanes lock the real pairs one on one, white flash only on big hits, one number stack per unit, the real Bloodridge chief
sprite, our leader on the field with a gold ring on the chief's tells, the approved Eclipse sheets (log 10634) with the
sim's dodge, Guard's wall only when a blow is guarded, a clean death, and their side breaking and running.
Usage: python -I allin_v3.py <work dir> <dump file> [stills ms list]"""
import sys, zipfile, io, os, math, json
from PIL import Image, ImageDraw, ImageFont, ImageOps

A, DUMP = sys.argv[1], sys.argv[2]
KS = "C:/Users/jacob/KindredSiege-Clean/"
R = KS + "Assets/StreamingAssets/CampaignArt/Bloodridge/"
bg = Image.open(R + "ground-underpaint.png").convert("RGBA")
for l in ["rear-burial-architecture.png", "rooted-entrance.png"]:
    bg.alpha_composite(Image.open(R + l).convert("RGBA"))
CM = KS + "Art/Studies/Class-Moves-20261106/"
JAWS = [Image.open(f"{CM}snare/v2-props/parts/jaws_anim/{i}.png").convert("RGBA").resize((96, 96), Image.NEAREST) for i in range(9)]
RISE = [Image.open(f"{CM}guard/parts/rise_anim/{i}.png").convert("RGBA") for i in range(9)]
EC = KS + "Art/Studies/Chief-Cut-Ins-20261018/spell-language/game-topdown/eclipse/"

def sheet(path):
    d = json.load(open(path + ".json")); im = Image.open(path + ".png").convert("RGBA")
    return [im.crop((f["frame"]["x"], f["frame"]["y"], f["frame"]["x"] + f["frame"]["w"], f["frame"]["y"] + f["frame"]["h"])) for f in d["frames"]]
E = {k: sheet(EC + "eclipse_" + k) for k in ["tell_under", "dark_under", "wait_under", "hit_behind", "hit_front", "break_under",
                                              "comet_over", "sun_tell_over", "sun_dark_over", "sun_flash_over", "sun_loop_over", "sun_out_over"]}
CA = KS + "Assets/Resources/CampaignArt/"
CHIEF = {k: [f.resize((round(f.width * 0.72), round(f.height * 0.72)), Image.NEAREST) for f in sheet(CA + f"bloodridge_chief_sw_{k}")] for k in ["idle", "attack", "hit", "death"]}
CHIEF["death"] = CHIEF["death"][len(CHIEF["death"]) // 3:]  # his clip opens with a last swing; start at the fall
LEADER = {k: sheet(CA + f"Leader/leader_se_{k}") for k in ["idle", "attack", "block", "hit", "victory"]}

TPS = 30; CHARGE = 1600; SEAM = 720
def ms(tick): return CHARGE + tick * 1000 // TPS

ZIPS = {}
def clip(cid, prefix, d):
    if cid not in ZIPS: ZIPS[cid] = zipfile.ZipFile(f"{A}/zips/{cid}.zip")
    z = ZIPS[cid]; dd = "south-east" if d == "se" else "south-west"
    names = sorted({n.split("/animations/")[1].split("/")[0] for n in z.namelist() if "/animations/" in n and n.split("/animations/")[1].startswith(prefix)})
    names = [n for n in names if f"_{d}_" in n]
    assert names, (cid, prefix, d)
    fr = sorted(n for n in z.namelist() if f"/animations/{names[0]}/{dd}/" in n)
    return [Image.open(io.BytesIO(z.read(x))).convert("RGBA") for x in fr]

def feet(im):
    b = im.split()[3].point(lambda v: 255 if v > 20 else 0).getbbox()
    return ((b[0] + b[2]) // 2, b[3]) if b else (im.width // 2, im.height)

def contact(frames, face):
    best, bi = None, len(frames) // 2
    for i, f in enumerate(frames[1:-2], 1):
        b = f.split()[3].point(lambda v: 255 if v > 20 else 0).getbbox()
        if not b: continue
        reach = b[2] if face > 0 else -b[0]
        if best is None or reach > best: best, bi = reach, i
    return bi

CIDS = {("warden", 0): "d917de88-8dfc-4de2-97f0-4dc46e06cb7d", ("warden", 1): "b3fef59f-4ba2-4f95-a526-1228e1f8b49d",
        ("skirmisher", 0): "a3aa47e7-da8b-47f5-a04d-dadb14721a65", ("skirmisher", 1): "a1195c8f-03c0-42f8-8f8f-135b8e98d09c",
        ("trapper", 0): "44547cca-99d8-432c-bc41-ab629db97136", ("trapper", 1): "720d540c-6f12-49b8-b409-3f84e8102bf2",
        ("marksman", 0): "9e0233b1-4a8a-4d81-99d8-53351b1cf28d", ("marksman", 1): "19b7cc53-fb54-45dd-95ee-433ed538dd10",
        ("medic", 0): "20b62125-093d-41ca-a5d2-eb2f15d3fd25", ("medic", 1): "e7aceef9-d12c-4c53-8817-318de3265c03"}

def kit(cls, side):
    cid = CIDS[(cls, side)]; d = "se" if side == 0 else "sw"
    c = {key: clip(cid, f"set_{cls}_{key}", d) for key in ["idle", "attack1", "attack2", "block", "hit", "death", "power"]}
    if cls == "trapper" and side == 0:
        try: c["block"] = clip(cid, "set_trapper_block2", d)
        except AssertionError: pass
    run = "lock_run" if (cls == "warden" and side == 1) else "allin_run"  # their Warden's old run lost his tall shield
    c["run"] = clip(cid, f"{run}_{d}", d)
    return c

# ---------- the fight ----------
units, events = {}, []
for line in open(DUMP):
    p = line.split()
    if p[0] == "U": units[p[2]] = dict(side=int(p[1]), cls=p[3].lower(), role=p[4])
    if p[0] == "E":
        tick, kind, side, actor = int(p[1]), p[2], int(p[3]), p[4]
        rest = p[5:]
        if actor == ">": actor, rest = "", p[4:]
        target = rest[1] if len(rest) > 1 and not rest[1].lstrip("-").isdigit() else ""
        nums = [x for x in rest[1:] if x.lstrip("-").isdigit()]
        detail = rest[-1] if rest and not rest[-1].lstrip("-").isdigit() and rest[-1] != target else ""
        events.append(dict(tick=tick, kind=kind, side=side, actor=actor, target=target, amount=int(nums[0]) if nums else 0, detail=detail))

T = "camp.11.alpha.alphahunt.alpha."
HOME = {"clarth.warden.1": (672, 448), "clarth.skirmisher.3": (664, 568), "clarth.trapper.4": (672, 690),
        "clarth.marksman.2": (482, 478), "clarth.medic.5": (470, 662), "leader": (338, 578),
        T + "1": (768, 448), T + "captain": (790, 572), T + "4": (836, 505), T + "2": (768, 690),
        T + "3": (985, 478), T + "0": (1000, 662)}
LANE = {"clarth.warden.1": T + "1", T + "1": "clarth.warden.1", "clarth.skirmisher.3": T + "captain", T + "captain": "clarth.skirmisher.3",
        "clarth.trapper.4": T + "2", T + "2": "clarth.trapper.4", T + "4": "clarth.skirmisher.3"}
U = {}
for uid, h in HOME.items():
    u = units[uid]; side = u["side"]
    if uid == "leader":
        clips = {"idle": LEADER["idle"], "attack1": LEADER["attack"], "attack2": LEADER["attack"], "block": LEADER["block"],
                 "hit": LEADER["hit"], "death": LEADER["hit"], "power": LEADER["attack"], "run": LEADER["idle"], "victory": LEADER["victory"]}
    elif u["role"] == "Chief":
        clips = {"idle": CHIEF["idle"], "attack1": CHIEF["attack"], "attack2": CHIEF["attack"], "block": CHIEF["hit"], "hit": CHIEF["hit"],
                 "death": CHIEF["death"], "power": CHIEF["attack"], "run": CHIEF["idle"]}
    else:
        clips = kit(u["cls"], side)
    U[uid] = dict(id=uid, side=side, cls=u["cls"], chief=u["role"] == "Chief", leader=uid == "leader", face=1 if side == 0 else -1,
                  clips=clips, home=h, x=float(h[0]), y=float(h[1]), acts=[], dead_at=None, alt=0,
                  melee=u["cls"] in ("warden", "skirmisher", "trapper") and uid != "leader", targets=[])

def timing(n, c, kind):
    if kind == "react": return [50, 50] + [80] * (n - 2)
    if kind == "death": return [70, 70] + [85] * (n - 3) + [10 ** 7]
    if kind == "loop": return [100] * n
    return [55 if i < c - 1 else 200 if i == c - 1 else 140 if i == c else 70 for i in range(n)]

FX, FREEZE = [], []

def busy(u, at):
    for s, fr, d, key in u["acts"]:
        if key in ("attack1", "attack2", "power") and s <= at < s + sum(d): return True
    return False

def act(u, key, at, kind="attack"):
    fr = u["clips"][key]
    c = contact(fr, u["face"]) if kind == "attack" else 1
    d = timing(len(fr), c, kind)
    start = at - (sum(d[:c]) if kind == "attack" else 40)
    u["acts"].append((start, fr, d, key))

def num(uid, t, text, col, big=False):
    FX.append(("num", t, 950, (uid, text, col, big)))

POWER = ("strike", "aimed", "flurry", "twincut", "rip")
seen_marks = {}
eclipse = None
end_t = None
for e in events:
    t = ms(e["tick"]); a = U.get(e["actor"]); v = U.get(e["target"]); k, det = e["kind"], e["detail"]
    if k in ("Hit", "Miss"):
        if a:
            power = det.startswith(POWER)
            key = "power" if power else ("attack1" if a["alt"] % 2 == 0 else "attack2"); a["alt"] += 1
            act(a, key, t)
            if v: a["targets"].append((t - 600, v["id"]))
            if a["cls"] == "marksman" and v or a["leader"] and v: FX.append(("bullet", t - 120, 140, (a["id"], v["id"])))
        if v and v["dead_at"] is None:
            big = e["amount"] >= 40 or det.startswith(POWER) or (a is not None and a["chief"])
            if k == "Miss":
                if not busy(v, t): act(v, "block", t, "react")
                num(v["id"], t, "MISS", (235, 225, 200))
            else:
                if det != "trap" and not busy(v, t): act(v, "hit", t, "react")
                FX += [("tint", t, 90, v["id"]), ("shove", t, 260, (v["id"], -v["face"] * (14 if big else 6)))]
                if big:
                    FX.append(("flash", t, 50, v["id"])); FREEZE.append((t, 80))
                num(v["id"], t, str(e["amount"]), (255, 200, 60) if big else (255, 80, 60), big)
                if det == "guarded":
                    FX.append(("wall", t - 120, 4 * 80 + 300, e["target"]))
    elif k == "Heal" and a and v:
        if not busy(a, t): act(a, "attack2", t)
        num(v["id"], t, "+" + str(e["amount"]), (120, 240, 130)); FX.append(("glow", t, 280, v["id"]))
    elif k == "TrapArmed" and a:
        if not busy(a, t): act(a, "power", t)
    elif k == "TrapSprung" and v:
        FX.append(("jaws", t - 80, 9 * 90 + 700, v["id"]))
    elif k == "Guard" and a and v:
        act(a, "power", t)
        FX += [("ring", t - 200, 2200, v["id"]), ("word", t, 900, (a["id"], "GUARD"))]
    elif k == "Mark" and v and det in ("bleed", "snare", "exposed"):
        if t - seen_marks.get((v["id"], det), -10 ** 9) > 2500:
            seen_marks[(v["id"], det)] = t; FX.append(("word", t + 150, 850, (v["id"], det.upper())))
    elif k == "Tell" and a and a["chief"]:
        FX.append(("parry", t, 1100, "leader"))
    elif k == "Windup" and a and a["chief"]:
        n = len(E["tell_under"]); dur = n * 80
        targets = [x for x in ("clarth.warden.1", "clarth.skirmisher.3", "clarth.trapper.4") if U[x]["dead_at"] is None]
        eclipse = dict(start=t, targets=targets, release=None)
        a["acts"].append((t, [CHIEF["attack"][0]] * 1 + [CHIEF["attack"][1]], [120, dur], "windup"))
        FX += [("parry", t, dur + 300, "leader"), ("title", t, 1200, "ECLIPSE")]
    elif k == "Release" and a and a["chief"] and eclipse:
        eclipse["release"] = t; eclipse["outcome"] = det
        for x in eclipse["targets"]:
            FX.append(("dodge", t, 2200, (x, -95)))
    elif k == "Kill" and v:
        v["dead_at"] = t
        act(v, "death", t, "death")
        last = any(x["kind"] == "End" for x in events if x["tick"] == e["tick"])
        FX += [("flash", t, 60, v["id"]), ("shake", t, 280, 5)]
        FREEZE.append((t, 300 if last else 160))
        if last:
            end_t = t
            FX += [("zoom", t, 2300, v["id"]), ("title", t + 900, 2600, "BREAK")]
        else:
            FX.append(("zoom", t, 1000, v["id"]))

# Eclipse comets: launch Release+1, each next on the frame the one before hits; frames of 80 ms.
COMET = []
if eclipse and eclipse["release"]:
    r = eclipse["release"]; sun = (UNITS_SUN := (HOME["clarth.skirmisher.3"][0] + 180, HOME["clarth.skirmisher.3"][1] - 380))
    launch = r + 80
    for i, x in enumerate(eclipse["targets"]):
        fx_, fy_ = HOME[x]
        path = math.hypot(fx_ - sun[0], fy_ - 2 - sun[1]); falls = max(4, round(path / 110))
        hit = launch + (falls - 1) * 80
        COMET.append(dict(target=x, feet=(fx_, fy_), launch=launch, hit=hit, falls=falls, slot=i))
        FX += [("ehit", hit, 14 * 80, (fx_, fy_)), ("shake", hit, 240, 5 if i == 0 else 4)]
        launch = hit
    eclipse["last_hit"] = COMET[-1]["hit"]; eclipse["sun"] = sun
    FX.append(("title", COMET[0]["hit"] + 80, 1100, "DODGED"))

# The rout: their nerve broke on the last kill; the living run off to the right, our leader cheers.
if end_t:
    for u in U.values():
        if u["side"] == 1 and u["dead_at"] is None:
            u["flee"] = end_t + 1500
    U["leader"]["acts"].append((end_t + 1400, LEADER["victory"], [100] * (len(LEADER["victory"]) - 1) + [10 ** 7], "victory"))
END_MS = (end_t or ms(max(e["tick"] for e in events))) + 5600

for u in U.values():
    u["targets"].sort()

def goal_xy(u, t):
    if u["dead_at"] is not None and t >= u["dead_at"]: return None
    if u.get("flee") and t >= u["flee"]: return (u["home"][0] + 420, u["home"][1])
    if not u["melee"]: return u["home"]
    tg = None
    for gt, tid in u["targets"]:
        if gt <= t and (U[tid]["dead_at"] is None or U[tid]["dead_at"] > gt): tg = tid
    if tg is None or tg == LANE.get(u["id"]): return u["home"]
    v = U[tg]
    return (v["x"] - u["face"] * (100 if v["chief"] else 88), v["y"] + (-46 if u["home"][1] < v["y"] else 46))

STEP = 40
def anim_time(rt):
    return rt - sum(min(l, max(0, rt - s)) for s, l in FREEZE)

positions = {}
for u in U.values():
    u["x"] = u["home"][0] - u["face"] * (320 if u["melee"] else 180)
frames_t = list(range(0, END_MS, STEP))
for i, rt in enumerate(frames_t):
    t = anim_time(rt); snap = {}
    for u in U.values():
        moving = False
        if t < CHARGE:
            p = min(1, t / CHARGE); ease = 1 - (1 - p) ** 2
            sx = u["home"][0] - u["face"] * (320 if u["melee"] else 180)
            u["x"] = sx + (u["home"][0] - sx) * ease; moving = p < 1
        else:
            g = goal_xy(u, t)
            if g:
                fast = u.get("flee") and t >= u["flee"]
                dx, dy = g[0] - u["x"], g[1] - u["y"]; dist = math.hypot(dx, dy); sp = (320 if fast else 190) * STEP / 1000
                if dist > 5:
                    u["x"] += dx / dist * min(sp, dist); u["y"] += dy / dist * min(sp, dist); moving = dist > 12
        snap[u["id"]] = [u["x"], u["y"], moving]
    positions[i] = {k_: tuple(v_) for k_, v_ in snap.items()}

try:
    NUMF = ImageFont.truetype("C:/Windows/Fonts/impact.ttf", 17); BIGF = ImageFont.truetype("C:/Windows/Fonts/impact.ttf", 24)
    SMALL = ImageFont.truetype("C:/Windows/Fonts/impact.ttf", 12); TITLE = ImageFont.truetype("C:/Windows/Fonts/impact.ttf", 40)
except Exception:
    NUMF = BIGF = SMALL = TITLE = ImageFont.load_default()

def tint(im, col, a):
    al = im.split()[3]; out = Image.blend(im, Image.new("RGBA", im.size, col + (255,)), a); out.putalpha(al); return out

def frame_of(u, t):
    cur = None
    for s, fr, d, key in u["acts"]:
        if s <= t: cur = (s, fr, d, key)
    if cur:
        s, fr, d, key = cur; lt = t - s; acc = 0
        for j, dd in enumerate(d):
            acc += dd
            if lt < acc: return fr[min(j, len(fr) - 1)], key
    if u["dead_at"] is not None and t >= u["dead_at"]: return u["clips"]["death"][-1], "dead"
    return None, None

def paste_center(c, im, x, y):
    c.alpha_composite(im, (int(x - im.width / 2), int(y - im.height / 2)))

BASE = (300, 330, 1140, 802)
WIDE = (200, 110, 1260, 706)  # shows the black sun during Eclipse
def lerp_box(a, b, k): return tuple(a[j] + (b[j] - a[j]) * k for j in range(4))

out_frames = []
for i, rt in enumerate(frames_t):
    t = anim_time(rt)
    c = bg.copy()
    act_fx = [f for f in FX if f[1] <= rt < f[1] + f[2]]
    pos = positions[i]
    # ---- ground layer
    if eclipse:
        ef = (rt - eclipse["start"]) // 80
        for x in eclipse["targets"]:
            fx_, fy_ = HOME[x]
            if 0 <= ef < len(E["tell_under"]) and (eclipse["release"] is None or rt < eclipse["release"]):
                paste_center(c, E["tell_under"][ef], fx_, fy_)
            if eclipse["release"]:
                cm = [m for m in COMET if m["target"] == x][0]
                if eclipse["release"] <= rt < eclipse["release"] + 80: paste_center(c, E["dark_under"][0], fx_, fy_)
                elif eclipse["release"] + 80 <= rt < cm["hit"]: paste_center(c, E["wait_under"][ef % 2], fx_, fy_)
                elif cm["hit"] + 14 * 80 <= rt < cm["hit"] + 14 * 80 + len(E["break_under"]) * 80:
                    paste_center(c, E["break_under"][(rt - cm["hit"] - 14 * 80) // 80], fx_, fy_)
    for f in act_fx:
        if f[0] == "ring" and f[3] in pos:
            x, y, _ = pos[f[3]]; k_ = min(4, (rt - f[1]) // 90)
            c.alpha_composite(RISE[k_], (int(x) - 64, int(y) - 108))
        if f[0] == "wall" and f[3] in pos:
            x, y, _ = pos[f[3]]; k_ = 5 + min(3, (rt - f[1]) // 80)
            c.alpha_composite(RISE[k_], (int(x) - 64, int(y) - 108))
        if f[0] == "parry" and f[3] in pos:
            x, y, _ = pos[f[3]]; p = (rt - f[1]) / f[2]
            d0 = ImageDraw.Draw(c)
            r_out = 74 - 40 * p  # the closing ring
            d0.ellipse((x - 46, y - 15, x + 46, y + 15), outline=(255, 205, 90, 255), width=3)
            d0.ellipse((x - r_out, y - r_out * 0.33, x + r_out, y + r_out * 0.33), outline=(255, 235, 160, 255), width=2)
        if f[0] == "ehit":
            fx_, fy_ = f[3]; k_ = (rt - f[1]) // 80
            im = E["hit_behind"][min(k_, len(E["hit_behind"]) - 1)]; c.alpha_composite(im, (int(fx_ - im.width / 2), int(fy_ - im.height + 30)))
    # ---- units
    for u in sorted(U.values(), key=lambda u_: pos[u_["id"]][1]):
        x, y, moving = pos[u["id"]]
        im, key = frame_of(u, t)
        fleeing = u.get("flee") and t >= u["flee"]
        if im is None or fleeing:
            if moving: im = u["clips"]["run"][int(t // 90) % len(u["clips"]["run"])]
            else: im = u["clips"]["idle"][int(t // 100) % len(u["clips"]["idle"])]
            if fleeing: im = ImageOps.mirror(im)
        dx = 0
        for f in act_fx:
            if f[0] == "flash" and f[3] == u["id"]: im = tint(im, (255, 255, 255), 1.0)
            if f[0] == "tint" and f[3] == u["id"] and ((rt - f[1]) // 80) % 2 == 0: im = tint(im, (210, 30, 20), 0.22)
            if f[0] == "glow" and f[3] == u["id"]: im = tint(im, (120, 255, 140), 0.35 * (1 - (rt - f[1]) / f[2]))
            if f[0] == "shove" and f[3][0] == u["id"]:
                p = (rt - f[1]) / f[2]; dx += f[3][1] * (p / 0.25 if p < 0.25 else 1 - (p - 0.25) / 0.75)
            if f[0] == "dodge" and f[3][0] == u["id"]:
                p = (rt - f[1]) / f[2]; dx += f[3][1] * (min(1, p / 0.15) if p < 0.7 else (1 - p) / 0.3)
        if eclipse and u["chief"] and eclipse["start"] <= rt < (eclipse["release"] or 0):
            im = tint(im, (255, 70, 30), 0.18 + 0.14 * math.sin(rt / 70))
        ax, ay = feet(im)
        c.alpha_composite(im, (int(x + dx) - ax, int(y) - ay))
    # ---- over layer
    d = ImageDraw.Draw(c)
    for f in act_fx:
        if f[0] == "jaws" and f[3] in pos:
            x, y, _ = pos[f[3]]; k_ = min(8, (rt - f[1]) // 90)
            c.alpha_composite(JAWS[k_], (int(x) - 48, int(y) - 84))
        if f[0] == "bullet":
            a_id, v_id = f[3]
            if a_id in pos and v_id in pos:
                ax_, ay_, _ = pos[a_id]; vx, vy, _ = pos[v_id]; fa = U[a_id]["face"]
                sx, sy = ax_ + fa * 50, ay_ - 70; ex, ey = vx, vy - 64; p = (rt - f[1]) / f[2]
                hx, hy = sx + (ex - sx) * p, sy + (ey - sy) * p; L = math.hypot(ex - sx, ey - sy) or 1
                tx, ty = hx - (ex - sx) / L * 26, hy - (ey - sy) / L * 26
                d.line((tx, ty, hx, hy), fill=(255, 225, 140), width=3); d.line((tx, ty, hx, hy), fill=(255, 255, 240), width=1)
        if f[0] == "ehit":
            fx_, fy_ = f[3]; k_ = (rt - f[1]) // 80
            im = E["hit_front"][min(k_, len(E["hit_front"]) - 1)]; c.alpha_composite(im, (int(fx_ - im.width / 2), int(fy_ - im.height + 30)))
    if eclipse and eclipse.get("release"):
        for m in COMET:
            if m["launch"] <= rt < m["hit"]:
                kf = (rt - m["launch"]) // 80 + 1; tt = kf / m["falls"]
                sx, sy = eclipse["sun"]; ex, ey = m["feet"][0], m["feet"][1] - 2
                ox, oy = round(sx + (ex - sx) * tt), round(sy + (ey - sy) * tt)
                cf = E["comet_over"][m["slot"]]; c.alpha_composite(cf, (ox - 60, oy - 152))
    if eclipse:
        sx, sy = eclipse.get("sun", (HOME["clarth.skirmisher.3"][0] + 180, HOME["clarth.skirmisher.3"][1] - 380))
        ef = (rt - eclipse["start"]) // 80; rel = eclipse.get("release")
        im = None
        if rel is None or rt < rel:
            if 6 <= ef < len(E["sun_tell_over"]): im = E["sun_tell_over"][ef]
        elif rt < rel + 80: im = E["sun_dark_over"][0]
        elif rt < rel + 160: im = E["sun_flash_over"][0]
        elif rt < eclipse["last_hit"] + 80: im = E["sun_loop_over"][(ef - 8) % len(E["sun_loop_over"])]
        elif rt < eclipse["last_hit"] + 80 + len(E["sun_out_over"]) * 80: im = E["sun_out_over"][(rt - eclipse["last_hit"] - 80) // 80]
        if im is not None: paste_center(c, im, sx, sy)
    # numbers: one stack per unit, newest lowest, three at most
    stacks = {}
    for f in sorted([f for f in act_fx if f[0] == "num"], key=lambda f: f[1]):
        stacks.setdefault(f[3][0], []).append(f)
    for uid, lst in stacks.items():
        if uid not in pos: continue
        x, y, _ = pos[uid]; lst = lst[-3:]
        for j, f in enumerate(reversed(lst)):
            p = (rt - f[1]) / f[2]; rise = int(10 * min(1, p * 3))
            fnt = BIGF if f[3][3] else NUMF
            ty = max(BASE[1] + 6, y - 150 - rise - j * 20)
            d.text((x - 10, ty), f[3][1], font=fnt, fill=f[3][2], stroke_width=2, stroke_fill=(18, 8, 6))
    for f in act_fx:
        if f[0] == "word" and f[3][0] in pos:
            x, y, _ = pos[f[3][0]]
            d.text((x + 22, max(BASE[1] + 6, y - 132)), f[3][1], font=SMALL, fill=(255, 175, 70), stroke_width=2, stroke_fill=(18, 8, 6))
    # camera
    box = BASE
    if eclipse:
        e0 = eclipse["start"] - 300; e1 = eclipse.get("last_hit", eclipse["start"] + 3000) + 1500
        if e0 <= rt < e1:
            k_ = min(1, (rt - e0) / 400, (e1 - rt) / 500); k_ = max(0, k_); box = lerp_box(BASE, WIDE, k_)
    zoom = [f for f in act_fx if f[0] == "zoom"]
    if zoom:
        f = zoom[-1]; p = (rt - f[1]) / f[2]
        k_ = min(1, p / 0.12) if p < 0.75 else max(0, (1 - p) / 0.25)
        x, y, _ = pos[f[3]]
        w = 840 - (840 - 540) * k_; h = w * 472 / 840
        cx = 720 * (1 - k_) + x * k_; cy = 566 * (1 - k_) + (y - 60) * k_
        cx = max(w / 2, min(1440 - w / 2, cx)); cy = max(h / 2, min(900 - h / 2, cy))
        box = (cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2)
    for f in act_fx:
        if f[0] == "shake":
            m = f[3] if ((rt - f[1]) // 40) % 2 == 0 else -f[3]; box = (box[0] + m, box[1], box[2] + m, box[3])
    box = tuple(int(round(v)) for v in box)
    o = c.crop(box).resize((840, 472), Image.NEAREST)
    od = ImageDraw.Draw(o)
    for f in act_fx:
        if f[0] == "title":
            tw = od.textlength(f[3], font=TITLE); od.text(((840 - tw) / 2, 30), f[3], font=TITLE, fill=(255, 228, 170), stroke_width=3, stroke_fill=(30, 8, 6))
    od.text((8, 454), f"real fight, rules 41, seed 5100 fight 9   tick {max(0, (t - CHARGE) * TPS // 1000)}", font=SMALL, fill=(220, 205, 180), stroke_width=1, stroke_fill=(0, 0, 0))
    out_frames.append(o.convert("RGB"))

durs = [STEP] * len(out_frames)
os.makedirs(f"{A}/out3", exist_ok=True)
out_frames[0].save(f"{A}/out3/all-in-v3.webp", save_all=True, append_images=out_frames[1:], duration=durs, loop=0, quality=88, method=4)
# audit sheets: every 200 ms
for s in range(0, len(out_frames), 5 * 20):
    sh = Image.new("RGB", (420 * 4, 236 * 5)); dd = ImageDraw.Draw(sh)
    for j, f in enumerate(out_frames[s:s + 100:5]):
        x, y = (j % 4) * 420, (j // 4) * 236; sh.paste(f.resize((420, 236), Image.NEAREST), (x, y))
        dd.rectangle((x, y, x + 64, y + 14), fill=(0, 0, 0)); dd.text((x + 3, y + 1), f"{(s + j * 5) * STEP} ms", fill=(255, 255, 0))
    sh.save(f"{A}/out3/audit_{s // 100}.png")
print("frames", len(out_frames), "ms", END_MS, "webp MB", round(os.path.getsize(f"{A}/out3/all-in-v3.webp") / 1e6, 1))
