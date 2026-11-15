#!/usr/bin/env python3
"""
PeachMe tuning harness - Python-first source of truth for the peach chase curve.

Mirrors RunnerSim's speed / stumble / boost / peach-gap dynamics step for step.
Dodging is abstracted: each obstacle is hit with a probability driven by bot
skill and hero traits (width, lane-change speed, jump time, can-slide).

Mode: checkpoint play. On game over the bot restarts the current chapter with
full lives, exactly like the real game. From one simulation we get:
  - clean clear %      (beat all 51 stages without a single game over)
  - first game over    (median stage index where the first game over happened)
  - game overs to win  (mean number of chapter restarts needed to finish)

Tuned values go back into SimCore/SimConfig.cs by hand. Python wins on disputes.

Run:  python3 tune_peach.py                 (default report)
      python3 tune_peach.py --runs 200 --dt 0.0166667
"""
import argparse
import random
import statistics
from dataclasses import dataclass
from multiprocessing import Pool

# ---------------------------------------------------------------------------
# Mirror of SimConfig.cs  (keep names identical, snake_case here)
# ---------------------------------------------------------------------------
CFG = dict(
    stage_count=51,             # 50 states in admission order + DC finale
    chapter_size=6,             # lives refill at each chapter start; checkpoint
    stage_length=450.0,         # metres per state
    difficulty_exponent=0.7,    # d = (idx/last)^exp ; <1 front-loads the ramp

    base_run_speed=9.0,         # m/s at stage 0
    speed_per_stage=0.06,       # m/s added per stage index

    stumble_speed_factor=0.20,  # speed factor straight after an obstacle hit
    boost_mul=1.35,
    boost_time=2.5,

    start_lives=3,
    max_lives=3,

    peach_start_gap=16.0,
    peach_max_gap=22.0,
    peach_base_factor=0.96,    # peach speed as fraction of nominal player speed
    peach_difficulty_factor=0.025,  # extra fraction at the final stage
    peach_wake_delay=2.0,       # peach sits still at run start (intro beat)
    catch_invuln=1.5,
    caught_reset_gap=12.0,
    mercy_per_retry=0.015,      # peach factor drop per game over in the same chapter
    mercy_max_steps=6,

    hazard_gap_penalty=3.0,     # fake news / flip-flop lets the peach surge

    row_spacing_easy=24.0,
    row_spacing_hard=18.0,
    start_clear=30.0,
    end_clear=20.0,
    double_obstacle_chance=0.8,  # times difficulty
    hazard_chance_base=0.15,
    hazard_chance_diff=0.30,
    hotdog_chance=0.06,
    star_line_chance=0.6,

    tape_erase_gap=3.0,
    sax_peach_factor=0.8,
)


@dataclass
class Hero:
    name: str
    speed_mul: float = 1.0
    half_width: float = 0.4
    lane_change_mul: float = 1.0
    jump_mul: float = 1.0
    can_slide: bool = True
    magnet: float = 0.0
    points_mul: float = 1.0
    hazard_mul: float = 1.0
    accel_time: float = 1.6
    special: str = "none"     # none | charge | sax | tape
    special_dur: float = 0.0
    special_cd: float = 0.0


# Mirror of HeroDef.Roster
HEROES = [
    Hero("Georgie Washingmachine", magnet=1.2),
    Hero("Abe Lincolnshire", speed_mul=1.02, half_width=0.35, jump_mul=1.4, accel_time=2.0),
    Hero("Teddy Roosterfelt", half_width=0.45, can_slide=False,
         special="charge", special_dur=2.5, special_cd=12.0),
    Hero("Donny Trumpf", speed_mul=1.0, half_width=0.45, lane_change_mul=1.15, points_mul=2.0),
    Hero("Bill Clintonne", special="sax", special_dur=2.0, special_cd=30.0),
    Hero("Dick Nixxon", points_mul=1.1, hazard_mul=1.5,
         special="tape", special_dur=0.1, special_cd=25.0),
]


def lerp(a, b, t):
    return a + (b - a) * t


def pick_kind(rng, d):
    w_block = 0.5 - 0.2 * d
    w_low = 0.3 + 0.1 * d
    r = rng.random()
    if r < w_block:
        return "block"
    if r < w_block + w_low:
        return "low"
    return "high"


def gen_stage(rng, d):
    """Same distribution as TrackGenerator.cs (not the same RNG stream)."""
    c = CFG
    rows = []
    z = c["start_clear"]
    end = c["stage_length"] - c["end_clear"]
    spacing = lerp(c["row_spacing_easy"], c["row_spacing_hard"], d)
    while True:
        z += spacing * rng.uniform(0.85, 1.15)
        if z > end:
            break
        n_obs = 2 if rng.random() < d * c["double_obstacle_chance"] else 1
        kinds = [pick_kind(rng, d) for _ in range(n_obs)]
        hazard = n_obs == 1 and rng.random() < c["hazard_chance_base"] + c["hazard_chance_diff"] * d
        hotdog = rng.random() < c["hotdog_chance"]
        rows.append((z, kinds, hazard, hotdog))
    return rows


def hit_prob(kind, hero, skill, crowded):
    miss = 1.0 - skill
    if kind == "block":
        m = (hero.half_width / 0.4) * hero.lane_change_mul ** 0.5
    elif kind == "low":
        m = 1.0 / hero.jump_mul ** 0.5
    else:
        m = 1.0 if hero.can_slide else 1.6 * (hero.half_width / 0.4)
    if crowded:
        m *= 1.3
    return min(1.0, miss * m)


class RunState:
    def __init__(self):
        self.t = 0.0
        self.gap = CFG["peach_start_gap"]
        self.lives = CFG["start_lives"]
        self.sf = 1.0
        self.boost = 0.0
        self.invuln = 0.0
        self.sp_timer = 0.0
        self.sp_cd = 0.0
        self.catches = 0
        self.mercy = 0


def run_stage(st, idx, rng, hero, skill, dt):
    """Returns True if the stage is cleared, False on game over."""
    c = CFG
    d = (idx / (c["stage_count"] - 1)) ** c["difficulty_exponent"]
    rows = gen_stage(rng, d)
    nominal = c["base_run_speed"] + c["speed_per_stage"] * idx
    peach_base = nominal * (c["peach_base_factor"] + c["peach_difficulty_factor"] * d
                            - c["mercy_per_retry"] * st.mercy)
    z = 0.0
    ri = 0
    while z < c["stage_length"]:
        st.t += dt

        # bot special use (mirrors a sensible player)
        if hero.special != "none" and st.sp_cd <= 0 and st.sp_timer <= 0:
            use = (hero.special == "charge"
                   or (hero.special == "sax" and st.gap < 10.0)
                   or (hero.special == "tape" and st.gap < 8.0))
            if use:
                st.sp_timer = hero.special_dur
                st.sp_cd = hero.special_cd
                if hero.special == "tape":
                    st.gap = min(c["peach_max_gap"], st.gap + c["tape_erase_gap"])

        # timers (same order as RunnerSim.UpdateTimers)
        st.boost = max(0.0, st.boost - dt)
        st.invuln = max(0.0, st.invuln - dt)
        st.sp_timer = max(0.0, st.sp_timer - dt)
        st.sp_cd = max(0.0, st.sp_cd - dt)
        st.sf = min(1.0, st.sf + dt / hero.accel_time)

        speed = nominal * hero.speed_mul * st.sf * (c["boost_mul"] if st.boost > 0 else 1.0)
        z += speed * dt

        while ri < len(rows) and rows[ri][0] <= z:
            _, kinds, hazard, hotdog = rows[ri]
            ri += 1
            charging = hero.special == "charge" and st.sp_timer > 0
            for k in kinds:
                if charging or st.invuln > 0:
                    continue
                if rng.random() < hit_prob(k, hero, skill, len(kinds) == 2):
                    st.sf = c["stumble_speed_factor"]
            if hazard and rng.random() < (1.0 - skill) * 0.8:
                st.gap -= c["hazard_gap_penalty"] * hero.hazard_mul
            if hotdog and rng.random() < min(1.0, skill + 0.1 * hero.magnet):
                st.boost = c["boost_time"]

        if st.t < c["peach_wake_delay"]:
            peach_speed = 0.0
        else:
            sax = hero.special == "sax" and st.sp_timer > 0
            peach_speed = peach_base * (c["sax_peach_factor"] if sax else 1.0)
        st.gap = min(c["peach_max_gap"], st.gap + (speed - peach_speed) * dt)

        if st.gap <= 0.0 and st.invuln <= 0.0:
            st.lives -= 1
            st.catches += 1
            if st.lives <= 0:
                return False
            st.gap = c["caught_reset_gap"]
            st.invuln = c["catch_invuln"]
            st.sf = 1.0
        st.gap = max(0.0, st.gap)
    return True


def run_game(args):
    hero_idx, skill, seed, dt = args
    hero = HEROES[hero_idx]
    rng = random.Random(seed)
    c = CFG
    st = RunState()
    game_overs = 0
    first_go = None
    idx = 0
    mercy = 0
    while idx < c["stage_count"]:
        if idx % c["chapter_size"] == 0 and idx > 0:
            st.lives = min(c["max_lives"], st.lives + 1)
        st.mercy = mercy
        if run_stage(st, idx, rng, hero, skill, dt):
            idx += 1
            if idx % c["chapter_size"] == 0:
                mercy = 0  # chapter cleared, mercy resets
            continue
        game_overs += 1
        if first_go is None:
            first_go = idx
        if game_overs > 200:
            break
        # checkpoint: restart chapter with full lives
        idx = (idx // c["chapter_size"]) * c["chapter_size"]
        mercy = min(c["mercy_max_steps"], mercy + 1)
        st = RunState()
        st.t = c["peach_wake_delay"]  # no intro on restart
    return game_overs, (first_go if first_go is not None else c["stage_count"])


def report(hero_idx, skill, runs, dt, seed, pool):
    jobs = [(hero_idx, skill, seed * 100003 + i, dt) for i in range(runs)]
    res = pool.map(run_game, jobs)
    gos = [r[0] for r in res]
    firsts = [r[1] for r in res]
    clean = 100.0 * sum(1 for g in gos if g == 0) / runs
    return clean, statistics.median(firsts), statistics.mean(gos)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--runs", type=int, default=60)
    ap.add_argument("--dt", type=float, default=1 / 30)
    ap.add_argument("--seed", type=int, default=1)
    a = ap.parse_args()

    skills = [("casual", 0.80), ("regular", 0.90), ("expert", 0.96)]
    targets = {
        "casual": "first GO ~8-20, <=15 GOs to win",
        "regular": "first GO ~25-40, 2-5 GOs to win",
        "expert": "clean clear 60-100%",
    }
    print(f"runs={a.runs} dt={a.dt:.4f}  stages={CFG['stage_count']}  chapter={CFG['chapter_size']}")
    with Pool() as pool:
        print("\nGeorgie (baseline) across skill bands")
        print(f"{'band':8} {'skill':>5} {'clean%':>7} {'firstGO':>8} {'GOs':>6}   target")
        for band, s in skills:
            clean, first, gos = report(0, s, a.runs, a.dt, a.seed, pool)
            print(f"{band:8} {s:5.2f} {clean:7.1f} {first:8.1f} {gos:6.2f}   {targets[band]}")

        print("\nAll heroes at 'regular' (0.90) - want them within ~1 GO of Georgie")
        print(f"{'hero':24} {'clean%':>7} {'firstGO':>8} {'GOs':>6}")
        for i, h in enumerate(HEROES):
            clean, first, gos = report(i, 0.90, a.runs, a.dt, a.seed, pool)
            print(f"{h.name:24} {clean:7.1f} {first:8.1f} {gos:6.2f}")


if __name__ == "__main__":
    main()
