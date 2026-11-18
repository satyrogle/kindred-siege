// Runs one Clarth against Bloodridge road fight in the unit combat study and writes the trace the renderer reads.
// usage: Brawl.exe <seed> <out.json>
using System;
using System.Collections.Generic;
using System.IO;
using KindredSiege.Sim.Brawl;

public static class Program
{
    static string TheirTimings = "blood_rot";

    static Fight Build(int seed)
    {
        var f = new Fight();
        ulong s = (ulong)seed;
        string br = TheirTimings;
        // ours, Clarth, on the left facing right (the game's ClarthStudy SE sheets)
        f.Add(0, Cls.Warden, "clarth", "Clarth Warden", 560, 610, s);
        f.Add(0, Cls.Skirmisher, "clarth", "Clarth Skirmisher", 520, 480, s);
        f.Add(0, Cls.Trapper, "clarth", "Clarth Trapper", 520, 735, s);
        f.Add(0, Cls.Marksman, "clarth", "Clarth Marksman", 320, 535, s);
        f.Add(0, Cls.Hexer, "clarth", "Clarth Hexer", 320, 690, s);
        // theirs, Bloodridge's band, on the right facing left (the SW sheets)
        f.Add(1, Cls.Warden, br, "Bloodridge Warden", 880, 610, s);
        f.Add(1, Cls.Skirmisher, br, "Bloodridge Skirmisher", 920, 480, s);
        f.Add(1, Cls.Trapper, br, "Bloodridge Trapper", 920, 735, s);
        f.Add(1, Cls.Marksman, br, "Bloodridge Marksman", 1120, 535, s);
        f.Add(1, Cls.Hexer, br, "Bloodridge Hexer", 1120, 690, s);
        return f;
    }

    public static int Main(string[] args)
    {
        int seed = args.Length > 0 ? int.Parse(args[0]) : 1;
        string outPath = args.Length > 1 ? args[1] : "trace.json";
        if (args.Length > 2 && args[2] == "same-timings") TheirTimings = "clarth";   // a check, not for film
        var f = Build(seed);
        int limit = 20 * 150;
        while (f.Tick < limit && !(f.Over && f.Tick > f.EndTick + 60)) f.Step();
        ulong sum = f.Checksum();

        var g = Build(seed);
        while (g.Tick < f.Tick) g.Step();
        bool same = g.Checksum() == sum;

        File.WriteAllText(outPath, f.TraceJson(seed));
        var count = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var dealt = new int[f.Units.Count];
        int cashes = 0, misses = 0, hits = 0;
        foreach (var e in f.Events)
        {
            string key = e.K == "hit" || e.K == "mark" || e.K == "defence" || e.K == "boost" || e.K == "miss" ? e.K + ":" + e.Detail : e.K;
            count.TryGetValue(key, out int c); count[key] = c + 1;
            if (e.K == "hit" && e.A >= 0) { dealt[e.A] += e.Amount; hits++; if (e.Tier == 2) cashes++; }
            if (e.K == "miss") misses++;
        }
        Console.WriteLine($"seed {seed}: ticks {f.Tick}, end {f.EndTick} ({f.EndTick * 50 / 1000.0:0.0} s), winner {(f.Winner == 0 ? "Clarth" : f.Winner == 1 ? "Bloodridge" : "none")}, deterministic {same}");
        Console.WriteLine($"hits {hits}, misses {misses}, cashes {cashes}");
        foreach (var kv in count) Console.WriteLine($"  {kv.Key,-24} {kv.Value}");
        foreach (var u in f.Units) Console.WriteLine($"  {u.Name,-22} hp {u.Hp,4}/{u.MaxHp,-4} dealt {dealt[u.Id],5}  {u.Act}");
        return same ? 0 : 2;
    }
}
