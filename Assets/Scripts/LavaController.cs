using UnityEngine;

public class LavaController : MonoBehaviour
{
    [Header("Lava Settings")]
    public float riseSpeed = 1.5f; // How fast the lava moves upwards per second
    public bool isRising = true;

    private void Update()
    {
        if (isRising)
        {
            // Move the lava up along the Y axis over time
            transform.Translate(Vector3.up * riseSpeed * Time.deltaTime);
        }
    }
}
