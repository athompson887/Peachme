# PeachMe - PROJECT_STATE

Mobile-only (iOS/Android) Unity 6 URP runner. A parody president flees a giant
peach through all 50 states in admission order (Delaware -> Hawaii), with a
Washington DC finale. Folder: ~/peachme

## Architecture
- `Assets/SimCore/` - pure C#, **no UnityEngine**. Fixed step 1/60, deterministic (SimRandom xorshift32).
  - `SimConfig.cs` - every tunable number. Mirrors `tools/tune_peach.py` CFG.
  - `RunnerSim.cs` - the sim. `Step(SimInput)` once per fixed tick; read `Events` after.
  - `TrackGenerator.cs` - seeded rows per stage; always leaves an obstacle-free lane.
  - `HeroDef.cs` - roster of 6 parody presidents (traits + specials).
  - `StageTable.cs` - 51 stages with biome tags; chapters of 6.
  - `SimEvents.cs` - event stream for audio (ElevenLabs), VFX, UI.
- Unity layer (not written yet): views, input, camera, audio. Reads sim, sends `SimInput`.

## Rules
- Python first: change `tools/tune_peach.py`, check the table, then copy numbers into `SimConfig.cs`.
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
- `mcs -langversion:experimental -out:SimSmoke.exe tools/SimSmoke.cs Assets/SimCore/*.cs && mono SimSmoke.exe`
  -> determinism, all heroes complete a campaign, no fully blocked rows. ALL PASS.

## Next
1. Unity layer M0: grey-box lanes, capsule hero, sphere peach, swipe input, fixed-step driver.
2. Tripo batch 1: peach (angry/squashed/smug), Abe, tax forms, red tape banner, briefcase, star, hot dog, fake news, flip-flop.
3. ElevenLabs: lines CSV keyed by SimEventType, offline generation script.
