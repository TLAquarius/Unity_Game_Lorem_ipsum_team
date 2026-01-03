using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class EnemyBase : MonoBehaviour
{
    [Header("Combat Stats")]
    public float touchDamage = 5f;
    public float attackDamage = 10f;

    [Header("Knockback Settings")]
    public Vector2 knockbackProfile = new Vector2(8f, 5f);

    [Header("Visual Feedback")]
    public Color damageColor = Color.red; // Color to flash when hit
    public float flashDuration = 0.1f;    // How fast it flashes
    private SpriteRenderer sr;
    private Color originalColor;
    private Coroutine flashRoutine;

    [Header("Audio Settings")]
    public AudioClip hitSound;
    public AudioClip walkSound;
    public AudioClip deathSound;           // <--- NEW: Drag your death sound here in Inspector
    public float walkSoundInterval = 0.4f; // Time between footsteps

    protected AudioSource audioSource;
    private float nextWalkSoundTime;

    protected virtual void Awake()
    {
        // Get Components automatically
        sr = GetComponentInChildren<SpriteRenderer>(); // Finds sprite even on child object
        audioSource = GetComponent<AudioSource>();

        if (sr != null) originalColor = sr.color;

        // Setup AudioSource defaults
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D Sound (Get quieter when far away)
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = 15f;
        }
    }

    // 1. TOUCH DAMAGE (Collision)
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            float hitDirection = Mathf.Sign(collision.transform.position.x - transform.position.x);
            ApplyCustomKnockback(collision.gameObject, touchDamage, hitDirection);
        }
    }

    // 2. APPLY DAMAGE & EFFECTS
    public void ApplyCustomKnockback(GameObject target, float damageAmount, float hitDirectionX)
    {
        // A. Deal Damage
        PlayerStats hp = target.GetComponent<PlayerStats>();
        if (hp != null) hp.TakeDamage(damageAmount);

        // B. Player Knockback
        Vector2 finalForce = new Vector2(knockbackProfile.x * hitDirectionX, knockbackProfile.y);
        PlayerController movement = target.GetComponent<PlayerController>();
        if (movement != null) movement.ApplyKnockback(finalForce);
    }

    // 3. GETTING HIT (Called by Child Scripts inside ReactToDamage)
    public void PlayHitFeedback()
    {
        // A. Play Sound
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        // B. Flash Sprite
        if (sr != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashEffect());
        }
    }

    // 4. NEW: DEATH SOUND (Called by EnemyStats)
    public void PlayDeathSound()
    {
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
    }

    // 5. WALKING SOUND (Called by Child Scripts in Update)
    public void HandleWalkSound(bool isMoving)
    {
        if (isMoving && Time.time >= nextWalkSoundTime)
        {
            if (walkSound != null && audioSource != null)
            {
                // Randomize pitch slightly for variety
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(walkSound, 0.5f); // 0.5f volume

                nextWalkSoundTime = Time.time + walkSoundInterval;
            }
        }
    }

    private IEnumerator FlashEffect()
    {
        sr.color = damageColor;
        yield return new WaitForSeconds(flashDuration);
        sr.color = originalColor;
    }
}