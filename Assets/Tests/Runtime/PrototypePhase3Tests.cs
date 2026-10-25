using NUnit.Framework;
using UnityEngine;

public sealed class PrototypePhase3Tests
{
    [Test]
    public void EnergyPreventsOverspendAndRegeneratesAfterDelayConfiguration()
    {
        var actor = new GameObject("EnergyTestActor");
        var energy = actor.AddComponent<PrototypeEnergy>();
        energy.Configure(100f, 20f, 1f);

        Assert.AreEqual(100f, energy.Current);
        Assert.IsTrue(energy.TrySpend(35f));
        Assert.AreEqual(65f, energy.Current);
        Assert.IsFalse(energy.TrySpend(70f));
        Assert.AreEqual(65f, energy.Current);
        Assert.IsFalse(energy.TrySpend(-1f));

        Object.DestroyImmediate(actor);
    }

    [Test]
    public void ElectricBoltConsumesEnergyAndCooldownBlocksDuplicateActivation()
    {
        var actor = new GameObject("ElectricAbilityTestActor");
        actor.AddComponent<CharacterController>();
        actor.AddComponent<PrototypeHealth>();
        actor.AddComponent<PrototypeEnergy>();
        var controller = actor.AddComponent<PrototypeElectricController>();
        var abilities = actor.AddComponent<PrototypeElectricAbilities>();
        var config = PrototypeGameConfig.CreateDefault();
        controller.Configure(config);
        abilities.Configure(config);

        var energy = actor.GetComponent<PrototypeEnergy>();
        var before = energy.Current;
        Assert.IsTrue(abilities.TryElectricBolt(Vector3.forward));
        Assert.Less(energy.Current, before);
        Assert.IsFalse(abilities.TryElectricBolt(Vector3.forward));

        foreach (var projectile in Object.FindObjectsByType<PrototypeProjectile>(FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(projectile.gameObject);
        }

        Object.DestroyImmediate(actor);
    }

    [Test]
    public void GiantControllerUsesDistinctCollisionProfileAndHeavyDamage()
    {
        var actor = new GameObject("GiantTestActor");
        var characterController = actor.AddComponent<CharacterController>();
        actor.AddComponent<PrototypeHealth>();
        var giant = actor.AddComponent<PrototypeStoneGiantController>();
        var combat = actor.AddComponent<PrototypeStoneGiantCombat>();
        var config = PrototypeGameConfig.CreateDefault();
        giant.Configure(config);
        combat.Configure(config);

        Assert.AreEqual(config.giantControllerHeight, characterController.height);
        Assert.AreEqual(config.giantControllerRadius, characterController.radius);
        Assert.Greater(actor.GetComponent<PrototypeHealth>().MaxHealth, config.playerHealth);
        Assert.IsFalse(giant.enabled);

        Object.DestroyImmediate(actor);
    }

    [Test]
    public void ChainLightningDamagesEachEnemyAtMostOnce()
    {
        var actor = new GameObject("ChainSource");
        actor.AddComponent<CharacterController>();
        actor.AddComponent<PrototypeHealth>();
        actor.AddComponent<PrototypeEnergy>();
        var controller = actor.AddComponent<PrototypeElectricController>();
        var abilities = actor.AddComponent<PrototypeElectricAbilities>();
        var config = PrototypeGameConfig.CreateDefault();
        controller.Configure(config);
        abilities.Configure(config);

        var first = CreateTestEnemy("ChainTargetA", new Vector3(0f, 1f, 3f), config);
        var second = CreateTestEnemy("ChainTargetB", new Vector3(0f, 1f, 7f), config);
        Physics.SyncTransforms();

        Assert.IsTrue(abilities.TryChainLightning(null));
        Assert.AreEqual(config.enemyHealth - config.chainLightningDamage, first.GetComponent<PrototypeHealth>().CurrentHealth);
        Assert.AreEqual(config.enemyHealth - config.chainLightningDamage, second.GetComponent<PrototypeHealth>().CurrentHealth);

        Object.DestroyImmediate(first.gameObject);
        Object.DestroyImmediate(second.gameObject);
        Object.DestroyImmediate(actor);
    }

    [Test]
    public void AreaDamageHitsEachTargetOnlyOnce()
    {
        var source = new GameObject("AreaSource");
        var target = new GameObject("AreaTarget");
        target.transform.position = Vector3.forward * 2f;
        var collider = target.AddComponent<SphereCollider>();
        collider.radius = 1f;
        var health = target.AddComponent<PrototypeHealth>();
        health.Initialize(100f);
        Physics.SyncTransforms();

        var hitCount = PrototypeCombatUtility.ApplyAreaDamage(Vector3.zero, 4f, 25f, PrototypeDamageType.Electric, source, 8);

        Assert.AreEqual(1, hitCount);
        Assert.AreEqual(75f, health.CurrentHealth);

        Object.DestroyImmediate(source);
        Object.DestroyImmediate(target);
    }

    [Test]
    public void DamageResistanceAndInvalidAmountsAreSafe()
    {
        var actor = new GameObject("ResistanceTestActor");
        var health = actor.AddComponent<PrototypeHealth>();
        health.Initialize(100f);
        health.ConfigureResistances(1f, 1f, 0.5f, 0.25f);

        health.ApplyDamage(new PrototypeDamageInfo(40f, PrototypeDamageType.Electric, actor.transform.position, Vector3.forward));
        Assert.AreEqual(80f, health.CurrentHealth);
        health.ApplyDamage(new PrototypeDamageInfo(40f, PrototypeDamageType.Impact, actor.transform.position, Vector3.forward));
        Assert.AreEqual(70f, health.CurrentHealth);
        health.ApplyDamage(float.NaN);
        health.ApplyDamage(float.PositiveInfinity);
        Assert.AreEqual(70f, health.CurrentHealth);

        Object.DestroyImmediate(actor);
    }

    [Test]
    public void WantedSystemEscalatesDeduplicatesAndDecaysAfterPursuitEnds()
    {
        var config = PrototypeGameConfig.CreateDefault();
        config.wantedDecayDelay = 0f;
        config.wantedDecayPerSecond = 20f;
        var wanted = new PrototypeWantedSystem();
        wanted.Configure(config);
        var source = new GameObject("WantedSource");
        var victim = new GameObject("WantedVictim");
        var incident = new PrototypeIncident(PrototypeIncidentType.AttackPolice, Vector3.zero, 3f, source, victim);

        Assert.IsTrue(wanted.RegisterIncident(incident));
        Assert.IsFalse(wanted.RegisterIncident(incident));
        Assert.AreEqual(PrototypeWantedLevel.ActivePursuit, wanted.Level);
        wanted.SetActivePursuits(1);
        wanted.Tick(1f);
        Assert.AreEqual(PrototypeWantedLevel.ActivePursuit, wanted.Level);
        wanted.SetActivePursuits(0);
        wanted.Tick(1f);
        Assert.AreEqual(PrototypeWantedLevel.Clear, wanted.Level);

        Object.DestroyImmediate(source);
        Object.DestroyImmediate(victim);
    }

    [Test]
    public void FactionRulesKeepPoliceAndHostileRobotsSelective()
    {
        Assert.IsTrue(PrototypeFactionRules.IsHostile(PrototypeFaction.Police, PrototypeFaction.HostileRobot));
        Assert.IsTrue(PrototypeFactionRules.IsHostile(PrototypeFaction.HostileRobot, PrototypeFaction.Player));
        Assert.IsFalse(PrototypeFactionRules.IsHostile(PrototypeFaction.Police, PrototypeFaction.Civilian));
        Assert.IsFalse(PrototypeFactionRules.IsHostile(PrototypeFaction.HostileRobot, PrototypeFaction.Sentinel));
    }

    private static PrototypeEnemy CreateTestEnemy(string name, Vector3 position, PrototypeGameConfig config)
    {
        var enemyObject = new GameObject(name);
        enemyObject.transform.position = position;
        enemyObject.AddComponent<CharacterController>();
        enemyObject.AddComponent<PrototypeHealth>();
        var enemy = enemyObject.AddComponent<PrototypeEnemy>();
        enemy.Configure(null, null, config);
        return enemy;
    }
}
