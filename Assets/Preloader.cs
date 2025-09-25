using UnityEngine;

public class Preloader : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float speed = 200f; // Rotation speed in degrees per second

    void Update()
    {
        // Rotate continuously on Z axis
        transform.Rotate(0f, 0f, -speed * Time.deltaTime);
    }
}
