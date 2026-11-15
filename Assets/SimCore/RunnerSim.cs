// PeachMe SimCore - pure C#, no UnityEngine references allowed in this folder.
// Fixed-step, deterministic runner sim. Dynamics mirror run_stage() in
// tools/tune_peach.py - change Python first, then this file.
//
// Unity contract:
//   - Call Step(input) exactly once per fixed tick (Config.FixedDt).
//   - After each Step, read Events (cleared at the start of the next Step).
//   - On Phase == StageComplete, play the "Welcome to X" beat, then BeginNextStage().
//   - On Phase == GameOver, show the retry screen, then RetryFromCheckpoint().

using System;
using System.Collections.Generic;

namespace PeachMe.SimCore
{
    public struct SimInput
    {
        public int LaneDelta;   // -1 left, +1 right, 0 none. Edge-triggered (one per swipe).
        public bool Jump;
        public bool Slide;
        public bool Special;

        public static readonly SimInput None = default;
    }

    public enum RunPhase { Running, StageComplete, Won, GameOver }

    public enum MoveState { Running, Jumping, Sliding }

    public sealed class RunnerSim
    {
        public readonly SimConfig Config;
        public readonly HeroDef Hero;
        public readonly uint RunSeed;
        public readonly List<SimEvent> Events = new List<SimEvent>(16);

        // --- Stage ---
        public int StageIndex { get; private set; }
        public StageInfo Stage => StageTable.Stages[StageIndex];
        public int Chapter => StageTable.ChapterOf(StageIndex, Config.ChapterSize);
        public float Difficulty { get; private set; }
        public IReadOnlyList<TrackItem> Items => _items;
        public RunPhase Phase { get; private set; }
        public double Time { get; private set; }
        public double StageTime { get; private set; }

        // --- Player ---
        public float Z { get; private set; }            // metres from stage start
        public float X { get; private set; }            // lateral metres, 0 = centre lane
        public int TargetLane { get; private set; }
        public MoveState Move { get; private set; }
        public float MoveTimer { get; private set; }
        public float SpeedFactor { get; private set; }  // 0..1 stumble recovery
        public float Speed { get; private set; }        // m/s this tick
        public float BoostTimer { get; private set; }
        public float InvulnTimer { get; private set; }
        public float SpecialTimer { get; private set; }
        public float SpecialCooldown { get; private set; }
        public bool SpecialActive => SpecialTimer > 0f;
        public bool SpecialReady => Hero.Special != SpecialKind.None && SpecialCooldown <= 0f && SpecialTimer <= 0f;

        // --- Peach ---
        public float PeachGap { get; private set; }
        public float PeachSpeed { get; private set; }
        public bool PeachAwake => _peachWakeTimer <= 0f;
        public float PeachZ => Z - PeachGap;              // for the Unity view

        // --- Meta ---
        public float Score { get; private set; }
        public int Lives { get; private set; }
        public int StarsCollected { get; private set; }
        public int GameOvers { get; private set; }
        public int MercySteps { get; private set; }

        private List<TrackItem> _items = new List<TrackItem>();
        private int _nextItem;
        private float _peachWakeTimer;
        private bool _peachWasClose;

        public RunnerSim(HeroDef hero, uint runSeed, SimConfig config = null, int startStage = 0)
        {
            Config = config ?? new SimConfig();
            Hero = hero ?? HeroDef.Roster[0];
            RunSeed = runSeed;
            StartFromStage(startStage, firstRun: true);
        }

        // ------------------------------------------------------------------
        // Public flow control
        // ------------------------------------------------------------------

        public void BeginNextStage()
        {
            if (Phase != RunPhase.StageComplete) return; // guard: only valid after StageCleared
            Events.Clear();
            int next = StageIndex + 1;
            if (next % Config.ChapterSize == 0)
            {
                MercySteps = 0;
                Lives = Math.Min(Config.MaxLives, Lives + 1);
                Events.Add(new SimEvent(SimEventType.ChapterStarted, StageTable.ChapterOf(next, Config.ChapterSize)));
                Events.Add(new SimEvent(SimEventType.LifeRestored, Lives));
            }
            LoadStage(next);
            Phase = RunPhase.Running;
        }

        public void RetryFromCheckpoint()
        {
            if (Phase != RunPhase.GameOver) return; // guard: only valid after RunOver
            MercySteps = Math.Min(Config.MercyMaxSteps, MercySteps + 1);
            StartFromStage(StageTable.ChapterStart(StageIndex, Config.ChapterSize), firstRun: false);
        }

        // ------------------------------------------------------------------
        // Tick
        // ------------------------------------------------------------------

        public void Step(SimInput input)
        {
            Events.Clear();
            // Intentional early return: the sim is frozen outside Running.
            // Unity must drive BeginNextStage / RetryFromCheckpoint to resume.
            if (Phase != RunPhase.Running) return;

            float dt = Config.FixedDt;
            Time += dt;
            StageTime += dt;

            HandleInput(input);
            UpdateTimers(dt);

            float prevZ = Z;
            Speed = NominalSpeed() * Hero.SpeedMul * SpeedFactor * (BoostTimer > 0f ? Config.BoostMul : 1f);
            Z += Speed * dt;

            UpdateLateral(dt);
            ResolveItems(prevZ, Z);
            UpdatePeach(dt);
            CheckStageEnd();
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private void StartFromStage(int stage, bool firstRun)
        {
            Events.Clear();
            Lives = Config.StartLives;
            PeachGap = Config.PeachStartGap;
            SpeedFactor = 1f;
            BoostTimer = 0f;
            InvulnTimer = 0f;
            SpecialTimer = 0f;
            SpecialCooldown = 0f;
            _peachWakeTimer = firstRun ? Config.PeachWakeDelay : 0f;
            _peachWasClose = false;
            if (firstRun)
            {
                Score = 0f;
                StarsCollected = 0;
                Events.Add(new SimEvent(SimEventType.RunStarted));
            }
            LoadStage(stage);
            Phase = RunPhase.Running;
        }

        private void LoadStage(int stage)
        {
            StageIndex = Math.Max(0, Math.Min(StageTable.Count - 1, stage));
            Difficulty = TrackGenerator.DifficultyOf(StageIndex, Config);
            _items = TrackGenerator.Generate(StageIndex, RunSeed, Config);
            _nextItem = 0;
            Z = 0f;
            StageTime = 0;
            TargetLane = Config.LaneCount / 2;
            X = LaneX(TargetLane);
            Move = MoveState.Running;
            MoveTimer = 0f;
            Events.Add(new SimEvent(SimEventType.StageStarted, StageIndex));
        }

        private float NominalSpeed()
        {
            return Config.BaseRunSpeed + Config.SpeedPerStage * StageIndex;
        }

        private float LaneX(int lane)
        {
            return (lane - (Config.LaneCount - 1) * 0.5f) * Config.LaneWidth;
        }

        private void HandleInput(SimInput input)
        {
            if (input.LaneDelta != 0)
            {
                int target = Math.Max(0, Math.Min(Config.LaneCount - 1, TargetLane + Math.Sign(input.LaneDelta)));
                if (target != TargetLane)
                {
                    TargetLane = target;
                    Events.Add(new SimEvent(SimEventType.LaneChanged, TargetLane));
                }
            }

            if (input.Jump && Move != MoveState.Jumping)
            {
                // Jump cancels a slide.
                Move = MoveState.Jumping;
                MoveTimer = Config.JumpTime * Hero.JumpTimeMul;
                Events.Add(new SimEvent(SimEventType.Jumped));
            }
            else if (input.Slide && Hero.CanSlide && Move != MoveState.Sliding)
            {
                // Slide during a jump = fast-fall into slide.
                Move = MoveState.Sliding;
                MoveTimer = Config.SlideTime;
                Events.Add(new SimEvent(SimEventType.Slid));
            }

            if (input.Special && SpecialReady)
            {
                SpecialTimer = Hero.SpecialDuration;
                SpecialCooldown = Hero.SpecialCooldown;
                Events.Add(new SimEvent(SimEventType.SpecialStarted, (int)Hero.Special));
                if (Hero.Special == SpecialKind.TapeErase)
                {
                    PeachGap = Math.Min(Config.PeachMaxGap, PeachGap + Config.TapeEraseGap);
                }
            }
        }

        private void UpdateTimers(float dt)
        {
            BoostTimer = Math.Max(0f, BoostTimer - dt);
            InvulnTimer = Math.Max(0f, InvulnTimer - dt);

            if (SpecialTimer > 0f)
            {
                SpecialTimer = Math.Max(0f, SpecialTimer - dt);
                if (SpecialTimer <= 0f)
                {
                    Events.Add(new SimEvent(SimEventType.SpecialEnded, (int)Hero.Special));
                }
            }

            if (SpecialCooldown > 0f)
            {
                SpecialCooldown = Math.Max(0f, SpecialCooldown - dt);
                if (SpecialCooldown <= 0f && Hero.Special != SpecialKind.None)
                {
                    Events.Add(new SimEvent(SimEventType.SpecialReady, (int)Hero.Special));
                }
            }

            SpeedFactor = Math.Min(1f, SpeedFactor + dt / Hero.AccelTime);

            if (MoveTimer > 0f)
            {
                MoveTimer = Math.Max(0f, MoveTimer - dt);
                if (MoveTimer <= 0f) Move = MoveState.Running;
            }
        }

        private void UpdateLateral(float dt)
        {
            float targetX = LaneX(TargetLane);
            float maxStep = Config.LaneWidth / (Config.LaneChangeTime * Hero.LaneChangeTimeMul) * dt;
            float dx = targetX - X;
            X = Math.Abs(dx) <= maxStep ? targetX : X + Math.Sign(dx) * maxStep;
        }

        private void ResolveItems(float prevZ, float z)
        {
            while (_nextItem < _items.Count && _items[_nextItem].Z <= z)
            {
                var item = _items[_nextItem++];
                if (item.Consumed) continue;

                float lateral = Math.Abs(X - LaneX(item.Lane));

                if (item.IsObstacle)
                {
                    if (lateral > Hero.HalfWidth + Config.ObstacleHalfWidth) continue;
                    if (item.Kind == ItemKind.TaxForms && Move == MoveState.Jumping) continue;
                    if (item.Kind == ItemKind.RedTape && Move == MoveState.Sliding) continue;
                    if (InvulnTimer > 0f) continue;

                    item.Consumed = true;
                    if (Hero.Special == SpecialKind.Charge && SpecialActive)
                    {
                        Score += Config.SmashPoints * Hero.PointsMul;
                        Events.Add(new SimEvent(SimEventType.ObstacleSmashed, (int)item.Kind));
                    }
                    else
                    {
                        SpeedFactor = Config.StumbleSpeedFactor;
                        Events.Add(new SimEvent(SimEventType.ObstacleHit, (int)item.Kind));
                    }
                }
                else if (item.IsPickup)
                {
                    if (lateral > Hero.HalfWidth + Config.PickupHalfWidth + Hero.MagnetRadius) continue;
                    item.Consumed = true;
                    if (item.Kind == ItemKind.Star)
                    {
                        StarsCollected++;
                        Score += Config.StarPoints * Hero.PointsMul;
                        Events.Add(new SimEvent(SimEventType.StarCollected));
                    }
                    else
                    {
                        BoostTimer = Config.BoostTime;
                        Events.Add(new SimEvent(SimEventType.HotDogCollected));
                    }
                }
                else if (item.IsHazard)
                {
                    if (lateral > Hero.HalfWidth + Config.PickupHalfWidth) continue;
                    item.Consumed = true;
                    float lost = Math.Min(Score, Config.HazardPoints * Hero.HazardPenaltyMul);
                    Score -= lost;
                    PeachGap -= Config.HazardGapPenalty * Hero.HazardPenaltyMul;
                    Events.Add(new SimEvent(SimEventType.HazardHit, (int)item.Kind, lost));
                }
            }

            Score += (z - prevZ) * Config.PointsPerMetre * Hero.PointsMul;
        }

        private void UpdatePeach(float dt)
        {
            if (_peachWakeTimer > 0f)
            {
                _peachWakeTimer -= dt;
                PeachSpeed = 0f;
                if (_peachWakeTimer <= 0f) Events.Add(new SimEvent(SimEventType.PeachWoke));
            }
            else
            {
                float factor = Config.PeachBaseFactor
                             + Config.PeachDifficultyFactor * Difficulty
                             - Config.MercyPerRetry * MercySteps;
                if (Hero.Special == SpecialKind.SaxDash && SpecialActive) factor *= Config.SaxPeachFactor;
                PeachSpeed = NominalSpeed() * factor;
            }

            PeachGap = Math.Min(Config.PeachMaxGap, PeachGap + (Speed - PeachSpeed) * dt);

            bool close = PeachGap < Config.PeachCloseGap;
            if (close && !_peachWasClose) Events.Add(new SimEvent(SimEventType.PeachClose, 0, PeachGap));
            if (!close && _peachWasClose) Events.Add(new SimEvent(SimEventType.PeachBackedOff, 0, PeachGap));
            _peachWasClose = close;

            if (PeachGap <= 0f && InvulnTimer <= 0f)
            {
                Lives--;
                Events.Add(new SimEvent(SimEventType.PeachCaught, Lives));
                if (Lives <= 0)
                {
                    GameOvers++;
                    Phase = RunPhase.GameOver;
                    Events.Add(new SimEvent(SimEventType.RunOver, StageTable.ChapterStart(StageIndex, Config.ChapterSize)));
                    return;
                }
                PeachGap = Config.CaughtResetGap;
                InvulnTimer = Config.CatchInvuln;
                SpeedFactor = 1f;
                _peachWasClose = false;
            }
            PeachGap = Math.Max(0f, PeachGap);
        }

        private void CheckStageEnd()
        {
            if (Phase != RunPhase.Running || Z < Config.StageLength) return;

            Events.Add(new SimEvent(SimEventType.StageCleared, StageIndex));
            if (StageIndex >= StageTable.Count - 1)
            {
                Phase = RunPhase.Won;
                Events.Add(new SimEvent(SimEventType.RunWon));
            }
            else
            {
                Phase = RunPhase.StageComplete;
            }
        }
    }
}
