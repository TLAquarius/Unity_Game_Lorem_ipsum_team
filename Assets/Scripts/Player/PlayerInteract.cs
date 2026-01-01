using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float interactRange = 1f;
    public LayerMask interactLayer; // Create a layer named "Interactable"
    private GameInput input;

    void Start()
    {
        input = GetComponent<GameInput>();
    }

    void Update()
    {
        if (input.IsInteractPressed())
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRange, interactLayer);
            foreach (Collider2D hit in hits)
            {
                IInteractable interactable = hit.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact();
                    return;
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