using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float interactRange = 1f;
    public LayerMask interactLayer; // Create a layer named "Interactable"
    public KeyCode interactKey = KeyCode.F;

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRange, interactLayer);
            foreach (Collider2D hit in hits)
            {
                // Check if the object has a script that implements IInteractable
                IInteractable interactable = hit.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact();
                    return; // Interact with only one thing at a time
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}