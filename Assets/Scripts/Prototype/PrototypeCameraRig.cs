using UnityEngine;

public sealed class PrototypeCameraRig : MonoBehaviour
{
    private Transform target;
    private PrototypeGameConfig config;
    private float yaw;
    private float pitch = 17f;
    private float targetDistance;
    private float targetHeight;

    public Transform Target => target;
    public float TargetDistance => targetDistance;
    public float TargetHeight => targetHeight;

    public void Configure(Transform followTarget, PrototypeGameConfig gameConfig)
    {
        target = followTarget;
        config = gameConfig;
        yaw = followTarget != null ? followTarget.eulerAngles.y : 0f;
        pitch = 17f;
        targetDistance = config != null ? config.cameraDistance : 7.5f;
        targetHeight = config != null ? config.cameraHeight : 3.2f;
        transform.tag = "MainCamera";
    }

    public void SetTarget(Transform followTarget)
    {
        SetTarget(followTarget, config != null ? config.cameraDistance : 7.5f, config != null ? config.cameraHeight : 3.2f);
    }

    public void SetTarget(Transform followTarget, float distance, float height)
    {
        target = followTarget;
        targetDistance = Mathf.Max(1f, distance);
        targetHeight = Mathf.Max(0.5f, height);
        if (followTarget != null)
        {
            yaw = followTarget.eulerAngles.y;
        }
    }

    private void LateUpdate()
    {
        if (target == null || config == null || PrototypeGameManager.IsPaused)
        {
            return;
        }

        var look = PrototypeInput.Look;
        yaw += look.x * 0.12f;
        pitch = Mathf.Clamp(pitch - look.y * 0.12f, -8f, 62f);

        var robot = target.GetComponent<PrototypeRobotController>();
        var transformer = target.GetComponent<PrototypeTransformerController>();
        var carMode = robot != null && robot.IsCarForm;
        var vehicleMode = transformer != null && transformer.IsVehicleForm;
        var pivotHeight = carMode ? config.carCameraHeight : transformer != null ? transformer.CurrentCameraHeight : targetHeight;
        var pivot = target.position + Vector3.up * pivotHeight;
        var rotation = Quaternion.Euler(pitch, yaw, 0f);
        var cameraDistance = carMode ? config.carCameraDistance : vehicleMode ? transformer.CurrentCameraDistance : targetDistance;
        var desiredPosition = pivot - rotation * Vector3.forward * cameraDistance;
        var direction = desiredPosition - pivot;
        var distance = direction.magnitude;
        if (distance > 0.01f && Physics.SphereCast(pivot, config.cameraCollisionRadius, direction.normalized, out var hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            desiredPosition = pivot + direction.normalized * Mathf.Max(0.8f, hit.distance - config.cameraCollisionRadius);
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-14f * Time.deltaTime));
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 1f - Mathf.Exp(-14f * Time.deltaTime));
    }
}
