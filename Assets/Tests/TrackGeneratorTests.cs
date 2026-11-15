// Plain geometry and determinism: a row with every lane walled is unwinnable,
// and the same seed must give the same track on every device.

using System.Collections.Generic;
using NUnit.Framework;
using PeachMe.SimCore;

namespace PeachMe.Tests
{
    public class TrackGeneratorTests
    {
        [Test]
        public void SameSeedSameTrack()
        {
            var cfg = new SimConfig();
            var a = TrackGenerator.Generate(20, 1234u, cfg);
            var b = TrackGenerator.Generate(20, 1234u, cfg);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Kind, b[i].Kind);
                Assert.AreEqual(a[i].Lane, b[i].Lane);
                Assert.AreEqual(a[i].Z, b[i].Z);
            }
        }

        [Test]
        public void DifferentStagesDiffer()
        {
            var cfg = new SimConfig();
            var a = TrackGenerator.Generate(3, 1234u, cfg);
            var b = TrackGenerator.Generate(4, 1234u, cfg);
            bool differ = a.Count != b.Count;
            for (int i = 0; !differ && i < a.Count; i++) differ = a[i].Z != b[i].Z || a[i].Lane != b[i].Lane;
            Assert.IsTrue(differ);
        }

        [Test]
        public void NoRowIsFullyBlockedOnAnyStage()
        {
            var cfg = new SimConfig();
            for (uint seed = 1; seed <= 5; seed++)
            {
                for (int s = 0; s < StageTable.Count; s++)
                {
                    var walls = new Dictionary<float, HashSet<int>>();
                    foreach (var it in TrackGenerator.Generate(s, seed, cfg))
                    {
                        if (!it.IsObstacle && !it.IsHazard) continue;
                        if (!walls.TryGetValue(it.Z, out var lanes)) walls[it.Z] = lanes = new HashSet<int>();
                        lanes.Add(it.Lane);
                    }
                    foreach (var kv in walls)
                    {
                        Assert.Less(kv.Value.Count, cfg.LaneCount,
                            $"seed {seed} stage {s} row at {kv.Key:0.0} m has no clean lane");
                    }
                }
            }
        }

        [Test]
        public void ItemsStayInsideTheStageAndLanes()
        {
            var cfg = new SimConfig();
            for (int s = 0; s < StageTable.Count; s++)
            {
                float prev = float.MinValue;
                foreach (var it in TrackGenerator.Generate(s, 99u, cfg))
                {
                    Assert.GreaterOrEqual(it.Lane, 0);
                    Assert.Less(it.Lane, cfg.LaneCount);
                    Assert.Greater(it.Z, 0f);
                    Assert.Less(it.Z, cfg.StageLength);
                    Assert.GreaterOrEqual(it.Z, prev, "items must be sorted by Z");
                    prev = it.Z;
                }
            }
        }

        [Test]
        public void LaterStatesAreBusier()
        {
            var cfg = new SimConfig();
            int early = 0, late = 0;
            for (uint seed = 1; seed <= 10; seed++)
            {
                foreach (var it in TrackGenerator.Generate(0, seed, cfg)) if (it.IsObstacle) early++;
                foreach (var it in TrackGenerator.Generate(50, seed, cfg)) if (it.IsObstacle) late++;
            }
            Assert.Greater(late, early);
        }
    }
}
