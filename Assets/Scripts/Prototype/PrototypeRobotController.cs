using UnityEngine;

[RequireComponent(typeof(PrototypeHealth))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public sealed class PrototypeRobotController : MonoBehaviour
{
    private PrototypeGameConfig config;
    private PrototypeHealth health;
    private CharacterController robotController;
    private Rigidbody carBody;
    private BoxCollider carCollider;
    private PrototypeTransformStateMachine stateMachine;
    private PrototypeRobotCombat combat;
    private PrototypeCarController carController;
    private Transform robotVisual;
    private Transform carVisual;
    private bool deployed;
    private bool switchedPhysicsForTransform;
    private float robotVerticalVelocity;

    public PrototypeRobotTransformState State => stateMachine != null ? stateMachine.State : PrototypeRobotTransformState.Robot;
    public PrototypeRobotForm Form => stateMachine != null ? stateMachine.Form : PrototypeRobotForm.Robot;
    public bool IsDeployed => deployed;
    public bool IsTransforming => stateMachine != null && stateMachine.IsTransforming;
    public bool IsRobotForm => Form == PrototypeRobotForm.Robot && !IsTransforming;
    public bool IsCarForm => Form == PrototypeRobotForm.Car && !IsTransforming;
    public float TransformProgress => stateMachine != null ? stateMachine.Progress : 1f;
    public bool IsGrounded => IsCarForm
        ? carController != null && carController.IsGrounded
        : robotController != null && (robotController.isGrounded || Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore));
    public float Speed => IsCarForm && carBody != null ? carBody.linearVelocity.magnitude : 0f;
    public Vector3 AimPoint => transform.position + Vector3.up * (IsCarForm ? 0.8f : 1.25f);
    public PrototypeHealth Health => health;
    public PrototypeCarController CarController => carController;

    public void Configure(PrototypeGameConfig gameConfig, Transform robotModel, Transform vehicleModel)
    {
        config = gameConfig;
        health = GetComponent<PrototypeHealth>();
        robotController = GetComponent<CharacterController>();
        carBody = GetComponent<Rigidbody>();
        carCollider = GetComponent<BoxCollider>();
        robotVisual = robotModel;
        carVisual = vehicleModel;
        stateMachine = new PrototypeTransformStateMachine(config.robotTransformDuration);
        combat = GetComponent<PrototypeRobotCombat>();
        carController = GetComponent<PrototypeCarController>();
        var profile = GetComponent<PrototypeCharacterProfile>();
        profile?.Configure(PrototypeCharacterId.AtlasRunner, "Atlas Runner", config.robotCameraDistance, config.robotCameraHeight, 2.1f, 0.48f);
        robotController.height = 2.1f;
        robotController.radius = 0.48f;
        robotController.center = Vector3.up * 1.05f;
        robotController.stepOffset = 0.3f;
        robotController.slopeLimit = 48f;
        carBody.mass = 1400f;
        carBody.linearDamping = 0.15f;
        carBody.angularDamping = 4f;
        carBody.interpolation = RigidbodyInterpolation.Interpolate;
        carBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        carBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        carBody.isKinematic = true;
        carCollider.size = new Vector3(2.25f, 0.85f, 4.1f);
        carCollider.center = new Vector3(0f, 0.55f, 0f);
        carCollider.enabled = false;
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
            SwitchToRobotPhysics();
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
        if (!carBody.isKinematic)
        {
            carBody.linearVelocity = Vector3.zero;
            carBody.angularVelocity = Vector3.zero;
        }
        carBody.isKinematic = true;
        carCollider.enabled = false;
        carController?.SetPhysicsEnabled(false);
        robotController.enabled = false;
        SetVisualForm(PrototypeRobotForm.Robot, 0f);
        enabled = false;
    }

    public bool TryToggleTransform()
    {
        if (!deployed || IsTransforming || stateMachine == null)
        {
            return false;
        }

        var wasRobotForm = IsRobotForm;
        var blocked = HasTransformationBlocker();
        var accepted = stateMachine.TryRequestToggle(IsGrounded, Speed, blocked);
        if (!accepted)
        {
            return false;
        }

        switchedPhysicsForTransform = false;
        robotVerticalVelocity = 0f;
        GetComponent<PrototypeProceduralPresentation>()?.PulseTransform();
        GetComponent<PrototypeAudioFeedback>()?.Play(PrototypeAudioCue.Transform);
        if (wasRobotForm && !carBody.isKinematic)
        {
            carBody.linearVelocity = Vector3.zero;
            carBody.angularVelocity = Vector3.zero;
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
            if (State == PrototypeRobotTransformState.TransformingToCar)
            {
                SwitchToCarPhysics();
            }
            else
            {
                SwitchToRobotPhysics();
            }
        }

        if (stateMachine.Advance(deltaTime))
        {
            var completedForm = stateMachine.Form;
            SetVisualForm(completedForm, 1f);
            if (completedForm == PrototypeRobotForm.Car)
            {
                SwitchToCarPhysics();
            }
            else
            {
                SwitchToRobotPhysics();
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
        else if (IsCarForm)
        {
            carController?.TickInput();
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
        var scanRadius = IsCarForm ? 2.1f : 1.45f;
        foreach (var collider in Physics.OverlapSphere(transform.position + Vector3.up, scanRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (collider == null || collider.transform.IsChildOf(transform) || collider.GetComponentInParent<PrototypeRobotController>() == this)
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

    private void SwitchToCarPhysics()
    {
        var position = transform.position;
        robotController.enabled = false;
        carCollider.enabled = true;
        carBody.isKinematic = false;
        if (!carBody.isKinematic)
        {
            carBody.linearVelocity = Vector3.zero;
            carBody.angularVelocity = Vector3.zero;
        }
        carBody.position = position;
        carBody.rotation = transform.rotation;
        carController?.SetPhysicsEnabled(true);
    }

    private void SwitchToRobotPhysics()
    {
        var position = carBody != null ? carBody.position : transform.position;
        var rotation = carBody != null ? carBody.rotation : transform.rotation;
        carController?.SetPhysicsEnabled(false);
        if (!carBody.isKinematic)
        {
            carBody.linearVelocity = Vector3.zero;
            carBody.angularVelocity = Vector3.zero;
        }
        carBody.isKinematic = true;
        carCollider.enabled = false;
        transform.position = position + Vector3.up * 0.12f;
        transform.rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
        robotController.enabled = true;
        robotVerticalVelocity = -2f;
    }

    private void SetRobotPhysics(bool robotMode)
    {
        if (robotMode)
        {
            SwitchToRobotPhysics();
        }
        else
        {
            SwitchToCarPhysics();
        }
    }

    private void SetVisualForm(PrototypeRobotForm form, float progress)
    {
        if (robotVisual == null || carVisual == null)
        {
            return;
        }

        var robotShown = form == PrototypeRobotForm.Robot ? 1f - Mathf.Clamp01(progress * 1.25f) : Mathf.Clamp01((progress - 0.15f) * 1.25f);
        var carShown = form == PrototypeRobotForm.Car ? 1f - Mathf.Clamp01(progress * 1.25f) : Mathf.Clamp01((progress - 0.15f) * 1.25f);
        if (!IsTransforming)
        {
            robotShown = form == PrototypeRobotForm.Robot ? 1f : 0f;
            carShown = form == PrototypeRobotForm.Car ? 1f : 0f;
        }

        robotVisual.gameObject.SetActive(robotShown > 0.01f);
        carVisual.gameObject.SetActive(carShown > 0.01f);
        robotVisual.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, robotShown);
        carVisual.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, carShown);
        robotVisual.localPosition = Vector3.up * Mathf.Lerp(0.25f, 0f, robotShown);
        carVisual.localPosition = Vector3.up * Mathf.Lerp(0.45f, 0f, carShown);
    }

    private void RecoverFromInvalidPosition()
    {
        if (float.IsNaN(transform.position.x) || float.IsNaN(transform.position.y) || float.IsNaN(transform.position.z) || transform.position.y < -8f)
        {
            carBody.isKinematic = true;
            carCollider.enabled = false;
            robotController.enabled = false;
            transform.position = new Vector3(0f, 1.05f, -28f);
            transform.rotation = Quaternion.identity;
            stateMachine.InterruptAndRecover();
            switchedPhysicsForTransform = false;
            SetVisualForm(PrototypeRobotForm.Robot, 1f);
            SetRobotPhysics(true);
        }
    }

    private void OnDied(PrototypeHealth deadHealth)
    {
        if (!carBody.isKinematic)
        {
            carBody.linearVelocity = Vector3.zero;
            carBody.angularVelocity = Vector3.zero;
        }
        carBody.isKinematic = true;
        deployed = false;
        enabled = false;
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= OnDied;
        }
    }
}
