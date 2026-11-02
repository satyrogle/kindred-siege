import json
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

# stops_build.py <take> : turn one raw stop 1 take into the new Bloodridge strip and its review files.
# 1. Snap the new part to the colours the strip already uses (one master palette, no fresh colours).
# 2. Cut the join along the path where the new pixels and Codex's grove pixels already agree best, inside the 64 px
#    overlap the model repainted, then keep her pixels exactly from there on.
# 3. Battle mocks at 1440x900 inside the court screen's own frame, with real fighters at native size.
# 4. The walk from stop 1 to stop 2 as a video, whole pixel steps.
REPO = Path("C:/Users/jacob/KindredSiege-Clean")
STUDY = REPO / "Art/Studies/Bloodridge-Stops-20261020"
STRIP = REPO / "Art/Studies/Bloodridge-Root-Crypt-20261019/panorama.png"
SCREEN = REPO / "Art/Studies/Bloodridge-Court-Refinement-20261013/battle-screen.png"
ART = REPO / "Assets/Resources/CampaignArt"
FF = "C:/Users/jacob/Desktop/PR/dark-lattice-site/node_modules/ffmpeg-static/ffmpeg.exe"
NEW, OLD_X, KEEP, VIEW = 1664, 688, 1728, (1376, 768)
STAGE_AT = (32, 66)
# Stops in the new strip's world x. Stop 2 is Codex's x 400 view, now framed by new art on its left.
STOPS = {"stop1": 0, "stop2": 1376, "stop3": 1400 - OLD_X + NEW, "stop4": 2752 - OLD_X + NEW}
# Feet in screen pixels, copied from the court screen's own formation. Ours face SE, theirs SW.
OURS = [("cult_zealot_se_idle", 420, 420, False), ("cult_shieldspear_se_idle", 600, 415, False),
        ("cult_preacher_se_idle", 420, 565, False), ("cult_tank_se_idle", 420, 700, False),
        ("cult_raider_se_idle", 590, 705, False)]
THEIRS = [("bloodridge_champion_sw_idle", 840, 420, False), ("bloodridge_shaman_sw_idle", 1020, 420, False),
          ("blood_rot_warden_se_idle", 840, 565, True), ("blood_rot_skirmisher_se_idle", 1020, 535, True),
          ("bloodridge_chief_sw_idle", 1160, 625, False), ("blood_rot_marksman_se_idle", 835, 700, True)]


def palette_snap(img, colours):
    a = np.asarray(img.convert("RGB")).reshape(-1, 3).astype(np.int32)
    pal = colours.astype(np.int32)
    out = np.empty_like(a)
    for i in range(0, len(a), 65536):
        chunk = a[i:i + 65536]
        d = ((chunk[:, None, :] - pal[None, :, :]) ** 2).sum(2)
        out[i:i + 65536] = pal[d.argmin(1)]
    return Image.fromarray(out.reshape(img.size[1], img.size[0], 3).astype(np.uint8))


def despeckle(img):
    """Lone pixels unlike all four neighbours take the colour most of their eight neighbours share."""
    a = np.asarray(img).astype(np.int32)
    key = (a[..., 0] << 16) | (a[..., 1] << 8) | a[..., 2]
    h, w = key.shape
    p = np.pad(key, 1, mode="edge")
    n4 = [p[0:h, 1:w + 1], p[2:h + 2, 1:w + 1], p[1:h + 1, 0:w], p[1:h + 1, 2:w + 2]]
    lone = np.all([key != n for n in n4], axis=0)
    n8 = n4 + [p[0:h, 0:w], p[0:h, 2:w + 2], p[2:h + 2, 0:w], p[2:h + 2, 2:w + 2]]
    stack = np.stack(n8, -1)
    fixed = key.copy()
    ys, xs = np.nonzero(lone)
    changed = 0
    for y, x in zip(ys, xs):
        vals, counts = np.unique(stack[y, x], return_counts=True)
        if counts.max() >= 5:
            fixed[y, x] = vals[counts.argmax()]
            changed += 1
    out = np.stack([(fixed >> 16) & 255, (fixed >> 8) & 255, fixed & 255], -1).astype(np.uint8)
    return Image.fromarray(out), changed


def seam(gen, old):
    """Per row, the column inside the overlap where the cut runs; a connected path of least colour difference."""
    g = np.asarray(gen).astype(np.int32)
    o = np.asarray(old).astype(np.int32)
    cost = np.abs(g - o).sum(2).astype(np.float64)
    h, w = cost.shape
    acc = cost.copy()
    back = np.zeros((h, w), np.int32)
    for y in range(1, h):
        for x in range(w):
            lo, hi = max(0, x - 1), min(w, x + 2)
            k = lo + int(np.argmin(acc[y - 1, lo:hi]))
            back[y, x] = k
            acc[y, x] += acc[y - 1, k]
    path = np.zeros(h, np.int32)
    path[-1] = int(np.argmin(acc[-1]))
    for y in range(h - 1, 0, -1):
        path[y - 1] = back[y, path[y]]
    return path, cost


def run_lengths(img):
    a = np.asarray(img.convert("RGB")).astype(np.int32)
    key = (a[..., 0] << 16) | (a[..., 1] << 8) | a[..., 2]
    same = (key[:, 1:] == key[:, :-1]).mean()
    return round(1.0 / max(1e-6, 1.0 - same), 2)


def build(take):
    raw = Image.open(STUDY / (take + "-raw.png")).convert("RGB")
    strip = Image.open(STRIP).convert("RGB")
    colours = np.unique(np.asarray(strip).reshape(-1, 3), axis=0)
    snapped = palette_snap(raw.crop((0, 0, KEEP, 768)), colours)
    snapped, lone = despeckle(snapped)
    band_old = strip.crop((OLD_X, 0, OLD_X + (KEEP - NEW), 768))
    path, cost = seam(snapped.crop((NEW, 0, KEEP, 768)), band_old)
    world = Image.new("RGB", (NEW + strip.width - OLD_X, 768))
    world.paste(strip.crop((OLD_X, 0, strip.width, 768)), (NEW, 0))
    left = np.asarray(snapped)
    w = np.asarray(world).copy()
    for y in range(768):
        w[y, :NEW + path[y]] = left[y, :NEW + path[y]]
    world = Image.fromarray(w)
    world.save(STUDY / (take + "-strip.png"))
    seam_cost = float(np.mean([cost[y, path[y]] for y in range(768)]))
    report = {"take": take, "strip": [world.width, 768], "colours_used": int(len(np.unique(w.reshape(-1, 3), axis=0))),
              "strip_palette": int(len(colours)), "lone_pixels_fixed": lone,
              "seam_mean_rgb_diff": round(seam_cost, 1), "seam_x_range": [int(NEW + path.min()), int(NEW + path.max())],
              "run_length_new": run_lengths(world.crop((0, 0, 1376, 768))),
              "run_length_grove": run_lengths(strip.crop((688, 0, 2064, 768))),
              "run_length_court": run_lengths(strip.crop((2752, 0, 4128, 768))), "stops": STOPS}
    (STUDY / (take + "-report.json")).write_text(json.dumps(report, indent=1))
    print(json.dumps(report))
    return world


def sprite(key, flip):
    j = json.loads((ART / (key + ".json")).read_text())
    f = j["frames"][0]["frame"]
    im = Image.open(ART / (key + ".png")).convert("RGBA").crop((f["x"], f["y"], f["x"] + f["w"], f["y"] + f["h"]))
    ax, ay = j["anchorX"], j["anchorY"]
    if flip:
        im = im.transpose(Image.FLIP_LEFT_RIGHT)
        ax = f["w"] - ax
    return im, int(round(ax)), int(round(ay))


HUD_ROWS = 79  # the court screen's top bar border runs down to y 79, over the stage's top 13 rows


def framed(screen, stage_rgb):
    out = screen.copy()
    out.paste(stage_rgb, STAGE_AT)
    out.paste(screen.crop((0, 0, 1440, HUD_ROWS)), (0, 0))
    return out


def mock(stage, fighters=True):
    screen = Image.open(SCREEN).convert("RGB")
    st = stage.convert("RGB").copy()
    if fighters:
        shade = Image.new("RGBA", st.size, (0, 0, 0, 0))
        d = ImageDraw.Draw(shade)
        for key, fx, fy, flip in OURS + THEIRS:
            x, y = fx - STAGE_AT[0], fy - STAGE_AT[1]
            d.ellipse((x - 30, y - 9, x + 30, y + 5), fill=(20, 12, 16, 90))
        st = Image.alpha_composite(st.convert("RGBA"), shade)
        for key, fx, fy, flip in sorted(OURS + THEIRS, key=lambda t: t[2]):
            im, ax, ay = sprite(key, flip)
            st.alpha_composite(im, (fx - STAGE_AT[0] - ax, fy - STAGE_AT[1] - ay))
    return framed(screen, st.convert("RGB"))


def label(img, text):
    d = ImageDraw.Draw(img)
    font = ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf", 26)
    d.rectangle((0, 0, img.width, 40), fill=(18, 14, 20))
    d.text((14, 5), text, font=font, fill=(236, 230, 222))
    return img


def review(take):
    world = Image.open(STUDY / (take + "-strip.png")).convert("RGB")
    court_screen = Image.open(SCREEN).convert("RGB")
    s1 = mock(world.crop((STOPS["stop1"], 0, STOPS["stop1"] + 1376, 768)))
    s2 = mock(world.crop((STOPS["stop2"], 0, STOPS["stop2"] + 1376, 768)))
    s1.save(STUDY / (take + "-stop1-battle.png"))
    s2.save(STUDY / (take + "-stop2-battle.png"))
    sheet = Image.new("RGB", (1440, 3 * 940), (18, 14, 20))
    for i, (im, t) in enumerate([(court_screen, "Your approved court (13 Oct), the reference"),
                                 (s1, "Stop 1, bare outskirts (new)"), (s2, "Stop 2, Codex's root grove, new art on its left")]):
        panel = Image.new("RGB", (1440, 940), (18, 14, 20))
        panel.paste(im, (0, 40))
        sheet.paste(label(panel, t), (0, i * 940))
    sheet.save(STUDY / (take + "-review-sheet.png"))
    print("review files saved for", take)


def walk(take, seconds=3.2, hold=1.0, fps=60, a=None, b=None, name="walk-stop1-to-stop2"):
    world = Image.open(STUDY / (take + "-strip.png")).convert("RGB")
    screen = Image.open(SCREEN).convert("RGB")
    a = STOPS["stop1"] if a is None else a
    b = STOPS["stop2"] if b is None else b
    n_hold, n_move = int(hold * fps), int(seconds * fps)
    xs = [a] * n_hold + [int(round(a + (b - a) * (0.5 - 0.5 * np.cos(np.pi * i / (n_move - 1))))) for i in range(n_move)] + [b] * n_hold
    out = STUDY / (take + "-" + name + ".mp4")
    enc = subprocess.Popen([FF, "-y", "-loglevel", "error", "-f", "rawvideo", "-pixel_format", "rgb24", "-video_size", "1440x900",
                            "-framerate", str(fps), "-i", "pipe:0", "-c:v", "libx264", "-preset", "slow", "-crf", "14",
                            "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(out)], stdin=subprocess.PIPE)
    for x in xs:
        f = framed(screen, world.crop((x, 0, x + 1376, 768)))
        enc.stdin.write(f.tobytes())
    enc.stdin.close()
    enc.wait()
    print("walk", out.name, "exit", enc.returncode, len(xs), "frames")


if __name__ == "__main__":
    cmd, take = sys.argv[1], sys.argv[2]
    if cmd == "build":
        build(take)
        review(take)
    elif cmd == "walk":
        walk(take)
