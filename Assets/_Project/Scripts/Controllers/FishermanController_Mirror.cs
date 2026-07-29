using Mirror;
using UnityEngine;

using PanicAtThePond.Managers;
using PanicAtThePond.Gameplay;
using PanicAtThePond.UI;
using PanicAtThePond.Shop;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.Controllers
{
public class FishermanController_Mirror : NetworkBehaviour
{
    public static FishermanController_Mirror Instance;
    public FishermanController FishermanController;

    [SerializeField] private Hook hookPrefab;
    internal Hook hook;


    private void Awake()
    {
        Instance = this;
    }

    [SyncVar(hook = nameof(OnCosmeticsChanged))]
    public string syncedHatName = "";
    [SyncVar(hook = nameof(OnCosmeticsChanged))]
    public string syncedHairName = "";

    [SyncVar(hook = nameof(OnDirectionChanged))]
    public bool syncedIsLeft = true;

    public void OnCosmeticsChanged(string oldVal, string newVal)
    {
        // SyncVar changed on a client (e.g. the catcher's CmdSetCosmetics reached the server and
        // replicated). Re-apply so remote players see the correct hat/hair on this fisherman.
        ApplySyncedCosmetics();
    }

    public void OnDirectionChanged(bool oldVal, bool newVal)
    {
        if (FishermanController != null)
        {
            FishermanController.isLeft = newVal;
            FishermanController.isRight = !newVal;
        }
    }

    [Command]
    public void CmdSetCosmetics(string hatName, string hairName)
    {
        syncedHatName = hatName;
        syncedHairName = hairName;
        ApplySyncedCosmetics();
    }

    [Command]
    public void CmdSetDirection(bool isLeft)
    {
        syncedIsLeft = isLeft;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // The FishermanController / its Animator may not be ready on the exact frame this object
        // spawns, and the initial SyncVar payload can arrive a frame after OnStartClient. Retry for a
        // short window so REMOTE players reliably see the synced hat/hair (fixes hats missing on
        // other players' screens in LAN).
        StartCoroutine(ApplySyncedCosmeticsWhenReady());
        OnDirectionChanged(syncedIsLeft, syncedIsLeft);
    }

    private System.Collections.IEnumerator ApplySyncedCosmeticsWhenReady()
    {
        float elapsed = 0f;
        const float timeout = 3f;

        while (elapsed < timeout)
        {
            if (FishermanController != null)
            {
                ApplySyncedCosmetics();

                // Once we actually have a cosmetic name to show, we're done. Until then keep polling
                // in case the SyncVar payload lands a frame later.
                if (!string.IsNullOrEmpty(syncedHatName) || !string.IsNullOrEmpty(syncedHairName))
                {
                    yield break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        ApplySyncedCosmetics();
    }

    private void ApplySyncedCosmetics()
    {
        if (FishermanController != null)
        {
            CosmeticRuntimeApplier.ApplyFishermanCosmeticsByName(FishermanController.gameObject, syncedHatName, syncedHairName);
        }
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
        Debug.Log("=== FishermanController_Mirror SpawnHook called ===");
        Debug.Log("isServer: " + isServer);
        Debug.Log("isClient: " + isClient);
        Debug.Log("isLocalPlayer: " + isLocalPlayer);

        if (isServer)
        {
            // Defensively fall back to Resources.Load if the Inspector reference was lost at runtime.
            Hook prefab = hookPrefab;
            if (prefab == null)
            {
                prefab = Resources.Load<Hook>("hookPrefab");
                Debug.LogWarning("[FishermanController_Mirror] hookPrefab was null -- fell back to Resources.Load. Result: " + (prefab != null ? prefab.name : "NULL"));
            }

            if (prefab == null)
            {
                Debug.LogError("[FishermanController_Mirror] Could not resolve hook prefab -- aborting hook spawn.");
                return;
            }

            hook = Instantiate(prefab, transform.position, Quaternion.identity);
            NetworkServer.Spawn(hook.gameObject, connectionToClient);
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

}