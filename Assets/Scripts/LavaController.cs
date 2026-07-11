using UnityEngine;

public class LavaController : MonoBehaviour
{
    [Header("Lava Settings")]
    public float riseSpeed = 1.5f;
    public bool isRising = true;

    private void Update()
    {
        if (isRising)
        {
            transform.Translate(Vector3.up * riseSpeed * Time.deltaTime);
        }
    }
}
