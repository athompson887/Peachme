// The run's flow rules: catches, game over, checkpoint retry, chapters, specials.
// Configs are bent on purpose (a fast peach, a short stage) so each rule is
// reached in a few hundred ticks rather than by luck.

using NUnit.Framework;
using PeachMe.SimCore;

namespace PeachMe.Tests
{
    public class RunnerSimTests
    {
        private static int RunUntil(RunnerSim sim, System.Func<RunnerSim, bool> done, int maxTicks = 60 * 600)
        {
            for (int t = 0; t < maxTicks; t++)
            {
                if (done(sim)) return t;
                sim.Step(SimInput.None);
            }
            Assert.Fail("condition not reached in " + maxTicks + " ticks");
            return -1;
        }

        private static SimConfig FastPeach()
        {
            return new SimConfig { PeachBaseFactor = 1.5f, PeachWakeDelay = 0f };
        }

        private static SimConfig ShortStages()
        {
            // No obstacles can be reached (StartClear past the end) and the peach never moves.
            return new SimConfig { StageLength = 20f, StartClear = 30f, PeachBaseFactor = 0f };
        }

        [Test]
        public void SameSeedSameRun()
        {
            var a = new RunnerSim(HeroDef.Roster[0], 77u);
            var b = new RunnerSim(HeroDef.Roster[0], 77u);
            for (int t = 0; t < 60 * 30; t++)
            {
                var input = new SimInput { LaneDelta = (t % 90 == 0) ? 1 : (t % 90 == 45 ? -1 : 0), Jump = t % 120 == 60 };
                a.Step(input);
                b.Step(input);
            }
            Assert.AreEqual(a.Z, b.Z);
            Assert.AreEqual(a.PeachGap, b.PeachGap);
            Assert.AreEqual(a.Score, b.Score);
        }

        [Test]
        public void PeachSleepsThroughTheIntro()
        {
            var sim = new RunnerSim(HeroDef.Roster[0], 1u);
            sim.Step(SimInput.None);
            Assert.AreEqual(0f, sim.PeachSpeed);
            Assert.IsFalse(sim.PeachAwake);
            RunUntil(sim, s => s.PeachAwake);
            sim.Step(SimInput.None);
            Assert.Greater(sim.PeachSpeed, 0f);
        }

        [Test]
        public void ACatchCostsALifeAndResetsTheGap()
        {
            var sim = new RunnerSim(HeroDef.Roster[0], 1u, FastPeach());
            RunUntil(sim, s => s.Lives < sim.Config.StartLives);
            Assert.AreEqual(sim.Config.StartLives - 1, sim.Lives);
            Assert.AreEqual(sim.Config.CaughtResetGap, sim.PeachGap, 0.5f);
            Assert.Greater(sim.InvulnTimer, 0f);
            Assert.AreEqual(RunPhase.Running, sim.Phase);
        }

        [Test]
        public void LastLifeGoneIsGameOverAndTheSimFreezes()
        {
            var sim = new RunnerSim(HeroDef.Roster[0], 1u, FastPeach());
            RunUntil(sim, s => s.Phase == RunPhase.GameOver);
            Assert.AreEqual(0, sim.Lives);
            Assert.AreEqual(1, sim.GameOvers);
            float z = sim.Z;
            sim.Step(SimInput.None);
            Assert.AreEqual(z, sim.Z, "a game over must not keep running");
        }

        [Test]
        public void RetryGoesToChapterStartWithFullLivesAndMercy()
        {
            var sim = new RunnerSim(HeroDef.Roster[0], 1u, FastPeach(), startStage: 8);
            RunUntil(sim, s => s.Phase == RunPhase.GameOver);
            sim.RetryFromCheckpoint();
            Assert.AreEqual(RunPhase.Running, sim.Phase);
            Assert.AreEqual(6, sim.StageIndex, "stage 8 is in the chapter that starts at 6");
            Assert.AreEqual(sim.Config.StartLives, sim.Lives);
            Assert.AreEqual(1, sim.MercySteps);
            Assert.AreEqual(0f, sim.Z);
        }

        [Test]
        public void MercyIsCapped()
        {
            var sim = new RunnerSim(HeroDef.Roster[0], 1u, FastPeach());
            for (int i = 0; i < sim.Config.MercyMaxSteps + 3; i++)
            {
                RunUntil(sim, s => s.Phase == RunPhase.GameOver);
                sim.RetryFromCheckpoint();
            }
            Assert.AreEqual(sim.Config.MercyMaxSteps, sim.MercySteps);
        }

        [Test]
        public void ClearingAStageWaitsForTheGame()
        {
            var sim = new RunnerSim(HeroDef.Roster[0], 1u, ShortStages());
            RunUntil(sim, s => s.Phase == RunPhase.StageComplete);
            float z = sim.Z;
            sim.Step(SimInput.None);
            Assert.AreEqual(z, sim.Z, "the sim waits for BeginNextStage");
            sim.BeginNextStage();
            Assert.AreEqual(1, sim.StageIndex);
            Assert.AreEqual(0f, sim.Z);
            Assert.AreEqual(RunPhase.Running, sim.Phase);
        }

        [Test]
        public void BeginNextStageIsIgnoredMidStage()
        {
            var sim = new RunnerSim(HeroDef.Roster[0], 1u);
            sim.Step(SimInput.None);
            sim.BeginNextStage();
            Assert.AreEqual(0, sim.StageIndex);
        }

        [Test]
        public void ANewChapterGivesBackALifeButNeverPastTheCap()
        {
            var cfg = ShortStages();
            var sim = new RunnerSim(HeroDef.Roster[0], 1u, cfg);
            bool sawChapter = false;
            for (int stage = 0; stage < 6; stage++)
            {
                RunUntil(sim, s => s.Phase == RunPhase.StageComplete);
                sim.BeginNextStage();
                foreach (var e in sim.Events) sawChapter |= e.Type == SimEventType.ChapterStarted;
            }
            Assert.AreEqual(6, sim.StageIndex);
            Assert.IsTrue(sawChapter);
            Assert.AreEqual(cfg.MaxLives, sim.Lives);
        }

        [Test]
        public void ClearingDcWinsTheGame()
        {
            var sim = new RunnerSim(HeroDef.Roster[0], 1u, ShortStages(), startStage: 50);
            RunUntil(sim, s => s.Phase == RunPhase.Won);
            Assert.AreEqual(50, sim.StageIndex);
        }

        [Test]
        public void TapeEraseGainsGroundOnce()
        {
            var dick = HeroDef.ById("dick");
            var sim = new RunnerSim(dick, 1u, new SimConfig { PeachStartGap = 5f });
            float before = sim.PeachGap;
            sim.Step(new SimInput { Special = true });
            Assert.AreEqual(before + sim.Config.TapeEraseGap, sim.PeachGap, 0.5f);
            Assert.IsFalse(sim.SpecialReady, "cooling down");
        }

        [Test]
        public void SaxSlowsThePeach()
        {
            var cfg = new SimConfig { PeachWakeDelay = 0f };
            var bill = new RunnerSim(HeroDef.ById("bill"), 1u, cfg);
            var plain = new RunnerSim(HeroDef.ById("georgie"), 1u, cfg);
            bill.Step(new SimInput { Special = true });
            plain.Step(SimInput.None);
            Assert.AreEqual(plain.PeachSpeed * cfg.SaxPeachFactor, bill.PeachSpeed, 0.001f);
        }

        [Test]
        public void TeddyCannotSlide()
        {
            var sim = new RunnerSim(HeroDef.ById("teddy"), 1u);
            sim.Step(new SimInput { Slide = true });
            Assert.AreEqual(MoveState.Running, sim.Move);
        }

        [Test]
        public void LaneChangesStopAtTheEdge()
        {
            var sim = new RunnerSim(HeroDef.Roster[0], 1u);
            for (int i = 0; i < 5; i++) sim.Step(new SimInput { LaneDelta = 1 });
            Assert.AreEqual(sim.Config.LaneCount - 1, sim.TargetLane);
        }

        [Test]
        public void EveryHeroSurvivesTenSecondsWithoutThrowing()
        {
            foreach (var hero in HeroDef.Roster)
            {
                var sim = new RunnerSim(hero, 5u);
                for (int t = 0; t < 600; t++)
                {
                    sim.Step(new SimInput { Special = t == 100, Jump = t % 50 == 0, Slide = t % 70 == 0 });
                }
                Assert.Greater(sim.Z, 50f, hero.DisplayName);
            }
        }
    }
}
