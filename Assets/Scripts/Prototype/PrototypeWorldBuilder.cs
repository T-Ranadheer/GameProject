using System.Collections.Generic;
using UnityEngine;

public sealed class PrototypeWorld
{
    public Transform Root;
    public Transform Beacon;
    public Transform Extraction;
    public readonly List<PrototypeEnemy> Enemies = new List<PrototypeEnemy>();
}

public static class PrototypeWorldBuilder
{
    public static PrototypeWorld Build(PrototypeGameConfig config)
    {
        var world = new PrototypeWorld
        {
            Root = new GameObject("MetroBlock_Greybox").transform
        };

        var groundMaterial = CreateMaterial(new Color(0.08f, 0.09f, 0.12f));
        var roadMaterial = CreateMaterial(new Color(0.025f, 0.03f, 0.04f));
        var sidewalkMaterial = CreateMaterial(new Color(0.28f, 0.3f, 0.34f));
        var buildingMaterial = CreateMaterial(new Color(0.16f, 0.22f, 0.3f));
        var buildingAccent = CreateMaterial(new Color(0.42f, 0.2f, 0.15f));

        CreateCube("Ground", world.Root, new Vector3(0f, -0.55f, 0f), new Vector3(80f, 1f, 80f), groundMaterial);
        CreateCube("NorthRoad", world.Root, new Vector3(0f, -0.02f, 14f), new Vector3(72f, 0.12f, 8f), roadMaterial);
        CreateCube("SouthRoad", world.Root, new Vector3(0f, -0.02f, -14f), new Vector3(72f, 0.12f, 8f), roadMaterial);
        CreateCube("EastRoad", world.Root, new Vector3(14f, -0.02f, 0f), new Vector3(8f, 0.12f, 72f), roadMaterial);
        CreateCube("WestRoad", world.Root, new Vector3(-14f, -0.02f, 0f), new Vector3(8f, 0.12f, 72f), roadMaterial);

        for (var x = -30; x <= 30; x += 6)
        {
            CreateCube("NorthSidewalk", world.Root, new Vector3(x, 0.08f, 20f), new Vector3(5.2f, 0.18f, 3f), sidewalkMaterial);
            CreateCube("SouthSidewalk", world.Root, new Vector3(x, 0.08f, -20f), new Vector3(5.2f, 0.18f, 3f), sidewalkMaterial);
        }

        for (var z = -30; z <= 30; z += 6)
        {
            CreateCube("EastSidewalk", world.Root, new Vector3(20f, 0.08f, z), new Vector3(3f, 0.18f, 5.2f), sidewalkMaterial);
            CreateCube("WestSidewalk", world.Root, new Vector3(-20f, 0.08f, z), new Vector3(3f, 0.18f, 5.2f), sidewalkMaterial);
        }

        var buildingPositions = new[]
        {
            new Vector3(-28f, 4f, 28f), new Vector3(-18f, 6f, 29f), new Vector3(18f, 5f, 29f), new Vector3(28f, 3f, 28f),
            new Vector3(-28f, 5f, 6f), new Vector3(28f, 7f, 6f), new Vector3(-28f, 4f, -7f), new Vector3(28f, 5f, -8f),
            new Vector3(-28f, 6f, -28f), new Vector3(-18f, 4f, -29f), new Vector3(18f, 7f, -29f), new Vector3(28f, 5f, -28f)
        };

        for (var i = 0; i < buildingPositions.Length; i++)
        {
            var position = buildingPositions[i];
            var size = new Vector3(i % 2 == 0 ? 6f : 8f, position.y * 2f, i % 3 == 0 ? 6f : 8f);
            CreateCube("Building_" + i, world.Root, position, size, i % 3 == 0 ? buildingAccent : buildingMaterial);
        }

        CreateCube("ArenaNorthWall", world.Root, new Vector3(0f, 2f, 38f), new Vector3(76f, 4f, 0.8f), buildingMaterial);
        CreateCube("ArenaSouthWall", world.Root, new Vector3(0f, 2f, -38f), new Vector3(76f, 4f, 0.8f), buildingMaterial);
        CreateCube("ArenaEastWall", world.Root, new Vector3(38f, 2f, 0f), new Vector3(0.8f, 4f, 76f), buildingMaterial);
        CreateCube("ArenaWestWall", world.Root, new Vector3(-38f, 2f, 0f), new Vector3(0.8f, 4f, 76f), buildingMaterial);

        CreateCube("CargoObstacleA", world.Root, new Vector3(-7f, 0.7f, 3f), new Vector3(2.5f, 1.4f, 2.5f), buildingAccent);
        CreateCube("CargoObstacleB", world.Root, new Vector3(8f, 1.1f, -2f), new Vector3(3f, 2.2f, 2f), buildingAccent);


        world.Beacon = CreateMarker("SignalBeacon", world.Root, new Vector3(0f, 0.5f, 24f), new Color(0.1f, 0.8f, 1f));
        world.Extraction = CreateMarker("ExtractionPoint", world.Root, new Vector3(0f, 0.5f, -24f), new Color(0.25f, 1f, 0.35f));
        return world;
    }

    public static PrototypePlayerController CreatePlayer(PrototypeGameConfig config, Transform parent)
    {
        var playerObject = new GameObject("NovaHero_Player");
        playerObject.layer = 2;
        playerObject.transform.SetParent(parent, false);
        playerObject.transform.position = new Vector3(0f, 1.05f, -28f);

        var controller = playerObject.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.42f;
        controller.center = Vector3.up * 0.9f;
        var health = playerObject.AddComponent<PrototypeHealth>();
        health.Initialize(config.playerHealth);
        health.ConfigureResistances(1f, 1f, 1f, 1f);
        playerObject.AddComponent<PrototypeFactionMember>().Configure(PrototypeFaction.Player);
        var profile = playerObject.AddComponent<PrototypeCharacterProfile>();
        profile.Configure(PrototypeCharacterId.OriginalHero, "Nova Hero", config.cameraDistance, config.cameraHeight, 1.8f, 0.42f);
        var player = playerObject.AddComponent<PrototypePlayerController>();
        player.Configure(config);
        var ability = playerObject.AddComponent<PrototypePlayerAbility>();
        ability.Configure(config);

        var presentationRoot = CreatePresentationRoot("NovaHero_Presentation", playerObject.transform);
        CreateVisualCapsule("HeroSuit", presentationRoot, new Vector3(0f, 0.9f, 0f), new Vector3(0.8f, 0.9f, 0.8f), CreateMaterial(new Color(0.08f, 0.45f, 0.82f)));
        CreateVisualSphere("EnergyCore", presentationRoot, new Vector3(0f, 1.15f, 0.38f), Vector3.one * 0.22f, CreateMaterial(new Color(0.1f, 0.9f, 1f), true));
        var presentation = playerObject.AddComponent<PrototypeProceduralPresentation>();
        presentation.Configure(presentationRoot, false);
        var audio = playerObject.AddComponent<PrototypeAudioFeedback>();
        audio.Configure(1.05f);
        return player;
    }

    public static PrototypeElectricController CreateElectricHero(PrototypeGameConfig config, Transform parent)
    {
        var electricObject = new GameObject("VoltWarden_ElectricHero");
        electricObject.layer = 2;
        electricObject.transform.SetParent(parent, false);
        electricObject.transform.position = new Vector3(0f, 1.05f, -28f);
        var controller = electricObject.AddComponent<CharacterController>();
        var health = electricObject.AddComponent<PrototypeHealth>();
        health.Initialize(config.electricHealth);
        health.ConfigureResistances(1f, 0.95f, 0.9f, 1f);
        electricObject.AddComponent<PrototypeFactionMember>().Configure(PrototypeFaction.Player);
        electricObject.AddComponent<PrototypeEnergy>();
        var profile = electricObject.AddComponent<PrototypeCharacterProfile>();
        profile.Configure(PrototypeCharacterId.ElectricHero, "Volt Warden", config.cameraDistance, config.cameraHeight, 1.8f, 0.42f);
        var electric = electricObject.AddComponent<PrototypeElectricController>();
        electric.Configure(config);
        var abilities = electricObject.AddComponent<PrototypeElectricAbilities>();
        abilities.Configure(config);

        var presentationRoot = CreatePresentationRoot("VoltWarden_Presentation", electricObject.transform);
        var suit = CreateMaterial(new Color(0.12f, 0.08f, 0.28f));
        var arc = CreateMaterial(new Color(0.2f, 0.85f, 1f), true);
        CreateVisualCapsule("VoltWardenSuit", presentationRoot, new Vector3(0f, 0.9f, 0f), new Vector3(0.84f, 0.92f, 0.84f), suit);
        CreateVisualSphere("VoltCore", presentationRoot, new Vector3(0f, 1.18f, 0.4f), Vector3.one * 0.24f, arc);
        CreateCube("VoltShoulderL", presentationRoot, new Vector3(-0.62f, 1.43f, 0f), new Vector3(0.25f, 0.42f, 0.42f), arc);
        CreateCube("VoltShoulderR", presentationRoot, new Vector3(0.62f, 1.43f, 0f), new Vector3(0.25f, 0.42f, 0.42f), arc);
        var electricPresentation = electricObject.AddComponent<PrototypeProceduralPresentation>();
        electricPresentation.Configure(presentationRoot, false);
        var electricAudio = electricObject.AddComponent<PrototypeAudioFeedback>();
        electricAudio.Configure(1.2f);
        return electric;
    }

    public static PrototypeStoneGiantController CreateStoneGiant(PrototypeGameConfig config, Transform parent)
    {
        var giantObject = new GameObject("StoneColossus_Giant");
        giantObject.layer = 2;
        giantObject.transform.SetParent(parent, false);
        giantObject.transform.position = new Vector3(0f, 1.05f, -28f);
        var controller = giantObject.AddComponent<CharacterController>();
        var health = giantObject.AddComponent<PrototypeHealth>();
        health.Initialize(config.giantHealth);
        health.ConfigureResistances(0.85f, 1f, 0.9f, 0.68f);
        giantObject.AddComponent<PrototypeFactionMember>().Configure(PrototypeFaction.Player);
        var profile = giantObject.AddComponent<PrototypeCharacterProfile>();
        profile.Configure(PrototypeCharacterId.StoneGiant, "Stone Colossus", config.giantCameraDistance, config.giantCameraHeight, config.giantControllerHeight, config.giantControllerRadius);
        var giant = giantObject.AddComponent<PrototypeStoneGiantController>();
        giant.Configure(config);
        var combat = giantObject.AddComponent<PrototypeStoneGiantCombat>();
        combat.Configure(config);

        var presentationRoot = CreatePresentationRoot("StoneColossus_Presentation", giantObject.transform);
        var stone = CreateMaterial(new Color(0.34f, 0.3f, 0.27f));
        var moss = CreateMaterial(new Color(0.2f, 0.45f, 0.28f));
        CreateVisualCapsule("GiantTorso", presentationRoot, new Vector3(0f, 1.95f, 0f), new Vector3(1.7f, 2.0f, 1.25f), stone);
        CreateVisualSphere("GiantHead", presentationRoot, new Vector3(0f, 3.6f, 0f), Vector3.one * 0.95f, stone);
        CreateCube("GiantArmL", presentationRoot, new Vector3(-1.35f, 1.9f, 0f), new Vector3(0.7f, 1.7f, 0.75f), stone);
        CreateCube("GiantArmR", presentationRoot, new Vector3(1.35f, 1.9f, 0f), new Vector3(0.7f, 1.7f, 0.75f), stone);
        CreateCube("GiantLegL", presentationRoot, new Vector3(-0.62f, 0.65f, 0f), new Vector3(0.78f, 1.3f, 0.85f), stone);
        CreateCube("GiantLegR", presentationRoot, new Vector3(0.62f, 0.65f, 0f), new Vector3(0.78f, 1.3f, 0.85f), stone);
        CreateVisualSphere("GiantMossMark", presentationRoot, new Vector3(0f, 3.62f, 0.8f), Vector3.one * 0.18f, moss);
        var giantPresentation = giantObject.AddComponent<PrototypeProceduralPresentation>();
        giantPresentation.Configure(presentationRoot, true);
        var giantAudio = giantObject.AddComponent<PrototypeAudioFeedback>();
        giantAudio.Configure(0.72f);
        return giant;
    }

    public static PrototypeRobotController CreateRobot(PrototypeGameConfig config, Transform parent)
    {
        var robotObject = new GameObject("AtlasRunner_Transformer");
        robotObject.transform.SetParent(parent, false);
        robotObject.transform.position = new Vector3(5f, 1.1f, -28f);
        robotObject.SetActive(false);

        var characterController = robotObject.AddComponent<CharacterController>();
        var rigidbody = robotObject.AddComponent<Rigidbody>();
        var boxCollider = robotObject.AddComponent<BoxCollider>();
        var health = robotObject.AddComponent<PrototypeHealth>();
        health.Initialize(config.playerHealth * 1.35f);
        health.ConfigureResistances(0.82f, 0.9f, 1f, 0.76f);
        robotObject.AddComponent<PrototypeFactionMember>().Configure(PrototypeFaction.Player);
        var profile = robotObject.AddComponent<PrototypeCharacterProfile>();
        profile.Configure(PrototypeCharacterId.AtlasRunner, "Atlas Runner", config.robotCameraDistance, config.robotCameraHeight, 2.1f, 0.48f);
        var robot = robotObject.AddComponent<PrototypeRobotController>();
        var car = robotObject.AddComponent<PrototypeCarController>();
        var combat = robotObject.AddComponent<PrototypeRobotCombat>();

        var robotVisual = new GameObject("AtlasRunner_RobotForm").transform;
        robotVisual.SetParent(robotObject.transform, false);
        CreateVisualCapsule("ArmoredTorso", robotVisual, new Vector3(0f, 1.1f, 0f), new Vector3(1.05f, 1.2f, 0.82f), CreateMaterial(new Color(0.18f, 0.48f, 0.58f)));
        CreateVisualSphere("CommandCore", robotVisual, new Vector3(0f, 1.45f, 0.46f), Vector3.one * 0.25f, CreateMaterial(new Color(1f, 0.56f, 0.08f), true));
        CreateCube("LeftShoulder", robotVisual, new Vector3(-0.75f, 1.65f, 0f), new Vector3(0.5f, 0.45f, 0.72f), CreateMaterial(new Color(0.12f, 0.28f, 0.35f)));
        CreateCube("RightShoulder", robotVisual, new Vector3(0.75f, 1.65f, 0f), new Vector3(0.5f, 0.45f, 0.72f), CreateMaterial(new Color(0.12f, 0.28f, 0.35f)));
        CreateCube("LeftLeg", robotVisual, new Vector3(-0.38f, 0.35f, 0f), new Vector3(0.42f, 0.75f, 0.55f), CreateMaterial(new Color(0.1f, 0.3f, 0.38f)));
        CreateCube("RightLeg", robotVisual, new Vector3(0.38f, 0.35f, 0f), new Vector3(0.42f, 0.75f, 0.55f), CreateMaterial(new Color(0.1f, 0.3f, 0.38f)));

        var carVisual = new GameObject("AtlasRunner_CarForm").transform;
        carVisual.SetParent(robotObject.transform, false);
        CreateCube("CarChassis", carVisual, new Vector3(0f, 0.55f, 0f), new Vector3(2.1f, 0.55f, 3.75f), CreateMaterial(new Color(0.08f, 0.3f, 0.38f)));
        CreateCube("CarCabin", carVisual, new Vector3(0f, 1.05f, -0.2f), new Vector3(1.55f, 0.8f, 1.7f), CreateMaterial(new Color(0.12f, 0.18f, 0.24f)));
        CreateCube("CarNose", carVisual, new Vector3(0f, 0.75f, 1.25f), new Vector3(1.75f, 0.4f, 1.05f), CreateMaterial(new Color(0.18f, 0.48f, 0.58f)));
        CreateSphereLight("CarCore", carVisual, new Vector3(0f, 0.65f, 1.78f), Vector3.one * 0.24f, CreateMaterial(new Color(1f, 0.56f, 0.08f), true));

        var wheelColliders = new WheelCollider[4];
        var wheelVisuals = new Transform[4];
        var wheelPositions = new[]
        {
            new Vector3(-1.05f, 0.38f, 1.28f), new Vector3(1.05f, 0.38f, 1.28f),
            new Vector3(-1.05f, 0.38f, -1.28f), new Vector3(1.05f, 0.38f, -1.28f)
        };
        for (var i = 0; i < wheelPositions.Length; i++)
        {
            var wheelObject = new GameObject("WheelCollider_" + i);
            wheelObject.transform.SetParent(robotObject.transform, false);
            wheelObject.transform.localPosition = wheelPositions[i];
            wheelColliders[i] = wheelObject.AddComponent<WheelCollider>();
            wheelVisuals[i] = CreatePrimitiveChild(PrimitiveType.Cylinder, "WheelVisual_" + i, carVisual, wheelPositions[i] - Vector3.up * 0.38f, new Vector3(0.42f, 0.18f, 0.42f), CreateMaterial(new Color(0.015f, 0.02f, 0.025f))).transform;
            wheelVisuals[i].localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        car.Configure(config, wheelColliders, wheelVisuals);
        combat.Configure(config);
        robot.Configure(config, robotVisual, carVisual);
        var robotPresentation = robotObject.AddComponent<PrototypeProceduralPresentation>();
        robotPresentation.Configure(robotVisual, true, true);
        var robotAudio = robotObject.AddComponent<PrototypeAudioFeedback>();
        robotAudio.Configure(0.85f);
        return robot;
    }

    public static PrototypeTransformerController CreateHelicopterTransformer(PrototypeGameConfig config, Transform parent)
    {
        var root = CreateTransformerRoot("AeroViper_HelicopterTransformer", new Vector3(7f, 1.1f, -28f), parent, config.helicopterHealth);
        var controller = root.AddComponent<PrototypeTransformerController>();
        var movement = root.AddComponent<PrototypeHelicopterController>();
        var combat = root.AddComponent<PrototypeTransformerCombat>();
        var robotVisual = CreatePresentationRoot("AeroViper_RobotForm", root.transform);
        var vehicleVisual = CreatePresentationRoot("AeroViper_HelicopterForm", root.transform);
        var armor = CreateMaterial(new Color(0.16f, 0.48f, 0.42f));
        var rotorMaterial = CreateMaterial(new Color(0.12f, 0.22f, 0.27f));
        var glow = CreateMaterial(new Color(0.18f, 0.9f, 0.75f), true);
        CreateVisualCapsule("AeroViperTorso", robotVisual, new Vector3(0f, 1.05f, 0f), new Vector3(0.95f, 1.1f, 0.78f), armor);
        CreateVisualSphere("AeroViperHead", robotVisual, new Vector3(0f, 1.85f, 0f), Vector3.one * 0.35f, glow);
        CreateCube("AeroViperWingArmL", robotVisual, new Vector3(-0.9f, 1.45f, 0f), new Vector3(0.75f, 0.25f, 0.9f), armor);
        CreateCube("AeroViperWingArmR", robotVisual, new Vector3(0.9f, 1.45f, 0f), new Vector3(0.75f, 0.25f, 0.9f), armor);
        CreateCube("AeroViperHelicopterBody", vehicleVisual, new Vector3(0f, 0.7f, 0f), new Vector3(2.6f, 0.9f, 3.4f), armor);
        CreateVisualSphere("AeroViperCockpit", vehicleVisual, new Vector3(0f, 0.95f, 1.15f), new Vector3(1.55f, 0.6f, 1.1f), glow);
        var rotor = CreatePrimitiveChild(PrimitiveType.Cylinder, "AeroViperRotor", vehicleVisual, new Vector3(0f, 1.35f, 0f), new Vector3(2.4f, 0.06f, 2.4f), rotorMaterial).transform;
        rotor.gameObject.AddComponent<PrototypeRotorAnimator>().Configure(220f);
        CreateCube("AeroViperTail", vehicleVisual, new Vector3(0f, 0.78f, -2.2f), new Vector3(0.4f, 0.35f, 1.8f), armor);
        CreateCube("AeroViperTailRotor", vehicleVisual, new Vector3(0.42f, 0.9f, -2.9f), new Vector3(0.08f, 1.1f, 0.08f), glow);
        movement.Configure(config, root.GetComponent<Rigidbody>(), root.GetComponent<BoxCollider>());
        controller.Configure(config, PrototypeCharacterId.HelicopterTransformer, "Aero Viper", PrototypeRobotForm.Helicopter, robotVisual, vehicleVisual, 2.1f, 0.48f);
        combat.ConfigureMuzzles(vehicleVisual.Find("AeroViperCockpit"), vehicleVisual.Find("AeroViperTail"));
        AddTransformerPresentation(root, robotVisual, 0.95f);
        root.SetActive(false);
        return controller;
    }

    public static PrototypeTransformerController CreateTankTransformer(PrototypeGameConfig config, Transform parent)
    {
        var root = CreateTransformerRoot("BastionTread_TankTransformer", new Vector3(-7f, 1.1f, -28f), parent, config.tankHealth);
        var controller = root.AddComponent<PrototypeTransformerController>();
        var movement = root.AddComponent<PrototypeTankController>();
        var combat = root.AddComponent<PrototypeTransformerCombat>();
        var robotVisual = CreatePresentationRoot("BastionTread_RobotForm", root.transform);
        var vehicleVisual = CreatePresentationRoot("BastionTread_TankForm", root.transform);
        var armor = CreateMaterial(new Color(0.34f, 0.38f, 0.42f));
        var trim = CreateMaterial(new Color(0.78f, 0.28f, 0.1f), true);
        CreateVisualCapsule("BastionTreadTorso", robotVisual, new Vector3(0f, 1.1f, 0f), new Vector3(1.2f, 1.3f, 0.95f), armor);
        CreateCube("BastionTreadShoulderL", robotVisual, new Vector3(-0.9f, 1.55f, 0f), new Vector3(0.6f, 0.65f, 0.8f), trim);
        CreateCube("BastionTreadShoulderR", robotVisual, new Vector3(0.9f, 1.55f, 0f), new Vector3(0.6f, 0.65f, 0.8f), trim);
        CreateCube("BastionTreadHull", vehicleVisual, new Vector3(0f, 0.7f, 0f), new Vector3(2.8f, 1.1f, 3.7f), armor);
        var turret = CreatePrimitiveChild(PrimitiveType.Cylinder, "BastionTreadTurret", vehicleVisual, new Vector3(0f, 1.35f, 0.1f), new Vector3(1.35f, 0.35f, 1.35f), trim).transform;
        var barrel = CreateCube("BastionTreadCannon", turret, new Vector3(0f, 0.05f, 1.35f), new Vector3(0.28f, 0.28f, 2.7f), armor).transform;
        var barrelTip = new GameObject("BastionTreadBarrelTip").transform;
        barrelTip.SetParent(barrel, false);
        barrelTip.localPosition = new Vector3(0f, 0f, 1.5f);
        CreateCube("BastionTreadTrackL", vehicleVisual, new Vector3(-1.75f, 0.48f, 0f), new Vector3(0.45f, 0.72f, 3.85f), trim);
        CreateCube("BastionTreadTrackR", vehicleVisual, new Vector3(1.75f, 0.48f, 0f), new Vector3(0.45f, 0.72f, 3.85f), trim);
        movement.Configure(config, root.GetComponent<Rigidbody>(), root.GetComponent<BoxCollider>());
        movement.ConfigureTurret(turret);
        controller.Configure(config, PrototypeCharacterId.TankTransformer, "Bastion Tread", PrototypeRobotForm.Tank, robotVisual, vehicleVisual, 2.35f, 0.56f);
        combat.ConfigureMuzzles(barrelTip, barrelTip);
        AddTransformerPresentation(root, robotVisual, 1.2f);
        root.SetActive(false);
        return controller;
    }

    public static PrototypeTransformerController CreateMotorcycleTransformer(PrototypeGameConfig config, Transform parent)
    {
        var root = CreateTransformerRoot("RazorCircuit_MotorcycleTransformer", new Vector3(11f, 1.1f, -28f), parent, config.motorcycleHealth);
        var controller = root.AddComponent<PrototypeTransformerController>();
        var movement = root.AddComponent<PrototypeMotorcycleController>();
        var combat = root.AddComponent<PrototypeTransformerCombat>();
        var robotVisual = CreatePresentationRoot("RazorCircuit_RobotForm", root.transform);
        var vehicleVisual = CreatePresentationRoot("RazorCircuit_MotorcycleForm", root.transform);
        var armor = CreateMaterial(new Color(0.44f, 0.12f, 0.34f));
        var glow = CreateMaterial(new Color(1f, 0.16f, 0.42f), true);
        CreateVisualCapsule("RazorCircuitTorso", robotVisual, new Vector3(0f, 1.05f, 0f), new Vector3(0.82f, 1f, 0.7f), armor);
        CreateCube("RazorCircuitBladeL", robotVisual, new Vector3(-0.75f, 1.25f, 0.25f), new Vector3(0.18f, 1.2f, 0.35f), glow);
        CreateCube("RazorCircuitBladeR", robotVisual, new Vector3(0.75f, 1.25f, 0.25f), new Vector3(0.18f, 1.2f, 0.35f), glow);
        CreateCube("RazorCircuitFrame", vehicleVisual, new Vector3(0f, 0.55f, 0f), new Vector3(0.62f, 0.55f, 2.35f), armor);
        CreateCube("RazorCircuitFront", vehicleVisual, new Vector3(0f, 0.92f, 0.7f), new Vector3(0.5f, 0.28f, 1.1f), glow);
        var wheelFrontObject = new GameObject("RazorCircuitFrontWheel");
        wheelFrontObject.transform.SetParent(root.transform, false);
        wheelFrontObject.transform.localPosition = new Vector3(0f, 0.35f, 1.2f);
        var wheelRearObject = new GameObject("RazorCircuitRearWheel");
        wheelRearObject.transform.SetParent(root.transform, false);
        wheelRearObject.transform.localPosition = new Vector3(0f, 0.35f, -1.05f);
        var frontWheel = wheelFrontObject.AddComponent<WheelCollider>();
        var rearWheel = wheelRearObject.AddComponent<WheelCollider>();
        var frontVisual = CreatePrimitiveChild(PrimitiveType.Cylinder, "RazorCircuitFrontWheelVisual", vehicleVisual, new Vector3(0f, 0.02f, 1.2f), new Vector3(0.72f, 0.14f, 0.72f), glow).transform;
        var rearVisual = CreatePrimitiveChild(PrimitiveType.Cylinder, "RazorCircuitRearWheelVisual", vehicleVisual, new Vector3(0f, 0.02f, -1.05f), new Vector3(0.78f, 0.16f, 0.78f), glow).transform;
        frontVisual.localRotation = Quaternion.Euler(0f, 0f, 90f);
        rearVisual.localRotation = Quaternion.Euler(0f, 0f, 90f);
        movement.Configure(config, root.GetComponent<Rigidbody>(), root.GetComponent<BoxCollider>());
        movement.ConfigureWheels(frontWheel, rearWheel, frontVisual, rearVisual, vehicleVisual);
        controller.Configure(config, PrototypeCharacterId.MotorcycleTransformer, "Razor Circuit", PrototypeRobotForm.Motorcycle, robotVisual, vehicleVisual, 2f, 0.44f);
        combat.ConfigureMuzzles(vehicleVisual.Find("RazorCircuitFront"), vehicleVisual.Find("RazorCircuitFrame"));
        AddTransformerPresentation(root, robotVisual, 0.78f);
        root.SetActive(false);
        return controller;
    }

    public static PrototypeTransformerController CreateJetTransformer(PrototypeGameConfig config, Transform parent)
    {
        var root = CreateTransformerRoot("SkyLance_JetTransformer", new Vector3(-11f, 1.1f, -28f), parent, config.jetHealth);
        var controller = root.AddComponent<PrototypeTransformerController>();
        var movement = root.AddComponent<PrototypeJetController>();
        var combat = root.AddComponent<PrototypeTransformerCombat>();
        var robotVisual = CreatePresentationRoot("SkyLance_RobotForm", root.transform);
        var vehicleVisual = CreatePresentationRoot("SkyLance_JetForm", root.transform);
        var armor = CreateMaterial(new Color(0.18f, 0.28f, 0.62f));
        var glow = CreateMaterial(new Color(0.32f, 0.78f, 1f), true);
        CreateVisualCapsule("SkyLanceTorso", robotVisual, new Vector3(0f, 1.08f, 0f), new Vector3(0.9f, 1.05f, 0.72f), armor);
        CreateCube("SkyLanceWingL", robotVisual, new Vector3(-0.9f, 1.3f, 0f), new Vector3(0.9f, 0.18f, 0.7f), glow);
        CreateCube("SkyLanceWingR", robotVisual, new Vector3(0.9f, 1.3f, 0f), new Vector3(0.9f, 0.18f, 0.7f), glow);
        CreateCube("SkyLanceFuselage", vehicleVisual, new Vector3(0f, 0.6f, 0f), new Vector3(1.2f, 0.72f, 4.8f), armor);
        CreateCube("SkyLanceNose", vehicleVisual, new Vector3(0f, 0.65f, 2.5f), new Vector3(0.72f, 0.5f, 1.6f), glow);
        CreateCube("SkyLanceWingL", vehicleVisual, new Vector3(-2f, 0.58f, 0.05f), new Vector3(3f, 0.16f, 1.35f), armor);
        CreateCube("SkyLanceWingR", vehicleVisual, new Vector3(2f, 0.58f, 0.05f), new Vector3(3f, 0.16f, 1.35f), armor);
        CreateCube("SkyLanceTailL", vehicleVisual, new Vector3(-0.7f, 1.1f, -2f), new Vector3(0.14f, 0.8f, 0.9f), glow);
        CreateCube("SkyLanceTailR", vehicleVisual, new Vector3(0.7f, 1.1f, -2f), new Vector3(0.14f, 0.8f, 0.9f), glow);
        movement.Configure(config, root.GetComponent<Rigidbody>(), root.GetComponent<BoxCollider>());
        controller.Configure(config, PrototypeCharacterId.JetTransformer, "Sky Lance", PrototypeRobotForm.Jet, robotVisual, vehicleVisual, 2.05f, 0.46f);
        combat.ConfigureMuzzles(vehicleVisual.Find("SkyLanceNose"), vehicleVisual.Find("SkyLanceFuselage"));
        AddTransformerPresentation(root, robotVisual, 0.9f);
        root.SetActive(false);
        return controller;
    }

    private static GameObject CreateTransformerRoot(string name, Vector3 position, Transform parent, float healthValue)
    {
        var root = new GameObject(name);
        root.layer = 2;
        root.transform.SetParent(parent, false);
        root.transform.position = position;
        root.AddComponent<CharacterController>();
        root.AddComponent<Rigidbody>();
        root.AddComponent<BoxCollider>();
        var health = root.AddComponent<PrototypeHealth>();
        health.Initialize(healthValue);
        health.ConfigureResistances(0.8f, 0.9f, 1f, 0.72f);
        root.AddComponent<PrototypeFactionMember>().Configure(PrototypeFaction.Player);
        return root;
    }

    private static void AddTransformerPresentation(GameObject root, Transform robotVisual, float pitch)
    {
        var presentation = root.AddComponent<PrototypeProceduralPresentation>();
        presentation.Configure(robotVisual, pitch > 1f, true);
        var audio = root.AddComponent<PrototypeAudioFeedback>();
        audio.Configure(pitch);
    }

    public static PrototypeEnemy CreateEnemy(Vector3 position, PrototypePlayerController player, PrototypeMissionController mission, PrototypeGameConfig config, Transform parent, int index)
    {
        var enemyObject = new GameObject("Sentinel_" + index);
        enemyObject.transform.SetParent(parent, false);
        enemyObject.transform.position = position;
        var controller = enemyObject.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.42f;
        controller.center = Vector3.up * 0.9f;
        var health = enemyObject.AddComponent<PrototypeHealth>();
        health.Initialize(config.enemyHealth);
        health.ConfigureResistances(1f, 1f, 1f, 1f);
        var enemy = enemyObject.AddComponent<PrototypeEnemy>();
        enemy.Configure(player, mission, config);
        CreateVisualCapsule("SentinelArmor", enemyObject.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.85f, 0.9f, 0.85f), CreateMaterial(new Color(0.72f, 0.08f, 0.12f)));
        CreateVisualSphere("SentinelEye", enemyObject.transform, new Vector3(0f, 1.15f, 0.38f), Vector3.one * 0.2f, CreateMaterial(new Color(1f, 0.55f, 0.08f), true));
        return enemy;
    }

    private static Transform CreateMarker(string name, Transform parent, Vector3 position, Color color)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        root.position = position;
        CreatePrimitiveChild(PrimitiveType.Cylinder, "MarkerColumn", root, Vector3.zero, new Vector3(1.4f, 0.25f, 1.4f), CreateMaterial(color, true));
        CreatePrimitiveChild(PrimitiveType.Sphere, "MarkerOrb", root, Vector3.up * 1.2f, Vector3.one * 0.55f, CreateMaterial(color, true));
        return root;
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().material = material;
        return cube;
    }

    private static Transform CreatePresentationRoot(string name, Transform parent)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        return root;
    }

    private static void CreateVisualCapsule(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        CreatePrimitiveChild(PrimitiveType.Capsule, name, parent, position, scale, material);
    }

    private static void CreateVisualSphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        CreatePrimitiveChild(PrimitiveType.Sphere, name, parent, position, scale, material);
    }

    private static void CreateSphereLight(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        CreateVisualSphere(name, parent, position, scale, material);
    }

    private static GameObject CreatePrimitiveChild(PrimitiveType primitiveType, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var child = GameObject.CreatePrimitive(primitiveType);
        child.name = name;
        child.transform.SetParent(parent, false);
        child.transform.localPosition = position;
        child.transform.localScale = scale;
        child.GetComponent<Renderer>().material = material;
        Object.Destroy(child.GetComponent<Collider>());
        return child;
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
}
