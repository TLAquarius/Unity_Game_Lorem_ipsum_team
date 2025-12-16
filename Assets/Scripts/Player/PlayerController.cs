using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Stats")]
    public float moveSpeed = 8f;
    public float jumpForce = 16f;

    [Header("Physics Feel (Snappy Falling)")]
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    [Header("Abilities")]
    public bool canDoubleJumpUnlocked = true;
    public bool canWallJumpUnlocked = true;

    [Header("Wall Interaction")]
    public Transform wallCheck;
    public float wallSlidingSpeed = 2f;
    public Vector2 wallJumpPower = new Vector2(8f, 16f);
    public float wallJumpDuration = 0.2f;
    private bool isWallSliding;
    private bool isWallJumping;

    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private bool canDash = true;
    private bool isDashing = false;

    [Header("Checks")]
    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public Vector2 wallCheckSize = new Vector2(0.5f, 1.5f);
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    // Internal State
    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private bool isTouchingWall;
    private bool doubleJumpAvailable;
    private int facingDirection = 1;

    // Layer IDs
    int playerLayer;
    int enemyLayer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerLayer = LayerMask.NameToLayer("Player");
        enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    void Update()
    {
        if (isDashing) return;

        // 1. INPUT
        if (!isWallJumping)
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
        }

        // Determine facing direction (only if moving)
        if (horizontalInput > 0) facingDirection = 1;
        else if (horizontalInput < 0) facingDirection = -1;

        // 2. JUMP INPUT
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Priority 1: Wall Jump (Must be sliding or touching wall in air)
            if (isTouchingWall && !isGrounded && canWallJumpUnlocked)
            {
                StartCoroutine(WallJumpRoutine());
            }
            // Priority 2: Ground Jump
            else if (isGrounded)
            {
                Jump();
            }
            // Priority 3: Double Jump
            else if (canDoubleJumpUnlocked && doubleJumpAvailable)
            {
                Jump();
                doubleJumpAvailable = false;
            }
        }

        // 3. DASH INPUT
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(DashRoutine());
        }

        CheckWallSlide();
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        isTouchingWall = Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0f, wallLayer);

        // --- DOUBLE JUMP RESET ---
        // Reset if we touch Ground OR start Wall Sliding
        if ((isGrounded || isWallSliding) && !Input.GetKey(KeyCode.Space))
        {
            doubleJumpAvailable = true;
            isWallJumping = false;
        }

        // --- MOVEMENT ---
        if (!isWallJumping && !isWallSliding)
        {
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }

        // Flip Character (Only if not wall jumping)
        if (!isWallJumping && horizontalInput != 0)
        {
            transform.localScale = new Vector3(horizontalInput, 1, 1);
        }

        // --- GRAVITY ---
        ApplyBetterGravity();
    }

    void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = fallMultiplier;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(KeyCode.Space) && !isWallJumping)
        {
            rb.gravityScale = lowJumpMultiplier;
        }
        else
        {
            rb.gravityScale = 1f;
        }
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    void CheckWallSlide()
    {
        // Conditions: Touching wall, In Air, Falling Down
        if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0)
        {
            // CRITICAL FIX: Only slide if pushing INTO the wall.
            // Since 'wallCheck' is in front, if Input matches Facing Direction, we are pushing into it.
            // If Input is 0 (Let go) or Opposite (Pull away), we STOP sliding.
            if (horizontalInput == facingDirection)
            {
                isWallSliding = true;
                // Clamp fall speed for the "Slide" effect
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -wallSlidingSpeed, float.MaxValue));
            }
            else
            {
                isWallSliding = false;
            }
        }
        else
        {
            isWallSliding = false;
        }
    }

    IEnumerator WallJumpRoutine()
    {
        isWallSliding = false;
        isWallJumping = true;

        // Jump AWAY from wall
        float jumpDirection = -facingDirection;

        // Apply Force
        rb.linearVelocity = new Vector2(jumpDirection * wallJumpPower.x, wallJumpPower.y);

        // Visual Flip
        transform.localScale = new Vector3(jumpDirection, 1, 1);
        facingDirection = (int)jumpDirection;

        // Lock Input for a split second
        yield return new WaitForSeconds(wallJumpDuration);

        isWallJumping = false;
    }

    private IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null) stats.SetInvincible(true);
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        if (stats != null) stats.SetInvincible(false);
        rb.gravityScale = originalGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
        if (wallCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(wallCheck.position, wallCheckSize);
        }
    }
}