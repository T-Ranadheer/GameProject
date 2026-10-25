# Phase 1 First Playable Prototype Report

Date: 2026-10-24

## Implemented

- Runtime-generated third-person hero prototype with grounded movement, sprinting, jumping, gravity, ground checks, arena bounds, and invalid-position recovery.
- Flight mode toggle with horizontal/vertical movement, takeoff, landing, and collision-aware CharacterController movement.
- Third-person camera rig with mouse look, smoothing, and sphere-cast collision avoidance.
- Energy projectile ability with target assist, cooldown, emissive visual feedback, projectile lifetime, and damage delivery.
- Sentinel enemy archetype with detection, pursuit, melee attacks, health, hit reaction, and death state.
- Runtime-generated greybox city block containing roads, sidewalks, buildings, obstacles, arena walls, signal beacon, and extraction point.
- Mission sequence: reach beacon, defeat four sentinels, reach extraction, complete/fail states, and restart.
- Health system, HUD, pause/resume/restart overlay, keyboard/mouse controls, and Android touch control layout.
- Runtime/editor/test assembly separation in `Prototype.Runtime`, `Prototype.Tests`, and `Prototype.PlayModeTests`.

## Files

- `Assets/Scenes/SampleScene.unity`
- `Assets/Scripts/Prototype/PrototypeBootstrap.cs`
- `Assets/Scripts/Prototype/Prototype.Runtime.asmdef`
- `Assets/Scripts/Prototype/PrototypeGameConfig.cs`
- `Assets/Scripts/Prototype/PrototypeGameManager.cs`
- `Assets/Scripts/Prototype/PrototypeHealth.cs`
- `Assets/Scripts/Prototype/PrototypeInput.cs`
- `Assets/Scripts/Prototype/PrototypePlayerController.cs`
- `Assets/Scripts/Prototype/PrototypeCameraRig.cs`
- `Assets/Scripts/Prototype/PrototypePlayerAbility.cs`
- `Assets/Scripts/Prototype/PrototypeProjectile.cs`
- `Assets/Scripts/Prototype/PrototypeEnemy.cs`
- `Assets/Scripts/Prototype/PrototypeMissionController.cs`
- `Assets/Scripts/Prototype/PrototypeWorldBuilder.cs`
- `Assets/Scripts/Prototype/PrototypeTouchControls.cs`
- `Assets/Scripts/Prototype/PrototypeHud.cs`
- `Assets/Editor/PrototypeBuildValidation.cs`
- `Assets/Tests/Runtime/PrototypeGameplayTests.cs`
- `Assets/Tests/Runtime/Prototype.Tests.asmdef`
- `Assets/Tests/PlayMode/PrototypePlayModeTests.cs`
- `Assets/Tests/PlayMode/Prototype.PlayModeTests.asmdef`

## Checks actually run

- Unity 6.3.27f1 script compilation and scene validation: passed. Result: `Builds/Validation/phase1-validation.txt`.
- Edit Mode tests: 4 passed, 0 failed. Result: `Builds/Validation/editmode-results-final.xml`.
- Play Mode test: 1 passed, 0 failed. Result: `Builds/Validation/playmode-results-2.xml`.
- Windows development build: succeeded. Result: `Builds/Windows/NovaHeroPrototype.exe` and `Builds/Validation/windows-build.txt`.
- Windows player smoke launch: executable remained alive for 15 seconds, then was stopped by the automation.
- Android development build: succeeded. Result: `Builds/Android/NovaHeroPrototype.apk` and `Builds/Validation/android-build.txt`.

## Limitations and truthful status

- Android build validation succeeded on the available toolchain, but no Android device/emulator gameplay session was available; Android device behavior is not claimed as validated.
- The city, hero, enemy, projectile, and UI visuals are procedural greybox/primitive content and need replacement with authored assets.
- Enemy navigation is local pursuit rather than baked NavMesh navigation; the package is present for a later navigation pass.
- Input uses the installed Input System directly and keeps the existing input action asset/project settings unchanged.
- Build summaries include Unity's reported warning/error counters even though the player build result was `Succeeded`; the corresponding logs show no project compiler errors, and the visible build log errors are Unity licensing/device initialization diagnostics.
- Generated development builds are kept under `Builds/` and are not required as source assets.

## Next milestone

Implement the first functional car-transforming robot using a reusable vehicle controller, robot/vehicle state machine, transformation constraints, and a focused test slice without changing the Phase 1 mission flow.
