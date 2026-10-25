using UnityEngine;
using UnityEngine.AI;

public abstract class PrototypeAiAgent : MonoBehaviour
{
    protected PrototypeGameConfig config;
    protected PrototypeHealth health;
    protected CharacterController controller;
    protected Rigidbody body;
    protected PrototypeFactionMember factionMember;
    protected float nextThinkTime;
    protected float attackTimer;
    protected float stateTimer;
    protected Transform target;
    protected PrototypeHealth targetHealth;
    protected Vector3 lastKnownPosition;
    protected bool hasLastKnownPosition;
    protected bool deathHandled;
    private NavMeshPath navigationPath;
    private int navigationCorner;
    private Vector3 navigationDestination;
    private Vector3 lastGroundPosition;
    private float stuckTimer;

    public PrototypeAiState State { get; protected set; } = PrototypeAiState.Idle;
    public PrototypeFaction Faction => factionMember != null ? factionMember.Faction : PrototypeFaction.Environment;
    public PrototypeHealth Health => health;
    public Transform Target => target;
    public PrototypeHealth TargetHealth => targetHealth;
    public Vector3 LastKnownPosition => lastKnownPosition;
    public bool HasLastKnownPosition => hasLastKnownPosition;
    public bool IsAlive => health != null && health.IsAlive && gameObject.activeInHierarchy;
    public virtual bool IsPursuing => State == PrototypeAiState.Pursue || State == PrototypeAiState.Attack;

    protected void ConfigureBase(PrototypeGameConfig gameConfig, PrototypeFaction faction, float maxHealth)
    {
        config = gameConfig ?? PrototypeGameConfig.CreateDefault();
        health = GetComponent<PrototypeHealth>() ?? gameObject.AddComponent<PrototypeHealth>();
        factionMember = GetComponent<PrototypeFactionMember>() ?? gameObject.AddComponent<PrototypeFactionMember>();
        factionMember.Configure(faction);
        health.Initialize(maxHealth);
        health.Damaged -= OnAgentDamaged;
        health.Died -= OnAgentDied;
        health.Damaged += OnAgentDamaged;
        health.Died += OnAgentDied;
        controller = GetComponent<CharacterController>();
        body = GetComponent<Rigidbody>();
        navigationPath = new NavMeshPath();
        navigationCorner = 0;
        navigationDestination = transform.position;
        lastGroundPosition = transform.position;
        stuckTimer = 0f;
        State = PrototypeAiState.Idle;
        stateTimer = 0f;
        attackTimer = 0f;
        nextThinkTime = 0f;
        enabled = true;
    }

    protected virtual void Update()
    {
        if (!IsAlive || PrototypeGameManager.IsPaused)
        {
            return;
        }

        attackTimer = Mathf.Max(0f, attackTimer - Time.deltaTime);
        stateTimer = Mathf.Max(0f, stateTimer - Time.deltaTime);
        if (Time.time < nextThinkTime)
        {
            return;
        }

        nextThinkTime = Time.time + GetThinkInterval();
        Think();
    }

    protected abstract void Think();

    protected virtual float GetThinkInterval()
    {
        return 0.2f;
    }

    public virtual void SetTarget(Transform newTarget, PrototypeHealth newHealth, bool rememberPosition = true)
    {
        target = newTarget;
        targetHealth = newHealth;
        if (rememberPosition && newTarget != null)
        {
            lastKnownPosition = newTarget.position;
            hasLastKnownPosition = true;
        }
    }

    public virtual void ClearTarget()
    {
        target = null;
        targetHealth = null;
    }

    public void SetState(PrototypeAiState state, float duration = 0f)
    {
        State = state;
        stateTimer = Mathf.Max(0f, duration);
    }

    protected bool TryAcquireActivePlayer(float range, float fieldOfView, bool requireLineOfSight)
    {
        var manager = PrototypeGameManager.Instance;
        if (manager == null || manager.ActiveTransform == null || manager.ActiveHealth == null || !manager.ActiveHealth.IsAlive)
        {
            return false;
        }

        var candidate = manager.ActiveTransform;
        if (!PrototypeAiUtility.IsValidTarget(candidate, manager.ActiveHealth) || !PrototypeFactionRules.IsHostile(Faction, PrototypeFaction.Player))
        {
            return false;
        }

        var offset = candidate.position - transform.position;
        if (offset.sqrMagnitude > range * range)
        {
            return false;
        }

        if (fieldOfView < 359f && offset.sqrMagnitude > 0.01f)
        {
            var angle = Vector3.Angle(transform.forward, offset);
            if (angle > fieldOfView * 0.5f)
            {
                return false;
            }
        }

        if (requireLineOfSight && !PrototypeAiUtility.HasLineOfSight(transform.position + Vector3.up, candidate, range, gameObject))
        {
            return false;
        }

        SetTarget(candidate, manager.ActiveHealth);
        return true;
    }

    protected bool HasVisibleTarget(float range, float fieldOfView)
    {
        if (!PrototypeAiUtility.IsValidTarget(target, targetHealth))
        {
            return false;
        }

        var offset = target.position - transform.position;
        if (offset.sqrMagnitude > range * range)
        {
            return false;
        }

        if (fieldOfView < 359f && offset.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, offset) > fieldOfView * 0.5f)
        {
            return false;
        }

        return PrototypeAiUtility.HasLineOfSight(transform.position + Vector3.up, target, range, gameObject);
    }

    protected void MoveGround(Vector3 destination, float speed)
    {
        var navigationDirection = FindNavigationDirection(destination);
        var direction = navigationDirection.sqrMagnitude > 0.001f
            ? navigationDirection
            : PrototypeAiUtility.FlattenedDirection(transform.position, destination);
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        if (controller != null && controller.enabled)
        {
            controller.Move(direction * speed * Mathf.Max(Time.deltaTime, 0.01f));
        }
        else if (body != null && !body.isKinematic)
        {
            var targetVelocity = direction * speed;
            var planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            body.AddForce((targetVelocity - planarVelocity) * 3.8f, ForceMode.Acceleration);
            body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, speed * 1.15f);
            body.MoveRotation(Quaternion.Slerp(body.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 4.5f));
        }
        else
        {
            transform.position += direction * speed * Mathf.Max(Time.deltaTime, 0.01f);
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 8f);
        if (Vector3.Distance(transform.position, lastGroundPosition) < 0.035f && speed > 0.5f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 1.2f)
            {
                var recovery = Quaternion.Euler(0f, 35f, 0f) * direction;
                if (controller != null && controller.enabled)
                {
                    controller.Move((recovery.normalized * speed + Vector3.up * 0.25f) * Time.deltaTime);
                }
                else if (body != null && !body.isKinematic)
                {
                    body.AddForce(recovery.normalized * speed * 2f + Vector3.up * 0.6f, ForceMode.Impulse);
                }
                else
                {
                    transform.position += recovery.normalized * 0.25f;
                }

                stuckTimer = 0f;
                navigationCorner = 0;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastGroundPosition = transform.position;
    }

    private Vector3 FindNavigationDirection(Vector3 destination)
    {
        if (!NavMesh.SamplePosition(transform.position, out var currentHit, 2.5f, NavMesh.AllAreas)
            || !NavMesh.SamplePosition(destination, out var destinationHit, 3.5f, NavMesh.AllAreas))
        {
            return Vector3.zero;
        }

        if (navigationPath == null || navigationCorner == 0 || Vector3.Distance(navigationDestination, destinationHit.position) > 1.5f
            || navigationPath.status != NavMeshPathStatus.PathComplete)
        {
            navigationPath ??= new NavMeshPath();
            NavMesh.CalculatePath(currentHit.position, destinationHit.position, NavMesh.AllAreas, navigationPath);
            navigationDestination = destinationHit.position;
            navigationCorner = navigationPath.corners != null && navigationPath.corners.Length > 1 ? 1 : 0;
        }

        if (navigationPath.corners == null || navigationCorner >= navigationPath.corners.Length)
        {
            return Vector3.zero;
        }

        var corner = navigationPath.corners[navigationCorner];
        if (Vector3.Distance(transform.position, corner) < 0.8f && navigationCorner + 1 < navigationPath.corners.Length)
        {
            navigationCorner++;
            corner = navigationPath.corners[navigationCorner];
        }

        return PrototypeAiUtility.FlattenedDirection(transform.position, corner);
    }

    protected void MoveDrone(Vector3 destination, float speed)
    {
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Mathf.Max(Time.deltaTime, 0.01f));
        var direction = destination - transform.position;
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }
    }

    protected bool IsTargetWithin(float distance)
    {
        return target != null && (target.position - transform.position).sqrMagnitude <= distance * distance;
    }

    protected void DamageTarget(float amount, PrototypeDamageType type, float knockback = 0f)
    {
        if (!PrototypeAiUtility.IsValidTarget(target, targetHealth))
        {
            return;
        }

        var direction = PrototypeAiUtility.FlattenedDirection(transform.position, target.position);
        targetHealth.ApplyDamage(new PrototypeDamageInfo(amount, type, target.position, direction), gameObject);
        if (knockback > 0f)
        {
            var body = targetHealth.GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic)
            {
                body.AddForce((direction + Vector3.up * 0.2f).normalized * knockback, ForceMode.Impulse);
                body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, 18f);
            }
        }
    }

    protected virtual void OnAgentDamaged(PrototypeHealth damagedHealth)
    {
    }

    protected virtual void OnAgentDied(PrototypeHealth deadHealth)
    {
        if (deathHandled)
        {
            return;
        }

        deathHandled = true;
        SetState(PrototypeAiState.Defeated);
        enabled = false;
    }

    protected virtual void OnDestroy()
    {
        if (health != null)
        {
            health.Damaged -= OnAgentDamaged;
            health.Died -= OnAgentDied;
        }
    }
}
