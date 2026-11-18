"""Big moments, one at a time. 7 Nov 2026.

Jacob on the one blow mock (stage44.py): "no flair of watching cool spells go off", "the units are clubbed together",
"if we can make use of them effectively then we might do wonders". He said "Yes, build it" to:
  - every fighter on his own spot, as the game places them (CampaignArtBattle.cs, Pit presentation: our front row at
    x 650, back 470, theirs 790 and 970, rows spread 420 to 700, leaders at 250 and 1160); an attacker steps in to
    70 px from his target, strikes and steps back (CampaignArtStage.cs), so there is no heap
  - small hits quick, 0.3 s each; big moments held long with their spell art
  - when a mark lands its effect plays once, then goes quiet
The same fight as Unit-Looks-20261107/looks42.py (seed 5100 road fight 9 at rules 42, dump42.txt), imported.

Big moments and their art (all existing; nothing here is drawn in code):
  power moves, heavies, crits, combo cash ins   the unit's own power clip, the table effects and words, the game's
                                                cash words (Assets/Resources/CampaignArt/UI/Marks, CampaignArtCashWords.cs)
  a trap set, a trap sprung                     the Trapper's power clip, the picked bone jaws
  a guarded hit                                 the picked twin rings wall
  an aimed shot                                 the PixelLab aim circle charging under the target
  a mark landing, Focus Fire                    the PixelLab marks (Unit-Looks-20261107/v2-marks), played once
  a kill                                        a long freeze, the white frame, shake, the death clip
  the chief's Eclipse                           the approved meteor Eclipse and its cue sheet
                                                (Chief-Cut-Ins-20261018/spell-language/game-topdown/eclipse)
Usage: python -I stage45.py <PixelLab zips dir> <out dir>"""
import sys, os, math, json, runpy
from PIL import Image, ImageDraw, ImageChops, ImageFilter

KS = "C:/Users/jacob/KindredSiege-Clean/"
L42 = KS + "Art/Studies/Unit-Looks-20261107/"
ECL = KS + "Art/Studies/Chief-Cut-Ins-20261018/spell-language/game-topdown/eclipse/"
MK = L42 + "v2-marks/"
ZIPS, OUT = sys.argv[1], sys.argv[2]
sys.argv = [L42 + "looks42.py", ZIPS, OUT]
G = runpy.run_path(L42 + "looks42.py", run_name="lib")["render"].__globals__   # looks42's live module globals

U, UNITS, events, orders = G["U"], G["units"], G["events"], G["orders"]
ms, tick_at, PX, FLIP, flat, tint, clip_hit, sheet = G["ms"], G["tick_at"], G["PX"], G["FLIP"], G["flat"], G["tint"], G["clip_hit"], G["sheet"]
end_t, END_MS, LAST, POWER, HITS, EFF, WORD = G["end_t"], G["END_MS"], G["LAST"], G["POWER"], G["HITS"], G["EFF"], G["WORD"]
JAWS, RISE, SNARE_RING, SMALL, NIGHT, MG = G["JAWS"], G["RISE"], G["SNARE_RING"], G["SMALL"], G["NIGHT"], G["MG"]
FX42 = G["FX"]

# ------------------------------------------------------------------ the camera: the whole line, both leaders, the black sun's sky
CAM = (190, 85, 1240, 735)
OX, OY = CAM[0] - MG, CAM[1] - MG
PW, PH = CAM[2] - CAM[0], CAM[3] - CAM[1]
G["OX"], G["OY"], G["PW"] = OX, OY, PW                  # looks42's own effect, word and title drawing reads these
BG = G["STAGE"].crop((OX, OY, CAM[2] + MG, CAM[3] + MG))

# ------------------------------------------------------------------ every fighter on his own spot (CampaignArtBattle.cs)
ROW = dict(warden="front", skirmisher="front", trapper="front", marksman="back", medic="back")
SPOT, counts, placed = {}, {}, {}
for uid in UNITS:
    u = U[uid]
    if not (u["leader"] or u["chief"]): counts[(u["side"], ROW[u["cls"]])] = counts.get((u["side"], ROW[u["cls"]]), 0) + 1
for uid in UNITS:
    u = U[uid]
    if u["leader"]: SPOT[uid] = (250, 640); continue
    if u["chief"]: SPOT[uid] = (1160, 640); continue
    key = (u["side"], ROW[u["cls"]]); slot = placed.get(key, 0); placed[key] = slot + 1; n = counts[key]
    y = 560 if n <= 1 else 420 + slot * 280 / (n - 1)
    if key == (1, "back"): y = 420 + slot * 110                                     # a chief is present
    SPOT[uid] = ({(0, "front"): 650, (0, "back"): 470, (1, "front"): 790, (1, "back"): 970}[key], int(round(y)))
POS = {uid: (x, y, False) for uid, (x, y) in SPOT.items()}

# ------------------------------------------------------------------ art
def pack(name):
    m = json.load(open(f"{MK}{name}/manifest.json")); out = dict(ms=m["frame_ms"], n=m["frames"])
    for c in m["clips"]:
        if c["name"] not in ("behind", "front"): continue
        fr = []; w, h = c["frame"]["w"], c["frame"]["h"]
        for shx in c["sheets"]:
            s = Image.open(f"{MK}{name}/{shx['file']}").convert("RGBA"); cols = shx["columns"]
            fr += [s.crop(((k % cols) * w, (k // cols) * h, (k % cols) * w + w, (k // cols) * h + h)) for k in range(shx["frames"])]
        out[c["name"]] = dict(frames=fr, off=(c["offset"]["x"], c["offset"]["y"]), start=c.get("start_frame", 0))
    return out
MARKS = {k: pack(k) for k in ("focus_floor", "aim_floor", "exposed_floor", "stasis_floor", "panic_floor", "bleed_floor", "open_body")}
MARK_OF = dict(bleed="bleed_floor", exposed="exposed_floor", stasis="stasis_floor", panic="panic_floor", opening="open_body", focus="focus_floor")
RED4 = [(0x2E, 0x12, 0x0D), (0x5E, 0x24, 0x19), (0x96, 0x40, 0x2C), (0xDA, 0x6B, 0x51)]
MINT4 = [(0x1F, 0x29, 0x24), (0x4C, 0x5F, 0x56), (0x76, 0x90, 0x7F), (0x99, 0xBE, 0xAC)]
GOLD4 = [(0x3D, 0x2D, 0x14), (0x7A, 0x5A, 0x28), (0xD6, 0xAA, 0x5C), (0xF2, 0xD2, 0x7C)]
CASHWORD = dict(pin="PINNED", opening="OPENED", rip="RIPPED")                     # CampaignArtCashWords.cs
CASH = {w: Image.open(KS + f"Assets/Resources/CampaignArt/UI/Marks/cash_{w.lower()}.png").convert("RGBA") for w in CASHWORD.values()}
E = {k: sheet(ECL + "eclipse_" + k) for k in ("sun_tell_over", "sun_dark_over", "sun_flash_over", "sun_launch_over", "sun_loop_over",
     "sun_out_over", "tell_under", "dark_under", "wait_under", "shadow4_under", "shadow5_under", "shadow6_under", "comet_over",
     "hit_behind", "hit_front", "break_under", "staff_tell_front", "staff_hit_front")}

def recolour(im, src, dst):
    out = im.copy(); po = out.load()
    for yy in range(im.height):
        for xx in range(im.width):
            r, g, b, a = po[xx, yy]
            if a: n = min(range(len(src)), key=lambda k: (src[k][0] - r) ** 2 + (src[k][1] - g) ** 2 + (src[k][2] - b) ** 2); po[xx, yy] = dst[n] + (a,)
    return out
AIMC = {}
def aim_frame(p, side):
    cl = MARKS["aim_floor"]["behind"]; i = min(25, max(0, int(p * 26))); key = (i, side)
    if key not in AIMC: AIMC[key] = cl["frames"][i] if side == 1 else recolour(cl["frames"][i], RED4, MINT4)
    return AIMC[key], cl["off"]

def remap(im, ramp):
    a = im.getchannel("A"); L = im.convert("L")
    vals = sorted(v for v, al in zip(L.getdata(), a.getdata()) if al > 20)
    if not vals: return im
    q = [vals[len(vals) * k // 4] for k in (1, 2, 3)]
    step = [0 if v < q[0] else 1 if v < q[1] else 2 if v < q[2] else 3 for v in range(256)]
    out = Image.merge("RGB", [L.point([ramp[step[v]][ch] for v in range(256)]) for ch in range(3)]).convert("RGBA"); out.putalpha(a)
    return out

def frame_in(clip, start, d, v, hold_last=False):
    lt = v - start
    if lt < 0: return None
    acc = 0
    for j, dd in enumerate(d):
        acc += dd
        if lt < acc: return min(j, len(clip["frames"]) - 1)
    return len(clip["frames"]) - 1 if hold_last else None

# ------------------------------------------------------------------ the beats
def impact(e): return e["kind"] in ("Hit", "Heal", "Miss") and e["actor"] in U and e["detail"] != "bleed"
def move(e): return e["kind"] in ("Release", "Land") and e["actor"] in U
KILLS = {(e["tick"], e["target"]) for e in events if e["kind"] == "Kill"}
HIT_OF = {id(h["e"]): h for h in HITS}
by_tick = {}
for e in events: by_tick.setdefault(e["tick"], []).append(e)
focus_at = {}
for o in orders:
    if o["detail"] == "FocusFire" and o["side"] == 0 and o["target"] in U: focus_at.setdefault(o["tick"], []).append(o["target"])
BEATS = []
for tk in sorted(set(by_tick) | set(focus_at)):
    for tg in focus_at.get(tk, []): BEATS.append(dict(kind="focus", tick=tk, A=None, T=U[tg], marks=["focus"]))
    blows, last = [], None
    for e in by_tick.get(tk, []):
        mv, im = move(e), impact(e)
        if not (mv or im): continue
        pair = (last is not None and last["blow"]["actor"] == e["actor"] and
                (last["blow"]["target"] == e["target"] or not last["blow"]["target"] or not e["target"]) and
                ((mv and not last["move"] and last["impact"]) or (im and last["move"] and not last["impact"])))
        if not pair:
            last = dict(kind="blow", tick=tk, blow=e, move=False, impact=False, marks=[], cash=None, extra=[]); blows.append(last)
        if im: last["blow"] = e; last["impact"] = True
        if mv: last["move"] = True
    for b in blows:
        b["A"], b["T"] = U[b["blow"]["actor"]], U.get(b["blow"]["target"])
        if b["A"]["chief"] and not b["impact"]: b["kind"] = "eclipse"
    BEATS += blows
    loose, tail = {}, []
    for e in by_tick.get(tk, []):
        k = e["kind"]
        if k == "Mark" and e["target"] in U:
            hit = next((b for b in blows if b["T"] is U[e["target"]]), None)
            if hit: hit["marks"].append(e["detail"])
            else: loose.setdefault(e["target"], []).append(e["detail"])
        elif k == "Cash" and CASHWORD.get(e["detail"]):
            hit = next((b for b in blows if b["T"] is U.get(e["target"]) and b["A"] is U.get(e["actor"])), None) or \
                  next((b for b in blows if b["T"] is U.get(e["target"])), None)
            if hit: hit["cash"] = CASHWORD[e["detail"]]
        elif k == "Defence" and e["detail"] == "sidestep" and e["actor"] in U:
            hit = next((b for b in blows if b["T"] is U[e["actor"]]), None)
            if hit: hit["extra"].append(e)
        elif k == "TrapArmed" and e["actor"] in U: tail.append(dict(kind="trapset", tick=tk, A=U[e["actor"]], T=None, marks=[]))
        elif k == "Snap" and e["detail"] == "panic" and e["actor"] in U: tail.append(dict(kind="mark", tick=tk, A=None, T=U[e["actor"]], marks=["panic"]))
        elif k == "Windup" and e["actor"] in U and U[e["actor"]]["chief"]: tail.append(dict(kind="tell", tick=tk, A=U[e["actor"]], T=None, marks=[]))
        elif k == "Kill" and e["target"] in U and not any(b["T"] is U[e["target"]] for b in blows):
            tail.append(dict(kind="kill", tick=tk, A=None, T=U[e["target"]], marks=[]))
    for tg, mks in loose.items(): BEATS.append(dict(kind="mark", tick=tk, A=None, T=U[tg], marks=mks))
    BEATS += tail
ECLIPSE_TARGETS = [U[x] for x in ("clarth.warden.1", "clarth.skirmisher.3", "clarth.trapper.4")]   # our front row, top to bottom
SUN = (SPOT["clarth.skirmisher.3"][0] + 180, SPOT["clarth.skirmisher.3"][1] - 380)            # eclipse_cues.json, blackSun rule

# ------------------------------------------------------------------ what each beat shows
def plan(b):
    tb = ms(b["tick"]); A, T = b.get("A"), b.get("T"); fx = []
    def add(k, s, du, data): fx.append((k, int(s), int(du), data))
    p = dict(kind=b["kind"], tb=tb, A=A, T=T, fx=fx, swing=None, react=None, melee=False, gold=None)
    Tid = T["id"] if T else None
    if b["kind"] == "blow":
        e = b["blow"]; h = HIT_OF.get(id(e)); heal, trap = e["kind"] == "Heal", e["detail"] == "trap"
        ranged = A["cls"] == "marksman" or A["leader"]; kill = T is not None and (b["tick"], Tid) in KILLS
        crit = bool(h and h["outcome"] == "crit")
        big = kill or b["move"] or e["detail"].startswith(POWER + ("guarded", "trap")) or crit or b["cash"] or b["marks"]
        aimed = e["detail"].startswith("aimed")
        lead, hold = (480, 1000) if aimed else (160, 1100) if kill else (160, 700) if big else (80, 300)
        size = h["size"] if h else "light"
        freeze = 300 if kill else dict(crit=200, heavy=120, light=60, dot=0)[size] if h else (120 if e["detail"] == "parry" else 0)
        if big and not kill: freeze = max(freeze, 120)
        p.update(lead=lead, hold=hold, tier="kill" if kill else "big" if big else "small", melee=not (ranged or heal or trap) and T is not None)
        if not trap:
            key = "attack2" if heal else "power" if (b["move"] or e["detail"].startswith(POWER)) else "attack"
            clip = A["clips"][key]; d = list(clip["ms"]); c = clip_hit(clip, A["face"]); d[c] += freeze
            at = tb - 120 if ranged and not heal else tb
            p["swing"] = (clip, at - sum(d[:c]), d); p["windup"] = sum(d[:c])
        if p["melee"]:
            # the contact spot is 70 px short of the target on his lane (CampaignArtStage.cs); a far one is run to
            hx, hy = SPOT[A["id"]]; tx, ty = SPOT[Tid]
            gx, gy = tx - math.copysign(70, tx - hx), ty; dist = math.hypot(gx - hx, gy - hy)
            follow = 120 if p["tier"] == "small" else 400 if kill else 280
            p["contact"], p["run"] = (gx, gy), 0
            if dist > 150:
                p["run"] = int(dist / 2.4)
                wind = min(p["windup"], 150 if p["tier"] == "small" else 300 if kill else 250)   # the swing's last frames before contact
                p["lead"] = p["run"] + wind; p["ret"] = p["lead"] + follow
                p["hold"] = max(p["ret"] + p["run"], p["lead"] + (900 if kill else 0))
                if aimed: pass
            else:
                p["ret"] = p["lead"] + follow
        if T is not None and not heal:
            if kill: dc = T["clips"]["death"]; d = list(dc["ms"]); d[-1] = 10 ** 7; p["react"] = (dc, tb, d)
            elif e["kind"] == "Miss" and e["detail"] == "parry": bc = T["clips"]["block"]; d = list(bc["ms"]); d[0] += 120; p["react"] = (bc, tb, d)
            elif e["kind"] == "Hit" and h and h["outcome"] == "block": bc = T["clips"]["block"]; d = list(bc["ms"]); d[0] += freeze; p["react"] = (bc, tb, d)
            elif e["kind"] == "Hit" and not trap: hc = T["clips"]["hit"]; d = list(hc["ms"]); d[0] += freeze; p["react"] = (hc, tb, d)
        if b["move"]: add("gold", tb - 200, 200, A["id"])                               # rules 26 cue 2
        if aimed and T: add("aim", tb - lead, lead - 120, (Tid, A["side"]))
        if ranged and not heal and T: add("bullet", tb - 120, 120, (A["id"], Tid))
        if heal and T: add("num", tb, 950, (Tid, "+" + str(e["amount"]), (120, 240, 130), False)); add("glow", tb, 280, Tid)
        elif e["kind"] == "Miss" and T:
            det = e["detail"]
            if det == "dodge": add("dodge", tb - 80, 600, (Tid, -T["face"] * 14)); add("eff", tb, EFF["dodge"]["dur"], ("dodge", Tid))
            elif det == "parry": add("eff", tb, EFF["parry"]["dur"], ("parry", Tid)); add("shove", tb + 120, 220, (A["id"], -A["face"] * 6))
            if det in WORD and not b["cash"]: add("word", tb + 40, 700, (Tid, det))
        elif e["kind"] == "Hit" and T and h:
            if trap: add("jaws", tb - 80, 9 * 90 + 700, Tid)
            name = "block" if h["outcome"] == "block" else "crit" if crit else "hit"
            add("eff", tb, EFF[name]["dur"], (name, Tid)); add("white", tb, 100 if kill else 40, Tid)
            if freeze: add("jitter", tb, freeze, Tid)
            add("tint", tb + 40, 320, Tid)
            kb = dict(crit=8, heavy=8, light=4, dot=0)[size]
            if kb and h["outcome"] != "block": add("shove", tb + freeze, 180, (Tid, -T["face"] * kb))
            if crit or kill: add("shake", tb, 300 if kill else 160, 4 if kill else 2)
            if h["outcome"] in ("block", "crit") and not b["cash"]: add("word", tb + 40, 700, (Tid, h["outcome"]))
            bign = size in ("crit", "heavy") or big
            add("num", tb, 950, (Tid, str(e["amount"]), (255, 200, 60) if bign else (255, 80, 60), bign))
            if e["detail"] == "guarded": add("wall", tb - 120, 4 * 80 + 300, Tid)
        if b["cash"] and T: add("cash", tb + 40, hold - lead, (Tid, b["cash"]))
        for x in b["extra"]:                                                             # a sidestep from a heavy
            if x["actor"] in U: add("dodge", tb - 80, 600, (x["actor"], -U[x["actor"]]["face"] * 14))
    elif b["kind"] in ("focus", "mark"):
        p.update(lead=160, hold=600, tier="spell")
    elif b["kind"] == "trapset":
        clip = A["clips"]["power"]; p.update(lead=160, hold=600, tier="spell", swing=(clip, tb - 160, list(clip["ms"])))
    elif b["kind"] == "kill":
        dc = T["clips"]["death"]; d = list(dc["ms"]); d[-1] = 10 ** 7
        p.update(lead=160, hold=1400, tier="kill", react=(dc, tb, d), also=[])
        add("white", tb, 100, Tid); add("jitter", tb, 300, Tid); add("shake", tb, 300, 4)
        combo = next((e for e in by_tick[b["tick"]] if e["kind"] == "Combo"), None)
        if combo:                                          # the finishing combo: every man it hits, at once
            add("title", tb, 1400, combo["detail"].upper())
            for e in by_tick[b["tick"]]:
                if e["kind"] != "Hit" or e["actor"] or e["target"] not in U: continue
                v_ = U[e["target"]]; add("num", tb, 950, (v_["id"], str(e["amount"]), (255, 200, 60), True))
                add("eff", tb, EFF["crit"]["dur"], ("crit", v_["id"]))
                if v_ is not T:
                    add("white", tb, 60, v_["id"]); add("tint", tb + 60, 320, v_["id"]); p["also"].append(v_)
    elif b["kind"] == "tell":
        p.update(lead=0, hold=1600, tier="eclipse"); add("title", tb, 1600, "ECLIPSE")
    elif b["kind"] == "eclipse":
        # the parry window first: the caster flashes gold (rules 26 cue 2), then the meteors fall; the sim says dodged
        p.update(lead=240, hold=240 + 2800, tier="eclipse"); add("title", tb, 2800, "DODGED"); add("gold", tb - 240, 240, A["id"])
    for mk in b.get("marks", []):
        if mk in MARK_OF and T: add("mark", tb, max(700, p["hold"] - p["lead"]), (Tid, mk))
        elif mk == "snare" and T and not (b["kind"] == "blow" and b["blow"]["detail"] == "trap"): add("snarering", tb, 700, Tid)
    p["key"] = (b["kind"], b["tick"], A["id"] if A else "", Tid or ""); p["tick"] = b["tick"]
    return p
PLANS = [plan(b) for b in BEATS]
for i, p in enumerate(PLANS): p["i"] = i

# ------------------------------------------------------------------ the Eclipse, from its cue sheet
def comet_plan():
    """Launch frame, fall frames and hit frame for each target: the first leaves on Release + 1, each next one leaves
    on the frame the one before it hits (eclipse_cues.json)."""
    out, launch = [], 1
    for u in ECLIPSE_TARGETS:
        fx_, fy_ = SPOT[u["id"]]; dist = math.hypot(fx_ - SUN[0], fy_ - 2 - SUN[1]); fall = max(4, round(dist / 110))
        hit = launch + fall - 1; out.append(dict(u=u, launch=launch, fall=fall, hit=hit)); launch = hit
    return out
COMETS = comet_plan()
LAST_HIT = COMETS[-1]["hit"]
S_TELL = S_REL = None                                     # screen ms when the tell and the release beats begin

def put_anchor(c, sh, j, x, y, flip=False):
    im = sh["frames"][j]; ax = sh["ax"]
    if flip: im = im.transpose(FLIP); ax = im.width - 1 - ax
    c.alpha_composite(im, (int(round(x)) - ax - OX, int(round(y)) - sh["ay"] - OY))

def eclipse_ground(c, S):
    """The tell under each target from the windup on, then the release's dark, wait, shadows and breaks."""
    if S_TELL is None or S < S_TELL: return
    if S_REL is None or S < S_REL:
        tf = (S - S_TELL) // 80
        for u in ECLIPSE_TARGETS:
            x, y = SPOT[u["id"]]
            if tf < 38: put_anchor(c, E["tell_under"], tf, x, y)
            else: put_anchor(c, E["wait_under"], (tf - 38) % 2, x, y)
        return
    f = (S - S_REL) // 80
    for k in COMETS:
        x, y = SPOT[k["u"]["id"]]
        if f == 0: put_anchor(c, E["dark_under"], 0, x, y)
        elif f < k["hit"]: put_anchor(c, E["wait_under"], (f - 1) % 2, x, y)
        sh = E.get(f"shadow{k['fall']}_under")
        if sh and k["launch"] <= f < k["launch"] + len(sh["frames"]): put_anchor(c, sh, f - k["launch"], x, y)
        if k["hit"] + 14 <= f < k["hit"] + 14 + len(E["break_under"]["frames"]): put_anchor(c, E["break_under"], f - k["hit"] - 14, x, y)
        if k["hit"] <= f < k["hit"] + 14: put_anchor(c, E["hit_behind"], f - k["hit"], x, y)
    # dodged (Release "dodged" in the sim): the burst goes up behind each man, nothing in front of him

def eclipse_over(c, S):
    if S_TELL is None or S < S_TELL: return
    chief = next(u for u in U.values() if u["chief"]); cx, cy = SPOT[chief["id"]]
    alive = not is_dead(chief, S, 10 ** 9)
    if S_REL is None or S < S_REL:
        tf = (S - S_TELL) // 80
        if alive: put_anchor(c, E["staff_tell_front"], min(tf, 37), cx, cy)
        if tf < 38: put_anchor(c, E["sun_tell_over"], tf, *SUN)
        else: put_anchor(c, E["sun_loop_over"], (tf - 38) % 8, *SUN)
        return
    f = (S - S_REL) // 80
    if f < 3 and alive: put_anchor(c, E["staff_hit_front"], f, cx, cy)
    for k in COMETS:                                       # comets first, so they leave from behind the disc
        x, y = SPOT[k["u"]["id"]]
        if k["launch"] <= f < k["hit"]:
            j = f - k["launch"] + 1; t = j / k["fall"]
            put_anchor(c, E["comet_over"], (f - k["launch"]) % len(E["comet_over"]["frames"]),
                       math.floor(SUN[0] + (x - SUN[0]) * t + 0.5), math.floor(SUN[1] + (y - 2 - SUN[1]) * t + 0.5))
    if f == 0: put_anchor(c, E["sun_dark_over"], 0, *SUN)
    elif f == 1: put_anchor(c, E["sun_flash_over"], 0, *SUN)
    elif f < LAST_HIT: put_anchor(c, E["sun_launch_over" if any(k["launch"] == f for k in COMETS[1:]) else "sun_loop_over"],
                                  0 if any(k["launch"] == f for k in COMETS[1:]) else f % 8, *SUN)
    elif f - LAST_HIT - 1 < len(E["sun_out_over"]["frames"]) and f > LAST_HIT: put_anchor(c, E["sun_out_over"], f - LAST_HIT - 1, *SUN)

def eclipse_rim(u, S):
    """The tell's orange rim round each target, on even tell frames 26 to 36 (and on after, until it drops)."""
    if S_TELL is None or S < S_TELL or (S_REL is not None and S >= S_REL) or u not in ECLIPSE_TARGETS: return None
    tf = (S - S_TELL) // 80
    return (229, 95, 35) if tf >= 26 and tf % 2 == 0 else None

def eclipse_dodge(u, S):
    if S_REL is None or S < S_REL or u not in ECLIPSE_TARGETS: return 0
    k = next(k for k in COMETS if k["u"] is u); s0 = S_REL + (k["hit"] - 2) * 80; p = (S - s0) / 1200
    if p < 0 or p >= 1: return 0
    return -u["face"] * 30 * (min(1, p / 0.1) if p < 0.75 else (1 - p) / 0.25)

# ------------------------------------------------------------------ deaths on the screen clock
DEATH_S = {}
def is_dead(u, S, v):
    if u["id"] in DEATH_S: return S >= DEATH_S[u["id"]]
    return u["dead_at"] is not None and v >= u["dead_at"]
def dead_pose(u, S):
    dc = u["clips"]["death"]; return dc, frame_in(dc, 0, dc["ms"], S - DEATH_S.get(u["id"], -10 ** 9), True)

# ------------------------------------------------------------------ drawing the fighters
DIM = {}
def dim(im):
    if id(im) not in DIM:
        r, g, b, a = im.split(); f = lambda v: v * 7 // 10
        DIM[id(im)] = (Image.merge("RGBA", (r.point(f), g.point(f), b.point(f), a)), im)
    return DIM[id(im)][0]

def rim_behind(im, col):
    m = im.getchannel("A").point(lambda v: 255 if v > 20 else 0)
    ring = ImageChops.subtract(m.filter(ImageFilter.MaxFilter(5)), m)
    base = Image.new("RGBA", im.size, col + (255,)); base.putalpha(ring); base.alpha_composite(im); return base

def where(u, v):
    x, y = SPOT[u["id"]]
    if u.get("flee") is not None and v >= u["flee"] and not is_dead(u, 10 ** 9, v): x += (v - u["flee"]) * 0.32
    return x, y

def idle_pose(u, v, S):
    for s, clip, d, key in u["acts"]:
        if not s <= v < s + sum(d): continue
        if key in ("windup", "victory"): return clip, frame_in(clip, s, d, v, True), False
        if key == "power" and sum(d) > 900:                                # a heavy's held tell
            c = clip_hit(clip, u["face"]); hit = s + sum(d[:c])
            if v < hit - 160: return clip, frame_in(clip, s, d, v), False
    fleeing = u.get("flee") is not None and v >= u["flee"]
    clip = u["clips"]["run"] if fleeing else u["clips"]["idle"]
    return clip, int(S // (90 if fleeing else 100)) % len(clip["frames"]), fleeing

def put_unit(c, clip, j, x, y, flip=False, mode=None, rim=None):
    im = clip["frames"][j]; ax, ay = clip["ax"], clip["ay"]
    if flip: im = im.transpose(FLIP); ax = im.width - 1 - ax
    if mode == "dim": im = dim(im)
    elif mode: im = mode(im)
    if rim: im = rim_behind(im, rim)
    c.alpha_composite(im, (int(round(x)) - ax - OX, int(round(y)) - ay - OY))

def flashes(uid, fx, v):
    white = red = gold = False; glow = 0.0; dx = 0
    for k, s, du, data in fx:
        if not s <= v < s + du: continue
        who = data[0] if isinstance(data, tuple) else data
        if who != uid: continue
        if k == "white": white = True
        elif k == "tint" and ((v - s) // 80) % 2 == 0: red = True
        elif k == "glow": glow = max(glow, 0.35 * (1 - (v - s) / du))
        elif k == "gold": gold = True
        elif k == "jitter": dx += 1 if ((v - s) // 40) % 2 == 0 else -1
        elif k == "shove": q = (v - s) / du; dx += data[1] * (q / 0.25 if q < 0.25 else 1 - (q - 0.25) / 0.75)
        elif k == "dodge": q = (v - s) / du; dx += data[1] * (min(1, q / 0.15) if q < 0.7 else (1 - q) / 0.3)
    stasis = any(k == "mark" and data == (uid, "stasis") and s <= v < s + du for k, s, du, data in fx)
    def paint(im):
        if white: return flat(im, (255, 255, 255))
        if gold: return tint(im, PX["gold"], 0.65)
        if stasis: im = remap(im, GOLD4)
        if red: im = tint(im, (210, 30, 20), 0.25)
        if glow > 0: im = tint(im, (120, 255, 140), glow)
        return im
    return (paint if (white or gold or red or glow > 0 or stasis) else None), dx

def marks_layer(c, fx, v, S, front):
    """A mark's PixelLab effect, once, from the beat that lands it; Focus Fire's lock on; the aim circle charging."""
    for k, s, du, data in fx:
        if not s <= v < s + du: continue
        if k == "mark":
            uid, mk = data; pk = MARKS[MARK_OF[mk]]; x, y = SPOT[uid]; piece = "front" if front else "behind"
            if piece not in pk: continue
            cl = pk[piece]; n = len(cl["frames"]); i = (v - s) // pk["ms"]
            if mk == "opening": i = min(i, 4)                                   # the claws hold their peak
            else: i %= n
            j = i - cl["start"]
            if 0 <= j < n:
                im = cl["frames"][j]; ox, oy = cl["off"]
                if mk == "opening":
                    oy = int(round((oy + im.height / 2) * (U[uid]["height"] / 125 - 1))) + oy
                    if U[uid]["side"] == 1: im = im.transpose(FLIP); ox = -(ox + im.width - 1)
                c.alpha_composite(im, (x + ox - OX, y + oy - OY))
        elif k == "aim" and not front:
            uid, side = data; x, y = SPOT[uid]; im, (ox, oy) = aim_frame((v - s) / du, side)
            c.alpha_composite(im, (x + ox - OX, y + oy - OY))
        elif k == "snarering" and not front:
            x, y = SPOT[data]; c.alpha_composite(SNARE_RING, (x - 65 - OX, y - 96 - OY))
        elif k == "wall" and not front:
            x, y = SPOT[data]; c.alpha_composite(RISE[5 + min(3, (v - s) // 80)], (x - 64 - OX, y - 108 - OY))

def words_layer(c, fx, v):
    live = [f for f in fx if f[1] <= v < f[1] + f[2]]
    cash = {f[3][0] for f in live if f[0] == "cash"}
    G["texts"](c, v, [f for f in live if f[0] in ("word", "num", "title") and not (f[0] == "word" and f[3][0] in cash)], POS, 0, {})
    for k, s, du, data in live:
        if k != "cash": continue
        uid, w = data; im = CASH[w]; x, y = SPOT[uid]; rise = int(6 * min(1, (v - s) / 300))
        c.alpha_composite(im, (int(x - im.width / 2) - OX, int(y - U[uid]["height"] - 8 - im.height - rise) - OY))

def titles_at(t): return [f for f in FX42 if f[0] == "title" and f[3] == "BREAK" and f[1] <= t < f[1] + f[2]]

# ------------------------------------------------------------------ one frame between beats, one frame inside a beat
# an armed trap lies open in front of its Trapper until it springs (the game's Pit screen puts it 85 px ahead of him)
TRAPS = []
for e in events:
    if e["kind"] == "TrapArmed" and e["actor"] in U:
        sp = next((x["tick"] for x in events if x["kind"] == "TrapSprung" and x["actor"] == e["actor"] and x["tick"] >= e["tick"]), 10 ** 6)
        TRAPS.append((e["actor"], e["tick"], sp))
def traps_layer(c, tick, skip=None):
    for uid, t0, t1 in TRAPS:
        if t0 <= tick < t1 and not (uid == skip and tick == t0):
            x, y = SPOT[uid]; x += 85 if U[uid]["side"] == 0 else -85
            c.alpha_composite(JAWS[0], (x - 48 - OX, y + 12 - 84 - OY))

def frame_run(v, S):
    c = BG.copy(); eclipse_ground(c, S); traps_layer(c, tick_at(v))
    for u in sorted(U.values(), key=lambda u_: SPOT[u_["id"]][1]):
        x, y = where(u, v)
        if is_dead(u, S, v): dc, j = dead_pose(u, S); put_unit(c, dc, j, x, y)
        else: clip, j, fl = idle_pose(u, v, S); put_unit(c, clip, j, x, y, fl, rim=eclipse_rim(u, S))
    eclipse_over(c, S)
    G["texts"](c, v, titles_at(v), POS, 0, {})
    return c, 0

def frame_beat(p, a, S):
    tb, A, T = p["tb"], p["A"], p["T"]; v = tb + a - p["lead"]
    c = BG.copy(); eclipse_ground(c, S)
    traps_layer(c, p["tick"], skip=A["id"] if (p["kind"] == "trapset" and a < p["lead"]) else None)
    marks_layer(c, p["fx"], v, S, False)
    def actor_state():
        """Where the attacker is and what he shows: a far target is run to and from, a near one stepped to on the
        swing; once he turns back the swing stops, so its smear never plays at home."""
        hx, hy = SPOT[A["id"]]; idle = A["clips"]["idle"]; sw = p["swing"]
        idle_f = (idle, int(S // 100) % len(idle["frames"]))
        if not p["melee"]:
            j = frame_in(*sw, v) if sw else None
            return (hx, hy, sw[0], j, False) if j is not None else (hx, hy) + idle_f + (False,)
        (gx, gy), run, lead, ret, hold = p["contact"], p["run"], p["lead"], p["ret"], p["hold"]
        if run:
            rc = A["clips"]["run"]; jr = int(S // 90) % len(rc["frames"])
            if a < run: q = a / run; return hx + (gx - hx) * q, hy + (gy - hy) * q, rc, jr, False
            if a < ret:
                j = frame_in(*sw, v)
                return (gx, gy, sw[0], j, False) if j is not None else (gx, gy) + idle_f + (False,)
            q = min(1, (a - ret) / max(1, hold - ret)); return gx + (hx - gx) * q, gy + (hy - gy) * q, rc, jr, True
        if a < lead: q = a / max(1, lead)
        elif a < ret: q = 1
        else: q = 1 - min(1, (a - ret) / max(1, hold - ret))
        q = q * q * (3 - 2 * q); x, y = hx + (gx - hx) * q, hy + (gy - hy) * q
        j = frame_in(*sw, v) if a < ret else None
        return (x, y, sw[0], j, False) if j is not None else (x, y) + idle_f + (False,)
    lit = {u["id"] for u in ([A] if A else []) + ([T] if T else []) + p.get("also", [])}
    if p["kind"] in ("tell", "eclipse"): lit |= {u["id"] for u in ECLIPSE_TARGETS}
    calm = p["tier"] == "small"                             # small hits never dim the field, so it does not strobe
    order = sorted(U.values(), key=lambda u_: (A is not None and u_ is A, SPOT[u_["id"]][1]))
    for u in order:
        x, y = SPOT[u["id"]]
        if u is A and p["kind"] in ("tell", "eclipse"):
            att = u["clips"]["attack"]; paint, _ = flashes(u["id"], p["fx"], v)
            j = 1 if p["kind"] == "tell" or a < p["lead"] else min(len(att["frames"]) - 1, 2 + (a - p["lead"]) // 120)
            put_unit(c, att, j, x, y, mode=paint); continue
        if u is A:
            x, y, clip, j, fl = actor_state(); paint, dx = flashes(u["id"], p["fx"], v)
            put_unit(c, clip, j, x + dx, y, fl, mode=paint); continue
        if u is T and p["react"] is not None and a >= p["lead"]:
            paint, dx = flashes(u["id"], p["fx"], v)
            put_unit(c, p["react"][0], frame_in(*p["react"], v, True), x + dx, y, mode=paint); continue
        if is_dead(u, S, tb):
            dc, j = dead_pose(u, S); put_unit(c, dc, j, x, y, mode=None if (calm or u["id"] in lit) else "dim"); continue
        paint, dx = flashes(u["id"], p["fx"], v)
        dx += eclipse_dodge(u, S)
        clip, j, fl = idle_pose(u, tb, S)
        put_unit(c, clip, j, x + dx, y, fl, mode=paint if (calm or u["id"] in lit) else "dim", rim=eclipse_rim(u, S))
    marks_layer(c, p["fx"], v, S, True)
    tpos = dict(POS)
    if p["melee"]: tpos[A["id"]] = actor_state()[:2] + (False,)
    G["over_common"](c, v, [f for f in p["fx"] if f[0] in ("eff", "bullet", "jaws") and f[1] <= v < f[1] + f[2]], tpos)
    eclipse_over(c, S)
    words_layer(c, p["fx"], v)
    shake = 0
    for k, s, du, data in p["fx"]:
        if k == "shake" and s <= v < s + du: shake = data if ((v - s) // 40) % 2 == 0 else -data
    if p["kind"] == "eclipse":
        f = (a - p["lead"]) // 80
        for kk in COMETS:
            if f in (kk["hit"], kk["hit"] + 1): shake = 4 if f == kk["hit"] else -2
    return c, shake

FTR = 20
def compose(c, shake, tick, note):
    fr = Image.new("RGB", (PW, PH + FTR), (14, 9, 7))
    fr.paste(c.crop((MG + shake, MG, MG + shake + PW, MG + PH)).convert("RGB"), (0, 0))
    ImageDraw.Draw(fr).text((8, PH + 3), f"{note}    tick {tick}", font=SMALL, fill=(200, 186, 160))
    return fr

def name(u): return "our leader" if u["leader"] else "their chief" if u["chief"] else ("our " if u["side"] == 0 else "their ") + u["cls"]
def caption(p):
    A, T, k = p["A"], p["T"], p["kind"]
    if k == "focus": return f"Focus Fire on {name(T)}"
    if k == "mark": return f"{name(T)}: " + ", ".join(x for x in BEATS[p["i"]]["marks"])
    if k == "trapset": return f"{name(A)} sets a trap"
    if k == "kill": return f"{name(T)} falls"
    if k == "tell": return f"{name(A)} calls the Eclipse"
    if k == "eclipse": return f"the Eclipse drops on our front row"
    e = BEATS[p["i"]]["blow"]
    verb = "heals" if e["kind"] == "Heal" else "springs a trap on" if e["detail"] == "trap" else "misses" if e["kind"] == "Miss" else "hits"
    return f"{name(A)} {verb} {name(T) if T else ''}"

def offsets(p):
    if p["tier"] == "eclipse": return sorted({0, *range(40, p["lead"], 80), *range(p["lead"], p["hold"], 80)})
    lead, hold = p["lead"], p["hold"]
    pts = sorted({0, *range(80, lead, 80), lead, lead + 40, *range(lead + 120, hold, 80)})
    return [x for x in pts if x < hold]

if __name__ == "__main__":
    os.makedirs(OUT + "/audit45", exist_ok=True)
    frames, v, S = [], 900, 0
    for p in PLANS:
        start = p["tb"] - p["lead"]
        while v < start:
            du = min(80, start - v); frames.append(("run", v, du)); v += du; S += du
        if p["kind"] == "tell": S_TELL = S
        if p["kind"] == "eclipse": S_REL = S + p["lead"]
        if p["T"] is not None and (p["tier"] == "kill"): DEATH_S[p["T"]["id"]] = S + p["lead"]
        offs = offsets(p)
        for n, a in enumerate(offs): frames.append(("beat", (p, a), (offs[n + 1] if n + 1 < len(offs) else p["hold"]) - a)); S += frames[-1][2]
        v = max(v, p["tb"])
    stop = (end_t or END_MS) + 2600
    while v < stop: frames.append(("run", v, 80)); v += 80; S += 80
    print("beats", len(PLANS), "small", sum(p["tier"] == "small" for p in PLANS), "big", sum(p["tier"] in ("big", "kill", "spell", "eclipse") for p in PLANS),
          "frames", len(frames), "screen s", round(S / 1000, 1), flush=True)
    clip, durs, stamps, S = [], [], [], 0
    for n, (kind, arg, du) in enumerate(frames):
        if kind == "run": c, sh = frame_run(arg, S); tk = tick_at(arg); note = ""
        else: p, a = arg; c, sh = frame_beat(p, a, S); tk = tick_at(p["tb"]); note = caption(p)
        clip.append(compose(c, sh, max(0, min(LAST, tk)), note)); durs.append(du); stamps.append(S); S += du
        if n % 100 == 0: print("frame", n, flush=True)
    clip[0].save(f"{OUT}/big-moments-v1.webp", save_all=True, append_images=clip[1:], duration=durs, loop=0,
                 quality=68, method=6, minimize_size=True, allow_mixed=True)
    picks, nxt = [], 0
    for k, s in enumerate(stamps):
        if s >= nxt: picks.append(k); nxt = s - s % 200 + 200
    cw, ch, per = PW // 3, (PH + FTR) // 3, 24
    for s0 in range(0, len(picks), per):
        shx = Image.new("RGB", (4 * (cw + 4), 6 * (ch + 14)), (0, 0, 0)); dd = ImageDraw.Draw(shx)
        for r, k in enumerate(picks[s0:s0 + per]):
            x0, y0 = (r % 4) * (cw + 4), (r // 4) * (ch + 14)
            shx.paste(clip[k].resize((cw, ch)), (x0, y0 + 14)); dd.text((x0 + 4, y0 + 1), f"{stamps[k]} ms", fill=(255, 255, 0))
        shx.save(f"{OUT}/audit45/audit_{s0 // per:02d}.png")
    ph = clip[len(clip) // 2]; ph.resize((390, round(ph.height * 390 / ph.width))).save(f"{OUT}/audit45/phone.png")
    print("frames", len(clip), "webp MB", round(os.path.getsize(f"{OUT}/big-moments-v1.webp") / 1e6, 2), "size", clip[0].size)
