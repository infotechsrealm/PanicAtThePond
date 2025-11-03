using UnityEngine;
using Mirror;

public class PlayerMove : NetworkBehaviour
{
    public float speed = 5f;

    void Update()
    {
        if (!isLocalPlayer) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        transform.Translate(new Vector3(h, 0, v) * speed * Time.deltaTime);
    }
}
