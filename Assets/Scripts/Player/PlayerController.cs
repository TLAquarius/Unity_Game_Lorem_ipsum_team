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

    // --- NEW: ANIMATOR REFERENCE ---
    private Animator anim;
    private Rigidbody2D rb;
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
        // Get the Animator you just added
        anim = GetComponent<Animator>();

        playerLayer = LayerMask.NameToLayer("Player");
        enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    void Update()
    {
        if (isDashing) return;

        if (!isWallJumping) horizontalInput = Input.GetAxisRaw("Horizontal");

        if (horizontalInput > 0) facingDirection = 1;
        else if (horizontalInput < 0) facingDirection = -1;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isTouchingWall && !isGrounded && canWallJumpUnlocked) StartCoroutine(WallJumpRoutine());
            else if (isGrounded) Jump();
            else if (canDoubleJumpUnlocked && doubleJumpAvailable)
            {
                Jump();
                doubleJumpAvailable = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash) StartCoroutine(DashRoutine());

        CheckWallSlide();

        // --- NEW: UPDATE ANIMATIONS ---
        UpdateAnimations();
    }

    // --- NEW FUNCTION ---
    void UpdateAnimations()
    {
        if (anim == null) return;

        // 1. Run vs Idle (HeroKnight uses 0 for Idle, 1 for Run)
        if (Mathf.Abs(horizontalInput) > 0.1f)
            anim.SetInteger("AnimState", 1);
        else
            anim.SetInteger("AnimState", 0);

        // 2. Air Physics
        anim.SetBool("Grounded", isGrounded);
        anim.SetFloat("AirSpeedY", rb.linearVelocity.y);

        // 3. Wall Slide
        anim.SetBool("WallSlide", isWallSliding);
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        isTouchingWall = Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0f, wallLayer);

        if ((isGrounded || isWallSliding) && !Input.GetKey(KeyCode.Space))
        {
            doubleJumpAvailable = true;
            isWallJumping = false;
        }

        if (!isWallJumping && !isWallSliding)
        {
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }

        if (!isWallJumping && horizontalInput != 0)
        {
            transform.localScale = new Vector3(horizontalInput, 1, 1);
        }

        ApplyBetterGravity();
    }

    void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0) rb.gravityScale = fallMultiplier;
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(KeyCode.Space) && !isWallJumping) rb.gravityScale = lowJumpMultiplier;
        else rb.gravityScale = 1f;
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // Trigger Jump Anim
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
        // Check if we have a prefab and if we are actually on the wall
        if (wallSlideDustPrefab != null && wallCheck != null)
        {
            // Spawn the dust at the wallCheck position (where player touches wall)
            GameObject dust = Instantiate(wallSlideDustPrefab, wallCheck.position, Quaternion.identity);

            // Optional: Flip dust if facing left/right so it blows away from wall
            // (Assuming standard particle rotation)
            if (transform.localScale.x < 0)
            {
                // If facing left, rotate dust to face left
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

        // Trigger Jump Anim (Some packs reuse Jump for WallJump)
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

        // Trigger Roll Anim
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

    // ... OnDrawGizmos remains the same ...
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null) { Gizmos.color = Color.blue; Gizmos.DrawWireCube(groundCheck.position, groundCheckSize); }
        if (wallCheck != null) { Gizmos.color = Color.red; Gizmos.DrawWireCube(wallCheck.position, wallCheckSize); }
    }
}