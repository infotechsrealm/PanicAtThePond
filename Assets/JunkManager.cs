using Photon.Pun;
using UnityEngine;

public class JunkManager : MonoBehaviourPunCallbacks
{
    public Rigidbody2D rb;

    bool isFreezed = false;
    public AudioSource audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!PhotonNetwork.IsMasterClient)
        {
            audioSource.Play();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (PhotonNetwork.IsMasterClient && !isFreezed)
        {
            Debug.Log("Transform position" + transform.position.y);
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
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
        isFreezed = true;
        rb.isKinematic = true;
    }
}
