using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PrototypeGameManager : MonoBehaviour
{
    public static PrototypeGameManager Instance { get; private set; }
    public static bool IsPaused => Instance != null && Instance.paused;

    private PrototypePlayerController player;
    private PrototypeHealth playerHealth;
    private PrototypeElectricController electricHero;
    private PrototypeStoneGiantController stoneGiant;
    private PrototypeMissionController mission;
    private PrototypeRobotController robot;
    private readonly List<PrototypeTransformerController> transformers = new List<PrototypeTransformerController>();
    private PrototypeTransformerController selectedTransformer;
    private PrototypeTransformerController activeTransformer;
    private PrototypeCameraRig cameraRig;
    private PrototypeCharacterId activeCharacterId = PrototypeCharacterId.OriginalHero;
    private PrototypeCharacterId returnCharacterId = PrototypeCharacterId.OriginalHero;
    private Transform activeTransform;
    private PrototypeHealth activeHealth;
    private bool paused;

    public PrototypePlayerController Player => player;
    public PrototypeElectricController ElectricHero => electricHero;
    public PrototypeStoneGiantController StoneGiant => stoneGiant;
    public PrototypeMissionController Mission => mission;
    public bool Paused => paused;
    public PrototypeRobotController Robot => robot;
    public bool IsRobotDeployed => (activeTransformer != null && activeTransformer.IsDeployed) || (robot != null && robot.IsDeployed);
    public PrototypeTransformerController SelectedRobot => selectedTransformer;
    public PrototypeTransformerController ActiveTransformer => activeTransformer;
    public IReadOnlyList<PrototypeTransformerController> Transformers => transformers;
    public PrototypeCharacterId SelectedRobotId => selectedTransformer != null ? selectedTransformer.CharacterId : PrototypeCharacterId.AtlasRunner;
    public PrototypeCharacterId ActiveCharacterId => activeCharacterId;
    public string ActiveCharacterName => GetActiveProfile()?.DisplayName ?? activeCharacterId.ToString();
    public Transform ActiveTransform => activeTransform;
    public PrototypeHealth ActiveHealth => activeHealth;
    public bool CanSwitchCharactersNow => CanSwitchCharacters();

    private void Awake()
    {
        Instance = this;
    }

    public void Configure(PrototypePlayerController playerController, PrototypeHealth health, PrototypeMissionController missionController)
    {
        player = playerController;
        playerHealth = health;
        mission = missionController;
        activeCharacterId = PrototypeCharacterId.OriginalHero;
        activeTransform = player != null ? player.transform : null;
        activeHealth = health;
        mission?.SetActiveCharacter(activeTransform, activeHealth);
    }

    public void ConfigureCharacters(PrototypeElectricController electricController, PrototypeStoneGiantController giantController)
    {
        electricHero = electricController;
        stoneGiant = giantController;
        if (electricHero != null)
        {
            electricHero.gameObject.SetActive(false);
        }

        if (stoneGiant != null)
        {
            stoneGiant.gameObject.SetActive(false);
        }
    }

    public void ConfigureRobot(PrototypeRobotController robotController, PrototypeCameraRig rig)
    {
        robot = robotController;
        cameraRig = rig;
        if (robot != null)
        {
            robot.gameObject.SetActive(true);
            robot.Undeploy();
            robot.gameObject.SetActive(false);
        }

        ApplyCameraProfile();
    }

    public void ConfigureTransformers(PrototypeTransformerController[] transformerControllers, PrototypeCameraRig rig)
    {
        cameraRig = rig;
        if (transformerControllers == null)
        {
            return;
        }

        foreach (var transformer in transformerControllers)
        {
            if (transformer == null)
            {
                continue;
            }

            transformers.Add(transformer);
            transformer.gameObject.SetActive(true);
            transformer.Undeploy();
            transformer.gameObject.SetActive(false);
            if (transformer.GetComponent<PrototypeRobotController>() != null)
            {
                robot = transformer.GetComponent<PrototypeRobotController>();
            }
        }

        selectedTransformer = GetTransformer(PrototypeCharacterId.AtlasRunner);
        if (selectedTransformer == null && robot == null && transformers.Count > 0)
        {
            selectedTransformer = transformers[0];
        }

        ApplyCameraProfile();
    }

    private void Update()
    {
        if (PrototypeInput.PausePressed && (mission == null || mission.State != PrototypeMissionState.Failed))
        {
            TogglePause();
        }

        if (paused)
        {
            return;
        }

        if (PrototypeInput.SelectOriginalPressed)
        {
            TrySelectCharacter(PrototypeCharacterId.OriginalHero);
        }
        else if (PrototypeInput.SelectElectricPressed)
        {
            TrySelectCharacter(PrototypeCharacterId.ElectricHero);
        }
        else if (PrototypeInput.SelectGiantPressed)
        {
            TrySelectCharacter(PrototypeCharacterId.StoneGiant);
        }
        else if (PrototypeInput.SelectAtlasPressed)
        {
            TrySelectRobot(PrototypeCharacterId.AtlasRunner);
        }
        else if (PrototypeInput.SelectHelicopterPressed)
        {
            TrySelectRobot(PrototypeCharacterId.HelicopterTransformer);
        }
        else if (PrototypeInput.SelectTankPressed)
        {
            TrySelectRobot(PrototypeCharacterId.TankTransformer);
        }
        else if (PrototypeInput.SelectMotorcyclePressed)
        {
            TrySelectRobot(PrototypeCharacterId.MotorcycleTransformer);
        }
        else if (PrototypeInput.SelectJetPressed)
        {
            TrySelectRobot(PrototypeCharacterId.JetTransformer);
        }
        else if (PrototypeInput.SwitchCharacterPressed)
        {
            TrySelectNextCharacter();
        }

        if (PrototypeInput.DeployRobotPressed && (selectedTransformer != null || robot != null))
        {
            if (IsRobotDeployed)
            {
                TryReturnToHero();
            }
            else
            {
                DeployRobot();
            }
        }
    }

    public void TogglePause()
    {
        if (mission != null && mission.State == PrototypeMissionState.Complete)
        {
            return;
        }

        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    public void Resume()
    {
        paused = false;
        Time.timeScale = 1f;
    }

    public void RestartMission()
    {
        Resume();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public bool TrySelectCharacter(PrototypeCharacterId characterId)
    {
        if (!CanSwitchCharacters() || characterId == PrototypeCharacterId.AtlasRunner || characterId == activeCharacterId)
        {
            return false;
        }

        var target = GetCharacterTransform(characterId);
        var targetHealth = GetCharacterHealth(characterId);
        if (target == null || targetHealth == null || !targetHealth.IsAlive)
        {
            return false;
        }

        var sourcePosition = activeTransform != null ? activeTransform.position : Vector3.zero;
        var sourceRotation = activeTransform != null ? activeTransform.rotation : Quaternion.identity;
        DeactivateCurrentCharacter();
        target.position = sourcePosition;
        target.rotation = sourceRotation;
        ActivateCharacter(characterId);
        mission?.SetActiveCharacter(activeTransform, activeHealth);
        SetEnemyTargets();
        ApplyCameraProfile();
        return true;
    }

    public bool TrySelectRobot(PrototypeCharacterId characterId)
    {
        if (!CanSwitchCharacters() || IsRobotDeployed)
        {
            return false;
        }

        if (characterId == PrototypeCharacterId.AtlasRunner)
        {
            if (robot == null || !robot.Health.IsAlive)
            {
                return false;
            }

            selectedTransformer = null;
            return true;
        }

        var transformer = GetTransformer(characterId);
        if (transformer == null || transformer.Health == null || !transformer.Health.IsAlive)
        {
            return false;
        }

        selectedTransformer = transformer;
        return true;
    }

    public bool TrySelectNextCharacter()
    {
        if (!CanSwitchCharacters())
        {
            return false;
        }

        var next = activeCharacterId == PrototypeCharacterId.OriginalHero
            ? PrototypeCharacterId.ElectricHero
            : activeCharacterId == PrototypeCharacterId.ElectricHero
                ? PrototypeCharacterId.StoneGiant
                : PrototypeCharacterId.OriginalHero;
        return TrySelectCharacter(next);
    }

    public bool DeployRobot()
    {
        if (IsRobotDeployed || activeTransform == null || activeHealth == null || !activeHealth.IsAlive || activeCharacterId == PrototypeCharacterId.AtlasRunner)
        {
            return false;
        }

        if (activeCharacterId == PrototypeCharacterId.OriginalHero && player != null && player.IsFlying)
        {
            return false;
        }

        returnCharacterId = activeCharacterId;
        var deploymentPosition = activeTransform.position + activeTransform.right * 2.4f;
        var deploymentRotation = activeTransform.rotation;
        DeactivateCurrentCharacter();
        if (selectedTransformer != null)
        {
            selectedTransformer.transform.SetPositionAndRotation(deploymentPosition, deploymentRotation);
            selectedTransformer.gameObject.SetActive(true);
            selectedTransformer.Deploy();
            activeTransformer = selectedTransformer;
            activeCharacterId = selectedTransformer.CharacterId;
            activeTransform = selectedTransformer.transform;
            activeHealth = selectedTransformer.Health;
        }
        else
        {
            if (robot == null || robot.IsDeployed)
            {
                return false;
            }

            robot.transform.SetPositionAndRotation(deploymentPosition, deploymentRotation);
            robot.gameObject.SetActive(true);
            robot.Deploy();
            activeCharacterId = PrototypeCharacterId.AtlasRunner;
            activeTransform = robot.transform;
            activeHealth = robot.Health;
        }
        mission?.SetActiveCharacter(activeTransform, activeHealth);
        SetEnemyTargets();
        ApplyCameraProfile();
        return true;
    }

    public bool TryReturnToHero()
    {
        if (activeTransformer != null)
        {
            if (!activeTransformer.IsDeployed || !activeTransformer.TryReturnToHeroSafe())
            {
                return false;
            }

            var transformerDestination = activeTransformer.transform.position + activeTransformer.transform.right * -2.4f;
            var transformerRotation = Quaternion.Euler(0f, activeTransformer.transform.eulerAngles.y, 0f);
            var transformerCharacter = returnCharacterId;
            activeTransformer.Undeploy();
            activeTransformer.gameObject.SetActive(false);
            activeTransformer = null;
            return RestoreHero(transformerCharacter, transformerDestination, transformerRotation);
        }

        if (robot == null || !robot.IsDeployed || !robot.TryReturnToHeroSafe())
        {
            return false;
        }

        var destination = robot.transform.position + robot.transform.right * -2.4f;
        var rotation = Quaternion.Euler(0f, robot.transform.eulerAngles.y, 0f);
        var destinationCharacter = returnCharacterId;
        robot.Undeploy();
        robot.gameObject.SetActive(false);
        var target = GetCharacterTransform(destinationCharacter);
        if (target == null)
        {
            destinationCharacter = PrototypeCharacterId.OriginalHero;
            target = player != null ? player.transform : null;
        }

        if (target == null)
        {
            return false;
        }

        target.SetPositionAndRotation(destination, rotation);
        ActivateCharacter(destinationCharacter);
        mission?.SetActiveCharacter(activeTransform, activeHealth);
        SetEnemyTargets();
        ApplyCameraProfile();
        return true;
    }

    private bool RestoreHero(PrototypeCharacterId destinationCharacter, Vector3 destination, Quaternion rotation)
    {
        var target = GetCharacterTransform(destinationCharacter);
        if (target == null)
        {
            destinationCharacter = PrototypeCharacterId.OriginalHero;
            target = player != null ? player.transform : null;
        }

        if (target == null)
        {
            return false;
        }

        target.SetPositionAndRotation(destination, rotation);
        ActivateCharacter(destinationCharacter);
        mission?.SetActiveCharacter(activeTransform, activeHealth);
        SetEnemyTargets();
        ApplyCameraProfile();
        return true;
    }

    private bool CanSwitchCharacters()
    {
        if (paused || IsRobotDeployed || activeCharacterId == PrototypeCharacterId.AtlasRunner || activeHealth == null || !activeHealth.IsAlive)
        {
            return false;
        }

        if (mission != null && (mission.State == PrototypeMissionState.Complete || mission.State == PrototypeMissionState.Failed))
        {
            return false;
        }

        if (activeTransform == null || !activeTransform.gameObject.activeInHierarchy || !IsFinite(activeTransform.position))
        {
            return false;
        }

        switch (activeCharacterId)
        {
            case PrototypeCharacterId.OriginalHero:
                return player != null && !player.IsFlying && HasNearbyGround(activeTransform.position, 3.5f);
            case PrototypeCharacterId.ElectricHero:
                return electricHero != null && HasNearbyGround(activeTransform.position, 4f);
            case PrototypeCharacterId.StoneGiant:
                return stoneGiant != null && HasNearbyGround(activeTransform.position, 4f);
            default:
                return false;
        }
    }

    private static bool HasNearbyGround(Vector3 position, float maxDistance)
    {
        return Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z)
            && !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
    }

    private void DeactivateCurrentCharacter()
    {
        if (activeCharacterId == PrototypeCharacterId.AtlasRunner || activeTransform == null)
        {
            return;
        }

        if (activeCharacterId == PrototypeCharacterId.ElectricHero)
        {
            electricHero?.SetControlEnabled(false);
        }
        else if (activeCharacterId == PrototypeCharacterId.StoneGiant)
        {
            stoneGiant?.SetControlEnabled(false);
        }
        else if (player != null)
        {
            player.enabled = false;
        }

        activeTransform.gameObject.SetActive(false);
    }

    private void ActivateCharacter(PrototypeCharacterId characterId)
    {
        activeCharacterId = characterId;
        var target = GetCharacterTransform(characterId);
        var health = GetCharacterHealth(characterId);
        activeTransform = target;
        activeHealth = health;
        if (target != null)
        {
            target.gameObject.SetActive(true);
        }

        if (characterId == PrototypeCharacterId.ElectricHero)
        {
            electricHero?.SetControlEnabled(true);
        }
        else if (characterId == PrototypeCharacterId.StoneGiant)
        {
            stoneGiant?.SetControlEnabled(true);
        }
        else if (characterId == PrototypeCharacterId.OriginalHero && player != null)
        {
            player.enabled = true;
        }
    }

    private Transform GetCharacterTransform(PrototypeCharacterId characterId)
    {
        switch (characterId)
        {
            case PrototypeCharacterId.ElectricHero:
                return electricHero != null ? electricHero.transform : null;
            case PrototypeCharacterId.StoneGiant:
                return stoneGiant != null ? stoneGiant.transform : null;
            case PrototypeCharacterId.AtlasRunner:
            case PrototypeCharacterId.HelicopterTransformer:
            case PrototypeCharacterId.TankTransformer:
            case PrototypeCharacterId.MotorcycleTransformer:
            case PrototypeCharacterId.JetTransformer:
                var transformer = GetTransformer(characterId);
                if (transformer != null)
                {
                    return transformer.transform;
                }

                return characterId == PrototypeCharacterId.AtlasRunner && robot != null ? robot.transform : null;
            default:
                return player != null ? player.transform : null;
        }
    }

    private PrototypeHealth GetCharacterHealth(PrototypeCharacterId characterId)
    {
        switch (characterId)
        {
            case PrototypeCharacterId.ElectricHero:
                return electricHero != null ? electricHero.Health : null;
            case PrototypeCharacterId.StoneGiant:
                return stoneGiant != null ? stoneGiant.Health : null;
            case PrototypeCharacterId.AtlasRunner:
            case PrototypeCharacterId.HelicopterTransformer:
            case PrototypeCharacterId.TankTransformer:
            case PrototypeCharacterId.MotorcycleTransformer:
            case PrototypeCharacterId.JetTransformer:
                var transformer = GetTransformer(characterId);
                if (transformer != null)
                {
                    return transformer.Health;
                }

                return characterId == PrototypeCharacterId.AtlasRunner && robot != null ? robot.Health : null;
            default:
                return playerHealth;
        }
    }

    private PrototypeCharacterProfile GetActiveProfile()
    {
        return activeTransform != null ? activeTransform.GetComponent<PrototypeCharacterProfile>() : null;
    }

    private void SetEnemyTargets()
    {
        if (activeTransform == null || activeHealth == null)
        {
            return;
        }

        foreach (var enemy in Object.FindObjectsByType<PrototypeEnemy>(FindObjectsSortMode.None))
        {
            enemy.SetTarget(activeTransform, activeHealth);
        }
    }

    private void ApplyCameraProfile()
    {
        if (cameraRig == null || activeTransform == null)
        {
            return;
        }

        var profile = activeTransform.GetComponent<PrototypeCharacterProfile>();
        var transformer = activeTransform.GetComponent<PrototypeTransformerController>();
        cameraRig.SetTarget(activeTransform,
            transformer != null ? transformer.CurrentCameraDistance : profile != null ? profile.CameraDistance : 7.5f,
            transformer != null ? transformer.CurrentCameraHeight : profile != null ? profile.CameraHeight : 3.2f);
    }

    private PrototypeTransformerController GetTransformer(PrototypeCharacterId characterId)
    {
        foreach (var transformer in transformers)
        {
            if (transformer != null && transformer.CharacterId == characterId)
            {
                return transformer;
            }
        }

        return null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        Time.timeScale = 1f;
    }
}
