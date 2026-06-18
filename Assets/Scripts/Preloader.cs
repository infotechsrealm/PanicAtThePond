using Photon.Pun;
using UnityEngine;

public class Preloader : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float speed = 200f;
    public RectTransform leftPoint;
    public RectTransform rightPoint;

    private bool movingRight = false;
    public RectTransform rect;

    public static Preloader Instence;
    private void Awake()
    {
        Instence = this;

        if (rect != null)
        {
            rect.localScale = Vector3.one;
        }
    }

    void Update()
    {
        if (rect == null || leftPoint == null || rightPoint == null)
        {
            return;
        }

        if (movingRight)
        {
            rect.anchoredPosition += Vector2.right * speed * Time.deltaTime;
            if (rect.anchoredPosition.x >= rightPoint.anchoredPosition.x)
            {
                movingRight = false;
                rect.localScale = Vector3.one;
            }
        }
        else
        {
            rect.anchoredPosition += Vector2.left * speed * Time.deltaTime;
            if (rect.anchoredPosition.x <= leftPoint.anchoredPosition.x)
            {
                movingRight = true;
                rect.localScale = new Vector3(-1f, 1f, 1f);
            }
        }
    }

}
