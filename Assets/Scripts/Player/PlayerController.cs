using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Stats")]
    public float moveSpeed = 8f;
    public float jumpForce = 16f;

    [Header("Physics Feel")]
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

    [Header("Visual Effects")]
    public GameObject wallSlideDustPrefab;

    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private bool canDash = true;
    private bool isDashing = false;

    [Header("Checks")]
    public Transform groundCheck;
    public Vector2 wallCheckSize = new Vector2(0.5f, 1.5f);
    public Vector2 groundCheckSize = new Vector2(0.5f, 1.5f);
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    // State Variables
    private bool isKnockedBack = false;

    // References
    private GameInput input;
    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer sr; // Reference for flashing red

    private float horizontalInput;
    private bool isGrounded;
    private bool isTouchingWall;
    private bool doubleJumpAvailable;
    private int facingDirection = 1;

    int playerLayer;
    int enemyLayer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        input = GetComponent<GameInput>();
        sr = GetComponentInChildren<SpriteRenderer>(); // Finds sprite even if on child object

        playerLayer = LayerMask.NameToLayer("Player");
        enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    void Update()
    {
        // 1. BLOCK INPUT IF HURT OR DASHING
        if (isDashing || isKnockedBack) return;

        // 2. INPUT
        if (!isWallJumping)
        {
            // Use your Input System if available, otherwise fallback to legacy for safety
            if (input != null)
            {
                Vector2 move = input.GetMovementInput();
                horizontalInput = move.x;
            }
            else
            {
                horizontalInput = Input.GetAxisRaw("Horizontal");
            }
        }

        // 3. FLIP CHARACTER
        if (!isWallJumping && horizontalInput != 0)
        {
            facingDirection = (horizontalInput > 0) ? 1 : -1;
            transform.localScale = new Vector3(facingDirection, 1, 1);
        }

        // 4. PHYSICS CHECKS
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        isTouchingWall = Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0f, wallLayer);

        // 5. JUMP LOGIC
        // Check input existence to prevent crashes
        bool jumpPressed = (input != null) ? input.IsJumpPressed() : Input.GetButtonDown("Jump");

        if (jumpPressed)
        {
            if (isTouchingWall && !isGrounded && canWallJumpUnlocked)
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

        // 6. DASH LOGIC
        bool dashPressed = (input != null) ? input.IsDashPressed() : Input.GetKeyDown(KeyCode.LeftShift);
        if (dashPressed && canDash)
            StartCoroutine(DashRoutine());

        // 7. SLIDE & ANIMATION
        CheckWallSlide();
        UpdateAnimations();
    }

    void UpdateAnimations()
    {
        if (anim == null) return;

        if (Mathf.Abs(horizontalInput) > 0.1f)
            anim.SetInteger("AnimState", 1);
        else
            anim.SetInteger("AnimState", 0);

        anim.SetBool("Grounded", isGrounded);
        anim.SetFloat("AirSpeedY", rb.linearVelocity.y);
        anim.SetBool("WallSlide", isWallSliding);
    }

    void FixedUpdate()
    {
        // DISABLE PHYSICS MOVEMENT IF HURT/DASHING
        if (isDashing || isKnockedBack) return;

        // Reset Double Jump
        bool jumpHeld = (input != null) ? input.IsJumpHeld() : Input.GetButton("Jump");

        if ((isGrounded || isWallSliding) && !jumpHeld)
        {
            doubleJumpAvailable = true;
            isWallJumping = false;
        }

        // Apply Movement
        if (!isWallJumping && !isWallSliding)
        {
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }

        ApplyBetterGravity(jumpHeld);
    }

    void ApplyBetterGravity(bool jumpHeld)
    {
        if (rb.linearVelocity.y < 0) rb.gravityScale = fallMultiplier;
        else if (rb.linearVelocity.y > 0 && !jumpHeld && !isWallJumping) rb.gravityScale = lowJumpMultiplier;
        else rb.gravityScale = 1f;
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        if (anim != null) anim.SetTrigger("Jump");
    }

    void CheckWallSlide()
    {
        if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0)
        {
            if (horizontalInput == facingDirection)
            {
                isWallSliding = true;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -wallSlidingSpeed, float.MaxValue));
            }
            else isWallSliding = false;
        }
        else isWallSliding = false;
    }

    public void AE_SlideDust()
    {
        if (wallSlideDustPrefab != null && wallCheck != null)
        {
            GameObject dust = Instantiate(wallSlideDustPrefab, wallCheck.position, Quaternion.identity);
            if (transform.localScale.x < 0)
            {
                Vector3 rot = dust.transform.rotation.eulerAngles;
                rot.y = 180;
                dust.transform.rotation = Quaternion.Euler(rot);
            }
        }
    }

    IEnumerator WallJumpRoutine()
    {
        isWallSliding = false;
        isWallJumping = true;

        if (anim != null) anim.SetTrigger("Jump");

        float jumpDirection = -facingDirection;
        rb.linearVelocity = new Vector2(jumpDirection * wallJumpPower.x, wallJumpPower.y);

        transform.localScale = new Vector3(jumpDirection, 1, 1);
        facingDirection = (int)jumpDirection;

        yield return new WaitForSeconds(wallJumpDuration);
        isWallJumping = false;
    }

    private IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;

        if (anim != null) anim.SetTrigger("Roll");

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

    // --- DATA SAVING ---
    public void SaveData(SaveData data)
    {
        if (canDoubleJumpUnlocked) data.unlockedFlags.Add("DoubleJump");
        if (canWallJumpUnlocked) data.unlockedFlags.Add("WallJump");

        data.positionX = transform.position.x;
        data.positionY = transform.position.y;
        data.positionZ = transform.position.z;
    }

    public void LoadData(SaveData data)
    {
        canDoubleJumpUnlocked = data.unlockedFlags.Contains("DoubleJump");
        canWallJumpUnlocked = data.unlockedFlags.Contains("WallJump");

        // Safety check to ensure we don't load 0,0,0 if no data exists
        if (data.positionX != 0 || data.positionY != 0)
        {
            transform.position = new Vector3(data.positionX, data.positionY, data.positionZ);
        }
    }

    // --- UPDATED KNOCKBACK SYSTEM ---
    // This now accepts a Vector2 so the Enemy calculates the direction and lift
    public void ApplyKnockback(Vector2 forceVector)
    {
        if (isKnockedBack) return; // Prevent infinite stun lock
        StartCoroutine(KnockbackRoutine(forceVector));
    }

    private IEnumerator KnockbackRoutine(Vector2 forceVector)
    {
        isKnockedBack = true;

        // 1. Reset velocity so the knockback feels consistent
        rb.linearVelocity = Vector2.zero;

        // 2. Apply the exact force vector from the enemy
        rb.AddForce(forceVector, ForceMode2D.Impulse);

        // 3. Visual Feedback (Flash Red)
        Color originalColor = Color.white;
        if (sr != null)
        {
            originalColor = sr.color;
            sr.color = Color.red;
        }

        // 4. Wait for Stun time (0.2s)
        yield return new WaitForSeconds(0.2f);

        // 5. Reset
        if (sr != null) sr.color = originalColor;
        isKnockedBack = false;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null) { Gizmos.color = Color.blue; Gizmos.DrawWireCube(groundCheck.position, groundCheckSize); }
        if (wallCheck != null) { Gizmos.color = Color.red; Gizmos.DrawWireCube(wallCheck.position, wallCheckSize); }
    }
}