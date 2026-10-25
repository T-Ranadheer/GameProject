using UnityEngine;

public sealed class PrototypeBootstrap : MonoBehaviour
{
    private void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        var config = PrototypeGameConfig.CreateDefault();
        var manager = gameObject.AddComponent<PrototypeGameManager>();
        var world = PrototypeWorldBuilder.Build(config);
        var aiCoordinator = gameObject.AddComponent<PrototypePoliceCoordinator>();
        aiCoordinator.Configure(config, world.Root);
        var aiMission = gameObject.AddComponent<PrototypeAiEncounterMission>();
        aiMission.Configure(aiCoordinator);
        var player = PrototypeWorldBuilder.CreatePlayer(config, world.Root);
        var electricHero = PrototypeWorldBuilder.CreateElectricHero(config, world.Root);
        var stoneGiant = PrototypeWorldBuilder.CreateStoneGiant(config, world.Root);
        var robot = PrototypeWorldBuilder.CreateRobot(config, world.Root);
        var helicopter = PrototypeWorldBuilder.CreateHelicopterTransformer(config, world.Root);
        var tank = PrototypeWorldBuilder.CreateTankTransformer(config, world.Root);
        var motorcycle = PrototypeWorldBuilder.CreateMotorcycleTransformer(config, world.Root);
        var jet = PrototypeWorldBuilder.CreateJetTransformer(config, world.Root);
        var playerHealth = player.GetComponent<PrototypeHealth>();
        var mission = gameObject.AddComponent<PrototypeMissionController>();

        foreach (var position in new[]
        {
            new Vector3(-10f, 1.05f, 19f),
            new Vector3(10f, 1.05f, 19f),
            new Vector3(-9f, 1.05f, 8f),
            new Vector3(9f, 1.05f, 8f)
        })
        {
            world.Enemies.Add(PrototypeWorldBuilder.CreateEnemy(position, player, mission, config, world.Root, world.Enemies.Count + 1));
        }

        mission.Configure(player, playerHealth, world.Beacon, world.Extraction, world.Enemies, config);
        manager.Configure(player, playerHealth, mission);
        manager.ConfigureCharacters(electricHero, stoneGiant);

        var cameraRig = CreateCamera(player, config);
        manager.ConfigureRobot(robot, cameraRig);
        manager.ConfigureTransformers(new[] { helicopter, tank, motorcycle, jet }, cameraRig);
        aiCoordinator.SpawnPoliceUnit(PrototypePoliceRole.PatrolOfficer, new Vector3(-18f, 1.05f, -15f));
        aiMission.StartHostileRobotChallenge(new Vector3(15f, 1.05f, 16f));
        var hud = gameObject.AddComponent<PrototypeHud>();
        hud.Configure(manager);
        PrototypeTouchControls.Create();
        CreateLighting(world.Root);
    }

    private static PrototypeCameraRig CreateCamera(PrototypePlayerController player, PrototypeGameConfig config)
    {
        var cameraObject = new GameObject("GameplayCamera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 68f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 180f;
        cameraObject.AddComponent<AudioListener>();
        var rig = cameraObject.AddComponent<PrototypeCameraRig>();
        rig.Configure(player.transform, config);
        cameraObject.transform.position = player.transform.position + new Vector3(0f, 3f, -7f);
        return rig;
    }

    private static void CreateLighting(Transform parent)
    {
        var lightObject = new GameObject("Sun_District");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.35f;
        light.color = new Color(0.75f, 0.84f, 1f);
        RenderSettings.ambientLight = new Color(0.12f, 0.16f, 0.22f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.08f, 0.11f, 0.17f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.008f;
    }
}
