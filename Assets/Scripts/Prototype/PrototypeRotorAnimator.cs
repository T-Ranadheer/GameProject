using UnityEngine;

public sealed class PrototypeRotorAnimator : MonoBehaviour
{
    private float degreesPerSecond;

    public void Configure(float speed)
    {
        degreesPerSecond = speed;
    }

    private void Update()
    {
        if (PrototypeGameManager.IsPaused)
        {
            return;
        }

        transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
