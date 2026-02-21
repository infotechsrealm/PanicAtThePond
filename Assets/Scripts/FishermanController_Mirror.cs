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

    public void CallAddScore_Mirror(string playerName, int amount)
    {
        if (isLocalPlayer) {
             CmdAddScore_Mirror(playerName, amount);
        }
    }

    [Command]
    public void CmdAddScore_Mirror(string playerName, int amount)
    {
         RpcAddScore_Mirror(playerName, amount);
    }

    [ClientRpc]
    public void RpcAddScore_Mirror(string playerName, int amount)
    {
         if (GS.Instance == null) return;
         if (!GS.Instance.playerScores.ContainsKey(playerName)) GS.Instance.playerScores[playerName] = 0;
         GS.Instance.playerScores[playerName] += amount;
    }

    public void CallTriggerRoundEnd_Mirror(string message)
    {
        if (isLocalPlayer) {
             CmdTriggerRoundEnd_Mirror(message);
        }
    }

    [Command]
    public void CmdTriggerRoundEnd_Mirror(string message)
    {
         RpcTriggerRoundEnd_Mirror(message);
    }

    [ClientRpc]
    public void RpcTriggerRoundEnd_Mirror(string message)
    {
         if (GameManager.Instance != null) {
             GameManager.Instance.EndRoundRPC(message);
         }
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