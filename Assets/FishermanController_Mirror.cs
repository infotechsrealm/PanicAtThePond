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
}