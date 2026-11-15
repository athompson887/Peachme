# Verification - the playtest list

Everything built and not yet seen in the game. A line is added in the same
commit as the change; it is ticked only after it has been seen on screen.
`[ ]` not yet seen - `[x]` seen and right - `[!]` seen and wrong (fix before
new work on the same surface).

## Grey-box (M0)

- [ ] **V-001 Scene builds.** Do: PeachMe > Build Run Scene. Expect: `Assets/Scenes/Run.unity` open, with Main Camera, Sun and Run; first in File > Build Profiles scene list; no errors in the Console.
- [ ] **V-002 HUD panel.** Do: open the project after first compile. Expect: `Assets/UI/Resources/PeachPanel.asset` exists with PeachTheme as its theme; Console says "[Toolkit] made ...".
- [ ] **V-003 Title screen.** Do: Play. Expect: Delaware ground, hero capsule, peach behind; HUD reads "1/51 Delaware", a hero name in the middle, strength/weakness and "Swipe left/right to pick, tap to run".
- [ ] **V-004 Hero pick.** Do: on the title, press Left/Right a few times. Expect: name, colour and capsule width change (Donny wide, Abe tall and thin, Abe black).
- [ ] **V-005 Run starts, peach wakes.** Do: Space. Expect: hero runs; "The peach is waking up..." for ~2 s then the peach starts rolling; "Welcome to Delaware" banner for ~1.5 s.
- [ ] **V-006 Lanes.** Do: Left/Right while running. Expect: one lane per press, quick slide across (~0.18 s), stops at the edges.
- [ ] **V-007 Jump/slide.** Do: Space over white tax forms; S under red tape. Expect: clears both; hitting either makes the hero lurch forward and the green gap bar shrink.
- [ ] **V-008 Briefcases.** Do: run into a brown briefcase. Expect: lurch, gap shrinks; can't jump or slide past it.
- [ ] **V-009 Pickups and hazards.** Do: run through stars, a hot dog, a grey scroll and a pink flip-flop. Expect: score up on stars; "HOT DOG BOOST!" and a visible speed-up; "FAKE NEWS! -50" / "FLIP-FLOP! -50" toasts and the gap bar drops.
- [ ] **V-010 Peach close.** Do: hit several obstacles in a row. Expect: gap bar turns orange under ~6 m, camera pulls back so the peach is in view.
- [ ] **V-011 Caught.** Do: keep hitting obstacles until caught. Expect: peach squashes, "SQUISHED! 2 left", hero blinks for ~1.5 s, peach reset further back.
- [ ] **V-012 Game over and retry.** Do: get caught three times. Expect: "Impeached!", "Tap to try again from Delaware"; Space/Enter restarts at the chapter's first state with 3 lives.
- [ ] **V-013 State cleared.** Do: reach the blue line at the end of Delaware. Expect: "Delaware cleared!" for ~1.6 s, then Pennsylvania with a new ground colour and the camera snapped, not flying back.
- [ ] **V-014 Specials.** Do: pick Teddy, Bill and Dick in turn and press E. Expect: Teddy "Charge!" and smashes obstacles for 2.5 s ("SMASH!"); Bill's peach visibly slows for 2 s; Dick's gap bar jumps up. Cooldown counts down in the HUD.
- [ ] **V-015 Voice log.** Do: play for a minute. Expect: `[Voice] commentator/run_start (no clip yet)` style lines in the Console, not spamming (at most one every ~2.5 s apart from priority lines); no errors.
- [ ] **V-016 Phone.** Do: build to a phone (portrait). Expect: swipes move/jump/slide, single tap starts/retries, double-tap fires the special; HUD sits inside the notch/safe area and is readable.

## Models (Tripo)

- [ ] **V-017 Grey box still there with no art.** Do: with `Assets/Resources/Art` empty, Play. Expect: everything as in V-003..V-014; no `[Art]` warnings in the Console.
- [ ] **V-018 First model swaps in.** Do: put `hero_abe.fbx` in `Assets/Resources/Art/Heroes/`, pick Abe, Play. Expect: Abe's model replaces the capsule, feet on the ground, 2.2 m tall (as tall as the old capsule), facing away from the camera. Other heroes are still capsules.
- [ ] **V-019 Model fits its box.** Do: add `item_briefcase.fbx` and `item_taxforms.fbx`, Play. Expect: each sits on the ground in its lane, no bigger than its grey-box shape was; you hit the briefcase exactly when the model touches you, not before or after.
- [ ] **V-020 Hero moves still read.** Do: with a hero model in, jump, slide and stumble. Expect: jumps clear tax forms; the slide squashes the model low enough to pass under red tape; the stumble lurches forward from the feet; the model blinks after a catch.
- [ ] **V-021 Peach model.** Do: add `peach_angry.fbx` (and `peach_smug`, `peach_squashed` if made). Expect: the face stays towards the hero and the peach waddles (rocks) as it moves rather than rolling; turns smug under ~6 m; swaps to squashed for ~0.45 s on a catch.
- [ ] **V-022 Backwards model.** Do: if any model faces the camera instead of away, set `yaw: 180f` in its spec in `ArtLibrary.cs`. Expect: it turns round and still sits in its lane.
