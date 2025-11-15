using Mirror;
using UnityEngine;

public class Hook_Mirror : NetworkBehaviour
{

    public Hook hook;

    private void Awake()
    {
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("=== SpawnHook CALLED ===");
        Debug.Log("isServer: " + isServer);
        Debug.Log("isClient: " + isClient);
        Debug.Log("isLocalPlayer: " + isLocalPlayer);
        Debug.Log("connectionToClient: " + connectionToClient);
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void ClientAndServerActions(NetworkIdentity ObjIdentity)
    {
        if (ObjIdentity != null)
        {
            ClientActionCall(ObjIdentity.netId);
        }
    }

    [Command]
    void ClientActionCall(uint ObjNetID)
    {
        Debug.Log("ClientActionCall Called");
        if (NetworkServer.spawned.TryGetValue(ObjNetID, out NetworkIdentity ObjIdentity))
        {

            GameObject obj = ObjIdentity.gameObject;

            Transform worm = obj.transform;
            worm.SetParent(hook.wormParent.transform, false);
            worm.localPosition = Vector3.zero;
            PolygonCollider2D col = worm.GetComponent<PolygonCollider2D>();

            if (col != null)
                col.enabled = false;

            hook.hasWorm = true;
            ServerActionCall(ObjNetID);
        }
    }

    [ClientRpc]
    void ServerActionCall(uint ObjNetID)
    {
        Debug.Log("ServerActionCall Called");

        if (NetworkClient.spawned.TryGetValue(ObjNetID, out NetworkIdentity ObjIdentity))
        {
            GameObject obj = ObjIdentity.gameObject;
            Transform worm = obj.transform;
            worm.SetParent(hook.wormParent.transform, false);
            worm.localPosition = Vector3.zero;
            PolygonCollider2D col = worm.GetComponent<PolygonCollider2D>();

            if (col != null)
                col.enabled = false;

            hook.hasWorm = true;

        }
    }

}
