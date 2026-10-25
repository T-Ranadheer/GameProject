using System.Collections.Generic;
using UnityEngine;

public sealed class PrototypePoliceCoordinator : MonoBehaviour
{
    public static PrototypePoliceCoordinator Instance { get; private set; }

    private readonly List<PrototypePoliceUnit> policeUnits = new List<PrototypePoliceUnit>();
    private readonly List<PrototypeHostileRobot> hostileRobots = new List<PrototypeHostileRobot>();
    private PrototypeGameConfig config;
    private Transform runtimeRoot;
    private Vector3 lastIncidentPosition;
    private PrototypeIncidentType lastIncidentType;
    private float responseTimer;
    private int reinforcementsSpawned;
    private int hostileDefeated;

    public PrototypeWantedSystem Wanted { get; } = new PrototypeWantedSystem();
    public int ActivePoliceUnits => CountAlive(policeUnits);
    public int ActiveHostileRobots => CountAlive(hostileRobots);
    public int HostileRobotsDefeated => hostileDefeated;
    public Vector3 LastIncidentPosition => lastIncidentPosition;
    public PrototypeIncidentType LastIncidentType => lastIncidentType;
    public string PursuitStatus => Wanted.Level == PrototypeWantedLevel.Clear ? "CLEAR" : Wanted.ActivePursuits > 0 ? "PURSUIT" : "SEARCH";

    public void Configure(PrototypeGameConfig gameConfig, Transform parent)
    {
        Instance = this;
        config = gameConfig ?? PrototypeGameConfig.CreateDefault();
        runtimeRoot = parent != null ? parent : transform;
        Wanted.Configure(config);
        Wanted.LevelChanged -= OnWantedLevelChanged;
        Wanted.LevelChanged += OnWantedLevelChanged;
    }

    private void Update()
    {
        if (config == null || PrototypeGameManager.IsPaused)
        {
            return;
        }

        responseTimer = Mathf.Max(0f, responseTimer - Time.deltaTime);
        Wanted.SetActivePursuits(CountPursuits());
        Wanted.Tick(Time.deltaTime);

        if (Wanted.Level != PrototypeWantedLevel.Clear && responseTimer <= 0f)
        {
            DispatchResponse();
            responseTimer = config.policeResponseInterval;
        }

        CleanupLists();
    }

    public bool RegisterIncident(PrototypeIncident incident)
    {
        if (!Wanted.RegisterIncident(incident))
        {
            return false;
        }

        lastIncidentPosition = incident.Position;
        lastIncidentType = incident.Type;
        return true;
    }

    public bool ReportDamageIncident(PrototypeHealth victimHealth, GameObject source, PrototypeDamageInfo damageInfo)
    {
        if (victimHealth == null || source == null)
        {
            return false;
        }

        var victimFaction = PrototypeAiUtility.GetFaction(victimHealth.gameObject);
        var sourceFaction = PrototypeAiUtility.GetFaction(source, PrototypeFaction.Environment);
        if (victimFaction != PrototypeFaction.Police || sourceFaction != PrototypeFaction.Player)
        {
            return false;
        }

        return RegisterIncident(new PrototypeIncident(PrototypeIncidentType.AttackPolice, victimHealth.transform.position, 1.4f, source, victimHealth.gameObject));
    }

    public bool StartHostileEncounter(Vector3 center)
    {
        CleanupLists();
        if (ActiveHostileRobots > 0 || config == null)
        {
            return false;
        }

        var positions = new[]
        {
            center + new Vector3(-5f, 1.05f, 4f),
            center + new Vector3(5f, 1.05f, 5f),
            center + new Vector3(0f, 1.05f, 9f)
        };
        SpawnHostileRobot(PrototypeHostileRobotRole.Scout, positions[0]);
        SpawnHostileRobot(PrototypeHostileRobotRole.RangedAssault, positions[1]);
        SpawnHostileRobot(PrototypeHostileRobotRole.HeavyMech, positions[2]);
        var activePlayer = PrototypeGameManager.Instance != null ? PrototypeGameManager.Instance.ActiveTransform : null;
        if (activePlayer != null)
        {
            RegisterIncident(new PrototypeIncident(PrototypeIncidentType.HostileRobotEvent, center, 1.2f, activePlayer.gameObject, gameObject));
        }
        return ActiveHostileRobots > 0;
    }

    public PrototypePoliceUnit SpawnPoliceUnit(PrototypePoliceRole role, Vector3 position)
    {
        CleanupLists();
        if (config == null || ActivePoliceUnits >= config.policeMaxActiveUnits)
        {
            return null;
        }

        var spawnPosition = FindSafeSpawnPosition(position, 1.1f);
        var objectName = role.ToString();
        var unitObject = new GameObject("AI_Police_" + objectName);
        unitObject.transform.SetParent(runtimeRoot, false);
        unitObject.transform.position = spawnPosition;
        unitObject.layer = 0;
        if (role == PrototypePoliceRole.PatrolVehicle)
        {
            var vehicleBody = unitObject.AddComponent<Rigidbody>();
            vehicleBody.mass = 1100f;
            vehicleBody.linearDamping = 0.45f;
            vehicleBody.angularDamping = 3.5f;
            vehicleBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            vehicleBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var vehicleCollider = unitObject.AddComponent<BoxCollider>();
            vehicleCollider.size = new Vector3(2f, 1.2f, 3.6f);
            vehicleCollider.center = new Vector3(0f, 0.6f, 0f);
        }
        else if (role != PrototypePoliceRole.PoliceDrone)
        {
            unitObject.AddComponent<CharacterController>();
        }

        var health = unitObject.AddComponent<PrototypeHealth>();
        var faction = unitObject.AddComponent<PrototypeFactionMember>();
        faction.Configure(PrototypeFaction.Police);
        var unit = unitObject.AddComponent<PrototypePoliceUnit>();
        BuildPoliceVisual(unitObject.transform, role);
        unit.Configure(config, this, role, spawnPosition);
        policeUnits.Add(unit);
        return unit;
    }

    public PrototypeHostileRobot SpawnHostileRobot(PrototypeHostileRobotRole role, Vector3 position)
    {
        CleanupLists();
        if (config == null || ActiveHostileRobots >= config.hostileRobotMaxActiveUnits)
        {
            return null;
        }

        var spawnPosition = FindSafeSpawnPosition(position, role == PrototypeHostileRobotRole.HeavyMech || role == PrototypeHostileRobotRole.Boss ? 1.8f : 1.05f);
        var objectName = role.ToString();
        var robotObject = new GameObject("AI_HostileRobot_" + objectName);
        robotObject.transform.SetParent(runtimeRoot, false);
        robotObject.transform.position = spawnPosition;
        robotObject.layer = 0;
        robotObject.AddComponent<CharacterController>();
        robotObject.AddComponent<PrototypeHealth>();
        var faction = robotObject.AddComponent<PrototypeFactionMember>();
        faction.Configure(PrototypeFaction.HostileRobot);
        var robot = robotObject.AddComponent<PrototypeHostileRobot>();
        BuildHostileVisual(robotObject.transform, role);
        robot.Configure(config, this, role);
        hostileRobots.Add(robot);
        return robot;
    }

    public void DespawnPoliceUnit(PrototypePoliceUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        policeUnits.Remove(unit);
        if (Application.isPlaying)
        {
            Destroy(unit.gameObject);
        }
    }

    public void DespawnHostileRobot(PrototypeHostileRobot robot)
    {
        if (robot == null)
        {
            return;
        }

        hostileRobots.Remove(robot);
        if (Application.isPlaying)
        {
            Destroy(robot.gameObject);
        }
    }

    public void NotifyPoliceUnitDefeated(PrototypePoliceUnit unit)
    {
        policeUnits.Remove(unit);
    }

    public void NotifyHostileRobotDefeated(PrototypeHostileRobot robot)
    {
        hostileRobots.Remove(robot);
        hostileDefeated++;
    }

    public void ResetEncounters()
    {
        for (var i = policeUnits.Count - 1; i >= 0; i--)
        {
            if (policeUnits[i] != null)
            {
                Destroy(policeUnits[i].gameObject);
            }
        }

        for (var i = hostileRobots.Count - 1; i >= 0; i--)
        {
            if (hostileRobots[i] != null)
            {
                Destroy(hostileRobots[i].gameObject);
            }
        }

        policeUnits.Clear();
        hostileRobots.Clear();
        hostileDefeated = 0;
        reinforcementsSpawned = 0;
        Wanted.Reset();
    }

    private void DispatchResponse()
    {
        var target = PrototypeGameManager.Instance != null ? PrototypeGameManager.Instance.ActiveTransform : null;
        var center = target != null ? target.position : lastIncidentPosition;
        var desired = Wanted.Level == PrototypeWantedLevel.MinorIncident ? 1
            : Wanted.Level == PrototypeWantedLevel.ActivePursuit ? 2
            : Wanted.Level == PrototypeWantedLevel.CoordinatedResponse ? 3
            : Wanted.Level == PrototypeWantedLevel.AdvancedTactical ? 4
            : 5;
        desired = Mathf.Min(desired, config.policeMaxActiveUnits);
        while (ActivePoliceUnits < desired && reinforcementsSpawned < config.policeMaxReinforcementsPerIncident)
        {
            var index = ActivePoliceUnits + reinforcementsSpawned;
            var role = ChoosePoliceRole(index);
            var angle = (index * 63f + 30f) * Mathf.Deg2Rad;
            var position = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * config.policeSpawnRadius;
            if (SpawnPoliceUnit(role, position) == null)
            {
                break;
            }

            reinforcementsSpawned++;
        }
    }

    private PrototypePoliceRole ChoosePoliceRole(int index)
    {
        if (Wanted.Level >= PrototypeWantedLevel.AdvancedTactical && index % 3 == 0)
        {
            return PrototypePoliceRole.TacticalRobot;
        }

        if (Wanted.Level >= PrototypeWantedLevel.CoordinatedResponse && index % 2 == 0)
        {
            return PrototypePoliceRole.PatrolRobot;
        }

        if (Wanted.Level >= PrototypeWantedLevel.AdvancedTactical && index % 4 == 1)
        {
            return PrototypePoliceRole.PoliceDrone;
        }

        return index % 2 == 0 ? PrototypePoliceRole.PatrolVehicle : PrototypePoliceRole.PatrolOfficer;
    }

    private int CountPursuits()
    {
        var count = 0;
        for (var i = 0; i < policeUnits.Count; i++)
        {
            if (policeUnits[i] != null && policeUnits[i].IsAlive && policeUnits[i].IsPursuing)
            {
                count++;
            }
        }

        return count;
    }

    private void OnWantedLevelChanged(PrototypeWantedLevel level)
    {
        if (level == PrototypeWantedLevel.Clear)
        {
            reinforcementsSpawned = 0;
        }
    }

    private void CleanupLists()
    {
        policeUnits.RemoveAll(unit => unit == null);
        hostileRobots.RemoveAll(robot => robot == null);
    }

    private Vector3 FindSafeSpawnPosition(Vector3 requested, float fallbackHeight)
    {
        var position = requested;
        if (!PrototypeAiUtility.IsValidPosition(position) || !PrototypeAiUtility.FindGround(position, 20f, out var ground))
        {
            position = new Vector3(Mathf.Clamp(requested.x, -config.arenaHalfExtent + 2f, config.arenaHalfExtent - 2f), fallbackHeight, Mathf.Clamp(requested.z, -config.arenaHalfExtent + 2f, config.arenaHalfExtent - 2f));
        }
        else
        {
            position = ground + Vector3.up * fallbackHeight;
        }

        for (var i = 0; i < 6; i++)
        {
            if (!Physics.CheckSphere(position, 0.85f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return position;
            }

            position += new Vector3(2f + i, 0f, 1.5f);
        }

        return position;
    }

    private static void BuildPoliceVisual(Transform root, PrototypePoliceRole role)
    {
        var material = CreateMaterial(role == PrototypePoliceRole.TacticalRobot || role == PrototypePoliceRole.PatrolRobot ? new Color(0.12f, 0.3f, 0.48f) : new Color(0.08f, 0.18f, 0.3f));
        var accent = CreateMaterial(new Color(0.25f, 0.65f, 1f), true);
        if (role == PrototypePoliceRole.PatrolVehicle)
        {
            CreatePrimitive(PrimitiveType.Cube, "PoliceCruiserBody", root, new Vector3(0f, 0.55f, 0f), new Vector3(1.8f, 0.55f, 3.2f), material);
            CreatePrimitive(PrimitiveType.Cube, "PoliceCruiserCabin", root, new Vector3(0f, 1.05f, -0.2f), new Vector3(1.35f, 0.65f, 1.4f), accent);
        }
        else if (role == PrototypePoliceRole.PoliceDrone)
        {
            CreatePrimitive(PrimitiveType.Sphere, "PoliceDroneBody", root, Vector3.zero, Vector3.one * 0.8f, material);
            CreatePrimitive(PrimitiveType.Cube, "PoliceDroneBeacon", root, Vector3.up * 0.32f, new Vector3(0.25f, 0.18f, 0.25f), accent);
        }
        else
        {
            var scale = role == PrototypePoliceRole.TacticalRobot ? new Vector3(1.2f, 1.5f, 1f) : new Vector3(0.9f, 1.1f, 0.8f);
            CreatePrimitive(PrimitiveType.Capsule, "PoliceRobotBody", root, Vector3.up * scale.y, scale, material);
            CreatePrimitive(PrimitiveType.Sphere, "PoliceRobotVisor", root, Vector3.up * (scale.y * 1.65f), Vector3.one * 0.28f, accent);
        }
    }

    private static void BuildHostileVisual(Transform root, PrototypeHostileRobotRole role)
    {
        var body = CreateMaterial(role == PrototypeHostileRobotRole.Scout ? new Color(0.55f, 0.12f, 0.16f) : role == PrototypeHostileRobotRole.RangedAssault ? new Color(0.48f, 0.2f, 0.08f) : new Color(0.23f, 0.08f, 0.1f));
        var accent = CreateMaterial(new Color(1f, 0.2f, 0.05f), true);
        var scale = role == PrototypeHostileRobotRole.Scout ? new Vector3(0.75f, 0.9f, 0.75f) : role == PrototypeHostileRobotRole.RangedAssault ? new Vector3(0.95f, 1.25f, 0.9f) : new Vector3(1.45f, 1.8f, 1.25f);
        CreatePrimitive(PrimitiveType.Capsule, "HostileRobotBody", root, Vector3.up * scale.y, scale, body);
        CreatePrimitive(PrimitiveType.Sphere, "HostileRobotCore", root, Vector3.up * (scale.y * 1.55f), Vector3.one * (role == PrototypeHostileRobotRole.Scout ? 0.2f : 0.32f), accent);
        if (role == PrototypeHostileRobotRole.HeavyMech || role == PrototypeHostileRobotRole.Boss)
        {
            CreatePrimitive(PrimitiveType.Cube, "HostileRobotShoulderL", root, new Vector3(-scale.x, scale.y, 0f), new Vector3(0.55f, 0.55f, 0.7f), body);
            CreatePrimitive(PrimitiveType.Cube, "HostileRobotShoulderR", root, new Vector3(scale.x, scale.y, 0f), new Vector3(0.55f, 0.55f, 0.7f), body);
        }
    }

    private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var objectInstance = GameObject.CreatePrimitive(type);
        objectInstance.name = name;
        objectInstance.transform.SetParent(parent, false);
        objectInstance.transform.localPosition = position;
        objectInstance.transform.localScale = scale;
        Object.Destroy(objectInstance.GetComponent<Collider>());
        objectInstance.GetComponent<Renderer>().sharedMaterial = material;
        return objectInstance;
    }

    private static Material CreateMaterial(Color color, bool emission = false)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { color = color };
        if (emission)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 3f);
        }

        return material;
    }

    private static int CountAlive<T>(List<T> list) where T : Object
    {
        var count = 0;
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private void OnDestroy()
    {
        Wanted.LevelChanged -= OnWantedLevelChanged;
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
