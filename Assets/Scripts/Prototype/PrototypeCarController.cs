using UnityEngine;

public sealed class PrototypeCarController : MonoBehaviour
{
    private PrototypeGameConfig config;
    private Rigidbody body;
    private BoxCollider bodyCollider;
    private WheelCollider[] wheels;
    private Transform[] wheelVisuals;
    private bool physicsEnabled;
    private float throttleInput;
    private float steeringInput;
    private bool brakeInput;

    public bool IsGrounded
    {
        get
        {
            if (wheels == null)
            {
                return false;
            }

            foreach (var wheel in wheels)
            {
                if (wheel != null && wheel.isGrounded)
                {
                    return true;
                }
            }

            return Physics.Raycast(transform.position + Vector3.up * 0.25f, Vector3.down, 1.25f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }
    }

    public static float CalculateTargetForwardSpeed(float throttle, float maxSpeed, float reverseSpeed)
    {
        return throttle >= 0f ? maxSpeed * Mathf.Clamp01(throttle) : -reverseSpeed * Mathf.Clamp01(-throttle);
    }

    public static float CalculateSteerAngle(float steering, float speed, float maxSpeed, float steeringRange)
    {
        var speedFactor = Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(1f, maxSpeed));
        return steering * steeringRange * Mathf.Lerp(1f, 0.35f, speedFactor);
    }

    public void Configure(PrototypeGameConfig gameConfig, WheelCollider[] wheelColliders, Transform[] wheelModels)
    {
        config = gameConfig;
        body = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<BoxCollider>();
        wheels = wheelColliders;
        wheelVisuals = wheelModels;

        foreach (var wheel in wheels)
        {
            if (wheel == null)
            {
                continue;
            }

            wheel.mass = 28f;
            wheel.radius = 0.38f;
            wheel.suspensionDistance = 0.24f;
            wheel.wheelDampingRate = 1.5f;
            wheel.forwardFriction = CreateFriction(1.4f, 0.65f);
            wheel.sidewaysFriction = CreateFriction(1.5f, 0.8f);
            wheel.ConfigureVehicleSubsteps(5f, 12, 15);
            wheel.enabled = false;
        }
    }

    public void SetPhysicsEnabled(bool enabled)
    {
        physicsEnabled = enabled;
        if (wheels != null)
        {
            foreach (var wheel in wheels)
            {
                if (wheel != null)
                {
                    wheel.enabled = enabled;
                }
            }
        }

        if (!enabled)
        {
            throttleInput = 0f;
            steeringInput = 0f;
            brakeInput = true;
        }
    }

    public void TickInput()
    {
        if (!physicsEnabled)
        {
            return;
        }

        var move = PrototypeInput.Move;
        throttleInput = move.y;
        steeringInput = move.x;
        brakeInput = PrototypeInput.BrakeHeld;
    }

    private void FixedUpdate()
    {
        if (!physicsEnabled || config == null || body == null || wheels == null || wheels.Length < 4)
        {
            return;
        }

        var forwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
        var targetForwardSpeed = CalculateTargetForwardSpeed(throttleInput, config.robotCarMaxSpeed, config.robotCarReverseSpeed);
        var canAccelerate = Mathf.Abs(forwardSpeed) < Mathf.Abs(targetForwardSpeed) || Mathf.Sign(forwardSpeed) != Mathf.Sign(targetForwardSpeed);
        var motorTorque = canAccelerate ? throttleInput * config.robotCarAcceleration * body.mass : 0f;
        var brakeTorque = brakeInput ? config.robotCarBrake * body.mass : 0f;
        var speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / Mathf.Max(1f, config.robotCarMaxSpeed));
        var steerAngle = CalculateSteerAngle(steeringInput, forwardSpeed, config.robotCarMaxSpeed, config.robotCarSteering);

        for (var i = 0; i < wheels.Length; i++)
        {
            var wheel = wheels[i];
            if (wheel == null)
            {
                continue;
            }

            var frontWheel = i < 2;
            wheel.steerAngle = frontWheel ? steerAngle : 0f;
            wheel.motorTorque = i >= 2 ? motorTorque : motorTorque * 0.35f;
            wheel.brakeTorque = brakeTorque;
            UpdateWheelVisual(i, wheel);
        }

        body.AddForce(-transform.up * body.linearVelocity.magnitude * config.robotCarTurnAssist, ForceMode.Force);
        body.AddForce(Vector3.down * Mathf.Clamp(body.linearVelocity.magnitude * 7f, 0f, 220f), ForceMode.Force);
        var velocity = body.linearVelocity;
        var horizontal = Vector3.ProjectOnPlane(velocity, Vector3.up);
        if (horizontal.magnitude > config.robotCarMaxSpeed * 1.08f)
        {
            horizontal = horizontal.normalized * config.robotCarMaxSpeed * 1.08f;
            body.linearVelocity = horizontal + Vector3.up * velocity.y;
        }

        if (IsGrounded && body.angularVelocity.sqrMagnitude > 20f)
        {
            body.angularVelocity = Vector3.ClampMagnitude(body.angularVelocity, 8f);
        }
    }

    private void UpdateWheelVisual(int index, WheelCollider wheel)
    {
        if (wheelVisuals == null || index >= wheelVisuals.Length || wheelVisuals[index] == null)
        {
            return;
        }

        wheel.GetWorldPose(out var position, out var rotation);
        wheelVisuals[index].SetPositionAndRotation(position, rotation);
    }

    private static WheelFrictionCurve CreateFriction(float stiffness, float extremumSlip)
    {
        return new WheelFrictionCurve
        {
            extremumSlip = extremumSlip,
            extremumValue = 1f,
            asymptoteSlip = extremumSlip * 2f,
            asymptoteValue = 0.75f,
            stiffness = stiffness
        };
    }
}
