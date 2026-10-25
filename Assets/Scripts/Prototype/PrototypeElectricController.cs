using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PrototypeHealth))]
[RequireComponent(typeof(PrototypeEnergy))]
public sealed class PrototypeElectricController : MonoBehaviour
{
    private CharacterController controller;
    private PrototypeHealth health;
    private PrototypeEnergy energy;
    private PrototypeGameConfig config;
    private float verticalVelocity;

    public bool IsGrounded => controller != null && (controller.isGrounded || Physics.CheckSphere(transform.position + Vector3.down * 0.82f, 0.2f));
    public bool IsAlive => health != null && health.IsAlive;
    public PrototypeHealth Health => health;
    public PrototypeEnergy Energy => energy;
    public Vector3 AimPoint => transform.position + Vector3.up * 1.35f;

    public void Configure(PrototypeGameConfig gameConfig)
    {
        config = gameConfig;
        controller = GetComponent<CharacterController>();
        health = GetComponent<PrototypeHealth>();
        energy = GetComponent<PrototypeEnergy>();
        controller.height = 1.8f;
        controller.radius = 0.42f;
        controller.center = Vector3.up * 0.9f;
        controller.stepOffset = 0.35f;
        controller.slopeLimit = 48f;
        health.Initialize(config.electricHealth);
        energy.Configure(config.energyMaximum, config.energyRegeneration, config.energyRegenerationDelay);
        var profile = GetComponent<PrototypeCharacterProfile>();
        profile?.Configure(PrototypeCharacterId.ElectricHero, "Volt Warden", config.cameraDistance, config.cameraHeight, 1.8f, 0.42f);
        enabled = false;
    }

    public void SetControlEnabled(bool value)
    {
        enabled = value;
        if (!value)
        {
            verticalVelocity = 0f;
        }
    }

    private void Update()
    {
        if (config == null || controller == null || !IsAlive || PrototypeGameManager.IsPaused)
        {
            return;
        }

        var input = PrototypeInput.Move;
        var camera = Camera.main;
        var forward = camera != null ? camera.transform.forward : Vector3.forward;
        var right = camera != null ? camera.transform.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        var direction = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        if (IsGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (PrototypeInput.JumpPressed && IsGrounded)
        {
            verticalVelocity = Mathf.Sqrt(config.electricJumpHeight * -2f * config.electricGravity);
        }

        verticalVelocity += config.electricGravity * Time.deltaTime;
        var speed = PrototypeInput.IsRunning ? config.electricRunSpeed : config.electricWalkSpeed;
        var motion = direction * speed;
        motion.y = verticalVelocity;
        controller.Move(motion * Time.deltaTime);
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 12f);
        }

        RecoverFromInvalidPosition();
    }

    private void RecoverFromInvalidPosition()
    {
        var position = transform.position;
        if (position.y < -8f || float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z))
        {
            controller.enabled = false;
            transform.position = new Vector3(0f, 1.05f, -28f);
            controller.enabled = true;
            verticalVelocity = 0f;
            return;
        }

        position.x = Mathf.Clamp(position.x, -config.arenaHalfExtent, config.arenaHalfExtent);
        position.z = Mathf.Clamp(position.z, -config.arenaHalfExtent, config.arenaHalfExtent);
        transform.position = position;
    }
}
