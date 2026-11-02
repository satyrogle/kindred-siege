import json
import math
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import barren_life as BL
import regions as RG
import stage_paint as SP
import town_fix as TF

# stage_region.py STAGE [paint] [life] | options | region [STAGE ...]
# Jacob, 30 Oct 2026, on the first five Bloodridge stages: "i said use same artstyle layering etc not copy paste the
# same shit and call it a day make bloodridge have more flavour and varirty if you compare where is camp and wheres
# this dog shit", then "yes go" to Claude's plan: five places of their own (red dunes at sunrise, their siege camp at
# noon, the ember canal at dusk, the necropolis by moonlight, the eclipse court), each with its own sky, ground and
# living layer, made the camp's way (same painter and palette rules, three layers, a loop). Stage 1 first, as the
# template. Bloodridge's look is their 3 Oct lock: Medjai desert guardians in Beksinski's mood, rotted sepia rags,
# burial bandages, corroded bronze, lava glowing through cracks. Layout and lanes as stage_paint.py.
# Not ignored, so results can be pushed for review (31 Oct). Under this script's own checkout: Jacob paints from a
# worktree of tools/stage-painter, because his main folder is shared with Codex on game-one. Inputs (sprites, the
# painter's base graph) are still read from RG.REPO, his main folder.
OUT = HERE.parents[1] / "Art/Stages/Bloodridge"
W, H, N, FPS = SP.W, SP.H, 96, 12
WHO = ("A battle stage in the land of the Bloodridge, Egyptian desert guardians after the Medjai, painted in the eerie "
       "dreamlike mood of Zdzislaw Beksinski: rotted sepia rags, burial bandages, corroded bronze, ashen stone and ember "
       "light glowing through cracks. Only the few things named for this place stand here, along the far edge; nothing "
       "else is built anywhere in the picture.")
T = ["#3e302c", "#7a6250", "#96703e", "#f07828"]  # Bloodridge's own sepia, bronze and ember
# The first five (red dunes, siege camp, ember canal, necropolis, eclipse court), 30 Oct, were replaced on 31 Oct
# by the region below; they are in git history (c9b1aa0).
# Jacob, 31 Oct 2026, on stage 1 (the red dunes): pterodactyls are the only animal anywhere, no variety, "what the fuck
# is that head in the background", and "i didn't say literally use [Medjai] culture ... they just look like medjai ...
# but they are all cultish kind of shit, but now you made them into a river of red sand". Asked what the place should
# be: "you built their town for camp same vibe", their settlement on camp 5's right hill (town_paint.py): low tents of
# rotted sepia rags, tall black obelisks with ember cracks, ragged banners with a dark red sun disc, bronze braziers and
# catapults, the pylon gate, bright and readable in full colour. Three takes of stage 1 in that vibe, three different
# places of their land, none on the camp's hill over the sea of clouds (the copied first five were rejected, 30 Oct):
# painted into one picture, numbered 1 to 3, for Jacob to pick from. No dunes, no red sand, no giant head or sword.
WHO_TOWN = ("A battle stage in the land of the Bloodridge, a sun cult of faceless hooded guardians, the same people as "
            "their settlement in the camp: low tents of rotted sepia and ash brown rags, tall black stone obelisks "
            "with thin glowing ember cracks, ragged banners with a dark red sun disc on poles, bronze braziers, ram "
            "skulls and burial bandages hung on poles, temples in the manner of ancient Egypt. Ash brown, sepia and "
            "corroded bronze with ember light, in bright clear full colour. Ember light glows only in the cracks of "
            "the obelisks and in braziers, never on the ground. Only the few things named for this place stand here, "
            "along the far edge; nothing else is built anywhere in the picture.")
AES_TOWN = ("a bright readable battle stage in a real lived in place of a sun cult, coherent pixel clusters, a real "
            "place with depth and scale, readable silhouettes, the open ground calm and even so fighters read on it")
LAY_TOWN = ("One battle stage seen from a fixed side oblique camera like a classic arcade fighting game stage, no "
            "people. The top third is sky and distant land, and the landmarks stand along the far edge of the open "
            "ground with their feet on one line a third of the way down. Everything below that line is one wide open "
            "flat ground where two armies will fight, calm and empty from the left edge to the right edge, with "
            "nothing standing on it.")
TOWN = dict(who=WHO_TOWN, aes=AES_TOWN, lay=LAY_TOWN)
OPTIONS = {
    "1": dict(TOWN, name="THE WAR CAMP", seed=2026103111, note="their army's tents on the valley floor in the morning",
              sky="a clear bright morning sky, deep blue above fading to pale blue at the horizon, a few small white "
                  "clouds",
              far="the long cliffs of the blood red ridge far away across a wide valley, a dark stepped pyramid and a "
                  "temple gate of two pylon towers small on its top, thin columns of smoke rising",
              light="Clear morning sun from the upper left, cool blue shadows, as discrete flat pixel colour planes, "
                    "the ground in calm even light.",
              pal=["#2a6ab8", "#8cc0ea", "#e6f2fa", "#a8423a", "#6e2a2a", "#c8c2bc", "#a49e9a", "#e2d6ba"] + T,
              floor="flat pale ash grey packed earth of the camp ground, trampled smooth, crisp and clear, calm and "
                    "even, no dust and no fog",
              plan=dict(sky0=[52, 110, 190], sky1=[196, 224, 244], far=[150, 62, 56], back=[160, 140, 122],
                        floor0=[200, 194, 188], floor1=[168, 162, 158], piece=[62, 48, 46]),
              back=[((0, 215, W, 290), "rows of low tents of rotted sepia and ash brown rags along the far edge, "
                                       "lines of burial bandages strung between poles"),
                    ((150, 110, 320, 290), "a tall pole hung with ram skulls and a ragged banner with a dark red sun "
                                           "disc, a bronze brazier with ember light at its foot"),
                    ((590, 120, 680, 290), "a tall black stone obelisk with thin glowing ember cracks"),
                    ((900, 100, 1250, 290), "an ancient siege catapult of corroded bronze with a tall throwing arm and "
                                            "a sling, a heap of rough boulders beside it")],
              near=["a dark broken rock", "a toppled bronze brazier and a heap of sepia rags"]),
    "2": dict(TOWN, name="THE OBELISK QUARRY", seed=2026103112, note="where they cut their obelisks, at noon",
              sky="a hard bright noon sky, clear pale blue, almost white near the horizon",
              far="tall quarried cliffs of black stone with bands of rust red rock, cut into huge square steps, small "
                  "rag awnings on the steps far away",
              light="Hard white noon sun from straight above, short dark shadows, as discrete flat pixel colour "
                    "planes, the ground in calm even light.",
              pal=["#5a9ad6", "#cfe4f6", "#ffffff", "#2e2a30", "#54484c", "#9a3a2c", "#d6d2cc", "#b4aea8"] + T,
              floor="flat pale grey cut stone floor of the quarry, big smooth slabs with faint chisel marks, crisp and "
                    "clear, calm and even, no dust, no fog and no rubble",
              plan=dict(sky0=[90, 150, 214], sky1=[232, 240, 246], far=[70, 58, 62], back=[110, 92, 88],
                        floor0=[210, 206, 200], floor1=[178, 174, 170], piece=[44, 38, 42]),
              back=[((0, 225, W, 290), "the foot of the quarry face along the far edge, cut blocks of black stone "
                                       "stacked in rows"),
                    ((110, 40, 320, 290), "a tall black obelisk half carved out of the cliff, still joined to the rock "
                                          "at its back, thin glowing ember cracks in it, a scaffold of poles and rag "
                                          "awnings around it"),
                    ((540, 180, 1000, 290), "a huge finished black obelisk lying on its side on log rollers, bound "
                                            "with thick ropes, ready to be hauled"),
                    ((1080, 70, 1280, 290), "a tall timber crane with a corroded bronze hook and ropes, a ragged banner "
                                            "with a dark red sun disc at its top")],
              near=["a rough black stone block", "a low pile of cut black stone blocks"]),
    "3": dict(TOWN, name="THE SUN SHRINE", seed=2026103113, note="their ritual ground in the late afternoon",
              sky="a warm late afternoon sky, clear blue above fading to warm gold at the horizon, a big bright sun low "
                  "on the right",
              far="the jagged crests of the blood red ridge running away into the distance, lit warm gold, two small "
                  "flying sun boats with crescent hulls and rag sails far away",
              light="Warm late afternoon sun from the right, long violet shadows, as discrete flat pixel colour "
                    "planes, the ground in calm even light.",
              pal=["#3a78c8", "#9cc8ec", "#f8d890", "#f0a850", "#b0443a", "#6a2a2a", "#e4d8c8", "#c0b0a0"] + T,
              floor="flat pale sand coloured paving stones of the shrine court, big worn slabs, crisp and clear, calm "
                    "and even, no dust and no fog, nothing glowing on the floor",
              plan=dict(sky0=[58, 120, 200], sky1=[244, 214, 150], far=[170, 72, 60], back=[150, 120, 100],
                        floor0=[222, 210, 190], floor1=[190, 176, 158], piece=[56, 40, 40]),
              back=[((0, 225, W, 290), "a low wall of dark stone along the far edge of the court, burial bandages "
                                       "fluttering from lines strung over it"),
                    ((460, 50, 920, 290), "a huge round sun disc of corroded bronze standing upright on a stepped "
                                          "altar of dark stone, ember light in the bowls at its foot"),
                    ((140, 70, 330, 290), "two tall black stone obelisks with thin glowing ember cracks, a pole hung "
                                          "with ram skulls between them"),
                    ((1050, 70, 1240, 290), "two tall black stone obelisks with thin glowing ember cracks, a ragged "
                                            "banner with a dark red sun disc between them")],
              near=["a dark broken stone edge", "a bronze offering bowl tipped over and some sepia rags"]),
}


# Jacob, 31 Oct 2026, after the three takes: "i like 3 it adds variety ..layers still suck", then "you have to make it
# like camp scene and make stages for whole region not 1 stage so 5 battles so 5 stages = 1 region with layers cool
# things happening in the back like camp animated stuff and much more and fucking draw new species of flying things and
# add more variety unseen tribes or other designed tribes can also be iun the background", then "do a hybrid of all 3"
# of Claude's three region plans (the road in, the ridge at war, the living land). The hybrid: the road from their
# border to the chief, dawn to eclipse; the war on Skullpeak and the Clarth running through it; other tribes trading,
# praying and hauling. Each stage is built in planes like camp 5: the sky and far land, a middle terrace of smaller
# things further off, the landmarks on the floor line (feet at row 287 between the lanes), nearer pieces at the far
# left and right that stand forward onto the edge of the floor (clear of the leaders and the back row), the calm floor,
# and the near corners. This pass paints the stills only. What moves in each ("moves", from the three plans) is the
# next pass: fighters and other tribes from existing sprites shrunk, new flyers painted on Jacob's GPU as pose sheets.
LAY_REGION = ("One battle stage seen from a fixed side oblique camera like a classic arcade fighting game stage, no "
              "people, with real depth in clear planes like a stage in a modern pixel art game: the sky and the far "
              "land at the top, a middle terrace with smaller things further away, then the landmarks of this place "
              "standing along the far edge of the open ground with their feet on one line a third of the way down, and "
              "at the far left and far right a few nearer pieces standing at the edge of the ground. Everything in the "
              "middle below that line is one wide open flat ground where two armies will fight, calm and empty, with "
              "nothing standing on it.")
REGION = dict(TOWN, lay=LAY_REGION)
STAGES = {
    "1": dict(REGION, name="THE TRIBUTE GATE", seed=2026103121, note="the border of their land at dawn",
              sky="a cool clear dawn sky, deep blue above fading through pink to pale gold at the horizon, a few thin "
                  "lit clouds",
              far="the long blood red ridge far away, lit pink and gold by the dawn, a small dark pyramid on its top",
              mid=[((60, 150, 520, 262), "a long caravan of a foreign tribe waiting in line on a rise far off: covered "
                                         "wagons of woven reeds and a giant shaggy pack beast carrying a tower of "
                                         "bundles"),
                   ((900, 170, 1300, 262), "a watchtower of poles and sepia rags on stilts and a row of low rag tents "
                                           "far off")],
              back=[((0, 232, W, 290), "a low wall of dark stone blocks along the far edge with a line of ragged "
                                       "banners with a dark red sun disc"),
                    ((520, 40, 860, 290), "the border gate: a dark stone temple gate of two sloped pylon towers, ember "
                                          "light in its open doorway"),
                    ((380, 90, 470, 290), "a tall black stone obelisk with thin glowing ember cracks"),
                    ((910, 90, 1000, 290), "a tall black stone obelisk with thin glowing ember cracks")],
              sides=[((0, 200, 300, 380), "a broken block of a pylon with a bronze brazier on it, ember light"),
                     ((1110, 180, W, 370), "a heap of tribute: bronze chests, rolled carpets and clay jars")],
              light="Cool low dawn light from the left, soft violet shadows, as discrete flat pixel colour planes, the "
                    "ground in calm even light.",
              pal=["#2a3e8a", "#7a8ac8", "#f0b0b8", "#f8d890", "#b04a4a", "#e2d6ba", "#c8b8a0"] + T,
              floor="flat pale sand coloured packed road of the border, smooth and worn, crisp and clear, calm and "
                    "even, no dust and no fog",
              plan=dict(sky0=[44, 62, 138], sky1=[244, 206, 170], far=[176, 80, 84], mid=[150, 120, 110],
                        back=[120, 96, 88], floor0=[222, 208, 182], floor1=[192, 178, 154], piece=[56, 42, 42]),
              near=["a dark broken stone edge", "a toppled clay jar and some sepia rags"],
              moves="their sun boats on patrol, a Clarth hunting party caught on the far ridge, the caravan's beast "
                    "shuffling, banners flapping, ash vultures with bandaged necks circling"),
    "2": dict(REGION, name="THE SIEGE LINE", seed=2026103122, note="their war camp in the morning",
              sky="a clear bright morning sky, deep blue above fading to pale blue at the horizon, a few small white "
                  "clouds",
              far="across a deep valley, the next ridge with a walled fortress of grey stone and steel on top, thin "
                  "turquoise glowing slits in its walls, columns of black smoke rising from it",
              mid=[((40, 170, 470, 262), "a captured camp of red hide tents over bone frames on a rise far off, some "
                                         "of them burnt black"),
                   ((880, 170, 1340, 262), "rows of low tents of rotted sepia rags far off, the rest of the war camp")],
              back=[((0, 232, W, 290), "rows of low tents of sepia rags along the far edge, lines of burial bandages "
                                       "strung between poles"),
                    ((390, 60, 700, 290), "a big ancient siege catapult of corroded bronze with its tall throwing arm "
                                          "swung up, a heap of rough boulders beside it"),
                    ((800, 90, 1060, 290), "a second bronze siege catapult with its arm cocked back, and a bronze "
                                           "brazier with ember light")],
              sides=[((0, 150, 330, 370), "a pole hung with ram skulls and a ragged banner with a dark red sun disc, "
                                          "a heap of rough boulders"),
                     ((1090, 170, W, 370), "an empty cage of corroded bronze bars on a cart and a rack of spears")],
              light="Clear morning sun from the upper left, cool blue shadows, as discrete flat pixel colour planes, "
                    "the ground in calm even light.",
              pal=["#2a6ab8", "#8cc0ea", "#e6f2fa", "#8a96a4", "#3ac8c0", "#b03a30", "#c8c2bc", "#a49e9a"] + T,
              floor="flat pale ash grey packed earth of the camp ground, trampled smooth, crisp and clear, calm and "
                    "even, no dust and no fog",
              plan=dict(sky0=[52, 110, 190], sky1=[196, 224, 244], far=[120, 128, 140], mid=[150, 112, 96],
                        back=[130, 110, 96], floor0=[200, 194, 188], floor1=[168, 162, 158], piece=[62, 48, 46]),
              near=["a dark broken rock", "a broken bronze catapult wheel"],
              moves="boulders arcing to the fortress and bursting on it, turquoise rifle flashes firing back, smoke, "
                    "the red hide tents burning, Clarth prisoners led to the cage, carrion flies and bronze shelled "
                    "flying scarabs"),
    "3": dict(REGION, name="THE SUN SHRINE", seed=2026103113, note="their ritual court in the late afternoon",
              sky="a warm late afternoon sky, clear blue above fading to warm gold at the horizon, a big bright sun on "
                  "the right",
              far="the jagged crests of the blood red ridge running away into the distance, lit warm gold",
              mid=[((60, 160, 420, 255), "a camp of pilgrims on a lower terrace far off: patched tents in many "
                                         "colours, foreign and strange, unlike the Bloodridge's"),
                   ((960, 150, 1330, 255), "a stone jetty sticking out from a cliff far off, flying sun boats with "
                                           "crescent hulls and rag sails moored at it")],
              back=[((0, 230, W, 290), "a low wall of dark stone along the far edge of the court, burial bandages "
                                       "fluttering from lines strung over it"),
                    ((470, 40, 910, 290), "a huge round sun disc of corroded bronze engraved with rays and a ring of "
                                          "glyphs, its rim glowing with ember light, standing upright on a stepped "
                                          "altar of dark stone, ember light in the bowls at its foot"),
                    ((330, 60, 450, 290), "a tall black stone obelisk with thin glowing ember cracks"),
                    ((930, 60, 1050, 290), "a tall black stone obelisk with thin glowing ember cracks")],
              sides=[((0, 120, 320, 370), "a tall black obelisk with ember cracks and a pole hung with ram skulls on a "
                                          "raised stone step"),
                     ((1090, 110, W, 370), "a tall black obelisk with ember cracks and a ragged banner with a dark red "
                                           "sun disc on a raised stone step")],
              light="Warm late afternoon sun from high on the right, short soft shadows, as discrete flat pixel colour "
                    "planes, the ground in calm even light.",
              pal=["#3a78c8", "#9cc8ec", "#f8d890", "#f0a850", "#b0443a", "#6a2a2a", "#e4d8c8", "#c0b0a0"] + T,
              floor="flat pale sand coloured paving stones of the shrine court, big worn slabs, crisp and clear, calm "
                    "and even, no dust and no fog, no long shadows across it, nothing glowing on the floor",
              plan=dict(sky0=[58, 120, 200], sky1=[244, 214, 150], far=[170, 72, 60], mid=[160, 120, 100],
                        back=[150, 120, 100], floor0=[222, 210, 190], floor1=[190, 176, 158], piece=[56, 40, 40]),
              near=["a dark broken stone edge", "a bronze offering bowl tipped over and some sepia rags"],
              moves="pilgrims of other tribes kneeling, prisoners marched up the altar steps, sun boats landing and "
                    "leaving, bandages and banners flapping, braziers flickering, huge sun moths around the fire"),
    "4": dict(REGION, name="THE NIGHT MARKET", seed=2026103124, note="their city of the dead by moonlight",
              sky="a clear night sky, deep blue with many stars and a huge pale moon",
              far="the blood red ridge in moonlight, its cliffs cut into rows of tomb fronts with dark doorways, and "
                  "far below them a canal of glowing orange lava crossed by a thin bronze bridge",
              mid=[((80, 160, 520, 262), "rows of small market stalls under rag awnings far off, lanterns hanging "
                                         "from them glowing warm"),
                   ((860, 160, 1300, 262), "more tomb fronts far off and a stair cut into the rock lit by lanterns")],
              back=[((0, 232, W, 290), "a low wall of pale stone along the far edge with lanterns on posts"),
                    ((360, 60, 560, 290), "a tall robed guardian statue of dark stone in an alcove, a deep hood with "
                                          "two ember eyes"),
                    ((820, 60, 1020, 290), "a second tall robed guardian statue of dark stone in an alcove, a deep "
                                           "hood with two ember eyes"),
                    ((600, 150, 780, 290), "a trader's stall under a rag awning: cages of mummified animals, clay "
                                           "jars and a lantern")],
              sides=[((0, 150, 320, 370), "an upright stone sarcophagus wrapped in burial bandages, lanterns around "
                                          "it"),
                     ((1090, 150, W, 370), "a stack of stone coffins and clay jars under a rag awning with a "
                                           "lantern")],
              light="Cool bright moonlight from the upper left and warm lantern light, deep blue shadows, as discrete "
                    "flat pixel colour planes, the ground pale and lit so it reads clearly.",
              pal=["#0e1430", "#1e2a5a", "#3a4a8a", "#dfe6f2", "#8a2a28", "#f0b050", "#c8c8d4", "#9aa0b4"] + T,
              floor="flat pale moonlit flagstones with drifted sand in the seams, crisp and clear, calm and even, lit "
                    "so it reads clearly, no fog",
              plan=dict(sky0=[14, 20, 48], sky1=[40, 54, 104], far=[96, 34, 36], mid=[84, 70, 80],
                        back=[100, 84, 92], floor0=[196, 198, 210], floor1=[160, 162, 178], piece=[30, 24, 32]),
              near=["a toppled stone sarcophagus lid", "a cracked clay jar and a fallen lantern"],
              moves="the statues' ember eyes blinking, a fight on the bronze bridge over the lava, a funeral boat "
                    "drifting past with lanterns, bandaged traders, lanterns swinging, ember bats and bone kites"),
    "5": dict(REGION, name="THE ECLIPSE COURT", seed=2026103125, note="the chief's court under his eclipse",
              sky="a crimson sky with a black sun in it, a burning ring of fire around the black sun",
              far="the dark ridge far away under the crimson sky",
              mid=[((60, 150, 460, 262), "the cult's festival tents far off with long ragged banners and big drums"),
                   ((1000, 120, 1340, 262), "a high dark cliff ledge far off on the right")],
              back=[((0, 230, W, 290), "a low wall of dark stone with tall poles of ragged banners with a dark red "
                                       "sun disc"),
                    ((300, 20, 1080, 290), "a great stepped pyramid of solid dark stone, fully opaque, behind the "
                                           "gate"),
                    ((420, 70, 960, 290), "the dark stone temple gate of two sloped pylon towers, ember light in its "
                                          "tall doorway, the court of the Bloodridge chief")],
              sides=[((0, 120, 320, 370), "a tall pole hung with ram skulls and a big bronze brazier with ember light "
                                          "on a raised step"),
                     ((1090, 120, W, 370), "a tall pole hung with ram skulls and a big bronze brazier with ember "
                                           "light on a raised step")],
              light="Red eclipse light from above with ember glow, deep crimson shadows, as discrete flat pixel colour "
                    "planes, the ground lit so it reads clearly.",
              pal=["#2a0a10", "#6a1418", "#c02a24", "#f06a2a", "#ffc070", "#1a1012", "#e8d8b8", "#b89870"] + T,
              floor="flat polished pale sandstone with a bronze sun disc inlay near the far edge, crisp and clear, "
                    "calm and even, no fog",
              plan=dict(sky0=[60, 10, 18], sky1=[200, 50, 40], far=[90, 30, 30], mid=[110, 50, 44],
                        back=[120, 60, 50], floor0=[220, 200, 166], floor1=[190, 166, 130], piece=[40, 20, 24]),
              near=["a dark broken stone edge", "a bronze drum and a heap of sepia rags"],
              moves="the eclipse ring burning, embers falling, the cult gathered in rows far off, the war stopping to "
                    "kneel, the hunters watching from the cliff, a giant sky whale drifting far off"),
}


def prompt(o):
    els = [{"type": "obj", "bbox": SP.bb((0, 0, W, 170)), "desc": o["sky"]},
           {"type": "obj", "bbox": SP.bb((0, 120, W, 250)), "desc": o["far"]}]
    els += [{"type": "obj", "bbox": SP.bb(b), "desc": d + ", small and far away on a middle terrace"}
            for b, d in o.get("mid", ())]
    els += [{"type": "obj", "bbox": SP.bb(b), "desc": d} for b, d in o["back"]]
    els += [{"type": "obj", "bbox": SP.bb(b), "desc": d + ", nearer, standing at the edge of the open ground"}
            for b, d in o.get("sides", ())]
    els += [{"type": "obj", "bbox": SP.bb((0, SP.FLOOR_Y, W, H)), "desc": o["floor"]},
            {"type": "obj", "bbox": SP.bb(SP.FRAMEBOX[0]), "desc": o["near"][0] + " in the near foreground, in shadow"},
            {"type": "obj", "bbox": SP.bb(SP.FRAMEBOX[1]), "desc": o["near"][1] + " in the near foreground, in shadow"}]
    return json.dumps({"high_level_description": o.get("who", WHO) + " " + SP.STYLE + " No people anywhere, the "
                                                 "fighters are added later. This place: " + o["name"].lower() + ", " +
                                                 o["note"] + ".",
                       "style_description": {"medium": "pixel_art", "art_style": RG.L3.ART,
                                             "aesthetics": o.get("aes", SP.AES), "lighting": o["light"],
                                             "color_palette": o["pal"]},
                       "compositional_deconstruction": {"background": o.get("lay", SP.LAY), "elements": els}})


def plan_region(o):
    """The colour plan in planes, like camp 5: the sky, the far land, a middle terrace, the landmarks' row, then the
    floor, whose far edge wanders a few rows around row 287 so the paint does not meet it in one ruled line (stage 1,
    30 Oct). Soft masses: lighter ones for the middle things (further off), dark ones for the landmarks, the side
    pieces that stand forward onto the floor's edge, and the near corners. Nothing on the floor between them."""
    rng = np.random.default_rng(o["seed"])
    P = {a: np.array(v, float) for a, v in o["plan"].items()}
    knots = rng.uniform(-1, 1, W // 96 + 3)
    edge = (SP.FLOOR_Y + 5 * np.interp(np.arange(W) / 96.0, np.arange(len(knots)), knots)).round().astype(int)
    Y = np.arange(H)[:, None, None]
    a = np.where(Y < 160, P["sky0"] + (P["sky1"] - P["sky0"]) * (Y / 160),
                 np.where(Y < 215, P["far"], np.where(Y < 258, P["mid"], P["back"]))) * np.ones((1, W, 1))
    below = np.arange(H)[:, None] >= edge[None, :]
    fl = P["floor0"] + (P["floor1"] - P["floor0"]) * ((Y - SP.FLOOR_Y) / (H - SP.FLOOR_Y))
    a = np.where(below[..., None], fl * np.ones((1, W, 1)), a)
    blobs = [(SP.shrink(b, 0.85), 130, P["mid"] * 0.85) for b, _ in o.get("mid", ())]
    blobs += [(o["back"][0][0], 90, P["back"] * 0.85)] + [(SP.shrink(b, 0.75), 160, P["piece"]) for b, _ in o["back"][1:]]
    blobs += [(SP.shrink(b, 0.9), 190, P["piece"]) for b, _ in o.get("sides", ())]
    blobs += [((x0, y0, x1, y1 + 160), 200, P["piece"]) for x0, y0, x1, y1 in SP.FRAMEBOX]
    off_floor = np.arange(H)[:, None] > edge[None, :] + 4
    for box, fill, col in blobs:
        m = Image.new("L", (W, H), 0)
        ImageDraw.Draw(m).ellipse(tuple(box), fill=fill)
        al = np.asarray(m.filter(ImageFilter.GaussianBlur(16))).astype(np.float64)
        if box[3] <= SP.FLOOR_Y + 10:
            al[off_floor] = 0  # landmarks never spill onto the floor; side pieces and corners do
        al = al[..., None] / 255
        a = a * (1 - al) + col * al
    small = rng.normal(0, 12, (H // 16 + 2, W // 16 + 2, 3)).astype(np.float32)
    a += np.dstack([np.asarray(Image.fromarray(small[..., c]).resize((W, H), Image.BICUBIC)) for c in range(3)])
    return Image.fromarray(np.clip(a, 0, 255).round().astype(np.uint8))


def paint(k):
    o = STAGES[k]
    SP.OUT, SP.PLAN = OUT, o["plan"]
    OUT.mkdir(parents=True, exist_ok=True)
    init = plan_region(o) if "mid" in o else SP.plan(o)
    init.save(OUT / ("stage-%s-init.png" % k))
    raw = SP.run("stage-%s" % k, init, prompt(o), o["seed"])
    raw.save(OUT / ("stage-%s-raw.png" % k))
    RG.clean(raw).save(OUT / ("stage-%s.png" % k))
    SP.test_fight(k)


def layers(k, a):
    """The game's three files (CampaignArtRegionalStage.cs): ground, the whole painting; rear, everything above the
    floor line; foreground, the dark near corner pieces cut out of the floor."""
    d = OUT / ("stage-%s-layers" % k)
    d.mkdir(exist_ok=True)
    rgba = np.dstack([a, np.full((H, W), 255, np.uint8)])
    Image.fromarray(rgba).save(d / "ground.png")
    rear = rgba.copy()
    rear[SP.FLOOR_Y:, :, 3] = 0
    Image.fromarray(rear).save(d / "rear.png")
    lu = a.astype(float) @ np.array([0.299, 0.587, 0.114])
    floor_lu = np.median(lu[SP.FLOOR_Y + 40:H - 140])
    fg = np.zeros((H, W, 4), np.uint8)
    for x0, y0, x1, y1 in SP.FRAMEBOX:
        x0, y0, x1, y1 = max(0, x0 - 40), y0 - 60, min(W, x1 + 40), y1
        m = lu[y0:y1, x0:x1] < floor_lu - 45
        fg[y0:y1, x0:x1][m] = rgba[y0:y1, x0:x1][m]
    Image.fromarray(fg).save(d / "foreground.png")


CALM = {}  # stage: (colour kept, the colour it leans to, how far). The red dunes had one, (0.7, (198, 168, 123), 0.4):
# their floor came out one loud red and Clarth's red cloaks sank into it (30 Oct look). The region's floors are pale.


def calm(k):
    """The stage's painting, its fight floor calmed where CALM says, faded in below the floor line. Always from the
    raw paint, so running it twice changes nothing."""
    if k not in CALM:
        return np.asarray(Image.open(OUT / ("stage-%s.png" % k)).convert("RGB"))
    keep, col, kk = CALM[k]
    f = np.asarray(RG.clean(Image.open(OUT / ("stage-%s-raw.png" % k)).convert("RGB"))).astype(float)
    lu = (f @ np.array([0.299, 0.587, 0.114]))[..., None]
    m = (lu + (f - lu) * keep) * (1 - kk) + np.array(col, float) * kk
    ramp = np.clip((np.arange(H) - (SP.FLOOR_Y - 4)) / 24.0, 0, 1)[:, None, None]
    a = np.asarray(RG.clean(Image.fromarray((f * (1 - ramp) + m * ramp).clip(0, 255).round().astype(np.uint8))))
    Image.fromarray(a).save(OUT / ("stage-%s.png" % k))
    SP.OUT = OUT
    SP.test_fight(k)
    return a


def life(k):
    """A seamless loop of the stage's living layer: sand blowing off the dune crests (specks that break up, never
    fade), scavengers circling, and the guardian's ember eyes flickering (only ember pixels inside the dark hood; the
    first loop caught the orange sunrise behind the head too)."""
    o, L = STAGES[k], STAGES[k].get("life", {})
    a = calm(k)
    layers(k, a)
    base = a.astype(float)
    rng = np.random.default_rng(o["seed"])
    lu = base @ np.array([0.299, 0.587, 0.114])
    sand = base[SP.FLOOR_Y + 20:SP.FLOOR_Y + 120].reshape(-1, 3)
    sand = sand[np.argsort(sand @ np.array([0.299, 0.587, 0.114]))][-len(sand) // 4:]  # the light sand tones
    y0, y1 = L.get("crest", (205, 290))
    specks = [dict(x=float(rng.uniform(0, W)), y=float(rng.uniform(y0, y1)), t0=int(rng.integers(0, N)),
                   life=int(rng.integers(14, 30)), vx=float(rng.uniform(1.6, 3.0)), vy=float(rng.uniform(-0.7, -0.2)),
                   c=sand[int(rng.integers(0, len(sand)))]) for _ in range(150)]
    eyes = np.zeros((H, W), bool)
    if L.get("embers"):
        bx0, by0, bx1, by1 = o["back"][1][0]
        e = (base[..., 0] > 200) & (base[..., 1] > 70) & (base[..., 1] < 190) & (base[..., 2] < 110)
        dark = np.asarray(Image.fromarray(lu.clip(0, 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(7))) < 95
        eyes[by0:by1, bx0:bx1] = (e & dark)[by0:by1, bx0:bx1]
    pt = BL.ptero_frames("far")
    sky = lu > -1
    sky[SP.FLOOR_Y - 60:] = False
    frames = []
    for t in range(N):
        cv = base.copy()
        if eyes.any():
            g = (1.0, 1.18, 1.05, 0.85, 1.12, 1.0)[(t // 2) % 6]
            cv[eyes] = np.clip(base[eyes] * g, 0, 255)
        for i, (cx, cy, rx, ry) in enumerate(L.get("birds", ())):
            for j in range(2):
                ph = 2 * math.pi * (t / N + j * 0.5 + i * 0.21)
                rgb, al = pt[(t // 2 + j * 3) % len(pt)]
                TF.put(cv, rgb[:, ::-1] if math.cos(ph) < 0 else rgb, al[:, ::-1] if math.cos(ph) < 0 else al,
                       cx + rx * math.sin(ph) - al.shape[1] / 2, cy + ry * math.cos(ph) - al.shape[0] / 2, vis=sky)
        for s in specks:
            u = (t - s["t0"]) % N
            if u >= s["life"]:
                continue
            x, y = s["x"] + s["vx"] * u, s["y"] + s["vy"] * u + 0.02 * u * u
            n = 2 if u < s["life"] * 0.5 else 1  # a short streak, then a single speck, then gone
            for q in range(n):
                xi, yi = int(x - q) % W, int(y)
                if 0 <= yi < SP.FLOOR_Y + 20:
                    cv[yi, xi] = s["c"]
        frames.append(cv.clip(0, 255).astype(np.uint8))
    dst = OUT / ("stage-%s-life.mp4" % k)
    p = subprocess.Popen([BL.FF, "-v", "error", "-y", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", "%dx%d" % (W, H),
                          "-r", str(FPS), "-i", "-", "-vf", "scale=%d:%d:flags=neighbor" % (W * 2, H * 2),
                          "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "16", str(dst)], stdin=subprocess.PIPE)
    for _ in range(3):
        for f in frames:
            p.stdin.write(f.tobytes())
    p.stdin.close()
    p.wait()
    strip = Image.new("RGB", (W, 3 * (H // 2) + 8))
    for i, t in enumerate((0, 32, 64)):
        strip.paste(Image.fromarray(frames[t]).resize((W // 2, H // 2), Image.BOX), ((i % 2) * (W // 2), (i // 2) * (H // 2 + 4)))
    Image.open(OUT / ("stage-%s-test-fight.png" % k)).resize((W // 2, H // 2), Image.BOX).save(OUT / "tmp.png")
    strip.paste(Image.open(OUT / "tmp.png"), (W // 2, H // 2 + 4))
    (OUT / "tmp.png").unlink()
    strip.save(OUT / ("look-stage-%s.png" % k))
    print("life", k, dst.name, "frames", N, "eye px", int(eyes.sum()), flush=True)


def options():
    """The three takes of stage 1 in OPTIONS, each painted as paint() does, then one picture of their test lineups
    numbered 1 to 3: look-stage-1-options.png. Paint only, no layers and no loop until Jacob picks."""
    SP.OUT = OUT
    OUT.mkdir(parents=True, exist_ok=True)
    for i, o in sorted(OPTIONS.items()):
        k = "1-option-" + i
        SP.PLAN = o["plan"]
        init = SP.plan(o)
        init.save(OUT / ("stage-%s-init.png" % k))
        raw = SP.run("stage-" + k, init, prompt(o), o["seed"])
        raw.save(OUT / ("stage-%s-raw.png" % k))
        RG.clean(raw).save(OUT / ("stage-%s.png" % k))
        SP.test_fight(k)
    try:
        font = ImageFont.truetype("arial.ttf", 22)
    except OSError:
        font = ImageFont.load_default()
    bar = 36
    s = Image.new("RGB", (W, 3 * (H + bar)), (18, 14, 20))
    d = ImageDraw.Draw(s)
    for j, (i, o) in enumerate(sorted(OPTIONS.items())):
        y = j * (H + bar)
        s.paste(Image.open(OUT / ("stage-1-option-%s-test-fight.png" % i)).convert("RGB"), (0, y + bar))
        d.text((10, y + 7), "%s   %s, %s" % (i, o["name"], o["note"]), fill=(239, 223, 190), font=font)
    s.save(OUT / "look-stage-1-options.png")
    print("options", "look-stage-1-options.png", flush=True)


def region(ks):
    """The region's stages painted, stills only (all five, or the numbers given), then one picture of every stage
    painted so far with its test lineup, numbered 1 to 5, at half size so it pastes into a chat: look-region.png."""
    for k in ks:
        paint(k)
    try:
        font = ImageFont.truetype("arial.ttf", 18)
    except OSError:
        font = ImageFont.load_default()
    tw, th, bar = W // 2, H // 2, 30
    s = Image.new("RGB", (2 * tw + 8, 3 * (th + bar)), (18, 14, 20))
    d = ImageDraw.Draw(s)
    for i, k in enumerate(sorted(STAGES)):
        p = OUT / ("stage-%s-test-fight.png" % k)
        if not p.exists():
            continue
        x, y = (i % 2) * (tw + 8), (i // 2) * (th + bar)
        s.paste(Image.open(p).convert("RGB").resize((tw, th), Image.BOX), (x, y + bar))
        d.text((x + 8, y + 5), "%s   %s, %s" % (k, STAGES[k]["name"], STAGES[k]["note"]), fill=(239, 223, 190),
               font=font)
    s.save(OUT / "look-region.png")
    print("region", "look-region.png", flush=True)


if __name__ == "__main__":
    k = sys.argv[1]
    if k == "options":
        options()
        sys.exit()
    if k == "region":
        region(sys.argv[2:] or sorted(STAGES))
        sys.exit()
    if "life" not in sys.argv[2:]:
        paint(k)
    if "paint" not in sys.argv[2:]:
        life(k)
