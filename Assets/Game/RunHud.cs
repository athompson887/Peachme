// The HUD, in UI Toolkit (no OnGUI - same rule as FlightSim2026: IMGUI draws
// over everything and re-lays itself several times a frame).
//
// It hosts its own UIDocument. The PanelSettings asset (with the runtime theme)
// is made by the editor's ToolkitAssets at Assets/UI/Resources/PeachPanel.asset;
// without it the HUD still draws its labels, with a warning, not an error -
// an error in the log pauses play.

using PeachMe.SimCore;
using UnityEngine;
using UnityEngine.UIElements;

namespace PeachMe.Game
{
    public sealed class RunHud : MonoBehaviour
    {
        public const string PanelResource = "PeachPanel";
        public const string PanelAssetPath = "Assets/UI/Resources/PeachPanel.asset";

        public float WelcomeSeconds = 1.5f;
        public float ToastSeconds = 0.9f;

        private RunDriver _driver;
        private UIDocument _document;
        private PanelSettings _settings;

        private VisualElement _safe;
        private Label _state, _lives, _score, _banner, _hint, _special, _toast;
        private VisualElement _gapFill;
        private Rect _appliedSafe;

        private float _welcomeTimer;
        private Flow _lastFlow = Flow.Title;
        private float _toastTimer;

        private void OnEnable()
        {
            _driver = FindAnyObjectByType<RunDriver>();
            if (_driver == null)
            {
                Debug.LogWarning("RunHud: no RunDriver in the scene - nothing to show.");
                enabled = false;
                return;
            }
            _driver.SimEventRaised += OnSimEvent;
            EnsureDocument();
        }

        private void OnDisable()
        {
            if (_driver != null) _driver.SimEventRaised -= OnSimEvent;
        }

        private void OnDestroy()
        {
            if (_settings != null) Destroy(_settings);
            if (_document != null) Destroy(_document.gameObject);
        }

        // ------------------------------------------------------------------

        private void EnsureDocument()
        {
            if (_document != null) return;
            var go = new GameObject("HUD document");
            go.transform.SetParent(transform, false);

            // Inactive while it is put together: a UIDocument enabled without
            // its PanelSettings makes no panel and does not try again.
            go.SetActive(false);
            _document = go.AddComponent<UIDocument>();
            _document.panelSettings = MakeSettings();

            // Screen-space only. With world-space input on, the event system
            // raycasts through the camera and LOGS AN ERROR when the pointer
            // leaves the game view - which pauses play (FlightSim2026, V-521).
            var input = go.AddComponent<PanelInputConfiguration>();
            input.processWorldSpaceInput = false;
            go.SetActive(true);
        }

        private PanelSettings MakeSettings()
        {
            var asset = Resources.Load<PanelSettings>(PanelResource);
            if (asset != null && asset.themeStyleSheet != null)
            {
                // A COPY: a ScriptableObject changed in play mode stays changed on disk.
                _settings = Instantiate(asset);
            }
            else
            {
                Debug.LogWarning($"[HUD] {PanelAssetPath} is missing or has no theme - "
                               + "run PeachMe > Make UI Panel. Drawing unthemed for now.");
                _settings = ScriptableObject.CreateInstance<PanelSettings>();
                _settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                _settings.referenceResolution = new Vector2Int(1080, 1920);
                _settings.match = 0.5f;
            }
            return _settings;
        }

        // The UIDocument makes its root in its own OnEnable, which is not
        // promised to run before ours - so build whenever the root exists and
        // our tree is not in it.
        private void Build()
        {
            var root = _document != null ? _document.rootVisualElement : null;
            if (root == null || (_safe != null && _safe.parent == root)) return;

            root.pickingMode = PickingMode.Ignore;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) root.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));

            _safe = new VisualElement { name = "safe", pickingMode = PickingMode.Ignore };
            _safe.AddToClassList("pm-safe");
            root.Add(_safe);
            _appliedSafe = new Rect(-1, -1, -1, -1);

            var top = Box("pm-top");
            var left = Box("pm-col");
            _state = Text("pm-state");
            _lives = Text("pm-small");
            left.Add(_state);
            left.Add(_lives);
            _score = Text("pm-score");
            top.Add(left);
            top.Add(_score);

            var gapTrack = Box("pm-gap-track");
            _gapFill = Box("pm-gap-fill");
            gapTrack.Add(_gapFill);

            var upper = Box("pm-col");
            upper.Add(top);
            upper.Add(gapTrack);

            _banner = Text("pm-banner");
            _toast = Text("pm-small");
            _toast.style.unityTextAlign = TextAnchor.MiddleCenter;
            var middle = Box("pm-col");
            middle.style.flexGrow = 1;
            middle.style.justifyContent = Justify.Center;
            middle.Add(_banner);
            middle.Add(_toast);

            _special = Text("pm-special");
            _hint = Text("pm-hint");
            var lower = Box("pm-col");
            lower.Add(_special);
            lower.Add(_hint);

            _safe.Add(upper);
            _safe.Add(middle);
            _safe.Add(lower);
        }

        private static VisualElement Box(string cls)
        {
            var v = new VisualElement { pickingMode = PickingMode.Ignore };
            v.AddToClassList(cls);
            return v;
        }

        private static Label Text(string cls)
        {
            var l = new Label { pickingMode = PickingMode.Ignore };
            l.AddToClassList(cls);
            return l;
        }

        private void FitSafeArea()
        {
            var sa = Screen.safeArea;
            if (sa == _appliedSafe || Screen.width <= 0 || Screen.height <= 0) return;
            _appliedSafe = sa;
            _safe.style.left = Length.Percent(100f * sa.xMin / Screen.width);
            _safe.style.right = Length.Percent(100f * (Screen.width - sa.xMax) / Screen.width);
            // Screen.safeArea is bottom-up; UI Toolkit is top-down.
            _safe.style.top = Length.Percent(100f * (Screen.height - sa.yMax) / Screen.height);
            _safe.style.bottom = Length.Percent(100f * sa.yMin / Screen.height);
        }

        // ------------------------------------------------------------------

        private void OnSimEvent(SimEvent e)
        {
            switch (e.Type)
            {
                case SimEventType.StageStarted: _welcomeTimer = WelcomeSeconds; break;
                case SimEventType.HazardHit: Toast(((ItemKind)e.Data == ItemKind.FakeNews ? "FAKE NEWS!" : "FLIP-FLOP!") + $"  -{e.Value:0}"); break;
                case SimEventType.ObstacleSmashed: Toast("SMASH!"); break;
                case SimEventType.HotDogCollected: Toast("HOT DOG BOOST!"); break;
                case SimEventType.PeachCaught: Toast(e.Data > 0 ? "SQUISHED! " + e.Data + " left" : "SQUISHED!"); break;
                case SimEventType.LifeRestored: Toast("+1 LIFE"); break;
                case SimEventType.SpecialStarted: Toast(SpecialName((SpecialKind)e.Data) + "!"); break;
            }
        }

        private void Toast(string text)
        {
            if (_toast == null) return; // HUD not built yet; the moment has passed
            _toast.text = text;
            _toastTimer = ToastSeconds;
        }

        private void Update()
        {
            Build();
            if (_safe == null) return; // root not ready this frame; Build retries next frame
            FitSafeArea();

            _welcomeTimer = Mathf.Max(0f, _welcomeTimer - Time.deltaTime);
            _toastTimer = Mathf.Max(0f, _toastTimer - Time.deltaTime);
            _toast.style.opacity = Mathf.Clamp01(_toastTimer / 0.3f);

            var sim = _driver.Sim;
            var hero = _driver.Hero;

            // The first state's StageStarted fires while the title is up, so
            // the welcome is (re)started on the move from title to running.
            if (_lastFlow == Flow.Title && _driver.Flow == Flow.Running) _welcomeTimer = WelcomeSeconds;
            _lastFlow = _driver.Flow;

            _state.text = $"{sim.StageIndex + 1}/{StageTable.Count}  {sim.Stage.Name}";
            _lives.text = $"{hero.DisplayName}   Lives {sim.Lives}";
            _score.text = ((int)sim.Score).ToString("N0");

            float frac = Mathf.Clamp01(sim.PeachGap / sim.Config.PeachMaxGap);
            _gapFill.style.width = Length.Percent(100f * frac);
            _gapFill.EnableInClassList("pm-gap-fill--danger", sim.PeachGap < sim.Config.PeachCloseGap);

            if (hero.Special == SpecialKind.None)
            {
                _special.text = "";
            }
            else
            {
                bool ready = sim.SpecialReady;
                _special.text = ready
                    ? SpecialName(hero.Special) + " ready - double-tap"
                    : SpecialName(hero.Special) + "  " + Mathf.CeilToInt(sim.SpecialCooldown) + "s";
                _special.EnableInClassList("pm-special--ready", ready);
            }

            switch (_driver.Flow)
            {
                case Flow.Title:
                    _banner.text = hero.DisplayName;
                    _hint.text = $"+ {hero.Strength}\n- {hero.Weakness}\n\nSwipe left/right to pick, tap to run";
                    break;
                case Flow.Running:
                    _banner.text = _welcomeTimer > 0f ? "Welcome to\n" + sim.Stage.Name : "";
                    _hint.text = sim.PeachAwake ? "" : "The peach is waking up...";
                    break;
                case Flow.Banner:
                    _banner.text = sim.Stage.Name + "\ncleared!";
                    _hint.text = "";
                    break;
                case Flow.GameOver:
                    int start = StageTable.ChapterStart(sim.StageIndex, sim.Config.ChapterSize);
                    _banner.text = "Impeached!";
                    _hint.text = "Tap to try again from " + StageTable.Stages[start].Name;
                    break;
                case Flow.Won:
                    _banner.text = "You made it\nto Washington!";
                    _hint.text = $"Score {(int)sim.Score:N0} - tap to play again";
                    break;
            }
        }

        private static string SpecialName(SpecialKind k)
        {
            switch (k)
            {
                case SpecialKind.Charge: return "Charge";
                case SpecialKind.SaxDash: return "Sax solo";
                case SpecialKind.TapeErase: return "Erase the tape";
                default: return "";
            }
        }
    }
}
