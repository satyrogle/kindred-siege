# Layer 1 rules (9 Nov 2026, for Jacob's OK)

- **SIZE:** World at 1x. Pals at 80% of now: figures about 110 to 150 px tall. A prop is never taller than its Pal. Leader as in the depth still (142 px).
- **PALETTE:** One master palette, below: the leader and units' darks, bone and rust, plus one ramp per Pal. Every PixelLab call gets it. Pal 16 colours max, prop 12, ground tile 8.
- **LIGHT:** Sun up right. Lit edges warm on the right and top, shadows fall down left, every object gets a small contact shadow.
- **SHADING:** 3 or 4 hard steps per material, shadows cooler, highlights warmer. No dither on figures or ground. No lone pixels.
- **OUTLINE:** 1 px, the darkest colour of the local ramp, never pure black, lighter on the lit side.
- **GROUND:** PixelLab tiles, not a painting: dirt (3 kinds), packed path, moss edge, stone lip. Paths join every station to the stream and to the leader's ridge.
- **FLORA:** Only these. 1: one dark tree kind, at the two top corners only, near silhouettes. 2: one ground tuft kind, in clusters of 3 to 7, only on path edges and stream banks. 3: the Glutton's rot fungus at his bed. Nothing scattered on open floor. Stones: 6 at most, beside paths.
- **WIND:** A gust left to right every 6 s. It moves tree leaf clumps (whole clumps, 1 px), ground tufts (a 4 frame sway), cloth and membranes on the Pals and stations (whole pieces). Nothing slides rows. No floating specks.
- **GONE:** All animals. The gold spirals. Sparkles and glints.
- **SPACE:** Stations stay where they are now, with open floor between them (lock: 'enough space for all the guys to have all their tools').

## Pals (from DESIGN_LOCKS)

- **BUTCHER:** War and rage. Towering, long limbs, massive hands, cold command. Forges weapons and gear, wears visible gear.
- **GLUTTON:** Rot and endurance. Enormous and swollen, laughing alien face, violet nacre with obsidian and ancient gold, rot on his flank. Always asleep, Jabba posture. Armour and body augments. v3 locked 10 Oct.
- **SHADE:** Change and tricks. Astral scholar priestess: black membranes on grown obsidian, markings under the skin. Always levitates. Unlocks orders.
- **WHISPERER:** Excess and charm. Wild, beautiful, feral astral being and goth woman, with floating energy fragments. Inviting first, dangerous up close. Skins and regalia.

All four: the leader's opalescent skin underneath, each with their own colour and corruption. Leader only: three eyes, see through brain, dreads, crown. Chaos god traits, never Games Workshop looks.

How they get drawn: 3 takes per Pal as PixelLab objects (about 6 credits a take), the leader as the style guide, your gate 2 picks only for pose and props. You pick per Pal. Then 3 takes of each Pal's props.