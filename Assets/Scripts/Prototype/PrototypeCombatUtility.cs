using System.Collections.Generic;
using UnityEngine;

public static class PrototypeCombatUtility
{
    public static int ApplyAreaDamage(Vector3 center, float radius, float damage, PrototypeDamageType damageType, GameObject source, int maxTargets = 64, float knockback = 0f)
    {
        if (radius <= 0f || damage <= 0f)
        {
            return 0;
        }

        var hitTargets = new HashSet<PrototypeHealth>();
        var colliders = Physics.OverlapSphere(center, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        var hitCount = 0;
        foreach (var collider in colliders)
        {
            if (collider == null || hitCount >= maxTargets)
            {
                break;
            }

            var health = collider.GetComponentInParent<PrototypeHealth>();
            if (health == null || health.gameObject == source || !health.IsAlive || !hitTargets.Add(health))
            {
                continue;
            }

            var direction = (health.transform.position - center).normalized;
            health.ApplyDamage(new PrototypeDamageInfo(damage, damageType, health.transform.position, direction), source);
            hitCount++;

            if (knockback > 0f)
            {
                var body = health.GetComponent<Rigidbody>();
                if (body != null && !body.isKinematic)
                {
                    body.AddForce((direction + Vector3.up * 0.2f).normalized * knockback, ForceMode.Impulse);
                    body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, 18f);
                }
            }
        }

        return hitCount;
    }

    public static bool IsValidEnemy(PrototypeEnemy enemy)
    {
        return enemy != null && enemy.IsAlive && enemy.gameObject.activeInHierarchy;
    }

    public static PrototypeEnemy FindNearestEnemy(Vector3 origin, float radius, PrototypeEnemy excluded = null)
    {
        PrototypeEnemy nearest = null;
        var nearestDistance = radius * radius;
        foreach (var enemy in Object.FindObjectsByType<PrototypeEnemy>(FindObjectsSortMode.None))
        {
            if (!IsValidEnemy(enemy) || enemy == excluded)
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
}
