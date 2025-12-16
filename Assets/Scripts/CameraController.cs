using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Transform target;

    public void SetTarget(Transform t)
    {
        target = t;
    }

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = new Vector3(
            target.position.x,
            target.position.y,
            -10f
        );
    }
}
