using Mirror;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using PanicAtThePond.Managers;
using PanicAtThePond.Gameplay;
using PanicAtThePond.UI;
using PanicAtThePond.Shop;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.Controllers
{
public class FishController_Mirror : NetworkBehaviour
{
    [Header("Input System")]
    public InputActionReference moveAction;

    [SerializeField] private FishController fishController;

    public GameObject wormPrefab;

    public List<WormManager> allHookWorms = new List<WormManager>();


    [SyncVar(hook = nameof(OnHatChanged))]
    public string syncedHatName = "";

    [SyncVar(hook = nameof(OnFishSpeciesChanged))]
    public int syncedFishSpeciesIndex = 0;

    public void OnFishSpeciesChanged(int oldFishSpecies, int newFishSpecies)
    {
        ApplySyncedFishSpecies();
        ApplySyncedHat();
    }

    public void OnHatChanged(string oldHat, string newHat)
    {
        ApplySyncedHat();
    }

    [Command]
    public void CmdSetFishSpecies(int fishSpeciesIndex)
    {
        syncedFishSpeciesIndex = fishSpeciesIndex;
        ApplySyncedFishSpecies();
        ApplySyncedHat();
    }

    [Command]
    public void CmdSetHat(string hatName)
    {
        syncedHatName = hatName;
        // Apply on server so all clients see it when SyncVar hooks fire
        ApplySyncedHat();
        ApplySyncedFishSpecies();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(ApplySyncedCosmeticsWhenReady());
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        
        int selectedFishSpecies = LocalPlayManager.GetSelectedFishIndex();
        CmdSetFishSpecies(selectedFishSpecies);

        string myHat = CosmeticRuntimeApplier.GetSelectedFishHatName();
        CmdSetHat(myHat);

        CosmeticRuntimeApplier.ApplyFishSpeciesByIndex(gameObject, selectedFishSpecies);
        CosmeticRuntimeApplier.ApplyFishHatByName(gameObject, myHat);
    }

    private System.Collections.IEnumerator ApplySyncedCosmeticsWhenReady()
    {
        float elapsed = 0f;
        const float timeout = 3f;

        while (elapsed < timeout)
        {
            ApplySyncedFishSpecies();
            ApplySyncedHat();

            if (!string.IsNullOrEmpty(syncedHatName))
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        ApplySyncedFishSpecies();
        ApplySyncedHat();
    }

    private void ApplySyncedFishSpecies()
    {
        CosmeticRuntimeApplier.ApplyFishSpeciesByIndex(gameObject, syncedFishSpeciesIndex);
    }

    private void ApplySyncedHat()
    {
        CosmeticRuntimeApplier.ApplyFishHatByName(gameObject, syncedHatName);
    }

    private void Awake()
    {
    }
    private void Start()
    {
        Debug.Log("=== FishController_Mirror CALLED ===");
        Debug.Log("isServer: " + isServer);
        Debug.Log("isClient: " + isClient);
        Debug.Log("isLocalPlayer: " + isLocalPlayer);
        Debug.Log("connectionToClient: " + connectionToClient);
        MarkMeDead(false);

        // PDF 1.1.8: "Make sure each player in the game can adjust their fish type, fish cosmetics
        // and Fisherman cosmetics individually."
        // This block used to apply THIS machine's own PlayerPrefs selection to every fish it saw,
        // including remote players' fish — which is exactly why the host's fish/cosmetic appeared on
        // everyone. Only the fish we own may be seeded from local prefs; every other fish must be
        // driven purely by its owner's replicated SyncVars.
        if (isLocalPlayer)
        {
            string hatName = CosmeticRuntimeApplier.GetSelectedFishHatName();
            int speciesIndex = LocalPlayManager.GetSelectedFishIndex();

            CosmeticRuntimeApplier.ApplyFishSpeciesByIndex(gameObject, speciesIndex);
            CosmeticRuntimeApplier.ApplyFishHatByName(gameObject, hatName);

            // Publish to the server so every other client re-skins this fish via the SyncVar hooks.
            CmdSetHat(hatName);
            CmdSetFishSpecies(speciesIndex);
        }
        else
        {
            ApplySyncedFishSpecies();
            ApplySyncedHat();
        }
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

    public void SetVissiblity_Mirror()
    {
        GS gsObj = GS.Instance;
        if (gsObj.IsMirrorMasterClient)
        {
            SetVisibility(gsObj.ReflectiveWater, gsObj.DeepWaters, gsObj.MurkyWaters, gsObj.ClearWaters);
        }
    }

    [ClientRpc]
    public void SetVisibility(bool reflectiveWater, bool deepWaters, bool murkyWaters, bool clearWaters)
    {
        GS gsObj = GS.Instance;

        gsObj.ClearWaters = clearWaters;
        gsObj.MurkyWaters = murkyWaters;
        gsObj.DeepWaters = deepWaters;
        gsObj.ReflectiveWater = reflectiveWater;

        Debug.Log($"[GS] Visibility updated: All={reflectiveWater}, Deep={deepWaters}, Murky={murkyWaters}, Clear={clearWaters}");
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

    public void CallHideFish_Mirror()
    {
        if (isLocalPlayer || isOwned)
        {
            CmdHideFish_Mirror();
        }
    }

    [Command]
    private void CmdHideFish_Mirror()
    {
        RpcHideFish_Mirror();
    }

    [ClientRpc]
    private void RpcHideFish_Mirror()
    {
        transform.localScale = Vector3.zero;
        if (GetComponent<FishController>() != null)
        {
            GetComponent<FishController>().isFisherMan = true;
        }
    }


    //generate FisherMan
    [SerializeField] private GameObject fishermanPrefab;

    public void RequestSpawnFisherman(string hatName = "", string hairName = "", bool useFishingBackground = false, bool useLargeFishingBackground = false)
    {
        if (isLocalPlayer)
        {
            CmdSpawnFishermanOnServer(hatName, hairName, useFishingBackground, useLargeFishingBackground);
        }
    }

    [Command]
    void CmdSpawnFishermanOnServer(string hatName, string hairName, bool useFishingBackground, bool useLargeFishingBackground)
    {
        // fishermanPrefab is sometimes null at runtime due to recompile/domain-reload losing
        // the Inspector reference. Defensively fall back to Resources.Load so the Command
        // still succeeds and the catcher isn't left stuck on the Loading overlay.
        GameObject prefab = fishermanPrefab;
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("FisherMan");
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("FisherMan (2) 1");
            }
            Debug.LogWarning($"[FishController_Mirror] fishermanPrefab reference was null — fell back to Resources.Load. Prefab used: {(prefab != null ? prefab.name : "NULL")}");
        }

        Vector3 spawnPos = GameManager.GetFishermanSpawnPosition(useFishingBackground, useLargeFishingBackground);
        GameObject fisherman = Instantiate(prefab, spawnPos, Quaternion.identity);
        GameManager.ApplyFishermanMapTransform(fisherman, useFishingBackground, useLargeFishingBackground);
        FishermanController_Mirror fishermanMirror = fisherman.GetComponent<FishermanController_Mirror>();
        if (fishermanMirror != null)
        {
            fishermanMirror.syncedHatName = hatName ?? string.Empty;
            fishermanMirror.syncedHairName = hairName ?? string.Empty;
        }
        // KeepAuthority is the exact equivalent of the old `keepAuthority: true` overload
        // (see Mirror NetworkServer.cs:1149). Makes it local player and gives authority.
        NetworkServer.ReplacePlayerForConnection(connectionToClient, fisherman, ReplacePlayerOptions.KeepAuthority);
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



   


    public void SpawnWorm(int length)
    {
        if (isServer)
        {
            // Defensively fall back to Resources.Load if the Inspector reference was lost at runtime.
            GameObject prefab = wormPrefab;
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("Worm");
                Debug.LogWarning("[FishController_Mirror] wormPrefab was null -- fell back to Resources.Load. Result: " + (prefab != null ? prefab.name : "NULL"));
            }

            for (int i = 0; i < length; i++)
            {
                GameObject worm = Instantiate(prefab, new Vector3(0f, 10f, 0f), Quaternion.identity);
                NetworkServer.Spawn(worm, connectionToClient);
            }
        }
    }

    public GameObject SetWormInJunk(NetworkIdentity wormIdentity)
    {
        if (wormIdentity == null) return null;

        if (isServer)
        {
            GameObject worm = wormIdentity.gameObject;

            // Find the local player's hook's worm parent
            if (FishermanController_Mirror.Instance != null &&
                FishermanController_Mirror.Instance.hook != null &&
                FishermanController_Mirror.Instance.hook.wormParent != null)
            {
                worm.transform.SetParent(FishermanController_Mirror.Instance.hook.wormParent, false);
                worm.transform.localPosition = Vector3.zero;
                worm.transform.localScale = Vector3.one;
            }
            else
            {
                Debug.LogWarning("[FishController_Mirror] SetWormInJunk: could not find worm parent");
            }

            // Add to tracking list
            WormManager wm = worm.GetComponent<WormManager>();
            if (wm != null && !allHookWorms.Contains(wm))
            {
                allHookWorms.Add(wm);
            }
        }

        return wormIdentity.gameObject;
    }
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
    public void CallMashPhase(float mashTimes)
    {
        Debug.Log("CallMashPhase");
        if (isLocalPlayer)
        {
            CMDCallMashPhase(mashTimes);
        }
    }

    [Command]
    public void CMDCallMashPhase(float mashTimes)
    {
        Debug.Log("CMDCallMashPhase");
        RPCCallMashPhase(mashTimes);
    }

    [ClientRpc]
    public void RPCCallMashPhase(float mashTimes)
    {
        Debug.Log("RPCCallMashPhase");
        MashPhaseManager.Instance.CallMashPhase_Mirror(mashTimes);
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


    //Set Junk in hook

    public void SetJunkInHook_Mirror(NetworkIdentity JunkNetId, NetworkIdentity HookNetId)
    {
        if (JunkNetId != null)
        {
            CMDSetJunkInHook_Mirror(JunkNetId.netId, HookNetId.netId);
        }
    }


    [Command]
    void CMDSetJunkInHook_Mirror(uint JunkNetId, uint HookNetId)
    {
        RPCDisableFish_Mirror(JunkNetId, HookNetId);
    }

    [ClientRpc]
    void RPCDisableFish_Mirror(uint JunkNetId, uint HookNetId)
    {
        if (NetworkClient.spawned.TryGetValue(JunkNetId, out NetworkIdentity JunkIdentity))
        {
            GameObject junk = JunkIdentity.gameObject;

            if (NetworkClient.spawned.TryGetValue(HookNetId, out NetworkIdentity HookIdentity))
            {
                GameObject hook = HookIdentity.gameObject;


                junk.GetComponent<PolygonCollider2D>().enabled = false;
                junk.transform.SetParent(hook.GetComponent<Hook>().wormParent);
                junk.transform.localPosition = Vector3.zero;
                fishController.carriedJunk = null;
                ReturnRoadOfHook();
            }
        }
    }


    public void MarkMeDead(bool res)
    {
        NetworkConnectionToClient conn = connectionToClient;
        if (conn != null)
        {
            conn.isDead = res;   // SET
            Debug.Log("@@@@@@@@@@@@@@Marked dead on server" + conn.isDead);
        }
    }


    public void SetDeadFish_Mirror(NetworkIdentity FishNetId)
    {
        CMDSetDeadFish_Mirror(FishNetId.netId);
    }

    [Command]
    public void CMDSetDeadFish_Mirror(uint FishNetId)
    {
        RPCSetDeadFish_Mirror(FishNetId);
    }

    [ClientRpc]
    public void RPCSetDeadFish_Mirror(uint FishNetId)
    {

        if (NetworkClient.spawned.TryGetValue(FishNetId, out NetworkIdentity FishIdentity))
        {
            FishController fish = FishIdentity.GetComponent<FishController>();
            fish.ApplyHungerDeathVisibleState();
            if (FishermanController.Instance != null)
            {
                FishermanController.Instance.PlayWinAnimation();
            }
            CallLessPlayerCount_Mirror();
        }
    }

    public void CallLessPlayerCount_Mirror()
    {
        CMDCallLessPlayerCount_Mirror();
    }


    [Command]
    public void CMDCallLessPlayerCount_Mirror()
    {
        RPCCallLessPlayerCount_Mirror();
    }

    [ClientRpc]
    public void RPCCallLessPlayerCount_Mirror()
    {
        Debug.Log("RPCCallLessPlayerCount_Mirror called");
        if (GameManager.Instance.isFisherMan)
        {
            Debug.Log("I m fisher man");
            GameManager.Instance.LessPlayerCount_Mirror();
        }
    }

    public void CallGamePause(bool isPause)
    {
        uint fishID = GetComponent<NetworkIdentity>().netId;
        GamePause(isPause, fishID);
    }

    public void GamePause(bool isPause, uint fishID)
    {
        CMDCallGamePause_Mirror(isPause, fishID);
    }

    [Command]
    public void CMDCallGamePause_Mirror(bool isPause, uint fishID)
    {
        RPCCallGamePause(isPause, fishID);
    }

    [ClientRpc]
    public void RPCCallGamePause(bool isPause, uint fishID)
    {
        uint thisFishID = GetComponent<NetworkIdentity>().netId;

        if (NetworkClient.spawned.TryGetValue(fishID, out NetworkIdentity FishIdentity))
        {
            if (thisFishID == fishID)
            {
                if (isPause)
                {
                    transform.GetComponent<PolygonCollider2D>().enabled = false;
                    var sr = GetComponent<SpriteRenderer>();
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.5f);
                    fishController.canMove = false;
                }
                else
                {
                    transform.GetComponent<PolygonCollider2D>().enabled = true;
                    var sr = GetComponent<SpriteRenderer>();
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
                    fishController.canMove = true;
                }
            }
        }
    }
}

}