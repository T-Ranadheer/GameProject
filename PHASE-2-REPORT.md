# Phase 2 Car-Transforming Robot Report

Date: 2026-10-24

## Result

Implemented the first functional transforming robot, **Atlas Runner**, while preserving the Phase 1 superhero, flight, combat, mission, pause, and restart flow.

The robot can be deployed with `R`, controlled in humanoid form, transformed with `T`, driven as a four-wheel physics vehicle, transformed back, used in combat, and undeployed to return control to the superhero.

## Implemented functionality

- Reusable transformation state machine with:
  - `Robot`
  - `TransformingToCar`
  - `Car`
  - `TransformingToRobot`
- Timed procedural transformation sequence with blended procedural visuals rather than a model swap.
- Safe transition gating for airborne, moving, blocked, and already-transforming states.
- Controller exclusivity: CharacterController is active only for robot movement; Rigidbody, BoxCollider, and WheelColliders are active only for car movement.
- Invalid-position recovery and stable-form recovery after interrupted transitions.
- Atlas Runner humanoid form with:
  - walking, running, jumping, grounded recovery
  - heavy melee attack on `E`
  - ranged energy attack on left mouse button
  - separate robot health
- Atlas Runner car form with:
  - four WheelColliders
  - throttle/reverse
  - steering
  - acceleration limiting
  - braking on `Space`
  - collision-aware Rigidbody settings
  - wheel visual synchronization
  - speed limiting and grounded recovery
- Camera target switching between superhero and robot, with separate car/robot camera distances and heights.
- Robot deployment and return flow through `R`.
- Enemy target switching while Atlas Runner is deployed, then restoration to the superhero when the robot is returned.
- Android touch buttons for robot deployment, transformation, melee, braking, and existing attack/movement controls.
- Additional robot configuration values in the existing prototype configuration object.

## Architecture

Key reusable systems:

- `PrototypeTransformStateMachine.cs` — deterministic form-transition state machine.
- `PrototypeRobotController.cs` — lifecycle, form switching, collision-shape ownership, movement-mode exclusivity, and recovery.
- `PrototypeCarController.cs` — Rigidbody/WheelCollider vehicle behavior and vehicle math helpers.
- `PrototypeRobotCombat.cs` — robot melee and ranged attacks.
- `PrototypeGameManager.cs` — deployment, return-to-hero, camera-target switching, and enemy-target switching.
- `PrototypeWorldBuilder.cs` — procedural Atlas Runner robot/car greybox construction.

The separation is intended to support future car, helicopter, tank, motorcycle, and jet transformer controllers without changing the transformation state-machine contract.

## Controls

### Windows PC

- `WASD` — move in robot form / drive in car form
- `Shift` — robot sprint
- `Space` — robot jump / car brake
- `T` — transform between robot and car
- `E` — heavy robot melee
- `Left mouse` — superhero or robot energy attack
- `Right mouse` — camera look
- `R` — deploy Atlas Runner / return to superhero
- `F` — existing superhero flight toggle
- `Esc` — pause

### Android

The existing runtime touch layout now includes robot deployment, transformation, melee, and brake buttons. The existing movement, flight, attack, and pause controls remain available. Touch controls are generated at runtime and scale through the existing CanvasScaler setup.

## Files changed

- `Assets/Scripts/Prototype/PrototypeGameConfig.cs`
- `Assets/Scripts/Prototype/PrototypeInput.cs`
- `Assets/Scripts/Prototype/PrototypeTransformStateMachine.cs`
- `Assets/Scripts/Prototype/PrototypeRobotController.cs`
- `Assets/Scripts/Prototype/PrototypeCarController.cs`
- `Assets/Scripts/Prototype/PrototypeRobotCombat.cs`
- `Assets/Scripts/Prototype/PrototypeGameManager.cs`
- `Assets/Scripts/Prototype/PrototypeWorldBuilder.cs`
- `Assets/Scripts/Prototype/PrototypeEnemy.cs`
- `Assets/Scripts/Prototype/PrototypeCameraRig.cs`
- `Assets/Scripts/Prototype/PrototypeBootstrap.cs`
- `Assets/Scripts/Prototype/PrototypeTouchControls.cs`
- `Assets/Scripts/Prototype/PrototypeHud.cs`
- `Assets/Tests/Runtime/PrototypeRobotTests.cs`
- `Assets/Tests/PlayMode/PrototypePlayModeTests.cs`
- `PHASE-2-REPORT.md`

## Automated checks actually executed

- Unity 6.3.27f1 compilation and scene validation: passed.
  - `Builds/Validation/phase2-validation-final.log`
  - `Builds/Validation/phase1-validation.txt`
- Edit Mode tests: **10 passed, 0 failed**.
  - `Builds/Validation/phase2-editmode-results-final.xml`
  - Covers Phase 1 health/ability/mission/flight behavior plus robot initial state, repeated transformations, invalid requests, interruption recovery, vehicle math, and robot health.
- Play Mode tests: **1 passed, 0 failed**.
  - `Builds/Validation/phase2-playmode-results-final.xml`
  - Loads the actual scene, verifies the robot deployment path, robot-to-car transition, car-to-robot transition, and return to superhero control.
- Windows development build: **succeeded**.
  - `Builds/Windows/NovaHeroPrototype.exe`
  - Unity build report size: 165,731,105 bytes.
  - Build report: `Builds/Validation/windows-build.txt`.
- Windows player smoke launch: executable remained alive for 15 seconds and was then stopped by automation.
- Android development build: **succeeded**.
  - `Builds/Android/NovaHeroPrototype.apk`
  - APK size: 61,832,889 bytes.
  - Unity build report size: 970,529,436 bytes.
  - Build report: `Builds/Validation/android-build.txt`.

## Warnings and errors investigated

The final Windows and Android build reports both show **0 errors**. The final Unity-reported warning counters are 13 for Windows and 32 for Android; these are build-pipeline/package warnings rather than project compiler failures.

The logs still contain Unity environment diagnostics:

- `LicensingClient has failed validation; ignoring`
- `Access token is unavailable; failed to update`
- `d3d12: failed to query info queue interface`

Unity subsequently resolved the installed Unity Personal entitlement and completed both builds successfully. No project C# compiler errors, build failure, Gradle failure, or runtime test failure remained in the final checks. Earlier Rigidbody API warnings were removed by using Unity 6 `linearVelocity`, `linearDamping`, and `angularDamping` APIs.

Intermediate failures were retained in their timestamped validation logs for traceability and were fixed before the final checks: two initial compile errors in the camera/robot additions, early Play Mode deployment/grounding assertions, and kinematic Rigidbody velocity warnings. The final result files listed above are the authoritative final runs.

## Known limitations

- Robot and vehicle visuals are organized procedural greybox geometry, not production art.
- No physical Android phone or emulator was available, so Android gameplay/device behavior is not claimed as validated.
- The vehicle uses WheelColliders and Rigidbody physics but is still a prototype handling model; suspension, drivetrain tuning, audio, and advanced traction need a later polish pass.
- Robot enemies still use the Phase 1 local pursuit behavior rather than a full NavMesh combat system.
- Robot deployment is currently a prototype character switch, not a finished character-selection UI or roster system.
- No production animation clips or authored sound assets were added.

## Next milestone

Build the electric superhero and giant-form foundation: shared power/energy systems, giant-scale collision and camera handling, and a reusable character-form framework that can coexist with Atlas Runner and the original superhero.
