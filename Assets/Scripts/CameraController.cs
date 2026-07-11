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
    public float verticalOffset = 1.0f;
    public float verticalDeadzone = 2f;

    [Header("Damping")]
    public float smoothTime = 0.15f;

    [Header("Look Ahead (Horizontal)")]
    public float lookAheadDistance = 2f;
    public float lookAheadSpeed = 3f;

    [Header("Look Up/Down (Vertical)")]
    public float lookUpDistance = 3f;
    public float lookDownDistance = 3f;
    public float lookDelay = 0.5f;
    public float fallLookDownThreshold = -12f;

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

        if (target != null)
        {
            playerController = target.GetComponent<PlayerController>();
            currentFocusY = target.position.y;
        }
    }

    private void LateUpdate()
    {
        if (target == null || playerController == null) return;

        targetLookAhead = playerController.facingDirection * lookAheadDistance;
        
        currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, Time.deltaTime * lookAheadSpeed);

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
            targetVerticalOffset = -lookDownDistance;
            lookTimer = 0f;
        }
        else
        {
            lookTimer = 0f;
            targetVerticalOffset = 0f;
        }

        currentVerticalOffset = Mathf.Lerp(currentVerticalOffset, targetVerticalOffset, Time.deltaTime * lookAheadSpeed);

        float distanceY = target.position.y - currentFocusY;
        if (distanceY > verticalDeadzone)
        {
            currentFocusY += distanceY - verticalDeadzone;
        }
        else if (distanceY < -verticalDeadzone)
        {
            currentFocusY += distanceY + verticalDeadzone;
        }

        Vector3 desiredPosition = new Vector3(
            target.position.x + currentLookAhead,
            currentFocusY + verticalOffset + currentVerticalOffset,
            transform.position.z
        );

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);

        if (leftEdge != null && rightEdge != null && cam != null)
        {
            float camHalfHeight = cam.orthographicSize;
            float camHalfWidth = cam.aspect * camHalfHeight;

            float clampX = Mathf.Clamp(transform.position.x, leftEdge.position.x + camHalfWidth, rightEdge.position.x - camHalfWidth);
            
            transform.position = new Vector3(clampX, transform.position.y, transform.position.z);
        }
    }
}
