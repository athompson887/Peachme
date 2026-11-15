// PeachMe SimCore - pure C#, no UnityEngine references allowed in this folder.
// Distribution mirrors gen_stage() in tools/tune_peach.py.

using System;
using System.Collections.Generic;

namespace PeachMe.SimCore
{
    public enum ItemKind
    {
        // Obstacles (cost speed -> peach gains)
        TaxForms,       // low: jump over
        RedTape,        // high: slide under
        Briefcase,      // block: change lane
        // Pickups
        Star,
        HotDog,         // speed boost
        // Hazards (cost points + gap)
        FakeNews,
        FlipFlop
    }

    public sealed class TrackItem
    {
        public ItemKind Kind;
        public int Lane;
        public float Z;          // metres from stage start
        public bool Consumed;

        public bool IsObstacle => Kind == ItemKind.TaxForms || Kind == ItemKind.RedTape || Kind == ItemKind.Briefcase;
        public bool IsPickup => Kind == ItemKind.Star || Kind == ItemKind.HotDog;
        public bool IsHazard => Kind == ItemKind.FakeNews || Kind == ItemKind.FlipFlop;
    }

    public static class TrackGenerator
    {
        public static float DifficultyOf(int stageIndex, SimConfig cfg)
        {
            float t = StageTable.Count <= 1 ? 0f : stageIndex / (float)(StageTable.Count - 1);
            return (float)Math.Pow(t, cfg.DifficultyExponent);
        }

        public static List<TrackItem> Generate(int stageIndex, uint runSeed, SimConfig cfg)
        {
            var rng = new SimRandom(SimRandom.Mix(runSeed, (uint)(stageIndex + 1)));
            float d = DifficultyOf(stageIndex, cfg);
            var items = new List<TrackItem>(128);
            var freeLanes = new List<int>(cfg.LaneCount);
            var blocked = new bool[cfg.LaneCount];

            float spacing = cfg.RowSpacingEasy + (cfg.RowSpacingHard - cfg.RowSpacingEasy) * d;
            float end = cfg.StageLength - cfg.EndClear;
            float z = cfg.StartClear;

            while (true)
            {
                z += spacing * rng.Range(0.85f, 1.15f);
                if (z > end) break;

                Array.Clear(blocked, 0, blocked.Length);
                int obstacleCount = rng.Chance(d * cfg.DoubleObstacleChance) ? 2 : 1;
                obstacleCount = Math.Min(obstacleCount, cfg.LaneCount - 1); // always leave a lane

                for (int i = 0; i < obstacleCount; i++)
                {
                    int lane;
                    do { lane = rng.Range(0, cfg.LaneCount); } while (blocked[lane]);
                    blocked[lane] = true;
                    items.Add(new TrackItem { Kind = PickObstacle(rng, d), Lane = lane, Z = z });
                }

                freeLanes.Clear();
                for (int l = 0; l < cfg.LaneCount; l++)
                {
                    if (!blocked[l]) freeLanes.Add(l);
                }

                // Hazard only when it still leaves a clean lane.
                bool hazard = obstacleCount == 1 && rng.Chance(cfg.HazardChanceBase + cfg.HazardChanceDiff * d);
                int hazardLane = -1;
                if (hazard && freeLanes.Count >= 2)
                {
                    hazardLane = freeLanes[rng.Range(0, freeLanes.Count)];
                    var kind = rng.Chance(0.5f) ? ItemKind.FakeNews : ItemKind.FlipFlop;
                    items.Add(new TrackItem { Kind = kind, Lane = hazardLane, Z = z });
                }

                if (rng.Chance(cfg.HotDogChance))
                {
                    int lane = freeLanes[rng.Range(0, freeLanes.Count)];
                    if (lane != hazardLane)
                    {
                        items.Add(new TrackItem { Kind = ItemKind.HotDog, Lane = lane, Z = z + 4f });
                    }
                }

                if (rng.Chance(cfg.StarLineChance))
                {
                    int lane = freeLanes[rng.Range(0, freeLanes.Count)];
                    if (lane != hazardLane)
                    {
                        for (int s = 0; s < cfg.StarsPerLine; s++)
                        {
                            float sz = z - 2f - s * cfg.StarSpacing;
                            items.Add(new TrackItem { Kind = ItemKind.Star, Lane = lane, Z = sz });
                        }
                    }
                }
            }

            // Stable sort by Z (List.Sort is unstable; tie-break on insertion order).
            var indexed = new List<(TrackItem item, int order)>(items.Count);
            for (int i = 0; i < items.Count; i++) indexed.Add((items[i], i));
            indexed.Sort((a, b) =>
            {
                int c = a.item.Z.CompareTo(b.item.Z);
                return c != 0 ? c : a.order.CompareTo(b.order);
            });
            for (int i = 0; i < items.Count; i++) items[i] = indexed[i].item;
            return items;
        }

        private static ItemKind PickObstacle(SimRandom rng, float d)
        {
            float wBlock = 0.5f - 0.2f * d;
            float wLow = 0.3f + 0.1f * d;
            float r = rng.NextFloat();
            if (r < wBlock) return ItemKind.Briefcase;
            if (r < wBlock + wLow) return ItemKind.TaxForms;
            return ItemKind.RedTape;
        }
    }
}
