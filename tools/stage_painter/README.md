# Stage painter

Paints Kindred Siege battle stages on Jacob's own GPU (Ideogram 4 in ArtLab), splits them into the game's three
layer files and renders a short living loop. Placeholder art, like all AI art in this project (DESIGN_LOCKS, 22 Oct).

The painting only runs on Jacob's PC: it needs ArtLab on port 8191 and his GPU. A cloud session can read and change
these scripts, but it cannot paint.

## Files

- `stage_region.py` is the one to run. It holds the five Bloodridge stages (red dunes, siege camp, ember canal,
  necropolis, eclipse court), each with its own sky, ground, light, palette, landmarks and living layer.
- `stage_paint.py` holds the layout shared by every stage (floor line at row 287, fight lanes, test lineup) and the
  call to the painter.
- `regions.py`, `barren_life.py`, `town_fix.py` and the rest are helpers copied from the camp work.
- `workflow-api.json` is the painter's base graph (also at Art/Studies/Stage-Pixel-Match-20261018/skullpeak on Jacob's
  PC, which is where the scripts read it from).

Known bug: importing the camp helpers adds the camp's bans (tents, banners, statues, buildings, ruins, bones) to the
painter's negative prompt, so stages get those banned too. Fix it in `stage_paint.negative()` before the next paint.

## On Jacob's PC (cmd)

Start the painter once per session:

```
powershell -ExecutionPolicy Bypass -File C:\Users\jacob\ComfyUI-Installs\ComfyUI\Start-ArtLab.ps1
```

Paint one stage, its layers and its loop (swap 1 for the stage number):

```
cd /d C:\Users\jacob\KindredSiege-Clean\tools\stage_painter
python stage_region.py 1
```

Send the results back for review:

```
cd /d C:\Users\jacob\KindredSiege-Clean
git add Art/Stages/Bloodridge
git commit -m "stages: Bloodridge stage 1 paint"
git push origin tools/stage-painter
```

## Output

`Art/Stages/Bloodridge/`: `stage-N.png` (the painting), `stage-N-raw.png`, `stage-N-init.png` (the colour plan),
`stage-N-test-fight.png` (test lineup on the game's lanes), `stage-N-layers/` (ground, rear and foreground, the files
`CampaignArtRegionalStage.cs` loads), `stage-N-life.mp4` (the loop) and `look-stage-N.png` (three loop frames plus the
lineup).
