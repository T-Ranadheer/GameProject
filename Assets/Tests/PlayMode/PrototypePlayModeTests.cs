using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class PrototypePlayModeTests
{
    [UnityTest]
    public IEnumerator SampleSceneBootstrapsPlayableRuntime()
    {
        SceneManager.LoadScene("Assets/Scenes/SampleScene.unity");
        yield return null;
        yield return null;

        Assert.IsNotNull(Object.FindFirstObjectByType<PrototypeBootstrap>());
        Assert.IsNotNull(Object.FindFirstObjectByType<PrototypePlayerController>());
        Assert.IsNotNull(Object.FindFirstObjectByType<PrototypeMissionController>());
        Assert.IsNotNull(GameObject.Find("GameplayCamera"));
        Assert.IsNotNull(GameObject.Find("SignalBeacon"));
        Assert.AreEqual(4, Object.FindObjectsByType<PrototypeEnemy>(FindObjectsSortMode.None).Length);
        var manager = Object.FindFirstObjectByType<PrototypeGameManager>();
        Assert.IsNotNull(manager.ElectricHero);
        Assert.IsNotNull(manager.StoneGiant);
        Assert.IsNotNull(manager.Robot);
        Assert.IsFalse(manager.Robot.IsDeployed);
        Assert.AreEqual(PrototypeRobotTransformState.Robot, manager.Robot.State);
        Assert.AreEqual(PrototypeCharacterId.OriginalHero, manager.ActiveCharacterId);

        Assert.IsTrue(manager.TrySelectCharacter(PrototypeCharacterId.ElectricHero));
        Assert.AreEqual(PrototypeCharacterId.ElectricHero, manager.ActiveCharacterId);
        Assert.IsFalse(manager.Player.enabled);
        Assert.IsTrue(manager.TrySelectCharacter(PrototypeCharacterId.StoneGiant));
        Assert.AreEqual(PrototypeCharacterId.StoneGiant, manager.ActiveCharacterId);
        Assert.Greater(manager.StoneGiant.GetComponent<CharacterController>().height, manager.Player.GetComponent<CharacterController>().height);
        Assert.IsTrue(manager.TrySelectCharacter(PrototypeCharacterId.OriginalHero));
        Assert.AreEqual(PrototypeCharacterId.OriginalHero, manager.ActiveCharacterId);

        Assert.IsTrue(manager.DeployRobot());
        Assert.IsTrue(manager.Robot.IsDeployed);
        yield return new WaitForFixedUpdate();
        Assert.IsTrue(manager.Robot.TryToggleTransform());
        yield return new WaitForSeconds(1.25f);
        Assert.IsTrue(manager.Robot.IsCarForm);
        Assert.IsFalse(manager.Robot.IsRobotForm);
        Assert.IsTrue(manager.Robot.TryToggleTransform());
        yield return new WaitForSeconds(1.25f);
        Assert.IsTrue(manager.Robot.IsRobotForm);
        Assert.IsFalse(manager.Robot.IsCarForm);
        Assert.IsTrue(manager.TryReturnToHero());
        Assert.IsFalse(manager.Robot.IsDeployed);
    }

    [UnityTest]
    public IEnumerator RosterRejectsAirborneSwitchAndRestoresRobotToPreviousHero()
    {
        SceneManager.LoadScene("Assets/Scenes/SampleScene.unity");
        yield return null;
        yield return null;
        yield return new WaitForFixedUpdate();

        var manager = Object.FindFirstObjectByType<PrototypeGameManager>();
        var cameraRig = Object.FindFirstObjectByType<PrototypeCameraRig>();
        Assert.IsNotNull(manager);
        Assert.AreEqual(PrototypeCharacterId.OriginalHero, manager.ActiveCharacterId);

        manager.Player.SetFlying(true);
        Assert.IsFalse(manager.TrySelectCharacter(PrototypeCharacterId.ElectricHero));
        manager.Player.SetFlying(false);
        Assert.IsTrue(manager.TrySelectCharacter(PrototypeCharacterId.ElectricHero));
        Assert.AreEqual(PrototypeCharacterId.ElectricHero, manager.ActiveCharacterId);
        Assert.AreEqual(manager.ElectricHero.transform, cameraRig.Target);

        var electricHealth = manager.ElectricHero.Health;
        electricHealth.ApplyDamage(17f);
        Assert.IsTrue(manager.TrySelectCharacter(PrototypeCharacterId.StoneGiant));
        Assert.AreEqual(PrototypeCharacterId.StoneGiant, manager.ActiveCharacterId);
        Assert.Greater(cameraRig.TargetHeight, 4f);
        Assert.IsTrue(manager.TrySelectCharacter(PrototypeCharacterId.ElectricHero));
        Assert.AreEqual(manager.ElectricHero.Health.MaxHealth - 17f, manager.ElectricHero.Health.CurrentHealth, 0.001f);

        Assert.IsTrue(manager.DeployRobot());
        yield return new WaitForFixedUpdate();
        Assert.IsFalse(manager.TrySelectCharacter(PrototypeCharacterId.OriginalHero));
        Assert.IsTrue(manager.TryReturnToHero());
        Assert.AreEqual(PrototypeCharacterId.ElectricHero, manager.ActiveCharacterId);
        Assert.IsTrue(manager.ElectricHero.gameObject.activeInHierarchy);
        Assert.IsFalse(manager.Player.gameObject.activeInHierarchy);
    }

    [UnityTest]
    public IEnumerator ElectricProjectileDamagesRuntimeEnemyAndInactiveCharactersStayQuiet()
    {
        SceneManager.LoadScene("Assets/Scenes/SampleScene.unity");
        yield return null;
        yield return null;
        yield return new WaitForFixedUpdate();

        var manager = Object.FindFirstObjectByType<PrototypeGameManager>();
        var enemy = Object.FindObjectsByType<PrototypeEnemy>(FindObjectsSortMode.None)[0];
        var targetHealth = enemy.GetComponent<PrototypeHealth>();
        Assert.IsTrue(manager.TrySelectCharacter(PrototypeCharacterId.ElectricHero));
        enemy.enabled = false;
        enemy.ClearTarget();
        enemy.transform.position = manager.ElectricHero.transform.position + Vector3.forward * 3f;
        manager.ElectricHero.transform.rotation = Quaternion.identity;
        Physics.SyncTransforms();

        var ability = manager.ElectricHero.GetComponent<PrototypeElectricAbilities>();
        var before = targetHealth.CurrentHealth;
        Assert.IsTrue(ability.TryElectricBolt(Vector3.forward));
        yield return new WaitForSeconds(0.5f);
        Assert.Less(targetHealth.CurrentHealth, before);

        var projectileCount = Object.FindObjectsByType<PrototypeProjectile>(FindObjectsSortMode.None).Length;
        Assert.LessOrEqual(projectileCount, 1);
        Assert.IsFalse(manager.Player.enabled);
        Assert.IsFalse(manager.StoneGiant.enabled);
    }

    [UnityTest]
    public IEnumerator PoliceCoordinatorSpawnsBoundedUnitsAndEscalatesWantedResponse()
    {
        SceneManager.LoadScene("Assets/Scenes/SampleScene.unity");
        yield return null;
        yield return null;
        yield return new WaitForFixedUpdate();

        var coordinator = Object.FindFirstObjectByType<PrototypePoliceCoordinator>();
        var manager = Object.FindFirstObjectByType<PrototypeGameManager>();
        Assert.IsNotNull(coordinator);
        Assert.GreaterOrEqual(coordinator.ActivePoliceUnits, 1);
        Assert.GreaterOrEqual(coordinator.ActiveHostileRobots, 3);

        var police = Object.FindFirstObjectByType<PrototypePoliceUnit>();
        Assert.IsNotNull(police);
        Assert.IsTrue(coordinator.RegisterIncident(new PrototypeIncident(PrototypeIncidentType.AttackPolice, police.transform.position, 4.8f, manager.ActiveTransform.gameObject, police.gameObject)));
        Assert.GreaterOrEqual((int)coordinator.Wanted.Level, (int)PrototypeWantedLevel.CoordinatedResponse);

        yield return new WaitForSeconds(1f);
        var config = PrototypeGameConfig.CreateDefault();
        Assert.LessOrEqual(coordinator.ActivePoliceUnits, config.policeMaxActiveUnits);
        Assert.LessOrEqual(coordinator.ActiveHostileRobots, config.hostileRobotMaxActiveUnits);
        Assert.IsTrue(coordinator.PursuitStatus == "PURSUIT" || coordinator.PursuitStatus == "SEARCH");
    }
}
