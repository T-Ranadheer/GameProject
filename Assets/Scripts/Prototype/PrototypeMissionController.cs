using System;
using System.Collections.Generic;
using UnityEngine;

public enum PrototypeMissionState
{
    ReachBeacon,
    ClearEnemies,
    ReachExtraction,
    Complete,
    Failed
}

public sealed class PrototypeMissionController : MonoBehaviour
{
    public event Action<PrototypeMissionState> StateChanged;

    private PrototypePlayerController player;
    private PrototypeHealth playerHealth;
    private Transform activeCharacter;
    private PrototypeHealth activeCharacterHealth;
    private Transform beacon;
    private Transform extraction;
    private PrototypeGameConfig config;
    private int totalEnemies;
    private int defeatedEnemies;

    public PrototypeMissionState State { get; private set; } = PrototypeMissionState.ReachBeacon;
    public string Objective { get; private set; } = "Reach the signal beacon";
    public int TotalEnemies => totalEnemies;
    public int DefeatedEnemies => defeatedEnemies;
    public int RemainingEnemies => Mathf.Max(0, totalEnemies - defeatedEnemies);

    public void Configure(PrototypePlayerController playerController, PrototypeHealth health, Transform missionBeacon, Transform extractionPoint, IReadOnlyList<PrototypeEnemy> enemies, PrototypeGameConfig gameConfig)
    {
        player = playerController;
        playerHealth = health;
        activeCharacter = playerController != null ? playerController.transform : null;
        activeCharacterHealth = health;
        beacon = missionBeacon;
        extraction = extractionPoint;
        totalEnemies = enemies != null ? enemies.Count : 0;
        defeatedEnemies = 0;
        config = gameConfig;
        State = PrototypeMissionState.ReachBeacon;
        Objective = "Reach the signal beacon";
    }

    public void SetActiveCharacter(Transform character, PrototypeHealth health)
    {
        activeCharacter = character;
        activeCharacterHealth = health;
    }

    private void Update()
    {
        if (activeCharacter == null || activeCharacterHealth == null || !activeCharacterHealth.IsAlive || State == PrototypeMissionState.Complete || State == PrototypeMissionState.Failed || PrototypeGameManager.IsPaused)
        {
            if (activeCharacterHealth != null && !activeCharacterHealth.IsAlive && State != PrototypeMissionState.Failed && State != PrototypeMissionState.Complete)
            {
                SetState(PrototypeMissionState.Failed, "Hero down — restart the mission");
            }
            return;
        }

        if (State == PrototypeMissionState.ReachBeacon && IsNear(beacon))
        {
            SetState(PrototypeMissionState.ClearEnemies, "Defeat the hostile sentinels");
        }
        else if (State == PrototypeMissionState.ClearEnemies && defeatedEnemies >= totalEnemies)
        {
            SetState(PrototypeMissionState.ReachExtraction, "Reach the extraction point");
        }
        else if (State == PrototypeMissionState.ReachExtraction && IsNear(extraction))
        {
            SetState(PrototypeMissionState.Complete, "Mission complete — district secured");
        }
    }

    public void NotifyBeaconReached()
    {
        if (State == PrototypeMissionState.ReachBeacon)
        {
            SetState(PrototypeMissionState.ClearEnemies, "Defeat the hostile sentinels");
        }
    }

    public void NotifyEnemyDefeated(PrototypeEnemy enemy)
    {
        if (State == PrototypeMissionState.Failed || State == PrototypeMissionState.Complete)
        {
            return;
        }

        defeatedEnemies = Mathf.Min(totalEnemies, defeatedEnemies + 1);
        if (State == PrototypeMissionState.ClearEnemies && defeatedEnemies >= totalEnemies)
        {
            SetState(PrototypeMissionState.ReachExtraction, "Reach the extraction point");
        }
    }

    public void NotifyExtractionReached()
    {
        if (State == PrototypeMissionState.ReachExtraction)
        {
            SetState(PrototypeMissionState.Complete, "Mission complete — district secured");
        }
    }

    private bool IsNear(Transform target)
    {
        return target != null && activeCharacter != null && Vector3.Distance(activeCharacter.position, target.position) <= config.missionMarkerRadius;
    }

    private void SetState(PrototypeMissionState state, string objective)
    {
        State = state;
        Objective = objective;
        StateChanged?.Invoke(state);
    }
}
