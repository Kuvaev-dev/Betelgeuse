"""Soft seamless cool-gray grain ONLY — no crater stamps (mesh owns bowls)."""
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Resources" / "Moon"
OUT.mkdir(parents=True, exist_ok=True)
N = 1536


def value_noise(shape: int, res: int, rng: np.random.Generator) -> np.ndarray:
    grid = rng.standard_normal((res + 1, res + 1)).astype(np.float32)
    lo, hi = float(grid.min()), float(grid.max())
    img = Image.fromarray(((grid - lo) / (hi - lo + 1e-8) * 255).astype(np.uint8), "L")
    img = img.resize((shape, shape), Image.Resampling.BICUBIC)
    return np.asarray(img, dtype=np.float32) / 255.0 * 2.0 - 1.0


def fbm(shape: int, octaves: int, base_res: int, seed: int) -> np.ndarray:
    rng = np.random.default_rng(seed)
    total = np.zeros((shape, shape), dtype=np.float32)
    amp = 1.0
    norm = 0.0
    res = base_res
    for _ in range(octaves):
        total += amp * value_noise(shape, max(2, res), rng)
        norm += amp
        amp *= 0.5
        res = max(2, int(res * 2))
    return total / max(1e-6, norm)


def seamless(a: np.ndarray, b: int = 120) -> np.ndarray:
    out = a.copy()
    h, w = out.shape
    b = min(b, w // 5, h // 5)
    t = np.linspace(0, 1, b, dtype=np.float32)
    t = t * t * (3 - 2 * t)
    left, right = out[:, :b].copy(), out[:, -b:].copy()
    top, bot = out[:b, :].copy(), out[-b:, :].copy()
    for i in range(b):
        out[:, i] = right[:, i] * (1 - t[i]) + left[:, i] * t[i]
        out[:, -b + i] = left[:, i] * (1 - t[i]) + right[:, i] * t[i]
    for i in range(b):
        out[i, :] = bot[i, :] * (1 - t[i]) + top[i, :] * t[i]
        out[-b + i, :] = top[i, :] * (1 - t[i]) + bot[i, :] * t[i]
    return out


def normal_from_h(h: np.ndarray, strength: float = 1.6) -> np.ndarray:
    dx = np.zeros_like(h)
    dy = np.zeros_like(h)
    dx[:, 1:-1] = (h[:, 2:] - h[:, :-2]) * 0.5
    dy[1:-1, :] = (h[2:, :] - h[:-2, :]) * 0.5
    dx[:, 0] = (h[:, 1] - h[:, -1]) * 0.5
    dx[:, -1] = (h[:, 0] - h[:, -2]) * 0.5
    dy[0, :] = (h[1, :] - h[-1, :]) * 0.5
    dy[-1, :] = (h[0, :] - h[-2, :]) * 0.5
    nx, ny, nz = -dx * strength, -dy * strength, np.ones_like(h)
    inv = 1.0 / np.sqrt(nx * nx + ny * ny + nz * nz)
    return np.clip(
        np.stack([(nx * inv) * 0.5 + 0.5, (ny * inv) * 0.5 + 0.5, (nz * inv) * 0.5 + 0.5], -1),
        0,
        1,
    )


def u8(a: np.ndarray) -> np.ndarray:
    return np.clip(a * 255 + 0.5, 0, 255).astype(np.uint8)


def main() -> None:
    print(f"soft gray grain {N}…")
    # Very soft height — micro dust only
    h = (
        fbm(N, 4, 2, 11) * 0.55
        + fbm(N, 3, 8, 29) * 0.30
        + fbm(N, 2, 24, 47) * 0.15
    ).astype(np.float32)
    h = seamless(h, 140)
    himg = Image.fromarray(
        u8((h - h.min()) / ((h.max() - h.min()) + 1e-8)), "L"
    ).filter(ImageFilter.GaussianBlur(1.2))
    h = np.asarray(himg, dtype=np.float32) / 255.0

    a = (
        0.50
        + fbm(N, 4, 2, 101) * 0.035
        + fbm(N, 3, 10, 131) * 0.018
        + fbm(N, 2, 40, 151) * 0.008
    ).astype(np.float32)
    # Soft mare
    mare = fbm(N, 3, 2, 201)
    mare = (mare - mare.min()) / ((mare.max() - mare.min()) + 1e-8)
    mare = np.clip((mare - 0.55) / 0.35, 0, 1)
    mare = mare * mare * (3 - 2 * mare)
    a -= mare.astype(np.float32) * 0.04
    a = seamless(a, 140)
    a = (a - float(a.mean())) * 0.65 + 0.50
    a = np.clip(a, 0.42, 0.58)

    rgb = np.clip(np.stack([a * 0.98, a * 1.00, a * 1.03], -1), 0, 1)
    nrm = normal_from_h(h, 1.4)

    Image.fromarray(u8(rgb), "RGB").save(OUT / "LunarSurfaceAlbedo.png", optimize=True)
    Image.fromarray(u8(nrm), "RGB").save(OUT / "LunarSurfaceNormal.png", optimize=True)
    Image.fromarray(u8(np.stack([h, h, h], -1)), "RGB").save(
        OUT / "LunarSurfaceHeight.png", optimize=True
    )

    gpath = OUT / "LrocColor2k.png"
    if gpath.exists():
        g = np.asarray(Image.open(gpath).convert("L"), dtype=np.float32) / 255.0
        g = np.clip((g - g.mean()) * 0.65 + 0.50, 0.28, 0.72)
        gr = np.stack([g * 0.98, g, g * 1.03], -1)
        Image.fromarray(u8(gr), "RGB").save(OUT / "LrocColor2k.png", optimize=True)

    (OUT / "SOURCE.txt").write_text(
        "Soft cool-gray regolith grain (seamless). Craters come from mesh only.\n",
        encoding="utf-8",
    )
    for p in sorted(OUT.glob("LunarSurface*")):
        print(p.name, p.stat().st_size)
    print("OK")


if __name__ == "__main__":
    main()
