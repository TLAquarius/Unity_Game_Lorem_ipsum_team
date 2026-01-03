using UnityEngine;
using System.Collections.Generic;

public class WindArea : MonoBehaviour
{
    [SerializeField] private WindController windController;
    [SerializeField] private bool affectPlayer = true;
    [SerializeField] private bool affectRigidbodies = true;
    [SerializeField] private string playerTag = "Player";
    
    private List<PlayerWindInteraction> playersInWind = new List<PlayerWindInteraction>();
    private List<Rigidbody2D> rigidbodiesInWind = new List<Rigidbody2D>();
    
    void Start()
    {
        if (windController == null)
            windController = GetComponent<WindController>();
    }
    
    void FixedUpdate()
    {
        if (windController == null) return;
        
        // Apply wind to players
        foreach (var player in playersInWind)
        {
            if (player != null)
            {
                Vector2 windForce = windController.GetWindForceAtPosition(player.transform.position);
                player.AddWindForce(windForce);
            }
        }
        
        // Apply wind to other rigidbodies
        if (affectRigidbodies)
        {
            foreach (var rb in rigidbodiesInWind)
            {
                if (rb != null)
                {
                    Vector2 windForce = windController.GetWindForceAtPosition(rb.position);
                    rb.AddForce(windForce);
                }
            }
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (affectPlayer && other.CompareTag(playerTag))
        {
            PlayerWindInteraction playerWind = other.GetComponent<PlayerWindInteraction>();
            if (playerWind != null && !playersInWind.Contains(playerWind))
            {
                playersInWind.Add(playerWind);
            }
        }
        
        if (affectRigidbodies)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null && !rigidbodiesInWind.Contains(rb))
            {
                rigidbodiesInWind.Add(rb);
            }
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (affectPlayer && other.CompareTag(playerTag))
        {
            PlayerWindInteraction playerWind = other.GetComponent<PlayerWindInteraction>();
            if (playerWind != null)
            {
                playersInWind.Remove(playerWind);
            }
        }
        
        if (affectRigidbodies)
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rigidbodiesInWind.Remove(rb);
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (windController != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.2f);
            
            // Visualize wind area
            BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                Gizmos.DrawCube(transform.position + (Vector3)boxCollider.offset, 
                    boxCollider.size * transform.localScale);
            }
            
            CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider != null)
            {
                Gizmos.DrawSphere(transform.position + (Vector3)circleCollider.offset, 
                    circleCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y));
            }
        }
    }
}