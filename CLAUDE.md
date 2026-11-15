# PeachMe

A one-thumb portrait runner for iOS and Android. A parody president flees a
giant peach through the 50 states in the order they joined the Union, finishing
in Washington DC. Unity 6000.5.5f1, URP. It is meant to stay **small, fun and
playful**: when a choice grows the project, pick the smaller one.

Same working pattern as FlightSim2026, trimmed to fit.

## Assemblies

| Assembly | Folder | Contains | Unity? |
|---|---|---|---|
| SimCore | `Assets/SimCore` | The run: speed, lanes, peach, track, scoring, events | **No** |
| Game | `Assets/Game` | Driver, input, view (models or grey box), camera, HUD, voice | Yes |
| GameEditor | `Assets/Editor` | Scene builder, UI panel asset | Editor only |
| Tests | `Assets/Tests` | The suite (runs headless) | No |

**SimCore has no `using UnityEngine` anywhere.** The asmdef sets
`noEngineReferences`, the headless suite will not compile with one, and
`check-unity-build.sh` greps for it. Keep it that way.

**RunDriver is plumbing, not rules.** It steps the sim, forwards input and
events. If a new `if` there changes what the sim is asked to do, it belongs in
SimCore where a test can see it.

## Numbers: Python first

`Tools/tune_peach.py` is the source of truth for the chase curve. Change a
number there, run it, read the table against the targets, *then* copy it into
`SimConfig.cs`. One number, one place: the Python CFG and `SimConfig` use the
same names, and a value in one that isn't in the other is a bug.

```
python3 Tools/tune_peach.py                  # ~30 s, 60 runs per row
```

Where a number is a choice rather than a measurement, say so in a comment next
to it.

## Running the tests

```
dotnet test Tools/SimCoreTests/SimCoreTests.csproj
```

Headless, no Unity. Do **not** run `dotnet test` on Unity's generated
`Tests.csproj`: it builds, finds no runner and exits 0 - which looks exactly
like a pass and is not one.

Tests assert against something outside the code (the admission order of the
states, plain geometry, a rule written in the test's name), never the code
against itself.

## Checking it compiles the way Unity does

```
./Tools/check-unity-build.sh
```

SimCore, Game, Tests and GameEditor with Unity's own Roslyn, then SimCore and
Game again as iOS and Android players. Game and GameEditor have no tests, so
this is the only guard on them short of opening Unity. Run it before saying
something compiles. (Open the project in Unity once first - it needs the
generated `.csproj` files.)

## First open

1. Open `~/peachme` in Unity 6000.5.5f1. It imports and generates the csproj files.
2. **PeachMe > Build Run Scene** - makes `Assets/Scenes/Run.unity` in code and
   puts it first in the build.
3. The HUD panel asset is made automatically after compile (or **PeachMe > Make UI Panel**).
4. Press Play. Arrows/WASD to move, Space to jump/start, S to slide, E for the special.

The scene is built, not hand-edited: change `SceneBuilder.cs` and rebuild it.

## Nothing is done until it has been seen

`Docs/Verification.md` is the playtest list. Every commit that changes what a
player sees or does adds a line there in the same commit: a tick box, **Do**
(exactly what to press) and **Expect** (what right looks like). A green suite is
not "done"; a ticked line is.

## Art

Tripo models go in `Assets/Resources/Art/<Folder>/<name>.fbx` (names and prompts
in `Docs/ArtPrompts.md`). `ArtLibrary` loads one if it exists and fits it
uniformly into its spec's box; if not, `RunView` draws the grey-box shape from
the same spec. **The sizes live in `ArtSpecs` (ArtLibrary.cs) and nowhere else**:
they are the sim's hit areas, so a model can't look smaller than what you hit.
A model that faces backwards gets `yaw: 180f` in its spec, never a rotation
written onto the model.

## Voices

ElevenLabs, offline: `Tools/Voice/lines.csv` -> `build-voice.py` ->
`Assets/Resources/Voice/<speaker>/<key>_<n>.mp3`. `Commentary.cs` maps sim
events to keys with cooldowns and logs a line when its clip doesn't exist yet.
**Voices are designed, never cloned or imitating a real person**, and the
characters are parody archetypes with no real likenesses or logos. That keeps
it clean for the stores.

## Things that will bite you

**An error in the log is a pause.** With Error Pause on, anything logged at
error level stops play and looks like a hang. Use `Debug.LogWarning` for
"missing but survivable" (a clip, a panel asset).

**Every early `return` in Update/FixedUpdate is a potential silent skip.**
Intentional ones carry a comment saying what they are waiting for. When a
control "does nothing", read the first line of the method that would have done it.

**Don't trust Inspector values to match code defaults**, and play-mode edits
revert on stop. If a serialized field matters, check it in the scene.

**The `.csproj` files are generated and gitignored.** Don't hand-edit them.

**A ScriptableObject changed in play mode stays changed on disk.** The HUD
copies its PanelSettings before touching it; do the same with any asset.

**UI Toolkit only - no `OnGUI`.** `check-unity-build.sh` fails on one.

**Unity 6 compiles SimCore at C# 9.** No file-scoped namespaces, no record
structs. The headless project is pinned to 9 so it fails first.

## Writing style in the source

Comments explain **why**, especially why something is not the obvious thing.
Where a bug was fixed, say what it was and what it cost.

Deliver whole files or whole methods, never fragments.
