using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset;

    void LateUpdate()
    {
        if (!target) return;

        Vector3 desiredPos = target.position + offset;
        Vector3 smoothPos = Vector3.Lerp(
            transform.position,
            desiredPos,
            smoothSpeed * Time.deltaTime
        );

        transform.position = new Vector3(
            smoothPos.x,
            smoothPos.y,
            transform.position.z
        );
    }
}
