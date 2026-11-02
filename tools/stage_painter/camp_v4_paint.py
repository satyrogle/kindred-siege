import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import camp5_layers as C5L
import regions as RG
import town_paint as TP

# camp_v4_paint.py [ships] [m1] [m2] [m3] [ring]
# Jacob, 28 Oct 2026: "go do 1-9 pass" on Claude's nine fixes for the war camp. Painted on his GPU (Ideogram 4 local)
# from colour plans drawn here, cut in camp_v4_parts.py:
#   ships   seven small flying craft, one hull for each job, scavenged metal with engine glow, no sails (item 1)
#   m1-m3   three looks for the Butcher's forge machine, painted over his anvil on a 1x patch of the camp (item 7)
#   ring    the scorched landing ring the teleport dragged along, cracked shell plates and the burnt out anchor (item 5)
# Patches are 512 x 320 at the camp's own pixel size, like the camp 5 patches. Prompts stay calm.
OUT, W, H = TP.OUT, TP.W, TP.H
STYLE = TP.STYLE
LIGHT = "Clear cold morning sun from the upper left, cool blue shadows, as discrete flat pixel colour planes."
CAMP_PAL = ["#d4c4a4", "#b0a084", "#7c7480", "#2e2c38", "#f2a24a", "#ece4ee", "#a898b8", "#e0a070", "#4a2350",
            "#a050e0"]
WHO = ("the camp of the last five survivors of a cult of cyborg flesh beings, an intergalactic species from a Type 4 "
       "civilization, on a bare high plateau of tan stone")
MCROP = (180, 96, 692, 416)
RCROP = (432, 280, 944, 600)
BLOCK = (386, 244, 500, 332)  # the Butcher's anvil and feet: x 36 to 150, y 102 to 190 of his 192 cell at (350, 142)
RING = (688, 440, 170, 61)  # centre and radii of the landing ring, on the free floor between the four Pals
ANCHOR = (688, 426)
PLATES = [[(596, 404), (622, 400), (626, 412), (600, 418)], [(744, 470), (774, 464), (778, 478), (748, 482)],
          [(812, 420), (834, 418), (836, 430), (814, 432)]]
WEAPONS = {"zealot": (585, 440), "tank": (795, 452), "raider": (705, 492)}  # sprite centres, placed in parts
STEP = {"ships": 2, "m1": 3, "m2": 3, "m3": 3, "ring": 3}
SEEDS = {"ships": 2026102841, "m1": 2026102842, "m2": 2026102843, "m3": 2026102844, "ring": 2026102845}
GUN, GUN_T, GUN_D = (46, 44, 56), (72, 70, 84), (30, 28, 36)
GLYPH, ROSE = (242, 162, 74), (224, 160, 112)
SHELL, SHELL_D = (236, 228, 238), (168, 152, 184)
VIOLET, VIOLET_D = (110, 50, 170), (60, 30, 90)
FORGE = ("the forge of the Butcher: a squat dark gunmetal heat core with glowing orange glyph seams and a glowing open "
         "mouth, capped with pearly opalescent shell, curved alien blades standing upright beside it")
MACHINES = {
    "m1": "the forge machine of the Butcher, their own advanced alien technology: a squat heavy console of dark "
          "gunmetal with a flat working top and a rose gold rim, glowing orange glyph seams running across its front, "
          "a small dark violet well glowing in the middle of its top where the work rests, seen from the front",
    "m2": "the forge machine of the Butcher, their own advanced alien technology: a wide shallow rose gold cradle like "
          "an open bowl filled with a swirling dark violet field, held up on a heavy low dark gunmetal base with "
          "glowing orange glyph seams, seen from the front",
    "m3": "the forge machine of the Butcher, their own advanced alien technology: a rounded press housing of pearly "
          "opalescent shell plates with iridescent hexagonal scale cells over a dark gunmetal core, a glowing orange "
          "glyph seam across its middle and a flat rose gold working plate on top, seen from the front"}
SHIP_BOXES = {"b_barge": (24, 70, 344, 310), "b_dart": (360, 70, 680, 310), "b_drum": (696, 70, 1016, 310),
              "b_fins": (1032, 70, 1352, 310), "c_fin": (60, 420, 420, 680), "c_sled": (508, 420, 868, 680),
              "c_jaw": (956, 420, 1316, 680)}
SHIP_TEXT = {
    "b_barge": "a long low flying cargo barge of the Bloodridge raiders: a crescent hull shaped like an ancient Egyptian "
               "sun boat with a tall curled prow and stern, built of scavenged black metal plates riveted together, a "
               "row of glowing ember orange engine vents along its belly and an open flat deck, side view, facing left",
    "b_dart": "a slim fast flying craft of the Bloodridge raiders: a narrow black metal hull with a pointed curled prow, "
              "two round engine pods hanging under it glowing ember orange at the back, three round black pods held "
              "under its belly, side view, facing left",
    "b_drum": "a squat round flying craft of the Bloodridge raiders: a short deep hull of patched black metal plates, a "
              "dark round dome on its deck, one big round engine glowing ember orange at its stern and small fins, "
              "side view, facing left",
    "b_fins": "a long thin flying craft of the Bloodridge raiders: a low black metal hull with two tall black metal fins "
              "shaped like obelisks rising from it, glowing ember orange engine slits along its side, side view, facing "
              "left",
    "c_fin": "a small fast flying skiff of the Clarth hunters: a sleek hull of pale sand shark bone plates bolted on a "
             "dark metal frame, one tall curved red fin like a shark's back fin, a glowing red engine at its stern, side "
             "view, facing right",
    "c_sled": "a wide flat flying sled of the Clarth hunters: a deck of pale bone plates on two dark metal runners, a red "
              "hide cover over its front, two small glowing red engines under it, side view, facing right",
    "c_jaw": "a small flying craft of the Clarth hunters made from the open jaw of a giant sand shark: pale bone with a "
             "row of teeth along its edge, red trims, a glowing red engine under it, side view, facing right"}


def bbc(box, crop):
    return TP.bb(box, crop)


def camp_base():
    """Frame 0 of the war camp without the moving layer: layer 1, layer 3, the Pal shadows and the Pals."""
    cv = Image.open(OUT / "layer-1-far.png").convert("RGB")
    for name in ("layer-3-floor.png", "pal-shadows.png"):
        im = Image.open(OUT / name).convert("RGBA")
        cv.paste(im, (0, 0), im)
    for nm, p in C5L.pal_sheets().items():
        rgb, al = p["frames"][0]
        im = Image.fromarray(np.dstack([rgb, al * 255]).clip(0, 255).astype(np.uint8), "RGBA")
        cv.paste(im, tuple(int(v) for v in p["at"]), im)
    return cv


def machine_shapes(k, d):
    """The plan of each look over the anvil block, in camp pixels. Every look fills the block's anvil rows."""
    x0, y0, x1, y1 = BLOCK
    cx = (x0 + x1) // 2
    if k == "m1":
        d.rectangle((x0, y0 + 12, x1, y1), fill=GUN)
        d.rectangle((x0 + 2, y0, x1 - 2, y0 + 12), fill=GUN_T)
        d.line((x0 + 2, y0 + 12, x1 - 2, y0 + 12), fill=ROSE, width=2)
        d.ellipse((cx - 16, y0 + 2, cx + 16, y0 + 10), fill=VIOLET)
        for gy in (y0 + 32, y0 + 58):
            d.line((x0 + 10, gy, x1 - 10, gy), fill=GLYPH, width=2)
            for gx in range(x0 + 16, x1 - 10, 14):
                d.line((gx, gy - 5, gx, gy + 5), fill=GLYPH, width=1)
        d.rectangle((x0, y1 - 6, x1, y1), fill=GUN_D)
    elif k == "m2":
        d.rectangle((x0 + 6, y0 + 38, x1 - 6, y1), fill=GUN)
        d.rectangle((x0 + 2, y1 - 8, x1 - 2, y1), fill=GUN_D)
        d.line((x0 + 16, y0 + 64, x1 - 16, y0 + 64), fill=GLYPH, width=2)
        d.ellipse((x0 - 4, y0 - 10, x1 + 4, y0 + 42), fill=ROSE)
        d.ellipse((x0 + 8, y0 - 4, x1 - 8, y0 + 18), fill=VIOLET_D)
        d.ellipse((x0 + 28, y0, x1 - 28, y0 + 10), fill=VIOLET)
    else:
        d.rectangle((x0 + 2, y0 + 40, x1 - 2, y1), fill=GUN)
        d.rounded_rectangle((x0 - 2, y0 - 6, x1 + 2, y0 + 70), radius=12, fill=SHELL)
        d.rounded_rectangle((x0 + 30, y0 + 24, x1 - 4, y0 + 66), radius=10, fill=SHELL_D)
        d.line((x0 + 2, y0 + 44, x1 - 2, y0 + 44), fill=GLYPH, width=2)
        d.ellipse((x0 + 24, y0 - 8, x1 - 24, y0 + 4), fill=ROSE)
        d.rectangle((x0, y1 - 6, x1, y1), fill=GUN_D)


def machine_plan(k):
    cv = camp_base()
    machine_shapes(k, ImageDraw.Draw(cv))
    m = Image.new("RGB", (W, H), (0, 0, 0))
    machine_shapes(k, ImageDraw.Draw(m))
    sil = np.asarray(m).any(2)
    Image.fromarray((sil * 255).astype(np.uint8)).save(OUT / ("v4-%s-plan-mask.png" % k))
    return cv.crop(MCROP)


def machine_prompt(k):
    els = [{"type": "obj", "bbox": bbc((242, 158, 440, 384), MCROP), "desc": FORGE},
           {"type": "obj", "bbox": bbc(BLOCK, MCROP), "desc": MACHINES[k]},
           {"type": "obj", "bbox": bbc((180, 330, 692, 416), MCROP),
            "desc": "flat bare warm tan stone of the plateau, wind scoured and cracked"}]
    return json.dumps({
        "high_level_description": "A close patch of " + WHO + ": their forge stands on the bare stone. " + STYLE,
        "style_description": {"medium": "pixel_art", "art_style": RG.L3.ART,
                              "aesthetics": "compact sacred alien technology, coherent pixel clusters",
                              "lighting": LIGHT, "color_palette": CAMP_PAL},
        "compositional_deconstruction": {"background": "A patch of the flat bare plateau floor with pale clouds beyond "
                                                       "its far edge, seen from a fixed side oblique camera like a "
                                                       "classic arcade game stage.", "elements": els}})


def ring_shapes(d, rng):
    cx, cy, rx, ry = RING
    d.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=(110, 92, 82))
    d.ellipse((cx - rx + 18, cy - ry + 8, cx + rx - 18, cy + ry - 8), fill=(96, 80, 74))
    for i in range(22):
        a = rng.uniform(0, 2 * np.pi)
        r0, r1 = rng.uniform(0.15, 0.4), rng.uniform(0.6, 0.95)
        col = (76, 62, 62) if i % 3 else (150, 156, 166)
        d.line((cx + rx * r0 * np.cos(a), cy + ry * r0 * np.sin(a), cx + rx * r1 * np.cos(a), cy + ry * r1 * np.sin(a)),
               fill=col, width=2 if i % 3 else 1)
    d.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), outline=(58, 46, 48), width=2)
    d.arc((cx - rx + 2, cy - ry + 2, cx + rx - 2, cy + ry - 2), 200, 340, fill=(170, 176, 188), width=1)
    for poly in PLATES:
        d.polygon(poly, fill=SHELL)
        d.line(poly[2:] + poly[:1], fill=SHELL_D, width=2)
    ax, ay = ANCHOR
    d.rectangle((ax - 26, ay + 8, ax + 26, ay + 22), fill=GUN)
    d.line((ax - 20, ay + 15, ax + 20, ay + 15), fill=(150, 96, 60), width=1)
    d.arc((ax - 22, ay - 40, ax + 22, ay + 12), 120, 420, fill=ROSE, width=5)


def ring_plan():
    cv = camp_base()
    rng = np.random.default_rng(SEEDS["ring"])
    ring_shapes(ImageDraw.Draw(cv), rng)
    cx, cy, rx, ry = RING
    m = Image.new("L", (W, H), 0)
    ImageDraw.Draw(m).ellipse((cx - rx - 3, cy - ry - 3, cx + rx + 3, cy + ry + 3), fill=255)
    m.save(OUT / "v4-ring-plan-mask.png")
    return cv.crop(RCROP)


def ring_prompt():
    cx, cy, rx, ry = RING
    ax, ay = ANCHOR
    els = [{"type": "obj", "bbox": bbc((cx - rx, cy - ry, cx + rx, cy + ry), RCROP),
            "desc": "a flat oval of scorched battlefield ground lying sunk in the stone, burnt dark brown and charcoal "
                    "earth with glassy fused streaks running out from its centre, cut off clean along its rim with a "
                    "thin bright glassy edge"},
           {"type": "obj", "bbox": bbc((ax - 28, ay - 42, ax + 28, ay + 24), RCROP),
            "desc": "a burnt out teleport anchor of their alien technology: a cracked rose gold hoop standing upright on "
                    "a low dark gunmetal base with dim orange glyph seams, scorched"}]
    els += [{"type": "obj", "bbox": bbc((min(p[0] for p in q), min(p[1] for p in q), max(p[0] for p in q),
                                         max(p[1] for p in q)), RCROP),
             "desc": "a cracked pearly opalescent armour plate with iridescent hexagonal scale cells and a rose gold "
                     "trim, lying flat"} for q in PLATES]
    return json.dumps({
        "high_level_description": "A close patch of " + WHO + ": where they landed by teleport, an oval of scorched "
                                  "ground from the distant battlefield came along with them and now lies in the stone. "
                                  + STYLE,
        "style_description": {"medium": "pixel_art", "art_style": RG.L3.ART,
                              "aesthetics": "the aftermath of a lost fight, coherent pixel clusters",
                              "lighting": LIGHT, "color_palette": CAMP_PAL + ["#6e5c52", "#4c3e3e", "#969ca6"]},
        "compositional_deconstruction": {"background": "flat bare warm tan stone of the plateau around it, wind "
                                                       "scoured and cracked, seen from a fixed side oblique camera "
                                                       "like a classic arcade game stage.", "elements": els}})


def ship_shapes(k, d):
    x0, y0, x1, y1 = SHIP_BOXES[k]
    mx, my = (x0 + x1) // 2, (y0 + y1) // 2
    hull, plate, ember, glow = (34, 28, 30), (64, 56, 58), (255, 138, 48), (255, 204, 128)
    bone, bone_d, red, metal, redg = (226, 214, 186), (170, 156, 130), (176, 52, 44), (60, 58, 64), (255, 90, 60)
    if k == "b_barge":
        d.polygon([(x0 + 30, my - 20), (x0 + 70, my + 28), (x1 - 70, my + 28), (x1 - 30, my - 20)], fill=hull)
        d.polygon([(x0 + 30, my - 20), (x0 + 14, my - 70), (x0 + 30, my - 76), (x0 + 50, my - 24)], fill=hull)
        d.polygon([(x1 - 30, my - 20), (x1 - 16, my - 62), (x1 - 30, my - 68), (x1 - 50, my - 24)], fill=hull)
        d.rectangle((x0 + 70, my - 26, x1 - 70, my - 16), fill=plate)
        for i in range(5):
            ex = x0 + 90 + i * (x1 - x0 - 180) // 4
            d.ellipse((ex - 6, my + 22, ex + 6, my + 32), fill=ember)
    elif k == "b_dart":
        d.polygon([(x0 + 20, my - 6), (x0 + 80, my - 18), (x1 - 30, my - 18), (x1 - 12, my), (x1 - 60, my + 12),
                   (x0 + 70, my + 12)], fill=hull)
        d.polygon([(x0 + 20, my - 6), (x0 + 12, my - 40), (x0 + 28, my - 42), (x0 + 40, my - 12)], fill=hull)
        for ex in (mx - 70, mx + 40):
            d.ellipse((ex - 22, my + 14, ex + 22, my + 34), fill=plate)
            d.ellipse((ex + 14, my + 18, ex + 24, my + 30), fill=ember)
        for i in range(3):
            d.ellipse((mx - 20 + 16 * i, my + 14, mx - 8 + 16 * i, my + 26), fill=(20, 16, 18))
    elif k == "b_drum":
        d.rounded_rectangle((mx - 96, my - 36, mx + 90, my + 40), radius=26, fill=hull)
        d.ellipse((mx - 36, my - 74, mx + 30, my - 26), fill=plate)
        d.ellipse((mx + 74, my - 18, mx + 118, my + 26), fill=ember)
        d.ellipse((mx + 86, my - 6, mx + 106, my + 14), fill=glow)
        d.polygon([(mx + 40, my - 36), (mx + 70, my - 64), (mx + 78, my - 36)], fill=hull)
    elif k == "b_fins":
        d.polygon([(x0 + 16, my + 6), (x0 + 40, my - 8), (x1 - 24, my - 8), (x1 - 14, my + 8), (x1 - 40, my + 22),
                   (x0 + 40, my + 22)], fill=hull)
        for fx in (mx - 50, mx + 40):
            d.polygon([(fx - 12, my - 8), (fx - 4, my - 92), (fx + 4, my - 92), (fx + 12, my - 8)], fill=hull)
            d.polygon([(fx - 4, my - 92), (fx, my - 104), (fx + 4, my - 92)], fill=plate)
        for i in range(6):
            d.line((x0 + 70 + 34 * i, my + 4, x0 + 84 + 34 * i, my + 4), fill=ember, width=3)
    elif k == "c_fin":
        d.polygon([(x0 + 30, my + 4), (x0 + 70, my - 14), (x1 - 40, my - 10), (x1 - 12, my + 6), (x1 - 50, my + 24),
                   (x0 + 60, my + 24)], fill=bone)
        d.line((x0 + 60, my + 22, x1 - 50, my + 22), fill=metal, width=4)
        d.polygon([(mx - 40, my - 12), (mx - 6, my - 96), (mx + 30, my - 92), (mx + 20, my - 12)], fill=red)
        d.ellipse((x0 + 16, my - 4, x0 + 40, my + 20), fill=redg)
    elif k == "c_sled":
        d.rectangle((x0 + 40, my - 16, x1 - 50, my + 4), fill=bone)
        for i in range(6):
            d.line((x0 + 60 + 40 * i, my - 16, x0 + 60 + 40 * i, my + 4), fill=bone_d, width=2)
        d.polygon([(x1 - 120, my - 16), (x1 - 60, my - 52), (x1 - 30, my - 30), (x1 - 50, my + 4)], fill=red)
        for ry_ in (my + 10, my + 22):
            d.line((x0 + 30, ry_, x1 - 30, ry_), fill=metal, width=5)
        for ex in (mx - 80, mx + 50):
            d.ellipse((ex - 10, my + 26, ex + 10, my + 40), fill=redg)
    else:
        d.pieslice((mx - 110, my - 70, mx + 110, my + 50), 0, 180, fill=bone)
        d.pieslice((mx - 80, my - 50, mx + 80, my + 26), 0, 180, fill=(226, 230, 236))
        for i in range(9):
            tx = mx - 92 + 23 * i
            d.polygon([(tx - 6, my - 10), (tx, my + 4), (tx + 6, my - 10)], fill=bone_d)
        d.line((mx - 108, my - 10, mx + 108, my - 10), fill=red, width=4)
        d.ellipse((mx - 16, my + 42, mx + 16, my + 62), fill=redg)


def ships_plan():
    im = Image.new("RGB", (W, H), TP.GREY)
    d = ImageDraw.Draw(im)
    for k in SHIP_BOXES:
        ship_shapes(k, d)
    return im


def ships_prompt():
    full = (0, 0, W, H)
    return json.dumps({
        "high_level_description": "A pixel art sprite sheet of seven small flying craft of two desert tribes, each one "
                                  "whole and separate with empty space around it, on a plain flat pale grey "
                                  "background, all side views. Top row: four different craft of the Bloodridge "
                                  "raiders, built from scavenged black metal plates in the shapes of ancient Egyptian "
                                  "sun boats, with glowing ember orange engines. Bottom row: three different craft of "
                                  "the Clarth hunters, built from pale sand shark bone on dark metal frames with red "
                                  "trims and glowing red engines. No sails, no masts, no oars, no wood. " + STYLE,
        "style_description": {"medium": "pixel_art", "art_style": RG.L3.ART,
                              "aesthetics": "clean game sprites, readable silhouettes, coherent pixel clusters",
                              "lighting": "Clear morning sun from the upper left, flat pixel colour planes.",
                              "color_palette": ["#221c1e", "#403638", "#5c5052", "#ff8a30", "#ffcc80", "#e2d6ba",
                                                "#aa9c82", "#b0342c", "#3c3a40", "#ff5a3c", "#e2e6ec"]},
        "compositional_deconstruction": {
            "background": "A plain flat pale grey background and nothing else: no sky, no clouds, no water, no ground.",
            "elements": [{"type": "obj", "bbox": bbc(b, full), "desc": SHIP_TEXT[k]} for k, b in SHIP_BOXES.items()]}})


if __name__ == "__main__":
    base_neg = RG.negative().replace(", repeated buildings, duplicated landmarks", "")
    for k in (sys.argv[1:] or ["ships", "m1", "m2", "m3", "ring"]):
        if k == "ships":
            neg = base_neg + (", people, figures, crowds, fire, flames, smoke, text, letters, signs, trees, plants, "
                              "water, sails, masts, oars, rigging, wooden hull, wooden planks, cloth, frames, borders")
            TP.run("v4-ships", ships_plan(), ships_prompt(), neg, SEEDS[k], STEP[k])
        elif k in MACHINES:
            neg = base_neg + (", people, figures, hands, arms, anvil, hammer, wood, planks, crates, barrels, cloth, "
                              "banners, text, letters, frames, borders")
            TP.run("v4-" + k, machine_plan(k), machine_prompt(k), neg, SEEDS[k], STEP[k])
        elif k == "ring":
            neg = base_neg + (", people, figures, bodies, bones, skeletons, weapons, swords, spears, shields, fire, "
                              "flames, smoke, wood, planks, tents, cloth, banners, flags, crates, barrels, ruins, "
                              "buildings, text, letters, frames, borders")
            TP.run("v4-ring", ring_plan(), ring_prompt(), neg, SEEDS[k], STEP[k])
