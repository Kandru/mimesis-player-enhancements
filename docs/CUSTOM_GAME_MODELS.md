# Custom 3D models in MelonLoader mods

Guide for adding world meshes or visual overrides to MIMESIS via a MelonLoader mod. This covers runtime-loaded geometry shipped beside your mod DLL — not vanilla prefab cloning (see [DEVELOPMENT.md](DEVELOPMENT.md) UI toolkit) or small embedded files ([`EmbeddedAssets`](../src/MimesisPlayerEnhancement/Util/EmbeddedAssets.cs)).

## When custom models make sense

| Use case | Typical approach | Multiplayer note |
|----------|------------------|------------------|
| Hub decoration, local props | Spawn a mesh in the lobby | Usually safe as local-only visuals |
| Dungeon set dressing | Raycast-placed prop at run start | Host-only spawn avoids duplicate props |
| Enemy/player cosmetic reskin | Parent mesh to actor, hide original renderers | Local-only; other clients still see vanilla mesh |
| Gameplay objects (pickups, doors) | Prefer patching vanilla spawns or networked prefabs | Requires FishNet awareness — not covered here |

Most mod visuals are **client-local**. Assume other players will not see your custom mesh unless you integrate with the game's networking layer.

## Asset preparation

1. **Export Wavefront OBJ** from Blender, Maya, etc. Keep scale in real-world meters when possible.
2. **Include MTL + textures** in the same folder. Reference textures with relative paths in the MTL (`map_Kd texture.png`).
3. **Separate collision mesh** (optional): export a simplified `_col.obj` when the visual mesh is too heavy for `MeshCollider`.
4. **Triangulate** faces in the DCC tool; fan-triangulate n-gons if your loader only handles triangles.
5. **Check orientation**: many OBJ exports use a different forward/up axis than Unity. You may need to negate X (or Y) on import.

Suggested mod folder layout (MelonLoader copies this tree to `MIMESIS/Mods/`):

```
MyMod/
  MyMod.dll
  Models/
    prop/
      prop.obj
      prop.mtl
      prop_albedo.png
    prop/
      prop_col.obj          # optional collision-only mesh
```

Resolve paths from the mod directory, not the working directory:

```csharp
string modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? AppDomain.CurrentDomain.BaseDirectory;
string objPath = Path.Combine(modDir, "Models", "prop", "prop.obj");
```

## Runtime mesh pipeline

High-level steps most OBJ-based loaders follow:

1. **Parse** `v`, `vt`, `vn`, `f`, `usemtl`, `mtllib` from the OBJ; resolve `f` indices into unified vertex streams (position + UV + normal per corner).
2. **Build `Mesh`**: `SetVertices`, optional `SetUVs` / `SetNormals`, `SetTriangles` per submesh; use `IndexFormat.UInt32` when vertex count exceeds 65 535.
3. **Materials**: parse MTL `map_Kd` paths, load image bytes into `Texture2D` (e.g. via `ImageConversion.LoadImage`), create one `Material` per submesh.
4. **Assemble GameObject**: child object with `MeshFilter` + `MeshRenderer`; assign `sharedMaterials`.
5. **Normalize height** (recommended): scale/offset the mesh child so bounds height = 1 and the bottom sits at y = 0. At spawn time, set `transform.localScale = Vector3.one * targetHeightMeters`.

Keep the loaded root **inactive** and mark it **`DontDestroyOnLoad`** if you reuse it as a template across scenes.

## Shaders and materials (URP)

MIMESIS uses Unity's Universal Render Pipeline. At runtime, probe shaders in fallback order until one resolves:

1. `Universal Render Pipeline/Lit`
2. `Universal Render Pipeline/Simple Lit`
3. `Universal Render Pipeline/Unlit`
4. `Standard`
5. `Unlit/Texture`

Assign albedo to both `_BaseMap` (URP) and `_MainTex` (built-in) when present. Log which shader was chosen once — pink materials mean none matched.

Set reasonable defaults: white base color, low smoothness/metallic for props. Avoid enabling emission unless intentional.

## Spawning patterns

### World prop

```csharp
// After loading template (inactive root):
Vector3 pos = anchor + forward * distance;
if (Physics.Raycast(pos + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 8f))
    pos = hit.point;
Quaternion rot = Quaternion.LookRotation(-forward);
GameObject instance = Object.Instantiate(template, pos, rot);
instance.transform.localScale = Vector3.one * heightMeters;
instance.SetActive(true);
```

Use the game's default layer mask for ground raycasts (inspect decompiled `Physics.Raycast` call sites under `deps/decompiled/`).

### Actor visual override

1. Find target actor (`FindObjectsOfType` or Harmony postfix on spawn).
2. `Instantiate` template, **parent to actor transform** (`worldPositionStays: false`).
3. Match scale to actor height (measure renderer bounds or use a known constant).
4. **Disable original `Renderer` components** on the actor — do not destroy networked components.
5. Track instance IDs; remove overrides when the actor despawns.

### Collision

- **Visual mesh collider**: `MeshCollider.sharedMesh = mesh` — non-convex only for static props.
- **Simplified mesh**: load a second OBJ with only geometry, add `MeshCollider` to the instance (not the shared template).
- Prefer convex colliders or primitive colliders for dynamic/moving objects.

## Lifecycle and performance

| Phase | Action |
|-------|--------|
| Mod init | Optional: register Harmony patches |
| Scene loaded | Preload templates once; reset spawned-instance lists |
| Per frame / interval | Scan for actors to reskin (throttle scans, e.g. 1 s) |
| Dungeon exit / hub exit | `Destroy` spawned props; clear override dictionaries |
| Mod unload | Destroy `DontDestroyOnLoad` template roots |

Cache templates after first load. Avoid parsing OBJ on every spawn.

## Multiplayer and host gating

- **World spawns**: gate with `HostApplyGate.ShouldApplyHostOnlyFeature()` (see this repo's features) so only the host creates shared geometry.
- **Local reskins**: safe on each client independently; expect asymmetry in co-op.
- **Audio tied to models**: use 2D `AudioSource` for global stingers or spatial audio at the prop position — clarify in logs whether all clients hear it.

Do not assume `Instantiate` of a local mesh replicates over FishNet.

## Alternatives

| Method | Best for | Trade-off |
|--------|----------|-----------|
| Runtime OBJ (this guide) | Quick iteration, no Unity project | No animation rig; manual parser maintenance |
| Unity AssetBundles | Animated/skinned assets, complex materials | Requires matching Unity/URP version; build pipeline |
| Embedded resources | Small data files (JSON, PNG) | Poor fit for large meshes; bloats DLL |
| Clone vanilla prefab parts | UI sprites, fonts | [`ModUiAssets`](../src/MimesisPlayerEnhancement/Ui/ModUiAssets.cs) pattern — not for new world geometry |

## Testing checklist

- [ ] Model loads with zero log warnings; missing MTL/texture paths logged clearly
- [ ] Correct facing and scale in hub **and** dungeon
- [ ] Materials not pink (shader resolved)
- [ ] Collider matches walkable/climbable intent
- [ ] Solo and multiplayer: no duplicate host props; reskins don't break actor logic
- [ ] Scene transition destroys instances; no `DontDestroyOnLoad` leaks after several runs
- [ ] Performance: no per-frame OBJ parsing; scan intervals throttled

## Related docs

- [DEVELOPMENT.md](DEVELOPMENT.md) — feature architecture, Harmony, host gating
- [AGENTS.md](../AGENTS.md) — game inspection via `deps/decompiled/`
