using Photon.Pun;
using UnityEngine;

public class Preloader : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float speed = 200f; // Rotation speed in degrees per second
    public RectTransform leftPoint;
    public RectTransform rightPoint;

    private bool movingRight = false;
    public RectTransform rect;

    public static Preloader Instence;
    private void Awake()
    {
        Instence = this;
    }

    void Update()
    {
        if (movingRight)
        {
            rect.anchoredPosition += Vector2.right * speed * Time.deltaTime;
            if (rect.anchoredPosition.x >= rightPoint.anchoredPosition.x)
            {
                movingRight = false;
                rect.localScale = new Vector3(1, 1, 1); // flip fish
            }
        }
        else
        {
            rect.anchoredPosition += Vector2.left * speed * Time.deltaTime;
            if (rect.anchoredPosition.x <= leftPoint.anchoredPosition.x)
            {
                movingRight = true;
                rect.localScale = new Vector3(-1, 1, 1); // face right
            }
        }
    }

}