using UnityEngine;

public class WindController : MonoBehaviour
{
    public enum WindDirection { Left, Right, Up, Down, TowardsCenter, AwayFromCenter, Custom }
    public enum WindType { Constant, Pulsing, Random, OneShot }
    
    [Header("Wind Properties")]
    [SerializeField] private WindDirection direction = WindDirection.Right;
    [SerializeField] private WindType windType = WindType.Constant;
    [SerializeField] private Vector2 customDirection = Vector2.right;
    [SerializeField] private float windForce = 10f;
    [SerializeField] private float maxForce = 20f;
    
    [Header("Pulsing/Random Settings")]
    [SerializeField] private float pulseFrequency = 2f;
    [SerializeField] private float randomMinForce = 5f;
    [SerializeField] private float randomMaxForce = 15f;
    
    [Header("Area of Effect")]
    [SerializeField] private bool useArea = true;
    [SerializeField] private Vector2 areaSize = new Vector2(5f, 2f);
    [SerializeField] private float radius = 3f;
    [SerializeField] private AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Visuals")]
    [SerializeField] private bool showWindLines = true;
    [SerializeField] private Color windColor = Color.cyan;
    [SerializeField] private float lineLengthMultiplier = 0.5f;
    
    private float currentForce;
    private float pulseTimer;
    private Vector2 currentWindDirection;
    
    void Start()
    {
        UpdateWindDirection();
        currentForce = windForce;
    }
    
    void Update()
    {
        UpdateWindDirection();
        
        switch (windType)
        {
            case WindType.Pulsing:
                PulseWind();
                break;
            case WindType.Random:
                RandomWind();
                break;
        }
        
        if (showWindLines)
        {
            DrawWindVisuals();
        }
    }
    
    void UpdateWindDirection()
    {
        switch (direction)
        {
            case WindDirection.Left:
                currentWindDirection = Vector2.left;
                break;
            case WindDirection.Right:
                currentWindDirection = Vector2.right;
                break;
            case WindDirection.Up:
                currentWindDirection = Vector2.up;
                break;
            case WindDirection.Down:
                currentWindDirection = Vector2.down;
                break;
            case WindDirection.Custom:
                currentWindDirection = customDirection.normalized;
                break;
        }
    }
    
    void PulseWind()
    {
        pulseTimer += Time.deltaTime * pulseFrequency;
        float pulseValue = (Mathf.Sin(pulseTimer * Mathf.PI * 2) + 1) * 0.5f;
        currentForce = Mathf.Lerp(windForce * 0.3f, windForce, pulseValue);
    }
    
    void RandomWind()
    {
        if (Random.value < 0.01f) // 1% chance per frame to change
        {
            currentForce = Random.Range(randomMinForce, randomMaxForce);
        }
    }
    
    public Vector2 GetWindForceAtPosition(Vector2 position)
    {
        if (!useArea)
        {
            return currentWindDirection * currentForce;
        }
        
        // Check if position is in area
        Vector2 localPos = transform.InverseTransformPoint(position);
        
        if (direction == WindDirection.TowardsCenter || direction == WindDirection.AwayFromCenter)
        {
            float distance = Vector2.Distance(position, transform.position);
            if (distance > radius) return Vector2.zero;
            
            Vector2 dir = (direction == WindDirection.TowardsCenter) 
                ? (Vector2)(transform.position - (Vector3)position).normalized 
                : (Vector2)(position - (Vector2)transform.position).normalized;
            
            float falloff = falloffCurve.Evaluate(distance / radius);
            return dir * currentForce * falloff;
        }
        else
        {
            // Rectangular area
            if (Mathf.Abs(localPos.x) > areaSize.x * 0.5f || 
                Mathf.Abs(localPos.y) > areaSize.y * 0.5f)
                return Vector2.zero;
            
            // Calculate falloff based on distance from center of area
            float normalizedX = Mathf.Clamp01(Mathf.Abs(localPos.x) / (areaSize.x * 0.5f));
            float normalizedY = Mathf.Clamp01(Mathf.Abs(localPos.y) / (areaSize.y * 0.5f));
            float falloff = falloffCurve.Evaluate(Mathf.Max(normalizedX, normalizedY));
            
            return currentWindDirection * currentForce * falloff;
        }
    }
    
    public void SetWindForce(float newForce)
    {
        currentForce = Mathf.Clamp(newForce, 0, maxForce);
    }
    
    public void SetWindDirection(Vector2 newDirection)
    {
        customDirection = newDirection.normalized;
        direction = WindDirection.Custom;
    }
    
    public void BlowOneShot(float force)
    {
        StartCoroutine(OneShotWind(force));
    }
    
    System.Collections.IEnumerator OneShotWind(float force)
    {
        float originalForce = currentForce;
        currentForce = force;
        yield return new WaitForSeconds(0.2f);
        currentForce = originalForce;
    }
    
    void DrawWindVisuals()
    {
        Vector2 windVector = currentWindDirection * currentForce * lineLengthMultiplier;
        
        if (useArea)
        {
            // Draw multiple lines in the area
            int lines = 10;
            for (int i = 0; i < lines; i++)
            {
                Vector2 pos = transform.position;
                pos.x += Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f);
                pos.y += Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f);
                
                Debug.DrawRay(pos, windVector, windColor);
            }
        }
        else
        {
            Debug.DrawRay(transform.position, windVector, windColor);
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(windColor.r, windColor.g, windColor.b, 0.3f);
        
        if (useArea)
        {
            if (direction == WindDirection.TowardsCenter || direction == WindDirection.AwayFromCenter)
            {
                Gizmos.DrawWireSphere(transform.position, radius);
            }
            else
            {
                Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, 1));
            }
        }
        
        // Draw direction arrow
        Gizmos.color = windColor;
        Vector3 arrowEnd = transform.position + (Vector3)currentWindDirection * 2f;
        Gizmos.DrawLine(transform.position, arrowEnd);
        
        // Draw arrowhead
        Vector3 right = Quaternion.Euler(0, 0, 30) * -currentWindDirection * 0.5f;
        Vector3 left = Quaternion.Euler(0, 0, -30) * -currentWindDirection * 0.5f;
        Gizmos.DrawLine(arrowEnd, arrowEnd + (Vector3)right);
        Gizmos.DrawLine(arrowEnd, arrowEnd + (Vector3)left);
    }
}