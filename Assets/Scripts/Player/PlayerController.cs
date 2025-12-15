using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Dash Settings")]
    public float dashSpeed = 15f;    // Much faster than walking
    public float dashDuration = 0.2f; // Short burst
    public float dashCooldown = 1f;   // Can't spam it
    private bool canDash = true;
    private bool isDashing = false;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private bool jumpRequest;
    private int facingDirection = 1; // 1 = Right, -1 = Left

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (isDashing) return; // Disable inputs while dashing

        // 1. Input Processing
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Track facing direction for the Dash
        if (horizontalInput > 0) facingDirection = 1;
        else if (horizontalInput < 0) facingDirection = -1;

        
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            jumpRequest = true;
        }

     
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(DashRoutine());
        }
    }

    void FixedUpdate()
    {
        if (isDashing) return; // Don't apply normal physics while dashing

        // Ground Check
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Apply Movement
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

        // Flip Character
        if (horizontalInput != 0)
            transform.localScale = new Vector3(horizontalInput, 1, 1);

        // Apply Jump
        if (jumpRequest)
        {
            rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
            jumpRequest = false;
        }
    }

    private IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;

        // Store original gravity to restore it later
        float originalGravity = rb.gravityScale;

        // Remove gravity so we dash in a straight line (even in air)
        rb.gravityScale = 0f;

        // Apply Dash Velocity
        rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0f);

        // Optional: Add a "Ghost" trail effect here later

        yield return new WaitForSeconds(dashDuration);

        // End Dash
        rb.gravityScale = originalGravity;
        isDashing = false;

        // Cooldown
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}