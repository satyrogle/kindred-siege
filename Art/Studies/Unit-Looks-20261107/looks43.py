"""Three looks for the marks, redone as a 2x close up. 7 Nov 2026, after my own audit of v1 and Jacob's "Yes, redo it".

The same fight, hit stack, words and staging as looks42.py (imported, not copied). What changed from v1:
  - two fixed close up shots at 2x, so every marked fighter is whole in frame: shot 1 the top lanes (our Warden and
    Skirmisher against T.1, the captain and T.4), shot 2 the back line (T.2, the captain, T.0)
  - every mark is PixelLab art (v2-marks/README.txt): the floor marks and the body look's bleed drops and claw marks from
    the VFX workbench, the overhead icons from create_image_pro styled on the game's own order icons, Focus Fire's
    overhead icon is the game's icon_focus, Guard and the snare use the picked twin rings, spike ring and bone jaws.
    Outlines, the gold statue remap, the blue rim and the panic flicker stay sprite shaders, as the game would do them
  - the game's colours at full strength; a Marksman's aim in the side colour, as the game's AIM text does (mint for
    ours, red for theirs)
  - the same information in all three looks: who guards whom and who aims at whom is a link in each look's own layer,
    feet to feet on the floor, head to head over the icons, chest to chest on the body
Usage: python -I looks43.py <PixelLab zips dir> <out dir>"""
import sys, os, json, math, runpy
from PIL import Image, ImageDraw, ImageChops, ImageFilter

HERE = "C:/Users/jacob/KindredSiege-Clean/Art/Studies/Unit-Looks-20261107/"
ZIPS, OUT = sys.argv[1], sys.argv[2]
sys.argv = [HERE + "looks42.py", ZIPS, OUT]
G = runpy.run_path(HERE + "looks42.py", run_name="lib")["render"].__globals__   # looks42's live module globals

U, FX, positions, frames_t = G["U"], G["FX"], G["positions"], G["frames_t"]
marks_at, ms, tick_at, frame_of = G["marks_at"], G["ms"], G["tick_at"], G["frame_of"]
OX, OY, BG, PX, NIGHT, FLIP = G["OX"], G["OY"], G["BG"], G["PX"], G["NIGHT"], G["FLIP"]
solid, tint, flat, dashed = G["solid"], G["tint"], G["flat"], G["dashed"]
RISE, JAWS, SNARE_RING = G["RISE"], G["JAWS"], G["SNARE_RING"]
END_MS, end_t, LAST, STEP = G["END_MS"], G["end_t"], G["LAST"], G["STEP"]
SMALL, LABEL, TITLE = G["SMALL"], G["LABEL"], G["TITLE"]

# ------------------------------------------------------------------ colours: the game's own, at full strength
COL = dict(guard=PX["mint"], focus=PX["red"], bleed=PX["red"], open=PX["orange"], exposed=PX["blue"],
           stasis=PX["yellow"], panic=PX["lilac"], snare=PX["lilac"])
AIMCOL = {0: PX["mint"], 1: PX["red"]}
RED4 = [(0x2E, 0x12, 0x0D), (0x5E, 0x24, 0x19), (0x96, 0x40, 0x2C), (0xDA, 0x6B, 0x51)]
MINT4 = [(0x1F, 0x29, 0x24), (0x4C, 0x5F, 0x56), (0x76, 0x90, 0x7F), (0x99, 0xBE, 0xAC)]
GOLD4 = [(0x3D, 0x2D, 0x14), (0x7A, 0x5A, 0x28), (0xD6, 0xAA, 0x5C), (0xF2, 0xD2, 0x7C)]

# ------------------------------------------------------------------ the PixelLab marks
MK = HERE + "v2-marks/"
def pack(name):
    m = json.load(open(f"{MK}{name}/manifest.json")); out = dict(ms=m["frame_ms"], n=m["frames"], loop=m["loop"])
    for c in m["clips"]:
        if c["name"] not in ("behind", "front"): continue
        fr = []; w, h = c["frame"]["w"], c["frame"]["h"]
        for sh in c["sheets"]:
            s = Image.open(f"{MK}{name}/{sh['file']}").convert("RGBA"); cols = sh["columns"]
            fr += [s.crop(((k % cols) * w, (k // cols) * h, (k % cols) * w + w, (k // cols) * h + h)) for k in range(sh["frames"])]
        out[c["name"]] = dict(frames=fr, off=(c["offset"]["x"], c["offset"]["y"]), start=c.get("start_frame", 0))
    return out
FLOOR = {k: pack(k + "_floor") for k in ("focus", "aim", "open", "exposed", "stasis", "panic", "bleed")}
BODY = {k: pack(k + "_body") for k in ("bleed", "open")}
BUILT_ON = 125                                         # body height of the Warden the VFX marks were built around

def recolour(im, src, dst):
    """Nearest colour of one ramp onto the same step of another (the aim marks in our mint)."""
    out = im.copy(); po = out.load(); w, h = im.size
    for yy in range(h):
        for xx in range(w):
            r, g, b, a = po[xx, yy]
            if a == 0: continue
            n = min(range(len(src)), key=lambda k: (src[k][0] - r) ** 2 + (src[k][1] - g) ** 2 + (src[k][2] - b) ** 2)
            po[xx, yy] = dst[n] + (a,)
    return out

def trim(im): return im.crop(im.getchannel("A").getbbox())
ICON = {k: trim(Image.open(f"{MK}icons/{k}.png").convert("RGBA")) for k in ("guard", "aim", "bleed", "snare", "open", "exposed", "stasis", "panic")}
ICON["focus"] = trim(G["FOCUS_ICON"])
ICON_RED = [(0x96, 0x40, 0x2C), (0xDA, 0x6B, 0x51), (0xEC, 0xB5, 0xA8)]
ICON_MINT = [(0x4C, 0x5F, 0x56), (0x99, 0xBE, 0xAC), (0xCC, 0xDE, 0xD5)]
def swap_exact(im, src, dst):
    out = im.copy(); po = out.load()
    for yy in range(im.height):
        for xx in range(im.width):
            p = po[xx, yy]
            if p[3] and p[:3] in src: po[xx, yy] = dst[src.index(p[:3])] + (p[3],)
    return out
AIM_ICON = {1: ICON["aim"], 0: swap_exact(ICON["aim"], ICON_RED, ICON_MINT)}

# ------------------------------------------------------------------ when each mark started, for one shot clips
RUNS, prev = {}, set()
for t in frames_t:
    now = {(u, k) for u, m in marks_at(t).items() for k in m}
    for key in now - prev: RUNS.setdefault(key, []).append([t, None])
    for key in prev - now: RUNS[key][-1][1] = t
    prev = now
def since(uid, kind, t):
    for s, e in RUNS.get((uid, kind), []):
        if s <= t and (e is None or t < e): return t - s
    return 0

def put(c, pk, piece, i, X, Y, mirror=False, dy=0):
    cl = pk.get(piece)
    if not cl: return
    j = i - cl["start"]
    if not 0 <= j < len(cl["frames"]): return
    im = cl["frames"][j]; ox, oy = cl["off"]
    if mirror: im = im.transpose(FLIP); ox = -(ox + im.width - 1)
    c.alpha_composite(im, (int(X + ox) - OX, int(Y + oy + dy) - OY))

def loop_i(k, t): pk = FLOOR[k]; return (t // pk["ms"]) % pk["n"]
AIMCACHE = {}
def aim_frame(p, side):
    i = min(25, int(p * 26)); key = (i, side)
    if key not in AIMCACHE:
        im = FLOOR["aim"]["behind"]["frames"][i - FLOOR["aim"]["behind"]["start"]]
        AIMCACHE[key] = im if side == 1 else recolour(im, RED4, MINT4)
    return AIMCACHE[key]

def line(c, p0, p1, col, t, upto=1.0):
    d = ImageDraw.Draw(c, "RGBA")
    dashed(d, (p0[0] - OX, p0[1] - OY), (p1[0] - OX, p1[1] - OY), col, phase=t // 60, upto=upto, dim=tuple(v // 2 for v in col))

def links(c, t, pos, mk, at):
    """Who guards whom and who aims at whom, drawn in the look's own layer: at(uid) gives the point on that fighter."""
    for uid, m in mk.items():
        if uid not in pos: continue
        if "guard" in m and m["guard"] in pos: line(c, at(m["guard"]), at(uid), COL["guard"], t)
        if "aim" in m and m["aim"][0] in pos:
            sh, p = m["aim"]; line(c, at(sh), at(uid), AIMCOL[U[sh]["side"]], t, upto=p)

# ------------------------------------------------------------------ look 1: the floor
def floor_under(c, t, pos, mk):
    links(c, t, pos, mk, lambda uid: (pos[uid][0], pos[uid][1] + 2))
    for uid, m in sorted(mk.items(), key=lambda kv: pos[kv[0]][1] if kv[0] in pos else 0):
        if uid not in pos: continue
        X, Y = int(pos[uid][0]), int(pos[uid][1])
        for k in ("bleed", "exposed", "panic", "stasis"):
            if k in m: put(c, FLOOR[k], "behind", loop_i(k, t), X, Y)
        if "open" in m:
            s = since(uid, "open", t); i = s // 83 if s < 4 * 83 else [3, 4, 5, 4][((s - 4 * 83) // 140) % 4]
            put(c, FLOOR["open"], "behind", i, X, Y)
        if "guard" in m: c.alpha_composite(RISE[4], (X - 64 - OX, Y - 108 - OY))
        if "focus" in m: put(c, FLOOR["focus"], "behind", loop_i("focus", t), X, Y)
        if "aim" in m:
            sh, p = m["aim"]; cl = FLOOR["aim"]["behind"]
            c.alpha_composite(aim_frame(p, U[sh]["side"]), (X + cl["off"][0] - OX, Y + cl["off"][1] - OY))
        if "snare" in m: c.alpha_composite(SNARE_RING, (X - 65 - OX, Y - 96 - OY))

def floor_over(mk):
    def post(c, u, t, X, Y):
        m = mk.get(u["id"])
        if not m: return
        for k in ("bleed", "panic", "stasis"):
            if k in m: put(c, FLOOR[k], "front", loop_i(k, t), X, Y)
    return post

# ------------------------------------------------------------------ look 2: over the head
ORDER = ("guard", "focus", "aim", "snare", "bleed", "open", "exposed", "stasis", "panic")
def head(uid, pos): return (pos[uid][0], pos[uid][1] - U[uid]["height"] - 18)
def overhead(c, t, pos, mk):
    links(c, t, pos, mk, lambda uid: head(uid, pos))
    for uid, m in mk.items():
        if uid not in pos: continue
        icons = []
        for k in ORDER:
            if k not in m: continue
            if k == "aim":
                sh, p = m["aim"]; period = max(60, int(320 - 240 * p))
                if (t // period) % 2 == 0: icons.append(AIM_ICON[U[sh]["side"]])
                else: icons.append(None)           # keeps its slot while it blinks off
            else: icons.append(ICON[k])
        if not icons: continue
        widths = [ICON["aim"].width if im is None else im.width for im in icons]
        x0 = int(pos[uid][0] - (sum(widths) + 2 * (len(icons) - 1)) / 2); base = int(pos[uid][1] - U[uid]["height"] - 4)
        for im, w in zip(icons, widths):
            if im is not None: c.alpha_composite(im, (x0 - OX, base - im.height - OY))
            x0 += w + 2

# ------------------------------------------------------------------ look 3: on the fighter
def remap(im, ramp):
    """The sprite's own shading onto the gold ramp by brightness (a statue): what the VFX remap layer does."""
    a = im.getchannel("A"); L = im.convert("L")
    vals = sorted(v for v, al in zip(L.getdata(), a.getdata()) if al > 20)
    if not vals: return im
    q = [vals[len(vals) * k // 4] for k in (1, 2, 3)]
    step = [0 if v < q[0] else 1 if v < q[1] else 2 if v < q[2] else 3 for v in range(256)]
    out = Image.merge("RGB", [L.point([ramp[step[v]][ch] for v in range(256)]) for ch in range(3)]).convert("RGBA")
    out.putalpha(a); return out

def body_marks(u, im, t, m, white=False):
    """white: the hit's contrast frame is on, so it stays a white silhouette; only the outlines go round it."""
    if not m: return im, 0
    mask = solid(im); out = im
    if "stasis" in m and not white: out = remap(out, GOLD4)
    if "panic" in m and not white and (t // 120) % 2 == 0: out = tint(out, COL["panic"], 0.45)
    if "exposed" in m and not white:
        rim = ImageChops.subtract(mask, mask.filter(ImageFilter.MinFilter(5)))
        lay = Image.new("RGBA", im.size, COL["exposed"] + (255,)); lay.putalpha(rim.point(lambda v: int(v * (0.75 + 0.25 * math.sin(t / 120)))))
        out = out.copy(); out.alpha_composite(lay)
    specs = []
    if "focus" in m: specs.append((COL["focus"], 2 + (t // 250) % 2))
    if "aim" in m:
        sh, p = m["aim"]; period = max(60, int(320 - 240 * p))
        if (t // period) % 2 == 0: specs.append((AIMCOL[U[sh]["side"]], 2))
    if "guard" in m: specs.append((COL["guard"], 2))
    if specs:
        base = Image.new("RGBA", im.size, (0, 0, 0, 0)); prev_ = mask
        for col, wd in specs[:2] + [(NIGHT, 1)]:
            dil = prev_.filter(ImageFilter.MaxFilter(2 * wd + 1)); ring = ImageChops.subtract(dil, prev_)
            lay = Image.new("RGBA", im.size, col + (255,)); lay.putalpha(ring); base.alpha_composite(lay); prev_ = dil
        base.alpha_composite(out); out = base
    dx = (1 if (t // 60) % 2 else -1) if "panic" in m else 0
    return out, dx

def body_dy(u, pk, piece):
    cl = pk[piece]; cy = cl["off"][1] + cl["frames"][0].height / 2
    return int(round(cy * (u["height"] / BUILT_ON - 1)))

def body_hooks(mk):
    def pre(c, u, t, X, Y):
        m = mk.get(u["id"])
        if m and "bleed" in m: put(c, BODY["bleed"], "behind", (t // BODY["bleed"]["ms"]) % BODY["bleed"]["n"], X, Y)
    def post(c, u, t, X, Y):
        m = mk.get(u["id"])
        if not m: return
        mir = u["side"] == 1
        if "bleed" in m:
            put(c, BODY["bleed"], "front", (t // BODY["bleed"]["ms"]) % BODY["bleed"]["n"], X, Y, mir, body_dy(u, BODY["bleed"], "front"))
        if "open" in m:
            s = since(u["id"], "open", t); i = s // 63 if s < 3 * 63 else [3, 4][(s // 200) % 2]
            put(c, BODY["open"], "front", i, X, Y, mir, body_dy(u, BODY["open"], "front"))
        if "snare" in m: c.alpha_composite(JAWS[8], (X - 48 - OX, Y - 84 - OY))
    return pre, post

# ------------------------------------------------------------------ the fighters, with hooks under and over each one
def draw_units2(c, t, act_fx, pos, mk=None, pre=None, post=None):
    eclipse = G["eclipse"]
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
        if jit: dx += jit
        if white: im = flat(im, (255, 255, 255))
        elif gold: im = tint(im, PX["gold"], 0.65)
        else:
            if red: im = tint(im, (210, 30, 20), 0.25)
            if glow > 0: im = tint(im, (120, 255, 140), glow)
            if eclipse and u["chief"] and eclipse["start"] <= t < (eclipse["release"] or 10 ** 9):
                im = tint(im, (255, 70, 30), 0.18 + 0.14 * math.sin(t / 70))
        if mk is not None:
            im, ddx = body_marks(u, im, t, mk.get(u["id"]), white); dx += ddx
        X, Y = int(round(x + dx)), int(round(y))
        if pre: pre(c, u, t, X, Y)
        c.alpha_composite(im, (X - ax - OX, Y - ay - OY))
        if post: post(c, u, t, X, Y)

# ------------------------------------------------------------------ the two shots and the frame
SHOTS = [dict(name="SHOT 1  the top lanes", box=(590, 285, 910, 595), t0=1000, t1=ms(140)),
         dict(name="SHOT 2  the back line", box=(745, 385, 1065, 695), t0=ms(140), t1=(end_t or END_MS) + 2400)]
SC = 2; BW, BH = 320, 310; PW2, PH2 = BW * SC, BH * SC; HDR2, FTR2 = 26, 24
NAMES2 = ["1  FLOOR   marks on the ground under the fighter", "2  OVERHEAD   icons over the head",
          "3  BODY   outlines and marks on the fighter"]

def scenes_at(t, i):
    pos = positions[i]; act_fx = [f for f in FX if f[1] <= t < f[1] + f[2]]; mk = marks_at(t)
    base = BG.copy(); G["ground_common"](base, t, act_fx, pos)
    sc = [base.copy(), base.copy(), base]
    floor_under(sc[0], t, pos, mk)
    draw_units2(sc[0], t, act_fx, pos, None, post=floor_over(mk))
    draw_units2(sc[1], t, act_fx, pos, None)
    pre, post = body_hooks(mk); draw_units2(sc[2], t, act_fx, pos, mk, pre, post)
    links(sc[2], t, pos, mk, lambda uid: (pos[uid][0], pos[uid][1] - U[uid]["height"] * 0.55))
    for L in range(3): G["over_common"](sc[L], t, act_fx, pos)
    overhead(sc[1], t, pos, mk)
    for L in range(3): G["texts"](sc[L], t, act_fx, pos, L, mk)
    shake = 0
    for f in act_fx:
        if f[0] == "shake": shake = f[3] if ((t - f[1]) // 40) % 2 == 0 else -f[3]
    return sc, shake

def crop2(scene, box, shake=0):
    return scene.crop((box[0] - OX + shake, box[1] - OY, box[2] - OX + shake, box[3] - OY)).resize((PW2, PH2), Image.NEAREST)

def compose2(sc, t, shake, shot):
    frame = Image.new("RGB", (PW2, 3 * (HDR2 + PH2) + FTR2), (14, 9, 7)); d = ImageDraw.Draw(frame)
    for L in range(3):
        y0 = L * (HDR2 + PH2)
        frame.paste(crop2(sc[L], shot["box"], shake).convert("RGB"), (0, y0 + HDR2))
        d.text((8, y0 + 4), NAMES2[L], font=LABEL, fill=(240, 222, 186))
    tick = max(0, min(LAST, tick_at(t)))
    d.text((8, 3 * (HDR2 + PH2) + 4), f"{shot['name']}    2x close up of the same real fight, rules 42    tick {tick}", font=SMALL, fill=(200, 186, 160))
    return frame

def readability(probes, scenes_for, per=5):
    """Each mark on one fighter, cut from all three looks at 1x (the size a 1440 wide screen shows it), from 210 px
    over the feet (the icons) to 40 px under them (the floor), five marks a sheet."""
    os.makedirs(OUT + "/audit2", exist_ok=True)
    for s0 in range(0, len(probes), per):
        rows = probes[s0:s0 + per]
        rd = Image.new("RGB", (100 + 3 * 172, len(rows) * 254 + 24), (20, 14, 12)); dr = ImageDraw.Draw(rd)
        for L in range(3): dr.text((100 + L * 172 + 4, 4), NAMES2[L].split("   ")[0], fill=(240, 222, 186))
        for r, (name, tk, uid) in enumerate(rows):
            i = ms(tk) // STEP; sc = scenes_for(i); x, y, _ = positions[i][uid]
            box = (int(x) - 84, int(y) - 210, int(x) + 84, int(y) + 40)
            dr.text((6, 24 + r * 254 + 6), name, fill=(255, 255, 0)); dr.text((6, 24 + r * 254 + 22), f"tick {tk}", fill=(200, 186, 160))
            for L in range(3):
                rd.paste(sc[L].crop((box[0] - OX, box[1] - OY, box[2] - OX, box[3] - OY)).convert("RGB"), (100 + L * 172, 24 + r * 254))
        rd.save(f"{OUT}/audit2/readability_1x_{s0 // per}.png")

if __name__ == "__main__":
    os.makedirs(OUT + "/audit2", exist_ok=True)
    whites = {i for i, t in enumerate(frames_t) if any(f[0] == "white" and f[1] <= t < f[1] + f[2] for f in FX)}
    plan = []                                            # (shot, frame index, kept for the webp, kept for the audit)
    for s, shot in enumerate(SHOTS):
        idx = [i for i, t in enumerate(frames_t) if shot["t0"] <= t < shot["t1"]]
        for n, i in enumerate(idx): plan.append((s, i, n % 2 == 0 or i in whites, n % 5 == 0))
    moments = {36: 0, 95: 0, 180: 1}                     # key moment stills: tick -> shot
    probes = [("guard", 40, "camp.11.alpha.alphahunt.alpha.captain"), ("focus", 70, "camp.11.alpha.alphahunt.alpha.1"),
              ("aim, theirs", 36, "clarth.skirmisher.3"), ("aim, ours", 100, "camp.11.alpha.alphahunt.alpha.1"),
              ("stasis", 95, "camp.11.alpha.alphahunt.alpha.1"), ("bleed", 80, "camp.11.alpha.alphahunt.alpha.captain"),
              ("snare", 36, "clarth.skirmisher.3"), ("open", 60, "clarth.skirmisher.3"), ("exposed", 60, "camp.11.alpha.alphahunt.alpha.4"),
              ("panic", 180, "camp.11.alpha.alphahunt.alpha.2")]
    want = {ms(k) // STEP for k in list(moments) + [p[1] for p in probes]}
    clip, durs, audit, kept_scenes = [], [], [], {}
    last_s = None
    for n, (s, i, keep, aud) in enumerate(plan):
        if not (keep or aud or i in want): continue
        t = frames_t[i]; sc, shake = scenes_at(t, i); fr = compose2(sc, t, shake, SHOTS[s])
        if i in want: kept_scenes[i] = (sc, s)
        if aud: audit.append((t, s, fr))
        if keep:
            if last_s is not None and s != last_s:      # a 700 ms card between the shots
                card = fr.copy(); dc = ImageDraw.Draw(card, "RGBA"); dc.rectangle((0, 0, card.width, card.height), fill=(0, 0, 0, 170))
                tw = dc.textlength(SHOTS[s]["name"], font=TITLE); dc.text(((card.width - tw) / 2, card.height / 2 - 20), SHOTS[s]["name"], font=TITLE, fill=(255, 228, 170))
                clip.append(card); durs.append(700)
            clip.append(fr); durs.append(None); last_s = s
            if len(clip) % 40 == 0: print("frames", len(clip), flush=True)
    # each kept frame lasts until the next kept one of its shot
    kept = [(s, i) for s, i, keep, aud in plan if keep]
    k = 0
    for j in range(len(clip)):
        if durs[j] is not None: continue
        s, i = kept[k]; nxt = kept[k + 1] if k + 1 < len(kept) and kept[k + 1][0] == s else None
        durs[j] = ((nxt[1] - i) * STEP) if nxt else 600; k += 1
    clip[0].save(f"{OUT}/unit-looks-closeup-v2.webp", save_all=True, append_images=clip[1:], duration=durs, loop=0,
                 quality=80, method=6, minimize_size=True, allow_mixed=True)
    # audit: every 200 ms, the three looks side by side at 1x (the size a 1440 wide screen shows them)
    per = 6
    for a0 in range(0, len(audit), per):
        sh = Image.new("RGB", (3 * BW + 80, per * (BH + 4)), (0, 0, 0)); dd = ImageDraw.Draw(sh)
        for r, (t, s, fr) in enumerate(audit[a0:a0 + per]):
            for L in range(3):
                panel = fr.crop((0, L * (HDR2 + PH2) + HDR2, PW2, L * (HDR2 + PH2) + HDR2 + PH2)).resize((BW, BH), Image.NEAREST)
                sh.paste(panel, (80 + L * BW, r * (BH + 4)))
            dd.text((4, r * (BH + 4) + 4), f"{t} ms", fill=(255, 255, 0)); dd.text((4, r * (BH + 4) + 20), f"tick {max(0, tick_at(t))}", fill=(255, 255, 0))
            dd.text((4, r * (BH + 4) + 36), f"shot {s + 1}", fill=(255, 255, 0))
        sh.save(f"{OUT}/audit2/audit_{a0 // per:02d}.png")
    readability(probes, lambda i: kept_scenes[i][0])
    # key moments for Jacob, at the clip's 2x: rows are moments, columns the three looks
    ks = Image.new("RGB", (3 * PW2 + 12, len(moments) * (PH2 + 26) + 30), (14, 9, 7)); dk = ImageDraw.Draw(ks)
    for L in range(3): dk.text((L * (PW2 + 6) + 8, 6), NAMES2[L], font=LABEL, fill=(240, 222, 186))
    for r, (tk, s) in enumerate(moments.items()):
        i = ms(tk) // STEP; sc, _ = kept_scenes[i]; y0 = 30 + r * (PH2 + 26)
        dk.text((8, y0 + 4), f"tick {tk}   {SHOTS[s]['name']}", font=SMALL, fill=(200, 186, 160))
        for L in range(3): ks.paste(crop2(sc[L], SHOTS[s]["box"]).convert("RGB"), (L * (PW2 + 6), y0 + 22))
    ks.save(f"{OUT}/unit-looks-closeup-key-moments.png")
    # the phone check: one frame at 390 px wide
    ph = clip[len(clip) // 3]; ph.resize((390, round(ph.height * 390 / ph.width)), Image.LANCZOS).save(f"{OUT}/audit2/phone.png")
    print("frames", len(clip), "webp MB", round(os.path.getsize(f"{OUT}/unit-looks-closeup-v2.webp") / 1e6, 2), "size", clip[0].size)
