using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    public float invincibilityDuration = 1f; // i-frames
    
    private int currentHealth;
    private float invincibilityTimer;
    private Vector3 initialSpawnPoint;

    [Header("Movement Settings")]
    public float moveSpeed = 8f; // Movement in Hollow Knight is fast and responsive
    public float acceleration = 40f; // How fast the player reaches top speed
    public float deceleration = 40f; // How fast the player stops when releasing the key
    
    [Header("Jump Settings")]
    public float jumpForce = 22f; 
    [Range(0f, 1f)]
    public float jumpCutMultiplier = 0.5f; // Cuts upward velocity by half when jump button is released (Variable Jump Height)
    public float maxFallSpeed = 25f; // Limits terminal velocity when falling for a long time
    
    [Header("Jump Assists (The Secret Sauce)")]
    public float coyoteTime = 0.15f; // Grace period to jump after falling off a ledge
    public float jumpBufferTime = 0.1f; // Grace period to queue a jump right before hitting the ground

    [Header("Physics Settings")]
    public float gravityScale = 3.5f; // High gravity is crucial for that snappy Hollow Knight feel

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Controls controls;
    
    private float horizontalInput;
    private bool isGrounded;
    
    // Properties for CameraController to read
    public bool isLookingUp { get; private set; }
    public bool isLookingDown { get; private set; }
    public float facingDirection { get; private set; } = 1f;
    public float currentVelocityY { get { return rb.linearVelocity.y; } }
    
    // Timers for the jump assists
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    private void Awake()
    {
        currentHealth = maxHealth;
        initialSpawnPoint = transform.position; // Store exactly where the player was placed in the editor
        
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale; // Enforce the snappy gravity
        
        // Initialize the auto-generated Controls class
        controls = new Controls();

        // Register input events
        controls.Player.Jump.performed += ctx => OnJumpPressed();
        controls.Player.Jump.canceled += ctx => OnJumpReleased();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Update()
    {
        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= Time.deltaTime;
        }

        // 1. Read movement inputs (1 if pressed, 0 if not)
        float right = controls.Player.m_right.ReadValue<float>();
        float left = controls.Player.m_left.ReadValue<float>();
        horizontalInput = right - left;

        // 2. Ground Check & Coyote Time
        CheckGrounded();
        
        if (isGrounded)
        {
            // Reset Coyote timer when on the ground
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            // Tick down Coyote timer when falling
            coyoteTimeCounter -= Time.deltaTime;
        }

        // 3. Jump Buffering Timer
        jumpBufferCounter -= Time.deltaTime;

        // 4. Try Executing the Jump
        // If we queued a jump recently AND we are still eligible to jump
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            ExecuteJump();
        }

        // Look Up/Down logic (now exposes state for the Camera Controller)
        isLookingUp = controls.Player.Look_up.IsPressed();
        isLookingDown = controls.Player.Look_Down.IsPressed();
    }

    private void FixedUpdate()
    {
        // Apply Physics-based movement
        Move();
        
        // Clamp maximum fall speed (prevent falling infinitely fast)
        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }

    private void Move()
    {
        // Smoothly accelerate and decelerate the player's horizontal movement
        float targetSpeed = horizontalInput * moveSpeed;
        float accelRate = (Mathf.Abs(horizontalInput) > 0.01f) ? acceleration : deceleration;
        
        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
        
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);

        // Flip the player's sprite to face the moving direction
        if (horizontalInput > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
            facingDirection = 1f;
        }
        else if (horizontalInput < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
            facingDirection = -1f;
        }
    }

    private void OnJumpPressed()
    {
        // We don't jump immediately here. Instead, we give a generous "buffer" window.
        // This makes it so if the player presses jump a few frames before hitting the floor, it still works.
        jumpBufferCounter = jumpBufferTime;
    }

    private void OnJumpReleased()
    {
        // Variable jump height: kill upward velocity if we let go of jump early
        if (rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            
            // Instantly consume coyote time so the player can't double-jump exploit
            coyoteTimeCounter = 0f;
        }
    }

    private void ExecuteJump()
    {
        // Reset vertical velocity before jumping for consistent jump heights
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        
        // Apply the jump force
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // Consume both counters so we don't accidentally jump again instantly
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
    }

    private void CheckGrounded()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Lava")) { Die(); return; }

        // Check if the collided object is on the "Obstacle" layer
        if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            TakeDamage(1);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Border"))
        {
            TakeDamage(2);
        }
    }

    // Re-apply damage if still touching after i-frames expire
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Lava")) { Die(); return; }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            TakeDamage(1);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Border"))
        {
            TakeDamage(2);
        }
    }

    // Checking triggers too, just in case the user made the obstacles triggers
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Lava")) { Die(); return; }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            TakeDamage(1);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Border"))
        {
            TakeDamage(2);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Lava")) { Die(); return; }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            TakeDamage(1);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Border"))
        {
            TakeDamage(2);
        }
    }

    private void TakeDamage(int damageAmount)
    {
        if (invincibilityTimer > 0) return; // Ignore damage if invincible

        currentHealth -= damageAmount;
        Debug.Log("Player took " + damageAmount + " damage! Current Health: " + currentHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Give i-frames
            invincibilityTimer = invincibilityDuration;
            
            // Pop the player back slightly like in Hollow Knight
            rb.linearVelocity = new Vector2(-facingDirection * 10f, 10f);
        }
    }

    private void Die()
    {
        Debug.Log("Player Died!");
        // Completely reload the scene to reset everything (including the rising lava)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the ground check circle in the editor
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
