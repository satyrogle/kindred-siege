"""Unit combat study renderer (Claude, 15 Nov 2026).

Reads the trace the C# fight sim wrote (sim/Brawl.cs) and films it with the game's own approved art:
the fight sheets in Assets/Resources/ClarthStudy (idle, attack, hit, death), Codex's Clarth step clips (Pairs45),
the new Bloodridge runs (art/runs_new, PixelLab started from each fighter's real idle frame), the 7 Nov attack table
effects (PixelLab VFX), the 6 Nov bone jaws snare, the game's words and fonts.

Presentation only. It never changes what the sim decided: freezes and knockback hold the screen's clocks, not the
sim's ticks (fight-feel). usage: render.py trace.json out.mp4 [--sheet-every-ms 200]
"""
import io, json, math, os, subprocess, sys
from PIL import Image, ImageDraw

REPO = 'C:/Users/jacob/KindredSiege-Clean/'
RES = REPO + 'Assets/Resources/'
STUDY = REPO + 'Art/Studies/Unit-Combat-20261115/'
FX = REPO + 'Art/Studies/Attack-Table-20261107/effects/'
FF = 'C:/Users/jacob/Desktop/PR/dark-lattice-site/node_modules/ffmpeg-static/ffmpeg.exe'
FPS = 30
TICK_MS = 50
W, H = 1440, 900
COURT = REPO + 'Assets/StreamingAssets/CampaignArt/Bloodridge/'

# ---------------------------------------------------------------- clips

class Clip:
    def __init__(self, frames, durs, ax, ay):
        self.frames, self.durs, self.ax, self.ay = frames, durs, ax, ay
        self.total = max(1, sum(durs))
        self._tint = {}

    def index(self, ms, loop=False):
        if loop:
            ms %= self.total
        acc = 0
        for i, d in enumerate(self.durs):
            acc += d
            if ms < acc:
                return i
        return len(self.frames) - 1

    def tinted(self, i, rgb):
        key = (i, rgb)
        if key not in self._tint:
            f = self.frames[i]
            a = f.split()[3]
            solid = Image.new('RGBA', f.size, rgb + (255,))
            solid.putalpha(a)
            self._tint[key] = solid
        return self._tint[key]

def sheet(base, fixed_ms=None):
    j = json.load(open(RES + base + '.json'))
    im = Image.open(RES + base + '.png').convert('RGBA')
    frames, durs = [], []
    for f in j['frames']:
        r = f['frame']
        frames.append(im.crop((r['x'], r['y'], r['x'] + r['w'], r['y'] + r['h'])))
        durs.append(fixed_ms or f.get('duration', 50))
    return Clip(frames, durs, j['anchorX'], j['anchorY'])

def pngs(folder, idx, ax, ay, ms):
    frames = [Image.open(os.path.join(folder, f'{i}.png')).convert('RGBA') for i in idx]
    return Clip(frames, [ms] * len(frames), ax, ay)

def vfx(name, part, flip=False):
    """One part of a PixelLab VFX Creator effect, as a clip anchored at the effect's own origin."""
    base = FX + name + '/game/'
    man = json.load(open(base + 'manifest.json'))
    clip = next(c for c in man['clips'] if c['name'] == part)
    fw, fh = clip['frame']['w'], clip['frame']['h']
    frames = []
    for sh in clip['sheets']:
        im = Image.open(base + sh['file']).convert('RGBA')
        for k in range(sh['frames']):
            cx, cy = (k % sh['columns']) * fw, (k // sh['columns']) * fh
            f = im.crop((cx, cy, cx + fw, cy + fh))
            frames.append(f.transpose(Image.FLIP_LEFT_RIGHT) if flip else f)
    ms = man.get('frame_ms', 63)
    return Clip(frames, [ms] * len(frames), fw // 2, fh // 2)

def extract(frame, box, pred):
    """The pixels of one frame inside box that pass pred, as a small sprite and its centre on the frame."""
    x0, y0, x1, y1 = [int(v) for v in box]
    px = frame.load()
    out = Image.new('RGBA', (x1 - x0, y1 - y0), (0, 0, 0, 0))
    po = out.load()
    for y in range(max(0, y0), min(frame.height, y1)):
        for x in range(max(0, x0), min(frame.width, x1)):
            p = px[x, y]
            if p[3] > 100 and pred(p):
                po[x - x0, y - y0] = p
    bb = out.getbbox()
    if not bb:
        return None, (0, 0)
    spr = out.crop(bb)
    return spr, (x0 + bb[0] + spr.width / 2, y0 + bb[1] + spr.height / 2)

def bright(p):
    mx, mn = max(p[:3]), min(p[:3])
    return mx > 200 and (mx - mn) < 110

def hot(p):
    r, g, b = p[:3]
    return r > 200 and g > 60 and b < 140 and r - b > 90

# ---------------------------------------------------------------- the game's fonts and words

GLYPHS = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.,!?:;\'"-+()0123456789/%*=_$'

def font(path, cell):
    im = Image.open(io.BytesIO(open(RES + path, 'rb').read())).convert('RGBA')
    cols = im.width // cell
    out = {}
    for i, ch in enumerate(GLYPHS):
        cx, cy = (i % cols) * cell, (i // cols) * cell
        g = im.crop((cx, cy, cx + cell, cy + cell))
        out[ch] = g
    return out

FONT_BIG = font('CampaignArt/Menu/font_logo.bytes', 32)
FONT_TINY = font('CampaignArt/Menu/font_tiny.bytes', 8)

def text_img(text, rgb, fnt, scale=1, outline=(20, 12, 14)):
    glyphs = []
    for ch in text:
        if ch == ' ':
            glyphs.append(None)
            continue
        g = fnt.get(ch) or fnt.get(ch.upper())
        glyphs.append(g)
    cell = next(g for g in glyphs if g is not None).width
    parts, x = [], 0
    for g in glyphs:
        if g is None:
            x += cell // 3
            continue
        bb = g.getbbox() or (0, 0, cell, cell)
        parts.append((g.crop((bb[0], 0, bb[2], cell)), x))
        x += bb[2] - bb[0] + max(1, cell // 10)
    img = Image.new('RGBA', (x + 2, cell + 2), (0, 0, 0, 0))
    for g, gx in parts:
        a = g.split()[3]
        col = Image.new('RGBA', g.size, rgb + (255,)); col.putalpha(a)
        dark = Image.new('RGBA', g.size, outline + (255,)); dark.putalpha(a)
        for dx, dy in ((0, 1), (1, 0), (1, 1), (2, 1), (1, 2)):
            img.alpha_composite(dark, (gx + dx, dy))
        img.alpha_composite(col, (gx + 1, 1))
    bb = img.getbbox()
    img = img.crop(bb) if bb else img
    if scale != 1:
        img = img.resize((img.width * scale, img.height * scale), Image.NEAREST)
    return img

def ui(path):
    return Image.open(RES + 'CampaignArt/UI/' + path).convert('RGBA')

MARK_RGB = {'opened': (240, 138, 36), 'pinned': (127, 224, 194), 'ripped': (214, 40, 46), 'hexed': (150, 200, 64)}
WORDS = {
    'opened': ui('Marks/cash_opened.png'), 'pinned': ui('Marks/cash_pinned.png'), 'ripped': ui('Marks/cash_ripped.png'),
    'hexed': text_img('HEXED', MARK_RGB['hexed'], FONT_BIG),
    'snare': ui('Words43/snare.png'), 'bleed': ui('Words43/bleed.png'), 'opening': ui('Words43/opening.png'),
    'pin': ui('Words43/pin.png'), 'sidestep': ui('Words43/sidestep.png'), 'snaptrap': ui('Words43/snaptrap.png'),
    'twincut': ui('Words43/twincut.png'), 'rearm': ui('Words43/rearm.png'),
    'hex': text_img('HEX', MARK_RGB['hexed'], FONT_BIG),
    'brace': text_img('BRACE', (226, 214, 190), FONT_BIG),
    'stepback': text_img('STEP BACK', (226, 214, 190), FONT_BIG),
    'evileye': text_img('EVIL EYE', MARK_RGB['hexed'], FONT_BIG),
    'badblood': text_img('BAD BLOOD', MARK_RGB['ripped'], FONT_BIG),
    'steady': text_img('STEADY', (226, 214, 190), FONT_BIG),
    'broken': text_img('BROKEN', (240, 138, 36), FONT_BIG),
}
MISS = text_img('MISS', (210, 206, 196), FONT_TINY, 2)
BLOCK = text_img('BLOCK', (176, 176, 176), FONT_TINY, 2)

# ---------------------------------------------------------------- the fighters' art

def gold_fix(clip):
    """Bloodridge's Marksman sheet carries the parry cue's gold body flash on every shot. Gold is the parry's alone
    (L1303), and nobody parries here, so each gold frame becomes the frame before it plus that frame's muzzle flash."""
    def golden(f):
        n = 0
        for p in f.getdata():
            if p[3] > 128 and p[0] > 190 and p[1] > 150 and p[2] < 120:
                n += 1
        return n
    fixed = list(clip.frames)
    base = clip.frames[0]
    for i, f in enumerate(clip.frames):
        if golden(f) > 60:
            prev = fixed[i - 1] if i > 0 else base
            out = prev.copy()
            pa, fa = prev.split()[3].load(), f.load()
            po = out.load()
            for y in range(f.height):
                for x in range(f.width):
                    if pa[x, y] < 40 and fa[x, y][3] > 100:
                        po[x, y] = fa[x, y]
            fixed[i] = out
    clip.frames = fixed
    return clip

class Art:
    def __init__(self, unit):
        side = unit['side']
        c = 'medic' if unit['cls'] == 'Hexer' else unit['cls'].lower()
        self.cls = unit['cls']
        if side == 0:
            b = f'ClarthStudy/clarth_{c}_se_'
            self.run = sheet(f'CampaignArt/Pairs45/clarth_{c}_se_step', fixed_ms=80)
        else:
            b = f'ClarthStudy/blood_rot_{c}_sw_'
            size = Image.open(STUDY + f'art/runs_new/{c}/1.png').width
            self.run = pngs(STUDY + f'art/runs_new/{c}', range(1, 9), size / 2, size - 14, 85)
        self.idle = sheet(b + 'idle')
        self.attack = sheet(b + 'attack')
        self.hit = sheet(b + 'hit')
        self.death = sheet(b + 'death')
        if side == 1 and c == 'marksman':
            gold_fix(self.attack)
        # the Hexer's curse: the orb and the burst come out of her own cast frames
        self.bolt = self.burst = None
        if c == 'medic':
            ax, ay = self.attack.ax, self.attack.ay
            if side == 0:
                orb, _ = extract(self.attack.frames[3], (ax + 10, ay - 80, ax + 60, ay - 25), bright)
                self.bolt = [orb] if orb else None
                bursts = []
                for k in (7, 8, 9, 10):
                    spr, _ = extract(self.attack.frames[k], (ax - 10, ay - 46, ax + 70, ay + 30), bright)
                    if spr: bursts.append(spr)
                self.burst = bursts
            else:
                tip, _ = extract(self.attack.frames[15], (ax - 70, ay - 40, ax + 10, ay + 30), hot)
                self.bolt = [tip] if tip else None
                bursts = []
                for k in (16, 17, 18, 19):
                    spr, _ = extract(self.attack.frames[k], (ax - 70, ay - 34, ax + 20, ay + 30), lambda p: hot(p) or bright(p))
                    if spr: bursts.append(spr)
                self.burst = bursts

# ---------------------------------------------------------------- effects

class Fx:
    """One drawn thing with its own clock in video frames (it keeps moving through freezes)."""
    def __init__(self, f0, x, y, clip=None, img=None, life=None, rise=0, layer=1, loop=False, hold=0, flip=False):
        self.f0, self.x, self.y, self.clip, self.img = f0, x, y, clip, img
        self.rise, self.layer, self.loop, self.hold, self.flip = rise, layer, loop, hold, flip
        self.life = life if life is not None else (int(clip.total * FPS / 1000) + 1 + hold if clip else 20)
        self.move = None   # (x0, y0, x1, y1, frames)

    def alive(self, f):
        return f - self.f0 < self.life

    def draw(self, canvas, f, ox, oy):
        age = f - self.f0
        x, y = self.x, self.y
        if self.move:
            x0, y0, x1, y1, n = self.move
            k = min(1.0, age / max(1, n))
            x, y = x0 + (x1 - x0) * k, y0 + (y1 - y0) * k - math.sin(math.pi * k) * 26
        if self.clip:
            ms = age * 1000 / FPS
            i = self.clip.index(ms, loop=self.loop)
            spr = self.clip.frames[i]
            canvas.alpha_composite(spr, (int(x - self.clip.ax + ox), int(y - self.clip.ay + oy)))
        elif self.img is not None:
            dy = -self.rise * min(1.0, age / 12)
            canvas.alpha_composite(self.img, (int(x - self.img.width / 2 + ox), int(y - self.img.height / 2 + dy + oy)))

# ---------------------------------------------------------------- the film

def main():
    trace_path, out_path = sys.argv[1], sys.argv[2]
    tr = json.load(open(trace_path))
    units = tr['units']
    frames = tr['frames']
    events = tr['events']
    last = len(frames) - 1
    # the accepted burial court, stacked the way CampaignArtBloodridgeReview.cs stacks it; its foreground goes over us
    stage = Image.open(COURT + 'ground-underpaint.png').convert('RGBA')
    for layer in ('rear-burial-architecture.png', 'rooted-entrance.png'):
        stage.alpha_composite(Image.open(COURT + layer).convert('RGBA'))
    front = Image.open(COURT + 'foreground.png').convert('RGBA')

    arts = [Art(u) for u in units]
    hit_star = {s: vfx('hit', 'part-hit_star', flip=(s == 1)) for s in (0, 1)}
    crit = {s: vfx('crit', 'part-crit_hit', flip=(s == 1)) for s in (0, 1)}
    sparks = {s: vfx('block', 'part-sparks_shower', flip=(s == 1)) for s in (0, 1)}
    dust = {s: vfx('dodge', 'part-dust_kick', flip=(s == 1)) for s in (0, 1)}
    blood = sheet('CampaignArt/Physical45/blood')
    jaws_dir = REPO + 'Art/Studies/Class-Moves-20261106/snare/v2-props/parts/jaws_anim'
    jaw_frames = [Image.open(os.path.join(jaws_dir, f'{i}.png')).convert('RGBA') for i in range(9)]
    jaws = Clip(jaw_frames, [60] * 9, jaw_frames[0].width / 2, jaw_frames[0].height * 0.72)

    by_tick = {}
    for e in events:
        by_tick.setdefault(e['t'], []).append(e)

    def state(t, i):
        s = frames[max(0, min(last, t))][i]
        return {'x': s[0] / 100, 'y': s[1] / 100, 'act': s[2], 'move': s[3], 'cf': s[4], 'age': s[5], 'hp': s[6], 'marks': s[7], 'tgt': s[8], 'mt': s[9]}

    fxs = []
    flash = {}       # unit -> (f0, tier)
    knock = {}       # unit -> (f0, px, dir)
    numbers = {}     # unit -> [f0, amount, tier, colour]
    bar_lag = {i: u['maxHp'] for i, u in enumerate(units)}
    word = None      # [f0, img, x, y, prio]
    trauma = 0.0
    trail = {i: [] for i in range(len(units))}

    proc = subprocess.Popen([FF, '-hide_banner', '-loglevel', 'error', '-y', '-f', 'rawvideo', '-pix_fmt', 'rgb24',
                             '-s', f'{W}x{H}', '-r', str(FPS), '-i', '-', '-c:v', 'libx264', '-preset', 'medium',
                             '-crf', '17', '-pix_fmt', 'yuv420p', out_path], stdin=subprocess.PIPE)

    sim_t, f, done_tick, freeze, last_freeze_end = 0.0, 0, -1, 0, -99
    jitter_units = set()
    step = 1000 / TICK_MS / FPS   # sim ticks per video frame
    end_t = last
    timeline = []                  # (video frame, sim tick) for the audit sheets

    def set_word(img, x, y, prio):
        nonlocal word
        if word is None or prio >= word[4] or f - word[0] > 14:
            word = [f, img, x, y - 112, prio]

    def spawn(e):
        nonlocal trauma
        k, a, b = e['k'], e['a'], e['b']
        if k == 'hit':
            v = state(e['t'], b)
            at = state(e['t'], a) if a >= 0 else v
            side = units[a]['side'] if a >= 0 else 0
            tier = e['tier']
            flash[b] = (f, tier)
            d = 1 if v['x'] >= at['x'] else -1
            knock[b] = (f, (3, 7, 10, 12)[min(3, tier)], d)
            ex, ey = e['x'], e['y']
            if e['d'] == 'blocked':
                fxs.append(Fx(f, ex, ey, sparks[side], layer=2))
                set_word(BLOCK, v['x'], v['y'] + 20, 0)
            elif tier >= 1:
                fxs.append(Fx(f, ex, ey, crit[side], layer=2))
                if e['kind'] in ('Blade', 'Hold'):
                    fxs.append(Fx(f, v['x'] + d * 8, v['y'] - 30, blood, layer=2))
            else:
                fxs.append(Fx(f, ex, ey, hit_star[side], layer=2))
            col = MARK_RGB.get(e['d'], (250, 246, 236) if tier == 0 else (255, 226, 120))
            n = numbers.get(b)
            if n and f - n[0] < 18:
                n[0], n[1], n[2], n[3] = f, n[1] + e['n'], max(n[2], tier), col if tier >= n[2] else n[3]
            else:
                numbers[b] = [f, e['n'], tier, col]
            if e['d'] in WORDS and tier == 2:
                set_word(WORDS[e['d']], v['x'], v['y'], 5)
                trauma = min(1.0, trauma + 0.35)
        elif k == 'bleedtick':
            n = numbers.get(b)
            if n and f - n[0] < 36:
                n[1] += e['n']
            else:
                numbers[b] = [f, e['n'], 0, MARK_RGB['ripped']]
        elif k == 'kill':
            trauma = min(1.0, trauma + 0.6)
        elif k == 'miss':
            v = state(e['t'], b) if b >= 0 else None
            if v:
                set_word(MISS, v['x'], v['y'] + 30, 1)
        elif k == 'dodge':
            v = state(e['t'], a)
            fxs.append(Fx(f, v['x'], v['y'] - 20, dust[units[a]['side']], layer=0))
        elif k == 'mark':
            v = state(e['t'], b)
            if e['d'] == 'snare':
                fxs.append(Fx(f, v['x'], v['y'], jaws, layer=0, hold=int(2.0 * FPS)))
                set_word(WORDS['snare'], v['x'], v['y'], 3)
            elif e['d'] == 'opening' and a != b:
                set_word(WORDS['opening'], v['x'], v['y'], 2)
            elif e['d'] == 'bleed' and e.get('kind', '').startswith('from:'):
                fxs.append(Fx(f, v['x'], v['y'] - 40, blood, layer=2))
        elif k == 'defence':
            v = state(e['t'], a)
            w = {'brace': 'brace', 'sidestep': 'sidestep', 'step back': 'stepback', 'snap trap': 'snaptrap', 'evil eye': 'evileye'}.get(e['d'])
            if w:
                set_word(WORDS[w], v['x'], v['y'], 4)
        elif k == 'boost':
            pass   # Steady and Rearm are quiet: their effect is the word's job only when it lands (one word per blow)
        elif k == 'boostmove':
            v = state(e['t'], a)
            w = {'pin': 'pin', 'twin': 'twincut', 'badblood': 'badblood'}.get(e['d'].split(':')[0])
            if w:
                set_word(WORDS[w], v['x'], v['y'], 3)
        elif k == 'broken':
            v = state(e['t'], b)
            set_word(WORDS['broken'], v['x'], v['y'], 4)
        elif k == 'launch':
            art = arts[a]
            if art.bolt:
                v = state(e['t'], b)
                fx = Fx(f, e['x'], e['y'], img=art.bolt[0], life=int(e['n'] * TICK_MS * FPS / 1000) + 1, layer=2)
                fx.move = (e['x'], e['y'], v['x'], v['y'] - 40, int(e['n'] * TICK_MS * FPS / 1000))
                fxs.append(fx)
        elif k == 'burst':
            art = arts[a]
            if art.burst:
                c = Clip(art.burst, [70] * len(art.burst), art.burst[0].width / 2, art.burst[0].height * 0.7)
                fxs.append(Fx(f, e['x'], e['y'] + 40, c, layer=2))

    def freeze_for(evs):
        best = 0
        for e in evs:
            if e['k'] == 'kill':
                best = max(best, 8)          # 260 ms
            elif e['k'] == 'hit' and e['d'] != 'blocked':
                best = max(best, (0, 3, 5, 8)[min(3, e['tier'])])   # 0, 100, 170 ms
        return best

    while sim_t <= end_t:
        cur = int(sim_t)
        while done_tick < cur:
            done_tick += 1
            evs = by_tick.get(done_tick, [])
            for e in evs:
                spawn(e)
            fr = freeze_for(evs)
            if fr and f - last_freeze_end > 6:
                freeze = fr
                jitter_units = {e['b'] for e in evs if e['k'] in ('hit', 'kill')}
                last_freeze_end = f + fr
        timeline.append((f, sim_t))

        # screen shake: trauma squared, whole pixels, never per frame noise
        shake = trauma * trauma * 7
        ox = int(round(math.sin(f * 1.7) * shake)); oy = int(round(math.cos(f * 2.3) * shake * 0.6))
        trauma = max(0.0, trauma - 1.4 / FPS)

        canvas = Image.new('RGBA', (W, H), (0, 0, 0, 255))
        canvas.alpha_composite(stage, (ox, oy))

        t0 = int(sim_t); t1 = min(last, t0 + 1); fr_a = sim_t - t0
        draw_list = []
        for i, u in enumerate(units):
            s0, s1 = state(t0, i), state(t1, i)
            x = s0['x'] + (s1['x'] - s0['x']) * fr_a
            y = s0['y'] + (s1['y'] - s0['y']) * fr_a
            draw_list.append((s0['act'] == 'Dead', y, i, x, s0))
        draw_list.sort(key=lambda d: (not d[0], d[1]))

        for fx in fxs:
            if fx.layer == 0 and fx.alive(f):
                fx.draw(canvas, f, ox, oy)

        for dead, y, i, x, s in draw_list:
            art = arts[i]
            act, age = s['act'], s['age']
            ms_age = (age + (sim_t - t0)) * TICK_MS
            hop = 0
            if act in ('Attack', 'Boost'):
                clip, idx = art.attack, min(len(art.attack.frames) - 1, s['cf'])
            elif act == 'Move':
                clip = art.run; idx = clip.index(ms_age, loop=True)
            elif act == 'Hurt':
                clip = art.hit; idx = clip.index(ms_age)
            elif act == 'Stagger':
                clip = art.hit; idx = clip.index(ms_age * 0.75)
            elif act == 'Dodge':
                clip = art.idle; idx = 0
                hop = math.sin(math.pi * min(1.0, ms_age / 300.0)) * 16
            elif act == 'Dead':
                clip = art.death; idx = clip.index(ms_age)
            else:
                clip = art.idle; idx = clip.index(sim_t * TICK_MS + i * 137, loop=True)
            spr = clip.frames[idx]
            # presentation knockback and jitter
            dx = 0
            if i in knock:
                k0, px, d = knock[i]
                k = (f - k0) / 5.0
                if k < 1:
                    dx = d * px * (1 - k)
            if freeze and i in jitter_units:
                dx += (1 if (f // 2) % 2 == 0 else -1)
            # the dodge trail: two fading copies of where he just was
            if act == 'Dodge':
                trail[i].append((x, y))
                for n, (tx, ty) in enumerate(trail[i][-6:-1:2]):
                    ghost = spr.copy(); ghost.putalpha(ghost.split()[3].point(lambda v, n=n: v * (0.18 + 0.12 * n)))
                    canvas.alpha_composite(ghost, (int(tx - clip.ax + ox), int(ty - clip.ay + oy - hop)))
            else:
                trail[i] = []
            # the hit flash: one white frame, then red flickers for a big blow
            if i in flash:
                fl0, tier = flash[i]
                age_f = f - fl0
                if age_f == 0:
                    spr = clip.tinted(idx, (255, 255, 255))
                elif tier >= 1 and age_f in (3, 4):
                    red = clip.tinted(idx, (214, 40, 46)); spr = Image.blend(spr, red, 0.4)
            canvas.alpha_composite(spr, (int(round(x - clip.ax + dx + ox)), int(round(y - clip.ay - hop + oy))))

        for fx in fxs:
            if fx.layer >= 1 and fx.alive(f):
                fx.draw(canvas, f, ox, oy)
        fxs[:] = [fx for fx in fxs if fx.alive(f)]
        canvas.alpha_composite(front, (ox, oy))

        d = ImageDraw.Draw(canvas)
        # bars, mark pips and running numbers
        for dead, y, i, x, s in draw_list:
            u = units[i]
            if s['act'] == 'Dead':
                continue
            hp, mx = s['hp'], u['maxHp']
            bar_lag[i] = max(hp, bar_lag[i] - mx * 0.012)
            bw = 46; bx = int(x - bw / 2 + ox); by = int(y + 10 + oy)
            d.rectangle((bx - 1, by - 1, bx + bw, by + 4), fill=(18, 12, 14))
            lag_w = int(bw * bar_lag[i] / mx)
            if lag_w >= 1:
                d.rectangle((bx, by, bx + lag_w - 1, by + 3), fill=(236, 228, 212))
            col = (92, 196, 178) if u['side'] == 0 else (204, 70, 54)
            hp_w = int(bw * hp / mx)
            if hp_w >= 1:
                d.rectangle((bx, by, bx + hp_w - 1, by + 3), fill=col)
            px_ = bx + bw + 3
            for bit, c in ((1, MARK_RGB['opened']), (2, MARK_RGB['hexed']), (4, MARK_RGB['pinned']), (8, MARK_RGB['ripped'])):
                if s['marks'] & bit:
                    d.rectangle((px_, by - 1, px_ + 4, by + 4), fill=(18, 12, 14))
                    d.rectangle((px_ + 1, by, px_ + 3, by + 3), fill=c)
                    px_ += 6
        for i, n in list(numbers.items()):
            age_f = f - n[0]
            if age_f > 30:
                del numbers[i]
                continue
            s = state(int(sim_t), i)
            scale = 3 if n[2] >= 2 else 2
            img = text_img(str(n[1]), n[3], FONT_TINY, scale)
            rise = min(1.0, age_f / 10) * 16
            pop = 1 if age_f < 2 else 0
            canvas.alpha_composite(img, (int(s['x'] - img.width / 2 + ox), int(s['y'] - 150 - rise - pop * 3 + oy)))
        if word and f - word[0] < 28:
            img, wx, wy = word[1], word[2], word[3]
            age_f = f - word[0]
            canvas.alpha_composite(img, (int(wx - img.width / 2 + ox), int(wy - min(10, age_f) + oy)))
        elif word:
            word = None

        proc.stdin.write(canvas.convert('RGB').tobytes())
        f += 1
        if freeze > 0:
            freeze -= 1
        else:
            sim_t += step
        if done_tick >= tr['end'] + 40 and tr['end'] >= 0:
            break

    proc.stdin.close(); proc.wait()
    json.dump(timeline, open(out_path + '.timeline.json', 'w'))
    print('wrote', out_path, f, 'frames', f / FPS, 's')

if __name__ == '__main__':
    main()
