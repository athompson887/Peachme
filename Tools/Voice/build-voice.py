#!/usr/bin/env python3
"""THE VOICES, BUILT ONCE (same pattern as FlightSim2026's Tools/Voice).

Every line in lines.csv is rendered with ElevenLabs, offline, into
Assets/Resources/Voice/<speaker>/<key>_<variant>.mp3 - the path Commentary.cs
loads. Nothing is generated at run time: the game ships plain clips, costs
nothing per play, and works with no network.

A clip whose words and voice have not changed (Tools/Voice/recipes/ records
what each was made from - kept out of Resources, which ships whole) is left alone, so re-running costs nothing. A clip no
line asks for any more is removed.

VOICES ARE DESIGNED, NOT CLONED. Each speaker in voices.json is a voice made
with ElevenLabs Voice Design from a description of a comic archetype - never a
clone or imitation of a real person. Put the voice IDs in voices.json:

    {"commentator": "<voice id>", "georgie": "<voice id>", ...}

Then:

    export ELEVENLABS_API_KEY=...
    python3 Tools/Voice/build-voice.py             # everything that changed
    python3 Tools/Voice/build-voice.py --dry-run   # say what would be built
    python3 Tools/Voice/build-voice.py --only abe  # one speaker
"""
import argparse
import csv
import json
import os
import sys
import urllib.error
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(ROOT, "Assets", "Resources", "Voice")
LINES = os.path.join(HERE, "lines.csv")
VOICES = os.path.join(HERE, "voices.json")
RECIPES = os.path.join(HERE, "recipes")

MODEL = "eleven_multilingual_v2"
FORMAT = "mp3_44100_128"
API = "https://api.elevenlabs.io/v1/text-to-speech/{voice}?output_format=" + FORMAT

# A CHOICE: a bit less stable than default so the comedy reads; tweak by ear.
SETTINGS = {"stability": 0.4, "similarity_boost": 0.75, "style": 0.35, "use_speaker_boost": True}


def load_lines():
    with open(LINES, newline="") as f:
        rows = list(csv.DictReader(f))
    seen = set()
    for r in rows:
        name = (r["speaker"], r["key"], r["variant"])
        if name in seen:
            sys.exit(f"lines.csv: duplicate {name}")
        seen.add(name)
    return rows


def recipe(text, voice):
    return f"{voice}\n{MODEL}\n{json.dumps(SETTINGS, sort_keys=True)}\n{text}\n"


def render(text, voice, key):
    body = json.dumps({"text": text, "model_id": MODEL, "voice_settings": SETTINGS}).encode()
    req = urllib.request.Request(API.format(voice=voice), data=body, method="POST", headers={
        "xi-api-key": key, "Content-Type": "application/json", "Accept": "audio/mpeg"})
    with urllib.request.urlopen(req, timeout=60) as resp:
        return resp.read()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--only", help="one speaker")
    a = ap.parse_args()

    voices = {}
    if os.path.exists(VOICES):
        with open(VOICES) as f:
            voices = json.load(f)
    key = os.environ.get("ELEVENLABS_API_KEY", "")
    if not a.dry_run and not key:
        sys.exit("Set ELEVENLABS_API_KEY (or use --dry-run).")

    rows = load_lines()
    wanted = set()
    built = skipped = missing_voice = failed = 0

    for r in rows:
        speaker, clip = r["speaker"], f'{r["key"]}_{r["variant"]}'
        if a.only and speaker != a.only:
            continue
        folder = os.path.join(OUT, speaker)
        mp3 = os.path.join(folder, clip + ".mp3")
        txt = os.path.join(RECIPES, speaker, clip + ".txt")
        wanted.add(os.path.normpath(mp3))

        voice = voices.get(speaker)
        if not voice:
            missing_voice += 1
            continue
        want = recipe(r["text"], voice)
        if os.path.exists(mp3) and os.path.exists(txt) and open(txt).read() == want:
            skipped += 1
            continue
        if a.dry_run:
            print(f"would build {speaker}/{clip}: {r['text']}")
            built += 1
            continue
        try:
            audio = render(r["text"], voice, key)
        except urllib.error.HTTPError as e:
            print(f"FAILED {speaker}/{clip}: HTTP {e.code} {e.read()[:200]!r}")
            failed += 1
            continue
        os.makedirs(folder, exist_ok=True)
        os.makedirs(os.path.dirname(txt), exist_ok=True)
        with open(mp3, "wb") as f:
            f.write(audio)
        with open(txt, "w") as f:
            f.write(want)
        print(f"built {speaker}/{clip}")
        built += 1

    # Stale clips: only within the speakers we were asked about.
    removed = 0
    if os.path.isdir(OUT) and not a.dry_run:
        for speaker in os.listdir(OUT):
            if a.only and speaker != a.only:
                continue
            folder = os.path.join(OUT, speaker)
            if not os.path.isdir(folder):
                continue
            for name in os.listdir(folder):
                if not name.endswith(".mp3"):
                    continue
                path = os.path.normpath(os.path.join(folder, name))
                if path not in wanted:
                    for p in (path, path + ".meta",
                              os.path.join(RECIPES, speaker, name[:-4] + ".txt")):
                        if os.path.exists(p):
                            os.remove(p)
                    removed += 1

    verb = "would build" if a.dry_run else "built"
    print(f"{verb} {built}, unchanged {skipped}, removed {removed}, failed {failed}, "
          f"no voice id yet {missing_voice} (add them to voices.json)")
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
