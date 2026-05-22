# Egg Design Handoff Prompt

## Purpose

Use this prompt when starting a new Codex session for Ovum Rumble egg skin design.

The next session should focus on **Blender-first egg asset creation**, followed by minimal Unity integration/verification so the asset can be checked in the project.

Blender is the production tool for modeling and visual quality. Unity is used only to import, place, and verify the result. Do not use Unity as the main design tool unless the user explicitly asks.

## Current Decisions

- Embercore is the current accepted visual baseline for production direction.
- Prismhorn hybrid/Unity-tuned attempt was discarded because the result drifted too far from the supplied reference and looked lower quality than Blender-only work.
- Future egg designs should be created one at a time.
- The user may provide a 5-view reference image for each egg skin.
- Quality target: polished stylized 3D game asset, not a rough procedural prototype.

## Core Instruction For New Session

Copy and use this instruction:

```text
We are working on Ovum Rumble egg skin designs.

Important: Build the egg primarily in Blender.
After the Blender asset is created, add it to Unity only enough to verify that it imports and displays correctly.
Do not use Unity as the main modeling/design tool.
Do not create a new Unity scene per egg.
Do not modify gameplay scripts, project settings, or unrelated Unity assets.

For each egg design:
1. Read the supplied reference image carefully.
2. Build the egg in Blender as a stylized 3D game asset.
3. Match the reference more than inventing a new interpretation.
4. Keep scale, pivot, and silhouette consistent with the existing Embercore egg baseline.
5. Export Blender-side deliverables:
   - .blend source file
   - .glb model
   - preview render image
   - optional impact FX .glb only if requested
6. Add the result to the existing Unity egg design preview workflow for verification.
7. Use the existing `EggDesign.unity` scene for previews. Do not create a separate scene for each egg.
8. Do not add HP, durability, breaking, Rigidbody fragments, Unity particles, VFX Graph, decals, or gameplay logic.
9. Before making files, state a concise plan and wait for my approval.
```

## Blender-First Workflow

For every new egg skin:

1. Inspect the reference image.
2. Identify the main visual language:
   - primary colors
   - secondary colors
   - surface pattern
   - silhouette
   - protrusions or ornaments
   - material feel
   - front/side/back differences
3. Create the Blender model:
   - egg body
   - major surface details
   - protruding details if present
   - clean geometry
   - readable game-scale silhouette
4. Render a Blender preview from the main view.
5. Compare against the reference.
6. If the Blender result is acceptable, export `.glb`.
7. Import/refresh in Unity.
8. Add the model to the existing `EggDesign.unity` preview scene.
9. Capture or verify a Unity preview.
10. Compare Unity display against the Blender preview and reference.

## Quality Rules

- Match the supplied reference first.
- Do not replace the design with a generic procedural pattern.
- Avoid tiny repeated details when the reference uses large readable shapes.
- Avoid over-bright emission that hides shape and color.
- Avoid overly smooth plain bodies when the reference has distinct surface forms.
- Keep ornaments chunky and stylized if the reference is chunky.
- Keep the egg readable from gameplay camera distance.
- Do not add floor plates, text labels, UI labels, or reference-view captions into the asset.

## Consistency Rules

All egg skins should feel like they belong to the same game.

Keep consistent:

- egg height and width range
- pivot at the bottom center or consistent origin suitable for Unity later
- forward-facing orientation
- stylized material language
- outline/readability level
- amount of detail visible from a mid-distance camera

Vary by skin:

- color palette
- surface motif
- protrusion type
- glow style
- theme-specific material feel

## File Naming

Use this pattern for Blender-only outputs:

```text
Assets/_Project/Art/Models/EggSkins_TextureProjected/<SkinName>/
  EggSkin_<SkinName>_Seamless.blend
  EggSkin_<SkinName>_Seamless.glb
  EggSkin_<SkinName>_Seamless_Preview.png
```

If impact FX is explicitly requested:

```text
Assets/_Project/Art/Models/EggSkins_TextureProjected/<SkinName>/
  EggSkin_<SkinName>_ImpactFX.blend
  EggSkin_<SkinName>_ImpactFX.glb
  EggSkin_<SkinName>_ImpactFX_Preview.png
```

Unity verification outputs may be created only when needed:

```text
Assets/_Project/Prefabs/Eggs/<SkinName>EggSkin.prefab
Assets/_Project/Scenes/EggDesign.unity
```

Do not create a new Unity scene per egg.

## Unity Work Scope

Unity work is allowed only for import and verification.

Allowed:

- refresh/import the `.glb`
- create or update a simple egg skin preview prefab if needed
- add the egg to the existing `Assets/_Project/Scenes/EggDesign.unity`
- capture a Unity preview screenshot
- verify Unity Console has no errors

Do not do these unless the user explicitly asks:

Do not do these unless the user explicitly asks:

- create or modify Unity materials
- attach scripts
- modify `01_Game.unity`
- create new design scenes
- tune URP shaders
- wire collision FX into gameplay

Allowed exception:

- `EggDesign.unity` may be modified as the shared preview lineup scene.
- A simple skin preview prefab may be created if Unity needs a stable reference to the imported `.glb`.

## MVP Boundary

Allowed for egg design:

- visual egg model
- Blender material setup
- geometry details
- preview render
- Unity import/display verification
- shared `EggDesign.unity` preview placement
- optional Blender-side FX mockup if requested

Not allowed unless requested as expansion:

- durability
- HP
- accumulated cracks
- real breaking behavior
- Rigidbody shell fragments
- gameplay effects
- Unity VFX Graph
- Unity particle systems
- decals
- skin stats

## Prismhorn Lesson Learned

For Prismhorn-like assets, avoid this failure mode:

- too much procedural reinterpretation
- too many small repeated panels
- wrong dominant color balance
- skinny spike-like protrusions when the reference has chunky crystals
- Unity material tuning before the Blender design is already close

Better approach:

- first make the Blender model closely match the reference
- use large readable crystal panels
- keep the color balance close to the reference
- use chunky low-poly side crystals
- only after Blender preview is close, do minimal Unity verification

## Start-Of-Task Checklist

Before creating a new egg:

- Confirm the skin name.
- Confirm the reference image path.
- Confirm this is Blender-first work with minimal Unity verification.
- State what details from the reference will be modeled.
- State the output file paths.
- State that `EggDesign.unity` will be reused for Unity preview.
- Wait for user approval.

## Completion Checklist

Before reporting done:

- `.blend` exists.
- `.glb` exists.
- preview image exists.
- Unity import/display was checked.
- the egg was added to the shared `EggDesign.unity` preview scene if requested for verification.
- preview visibly matches the reference direction.
- no new per-egg Unity scene was created.
- no gameplay scene, gameplay prefab, project setting, or script was modified.
- no unrelated files were changed.
- no temporary Blender backup files such as `.blend1` are left unless explicitly needed.
