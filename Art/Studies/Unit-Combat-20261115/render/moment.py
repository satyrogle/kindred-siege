"""A close strip of one moment of a film: consecutive frames cropped around a point, 1x, under 1900 px wide.
usage: moment.py film.mp4 out.png first_frame count step cx cy [w h]"""
import subprocess, sys
from PIL import Image, ImageDraw

FF = 'C:/Users/jacob/Desktop/PR/dark-lattice-site/node_modules/ffmpeg-static/ffmpeg.exe'
film, out = sys.argv[1], sys.argv[2]
first, count, step = int(sys.argv[3]), int(sys.argv[4]), int(sys.argv[5])
cx, cy = int(sys.argv[6]), int(sys.argv[7])
w = int(sys.argv[8]) if len(sys.argv) > 8 else 360
h = int(sys.argv[9]) if len(sys.argv) > 9 else 300
W, H = 1440, 900
x0 = max(0, min(W - w, cx - w // 2)); y0 = max(0, min(H - h, cy - h // 2))
last = first + count * step
vf = f"select='between(n\\,{first}\\,{last - 1})*not(mod(n-{first}\\,{step}))',crop={w}:{h}:{x0}:{y0}"
raw = subprocess.run([FF, '-hide_banner', '-loglevel', 'error', '-i', film, '-vf', vf, '-vsync', '0', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-'], capture_output=True).stdout
n = len(raw) // (w * h * 3)
cols = max(1, 1900 // w)
rows = (n + cols - 1) // cols
sheet = Image.new('RGB', (cols * w, rows * (h + 14)), (20, 20, 24))
d = ImageDraw.Draw(sheet)
for k in range(n):
    im = Image.frombytes('RGB', (w, h), raw[k * w * h * 3:(k + 1) * w * h * 3])
    x, y = (k % cols) * w, (k // cols) * (h + 14)
    sheet.paste(im, (x, y))
    d.text((x + 3, y + h + 1), f'f{first + k * step}', fill=(255, 230, 90))
sheet.save(out)
print(out, n, 'frames', sheet.size)
