using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Tooltip("The Transform the camera should follow (set by GameManager at runtime).")]
    public Transform target;

    [Tooltip("How quickly the camera catches up to the target.")]
    public float smoothTime = 0.15f;

    private Vector3 _velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null) return;

        // Keep the camera's Z as-is (e.g., -10), follow X/Y
        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
    }
}