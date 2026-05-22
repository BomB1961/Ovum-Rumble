# Egg Impact FX MVP and Expansion Plan

## Purpose

This document separates what belongs in the current MVP from what should be implemented later for egg collision, cracking, and breaking effects.

The project rule is simple: the basic egg-flicking game must remain playable even if presentation features are disabled.

## MVP Scope

The MVP can include collision feedback that improves game feel without changing the core rules.

Implemented or allowed in MVP:

- Detect egg-to-egg collisions through `EggController`.
- Calculate collision strength from relative velocity.
- Spawn a short themed impact effect at the contact point.
- Scale the effect by collision strength.
- Keep the effect optional through Inspector settings.
- Keep existing dust particles, sound, turn flow, fall detection, and win conditions unchanged.

Current MVP implementation:

- `EggController.CollisionDetailedOccurred` exposes impact strength, contact point, and contact normal.
- `ThemedImpactFxController` listens to spawned eggs and creates the Embercore impact effect.
- `ThemedImpactFxInstance` animates lava glow, basalt shards, sparks, and a short impact flash.
- `Assets/Scenes/01_Game.unity` connects the controller to the existing `EffectController` object.

## Not MVP

The following features should not be added until the core match loop is stable and the team explicitly moves to an expansion phase.

- Egg HP or durability.
- Permanent damage state on eggs.
- Accumulated crack visuals.
- Switching the egg material based on damage.
- Realtime emission or crack value changes on the egg material.
- Breaking, bursting, or removing eggs because of collision damage.
- Rigidbody shell fragments launched from the egg.
- Decal projection for cracks.
- VFX Graph-based impact effects.
- Theme-specific gameplay behavior.

## Expansion Phase Proposal

### Phase 1: Damage Data Only

Goal: add durability without changing visuals first.

- Add an `EggDurability` component.
- Store max durability and current durability.
- Convert collision impact into damage.
- Expose events such as `DamageChanged` and `Broken`.
- Do not remove or destroy eggs until rules are approved.

### Phase 2: Crack Visuals

Goal: show damage without changing gameplay.

- Add `EggCrackVisualController`.
- Map durability percent to crack level.
- Swap between prepared crack materials or meshes.
- Keep visuals cosmetic and reversible for tuning.

### Phase 3: Break Presentation

Goal: make breaking feel satisfying once durability rules are accepted.

- Add a break FX prefab per egg theme.
- Hide or replace the intact egg mesh on break.
- Spawn shell pieces with short-lived Rigidbody motion.
- Play theme-specific flash, particles, sound, and camera impulse.

### Phase 4: Advanced VFX

Goal: improve quality after the gameplay and art direction are locked.

- Add VFX Graph or Particle System variants.
- Add decal cracks if URP renderer setup supports it.
- Add pooled FX instances for performance.
- Add per-theme data assets for lava, frost, jungle, storm, and other egg skins.

## Unity Setup Notes

For the current MVP scene:

- Keep `ThemedImpactFxController` on the `EffectController` GameObject.
- Assign `EggSpawner`.
- Assign `EmbercoreImpactFX.prefab`.
- Tune only these values for MVP:
  - `Enable Themed Impact Fx`
  - `Min Impact For Effect`
  - `Max Impact For Scale`
  - `Min Effect Scale`
  - `Max Effect Scale`
  - `Surface Offset`

Do not add durability or break rules to `01_Game.unity` during MVP stabilization.

## Done Criteria for MVP FX

- Unity compiles with no Console errors.
- A normal match can still start and finish.
- Egg collisions still push eggs physically.
- Collision FX appears only on meaningful impacts.
- Disabling `ThemedImpactFxController` or `Enable Themed Impact Fx` preserves the original game loop.
