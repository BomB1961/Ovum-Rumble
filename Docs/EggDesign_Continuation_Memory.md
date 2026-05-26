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
  - Surface seam issue: do not keep straight vertical panel/seam lines in the Tidecrest albedo. If a vertical line reads on the shell in Unity, remove the line from the texture rather than covering it with exterior geometry.
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
  - Tidecrest FX should stay at the collision point and dissipate there. Avoid long forward-projecting water jets or surface ripple/crack lines; use compact rings, local droplets, and sparse burst fragments instead.
- Preview alignment note:
  - In `Assets/_Project/Scenes/EggDesign.unity`, keep the Tidecrest looping impact FX preview visually centered on the Tidecrest egg preview.
  - If the egg body/pivot changes, re-check `Tidecrest_Egg_Visual` and `Tidecrest_ImpactFX_LoopingPreview` positions so the FX does not appear offset from the egg.
  - EggDesign scene preview standard: Embercore, Prismhorn, and Tidecrest should share a 2.20 unit visual height, 1.55 unit max horizontal footprint, and y=0 bottom contact.
  - Runtime egg prefab standard: playable egg prefabs should keep a 1.00 unit visual height unless gameplay explicitly requires a different scale.
- Seam cleanup note:
  - 2026-05-26: Tidecrest front vertical UV seam was removed in Blender by smoothing the horizontal wrap boundary in `EggSkin_Tidecrest_SurfaceTexture.png`, keeping the body as one mesh with one material, and offsetting the body UVs by +0.5 on U so the wrap boundary no longer sits on the front view.
  - Re-exported `EggSkin_Tidecrest_Seamless.glb` from `EggSkin_Tidecrest_Seamless.blend` and refreshed Unity. `EggSkin_Tidecrest_UnityPreview.png` now shows the front without the vertical line.

### Toxitide

- Toxic slime/cracked shell egg based on the supplied five-view slime egg reference.
- Construction follows the approved Tidecrest body workflow: one egg body mesh, one UV surface texture, and one painted glossy shell material.
- Do not add separate exterior slime, bubble, or cracked plate geometry to the Toxitide body skin. Slime cap, drips, bubbles, purple cracked shell, and toxic green fissures are painted into the UV texture.
- FX theme: toxic green slime burst with purple shell chips and glowing corrosive fissures.
- Model assets:
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Toxitide/EggSkin_Toxitide_Seamless.blend`
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Toxitide/EggSkin_Toxitide_Seamless.glb`
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Toxitide/EggSkin_Toxitide_SurfaceTexture.png`
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Toxitide/EggSkin_Toxitide_Seamless_Preview.png`
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Toxitide/EggSkin_Toxitide_UnityPreview.png`
- FX assets:
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Toxitide/EggSkin_Toxitide_ImpactFX.blend`
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Toxitide/EggSkin_Toxitide_ImpactFX.glb`
  - `Assets/_Project/Art/Models/EggSkins_TextureProjected/Toxitide/EggSkin_Toxitide_ImpactFX_Preview.png`
  - `Assets/_Project/Prefabs/Effects/ToxitideImpactFX.prefab`
- Runtime theme:
  - `EggSkinFxTheme.Toxitide`
  - `ThemedImpactFxController` selects `toxitideImpactFxPrefab`, falling back to Embercore if missing.
  - Toxitide FX was rebuilt so its burst reads upright and parallel with the other egg FX previews without the Prismhorn rotation offset.
- Seam and preview notes:
  - 2026-05-26: Toxitide had a straight vertical line visible on the front shell. The fix was to smooth the visible center band in `EggSkin_Toxitide_SurfaceTexture.png`, soften the texture wrap edges for mip safety, and re-export `EggSkin_Toxitide_Seamless.glb`.
  - Do not solve this kind of body seam by adding cover geometry. Fix the UV placement and/or texture continuity, then verify the Unity preview from the front.
  - 2026-05-26: Toxitide ImpactFX initially exported on the XZ floor plane and appeared as a flat horizontal band in Unity. Reoriented the ImpactFX source geometry into the XY impact plane, kept the prefab root at identity rotation, and re-exported `EggSkin_Toxitide_ImpactFX.glb`.
  - `Toxitide_ImpactFX_LoopingPreview` must override `ThemedImpactFxInstance.destroyOnComplete` to false in `EggDesign.unity`. If this remains true, Play mode destroys the preview root after the first cycle and the FX only appears once.
- Scene preview:
  - `Assets/_Project/Scenes/EggDesign.unity` contains `Toxitide_Egg_Visual` to the right of Tidecrest.
  - `Assets/_Project/Scenes/EggDesign.unity` contains `Toxitide_ImpactFX_LoopingPreview` and `EggDesign_ToxitideImpactFXPreviewController`.
  - Preview verification: 1 renderer, `Toxitide_SurfacePaintedShell` material, bounds approximately 1.49 x 2.20 x 1.49.
  - ImpactFX verification after orientation fix: prefab root rotation is identity, prefab bounds approximately 1.16 x 1.08 x 0.36, EggDesign preview bounds approximately 1.10 x 1.15 x 0.79, and `destroyOnComplete` is false on the looping preview.

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
  - Toxitide -> `toxitideImpactFxPrefab`
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

For a new egg named `NewEggName`, use this same structure. A new egg is not considered complete until both the body skin and its themed impact FX are built, imported, wired, previewed, and documented.

- Body construction standard:
  - Build the egg body as one mesh with one UV surface texture and one painted glossy shell material.
  - Do not add exterior decorative body geometry when the design can be represented in the UV texture.
  - Always place the UV wrap seam away from the front presentation view, preferably on the rear side of the egg.
  - Always smooth the left/right texture wrap edge before export so mipmaps do not reveal a vertical line.
- Completion standard:
  - Every new egg must include the body skin and a matching themed impact FX.
  - Do not stop at the body skin unless the user explicitly asks for a body-only draft.
  - The themed FX must be connected to runtime selection and represented in the EggDesign preview scene.
- Folder: `Assets/_Project/Art/Models/EggSkins_TextureProjected/NewEggName/`
- Body files:
  - `EggSkin_NewEggName_Seamless.blend`
  - `EggSkin_NewEggName_Seamless.glb`
  - `EggSkin_NewEggName_SurfaceTexture.png`
  - `EggSkin_NewEggName_Seamless_Preview.png`
  - `EggSkin_NewEggName_UnityPreview.png`
- FX files:
  - `EggSkin_NewEggName_ImpactFX.blend`
  - `EggSkin_NewEggName_ImpactFX.glb`
  - `EggSkin_NewEggName_ImpactFX_Preview.png`
- FX orientation standard:
  - The FX prefab root should use identity local rotation unless there is a proven import-axis mismatch.
  - Build the GLB so the local burst axis faces Unity local +Z and the preview reads upright/parallel with Embercore, Tidecrest, and Toxitide.
  - Only add a `ThemedImpactFxController` rotation offset when the exported GLB cannot be corrected cleanly; Prismhorn is the known exception.
- Prefab:
  - `Assets/_Project/Prefabs/Effects/NewEggNameImpactFX.prefab`
- Code:
  - Add enum value to `EggSkinFxTheme`.
  - Add serialized prefab field to `ThemedImpactFxController`.
  - Add selection case with Embercore fallback.
  - Add rotation offset only if the GLB local burst axis does not face Unity local +Z after export cleanup.
- Unity:
  - Refresh and compile with `unity-cli editor refresh --compile`.
  - Wire the new FX prefab in `Assets/Scenes/01_Game.unity`.
  - Add preview objects and looping FX preview in `Assets/_Project/Scenes/EggDesign.unity`.
  - Tag the chosen egg prefab with the new `EggSkinTheme`.
  - Confirm the new egg preview includes both `NewEggName_Egg_Visual` and `NewEggName_ImpactFX_LoopingPreview`.
  - In the EggDesign preview scene, set the looping preview FX instance `destroyOnComplete` to false so Play mode can repeat the effect.

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
