// Grey-box view: primitives only, built in code, so the chase can be felt
// before any Tripo asset exists. Swapping in models later means replacing the
// Make* methods; nothing else reads the shapes.
//
// World space = sim space: x = sim X, z = sim Z (metres from the stage start),
// y up. The sim resets Z to 0 each state, so the whole stage is rebuilt then.

using System.Collections.Generic;
using PeachMe.SimCore;
using UnityEngine;

namespace PeachMe.Game
{
    public sealed class GreyBoxView : MonoBehaviour
    {
        public float JumpHeight = 1.6f;
        public float PeachRadius = 1.4f;

        private RunDriver _driver;
        private Material _baseMaterial;
        private readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();

        private Transform _stageRoot;
        private Transform _hero;
        private Renderer _heroRenderer;
        private float _heroHeight = 1.8f;
        private Transform _peach;
        private Transform _peachBody;
        private float _peachRoll;
        private float _peachSquash;
        private readonly List<(TrackItem item, GameObject go)> _items = new List<(TrackItem, GameObject)>();

        public Transform Hero => _hero;

        private void OnEnable()
        {
            _driver = FindAnyObjectByType<RunDriver>();
            if (_driver == null)
            {
                Debug.LogWarning("GreyBoxView: no RunDriver in the scene - nothing to draw.");
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
                    _peachSquash = 0.45f;
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
                var (item, go) = _items[i];
                bool show = !item.Consumed;
                if (go.activeSelf != show) go.SetActive(show);
                if (show && item.Kind == ItemKind.Star) go.transform.Rotate(0f, 180f * Time.deltaTime, 0f, Space.World);
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
                squat = 0.45f;
            }

            _hero.position = new Vector3(sim.X, y, sim.Z);
            var s = _hero.localScale;
            _hero.localScale = new Vector3(s.x, _heroHeight * 0.5f * squat, s.z);
            _hero.position += Vector3.up * (_heroHeight * 0.5f * squat);

            // Stumble shows as a forward lurch, recovering with SpeedFactor.
            float lurch = (1f - sim.SpeedFactor) * 35f;
            _hero.rotation = Quaternion.Euler(lurch, 0f, 0f);

            // Blink while invulnerable after a catch.
            _heroRenderer.enabled = sim.InvulnTimer <= 0f || Mathf.Repeat(Time.time * 10f, 1f) > 0.5f;
        }

        private void PlacePeach(RunnerSim sim)
        {
            float frontZ = sim.PeachZ;
            _peach.position = new Vector3(0f, PeachRadius, frontZ - PeachRadius);

            _peachRoll += sim.PeachSpeed * Time.deltaTime / PeachRadius * Mathf.Rad2Deg;
            _peachBody.localRotation = Quaternion.Euler(_peachRoll, 0f, 0f);

            _peachSquash = Mathf.Max(0f, _peachSquash - Time.deltaTime);
            float k = Mathf.Sin(_peachSquash / 0.45f * Mathf.PI) * 0.3f;
            _peach.localScale = new Vector3(1f + k, 1f - k, 1f + k);
        }

        // ------------------------------------------------------------------
        // Building
        // ------------------------------------------------------------------

        private void BuildHero(HeroDef hero)
        {
            if (_hero != null) Destroy(_hero.gameObject);
            var go = Make(PrimitiveType.Capsule, transform, HeroColour(hero.Id));
            go.name = "Hero_" + hero.Id;
            float width = hero.HalfWidth * 2f;
            _heroHeight = hero.Id == "abe" ? 2.2f : 1.8f;
            go.transform.localScale = new Vector3(width, _heroHeight * 0.5f, width);
            _hero = go.transform;
            _heroRenderer = go.GetComponent<Renderer>();
        }

        private void BuildPeach()
        {
            if (_peach != null) Destroy(_peach.gameObject);
            var root = new GameObject("Peach").transform;
            root.SetParent(transform, false);
            var body = Make(PrimitiveType.Sphere, root, new Color(1f, 0.6f, 0.4f));
            body.name = "Body";
            body.transform.localScale = Vector3.one * PeachRadius * 2f;
            var leaf = Make(PrimitiveType.Cube, body.transform, new Color(0.3f, 0.7f, 0.2f));
            leaf.name = "Leaf";
            leaf.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            leaf.transform.localScale = new Vector3(0.12f, 0.06f, 0.25f);
            _peach = root;
            _peachBody = body.transform;
        }

        private void BuildStage(RunnerSim sim)
        {
            if (_stageRoot != null) Destroy(_stageRoot.gameObject);
            _items.Clear();
            _stageRoot = new GameObject("Stage_" + sim.Stage.Code).transform;
            _stageRoot.SetParent(transform, false);

            var cfg = sim.Config;
            float width = cfg.LaneCount * cfg.LaneWidth + 2f;
            float length = cfg.StageLength + 80f;

            var ground = Make(PrimitiveType.Cube, _stageRoot, BiomeColour(sim.Stage.Biome));
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(width, 0.2f, length);
            ground.transform.localPosition = new Vector3(0f, -0.1f, cfg.StageLength * 0.5f - 20f);

            for (int i = 1; i < cfg.LaneCount; i++)
            {
                float x = (i - cfg.LaneCount * 0.5f) * cfg.LaneWidth;
                var line = Make(PrimitiveType.Cube, _stageRoot, new Color(1f, 1f, 1f, 1f));
                line.name = "LaneLine";
                line.transform.localScale = new Vector3(0.06f, 0.02f, length);
                line.transform.localPosition = new Vector3(x, 0.01f, cfg.StageLength * 0.5f - 20f);
            }

            var finish = Make(PrimitiveType.Cube, _stageRoot, new Color(0.15f, 0.25f, 0.8f));
            finish.name = "StateLine";
            finish.transform.localScale = new Vector3(width, 0.03f, 1.2f);
            finish.transform.localPosition = new Vector3(0f, 0.02f, cfg.StageLength);

            foreach (var item in sim.Items)
            {
                var go = MakeItem(item, cfg);
                _items.Add((item, go));
            }
        }

        private GameObject MakeItem(TrackItem item, SimConfig cfg)
        {
            float x = (item.Lane - (cfg.LaneCount - 1) * 0.5f) * cfg.LaneWidth;
            GameObject go;
            switch (item.Kind)
            {
                case ItemKind.TaxForms:   // low: jump it
                    go = Make(PrimitiveType.Cube, _stageRoot, new Color(0.95f, 0.95f, 0.9f));
                    go.transform.localScale = new Vector3(1.5f, 0.6f, 0.6f);
                    go.transform.localPosition = new Vector3(x, 0.3f, item.Z);
                    break;
                case ItemKind.RedTape:    // high: slide under it
                    go = Make(PrimitiveType.Cube, _stageRoot, new Color(0.85f, 0.1f, 0.1f));
                    go.transform.localScale = new Vector3(1.7f, 0.35f, 0.3f);
                    go.transform.localPosition = new Vector3(x, 1.35f, item.Z);
                    break;
                case ItemKind.Briefcase:  // block: change lane
                    go = Make(PrimitiveType.Cube, _stageRoot, new Color(0.4f, 0.25f, 0.12f));
                    go.transform.localScale = new Vector3(1.5f, 1.9f, 0.8f);
                    go.transform.localPosition = new Vector3(x, 0.95f, item.Z);
                    break;
                case ItemKind.Star:
                    go = Make(PrimitiveType.Cube, _stageRoot, new Color(1f, 0.85f, 0.1f));
                    go.transform.localScale = new Vector3(0.45f, 0.45f, 0.1f);
                    go.transform.localPosition = new Vector3(x, 0.8f, item.Z);
                    break;
                case ItemKind.HotDog:
                    go = Make(PrimitiveType.Capsule, _stageRoot, new Color(0.8f, 0.4f, 0.2f));
                    go.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);
                    go.transform.localPosition = new Vector3(x, 0.6f, item.Z);
                    go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    break;
                case ItemKind.FakeNews:
                    go = Make(PrimitiveType.Cylinder, _stageRoot, new Color(0.55f, 0.55f, 0.6f));
                    go.transform.localScale = new Vector3(0.4f, 0.45f, 0.4f);
                    go.transform.localPosition = new Vector3(x, 0.45f, item.Z);
                    go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    break;
                default:                  // FlipFlop
                    go = Make(PrimitiveType.Cube, _stageRoot, new Color(0.9f, 0.2f, 0.7f));
                    go.transform.localScale = new Vector3(0.7f, 0.12f, 1.3f);
                    go.transform.localPosition = new Vector3(x, 0.1f, item.Z);
                    break;
            }
            go.name = item.Kind.ToString();
            return go;
        }

        // ------------------------------------------------------------------
        // Primitives & colours
        // ------------------------------------------------------------------

        private GameObject Make(PrimitiveType type, Transform parent, Color colour)
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
            return go;
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
