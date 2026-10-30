"""
Generate Bixby's Night 1 voice lines with ElevenLabs, named the way the game expects.

Reads a CSV (filename,text) and writes one MP3 per row into the game's VO folder.
Lines that already have a file are skipped, so re-running only fills gaps; use --force
to regenerate everything (or delete the files you want redone).

Usage (from the project root):
    set ELEVENLABS_API_KEY=...            (PowerShell: $env:ELEVENLABS_API_KEY="...")
    python Tools/VO/generate_vo.py --voice <voice_id>
    python Tools/VO/generate_vo.py --voice <voice_id> --only "01,05,30a"
      (keep the quotes in PowerShell, or it turns 01,05 into the numbers 1 and 5)
    Bixby: --voice hN9FLDCGtLfNLxsUPxa7 --model eleven_v3 --stability 0.5
      (v3 only accepts stability 0, 0.5 or 1)

Then in Unity: Campfire Friends > Camp > Update Bixby Voice Lines.

No packages needed beyond the Python standard library.
"""
import argparse
import csv
import json
import os
import sys
import time
import urllib.error
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.normpath(os.path.join(HERE, "..", ".."))
DEFAULT_CSV = os.path.join(HERE, "night01_bixby.csv")
DEFAULT_OUT = os.path.join(PROJECT, "Assets", "Audio", "VO", "Night01")


def generate(api_key, voice, model, text, settings):
    url = f"https://api.elevenlabs.io/v1/text-to-speech/{voice}?output_format=mp3_44100_128"
    body = json.dumps({"text": text, "model_id": model, "voice_settings": settings}).encode("utf-8")
    request = urllib.request.Request(url, data=body, method="POST", headers={
        "xi-api-key": api_key,
        "Content-Type": "application/json",
        "Accept": "audio/mpeg",
    })
    with urllib.request.urlopen(request, timeout=120) as response:
        return response.read()


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--voice", required=True, help="ElevenLabs voice id for Bixby")
    parser.add_argument("--csv", default=DEFAULT_CSV)
    parser.add_argument("--out", default=DEFAULT_OUT)
    parser.add_argument("--model", default="eleven_multilingual_v2")
    parser.add_argument("--only", default="", help="comma-separated labels to do, e.g. 01,05,30a")
    parser.add_argument("--force", action="store_true", help="regenerate files that already exist")
    # A fast-talking, slightly unstable salesman: less stability, some style.
    parser.add_argument("--stability", type=float, default=0.4)
    parser.add_argument("--similarity", type=float, default=0.8)
    parser.add_argument("--style", type=float, default=0.35)
    args = parser.parse_args()

    api_key = os.environ.get("ELEVENLABS_API_KEY")
    if not api_key:
        sys.exit("Set ELEVENLABS_API_KEY first (your key never goes in this file).")

    os.makedirs(args.out, exist_ok=True)
    only = {x.strip().lower() for x in args.only.split(",") if x.strip()}
    settings = {"stability": args.stability, "similarity_boost": args.similarity, "style": args.style}

    with open(args.csv, newline="", encoding="utf-8") as f:
        rows = list(csv.DictReader(f))

    done = skipped = failed = 0
    for row in rows:
        name, text = row["filename"].strip(), row["text"].strip()
        # --only takes labels (01, 30a, b02) or whole file names.
        if only and name.lower() not in only and name.lower().rsplit("_", 1)[-1] not in only:
            continue
        path = os.path.join(args.out, name + ".mp3")
        if os.path.exists(path) and not args.force:
            skipped += 1
            continue
        try:
            audio = generate(api_key, args.voice, args.model, text, settings)
        except urllib.error.HTTPError as e:
            print(f"  {name}: failed ({e.code} {e.read().decode('utf-8', 'replace')[:200]})")
            failed += 1
            continue
        with open(path, "wb") as out:
            out.write(audio)
        print(f"  {name}: {len(audio) // 1024} KB")
        done += 1
        time.sleep(0.5)  # be gentle with the rate limit

    print(f"Generated {done}, skipped {skipped} existing, {failed} failed -> {args.out}")


if __name__ == "__main__":
    main()
