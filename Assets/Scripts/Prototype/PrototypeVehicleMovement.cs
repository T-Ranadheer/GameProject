using UnityEngine;

public abstract class PrototypeVehicleMovement : MonoBehaviour
{
    protected PrototypeGameConfig config;
    protected Rigidbody body;
    protected BoxCollider vehicleCollider;
    protected bool physicsEnabled;

    public abstract PrototypeRobotForm VehicleForm { get; }
    public bool PhysicsEnabled => physicsEnabled;
    public float Speed => body != null ? body.linearVelocity.magnitude : 0f;
    public virtual bool IsGrounded => Physics.Raycast(transform.position + Vector3.up * 0.4f, Vector3.down, 1.8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    public virtual bool CanTransformToRobot => IsGrounded && Speed < 2.5f;

    public virtual void Configure(PrototypeGameConfig gameConfig, Rigidbody rigidbody, BoxCollider collider)
    {
        config = gameConfig;
        body = rigidbody;
        vehicleCollider = collider;
    }

    public abstract void TickInput();

    public virtual void SetPhysicsEnabled(bool enabled)
    {
        physicsEnabled = enabled;
    }

    public virtual void Recover(Vector3 safePosition)
    {
        if (body == null)
        {
            transform.position = safePosition;
            return;
        }

        body.position = safePosition;
        body.rotation = Quaternion.Euler(0f, body.rotation.eulerAngles.y, 0f);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    protected static Vector3 ClampHorizontalSpeed(Vector3 velocity, float maximum)
    {
        var horizontal = Vector3.ProjectOnPlane(velocity, Vector3.up);
        if (horizontal.magnitude > maximum)
        {
            horizontal = horizontal.normalized * maximum;
        }

        return horizontal + Vector3.up * velocity.y;
    }
}

public sealed class PrototypeHelicopterController : PrototypeVehicleMovement
{
    private float forwardInput;
    private float yawInput;
    private float verticalInput;

    public override PrototypeRobotForm VehicleForm => PrototypeRobotForm.Helicopter;
    public override bool IsGrounded => Physics.Raycast(transform.position + Vector3.up * 0.35f, Vector3.down, 1.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    public override bool CanTransformToRobot => IsGrounded || (Speed < 2.5f && transform.position.y < 4.5f);

    public override void Configure(PrototypeGameConfig gameConfig, Rigidbody rigidbody, BoxCollider collider)
    {
        base.Configure(gameConfig, rigidbody, collider);
        body.useGravity = false;
        body.mass = 950f;
        body.linearDamping = 1.1f;
        body.angularDamping = 5f;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public override void TickInput()
    {
        if (!physicsEnabled)
        {
            return;
        }

        var move = PrototypeInput.Move;
        forwardInput = move.y;
        yawInput = move.x;
        verticalInput = PrototypeInput.FlightVertical;
    }

    public override void SetPhysicsEnabled(bool enabled)
    {
        base.SetPhysicsEnabled(enabled);
        if (body != null)
        {
            body.useGravity = false;
        }

        if (!enabled)
        {
            forwardInput = 0f;
            yawInput = 0f;
            verticalInput = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (!physicsEnabled || body == null || config == null)
        {
            return;
        }

        var yaw = yawInput * config.helicopterYawSpeed * Time.fixedDeltaTime;
        body.MoveRotation(body.rotation * Quaternion.Euler(0f, yaw, 0f));
        var desiredVelocity = transform.forward * (forwardInput * config.helicopterMaxSpeed);
        desiredVelocity += Vector3.up * (verticalInput * config.helicopterVerticalSpeed);
        var acceleration = (desiredVelocity - body.linearVelocity) * config.helicopterAcceleration;
        body.AddForce(acceleration, ForceMode.Acceleration);
        body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, config.helicopterMaxSpeed + config.helicopterVerticalSpeed * 0.55f);

        if (body.position.y > config.helicopterMaxAltitude)
        {
            body.position = new Vector3(body.position.x, config.helicopterMaxAltitude, body.position.z);
            if (body.linearVelocity.y > 0f)
            {
                body.linearVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
            }
        }

        if (body.position.y < 1.2f && body.linearVelocity.y < 0f)
        {
            body.linearVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
        }
    }
}

public sealed class PrototypeTankController : PrototypeVehicleMovement
{
    private float throttleInput;
    private float steeringInput;
    private bool brakeInput;
    private Transform turret;
    private float turretYaw;

    public override PrototypeRobotForm VehicleForm => PrototypeRobotForm.Tank;
    public override bool IsGrounded => Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, 1.6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

    public void ConfigureTurret(Transform turretTransform)
    {
        turret = turretTransform;
    }

    public override void Configure(PrototypeGameConfig gameConfig, Rigidbody rigidbody, BoxCollider collider)
    {
        base.Configure(gameConfig, rigidbody, collider);
        body.useGravity = true;
        body.mass = 2400f;
        body.linearDamping = 0.45f;
        body.angularDamping = 3.5f;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public override void TickInput()
    {
        if (!physicsEnabled)
        {
            return;
        }

        var move = PrototypeInput.Move;
        throttleInput = move.y;
        steeringInput = move.x;
        brakeInput = PrototypeInput.BrakeHeld;
        var look = PrototypeInput.Look;
        turretYaw += look.x * 0.16f;
        if (turret != null)
        {
            turret.localRotation = Quaternion.Euler(0f, turretYaw, 0f);
        }
    }

    public override void SetPhysicsEnabled(bool enabled)
    {
        base.SetPhysicsEnabled(enabled);
        if (body != null)
        {
            body.useGravity = enabled;
        }

        if (!enabled)
        {
            throttleInput = 0f;
            steeringInput = 0f;
            brakeInput = true;
        }
    }

    private void FixedUpdate()
    {
        if (!physicsEnabled || body == null || config == null)
        {
            return;
        }

        var speed = Vector3.Dot(body.linearVelocity, transform.forward);
        var target = throttleInput >= 0f
            ? config.tankMaxSpeed * throttleInput
            : -config.tankReverseSpeed * Mathf.Abs(throttleInput);
        if (Mathf.Abs(speed) < Mathf.Abs(target) || Mathf.Sign(speed) != Mathf.Sign(target))
        {
            body.AddForce(transform.forward * throttleInput * config.tankAcceleration, ForceMode.Acceleration);
        }

        var steerScale = Mathf.Lerp(1f, 0.42f, Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(1f, config.tankMaxSpeed)));
        body.MoveRotation(body.rotation * Quaternion.Euler(0f, steeringInput * config.tankSteering * steerScale * Time.fixedDeltaTime, 0f));
        if (brakeInput)
        {
            body.linearVelocity = Vector3.Lerp(body.linearVelocity, Vector3.zero, Mathf.Clamp01(config.tankBrake * Time.fixedDeltaTime));
        }

        body.linearVelocity = ClampHorizontalSpeed(body.linearVelocity, config.tankMaxSpeed * 1.08f);
        if (IsGrounded && body.angularVelocity.sqrMagnitude > 16f)
        {
            body.angularVelocity = Vector3.ClampMagnitude(body.angularVelocity, 4f);
        }
    }
}

public sealed class PrototypeMotorcycleController : PrototypeVehicleMovement
{
    private WheelCollider frontWheel;
    private WheelCollider rearWheel;
    private Transform frontVisual;
    private Transform rearVisual;
    private float throttleInput;
    private float steeringInput;
    private bool brakeInput;
    private float boostRemaining;
    private float boostCooldown;
    private Transform leanVisual;

    public override PrototypeRobotForm VehicleForm => PrototypeRobotForm.Motorcycle;
    public override bool IsGrounded => (frontWheel != null && frontWheel.isGrounded) || (rearWheel != null && rearWheel.isGrounded)
        || Physics.Raycast(transform.position + Vector3.up * 0.25f, Vector3.down, 1.45f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

    public void ConfigureWheels(WheelCollider front, WheelCollider rear, Transform frontModel, Transform rearModel, Transform leanRoot)
    {
        frontWheel = front;
        rearWheel = rear;
        frontVisual = frontModel;
        rearVisual = rearModel;
        leanVisual = leanRoot;
        ConfigureWheel(frontWheel);
        ConfigureWheel(rearWheel);
    }

    public override void Configure(PrototypeGameConfig gameConfig, Rigidbody rigidbody, BoxCollider collider)
    {
        base.Configure(gameConfig, rigidbody, collider);
        body.useGravity = true;
        body.mass = 380f;
        body.linearDamping = 0.18f;
        body.angularDamping = 5f;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public override void TickInput()
    {
        if (!physicsEnabled)
        {
            return;
        }

        var move = PrototypeInput.Move;
        throttleInput = move.y;
        steeringInput = move.x;
        brakeInput = PrototypeInput.BrakeHeld;
        boostCooldown = Mathf.Max(0f, boostCooldown - Time.deltaTime);
        if (PrototypeInput.Ability2Pressed && boostCooldown <= 0f)
        {
            boostRemaining = config.motorcycleBoostDuration;
            boostCooldown = config.motorcycleBoostCooldown;
        }

        if (leanVisual != null)
        {
            var lean = Mathf.Lerp(0f, -steeringInput * 22f, Mathf.Clamp01(Mathf.Abs(body.linearVelocity.magnitude) / 8f));
            leanVisual.localRotation = Quaternion.Slerp(leanVisual.localRotation, Quaternion.Euler(0f, 0f, lean), Time.deltaTime * 9f);
        }
    }

    public override void SetPhysicsEnabled(bool enabled)
    {
        base.SetPhysicsEnabled(enabled);
        if (frontWheel != null) frontWheel.enabled = enabled;
        if (rearWheel != null) rearWheel.enabled = enabled;
        if (body != null) body.useGravity = enabled;
        if (!enabled)
        {
            throttleInput = 0f;
            steeringInput = 0f;
            brakeInput = true;
            boostRemaining = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (!physicsEnabled || body == null || config == null || frontWheel == null || rearWheel == null)
        {
            return;
        }

        boostRemaining = Mathf.Max(0f, boostRemaining - Time.fixedDeltaTime);
        var maxSpeed = boostRemaining > 0f ? config.motorcycleBoostSpeed : config.motorcycleMaxSpeed;
        var speed = Vector3.Dot(body.linearVelocity, transform.forward);
        var target = throttleInput >= 0f ? maxSpeed * throttleInput : -config.motorcycleReverseSpeed * Mathf.Abs(throttleInput);
        var torque = throttleInput * config.motorcycleAcceleration * body.mass;
        if (Mathf.Abs(speed) >= Mathf.Abs(target) && Mathf.Sign(speed) == Mathf.Sign(target))
        {
            torque = 0f;
        }

        frontWheel.steerAngle = steeringInput * config.motorcycleSteering;
        rearWheel.motorTorque = torque;
        frontWheel.brakeTorque = brakeInput ? config.motorcycleBrake * body.mass : 0f;
        rearWheel.brakeTorque = brakeInput ? config.motorcycleBrake * body.mass : 0f;
        body.AddForce(-transform.up * body.linearVelocity.magnitude * 2.5f, ForceMode.Force);
        body.linearVelocity = ClampHorizontalSpeed(body.linearVelocity, maxSpeed * 1.08f);
        UpdateWheelVisual(frontWheel, frontVisual);
        UpdateWheelVisual(rearWheel, rearVisual);
    }

    private static void ConfigureWheel(WheelCollider wheel)
    {
        if (wheel == null)
        {
            return;
        }

        wheel.mass = 18f;
        wheel.radius = 0.34f;
        wheel.suspensionDistance = 0.2f;
        wheel.wheelDampingRate = 1.8f;
        wheel.forwardFriction = CreateFriction(1.35f, 0.55f);
        wheel.sidewaysFriction = CreateFriction(1.6f, 0.72f);
        wheel.ConfigureVehicleSubsteps(5f, 10, 12);
        wheel.enabled = false;
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

    private static void UpdateWheelVisual(WheelCollider wheel, Transform visual)
    {
        if (wheel == null || visual == null)
        {
            return;
        }

        wheel.GetWorldPose(out var position, out var rotation);
        visual.SetPositionAndRotation(position, rotation);
    }
}

public sealed class PrototypeJetController : PrototypeVehicleMovement
{
    private float throttleInput;
    private float yawInput;
    private float pitchInput;

    public override PrototypeRobotForm VehicleForm => PrototypeRobotForm.Jet;
    public override bool IsGrounded => Physics.Raycast(transform.position + Vector3.up * 0.35f, Vector3.down, 1.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    public override bool CanTransformToRobot => IsGrounded || (Speed < 4f && transform.position.y < 5f);

    public override void Configure(PrototypeGameConfig gameConfig, Rigidbody rigidbody, BoxCollider collider)
    {
        base.Configure(gameConfig, rigidbody, collider);
        body.useGravity = false;
        body.mass = 780f;
        body.linearDamping = 0.35f;
        body.angularDamping = 4f;
        body.constraints = RigidbodyConstraints.None;
    }

    public override void TickInput()
    {
        if (!physicsEnabled)
        {
            return;
        }

        var move = PrototypeInput.Move;
        throttleInput = move.y;
        yawInput = move.x;
        pitchInput = PrototypeInput.FlightVertical;
    }

    public override void SetPhysicsEnabled(bool enabled)
    {
        base.SetPhysicsEnabled(enabled);
        if (body != null) body.useGravity = false;
        if (!enabled)
        {
            throttleInput = 0f;
            yawInput = 0f;
            pitchInput = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (!physicsEnabled || body == null || config == null)
        {
            return;
        }

        var forwardSpeed = throttleInput >= 0f
            ? config.jetCruiseSpeed + throttleInput * (config.jetMaxSpeed - config.jetCruiseSpeed)
            : config.jetCruiseSpeed * 0.45f;
        var pitch = -pitchInput * config.jetTurnSpeed * Time.fixedDeltaTime;
        var yaw = yawInput * config.jetTurnSpeed * Time.fixedDeltaTime;
        var roll = -yawInput * 42f * Time.fixedDeltaTime;
        body.MoveRotation(body.rotation * Quaternion.Euler(pitch, yaw, roll));
        var desiredVelocity = transform.forward * forwardSpeed + Vector3.up * (pitchInput * config.jetVerticalControl);
        body.linearVelocity = Vector3.Lerp(body.linearVelocity, desiredVelocity, Mathf.Clamp01(config.jetAcceleration * Time.fixedDeltaTime));
        body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, config.jetMaxSpeed);

        if (body.position.y > config.jetMaxAltitude)
        {
            body.position = new Vector3(body.position.x, config.jetMaxAltitude, body.position.z);
            if (body.linearVelocity.y > 0f) body.linearVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
        }

        if (body.position.y < 1.25f && body.linearVelocity.y < 0f)
        {
            body.linearVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
        }
    }
}
