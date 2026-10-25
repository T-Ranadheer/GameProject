using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PrototypeHealth))]
public sealed class PrototypeStoneGiantController : MonoBehaviour
{
    private CharacterController controller;
    private PrototypeHealth health;
    private PrototypeGameConfig config;
    private float verticalVelocity;

    public bool IsGrounded => controller != null && (controller.isGrounded || Physics.CheckSphere(transform.position + Vector3.down * 1.75f, 0.3f));
    public bool IsAlive => health != null && health.IsAlive;
    public PrototypeHealth Health => health;
    public Vector3 AimPoint => transform.position + Vector3.up * 2.3f;

    public void Configure(PrototypeGameConfig gameConfig)
    {
        config = gameConfig;
        controller = GetComponent<CharacterController>();
        health = GetComponent<PrototypeHealth>();
        controller.height = config.giantControllerHeight;
        controller.radius = config.giantControllerRadius;
        controller.center = Vector3.up * (config.giantControllerHeight * 0.5f);
        controller.stepOffset = 0.55f;
        controller.slopeLimit = 42f;
        health.Initialize(config.giantHealth);
        var profile = GetComponent<PrototypeCharacterProfile>();
        profile?.Configure(PrototypeCharacterId.StoneGiant, "Stone Colossus", config.giantCameraDistance, config.giantCameraHeight, config.giantControllerHeight, config.giantControllerRadius);
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
            verticalVelocity = Mathf.Sqrt(config.giantJumpHeight * -2f * config.giantGravity);
        }

        verticalVelocity += config.giantGravity * Time.deltaTime;
        var speed = PrototypeInput.IsRunning ? config.giantRunSpeed : config.giantWalkSpeed;
        var motion = direction * speed;
        motion.y = verticalVelocity;
        controller.Move(motion * Time.deltaTime);
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 7f);
        }

        RecoverFromInvalidPosition();
    }

    private void RecoverFromInvalidPosition()
    {
        var position = transform.position;
        if (position.y < -8f || float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z))
        {
            controller.enabled = false;
            transform.position = new Vector3(0f, 1.95f, -28f);
            controller.enabled = true;
            verticalVelocity = 0f;
            return;
        }

        position.x = Mathf.Clamp(position.x, -config.arenaHalfExtent, config.arenaHalfExtent);
        position.z = Mathf.Clamp(position.z, -config.arenaHalfExtent, config.arenaHalfExtent);
        transform.position = position;
    }
}
