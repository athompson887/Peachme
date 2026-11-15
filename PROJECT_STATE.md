# PeachMe - PROJECT_STATE

The design itself is in `Docs/Plan.md`.

Mobile-only (iOS/Android) Unity 6 URP runner. A parody president flees a giant
peach through all 50 states in admission order (Delaware -> Hawaii), with a
Washington DC finale. Folder: ~/peachme

## Architecture
- `Assets/SimCore/` - pure C#, **no UnityEngine**. Fixed step 1/60, deterministic (SimRandom xorshift32).
  - `SimConfig.cs` - every tunable number. Mirrors `Tools/tune_peach.py` CFG.
  - `RunnerSim.cs` - the sim. `Step(SimInput)` once per fixed tick; read `Events` after.
  - `TrackGenerator.cs` - seeded rows per stage; always leaves an obstacle-free lane.
  - `HeroDef.cs` - roster of 6 parody presidents (traits + specials).
  - `StageTable.cs` - 51 stages with biome tags; chapters of 6.
  - `SimEvents.cs` - event stream for audio (ElevenLabs), VFX, UI.
- `Assets/Game/` (Unity): `RunDriver` (fixed-step accumulator, flow: Title/Running/Banner/GameOver/Won),
  `RunInput` (swipes, keys, pad), `RunView` + `ArtLibrary` (Tripo models when present, grey-box primitives otherwise), `ChaseCamera`, `RunHud` (UI Toolkit),
  `Commentary` (event -> ElevenLabs clip, with cooldowns).
- `Assets/Editor/`: `SceneBuilder` (PeachMe > Build Run Scene), `ToolkitAssets` (HUD PanelSettings).
- `Assets/Tests/`: 24 NUnit tests, run headless by `Tools/SimCoreTests`.
- Project settings, URP assets and .gitattributes taken from FlightSim2026; portrait only,
  bundle id `com.athompson887.peachme`. See CLAUDE.md for the rules.

## Rules
- Python first: change `Tools/tune_peach.py`, check the table, then copy numbers into `SimConfig.cs`.
- Whole-file delivery.
- Every early `return` in Step/Update is a potential silent skip - comment the intentional ones.
- Don't trust Inspector values to match code defaults.
- Characters are parody archetypes only: no real likenesses, logos, or cloned voices.

## Flow
Running -> StageComplete (Unity plays "Welcome to X", calls `BeginNextStage`) -> ...
Caught with 0 lives -> GameOver -> Unity calls `RetryFromCheckpoint` (chapter start, full lives, mercy +1).
Final stage cleared -> Won.

## Tuning snapshot (tune_peach.py, 60 runs, checkpoint play)
| band | skill | clean clear | first game over | game overs to win |
|---|---|---|---|---|
| casual | 0.80 | 0% | stage 15 | 18.7 (target <=15, slightly high) |
| regular | 0.90 | 0% | stage 36 | 2.9 |
| expert | 0.96 | 98% | - | 0.02 |
Heroes at regular: all 1.5-4.0 game overs (Bill strongest, Donny weakest - his trade-off is 2x points).

## Verification
- `dotnet test Tools/SimCoreTests/SimCoreTests.csproj` - 24 tests (also on GitHub Actions).
- `./Tools/check-unity-build.sh` - Game/Editor compile as Unity and as iOS/Android players.
- `Docs/Verification.md` - playtest list, V-001..V-016 unticked.

## Next
1. Open in Unity, Build Run Scene, play through Docs/Verification.md (M0 grey-box).
2. Tripo: Docs/ArtPrompts.md has every prompt, size and file name; start with batch 1 there.
3. ElevenLabs: design 7 voices, put IDs in Tools/Voice/voices.json, run build-voice.py (100 lines ready).
