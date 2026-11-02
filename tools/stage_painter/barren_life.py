import json
import math
import shutil
import subprocess
import sys
import time
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
OLD = Path("C:/Users/jacob/AppData/Local/Temp/claude/C--Users-jacob-Desk-42/6569067f-8aa7-490e-b59a-c16d4469b0b5/scratchpad")
sys.path.insert(0, str(OLD))
import grove3 as G

# barren_life.py depth | plan | render [camp numbers] | grid
# Layer 2 of the barren camps, alive (Jacob, 27 Oct: "always make the 2nd layer animated bro like pterodactyls flying
# clouds moving"). Same pieces as the first six: the pterodactyl wing beat and the cloud sprites in
# Camp-Layers-20261027/life, plus a depth map and a sky mask per camp from Depth Anything 3 on his GPU. A flyer draws
# only where the scene is further away than it is, and never over a Pal. 15 s at 12 fps, so the 30 frame wing cycle
# and every flight path loop clean; only the slow clouds still jump back at the loop.
D = Path("C:/Users/jacob/KindredSiege-Clean/Art/Studies/Camp-Barren-20261027")
L = D / "life"
PIECES = Path("C:/Users/jacob/KindredSiege-Clean/Art/Studies/Camp-Layers-20261027/life")
INP = Path("C:/Users/jacob/ComfyUI-Installs/ComfyUI/ComfyUI-ArtLab/input/ks-camp-barren-20261027")
FF = "C:/Users/jacob/Desktop/PR/dark-lattice-site/node_modules/ffmpeg-static/ffmpeg.exe"
PLANS = HERE / "barren-life-plans.json"
CAMPS = ["1", "2", "3", "4", "5", "6"]
W, H, FPS, SEC = 1376, 768, 12, 15
N = FPS * SEC
CLOUD_BOX = [(32, 232, 418, 360), (502, 152, 890, 360), (966, 298, 1331, 359)]


def depth():
    INP.mkdir(parents=True, exist_ok=True)
    L.mkdir(parents=True, exist_ok=True)
    pids = {}
    for k in CAMPS:
        shutil.copy(D / ("camp-%s.png" % k), INP / ("camp-%s.png" % k))
        g = {"1": {"class_type": "LoadImage", "inputs": {"image": "ks-camp-barren-20261027/camp-%s.png" % k}},
             "40": {"class_type": "LoadDA3Model",
                    "inputs": {"model_name": "depth_anything_3_mono_large.safetensors", "weight_dtype": "default"}},
             "41": {"class_type": "DA3Inference",
                    "inputs": {"da3_model": ["40", 0], "image": ["1", 0], "resolution": 1022,
                               "resize_method": "upper_bound_resize", "mode": "mono"}},
             "44": {"class_type": "DA3Render",
                    "inputs": {"da3_geometry": ["41", 0], "output": "depth", "output.normalization": "min_max",
                               "output.apply_sky_clip": True}},
             "45": {"class_type": "SaveImage",
                    "inputs": {"images": ["44", 0], "filename_prefix": "KS-Camp-Barren-20261027/depth-%s" % k}},
             "46": {"class_type": "DA3Render",
                    "inputs": {"da3_geometry": ["41", 0], "output": "sky_mask", "output.colored": False}},
             "47": {"class_type": "SaveImage",
                    "inputs": {"images": ["46", 0], "filename_prefix": "KS-Camp-Barren-20261027/sky-%s" % k}}}
        pids[k] = G.api("/prompt", {"prompt": g, "front": True})["prompt_id"]
    t0 = time.time()
    while pids and time.time() - t0 < 1800:
        for k, pid in list(pids.items()):
            h = G.api("/history/" + pid).get(pid)
            if not h:
                continue
            if h.get("status", {}).get("status_str") == "error":
                print(k, "error", json.dumps(h["status"])[:300], flush=True)
                pids.pop(k)
                continue
            outs = h.get("outputs", {})
            if "45" not in outs or "47" not in outs:
                continue
            for node, name, rs in (("45", "depth", Image.BILINEAR), ("47", "sky", Image.NEAREST)):
                im = outs[node]["images"][0]
                m = Image.open(G.OUTPUT / im.get("subfolder", "") / im["filename"]).convert("L").resize((W, H), rs)
                m.save(L / ("%s-%s.png" % (name, k)))
            a = np.asarray(Image.open(L / ("depth-%s.png" % k)))
            s = np.asarray(Image.open(L / ("sky-%s.png" % k))) > 127
            print(k, "depth top", int(a[:40].mean()), "bottom", int(a[-60:].mean()), "sky %.0f%%" % (100 * s.mean()),
                  "%.0fs" % (time.time() - t0), flush=True)
            pids.pop(k)
        time.sleep(3)
    print("depth left", list(pids), flush=True)


def pal_mask(k):
    base = np.asarray(Image.open(D / ("camp-%s-with-pals.png" % k)).convert("RGB")).astype(np.int32)
    bare = np.asarray(Image.open(D / ("camp-%s.png" % k)).convert("RGB")).astype(np.int32)
    m = (np.abs(base - bare).sum(2) > 0).astype(np.uint8) * 255
    return np.asarray(Image.fromarray(m).filter(ImageFilter.MaxFilter(5))) > 127


def maps(k):
    dep = np.asarray(Image.open(L / ("depth-%s.png" % k)).convert("L")).astype(np.float64)
    sky = np.asarray(Image.open(L / ("sky-%s.png" % k)).convert("L")) > 127
    return dep, sky, pal_mask(k)


def make_plans():
    """Depth cuts from each camp's own maps: far flyers sit beyond the horizon land, the mid one flies in front of it
    but behind the back stations and the back row Pals, the near one is hidden only by the nearest frame."""
    P = {}
    look = Image.new("RGB", (2 * 688 + 8, 3 * 384 + 16), (18, 14, 20))
    for i, k in enumerate(CAMPS):
        dep, sky, pal = maps(k)
        low = [np.nonzero(sky[:, x])[0].max() for x in range(0, W, 8) if sky[:, x].any()]
        hy = int(np.median(low)) if low else 200
        band = ~sky[hy:hy + 40] & ~pal[hy:hy + 40]
        far = float(np.median(dep[hy:hy + 40][band])) if band.any() else 40.0
        back = ~sky[300:330] & ~pal[300:330]
        bk = float(np.median(dep[300:330][back])) if back.any() else 150.0
        thr = [round(max(8, far - 12)), round(max(far + 12, (far + bk) / 2)), 235]
        sh = 170 * i
        P[k] = {"horizon": hy, "depth": thr,
                "clouds": [[(300 + sh) % W, round(hy * 0.22), 6], [(900 + sh) % W, round(hy * 0.42), 4],
                           [(1250 + sh) % W, round(hy * 0.30), 8]],
                "flyers": [{"size": "near", "layer": "near", "kind": "line", "dir": 1, "v": 150, "x0": (300 + 2 * sh) % 2250,
                            "cy": max(70, round(hy * 0.45)), "amp": 14, "wave": 700, "bob": 3, "ph": 0},
                           {"size": "mid", "layer": "mid", "kind": "line", "dir": -1, "v": 105, "x0": (700 + sh) % 1575,
                            "cy": hy + 5, "amp": 24, "wave": 900, "bob": 2, "ph": 5, "haze": 0.15},
                           {"size": "far", "layer": "far", "kind": "circle", "cx": 420, "cy": hy - 14, "rx": 90, "ry": 14,
                            "laps": 2, "phase": round(0.3 * i, 2), "haze": 0.4},
                           {"size": "far", "layer": "far", "kind": "circle", "cx": 1060, "cy": hy - 20, "rx": 70, "ry": 12,
                            "laps": -2, "phase": round(2.0 + 0.3 * i, 2), "haze": 0.4}]}
        print(k, "horizon", hy, "far", round(far), "back", round(bk), "depth", thr, flush=True)
        # look: red where the mid flyer is hidden, blue where the far ones are, the horizon in yellow
        a = np.asarray(Image.open(D / ("camp-%s-with-pals.png" % k)).convert("RGB")).astype(np.float64)
        hid_mid = ~((sky | (dep < thr[1])) & ~pal)
        hid_far = ~((sky | (dep < thr[0])) & ~pal) & ~hid_mid
        a[hid_mid] = a[hid_mid] * 0.55 + np.array([255, 40, 40]) * 0.45
        a[hid_far] = a[hid_far] * 0.6 + np.array([40, 90, 255]) * 0.4
        a[max(0, hy - 1):hy + 1] = [255, 230, 0]
        look.paste(Image.fromarray(a.clip(0, 255).astype(np.uint8)).resize((688, 384), Image.BOX),
                   ((i % 2) * 696, (i // 2) * 392))
    look.save(HERE / "look-barren-masks.png")
    PLANS.write_text(json.dumps(P, indent=1))


def lum(x):
    return x[..., 0] * 0.299 + x[..., 1] * 0.587 + x[..., 2] * 0.114


def box_shrink(rgb, a, w, sq=1.0):
    h = max(1, round(rgb.shape[0] * w / rgb.shape[1] * sq))
    num = np.dstack([np.asarray(Image.fromarray((rgb[..., c] * a).astype(np.float32)).resize((w, h), Image.BOX))
                     for c in range(3)])
    den = np.asarray(Image.fromarray(a.astype(np.float32)).resize((w, h), Image.BOX))
    return num / np.maximum(den[..., None], 1e-6), den


def cloud_sprites(base, sky):
    """The three painted clouds, cut small and recoloured to this camp's own sky ramp."""
    cr = np.asarray(Image.open(PIECES / "clouds-raw.png").convert("RGB")).astype(np.float64)
    cm = np.asarray(Image.open(PIECES / "clouds-mask.png").convert("L")).astype(np.float64) / 255
    s2 = sky.copy()
    s2[:40] = False
    px = base[s2]
    order = np.argsort(lum(px))
    n = len(order)
    mid = px[order[int(n * 0.4):int(n * 0.6) + 1]].mean(0)
    hi = px[order[int(n * 0.97):]].mean(0)
    white = np.array([255.0, 250, 240])
    ramp = np.array([mid * 0.85 + hi * 0.15, mid * 0.55 + hi * 0.45, mid * 0.25 + hi * 0.75, hi * 0.6 + white * 0.4])
    out = []
    for (x0, y0, x1, y1), w in zip(CLOUD_BOX, (230, 180, 150)):
        rgb, a = box_shrink(cr[y0:y1, x0:x1], cm[y0:y1, x0:x1], w, 0.7)
        al = a >= 0.5
        qs = np.percentile(lum(rgb)[al], [30, 60, 85])
        out.append((ramp[np.digitize(lum(rgb), qs)].clip(0, 255), al))
    return out


def ptero_frames(size):
    fr = []
    for i in range(8):
        im = np.asarray(Image.open(PIECES / ("ptero-%s-%d.png" % (size, i))).convert("RGBA"))
        fr.append((im[..., :3].astype(np.float64), im[..., 3] > 127))
    return fr


def paste(cv, rgb, al, cx, cy, vis, haze=None, hz=0.0, flip=False):
    if flip:
        rgb, al = rgb[:, ::-1], al[:, ::-1]
    h, w = al.shape
    x0, y0 = int(round(cx - w / 2)), int(round(cy - h / 2))
    xa, ya, xb, yb = max(0, x0), max(0, y0), min(W, x0 + w), min(H, y0 + h)
    if xa >= xb or ya >= yb:
        return
    sub = al[ya - y0:yb - y0, xa - x0:xb - x0] & vis[ya:yb, xa:xb]
    src = rgb[ya - y0:yb - y0, xa - x0:xb - x0]
    if haze is not None and hz > 0:
        src = src * (1 - hz) + haze * hz
    reg = cv[ya:yb, xa:xb]
    reg[sub] = src[sub]


def flap_index(t, phase, flaps=2, glide=14, hold=3):
    """Two wing beats of 8 frames, then a glide holding the level wing frame. 30 frames a cycle."""
    k = (t + phase) % (8 * flaps + glide)
    return k % 8 if k < 8 * flaps else hold


def render(k, plan, sink=None, keep=()):
    base = np.asarray(Image.open(D / ("camp-%s-with-pals.png" % k)).convert("RGB")).astype(np.float64)
    dep, sky, pal = maps(k)
    haze = base[sky].mean(0)
    skyv = sky & ~pal
    clouds = cloud_sprites(base, skyv) if plan["clouds"] else []
    vis = {name: (sky | (dep < v)) & ~pal for name, v in zip(("far", "mid", "near"), plan["depth"])}
    pt = {s: ptero_frames(s) for s in ("near", "mid", "far")}
    shots = []
    for t in range(N):
        cv = base.copy()
        for (rgb, al), (cx0, cy, v) in zip(clouds, plan["clouds"]):
            span = W + al.shape[1]
            paste(cv, rgb, al, (cx0 - v * t / FPS) % span - al.shape[1] / 2, cy, skyv)
        for f in plan["flyers"]:
            if f["kind"] == "circle":
                ang = 2 * math.pi * (t / N) * f["laps"] + f["phase"]
                cx = f["cx"] + f["rx"] * math.cos(ang)
                cy = f["cy"] + f["ry"] * math.sin(ang)
                flip = math.sin(ang) * f["laps"] > 0
                i = flap_index(t, int(f["phase"] * 10), 1, 22)
            else:
                s = (f["x0"] + f["v"] * t / FPS) % (f["v"] * SEC)
                cx = s - 140 if f["dir"] > 0 else W + 140 - s
                cy = f["cy"] + f["amp"] * math.sin(2 * math.pi * s / f["wave"])
                flip = f["dir"] < 0
                i = flap_index(t, f.get("ph", 0))
            bob = f.get("bob", 0) * (-1 if i in (4, 5, 6) else (1 if i in (1, 2) else 0))
            rgb, al = pt[f["size"]][i]
            paste(cv, rgb, al, cx, cy + bob, vis[f["layer"]], haze, f.get("haze", 0), flip)
        out = cv.clip(0, 255).astype(np.uint8)
        if sink:
            sink.stdin.write(out.tobytes())
        if t in keep:
            shots.append(out)
    return shots


def videos(which):
    plans = json.loads(PLANS.read_text())
    for k in which:
        dst = D / ("camp-%s-life.mp4" % k)
        p = subprocess.Popen([FF, "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", "%dx%d" % (W, H),
                              "-r", str(FPS), "-i", "-", "-vf", "scale=%d:%d:flags=neighbor" % (W * 2, H * 2),
                              "-c:v", "libx264", "-preset", "slow", "-crf", "18", "-pix_fmt", "yuv420p",
                              "-movflags", "+faststart", str(dst)], stdin=subprocess.PIPE)
        shots = render(k, plans[k], p, keep=(30, 75, 120, 165))
        p.stdin.close()
        p.wait()
        sh = Image.new("RGB", (2 * 688 + 12, 2 * 384 + 12), (20, 20, 20))
        for j, s in enumerate(shots):
            sh.paste(Image.fromarray(s).resize((688, 384), Image.BOX), ((j % 2) * 700, (j // 2) * 396))
        sh.save(HERE / ("look-barren-%s-life.png" % k))
        Image.fromarray(shots[1]).save(D / ("camp-%s-life-still.png" % k))
        print(k, "video MB %.1f" % (dst.stat().st_size / 1e6), flush=True)


def grid():
    ins = []
    for k in CAMPS:
        ins += ["-i", str(D / ("camp-%s-life.mp4" % k))]
    font = "C\\:/Windows/Fonts/arialbd.ttf"
    parts = ["[%d:v]scale=688:384:flags=area,drawtext=fontfile='%s':text='%s':x=14:y=10:fontsize=40:fontcolor=white:"
             "borderw=3:bordercolor=black[v%d]" % (i, font, k, i) for i, k in enumerate(CAMPS)]
    fc = (";".join(parts) + ";" + "".join("[v%d]" % i for i in range(6)) +
          "xstack=inputs=6:layout=0_0|w0_0|0_h0|w0_h0|0_h0+h2|w0_h0+h2[out]")
    dst = D / "camps-barren-grid.mp4"
    subprocess.run([FF, "-y", "-loglevel", "error"] + ins + ["-filter_complex", fc, "-map", "[out]", "-c:v", "libx264",
                    "-preset", "slow", "-crf", "20", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(dst)], check=True)
    subprocess.run([FF, "-y", "-loglevel", "error", "-ss", "6", "-i", str(dst), "-frames:v", "1",
                    str(HERE / "look-barren-grid-frame.png")], check=True)
    print("grid MB %.1f" % (dst.stat().st_size / 1e6), flush=True)


if __name__ == "__main__":
    step = sys.argv[1]
    if step == "depth":
        depth()
    elif step == "plan":
        make_plans()
    elif step == "render":
        videos(sys.argv[2:] or CAMPS)
    elif step == "grid":
        grid()
