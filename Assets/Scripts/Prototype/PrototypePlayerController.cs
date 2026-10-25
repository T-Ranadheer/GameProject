using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PrototypeHealth))]
public sealed class PrototypePlayerController : MonoBehaviour
{
    public enum MovementMode
    {
        Grounded,
        Flying
    }

    public event Action<bool> FlightStateChanged;

    private CharacterController controller;
    private PrototypeHealth health;
    private PrototypeGameConfig config;
    private float verticalVelocity;
    private MovementMode movementMode = MovementMode.Grounded;

    public bool IsFlying => movementMode == MovementMode.Flying;
    public bool IsGrounded => controller != null && (controller.isGrounded || Physics.CheckSphere(transform.position + Vector3.down * 0.82f, 0.18f));
    public MovementMode CurrentMovementMode => movementMode;

    public void Configure(PrototypeGameConfig gameConfig)
    {
        config = gameConfig;
        controller = GetComponent<CharacterController>();
        health = GetComponent<PrototypeHealth>();
        controller.height = 1.8f;
        controller.radius = 0.42f;
        controller.center = Vector3.up * 0.9f;
        controller.stepOffset = 0.35f;
        controller.slopeLimit = 48f;
    }

    private void Update()
    {
        if (config == null || controller == null || health == null || !health.IsAlive || PrototypeGameManager.IsPaused)
        {
            return;
        }

        if (PrototypeInput.ToggleFlightPressed)
        {
            SetFlying(!IsFlying);
        }

        var input = PrototypeInput.Move;
        var camera = Camera.main;
        var forward = camera != null ? camera.transform.forward : Vector3.forward;
        var right = camera != null ? camera.transform.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        var desiredDirection = (forward * input.y + right * input.x);
        if (desiredDirection.sqrMagnitude > 1f)
        {
            desiredDirection.Normalize();
        }

        if (IsFlying)
        {
            UpdateFlight(desiredDirection);
        }
        else
        {
            UpdateGroundMovement(desiredDirection);
        }

        RecoverFromInvalidPosition();
    }

    public void SetFlying(bool flying)
    {
        if (flying == IsFlying || health == null || !health.IsAlive)
        {
            return;
        }

        movementMode = flying ? MovementMode.Flying : MovementMode.Grounded;
        verticalVelocity = flying ? 0f : Mathf.Min(verticalVelocity, 0f);
        FlightStateChanged?.Invoke(flying);
    }

    private void UpdateGroundMovement(Vector3 desiredDirection)
    {
        if (IsGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (PrototypeInput.JumpPressed && IsGrounded)
        {
            verticalVelocity = Mathf.Sqrt(config.jumpHeight * -2f * config.gravity);
        }

        verticalVelocity += config.gravity * Time.deltaTime;
        var speed = IsRunning() ? config.runSpeed : config.walkSpeed;
        var movement = desiredDirection * speed;
        movement.y = verticalVelocity;
        controller.Move(movement * Time.deltaTime);
        FaceMovement(desiredDirection);
    }

    private void UpdateFlight(Vector3 desiredDirection)
    {
        var vertical = PrototypeInput.FlightVertical;
        var movement = desiredDirection * config.flightSpeed;
        movement.y = vertical * config.flightVerticalSpeed;
        controller.Move(movement * Time.deltaTime);
        FaceMovement(desiredDirection);

        if (transform.position.y <= 1.05f && vertical < 0f)
        {
            transform.position = new Vector3(transform.position.x, 1.01f, transform.position.z);
            SetFlying(false);
        }
    }

    private bool IsRunning()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        return keyboard != null && keyboard.leftShiftKey.isPressed;
    }

    private void FaceMovement(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 12f);
    }

    private void RecoverFromInvalidPosition()
    {
        if (config == null)
        {
            return;
        }

        var position = transform.position;
        if (position.y < -8f || float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z))
        {
            controller.enabled = false;
            transform.position = new Vector3(0f, 1.05f, -28f);
            controller.enabled = true;
            SetFlying(false);
            verticalVelocity = 0f;
            return;
        }

        position.x = Mathf.Clamp(position.x, -config.arenaHalfExtent, config.arenaHalfExtent);
        position.z = Mathf.Clamp(position.z, -config.arenaHalfExtent, config.arenaHalfExtent);
        transform.position = position;
    }
}
