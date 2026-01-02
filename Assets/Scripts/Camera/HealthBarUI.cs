using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    public PlayerStats playerStats;
    public Image healthFill;

    void Update()
    {
        if (!playerStats || !healthFill) return;

        healthFill.fillAmount =
            playerStats.currentHP / playerStats.maxHP;
    }
}
