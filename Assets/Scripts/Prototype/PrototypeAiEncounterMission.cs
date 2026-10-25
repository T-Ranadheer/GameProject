using UnityEngine;

public enum PrototypeAiEncounterMissionState
{
    Idle,
    Active,
    Complete,
    Failed
}

public sealed class PrototypeAiEncounterMission : MonoBehaviour
{
    private PrototypePoliceCoordinator coordinator;
    private bool startedByPlayer;

    public PrototypeAiEncounterMissionState State { get; private set; } = PrototypeAiEncounterMissionState.Idle;
    public string Objective { get; private set; } = "Optional AI encounters available";
    public bool IsActive => State == PrototypeAiEncounterMissionState.Active;

    public void Configure(PrototypePoliceCoordinator policeCoordinator)
    {
        coordinator = policeCoordinator;
        State = PrototypeAiEncounterMissionState.Idle;
        Objective = "Press H to start a hostile-robot encounter";
    }

    public bool StartHostileRobotChallenge(Vector3 center, bool playerInitiated = false)
    {
        if (coordinator == null || State == PrototypeAiEncounterMissionState.Active)
        {
            return false;
        }

        if (!coordinator.StartHostileEncounter(center))
        {
            return false;
        }

        startedByPlayer = playerInitiated;
        State = PrototypeAiEncounterMissionState.Active;
        Objective = "Defeat the hostile robot encounter";
        return true;
    }

    private void Update()
    {
        if (coordinator == null)
        {
            return;
        }

        if (State == PrototypeAiEncounterMissionState.Idle && PrototypeInput.TriggerHostileEncounterPressed)
        {
            var manager = PrototypeGameManager.Instance;
            var center = manager != null && manager.ActiveTransform != null ? manager.ActiveTransform.position + Vector3.forward * 10f : Vector3.zero;
            StartHostileRobotChallenge(center, true);
        }

        if (State == PrototypeAiEncounterMissionState.Active && coordinator.ActiveHostileRobots == 0 && coordinator.HostileRobotsDefeated > 0)
        {
            State = PrototypeAiEncounterMissionState.Complete;
            Objective = startedByPlayer ? "Hostile robot encounter complete" : "Hostile robot encounter cleared";
        }
    }
}
