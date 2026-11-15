// The one seam between Unity's frame and the sim's fixed tick.
// It owns the RunnerSim, steps it at Config.FixedDt from an accumulator, and
// re-broadcasts every SimEvent to the views. No game rule lives here: if a new
// `if` changes what the sim is asked to do, it belongs in SimCore.

using System;
using PeachMe.SimCore;
using UnityEngine;

namespace PeachMe.Game
{
    public enum Flow { Title, Running, Banner, GameOver, Won }

    [DefaultExecutionOrder(-100)]
    public sealed class RunDriver : MonoBehaviour
    {
        [Tooltip("Seconds the 'Welcome to X' banner holds between states.")]
        public float BannerSeconds = 1.6f;

        [Tooltip("Cap on sim steps per frame so a hitch can't spiral.")]
        public int MaxStepsPerFrame = 8;

        public RunnerSim Sim { get; private set; }
        public Flow Flow { get; private set; } = Flow.Title;
        public int HeroIndex { get; private set; }
        public HeroDef Hero => HeroDef.Roster[HeroIndex];
        public float BannerTimer { get; private set; }

        /// <summary>Every sim event, in order, after the step that raised it.</summary>
        public event Action<SimEvent> SimEventRaised;

        /// <summary>A fresh RunnerSim exists (new run). Views rebuild from it.</summary>
        public event Action<RunnerSim> RunCreated;

        private RunInput _input;
        private float _accumulator;
        private uint _seed;

        private void Awake()
        {
            _input = GetComponent<RunInput>();
            if (_input == null) _input = gameObject.AddComponent<RunInput>();
            Application.targetFrameRate = 60;
            _seed = (uint)Environment.TickCount;
            CreateRun(); // a sim exists from the first frame so views never see null
        }

        private void Update()
        {
            switch (Flow)
            {
                case Flow.Title: UpdateTitle(); break;
                case Flow.Running: UpdateRunning(); break;
                case Flow.Banner: UpdateBanner(); break;
                case Flow.GameOver: UpdateGameOver(); break;
                case Flow.Won: UpdateWon(); break;
            }
        }

        // ------------------------------------------------------------------

        private void UpdateTitle()
        {
            var pending = _input.Consume();
            if (pending.LaneDelta != 0)
            {
                int n = HeroDef.Roster.Count;
                HeroIndex = (HeroIndex + pending.LaneDelta + n) % n;
                CreateRun();
            }
            if (pending.Jump || _input.ConsumeConfirm())
            {
                _accumulator = 0f;
                Flow = Flow.Running;
            }
        }

        private void UpdateRunning()
        {
            _accumulator += Time.deltaTime;
            float dt = Sim.Config.FixedDt;
            int steps = 0;
            while (_accumulator >= dt && steps < MaxStepsPerFrame && Sim.Phase == RunPhase.Running)
            {
                // Edge-triggered input goes to the FIRST step only, so one swipe is one lane.
                var input = steps == 0 ? _input.Consume() : SimInput.None;
                Sim.Step(input);
                Dispatch();
                _accumulator -= dt;
                steps++;
            }
            if (steps == MaxStepsPerFrame) _accumulator = 0f; // drop the backlog after a hitch

            switch (Sim.Phase)
            {
                case RunPhase.StageComplete:
                    BannerTimer = BannerSeconds;
                    Flow = Flow.Banner;
                    break;
                case RunPhase.GameOver:
                    _input.Clear();
                    Flow = Flow.GameOver;
                    break;
                case RunPhase.Won:
                    _input.Clear();
                    Flow = Flow.Won;
                    break;
            }
        }

        private void UpdateBanner()
        {
            BannerTimer -= Time.deltaTime;
            _input.Clear(); // swipes during the banner shouldn't leak into the next state
            if (BannerTimer > 0f) return; // intentional: wait out the banner
            Sim.BeginNextStage();
            Dispatch();
            _accumulator = 0f;
            Flow = Flow.Running;
        }

        private void UpdateGameOver()
        {
            if (!_input.ConsumeConfirm()) return; // intentional: wait for a tap
            Sim.RetryFromCheckpoint();
            Dispatch();
            _accumulator = 0f;
            Flow = Flow.Running;
        }

        private void UpdateWon()
        {
            if (!_input.ConsumeConfirm()) return; // intentional: wait for a tap
            CreateRun();
            Flow = Flow.Title;
        }

        private void CreateRun()
        {
            _seed = _seed * 1664525u + 1013904223u;
            Sim = new RunnerSim(Hero, _seed);
            _input.Clear();
            RunCreated?.Invoke(Sim);
            Dispatch();
        }

        private void Dispatch()
        {
            var events = Sim.Events;
            for (int i = 0; i < events.Count; i++)
            {
                SimEventRaised?.Invoke(events[i]);
            }
        }
    }
}
