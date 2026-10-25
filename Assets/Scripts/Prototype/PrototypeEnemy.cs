using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PrototypeHealth))]
public sealed class PrototypeEnemy : MonoBehaviour
{
    private CharacterController controller;
    private PrototypeHealth health;
    private PrototypePlayerController player;
    private Transform targetTransform;
    private PrototypeHealth targetHealth;
    private PrototypeMissionController mission;
    private PrototypeGameConfig config;
    private float attackTimer;
    private Renderer[] renderers;
    private bool deathHandled;

    public bool IsAlive => health != null && health.IsAlive;
    public Vector3 AimPoint => transform.position + Vector3.up * 1.1f;
    public bool HasValidTarget => targetTransform != null && targetHealth != null && targetHealth.IsAlive;
    public PrototypeHealth TargetHealth => targetHealth;

    public void Configure(PrototypePlayerController playerController, PrototypeMissionController missionController, PrototypeGameConfig gameConfig)
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        controller = GetComponent<CharacterController>();
        health = GetComponent<PrototypeHealth>();
        player = playerController;
        targetTransform = playerController != null ? playerController.transform : null;
        targetHealth = playerController != null ? playerController.GetComponent<PrototypeHealth>() : null;
        mission = missionController;
        config = gameConfig;
        controller.height = 1.8f;
        controller.radius = 0.42f;
        controller.center = Vector3.up * 0.9f;
        controller.stepOffset = 0.3f;
        health.Initialize(config.enemyHealth);
        health.Damaged += OnDamaged;
        health.Died += OnDied;
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void SetTarget(Transform newTarget, PrototypeHealth newHealth)
    {
        targetTransform = newTarget;
        targetHealth = newHealth;
    }

    public void ClearTarget()
    {
        targetTransform = null;
        targetHealth = null;
    }

    private void Update()
    {
        if (!IsAlive || !HasValidTarget || PrototypeGameManager.IsPaused)
        {
            return;
        }

        var offset = targetTransform.position - transform.position;
        var flatOffset = Vector3.ProjectOnPlane(offset, Vector3.up);
        var distance = flatOffset.magnitude;
        if (distance > config.enemyDetectRange)
        {
            return;
        }

        if (distance > config.enemyAttackRange)
        {
            var direction = flatOffset.normalized;
            controller.Move(direction * config.enemySpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 8f);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flatOffset.normalized), Time.deltaTime * 10f);
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                targetHealth.ApplyDamage(config.enemyAttackDamage, gameObject);
                attackTimer = config.enemyAttackCooldown;
            }
        }
    }

    private void OnDamaged(PrototypeHealth damagedHealth)
    {
        if (renderers == null)
        {
            return;
        }

        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 0.45f, 0.1f);
            }
        }
    }

    private void OnDied(PrototypeHealth deadHealth)
    {
        if (deathHandled)
        {
            return;
        }

        deathHandled = true;
        controller.enabled = false;
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.material.color = new Color(0.12f, 0.12f, 0.14f);
            }
        }

        mission?.NotifyEnemyDefeated(this);
        Destroy(gameObject, 0.65f);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }
    }
}
