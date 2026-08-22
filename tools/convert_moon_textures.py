"""Convert NASA CGI Moon Kit maps into Unity Resources assets + local surface tiles."""
from __future__ import annotations

import os
from pathlib import Path

from PIL import Image, ImageEnhance, ImageFilter, ImageOps

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Assets" / "Textures" / "Moon"
OUT = ROOT / "Assets" / "Resources" / "Moon"
OUT.mkdir(parents=True, exist_ok=True)


def save_rgb(img: Image.Image, path: Path, size=None) -> None:
    if size is not None:
        img = img.resize(size, Image.Resampling.LANCZOS)
    img = img.convert("RGB")
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.suffix.lower() in {".jpg", ".jpeg"}:
        img.save(path, quality=92, optimize=True)
    else:
        img.save(path, optimize=True)
    print(f"wrote {path} {img.size} ({path.stat().st_size} bytes)")


def crop_interesting_region(color: Image.Image, height: Image.Image | None) -> Image.Image:
    """Pick a high-contrast equatorial patch good for a landing disk."""
    w, h = color.size
    # Near-side equatorial highlands / mare boundary — around lon ~ -20..20, lat ~ -10..20
    # equirect: x = (lon+180)/360 * w, y = (90-lat)/180 * h
    # Use Mare Imbrium / highlands edge region for crater variety.
    cx = int(w * (180 - 16) / 360)  # lon ~ -16°
    cy = int(h * (90 - 8) / 180)    # lat ~ 8°
    # Cover ~40°x40° so tiling still has big craters at disk scale
    half_w = int(w * 40 / 360 / 2)
    half_h = int(h * 40 / 180 / 2)
    box = (
        max(0, cx - half_w),
        max(0, cy - half_h),
        min(w, cx + half_w),
        min(h, cy + half_h),
    )
    patch = color.crop(box)
    # Boost local contrast a bit — lunar maps are often flat mid-gray
    patch = ImageOps.autocontrast(patch, cutoff=1)
    patch = ImageEnhance.Contrast(patch).enhance(1.15)
    patch = ImageEnhance.Color(patch).enhance(0.85)
    return patch


def make_normal_from_height(height_l: Image.Image, strength: float = 4.0) -> Image.Image:
    """Sobel-ish normal map from grayscale height."""
    h = height_l.convert("L")
    # slight blur reduces noise spikes
    h = h.filter(ImageFilter.GaussianBlur(radius=0.8))
    px = h.load()
    w, ht = h.size
    out = Image.new("RGB", (w, ht))
    op = out.load()
    for y in range(ht):
        y0 = max(0, y - 1)
        y1 = min(ht - 1, y + 1)
        for x in range(w):
            x0 = max(0, x - 1)
            x1 = min(w - 1, x + 1)
            dx = (px[x1, y] - px[x0, y]) / 255.0
            dy = (px[x, y1] - px[x, y0]) / 255.0
            nx = -dx * strength
            ny = -dy * strength
            nz = 1.0
            inv = (nx * nx + ny * ny + nz * nz) ** 0.5
            if inv > 1e-8:
                nx /= inv
                ny /= inv
                nz /= inv
            op[x, y] = (
                int((nx * 0.5 + 0.5) * 255),
                int((ny * 0.5 + 0.5) * 255),
                int((nz * 0.5 + 0.5) * 255),
            )
    return out


def make_seamless(img: Image.Image, blend: int = 64) -> Image.Image:
    """Cheap edge blend so world-UV tiling is less obvious."""
    img = img.convert("RGB")
    w, h = img.size
    b = min(blend, w // 8, h // 8)
    if b < 4:
        return img
    base = img.copy()
    # horizontal wrap blend
    left = img.crop((0, 0, b, h))
    right = img.crop((w - b, 0, w, h))
    for i in range(b):
        t = i / (b - 1)
        # ease
        t = t * t * (3 - 2 * t)
        col_l = right.crop((i, 0, i + 1, h))
        col_r = left.crop((i, 0, i + 1, h))
        mixed = Image.blend(col_l, col_r, t)
        base.paste(mixed, (i, 0))
        mixed2 = Image.blend(col_r, col_l, t)
        base.paste(mixed2, (w - b + i, 0))
    # vertical wrap blend
    top = base.crop((0, 0, w, b))
    bot = base.crop((0, h - b, w, h))
    for i in range(b):
        t = i / (b - 1)
        t = t * t * (3 - 2 * t)
        row_t = bot.crop((0, i, w, i + 1))
        row_b = top.crop((0, i, w, i + 1))
        mixed = Image.blend(row_t, row_b, t)
        base.paste(mixed, (0, i))
        mixed2 = Image.blend(row_b, row_t, t)
        base.paste(mixed2, (0, h - b + i))
    return base


def main() -> None:
    color_path = SRC / "lroc_color_poles_4k.tif"
    if not color_path.exists():
        color_path = SRC / "lroc_color_2k.jpg"
    color = Image.open(color_path).convert("RGB")
    print("source color", color.size)

    # Full globe maps (for optional sky moon sphere later)
    save_rgb(color, OUT / "LrocColor4k.png", size=(2048, 1024) if color.size[0] > 2048 else None)
    if (SRC / "lroc_color_2k.jpg").exists():
        save_rgb(Image.open(SRC / "lroc_color_2k.jpg"), OUT / "LrocColor2k.png")

    height_src = None
    if (SRC / "ldem_3_8bit.jpg").exists():
        height_src = Image.open(SRC / "ldem_3_8bit.jpg").convert("L")
        save_rgb(height_src.convert("RGB"), OUT / "LdemHeight.jpg")

    # Local landing-disk albedo: interesting patch, upscaled, slight seamless
    patch = crop_interesting_region(color, height_src)
    # Upscale for disk detail
    target = 2048
    patch = patch.resize((target, target), Image.Resampling.LANCZOS)
    # Add fine grain so it doesn't look plastic when close
    noise = Image.effect_noise((target, target), 18).convert("L")
    noise = ImageOps.autocontrast(noise)
    grain = Image.merge("RGB", (noise, noise, noise))
    patch = Image.blend(patch, grain, 0.06)
    patch = make_seamless(patch, blend=96)
    patch = ImageEnhance.Brightness(patch).enhance(1.05)
    save_rgb(patch, OUT / "LunarSurfaceAlbedo.png")

    # Height patch from DEM if available, else from albedo luminance
    if height_src is not None:
        # same geographic crop as color
        w, h = color.size
        cx = int(w * (180 - 16) / 360)
        cy = int(h * (90 - 8) / 180)
        half_w = int(w * 40 / 360 / 2)
        half_h = int(h * 40 / 180 / 2)
        # map crop to height image coords
        hw, hh = height_src.size
        sx = hw / w
        sy = hh / h
        hbox = (
            max(0, int((cx - half_w) * sx)),
            max(0, int((cy - half_h) * sy)),
            min(hw, int((cx + half_w) * sx)),
            min(hh, int((cy + half_h) * sy)),
        )
        hpatch = height_src.crop(hbox).resize((target, target), Image.Resampling.BICUBIC)
        hpatch = ImageOps.autocontrast(hpatch, cutoff=0.5)
    else:
        hpatch = patch.convert("L")
        hpatch = ImageOps.autocontrast(hpatch)

    hpatch = make_seamless(hpatch.convert("RGB"), blend=96).convert("L")
    save_rgb(hpatch.convert("RGB"), OUT / "LunarSurfaceHeight.png")

    normal = make_normal_from_height(hpatch, strength=5.5)
    normal = make_seamless(normal, blend=64)
    save_rgb(normal, OUT / "LunarSurfaceNormal.png")

    # Small attribution note
    (OUT / "SOURCE.txt").write_text(
        "NASA Scientific Visualization Studio — CGI Moon Kit (ID 4720)\n"
        "https://svs.gsfc.nasa.gov/4720/\n"
        "Public domain / NASA media guidelines.\n"
        "Processed into local landing-disk albedo/normal tiles for Betelgeuse.\n",
        encoding="utf-8",
    )
    print("OK")


if __name__ == "__main__":
    main()
