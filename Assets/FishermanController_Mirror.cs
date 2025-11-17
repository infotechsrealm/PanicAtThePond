using Mirror;
using UnityEngine;
using static FishController_Mirror;

public class FishermanController_Mirror : NetworkBehaviour
{
    public FishermanController FishermanController;

    private void Start()
    {
        Debug.Log("=== FishermanController_Mirror CALLED ===");
        Debug.Log("isServer: " + isServer);
        Debug.Log("isClient: " + isClient);
        Debug.Log("isLocalPlayer: " + isLocalPlayer);
        Debug.Log("connectionToClient: " + connectionToClient);
    }

    public void CallLoadReleaseCast()
    {

        if (isLocalPlayer)
        {
            Debug.Log("FishermanController_Mirror is local ");

            RPCCallLoadReleaseCast();
        }
        else if (isServer)
        {
            FishermanController.LoadReleaseCast();

        }
    }

    [Command]
    void RPCCallLoadReleaseCast()
    {
        FishermanController.LoadReleaseCast();
    }

    //generat hook
    public void SpawnHook()
    {
        Debug.Log("=== FishermanController_Mirror CALLED ===");
        Debug.Log("isServer: " + isServer);
        Debug.Log("isClient: " + isClient);
        Debug.Log("isLocalPlayer: " + isLocalPlayer);
        Debug.Log("connectionToClient: " + connectionToClient);


        GameObject temphook = Instantiate(FishermanController.hookPrefab, FishermanController.currentRod.position, Quaternion.identity);
        NetworkServer.Spawn(temphook, connectionToClient); // 🔹 gives authority to caller client

       /* if (isLocalPlayer)
        {
            Debug.Log("FishermanController_Mirror is local ");

            CmdSpawnHook();
        }
        else if(isServer) 
        {
            Debug.Log("FishermanController_Mirror is isServer ");
            GameObject temphook = Instantiate(FishermanController.hookPrefab, FishermanController.currentRod.position, Quaternion.identity);
            NetworkServer.Spawn(temphook, connectionToClient); // 🔹 gives authority to caller client
        }
        else
        {
            
            *//*Debug.Log("FishermanController_Mirror is not server and not local");
            NetworkClient.Send(new SpawnHookMessage());*//*

          //   NetworkClient.Send(new SpawnHookMessage());

            *//* GameObject temphook = Instantiate(FishermanController.hookPrefab, FishermanController.currentRod.position, Quaternion.identity);
             NetworkServer.Spawn(temphook);*//*
        }*/
    }

    [Command]
    void CmdSpawnHook()
    {
        GameObject temphook = Instantiate(FishermanController.hookPrefab, FishermanController.currentRod.position, Quaternion.identity);
        NetworkServer.Spawn(temphook, connectionToClient); // 🔹 gives authority to caller client
    }


   

















    // set junk 
    public void TryToSetJunkRod(NetworkIdentity hookIDidentity , Vector3 curruntRod)
    {
        if (hookIDidentity != null)
        {
            CmdSetJunkRod(hookIDidentity.netId , curruntRod);
        }
    }

    [Command]
    void CmdSetJunkRod(uint hookID , Vector3 curruntRod)
    {
        Debug.Log("CmdSetJunkRod called" + curruntRod);
        if (NetworkServer.spawned.TryGetValue(hookID, out NetworkIdentity hookIDidentity))
        {
            GameObject hookobj = hookIDidentity.gameObject;

            Hook hook = hookobj.GetComponent<Hook>();

            if (hook != null)
            {
                hook.rodTip = curruntRod;
            }

            RpcSetJunkRod(hookID, curruntRod);
        }
    }

    [ClientRpc]
    void RpcSetJunkRod(uint hookID, Vector3 curruntRod)
    {
        Debug.Log("RpcSetJunkRod called" + curruntRod);

        if (NetworkClient.spawned.TryGetValue(hookID, out NetworkIdentity hookIDidentity))
        {
            GameObject hookobj = hookIDidentity.gameObject;

            Hook hook = hookobj.GetComponent<Hook>();

            if (hook != null)
            {
                hook.rodTip = curruntRod;
            }
        }
    }
}