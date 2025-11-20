using Mirror;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FishController_Mirror : NetworkBehaviour
{
    [Header("Input System")]
    public InputActionReference moveAction;

    public FishController fishController;

    public GameObject wormPrefab;

    public List<WormManager> allHookWorms = new List<WormManager>();

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
    }

    [Command]
    void CmdSpawnFishermanOnServer()
    {
        Vector3 spawnPos = new Vector3(0f, 3.15f, 0f);
        GameObject fisherman = Instantiate(fishermanPrefab, spawnPos, Quaternion.identity);
        NetworkServer.Spawn(fisherman, connectionToClient); // 🔹 gives authority to caller client
        SpawnWorm(GameManager.Instance.fishermanWorms);
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
        Debug.Log("lengthlengthlengthlengthlengthlengthlengthlengthlengthlengthlengthlengthlengthlengthlengthlength: " + length);

        if (isServer)
        {
            Debug.Log("isServerisServerisServerisServerisServerisServerisServerisServerisServer: " + isServer);


            for (int i = 0; i < length; i++)
            {
                Debug.Log("iiiiiiiiiiiiiiiiiiiiiiiiiiiiii: " + i);

                GameObject worm = Instantiate(wormPrefab, new Vector3(0f, 10f, 0f), Quaternion.identity);
                NetworkServer.Spawn(worm, connectionToClient); // 🔹 gives authority to caller client
            }
        }
        else
        {
            Debug.Log("isServerisServerisServerisServerisServerisServerisServerisServerisServer: " + isServer);

        }
    }

    //set worm in Hook
    public GameObject SetWormInJunk(NetworkIdentity hookIdentity)
    {
        if (hookIdentity != null && allHookWorms.Count > 0)
        {
            NetworkIdentity n = allHookWorms[0].GetComponent<NetworkIdentity>();

            CmdSetWormInJunk(hookIdentity.netId, n.netId);

            GameObject worm = allHookWorms[0].gameObject;

            allHookWorms.RemoveAt(0);  // safly remove
            return worm;
        }

        return null;
    }


    [Command]
    void CmdSetWormInJunk(uint hookNetId, uint wormNetId)
    {
        RPCSetWormInJunk(hookNetId, wormNetId);
    }

    [ClientRpc]
    void RPCSetWormInJunk(uint junkNetId, uint wormNetId)
    {
        if (NetworkClient.spawned.TryGetValue(junkNetId, out NetworkIdentity junkIdentity))
        {
            Hook hook = junkIdentity.gameObject.GetComponent<Hook>();
            hook.hasWorm = true;

            if (NetworkClient.spawned.TryGetValue(wormNetId, out NetworkIdentity WormIdentity))
            {
                Transform worm = WormIdentity.transform;
                worm.SetParent(hook.wormParent, false);
                worm.localScale = Vector3.one;
                worm.localPosition = Vector3.zero;
            }
        }
    }

    //
    public void EnableWormCollider(NetworkIdentity NetId)
    {
        if (NetId != null)
        {
            CmdEnableWormCollider(NetId.netId);
        }
    }


    [Command]
    void CmdEnableWormCollider(uint NetId)
    {
        RPCEnableWormCollider(NetId);
    }

    [ClientRpc]
    void RPCEnableWormCollider(uint NetId)
    {
        if (NetworkClient.spawned.TryGetValue(NetId, out NetworkIdentity Identity))
        {
            GameObject worm = Identity.gameObject;
            PolygonCollider2D col = worm.GetComponent<PolygonCollider2D>();
            if (col != null)
            {
                col.enabled = true;
            }
        }
    }

    public void DropWorm(NetworkIdentity NetId)
    {
        if (NetId != null)
        {
            CmdDropWorm(NetId.netId);
        }
    }


    [Command]
    void CmdDropWorm(uint NetId)
    {
        RPCDropWorm(NetId);
    }

    [ClientRpc]
    void RPCDropWorm(uint NetId)
    {
        if (NetworkClient.spawned.TryGetValue(NetId, out NetworkIdentity Identity))
        {
            GameObject wormInstance = Identity.gameObject;
            wormInstance.transform.parent = null;
            wormInstance = null;
        }
    }



    public void ReturnRoadOfHook()
    {
        if (isLocalPlayer)
        {
            CMDReturnRoadOfHook();
        }
    }

    [Command]
    public void CMDReturnRoadOfHook()
    {

        RPCReturnRoadOfHook();
    }

    [ClientRpc]
    public void RPCReturnRoadOfHook()
    {

        Hook.Instance.LoadReturnToRod_Mirror();
    }


    //mash phase start in fisher man
    public void CallMashPhase()
    {
        Debug.Log("CallMashPhase");
        if (isLocalPlayer)
        {
            CMDCallMashPhase();
        }
    }

    [Command]
    public void CMDCallMashPhase()
    {
        Debug.Log("CMDCallMashPhase");
        RPCCallMashPhase();
    }

    [ClientRpc]
    public void RPCCallMashPhase()
    {
        Debug.Log("RPCCallMashPhase");
        MashPhaseManager.Instance.CallMashPhase_Mirror();
    }


    public void CallDisableMashPhase()
    {
        CMDDisableMashPhase();
    }

    [Command]
    public void CMDDisableMashPhase()
    {
        RPCDisableMashPhase();
    }

    [ClientRpc]
    public void RPCDisableMashPhase()
    {
        MashPhaseManager.Instance.DisableMashPhase();
    }


    public void PutFishInHook_Mirror(NetworkIdentity FishNetId, NetworkIdentity HookNetId)
    {
        if (FishNetId != null)
        {
            CMDPutFishInHook(FishNetId.netId, HookNetId.netId);
        }
    }


    [Command]
    void CMDPutFishInHook(uint FishNetId, uint HookNetId)
    {
        RPCPutFishInHook(FishNetId, HookNetId);
    }

    [ClientRpc]
    void RPCPutFishInHook(uint FishNetId, uint HookNetId)
    {
        if (NetworkClient.spawned.TryGetValue(FishNetId, out NetworkIdentity FishIdentity))
        {
            GameObject fish = FishIdentity.gameObject;

            if (NetworkClient.spawned.TryGetValue(HookNetId, out NetworkIdentity HookIdentity))
            {
                GameObject hook = HookIdentity.gameObject;

                Transform fishParent = hook.GetComponent<Hook>().wormParent;
                fish.transform.GetComponent<PolygonCollider2D>().enabled = false;
                fish.transform.SetParent(fishParent);
                fish.transform.eulerAngles = new Vector3(0f, 0f, -90f);
                fish.transform.localPosition = Vector3.zero;
                ReturnRoadOfHook();
            }
        }
    }


    public void DisableFish_Mirror(NetworkIdentity FishNetId)
    {
        CMDDisableFish_Mirror(FishNetId.netId);
    }

    [Command]
    public void CMDDisableFish_Mirror(uint FishNetId)
    {
        RPCDisableFish_Mirror(FishNetId);
    }

    [ClientRpc]
    public void RPCDisableFish_Mirror(uint FishNetId)
    {

        if (NetworkClient.spawned.TryGetValue(FishNetId, out NetworkIdentity FishIdentity))
        {
            GameObject fish = FishIdentity.gameObject;
            fish.transform.SetParent(null, false);
            fish.transform.localScale = Vector3.zero;
        }
    }

}
