using Mirror;
using Photon.Pun;
using UnityEngine;

public class JunkManager_Mirror : NetworkBehaviour
{
    public JunkManager junkManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Start()
    {
        Debug.Log("=== JunkManager_Mirror CALLED ===");
        Debug.Log("isServer: " + isServer);
        Debug.Log("isClient: " + isClient);
        Debug.Log("isLocalPlayer: " + isLocalPlayer);
        Debug.Log("connectionToClient: " + connectionToClient);
    }

    public void RequestFreezeObject()
    {
        if (GS.Instance.IsMirrorMasterClient)
        {
            if (transform.position.y < -4f)
            {
                transform.position = new Vector2(transform.position.x, -4f);
            }

            GetComponent<PolygonCollider2D>().enabled = true;
            junkManager.photonRigidbody2DView.enabled = false;
            junkManager.isFreezed = true;
            junkManager.rb.linearVelocity = Vector2.zero;
            junkManager.rb.gravityScale = 0f;
            junkManager.rb.bodyType = RigidbodyType2D.Kinematic; // stops all physics


            FreezeObject();
        }
    }


    [ClientRpc]
    void FreezeObject()
    {
        if (transform.position.y < -4f)
        {
            transform.position = new Vector2(transform.position.x, -4f);
        }
        GetComponent<PolygonCollider2D>().enabled = true;
        junkManager.photonRigidbody2DView.enabled = false;
        junkManager.isFreezed = true;
        junkManager.rb.linearVelocity = Vector2.zero;
        junkManager.rb.gravityScale = 0f;
        junkManager.rb.bodyType = RigidbodyType2D.Kinematic; // stops all physics
    }
}
