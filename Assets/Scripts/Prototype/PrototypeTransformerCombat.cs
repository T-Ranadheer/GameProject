using System.Collections.Generic;
using UnityEngine;

public sealed class PrototypeTransformerCombat : MonoBehaviour
{
    private PrototypeGameConfig config;
    private PrototypeTransformerController transformer;
    private PrototypeHealth health;
    private float primaryCooldown;
    private float secondaryCooldown;
    private float collisionCooldown;
    private Transform primaryMuzzle;
    private Transform secondaryMuzzle;

    public bool CanPrimaryAttack => primaryCooldown <= 0f;
    public bool CanSecondaryAttack => secondaryCooldown <= 0f;
    public float PrimaryCooldownRemaining => primaryCooldown;
    public float SecondaryCooldownRemaining => secondaryCooldown;

    public void Configure(PrototypeGameConfig gameConfig, PrototypeTransformerController controller)
    {
        config = gameConfig;
        transformer = controller;
        health = GetComponent<PrototypeHealth>();
        primaryMuzzle = CreateMuzzle("PrimaryMuzzle", new Vector3(0f, 1.15f, 1.25f));
        secondaryMuzzle = CreateMuzzle("SecondaryMuzzle", new Vector3(0f, 0.95f, 1.35f));
    }

    public void ConfigureMuzzles(Transform primary, Transform secondary)
    {
        primaryMuzzle = primary != null ? primary : primaryMuzzle;
        secondaryMuzzle = secondary != null ? secondary : secondaryMuzzle;
    }

    private void Update()
    {
        primaryCooldown = Mathf.Max(0f, primaryCooldown - Time.deltaTime);
        secondaryCooldown = Mathf.Max(0f, secondaryCooldown - Time.deltaTime);
        collisionCooldown = Mathf.Max(0f, collisionCooldown - Time.deltaTime);
        if (PrototypeGameManager.IsPaused || transformer == null || !transformer.IsDeployed || health == null || !health.IsAlive)
        {
            return;
        }

        if (PrototypeInput.RobotMeleePressed && transformer.IsRobotForm)
        {
            TryMeleeAttack();
        }

        if (PrototypeInput.AttackPressed)
        {
            TryPrimaryAttack();
        }

        if (PrototypeInput.SecondaryAttackPressed)
        {
            TrySecondaryAttack();
        }
    }

    public bool TryMeleeAttack()
    {
        if (!CanPrimaryAttack || transformer == null || !transformer.IsRobotForm)
        {
            return false;
        }

        var center = transform.position + transform.forward * (config.robotMeleeRange * 0.55f) + Vector3.up * 1f;
        var hitTargets = new HashSet<PrototypeHealth>();
        foreach (var collider in Physics.OverlapSphere(center, config.robotMeleeRange * 0.65f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            var enemy = collider.GetComponentInParent<PrototypeEnemy>();
            var enemyHealth = enemy != null ? enemy.GetComponent<PrototypeHealth>() : null;
            if (enemy != null && enemy.IsAlive && enemyHealth != null && hitTargets.Add(enemyHealth))
            {
                enemyHealth.ApplyDamage(new PrototypeDamageInfo(config.robotMeleeDamage, PrototypeDamageType.Impact, enemy.AimPoint, transform.forward), gameObject);
            }
        }

        primaryCooldown = config.robotMeleeCooldown;
        GetComponent<PrototypeProceduralPresentation>()?.PulseAttack(1f);
        GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.HeavyImpact);
        return true;
    }

    public bool TryPrimaryAttack()
    {
        if (!CanPrimaryAttack || transformer == null || health == null || !health.IsAlive)
        {
            return false;
        }

        var damage = config.robotProjectileDamage;
        var cooldown = config.robotProjectileCooldown;
        var speed = config.projectileSpeed;
        var type = PrototypeDamageType.Energy;
        var scale = 0.32f;
        switch (transformer.VehicleForm)
        {
            case PrototypeRobotForm.Helicopter:
                damage = config.helicopterPrimaryDamage;
                cooldown = config.helicopterPrimaryCooldown;
                speed = config.projectileSpeed * 1.2f;
                scale = 0.28f;
                break;
            case PrototypeRobotForm.Tank:
                damage = config.tankCannonDamage;
                cooldown = config.tankCannonCooldown;
                speed = config.projectileSpeed * 0.8f;
                type = PrototypeDamageType.Impact;
                scale = 0.5f;
                break;
            case PrototypeRobotForm.Motorcycle:
                damage = config.robotProjectileDamage * 0.82f;
                cooldown = config.robotProjectileCooldown * 0.8f;
                speed = config.projectileSpeed * 1.25f;
                break;
            case PrototypeRobotForm.Jet:
                damage = config.jetPrimaryDamage;
                cooldown = config.jetPrimaryCooldown;
                speed = config.projectileSpeed * 1.35f;
                scale = 0.24f;
                break;
        }

        if (!transformer.IsRobotForm && transformer.IsVehicleForm)
        {
            SpawnProjectile(primaryMuzzle, damage, speed, type, scale, false);
        }
        else if (transformer.IsRobotForm)
        {
            SpawnProjectile(primaryMuzzle, config.robotProjectileDamage, config.projectileSpeed * 0.9f, PrototypeDamageType.Energy, 0.36f, false);
        }
        else
        {
            return false;
        }

        primaryCooldown = cooldown;
        GetComponent<PrototypeProceduralPresentation>()?.PulseAttack(0.8f);
        GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.Attack);
        return true;
    }

    public bool TrySecondaryAttack()
    {
        if (!CanSecondaryAttack || transformer == null || !transformer.IsVehicleForm)
        {
            return false;
        }

        float damage;
        float cooldown;
        float speed;
        switch (transformer.VehicleForm)
        {
            case PrototypeRobotForm.Helicopter:
                damage = config.helicopterSecondaryDamage;
                cooldown = config.helicopterSecondaryCooldown;
                speed = config.projectileSpeed * 0.75f;
                break;
            case PrototypeRobotForm.Tank:
                damage = config.tankSecondaryDamage;
                cooldown = config.tankSecondaryCooldown;
                speed = config.projectileSpeed;
                break;
            case PrototypeRobotForm.Motorcycle:
                damage = config.motorcycleCollisionDamage;
                cooldown = 1.8f;
                speed = config.projectileSpeed * 1.15f;
                break;
            case PrototypeRobotForm.Jet:
                damage = config.jetSecondaryDamage;
                cooldown = config.jetSecondaryCooldown;
                speed = config.projectileSpeed * 0.9f;
                break;
            default:
                return false;
        }

        SpawnProjectile(secondaryMuzzle, damage, speed, PrototypeDamageType.Impact, 0.46f, true);
        secondaryCooldown = cooldown;
        GetComponent<PrototypeProceduralPresentation>()?.PulseAttack(1.1f);
        GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.HeavyImpact);
        return true;
    }

    public void HandleVehicleCollision(Collision collision)
    {
        if (collision == null || transformer == null || !transformer.IsVehicleForm || collisionCooldown > 0f)
        {
            return;
        }

        var enemy = collision.collider != null ? collision.collider.GetComponentInParent<PrototypeEnemy>() : null;
        if (enemy == null || !enemy.IsAlive)
        {
            return;
        }

        var impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < 4f)
        {
            return;
        }

        var damage = transformer.VehicleForm == PrototypeRobotForm.Motorcycle
            ? config.motorcycleCollisionDamage
            : Mathf.Clamp(impactSpeed * 2.2f, 12f, 70f);
        enemy.GetComponent<PrototypeHealth>()?.ApplyDamage(new PrototypeDamageInfo(damage, PrototypeDamageType.Impact, collision.GetContact(0).point, collision.relativeVelocity.normalized), gameObject);
        collisionCooldown = 0.35f;
    }

    private void SpawnProjectile(Transform muzzle, float damage, float speed, PrototypeDamageType type, float scale, bool heavy)
    {
        if (muzzle == null)
        {
            return;
        }

        var camera = Camera.main;
        var direction = muzzle.forward;
        if (camera != null)
        {
            direction = camera.transform.forward;
            if (Physics.Raycast(camera.transform.position, direction, out var hit, 140f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                var enemy = hit.collider.GetComponentInParent<PrototypeEnemy>();
                if (enemy != null && enemy.IsAlive)
                {
                    direction = (enemy.AimPoint - muzzle.position).normalized;
                }
            }
        }

        var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = heavy ? "TransformerHeavyProjectile" : "TransformerProjectile";
        projectileObject.layer = 2;
        projectileObject.transform.position = muzzle.position + direction.normalized * 0.45f;
        projectileObject.transform.localScale = Vector3.one * scale;
        var collider = projectileObject.GetComponent<Collider>();
        collider.isTrigger = true;
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        material.color = heavy ? new Color(1f, 0.28f, 0.08f) : new Color(0.15f, 0.75f, 1f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", material.color * (heavy ? 3.5f : 2.5f));
        projectileObject.GetComponent<Renderer>().material = material;
        var projectile = projectileObject.AddComponent<PrototypeProjectile>();
        projectile.Configure(direction, speed, damage, gameObject, type);
    }

    private Transform CreateMuzzle(string name, Vector3 localPosition)
    {
        var muzzleObject = new GameObject(name);
        muzzleObject.transform.SetParent(transform, false);
        muzzleObject.transform.localPosition = localPosition;
        return muzzleObject.transform;
    }
}
