import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import stops_build as sb

# stops_grade.py <take> <new_take>
# 1. The new stop 1 art is warmer and redder than Codex's grove, so the walk shows a colour line at the join even with
#    no seam in the drawing. Ease the new art's colour toward the grove's over x 900..join (ground greying toward the
#    water), measured in Lab on the floor and the scenery separately, then snap back to the strip's colours.
# 2. Her bone white scrub at the bottom of stop 2 is the brightest thing on screen, right under the front row. Pull its
#    near white pixels down to the dry thorn values around it. Same shape, no new pixels drawn.
REPO = Path("C:/Users/jacob/KindredSiege-Clean")
STUDY = REPO / "Art/Studies/Bloodridge-Stops-20261020"
OLD = REPO / "Art/Studies/Bloodridge-Root-Crypt-20261019/panorama.png"
NEW, OLD_X, JOIN, RAMP0 = 1664, 688, 1728, 900
SCRUB = (2020, 610, 2330, 768)


def srgb_to_lab(rgb):
    c = rgb / 255.0
    c = np.where(c > 0.04045, ((c + 0.055) / 1.055) ** 2.4, c / 12.92)
    m = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]])
    xyz = c @ m.T / np.array([0.95047, 1.0, 1.08883])
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], -1)


def lab_to_srgb(lab):
    fy = (lab[..., 0] + 16) / 116
    fx, fz = fy + lab[..., 1] / 500, fy - lab[..., 2] / 200
    f = np.stack([fx, fy, fz], -1)
    xyz = np.where(f ** 3 > 0.008856, f ** 3, (f - 16 / 116) / 7.787) * np.array([0.95047, 1.0, 1.08883])
    m = np.array([[3.2406, -1.5372, -0.4986], [-0.9689, 1.8758, 0.0415], [0.0557, -0.2040, 1.0570]])
    c = xyz @ m.T
    c = np.where(c > 0.0031308, 1.055 * np.clip(c, 0, None) ** (1 / 2.4) - 0.055, 12.92 * c)
    return np.clip(np.round(c * 255), 0, 255).astype(np.uint8)


def band_mean(lab, x0, x1, y0, y1):
    p = lab[y0:y1, x0:x1].reshape(-1, 3)
    keep = (p[:, 0] > 18) & (p[:, 0] < 82)
    return p[keep].mean(0)


def run(take, new_take):
    strip = Image.open(STUDY / (take + "-strip.png")).convert("RGB")
    old = Image.open(OLD).convert("RGB")
    a = np.asarray(strip).astype(np.float64)
    lab = srgb_to_lab(a)
    colours = np.unique(np.asarray(strip).reshape(-1, 3), axis=0)
    # Which pixels left of the join are new: anything that differs from her strip at the same world spot.
    old_here = np.zeros_like(a)
    old_here[:, NEW:] = np.asarray(old).astype(np.float64)[:, OLD_X:OLD_X + a.shape[1] - NEW]
    is_new = np.zeros(a.shape[:2], bool)
    is_new[:, :NEW] = True
    is_new[:, NEW:JOIN] = np.abs(a[:, NEW:JOIN] - old_here[:, NEW:JOIN]).sum(2) > 0
    shifts = {}
    for name, (y0, y1) in {"scenery": (0, 360), "floor": (360, 768)}.items():
        mine = band_mean(lab, JOIN - 330, NEW, y0, y1)
        hers = band_mean(lab, JOIN, JOIN + 330, y0, y1)
        shifts[name] = (hers - mine) * np.array([0.6, 1.0, 1.0])
    ys = np.arange(768)[:, None]
    t_floor = np.clip((ys - 300) / 120.0, 0, 1)
    shift = shifts["scenery"] * (1 - t_floor)[..., None] + shifts["floor"] * t_floor[..., None]
    xs = np.arange(a.shape[1])[None, :]
    u = np.clip((xs - RAMP0) / float(JOIN - RAMP0), 0, 1)
    w = (u * u * (3 - 2 * u))[..., None]
    graded = lab + w * shift
    rgb = lab_to_srgb(graded)
    out = np.asarray(strip).copy()
    region = is_new & (xs >= RAMP0)
    out[region] = rgb[region]
    # The scrub: near white pixels in its box take the value of dry thorn, keeping their relative light.
    x0, y0, x1, y1 = SCRUB
    box = srgb_to_lab(out[y0:y1, x0:x1].astype(np.float64))
    # Measured 20 Oct: the scrub is L 62..73 at chroma about 5; her flagstones top out near L 59 at chroma about 14.
    bright = (box[..., 0] > 60) & (np.hypot(box[..., 1], box[..., 2]) < 10)
    surround = box[~bright].mean(0)
    box[bright, 0] = surround[0] + (box[bright, 0] - 60) * 0.45 + 6
    box[bright, 1] = surround[1] * 0.8
    box[bright, 2] = surround[2] * 0.8
    patch = out[y0:y1, x0:x1].copy()
    patch[bright] = lab_to_srgb(box)[bright]
    out[y0:y1, x0:x1] = patch
    img = sb.palette_snap(Image.fromarray(out), colours)
    img.save(STUDY / (new_take + "-strip.png"))
    report = {"from": take, "ramp_x": [RAMP0, JOIN], "lab_shift_scenery": [round(v, 2) for v in shifts["scenery"]],
              "lab_shift_floor": [round(v, 2) for v in shifts["floor"]], "scrub_pixels_toned": int(bright.sum()),
              "colours": int(len(np.unique(np.asarray(img).reshape(-1, 3), axis=0)))}
    (STUDY / (new_take + "-grade.json")).write_text(json.dumps(report, indent=1))
    print(json.dumps(report))


if __name__ == "__main__":
    run(sys.argv[1], sys.argv[2])
