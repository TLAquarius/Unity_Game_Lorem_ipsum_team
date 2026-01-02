using UnityEngine;
using System.Collections;

public class TrapdoorNoReset2D : MonoBehaviour
{
    [Header("Timing Settings")]
    [SerializeField] private float disappearTime = 3f;     // Time before disappearing
    [SerializeField] private float respawnDelay = 3f;     // Time before reappearing
    
    [Header("Visual Settings")]
    [SerializeField] private bool useWarning = true;
    [SerializeField] private float warningTime = 1f;      // Warning before disappearing
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private int blinkCount = 5;
    
    // Components
    private SpriteRenderer spriteRenderer;
    private Collider2D trapdoorCollider;
    private Color originalColor;
    
    // State
    private bool isActive = true;
    private bool isCounting = false;      // Timer is counting
    private bool hasStarted = false;      // Player has triggered the timer
    private float currentTimer = 0f;
    private Coroutine currentCoroutine;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        trapdoorCollider = GetComponent<Collider2D>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    void Update()
    {
        // If timer has started and trapdoor is active, keep counting
        if (hasStarted && isActive)
        {
            currentTimer += Time.deltaTime;
            
            // Visual feedback - change color based on time
            if (spriteRenderer != null)
            {
                float progress = currentTimer / disappearTime;
                spriteRenderer.color = Color.Lerp(originalColor, warningColor, progress);
            }
            
            // Check if time is complete
            if (currentTimer >= disappearTime)
            {
                if (currentCoroutine == null)
                {
                    currentCoroutine = StartCoroutine(DisappearSequence());
                }
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isActive && collision.gameObject.CompareTag("Player"))
        {
            // Start the timer when player first steps on
            if (!hasStarted)
            {
                hasStarted = true;
                currentTimer = 0f;
                isCounting = true;
                Debug.Log("Trapdoor timer started!");
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        // DELIBERATELY EMPTY - don't reset when player leaves
        // The timer continues counting even if player jumps off
    }

    IEnumerator DisappearSequence()
    {
        isActive = false;
        isCounting = false;
        
        Debug.Log("Trapdoor disappearing...");
        
        // Optional warning phase with blinking
        if (useWarning && warningTime > 0)
        {
            yield return StartCoroutine(BlinkWarning());
        }
        
        // Disable the trapdoor
        if (spriteRenderer != null) 
        {
            spriteRenderer.enabled = false;
        }
        
        if (trapdoorCollider != null) 
        {
            trapdoorCollider.enabled = false;
        }
        
        // Wait for respawn
        yield return new WaitForSeconds(respawnDelay);
        
        // Re-enable the trapdoor
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalColor;
        }
        
        if (trapdoorCollider != null) 
        {
            trapdoorCollider.enabled = true;
        }
        
        // Reset all state
        isActive = true;
        hasStarted = false;
        isCounting = false;
        currentTimer = 0f;
        currentCoroutine = null;
        
        Debug.Log("Trapdoor reset and ready!");
    }

    IEnumerator BlinkWarning()
    {
        float elapsed = 0f;
        bool isVisible = true;
        
        while (elapsed < warningTime)
        {
            elapsed += Time.deltaTime;
            
            // Calculate blink frequency
            float blinkInterval = warningTime / (blinkCount * 2);
            int currentBlink = Mathf.FloorToInt(elapsed / blinkInterval);
            
            // Toggle visibility
            isVisible = currentBlink % 2 == 0;
            
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = isVisible;
            }
            
            yield return null;
        }
        
        // Ensure it's visible one last time
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }
    
    // Optional: Public method to manually reset if needed
    public void ResetTrapdoor()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        
        isActive = true;
        hasStarted = false;
        isCounting = false;
        currentTimer = 0f;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalColor;
        }
        
        if (trapdoorCollider != null)
        {
            trapdoorCollider.enabled = true;
        }
    }
}