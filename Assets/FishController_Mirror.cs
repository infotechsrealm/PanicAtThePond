using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class FishController_Mirror : NetworkBehaviour
{
    public static FishController_Mirror Instance;

    [Header("Input System")]
    public InputActionReference moveAction;

    public FishController fishController;

    private void Awake()
    {
        Instance = this;
    }

    public void Destroy_Mirror(GameObject target)
    {
        if (NetworkServer.active)
        {
            NetworkServer.Destroy(target);
        }
        else if (NetworkClient.active)
        {
            CmdRequestDestroy(target);
        }
    }

    [Command]
    void CmdRequestDestroy(GameObject target)
    {
        NetworkServer.Destroy(target);
    }

    public GameObject fishermanPrefab;

    public void RequestSpawnFisherman()
    {
        if (isLocalPlayer)
        {
            CmdSpawnFishermanOnServer();
        }
        else
        {
            Vector3 spawnPos = new Vector3(0f, 3.15f, 0f);
            GameObject fisherman = Instantiate(fishermanPrefab, spawnPos, Quaternion.identity);
           /* if(!fishController.mirrorIdentity.isLocalPlayer)
            {
                fisherman.GetComponent<FishermanController>().networkTransformUnreliable.syncDirection = SyncDirection.ServerToClient;
            }*/
            NetworkServer.Spawn(fisherman, connectionToClient); // 🔹 gives authority to caller client
        }
    }

    [Command]
    void CmdSpawnFishermanOnServer()
    {
        Vector3 spawnPos = new Vector3(0f, 3.15f, 0f);
        GameObject fisherman = Instantiate(fishermanPrefab, spawnPos, Quaternion.identity);
      /*  if (!fishController.mirrorIdentity.isLocalPlayer)
        {
            fisherman.GetComponent<FishermanController>().networkTransformUnreliable.syncDirection = SyncDirection.ServerToClient;
        }*/
        NetworkServer.Spawn(fisherman, connectionToClient); // 🔹 gives authority to caller client
    }


    public void TryPickupJunk()
    {
        NetworkIdentity junkIdentity = fishController.carriedJunk.GetComponent<NetworkIdentity>();
        if (junkIdentity != null)
        {
            CmdRequestSetJunkInFish(junkIdentity.netId);
        }
    }

    [Command]
    void CmdRequestSetJunkInFish(uint junkNetId)
    {
        if (NetworkServer.spawned.TryGetValue(junkNetId, out NetworkIdentity junkIdentity))
        {
            GameObject junk = junkIdentity.gameObject;

            // Server side parenting
            junk.GetComponent<PolygonCollider2D>().enabled = false;
            junk.transform.SetParent(fishController.junkHolder);
            junk.transform.localPosition = Vector3.zero;

            // 🟢 Notify this client to update local visuals
            TargetSetJunkInFish(connectionToClient, junkNetId);
        }
    }

    [TargetRpc]
    public void TargetSetJunkInFish(NetworkConnection target, uint netId)
    {
        if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity identity))
        {
            GameObject junk = identity.gameObject;

            fishController.carriedJunk = junk;
            fishController.carriedJunk.GetComponent<PolygonCollider2D>().enabled = false;
            fishController.carriedJunk.transform.SetParent(fishController.junkHolder);
            fishController.carriedJunk.transform.localPosition = Vector3.zero;
        }
    }

}
