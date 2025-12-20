using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GameScaler : MonoBehaviour
{
    void Start()
    {
        With();
        Height();
    }

   

    void With()
    {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;

            // Sprite size
            float spriteWidth = sr.bounds.size.x;

            // Camera width (world space me)
            float worldHeight = Camera.main.orthographicSize * 2f;
            float worldWidth = worldHeight * Screen.width / Screen.height;

            // Scale for width
            float scale = worldWidth / spriteWidth;

            transform.localScale = new Vector3(scale, scale, 1);
    }

    void Height()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        // Sprite size
        float spriteHeight = sr.bounds.size.y;

        // Camera height (world units)
        float worldHeight = Camera.main.orthographicSize * 2f;

        // Scale for height
        float scale = worldHeight / spriteHeight;

        transform.localScale = new Vector3(scale, scale, 1);
    }
}

