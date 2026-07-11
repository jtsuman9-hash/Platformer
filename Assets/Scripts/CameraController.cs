using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target;
    private PlayerController playerController;

    [Header("Boundaries (Edges)")]
    public Transform leftEdge;
    public Transform rightEdge;

    [Header("Framing")]
    public float verticalOffset = 1.0f; // Pushes camera up so the player sits in the lower part of the screen
    public float verticalDeadzone = 2f; // How far the player can move up/down before the camera follows

    [Header("Damping")]
    public float smoothTime = 0.15f; // How "laggy" or smooth the camera is

    [Header("Look Ahead (Horizontal)")]
    public float lookAheadDistance = 2f;
    public float lookAheadSpeed = 3f;

    [Header("Look Up/Down (Vertical)")]
    public float lookUpDistance = 3f;
    public float lookDownDistance = 3f;
    public float lookDelay = 0.5f; // How long to hold before looking
    public float fallLookDownThreshold = -12f; // Velocity at which camera automatically pans down

    private Vector3 currentVelocity;
    
    private float targetLookAhead;
    private float currentLookAhead;

    private float lookTimer;
    private float targetVerticalOffset;
    private float currentVerticalOffset;
    
    private float currentFocusY;
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();

        // Try to automatically find the Player if a target is assigned
        if (target != null)
        {
            playerController = target.GetComponent<PlayerController>();
            currentFocusY = target.position.y;
        }
    }

    private void LateUpdate()
    {
        if (target == null || playerController == null) return;

        // 1. Horizontal Look Ahead
        // Determine where we should look ahead based on which way the player is facing
        targetLookAhead = playerController.facingDirection * lookAheadDistance;
        
        // Smoothly transition to the horizontal look ahead distance
        currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, Time.deltaTime * lookAheadSpeed);

        // 2. Vertical Look Up/Down
        if (playerController.isLookingUp)
        {
            lookTimer += Time.deltaTime;
            if (lookTimer >= lookDelay)
            {
                targetVerticalOffset = lookUpDistance;
            }
        }
        else if (playerController.isLookingDown)
        {
            lookTimer += Time.deltaTime;
            if (lookTimer >= lookDelay)
            {
                targetVerticalOffset = -lookDownDistance;
            }
        }
        else if (playerController.currentVelocityY < fallLookDownThreshold)
        {
            // Automatically pan the camera down if the player is falling fast
            targetVerticalOffset = -lookDownDistance;
            lookTimer = 0f;
        }
        else
        {
            // Reset look timer and offset if not holding up/down and not falling
            lookTimer = 0f;
            targetVerticalOffset = 0f;
        }

        // Smoothly transition vertical offset
        currentVerticalOffset = Mathf.Lerp(currentVerticalOffset, targetVerticalOffset, Time.deltaTime * lookAheadSpeed);

        // 3. Vertical Deadzone (Ignores small jumps)
        float distanceY = target.position.y - currentFocusY;
        if (distanceY > verticalDeadzone)
        {
            currentFocusY += distanceY - verticalDeadzone;
        }
        else if (distanceY < -verticalDeadzone)
        {
            currentFocusY += distanceY + verticalDeadzone;
        }

        // 4. Calculate the desired camera position
        Vector3 desiredPosition = new Vector3(
            target.position.x + currentLookAhead,
            currentFocusY + verticalOffset + currentVerticalOffset,
            transform.position.z // Keep the camera's original Z depth (usually -10)
        );

        // 5. Smoothly glide the camera to the desired position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);

        // 6. Clamp to Edges (if assigned)
        if (leftEdge != null && rightEdge != null && cam != null)
        {
            // Calculate camera's width based on current screen size
            float camHalfHeight = cam.orthographicSize;
            float camHalfWidth = cam.aspect * camHalfHeight;

            // Prevent the camera's center from going past the edge minus half the camera's width
            float clampX = Mathf.Clamp(transform.position.x, leftEdge.position.x + camHalfWidth, rightEdge.position.x - camHalfWidth);
            
            // Apply the clamp
            transform.position = new Vector3(clampX, transform.position.y, transform.position.z);
        }
    }
}
