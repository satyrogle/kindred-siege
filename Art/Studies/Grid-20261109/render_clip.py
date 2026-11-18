"""Draws a rules 44 grid fight straight from a sim dump (GridDump.exe, one JSON line per tick) as an mp4.

Nothing here decides anything: every panel, step, swap, blow, cross guard, trap and kill comes from the dump. It is a plain
debug view in the Grid Class Shapes page's colours, not game art. Usage:
    python render_clip.py <dump.jsonl> <out.mp4> [ticks_per_frame]
"""
import json
import subprocess
import sys

from PIL import Image, ImageDraw, ImageFont

FFMPEG = r"C:/Users/jacob/Desktop/PR/dark-lattice-site/node_modules/ffmpeg-static/ffmpeg.exe"
W, H, FPS = 1280, 720, 30

INK, SHEET, LINE = (15, 11, 9), (26, 19, 15), (59, 45, 37)
BONE, MUTED, GOLD = (239, 223, 190), (173, 154, 134), (214, 170, 92)
OURS, THEIRS, ORANGE = (153, 190, 172), (218, 107, 81), (238, 154, 77)
OURPANEL, THEIRPANEL = (34, 48, 42), (53, 31, 26)

F = r"C:/Windows/Fonts/"
DISPLAY = ImageFont.truetype(F + "consolab.ttf", 30)
SMALLD = ImageFont.truetype(F + "consolab.ttf", 15)
TAG = ImageFont.truetype(F + "consolab.ttf", 17)
BODY = ImageFont.truetype(F + "segoeui.ttf", 17)
BODYS = ImageFont.truetype(F + "segoeui.ttf", 15)
NUM = ImageFont.truetype(F + "consolab.ttf", 18)

P, BOX = 112, 100
X_OURS, X_THEIRS, Y0 = 264, 680, 168
TAGS = {"Warden": "W", "Skirmisher": "SK", "Marksman": "MK", "Trapper": "TR", "Medic": "MD"}


def panel_xy(side, col, lane):
    """Centre of a panel: our back row on the far left, their back row on the far right, fronts at the middle line."""
    x0 = X_OURS + col * P if side == 0 else X_THEIRS + (2 - col) * P
    return x0 + BOX / 2, Y0 + lane * P + BOX / 2


def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def main():
    src, out = sys.argv[1], sys.argv[2]
    per = int(sys.argv[3]) if len(sys.argv) > 3 else 2
    rows = [json.loads(l) for l in open(src, encoding="utf-8")]
    head, ticks = rows[0], rows[1:]
    units = head["units"]
    idx = {u["id"]: i for i, u in enumerate(units)}

    # Names in words: "your Trapper", "their Warden 2" when a band has two.
    names = []
    for i, u in enumerate(units):
        same = [v for v in units if v["side"] == u["side"] and v["cls"] == u["cls"] and v["role"] == u["role"]]
        n = "" if len(same) == 1 else " " + str(same.index(u) + 1)
        who = "leader" if u["role"] != "Fighter" else u["cls"] + n
        names.append(("your " if u["side"] == 0 else "their ") + who)

    # Start positions, and every change of panel as a tween.
    start = ticks[0]["u"]
    pos_from = [tuple(start[i][:2]) for i in range(len(units))]
    tween = [None] * len(units)  # (from, to, frame started)
    flashes, ticker = [], []
    dead_at = [None] * len(units)
    fled_at = [None] * len(units)
    focus = [None, None]
    numbers = {}

    proc = subprocess.Popen([FFMPEG, "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W}x{H}",
                             "-r", str(FPS), "-i", "-", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", out],
                            stdin=subprocess.PIPE)

    def word_dir(side, a, b):
        dc, dl = b[0] - a[0], b[1] - a[1]
        if dc == 1:
            return "forward"
        if dc == -1:
            return "back"
        return "up" if dl == -1 else "down"

    def note(frame, text, colour):
        # The same line already on the list is not repeated.
        if any(x[1] == text for x in ticker):
            return
        ticker.insert(0, (frame, text, colour))
        del ticker[6:]

    intro = FPS  # one second on the starting panels
    frames = intro + (len(ticks) + per - 1) // per + int(FPS * 2.5)
    last = ticks[-1]
    ti = 0
    for frame in range(frames):
        # Advance the sim ticks this frame covers, reading their events.
        if frame >= intro:
            for _ in range(per):
                if ti >= len(ticks):
                    break
                row = ticks[ti]
                ti += 1
                for i, (c, l, hp, alive) in enumerate(row["u"]):
                    if c >= 0 and (c, l) != pos_from[i] and pos_from[i][0] >= 0:
                        cur = current_pos(i, frame, pos_from, tween, units)
                        tween[i] = (cur, panel_xy(units[i]["side"], c, l), frame)
                    pos_from[i] = (c, l)
                crossed = {}
                for kind, aside, actor, target, amount, detail in row["e"]:
                    a, t = idx.get(actor), idx.get(target)
                    if kind == "Defence" and detail == "cross" and a is not None and t is not None:
                        crossed[a] = t
                    if kind == "Hit" and a is not None and t is not None and detail not in ("trap", "bleed", "burn"):
                        # A blow the Warden's cross took: drawn down its lane to the fighter it was aimed at.
                        aimed = crossed.get(t) if detail.startswith("guarded") else None
                        flashes.append(("blow", frame, a, aimed if aimed is not None else t, amount))
                        add_number(numbers, t, amount, frame, BONE)
                    elif kind == "Miss" and a is not None and t is not None:
                        flashes.append(("miss", frame, a, t, 0))
                    elif kind == "Heal" and a is not None and t is not None:
                        flashes.append(("heal", frame, a, t, amount))
                        add_number(numbers, t, amount, frame, OURS, heal=True)
                    elif kind == "Defence" and detail == "cross" and a is not None and t is not None:
                        flashes.append(("cross", frame, a, t, 0))
                        note(frame, names[a] + " steps in front of the blow on " + names[t] + " (his cross)", GOLD)
                    elif kind == "TrapArmed" and a is not None:
                        note(frame, names[a] + " sets the trap on their front panel, " + ["top", "middle", "bottom"][max(0, row["trap"][aside])] + " lane", ORANGE)
                    elif kind == "TrapSprung" and t is not None:
                        flashes.append(("sprung", frame, a, t, 0))
                        note(frame, "the trap springs on " + names[t] + " standing there", ORANGE)
                    elif kind == "Step" and a is not None:
                        c, l = divmod(amount, 3)
                        if detail == "swap":
                            note(frame, names[a] + " trades panels to make room", MUTED)
                        elif detail == "room":
                            note(frame, names[a] + " steps forward to make room", MUTED)
                        elif detail == "fallback":
                            note(frame, names[a] + " falls back a row", MUTED)
                        elif units[a]["role"] != "Fighter":
                            note(frame, names[a] + " slides along the back row to find a target", BONE)
                        elif units[a]["cls"] == "Medic":
                            note(frame, names[a] + " steps toward a hurt ally", BONE)
                        elif units[a]["cls"] == "Warden":
                            note(frame, names[a] + " steps back to the centre", BONE)
                        else:
                            note(frame, names[a] + " has nobody in reach and steps one panel", BONE)
                    elif kind == "Kill" and t is not None:
                        dead_at[t] = frame
                        note(frame, names[t] + " falls", THEIRS if units[t]["side"] == 0 else OURS)
                    elif kind == "Snap" and detail == "fled" and a is not None:
                        fled_at[a] = frame
                        note(frame, names[a] + " flees", MUTED)
                    elif kind == "Directive" and aside == 0 and detail in ("FocusFire", "FallBack", "Hold", "Unleash"):
                        label = {"FocusFire": "Focus Fire", "FallBack": "Fall Back"}.get(detail, detail)
                        note(frame, "order: " + label + (" on " + names[t] if t is not None else ""), GOLD)
                        if detail == "FocusFire" and t is not None:
                            focus[0] = (t, frame)
        row = ticks[min(max(ti - 1, 0), len(ticks) - 1)]
        img = Image.new("RGB", (W, H), INK)
        d = ImageDraw.Draw(img, "RGBA")
        draw_frame(d, frame, row, units, names, pos_from, tween, flashes, ticker, dead_at, fled_at, head, ti, len(ticks), per, intro, last, numbers)
        proc.stdin.write(img.tobytes())
        if frame == intro + 40:
            img.save(out.replace(".mp4", "-still.png"))
    proc.stdin.close()
    proc.wait()


def add_number(numbers, t, amount, frame, colour, heal=False):
    """One running number per fighter: blows inside two thirds of a second add up, as the fight feel rules ask."""
    key = (t, heal)
    old = numbers.get(key)
    if old is not None and frame - old[1] <= 20:
        numbers[key] = (old[0] + amount, frame, colour)
    else:
        numbers[key] = (amount, frame, colour)


def current_pos(i, frame, pos_from, tween, units):
    if tween[i] is not None:
        a, b, f0 = tween[i]
        k = min(1.0, (frame - f0) / 6.0)
        if k < 1.0:
            k = k * k * (3 - 2 * k)
            return (a[0] + (b[0] - a[0]) * k, a[1] + (b[1] - a[1]) * k)
        return b
    c, l = pos_from[i]
    return panel_xy(units[i]["side"], c, l)


def draw_frame(d, frame, row, units, names, pos_from, tween, flashes, ticker, dead_at, fled_at, head, ti, total, per, intro, last, numbers):
    # Header.
    d.text((40, 26), "RULES 44  THE GRID", font=DISPLAY, fill=GOLD)
    d.text((40, 64), head["fight"] + ", a road fight straight from the sim, " + str(per) + "x speed", font=BODY, fill=MUTED)
    secs = max(0, ti) / 30.0
    d.text((W - 40, 30), "%d:%04.1f" % (int(secs // 60), secs % 60), font=DISPLAY, fill=BONE, anchor="ra")

    # Band titles and row labels.
    d.text((X_OURS + 1.5 * P - 6, Y0 - 52), "YOUR BAND", font=SMALLD, fill=OURS, anchor="ma")
    d.text((X_THEIRS + 1.5 * P - 6, Y0 - 52), "THEIR BAND", font=SMALLD, fill=THEIRS, anchor="ma")
    for c, label in enumerate(["BACK", "MIDDLE", "FRONT"]):
        d.text((X_OURS + c * P + BOX / 2, Y0 - 26), label, font=BODYS, fill=MUTED, anchor="ma")
        d.text((X_THEIRS + (2 - c) * P + BOX / 2, Y0 - 26), label, font=BODYS, fill=MUTED, anchor="ma")
    for l, label in enumerate(["top", "middle", "bottom"]):
        d.text((X_OURS - 16, Y0 + l * P + BOX / 2), label, font=BODYS, fill=MUTED, anchor="rm")
    d.line([(640, Y0 - 8), (640, Y0 + 3 * P)], fill=LINE, width=2)

    # Panels.
    for side in (0, 1):
        for c in range(3):
            for l in range(3):
                x, y = panel_xy(side, c, l)
                d.rounded_rectangle([x - BOX / 2, y - BOX / 2, x + BOX / 2, y + BOX / 2], radius=10,
                                    fill=OURPANEL if side == 0 else THEIRPANEL, outline=LINE, width=1)

    alive = [u[3] == 1 and fled_at[i] is None for i, u in enumerate(row["u"])]
    # The Warden's cross: the four panels touching a band Warden while another fighter of his stands.
    for i, u in enumerate(units):
        if not alive[i] or u["cls"] != "Warden" or u["role"] != "Fighter":
            continue
        others = [j for j, v in enumerate(units) if j != i and v["side"] == u["side"] and alive[j] and v["role"] == "Fighter"]
        if not others:
            continue
        c, l = pos_from[i]
        for dc, dl in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if 0 <= c + dc < 3 and 0 <= l + dl < 3:
                x, y = panel_xy(u["side"], c + dc, l + dl)
                d.rounded_rectangle([x - BOX / 2 + 4, y - BOX / 2 + 4, x + BOX / 2 - 4, y + BOX / 2 - 4], radius=8,
                                    outline=GOLD + (90,), width=2)

    # Traps: side s's trap sits on the other band's front panel of its lane.
    for s in (0, 1):
        lane = row["trap"][s]
        if lane >= 0:
            x, y = panel_xy(1 - s, 2, lane)
            jaw = [(x - 22, y + 30), (x - 12, y + 18), (x - 4, y + 30), (x + 4, y + 18), (x + 12, y + 30), (x + 22, y + 18)]
            d.line(jaw, fill=ORANGE, width=3)
            d.text((x, y + 36), "TRAP", font=SMALLD, fill=ORANGE, anchor="ma")

    # Units.
    for i, u in enumerate(units):
        c, l, hp, live = row["u"][i]
        if c < 0:
            continue
        x, y = current_pos(i, frame, pos_from, tween, units)
        y -= 8
        if not live or fled_at[i] is not None:
            gone = dead_at[i] if dead_at[i] is not None else fled_at[i]
            if gone is None or frame - gone > 36:
                continue
            k = (frame - gone) / 36.0
            col = mix(MUTED, INK, k)
            d.ellipse([x - 30, y - 30, x + 30, y + 30], outline=col, width=3)
            if dead_at[i] is not None:
                d.line([(x - 22, y - 22), (x + 22, y + 22)], fill=mix(THEIRS, INK, k), width=5)
                d.line([(x - 22, y + 22), (x + 22, y - 22)], fill=mix(THEIRS, INK, k), width=5)
            else:
                d.text((x, y), "fled", font=TAG, fill=col, anchor="mm")
            continue
        ring = GOLD if u["role"] != "Fighter" else (OURS if u["side"] == 0 else THEIRS)
        d.ellipse([x - 30, y - 30, x + 30, y + 30], fill=SHEET, outline=ring, width=4)
        d.text((x, y), "LDR" if u["role"] != "Fighter" else TAGS.get(u["cls"], u["cls"][:2].upper()), font=TAG, fill=BONE, anchor="mm")
        frac = max(0.0, min(1.0, hp / float(u["max"])))
        d.rectangle([x - 36, y + 36, x + 36, y + 42], fill=LINE)
        d.rectangle([x - 36, y + 36, x - 36 + 72 * frac, y + 42], fill=OURS if u["side"] == 0 else THEIRS)

    # Blows, misses, heals, cross guards and springs, each fading over a third of a second or so.
    keep = []
    for f in flashes:
        kind, f0, a, t, amount = f
        age = frame - f0
        life = 20 if kind in ("blow", "heal", "cross", "sprung") else 12
        if age > life:
            continue
        keep.append(f)
        k = 1 - age / float(life)
        ax, ay = current_pos(a, frame, pos_from, tween, units) if a is not None else (0, 0)
        tx, ty = current_pos(t, frame, pos_from, tween, units)
        ay -= 8
        ty -= 8
        if kind == "blow":
            colour = OURS if units[a]["side"] == 0 else THEIRS
            d.line([(ax, ay), (tx, ty)], fill=colour + (int(220 * k),), width=3)
        elif kind == "miss":
            d.line([(ax, ay), (tx, ty)], fill=MUTED + (int(150 * k),), width=1)
            d.text((tx + 18, ty - 30), "miss", font=BODYS, fill=MUTED + (int(255 * k),), anchor="mm")
        elif kind == "heal":
            d.line([(ax, ay), (tx, ty)], fill=OURS + (int(160 * k),), width=2)
        elif kind == "cross":
            d.line([(ax, ay), (tx, ty)], fill=GOLD + (int(230 * k),), width=4)
            d.ellipse([ax - 38, ay - 38, ax + 38, ay + 38], outline=GOLD + (int(255 * k),), width=4)
            d.text((ax, ay + 51), "CROSS", font=SMALLD, fill=GOLD + (int(255 * k),), anchor="mm")
        elif kind == "sprung":
            d.ellipse([tx - 40, ty - 40, tx + 40, ty + 40], outline=ORANGE + (int(255 * k),), width=5)
            d.text((tx, ty + 51), "SNARED", font=SMALLD, fill=ORANGE + (int(255 * k),), anchor="mm")
    flashes[:] = keep

    # One running number per fighter, rising and fading; heals to the left in green.
    for (t, heal), (amount, f0, colour) in list(numbers.items()):
        age = frame - f0
        if age > 24:
            del numbers[(t, heal)]
            continue
        k = 1 - age / 24.0
        tx, ty = current_pos(t, frame, pos_from, tween, units)
        x = tx - 30 if heal else tx + 30
        d.text((x, ty - 26 - 10 * (1 - k)), ("+" if heal else "") + str(amount), font=NUM, fill=colour + (int(255 * k),), anchor="mm")

    # The newest notes, top first.
    d.text((40, 548), "WHAT THE SIM DID", font=SMALLD, fill=MUTED)
    for n, (f0, text, colour) in enumerate(ticker[:5]):
        fade = 1.0 if n == 0 else max(0.35, 1 - n * 0.16)
        d.text((40, 574 + n * 24), text, font=BODY, fill=colour + (int(255 * fade),))
    # Key.
    kx = 860
    d.text((kx, 548), "KEY", font=SMALLD, fill=MUTED)
    lines = ["W Warden   SK Skirmisher   MK Marksman", "TR Trapper   MD Medic   LDR leader",
             "gold squares: the Warden's cross", "line: a blow, number: damage"]
    for n, text in enumerate(lines):
        d.text((kx, 574 + n * 24), text, font=BODYS, fill=MUTED)

    if frame < intro:
        d.text((640, Y0 + 3 * P + 16), "start: each band on its own 3 by 3 panels", font=BODY, fill=BONE, anchor="ma")
    if ti >= total:
        end = last["e"][-1] if last["e"] else None
        won = "your band wins" if end and end[1] == 0 else "their band wins"
        d.text((640, Y0 + 3 * P + 16), won + " (" + (end[5] if end else "") + ")", font=BODY, fill=GOLD, anchor="ma")


if __name__ == "__main__":
    main()
