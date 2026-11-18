"""One lane mock (Jacob, 9 Nov 2026: "its still missing game feel ... every unit comes with attack/def animations that
carry these fights", then "k yes go" to: one lane, one locked pair, every blow = its shape's panels light up, the
attacker's own move, the defender's own answer).

The middle lane of the camp 2 Stoneveil war party's start (DESIGN_LOCKS 1372 locked pairs, 1362 grid shapes): your
leader (a Marksman), Warden and Skirmisher against their Skirmisher and Warden. Hand written beats, not sim output.
Sprites are the game's own 6 Nov clips (Assets/Resources/Table42Review/Units) plus two dodge clips made today in
PixelLab from the same approved characters. Hit stack from the fight-feel skill: white frame, freeze, jitter, spark,
number. Usage: python lane.py <out.mp4>
"""
import json, os, subprocess, sys
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
UNITS = os.path.join(REPO, "Assets", "Resources", "Table42Review", "Units")
COURT = os.path.join(REPO, "Builds", "KindredSiege-Grid-Contact-Mock-20261109", "assets", "court.png")
FFMPEG = r"C:/Users/jacob/Desktop/PR/dark-lattice-site/node_modules/ffmpeg-static/ffmpeg.exe"
W, H, FPS, S = 1440, 810, 30, 2  # canvas, frames a second, sprite scale (whole pixels only)

GOLD, OURS, THEIRS, BONE, BLUE, INK = (214, 170, 92), (153, 190, 172), (218, 107, 81), (239, 223, 190), (141, 182, 224), (15, 11, 9)
F = r"C:/Windows/Fonts/"
BIG = ImageFont.truetype(F + "consolab.ttf", 44)
MID = ImageFont.truetype(F + "consolab.ttf", 30)
SMALL = ImageFont.truetype(F + "consolab.ttf", 20)


# ---------------------------------------------------------------- clips
class Clip:
    def __init__(self, frames, durs, ax, ay, hit=None):
        self.frames, self.durs, self.ax, self.ay, self.hit = frames, durs, ax, ay, hit
        self.length = sum(durs) / 1000.0

    def at(self, t, hold=True):
        t = max(0.0, t) * 1000.0
        acc = 0
        for i, d in enumerate(self.durs):
            acc += d
            if t < acc:
                return i
        return len(self.durs) - 1 if hold else int(t // (self.length * 1000)) % len(self.durs)

    def hit_time(self):
        return sum(self.durs[: self.hit]) / 1000.0 if self.hit is not None else 0.0


def load(name):
    d = json.load(open(os.path.join(UNITS, name + ".json")))
    sheet = Image.open(os.path.join(UNITS, name + ".png")).convert("RGBA")
    frames, durs = [], []
    for f in d["frames"]:
        r = f["frame"]
        frames.append(sheet.crop((r["x"], r["y"], r["x"] + r["w"], r["y"] + r["h"])))
        durs.append(f.get("duration", 80))
    return Clip(frames, durs, d["anchorX"], d["anchorY"], d.get("hitFrame"))


def load_folder(folder, ms):
    """A PixelLab clip saved as 0.png, 1.png, ...: feet anchor at the centre bottom of frame 0's body."""
    names = sorted([n for n in os.listdir(folder) if n.endswith(".png")], key=lambda n: int(n[:-4]))
    frames = [Image.open(os.path.join(folder, n)).convert("RGBA") for n in names]
    box = frames[0].split()[3].point(lambda v: 255 if v > 20 else 0).getbbox()
    return Clip(frames, [ms] * len(frames), (box[0] + box[2]) / 2.0, box[3])


C = {}
for side, tribe, face in ((0, "clarth", "se"), (1, "blood_rot", "sw")):
    for cls in ("warden", "skirmisher", "marksman"):
        for mv in ("idle", "attack", "attack2", "block", "hit", "power", "death"):
            n = f"{tribe}_{cls}_{face}_{mv}"
            if os.path.exists(os.path.join(UNITS, n + ".json")):
                C[(side, cls, mv)] = load(n)
for side, face in ((0, "se"), (1, "sw")):
    folder = os.path.join(HERE, "clips", f"skirmisher_hop_{face}")
    if os.path.isdir(folder):
        C[(side, "skirmisher", "dodge")] = load_folder(folder, 60)

# ---------------------------------------------------------------- the floor: each band's 3 by 3 panels
PW, GAP, X0 = 222, 44, 33
LANE_Y = {0: 470, 1: 610, 2: 750}   # panel centre lines; the camera sits on the middle lane
LANE_H = {0: 104, 1: 128, 2: 116}


def col_x(side, col):
    ix = col if side == 0 else 3 + (2 - col)
    return X0 + ix * PW + (GAP if ix >= 3 else 0) + PW / 2


def panel_box(side, col, lane):
    x, y, h = col_x(side, col), LANE_Y[lane], LANE_H[lane]
    return (x - PW / 2 + 8, y - h / 2, x + PW / 2 - 8, y + h / 2)


# ---------------------------------------------------------------- the cast: the middle lane at the start
class Actor:
    def __init__(self, key, side, cls, col, name, lead=False):
        self.key, self.side, self.cls, self.col, self.name, self.lead = key, side, cls, col, name, lead
        self.home = col_x(side, col)
        self.clips = []     # (t0, move)
        self.moves = []     # (t0, t1, x0, x1)
        self.freezes = []   # (t0, dur)
        self.flash = []     # t0 of a white frame
        self.dead_at = None
        self.gone_at = None

    def play(self, t0, mv):
        self.clips.append((t0, mv))

    def slide(self, t0, dur, x1):
        x0 = self.x(t0)
        self.moves.append((t0, t0 + dur, x0, x1))

    def x(self, t):
        x = self.home
        for a, b, x0, x1 in self.moves:
            if t >= b:
                x = x1
            elif t >= a:
                k = (t - a) / (b - a)
                k = k * k * (3 - 2 * k)
                x = x0 + (x1 - x0) * k
        return x

    def clock(self, t, since):
        """Animation time since a clip started, held still through freezes."""
        held = 0.0
        for a, d in self.freezes:
            if a >= since and a < t:
                held += min(d, t - a)
        return t - since - held

    def pose(self, t):
        mv, t0 = "idle", -99.0
        for a, m in self.clips:
            if a <= t:
                mv, t0 = m, a
        clip = C.get((self.side, self.cls, mv)) or C[(self.side, self.cls, "idle")]
        local = self.clock(t, t0)
        if mv == "idle":
            return clip, clip.at(local % clip.length)
        if mv != "death" and local >= clip.length:
            idle = C[(self.side, self.cls, "idle")]
            return idle, idle.at((local - clip.length) % idle.length)
        return clip, clip.at(local)


A = {
    "ldr": Actor("ldr", 0, "marksman", 0, "your leader", lead=True),
    "w": Actor("w", 0, "warden", 1, "your Warden"),
    "sk": Actor("sk", 0, "skirmisher", 2, "your Skirmisher"),
    "esk": Actor("esk", 1, "skirmisher", 2, "their Skirmisher"),
    "ew": Actor("ew", 1, "warden", 1, "their Warden"),
}
ORDER = ["ldr", "w", "ew", "sk", "esk"]  # drawn back to front: the pair on top

# ---------------------------------------------------------------- the beats
LIGHT, WORDS, BANNERS, TETHER, SHOTS, SHAKE, FRONT = [], [], [], [], [], [], []


def light(t0, panels, colour, dur=1.05):
    LIGHT.append((t0, dur, panels, colour))


def word(t0, actor, text, colour, big=True):
    # one running number per fighter: a number landing within half a second of the last adds to it
    if text.isdigit():
        for i in range(len(WORDS) - 1, -1, -1):
            w0, wa, wt, wc, wb = WORDS[i]
            if wa == actor and wt.isdigit() and 0 <= t0 - w0 <= 0.5:
                WORDS[i] = (t0, actor, str(int(wt) + int(text)), colour, big)
                return
    WORDS.append((t0, actor, text, colour, big))


def banner(t0, text, colour, dur=1.3):
    BANNERS.append((t0, dur, text, colour))


def freeze(t0, actors, dur):
    for a in actors:
        A[a].freezes.append((t0, dur))


def contact(atk, dfn, t_hit, kind, number=0, x_dfn=None, freeze_ms=0.09):
    """The answer the defender plays around the attacker's contact frame."""
    d = A[dfn]
    if kind == "HIT":
        d.play(t_hit, "hit")
        d.flash.append(t_hit)
        word(t_hit, dfn, str(number), BONE)
    elif kind in ("BLOCK", "CROSS"):
        d.play(t_hit - 0.18, "block")
        word(t_hit, dfn, kind + (" " + str(number) if number else ""), GOLD)
    elif kind == "PARRY":
        d.play(t_hit - 0.12, "block")
        word(t_hit, dfn, "PARRY", BLUE)
        freeze_ms = 0.16
    elif kind == "DODGE":
        d.play(t_hit - 0.2, "dodge")
        back = -45 if d.side == 0 else 45
        d.slide(t_hit - 0.2, 0.16, d.home + back)   # out of the blade's reach
        d.slide(t_hit + 0.35, 0.25, d.home)
        word(t_hit, dfn, "DODGE", BLUE)
        freeze_ms = 0.0
    if freeze_ms:
        freeze(t_hit, [atk, dfn], freeze_ms)


def dash_cut(t, atk, dfn, kind, number=0, mv="attack", reach_x=None, cross=None):
    """Lane dash: the Skirmisher darts down her lane to the first body, cuts, and dashes home."""
    a, d = A[atk], A[dfn]
    clip = C[(a.side, a.cls, mv)]
    face = 1 if a.side == 0 else -1
    target_x = reach_x if reach_x is not None else d.x(t)
    if cross:
        target_x = d.home + face * -70   # the Warden's shield, in front of the ward
    stand = target_x - face * 150
    a.slide(t, 0.16, stand)
    a.play(t + 0.12, mv)
    t_hit = t + 0.12 + clip.hit_time()
    if cross:
        w = A[cross]
        # he steps in front of the ward, toward the blow, and the ward gives him room
        w.slide(t_hit - 0.34, 0.22, target_x)
        d.slide(t_hit - 0.34, 0.22, d.home + face * 60)
        FRONT.append((t_hit - 0.34, t_hit + 0.85, cross))
        contact(atk, cross, t_hit, "CROSS", number)
        w.slide(t_hit + 0.6, 0.25, w.home)
        d.slide(t_hit + 0.6, 0.25, d.home)
    else:
        contact(atk, dfn, t_hit, kind, number)
    a.slide(t + 0.12 + clip.length - 0.05, 0.2, a.home)
    return t_hit


def lane_shot(t, atk, dfn, kind, number):
    a = A[atk]
    clip = C[(a.side, a.cls, "attack")]
    a.play(t, "attack")
    t_hit = t + clip.hit_time()
    SHOTS.append((t_hit, 0.09, atk, dfn))
    contact(atk, dfn, t_hit + 0.09, kind, number, freeze_ms=0.06)
    return t_hit


def pair(t, a, b, join=False):
    TETHER.append((t, a, b, join))


def unpair(t, a, b):
    TETHER.append((t, a, b, None))


def ours_mid(c):
    return [(0, c, 1)]


def theirs_mid(c):
    return [(1, c, 1)]


t = 0.4
pair(t, "sk", "esk")
pair(t, "ldr", "esk", join=True)
banner(t, "LOCKED PAIR · your Skirmisher and theirs. Your leader joins her beat.", BONE, 2.0)

# 1. Their Skirmisher is faster in the tie and goes first: LANE DASH at the first body in his lane. Your Warden's cross.
t = 2.3
banner(t, "THEIR SKIRMISHER · LANE DASH", THEIRS)
light(t, ours_mid(2), THEIRS)
light(t + 0.25, [(0, 1, 0), (0, 1, 2), (0, 0, 1), (0, 2, 1)], GOLD, 1.2)  # his cross: the four panels touching his
banner(t + 0.45, "YOUR WARDEN · GUARD THE CROSS", GOLD, 1.0)
dash_cut(t + 0.45, "esk", "sk", "CROSS", 20, cross="w")

# 2. Your Skirmisher answers, and your leader's shot lands on her beat.
t = 4.5
banner(t, "YOUR SKIRMISHER · LANE DASH, YOUR LEADER · LANE SHOT", OURS)
light(t, theirs_mid(2), OURS)
light(t, [(0, 1, 1), (0, 2, 1)], (120, 160, 150), 0.55)  # his shot passes over his own side
th = dash_cut(t + 0.45, "sk", "esk", "DODGE", 0)
lane_shot(th - 0.02, "ldr", "esk", "HIT", 14)

# 3. Their swing back: she parries.
t = 6.9
banner(t, "THEIR SKIRMISHER · LANE DASH", THEIRS)
light(t, ours_mid(2), THEIRS)
dash_cut(t + 0.45, "esk", "sk", "PARRY", 0, mv="attack")

# 4. She opens him up after the parry: her flurry, the shot on the beat, and he falls.
t = 9.0
banner(t, "YOUR SKIRMISHER · TWIN CUT", OURS)
light(t, theirs_mid(2), OURS)
th = dash_cut(t + 0.45, "sk", "esk", "HIT", 52, mv="power")
lane_shot(th + 0.05, "ldr", "esk", "HIT", 14)
A["esk"].dead_at = th + 0.35
A["esk"].play(th + 0.35, "death")
A["esk"].gone_at = th + 2.2
SHAKE.append((th + 0.35, 0.18))
unpair(th + 0.35, "sk", "esk")
unpair(th + 0.35, "ldr", "esk")

# 5. Her opponent fell: she takes the next free enemy in her lane, their Warden, and the leader's shots follow.
t = 12.2
pair(t, "sk", "ew")
pair(t, "ldr", "ew", join=True)
banner(t, "OPPONENT DOWN · she takes the next free enemy in her lane", BONE, 1.8)
t = 14.2
banner(t, "YOUR SKIRMISHER · LANE DASH", OURS)
light(t, theirs_mid(1) + theirs_mid(2), OURS)
dash_cut(t + 0.45, "sk", "ew", "BLOCK", 10)
t = 16.1
banner(t, "YOUR LEADER · LANE SHOT", OURS)
light(t, [(0, 1, 1), (0, 2, 1)] + theirs_mid(2) + theirs_mid(1), OURS)
lane_shot(t + 0.3, "ldr", "ew", "HIT", 12)
END = 18.0


# ---------------------------------------------------------------- drawing
court = Image.open(COURT).convert("RGB")
cw, ch = court.size
bg = court.crop((0, 240, W // S, 240 + H // S)).resize((W, H), Image.NEAREST)
bg = Image.blend(bg, Image.new("RGB", bg.size, INK), 0.28)


def draw_panels(d, t):
    for side in (0, 1):
        for col in range(3):
            for lane in (0, 1, 2):
                x0, y0, x1, y1 = panel_box(side, col, lane)
                base = OURS if side == 0 else THEIRS
                a = 55 if lane == 1 else 26
                d.rounded_rectangle([x0, y0, x1, y1], radius=10, fill=base + (a // 3,), outline=base + (a + 40,), width=2)
    for t0, dur, panels, colour in LIGHT:
        age = t - t0
        if age < 0 or age > dur:
            continue
        k = 1 - age / dur
        for side, col, lane in panels:
            x0, y0, x1, y1 = panel_box(side, col, lane)
            k = 1.0 if age < dur - 0.25 else (dur - age) / 0.25
            pulse = (0.7 if int(age * 12) % 2 else 1.0) if age < 0.35 else 1.0
            d.rounded_rectangle([x0, y0, x1, y1], radius=10, fill=colour + (int(165 * k * pulse),), outline=(255, 244, 214) + (int(235 * k),), width=5)


def feet_y():
    return LANE_Y[1] + 28


def draw_actor(img, t, a, shake):
    if a.gone_at is not None and t >= a.gone_at:
        return
    clip, i = a.pose(t)
    fr = clip.frames[i]
    if a.dead_at is not None and t > a.gone_at - 0.6:
        fade = max(0.0, (a.gone_at - t) / 0.6)
        alpha = fr.split()[3].point(lambda v: int(v * fade))
        fr = fr.copy(); fr.putalpha(alpha)
    if any(0 <= t - f < 1.0 / FPS + 0.001 for f in a.flash):
        white = Image.new("RGBA", fr.size, (255, 255, 255, 255)); white.putalpha(fr.split()[3]); fr = white
    big = fr.resize((fr.width * S, fr.height * S), Image.NEAREST)
    jx = 0
    for a0, dur in a.freezes:
        if a0 <= t < a0 + dur and any(abs(a0 - f) < 0.01 for f in a.flash):
            jx = 2 if int((t - a0) * 60) % 2 == 0 else -2
    x = int(round(a.x(t) - clip.ax * S + jx + shake[0]))
    y = int(round(feet_y() - clip.ay * S + shake[1]))
    # a soft shadow and a ring at the feet, gold for the leader
    sh = Image.new("RGBA", img.size, (0, 0, 0, 0)); sd = ImageDraw.Draw(sh)
    fx, fy = a.x(t) + shake[0], feet_y() + shake[1]
    sd.ellipse([fx - 62, fy - 14, fx + 62, fy + 14], fill=(0, 0, 0, 90))
    ring = GOLD if a.lead else (OURS if a.side == 0 else THEIRS)
    if a.dead_at is None or t < a.dead_at:
        sd.ellipse([fx - 58, fy - 12, fx + 58, fy + 12], outline=ring + (200,), width=3)
    img.alpha_composite(sh)
    img.alpha_composite(big, (x, y))


def draw_overlay(img, t, shake):
    d = ImageDraw.Draw(img)
    # locked pairs: a dashed tether at the feet
    live = {}
    for t0, a, b, join in TETHER:
        if t0 <= t:
            if join is None:
                live.pop((a, b), None)
            else:
                live[(a, b)] = join
    for (a, b), join in live.items():
        xa, xb, y = A[a].x(t), A[b].x(t), feet_y() + 34 + (10 if join else 0)
        col = (239, 223, 190, 120 if join else 220)
        step = 18
        x = min(xa, xb)
        while x < max(xa, xb):
            d.line([(x, y), (min(x + 10, max(xa, xb)), y)], fill=col, width=3 if not join else 2)
            x += step
    # lane shots: a hot tracer from the muzzle to the chest
    for t0, dur, a, b in SHOTS:
        if t0 <= t < t0 + dur + 0.08:
            k = min(1.0, (t - t0) / dur)
            x0, y0 = A[a].x(t) + 70, feet_y() - 120
            x1, y1 = A[b].x(t) - 20, feet_y() - 110
            xe = x0 + (x1 - x0) * k
            d.line([(x0 + (x1 - x0) * max(0, k - 0.35), y0), (xe, y0 + (y1 - y0) * k)], fill=(255, 236, 190, 255), width=5)
    # words over the defender, rising
    for t0, key, text, colour, big in WORDS:
        age = t - t0
        if age < 0 or age > 0.9:
            continue
        a = 255 if age < 0.6 else int(255 * (0.9 - age) / 0.3)
        x, y = A[key].x(t), feet_y() - 300 - age * 26
        if text.isdigit():   # numbers beside the head, words above it, so they never stack
            x, y = x + (70 if A[key].side == 1 else -70), feet_y() - 245 - age * 26
        font = BIG if big else MID
        d.text((x, y), text, font=font, fill=colour + (a,), anchor="mm", stroke_width=4, stroke_fill=INK + (a,))
    # the move name, like a battle scene's attack banner
    for t0, dur, text, colour in BANNERS:
        age = t - t0
        if age < 0 or age > dur:
            continue
        a = 255 if age < dur - 0.25 else int(255 * (dur - age) / 0.25)
        font = MID if MID.getlength(text) < 1080 else SMALL
        half = font.getlength(text) / 2 + 36
        d.rounded_rectangle([W / 2 - half, 64, W / 2 + half, 124], radius=6, fill=INK + (int(a * 0.75),))
        d.text((W / 2, 94), text, font=font, fill=colour + (a,), anchor="mm")
    d.text((24, 22), "ONE LANE · LOCKED PAIR · grid rules 44, mock, hand written beats", font=SMALL, fill=BONE + (200,))
    d.text((W - 24, 22), "%.1f s" % min(t, END), font=SMALL, fill=BONE + (200,), anchor="ra")
    labels = [("YOUR BAND", OURS, col_x(0, 1)), ("THEIR BAND", THEIRS, col_x(1, 1))]
    for text, colour, x in labels:
        d.text((x, LANE_Y[2] + 66), text, font=SMALL, fill=colour + (230,), anchor="mm")


def render(out):
    proc = subprocess.Popen([FFMPEG, "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W}x{H}",
                             "-r", str(FPS), "-i", "-", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "17", out],
                            stdin=subprocess.PIPE)
    n = int(END * FPS)
    for f in range(n):
        t = f / FPS
        shake = (0, 0)
        for t0, dur in SHAKE:
            if t0 <= t < t0 + dur:
                shake = ((4 if int(t * 60) % 2 else -4), (2 if int(t * 45) % 2 else -2))
        img = bg.convert("RGBA")
        layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
        draw_panels(ImageDraw.Draw(layer), t)
        img.alpha_composite(layer)
        order = list(ORDER)
        for t0, t1, key in FRONT:   # a Warden stepping into a blow is drawn in front
            if t0 <= t < t1:
                order.remove(key); order.append(key)
        for key in order:
            draw_actor(img, t, A[key], shake)
        ov = Image.new("RGBA", img.size, (0, 0, 0, 0))
        draw_overlay(ov, t, shake)
        img.alpha_composite(ov)
        rgb = img.convert("RGB")
        proc.stdin.write(rgb.tobytes())
        if f in (int(2.95 * FPS), int(5.0 * FPS), int(7.5 * FPS), int(9.75 * FPS)):
            rgb.save(out.replace(".mp4", "-%04.1f.png" % t))
    proc.stdin.close()
    proc.wait()


if __name__ == "__main__":
    missing = [k for k in ((0, "skirmisher", "dodge"), (1, "skirmisher", "dodge")) if k not in C]
    if missing:
        print("dodge clips missing, using block in their place:", missing)
        for k in missing:
            C[k] = C[(k[0], "skirmisher", "block")]
    render(sys.argv[1])
    print("rendered", sys.argv[1])
