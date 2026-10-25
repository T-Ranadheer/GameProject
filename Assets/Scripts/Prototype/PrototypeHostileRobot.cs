using UnityEngine;

public sealed class PrototypeHostileRobot : PrototypeAiAgent
{
    private PrototypePoliceCoordinator coordinator;
    private PrototypeHostileRobotRole role;
    private float lifetime;
    private float phaseHealthThreshold;
    private bool bossPhaseTwo;

    public PrototypeHostileRobotRole Role => role;
    public bool BossPhaseTwo => bossPhaseTwo;

    public void Configure(PrototypeGameConfig gameConfig, PrototypePoliceCoordinator policeCoordinator, PrototypeHostileRobotRole hostileRole)
    {
        coordinator = policeCoordinator;
        role = hostileRole;
        var maxHealth = role == PrototypeHostileRobotRole.Scout ? gameConfig.hostileScoutHealth
            : role == PrototypeHostileRobotRole.RangedAssault ? gameConfig.hostileRangedHealth
            : role == PrototypeHostileRobotRole.HeavyMech ? gameConfig.hostileHeavyHealth
            : gameConfig.hostileHeavyHealth * 1.6f;
        lifetime = gameConfig.policeUnitLifetime * 2f;
        phaseHealthThreshold = maxHealth * gameConfig.hostileBossPhaseTwoHealthPercent;
        ConfigureBase(gameConfig, PrototypeFaction.HostileRobot, maxHealth);
        health.ConfigureResistances(role == PrototypeHostileRobotRole.HeavyMech || role == PrototypeHostileRobotRole.Boss ? 0.8f : 1f, 0.95f, 0.7f, role == PrototypeHostileRobotRole.HeavyMech || role == PrototypeHostileRobotRole.Boss ? 0.72f : 1f);
        SetState(PrototypeAiState.Pursue);
    }

    public override bool IsPursuing => State == PrototypeAiState.Pursue || State == PrototypeAiState.Attack;

    protected override float GetThinkInterval()
    {
        return role == PrototypeHostileRobotRole.Scout ? 0.12f : role == PrototypeHostileRobotRole.HeavyMech || role == PrototypeHostileRobotRole.Boss ? 0.28f : 0.2f;
    }

    protected override void Think()
    {
        lifetime -= GetThinkInterval();
        if (lifetime <= 0f)
        {
            coordinator?.DespawnHostileRobot(this);
            return;
        }

        if (role == PrototypeHostileRobotRole.Boss && !bossPhaseTwo && health != null && health.CurrentHealth <= phaseHealthThreshold)
        {
            bossPhaseTwo = true;
            SetState(PrototypeAiState.Recover, 1.2f);
        }

        if (!PrototypeAiUtility.IsValidTarget(target, targetHealth) || !HasVisibleTarget(config.hostileRobotDetectRange, 360f))
        {
            TryAcquireActivePlayer(config.hostileRobotDetectRange, 360f, false);
        }

        if (!PrototypeAiUtility.IsValidTarget(target, targetHealth))
        {
            SetState(PrototypeAiState.Idle);
            return;
        }

        var distance = Vector3.Distance(transform.position, target.position);
        var attackRange = role == PrototypeHostileRobotRole.Scout ? config.hostileScoutAttackRange
            : role == PrototypeHostileRobotRole.RangedAssault ? config.hostileRangedAttackRange
            : config.hostileHeavyAttackRange;
        if (distance > attackRange || (role == PrototypeHostileRobotRole.RangedAssault && distance < 5f))
        {
            MoveGround(target.position + PreferredOffset(), GetMovementSpeed());
            SetState(PrototypeAiState.Pursue);
            return;
        }

        SetState(PrototypeAiState.Attack);
        if (attackTimer > 0f)
        {
            return;
        }

        attackTimer = GetAttackCooldown();
        if (role == PrototypeHostileRobotRole.RangedAssault)
        {
            FireProjectile();
        }
        else if (role == PrototypeHostileRobotRole.HeavyMech || role == PrototypeHostileRobotRole.Boss)
        {
            PrototypeCombatUtility.ApplyAreaDamage(transform.position + transform.forward * 1.8f + Vector3.up * 1f, config.hostileHeavyAttackRange, GetAttackDamage(), PrototypeDamageType.Impact, gameObject, 16, role == PrototypeHostileRobotRole.Boss ? 7f : 5f);
        }
        else
        {
            DamageTarget(GetAttackDamage(), PrototypeDamageType.Physical, 2f);
        }
    }

    private Vector3 PreferredOffset()
    {
        if (role != PrototypeHostileRobotRole.RangedAssault || target == null)
        {
            return Vector3.zero;
        }

        var away = PrototypeAiUtility.FlattenedDirection(target.position, transform.position);
        return away * 7f;
    }

    private float GetMovementSpeed()
    {
        if (role == PrototypeHostileRobotRole.Scout)
        {
            return config.hostileScoutSpeed;
        }

        if (role == PrototypeHostileRobotRole.RangedAssault)
        {
            return config.hostileRangedSpeed;
        }

        return config.hostileHeavySpeed;
    }

    private float GetAttackCooldown()
    {
        if (role == PrototypeHostileRobotRole.Scout)
        {
            return config.hostileScoutCooldown;
        }

        if (role == PrototypeHostileRobotRole.RangedAssault)
        {
            return config.hostileRangedCooldown;
        }

        return config.hostileHeavyCooldown;
    }

    private float GetAttackDamage()
    {
        if (role == PrototypeHostileRobotRole.Scout)
        {
            return config.hostileScoutDamage;
        }

        if (role == PrototypeHostileRobotRole.RangedAssault)
        {
            return config.hostileRangedDamage;
        }

        return config.hostileHeavyDamage * (bossPhaseTwo ? 1.25f : 1f);
    }

    private void FireProjectile()
    {
        if (!PrototypeAiUtility.IsValidTarget(target, targetHealth))
        {
            return;
        }

        var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "HostileRobotBolt";
        projectileObject.layer = 2;
        projectileObject.transform.position = transform.position + Vector3.up * 1.1f + transform.forward * 0.5f;
        projectileObject.transform.localScale = Vector3.one * 0.3f;
        var collider = projectileObject.GetComponent<Collider>();
        collider.isTrigger = true;
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        material.color = new Color(1f, 0.18f, 0.1f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(1f, 0.1f, 0.02f) * 3f);
        projectileObject.GetComponent<Renderer>().sharedMaterial = material;
        var direction = (target.position + Vector3.up * 0.8f - projectileObject.transform.position).normalized;
        projectileObject.AddComponent<PrototypeProjectile>().Configure(direction, config.hostileRangedProjectileSpeed, GetAttackDamage(), gameObject, PrototypeDamageType.Energy);
    }

    protected override void OnAgentDied(PrototypeHealth deadHealth)
    {
        base.OnAgentDied(deadHealth);
        coordinator?.NotifyHostileRobotDefeated(this);
        if (Application.isPlaying)
        {
            Destroy(gameObject, 0.45f);
        }
    }
}
