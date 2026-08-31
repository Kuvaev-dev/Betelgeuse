# Environment look (Earth LZ)

Runtime look is built by `EnvironmentBuilder` in Play mode.

- **Sky:** Poly Haven equirectangular `Resources/Textures/sky_cloud.jpg` as URP `Skybox/Panoramic` (see `EnvironmentTextures.MakePanoramicSkybox`).
- **Ground:** URP shader `Betelgeuse/LandingRangeGround` — tiled grass albedo/normal (world XZ, 16 m) × baked macro tint. Horizon skirt extends past the 2 km terrain disk.
- **Pad:** unchanged concrete LZ markings.

Textures: Poly Haven CC0 (`aerial_grass_rock`, sky panorama). No paid Asset Store packages.
