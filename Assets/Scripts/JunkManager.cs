using Photon.Pun;
using System.Collections;
using UnityEngine;

public class JunkManager : MonoBehaviourPunCallbacks
{
    public Rigidbody2D rb;

    bool isFreezed = false;
    public AudioSource audioSource;
    internal bool inWater = false;
    public GameObject waterDrop;
    public PhotonRigidbody2DView photonRigidbody2DView;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!PhotonNetwork.IsMasterClient)
        {
            GS.Instance.SetVolume(audioSource);
            audioSource.Play();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (PhotonNetwork.IsMasterClient && !isFreezed)
        {
            if (transform.position.y < -4f)
            {
                CallFreezeObjectRPC();
            }
        }
    }

    public void CallFreezeObjectRPC()
    {
        photonView.RPC(nameof(FreezeObject), RpcTarget.AllBuffered);
    }

    [PunRPC]
    void FreezeObject()
    {
        if (transform.position.y < -4f)
        {
            transform.position = new Vector2(transform.position.x, -4f);
        }
        GetComponent<PolygonCollider2D>().enabled = true;
        photonRigidbody2DView.enabled = false;
        isFreezed = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic; // stops all physics
    }

    private void OnTriggerEnter2D(Collider2D collision) 
    {
        Debug.Log("collision  = "+collision.gameObject.tag);
        if(collision.gameObject.tag == "Water")
        {
            if (!inWater)
            {
                Vector2 hitPos = transform.position;
                Instantiate(waterDrop, hitPos, Quaternion.identity);

                inWater = true;
                StartCoroutine(ReduceGravity());
            }
        }
    }

    IEnumerator ReduceGravity()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        // Continue reducing until gravity is near 0.2
        while (rb.gravityScale > 0.2f)
        {
            // 🪶 Slowly reduce gravity
            rb.gravityScale -= 0.2f;

            // 🌊 Smoothly reduce velocity (simulate thick water drag)
            rb.linearVelocity *= 0.8f;  // 0.8 means it loses 20% speed each step

            yield return new WaitForSeconds(0.05f);
        }

        // Final fine-tune
        rb.gravityScale = 0.01f;
        rb.linearVelocity *= 0.5f; // slow a bit more at the end
    }

    public void LeaveByFish()
    {
        GetComponent<PolygonCollider2D>().enabled = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        transform.SetParent(null);
        rb.gravityScale = 1f;
        StartCoroutine(ReduceGravity());
        isFreezed = false;
        photonRigidbody2DView.enabled = true;
    }
}