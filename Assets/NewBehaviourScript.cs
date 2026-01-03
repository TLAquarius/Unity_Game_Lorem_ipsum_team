using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    public AudioSource audioSource;
    
    [Header("Audio Clips")]
    public AudioClip jumpSound;
    public AudioClip attackSound;
    public AudioClip walkSound;
    public AudioClip shootSound;
    public AudioClip rollSound;
    public AudioClip hurtSound;
    public AudioClip deathSound;


    // Hàm để gọi từ Script di chuyển hoặc Animation Event
    public void PlayJump() => audioSource.PlayOneShot(jumpSound);
    public void PlayAttack() => audioSource.PlayOneShot(attackSound);
    public void PlayFootstep() 
    {
    if (audioSource != null && walkSound != null)
        audioSource.PlayOneShot(walkSound);
    }
    public void PlayShoot() => audioSource.PlayOneShot(shootSound);
    public void PlayRoll() => audioSource.PlayOneShot(rollSound);
    public void PlayHurt() => audioSource.PlayOneShot(hurtSound);

}