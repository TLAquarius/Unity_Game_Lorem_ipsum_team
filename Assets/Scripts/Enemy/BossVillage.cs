using UnityEngine;
using System.Collections;

public class BossVillage : BossBase // Inherits from BossBase
{
    [Header("Village Boss Skills")]
    public float chargeSpeed = 8f;
    public float smashRange = 2.5f;

    private bool isAttacking = false;

    void Update()
    {
        if (player == null || isDead) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (!isAttacking)
        {
            if (dist > smashRange)
            {
                // Chase Player
                transform.position = Vector2.MoveTowards(transform.position, player.position, 2f * Time.deltaTime);
            }
            else
            {
                StartCoroutine(SmashAttack());
            }
        }
    }

    IEnumerator SmashAttack()
    {
        isAttacking = true;

        // 1. Telegraph (Red Flash) - Player must DASH now!
        GetComponent<SpriteRenderer>().color = Color.yellow;
        yield return new WaitForSeconds(1.0f); // Slow warning

        // 2. Attack
        GetComponent<SpriteRenderer>().color = Color.white;
        Debug.Log("VILLAGE BOSS SMASH!");

        // Logic to deal damage in a circle...
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, smashRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player")) hit.GetComponent<PlayerStats>().TakeDamage(20);
        }

        yield return new WaitForSeconds(2.0f); // Long recovery
        isAttacking = false;
    }
}