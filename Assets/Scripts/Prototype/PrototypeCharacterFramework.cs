using System;
using UnityEngine;

public enum PrototypeCharacterId
{
    OriginalHero,
    ElectricHero,
    StoneGiant,
    AtlasRunner,
    HelicopterTransformer,
    TankTransformer,
    MotorcycleTransformer,
    JetTransformer
}

public enum PrototypeDamageType
{
    Physical,
    Energy,
    Electric,
    Impact
}

public readonly struct PrototypeDamageInfo
{
    public readonly float Amount;
    public readonly PrototypeDamageType Type;
    public readonly Vector3 HitPoint;
    public readonly Vector3 Direction;

    public PrototypeDamageInfo(float amount, PrototypeDamageType type, Vector3 hitPoint, Vector3 direction)
    {
        Amount = amount;
        Type = type;
        HitPoint = hitPoint;
        Direction = direction;
    }
}

public enum PrototypeAudioCue
{
    Attack,
    Impact,
    Lightning,
    HeavyImpact,
    Switch,
    Transform,
    Mission
}

public sealed class PrototypeCharacterProfile : MonoBehaviour
{
    [SerializeField] private PrototypeCharacterId characterId;
    [SerializeField] private string displayName = "Character";
    [SerializeField] private float cameraDistance = 7.5f;
    [SerializeField] private float cameraHeight = 3.2f;
    [SerializeField] private float collisionHeight = 1.8f;
    [SerializeField] private float collisionRadius = 0.42f;

    public PrototypeCharacterId CharacterId => characterId;
    public string DisplayName => displayName;
    public float CameraDistance => cameraDistance;
    public float CameraHeight => cameraHeight;
    public float CollisionHeight => collisionHeight;
    public float CollisionRadius => collisionRadius;

    public void Configure(PrototypeCharacterId id, string name, float distance, float height, float controllerHeight, float radius)
    {
        characterId = id;
        displayName = string.IsNullOrWhiteSpace(name) ? id.ToString() : name;
        cameraDistance = Mathf.Max(1f, distance);
        cameraHeight = Mathf.Max(0.5f, height);
        collisionHeight = Mathf.Max(0.5f, controllerHeight);
        collisionRadius = Mathf.Max(0.1f, radius);
    }
}

public sealed class PrototypeEnergy : MonoBehaviour
{
    private float regenerationDelayRemaining;

    public event Action<float, float> Changed;

    public float Current { get; private set; }
    public float Maximum { get; private set; }
    public float RegenerationPerSecond { get; private set; }
    public float RegenerationDelay { get; private set; }
    public bool CanSpend(float amount) => amount >= 0f && Current + 0.0001f >= amount;

    public void Configure(float maximum, float regenerationPerSecond, float regenerationDelay)
    {
        Maximum = Mathf.Max(0f, maximum);
        RegenerationPerSecond = Mathf.Max(0f, regenerationPerSecond);
        RegenerationDelay = Mathf.Max(0f, regenerationDelay);
        Current = Maximum;
        regenerationDelayRemaining = 0f;
        Changed?.Invoke(Current, Maximum);
    }

    public bool TrySpend(float amount)
    {
        if (!CanSpend(amount))
        {
            return false;
        }

        Current = Mathf.Clamp(Current - amount, 0f, Maximum);
        regenerationDelayRemaining = RegenerationDelay;
        Changed?.Invoke(Current, Maximum);
        return true;
    }

    public void Restore(float amount)
    {
        Current = Mathf.Clamp(Current + Mathf.Max(0f, amount), 0f, Maximum);
        Changed?.Invoke(Current, Maximum);
    }

    public void RestoreFull()
    {
        Current = Maximum;
        regenerationDelayRemaining = 0f;
        Changed?.Invoke(Current, Maximum);
    }

    private void Update()
    {
        if (PrototypeGameManager.IsPaused || Maximum <= 0f)
        {
            return;
        }

        if (regenerationDelayRemaining > 0f)
        {
            regenerationDelayRemaining = Mathf.Max(0f, regenerationDelayRemaining - Time.deltaTime);
            return;
        }

        var previous = Current;
        Current = Mathf.Min(Maximum, Current + RegenerationPerSecond * Time.deltaTime);
        if (!Mathf.Approximately(previous, Current))
        {
            Changed?.Invoke(Current, Maximum);
        }
    }
}

public sealed class PrototypeProceduralPresentation : MonoBehaviour
{
    private Transform visualRoot;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 baseLocalScale;
    private Vector3 lastWorldPosition;
    private float cycle;
    private float attackTimer;
    private float landingTimer;
    private bool heavy;
    private bool preserveRootTransform;

    public void Configure(Transform root, bool isHeavy, bool preserveFormTransform = false)
    {
        visualRoot = root;
        heavy = isHeavy;
        preserveRootTransform = preserveFormTransform;
        baseLocalPosition = root != null ? root.localPosition : Vector3.zero;
        baseLocalRotation = root != null ? root.localRotation : Quaternion.identity;
        baseLocalScale = root != null ? root.localScale : Vector3.one;
        lastWorldPosition = transform.position;
    }

    public void PulseAttack(float intensity = 1f)
    {
        attackTimer = Mathf.Max(attackTimer, Mathf.Clamp(intensity, 0.25f, 2f) * 0.22f);
    }

    public void PulseLanding(float intensity = 1f)
    {
        landingTimer = Mathf.Max(landingTimer, Mathf.Clamp(intensity, 0.25f, 2f) * 0.18f);
    }

    public void PulseTransform()
    {
        attackTimer = Mathf.Max(attackTimer, 0.35f);
    }

    private void LateUpdate()
    {
        if (visualRoot == null || !gameObject.activeInHierarchy || PrototypeGameManager.IsPaused)
        {
            return;
        }

        var delta = transform.position - lastWorldPosition;
        lastWorldPosition = transform.position;
        var speed = Vector3.ProjectOnPlane(delta, Vector3.up).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        var movementAmount = Mathf.Clamp01(speed / (heavy ? 6f : 9f));
        cycle += speed * (heavy ? 3.5f : 7.5f) * Time.deltaTime;
        attackTimer = Mathf.Max(0f, attackTimer - Time.deltaTime);
        landingTimer = Mathf.Max(0f, landingTimer - Time.deltaTime);

        var bob = Mathf.Sin(cycle) * (heavy ? 0.035f : 0.06f) * movementAmount;
        bob += landingTimer > 0f ? Mathf.Sin(landingTimer * 38f) * landingTimer * 0.18f : 0f;
        var attack = attackTimer > 0f ? attackTimer / 0.35f : 0f;
        var localPosition = preserveRootTransform ? visualRoot.localPosition : baseLocalPosition + Vector3.up * bob;
        var localRotation = baseLocalRotation * Quaternion.Euler(-attack * (heavy ? 10f : 6f), Mathf.Sin(cycle * 0.5f) * movementAmount * (heavy ? 2.5f : 4f), attack * (heavy ? 4f : 2f));
        visualRoot.localPosition = localPosition;
        visualRoot.localRotation = localRotation;
        var scale = 1f + attack * (heavy ? 0.035f : 0.02f);
        visualRoot.localScale = preserveRootTransform ? visualRoot.localScale : baseLocalScale * scale;
    }
}

public sealed class PrototypeAudioFeedback : MonoBehaviour
{
    private AudioSource source;
    private AudioClip[] clips;
    private float basePitch = 1f;

    public void Configure(float pitch = 1f)
    {
        basePitch = Mathf.Clamp(pitch, 0.5f, 2f);
        source = GetComponent<AudioSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.spatialBlend = 0.65f;
        source.volume = 0.22f;
        clips = new AudioClip[System.Enum.GetValues(typeof(PrototypeAudioCue)).Length];
    }

    public void Play(PrototypeAudioCue cue)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (source == null)
        {
            Configure(basePitch);
        }

        var index = (int)cue;
        if (clips[index] == null)
        {
            clips[index] = CreateTone(cue);
        }

        source.pitch = basePitch * (1f + index * 0.035f);
        source.PlayOneShot(clips[index]);
    }

    private AudioClip CreateTone(PrototypeAudioCue cue)
    {
        var sampleRate = 22050;
        var duration = cue == PrototypeAudioCue.Mission ? 0.18f : 0.075f;
        var samples = Mathf.CeilToInt(sampleRate * duration);
        var clip = AudioClip.Create("PrototypeCue_" + cue, samples, 1, sampleRate, false);
        var data = new float[samples];
        var frequency = 260f + (int)cue * 82f;
        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)sampleRate;
            var envelope = 1f - i / (float)samples;
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.22f;
        }

        clip.SetData(data, 0);
        return clip;
    }
}
