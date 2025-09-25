

using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GameScaler : MonoBehaviour
{
    void Start()
    {
        ScaleBackground();
    }

    void ScaleBackground()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        // Camera ke height aur width
        float screenHeight = Camera.main.orthographicSize * 2f;
        float screenWidth = screenHeight * Screen.width / Screen.height;

        // Sprite ke size
        float spriteHeight = sr.sprite.bounds.size.y;
        float spriteWidth = sr.sprite.bounds.size.x;

        // Scale calculate karna
        Vector3 scale = transform.localScale;
        scale.x = screenWidth / spriteWidth;
        scale.y = screenHeight / spriteHeight;

        transform.localScale = scale;
    }
}

