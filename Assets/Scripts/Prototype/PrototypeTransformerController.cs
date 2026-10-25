using UnityEngine;

[RequireComponent(typeof(PrototypeHealth))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public sealed class PrototypeTransformerController : MonoBehaviour
{
    private PrototypeGameConfig config;
    private PrototypeHealth health;
    private CharacterController robotController;
    private Rigidbody vehicleBody;
    private BoxCollider vehicleCollider;
    private PrototypeTransformStateMachine stateMachine;
    private PrototypeVehicleMovement vehicleMovement;
    private PrototypeTransformerCombat combat;
    private Transform robotVisual;
    private Transform vehicleVisual;
    private PrototypeCharacterProfile profile;
    private bool deployed;
    private bool switchedPhysicsForTransform;
    private float robotVerticalVelocity;

    public PrototypeCharacterId CharacterId { get; private set; }
    public PrototypeRobotForm VehicleForm { get; private set; }
    public PrototypeRobotTransformState State => stateMachine != null ? stateMachine.State : PrototypeRobotTransformState.Robot;
    public PrototypeRobotForm Form => stateMachine != null ? stateMachine.Form : PrototypeRobotForm.Robot;
    public bool IsDeployed => deployed;
    public bool IsTransforming => stateMachine != null && stateMachine.IsTransforming;
    public bool IsRobotForm => Form == PrototypeRobotForm.Robot && !IsTransforming;
    public bool IsVehicleForm => Form == VehicleForm && !IsTransforming;
    public float TransformProgress => stateMachine != null ? stateMachine.Progress : 1f;
    public bool IsGrounded => IsRobotForm
        ? robotController != null && (robotController.isGrounded || Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        : vehicleMovement != null && vehicleMovement.IsGrounded;
    public float Speed => IsVehicleForm && vehicleMovement != null ? vehicleMovement.Speed : 0f;
    public Vector3 AimPoint => transform.position + Vector3.up * (IsVehicleForm ? 0.9f : 1.35f);
    public PrototypeHealth Health => health;
    public PrototypeVehicleMovement VehicleMovement => vehicleMovement;
    public PrototypeTransformerCombat Combat => combat;
    public float CurrentCameraDistance => IsVehicleForm ? GetCameraDistance(VehicleForm) : config.robotCameraDistance;
    public float CurrentCameraHeight => IsVehicleForm ? GetCameraHeight(VehicleForm) : config.robotCameraHeight;

    public void Configure(PrototypeGameConfig gameConfig, PrototypeCharacterId characterId, string displayName, PrototypeRobotForm vehicleForm,
        Transform humanoidModel, Transform vehicleModel, float controllerHeight, float controllerRadius)
    {
        config = gameConfig;
        CharacterId = characterId;
        VehicleForm = vehicleForm;
        health = GetComponent<PrototypeHealth>();
        robotController = GetComponent<CharacterController>();
        vehicleBody = GetComponent<Rigidbody>();
        vehicleCollider = GetComponent<BoxCollider>();
        stateMachine = new PrototypeTransformStateMachine(config.robotTransformDuration, vehicleForm);
        robotVisual = humanoidModel;
        vehicleVisual = vehicleModel;
        profile = GetComponent<PrototypeCharacterProfile>();
        profile?.Configure(characterId, displayName, GetCameraDistance(vehicleForm), GetCameraHeight(vehicleForm), controllerHeight, controllerRadius);
        combat = GetComponent<PrototypeTransformerCombat>();
        vehicleMovement = GetComponent<PrototypeVehicleMovement>();

        robotController.height = controllerHeight;
        robotController.radius = controllerRadius;
        robotController.center = Vector3.up * (controllerHeight * 0.5f);
        robotController.stepOffset = 0.3f;
        robotController.slopeLimit = 48f;
        vehicleBody.isKinematic = true;
        vehicleBody.linearDamping = 0.25f;
        vehicleBody.angularDamping = 4f;
        vehicleBody.interpolation = RigidbodyInterpolation.Interpolate;
        vehicleBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        vehicleCollider.size = GetVehicleColliderSize(vehicleForm);
        vehicleCollider.center = new Vector3(0f, vehicleCollider.size.y * 0.5f, 0f);
        vehicleCollider.enabled = false;
        vehicleMovement?.SetPhysicsEnabled(false);
        combat?.Configure(config, this);
        health.Died += OnDied;
        SetVisualForm(PrototypeRobotForm.Robot, 1f);
        SetRobotPhysics(true);
        enabled = false;
    }

    public void Deploy()
    {
        deployed = true;
        enabled = true;
        if (stateMachine.State != PrototypeRobotTransformState.Robot)
        {
            stateMachine.InterruptAndRecover();
        }

        SetVisualForm(PrototypeRobotForm.Robot, 1f);
        SetRobotPhysics(true);
    }

    public bool TryReturnToHeroSafe()
    {
        return deployed && IsRobotForm && IsGrounded && Speed < 1.5f;
    }

    public void Undeploy()
    {
        deployed = false;
        vehicleMovement?.SetPhysicsEnabled(false);
        vehicleBody.linearVelocity = Vector3.zero;
        vehicleBody.angularVelocity = Vector3.zero;
        vehicleBody.isKinematic = true;
        vehicleCollider.enabled = false;
        robotController.enabled = false;
        SetVisualForm(PrototypeRobotForm.Robot, 0f);
        enabled = false;
    }

    public bool TryToggleTransform()
    {
        if (!deployed || IsTransforming || stateMachine == null || health == null || !health.IsAlive)
        {
            return false;
        }

        var blocked = HasTransformationBlocker();
        var canTransform = IsRobotForm ? IsGrounded : vehicleMovement != null && vehicleMovement.CanTransformToRobot;
        if (!stateMachine.TryRequestToggle(canTransform, Speed, blocked))
        {
            return false;
        }

        switchedPhysicsForTransform = false;
        robotVerticalVelocity = 0f;
        GetComponent<PrototypeProceduralPresentation>()?.PulseTransform();
        GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.Transform);
        if (!vehicleBody.isKinematic)
        {
            vehicleBody.linearVelocity = Vector3.zero;
            vehicleBody.angularVelocity = Vector3.zero;
        }

        return true;
    }

    public void AdvanceTransform(float deltaTime)
    {
        if (!IsTransforming)
        {
            return;
        }

        var progress = TransformProgress;
        SetVisualForm(Form, progress);
        if (!switchedPhysicsForTransform && progress >= 0.5f)
        {
            switchedPhysicsForTransform = true;
            if (State == PrototypeRobotTransformState.TransformingToRobot)
            {
                SwitchToRobotPhysics();
            }
            else
            {
                SwitchToVehiclePhysics();
            }
        }

        if (stateMachine.Advance(deltaTime))
        {
            var completedForm = stateMachine.Form;
            SetVisualForm(completedForm, 1f);
            if (completedForm == PrototypeRobotForm.Robot)
            {
                SwitchToRobotPhysics();
            }
            else
            {
                SwitchToVehiclePhysics();
            }
        }
    }

    private void Update()
    {
        if (!deployed || PrototypeGameManager.IsPaused || health == null || !health.IsAlive)
        {
            return;
        }

        if (PrototypeInput.TransformPressed)
        {
            TryToggleTransform();
        }

        if (IsTransforming)
        {
            AdvanceTransform(Time.deltaTime);
            return;
        }

        if (IsRobotForm)
        {
            UpdateRobotMovement();
        }
        else if (IsVehicleForm)
        {
            vehicleMovement?.TickInput();
        }

        RecoverFromInvalidPosition();
    }

    private void UpdateRobotMovement()
    {
        var input = PrototypeInput.Move;
        var camera = Camera.main;
        var forward = camera != null ? camera.transform.forward : Vector3.forward;
        var right = camera != null ? camera.transform.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        var direction = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        if (robotController.isGrounded && robotVerticalVelocity < 0f)
        {
            robotVerticalVelocity = -2f;
        }

        if (PrototypeInput.JumpPressed && robotController.isGrounded)
        {
            robotVerticalVelocity = Mathf.Sqrt(config.robotJumpHeight * -2f * config.robotGravity);
        }

        robotVerticalVelocity += config.robotGravity * Time.deltaTime;
        var speed = PrototypeInput.IsRunning ? config.robotRunSpeed : config.robotWalkSpeed;
        var motion = direction * speed;
        motion.y = robotVerticalVelocity;
        robotController.Move(motion * Time.deltaTime);
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);
        }
    }

    private bool HasTransformationBlocker()
    {
        var scanRadius = IsVehicleForm ? 2.3f : 1.45f;
        foreach (var collider in Physics.OverlapSphere(transform.position + Vector3.up, scanRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (collider == null || collider.transform.IsChildOf(transform) || collider.GetComponentInParent<PrototypeTransformerController>() == this)
            {
                continue;
            }

            if (collider.GetComponentInParent<PrototypeEnemy>() != null || collider.GetComponentInParent<PrototypePlayerController>() != null)
            {
                return true;
            }

            var closest = collider.ClosestPoint(transform.position + Vector3.up);
            if (Vector3.Distance(closest, transform.position + Vector3.up) < 0.55f)
            {
                return true;
            }
        }

        return false;
    }

    private void SwitchToVehiclePhysics()
    {
        var position = transform.position;
        robotController.enabled = false;
        vehicleCollider.enabled = true;
        vehicleBody.isKinematic = false;
        vehicleBody.position = position;
        vehicleBody.rotation = transform.rotation;
        vehicleMovement?.SetPhysicsEnabled(true);
    }

    private void SwitchToRobotPhysics()
    {
        var position = vehicleBody != null ? vehicleBody.position : transform.position;
        var rotation = vehicleBody != null ? vehicleBody.rotation : transform.rotation;
        vehicleMovement?.SetPhysicsEnabled(false);
        vehicleBody.linearVelocity = Vector3.zero;
        vehicleBody.angularVelocity = Vector3.zero;
        vehicleBody.isKinematic = true;
        vehicleCollider.enabled = false;
        transform.position = position + Vector3.up * 0.12f;
        transform.rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
        robotController.enabled = true;
        robotVerticalVelocity = -2f;
    }

    private void SetRobotPhysics(bool robotMode)
    {
        if (robotMode) SwitchToRobotPhysics();
        else SwitchToVehiclePhysics();
    }

    private void SetVisualForm(PrototypeRobotForm form, float progress)
    {
        if (robotVisual == null || vehicleVisual == null)
        {
            return;
        }

        var robotShown = form == PrototypeRobotForm.Robot ? 1f - Mathf.Clamp01(progress * 1.25f) : Mathf.Clamp01((progress - 0.15f) * 1.25f);
        var vehicleShown = form == PrototypeRobotForm.Robot ? Mathf.Clamp01((progress - 0.15f) * 1.25f) : 1f - Mathf.Clamp01(progress * 1.25f);
        if (!IsTransforming)
        {
            robotShown = form == PrototypeRobotForm.Robot ? 1f : 0f;
            vehicleShown = form == PrototypeRobotForm.Robot ? 0f : 1f;
        }

        robotVisual.gameObject.SetActive(robotShown > 0.01f);
        vehicleVisual.gameObject.SetActive(vehicleShown > 0.01f);
        robotVisual.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, robotShown);
        vehicleVisual.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, vehicleShown);
        robotVisual.localPosition = Vector3.up * Mathf.Lerp(0.25f, 0f, robotShown);
        vehicleVisual.localPosition = Vector3.up * Mathf.Lerp(0.4f, 0f, vehicleShown);
    }

    private void RecoverFromInvalidPosition()
    {
        var position = transform.position;
        var invalid = float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z)
            || float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z)
            || position.y < -8f || Mathf.Abs(position.x) > config.arenaHalfExtent + 8f || Mathf.Abs(position.z) > config.arenaHalfExtent + 8f;
        if (!invalid)
        {
            return;
        }

        var safePosition = new Vector3(0f, 1.05f, -28f);
        vehicleBody.isKinematic = true;
        vehicleCollider.enabled = false;
        robotController.enabled = false;
        vehicleMovement?.Recover(safePosition);
        transform.SetPositionAndRotation(safePosition, Quaternion.identity);
        stateMachine.InterruptAndRecover();
        switchedPhysicsForTransform = false;
        SetVisualForm(PrototypeRobotForm.Robot, 1f);
        SetRobotPhysics(true);
    }

    private void OnDied(PrototypeHealth deadHealth)
    {
        vehicleMovement?.SetPhysicsEnabled(false);
        vehicleBody.linearVelocity = Vector3.zero;
        vehicleBody.angularVelocity = Vector3.zero;
        vehicleBody.isKinematic = true;
        deployed = false;
        enabled = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        combat?.HandleVehicleCollision(collision);
    }

    private float GetCameraDistance(PrototypeRobotForm form)
    {
        switch (form)
        {
            case PrototypeRobotForm.Helicopter: return config.helicopterCameraDistance;
            case PrototypeRobotForm.Tank: return config.tankCameraDistance;
            case PrototypeRobotForm.Motorcycle: return config.motorcycleCameraDistance;
            case PrototypeRobotForm.Jet: return config.jetCameraDistance;
            default: return config.robotCameraDistance;
        }
    }

    private float GetCameraHeight(PrototypeRobotForm form)
    {
        switch (form)
        {
            case PrototypeRobotForm.Helicopter: return config.helicopterCameraHeight;
            case PrototypeRobotForm.Tank: return config.tankCameraHeight;
            case PrototypeRobotForm.Motorcycle: return config.motorcycleCameraHeight;
            case PrototypeRobotForm.Jet: return config.jetCameraHeight;
            default: return config.robotCameraHeight;
        }
    }

    private static Vector3 GetVehicleColliderSize(PrototypeRobotForm form)
    {
        switch (form)
        {
            case PrototypeRobotForm.Helicopter: return new Vector3(3.4f, 1.25f, 4.2f);
            case PrototypeRobotForm.Tank: return new Vector3(3.3f, 1.35f, 4.1f);
            case PrototypeRobotForm.Motorcycle: return new Vector3(1.1f, 1.35f, 2.8f);
            case PrototypeRobotForm.Jet: return new Vector3(3.6f, 1.15f, 5.6f);
            default: return new Vector3(2.25f, 0.85f, 4.1f);
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= OnDied;
        }
    }
}
