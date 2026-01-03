using UnityEngine;

public class PlayerWindInteraction : MonoBehaviour
{
    [Header("Wind Response")]
    [SerializeField] private float windResistance = 0.8f;
    [SerializeField] private float weight = 1f;
    [SerializeField] private bool canBeBlown = true;
    [SerializeField] private bool useRigidbody = true;
    
    [Header("Audio")]
    [SerializeField] private AudioSource windAudio;
    [SerializeField] private float maxVolume = 0.5f;
    [SerializeField] private float volumeLerpSpeed = 2f;
    
    private Rigidbody2D rb;
    private CharacterController characterController;
    private Vector2 totalWindForce;
    private float targetVolume;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        characterController = GetComponent<CharacterController>();
        
        if (windAudio != null)
        {
            windAudio.loop = true;
            windAudio.volume = 0;
        }
    }
    
    void FixedUpdate()
    {
        if (!canBeBlown || totalWindForce.magnitude < 0.1f)
        {
            targetVolume = 0;
            UpdateAudio();
            return;
        }
        
        ApplyWindForce();
        UpdateAudio();
        totalWindForce = Vector2.zero;
    }
    
    void ApplyWindForce()
    {
        Vector2 effectiveForce = totalWindForce * (1f / weight);
        
        if (useRigidbody && rb != null)
        {
            rb.AddForce(effectiveForce, ForceMode2D.Force);
        }
        else if (characterController != null)
        {
            characterController.Move(effectiveForce * Time.fixedDeltaTime);
        }
        else
        {
            transform.Translate(effectiveForce * Time.fixedDeltaTime, Space.World);
        }
    }
    
    public void AddWindForce(Vector2 force)
    {
        totalWindForce += force * windResistance;
        
        // Update audio based on wind strength
        if (windAudio != null)
        {
            targetVolume = Mathf.Clamp01(force.magnitude / 20f) * maxVolume;
        }
    }
    
    void UpdateAudio()
    {
        if (windAudio != null)
        {
            windAudio.volume = Mathf.Lerp(windAudio.volume, targetVolume, 
                Time.deltaTime * volumeLerpSpeed);
            
            if (targetVolume > 0.01f && !windAudio.isPlaying)
            {
                windAudio.Play();
            }
            else if (targetVolume < 0.01f && windAudio.isPlaying)
            {
                windAudio.Stop();
            }
        }
    }
    
    public void SetCanBeBlown(bool state)
    {
        canBeBlown = state;
    }
    
    public void SetWeight(float newWeight)
    {
        weight = Mathf.Max(0.1f, newWeight);
    }
}