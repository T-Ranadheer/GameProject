using UnityEngine;

[RequireComponent(typeof(PrototypeHealth))]
public sealed class PrototypePlayerAbility : MonoBehaviour
{
    private PrototypeGameConfig config;
    private float cooldownRemaining;
    private Transform muzzle;
    private PrototypeEnergy energy;

    public float CooldownRemaining => cooldownRemaining;
    public bool CanFire => cooldownRemaining <= 0f;

    public void Configure(PrototypeGameConfig gameConfig)
    {
        config = gameConfig;
        energy = GetComponent<PrototypeEnergy>();
        if (energy == null)
        {
            energy = gameObject.AddComponent<PrototypeEnergy>();
        }

        energy.Configure(config.energyMaximum, config.energyRegeneration, config.energyRegenerationDelay);
        var muzzleObject = new GameObject("EnergyMuzzle");
        muzzleObject.transform.SetParent(transform, false);
        muzzleObject.transform.localPosition = new Vector3(0f, 1.05f, 0.85f);
        muzzle = muzzleObject.transform;
    }

    private void Update()
    {
        cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
        if (!PrototypeGameManager.IsPaused && PrototypeInput.AttackPressed)
        {
            TryFireAtCameraTarget();
        }
    }

    public bool TryFireAt(Vector3 direction)
    {
        if (config == null || energy == null || !CanFire || direction.sqrMagnitude < 0.001f || !energy.TrySpend(config.projectileEnergyCost))
        {
            return false;
        }

        var start = muzzle != null ? muzzle.position : transform.position + Vector3.up;
        var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "EnergyBolt";
        projectileObject.layer = 2;
        projectileObject.transform.position = start + direction.normalized * 0.5f;
        projectileObject.transform.localScale = Vector3.one * 0.28f;
        var collider = projectileObject.GetComponent<Collider>();
        collider.isTrigger = true;

        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = new Color(0.15f, 0.85f, 1f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(0.05f, 0.55f, 1f) * 5f);
        projectileObject.GetComponent<Renderer>().material = material;

        var trail = projectileObject.AddComponent<TrailRenderer>();
        trail.time = 0.18f;
        trail.startWidth = 0.13f;
        trail.endWidth = 0f;
        trail.material = material;

        var projectile = projectileObject.AddComponent<PrototypeProjectile>();
        projectile.Configure(direction, config.projectileSpeed, config.projectileDamage, gameObject);
        cooldownRemaining = config.projectileCooldown;
        GetComponent<PrototypeProceduralPresentation>()?.PulseAttack(0.75f);
        GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.Attack);
        return true;
    }

    public void ResetCooldown()
    {
        cooldownRemaining = 0f;
    }

    private void TryFireAtCameraTarget()
    {
        var camera = Camera.main;
        var direction = camera != null ? camera.transform.forward : transform.forward;
        var origin = camera != null ? camera.transform.position : transform.position + Vector3.up;

        if (camera != null && Physics.Raycast(origin, direction, out var hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            var enemy = hit.collider.GetComponentInParent<PrototypeEnemy>();
            if (enemy != null && enemy.IsAlive)
            {
                direction = (enemy.AimPoint - (muzzle != null ? muzzle.position : transform.position)).normalized;
            }
        }
        else
        {
            var nearest = FindBestTarget(origin, direction);
            if (nearest != null)
            {
                direction = (nearest.AimPoint - (muzzle != null ? muzzle.position : transform.position)).normalized;
            }
        }

        TryFireAt(direction);
    }

    private PrototypeEnemy FindBestTarget(Vector3 origin, Vector3 forward)
    {
        PrototypeEnemy best = null;
        var bestDistance = float.MaxValue;
        foreach (var enemy in FindObjectsByType<PrototypeEnemy>(FindObjectsSortMode.None))
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            var offset = enemy.AimPoint - origin;
            var distance = offset.magnitude;
            if (distance > 45f || Vector3.Dot(forward, offset.normalized) < 0.72f || distance >= bestDistance)
            {
                continue;
            }

            best = enemy;
            bestDistance = distance;
        }

        return best;
    }
}
