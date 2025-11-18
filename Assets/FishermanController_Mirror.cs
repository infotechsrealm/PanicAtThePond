using Mirror;
using UnityEngine;

public class FishermanController_Mirror : NetworkBehaviour
{
    public static FishermanController_Mirror Instance;
    public FishermanController FishermanController;

    public Hook hookPrefab;
    internal Hook hook;


    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        Debug.Log("=== FishermanController_Mirror CALLED ===");
        Debug.Log("isServer: " + isServer);
        Debug.Log("isClient: " + isClient);
        Debug.Log("isLocalPlayer: " + isLocalPlayer);
        Debug.Log("connectionToClient: " + connectionToClient);

        SpawnHook();
    }
   
    //generat hook
    public void SpawnHook()
    {
        Debug.Log("=== FishermanController_Mirror CALLED ===");
        Debug.Log("isServer: " + isServer);
        Debug.Log("isClient: " + isClient);
        Debug.Log("isLocalPlayer: " + isLocalPlayer);
   
        if (isServer)
        {
            if (hookPrefab == null)
            {
                Debug.LogError("Hook Prefab not assigned!");
                return;
            }

            hook = Instantiate(hookPrefab, transform.position, Quaternion.identity);
            NetworkServer.Spawn(hook.gameObject, connectionToClient); // 🔹 gives authority to caller client
        }
    }

    // set junk 
    public void TryToSetJunkRod( Vector3 curruntRod)
    {
        Debug.Log("TryToSetJunkRod called"); 

        hook.transform.position = curruntRod;
        hook.transform.localScale = Vector3.one;
        NetworkIdentity hookIDidentity = hook.GetComponent<NetworkIdentity>();

        if (hookIDidentity != null)
        {
            CmdSetJunkRod(hookIDidentity.netId, curruntRod);
        }
    }

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



           // RpcSetJunkRod(hookID, curruntRod);
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