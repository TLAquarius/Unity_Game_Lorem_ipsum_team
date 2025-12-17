using UnityEngine;

public class DestroyEffect : MonoBehaviour
{
    // This function name matches the event in SlideDust.anim
    public void destroyEvent()
    {
        Destroy(gameObject);
    }
}