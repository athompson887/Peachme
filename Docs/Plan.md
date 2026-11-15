# PeachMe - the plan

What the game is, and the decisions made so far. `PROJECT_STATE.md` says where
the build is; `CLAUDE.md` says how to work on it; this says what we're making.

## Pitch

A one-thumb runner. A giant, angry peach rolls after a parody US president
across all 50 states. Outrun it to Washington DC and you win. It's a play on
"impeach": fun and silly, never rude.

## Decided

| Question | Decision |
|---|---|
| Name and folder | PeachMe, `~/peachme`, github.com/athompson887/Peachme |
| Platforms | Mobile only (iOS and Android), portrait |
| Engine | Unity 6000.5.5f1, URP, FlightSim2026's SimCore/Unity split |
| Art | Tripo models (`Docs/ArtPrompts.md`), grey-box primitives until they arrive |
| Voices | ElevenLabs, generated offline, designed voices (never cloned) |
| Old Flutter version | Stays in its own repo, `inpeachme`, untouched |

## Core loop

- **Controls:** swipe left/right to change lane, up to jump, down to slide, double-tap for the hero's special. Keys and gamepad work in the editor.
- **The peach** chases from behind. Hit an obstacle and you stumble, so it gains. Pick up a hazard and it surges. Grab a hot dog and you pull away.
- **Caught** costs a life. Lose all three and it's game over: you restart from the start of the chapter.
- **Win** by clearing Washington DC, stage 51.

## The run

- **Stages:** the 50 states in the order they joined the Union (Delaware first, Hawaii 50th), then a DC finale. 450 m each.
- **Chapters:** 6 states each (9 in all). Each new chapter gives back a life, up to 3, and is the restart point after a game over.
- **Difficulty** ramps across the 51 stages: faster running, busier track, a slightly faster peach.
- **Mercy:** each game over in the same chapter slows the peach a little (capped), so everyone gets through eventually. It resets when the chapter is cleared.
- **Tracks** are generated from a seed, so we don't hand-build 51 levels. Every row always leaves a clean lane.
- **Looks** come from each state's biome: coast, forest, plains, desert, mountain, swamp, snow, tropical or city.

## Things on the track

| Kind | Items | What happens |
|---|---|---|
| Obstacles | Tax forms (jump), red tape (slide), lobbyist briefcase (change lane) | Hit one: stumble, the peach gains |
| Pickups | Gold stars, hot dogs | Stars are points; a hot dog is a speed boost |
| Hazards | Fake news scrolls, flip-flops | Lose points and the peach surges |

## The roster

Fictional parodies: the names and props do the work, never a real likeness.

| Hero | Strength | Weakness |
|---|---|---|
| Georgie Washingmachine | Wide pickup magnet | No special move |
| Abe Lincolnshire | Tall, big jumps | Slow to get going |
| Teddy Roosterfelt | Charge: smashes obstacles | Can't slide |
| Donny Trumpf | Double points | Wide and slow to turn |
| Bill Clintonne | Sax solo slows the peach | Long cooldown |
| Dick Nixxon | Erase the tape: gain ground | Hazards cost him more |

More can be added later as unlocks.

## Tone

Light humour, no cruelty. The commentator and hero barks (`Tools/Voice/lines.csv`)
riff on the state nicknames and each hero's archetype. No real flags, seals,
logos or slogans, and no real faces or cloned voices, which also keeps it clear
for App Store and Play review.

## Milestones

| | Milestone | Status |
|---|---|---|
| M0 | SimCore, Python tuning, grey-box chase in Unity | Code done; first play-through to do (`Docs/Verification.md`) |
| M1 | One state fully playable with one hero, real art | Next: Tripo batch 1 |
| M2 | Tripo assets in: 3 heroes, 3 biomes | |
| M3 | All 51 stages, the full roster, scoring and the win flow | Sim already covers all 51 |
| M4 | Polish, ElevenLabs voices, phone builds | Voice pipeline and 100 lines ready |

## Still open

- Scenery placement along the track, and the DC finale scene.
- What you spend points on (unlocks? cosmetics?) or whether it's just a high score.
- Casual players need about 19 game overs to finish; the target was 15 or fewer.
