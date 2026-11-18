import json
import math
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import stage_life as SL

SP, SR, RG = SL.SP, SL.SR, SL.RG

# stage_beasts.py [paint|cut|look|sheet] [STAGE ...]
# Jacob, 4 Nov 2026: the sun boats over the Bloodridge stages read as "ships on mountains". The stages should show a
# living world far off, the way the camp shows two tribes' towns at war. One beast a stage: the sand shark swimming
# the dune (1), a giant sea tortoise wading the far sea with a Thundercoast hut town on its shell (2), a hill sized
# scarab pushing the sun (3), a bone walker with a lantern house behind the night ridge (4), a sky serpent coiled round
# the eclipse sun (5). Each is painted at 4x inside its stage's own crop from a drawn start (the route that made the
# pilgrims, 4 Nov: "these are great"), cut with BiRefNet on his GPU and shrunk to 1x into the life folder.
# stage_life.render() calls draw() where it drew the boats; nothing on the fighters' floor changes.
# A beast plays on keys (t, dx, dy, sink): sink pushes it down behind its base line, so it rises out of the sand, the
# sea or the ridge and goes back in, throwing up a spray of that ground. A beast with two runs is painted twice from
# the same seed with only the moving part redrawn; the second pose takes the first one's pixels and mask everywhere
# else, so only that part moves.
#   paint  paints every run, then cuts      cut    cuts the painted runs again (look-beasts.png)
#   look   look-beasts.png only             sheet  frames from the rendered loops (look-beasts-life.png)
S = 4
N = SL.N
LIFE = SL.LIFE
WORK = LIFE / "beasts"  # starts, paintings, workflows and the big cuts; the 1x cuts and beasts.json sit in LIFE
BJ = LIFE / "beasts.json"
LW = np.array([0.299, 0.587, 0.114])
NEG = ", cartoon face, eyebrows, cute, chibi, mascot, smiling, angry cartoon eyes, comic, emoji, sticker"
SMOKE = np.array([[84.0, 84, 92], [124, 124, 132], [170, 170, 178]])
CACHE = {}

SHARK = ("the giant sand shark of the red dunes: a huge pale sand coloured shark with a dark ridged back, rows of long "
         "teeth and small black eyes, swimming through the sand of the dune as if it were water, a wild animal")
TORTOISE = ("a colossal sea tortoise wading through the far shallow sea, a whole nomad town of the Thundercoast on its "
            "high domed shell: small huts of oiled dark blue cloth, little brass domes and masts with blue flags, "
            "its old wrinkled head on a long neck, a wild animal")
SCARAB = ("a colossal sacred scarab beetle the size of a hill, its dark corroded bronze shell catching the light, "
          "pushing the sun across the sky with its hind legs, its head down, seen from the side, a wild animal")
WALKER = ("a colossal bone walker: the skeleton of a giant long necked beast walking behind the far ridge at night, "
          "pale moonlit bones, a small dark tomb house strapped on its back with two warm glowing lanterns hanging "
          "from its eaves")
SERPENT = ("a giant sky serpent of dark crimson and black scales coiled around the black eclipse sun, its jaws on "
           "the burning ring as if eating the sun, a few glowing ember scales, a wild beast")

SHARK_C = dict(sand=(214, 170, 120), dark=(84, 58, 56), belly=(236, 220, 190), teeth=(250, 246, 230), eye=(24, 18, 20))
TORT_C = dict(shell=(70, 78, 72), plate=(50, 56, 54), lit=(150, 160, 140), skin=(128, 118, 98), eye=(24, 20, 20),
              cloth=(40, 56, 96), brass=(200, 152, 72), mast=(92, 70, 52), flag=(60, 100, 190))
SCAR_C = dict(dark=(46, 34, 30), mid=(96, 70, 46), lit=(176, 128, 66), leg=(36, 26, 22), horn=(120, 90, 56))
WALK_C = dict(bone=(212, 210, 200), shade=(118, 124, 150), house=(56, 46, 60), roof=(40, 32, 46),
              glow=(252, 196, 96), rope=(84, 70, 64), socket=(20, 18, 30))
SERP_C = dict(dark=(46, 16, 22), lit=(176, 62, 46), ember=(252, 150, 64), head_dark=(46, 16, 22),
              head_lit=(176, 62, 46), head_mouth=(24, 6, 10), head_teeth=(240, 222, 200), head_eye=(255, 206, 96))


# ------------------------------------------------------------------ the drawn starts (4x crop pixels)

def frame(box, crop):
    """X(f), Y(f): a fraction of a 1x stage box in the 4x crop's pixels."""
    x0, y0 = (box[0] - crop[0]) * S, (box[1] - crop[1]) * S
    w, h = (box[2] - box[0]) * S, (box[3] - box[1]) * S
    return (lambda f: x0 + w * f), (lambda f: y0 + h * f)


def shark(d, name, X, Y, c, k):
    """shark1.py's fin and dive. The breach is redrawn side on, leaping up and left out of the dune with its jaws open:
    shark1's front on start came out a grinning mascot twice, the second time with the cartoon words in the negative
    (4 Nov), because the start drew a face."""
    if name == "fin":
        d.polygon([(X(0.2), Y(1.0)), (X(0.42), Y(0.0)), (X(0.62), Y(0.16)), (X(0.9), Y(1.0))], fill=c("dark"))
        d.polygon([(X(0.0), Y(1.0)), (X(0.14), Y(0.76)), (X(0.4), Y(0.86)), (X(0.72), Y(0.72)), (X(1.0), Y(0.82)),
                   (X(1.0), Y(1.0))], fill=c("sand"))
    elif name == "breach":
        bx, by = X(0.74), Y(1.0)  # where it leaves the sand
        ax, ay = X(0.06) - bx, Y(0.04) - by  # to the snout
        L = math.hypot(ax, ay)
        nx, ny = -ay / L, ax / L  # toward its back, up and right

        def P(*pts):
            return [(bx + u * ax + v * L * nx, by + u * ay + v * L * ny) for u, v in pts]
        d.polygon(P((0, 0.11), (0.3, 0.13), (0.55, 0.115), (0.75, 0.085), (0.9, 0.05), (1.0, 0.01), (0.97, -0.015),
                    (0.8, -0.03), (0.74, -0.1), (0.5, -0.115), (0.25, -0.11), (0, -0.1)), fill=c("sand"))
        d.polygon(P((0, 0.11), (0.3, 0.13), (0.55, 0.115), (0.75, 0.085), (0.9, 0.05), (0.97, 0.025), (0.9, 0.025),
                    (0.6, 0.02), (0.3, 0.0), (0, -0.01)), fill=c("dark"))  # the dark back
        d.polygon(P((0.36, 0.12), (0.44, 0.27), (0.56, 0.11)), fill=c("dark"))  # dorsal fin
        d.polygon(P((0.62, -0.1), (0.48, -0.26), (0.52, -0.1)), fill=c("dark"))  # pectoral fin
        d.polygon(P((0.8, -0.03), (0.99, -0.012), (0.95, -0.115), (0.78, -0.065)), fill=c("eye"))  # the open mouth
        d.polygon(P((0.78, -0.065), (0.95, -0.115), (0.92, -0.14), (0.74, -0.115)), fill=c("belly"))  # lower jaw
        for f in (0.83, 0.87, 0.91, 0.95):
            d.polygon(P((f, -0.022), (f + 0.012, -0.055), (f + 0.024, -0.02)), fill=c("teeth"))
        for i in range(4):
            u = 0.81 + i * 0.035
            v = -0.07 - (u - 0.8) * 0.3
            d.polygon(P((u, v), (u + 0.012, v + 0.03), (u + 0.024, v - 0.007)), fill=c("teeth"))
        for f in (0.68, 0.71, 0.74):  # gills
            d.line(P((f, -0.07), (f + 0.01, 0.03)), fill=c("dark"), width=S)
        ex, ey = P((0.86, 0.035))[0]
        d.ellipse((ex - 0.8 * S, ey - 0.8 * S, ex + 0.8 * S, ey + 0.8 * S), fill=c("eye"))
    else:
        d.polygon([(X(0.3), Y(1.0)), (X(0.42), Y(0.5)), (X(0.26), Y(0.0)), (X(0.5), Y(0.3)), (X(0.78), Y(0.0)),
                   (X(0.6), Y(0.5)), (X(0.74), Y(1.0))], fill=c("dark"))
        d.polygon([(X(0.36), Y(1.0)), (X(0.46), Y(0.56)), (X(0.56), Y(0.56)), (X(0.68), Y(1.0))], fill=c("sand"))


def tortoise(d, name, X, Y, c, k):
    """Facing right: the dome above the waterline (the box bottom), the head on its neck, four huts and two masts."""
    cf, rx, ry = 0.43, 0.35, 0.7

    def top(f):
        return 1.0 - ry * math.sqrt(max(0.0, 1 - ((f - cf) / rx) ** 2))
    d.pieslice((X(cf - rx), Y(1 - ry), X(cf + rx), Y(1 + ry)), 180, 360, fill=c("shell"))
    for j in (0.25, 0.5, 0.75):  # the shell's plates
        d.arc((X(cf - rx * j), Y(1 - ry * j), X(cf + rx * j), Y(1 + ry * j)), 200, 340, fill=c("plate"), width=S)
    d.arc((X(cf - rx), Y(1 - ry), X(cf + rx), Y(1 + ry)), 200, 290, fill=c("lit"), width=S)
    d.polygon([(X(0.74), Y(1.0)), (X(0.78), Y(0.72)), (X(0.86), Y(0.56)), (X(0.96), Y(0.5)), (X(1.0), Y(0.58)),
               (X(0.94), Y(0.66)), (X(0.88), Y(0.74)), (X(0.86), Y(1.0))], fill=c("skin"))
    d.ellipse((X(0.945), Y(0.53), X(0.965), Y(0.57)), fill=c("eye"))
    for f, w, hh in ((0.24, 0.08, 0.14), (0.35, 0.1, 0.2), (0.48, 0.1, 0.17), (0.6, 0.08, 0.12)):
        b = top(f + w / 2) + 0.04
        d.rectangle((X(f), Y(b - hh), X(f + w), Y(b)), fill=c("cloth"))
        d.ellipse((X(f + 0.005), Y(b - hh - 0.06), X(f + w - 0.005), Y(b - hh + 0.04)), fill=c("brass"))
    for f in (0.32, 0.56):
        d.line([(X(f), Y(top(f))), (X(f), Y(0.02))], fill=c("mast"), width=S)
        d.polygon([(X(f), Y(0.02)), (X(f + 0.06), Y(0.05)), (X(f), Y(0.09))], fill=c("flag"))


SCARAB_LEGS = {
    "push-a": [[(0.66, 0.42), (0.84, 0.26), (1.0, 0.18)], [(0.7, 0.52), (0.88, 0.5), (1.0, 0.42)],
               [(0.5, 0.66), (0.6, 0.84), (0.54, 1.0)], [(0.3, 0.72), (0.22, 0.9), (0.08, 1.0)]],
    "push-b": [[(0.66, 0.42), (0.86, 0.3), (1.0, 0.26)], [(0.7, 0.52), (0.86, 0.58), (1.0, 0.52)],
               [(0.5, 0.66), (0.46, 0.86), (0.36, 1.0)], [(0.3, 0.72), (0.3, 0.92), (0.22, 1.0)]]}


def scarab(d, name, X, Y, c, k):
    """Seen from the side facing left, head down, rear up toward the sun, hind legs on its edge; the two poses only
    move the legs."""
    for leg in SCARAB_LEGS[name]:
        d.line([(X(a), Y(b)) for a, b in leg], fill=c("leg"), width=2 * S, joint="curve")
    w = X(1) - X(0)
    ax, ay = math.cos(math.radians(-25)), math.sin(math.radians(-25))  # toward the rear
    nx, ny = -ay, ax

    def oval(f, rx, ry, a0=0.0, a1=360.0, n=32):
        ox, oy = X(0.5) + f * w * ax, Y(0.5) + f * w * ay
        return [(ox + rx * w * math.cos(a) * ax + ry * w * math.sin(a) * nx,
                 oy + rx * w * math.cos(a) * ay + ry * w * math.sin(a) * ny)
                for a in np.radians(np.linspace(a0, a1, n))]
    d.polygon(oval(0.0, 0.3, 0.2), fill=c("dark"))  # the wing cases
    d.line(oval(0.0, 0.22, 0.12, -150, -30, 12), fill=c("mid"), width=S)
    d.line(oval(0.0, 0.3, 0.2, -110, 20, 16), fill=c("lit"), width=S)  # the rim the sun lights
    d.polygon(oval(-0.36, 0.12, 0.15), fill=c("dark"))  # the thorax
    d.line(oval(-0.36, 0.12, 0.15, -120, -20, 8), fill=c("mid"), width=S)
    head = oval(-0.5, 0.08, 0.12)
    d.polygon(head, fill=c("dark"))
    hx, hy = X(0.5) - 0.5 * w * ax, Y(0.5) - 0.5 * w * ay
    d.polygon([(hx - 0.02 * w, hy - 0.04 * w), (hx - 0.12 * w, hy - 0.16 * w), (hx + 0.03 * w, hy - 0.06 * w)],
              fill=c("horn"))


def walker(d, name, X, Y, c, k):
    """Facing right: only the back, neck and skull above the ridge (the box bottom), the house on its back."""
    def P(pts):
        return [(X(a), Y(b)) for a, b in pts]
    for f in (0.12, 0.2, 0.28, 0.36, 0.44, 0.52):  # ribs, going down behind the ridge
        d.line(P([(f, 0.84), (f + 0.03, 0.94), (f + 0.01, 1.04)]), fill=c("shade"), width=S)
    d.line(P([(0.02, 0.9), (0.2, 0.84), (0.46, 0.82), (0.62, 0.84)]), fill=c("bone"), width=2 * S, joint="curve")
    d.line(P([(0.6, 0.84), (0.7, 0.64), (0.78, 0.42), (0.84, 0.24)]), fill=c("bone"), width=2 * S, joint="curve")
    for a, b in ((0.66, 0.72), (0.74, 0.52), (0.81, 0.33)):  # neck bones
        d.ellipse((X(a) - 1.5 * S, Y(b) - 1.5 * S, X(a) + 1.5 * S, Y(b) + 1.5 * S), fill=c("bone"))
    d.polygon(P([(0.78, 0.2), (0.86, 0.1), (0.96, 0.12), (1.0, 0.2), (0.96, 0.26), (0.84, 0.28)]), fill=c("bone"))
    d.ellipse((X(0.87) - S, Y(0.16) - S, X(0.87) + S, Y(0.16) + S), fill=c("socket"))
    d.line(P([(0.9, 0.28), (0.98, 0.3)]), fill=c("bone"), width=S)
    d.polygon(P([(0.12, 0.8), (0.12, 0.46), (0.5, 0.46), (0.5, 0.8)]), fill=c("house"))
    d.polygon(P([(0.08, 0.48), (0.31, 0.22), (0.54, 0.48)]), fill=c("roof"))
    d.rectangle((X(0.27), Y(0.56), X(0.35), Y(0.68)), fill=c("glow"))
    for f in (0.08, 0.54):  # lanterns on the eaves
        d.line(P([(f, 0.48), (f, 0.58)]), fill=c("rope"), width=S)
        d.ellipse((X(f) - 1.5 * S, Y(0.58), X(f) + 1.5 * S, Y(0.58) + 4 * S), fill=c("glow"))
    d.line(P([(0.16, 0.8), (0.2, 0.86)]), fill=c("rope"), width=S)
    d.line(P([(0.46, 0.8), (0.42, 0.86)]), fill=c("rope"), width=S)


def serpent(d, name, X, Y, c, k):
    """A coil round the ring, outside its glow band, the head at the upper right: on the rim (bite) or drawn back with
    its jaws wide (rear). Head colours are named head_ so the hole over the sun can spare the head."""
    cx, cy, rad = ring4(k)
    R, T = rad + 20 * S, 11 * S

    def at(a, r):
        return cx + r * math.cos(math.radians(a)), cy + r * math.sin(math.radians(a))
    # the body: a loose spiral from the neck (-25 deg) clockwise round the sun to the tail, swelling and thinning, so it
    # reads as a snake and not a frame (4 Nov: the even ring looked like a porthole)
    body = []
    for a in np.linspace(-25, 300, 90):
        f = (a + 25) / 325
        body.append((a, R + 4 * S - 6 * S * f + 1.5 * S * math.sin(math.radians(a * 3)), T * (1 - 0.8 * f ** 1.6)))
    for a, r, w in body:
        x, y = at(a, r)
        d.ellipse((x - w / 2, y - w / 2, x + w / 2, y + w / 2), fill=c("dark"))
    for a, r, w in body[4:-6]:  # the inner edge, lit by the ring
        x, y = at(a, r - w / 2 + 1.2 * S)
        d.ellipse((x - S, y - S, x + S, y + S), fill=c("lit"))
    for a, r, w in body[3:-12:7]:
        x, y = at(a + 3, r)
        d.ellipse((x - S, y - S, x + S, y + S), fill=c("ember"))
    bite = name == "bite"
    th = math.radians(315 if bite else 305)
    r0, L, gape = (R + 4 * S, 18 * S, 4 * S) if bite else (R + 18 * S, 16 * S, 12 * S)
    ux, uy = -math.cos(th), -math.sin(th)  # toward the sun
    vx, vy = -uy, ux
    hx, hy = cx + r0 * math.cos(th), cy + r0 * math.sin(th)

    def P(*pts):
        return [(hx + ux * u + vx * v, hy + uy * u + vy * v) for u, v in pts]
    nx, ny = at(-25, R + 4 * S)
    d.line([(nx, ny)] + P((-2 * S, 0)), fill=c("head_dark"), width=int(T * 0.85))
    d.ellipse((nx - T * 0.45, ny - T * 0.45, nx + T * 0.45, ny + T * 0.45), fill=c("head_dark"))
    d.polygon(P((-3 * S, 7 * S), (0.5 * L, 6 * S), (L, gape / 2 + 2 * S), (L, gape / 2), (4 * S, 0.5 * S),
                (-3 * S, -1 * S)), fill=c("head_dark"))  # the skull and upper jaw
    d.polygon(P((-2 * S, -6 * S), (0.5 * L, -5 * S), (0.92 * L, -gape / 2 - 1.5 * S), (0.88 * L, -gape / 2),
                (4 * S, -0.5 * S)), fill=c("head_dark"))  # the lower jaw
    d.polygon(P((4 * S, 0.5 * S), (L, gape / 2), (0.88 * L, -gape / 2), (4 * S, -0.5 * S)), fill=c("head_mouth"))
    for f in (0.4, 0.6, 0.8):
        g = 0.5 * S + (gape / 2 - 0.5 * S) * f
        d.polygon(P((f * L, g), (f * L + 1.2 * S, g - 2 * S), (f * L + 2.4 * S, g)), fill=c("head_teeth"))
        d.polygon(P((f * L, -g), (f * L + 1.2 * S, -g + 2 * S), (f * L + 2.4 * S, -g)), fill=c("head_teeth"))
    d.polygon(P((-3 * S, 6 * S), (-8 * S, 11 * S), (1 * S, 6.5 * S)), fill=c("head_dark"))  # horns behind the eye
    d.polygon(P((-3 * S, -5 * S), (-7 * S, -9 * S), (0, -5.5 * S)), fill=c("head_dark"))
    d.line(P((-1 * S, 6.5 * S), (0.6 * L, 5.5 * S)), fill=c("head_lit"), width=S)
    ex, ey = P((3 * S, 3.6 * S))[0]
    d.ellipse((ex - 1.4 * S, ey - 1.4 * S, ex + 1.4 * S, ey + 1.4 * S), fill=c("head_eye"))


# ------------------------------------------------------------------ the five beasts
# crop: the 1x stage rect painted at 4x. runs: one list of (pose, 1x box, what that pose is) per painting, all from
# the same seed. tracks: how each pose plays: pose or cycle [(pose, frames)], keys (t, dx, dy, sink) with sink "h" all
# under, base (the 1x line it sinks behind, default its own bottom), bob (period, px), wake, bursts (t, size) of its
# own ground thrown up at the base, smoke (1x points), glow (warm pixels flicker), legs (period, zones (col0, col1,
# row0, phase) of the 1x cut that swing from row0 down). ramp: a 1x box of the ground thrown up and how much it is
# lifted toward white. blank: 1x circles hidden from BiRefNet so the sun is not taken as the beast.
BEASTS = {
    "1": dict(label="sand shark", crop=(1016, 60, 1272, 220), seed=2026110421, beast=SHARK, shape=shark, pal=SHARK_C,
              scene="the long blood red dune far away lit pink and gold by the dawn, a small dark pyramid on its top, "
                    "rag tents and banners of the border camp in front of it",
              ramp=((1080, 150, 1140, 166), 0.0),
              runs=[[("fin", (1024, 158, 1064, 188), "only its tall dark dorsal fin and the ridge of its back cutting "
                                                     "through the dune's surface, a wave of sand curling off it"),
                     ("breach", (1064, 108, 1150, 170), "bursting up out of the dune, its head and open jaws full of "
                                                        "teeth rising above the ridge, sand pouring off its snout"),
                     ("dive", (1200, 126, 1256, 170), "diving back into the dune tail first, its crescent tail fin "
                                                      "up in the air, sand splashing")]],
              # the dive plays 26 px left of where it was painted, clear of the far skirmish at x 1238 to 1314
              tracks=[dict(pose="fin", keys=[(0, 0, 0, "h"), (6, 2, -1, 0), (40, 30, -10, 0), (48, 34, -11, "h")],
                           wake=True, bursts=[(42, 0.3)]),
                      dict(pose="breach", keys=[(56, 0, 0, "h"), (62, 0, 0, 0), (80, 0, -2, 0), (92, 0, 0, "h")],
                           bursts=[(56, 0.6), (84, 0.5)]),
                      dict(pose="dive", keys=[(100, -26, 0, "h"), (104, -26, 0, 0), (114, -26, 0, 0),
                                              (128, -26, 0, "h")],
                           bursts=[(100, 0.4), (118, 0.4)])],
              look=[24, 60, 86, 108]),
    "2": dict(label="sea tortoise town", crop=(1080, 40, 1336, 200), seed=2026110422, beast=TORTOISE, shape=tortoise,
              pal=TORT_C,
              scene="a bright morning sky with white haze over a calm turquoise sea far away, low grey hills in front "
                    "of the sea, a dark stone obelisk at the left edge",
              ramp=((1150, 146, 1270, 155), 0.35),
              runs=[[("tortoise", (1150, 104, 1270, 156), "facing right, wading through the sea, its high domed shell "
                                                          "and the town on it above the water, its head raised")]],
              tracks=[dict(pose="tortoise", base=156, bob=(24, 1), wake=True,
                           keys=[(0, -20, 0, "h"), (20, -18, 0, 0), (122, 14, 0, 0), (142, 16, 0, "h")],
                           bursts=[(2, 0.5), (126, 0.5)], smoke=[(1198, 108), (1214, 111)])],
              look=[10, 40, 100, 134]),
    "3": dict(label="sun scarab", crop=(1080, 0, 1336, 160), seed=2026110423, beast=SCARAB, shape=scarab, pal=SCAR_C,
              scene="a warm afternoon sky of pale gold haze and soft clouds, a big pale sun, the tops of two dark "
                    "stone pillars at the bottom",
              blank=[(1243, 67, 48)],
              runs=[[("push-a", (1120, 30, 1212, 112), "facing left, its head down, its hind legs pressed against the "
                                                       "sun's edge")],
                    [("push-b", (1120, 30, 1212, 112), "facing left, its head down, its hind legs pressed against the "
                                                       "sun's edge")]],
              tracks=[dict(cycle=[("push-a", 9), ("push-b", 9)], keys=[(0, 0, 0, 0), (N, 0, 0, 0)])],
              look=[0, 9, 72, 81]),
    "4": dict(label="bone walker", crop=(560, 20, 816, 180), seed=2026110424, beast=WALKER, shape=walker, pal=WALK_C,
              scene="a deep blue night sky with a few stars over a dark blue ridge far away, two dark red banners "
                    "hanging from wooden bars at the sides, a wooden canopy frame below",
              runs=[[("walker", (630, 62, 690, 113), "facing right, walking along behind the ridge, only its back, "
                                                     "neck and skull showing above it")]],
              # the painter gave it legs standing on the ridge line, so they swing: rear legs (cols 0 to 21 of the
              # 4 Nov cut, from row 38) against the front legs (cols 38 to 46, from row 42); a recut needs new zones
              tracks=[dict(pose="walker", base=113, bob=(16, 1), glow=True,
                           legs=(16, [(0, 22, 38, 0.0), (38, 47, 42, math.pi)]),
                           keys=[(0, -12, 0, "h"), (24, -6, 0, 0), (120, 72, 0, 0), (143, 78, 0, "h")])],
              look=[12, 40, 44, 136]),
    "5": dict(label="sky serpent", crop=(220, 0, 476, 160), seed=2026110425, beast=SERPENT, shape=serpent, pal=SERP_C,
              scene="a crimson eclipse sky, the black sun with a burning ring of fire, a red light beam crossing the sky",
              ring=(295, 10, 400, 112),
              runs=[[("bite", (296, 14, 396, 112), "its head at the burning ring, jaws open")],
                    [("rear", (296, 14, 396, 112), "its head at the burning ring, jaws open")]],
              tracks=[dict(cycle=[("bite", 54), ("rear", 18)], keys=[(0, 0, 0, 0), (N, 0, 0, 0)], glow=True)],
              look=[10, 58, 80, 130]),
}


# ------------------------------------------------------------------ painting and cutting

def still(k):
    key = ("still", k)
    if key not in CACHE:
        CACHE[key] = np.asarray(Image.open(SL.OUT / ("stage-%s.png" % k)).convert("RGB")).astype(np.float64)
    return CACHE[key]


def ring4(k):
    """The black disc's centre and radius in the 4x crop's pixels. Only near black counts: SL.ring_mask's mean under
    40 also takes the dark crimson sky above the sun (4 Nov: it put the centre 25 px too high)."""
    B = BEASTS[k]
    x0, y0, x1, y1 = B["ring"]
    ys, xs = np.nonzero(still(k)[y0:y1, x0:x1].mean(2) < 20)
    cx, cy, rad = x0 + xs.mean(), y0 + ys.mean(), math.sqrt(len(ys) / math.pi)
    return (cx - B["crop"][0]) * S, (cy - B["crop"][1]) * S, rad * S


def size4(k):
    c = BEASTS[k]["crop"]
    return (c[3] - c[1]) * S, (c[2] - c[0]) * S


def shape_mask(k, r, name=None, head=False):
    key = ("mask", k, r, name, head)
    if key not in CACHE:
        B = BEASTS[k]
        h, w = size4(k)
        im = Image.new("L", (w, h), 0)
        d = ImageDraw.Draw(im)
        col = (lambda n: 255 if n.startswith("head") else None) if head else (lambda n: 255)
        for nm, box, _ in B["runs"][r]:
            if name is None or nm == name:
                X, Y = frame(box, B["crop"])
                B["shape"](d, nm, X, Y, col, k)
        CACHE[key] = np.asarray(im) > 0
    return CACHE[key]


def dilate(m, r):
    """A square grown r pixels (box sums, no scipy)."""
    a = np.pad(m.astype(np.int32), r)
    c = np.pad(a.cumsum(0).cumsum(1), ((1, 0), (1, 0)))
    h, w = m.shape
    q = 2 * r + 1
    return (c[q:q + h, q:q + w] - c[:h, q:q + w] - c[q:q + h, :w] + c[:h, :w]) > 0


def circles(k):
    B = BEASTS[k]
    out = [((x - B["crop"][0]) * S, (y - B["crop"][1]) * S, rr * S) for x, y, rr in B.get("blank", ())]
    if "ring" in B:
        cx, cy, rad = ring4(k)
        out.append((cx, cy, rad + 12 * S))
    return out


def disc(k, cs):
    h, w = size4(k)
    yy, xx = np.mgrid[0:h, 0:w]
    m = np.zeros((h, w), bool)
    for cx, cy, rr in cs:
        m |= np.hypot(xx - cx, yy - cy) < rr
    return m


def hole(k, r):
    """The ring and its glow band, kept clear of the serpent so render()'s ring glow still shows, but not its head."""
    if "ring" not in BEASTS[k]:
        return np.zeros(size4(k), bool)
    return disc(k, circles(k)[-1:]) & ~dilate(shape_mask(k, r, head=True), 3 * S)


def make_init(k, r):
    B = BEASTS[k]
    base = Image.open(SL.OUT / ("stage-%s.png" % k)).convert("RGB").crop(B["crop"])
    init = base.resize((base.width * S, base.height * S), Image.LANCZOS)
    d = ImageDraw.Draw(init)
    for name, box, _ in B["runs"][r]:
        X, Y = frame(box, B["crop"])
        B["shape"](d, name, X, Y, lambda n: B["pal"][n], k)
    return init


def neg():
    n = SL.neg_patch()
    for w in (" giant,", " colossal,", " huge figures,", " close up,", " cropped,"):
        n = n.replace(w, "")
    return n + NEG


def prompt(k):
    """The same words for every run of a beast, so two runs only differ where their starts differ."""
    B, o = BEASTS[k], SR.STAGES[k]
    run = B["runs"][0]
    times = ", shown %d times in %d moments" % (len(run), len(run)) if len(run) > 1 else ""
    return json.dumps({
        "high_level_description": "A close piece of a pixel art battle stage background: " + B["scene"] + ". In it, "
                                  "far off: " + B["beast"] + times + ", in the same pixel art, colours and light as "
                                  "everything around it. " + SP.STYLE,
        "style_description": {"medium": "pixel_art", "art_style": RG.L3.ART,
                              "aesthetics": "one coherent pixel art scene, coherent pixel clusters, a clear readable "
                                            "creature, everything lit by the same light",
                              "lighting": o["light"], "color_palette": o["pal"]},
        "compositional_deconstruction": {"background": B["scene"],
                                         "elements": [{"type": "obj", "bbox": SL.pbb(b, B["crop"]),
                                                       "desc": B["beast"] + ", " + t} for _, b, t in run]}})


def paint(k):
    B = BEASTS[k]
    WORK.mkdir(parents=True, exist_ok=True)
    old, SP.OUT = SP.OUT, WORK
    try:
        for r in range(len(B["runs"])):
            init = make_init(k, r)
            init.save(WORK / ("beast-%s-init-%d.png" % (k, r)))
            raw = SP.run("beast-%s-%d" % (k, r), init, prompt(k), B["seed"], step=3, neg=neg())
            if raw.size != init.size:
                raw = raw.resize(init.size, Image.LANCZOS)
            raw.save(WORK / ("beast-%s-raw-%d.png" % (k, r)))
    finally:
        SP.OUT = old


def snap(m):
    """The box round a mask, on the 4x grid so the 1x sprite lands on whole stage pixels."""
    ys, xs = np.nonzero(m)
    return ys.min() // S * S, -(-(ys.max() + 1) // S) * S, xs.min() // S * S, -(-(xs.max() + 1) // S) * S


def cut(k):
    """BiRefNet on each pose's part of its painting (the sun hidden from it), kept near the drawn start, then one
    shrink for the whole beast so every pose shares its 12 colours. A group is kept if it touches the drawn start or
    is big (4 Nov: a loose speck in the margin above the scarab blinked on and off with its legs)."""
    B = BEASTS[k]
    pos = json.loads(BJ.read_text()) if BJ.exists() else {}
    imgs, full, parts = [], {}, []
    for r, run in enumerate(B["runs"]):
        img = np.asarray(Image.open(WORK / ("beast-%s-raw-%d.png" % (k, r))).convert("RGB")).astype(np.float64)
        region = None
        if r:
            region = dilate(shape_mask(k, r) != shape_mask(k, 0), 8 * S)
            img = np.where(region[..., None], img, imgs[0])
            Image.fromarray(img.astype(np.uint8)).save(WORK / ("beast-%s-mix-%d.png" % (k, r)))
        imgs.append(img)
        seen = img.copy()
        cs = circles(k)
        if cs:
            blank = disc(k, cs) & ~dilate(shape_mask(k, r), 2 * S)
            far = ~dilate(shape_mask(k, r), 6 * S) & ~blank
            seen[blank] = np.median(img[far], 0)
        for j, (name, _, _) in enumerate(run):
            drawn = shape_mask(k, r, name)
            near = dilate(drawn, 6 * S) & ~hole(k, r)
            y0, y1, x0, x1 = snap(near)
            sub = Image.fromarray(seen[y0:y1, x0:x1].astype(np.uint8))
            m = SL.cutout("beast-%s-%s" % (k, name), sub.resize((sub.width * 2, sub.height * 2), Image.LANCZOS))
            m = np.asarray(Image.fromarray((m * 255).astype(np.uint8)).resize(sub.size, Image.BOX)) > 127
            M = np.zeros(near.shape, bool)
            M[y0:y1, x0:x1] = m
            M &= near
            if r and (0, j) in full:
                M = np.where(region, M, full[(0, j)])
            lab, n = SL.components(M)
            if n == 0:
                print("nothing cut for", k, name, flush=True)
                continue
            sizes = np.bincount(lab.ravel(), minlength=n + 1)[1:]
            touch = np.bincount(lab[drawn], minlength=n + 1)[1:]
            keep = np.isin(lab, 1 + np.nonzero(((touch > 0) & (sizes >= 4 * S * S)) | (sizes >= 0.05 * sizes.max()))[0])
            full[(r, j)] = keep
            y0, y1, x0, x1 = snap(keep)
            parts.append((name, x0, y0, img[y0:y1, x0:x1], keep[y0:y1, x0:x1]))
            Image.fromarray(np.dstack([img[y0:y1, x0:x1], keep[y0:y1, x0:x1] * 255.0]).astype(np.uint8), "RGBA").save(
                WORK / ("beast-%s-%s-big.png" % (k, name)))
    if not parts:
        return
    hh = max(p[4].shape[0] for p in parts)
    gap = 2 * S
    canvas = np.zeros((hh, sum(p[4].shape[1] for p in parts) + gap * (len(parts) - 1), 4))
    x, spans = 0, []
    for name, px, py, sub, keep in parts:
        h, w = keep.shape
        canvas[:h, x:x + w, :3] = sub
        canvas[:h, x:x + w, 3] = keep * 255.0
        spans.append((name, x // S, w // S, h // S, px, py))
        x += w + gap
    col, mm = SL.shrink(canvas, scale=1.0 / S, ncol=12, outline=False)
    for name, sx, sw, sh, px, py in spans:
        rgba = np.dstack([col[:sh, sx:sx + sw], mm[:sh, sx:sx + sw] * 255.0]).clip(0, 255).astype(np.uint8)
        Image.fromarray(rgba, "RGBA").save(LIFE / ("beast-%s-%s.png" % (k, name)))
        pos["%s-%s" % (k, name)] = [int(B["crop"][0] + px // S), int(B["crop"][1] + py // S)]
    BJ.write_text(json.dumps(pos, indent=1))
    print("cut", k, {s[0]: pos["%s-%s" % (k, s[0])] for s in spans}, flush=True)


def look_cuts(ks):
    """Per stage and run: the painting at 1x size, then the stage crop with that run's cuts pasted, 2x: look-beasts.png."""
    pos = json.loads(BJ.read_text()) if BJ.exists() else {}
    rows = []
    for k in ks:
        B = BEASTS[k]
        cr = B["crop"]
        tiles = []
        for r, run in enumerate(B["runs"]):
            p = WORK / ("beast-%s-%s-%d.png" % (k, "mix" if r else "raw", r))
            if p.exists():
                tiles.append(Image.open(p).convert("RGB").resize((cr[2] - cr[0], cr[3] - cr[1]), Image.BOX))
            cv = Image.open(SL.OUT / ("stage-%s.png" % k)).convert("RGB")
            for name, _, _ in run:
                q = LIFE / ("beast-%s-%s.png" % (k, name))
                if q.exists() and "%s-%s" % (k, name) in pos:
                    im = Image.open(q).convert("RGBA")
                    cv.paste(im, tuple(pos["%s-%s" % (k, name)]), im)
            c = cv.crop(cr)
            tiles.append(c.resize((c.width * 2, c.height * 2), Image.NEAREST))
        rows.append(("%s  %s" % (k, B["label"]), tiles))
    _strip(rows, SL.OUT / "look-beasts.png")


def _strip(rows, dst):
    wid = max(sum(t.width + 8 for t in tiles) + 8 for _, tiles in rows)
    hgt = sum(max([t.height for t in tiles] + [20]) + 30 for _, tiles in rows)
    s = Image.new("RGB", (wid, hgt), (18, 14, 20))
    d = ImageDraw.Draw(s)
    y = 0
    for label, tiles in rows:
        d.text((8, y + 5), label, fill=(239, 223, 190), font=SL.font(16))
        x = 8
        for t in tiles:
            s.paste(t, (x, y + 26))
            x += t.width + 8
        y += max([t.height for t in tiles] + [20]) + 30
    s.save(dst)
    print(dst.name, s.size, flush=True)


# ------------------------------------------------------------------ playing them (stage_life.render calls draw)

def sprite(k, pose):
    key = ("sprite", k, pose)
    if key not in CACHE:
        p = LIFE / ("beast-%s-%s.png" % (k, pose))
        pos = json.loads(BJ.read_text()) if BJ.exists() else {}
        if not p.exists() or "%s-%s" % (k, pose) not in pos:
            print("no beast", k, pose, "cut yet", flush=True)
            CACHE[key] = None
        else:
            a = np.asarray(Image.open(p).convert("RGBA")).astype(np.float64)
            rgb, al = a[..., :3], a[..., 3] > 127
            r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
            warm = al & (r > 180) & (g > 100) & (b < 150) & (r - b > 70)
            CACHE[key] = (rgb, al, tuple(pos["%s-%s" % (k, pose)]), warm)
    return CACHE[key]


def pose_at(tr, t):
    if "pose" in tr:
        return tr["pose"]
    u = t % sum(n for _, n in tr["cycle"])
    for name, n in tr["cycle"]:
        if u < n:
            return name
        u -= n


def key_at(keys, t, hv):
    """(dx, dy, sink) at frame t, None outside the keys; sink "h" is hv, the height above the base line."""
    if not keys[0][0] <= t <= keys[-1][0]:
        return None
    for a, b in zip(keys, keys[1:]):
        if a[0] <= t <= b[0]:
            f = (t - a[0]) / max(1, b[0] - a[0])
            sa, sb = (hv if a[3] == "h" else a[3]), (hv if b[3] == "h" else b[3])
            return a[1] + (b[1] - a[1]) * f, a[2] + (b[2] - a[2]) * f, sa + (sb - sa) * f


def ground(k):
    """Three tones of the ground the beast throws up, from the still itself (lifted toward white for sea foam)."""
    key = ("ground", k)
    if key not in CACHE:
        (x0, y0, x1, y1), lift = BEASTS[k]["ramp"]
        a = still(k)[y0:y1, x0:x1].reshape(-1, 3)
        a = a[np.argsort(a @ LW)]
        rp = np.array([q.mean(0) for q in np.array_split(a, 3)])
        rp[2] = rp[2] * 0.75 + 255 * 0.25
        CACHE[key] = rp * (1 - lift) + 255 * lift
    return CACHE[key]


def puff(scale, ramp):
    """The camp's smoke puff, shrunk, in a ramp's three tones."""
    key = ("puff", scale, tuple(np.round(ramp).ravel()))
    if key not in CACHE:
        fx = SL.fxload("ks-camp-smoke-puff", scale)
        px = np.vstack([rgb[al] for rgb, al in fx["frames"] if al.any()])
        qs = np.percentile(px @ LW, [33, 66])
        CACHE[key] = dict(frames=[(ramp[np.digitize(rgb @ LW, qs)], al) for rgb, al in fx["frames"]],
                          ox=fx["ox"], oy=fx["oy"])
    return CACHE[key]


def swing(rgb, al, legs, t):
    """Legs that scissor: each zone's rows from row0 down slide sideways, the feet most (4 Nov: the walker's painted
    legs stood still while it crossed 90 px)."""
    period, zones = legs
    rgb, al = rgb.copy(), al.copy()
    src, sal = rgb.copy(), al.copy()
    h, w = al.shape
    for c0, c1, r0, ph in zones:
        for r in range(r0, h):
            s = int(round(2 * (r - r0 + 1) / max(1, h - r0) * math.sin(2 * math.pi * t / period + ph)))
            if not s:
                continue
            al[r, c0:c1] = False
            lo, hi = max(0, c0 + s), min(w, c1 + s)
            m = sal[r, lo - s:hi - s]
            al[r, lo:hi] |= m
            rgb[r, lo:hi][m] = src[r, lo - s:hi - s][m]
    return rgb, al


def spray(cv, gr, x0, x1, base, u, seed, size):
    """Ground thrown up in single pixels that arc and fall back (4 Nov: shrunk smoke puffs read as white dots)."""
    rng = np.random.default_rng(seed)
    for i in range(max(6, int(36 * size))):
        sx, vx = rng.uniform(x0, x1), rng.uniform(-0.8, 0.8)
        vy, wait = rng.uniform(-2.2, -1.2) * (1 + 2 * size), int(rng.integers(0, 4))
        v = u - wait
        if 0 <= v < 14:
            y = base + vy * v + 0.3 * v * v
            if y <= base:
                SL.dot(cv, sx + vx * v, y, gr[2] if (i + v // 3) % 3 == 0 else gr[1])


def place(k, tr, t):
    """Where a track's pose is at frame t: (sprite, x, y, clip line, sink, hv, dx, dy, bob) or None."""
    sp = sprite(k, pose_at(tr, t))
    if sp is None:
        return None
    rgb, al, (px, py), _ = sp
    base0 = tr.get("base", py + al.shape[0])
    st = key_at(tr["keys"], t, base0 - py)
    if st is None:
        return None
    dx, dy, sink = st
    bob = tr["bob"][1] if "bob" in tr and (t // max(1, tr["bob"][0] // 2)) % 2 else 0
    return sp, int(round(px + dx)), int(round(py + dy + sink - bob)), int(round(base0 + dy)), sink, base0 - py, dx, \
        dy, bob


def draw(cv, k, t):
    """Every beast track of stage k on frame t (cv is the frame, float, 1x)."""
    B = BEASTS.get(k)
    if B is None:
        return
    for n, tr in enumerate(B["tracks"]):
        at = place(k, tr, t)
        if at is not None:
            (rgb, al, (px, py), warm), x, y, clip, sink, hv, dx, dy, bob = at
            rows = min(clip - y, al.shape[0])  # rounding can put the clip a row past the sprite
            if sink < hv and rows > 0:
                c, a = rgb, al
                if "legs" in tr:
                    c, a = swing(c, a, tr["legs"], t)
                c, a = c[:rows], a[:rows]
                if tr.get("glow") and warm[:rows].any():
                    c = c.copy()
                    yy, xx = np.nonzero(warm[:rows])
                    c[yy, xx] = np.clip(c[yy, xx] * (1 + 0.2 * np.sin(2 * math.pi * 5 * t / N + (yy * 7 + xx * 3) % 6.3))
                                        [:, None], 0, 255)
                SL.blit(cv, c, a, x, y)
                if tr.get("wake"):  # one soft broken row just under it, drifting
                    gr = ground(k)
                    cols = np.nonzero(a[rows - 1])[0]
                    if len(cols):
                        for cx in range(cols.min() - 1, cols.max() + 2):
                            u = (cx + t // 3) % 4
                            if u < 2:
                                SL.dot(cv, x + cx, y + rows, gr[2] if u == 0 else gr[1])
                for j, (sx, sy) in enumerate(tr.get("smoke", ()) if sink < hv * 0.5 else ()):
                    fx = puff(0.25, SMOKE)
                    for i in range(3):
                        v = (t + j * 11 + i * 16) % 48
                        fr, fa = fx["frames"][min(len(fx["frames"]) - 1, v // 5)]
                        SL.blit(cv, fr, fa, sx + dx + v * 0.12 + 1.5 * math.sin(v / 6 + j) - fx["ox"],
                                sy + dy - bob - v * 0.4 - fx["oy"])
        for t0, size in tr.get("bursts", ()):
            u = (t - t0) % N
            if u >= 20:
                continue
            at0 = place(k, tr, min(max(t0, tr["keys"][0][0]), tr["keys"][-1][0]))
            if at0 is not None:
                al0, x0, clip0 = at0[0][1], at0[1], at0[3]
                spray(cv, ground(k), x0, x0 + al0.shape[1], clip0, u, int(k) * 1000 + n * 100 + t0, size)


# ------------------------------------------------------------------ the look sheet from the rendered loops

def view(k, t, tw, th):
    boxes = []
    for tr in BEASTS[k]["tracks"]:
        at = place(k, tr, t)
        if at is not None and at[4] < at[5]:
            al = at[0][1]
            boxes.append((at[1], at[2], at[1] + al.shape[1], at[3]))
    if not boxes:
        sp = sprite(k, pose_at(BEASTS[k]["tracks"][0], 0))
        if sp is None:
            return 0, 0
        boxes = [(sp[2][0], sp[2][1], sp[2][0] + sp[1].shape[1], sp[2][1] + sp[1].shape[0])]
    cx = (min(b[0] for b in boxes) + max(b[2] for b in boxes)) / 2
    cy = (min(b[1] for b in boxes) + max(b[3] for b in boxes)) / 2
    return int(min(max(0, cx - tw / 2), SL.W - tw)), int(min(max(0, cy - th / 2), SL.TOP - th))


def sheet(ks, tw=170, th=100, sc=2):
    """Four moments of each beast, cut from the rendered stage-k-life.mp4 (its first loop), 2x: look-beasts-life.png."""
    rows = []
    for k in ks:
        B = BEASTS[k]
        want, got = set(B["look"]), {}
        n = SL.W * SL.H * 3
        p = subprocess.Popen([SL.FF, "-v", "error", "-i", str(SL.OUT / ("stage-%s-life.mp4" % k)), "-vf",
                              "scale=%d:%d:flags=neighbor" % (SL.W, SL.H), "-frames:v", str(N), "-f", "rawvideo",
                              "-pix_fmt", "rgb24", "-"], stdout=subprocess.PIPE)
        for t in range(N):
            buf = p.stdout.read(n)
            if len(buf) < n:
                break
            if t in want:
                got[t] = np.frombuffer(buf, np.uint8).reshape(SL.H, SL.W, 3)
        p.stdout.close()
        p.wait()
        tiles = []
        for t in B["look"]:
            if t in got:
                x0, y0 = view(k, t, tw, th)
                tiles.append(Image.fromarray(got[t][y0:y0 + th, x0:x0 + tw]).resize((tw * sc, th * sc), Image.NEAREST))
        rows.append(("%s  %s, %s   frames %s of %d (12 fps)" % (k, SR.STAGES[k]["name"], B["label"],
                                                                ", ".join(map(str, B["look"])), N), tiles))
    _strip(rows, SL.OUT / "look-beasts-life.png")


if __name__ == "__main__":
    args = sys.argv[1:]
    mode = args[0] if args and args[0] in ("paint", "cut", "look", "sheet") else "look"
    ks = [a for a in args if a in BEASTS] or sorted(BEASTS)
    LIFE.mkdir(parents=True, exist_ok=True)
    if mode == "paint":
        for k in ks:
            paint(k)
    if mode in ("paint", "cut"):
        for k in ks:
            cut(k)
    if mode in ("paint", "cut", "look"):
        look_cuts(ks)
    if mode == "sheet":
        sheet(ks)
