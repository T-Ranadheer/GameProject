using NUnit.Framework;
using UnityEngine;

public sealed class PrototypeGameplayTests
{
    [Test]
    public void HealthDamageAndDeathAreDeterministic()
    {
        var actor = new GameObject("HealthTestActor");
        var health = actor.AddComponent<PrototypeHealth>();
        health.Initialize(50f);

        health.ApplyDamage(15f);
        Assert.AreEqual(35f, health.CurrentHealth);
        Assert.IsTrue(health.IsAlive);

        health.ApplyDamage(35f);
        Assert.AreEqual(0f, health.CurrentHealth);
        Assert.IsFalse(health.IsAlive);
        Object.DestroyImmediate(actor);
    }

    [Test]
    public void PlayerFlightStateTransitionsAreExplicit()
    {
        var actor = new GameObject("FlightTestActor");
        actor.AddComponent<CharacterController>();
        actor.AddComponent<PrototypeHealth>();
        var player = actor.AddComponent<PrototypePlayerController>();
        player.Configure(PrototypeGameConfig.CreateDefault());

        Assert.IsFalse(player.IsFlying);
        player.SetFlying(true);
        Assert.IsTrue(player.IsFlying);
        player.SetFlying(false);
        Assert.IsFalse(player.IsFlying);
        Object.DestroyImmediate(actor);
    }

    [Test]
    public void AbilityCooldownBlocksImmediateSecondShot()
    {
        var actor = new GameObject("AbilityTestActor");
        actor.AddComponent<PrototypeHealth>();
        var ability = actor.AddComponent<PrototypePlayerAbility>();
        ability.Configure(PrototypeGameConfig.CreateDefault());

        Assert.IsTrue(ability.TryFireAt(Vector3.forward));
        Assert.IsFalse(ability.CanFire);
        Assert.IsFalse(ability.TryFireAt(Vector3.forward));
        ability.ResetCooldown();
        Assert.IsTrue(ability.CanFire);
        Object.DestroyImmediate(actor);
    }

    [Test]
    public void MissionTransitionsThroughBeaconCombatAndExtraction()
    {
        var root = new GameObject("MissionTestRoot");
        var playerObject = new GameObject("MissionTestPlayer");
        playerObject.transform.SetParent(root.transform);
        playerObject.AddComponent<CharacterController>();
        var playerHealth = playerObject.AddComponent<PrototypeHealth>();
        playerHealth.Initialize(100f);
        var player = playerObject.AddComponent<PrototypePlayerController>();
        player.Configure(PrototypeGameConfig.CreateDefault());
        var beacon = new GameObject("Beacon").transform;
        beacon.SetParent(root.transform);
        beacon.position = playerObject.transform.position;
        var extraction = new GameObject("Extraction").transform;
        extraction.SetParent(root.transform);
        extraction.position = playerObject.transform.position;
        var mission = root.AddComponent<PrototypeMissionController>();
        mission.Configure(player, playerHealth, beacon, extraction, new System.Collections.Generic.List<PrototypeEnemy>(), PrototypeGameConfig.CreateDefault());

        mission.NotifyBeaconReached();
        Assert.AreEqual(PrototypeMissionState.ClearEnemies, mission.State);
        mission.NotifyEnemyDefeated(null);
        Assert.AreEqual(PrototypeMissionState.ReachExtraction, mission.State);
        mission.NotifyExtractionReached();
        Assert.AreEqual(PrototypeMissionState.Complete, mission.State);

        Object.DestroyImmediate(root);
    }
}
