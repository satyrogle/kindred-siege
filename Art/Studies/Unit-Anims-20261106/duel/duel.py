"""Warden against their Skirmisher, staged from a real rules 41 fight (tutorial, seed 5, FightDump).
Two renders from the same beats: PLAIN (every frame 100 ms, nothing else) and FULL (snap and hold timing,
hit stop on contact, white flash and red tint on the victim, jitter, knockback, rising numbers, a power move
freeze with the screen dimmed and the camera pushed in, a long finisher freeze on the kill).
Usage: python -I duel.py <duel dir>"""
import sys, zipfile, io, os
from PIL import Image, ImageDraw, ImageFont

A = sys.argv[1]
R = "C:/Users/jacob/KindredSiege-Clean/Assets/StreamingAssets/CampaignArt/Bloodridge/"
bg = Image.open(R + "ground-underpaint.png").convert("RGBA")
for l in ["rear-burial-architecture.png", "rooted-entrance.png"]:
    bg.alpha_composite(Image.open(R + l).convert("RGBA"))

W_ID, S_ID = "d917de88-8dfc-4de2-97f0-4dc46e06cb7d", "a1195c8f-03c0-42f8-8f8f-135b8e98d09c"

def clip(cid, name, d):
    z = zipfile.ZipFile(f"{A}/zips/{cid}.zip")
    n = sorted(x for x in z.namelist() if f"/animations/{name}/{d}/" in x)
    assert n, name
    return [Image.open(io.BytesIO(z.read(x))).convert("RGBA") for x in n]

def feet(im):
    b = im.split()[3].point(lambda v: 255 if v > 20 else 0).getbbox()
    return ((b[0] + b[2]) // 2, b[3])

def contact(frames, facing):
    """the frame reaching furthest toward the enemy"""
    best, bi = None, len(frames) // 2
    for i, f in enumerate(frames[1:-2], 1):
        b = f.split()[3].point(lambda v: 255 if v > 20 else 0).getbbox()
        if not b: continue
        reach = b[2] if facing > 0 else -b[0]
        if best is None or reach > best: best, bi = reach, i
    return bi

def load(cid, d, unit):
    c = {k: clip(cid, f"set_{unit}_{k}_{d}_20261106", "south-east" if d == "se" else "south-west")
         for k in (["idle", "attack1_bash", "attack2_slash", "block", "hit", "death", "power_guard"] if unit == "warden"
                   else ["idle", "attack1_hook", "attack2_chop", "block", "hit", "death", "power_strike"])}
    return c

WC = load(W_ID, "se", "warden"); SC = load(S_ID, "sw", "skirmisher")
UNITS = {"W": dict(clips=WC, x=664, y=610, face=1), "S": dict(clips=SC, x=764, y=610, face=-1)}
for u in UNITS.values():
    u["anchor"] = feet(u["clips"]["idle"][0])

# Beats in the fight's own order. (sim tick, kind, attacker, attack clip, victim, reaction, damage, caption)
BEATS = [
    (92, "power", "W", "power_guard", None, None, 0, "tick 92  WARDEN GUARDS THE MEDIC"),
    (92, "hit", "S", "attack1_hook", "W", "block", 6, "tick 92  Skirmisher hits the Warden (guarded)"),
    (122, "hit", "S", "attack2_chop", "W", "block", 5, "tick 122  Skirmisher hits the Warden (guarded)"),
    (239, "miss", "W", "attack2_slash", "S", "block", 0, "tick 239  Warden swings, misses"),
    (242, "hit", "S", "attack1_hook", "W", "block", 7, "tick 242  Skirmisher hits the Warden (guarded)"),
    (275, "hit", "W", "attack1_bash", "S", "hit", 14, "tick 275  Warden lands a bash"),
    (302, "hit", "S", "attack2_chop", "W", "block", 7, "tick 302  Skirmisher hits the Warden (guarded)"),
    (311, "hit", "W", "attack2_slash", "S", "hit", 10, "tick 311  Warden lands a slash"),
    (332, "kill", None, None, "S", "death", 75, "tick 332  our Skirmisher's combo kills him (off screen)"),
]

def timing(n, c, full, kind):
    if not full: return [100] * n
    if kind == "death": return [60, 60] + [90] * (n - 3) + [100]
    if kind == "react": return [50, 50] + [80] * (n - 2)
    t = []
    for i in range(n):
        t.append(60 if i < c - 1 else 240 if i == c - 1 else 170 if i == c else 70)
    return t

def build(full):
    """returns per unit action lists [(anim_start, frames, durations)], freezes [(real_start, len)], fx list"""
    acts = {"W": [], "S": []}; fx = []; freezes = []
    t = 400  # anim clock in ms
    def real(at):
        return at + sum(l for s, l in freezes if s <= at + sum(0 for _ in [0]))
    for tick, kind, att, aclip, vic, react, dmg, cap in BEATS:
        if kind == "power":
            fr = UNITS[att]["clips"][aclip]; d = timing(len(fr), contact(fr, UNITS[att]["face"]), full, "attack")
            if full:
                rs = t + sum(l for s, l in freezes)  # real time the move starts
                freezes.append((rs, 500)); fx.append(("dim", rs, 500 + sum(d))); fx.append(("zoom", rs, 500 + sum(d), att))
                fx.append(("title", rs, 500 + sum(d), "GUARD"))
            acts[att].append((t, fr, d)); fx.append(("cap", t + sum(l for s, l in freezes) - (500 if full else 0), sum(d) + 500, cap)); t += sum(d) + (0 if full else 0); continue
        if kind == "kill":
            fr = UNITS[vic]["clips"]["death"]; d = timing(len(fr), 1, full, "death")
            ct = t + 120
            if full:
                rs = ct + sum(l for s, l in freezes)
                fx += [("flash", rs, 60, vic), ("num", rs, 900, vic, dmg), ("zoom", rs, 2600, vic), ("shake", rs, 300)]
                freezes.append((rs, 450))
            acts[vic].append((t, fr, d + [100000])); fx.append(("cap", t + sum(l for s, l in freezes), 3000, cap)); t += sum(d) + 1500; continue
        fr = UNITS[att]["clips"][aclip]; c = contact(fr, UNITS[att]["face"]); d = timing(len(fr), c, full, "attack")
        ct = t + sum(d[:c])  # anim time of contact
        acts[att].append((t, fr, d))
        rf = UNITS[vic]["clips"][react]; rd = timing(len(rf), 1, full, "react")
        acts[vic].append((ct - (rd[0] if full else 100), rf, rd))
        if full:
            rs = ct + sum(l for s, l in freezes)
            hs = 0 if kind == "miss" else (120 if dmg >= 10 else 80)
            if hs: freezes.append((rs, hs))
            if kind == "hit":
                fx += [("flash", rs, 50, vic), ("tint", rs + 50, 160, vic), ("jit", rs, hs, vic),
                       ("knock", rs, hs + 120, vic, UNITS[att]["face"]), ("num", rs, 900, vic, dmg)]
            else:
                fx += [("num", rs, 700, vic, "MISS")]
        fx.append(("cap", t + sum(l for s, l in freezes), sum(d), cap))
        t += sum(d) - (150 if full else 0)
    end = t + 800
    return acts, freezes, fx, end

def frame_at(acts_u, anim_t, idle):
    cur = None
    for s, fr, d in acts_u:
        if s <= anim_t: cur = (s, fr, d)
    if cur:
        s, fr, d = cur; lt = anim_t - s
        acc = 0
        for i, dd in enumerate(d):
            acc += dd
            if lt < acc: return fr[min(i, len(fr) - 1)]
    k = int(anim_t // 100) % len(idle)
    return idle[k]

try:
    FONT = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 13)
    BIG = ImageFont.truetype("C:/Windows/Fonts/impact.ttf", 22)
except Exception:
    FONT = BIG = ImageFont.load_default()

def tint(im, col, a):
    al = im.split()[3]
    solid = Image.new("RGBA", im.size, col + (255,))
    out = Image.blend(im, solid, a)
    out.putalpha(al)
    return out

def render(full, name):
    acts, freezes, fx, end_anim = build(full)
    total_freeze = sum(l for s, l in freezes)
    STEP = 20
    real_end = end_anim + total_freeze
    BOX = (432, 380, 992, 695)  # 560 x 315
    frames = []
    for rt in range(0, real_end, STEP):
        # anim clock = real time minus frozen time so far
        frozen = sum(min(l, max(0, rt - s)) for s, l in freezes)
        at = rt - frozen
        c = bg.copy()
        active = [f for f in fx if f[1] <= rt < f[1] + f[2]]
        dim = any(f[0] == "dim" for f in active)
        if dim:
            c = Image.blend(c, Image.new("RGBA", c.size, (10, 6, 8, 255)), 0.6)
        for key in sorted(UNITS, key=lambda k: UNITS[k]["y"]):
            u = UNITS[key]
            im = frame_at(acts[key], at, u["clips"]["idle"])
            dx = 0
            for f in active:
                if len(f) > 3 and f[3] == key:
                    if f[0] == "flash": im = tint(im, (255, 255, 255), 1.0)
                    if f[0] == "tint" and ((rt - f[1]) // 80) % 2 == 0: im = tint(im, (200, 30, 20), 0.45)
                    if f[0] == "jit": dx += 1 if ((rt - f[1]) // 40) % 2 == 0 else -1
                    if f[0] == "knock":
                        p = (rt - f[1]) / f[2]; dx += round(f[4] * 8 * (1 - p) if p > 0.3 else f[4] * 8 * p / 0.3)
            ax, ay = feet(im)
            c.alpha_composite(im, (u["x"] - ax + dx, u["y"] - ay))
        d = ImageDraw.Draw(c)
        for f in active:
            if f[0] == "num":
                u = UNITS[f[3]]; p = (rt - f[1]) / f[2]
                y = u["y"] - 150 - int(30 * min(1, p * 2))
                txt = str(f[4]); col = (255, 240, 200) if txt == "MISS" else (255, 70, 50)
                d.text((u["x"] - 12, y), txt, font=BIG, fill=col, stroke_width=3, stroke_fill=(20, 8, 6))
            if f[0] == "title":
                d.text((UNITS[f[3] and "W"]["x"] - 40, UNITS["W"]["y"] - 170), f[3], font=BIG, fill=(255, 230, 170), stroke_width=3, stroke_fill=(30, 10, 6))
        cap = [f for f in active if f[0] == "cap"]
        box = BOX
        zoom = [f for f in active if f[0] == "zoom"]
        if zoom:
            f = zoom[-1]; p = (rt - f[1]) / f[2]
            k = min(1, p / 0.15) if p < 0.8 else max(0, (1 - p) / 0.2)
            u = UNITS[f[3]]; cx, cy = u["x"] + 60 * -u["face"] * 0, u["y"] - 90
            w = 560 - (560 - 360) * k; h = w * 315 / 560
            ox = (BOX[0] + BOX[2]) / 2 * (1 - k) + cx * k; oy = (BOX[1] + BOX[3]) / 2 * (1 - k) + cy * k
            box = (int(ox - w / 2), int(oy - h / 2), int(ox - w / 2) + int(w), int(oy - h / 2) + int(h))
        sh = [f for f in active if f[0] == "shake"]
        if sh:
            m = 4 if ((rt - sh[0][1]) // 40) % 2 == 0 else -4
            box = (box[0] + m, box[1], box[2] + m, box[3])
        out = c.crop(box).resize((1120, 630), Image.NEAREST)
        od = ImageDraw.Draw(out)
        if cap:
            od.rectangle((0, 600, 1120, 630), fill=(14, 9, 10))
            od.text((12, 607), cap[-1][3] + ("" if full else "   [plain]"), font=FONT, fill=(230, 215, 190))
        frames.append(out.convert("RGB"))
    # merge identical neighbours into longer frames
    outf, durs = [], []
    for f in frames:
        if outf and f.tobytes() == outf[-1].tobytes(): durs[-1] += STEP
        else: outf.append(f); durs.append(STEP)
    small = [f.resize((560, 315), Image.NEAREST) for f in outf]
    pal = [f.convert("P", palette=Image.ADAPTIVE, colors=255) for f in small]
    os.makedirs(f"{A}/out", exist_ok=True)
    pal[0].save(f"{A}/out/{name}.gif", save_all=True, append_images=pal[1:], duration=durs, loop=0, disposal=2)
    return outf, durs, real_end, fx

pf, pd, plen, _ = render(False, "duel-plain")
ff, fd, flen, ffx = render(True, "duel-full")
print("plain", len(pf), "frames", plen, "ms   full", len(ff), "frames", flen, "ms")
# stills of the full one: the guard, and just after three contacts and the kill
acc = 0; marks = {}
nums = sorted(f[1] for f in ffx if f[0] == "num")
want = [900, nums[0] + 160, nums[2] + 160, nums[4] + 30, nums[4] + 200, nums[-1] + 900]
for f, d in zip(ff, fd):
    for w in want:
        if acc <= w < acc + d: marks[w] = f
    acc += d
sheet = Image.new("RGB", (1120, 630 * len(marks)))
for i, w in enumerate(sorted(marks)): sheet.paste(marks[w], (0, 630 * i))
sheet.save(f"{A}/out/full-stills.png")
