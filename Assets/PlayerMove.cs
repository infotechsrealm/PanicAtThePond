using UnityEngine;
using Mirror;

public class PlayerMove : NetworkBehaviour
{
    public float speed = 5f;

    void Update()
    {
        // 👇 सिर्फ local player का input allow करें
        if (!isLocalPlayer) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(h, v, 0) * speed * Time.deltaTime;
        transform.Translate(move);
    }
}
