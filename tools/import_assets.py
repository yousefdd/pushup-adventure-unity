#!/usr/bin/env python3
"""Copies the game's own art, fonts and sounds from the Flutter project into
Assets/Resources, as PNG sprites with import settings Unity reads as-is.

    python3 tools/import_assets.py /path/to/flutter/project

Paintings become PNG (Unity does not import WebP). Each gets a .meta so it
imports as a sprite with no mipmaps; scenery repeats so it can scroll.
"""
import hashlib
import shutil
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
RES = ROOT / "Assets" / "Resources"

FONTS = [
    "Tajawal-Bold.ttf", "Tajawal-ExtraBold.ttf", "Lalezar-Regular.ttf", "LilitaOne-Regular.ttf",
    "nunito_extrabold.ttf", "nunito_black.ttf",
    "OFL-tajawal.txt", "OFL-lalezar.txt", "OFL-LilitaOne.txt", "OFL-Nunito.txt",
]

SPRITE_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  serializedVersion: 12
  mipmaps:
    enableMipMap: 0
  isReadable: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: {wrap}
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  alphaUsage: 1
  alphaIsTransparency: 1
  textureType: 8
  textureShape: 1
  maxTextureSize: 2048
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 1024
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
  spritePackingTag:
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def guid(path: str) -> str:
    """Stable per path, so re-running the import changes nothing in git."""
    return hashlib.md5(("pushup-adventure:" + path).encode()).hexdigest()


def main(src: Path) -> None:
    art = RES / "Art"
    art.mkdir(parents=True, exist_ok=True)
    pictures = sorted((src / "assets" / "art").glob("*.webp")) + [src / "assets" / "rpg" / "flame_hero.png"]
    for p in pictures:
        name = p.stem + ".png"
        out = art / name
        Image.open(p).save(out, optimize=True)
        rel = "Assets/Resources/Art/" + name
        wrap = 0 if p.stem.startswith("bg_") else 1  # 0 = repeat
        (art / (name + ".meta")).write_text(SPRITE_META.format(guid=guid(rel), wrap=wrap))
    print(f"{len(pictures)} pictures")

    fonts = RES / "Fonts"
    fonts.mkdir(parents=True, exist_ok=True)
    for f in FONTS:
        shutil.copy(src / "assets" / "fonts" / f, fonts / f)
    print(f"{len(FONTS)} font files")

    sfx = RES / "Sfx"
    sfx.mkdir(parents=True, exist_ok=True)
    wavs = sorted((src / "assets" / "sfx").glob("*.wav"))
    for w in wavs:
        shutil.copy(w, sfx / w.name)
    print(f"{len(wavs)} sounds")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    main(Path(sys.argv[1]))
