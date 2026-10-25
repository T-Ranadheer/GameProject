using System;
using UnityEngine;

public sealed class PrototypeHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    public event Action<PrototypeHealth> Damaged;
    public event Action<PrototypeHealth> Died;
    public event Action<PrototypeHealth, PrototypeDamageInfo> DamagedWithInfo;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsAlive { get; private set; } = true;
    public PrototypeDamageType LastDamageType { get; private set; } = PrototypeDamageType.Physical;

    private readonly float[] damageMultipliers = { 1f, 1f, 1f, 1f };

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void Initialize(float health)
    {
        maxHealth = Mathf.Max(1f, health);
        CurrentHealth = maxHealth;
        IsAlive = true;
        LastDamageType = PrototypeDamageType.Physical;
    }

    public void ConfigureResistances(float physical, float energy, float electric, float impact)
    {
        damageMultipliers[(int)PrototypeDamageType.Physical] = Mathf.Clamp(physical, 0f, 5f);
        damageMultipliers[(int)PrototypeDamageType.Energy] = Mathf.Clamp(energy, 0f, 5f);
        damageMultipliers[(int)PrototypeDamageType.Electric] = Mathf.Clamp(electric, 0f, 5f);
        damageMultipliers[(int)PrototypeDamageType.Impact] = Mathf.Clamp(impact, 0f, 5f);
    }

    public void ApplyDamage(float amount, GameObject source = null)
    {
        ApplyDamage(new PrototypeDamageInfo(amount, PrototypeDamageType.Physical, transform.position, Vector3.zero), source);
    }

    public void ApplyDamage(PrototypeDamageInfo damageInfo, GameObject source = null)
    {
        if (!IsAlive || damageInfo.Amount <= 0f || float.IsNaN(damageInfo.Amount) || float.IsInfinity(damageInfo.Amount))
        {
            return;
        }

        var typeIndex = Mathf.Clamp((int)damageInfo.Type, 0, damageMultipliers.Length - 1);
        var appliedAmount = damageInfo.Amount * damageMultipliers[typeIndex];
        if (appliedAmount <= 0f)
        {
            return;
        }

        LastDamageType = damageInfo.Type;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - appliedAmount);
        PrototypePoliceCoordinator.Instance?.ReportDamageIncident(this, source, damageInfo);
        Damaged?.Invoke(this);
        DamagedWithInfo?.Invoke(this, damageInfo);

        if (CurrentHealth <= 0f)
        {
            IsAlive = false;
            Died?.Invoke(this);
        }
    }

    public void RestoreFull()
    {
        CurrentHealth = maxHealth;
        IsAlive = true;
        LastDamageType = PrototypeDamageType.Physical;
    }
}
