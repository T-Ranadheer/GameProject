using UnityEngine;

public sealed class PrototypeProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float damage;
    private GameObject owner;
    private float lifetime;
    private PrototypeDamageType damageType = PrototypeDamageType.Energy;

    public void Configure(Vector3 travelDirection, float travelSpeed, float projectileDamage, GameObject projectileOwner)
    {
        Configure(travelDirection, travelSpeed, projectileDamage, projectileOwner, PrototypeDamageType.Energy);
    }

    public void Configure(Vector3 travelDirection, float travelSpeed, float projectileDamage, GameObject projectileOwner, PrototypeDamageType projectileDamageType)
    {
        direction = travelDirection.normalized;
        speed = travelSpeed;
        damage = projectileDamage;
        owner = projectileOwner;
        damageType = projectileDamageType;
        lifetime = 4f;
    }

    private void Update()
    {
        var distance = speed * Time.deltaTime;
        var nextPosition = transform.position + direction * distance;
        foreach (var collider in Physics.OverlapCapsule(transform.position, nextPosition, 0.22f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            var health = collider != null ? collider.GetComponentInParent<PrototypeHealth>() : null;
            if (health != null && health.gameObject != owner && (owner == null || !collider.transform.IsChildOf(owner.transform)))
            {
                Impact(collider, nextPosition);
                return;
            }
        }

        if (Physics.SphereCast(transform.position, 0.18f, direction, out var hit, distance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            if (owner == null || !hit.collider.transform.IsChildOf(owner.transform))
            {
                Impact(hit.collider, hit.point);
                return;
            }
        }

        foreach (var collider in Physics.OverlapSphere(nextPosition, 0.22f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            if (collider != null && (owner == null || !collider.transform.IsChildOf(owner.transform)) && collider.GetComponentInParent<PrototypeHealth>() != null)
            {
                Impact(collider, nextPosition);
                return;
            }
        }

        transform.position = nextPosition;
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void Impact(Collider collider, Vector3 hitPoint)
    {
        var health = collider != null ? collider.GetComponentInParent<PrototypeHealth>() : null;
        if (health != null && health.gameObject != owner)
        {
            health.ApplyDamage(new PrototypeDamageInfo(damage, damageType, hitPoint, direction), owner);
        }

        if (Application.isPlaying)
        {
            Destroy(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }
}
