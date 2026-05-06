using Mirror;
using Photon.Pun;
using Steamworks;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class FishController : MonoBehaviourPunCallbacks 
{
    public static FishController Instance;  

    public PhotonTransformViewClassic photonTransformViewClassic;

    public FishController_Mirror fishController_Mirror;

    public FishermanController fishermanController;

    [SerializeField]
    internal InputActionReference inputAction;
  
    [Header("Fish Stats")]
    public int hunger = 100;
    public float speed = 5f;

    public Vector2 minBounds = new Vector2(-8f, -4f);
    public Vector2 maxBounds = new Vector2(8f, 4f);

    public float floatSpeed = 2f;
    public Transform junkHolder;
    public int myPlayerID;

    public AudioSource 
           audioSource,
           audioSourceForFishMove;

    public AudioClip fishEatWarmSound;
    public AudioClip fishCameToSurfaceSound;

    public Animator animator;

    internal float originalScaleX;
    internal float originalScaleY;
    public bool canMove = true;
    internal bool catchadeFish = false;
    internal GameObject carriedJunk;

    [Header("Floating on Death")]
    public bool isDead = false;
   internal bool isFisherMan= false;
    private Rigidbody2D rb;

    public NetworkIdentity mirrorIdentity;

    public GameObject bubble_Junk,bubblle_Fish;

    internal GameObject bubbleanim;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        originalScaleX = transform.localScale.x;
        originalScaleY = transform.localScale.y;
        rb = GetComponent<Rigidbody2D>();
    }
    void Start()
    {
        ApplyConfiguredTroutSpeed();

        if (GS.Instance.isLan)
        {
            if (mirrorIdentity != null && mirrorIdentity.isLocalPlayer)
            {
                fishController_Mirror.SetVissiblity_Mirror();
                GameManager.Instance.myFish = this;
                fishController_Mirror.CallAddScore_Mirror(GS.Instance.nickName, 0);
            }
        }
        else
        {
            if (photonView.IsMine)
            {
                GameManager.Instance.myFish = this;
                GameManager.Instance.AddPlayerScore(PhotonNetwork.LocalPlayer.NickName, 0);
            }
        }
        GameManager.Instance.allFishes.Add(this);
    }

    private void ApplyConfiguredTroutSpeed()
    {
        if (GS.Instance == null || GS.Instance.scoreSystemSettings == null)
        {
            return;
        }

        speed = GS.Instance.scoreSystemSettings.GetTroutSpeed();
    }

    public void SetVissiblity_Mirror()
    {
        GS gsObj = GS.Instance;
        if (gsObj.IsMirrorMasterClient)
        {
            photonView.RPC(nameof(SetVisibility), RpcTarget.All, gsObj.ReflectiveWater, gsObj.DeepWaters, gsObj.MurkyWaters, gsObj.ClearWaters);
        }
    }

    [PunRPC]
    public void SetVisibility(bool reflectiveWater, bool deepWaters, bool murkyWaters, bool clearWaters)
    {
        GS gsObj = GS.Instance;

        gsObj.ClearWaters = clearWaters;
        gsObj.MurkyWaters = murkyWaters;
        gsObj.DeepWaters = deepWaters;
        gsObj.ReflectiveWater = reflectiveWater;

        Debug.Log($"[GS] Visibility updated: All={reflectiveWater}, Deep={deepWaters}, Murky={murkyWaters}, Clear={clearWaters}");
    }

    void FixedUpdate()
    {
        if(isFisherMan)
        {
            return;
        }

        if (GS.Instance.isLan)
        {
            if (!mirrorIdentity.isLocalPlayer)
            {
                return;
            }
        }
        else
        {
            if (!photonView.IsMine)
            {
                return;
            }
        }
        

        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }


        Vector2 move;
        if (GS.Instance.isLan)
        {
            if(mirrorIdentity.isLocalPlayer)
            {
                // move = inputAction.action.ReadValue<Vector2>();
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");

                 move = new Vector2(horizontal, vertical);
            }
            else
            {
                return;
            }
        }
        else
        {
          //   move = inputAction.action.ReadValue<Vector2>();

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            move = new Vector2(horizontal, vertical);
        }

        float moveX = move.x;
        float moveY = move.y;

        if (moveX != 0 || moveY != 0)
        {
            if (!audioSourceForFishMove.isPlaying)
            {
                GS.Instance.SetSFXVolume(audioSourceForFishMove);
                audioSourceForFishMove.Play();
            }

            if (!animator.GetBool("isMove"))
            {
                animator.SetBool("isMove", true);
            }
        }
        else
        {
            if (audioSourceForFishMove.isPlaying)
            {
                GS.Instance.SetSFXVolume(audioSourceForFishMove);
                audioSourceForFishMove.Pause();
            }

            if (animator.GetBool("isMove"))
            {
                animator.SetBool("isMove", false);
            }
        }

        rb.linearVelocity = new Vector2(moveX, moveY) * speed;

        // Flip fish based on direction
        if (moveX < 0)
            transform.localScale = new Vector3(originalScaleX, originalScaleY, 1);
        else if (moveX > 0)
            transform.localScale = new Vector3(-originalScaleX, originalScaleY, 1);

        // Clamp position
        Vector3 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, minBounds.x, maxBounds.x);
        clampedPos.y = Mathf.Clamp(clampedPos.y, minBounds.y, maxBounds.y);
        transform.position = clampedPos;

        // Check hunger
        if (!isDead && HungerSystem.Instance != null && HungerSystem.Instance.hungerBar.value <= 0)
        {
            StartCoroutine(FloatToSurface());
        }

        if (carriedJunk != null)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Debug.Log("Fish is leve the junk");
                if (GS.Instance.isLan)
                {
                    fishController_Mirror.TryLeaveJunk(carriedJunk.GetComponent<NetworkIdentity>());
                    carriedJunk = null;
                }
                else
                {
                    int junkId = carriedJunk.GetComponent<PhotonView>().ViewID;
                    photonView.RPC(nameof(LeaveJunk), RpcTarget.AllBuffered, junkId);
                    PhotonNetwork.SendAllOutgoingCommands();
                    carriedJunk = null;
                }
            }
        }
    }

    private IEnumerator FloatToSurface()
    {
        

        animator.SetBool("isFight", false);
        animator.SetBool("isDead", true);

        canMove = false;
        isDead = true;
        ApplyHungerDeathVisibleState();

        if (GS.Instance.isLan)
        {
            fishController_Mirror.SetDeadFish_Mirror(GetComponent<NetworkIdentity>());
        }
        else
        {
            photonView.RPC(nameof(ApplyHungerDeathVisibleStateRPC), RpcTarget.AllBuffered);
            GameManager.Instance.CallLessPlayerCountRPC();
        }


        rb.linearVelocity = Vector2.zero;
        transform.GetComponent<PolygonCollider2D>().enabled = false;

        if (carriedJunk != null)
        {
            int viewId = carriedJunk.GetComponent<PhotonView>().ViewID;
            if (GS.Instance.isLan)
            {
                fishController_Mirror.TryLeaveJunk(carriedJunk.GetComponent<NetworkIdentity>());
                carriedJunk = null;
            }
            else
            {
                photonView.RPC(nameof(LeaveJunk), RpcTarget.OthersBuffered, viewId);
            }
        }

        float targetY = maxBounds.y; // Surface
        HungerSystem.Instance.canDecrease = false;

        while (transform.position.y < targetY)
        {
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;
            yield return null;
        }

        PlaySFX(fishCameToSurfaceSound);

        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);

        if (GameManager.Instance != null && GameManager.Instance.gameOverText != null)
        {
            GameManager.Instance.CallFishermanWinAnimationRPC();
            GameManager.Instance.CallShowGameOverRPC("You all Starve!");
        }

    }

    [PunRPC]
    public void ApplyHungerDeathVisibleStateRPC()
    {
        ApplyHungerDeathVisibleState();
    }

    public void ApplyHungerDeathVisibleState()
    {
        isDead = true;
        canMove = false;

        if (animator != null)
        {
            animator.SetBool("isFight", false);
            animator.SetBool("isMove", false);
            animator.SetBool("isDead", true);
        }

        PolygonCollider2D fishCollider = GetComponent<PolygonCollider2D>();
        if (fishCollider != null)
        {
            fishCollider.enabled = false;
        }

        ForceDeadFishVisible();
    }

    public void ForceDeadFishVisible()
    {
        transform.localScale = new Vector3(
            Mathf.Approximately(originalScaleX, 0f) ? 1f : originalScaleX,
            Mathf.Approximately(originalScaleY, 0f) ? 1f : originalScaleY,
            1f);

        foreach (SpriteRenderer spriteRenderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            spriteRenderer.enabled = true;
            Color color = spriteRenderer.color;
            spriteRenderer.color = new Color(color.r, color.g, color.b, 1f);
            spriteRenderer.sortingLayerID = 0;
            spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 1000);
        }
    }

    //when Fish is Die and User press space key.
    [PunRPC]
    public void LeaveJunk(int junkId)
    {
        GameObject junk = PhotonView.Find(junkId).gameObject;
        junk.GetComponent<JunkManager>().LeaveByFish();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (photonView.IsMine || mirrorIdentity.isLocalPlayer)
        {
            if (audioSourceForFishMove.isPlaying)
            {
                GS.Instance.SetSFXVolume(audioSourceForFishMove);
                audioSourceForFishMove.Pause();
            }

            if (other.CompareTag("HookWorm"))
            {
                PlaySFX(fishEatWarmSound);
                animator.SetTrigger("isEat");

                if (GS.Instance.isLan)
                {
                    other.gameObject.GetComponent<PolygonCollider2D>().enabled = false;
                    fishController_Mirror.Destroy_Mirror(other.gameObject);
                    if (carriedJunk != null)
                    {
                        DropJunkToHook_mirror();
                        return;
                    }
                }
                else
                {
                    photonView.RPC(nameof(SetFishAsFishermanRPC), RpcTarget.All, other.GetComponent<PhotonView>().ViewID, false);
                    photonView.RPC(nameof(DestroyWormRPC), RpcTarget.All, other.GetComponent<PhotonView>().ViewID);
                    PhotonNetwork.SendAllOutgoingCommands();
                    if (carriedJunk != null)
                    {
                        DropJunkToHook();
                        return;
                    }
                }

               

                animator.SetBool("isFight", true);
                animator.SetBool("isMove", false);
                catchadeFish = true;
                canMove = false;
                MiniGameManager.Instance.StartMiniGame();

            }

            if (other.gameObject.name.Contains("Golden Fish"))
            {
                if (photonView.IsMine || mirrorIdentity.isLocalPlayer)
                {
                    GameManager.Instance.UnlockAchievement("WHAT_A_SNACK");

                    int goldenFishBonusPoints = GS.Instance != null && GS.Instance.scoreSystemSettings != null
                        ? GS.Instance.scoreSystemSettings.GetGoldenFishBonusPoints()
                        : 0;
                    if (goldenFishBonusPoints > 0)
                    {
                        string myName = "Player";
                        if (GS.Instance != null && GS.Instance.isLan) myName = GS.Instance.nickName;
                        else if (PhotonNetwork.InRoom) myName = PhotonNetwork.LocalPlayer.NickName;
                        GameManager.Instance.AddPlayerScore(myName, goldenFishBonusPoints);
                    }
                }
                
                animator.SetTrigger("isEat");
                GameManager.Instance.isFisherMan = true;
                PlaySFX(fishEatWarmSound);
                GameManager.Instance.goldWormEatByFish = true;
                GS.Instance.SetSFXVolume(audioSource);
                audioSource.Play();
                other.gameObject.transform.localScale = Vector3.zero;
                other.gameObject.GetComponent<PolygonCollider2D>().enabled = false;
                StartCoroutine(GenerateFisherMan(other.gameObject));
            }
            
            if (other.CompareTag("Worm"))
            {
                PlaySFX(fishEatWarmSound);
                animator.SetTrigger("isEat");
                CollectCoin();
                
                if (photonView.IsMine || mirrorIdentity.isLocalPlayer)
                {
                    string myName = "Player";
                    if (GS.Instance != null && GS.Instance.isLan) myName = GS.Instance.nickName;
                    else if (PhotonNetwork.InRoom) myName = PhotonNetwork.LocalPlayer.NickName;
                    int wormPoints = GS.Instance != null && GS.Instance.scoreSystemSettings != null
                        ? GS.Instance.scoreSystemSettings.GetFishEatWormPoints()
                        : 1;
                    GameManager.Instance.AddPlayerScore(myName, wormPoints);
                    
                    if (!GS.Instance.wormsEatenThisRound.ContainsKey(myName)) GS.Instance.wormsEatenThisRound[myName] = 0;
                    GS.Instance.wormsEatenThisRound[myName]++;
                    if (GS.Instance.wormsEatenThisRound[myName] >= 30) GameManager.Instance.UnlockAchievement("GULPER");
                }

                float hungerWormRateAmount = GS.Instance != null && GS.Instance.scoreSystemSettings != null
                    ? GS.Instance.scoreSystemSettings.GetHungerWormRateAmount()
                    : 15f;

                if (GS.Instance.isLan)
                {
                    fishController_Mirror.Destroy_Mirror(other.gameObject);
                    HungerSystem.Instance.AddHunger(hungerWormRateAmount);
                }
                else
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        DestroyWormRPC(other.GetComponent<PhotonView>().ViewID);
                        HungerSystem.Instance.AddHunger(hungerWormRateAmount);
                    }
                    else
                    {
                        photonView.RPC(nameof(DestroyWormRPC), RpcTarget.MasterClient, other.GetComponent<PhotonView>().ViewID);
                        PhotonNetwork.SendAllOutgoingCommands();
                        HungerSystem.Instance.AddHunger(hungerWormRateAmount);
                    }
                }
            }

            if (other.CompareTag("Junk") && carriedJunk == null && !catchadeFish)
            {
                Debug.Log("collide with junk");

                animator.SetTrigger("isEat");
                JunkManager junk = other.GetComponent<JunkManager>();
                carriedJunk = junk.gameObject;

                if (GS.Instance.isLan)
                {
                    Debug.Log("is master Client = " + GS.Instance.IsMirrorMasterClient);
                    junk.CallFreezeObjectRPC();
                    fishController_Mirror.TryPickupJunk(carriedJunk.GetComponent<NetworkIdentity>());
                }
                else
                {
                    carriedJunk.transform.SetParent(junkHolder);
                    carriedJunk.transform.localPosition = Vector3.zero;
                    carriedJunk.GetComponent<PolygonCollider2D>().enabled = false;

                    junk.CallFreezeObjectRPC();
                    int viewId = other.GetComponent<PhotonView>().ViewID;
                    photonView.RPC(nameof(SetJunkInFish), RpcTarget.OthersBuffered, viewId);
                    PhotonNetwork.SendAllOutgoingCommands();
                }
            }
        }
    }


    [PunRPC]
    void SetJunkInFish(int viewId)
    {
        carriedJunk = PhotonView.Find(viewId).gameObject;
        carriedJunk.GetComponent<PolygonCollider2D>().enabled = false;
        carriedJunk.transform.SetParent(junkHolder);
        carriedJunk.transform.localPosition = Vector3.zero;
    }

    IEnumerator GenerateFisherMan(GameObject other)
    {
        yield return new WaitForSeconds(0.25f);

        if (GS.Instance.isLan)
        {
            Debug.Log("GenerateFisherMan");
            fishController_Mirror.Destroy_Mirror(other);
            transform.localScale =Vector3.zero;
            isFisherMan = true;
            GameManager.Instance.LoadSpawnFisherman();
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                WormSpawner.Instance.DestroyAllWorms();
                PhotonNetwork.Destroy(other.gameObject);
                photonView.RPC(nameof(ResetFishHungerForRoleTransitionRPC), RpcTarget.All);
                GameManager.Instance.LoadSpawnFisherman();
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                photonView.RPC(nameof(LoadDestroyAllWormsRPC), RpcTarget.MasterClient);
                photonView.RPC(nameof(DestroyWormRPC), RpcTarget.MasterClient, other.GetComponent<PhotonView>().ViewID);
                photonView.RPC(nameof(ResetFishHungerForRoleTransitionRPC), RpcTarget.All);
                GameManager.Instance.LoadGetIdAndChangeHost();
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    [PunRPC]
    private void ResetFishHungerForRoleTransitionRPC()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetFishHungerForRoleTransition();
        }
    }

    [PunRPC]
    public void SetFishAsFishermanRPC(int viewID, bool result)
    {
        PhotonView pv = PhotonView.Find(viewID);
        pv.gameObject.GetComponent<PolygonCollider2D>().enabled = result;
    }

    [PunRPC]
    public void LoadDestroyAllWormsRPC()
    {
        WormSpawner.Instance.DestroyAllWorms();
    }
    [PunRPC]
    void DestroyWormRPC(int viewID)
    {
        PhotonView pv = PhotonView.Find(viewID);
        if (pv != null)
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.Destroy(pv.gameObject);
        }
    }

    void DropJunkToHook()
    {
        Hook hook = Hook.Instance;

        if (hook != null)
        {
            HungerSystem.Instance.AddHunger(75f);
            int viewId = hook.GetComponent<PhotonView>().ViewID;
            photonView.RPC(nameof(SetJunkInHook), RpcTarget.All, viewId);
            PhotonNetwork.SendAllOutgoingCommands();

        }
        hook.CallRpcToReturnRod();
    }

    void DropJunkToHook_mirror()
    {
        Hook hook = Hook.Instance;

        if (hook != null)
        {
            HungerSystem.Instance.AddHunger(75f);

            fishController_Mirror.SetJunkInHook_Mirror(carriedJunk.GetComponent<NetworkIdentity>(), hook.GetComponent<NetworkIdentity>());
        }
    }

    [PunRPC]
    void SetJunkInHook(int viewId)
    {
       
        Hook hook = PhotonView.Find(viewId).GetComponent<Hook>();
      /*  Vector2 pos = new Vector2(hook.rodTip.position.x, 0.6f);
        bubbleanim = Instantiate(bubble_Junk);
        bubbleanim.transform.position = pos;
*/
        carriedJunk.GetComponent<PolygonCollider2D>().enabled = false;
        carriedJunk.transform.SetParent(hook.wormParent);
        carriedJunk.transform.localPosition = Vector3.zero;
        carriedJunk = null;
    }


    //When Fish Loss in MashPhase.
    public void PutFishInHook()
    {
        if (GS.Instance.isLan)
        {
            NetworkIdentity fishViewID = GameManager.Instance.myFish.GetComponent<NetworkIdentity>();
            NetworkIdentity hookViewID = Hook.Instance.GetComponent<NetworkIdentity>();
            fishController_Mirror.PutFishInHook_Mirror(fishViewID, hookViewID);
        }
        else
        {
            int fishViewID = GameManager.Instance.myFish.GetComponent<PhotonView>().ViewID;
            int hookViewID = Hook.Instance.GetComponent<PhotonView>().ViewID;
            photonView.RPC(nameof(PutFishInHookRPC), RpcTarget.All, fishViewID, hookViewID);
            PhotonNetwork.SendAllOutgoingCommands();
        }

        if (photonView.IsMine || mirrorIdentity.isLocalPlayer)
        {
            if (GameManager.Instance != null && GameManager.Instance.gameOverText != null)
            {
                GameManager.Instance.ShowGameOver("You lose!");
                canMove = false;
                isDead = true;
                HungerSystem.Instance.canDecrease = false;
            }
        }
    }


    [PunRPC]
    public void PutFishInHookRPC(int fishId, int hookId)
    {
        //Do not Change Linr.
        GameObject fish = PhotonView.Find(fishId).gameObject;
        fish.GetComponent<PhotonTransformViewClassic>().enabled = false;
        Hook hook = PhotonView.Find(hookId).GetComponent<Hook>();
        Transform fishParent = hook.wormParent;
        fish.transform.GetComponent<PolygonCollider2D>().enabled = false;
        fish.transform.SetParent(fishParent);
        fish.transform.eulerAngles = new Vector3(0f, 0f, -90f);
        fish.transform.localPosition = Vector3.zero;
        if (catchadeFish)
        {
            hook.CallRpcToReturnRod();
        }
        PhotonNetwork.SendAllOutgoingCommands();
    }



    //call this function in GameManager
    public void CallWinFishRPC()
    {
        if (GS.Instance.isLan)
        {
            if (transform.localScale == Vector3.zero)
            {
                fishController_Mirror.TryWinFish();
            }
        }
        else
        {
            photonView.RPC(nameof(WinFish), RpcTarget.Others);
        }
    }

    //call this function in GameManager
    public void CallAllWinFishRPC()
    {
        photonView.RPC(nameof(WinFish), RpcTarget.All);
    }

    [PunRPC]
    public void WinFish()
    {
        if (photonView.IsMine && !isDead)
        {
            GameManager.Instance.ShowGameOver("You win!");
            canMove = false;
            HungerSystem.Instance.canDecrease = false;
            GetComponent<PolygonCollider2D>().enabled = false;
            isDead = true;
            animator.SetBool("isJoyful", true);
        }
    }

    public void WinFish_mirror()
    {
        if (!isDead)
        {
            GameManager.Instance.ShowGameOver("You win!");
            canMove = false;
            HungerSystem.Instance.canDecrease = false;
            GetComponent<PolygonCollider2D>().enabled = false;
            isDead = true;
            animator.SetBool("isJoyful", true);
        }
    }

    //When Hook is Destroy Call This Function
    public void DestroyCatchFish()
    {
        if (catchadeFish)
        {
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
    void PlaySFX(AudioClip playClip)
    {
        audioSource.clip = playClip;
        GS.Instance.SetSFXVolume(audioSource);
        audioSource.Play();
    }

    //When Game is paused Fish movement is stoped and Fad it.
    public void CallGamePauseRPC(bool isPause)
    {
        int fishViewID = GameManager.Instance.myFish.GetComponent<PhotonView>().ViewID;
        photonView.RPC(nameof(GamePause), RpcTarget.AllBuffered, isPause, fishViewID);
        PhotonNetwork.SendAllOutgoingCommands();
    }

    [PunRPC]
    public void GamePause(bool isPause, int fishID)
    {
        int thisFishID = GetComponent<PhotonView>().ViewID;

        if (thisFishID == fishID)
        {
            if (isPause)
            {
                transform.GetComponent<PolygonCollider2D>().enabled = false;
                var sr = GetComponent<SpriteRenderer>();
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.5f);
                canMove = false;
            }
            else
            {
                transform.GetComponent<PolygonCollider2D>().enabled = true;
                var sr = GetComponent<SpriteRenderer>();
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
                canMove = true;
            }
        }
    }

   

    private void OnApplicationQuit()
    {
        if (!GS.Instance.isLan)
        {
            if (!isDead)
            {
                GameManager.Instance.CallLessPlayerCountRPC();
                PhotonNetwork.SendAllOutgoingCommands(); // send it now
            }
        }
    }

    private int CoinCount = 0;
    private int MaxCoins = 2;

    public void CollectCoin()
    {
        if (SteamManager.Initialized)
        {
            // Update coin stats
            SteamUserStats.GetStat("LEVEL_01_COIN_COUNT", out CoinCount);
            CoinCount++;
            SteamUserStats.SetStat("LEVEL_01_COIN_COUNT", CoinCount);
            SteamUserStats.StoreStats();

            // Check achievement condition
            if (CoinCount >= MaxCoins)
            {
                SteamUserStats.GetAchievement("LEVEL_01_ALL_COINS", out bool achievementCompleted);

                if (!achievementCompleted)
                {
                    SteamUserStats.SetAchievement("LEVEL_01_ALL_COINS");
                    SteamUserStats.StoreStats();
                }
            }
        }
    }

    public void DestroyThisGameobject()
    {
        NetworkServer.Destroy(gameObject);
    }

}
