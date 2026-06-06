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

    [SyncVar(hook = nameof(OnCosmeticsChanged))]
    public string syncedHatName = "";
    [SyncVar(hook = nameof(OnCosmeticsChanged))]
    public string syncedHairName = "";

    public void OnCosmeticsChanged(string oldVal, string newVal)
    {
        if (FishermanController != null)
            CosmeticRuntimeApplier.ApplyFishermanCosmeticsByName(FishermanController.gameObject, syncedHatName, syncedHairName);
    }

    [Command]
    public void CmdSetCosmetics(string hatName, string hairName)
    {
        syncedHatName = hatName;
        syncedHairName = hairName;
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

    public void CallShowGameOver_Mirror(string message)
    {
        if (isLocalPlayer)
        {
            CmdShowGameOver_Mirror(message);
        }
    }

    [Command]
    public void CmdShowGameOver_Mirror(string message)
    {
        RpcShowGameOver_Mirror(message);
    }

    [ClientRpc]
    public void RpcShowGameOver_Mirror(string message)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowGameOver(message);
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

    public void CallSetTrigger_Mirror(string triggerName)
    {
        if (isLocalPlayer) CmdSetTrigger_Mirror(triggerName);
    }

    [Command]
    private void CmdSetTrigger_Mirror(string triggerName)
    {
        RpcSetTrigger_Mirror(triggerName);
    }

    [ClientRpc]
    private void RpcSetTrigger_Mirror(string triggerName)
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null) animator.SetTrigger(triggerName);
    }

    public void CallResetTrigger_Mirror(string triggerName)
    {
        if (isLocalPlayer) CmdResetTrigger_Mirror(triggerName);
    }

    [Command]
    private void CmdResetTrigger_Mirror(string triggerName)
    {
        RpcResetTrigger_Mirror(triggerName);
    }

    [ClientRpc]
    private void RpcResetTrigger_Mirror(string triggerName)
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null) animator.ResetTrigger(triggerName);
    }

    public void CallSetBool_Mirror(string boolName, bool value)
    {
        if (isLocalPlayer) CmdSetBool_Mirror(boolName, value);
    }

    [Command]
    private void CmdSetBool_Mirror(string boolName, bool value)
    {
        RpcSetBool_Mirror(boolName, value);
    }

    [ClientRpc]
    private void RpcSetBool_Mirror(string boolName, bool value)
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null) animator.SetBool(boolName, value);
    }
}