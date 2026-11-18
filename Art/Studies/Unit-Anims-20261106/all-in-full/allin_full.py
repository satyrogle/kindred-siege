"""All in fight, staged from a real rules 41 fight (seed 5100, fight 9, alpha hunt, FightDump2 -> dump55.txt).
Everyone fights at once on the sim's own ticks. Melee units walk to whoever they are hitting and get shoved back by hits,
every blow lands on a block or hit in its target, numbers and status words pop, Guard raises the twin rings, Snare
springs the bone jaws, the chief's Eclipse winds up and goes off, kills and the last kill get the big freeze.
Usage: python -I allin_full.py <work dir> <dump file>"""
import sys, zipfile, io, os, math, glob
from PIL import Image, ImageDraw, ImageFont

A, DUMP = sys.argv[1], sys.argv[2]
KS = "C:/Users/jacob/KindredSiege-Clean/"
R = KS + "Assets/StreamingAssets/CampaignArt/Bloodridge/"
bg = Image.open(R + "ground-underpaint.png").convert("RGBA")
for l in ["rear-burial-architecture.png", "rooted-entrance.png"]:
    bg.alpha_composite(Image.open(R + l).convert("RGBA"))
CM = KS + "Art/Studies/Class-Moves-20261106/"
JAWS = [Image.open(f"{CM}snare/v2-props/parts/jaws_anim/{i}.png").convert("RGBA").resize((72, 72), Image.NEAREST) for i in range(9)]
RISE = [Image.open(f"{CM}guard/parts/rise_anim/{i}.png").convert("RGBA") for i in range(9)]

TPS = 30; CHARGE = 1600; SEAM = 722
def ms(tick): return CHARGE + tick * 1000 // TPS

# ---------- clips ----------
ZIPS = {}
def zf(cid):
    if cid not in ZIPS: ZIPS[cid] = zipfile.ZipFile(f"{A}/zips/{cid}.zip")
    return ZIPS[cid]
def clip(cid, prefix, d):
    z = zf(cid); dd = "south-east" if d == "se" else "south-west"
    names = sorted({n.split("/animations/")[1].split("/")[0] for n in z.namelist() if "/animations/" in n and n.split("/animations/")[1].startswith(prefix)})
    names = [n for n in names if f"_{d}_" in n]
    assert names, (cid, prefix, d)
    nm = names[0]
    fr = sorted(n for n in z.namelist() if f"/animations/{nm}/{dd}/" in n)
    return [Image.open(io.BytesIO(z.read(x))).convert("RGBA") for x in fr]

def scale(frames, k):
    return frames if k == 1 else [f.resize((round(f.width * k), round(f.height * k)), Image.NEAREST) for f in frames]

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

def kit(cls, side, k=1.0):
    cid = CIDS[(cls, side)]; d = "se" if side == 0 else "sw"
    c = {key: scale(clip(cid, f"set_{cls}_{key}", d), k) for key in ["idle", "attack1", "attack2", "block", "hit", "death", "power"]}
    c["run"] = scale(clip(cid, f"allin_run_{d}", d), k)
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
        amount = int(nums[0]) if nums else 0
        detail = rest[-1] if rest and not rest[-1].lstrip("-").isdigit() and rest[-1] != target else ""
        events.append(dict(tick=tick, kind=kind, side=side, actor=actor, target=target, amount=amount, detail=detail))

# Placement: lanes on the burial court, ours facing south east (right), theirs south west (left).
HOME = {}
for uid, u in units.items():
    if u["role"] == "Pal": continue
    s, c = u["side"], u["cls"]
    chief = u["role"] == "Chief"
    if s == 0:
        HOME[uid] = {"skirmisher": (650, 470), "warden": (662, 572), "trapper": (648, 690), "marksman": (470, 515), "medic": (455, 640)}[c]
    else:
        HOME[uid] = (800, 600) if chief else {"skirmisher": (792, 470), "warden": (780, 545), "trapper": (790, 700), "marksman": (975, 515), "medic": (995, 640)}[c]
U = {}
for uid, h in HOME.items():
    u = units[uid]; chief = u["role"] == "Chief"
    U[uid] = dict(id=uid, side=u["side"], cls=u["cls"], chief=chief, face=1 if u["side"] == 0 else -1,
                  clips=kit(u["cls"], u["side"], 1.3 if chief else 1.0), home=h, x=float(h[0]), y=float(h[1]),
                  acts=[], dead_at=None, alt=0, melee=u["cls"] in ("warden", "skirmisher", "trapper"))

def timing(n, c, kind):
    if kind == "react": return [50, 50] + [80] * (n - 2)
    if kind == "death": return [60, 60] + [90] * (n - 3) + [10 ** 7]
    return [55 if i < c - 1 else 200 if i == c - 1 else 140 if i == c else 70 for i in range(n)]

FX = []      # (kind, start, length, data)
FREEZE = []  # (real start, length)
SLOW = []    # (start, length, factor)

def act(u, key, at, kind="attack"):
    fr = u["clips"][key]
    c = contact(fr, u["face"]) if kind == "attack" else 1
    d = timing(len(fr), c, kind)
    start = at - (sum(d[:c]) if kind == "attack" else 40)
    u["acts"].append((start, fr, d, key))
    return start

last_big = -10 ** 9
for e in events:
    t = ms(e["tick"]); a = U.get(e["actor"]); v = U.get(e["target"])
    k, det = e["kind"], e["detail"]
    if k in ("Hit", "Miss"):
        if a:
            power = det.startswith(("strike", "aimed", "flurry", "twincut", "rip"))
            key = "power" if power else ("attack1" if a["alt"] % 2 == 0 else "attack2"); a["alt"] += 1
            act(a, key, t)
            if a["melee"] and v: a.setdefault("goals", []).append((t - 500, e["target"]))
            if a["cls"] == "marksman" and v: FX.append(("tracer", t, 70, (a["id"], v["id"])))
        elif e["actor"] == "leader" and v:
            FX.append(("tracer", t, 70, ("leader", v["id"])))
        if v and v["dead_at"] is None:
            if k == "Miss":
                act(v, "block", t, "react"); FX.append(("word", t, 700, (v["id"], "MISS", (240, 230, 200))))
            else:
                if det != "trap": act(v, "hit", t, "react")
                big = e["amount"] >= 40
                FX += [("flash", t, 50, v["id"]), ("tint", t + 50, 170, v["id"]), ("shove", t, 220, (v["id"], -v["face"] * (18 if big else 9))),
                       ("num", t, 900, (v["id"], str(e["amount"]), (255, 70, 50) if not big else (255, 200, 60)))]
                if big and t - last_big > 900:
                    FREEZE.append((t, 90)); FX.append(("shake", t, 200, 3)); last_big = t
    elif k == "Heal" and a and v:
        act(a, "attack2", t)
        FX += [("num", t, 900, (v["id"], "+" + str(e["amount"]), (110, 240, 120))), ("glow", t, 260, v["id"])]
    elif k == "TrapArmed" and a:
        act(a, "power", t)
    elif k == "TrapSprung" and v:
        FX.append(("jaws", t - 80, 9 * 90 + 900, v["id"]))
    elif k == "Guard" and a and v:
        act(a, "power", t)
        FX += [("rise", t - 200, 9 * 90 + 700, v["id"]), ("word", t, 900, (a["id"], "GUARD", (250, 235, 200)))]
    elif k == "Mark" and v and det in ("bleed", "snare", "stasis", "exposed", "opening"):
        FX.append(("word", t + 120, 800, (v["id"], det.upper(), (255, 170, 60))))
    elif k == "Windup" and a and a["chief"]:
        dur = e["amount"] * 1000 // TPS
        a["acts"].append((t, a["clips"]["power"][:6] + [a["clips"]["power"][6]] * 1, [80] * 6 + [dur - 480], "windup"))
        FX += [("charge", t, dur, a["id"]), ("title", t, 1100, e["detail"].upper())]
    elif k == "Release" and a:
        FREEZE.append((t, 250)); FX += [("bigflash", t, 120, None), ("shake", t, 450, 6), ("title", t, 1000, e["detail"].upper())]
        for w in U.values():
            if w["side"] != a["side"] and w["dead_at"] is None and w["melee"]:
                FX.append(("shove", t, 500, (w["id"], -w["face"] * 34)))
    elif k == "Kill" and v:
        v["dead_at"] = t
        act(v, "death", t, "death")
        last = any(x["kind"] == "End" for x in events if x["tick"] == e["tick"])
        FREEZE.append((t, 450 if last else 260)); FX += [("zoom", t, 2600 if last else 1300, v["id"]), ("shake", t, 300, 5)]
        if last: SLOW.append((t + 450, 1200, 0.4)); FX.append(("title", t + 300, 2200, "BREAK"))

END_MS = ms(max(e["tick"] for e in events)) + 3200

# Melee goals: who each melee unit is walking to. A unit keeps its last goal until a new one.
for u in U.values():
    u["goals"] = sorted(u.get("goals", []))

def goal_xy(u, t):
    if u["dead_at"] is not None and t >= u["dead_at"]: return None
    tg = None
    for gt, tid in u["goals"]:
        if gt <= t and (U[tid]["dead_at"] is None or U[tid]["dead_at"] > gt): tg = tid
    if not u["melee"] or tg is None: return u["home"]
    v = U[tg]
    # The two front lines meet at a fixed seam; a unit steps up to the seam and leans toward its target's lane.
    half = 46 + (16 if v["chief"] or u["chief"] else 0)
    gy = u["home"][1] * 0.5 + v["y"] * 0.5
    gy = max(u["home"][1] - 40, min(u["home"][1] + 40, gy))
    return (SEAM - u["face"] * half, gy)

# ---------- simulate positions (real time, 20 ms steps) ----------
STEP = 40
def anim_time(rt):
    fz = sum(min(l, max(0, rt - s)) for s, l in FREEZE)
    t = rt - fz
    for s, l, f in SLOW:
        if t > s: t -= min(l, t - s) * (1 - f)
    return t

positions = {}  # frame index -> {uid: (x, y, moving)}
for u in U.values():
    u["x"], u["y"] = u["home"][0] - u["face"] * (300 if u["melee"] else 170), u["home"][1]
frames_t = list(range(0, END_MS, STEP))
for i, rt in enumerate(frames_t):
    t = anim_time(rt); snap = {}
    for u in U.values():
        moving = False
        if t < CHARGE:
            p = min(1, t / CHARGE); ease = 1 - (1 - p) ** 2
            sx = u["home"][0] - u["face"] * (300 if u["melee"] else 170)
            u["x"] = sx + (u["home"][0] - sx) * ease; moving = p < 1
        else:
            g = goal_xy(u, t)
            if g:
                dx, dy = g[0] - u["x"], g[1] - u["y"]; dist = math.hypot(dx, dy)
                sp = 170 * STEP / 1000
                if dist > 6:
                    u["x"] += dx / dist * min(sp, dist); u["y"] += dy / dist * min(sp, dist); moving = dist > 14
        snap[u["id"]] = [u["x"], u["y"], moving]
    # allies never stack: push apart living units of one side that stand too close
    alive = [u for u in U.values() if u["dead_at"] is None or t < u["dead_at"]]
    for a_ in alive:
        for b_ in alive:
            if a_["id"] >= b_["id"] or a_["side"] != b_["side"]: continue
            dx, dy = b_["x"] - a_["x"], b_["y"] - a_["y"]; dist = math.hypot(dx, dy) or 1
            if dist < 88:
                push = (88 - dist) / 2
                a_["y"] -= dy / dist * push if dy else push; b_["y"] += dy / dist * push if dy else push
                a_["x"] -= dx / dist * push * 0.3; b_["x"] += dx / dist * push * 0.3
                snap[a_["id"]][:2] = [a_["x"], a_["y"]]; snap[b_["id"]][:2] = [b_["x"], b_["y"]]
    positions[i] = {k: tuple(v) for k, v in snap.items()}

# ---------- render ----------
try:
    FONT = ImageFont.truetype("C:/Windows/Fonts/impact.ttf", 20); SMALL = ImageFont.truetype("C:/Windows/Fonts/impact.ttf", 13)
    TITLE = ImageFont.truetype("C:/Windows/Fonts/impact.ttf", 40)
except Exception:
    FONT = SMALL = TITLE = ImageFont.load_default()

def tint(im, col, a):
    al = im.split()[3]; out = Image.blend(im, Image.new("RGBA", im.size, col + (255,)), a); out.putalpha(al); return out

def frame_of(u, t):
    cur = None
    for s, fr, d, key in u["acts"]:
        if s <= t: cur = (s, fr, d, key)
    if cur:
        s, fr, d, key = cur; lt = t - s; acc = 0
        for i, dd in enumerate(d):
            acc += dd
            if lt < acc: return fr[min(i, len(fr) - 1)], key
    if u["dead_at"] is not None and t >= u["dead_at"]:
        return u["clips"]["death"][-1], "dead"
    return None, None

BASE = (300, 330, 1140, 802)  # 840 x 472
out_frames = []
for i, rt in enumerate(frames_t):
    t = anim_time(rt)
    c = bg.copy()
    act_fx = [f for f in FX if f[1] <= rt < f[1] + f[2]]
    for f in act_fx:  # ground pieces under everyone
        if f[0] == "rise" and f[3] in positions[i]:
            x, y, _ = positions[i][f[3]]; k = min(8, (rt - f[1]) // 90); im = RISE[k]
            c.alpha_composite(im, (int(x) - 64, int(y) - 108))
    shade = any(f[0] in ("charge",) for f in act_fx)
    order = sorted(U.values(), key=lambda u: positions[i][u["id"]][1])
    for u in order:
        x, y, moving = positions[i][u["id"]]
        im, key = frame_of(u, t)
        if im is None:
            if moving: im = u["clips"]["run"][int(t // 90) % len(u["clips"]["run"])]
            else: im = u["clips"]["idle"][int(t // 100) % len(u["clips"]["idle"])]
        dx = 0
        for f in act_fx:
            if f[0] == "flash" and f[3] == u["id"]: im = tint(im, (255, 255, 255), 1.0)
            if f[0] == "tint" and f[3] == u["id"] and ((rt - f[1]) // 80) % 2 == 0: im = tint(im, (210, 30, 20), 0.45)
            if f[0] == "glow" and f[3] == u["id"]: im = tint(im, (120, 255, 140), 0.35 * (1 - (rt - f[1]) / f[2]))
            if f[0] == "charge" and f[3] == u["id"]: im = tint(im, (255, 60, 30), 0.25 + 0.2 * math.sin(rt / 70))
            if f[0] == "shove" and f[3][0] == u["id"]:
                p = (rt - f[1]) / f[2]; dx += f[3][1] * (p / 0.25 if p < 0.25 else 1 - (p - 0.25) / 0.75 * 0.6)
        ax, ay = feet(im)
        c.alpha_composite(im, (int(x + dx) - ax, int(y) - ay))
    d = ImageDraw.Draw(c)
    for f in act_fx:
        p = (rt - f[1]) / f[2]
        if f[0] == "jaws" and f[3] in positions[i]:
            x, y, _ = positions[i][f[3]]; k = min(8, (rt - f[1]) // 90)
            c.alpha_composite(JAWS[k], (int(x) - 36, int(y) - 62))
        if f[0] == "tracer":
            a_id, v_id = f[3]
            if v_id in positions[i]:
                vx, vy, _ = positions[i][v_id]
                if a_id == "leader": sx, sy = 300, 420
                else:
                    ax_, ay_, _ = positions[i][a_id]; fa = U[a_id]["face"]; sx, sy = ax_ + fa * 46, ay_ - 62
                d.line((sx, sy, vx, vy - 60), fill=(255, 240, 170), width=3); d.line((sx, sy, vx, vy - 60), fill=(255, 255, 255), width=1)
        if f[0] in ("num", "word") and f[3][0] in positions[i]:
            x, y, _ = positions[i][f[3][0]]
            rise = int(34 * min(1, p * 2.2))
            fnt = FONT if f[0] == "num" else SMALL
            off = -12 if f[0] == "num" else 18
            d.text((x - 14 + off, y - 150 - rise - (0 if f[0] == "num" else 14)), f[3][1], font=fnt, fill=f[3][2], stroke_width=2, stroke_fill=(18, 8, 6))
    if any(f[0] == "bigflash" for f in act_fx):
        c = Image.blend(c, Image.new("RGBA", c.size, (255, 235, 210, 255)), 0.55)
    box = BASE
    zoom = [f for f in act_fx if f[0] == "zoom"]
    if zoom:
        f = zoom[-1]; p = (rt - f[1]) / f[2]
        k = min(1, p / 0.12) if p < 0.75 else max(0, (1 - p) / 0.25)
        x, y, _ = positions[i][f[3]]
        w = 840 - (840 - 520) * k; h = w * 472 / 840
        cx = 720 * (1 - k) + x * k; cy = 566 * (1 - k) + (y - 70) * k
        cx = max(w / 2, min(1440 - w / 2, cx)); cy = max(h / 2, min(900 - h / 2, cy))
        box = (int(cx - w / 2), int(cy - h / 2), int(cx - w / 2) + int(w), int(cy - h / 2) + int(h))
    for f in act_fx:
        if f[0] == "shake":
            m = f[3] if ((rt - f[1]) // 40) % 2 == 0 else -f[3]; box = (box[0] + m, box[1], box[2] + m, box[3])
    o = c.crop(box).resize((840, 472), Image.NEAREST)
    od = ImageDraw.Draw(o)
    for f in act_fx:
        if f[0] == "title":
            tw = od.textlength(f[3], font=TITLE); od.text(((840 - tw) / 2, 34), f[3], font=TITLE, fill=(255, 228, 170), stroke_width=3, stroke_fill=(30, 8, 6))
    od.text((8, 452), f"real fight, rules 41, seed 5100 fight 9   tick {max(0, (t - CHARGE) * TPS // 1000)}", font=SMALL, fill=(220, 205, 180), stroke_width=1, stroke_fill=(0, 0, 0))
    out_frames.append(o.convert("RGB"))

outf, durs = [], []
for f in out_frames:
    if outf and f.tobytes() == outf[-1].tobytes(): durs[-1] += STEP
    else: outf.append(f); durs.append(STEP)
os.makedirs(f"{A}/out", exist_ok=True)
_samp = Image.new("RGB", (840, 472 * 8))
for _j, _f in enumerate(outf[:: max(1, len(outf) // 8)][:8]): _samp.paste(_f, (0, 472 * _j))
_gp = _samp.quantize(colors=200, method=Image.Quantize.MEDIANCUT)
pal = [f.quantize(palette=_gp, dither=Image.Dither.NONE) for f in outf]
pal[0].save(f"{A}/out/all-in-full.gif", save_all=True, append_images=pal[1:], duration=durs, loop=0, disposal=2)
# stills at chosen real times
want = [int(a) for a in sys.argv[3].split(",")] if len(sys.argv) > 3 else [1000, 2600, 4500, 6600, 8200, 11000, 13200]
acc = 0; picks = []
for f, d in zip(outf, durs):
    for w in want:
        if acc <= w < acc + d: picks.append((w, f))
    acc += d
sheet = Image.new("RGB", (840 * 2, 472 * ((len(picks) + 1) // 2)))
for j, (w, f) in enumerate(picks):
    sheet.paste(f, ((j % 2) * 840, (j // 2) * 472))
    ImageDraw.Draw(sheet).text(((j % 2) * 840 + 8, (j // 2) * 472 + 6), f"{w} ms", fill=(255, 255, 0))
sheet.save(f"{A}/out/stills.png")
print("frames", len(outf), "length ms", END_MS, "gif MB", round(os.path.getsize(f"{A}/out/all-in-full.gif") / 1e6, 1))
