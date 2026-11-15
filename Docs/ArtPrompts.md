# Tripo prompt sheet

Every 3D asset in PeachMe, with the prompt to paste into Tripo (text to model),
the size it must come out at, and the file name it goes in under.

The sizes are not choices made here. They are the grey-box shapes in
`Assets/Game/GreyBoxView.cs`, which match the sim's hit widths in `SimConfig`
and `HeroDef`. A model that looks bigger than its hitbox feels unfair: the player
"misses" a briefcase and still hits it. If a model has to be a different size,
change the sim first, in Python, then here.

## House rules

**Paste this style preamble at the start of every prompt:**

```
Chunky low-poly stylised cartoon, mobile game asset, toy-like proportions, clean simple topology, flat bright colours with soft hand-painted shading, big readable silhouette from behind and above, single object, centred, no background, no ground plane, no text, no logos, no letters
```

**Negative prompt** (where Tripo offers one):

```
realistic, photorealistic, real person likeness, celebrity face, text, letters, logo, flag, watermark, base, pedestal, ground, multiple objects, thin spikes, floating parts
```

**Parody, not portraits.** The six heroes are archetypes built from props and
silhouette (a hat, a tie, a wig, a saxophone), never a real face. No real flags,
seals, party logos or campaign slogans on anything. That keeps it a parody and
keeps it through App Store and Play review. If a result looks like a real
person's face, reroll it.

**Export:** FBX, which Unity imports with no extra package. GLB also works but
needs the glTFast package. Both are stored in Git LFS (`.gitattributes`).

**Import conventions.** Check these in the asset viewer before committing anything:

| Rule | Value |
|---|---|
| Units | 1 unit = 1 metre |
| Facing | Character looks down **+Z** (the direction of running) |
| Pivot | Heroes, obstacles, props: **feet / base centre**. Peach and pickups: **centre** |
| Up | +Y |
| Root rotation | Never overwrite the importer's root rotation; compose with it (FlightSim2026 rule) |

**Budgets (choices, sized for mid-range phones):**

| Kind | Triangles | Texture |
|---|---|---|
| Hero | ≤ 8,000 | 1024² |
| Peach | ≤ 4,000 | 1024² |
| Obstacle / pickup / hazard | ≤ 1,500 | 512² |
| Scenery prop | ≤ 2,000 | 512² |
| DC landmark | ≤ 6,000 | 1024² |

Use Tripo's low-poly / face-limit option at these numbers rather than
decimating afterwards.

**Files:** `Assets/Art/<Folder>/<name>.fbx`, all lower case with underscores, as listed below.

---

## 1. The Peach (`Assets/Art/Peach/`)

Size: a sphere **2.8 m across** (radius 1.4). Pivot at the centre. It rolls
around X in code, so the body must be close to round, with the face on the
**+Z side, facing the hero** (it chases from behind, so the camera sees the face).

| File | Prompt (after the preamble) |
|---|---|
| `peach_angry.fbx` | `Giant round peach character, furious cartoon face on the front, thick angry eyebrows, wide snarling mouth with blunt rounded teeth, one green leaf and short stalk on top, soft orange-pink fuzzy skin with a red blush and a visible crease down one side, almost perfectly spherical so it can roll` |
| `peach_smug.fbx` | `Giant round peach character, smug self-satisfied cartoon face, half-closed eyes, one raised eyebrow, sly grin, one green leaf and short stalk on top, orange-pink fuzzy skin with a crease down one side, almost perfectly spherical` |
| `peach_squashed.fbx` | `Giant peach character squashed flat like a cushion after landing on something, wide and short, dizzy cartoon face with spiral eyes, leaf bent sideways, orange-pink fuzzy skin` |

Angry is the normal chase. Smug is for when it's gaining (gap below 6 m).
Squashed is the "caught you" frame.

## 2. Heroes (`Assets/Art/Heroes/`)

Pivot at the feet, facing +Z, standing in a **neutral A-pose** so it can be
rigged (use Tripo's auto-rig if you want running animation from it). Width is
shoulder to shoulder, which is the sim's hit width.

| File | Height × width | Prompt (after the preamble) |
|---|---|---|
| `hero_georgie.fbx` | 1.8 × 0.8 m | `Cheerful cartoon colonial gentleman, white powdered wig tied in a ponytail with a black ribbon, navy blue long coat with gold buttons and buff waistcoat, white cravat, knee breeches and buckled shoes, rosy cheeks, determined grin, stylised archetype not a real person, A-pose` |
| `hero_abe.fbx` | 2.2 × 0.7 m | `Very tall thin cartoon gentleman, tall black stovepipe hat, long black frock coat, black bow tie, chin-strap beard with no moustache, long legs, slightly worried kind expression, stylised archetype not a real person, A-pose` |
| `hero_teddy.fbx` | 1.8 × 0.9 m | `Stocky energetic cartoon adventurer, khaki rough-rider cavalry uniform with neckerchief, wide-brim campaign hat, small round spectacles, huge bushy moustache, big toothy grin, fists clenched, stylised archetype not a real person, A-pose` |
| `hero_donny.fbx` | 1.8 × 0.9 m | `Broad cartoon businessman, big swooping golden-blond hairdo, navy suit, extremely long bright red tie hanging past the belt, white shirt, pouting confident expression, stylised archetype not a real person, A-pose` |
| `hero_bill.fbx` | 1.8 × 0.8 m | `Friendly cartoon politician, fluffy grey hair, dark blue suit, loosened tie, big warm grin, golden saxophone slung on a strap across the chest, stylised archetype not a real person, A-pose` |
| `hero_dick.fbx` | 1.8 × 0.8 m | `Shifty cartoon politician, dark slicked-back hair, heavy eyebrows, dark grey suit, nervous sideways glance, a tangle of brown cassette tape spilling from his jacket pocket, stylised archetype not a real person, A-pose` |

The widths follow `HeroDef.HalfWidth × 2`: Donny and Teddy are wide (0.45),
Abe is narrow (0.35), the rest are 0.4. Abe is tall because the grey-box
draws him at 2.2 m.

## 3. Obstacles (`Assets/Art/Items/`)

Pivot at the base centre, facing +Z (the hero runs into the −Z face, so that's
the face that should read). These sizes are the hitboxes, so stay inside them.

| File | Size (W × H × D) | What the player does | Prompt (after the preamble) |
|---|---|---|---|
| `item_taxforms.fbx` | 1.5 × 0.6 × 0.6 m | Jump | `Low wide messy pile of white paper tax forms and folders, a few sheets sliding off, paperclips, one rubber stamp on top, chunky and readable, no writing` |
| `item_redtape.fbx` | 1.7 wide, bar 0.35 tall, bottom of bar at 1.2 m | Slide under | `Thick sagging band of bright red tape strung between two short wooden posts, like a barrier ribbon, wide and low enough to duck under, bold and readable` |
| `item_briefcase.fbx` | 1.5 × 1.9 × 0.8 m | Change lane | `Giant upright brown leather lobbyist briefcase, gold clasps, bulging with banknotes poking out of the seams, standing on its end like a wall, no text or symbols on the money` |

For the red tape, the posts must be outside the 1.7 m width or thin enough not
to look solid. The player slides under the bar, not between the posts.

## 4. Pickups (`Assets/Art/Items/`)

Pivot at the centre. They spin or bob in code, so keep them symmetrical.

| File | Size | Prompt (after the preamble) |
|---|---|---|
| `item_star.fbx` | 0.45 m across, 0.1 m thick | `Chunky five-pointed gold star coin, bevelled edges, glossy and bright, slightly puffy cartoon shape` |
| `item_hotdog.fbx` | ~1.0 m long, 0.35 m thick | `Oversized cartoon hot dog in a soft bun with a wavy line of yellow mustard, cheerful and appetising, lying horizontally` |

## 5. Hazards (`Assets/Art/Items/`)

These cost points and let the peach catch up, so they should look tempting but
a bit shady. Pivot at the base centre.

| File | Size | Prompt (after the preamble) |
|---|---|---|
| `item_fakenews.fbx` | 0.9 long × 0.4 thick | `Rolled-up grey newspaper scroll tied with a string, smudgy grey ink blotches instead of words, slightly crumpled, lying horizontally, no readable text` |
| `item_flipflop.fbx` | 0.7 × 0.12 × 1.3 m | `Giant bright pink beach flip-flop sandal lying flat, chunky foam sole, glossy strap, seen from above` |

## 6. Track furniture (`Assets/Art/Track/`)

| File | Size | Prompt (after the preamble) |
|---|---|---|
| `track_stateline.fbx` | 9 m wide, 4 m tall | `Simple cartoon finish-line arch made of two blue posts and a blank horizontal banner, bunting flags along the top, blank banner with no text, no stars or stripes pattern` |

The banner is left blank on purpose: the state's name is drawn on it in code,
so one model serves all 51 states.

## 7. Scenery by biome (`Assets/Art/Scenery/`)

Placed off the edges of the track (beyond x = ±4 m), so they never touch play.
Two or three per biome is enough to tell the states apart at running speed.
Pivot at the base centre. Make them 3 to 8 m tall.

| Biome | Files and prompts (after the preamble) |
|---|---|
| Coast | `scen_coast_lighthouse.fbx`: `Short chubby red and white striped lighthouse on a little rock` · `scen_coast_dune.fbx`: `Small sand dune with tufts of beach grass and a striped beach umbrella` |
| Forest | `scen_forest_pine.fbx`: `Rounded cartoon pine tree, layered chunky foliage` · `scen_forest_oak.fbx`: `Round fluffy cartoon oak tree with thick trunk` |
| Plains | `scen_plains_barn.fbx`: `Small red barn with white trim and a hay bale beside it` · `scen_plains_corn.fbx`: `Clump of tall cartoon corn stalks` |
| Desert | `scen_desert_cactus.fbx`: `Tall saguaro cactus with two raised arms, rounded and friendly` · `scen_desert_mesa.fbx`: `Small flat-topped red rock mesa with layered stripes` |
| Mountain | `scen_mountain_peak.fbx`: `Chunky low-poly mountain with a snowy cap, wide base` · `scen_mountain_cabin.fbx`: `Small log cabin with a stone chimney` |
| Swamp | `scen_swamp_cypress.fbx`: `Cypress tree with knobbly roots and hanging moss` · `scen_swamp_gator.fbx`: `Lazy cartoon alligator lying on a log, eyes half closed, friendly` |
| Snow | `scen_snow_fir.fbx`: `Snow-covered fir tree, puffy snow clumps` · `scen_snow_snowman.fbx`: `Cheerful three-ball snowman with a scarf and a carrot nose` |
| Tropical | `scen_tropical_palm.fbx`: `Curvy palm tree with coconuts` · `scen_tropical_hut.fbx`: `Small thatched beach hut on stilts` |
| City | `scen_city_block.fbx`: `Chunky cartoon brick apartment building, four storeys, rooftop water tower` · `scen_city_hydrant.fbx`: `Oversized red fire hydrant` |

## 8. The finale (`Assets/Art/Finale/`)

Washington DC, stage 51. One landmark at the end of the last track is enough.

| File | Size | Prompt (after the preamble) |
|---|---|---|
| `finale_capitol.fbx` | ~40 m wide, 30 m tall | `Grand white neoclassical government building with a large central dome, columns and wide steps, cartoon proportions, simplified, no flags, no text` |
| `finale_obelisk.fbx` | ~4 m wide, 35 m tall | `Tall plain white stone obelisk monument, simple and clean` |

---

## Order to make them in

1. `peach_angry`, `hero_abe`, `item_taxforms`, `item_briefcase`, `item_star`. That's enough to swap into the grey-box and feel it.
2. The other two obstacles, the other pickup, the hazards and the state line.
3. The other five heroes.
4. Scenery, one biome at a time, starting with Coast and Forest (states 1 to 5).
5. The peach variants and the finale.

## Before you keep one

- Is it the right size? Drop it next to the grey-box shape in the asset viewer.
- Is the pivot right, and does it face +Z?
- Is it under the triangle budget?
- No text, logos, flags or a real person's face anywhere on it?
- Does it read when it's small and seen from behind and above, which is how the camera sees it?
