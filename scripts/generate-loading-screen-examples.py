#!/usr/bin/env python3
"""Generate 1920x1080 example PNG templates for custom loading screen themes."""

from __future__ import annotations

import argparse
from pathlib import Path

try:
    from PIL import Image, ImageDraw, ImageFont
except ImportError as exc:
    raise SystemExit("Pillow is required: pip install pillow") from exc

WIDTH = 1920
HEIGHT = 1080

TEMPLATES: list[tuple[str, tuple[int, int, int], str, str]] = [
    (
        "example-dungeon-loading.png",
        (24, 18, 58),
        "Dungeon / loading.png",
        "Shown while the dungeon is generating locally.",
    ),
    (
        "example-dungeon-wait.png",
        (42, 24, 92),
        "Dungeon / wait.png",
        "Shown while waiting for all players to finish loading.",
    ),
    (
        "example-dungeon-background.png",
        (32, 36, 48),
        "Dungeon / background.png",
        "Fallback for both loading and wait when phase files are missing.",
    ),
    (
        "example-intramwaiting-background.png",
        (18, 52, 68),
        "InTramWaiting / background.png",
        "Tram waiting room scene overlay.",
    ),
    (
        "example-maintenance-background.png",
        (54, 42, 28),
        "Maintenance / background.png",
        "Maintenance bay scene overlay.",
    ),
    (
        "example-firstenter-background.png",
        (20, 64, 44),
        "FirstEnter / background.png",
        "First session entry overlay.",
    ),
    (
        "example-deathmatch-background.png",
        (72, 22, 28),
        "DeathMatch / background.png",
        "Death match arena scene overlay.",
    ),
]


def load_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    for candidate in (
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "/usr/share/fonts/TTF/DejaVuSans.ttf",
        "/usr/share/fonts/dejavu/DejaVuSans.ttf",
    ):
        path = Path(candidate)
        if path.is_file():
            return ImageFont.truetype(str(path), size=size)
    return ImageFont.load_default()


def wrap_text(draw: ImageDraw.ImageDraw, text: str, font: ImageFont.ImageFont, max_width: int) -> list[str]:
    words = text.split()
    if not words:
        return [""]

    lines: list[str] = []
    current = words[0]
    for word in words[1:]:
        candidate = f"{current} {word}"
        if draw.textlength(candidate, font=font) <= max_width:
            current = candidate
        else:
            lines.append(current)
            current = word
    lines.append(current)
    return lines


def render_template(
    file_name: str,
    color: tuple[int, int, int],
    title: str,
    subtitle: str,
) -> Image.Image:
    image = Image.new("RGB", (WIDTH, HEIGHT), color)
    draw = ImageDraw.Draw(image)

    title_font = load_font(72)
    subtitle_font = load_font(36)
    meta_font = load_font(28)

    title_lines = wrap_text(draw, title, title_font, WIDTH - 240)
    subtitle_lines = wrap_text(draw, subtitle, subtitle_font, WIDTH - 240)
    meta_lines = [
        f"Template: {file_name}",
        "Size: 1920 x 1080 (16:9)",
        "Place edited copies under Assets/CustomLoadingScreen/<Context>/<theme>/",
    ]

    block: list[tuple[str, ImageFont.ImageFont]] = []
    for line in title_lines:
        block.append((line, title_font))
    block.append(("", subtitle_font))
    for line in subtitle_lines:
        block.append((line, subtitle_font))
    block.append(("", meta_font))
    for line in meta_lines:
        block.append((line, meta_font))

    total_height = 0
    line_heights: list[int] = []
    for text, font in block:
        if not text:
            line_heights.append(24)
            total_height += 24
            continue
        bbox = draw.textbbox((0, 0), text, font=font)
        height = bbox[3] - bbox[1]
        line_heights.append(height)
        total_height += height + 12

    y = (HEIGHT - total_height) // 2
    for (text, font), line_height in zip(block, line_heights, strict=True):
        if not text:
            y += line_height
            continue
        width = draw.textlength(text, font=font)
        draw.text(((WIDTH - width) / 2, y), text, fill=(236, 240, 248), font=font)
        y += line_height + 12

    return image


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--output",
        default="assets/examples/custom-loading-screen",
        help="Directory for generated PNG templates",
    )
    args = parser.parse_args()

    output_dir = Path(args.output).resolve()
    output_dir.mkdir(parents=True, exist_ok=True)

    for file_name, color, title, subtitle in TEMPLATES:
        path = output_dir / file_name
        render_template(file_name, color, title, subtitle).save(path, "PNG")
        print(f"Wrote {path}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
