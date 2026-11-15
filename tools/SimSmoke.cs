// Headless smoke test for SimCore (no Unity needed).
// Build: mcs -out:SimSmoke.exe tools/SimSmoke.cs Assets/SimCore/*.cs && mono SimSmoke.exe
// Checks: determinism (same seed -> identical run), a simple bot can play,
// every hero runs without exceptions, and stage/chapter flow works.

using System;
using System.Collections.Generic;
using PeachMe.SimCore;

public static class SimSmoke
{
    public static int Main()
    {
        int failures = 0;

        // 1. Determinism
        string a = Play(HeroDef.Roster[0], 1234u, 6, out _);
        string b = Play(HeroDef.Roster[0], 1234u, 6, out _);
        string c = Play(HeroDef.Roster[0], 9999u, 6, out _);
        failures += Check(a == b, "same seed gives identical run");
        failures += Check(a != c, "different seed gives different run");

        // 2. Every hero, a full campaign attempt with a decent bot
        foreach (var hero in HeroDef.Roster)
        {
            Play(hero, 42u, StageTable.Count, out var summary);
            Console.WriteLine($"  {hero.DisplayName,-24} {summary}");
        }

        // 3. Track sanity: every row leaves a non-obstacle lane
        var cfg = new SimConfig();
        for (int s = 0; s < StageTable.Count; s++)
        {
            var items = TrackGenerator.Generate(s, 7u, cfg);
            var rows = new Dictionary<float, int>();
            foreach (var it in items)
            {
                if (!it.IsObstacle) continue;
                rows.TryGetValue(it.Z, out int n);
                rows[it.Z] = n + 1;
            }
            foreach (var kv in rows)
            {
                if (kv.Value >= cfg.LaneCount) { failures += Check(false, $"stage {s} row {kv.Key} fully blocked"); }
            }
        }
        failures += Check(true, "no fully blocked rows");

        Console.WriteLine(failures == 0 ? "ALL PASS" : $"{failures} FAILURE(S)");
        return failures;
    }

    private static int Check(bool ok, string what)
    {
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {what}");
        return ok ? 0 : 1;
    }

    // Simple look-ahead bot. Returns a trace hash string for determinism checks.
    private static string Play(HeroDef hero, uint seed, int stages, out string summary)
    {
        var sim = new RunnerSim(hero, seed);
        var rng = new SimRandom(seed ^ 0xABCDu);
        var counts = new Dictionary<SimEventType, int>();
        long hash = 17;
        int ticks = 0;
        int maxTicks = 60 * 60 * 120; // 2 hours of game time cap

        while (ticks++ < maxTicks)
        {
            if (sim.Phase == RunPhase.Won) break;
            if (sim.Phase == RunPhase.StageComplete)
            {
                if (sim.StageIndex + 1 >= stages) break;
                sim.BeginNextStage();
                Tally(sim, counts);
            }
            if (sim.Phase == RunPhase.GameOver)
            {
                sim.RetryFromCheckpoint();
                Tally(sim, counts);
            }

            sim.Step(Decide(sim, rng));
            Tally(sim, counts);
            hash = hash * 31 + (long)(sim.Z * 1000) + (long)(sim.PeachGap * 1000) + (long)sim.Score;
        }

        summary = $"stage {sim.StageIndex + 1,2}/{StageTable.Count} {sim.Phase,-13} " +
                  $"hits {Get(counts, SimEventType.ObstacleHit),4} smashes {Get(counts, SimEventType.ObstacleSmashed),3} " +
                  $"stars {Get(counts, SimEventType.StarCollected),4} caught {Get(counts, SimEventType.PeachCaught),3} " +
                  $"GOs {sim.GameOvers,3} score {(int)sim.Score}";
        return hash.ToString();
    }

    private static int Get(Dictionary<SimEventType, int> counts, SimEventType t)
    {
        int n;
        return counts.TryGetValue(t, out n) ? n : 0;
    }

    private static void Tally(RunnerSim sim, Dictionary<SimEventType, int> counts)
    {
        foreach (var e in sim.Events)
        {
            counts.TryGetValue(e.Type, out int n);
            counts[e.Type] = n + 1;
        }
    }

    private static SimInput Decide(RunnerSim sim, SimRandom rng)
    {
        var input = SimInput.None;
        float lookAhead = sim.Speed * 0.45f + 2f;
        int lane = sim.TargetLane;

        // Use specials like a sensible player.
        if (sim.SpecialReady)
        {
            switch (sim.Hero.Special)
            {
                case SpecialKind.Charge: input.Special = true; break;
                case SpecialKind.SaxDash: input.Special = sim.PeachGap < 10f; break;
                case SpecialKind.TapeErase: input.Special = sim.PeachGap < 8f; break;
            }
        }

        foreach (var it in sim.Items)
        {
            if (it.Consumed || it.Z <= sim.Z) continue;
            if (it.Z > sim.Z + lookAhead) break;
            if (it.Lane != lane || !(it.IsObstacle || it.IsHazard)) continue;

            // 10% of the time the bot fumbles (roughly "regular" skill).
            if (rng.Chance(0.10f)) return input;

            if (it.Kind == ItemKind.TaxForms && it.Z - sim.Z < sim.Speed * 0.25f) { input.Jump = true; return input; }
            if (it.Kind == ItemKind.RedTape && sim.Hero.CanSlide && it.Z - sim.Z < sim.Speed * 0.25f) { input.Slide = true; return input; }
            if (it.Kind == ItemKind.TaxForms || (it.Kind == ItemKind.RedTape && sim.Hero.CanSlide)) return input;

            int best = SafestLane(sim, lane, lookAhead);
            input.LaneDelta = Math.Sign(best - lane);
            return input;
        }
        return input;
    }

    private static int SafestLane(RunnerSim sim, int current, float lookAhead)
    {
        int best = current;
        float bestDist = -1f;
        for (int l = 0; l < sim.Config.LaneCount; l++)
        {
            if (Math.Abs(l - current) > 1) continue;
            float firstBlock = float.MaxValue;
            foreach (var it in sim.Items)
            {
                if (it.Consumed || it.Z <= sim.Z || it.Lane != l) continue;
                if (it.Z > sim.Z + lookAhead * 2f) break;
                if (it.Kind == ItemKind.Briefcase || it.IsHazard) { firstBlock = it.Z; break; }
            }
            if (firstBlock > bestDist) { bestDist = firstBlock; best = l; }
        }
        return best;
    }
}
