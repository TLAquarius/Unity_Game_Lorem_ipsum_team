using UnityEngine;

public class WindParticles : MonoBehaviour
{
    [SerializeField] private WindController windController;
    [SerializeField] private ParticleSystem windParticles;
    [SerializeField] private float emissionRateMultiplier = 10f;
    [SerializeField] private float particleSpeedMultiplier = 0.5f;
    
    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.MainModule mainModule;
    
    void Start()
    {
        if (windParticles == null)
            windParticles = GetComponent<ParticleSystem>();
        
        if (windParticles != null)
        {
            emission = windParticles.emission;
            mainModule = windParticles.main;
        }
    }
    
    void Update()
    {
        if (windController == null || windParticles == null) return;
        
        // Update particle system based on wind strength
        float windStrength = windController.GetWindForceAtPosition(transform.position).magnitude;
        
        // Emission rate based on wind strength
        emission.rateOverTime = windStrength * emissionRateMultiplier;
        
        // Particle speed based on wind direction
        Vector2 windDir = windController.GetWindForceAtPosition(transform.position).normalized;
        mainModule.startSpeed = windStrength * particleSpeedMultiplier;
        
        // Rotate particle system to face wind direction
        if (windDir.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(windDir.y, windDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}