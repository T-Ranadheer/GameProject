using System.Collections.Generic;
using UnityEngine;

public sealed class PrototypeElectricAbilities : MonoBehaviour
{
    private PrototypeGameConfig config;
    private PrototypeElectricController controller;
    private PrototypeEnergy energy;
    private PrototypeHealth health;
    private Transform muzzle;
    private float boltCooldown;
    private float strikeCooldown;
    private float chainCooldown;
    private float shockwaveCooldown;
    private float meleeCooldown;

    public float BoltCooldownRemaining => boltCooldown;
    public float LightningStrikeCooldownRemaining => strikeCooldown;
    public float ChainLightningCooldownRemaining => chainCooldown;
    public float ShockwaveCooldownRemaining => shockwaveCooldown;
    public float ChargedMeleeCooldownRemaining => meleeCooldown;

    public void Configure(PrototypeGameConfig gameConfig)
    {
        config = gameConfig;
        controller = GetComponent<PrototypeElectricController>();
        health = GetComponent<PrototypeHealth>();
        energy = GetComponent<PrototypeEnergy>();
        var muzzleObject = new GameObject("ElectricMuzzle");
        muzzleObject.transform.SetParent(transform, false);
        muzzleObject.transform.localPosition = new Vector3(0f, 1.25f, 0.9f);
        muzzle = muzzleObject.transform;
    }

    private void Update()
    {
        boltCooldown = Mathf.Max(0f, boltCooldown - Time.deltaTime);
        strikeCooldown = Mathf.Max(0f, strikeCooldown - Time.deltaTime);
        chainCooldown = Mathf.Max(0f, chainCooldown - Time.deltaTime);
        shockwaveCooldown = Mathf.Max(0f, shockwaveCooldown - Time.deltaTime);
        meleeCooldown = Mathf.Max(0f, meleeCooldown - Time.deltaTime);

        if (PrototypeGameManager.IsPaused || controller == null || !controller.IsAlive || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (PrototypeInput.AttackPressed)
        {
            TryElectricBolt(GetAimDirection());
        }

        if (PrototypeInput.Ability2Pressed)
        {
            TryLightningStrike(GetAimPoint());
        }

        if (PrototypeInput.Ability3Pressed)
        {
            TryChainLightning(null);
        }

        if (PrototypeInput.Ability4Pressed)
        {
            TryElectricShockwave();
        }

        if (PrototypeInput.RobotMeleePressed)
        {
            TryChargedMelee();
        }
    }

    public bool TryElectricBolt(Vector3 direction)
    {
        if (config == null || energy == null || boltCooldown > 0f || direction.sqrMagnitude < 0.001f || !energy.TrySpend(config.electricBoltEnergyCost))
        {
            return false;
        }

        var start = muzzle != null ? muzzle.position : transform.position + Vector3.up;
        var projectileObject = CreateEffectSphere("ElectricBolt", start + direction.normalized * 0.45f, 0.24f, new Color(0.3f, 0.9f, 1f), true);
        var trail = projectileObject.AddComponent<TrailRenderer>();
        trail.time = 0.22f;
        trail.startWidth = 0.14f;
        trail.endWidth = 0f;
        trail.sharedMaterial = projectileObject.GetComponent<Renderer>().sharedMaterial;
        projectileObject.AddComponent<PrototypeProjectile>().Configure(direction, config.electricBoltSpeed, config.electricBoltDamage, gameObject, PrototypeDamageType.Electric);
        boltCooldown = config.electricBoltCooldown;
        GetComponent<PrototypeProceduralPresentation>()?.PulseAttack(0.85f);
        GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.Lightning);
        return true;
    }

    public bool TryLightningStrike(Vector3 point)
    {
        if (config == null || energy == null || strikeCooldown > 0f || !energy.TrySpend(config.lightningStrikeEnergyCost))
        {
            return false;
        }

        PrototypeCombatUtility.ApplyAreaDamage(point, config.lightningStrikeRadius, config.lightningStrikeDamage, PrototypeDamageType.Electric, gameObject, 64, 1.5f);
        CreateAreaEffect("LightningStrike", point, config.lightningStrikeRadius, new Color(0.4f, 0.8f, 1f));
        CreateLightningArc(point);
        strikeCooldown = config.lightningStrikeCooldown;
        GetComponent<PrototypeProceduralPresentation>()?.PulseAttack(1f);
        GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.Lightning);
        return true;
    }

    public bool TryChainLightning(PrototypeEnemy initialTarget)
    {
        if (config == null || energy == null || chainCooldown > 0f || !energy.TrySpend(config.chainLightningEnergyCost))
        {
            return false;
        }

        var first = PrototypeCombatUtility.IsValidEnemy(initialTarget) ? initialTarget : PrototypeCombatUtility.FindNearestEnemy(transform.position + transform.forward * 3f, 18f);
        if (first == null)
        {
            energy.Restore(config.chainLightningEnergyCost);
            return false;
        }

        var hitTargets = new HashSet<PrototypeEnemy> { first };
        var arcPoints = new List<Vector3> { transform.position + Vector3.up * 1.2f, first.AimPoint };
        first.GetComponent<PrototypeHealth>().ApplyDamage(new PrototypeDamageInfo(config.chainLightningDamage, PrototypeDamageType.Electric, first.AimPoint, (first.transform.position - transform.position).normalized), gameObject);
        var current = first;
        for (var i = 1; i < config.chainLightningMaxTargets; i++)
        {
            var next = FindNearestUnhit(current.AimPoint, config.chainLightningRadius, hitTargets);
            if (next == null)
            {
                break;
            }

            hitTargets.Add(next);
            next.GetComponent<PrototypeHealth>().ApplyDamage(new PrototypeDamageInfo(config.chainLightningDamage, PrototypeDamageType.Electric, next.AimPoint, (next.transform.position - current.transform.position).normalized), gameObject);
            arcPoints.Add(next.AimPoint);
            current = next;
        }

        CreateLightningArc(arcPoints);
        chainCooldown = config.chainLightningCooldown;
        GetComponent<PrototypeProceduralPresentation>()?.PulseAttack(1f);
        GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.Lightning);
        return true;
    }

    public bool TryElectricShockwave()
    {
        if (config == null || energy == null || shockwaveCooldown > 0f || !energy.TrySpend(config.electricShockwaveEnergyCost))
        {
            return false;
        }

        PrototypeCombatUtility.ApplyAreaDamage(transform.position + Vector3.up * 0.8f, config.electricShockwaveRadius, config.electricShockwaveDamage, PrototypeDamageType.Electric, gameObject, 64, 3f);
        CreateAreaEffect("ElectricShockwave", transform.position + Vector3.up * 0.15f, config.electricShockwaveRadius, new Color(0.1f, 0.65f, 1f));
        shockwaveCooldown = config.electricShockwaveCooldown;
        GetComponent<PrototypeProceduralPresentation>()?.PulseAttack(1.15f);
        GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.HeavyImpact);
        return true;
    }

    public bool TryChargedMelee()
    {
        if (config == null || energy == null || meleeCooldown > 0f || !energy.TrySpend(config.chargedMeleeEnergyCost))
        {
            return false;
        }

        var center = transform.position + transform.forward * (config.chargedMeleeRange * 0.62f) + Vector3.up * 1f;
        PrototypeCombatUtility.ApplyAreaDamage(center, config.chargedMeleeRange * 0.62f, config.chargedMeleeDamage, PrototypeDamageType.Electric, gameObject, 16, 4f);
        CreateAreaEffect("ChargedMelee", center, config.chargedMeleeRange * 0.55f, new Color(0.25f, 0.85f, 1f));
        meleeCooldown = config.chargedMeleeCooldown;
        GetComponent<PrototypeProceduralPresentation>()?.PulseAttack(0.9f);
        GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.Attack);
        return true;
    }

    private Vector3 GetAimDirection()
    {
        var camera = Camera.main;
        var direction = camera != null ? camera.transform.forward : transform.forward;
        var origin = camera != null ? camera.transform.position : transform.position + Vector3.up;
        var target = PrototypeCombatUtility.FindNearestEnemy(origin, 45f);
        if (target != null && Vector3.Dot(direction.normalized, (target.AimPoint - origin).normalized) > 0.5f)
        {
            direction = target.AimPoint - (muzzle != null ? muzzle.position : transform.position);
        }

        return direction.normalized;
    }

    private Vector3 GetAimPoint()
    {
        var camera = Camera.main;
        if (camera != null && Physics.Raycast(camera.transform.position, camera.transform.forward, out var hit, 70f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        var target = PrototypeCombatUtility.FindNearestEnemy(transform.position + transform.forward * 3f, 22f);
        return target != null ? target.AimPoint : transform.position + transform.forward * 8f;
    }

    private static PrototypeEnemy FindNearestUnhit(Vector3 origin, float radius, HashSet<PrototypeEnemy> alreadyHit)
    {
        PrototypeEnemy nearest = null;
        var nearestDistance = radius * radius;
        foreach (var enemy in Object.FindObjectsByType<PrototypeEnemy>(FindObjectsSortMode.None))
        {
            if (!PrototypeCombatUtility.IsValidEnemy(enemy) || alreadyHit.Contains(enemy))
            {
                continue;
            }

            var distance = (enemy.AimPoint - origin).sqrMagnitude;
            if (distance <= nearestDistance)
            {
                nearest = enemy;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private static GameObject CreateEffectSphere(string name, Vector3 position, float radius, Color color, bool emissive)
    {
        var effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = name;
        effect.layer = 2;
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * radius;
        DestroyObject(effect.GetComponent<Collider>());
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = color };
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 4f);
        }

        effect.GetComponent<Renderer>().sharedMaterial = material;
        return effect;
    }

    private static void CreateAreaEffect(string name, Vector3 position, float radius, Color color)
    {
        var effect = CreateEffectSphere(name, position, Mathf.Max(0.2f, radius * 0.25f), color, true);
        effect.transform.localScale = Vector3.one * Mathf.Max(0.2f, radius * 0.45f);
        if (Application.isPlaying)
        {
            Object.Destroy(effect, 0.28f);
        }
        else
        {
            Object.DestroyImmediate(effect);
        }
    }

    private static void CreateLightningArc(Vector3 point)
    {
        CreateLightningArc(new List<Vector3> { point + Vector3.up * 10f, point });
    }

    private static void CreateLightningArc(List<Vector3> points)
    {
        if (points == null || points.Count < 2)
        {
            return;
        }

        var arc = new GameObject("ElectricArc");
        var line = arc.AddComponent<LineRenderer>();
        line.positionCount = points.Count;
        line.startWidth = 0.11f;
        line.endWidth = 0.02f;
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        material.color = new Color(0.3f, 0.85f, 1f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(0.2f, 0.8f, 1f) * 3f);
        line.sharedMaterial = material;
        line.SetPositions(points.ToArray());
        if (Application.isPlaying)
        {
            Object.Destroy(arc, 0.18f);
        }
        else
        {
            Object.DestroyImmediate(arc);
        }
    }

    private static void DestroyObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(target);
        }
        else
        {
            Object.DestroyImmediate(target);
        }
    }
}
