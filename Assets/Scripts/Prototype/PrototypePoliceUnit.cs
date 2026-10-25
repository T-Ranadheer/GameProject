using UnityEngine;

public sealed class PrototypePoliceUnit : PrototypeAiAgent
{
    private PrototypePoliceCoordinator coordinator;
    private PrototypePoliceRole role;
    private Vector3 patrolOrigin;
    private float patrolAngle;
    private float incidentTimer;
    private float lifetime;
    private bool isDrone;
    private Rigidbody vehicleBody;

    public PrototypePoliceRole Role => role;
    public bool IsDrone => isDrone;
    public override bool IsPursuing => State == PrototypeAiState.Pursue || State == PrototypeAiState.Attack;

    public void Configure(PrototypeGameConfig gameConfig, PrototypePoliceCoordinator policeCoordinator, PrototypePoliceRole policeRole, Vector3 origin)
    {
        coordinator = policeCoordinator;
        role = policeRole;
        isDrone = role == PrototypePoliceRole.PoliceDrone;
        vehicleBody = role == PrototypePoliceRole.PatrolVehicle ? GetComponent<Rigidbody>() : null;
        patrolOrigin = origin;
        patrolAngle = 0f;
        lifetime = gameConfig != null ? gameConfig.policeUnitLifetime : 35f;
        var maxHealth = role == PrototypePoliceRole.TacticalRobot || role == PrototypePoliceRole.PatrolRobot ? gameConfig.playerHealth * 1.35f : gameConfig.enemyHealth * 1.3f;
        ConfigureBase(gameConfig, PrototypeFaction.Police, maxHealth);
        if (isDrone)
        {
            var existingController = GetComponent<CharacterController>();
            if (existingController != null)
            {
                existingController.enabled = false;
            }
        }

        SetState(PrototypeAiState.Patrol);
    }

    protected override float GetThinkInterval()
    {
        return isDrone ? Mathf.Max(0.1f, config.policePerceptionInterval * 1.5f) : Mathf.Max(0.1f, config.policePerceptionInterval);
    }

    protected override void Think()
    {
        if (coordinator == null)
        {
            return;
        }

        lifetime -= GetThinkInterval();
        if (lifetime <= 0f && coordinator.Wanted.Level == PrototypeWantedLevel.Clear)
        {
            coordinator.DespawnPoliceUnit(this);
            return;
        }

        var wanted = coordinator.Wanted.Level;
        if (wanted == PrototypeWantedLevel.Clear)
        {
            ClearTarget();
            SetState(PrototypeAiState.Patrol);
            Patrol();
            return;
        }

        var canSee = HasVisibleTarget(config.policePerceptionRange, config.policeFieldOfView);
        if (!canSee && TryAcquireActivePlayer(config.policePerceptionRange, config.policeFieldOfView, true))
        {
            canSee = true;
        }

        if (canSee)
        {
            lastKnownPosition = target.position;
            hasLastKnownPosition = true;
            SetState(PrototypeAiState.Pursue, config.policeSearchDuration);
        }

        if (State == PrototypeAiState.Pursue || State == PrototypeAiState.Attack)
        {
            PursueAndAttack();
        }
        else if (State == PrototypeAiState.Search || (hasLastKnownPosition && stateTimer > 0f))
        {
            Search();
        }
        else
        {
            SetState(PrototypeAiState.Investigate, config.policeInvestigationDuration);
            Search();
        }
    }

    private void Patrol()
    {
        patrolAngle += 32f * GetThinkInterval();
        var radius = role == PrototypePoliceRole.PatrolVehicle ? 7f : 4f;
        var destination = patrolOrigin + new Vector3(Mathf.Cos(patrolAngle * Mathf.Deg2Rad), 0f, Mathf.Sin(patrolAngle * Mathf.Deg2Rad)) * radius;
        if (isDrone)
        {
            destination.y = patrolOrigin.y;
            MoveDrone(destination, config.policeDroneSpeed);
        }
        else
        {
            MoveGround(destination, config.policePatrolSpeed);
        }
    }

    private void PursueAndAttack()
    {
        if (!PrototypeAiUtility.IsValidTarget(target, targetHealth))
        {
            ClearTarget();
            SetState(PrototypeAiState.Search, config.policeSearchDuration);
            return;
        }

        lastKnownPosition = target.position;
        hasLastKnownPosition = true;
        var distance = Vector3.Distance(transform.position, target.position);
        var attackRange = role == PrototypePoliceRole.PatrolRobot || role == PrototypePoliceRole.TacticalRobot ? config.policeAttackRange + 0.8f : config.policeAttackRange;
        if (distance > attackRange)
        {
            if (isDrone)
            {
                var destination = target.position + Vector3.up * config.policeDroneHeight;
                MoveDrone(destination, config.policeDroneSpeed);
            }
            else
            {
                MoveGround(target.position, role == PrototypePoliceRole.PatrolVehicle ? config.policePursuitSpeed + 1.5f : config.policePursuitSpeed);
            }

            SetState(PrototypeAiState.Pursue, config.policeSearchDuration);
            return;
        }

        SetState(PrototypeAiState.Attack, config.policeSearchDuration);
        if (attackTimer > 0f)
        {
            return;
        }

        attackTimer = role == PrototypePoliceRole.TacticalRobot ? config.policeProjectileCooldown : config.policeAttackCooldown;
        if (role == PrototypePoliceRole.TacticalRobot || role == PrototypePoliceRole.PoliceDrone)
        {
            FireProjectile();
        }
        else
        {
            DamageTarget(config.policeAttackDamage, PrototypeDamageType.Physical, role == PrototypePoliceRole.PatrolRobot ? 2f : 0.5f);
        }
    }

    private void Search()
    {
        if (!hasLastKnownPosition || stateTimer <= 0f)
        {
            SetState(PrototypeAiState.Patrol);
            return;
        }

        if (isDrone)
        {
            MoveDrone(lastKnownPosition + Vector3.up * config.policeDroneHeight, config.policeDroneSpeed);
        }
        else
        {
            MoveGround(lastKnownPosition, config.policePatrolSpeed + 0.8f);
        }

        SetState(PrototypeAiState.Search, stateTimer);
    }

    private void FireProjectile()
    {
        if (!PrototypeAiUtility.IsValidTarget(target, targetHealth))
        {
            return;
        }

        var objectName = role == PrototypePoliceRole.PoliceDrone ? "PoliceDronePulse" : "TacticalPoliceBolt";
        var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = objectName;
        projectileObject.layer = 2;
        projectileObject.transform.position = transform.position + Vector3.up * (isDrone ? 0f : 1f) + transform.forward * 0.6f;
        projectileObject.transform.localScale = Vector3.one * 0.22f;
        var collider = projectileObject.GetComponent<Collider>();
        collider.isTrigger = true;
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        material.color = role == PrototypePoliceRole.PoliceDrone ? new Color(0.3f, 0.75f, 1f) : new Color(0.25f, 0.55f, 1f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", material.color * 3f);
        projectileObject.GetComponent<Renderer>().sharedMaterial = material;
        var direction = (target.position + Vector3.up * 0.8f - projectileObject.transform.position).normalized;
        projectileObject.AddComponent<PrototypeProjectile>().Configure(direction, config.projectileSpeed * 0.8f, config.policeProjectileDamage, gameObject, PrototypeDamageType.Energy);
    }

    protected override void OnAgentDied(PrototypeHealth deadHealth)
    {
        base.OnAgentDied(deadHealth);
        coordinator?.NotifyPoliceUnitDefeated(this);
        if (Application.isPlaying)
        {
            Destroy(gameObject, 0.45f);
        }
    }

    private void FixedUpdate()
    {
        if (role != PrototypePoliceRole.PatrolVehicle || vehicleBody == null || vehicleBody.isKinematic)
        {
            return;
        }

        vehicleBody.linearVelocity = Vector3.ClampMagnitude(vehicleBody.linearVelocity, config.policePursuitSpeed + 2f);
        if (vehicleBody.angularVelocity.sqrMagnitude > 12f)
        {
            vehicleBody.angularVelocity = Vector3.ClampMagnitude(vehicleBody.angularVelocity, 3.5f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (role != PrototypePoliceRole.PatrolVehicle || vehicleBody == null || collision == null)
        {
            return;
        }

        vehicleBody.linearVelocity = Vector3.ProjectOnPlane(vehicleBody.linearVelocity, Vector3.up) * 0.45f;
        vehicleBody.angularVelocity = Vector3.zero;
    }
}
