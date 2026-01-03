using UnityEngine;

public class UIFixedRotation : MonoBehaviour
{
    private Transform parent;

    void Start()
    {
        parent = transform.parent;
    }

    void LateUpdate()
    {
        // 1. Keep Rotation locked to 0
        transform.rotation = Quaternion.identity;

        // 2. Fix Scale flipping
        // If parent is flipped (-1), we flip the UI to (-1) so -1 * -1 = 1 (Normal looking)
        if (parent.localScale.x < 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }
}