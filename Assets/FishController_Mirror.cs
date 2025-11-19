using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class FishController_Mirror : NetworkBehaviour
{
    [Header("Input System")]
    public InputActionReference moveAction;

    public FishController fishController;


    public GameObject wormPrefab;
    private void Start()
    {
        Debug.Log("=== FishController_Mirror CALLED ===");
        Debug.Log("isServer: " + isServer);
        Debug.Log("isClient: " + isClient);
        Debug.Log("isLocalPlayer: " + isLocalPlayer);
        Debug.Log("connectionToClient: " + connectionToClient);
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


    //generate FisherMan
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
            NetworkServer.Spawn(fisherman, connectionToClient); // 🔹 gives authority to caller client
            SpawnWorm(3);
        }
    }

    [Command]
    void CmdSpawnFishermanOnServer()
    {
        Vector3 spawnPos = new Vector3(0f, 3.15f, 0f);
        GameObject fisherman = Instantiate(fishermanPrefab, spawnPos, Quaternion.identity);
        NetworkServer.Spawn(fisherman, connectionToClient); // 🔹 gives authority to caller client
        SpawnWorm(3);
    }

    //Catch Junk in the Fish Mouth
    public void TryPickupJunk(NetworkIdentity junkIdentity)
    {
        if (junkIdentity != null)
        {
            CmdPickupJunk(junkIdentity.netId);
        }
    }

    [Command]
    void CmdPickupJunk(uint junkNetId)
    {
        Debug.Log("CmdPickupJunk called ");

        if (NetworkServer.spawned.TryGetValue(junkNetId, out NetworkIdentity junkIdentity))
        {
            GameObject junk = junkIdentity.gameObject;

            junk.GetComponent<PolygonCollider2D>().enabled = false;
            junk.transform.SetParent(fishController.junkHolder);
            junk.transform.localPosition = Vector3.zero;
            junk.GetComponent<JunkManager>().junkManager_Mirror.RequestFreezeObject();

            RpcPickupJunk(junkNetId);
        }
    }


    [ClientRpc]
    void RpcPickupJunk(uint junkNetId)
    {
        Debug.Log("RpcPickupJunk called ");
        if (NetworkClient.spawned.TryGetValue(junkNetId, out NetworkIdentity identity))
        {
            GameObject junk = identity.gameObject;

            junk.GetComponent<PolygonCollider2D>().enabled = false;
            junk.transform.SetParent(fishController.junkHolder);
            junk.transform.localPosition = Vector3.zero;
        }
    }

    //Leave Junk
    public void TryLeaveJunk(NetworkIdentity junkIdentity)
    {
        if (junkIdentity != null)
        {
            CmdLeaveJunk(junkIdentity.netId);
        }
    }

    [Command]
    void CmdLeaveJunk(uint junkNetId)
    {
        if (NetworkServer.spawned.TryGetValue(junkNetId, out NetworkIdentity junkIdentity))
        {
            GameObject junk = junkIdentity.gameObject;

            junk.GetComponent<JunkManager>().LeaveByFish();

            RpcLeaveJunk(junkNetId);
        }
    }

    [ClientRpc]
    void RpcLeaveJunk(uint junkNetId)
    {

        if (NetworkClient.spawned.TryGetValue(junkNetId, out NetworkIdentity identity))
        {
            GameObject junk = identity.gameObject;

            junk.GetComponent<JunkManager>().LeaveByFish();

        }
    }

    //winFish
    public void TryWinFish()
    {
        Debug.Log("TryWinFish called");

        if (isLocalPlayer)
        {
            CmdWinFish();
        }
    }

   [Command]
    void CmdWinFish()
    {
        Debug.Log(" [Command] CmdWinFish called in server  ");
            RpcWinFish();
    }


    [ClientRpc]
    void RpcWinFish()
    {
        Debug.Log("  [ClientRpc] RpcWinFish called in remote player");

        if (!fishController.isFisherMan)
        {
            for (int i = 0; i < GameManager.Instance.allFishes.Count; i++)
            {
                if (GameManager.Instance.allFishes[i].transform.localScale != Vector3.zero)
                {
                    GameManager.Instance.allFishes[i].WinFish_mirror();
                }
            }
        }
    }


    public void LessCounter()
    {
        Debug.Log("LessCounter called");
        if (isLocalPlayer)
        {
            CmdLessCounter();
        }
    }

    [Command]
    public void CmdLessCounter()
    {
        Debug.Log("CmdLessCounter called");

        ClientCmdLessCounter();
    }

    [ClientRpc]
    public void ClientCmdLessCounter()
    {
        Debug.Log("ClientCmdLessCounter called");

        if (GameManager.Instance.isFisherMan)
        {
            GameManager.Instance.LessPlayerCount_Mirror();
        }
    }


    public void SpawnWorm(int length)
    {
        Debug.Log("=== FishermanController_Mirror CALLED ===");
        Debug.Log("isServer: " + isServer);
        Debug.Log("isClient: " + isClient);
        Debug.Log("isLocalPlayer: " + isLocalPlayer);

        if (isServer)
        {
            for (int i = 0; i < length; i++)
            {
                GameObject worm = Instantiate(wormPrefab, Vector3.zero, Quaternion.identity);
                NetworkServer.Spawn(worm, connectionToClient); // 🔹 gives authority to caller client
                worm.transform.position = Vector3.zero;
            }
        }
    }
}
