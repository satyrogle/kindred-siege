// Layer 7: the nerve ladder (Battle Brothers' five visible states; Halo 3 "leader death breaks followers"; Darkest
// Dungeon's break that branches by character). One step per named event, checked against Will. A rally lifts a broken
// man to Wavering, never Steady. Broken branches by personality: flee, freeze, rage, or stand by our weakest.
// Layer 5 lives here too: events open short stimulus windows in which special reactions become legal (Halo 2).
using System;

namespace KindredSiege.Sim.Brawl2
{
    public sealed class Stimulus { public string Kind = ""; public int Source = -1, Other = -1, Until; }

    public sealed partial class Fight
    {
        static int TraitResolve(Trait t) => t switch
        {
            Trait.Steady => 15, Trait.Veteran => 10, Trait.Loyal => 10, Trait.Vengeful => 5, Trait.Reckless => 5,
            Trait.Recruit => -10, Trait.Nervous => -15, Trait.Coward => -20, _ => 0
        };

        static BreakKind BreakOf(Trait t) => t switch
        {
            Trait.Coward => BreakKind.Flee, Trait.Nervous => BreakKind.Freeze, Trait.Recruit => BreakKind.Freeze,
            Trait.Vengeful => BreakKind.Rage, Trait.Reckless => BreakKind.Rage, Trait.Loyal => BreakKind.Shield,
            _ => BreakKind.Flee
        };

        /// One check against Will; a fail steps him down the ladder.
        public void NerveCheck(Unit u, string why, int steps)
        {
            if (!u.Alive || u.Nerve == Nerve.Broken) return;
            int resolve = u.Six.Will + TraitResolve(u.Trait) - (int)u.Nerve * 8;
            int roll = u.Rng.Next(100);
            if (roll < resolve) return;
            var before = u.Nerve;
            u.Nerve = (Nerve)Math.Min((int)Nerve.Broken, (int)u.Nerve + steps);
            if (u.Nerve == before) return;
            Emit("nerve", u.Id, -1, (int)u.Nerve, u.Nerve.ToString() + ":" + why, u.Px, u.Py - 110, u.Nerve >= Nerve.Breaking ? 1 : 0);
            if (u.Nerve == Nerve.Broken) Break(u);
        }

        public void Lift(Unit u, string why)
        {
            if (!u.Alive) return;
            if (u.Nerve == Nerve.Broken) { Rally(u, why); return; }
            if (u.Nerve == Nerve.Confident) return;
            u.Nerve = (Nerve)((int)u.Nerve - 1);
            Emit("nerve", u.Id, -1, (int)u.Nerve, u.Nerve.ToString() + ":" + why, u.Px, u.Py - 110, 0);
        }

        void Break(Unit u)
        {
            u.Breaking = BreakOf(u.Trait);
            u.BreakSince = _tick;
            Referee.Leave(u); Referee.Release(this, u);
            Emit("break", u.Id, -1, 0, u.Breaking.ToString().ToLowerInvariant(), u.Px, u.Py - 110, 2);
            if (u.Breaking == BreakKind.Freeze) { u.CowerUntil = _tick + R.T(3000); if (Free(u)) { u.Act = Act.Cower; u.Age = 0; u.Moving = false; } }
            if (u.Breaking == BreakKind.Shield)
            {
                Unit weak = null; int pct = 1001;
                foreach (var a in _u) if (a.Alive && a.Side == u.Side && a != u && a.Hp * 1000 / a.MaxHp < pct) { pct = a.Hp * 1000 / a.MaxHp; weak = a; }
                u.ShieldFor = weak != null ? weak.Id : -1;
                if (weak == null) u.Breaking = BreakKind.Rage;
            }
        }

        void Rally(Unit u, string why)
        {
            u.Breaking = BreakKind.None; u.Nerve = Nerve.Wavering; u.ShieldFor = -1;
            if (u.Act == Act.Flee || u.Act == Act.Cower) { u.Act = Act.Idle; u.Age = 0; u.Moving = false; }
            Emit("rally", u.Id, -1, 0, why, u.Px, u.Py - 110, 1);
        }

        void NerveTimers()
        {
            foreach (var u in _u)
            {
                if (!u.Alive) continue;
                var near = Nearest(u, enemy: true, maxPx: 260);
                if (near != null) u.LastThreatAt = _tick;
                switch (u.Breaking)
                {
                    case BreakKind.Flee:
                        if (_tick - u.LastThreatAt > R.T(3500) && _tick - u.BreakSince > R.T(2000)) Rally(u, "caught his breath");
                        break;
                    case BreakKind.Freeze:
                        if (_tick >= u.CowerUntil) Rally(u, "shook it off");
                        break;
                    case BreakKind.Shield:
                        var a = Get(u.ShieldFor);
                        if (a == null || !a.Alive || _tick - u.BreakSince > R.T(7000)) Rally(u, "stood his ground");
                        break;
                }
                // outnumbered: two or more of them closing on him
                if (_tick >= u.OutnumberedCheckAt)
                {
                    u.OutnumberedCheckAt = _tick + R.T(2000);
                    int on = 0;
                    foreach (var e in _u) if (e.Alive && e.Side != u.Side && e.Target == u.Id && Dist(e, u) < 220) on++;
                    if (on >= 2) NerveCheck(u, "outnumbered", 1);
                }
            }
        }

        // ------------------------------------------------------------------ the named events (layers 5 and 7)

        void OnDeath(Unit victim, Unit killer)
        {
            foreach (var a in _u)
            {
                if (!a.Alive || a == victim) continue;
                int d = Dist(a, victim);
                if (a.Side == victim.Side)
                {
                    if (d < 350) { Stim(a, "allyfell", killer != null ? killer.Id : -1, victim.Id, R.T(1500)); if (killer != null) a.Mem.Killer = killer.Id; }
                    if (d < 320) NerveCheck(a, "friend fell", victim.MaxHp > a.MaxHp ? 2 : 1);
                }
                else if (d < 300 && a.Nerve >= Nerve.Breaking) Lift(a, "their man fell");
            }
            if (killer != null && killer.Alive)
            {
                Lift(killer, "a kill");
                if (killer.Breaking == BreakKind.Rage) Rally(killer, "blood drawn");
            }
        }

        void OnWound(Unit t, Unit a, int dmg, bool cashed)
        {
            if (a != null) t.Mem.DamageFrom[a.Id] += dmg;
            if (dmg * 100 >= t.MaxHp * 15) NerveCheck(t, "badly hurt", 1);
            if (cashed)
            {
                NerveCheck(t, "marked", 1);
                foreach (var b in _u) if (b.Alive && a != null && b.Side == a.Side && b != a && Dist(b, t) < 260) Stim(b, "cashed", a.Id, t.Id, R.T(900));
                if (a != null) foreach (var b in _u) if (b.Alive && b.Side == a.Side && b != a && Dist(b, a) < 250 && b.Nerve >= Nerve.Wavering) Lift(b, "a cash");
            }
        }

        public void Stim(Unit u, string kind, int source, int other, int ticks)
            => u.Stimuli.Add(new Stimulus { Kind = kind, Source = source, Other = other, Until = _tick + ticks });

        void ExpireStimuli() { foreach (var u in _u) u.Stimuli.RemoveAll(s => s.Until <= _tick); }
    }
}
