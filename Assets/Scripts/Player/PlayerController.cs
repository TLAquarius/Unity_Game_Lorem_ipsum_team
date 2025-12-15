using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Stats")]
    public float moveSpeed = 8f;
    public float jumpForce = 16f; // Increased because we are increasing gravity

    [Header("Physics Feel (Snappy Falling)")]
    public float fallMultiplier = 2.5f; // Gravity increases when falling
    public float lowJumpMultiplier = 2f; // Gravity increases if you tap space lightly

    [Header("Abilities")]
    public bool canDoubleJumpUnlocked = true;
    public bool canWallJumpUnlocked = true;

    [Header("Wall Interaction")]
    public Transform wallCheck;
    public float wallSlidingSpeed = 2f;
    public Vector2 wallJumpPower = new Vector2(8f, 16f); // X is push-off force, Y is up force
    public float wallJumpDuration = 0.2f; // Time input is ignored after wall jump
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
        // We only read input if we are NOT currently in the middle of a wall jump kick
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
            if (isWallSliding && canWallJumpUnlocked)
            {
                StartCoroutine(WallJumpRoutine());
            }
            else if (isGrounded)
            {
                Jump();
            }
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
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, checkRadius, wallLayer);

        // Reset Double Jump
        if (isGrounded && !Input.GetKey(KeyCode.Space))
        {
            doubleJumpAvailable = true;
            isWallJumping = false; // Safety reset
        }

        // --- SECTION 1: MOVEMENT ---
        // Only move if not wall jumping
        if (!isWallJumping && !isWallSliding)
        {
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }
        else if (isWallSliding)
        {
            // Optional: Allow slight movement off wall to let go? 
            // For now, we lock X movement while sliding to stick to wall
        }

        // Flip Character
        if (!isWallJumping && horizontalInput != 0)
        {
            transform.localScale = new Vector3(horizontalInput, 1, 1);
        }

        // --- SECTION 2: BETTER GRAVITY (Snappy Fall) ---
        ApplyBetterGravity();
    }

    void ApplyBetterGravity()
    {
        // 1. Fast Falling: If we are falling (velocity < 0)
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = fallMultiplier;
        }
        // 2. Short Hop: If we are jumping up but let go of Space
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(KeyCode.Space) && !isWallJumping)
        {
            rb.gravityScale = lowJumpMultiplier;
        }
        // 3. Normal Gravity
        else
        {
            rb.gravityScale = 1f;
        }
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Reset Y for consistent height
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    void CheckWallSlide()
    {
        if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0)
        {
            // Only slide if pushing TOWARDS the wall (Optional, but feels better)
            if (horizontalInput != 0)
            {
                isWallSliding = true;
                // Clamp fall speed
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

    // --- SECTION 3: WALL JUMP WITH MOVEMENT LOCK ---
    IEnumerator WallJumpRoutine()
    {
        isWallSliding = false;
        isWallJumping = true; // LOCK MOVEMENT

        // Calculate direction: Jump AWAY from wall
        // If facing Right (1), we want to jump Left (-1)
        float jumpDirection = -facingDirection;

        // Force: X pushes away, Y pushes up
        rb.linearVelocity = new Vector2(jumpDirection * wallJumpPower.x, wallJumpPower.y);

        // Check facing direction to flip sprite immediately
        transform.localScale = new Vector3(jumpDirection, 1, 1);
        facingDirection = (int)jumpDirection;

        // Wait for a fraction of a second (The "Kick" time)
        yield return new WaitForSeconds(wallJumpDuration);

        // Unlock movement
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
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        if (wallCheck != null) Gizmos.DrawWireSphere(wallCheck.position, checkRadius);
    }
}