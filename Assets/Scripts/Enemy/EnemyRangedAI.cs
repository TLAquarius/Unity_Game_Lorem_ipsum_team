using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyRangedAI : EnemyBase
{
    public enum State { Idle, Patrol, Chase, Attack, Hit }
    public State currentState;

    [Header("References")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public Transform groundCheck;
    public LayerMask playerLayer;

    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float patrolWaitTime = 2f;
    public Transform[] patrolPoints;

    [Header("Combat Ranges")]
    public float detectionRange = 10f;
    public float shootingRange = 6f;
    public float retreatRange = 3f;

    [Header("Attack Settings")]
    public float fireRate = 2f;
    public float attackWindUp = 0.3f;

    private float nextFireTime;
    private int patrolIndex;
    private bool isProvoked = false;
    // REMOVED: private bool isAttacking; << This caused the warning

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        EnemyStats myStats = GetComponent<EnemyStats>();
        if (myStats != null) myStats.OnTakeDamage += ReactToDamage; // Subscribe

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        currentState = State.Patrol;
        StartCoroutine(MainLogic());
    }

    void Update()
    {
        if (anim != null)
        {
            anim.SetBool("isMoving", Mathf.Abs(rb.linearVelocity.x) > 0.1f);
        }
    }

    void ReactToDamage()
    {
        if (currentState != State.Attack && currentState != State.Chase)
        {
            currentState = State.Chase;
            StopCoroutine("ResetProvocation");
            StartCoroutine(ResetProvocation());
        }
    }

    IEnumerator ResetProvocation()
    {
        isProvoked = true;
        yield return new WaitForSeconds(4.0f); // Ranged enemies stay alert longer
        isProvoked = false;
    }

    IEnumerator MainLogic()
    {
        while (true)
        {
            if (player == null) yield break;

            switch (currentState)
            {
                case State.Patrol:
                    yield return StartCoroutine(PatrolRoutine());
                    break;
                case State.Chase:
                    yield return StartCoroutine(ChaseRoutine());
                    break;
                case State.Attack:
                    yield return StartCoroutine(AttackRoutine());
                    break;
            }
            yield return null;
        }
    }

    IEnumerator PatrolRoutine()
    {
        if (patrolPoints.Length == 0)
        {
            while (currentState == State.Patrol)
            {
                if (Vector2.Distance(transform.position, player.position) < detectionRange)
                {
                    currentState = State.Chase;
                }
                yield return new WaitForSeconds(0.2f);
            }
            yield break;
        }

        Transform target = patrolPoints[patrolIndex];

        while (currentState == State.Patrol)
        {
            if (Vector2.Distance(transform.position, player.position) < detectionRange)
            {
                currentState = State.Chase;
                yield break;
            }

            MoveTowards(target.position, moveSpeed * 0.5f);

            if (Vector2.Distance(transform.position, target.position) < 0.5f)
            {
                rb.linearVelocity = Vector2.zero;
                yield return new WaitForSeconds(patrolWaitTime);
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                target = patrolPoints[patrolIndex];
            }
            yield return null;
        }
    }

    IEnumerator ChaseRoutine()
    {
        while (currentState == State.Chase)
        {
            float dist = Vector2.Distance(transform.position, player.position);

            if (!isProvoked && dist > detectionRange * 1.5f)
            {
                currentState = State.Patrol;
                yield break;
            }

            if (dist < retreatRange)
            {
                if (CheckGroundBackwards())
                {
                    MoveAway(player.position, moveSpeed);
                }
                else
                {
                    currentState = State.Attack;
                    yield break;
                }
            }
            else if (dist > shootingRange)
            {
                if (CheckGroundAhead())
                {
                    MoveTowards(player.position, moveSpeed);
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
                if (Time.time >= nextFireTime)
                {
                    currentState = State.Attack;
                    yield break;
                }
            }

            yield return null;
        }
    }

    IEnumerator AttackRoutine()
    {
        // Removed isAttacking = true; 
        rb.linearVelocity = Vector2.zero;
        FaceTarget(player.position);

        if (anim) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(attackWindUp);

        Shoot();

        float cooldown = 1f / fireRate;
        nextFireTime = Time.time + cooldown;

        yield return new WaitForSeconds(0.5f);

        // Removed isAttacking = false;
        currentState = State.Chase;
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Vector2 dir = (player.position - firePoint.position).normalized;

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = dir * 10f;
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void MoveTowards(Vector2 target, float speed)
    {
        float direction = Mathf.Sign(target.x - transform.position.x);
        transform.localScale = new Vector3(direction, 1, 1);
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
    }

    void MoveAway(Vector2 target, float speed)
    {
        float direction = Mathf.Sign(transform.position.x - target.x);
        FaceTarget(player.position);
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
    }

    void FaceTarget(Vector2 target)
    {
        float direction = Mathf.Sign(target.x - transform.position.x);
        transform.localScale = new Vector3(direction, 1, 1);
    }

    bool CheckGroundAhead()
    {
        if (groundCheck == null) return true;
        float facing = transform.localScale.x;
        Vector2 checkPos = (Vector2)groundCheck.position + new Vector2(facing * 0.5f, 0);
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, 1f, LayerMask.GetMask("Ground"));
        return hit.collider != null;
    }

    bool CheckGroundBackwards()
    {
        if (groundCheck == null) return true;
        float facing = transform.localScale.x;
        Vector2 checkPos = (Vector2)groundCheck.position - new Vector2(facing * 0.5f, 0);
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, 1f, LayerMask.GetMask("Ground"));
        return hit.collider != null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootingRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, retreatRange);
    }
}