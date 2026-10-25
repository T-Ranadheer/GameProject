using System;
using System.Collections.Generic;
using UnityEngine;

public enum PrototypeFaction
{
    Player,
    Civilian,
    Police,
    HostileRobot,
    Sentinel,
    Environment
}

public enum PrototypeAiState
{
    Idle,
    Patrol,
    Investigate,
    Pursue,
    Search,
    Attack,
    Recover,
    Disabled,
    Defeated
}

public enum PrototypeIncidentType
{
    AssaultCivilian,
    DamageProtectedObject,
    AttackPolice,
    AttackMilitary,
    MissionCrime,
    HostileRobotEvent
}

public enum PrototypeWantedLevel
{
    Clear,
    MinorIncident,
    ActivePursuit,
    CoordinatedResponse,
    AdvancedTactical,
    MaximumResponse
}

public enum PrototypePoliceRole
{
    PatrolOfficer,
    PatrolVehicle,
    PatrolRobot,
    TacticalRobot,
    PoliceDrone
}

public enum PrototypeHostileRobotRole
{
    Scout,
    RangedAssault,
    HeavyMech,
    Boss
}

public readonly struct PrototypeIncident
{
    public readonly PrototypeIncidentType Type;
    public readonly Vector3 Position;
    public readonly float Severity;
    public readonly GameObject Source;
    public readonly GameObject Victim;
    public readonly bool Suppressed;

    public PrototypeIncident(PrototypeIncidentType type, Vector3 position, float severity, GameObject source, GameObject victim, bool suppressed = false)
    {
        Type = type;
        Position = position;
        Severity = Mathf.Max(0f, severity);
        Source = source;
        Victim = victim;
        Suppressed = suppressed;
    }

    public bool IsValid => !Suppressed && Severity > 0f && IsFinite(Position);

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z)
            && !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
    }
}

public static class PrototypeFactionRules
{
    public static bool IsHostile(PrototypeFaction source, PrototypeFaction target)
    {
        if (source == target || source == PrototypeFaction.Environment || target == PrototypeFaction.Environment)
        {
            return false;
        }

        if (source == PrototypeFaction.Police && target == PrototypeFaction.HostileRobot)
        {
            return true;
        }

        if (source == PrototypeFaction.HostileRobot && target == PrototypeFaction.Police)
        {
            return true;
        }

        if (source == PrototypeFaction.HostileRobot && target == PrototypeFaction.Player)
        {
            return true;
        }

        if (source == PrototypeFaction.Police && target == PrototypeFaction.Player)
        {
            return true;
        }

        if (source == PrototypeFaction.Sentinel && target == PrototypeFaction.Player)
        {
            return true;
        }

        return false;
    }
}

public sealed class PrototypeFactionMember : MonoBehaviour
{
    [SerializeField] private PrototypeFaction faction = PrototypeFaction.Environment;

    public PrototypeFaction Faction => faction;

    public void Configure(PrototypeFaction value)
    {
        faction = value;
    }
}

public static class PrototypeAiUtility
{
    public static bool IsValidTarget(Transform target, PrototypeHealth health)
    {
        return target != null && health != null && health.IsAlive && target.gameObject.activeInHierarchy;
    }

    public static PrototypeFaction GetFaction(GameObject actor, PrototypeFaction fallback = PrototypeFaction.Environment)
    {
        if (actor == null)
        {
            return fallback;
        }

        var member = actor.GetComponentInParent<PrototypeFactionMember>();
        return member != null ? member.Faction : fallback;
    }

    public static bool IsHostile(GameObject source, GameObject target)
    {
        return PrototypeFactionRules.IsHostile(GetFaction(source), GetFaction(target));
    }

    public static bool HasLineOfSight(Vector3 origin, Transform target, float maxDistance, GameObject observer)
    {
        if (target == null)
        {
            return false;
        }

        var targetPoint = target.position + Vector3.up * 0.9f;
        var offset = targetPoint - origin;
        if (offset.sqrMagnitude > maxDistance * maxDistance)
        {
            return false;
        }

        if (!Physics.Raycast(origin, offset.normalized, out var hit, offset.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        return hit.transform == target || hit.transform.IsChildOf(target) || (observer != null && hit.transform.IsChildOf(observer.transform));
    }

    public static Vector3 FlattenedDirection(Vector3 from, Vector3 to)
    {
        var offset = Vector3.ProjectOnPlane(to - from, Vector3.up);
        return offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector3.zero;
    }

    public static bool IsValidPosition(Vector3 position)
    {
        return !float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsNaN(position.z)
            && !float.IsInfinity(position.x) && !float.IsInfinity(position.y) && !float.IsInfinity(position.z)
            && position.y > -8f;
    }

    public static bool FindGround(Vector3 position, float maxDistance, out Vector3 groundPoint)
    {
        if (Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, out var hit, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            groundPoint = hit.point;
            return true;
        }

        groundPoint = position;
        return false;
    }
}

public sealed class PrototypeWantedSystem
{
    private readonly List<PrototypeIncident> recentIncidents = new List<PrototypeIncident>();
    private PrototypeGameConfig config;
    private float heat;
    private float timeSinceIncident;
    private int activePursuits;

    public PrototypeWantedLevel Level { get; private set; } = PrototypeWantedLevel.Clear;
    public float Heat => heat;
    public int ActivePursuits => activePursuits;
    public int RecentIncidentCount => recentIncidents.Count;
    public event Action<PrototypeWantedLevel> LevelChanged;

    public void Configure(PrototypeGameConfig gameConfig)
    {
        config = gameConfig ?? PrototypeGameConfig.CreateDefault();
        Reset();
    }

    public bool RegisterIncident(PrototypeIncident incident)
    {
        if (!incident.IsValid || IsDuplicate(incident))
        {
            return false;
        }

        recentIncidents.Add(incident);
        heat = Mathf.Clamp(heat + incident.Severity, 0f, config.wantedMaximumHeat);
        timeSinceIncident = 0f;
        RecalculateLevel();
        return true;
    }

    public void SetActivePursuits(int count)
    {
        activePursuits = Mathf.Max(0, count);
    }

    public void Tick(float deltaTime)
    {
        timeSinceIncident += Mathf.Max(0f, deltaTime);
        for (var i = recentIncidents.Count - 1; i >= 0; i--)
        {
            if (recentIncidents[i].Source == null || recentIncidents[i].Victim == null)
            {
                recentIncidents.RemoveAt(i);
            }
        }

        if (activePursuits <= 0 && timeSinceIncident >= config.wantedDecayDelay)
        {
            heat = Mathf.Max(0f, heat - config.wantedDecayPerSecond * Mathf.Max(0f, deltaTime));
            RecalculateLevel();
        }
    }

    public void Reset()
    {
        heat = 0f;
        timeSinceIncident = 0f;
        activePursuits = 0;
        recentIncidents.Clear();
        SetLevel(PrototypeWantedLevel.Clear);
    }

    private bool IsDuplicate(PrototypeIncident incident)
    {
        var dedupeDistance = config != null ? config.incidentDedupeDistance : 2.5f;
        for (var i = 0; i < recentIncidents.Count; i++)
        {
            var previous = recentIncidents[i];
            if (previous.Type == incident.Type && previous.Source == incident.Source && Vector3.Distance(previous.Position, incident.Position) <= dedupeDistance)
            {
                return true;
            }
        }

        return false;
    }

    private void RecalculateLevel()
    {
        var next = heat >= config.wantedLevel5Heat ? PrototypeWantedLevel.MaximumResponse
            : heat >= config.wantedLevel4Heat ? PrototypeWantedLevel.AdvancedTactical
            : heat >= config.wantedLevel3Heat ? PrototypeWantedLevel.CoordinatedResponse
            : heat >= config.wantedLevel2Heat ? PrototypeWantedLevel.ActivePursuit
            : heat >= config.wantedLevel1Heat ? PrototypeWantedLevel.MinorIncident
            : PrototypeWantedLevel.Clear;
        SetLevel(next);
    }

    private void SetLevel(PrototypeWantedLevel value)
    {
        if (Level == value)
        {
            return;
        }

        Level = value;
        LevelChanged?.Invoke(value);
    }
}
