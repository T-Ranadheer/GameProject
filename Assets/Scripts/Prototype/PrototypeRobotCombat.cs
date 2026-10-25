using UnityEngine;

public sealed class PrototypeRobotCombat : MonoBehaviour
{
    private PrototypeGameConfig config;
    private PrototypeRobotController robot;
    private PrototypeHealth health;
    private float meleeCooldown;
    private float rangedCooldown;
    private Transform muzzle;

    public bool CanMelee => meleeCooldown <= 0f;
    public bool CanFire => rangedCooldown <= 0f;

    public void Configure(PrototypeGameConfig gameConfig)
    {
        config = gameConfig;
        robot = GetComponent<PrototypeRobotController>();
        health = GetComponent<PrototypeHealth>();
        var muzzleObject = new GameObject("RobotEnergyMuzzle");
        muzzleObject.transform.SetParent(transform, false);
        muzzleObject.transform.localPosition = new Vector3(0f, 1.3f, 1f);
        muzzle = muzzleObject.transform;
    }

    private void Update()
    {
        meleeCooldown = Mathf.Max(0f, meleeCooldown - Time.deltaTime);
        rangedCooldown = Mathf.Max(0f, rangedCooldown - Time.deltaTime);
        if (PrototypeGameManager.IsPaused || robot == null || !robot.IsDeployed || !robot.IsRobotForm || health == null || !health.IsAlive)
        {
            return;
        }

        if (PrototypeInput.RobotMeleePressed)
        {
            TryMeleeAttack();
        }

        if (PrototypeInput.AttackPressed)
        {
            TryRangedAttack();
        }
    }

    public bool TryMeleeAttack()
    {
        if (!CanMelee || robot == null || !robot.IsRobotForm)
        {
            return false;
        }

        var center = transform.position + transform.forward * (config.robotMeleeRange * 0.55f) + Vector3.up * 1f;
        var hitTargets = new System.Collections.Generic.HashSet<PrototypeHealth>();
        foreach (var collider in Physics.OverlapSphere(center, config.robotMeleeRange * 0.65f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            var enemy = collider.GetComponentInParent<PrototypeEnemy>();
            var enemyHealth = enemy != null ? enemy.GetComponent<PrototypeHealth>() : null;
            if (enemy != null && enemy.IsAlive && enemyHealth != null && hitTargets.Add(enemyHealth))
            {
                enemyHealth.ApplyDamage(new PrototypeDamageInfo(config.robotMeleeDamage, PrototypeDamageType.Impact, enemy.AimPoint, transform.forward), gameObject);
            }
        }

        meleeCooldown = config.robotMeleeCooldown;
        GetComponent<PrototypeProceduralPresentation>()?.PulseAttack(1f);
        GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.HeavyImpact);
        return true;
    }

    public bool TryRangedAttack()
    {
        if (!CanFire || robot == null || !robot.IsRobotForm)
        {
            return false;
        }

        var camera = Camera.main;
        var direction = camera != null ? camera.transform.forward : transform.forward;
        var origin = camera != null ? camera.transform.position : muzzle.position;
        if (camera != null && Physics.Raycast(origin, direction, out var hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            var enemy = hit.collider.GetComponentInParent<PrototypeEnemy>();
            if (enemy != null && enemy.IsAlive)
            {
                direction = (enemy.AimPoint - muzzle.position).normalized;
            }
        }

        var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "RobotEnergyBolt";
        projectileObject.layer = 2;
        projectileObject.transform.position = muzzle.position + direction.normalized * 0.4f;
        projectileObject.transform.localScale = Vector3.one * 0.36f;
        var collider = projectileObject.GetComponent<Collider>();
        collider.isTrigger = true;
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        material.color = new Color(1f, 0.45f, 0.08f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(1f, 0.18f, 0.02f) * 4f);
        projectileObject.GetComponent<Renderer>().material = material;
        var projectile = projectileObject.AddComponent<PrototypeProjectile>();
        projectile.Configure(direction, config.projectileSpeed * 0.9f, config.robotProjectileDamage, gameObject);
        rangedCooldown = config.robotProjectileCooldown;
        GetComponent<PrototypeProceduralPresentation>()?.PulseAttack(0.8f);
        GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.Attack);
        return true;
    }
}
