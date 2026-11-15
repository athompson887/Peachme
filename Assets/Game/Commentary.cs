// Maps sim events to voice lines. The sim stays silent; this decides what is
// worth saying and how often, so the commentator never nags.
//
// Clips are generated offline from Tools/Voice/lines.csv with ElevenLabs and
// land in Assets/Resources/Voice/<speaker>/<key>_<n>.mp3 (peach_close_1,
// peach_close_2, ...). A missing clip is fine - the line is logged instead,
// so this works before any audio exists.

using System.Collections.Generic;
using PeachMe.SimCore;
using UnityEngine;

namespace PeachMe.Game
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class Commentary : MonoBehaviour
    {
        [Tooltip("Minimum seconds between any two ordinary lines.")]
        public float GlobalGap = 2.5f;

        [Tooltip("Minimum seconds before the same line key is used again.")]
        public float KeyCooldown = 12f;

        [Tooltip("How many variants of each key to look for (key_1 .. key_N).")]
        public int MaxVariants = 4;

        public bool LogMissingClips = true;

        private enum Urgency
        {
            Normal,     // dropped if anything was said recently
            Priority,   // cuts off whatever is playing
            Chained     // waits for the current line to finish
        }

        private RunDriver _driver;
        private AudioSource _source;
        private float _lastAny = -100f;
        private Flow _lastFlow = Flow.Title;
        private readonly Dictionary<string, float> _lastByKey = new Dictionary<string, float>();
        private readonly Dictionary<string, List<AudioClip>> _clips = new Dictionary<string, List<AudioClip>>();
        private readonly Queue<AudioClip> _queue = new Queue<AudioClip>();
        private readonly System.Random _rng = new System.Random();

        private void OnEnable()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _driver = FindAnyObjectByType<RunDriver>();
            if (_driver == null)
            {
                Debug.LogWarning("Commentary: no RunDriver in the scene.");
                enabled = false;
                return;
            }
            _driver.SimEventRaised += OnSimEvent;
        }

        private void OnDisable()
        {
            if (_driver != null) _driver.SimEventRaised -= OnSimEvent;
        }

        private void Update()
        {
            // The opening lines wait for the player to start: on the title a
            // new run is made for every hero browsed past.
            if (_lastFlow == Flow.Title && _driver.Flow == Flow.Running)
            {
                Say("commentator", "run_start", Urgency.Priority);
                Say("commentator", "welcome_" + _driver.Sim.Stage.Code.ToLowerInvariant(), Urgency.Chained);
            }
            _lastFlow = _driver.Flow;

            if (!_source.isPlaying && _queue.Count > 0) _source.PlayOneShot(_queue.Dequeue());
        }

        private void OnSimEvent(SimEvent e)
        {
            if (_driver.Flow == Flow.Title) return; // browsing heroes: stay quiet

            // Hero barks use the hero's speaker; everything else is the commentator.
            string hero = _driver.Hero.Id;
            switch (e.Type)
            {
                case SimEventType.PeachWoke: Say("commentator", "peach_wakes", Urgency.Chained); break;
                case SimEventType.StageStarted: Say("commentator", "welcome_" + _driver.Sim.Stage.Code.ToLowerInvariant(), Urgency.Chained); break;
                case SimEventType.StageCleared: Say(hero, "state_cleared", Urgency.Priority); break;
                case SimEventType.ChapterStarted: Say("commentator", "chapter", Urgency.Chained); break;
                case SimEventType.PeachClose: Say("commentator", "peach_close"); break;
                case SimEventType.PeachBackedOff: Say(hero, "escaped"); break;
                case SimEventType.PeachCaught: Say("commentator", "caught", Urgency.Priority); break;
                case SimEventType.ObstacleHit: Say(hero, "stumble"); break;
                case SimEventType.HazardHit: Say("commentator", (ItemKind)e.Data == ItemKind.FakeNews ? "fake_news" : "flip_flop"); break;
                case SimEventType.HotDogCollected: Say(hero, "hot_dog"); break;
                case SimEventType.SpecialStarted: Say(hero, "special", Urgency.Priority); break;
                case SimEventType.RunOver: Say("commentator", "game_over", Urgency.Priority); break;
                case SimEventType.RunWon: Say("commentator", "won", Urgency.Priority); break;
            }
        }

        private void Say(string speaker, string key, Urgency urgency = Urgency.Normal)
        {
            float now = Time.unscaledTime;
            string slot = speaker + "/" + key;
            if (urgency == Urgency.Normal && (now - _lastAny < GlobalGap || _source.isPlaying)) return; // don't talk over
            if (_lastByKey.TryGetValue(slot, out float last) && now - last < KeyCooldown) return; // said it recently

            _lastAny = now;
            _lastByKey[slot] = now;

            var clips = ClipsFor(speaker, key);
            if (clips.Count == 0)
            {
                if (LogMissingClips) Debug.Log($"[Voice] {slot} (no clip yet)");
                return;
            }
            var clip = clips[_rng.Next(clips.Count)];

            switch (urgency)
            {
                case Urgency.Priority:
                    _queue.Clear();
                    _source.Stop();
                    _source.PlayOneShot(clip);
                    break;
                case Urgency.Chained:
                    if (_queue.Count < 2) _queue.Enqueue(clip); // short queue: a stale line is worse than none
                    break;
                default:
                    _source.PlayOneShot(clip);
                    break;
            }
        }

        private List<AudioClip> ClipsFor(string speaker, string key)
        {
            string slot = speaker + "/" + key;
            if (_clips.TryGetValue(slot, out var list)) return list;
            list = new List<AudioClip>();
            for (int i = 1; i <= MaxVariants; i++)
            {
                var clip = Resources.Load<AudioClip>($"Voice/{speaker}/{key}_{i}");
                if (clip != null) list.Add(clip);
            }
            _clips[slot] = list;
            return list;
        }
    }
}
