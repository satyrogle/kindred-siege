import copy
import json
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

from PIL import Image

# grove3.py backdrop | sheets
# Jacob, 21 Oct ("go"): the root grove in his three layers, as a clickable page on his PC. 1 a static backdrop that
# mimics the region, 2 the world with little Kingdom Rush style scenes you click for achievements, 3 the battle floor.
# This script makes the generated pieces with the Clarth bust's Ideogram graph and the Clarth BiRefNet graph (the cut
# in pipeline): the Bloodridge vista for layer 1, and one sprite sheet per scene (crows, ghoul, hand) plus its mask.
REPO = Path("C:/Users/jacob/KindredSiege-Clean")
STUDY = REPO / "Art/Studies/Bloodridge-Grove-Layers-20261021"
CLARTH = REPO / "Art/Studies/Chief-Cut-Ins-20261018/clarth/"
OUTPUT = Path("C:/Users/jacob/ComfyUI-Shared/output/ArtLab")
INPUT = Path("C:/Users/jacob/ComfyUI-Installs/ComfyUI/ComfyUI-ArtLab/input/ks-bloodridge-grove-20261021")
API = "http://127.0.0.1:8191"
FLAT = "Plain flat light grey, completely empty, no ground, no shadows, no texture. No text, no border, no signature, no watermark."
STYLE = "Pixel art game asset, fine native pixel clusters, dusty sepia, ash grey and violet-brown palette with a dull warm light from the upper left, like a dark fantasy pixel art game."


def graph_of(path):
    h = json.load(open(path, encoding="utf-8"))
    p = h[next(iter(h))]["prompt"]
    return copy.deepcopy(p[2] if isinstance(p, list) else p)


def api(path, body=None):
    req = urllib.request.Request(API + path, data=None if body is None else json.dumps(body).encode(),
                                 headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.load(r)


def wait_image(pid):
    while True:
        try:
            h = api("/history/" + pid).get(pid)
        except (OSError, urllib.error.URLError):
            time.sleep(10)
            continue
        if h:
            if h.get("status", {}).get("status_str") == "error":
                raise RuntimeError(json.dumps(h["status"])[:800])
            imgs = [i for o in h.get("outputs", {}).values() for i in o.get("images", [])]
            if imgs:
                return Image.open(OUTPUT / imgs[0].get("subfolder", "") / imgs[0]["filename"])
        time.sleep(4)


def ideogram(key, high, background, elements, negative, seed, w, h, aspect):
    g = graph_of(CLARTH / "clarth-bust-2-pointed.history.json")
    text = {"aspect_ratio": aspect, "high_level_description": high,
            "compositional_deconstruction": {"background": background, "elements": elements}}
    g["4"]["inputs"]["text"] = json.dumps(text)
    g["5"]["inputs"]["text"] = negative
    g["11"]["inputs"]["noise_seed"] = seed
    g["14"]["inputs"]["filename_prefix"] = "KS-Bloodridge-Grove-20261021/" + key
    for n in g.values():
        if n["class_type"] in ("EmptyFlux2LatentImage", "EmptySD3LatentImage", "EmptyLatentImage", "Ideogram4Scheduler"):
            n["inputs"].update(width=w, height=h)
    (STUDY / (key + "-workflow.json")).write_text(json.dumps(g, indent=1))
    img = wait_image(api("/prompt", {"prompt": g})["prompt_id"]).convert("RGB")
    img.save(STUDY / (key + "-raw.png"))
    return img


def mask(key, img):
    INPUT.mkdir(parents=True, exist_ok=True)
    img.save(INPUT / (key + ".png"))
    mg = graph_of(CLARTH / "clarth-2-pointed-mask.history.json")
    mg["1"]["inputs"]["image"] = "ks-bloodridge-grove-20261021/" + key + ".png"
    mg["5"]["inputs"]["filename_prefix"] = "KS-Bloodridge-Grove-20261021/" + key + "-mask"
    m = wait_image(api("/prompt", {"prompt": mg})["prompt_id"]).convert("L")
    m.save(STUDY / (key + "-mask.png"))


NEG = "text, letters, numbers, watermark, border, frame, grid lines, background scenery, ground, shadow on the ground, blurry, smooth gradient, 3d render, photo, thick black outline, cartoon, bright saturated colours, people"


def backdrop():
    ideogram("backdrop",
             STYLE + " A far distant view only, for the static backdrop layer of a 2.5D battle scene: under a pale dusty mauve sky, the long eroded limestone ridge of Bloodridge stretches across the whole picture, with burial terraces, stairs and dark tomb doors cut into its face, and on its highest point the chief's stepped burial court with one tall dark gate, small and far away. Everything is low contrast and hazy, pale grey-mauve and light sepia, the further the paler. The bottom third is a soft hazy band of far ruins and dust.",
             "A plain pale dusty mauve sky filling the top, empty and calm.",
             [{"type": "obj", "bbox": [330, 0, 1000, 1000], "desc": "The long far ridge of Bloodridge with burial terraces and tomb doors, the stepped court and its tall dark gate on the highest point, pale and hazy."}],
             "foreground, near objects, trees, people, creatures, text, watermark, border, high contrast, dark sky, night, sun, clouds, thick black outline, cartoon, photo, 3d render",
             2026102172, 1376, 448, "3:1")


def sheets():
    poses = lambda n: [{"type": "obj", "bbox": [200, int(1000 * i / n) + 20, 800, int(1000 * (i + 1) / n) - 20], "desc": d} for i, d in enumerate(n_desc)]
    sets = {
        "crows": (["a black crow standing, side view facing left", "the same crow pecking down at the ground",
                   "the same crow with its wings raised, taking off", "the same crow flying, wings swept down"],
                  STYLE + " A sprite sheet: the same small black crow drawn four times in one row, evenly spaced with wide empty space between them, each in a different pose. Glossy black-violet feathers, a dull warm rim of light, a little grey beak.", 2026102173),
        "ghoul": (["a hunched gravedigger ghoul holding a wooden shovel raised high over his shoulder, side view facing left",
                   "the same ghoul stabbing the shovel down into a small heap of earth",
                   "the same ghoul toppling backwards in surprise, arms up, the shovel flying"],
                  STYLE + " A sprite sheet: the same thin hunched ghoul gravedigger with grey-green skin, a bald head, big ears and tattered brown rags, drawn three times in one row, evenly spaced with wide empty space between them, each in a different pose. Funny, deadpan, not scary.", 2026102174),
        "hand": (["a pale bony skeletal hand and forearm rising straight up, open, tilted left, waving",
                  "the same hand open, tilted right, waving", "the same hand giving a thumbs up"],
                 STYLE + " A sprite sheet: the same pale bony skeletal hand and forearm drawn three times in one row, evenly spaced with wide empty space between them, each in a different pose, the wrist cut off flat at the bottom as if rising out of water. Funny, deadpan.", 2026102175),
    }
    for key, (descs, high, seed) in sets.items():
        n_desc = descs
        els = poses(len(descs))
        img = ideogram("sheet-" + key, high, FLAT, els, NEG, seed, 1376, 512, "3:1")
        mask("sheet-" + key, img)
        print("sheet", key, "saved", flush=True)


if __name__ == "__main__":
    STUDY.mkdir(parents=True, exist_ok=True)
    {"backdrop": backdrop, "sheets": sheets}[sys.argv[1]]()
