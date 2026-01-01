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
    private GameInput input;
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
        anim = GetComponent<Animator>();
        input = GetComponent<GameInput>();

        playerLayer = LayerMask.NameToLayer("Player");
        enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    void Update()
    {
        if (isDashing) return;

        // 1. INPUT
        if (!isWallJumping)
        {
            Vector2 move = input.GetMovementInput();
            horizontalInput = move.x;
        }

        // 2. FLIP CHARACTER INSTANTLY (Moved from FixedUpdate)
        // This moves the 'WallCheck' object immediately so we don't detect the wall behind us
        if (!isWallJumping && horizontalInput != 0)
        {
            facingDirection = (horizontalInput > 0) ? 1 : -1;
            transform.localScale = new Vector3(facingDirection, 1, 1);
        }

        // 3. UPDATE CHECKS INSTANTLY
        // Perform these checks here so animation doesn't lag behind physics
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        isTouchingWall = Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0f, wallLayer);

        // 4. JUMP LOGIC
        if (input.IsJumpPressed())
        {
            if (isTouchingWall && !isGrounded && canWallJumpUnlocked) StartCoroutine(WallJumpRoutine());
            else if (isGrounded) Jump();
            else if (canDoubleJumpUnlocked && doubleJumpAvailable)
            {
                Jump();
                doubleJumpAvailable = false;
            }
        }

        if (input.IsDashPressed() && canDash)
            StartCoroutine(DashRoutine());

        // 5. SLIDE LOGIC
        CheckWallSlide();

        // 6. ANIMATIONS
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
        if (isDashing) return;

        // Note: isGrounded/isTouchingWall checks moved to Update for responsiveness
        // Reset Double Jump
        if ((isGrounded || isWallSliding) && !input.IsJumpHeld())
        {
            doubleJumpAvailable = true;
            isWallJumping = false;
        }

        // Apply Movement
        if (!isWallJumping && !isWallSliding)
        {
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }

        // Removed Transform Flip from here to avoid the lag

        ApplyBetterGravity();
    }

    void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0) rb.gravityScale = fallMultiplier;
        else if (rb.linearVelocity.y > 0 && !input.IsJumpHeld() && !isWallJumping) rb.gravityScale = lowJumpMultiplier;
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
            // Now that 'facingDirection' updates instantly in Update(),
            // if you press AWAY from the wall, facingDirection flips, 
            // wallCheck moves away, isTouchingWall becomes false instantly.
            // But if we are still technically touching it, this logic ensures we must be pushing INTO it.
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

        // Force flip immediately for the jump visual
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

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null) { Gizmos.color = Color.blue; Gizmos.DrawWireCube(groundCheck.position, groundCheckSize); }
        if (wallCheck != null) { Gizmos.color = Color.red; Gizmos.DrawWireCube(wallCheck.position, wallCheckSize); }
    }
}