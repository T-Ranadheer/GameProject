using UnityEngine;

public sealed class PrototypeStoneGiantCombat : MonoBehaviour
{
    private PrototypeGameConfig config;
    private PrototypeStoneGiantController giant;
    private PrototypeHealth health;
    private float punchCooldown;
    private float slamCooldown;
    private float shockwaveCooldown;

    public float PunchCooldownRemaining => punchCooldown;
    public float SlamCooldownRemaining => slamCooldown;
    public float ShockwaveCooldownRemaining => shockwaveCooldown;

    public void Configure(PrototypeGameConfig gameConfig)
    {
        config = gameConfig;
        giant = GetComponent<PrototypeStoneGiantController>();
        health = GetComponent<PrototypeHealth>();
    }

    private void Update()
    {
        punchCooldown = Mathf.Max(0f, punchCooldown - Time.deltaTime);
        slamCooldown = Mathf.Max(0f, slamCooldown - Time.deltaTime);
        shockwaveCooldown = Mathf.Max(0f, shockwaveCooldown - Time.deltaTime);

        if (PrototypeGameManager.IsPaused || giant == null || !giant.IsAlive || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (PrototypeInput.RobotMeleePressed)
        {
            TryHeavyPunch();
        }

        if (PrototypeInput.Ability2Pressed)
        {
            TryGroundSlam();
        }

        if (PrototypeInput.Ability3Pressed)
        {
            TryShockwave();
        }

        if (PrototypeInput.Ability4Pressed)
        {
            TryThrowProp();
        }
    }

    public bool TryHeavyPunch()
    {
        if (config == null || giant == null || punchCooldown > 0f || !giant.IsGrounded)
        {
            return false;
        }

        var center = transform.position + transform.forward * (config.giantPunchRange * 0.65f) + Vector3.up * 1.8f;
        PrototypeCombatUtility.ApplyAreaDamage(center, config.giantPunchRange * 0.6f, config.giantPunchDamage, PrototypeDamageType.Impact, gameObject, 16, 7f);
        CreateImpactEffect(center, config.giantPunchRange * 0.45f, new Color(0.55f, 0.42f, 0.3f));
        punchCooldown = config.giantPunchCooldown;
        GetComponent<PrototypeProceduralPresentation>()?.PulseAttack(1f);
        GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.HeavyImpact);
        return true;
    }

    public bool TryGroundSlam()
    {
        if (config == null || giant == null || slamCooldown > 0f || !giant.IsGrounded)
        {
            return false;
        }

        var center = transform.position + Vector3.down * 0.2f;
        PrototypeCombatUtility.ApplyAreaDamage(center, config.giantSlamRadius, config.giantSlamDamage, PrototypeDamageType.Impact, gameObject, 64, 9f);
        CreateImpactEffect(center, config.giantSlamRadius * 0.55f, new Color(0.44f, 0.32f, 0.22f));
        slamCooldown = config.giantSlamCooldown;
        GetComponent<PrototypeProceduralPresentation>()?.PulseAttack(1.35f);
        GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.HeavyImpact);
        return true;
    }

    public bool TryShockwave()
    {
        if (config == null || giant == null || shockwaveCooldown > 0f || !giant.IsGrounded)
        {
            return false;
        }

        PrototypeCombatUtility.ApplyAreaDamage(transform.position, config.giantShockwaveRadius, config.giantShockwaveDamage, PrototypeDamageType.Impact, gameObject, 64, 6f);
        CreateImpactEffect(transform.position, config.giantShockwaveRadius * 0.7f, new Color(0.6f, 0.44f, 0.3f));
        shockwaveCooldown = config.giantShockwaveCooldown;
        GetComponent<PrototypeProceduralPresentation>()?.PulseAttack(1.25f);
        GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.HeavyImpact);
        return true;
    }

    public bool TryThrowProp()
    {
        var colliders = Physics.OverlapSphere(transform.position + transform.forward * 2.5f + Vector3.up * 1.5f, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (var collider in colliders)
        {
            var body = collider.attachedRigidbody;
            if (body == null || body.isKinematic || collider.GetComponentInParent<PrototypeEnemy>() != null || collider.GetComponentInParent<PrototypeHealth>() != null)
            {
                continue;
            }

            body.AddForce((transform.forward + Vector3.up * 0.35f).normalized * 12f, ForceMode.Impulse);
            GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.Attack);
            return true;
        }

        return false;
    }

    private static void CreateImpactEffect(Vector3 position, float radius, Color color)
    {
        var effect = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        effect.name = "GiantImpactEffect";
        effect.layer = 2;
        effect.transform.position = position;
        effect.transform.localScale = new Vector3(radius, 0.04f, radius);
        if (Application.isPlaying)
        {
            Object.Destroy(effect.GetComponent<Collider>());
        }
        else
        {
            Object.DestroyImmediate(effect.GetComponent<Collider>());
        }
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = color };
        effect.GetComponent<Renderer>().sharedMaterial = material;
        if (Application.isPlaying)
        {
            Object.Destroy(effect, 0.3f);
        }
        else
        {
            Object.DestroyImmediate(effect);
        }
    }
}
