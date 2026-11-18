"""Audit sheets: every 200 ms of a film, cropped to the fight floor, 20 frames a sheet, each under 1900 px wide.
usage: audit_sheets.py film.mp4 outdir [step_ms] [scale]"""
import json, os, subprocess, sys
from PIL import Image, ImageDraw

FF = 'C:/Users/jacob/Desktop/PR/dark-lattice-site/node_modules/ffmpeg-static/ffmpeg.exe'
film, out = sys.argv[1], sys.argv[2]
step_ms = int(sys.argv[3]) if len(sys.argv) > 3 else 200
scale = float(sys.argv[4]) if len(sys.argv) > 4 else 0.4
os.makedirs(out, exist_ok=True)
W, H = 1440, 900
crop = (150, 300, 1310, 830)
tl = {}
try:
    for f, t in json.load(open(film + '.timeline.json')):
        tl[f] = t
except Exception:
    pass
fps = 30
if len(tl) > 2:                                    # the film's rate, read off its timeline (50 ms ticks): 30 or 60
    ks = sorted(tl); dt = tl[ks[-1]] - tl[ks[-2]]
    if dt > 0: fps = round(20 / dt)
every = max(1, round(step_ms * fps / 1000))
cw, ch = int((crop[2] - crop[0]) * scale), int((crop[3] - crop[1]) * scale)
# ffmpeg keeps only every Nth frame, already cropped and shrunk, so memory stays small
vf = f"select='not(mod(n\\,{every}))',crop={crop[2] - crop[0]}:{crop[3] - crop[1]}:{crop[0]}:{crop[1]},scale={cw}:{ch}:flags=neighbor"
raw = subprocess.run([FF, '-hide_banner', '-loglevel', 'error', '-i', film, '-vf', vf, '-vsync', '0', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-'], capture_output=True).stdout
count = len(raw) // (cw * ch * 3)
picks = [k * every for k in range(count)]
n = count * every
cols, rows = 4, 5
for s in range(0, len(picks), cols * rows):
    sheet = Image.new('RGB', (cols * cw, rows * (ch + 14)), (20, 20, 24))
    d = ImageDraw.Draw(sheet)
    for k, fi in enumerate(picks[s:s + cols * rows]):
        j = s + k
        im = Image.frombytes('RGB', (cw, ch), raw[j * cw * ch * 3:(j + 1) * cw * ch * 3])
        x, y = (k % cols) * cw, (k // cols) * (ch + 14)
        sheet.paste(im, (x, y))
        tick = tl.get(fi)
        d.text((x + 3, y + ch + 1), f'f{fi} {fi / fps:.1f}s' + (f' tick {tick:.1f}' if tick is not None else ''), fill=(255, 230, 90))
    p = os.path.join(out, f'sheet_{s // (cols * rows):02d}.png')
    sheet.save(p)
print(n, 'frames,', len(picks), 'picks,', (len(picks) + cols * rows - 1) // (cols * rows), 'sheets in', out)
