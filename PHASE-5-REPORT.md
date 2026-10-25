# Phase 5 Report — Police AI, Wanted Levels, Police Robots, and Hostile Robots

Date: 2026-10-25  
Unity: 6000.3.27f1 LTS  
Rendering: URP

## Executive result

Implemented a bounded prototype AI framework that makes the existing greybox city react to incidents and supports police escalation, police robots, an aerial police drone, hostile robot archetypes, faction filtering, and a repeatable hostile-robot encounter.

The existing original superhero, Volt Warden, Stone Colossus, Atlas Runner robot/car transformation, mission flow, character switching, and combat systems remain functional in the final regression runs.

Physical Android validation was not possible because `adb` was not available and no authorized Android device or emulator was connected. Android build generation succeeded, but Android gameplay and frame-rate validation are not claimed.

## Police AI architecture

- `PrototypeAiFramework.cs`
  - Faction definitions and selective hostility rules.
  - Incident data structure.
  - Wanted-level enum and shared AI states.
  - Bounded line-of-sight, target, ground, and position helpers.
  - Five-level wanted heat system with duplicate-incident protection and decay.
- `PrototypeAiAgents.cs`
  - Reusable AI lifecycle, target, state, movement, damage, and death behavior.
- `PrototypePoliceCoordinator.cs`
  - Incident registration.
  - Wanted-level tracking.
  - Pursuit counting.
  - Bounded police response spawning.
  - Police/hostile-robot encounter lists and cleanup.
  - Safe spawn-position validation.
- `PrototypePoliceUnit.cs`
  - Patrol officer.
  - Patrol vehicle prototype.
  - Patrol/pursuit police robot.
  - Tactical police robot.
  - Aerial police drone.
- `PrototypeAiEncounterMission.cs`
  - Repeatable hostile-robot encounter state and completion tracking.

## Wanted-level behavior

The system uses configurable heat thresholds for:

- Clear.
- Minor Incident.
- Active Pursuit.
- Coordinated Response.
- Advanced Tactical.
- Maximum Response.

Incidents are deduplicated by type, source, and position. Heat decays only after the configured delay and when active pursuits have ended. Police unit counts and reinforcements are capped by configuration.

Damage to a faction-marked police actor by a player-faction actor can register an `AttackPolice` incident through the shared health path. Hostile robot encounter events use the same central coordinator rather than independent per-agent wanted updates.

## Perception, pursuit, and escape

- Configurable perception range, field of view, refresh interval, and line-of-sight checks.
- Last-known-position retention when the player is no longer visible.
- Patrol, investigation/search, pursuit, and attack states.
- Pursuit handoff behavior through the coordinator's shared active-unit state.
- Wanted persistence through temporary line-of-sight loss.
- Search termination and return to patrol after the search timer expires.
- Safe target clearing when the player or target health is disabled/destroyed.

The current greybox does not contain a baked NavMesh asset. The prototype uses bounded local CharacterController movement, safe ground checks, arena limits, and last-known-position search rather than claiming full NavMesh pathfinding. This limitation is explicit and remains a later navigation pass.

## Police robots and drone

### Patrol/pursuit police robot

- Patrol orbits around an assigned origin.
- Detects the active player target.
- Pursues and engages at close range.
- Uses configurable health, speed, attack range, damage, and cooldown behavior.

### Tactical police robot

- Higher durability.
- Pursuit support and ranged energy attack.
- Configurable attack timing and damage.
- Participates in bounded higher-level wanted responses.

### Police drone

- Uses bounded airborne movement rather than helicopter physics.
- Follows an elevated target position.
- Provides reconnaissance/pursuit support.
- Uses ranged projectile behavior and safe cleanup.

## Hostile robot classes

### Scout Robot

- Fast movement.
- Close-range attack.
- Lower durability.

### Ranged Assault Robot

- Preferred-distance behavior.
- Ranged projectile attack.
- Repositions when too close or obstructed.

### Heavy Mech

- Slow movement.
- High durability.
- Area impact attack with bounded knockback.

### Boss extension point

The hostile robot role includes a boss variant with a health-threshold phase transition and stronger phase-two damage behavior. A modest encounter can use this role without requiring a cinematic or separate boss system.

## Faction rules and combat integration

- Player, civilian, police, hostile robot, Sentinel, and environment factions are explicit.
- Police are hostile to player targets only when their response is active and to hostile robots when the relationship requires it.
- Hostile robots are hostile to player and police factions.
- Police and hostile robots are not globally hostile to every other actor.
- Existing health, damage types, projectile cleanup, resistance, and knockback limits are reused.
- Electrical damage interacts with robot resistance values instead of applying universal immunity or unlimited disabling.

## UI and diagnostics

- HUD now displays wanted level, pursuit/search status, active police count, hostile-robot count, and hostile-encounter status.
- `H` starts a hostile-robot encounter in the prototype runtime.
- Public coordinator properties expose wanted state, active-unit counts, incident position/type, pursuit status, and defeated hostile count for structured runtime diagnostics.
- No unbounded per-agent logging was added.

## Files changed

New runtime systems:

- `Assets/Scripts/Prototype/PrototypeAiFramework.cs`
- `Assets/Scripts/Prototype/PrototypeAiAgents.cs`
- `Assets/Scripts/Prototype/PrototypePoliceCoordinator.cs`
- `Assets/Scripts/Prototype/PrototypePoliceUnit.cs`
- `Assets/Scripts/Prototype/PrototypeHostileRobot.cs`
- `Assets/Scripts/Prototype/PrototypeAiEncounterMission.cs`

Updated runtime systems:

- `Assets/Scripts/Prototype/PrototypeGameConfig.cs`
- `Assets/Scripts/Prototype/PrototypeInput.cs`
- `Assets/Scripts/Prototype/PrototypeHealth.cs`
- `Assets/Scripts/Prototype/PrototypeProjectile.cs`
- `Assets/Scripts/Prototype/PrototypeWorldBuilder.cs`
- `Assets/Scripts/Prototype/PrototypeBootstrap.cs`
- `Assets/Scripts/Prototype/PrototypeHud.cs`

Updated tests/validation:

- `Assets/Tests/Runtime/PrototypePhase3Tests.cs`
- `Assets/Tests/PlayMode/PrototypePlayModeTests.cs`
- `Assets/Editor/PrototypeBuildValidation.cs`
- `PHASE-5-REPORT.md`

Previous phase reports were preserved. `R:\Mygame\docs\PHASE-0-AUDIT.md` was preserved unchanged; its SHA-256 remained `F56DF5B7D6C678BB65D0E8A11858677E2009EC07FD482A8F75F5A0C56AA1B0F9`.

## Tests actually executed

- Edit Mode: **18 passed, 0 failed, 0 skipped**. Evidence: `Builds/Validation/phase5-editmode-results-final.xml`.
- Play Mode: **4 passed, 0 failed, 0 skipped**. Evidence: `Builds/Validation/phase5-playmode-results-final.xml`.
- Phase 5 scene validation: passed. Evidence: `Builds/Validation/phase5-validation.txt` and `Builds/Validation/phase5-validation-final.log`.

The new tests cover:

- Wanted-level escalation, deduplication, pursuit hold, and decay.
- Faction hostility filtering.
- Police coordinator spawning and response caps.
- Hostile robot encounter registration and bounded active counts.
- Runtime scene initialization with AI coordinator, police unit, and hostile robot classes.
- Existing roster, projectile, switching, robot deployment, and transformation regression behavior.

## Build results

- Windows development build: **succeeded**, 0 errors, 0 warnings. Evidence: `Builds/Validation/phase5-windows-build.txt` and `Builds/Validation/phase5-windows-build-final.log`.
- Android development build: **succeeded**, 0 errors, 19 warnings. Evidence: `Builds/Validation/phase5-android-build.txt` and `Builds/Validation/phase5-android-build-final.log`.
- Windows smoke launch: Phase 5 executable remained alive for 15 seconds and was stopped by automation.
- Player log: `C:\Users\thumm\AppData\LocalLow\DefaultCompany\GameProject\Player.log`; inspected final log had 0 error/exception/failed matches.

## Build and package sizes

- Windows Unity build-report size: 165,136,304 bytes.
- Windows player directory: 165,329,421 bytes.
- Android Unity build-report size: 973,631,473 bytes.
- Android APK file: 61,905,398 bytes.
- Unity Library directory: 8,366,393,883 bytes.

Build-report accounting, packaged file size, and generated Library size are reported separately and are not interchangeable.

## Warning and error analysis

- Final Windows build: 0 warnings, 0 errors.
- Final Android build: 19 warnings, 0 errors.
- Final validation/build log scans found no project C# compiler errors, build-failure markers, Gradle failure, or unhandled exception markers.
- Android warnings remain package/build-pipeline/environment diagnostics and were not indiscriminately suppressed.
- No physical Android test was possible because `adb` was not found in the environment.

## Known limitations

- No NavMesh was baked or verified for the greybox scene. Current ground AI uses bounded local movement and safe recovery rather than claiming full pathfinding.
- Police vehicle behavior is a lightweight pursuit movement prototype, not a full WheelCollider police-car controller.
- Police and hostile robot geometry, audio, and VFX are procedural prototype content rather than production assets.
- The police drone uses bounded aerial movement, not full helicopter physics.
- Wanted-level persistence is runtime-only; save-game persistence is deferred.
- Mission integration currently provides a repeatable hostile-robot encounter rather than a large branching police campaign.
- Android touch behavior, hardware performance, startup time, and memory use remain unverified.
- No large external art, animation, or sound packages were downloaded.

## Manual actions required

To validate Android gameplay, connect and authorize a physical Android device with USB debugging, install Android Platform Tools so `adb` is available, and ensure `adb devices` reports the device as authorized. Then install `Builds/Android/Phase5/NovaHeroPrototype.apk`, launch it, and collect device logs/performance data.

## Next recommended milestone

Proceed to Phase 6 only after a short navigation/police behavior polish pass and physical Android validation where possible. The next major feature milestone is the remaining transforming robot roster: helicopter, tank, motorcycle, and jet, reusing the existing transformation contract while adding distinct movement and combat controllers.
