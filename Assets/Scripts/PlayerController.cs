using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    public float invincibilityDuration = 1f;
    
    private int currentHealth;
    private float invincibilityTimer;
    private Vector3 initialSpawnPoint;
    private bool isDead = false;
    private bool isFinished = false;

    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float acceleration = 40f;
    public float deceleration = 40f;
    
    [Header("Jump Settings")]
    public float jumpForce = 22f; 
    [Range(0f, 1f)]
    public float jumpCutMultiplier = 0.5f;
    public float maxFallSpeed = 25f;
    
    [Header("Jump Assists")]
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.1f;

    [Header("Physics Settings")]
    public float gravityScale = 3.5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Controls controls;
    
    private float horizontalInput;
    private bool isGrounded;
    
    public bool isLookingUp { get; private set; }
    public bool isLookingDown { get; private set; }
    public float facingDirection { get; private set; } = 1f;
    public float currentVelocityY { get { return rb.linearVelocity.y; } }
    
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    private void Awake()
    {
        currentHealth = maxHealth;
        initialSpawnPoint = transform.position;
        
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        
        controls = new Controls();

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
        if (isDead) return;

        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= Time.deltaTime;
        }

        if (isFinished)
        {
            horizontalInput = 0;
            isLookingUp = false;
            isLookingDown = false;
        }
        else
        {
            float right = controls.Player.m_right.ReadValue<float>();
            float left = controls.Player.m_left.ReadValue<float>();
            horizontalInput = right - left;

            jumpBufferCounter -= Time.deltaTime;
            if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
            {
                ExecuteJump();
            }

            isLookingUp = controls.Player.Look_up.IsPressed();
            isLookingDown = controls.Player.Look_Down.IsPressed();
        }

        CheckGrounded();
        
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        Move();
        
        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }

    private void Move()
    {
        float targetSpeed = horizontalInput * moveSpeed;
        float accelRate = (Mathf.Abs(horizontalInput) > 0.01f) ? acceleration : deceleration;
        
        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
        
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);

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
        if (isDead || isFinished) return;
        jumpBufferCounter = jumpBufferTime;
    }

    private void OnJumpReleased()
    {
        if (isDead || isFinished) return;
        if (rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            coyoteTimeCounter = 0f;
        }
    }

    private void ExecuteJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
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
        if (isDead) return;
        if (collision.gameObject.CompareTag("Finish") || collision.gameObject.layer == LayerMask.NameToLayer("Finish")) { WinLevel(); return; }
        if (collision.gameObject.layer == LayerMask.NameToLayer("Lava")) { SinkInLava(); return; }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            TakeDamage(1);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Border"))
        {
            TakeDamage(2);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;
        if (collision.gameObject.CompareTag("Finish") || collision.gameObject.layer == LayerMask.NameToLayer("Finish")) { WinLevel(); return; }
        if (collision.gameObject.layer == LayerMask.NameToLayer("Lava")) { SinkInLava(); return; }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            TakeDamage(1);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Border"))
        {
            TakeDamage(2);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;
        if (collision.gameObject.CompareTag("Finish") || collision.gameObject.layer == LayerMask.NameToLayer("Finish")) { WinLevel(); return; }
        if (collision.gameObject.layer == LayerMask.NameToLayer("Lava")) { SinkInLava(); return; }

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
        if (isDead) return;
        if (collision.gameObject.CompareTag("Finish") || collision.gameObject.layer == LayerMask.NameToLayer("Finish")) { WinLevel(); return; }
        if (collision.gameObject.layer == LayerMask.NameToLayer("Lava")) { SinkInLava(); return; }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            TakeDamage(1);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Border"))
        {
            TakeDamage(2);
        }
    }

    private void WinLevel()
    {
        if (isDead || isFinished) return;
        isFinished = true;
        
        LavaController lava = FindAnyObjectByType<LavaController>();
        if (lava != null)
        {
            lava.isRising = false;
        }
    }

    private void SinkInLava()
    {
        if (isDead) return;
        isDead = true;
        
        CameraController camController = Camera.main.GetComponent<CameraController>();
        if (camController != null)
        {
            camController.target = null;
        }

        Invoke("Die", 0.5f);
    }

    private void TakeDamage(int damageAmount)
    {
        if (invincibilityTimer > 0) return;

        currentHealth -= damageAmount;
        Debug.Log("Player took " + damageAmount + " damage! Current Health: " + currentHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            invincibilityTimer = invincibilityDuration;
            rb.linearVelocity = new Vector2(-facingDirection * 10f, 10f);
        }
    }

    private void Die()
    {
        Debug.Log("Player Died!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
