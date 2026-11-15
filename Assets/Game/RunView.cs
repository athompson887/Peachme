// Draws the run: hero, peach, track, items. Each thing is a model from
// ArtLibrary when one exists in Assets/Resources/Art, and its grey-box
// primitive when not - so Tripo models can arrive one at a time and the game
// is always playable. (Was GreyBoxView; renamed once it drew models too.)
//
// World space = sim space: x = sim X, z = sim Z (metres from the stage start),
// y up. The sim resets Z to 0 each state, so the stage is rebuilt then.
//
// Every thing is a HOLDER the view moves; the model or primitive sits inside
// it, already fitted to the spec's box. Nothing here sizes a model.

using System.Collections.Generic;
using PeachMe.SimCore;
using UnityEngine;

namespace PeachMe.Game
{
    public sealed class RunView : MonoBehaviour
    {
        public float JumpHeight = 1.6f;

        [Tooltip("Swap the peach to its smug face when it's this close (m). 0 = never.")]
        public float SmugGap = 6f;

        private const float SquashSeconds = 0.45f;

        private RunDriver _driver;
        private Material _baseMaterial;
        private readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();

        private Transform _stageRoot;

        private Transform _hero;
        private Renderer[] _heroRenderers;

        private Transform _peach;        // at the ball's centre; moved every frame
        private Transform _peachRoll;    // rolls about X (grey ball) or wobbles (a model with a face)
        private bool _peachHasFace;
        private Transform _peachAngry;   // model or grey-box ball
        private Transform _peachSmug;    // model, or null
        private Transform _peachSquashed;// model, or null
        private float _peachRollDeg;
        private float _squashTimer;

        private readonly List<(TrackItem item, Transform t)> _items = new List<(TrackItem, Transform)>();

        public Transform Hero => _hero;

        private void OnEnable()
        {
            _driver = FindAnyObjectByType<RunDriver>();
            if (_driver == null)
            {
                Debug.LogWarning("RunView: no RunDriver in the scene - nothing to draw.");
                enabled = false;
                return;
            }
            _driver.RunCreated += OnRunCreated;
            _driver.SimEventRaised += OnSimEvent;
        }

        private void OnDisable()
        {
            if (_driver == null) return;
            _driver.RunCreated -= OnRunCreated;
            _driver.SimEventRaised -= OnSimEvent;
        }

        private void Start()
        {
            // RunDriver.Awake has already made the first run; build it.
            if (_driver != null && _driver.Sim != null) OnRunCreated(_driver.Sim);
        }

        // ------------------------------------------------------------------

        private void OnRunCreated(RunnerSim sim)
        {
            BuildHero(sim.Hero);
            BuildPeach();
            BuildStage(sim);
        }

        private void OnSimEvent(SimEvent e)
        {
            switch (e.Type)
            {
                case SimEventType.StageStarted:
                    BuildStage(_driver.Sim);
                    break;
                case SimEventType.PeachCaught:
                    _squashTimer = SquashSeconds;
                    break;
            }
        }

        private void LateUpdate()
        {
            var sim = _driver != null ? _driver.Sim : null;
            if (sim == null || _hero == null) return; // nothing built yet

            PlaceHero(sim);
            PlacePeach(sim);

            for (int i = 0; i < _items.Count; i++)
            {
                var (item, t) = _items[i];
                bool show = !item.Consumed;
                if (t.gameObject.activeSelf != show) t.gameObject.SetActive(show);
                if (show && item.Kind == ItemKind.Star) t.Rotate(0f, 180f * Time.deltaTime, 0f, Space.World);
            }
        }

        // ------------------------------------------------------------------
        // Placement
        // ------------------------------------------------------------------

        private void PlaceHero(RunnerSim sim)
        {
            float y = 0f;
            float squat = 1f;
            if (sim.Move == MoveState.Jumping)
            {
                float total = sim.Config.JumpTime * sim.Hero.JumpTimeMul;
                float p = total > 0f ? 1f - sim.MoveTimer / total : 1f;
                y = Mathf.Sin(Mathf.PI * Mathf.Clamp01(p)) * JumpHeight;
            }
            else if (sim.Move == MoveState.Sliding)
            {
                // A CHOICE: 0.45 of standing height reads as a slide and is
                // under the red tape's 1.2 m bar for every hero (2.2 x 0.45 = 0.99).
                squat = 0.45f;
            }

            // The holder's pivot is the feet, so squat and lurch both work from the ground.
            _hero.position = new Vector3(sim.X, y, sim.Z);
            _hero.localScale = new Vector3(1f, squat, 1f);

            // Stumble shows as a forward lurch, recovering with SpeedFactor.
            float lurch = (1f - sim.SpeedFactor) * 35f;
            _hero.rotation = Quaternion.Euler(lurch, 0f, 0f);

            // Blink while invulnerable after a catch.
            bool visible = sim.InvulnTimer <= 0f || Mathf.Repeat(Time.time * 10f, 1f) > 0.5f;
            for (int i = 0; i < _heroRenderers.Length; i++) _heroRenderers[i].enabled = visible;
        }

        private void PlacePeach(RunnerSim sim)
        {
            float r = ArtSpecs.PeachRadius;
            _peach.position = new Vector3(0f, r, sim.PeachZ - r);

            _peachRollDeg += sim.PeachSpeed * Time.deltaTime / r * Mathf.Rad2Deg;
            if (_peachHasFace)
            {
                // A model's face must stay towards the hero, so it waddles rather
                // than rolls: a rock in time with the distance it covers. A CHOICE:
                // 10 degrees forward-back, 6 side to side.
                float a = _peachRollDeg * Mathf.Deg2Rad;
                _peachRoll.localRotation = Quaternion.Euler(Mathf.Sin(a * 2f) * 10f, 0f, Mathf.Sin(a) * 6f);
            }
            else
            {
                _peachRoll.localRotation = Quaternion.Euler(_peachRollDeg, 0f, 0f);
            }

            _squashTimer = Mathf.Max(0f, _squashTimer - Time.deltaTime);
            bool squashing = _squashTimer > 0f;

            if (_peachSquashed != null)
            {
                // A squashed model exists: swap to it rather than squeezing the ball.
                _peachSquashed.gameObject.SetActive(squashing);
                _peachRoll.gameObject.SetActive(!squashing);
                _peach.localScale = Vector3.one;
            }
            else
            {
                float k = Mathf.Sin(_squashTimer / SquashSeconds * Mathf.PI) * 0.3f;
                _peach.localScale = new Vector3(1f + k, 1f - k, 1f + k);
            }

            if (_peachSmug != null)
            {
                bool smug = SmugGap > 0f && sim.PeachGap < SmugGap;
                _peachSmug.gameObject.SetActive(smug);
                _peachAngry.gameObject.SetActive(!smug);
            }
        }

        // ------------------------------------------------------------------
        // Building
        // ------------------------------------------------------------------

        private void BuildHero(HeroDef hero)
        {
            if (_hero != null) Destroy(_hero.gameObject);
            var spec = ArtSpecs.ForHero(hero);
            _hero = ArtLibrary.TryMake(spec, transform, "Hero_" + hero.Id);
            if (_hero == null)
            {
                _hero = Holder("Hero_" + hero.Id, transform);
                var capsule = Prim(PrimitiveType.Capsule, _hero, HeroColour(hero.Id));
                // A capsule is 2 units tall at scale 1, centred.
                capsule.localScale = new Vector3(spec.Size.x, spec.Size.y * 0.5f, spec.Size.z);
                capsule.localPosition = new Vector3(0f, spec.Size.y * 0.5f, 0f);
            }
            _heroRenderers = _hero.GetComponentsInChildren<Renderer>(true);
        }

        private void BuildPeach()
        {
            if (_peach != null) Destroy(_peach.gameObject);
            _peach = Holder("Peach", transform);
            _peachRoll = Holder("Roll", _peach);

            _peachAngry = ArtLibrary.TryMake(ArtSpecs.PeachAngry, _peachRoll, "Angry");
            _peachHasFace = _peachAngry != null;
            if (_peachAngry == null) _peachAngry = GreyPeach(_peachRoll);

            // The other faces only exist as models; with none, the angry one does it all.
            _peachSmug = ArtLibrary.TryMake(ArtSpecs.PeachSmug, _peachRoll, "Smug");
            if (_peachSmug != null) _peachSmug.gameObject.SetActive(false);

            _peachSquashed = ArtLibrary.TryMake(ArtSpecs.PeachSquashed, _peach, "Squashed");
            if (_peachSquashed != null)
            {
                // Base pivot on the ground: the holder is at the ball's centre, one radius up.
                _peachSquashed.localPosition = new Vector3(0f, -ArtSpecs.PeachRadius, 0f);
                _peachSquashed.gameObject.SetActive(false);
            }
        }

        private Transform GreyPeach(Transform parent)
        {
            var body = Holder("Angry", parent);
            var ball = Prim(PrimitiveType.Sphere, body, new Color(1f, 0.6f, 0.4f));
            ball.localScale = Vector3.one * ArtSpecs.PeachRadius * 2f;
            var leaf = Prim(PrimitiveType.Cube, ball, new Color(0.3f, 0.7f, 0.2f));
            leaf.localPosition = new Vector3(0f, 0.52f, 0f);
            leaf.localScale = new Vector3(0.12f, 0.06f, 0.25f);
            return body;
        }

        private void BuildStage(RunnerSim sim)
        {
            if (_stageRoot != null) Destroy(_stageRoot.gameObject);
            _items.Clear();
            _stageRoot = Holder("Stage_" + sim.Stage.Code, transform);

            var cfg = sim.Config;
            float width = cfg.LaneCount * cfg.LaneWidth + 2f;
            float length = cfg.StageLength + 80f;

            var ground = Prim(PrimitiveType.Cube, _stageRoot, BiomeColour(sim.Stage.Biome));
            ground.name = "Ground";
            ground.localScale = new Vector3(width, 0.2f, length);
            ground.localPosition = new Vector3(0f, -0.1f, cfg.StageLength * 0.5f - 20f);

            for (int i = 1; i < cfg.LaneCount; i++)
            {
                float x = (i - cfg.LaneCount * 0.5f) * cfg.LaneWidth;
                var line = Prim(PrimitiveType.Cube, _stageRoot, Color.white);
                line.name = "LaneLine";
                line.localScale = new Vector3(0.06f, 0.02f, length);
                line.localPosition = new Vector3(x, 0.01f, cfg.StageLength * 0.5f - 20f);
            }

            var stateLine = ArtLibrary.TryMake(ArtSpecs.StateLine, _stageRoot, "StateLine");
            if (stateLine == null)
            {
                stateLine = Holder("StateLine", _stageRoot);
                var strip = Prim(PrimitiveType.Cube, stateLine, new Color(0.15f, 0.25f, 0.8f));
                strip.localScale = new Vector3(width, 0.03f, 1.2f);
                strip.localPosition = new Vector3(0f, 0.02f, 0f);
            }
            stateLine.localPosition = new Vector3(0f, 0f, cfg.StageLength);

            foreach (var item in sim.Items)
            {
                _items.Add((item, MakeItem(item, cfg)));
            }
        }

        private Transform MakeItem(TrackItem item, SimConfig cfg)
        {
            var spec = ArtSpecs.ForItem(item.Kind);
            float x = (item.Lane - (cfg.LaneCount - 1) * 0.5f) * cfg.LaneWidth;

            var t = ArtLibrary.TryMake(spec, _stageRoot, item.Kind.ToString());
            if (t == null) t = GreyItem(item.Kind, spec);
            t.localPosition = new Vector3(x, spec.Y, item.Z);
            return t;
        }

        // Grey-box shapes, sized from the same spec the model would be fitted to.
        private Transform GreyItem(ItemKind kind, ArtSpec spec)
        {
            var holder = Holder(kind.ToString(), _stageRoot);
            Vector3 s = spec.Size;
            bool baseUp = spec.Pivot == Pivot.Base;
            Transform shape;
            switch (kind)
            {
                case ItemKind.TaxForms: shape = Prim(PrimitiveType.Cube, holder, new Color(0.95f, 0.95f, 0.9f)); break;
                case ItemKind.Briefcase: shape = Prim(PrimitiveType.Cube, holder, new Color(0.4f, 0.25f, 0.12f)); break;
                case ItemKind.Star: shape = Prim(PrimitiveType.Cube, holder, new Color(1f, 0.85f, 0.1f)); break;
                case ItemKind.FlipFlop: shape = Prim(PrimitiveType.Cube, holder, new Color(0.9f, 0.2f, 0.7f)); break;
                case ItemKind.RedTape:
                    // Just the bar - the part you slide under - at the top of the spec's box.
                    shape = Prim(PrimitiveType.Cube, holder, new Color(0.85f, 0.1f, 0.1f));
                    shape.localScale = new Vector3(1.7f, ArtSpecs.RedTapeBar, s.z);
                    shape.localPosition = new Vector3(0f, ArtSpecs.RedTapeTop - ArtSpecs.RedTapeBar * 0.5f, 0f);
                    return holder;
                case ItemKind.HotDog:
                case ItemKind.FakeNews:
                    // Lying along x. A capsule/cylinder is 2 units long on its axis at scale 1.
                    shape = Prim(kind == ItemKind.HotDog ? PrimitiveType.Capsule : PrimitiveType.Cylinder, holder,
                                 kind == ItemKind.HotDog ? new Color(0.8f, 0.4f, 0.2f) : new Color(0.55f, 0.55f, 0.6f));
                    shape.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    shape.localScale = new Vector3(s.y, s.x * 0.5f, s.z);
                    return holder;
                default:
                    shape = Prim(PrimitiveType.Cube, holder, Color.grey); break;
            }
            shape.localScale = s;
            shape.localPosition = baseUp ? new Vector3(0f, s.y * 0.5f, 0f) : Vector3.zero;
            return holder;
        }

        // ------------------------------------------------------------------
        // Primitives & colours
        // ------------------------------------------------------------------

        private static Transform Holder(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        private Transform Prim(PrimitiveType type, Transform parent, Color colour)
        {
            var go = GameObject.CreatePrimitive(type);
            // The sim does all collision; Unity physics must not.
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<Renderer>();
            if (_baseMaterial == null) _baseMaterial = r.sharedMaterial; // the pipeline's default Lit
            r.sharedMaterial = MaterialFor(colour);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        private Material MaterialFor(Color c)
        {
            if (_materials.TryGetValue(c, out var m)) return m;
            m = new Material(_baseMaterial) { color = c };
            _materials[c] = m;
            return m;
        }

        private static Color HeroColour(string id)
        {
            switch (id)
            {
                case "georgie": return new Color(0.85f, 0.85f, 0.95f);
                case "abe": return new Color(0.12f, 0.12f, 0.14f);
                case "teddy": return new Color(0.55f, 0.45f, 0.25f);
                case "donny": return new Color(0.15f, 0.2f, 0.45f);
                case "bill": return new Color(0.35f, 0.35f, 0.4f);
                default: return new Color(0.3f, 0.3f, 0.25f);
            }
        }

        private static Color BiomeColour(Biome b)
        {
            switch (b)
            {
                case Biome.Coast: return new Color(0.85f, 0.8f, 0.6f);
                case Biome.Forest: return new Color(0.25f, 0.5f, 0.25f);
                case Biome.Plains: return new Color(0.6f, 0.7f, 0.3f);
                case Biome.Desert: return new Color(0.85f, 0.65f, 0.4f);
                case Biome.Mountain: return new Color(0.5f, 0.5f, 0.5f);
                case Biome.Swamp: return new Color(0.3f, 0.4f, 0.3f);
                case Biome.Snow: return new Color(0.92f, 0.95f, 1f);
                case Biome.Tropical: return new Color(0.4f, 0.75f, 0.45f);
                default: return new Color(0.45f, 0.45f, 0.5f); // City
            }
        }
    }
}
