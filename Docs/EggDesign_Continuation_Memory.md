# Egg Design Continuation Memory

Last updated: 2026-05-26

## Branch and Remote

- Current branch: `egg-design`
- Remote branch: `origin/egg-design`
- Latest pushed egg-design commit from this workstream: `4157e09 Add Tidecrest egg design and themed FX`
- Earlier pushed Prismhorn commit: `768782e Add Prismhorn egg design and themed FX`
- Do not modify `origin/main` for egg design work unless the user explicitly asks.

## Existing Dirty Worktree Items to Exclude

These changes were present outside the egg design commits and should not be staged unless the user explicitly asks:

- `Assets/TextMesh Pro/Fonts/BlackHanSans-Regular SDF.asset`
- `Docs/Egg_Design_Handoff_Prompt.md`
- `Docs/gamedesign.md`
- `Docs/gameplan.md`

## Completed Egg Designs

### Embercore

- Existing reference design and baseline FX format.
- Model assets: `Assets/_Project/Art/Models/EggSkins_TextureProjected/Embercore/`
- FX prefab: `Assets/_Project/Prefabs/Effects/EmbercoreImpactFX.prefab`
- Scene preview: `Assets/_Project/Scenes/EggDesign.unity`

### Prismhorn

- Crystal/faceted egg with full-surface non-overlapping hex panels.
- Sparse irregular purple side crystals across the egg, not dense or evenly mirrored.
- Model assets:
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Prismhorn/EggSkin_Prismhorn_Seamless.blend`
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Prismhorn/EggSkin_Prismhorn_Seamless.glb`
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Prismhorn/EggSkin_Prismhorn_Seamless_Preview.png`
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Prismhorn/EggSkin_Prismhorn_UnityPreview.png`
- FX assets:
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Prismhorn/EggSkin_Prismhorn_ImpactFX.blend`
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Prismhorn/EggSkin_Prismhorn_ImpactFX.glb`
  - `Assets/_Project/Prefabs/Effects/PrismhornImpactFX.prefab`
- Runtime theme:
  - `EggSkinFxTheme.Prismhorn`
  - `razorbill_egg.prefab` is tagged as Prismhorn.
- Important FX note:
  - Prismhorn needs a `Quaternion.Euler(90f, 0f, 0f)` theme rotation offset in `ThemedImpactFxController` so the FX composition faces the collision normal like Embercore.

### Tidecrest

- Ocean-themed egg based on `Tidecrest.png`.
- Current preferred construction: one egg body mesh with a UV texture and one painted shell material.
- Do not use protruding exterior reef, wave-ring, coral, or tube-shell meshes for the Tidecrest body skin.
- Visual ingredients should remain: aqua/deep-blue shell, creamy foam wave, teal swell bands, painted wave seams, coral motifs, sand tube shell motifs.
- The ingredients should read as painted/enamel/inlaid surface art under a glossy shell clearcoat, not as separate geometry attached outside the egg silhouette.
- Art quality note:
  - The UV texture workflow is approved, but the texture must match the other eggs' quality level.
  - Avoid flat/mobile-icon color. Use deeper blue range, richer aqua variation, tighter value contrast, painted highlights, small panel/seam details, and glossy material response so Tidecrest does not look dull beside Embercore and Prismhorn.
- Model assets:
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Tidecrest/EggSkin_Tidecrest_Seamless.blend`
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Tidecrest/EggSkin_Tidecrest_Seamless.glb`
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Tidecrest/EggSkin_Tidecrest_SurfaceTexture.png`
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Tidecrest/EggSkin_Tidecrest_Seamless_Preview.png`
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Tidecrest/EggSkin_Tidecrest_UnityPreview.png`
- FX assets:
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Tidecrest/EggSkin_Tidecrest_ImpactFX.blend`
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Tidecrest/EggSkin_Tidecrest_ImpactFX.glb`
  - `Assets/_Project/Prefabs/Effects/TidecrestImpactFX.prefab`
- Runtime theme:
  - `EggSkinFxTheme.Tidecrest`
  - `YoshiEgg.prefab` is tagged as Tidecrest.
- FX note:
  - Tidecrest FX was built to face correctly without the Prismhorn rotation offset.
  - Removed `BasaltShard_Tidecrest_CoralShellChip` pieces from the Tidecrest impact FX because they read as stray objects stuck to the egg surface. Keep future Tidecrest FX fragments away from the body silhouette unless they are clearly moving impact particles.
- Preview alignment note:
  - In `Assets/_Project/Scenes/EggDesign.unity`, keep the Tidecrest looping impact FX preview visually centered on the Tidecrest egg preview.
  - If the egg body/pivot changes, re-check `Tidecrest_Egg_Visual` and `Tidecrest_ImpactFX_LoopingPreview` positions so the FX does not appear offset from the egg.

## Runtime FX Selection Structure

Files:

- `Assets/_Project/Scripts/Presentation/EggSkinTheme.cs`
- `Assets/_Project/Scripts/Presentation/ThemedImpactFxController.cs`
- `Assets/_Project/Scripts/Presentation/ThemedImpactFxInstance.cs`
- `Assets/_Project/Scripts/Presentation/EggDesignImpactFxPreview.cs`

Current behavior:

- Each egg prefab can have `EggSkinTheme`.
- `ThemedImpactFxController` reads the egg theme on collision.
- It selects the matching themed impact FX prefab:
  - Embercore -> `embercoreImpactFxPrefab`
  - Prismhorn -> `prismhornImpactFxPrefab`
  - Tidecrest -> `tidecrestImpactFxPrefab`
- If a themed prefab is missing, it falls back to Embercore.

FX child object naming matters because `ThemedImpactFxInstance` animates by substring:

- `ImpactFlash`
- `HeatHaze`
- `OptionalCrackGlow`
- `Spark`
- `BasaltShard`
- `LavaCrack`

Use those substrings in future FX GLB object names so the existing animation logic works without new code.

## Scene and Prefab Wiring

- Main game scene: `Assets/Scenes/01_Game.unity`
  - Contains `ThemedImpactFxController`.
  - Its serialized prefab fields must be wired for all available themes.
- Design preview scene: `Assets/_Project/Scenes/EggDesign.unity`
  - Contains visual previews for Embercore, Prismhorn, Tidecrest.
  - Contains looping impact FX preview controllers for each themed FX.
- Egg prefabs:
  - `Assets/_Project/Prefabs/Eggs/Egg.prefab` -> Embercore
  - `Assets/_Project/Prefabs/Eggs/razorbill_egg.prefab` -> Prismhorn
  - `Assets/_Project/Prefabs/Eggs/YoshiEgg.prefab` -> Tidecrest

## Format for the Next Egg

For a new egg named `NewEggName`, use this same structure:

- Folder: `Assets/_Project/Art/Models/EggSkins_TextureProjected/NewEggName/`
- Body files:
  - `EggSkin_NewEggName_Seamless.blend`
  - `EggSkin_NewEggName_Seamless.glb`
  - `EggSkin_NewEggName_Seamless_Preview.png`
  - `EggSkin_NewEggName_UnityPreview.png`
- FX files:
  - `EggSkin_NewEggName_ImpactFX.blend`
  - `EggSkin_NewEggName_ImpactFX.glb`
  - `EggSkin_NewEggName_ImpactFX_Preview.png`
- Prefab:
  - `Assets/_Project/Prefabs/Effects/NewEggNameImpactFX.prefab`
- Code:
  - Add enum value to `EggSkinFxTheme`.
  - Add serialized prefab field to `ThemedImpactFxController`.
  - Add selection case with Embercore fallback.
  - Add rotation offset only if the GLB local burst axis does not face Unity local +Z.
- Unity:
  - Refresh and compile with `unity-cli editor refresh --compile`.
  - Wire the new FX prefab in `Assets/Scenes/01_Game.unity`.
  - Add preview objects and looping FX preview in `Assets/_Project/Scenes/EggDesign.unity`.
  - Tag the chosen egg prefab with the new `EggSkinTheme`.

## Required Verification

Before committing future egg work:

- Run `unity-cli status` from `C:\Users\admin\Unity\Ovum Rumble` and confirm the project path matches.
- Run `unity-cli editor refresh --compile`.
- Run `unity-cli console --type error,warning --lines 100` and confirm it returns `[]`.
- Capture a Unity preview screenshot into the new egg folder.
- Confirm no `.blend1` files remain in the new egg folder.
- Confirm GLB imports do not include preview-only cameras or lights.
- Stage only the new egg work and related scene/code/prefab changes.
- Exclude unrelated dirty files unless the user explicitly asks to include them.
