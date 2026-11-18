"""One blow at a time: the rules 42 fight shown the way Jacob picked on 6 Nov (rules 40) and Codex's screen already does
it (Assets/Scripts/Slice/CampaignArtStage.cs). 7 Nov 2026, after "there is so much going on i cant understand anything"
on the marks close up, and "Yes, strip it back".

Same fight as Art/Studies/Unit-Looks-20261107/looks42.py (seed 5100 road fight 9 at rules 42, dump42.txt), imported, with
the same clips, table effects, words and numbers. What the screen does, copied from CampaignArtStage.cs:
  - one beat per blow: a Hit, Heal or Miss with an attacker, paired with its Release on the same tick; beats queue in
    event order, so two blows on one tick play one after the other
  - the sim stops for each beat and the screen holds it 0.5 s; the strike lands 0.12 s in
  - the attacker comes to the front; a melee attacker steps onto the target's lane to 70 px from him and back
  - the target reacts when it lands; only this blow's number and word show
  - everyone else waits in idle, dimmed to 70 percent (CampaignArtBattle.cs, StageBystander)
Stripped back on top of that: no marks, no links, no bleed tick numbers, no Guard rings. Between beats the sim runs as
normal: moves, deaths, the Eclipse tell and the rout.
Usage: python -I stage44.py <PixelLab zips dir> <out dir>"""
import sys, os, math, runpy
from PIL import Image, ImageDraw

L42 = "C:/Users/jacob/KindredSiege-Clean/Art/Studies/Unit-Looks-20261107/"
ZIPS, OUT = sys.argv[1], sys.argv[2]
sys.argv = [L42 + "looks42.py", ZIPS, OUT]
G = runpy.run_path(L42 + "looks42.py", run_name="lib")["render"].__globals__   # looks42's live module globals

U, FX, events, positions = G["U"], G["FX"], G["events"], G["positions"]
ms, tick_at, STEP, OX, OY, BG, MG = G["ms"], G["tick_at"], G["STEP"], G["OX"], G["OY"], G["BG"], G["MG"]
PW, PH, PX, FLIP, flat, tint, clip_hit = G["PW"], G["PH"], G["PX"], G["FLIP"], G["flat"], G["tint"], G["clip_hit"]
end_t, END_MS, LAST, POWER, HITS = G["end_t"], G["END_MS"], G["LAST"], G["POWER"], G["HITS"]
EFF, WORD = G["EFF"], G["WORD"]
SMALL = G["SMALL"]

LEAD, HOLD = 120, 500                      # CampaignArtStage.cs: _stageLead .12 s, StageSeconds .5 s
def idx(v): return min(len(positions) - 1, max(0, int(v) // STEP))

# ------------------------------------------------------------------ the beats, paired the way PresentBattleEvents does
def impact(e): return e["kind"] in ("Hit", "Heal", "Miss") and e["actor"] in U and e["detail"] != "bleed"
def move(e): return e["kind"] in ("Release", "Land") and e["actor"] in U
BEATS, by_tick = [], {}
for e in events: by_tick.setdefault(e["tick"], []).append(e)
for tk in sorted(by_tick):
    last = None
    for e in by_tick[tk]:
        mv, im = move(e), impact(e)
        if not (mv or im): continue
        pair = (last is not None and last["blow"]["actor"] == e["actor"] and
                (last["blow"]["target"] == e["target"] or not last["blow"]["target"] or not e["target"]) and
                ((mv and not last["move"] and last["impact"]) or (im and last["move"] and not last["impact"])))
        if not pair:
            last = dict(blow=e, move=False, impact=False, tick=tk); BEATS.append(last)
        if im: last["blow"] = e; last["impact"] = True
        if mv: last["move"] = True
KILLS = {(e["tick"], e["target"]) for e in events if e["kind"] == "Kill"}
HIT_OF = {id(h["e"]): h for h in HITS}

# ------------------------------------------------------------------ each beat's own clips: the attacker's swing, the target's reaction
ALT = {}
def frame_in(clip, start, d, v, hold_last=False):
    lt = v - start
    if lt < 0: return None
    acc = 0
    for j, dd in enumerate(d):
        acc += dd
        if lt < acc: return min(j, len(clip["frames"]) - 1)
    return len(clip["frames"]) - 1 if hold_last else None

def plan_beat(b):
    e = b["blow"]; tb = ms(b["tick"]); A, T = U[e["actor"]], U.get(e["target"])
    ranged = A["cls"] == "marksman" or A["leader"]
    heal, trap = e["kind"] == "Heal", e["detail"] == "trap"
    h = HIT_OF.get(id(e))
    # held alone, every blow gets its full hit stop (looks42's "3 within 100 ms, only the biggest" was for blows at once)
    freeze = dict(crit=200, heavy=120, light=60, dot=0)[h["size"]] if h else (120 if e["detail"] == "parry" else 0)
    # the attacker: his swing, its contact frame on the landing (a shooter's muzzle 120 ms before, the shot flies)
    swing = None
    if not trap:
        if heal: key = "attack2"
        elif b["move"] or e["detail"].startswith(POWER): key = "power"
        else:
            key = "attack" if (ALT.get(A["id"], 0) % 2 == 0 or A["cls"] == "medic") else "attack2"; ALT[A["id"]] = ALT.get(A["id"], 0) + 1
        clip = A["clips"][key]; d = list(clip["ms"]); c = clip_hit(clip, A["face"]); d[c] += freeze
        at = tb - 120 if ranged and not heal else tb
        swing = (clip, at - sum(d[:c]), d)
    # the target: the reaction it lands with
    react = None
    if T is not None and not heal:
        if (b["tick"], T["id"]) in KILLS:
            dc = T["clips"]["death"]; d = list(dc["ms"]); d[-1] = 10 ** 7; react = (dc, tb, d)
        elif e["kind"] == "Miss" and e["detail"] == "parry":
            bc = T["clips"]["block"]; d = list(bc["ms"]); d[0] += 120; react = (bc, tb, d)
        elif e["kind"] == "Hit" and h and h["outcome"] == "block":
            bc = T["clips"]["block"]; d = list(bc["ms"]); d[0] += freeze; react = (bc, tb, d)
        elif e["kind"] == "Hit" and not trap:
            hc = T["clips"]["hit"]; d = list(hc["ms"]); d[0] += freeze; react = (hc, tb, d)
    # only this blow's effects, word and number, made from this blow alone (looks42's land() rules, one blow at a time)
    fx = []; Tid = T["id"] if T else None
    def add(k, s, du, data): fx.append((k, int(s), int(du), data))
    if b["move"]: add("gold", tb - 200, 200, A["id"])                     # rules 26 cue 2: the release flashes gold
    if ranged and not heal and T: add("bullet", tb - 120, 120, (A["id"], Tid))
    if heal and T:
        add("num", tb, 950, (Tid, "+" + str(e["amount"]), (120, 240, 130), False)); add("glow", tb, 280, Tid)
    elif e["kind"] == "Miss" and T:
        det = e["detail"]
        if det == "dodge": add("dodge", tb - 80, 600, (Tid, -T["face"] * 14)); add("eff", tb, EFF["dodge"]["dur"], ("dodge", Tid))
        elif det == "parry": add("eff", tb, EFF["parry"]["dur"], ("parry", Tid)); add("shove", tb + 120, 220, (A["id"], -A["face"] * 6))
        if det in WORD: add("word", tb + 40, 700, (Tid, det))
    elif e["kind"] == "Hit" and T and h:
        size = h["size"]
        if trap: add("jaws", tb - 80, 9 * 90 + 700, Tid)
        name = "block" if h["outcome"] == "block" else "crit" if h["outcome"] == "crit" else "hit"
        add("eff", tb, EFF[name]["dur"], (name, Tid)); add("white", tb, 40, Tid)
        if freeze: add("jitter", tb, freeze, Tid)
        add("tint", tb + 40, 320, Tid)
        kb = dict(crit=8, heavy=8, light=4, dot=0)[size]
        if kb and h["outcome"] != "block": add("shove", tb + freeze, 180, (Tid, -T["face"] * kb))
        if h["outcome"] == "crit": add("shake", tb, 160, 2)
        if h["outcome"] in ("block", "crit"): add("word", tb + 40, 700, (Tid, h["outcome"]))
        big = size in ("crit", "heavy")
        add("num", tb, 950, (Tid, str(e["amount"]), (255, 200, 60) if big else (255, 80, 60), big))
        if e["detail"] == "guarded": add("wall", tb - 120, 620, Tid)
    if T and (b["tick"], Tid) in KILLS: add("white", tb, 60, Tid); add("shake", tb, 240, 2)
    melee = not (ranged or heal or trap) and T is not None
    kill = T is not None and (b["tick"], T["id"]) in KILLS
    verb = ("heals" if heal else "springs a trap on" if trap else "misses" if e["kind"] == "Miss" else
            ("casts Eclipse" if A["chief"] else "lets loose") if e["kind"] in ("Release", "Land") else "hits")
    return dict(tb=tb, A=A, T=T, swing=swing, react=react, fx=fx, melee=melee, kill=kill, verb=verb)
PLANS = [plan_beat(b) for b in BEATS]
def name(u): return "our leader" if u["leader"] else "their chief" if u["chief"] else ("our " if u["side"] == 0 else "their ") + u["cls"]

# a fighter dies on the screen clock, from the moment his killing blow lands in its beat, so he never stands back up
DEATH_S = {}
def is_dead(u, S, v):
    if u["id"] in DEATH_S: return S >= DEATH_S[u["id"]]
    return u["dead_at"] is not None and v >= u["dead_at"]
def dead_pose(u, S):
    dc = u["clips"]["death"]; return dc, frame_in(dc, 0, dc["ms"], S - DEATH_S.get(u["id"], -10 ** 9), True)

# ------------------------------------------------------------------ drawing
DIM = {}
def dim(im):
    if id(im) not in DIM:
        r, g, b, a = im.split(); f = lambda v: v * 7 // 10
        DIM[id(im)] = (Image.merge("RGBA", (r.point(f), g.point(f), b.point(f), a)), im)
    return DIM[id(im)][0]

def run_pose(u, v, S, moving):
    """Between beats: no swings (they wait for their beat); a held windup, a death, the victory and the rout show."""
    if is_dead(u, S, v):
        dc, j = dead_pose(u, S); return dc, j, False
    for s, clip, d, key in u["acts"]:
        if not s <= v < s + sum(d): continue
        if key in ("windup", "victory"): return clip, frame_in(clip, s, d, v, True), False
        if key == "power" and sum(d) > 900:                                # a heavy's held tell
            c = clip_hit(clip, u["face"]); hit = s + sum(d[:c])
            if v < hit - LEAD: return clip, frame_in(clip, s, d, v), False
    fleeing = u.get("flee") is not None and v >= u["flee"]
    clip = u["clips"]["run"] if (moving or fleeing) else u["clips"]["idle"]
    return clip, int(S // (90 if (moving or fleeing) else 100)) % len(clip["frames"]), fleeing

def put_unit(c, u, clip, j, x, y, flip=False, mode=None):
    im = clip["frames"][j]; ax, ay = clip["ax"], clip["ay"]
    if flip: im = im.transpose(FLIP); ax = im.width - 1 - ax
    if mode == "dim": im = dim(im)
    elif mode: im = mode(im)
    c.alpha_composite(im, (int(round(x)) - ax - OX, int(round(y)) - ay - OY))

def flashes(u, fx, v):
    """The target's white frame, red flicker, heal glow and the attacker's gold cue, each once; plus jitter and shoves."""
    white = red = gold = False; glow = 0.0; dx = 0
    for k, s, du, data in fx:
        if not s <= v < s + du: continue
        who = data[0] if isinstance(data, tuple) else data
        if who != u["id"]: continue
        if k == "white": white = True
        elif k == "tint" and ((v - s) // 80) % 2 == 0: red = True
        elif k == "glow": glow = max(glow, 0.35 * (1 - (v - s) / du))
        elif k == "gold": gold = True
        elif k == "jitter": dx += 1 if ((v - s) // 40) % 2 == 0 else -1
        elif k == "shove":
            p = (v - s) / du; dx += data[1] * (p / 0.25 if p < 0.25 else 1 - (p - 0.25) / 0.75)
        elif k == "dodge":
            p = (v - s) / du; dx += data[1] * (min(1, p / 0.15) if p < 0.7 else (1 - p) / 0.3)
    def paint(im):
        if white: return flat(im, (255, 255, 255))
        if gold: return tint(im, PX["gold"], 0.65)
        if red: im = tint(im, (210, 30, 20), 0.25)
        if glow > 0: im = tint(im, (120, 255, 140), glow)
        return im
    return (paint if (white or gold or red or glow > 0) else None), dx

def ground(c, t, extra=()):
    keep = [f for f in FX if f[0] == "parry" and f[1] <= t < f[1] + f[2]] + [f for f in extra if f[0] == "wall"]
    G["ground_common"](c, t, keep, positions[idx(t)])

def titles_at(t): return [f for f in FX if f[0] == "title" and f[1] <= t < f[1] + f[2]]

def frame_run(v, S):
    i = idx(v); pos = positions[i]; c = BG.copy(); ground(c, v)
    for u in sorted(U.values(), key=lambda u_: pos[u_["id"]][1]):
        x, y, moving = pos[u["id"]]; clip, j, fl = run_pose(u, v, S, moving); put_unit(c, u, clip, j, x, y, fl)
    G["texts"](c, v, titles_at(v), pos, 0, {})
    return c, 0

def frame_beat(p, a, S):
    tb = p["tb"]; v = tb + a - LEAD; pos = positions[idx(tb)]; A, T = p["A"], p["T"]
    c = BG.copy(); ground(c, tb, p["fx"])
    def actor_xy():
        x, y, _ = pos[A["id"]]
        if not p["melee"]: return x, y
        tx, ty, _ = pos[T["id"]]; dx, dy = tx - x, ty - y
        dx -= math.copysign(min(abs(dx), 70), dx) if dx else 0
        ph = a / LEAD if a < LEAD else 1 - min(1, max(0, (a - LEAD - 55) / (HOLD - LEAD - 55)))
        s = ph * ph * (3 - 2 * ph)
        return x + dx * s, y + dy * s
    order = sorted(U.values(), key=lambda u_: (u_["id"] == A["id"], pos[u_["id"]][1]))   # the attacker in front
    for u in order:
        x, y, moving = pos[u["id"]]
        if u is A:
            x, y = actor_xy(); paint, dx = flashes(u, p["fx"], v)
            j = frame_in(*p["swing"], v) if p["swing"] else None
            if j is not None: put_unit(c, u, p["swing"][0], j, x + dx, y, mode=paint)
            else:
                clip = u["clips"]["idle"]; put_unit(c, u, clip, int(S // 100) % len(clip["frames"]), x + dx, y, mode=paint)
        elif u is T:
            paint, dx = flashes(u, p["fx"], v)
            j = frame_in(*p["react"], v, True) if (p["react"] and a >= LEAD) else None
            if j is not None: put_unit(c, u, p["react"][0], j, x + dx, y, mode=paint)
            elif is_dead(u, S, tb):
                dc, j = dead_pose(u, S); put_unit(c, u, dc, j, x, y)
            else:
                clip = u["clips"]["idle"]; put_unit(c, u, clip, int(S // 100) % len(clip["frames"]), x + dx, y, mode=paint)
        else:                                                              # everyone else waits, dimmed
            if is_dead(u, S, tb):
                dc, j = dead_pose(u, S); put_unit(c, u, dc, j, x, y, mode="dim"); continue
            clip, j, fl = run_pose(u, tb, S, False); put_unit(c, u, clip, j, x, y, fl, mode="dim")
    tpos = dict(pos)
    if p["melee"]: tpos[A["id"]] = actor_xy() + (False,)
    live = [f for f in p["fx"] if f[1] <= v < f[1] + f[2]]
    G["over_common"](c, v, [f for f in live if f[0] in ("eff", "bullet", "jaws")], tpos)
    G["texts"](c, v, [f for f in live if f[0] in ("word", "num")] + titles_at(tb), tpos, 0, {})
    shake = 0
    for k, s, du, data in live:
        if k == "shake": shake = data if ((v - s) // 40) % 2 == 0 else -data
    return c, shake

FTR = 20
def compose(c, shake, tick, note):
    fr = Image.new("RGB", (PW, PH + FTR), (14, 9, 7))
    fr.paste(c.crop((MG + shake, MG, MG + shake + PW, MG + PH)).convert("RGB"), (0, 0))
    ImageDraw.Draw(fr).text((8, PH + 3), f"one blow at a time, each held 0.5 s    {note}    tick {tick}", font=SMALL, fill=(200, 186, 160))
    return fr

if __name__ == "__main__":
    os.makedirs(OUT + "/audit", exist_ok=True)
    plan, v, S = [], 900, 0                                                # (kind, args, screen ms)
    for p in PLANS:
        start = p["tb"] - LEAD
        while v < start:
            du = min(80, start - v); plan.append(("run", v, du)); v += du; S += du
        if p["kill"]: DEATH_S[p["T"]["id"]] = S + LEAD
        for a, du in zip((0, 60, 120, 160, 240, 320, 400), (60, 60, 40, 80, 80, 80, 100)): plan.append(("beat", (p, a), du)); S += du
        v = max(v, p["tb"])
    stop = (end_t or END_MS) + 2400
    while v < stop: plan.append(("run", v, 80)); v += 80
    S = 0
    print("beats", len(PLANS), "frames", len(plan), "screen s", round(sum(x[2] for x in plan) / 1000, 1), flush=True)
    clip, durs, stamps = [], [], []
    for n, (kind, arg, du) in enumerate(plan):
        if kind == "run": c, sh = frame_run(arg, S); tk = tick_at(arg); note = ""
        else:
            p, a = arg; c, sh = frame_beat(p, a, S); tk = tick_at(p["tb"])
            note = f"{name(p['A'])} {p['verb']} {name(p['T']) if p['T'] else ''}"
        clip.append(compose(c, sh, max(0, min(LAST, tk)), note)); durs.append(du); stamps.append(S); S += du
        if n % 100 == 0: print("frame", n, flush=True)
    clip[0].save(f"{OUT}/one-blow-v1.webp", save_all=True, append_images=clip[1:], duration=durs, loop=0,
                 quality=80, method=6, minimize_size=True, allow_mixed=True)
    # audit: every 200 ms of screen time, half size, four across
    picks, nxt = [], 0
    for k, s in enumerate(stamps):
        if s >= nxt: picks.append(k); nxt = s - s % 200 + 200
    cw, ch, per = PW // 2, (PH + FTR) // 2, 24
    for s0 in range(0, len(picks), per):
        sh_ = Image.new("RGB", (4 * (cw + 4), 6 * (ch + 16)), (0, 0, 0)); dd = ImageDraw.Draw(sh_)
        for r, k in enumerate(picks[s0:s0 + per]):
            x0, y0 = (r % 4) * (cw + 4), (r // 4) * (ch + 16)
            sh_.paste(clip[k].resize((cw, ch)), (x0, y0 + 16)); dd.text((x0 + 4, y0 + 2), f"{stamps[k]} ms", fill=(255, 255, 0))
        sh_.save(f"{OUT}/audit/audit_{s0 // per:02d}.png")
    ph = clip[len(clip) // 3]; ph.resize((390, round(ph.height * 390 / ph.width))).save(f"{OUT}/audit/phone.png")
    print("frames", len(clip), "webp MB", round(os.path.getsize(f"{OUT}/one-blow-v1.webp") / 1e6, 2), "size", clip[0].size)
