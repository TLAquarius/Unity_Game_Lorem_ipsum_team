using UnityEngine;

public class Audio : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip footstepSound;
    public AudioClip jumpSound;
    public AudioClip attackSound;
    public AudioClip hurtSound;
    public AudioClip deathSound;
    public AudioClip rollSound;
    public AudioClip shootSound;

// Hàm này sẽ được gọi từ cửa sổ Animation
    public void PlayFootstep()
    {
        if (audioSource != null && footstepSound != null)
        {
            // Chỉnh pitch ngẫu nhiên một chút để nghe tự nhiên hơn
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(footstepSound);
        }
    }
    public void Playjump()
    {
        if (audioSource != null && jumpSound != null)
        {
            audioSource.PlayOneShot(jumpSound);
        }
    }
    public void PlayAttack()
    {
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }
    public void Playroll()
    {
        if (audioSource != null && rollSound != null)
        {
            audioSource.PlayOneShot(rollSound);
        }
    }
    public void PlayHurt()
    {
        if (audioSource != null && hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }
    }
    public void PlayDeath()
    {
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
    }
    public void Playshoot()
    {
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }
}
