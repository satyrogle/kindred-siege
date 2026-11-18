"""One time roll: which classes each tribe fields (Jacob, 15 Nov 2026: "need to write a randomizer for it", then "Roll once").

Each tribe fields PER_TRIBE classes, picked from the classes unlocked up to its tier (the five starters plus the two of every
tier up to its own). A tribe never fields a class its lore bans, always fields its musts, and always fields at least one of
its own tier's two, so beating it unlocks something you have fought (DESIGN_LOCKS line 1539).

Every tribe rolls from its own hash, so changing one tribe's bans or musts never reshuffles another tribe. The same seed
always gives the same table, on any machine and any Python version (sha256, no random module).

Usage: python tools/class_roll/class_roll.py [--seed TEXT] [--per-tribe N]
"""
import argparse
import hashlib

STARTERS = ["Warden", "Marksman", "Skirmisher", "Trapper", "Hexer"]

# DESIGN_LOCKS line 1541: the two new classes each tier adds.
TIER_CLASSES = {
    1: ["Vanguard", "Berserker"],
    2: ["Artillerist", "Field Engineer"],
    3: ["Duelist", "Infiltrator"],
    4: ["Binder", "Recon"],
    5: ["Chemist", "Disruptor"],
    6: ["Conduit", "Herald"],
}

# The 23 enemy tribes, about four per tier (DESIGN_LOCKS line 197). Only four are named so far; the rest are slots until
# Jacob names them and writes their lore. "ban": classes the lore rules out. "must": classes the tribe always fields.
TRIBES = [
    {"tier": 1, "name": "Bloodridge", "must": ["Vanguard"], "ban": []},
    {"tier": 1, "name": "Stoneveil", "must": ["Berserker"], "ban": ["Artillerist"]},
    {"tier": 1, "name": "Tier 1 tribe C", "must": [], "ban": []},
    {"tier": 1, "name": "Tier 1 tribe D", "must": [], "ban": []},
    {"tier": 2, "name": "Skullpeak", "must": ["Artillerist"], "ban": []},
    {"tier": 2, "name": "Thundercoast", "must": ["Field Engineer"], "ban": []},
    {"tier": 2, "name": "Tier 2 tribe C", "must": [], "ban": []},
    {"tier": 2, "name": "Tier 2 tribe D", "must": [], "ban": []},
    {"tier": 3, "name": "Tier 3 tribe A", "must": [], "ban": []},
    {"tier": 3, "name": "Tier 3 tribe B", "must": [], "ban": []},
    {"tier": 3, "name": "Tier 3 tribe C", "must": [], "ban": []},
    {"tier": 3, "name": "Tier 3 tribe D", "must": [], "ban": []},
    {"tier": 4, "name": "Tier 4 tribe A", "must": [], "ban": []},
    {"tier": 4, "name": "Tier 4 tribe B", "must": [], "ban": []},
    {"tier": 4, "name": "Tier 4 tribe C", "must": [], "ban": []},
    {"tier": 4, "name": "Tier 4 tribe D", "must": [], "ban": []},
    {"tier": 5, "name": "Tier 5 tribe A", "must": [], "ban": []},
    {"tier": 5, "name": "Tier 5 tribe B", "must": [], "ban": []},
    {"tier": 5, "name": "Tier 5 tribe C", "must": [], "ban": []},
    {"tier": 5, "name": "Tier 5 tribe D", "must": [], "ban": []},
    {"tier": 6, "name": "Tier 6 tribe A", "must": [], "ban": []},
    {"tier": 6, "name": "Tier 6 tribe B", "must": [], "ban": []},
    {"tier": 6, "name": "Tier 6 tribe C", "must": [], "ban": []},
]

ALL_CLASSES = STARTERS + [c for t in sorted(TIER_CLASSES) for c in TIER_CLASSES[t]]


def rank(seed, tribe, cls):
    return hashlib.sha256(f"{seed}|{tribe}|{cls}".encode("utf-8")).hexdigest()


def roll_tribe(seed, tribe, per_tribe):
    tier, name = tribe["tier"], tribe["name"]
    unlocked = STARTERS + [c for t in range(1, tier + 1) for c in TIER_CLASSES[t]]
    for c in tribe["must"] + tribe["ban"]:
        if c not in ALL_CLASSES:
            raise SystemExit(f"{name}: unknown class {c!r}")
    pool = [c for c in unlocked if c not in tribe["ban"]]
    order = sorted(pool, key=lambda c: rank(seed, name, c))
    notes = []

    picks = []
    for c in tribe["must"]:
        if c in tribe["ban"]:
            raise SystemExit(f"{name}: {c} is both a must and a ban")
        if c not in unlocked:
            raise SystemExit(f"{name}: must {c} is not unlocked by tier {tier}")
        if c not in picks:
            picks.append(c)

    own = [c for c in TIER_CLASSES[tier] if c in pool]
    if not own:
        notes.append(f"both tier {tier} classes are banned, so beating it unlocks classes it never fields")
    elif not any(c in picks for c in own):
        picks.append(min(own, key=lambda c: rank(seed, name, c)))

    for c in order:
        if len(picks) >= per_tribe:
            break
        if c not in picks:
            picks.append(c)

    if len(picks) > per_tribe:
        notes.append(f"its musts make {len(picks)} classes, more than {per_tribe}")
    if len(picks) < per_tribe:
        notes.append(f"its bans leave only {len(picks)} classes")
    return sorted(picks, key=ALL_CLASSES.index), notes


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--seed", default="kindred-siege-class-roll-1")
    ap.add_argument("--per-tribe", type=int, default=5)
    args = ap.parse_args()

    print(f"Seed: {args.seed}   Classes per tribe: {args.per_tribe}\n")
    print("| Tier | Tribe | Classes it fields |")
    print("|---|---|---|")
    total, fielded = 0, {c: 0 for c in ALL_CLASSES}
    all_notes = []
    for tribe in TRIBES:
        picks, notes = roll_tribe(args.seed, tribe, args.per_tribe)
        total += len(picks)
        for c in picks:
            fielded[c] += 1
        print(f"| {tribe['tier']} | {tribe['name']} | {', '.join(picks)} |")
        all_notes += [f"{tribe['name']}: {n}" for n in notes]

    print(f"\nEnemy tribe and class pairs, one rank 3 morph each: {total}")
    print("\nHow many tribes field each class:")
    for c in ALL_CLASSES:
        print(f"  {c}: {fielded[c]}")
    if all_notes:
        print("\nWarnings:")
        for n in all_notes:
            print(f"  {n}")


if __name__ == "__main__":
    main()
