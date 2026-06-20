using Mirror;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FishermanController : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
{

    public static FishermanController Instance;

    public FishermanController_Mirror fishermanController_Mirror;
    public InputActionReference inputAction;

    public NetworkTransformUnreliable networkTransformUnreliable;

    public GameObject hookPrefab;

    public FishController fishController;


    [Header("Movement")]
    public float moveSpeed;

    [Header("Rod Selection")]
    public Transform leftRod;
    public Transform rightRod;


    public float meterSpeed = 2f;
    public float maxCastDistance = 10f;

    public int catchadFish = 0;

    [Header("Horizontal Bounds")]
    public float minX = -6.5f;
    public float maxX = 6.5f;

    public Animator animator;

    public AudioSource cricketChirping;
    public AudioSource fisherManSounds;
    public AudioSource boatMoveSound;

    public AudioClip throwWorm;
    public AudioClip stopBoat;

    public int catchadeFishID;

    internal GameObject leftHook = null;
    internal GameObject rightHook = null;

    internal Transform currentRod;
    internal Slider castingMeter;
    internal int worms;
    public bool isCasting = false,
                  isCanMove = true,
                  isMoving = false,
                  isFisherMan = false,
                  isCanCast = true,
                  isIdel = false,
                  isRight = false,
                  isLeft = true,
                  meterIncreasing = true;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        GameManager gameManager = GameManager.Instance;
        Debug.Log("FishermanController Start called");
        gameManager.LoadPreloderOnOff(false);

        fishController = GameManager.Instance.myFish;

        if (GS.Instance.isLan)
        {
            if (GameManager.Instance.isFisherMan)
            {
                // Guard the cosmetic + command calls: a Mirror authority warning or a transient
                // null here must never abort Start(), otherwise the catcher would skip the rest of
                // the fisherman setup (and the preloader-hide / fisherManIsSpawned flag) and hang.
                try
                {
                    string hat = CosmeticRuntimeApplier.GetSelectedFishermanHatName();
                    string hair = CosmeticRuntimeApplier.GetSelectedFishermanHairName();
                    CosmeticRuntimeApplier.ApplyFishermanCosmeticsByName(gameObject, hat, hair);
                    if (fishermanController_Mirror != null)
                    {
                        fishermanController_Mirror.CmdSetCosmetics(hat, hair);
                        fishermanController_Mirror.CmdSetDirection(isLeft);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[FishermanController] LAN cosmetic setup failed (continuing): {e.Message}");
                }
            }
        }
        else
        {
            if (GameManager.Instance.isFisherMan && photonView.IsMine)
            {
                string hat = CosmeticRuntimeApplier.GetSelectedFishermanHatName();
                string hair = CosmeticRuntimeApplier.GetSelectedFishermanHairName();
                CosmeticRuntimeApplier.ApplyFishermanCosmeticsByName(gameObject, hat, hair);
                photonView.RPC(nameof(RpcSetFishermanCosmetics), RpcTarget.AllBuffered, hat, hair);
                photonView.RPC(nameof(RpcSetDirection), RpcTarget.Others, isLeft);
            }
        }

        if (gameManager == null)
        {
            Debug.LogError("GameManager instance not found!");
            return;
        }

        // Assign references first before using them

        castingMeter = gameManager.castingMeter;
        worms = gameManager.fishermanWorms;

        if (PhotonNetwork.IsMasterClient || GS.Instance.isLan)
        {
            // Reset casting meter if available
            if (castingMeter != null)
                castingMeter.value = 0;
            else
                Debug.LogWarning("Casting meter reference missing!");

            // Setup fisherman-related visuals and UI
            if (PhotonNetwork.IsMasterClient || GameManager.Instance.isFisherMan)
            {
                gameManager.hungerBar.SetActive(false);
                if (HungerSystem.Instance != null)
                {
                    HungerSystem.Instance.gameObject.SetActive(false);
                    HungerSystem.Instance.canDecrease = false;
                }
                gameManager.fisherManObjects.SetActive(true);
                gameManager.UpdateUI(worms);
            }

            gameManager.LoadPreloderOnOff(false);

            if(GS.Instance.isLan)
            {
                GameManager.Instance.fisherManIsSpawned = true;
                GameManager.Instance.goldWormEatByFish = false;
            }
            else
            {
                gameManager.CallFisherManSpawnedRPC(true);
                GameManager.Instance.goldWormEatByFish = false;
            }


            // Start spawning logic
            

            StartCoroutine(PlayCricketRandomly());

            if(GS.Instance.isLan)
            {
                GameManager.Instance.myFish.fishermanController = this;
                if (JunkSpawner.Instance != null)
                {
                    JunkSpawner.Instance.canSpawn = true;
                    JunkSpawner.Instance.LoadSpawnJunk();
                }
            }
            else
            {
                if (JunkSpawner.Instance != null)
                {
                    JunkSpawner.Instance.canSpawn = true;
                    JunkSpawner.Instance.LoadSpawnJunk();
                }

                if (WormSpawner.Instance != null)
                {
                    WormSpawner.Instance.canSpawn = true;
                    WormSpawner.Instance.LoadSpawnWorm();
                }
                CheckWorms();
            }
        }

        ApplyVisibilityMode(gameManager);

    }

    private void ApplyVisibilityMode(GameManager gameManager)
    {
        if (gameManager == null || GS.Instance == null)
        {
            return;
        }

        if (gameManager.water != null)
        {
            gameManager.water.SetActive(false);
        }

        if (gameManager.sky != null)
        {
            gameManager.sky.SetActive(false);
        }

        //Everyone can see everyone.
        if (GS.Instance.ClearWaters)
        {
            if (PhotonNetwork.IsMasterClient || GameManager.Instance.isFisherMan )
            {
                // Enable background 3
                if (gameManager.water != null)
                    gameManager.water.SetActive(false);
            }
            else
            {
                // Non-master setup
                if (gameManager.sky != null)
                    gameManager.sky.SetActive(false);
            }
        }

        //Both sides hidden (blind match).
        if (GS.Instance.DeepWaters)
        {
            if (PhotonNetwork.IsMasterClient || GameManager.Instance.isFisherMan)
            {
                // Enable background 3
                if (gameManager.water != null)
                    gameManager.water.SetActive(true);
            }
            else
            {
                // Non-master setup
                if (gameManager.sky != null)
                    gameManager.sky.SetActive(false);
            }
        }

        //Fish can see the fisherman, but he can’t see them.
        if (GS.Instance.MurkyWaters)
        {
            

            if (PhotonNetwork.IsMasterClient || GameManager.Instance.isFisherMan)
            {
                // Enable background 3
                if (gameManager.water != null)
                    gameManager.water.SetActive(true);
            }
            else
            {
                // Non-master setup
                if (gameManager.sky != null)
                    gameManager.sky.SetActive(true);
            }
        }

        //Fisherman can see fish, but fish can’t see him.
        if (GS.Instance.ReflectiveWater)
        {
            if (PhotonNetwork.IsMasterClient || GameManager.Instance.isFisherMan)
            {
                // Enable background 3
                if (gameManager.water != null)
                    gameManager.water.SetActive(false);
            }
            else
            {
                // Non-master setup
                if (gameManager.sky != null)
                    gameManager.sky.SetActive(true);
            }
        }
    }

    IEnumerator PlayCricketRandomly()
    {
        while (true)
        {
            float waitBeforePlay = Random.Range(20f, 30f);
            yield return new WaitForSeconds(waitBeforePlay);

            // Play the sound
            GS.Instance.SetSFXVolume(cricketChirping);

            cricketChirping.Play();

            // Random duration to play the sound (2–5 seconds)
            float playDuration = Random.Range(2f, 5f);
            yield return new WaitForSeconds(playDuration);

            // Stop the sound
            cricketChirping.Pause();
        }
    }

    void Update()
    {
        if (GS.Instance.isLan)
        {
            if (!GameManager.Instance.isFisherMan)
            {
                return;
            }
        }
        else
        {
            if (!PhotonNetwork.IsMasterClient || !isCanCast)
                return;
        }

        if (isCanMove)
        {
            FisherManMovement();

            if (!isMoving)
                HandleRodSelection();
        }

        FisherManCastingFish();
    }



    void FisherManMovement()
    {
        if (leftHook == null && rightHook == null && !isCasting)
        {
            // float moveInput = Input.GetAxisRaw("Horizontal");
            float moveInput;
            if (GS.Instance.isLan)
            {
                if (GameManager.Instance.isFisherMan)
                {
                    // move = inputAction.action.ReadValue<Vector2>();
                    float horizontal = Input.GetAxis("Horizontal");

                    moveInput = horizontal; //= new Vector2(horizontal, vertical);

                    if (moveInput > 0)
                    {
                        moveInput = 1;
                    }
                    else if (moveInput < 0)
                    {
                        moveInput = -1;
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                moveInput = inputAction.action.ReadValue<Vector2>().x;
            }


            Vector3 move = new Vector3(moveInput * moveSpeed * Time.deltaTime, 0, 0);
            transform.position += move;

            // Clamp only X position
            Vector3 clampedPos = transform.position;
            clampedPos.x = Mathf.Clamp(clampedPos.x, minX, maxX);
            transform.position = clampedPos;
            isMoving = false;

            if (moveInput != 0)
            {
                if (!boatMoveSound.isPlaying)
                {
                    GS.Instance.SetSFXVolume(boatMoveSound);
                    boatMoveSound.Play();
                }

                isMoving = true;
                isIdel = false;

                // Drop rod when moving
                currentRod = null;

                // Reset fishing triggers (always do this once)
                ResetTriggerSync("leftOurToPole_l");
                ResetTriggerSync("rightOurToPole_r");

                if (isLeft)
                {
                    // Cancel fishing
                    SetBoolSync("fishing_l", false);

                    // Movement (mutually exclusive)
                    SetBoolSync("moveForward_l", moveInput < 0);
                    SetBoolSync("moveBackward_l", moveInput > 0);
                }
                else if (isRight)
                {
                    SetBoolSync("fishing_r", false);
                    SetBoolSync("moveReverceForward_r", moveInput > 0);
                    SetBoolSync("moveReverceBackward_r", moveInput < 0);
                }
            }
            else
            {
                if (boatMoveSound.isPlaying)
                {
                    boatMoveSound.Stop();
                    PlaySFX(stopBoat);
                }

                // No movement → reset all movement states
                if (isLeft)
                {
                    SetBoolSync("moveForward_l", false);
                    SetBoolSync("moveBackward_l", false);
                }
                else if (isRight)
                {
                    SetBoolSync("moveReverceForward_r", false);
                    SetBoolSync("moveReverceBackward_r", false);
                }
            }
        }
    }

    void SelectRoad(Transform rod)
    {
        isIdel = true;
        currentRod = rod;
    }

    void HandleRodSelection()
    {
        float moveInputY;
        if (GS.Instance.isLan)
        {
            if (GameManager.Instance.isFisherMan)
            {
                // move = inputAction.action.ReadValue<Vector2>();
                float vertical = Input.GetAxis("Vertical");

                moveInputY = vertical; //= new Vector2(horizontal, vertical);

                if (moveInputY > 0)
                {
                    moveInputY = 1;
                }
                else if (moveInputY < 0)
                {
                    moveInputY = -1;
                }
            }
            else
            {
                return;
            }
        }
        else
        {
            // move = inputAction.action.ReadValue<Vector2>();
            float vertical = Input.GetAxis("Vertical");

            moveInputY = vertical; //= new Vector2(horizontal, vertical);

            if (moveInputY > 0)
            {
                moveInputY = 1;
            }
            else if (moveInputY < 0)
            {
                moveInputY = -1;
            }
        }

        if (moveInputY == 1)
        {
            //Don't Change Line 
            isLeft = true;
            isRight = false;
            SyncDirectionState();

            if (currentRod != leftRod)
            {
                SetBoolSync("idel_r", false);
                SetBoolSync("idel_l", true);
                SetBoolSync("fishing_l", true);
                SetBoolSync("fishing_r", false);
                SetTriggerSync("leftOurToPole_l");
                SelectRoad(leftRod);
            }
        }
        else if (moveInputY == -1)
        {
            //Don't Change Line 
            isLeft = false;
            isRight = true;
            SyncDirectionState();

            if (currentRod != rightRod)
            {
                SetBoolSync("idel_l", false);
                SetBoolSync("idel_r", true);
                SetBoolSync("fishing_l", false);
                SetBoolSync("fishing_r", true);
                SetTriggerSync("rightOurToPole_r");
                SelectRoad(rightRod);
            }
        }
    }
    bool castReleased = false;

    void FisherManCastingFish()
    {
        // CAST START
        if (!isCasting && Keyboard.current.xKey.isPressed && Keyboard.current.vKey.isPressed)
        {
            Debug.Log("FisherManCastingFish");

            if (currentRod == null)
            {
                GameManager.Instance.messageText.text =
                    "Please select a rod first using the 'W' & 'S' key.";
                return;
            }

            if (Keyboard.current.xKey.wasPressedThisFrame){GameManager.Instance.messageText.text = " ";}
            if (Keyboard.current.vKey.wasPressedThisFrame){GameManager.Instance.messageText.text = " ";}

            if (leftHook != null || rightHook != null)
            {
                Debug.Log("Rod already has a hook!");
                return;
            }

            isCasting = true;
            castReleased = false; // reset
            StartCoroutine(CastMeterRoutine());
        }

        // CAST RELEASE (ONLY ONCE)
        if (isCasting && !castReleased && (Keyboard.current.xKey.wasReleasedThisFrame || Keyboard.current.vKey.wasReleasedThisFrame))
        {
            castReleased = true;
            LoadReleaseCast();
        }
    }


    IEnumerator CastMeterRoutine()
    {
        while (isCasting)
        {
            if (meterIncreasing)
            {
                castingMeter.value += Time.deltaTime * meterSpeed;
                if (castingMeter.value >= 1f) meterIncreasing = false;
            }
            else
            {
                castingMeter.value -= Time.deltaTime * meterSpeed;
                if (castingMeter.value <= 0f) meterIncreasing = true;
            }
            yield return null;
        }
    }

    public void LoadReleaseCast()
    {
        StartCoroutine(ReleaseCast());
    }

    IEnumerator ReleaseCast()
    {
        Debug.Log("ReleaseCast");
        isCasting = false;
        isCanMove = false;

        PlaySFX(throwWorm);

        StopCoroutine(CastMeterRoutine());


        if (currentRod == leftRod)
        {
            SetTriggerSync("casting_l");
        }
        else
        {
            SetTriggerSync("casting_r");
        }

        yield return new WaitForSeconds(0.5f);

        Hook hook;
        hook = null;


        if (GS.Instance.isLan)
        {
            hook = fishermanController_Mirror.hook;
            if (hook.hook_Mirror != null)
            {
                hook.hook_Mirror.RpcSetJunkRod(currentRod.position);
            }
            hook.TryToSetJunkRod(currentRod.position);
            float castDistance = castingMeter.value * maxCastDistance;
            fishermanController_Mirror.hook.LaunchDownWithDistance(castDistance, currentRod);
        }
        else
        {
            hook = PhotonNetwork.Instantiate(hookPrefab.name, currentRod.position, Quaternion.identity).GetComponent<Hook>();
            int hookID = hook.GetComponent<PhotonView>().ViewID;
            hook.AttachWorm();
            float castDistance = castingMeter.value * maxCastDistance;
            hook.LaunchDownWithDistance(castDistance, currentRod);
        }


        if (hook != null)
        {
            if (currentRod == leftRod)
            {
                leftHook = hook.gameObject;
            }
            else
            {
                rightHook = hook.gameObject;
            }

            if (worms > 0)
            {
                worms--;
                GameManager.Instance.UpdateUI(worms);
            }
            if (GS.Instance != null && GameManager.Instance.isFisherMan)
            {
                GS.Instance.currentRoundWormsUsed++;
            }

            castingMeter.value = 0;
        }
        else
        {
            Debug.Log("Hook is null");
        }

    }

   /* [PunRPC]
    void SetupHookRodRPC(int hookID, Vector3 curruntRod)
    {
        PhotonView hookView = PhotonView.Find(hookID);

        if (hookView != null)
        {
            Hook hook = hookView.GetComponent<Hook>();
            if (hook != null)
            {
                hook.rodTip = curruntRod;
            }

        }
    }*/

    public void ClearHookReference(GameObject hook)
    {
        if (hook == leftHook) leftHook = null;
        if (hook == rightHook) rightHook = null;
    }



    // Check worms and print lose message
    public void CheckWorms()
    {
        Debug.Log("catchadFish = " + catchadFish + " GameManager.instance.totalPlayers = " + GameManager.Instance.totalPlayers);
        if (catchadFish >= (GameManager.Instance.totalPlayers - 1))
        {
            if (GameManager.Instance != null && GameManager.Instance.gameOverText != null)
            {
                PlayWinAnimation();

                Debug.Log("Fisherman Win!");

                if (GS.Instance.isLan)
                {
                    if (GameManager.Instance.myFish.isFisherMan)
                    {
                        GameManager.Instance.ShowGameOver("Fisherman Win!");
                        GameManager.Instance.TriggerRoundEnd("Fisherman Win!");
                    }

                    if(GS.Instance.IsMirrorMasterClient)
                    {
                        WormSpawner.Instance.EnableWormDaceAnimation();
                    }
                }
                else
                {
                    GameManager.Instance.CallCoverBGDisableRPC();
                    WormSpawner.Instance.EnableWormDaceAnimation();
                    GameManager.Instance.ShowGameOver("Fisherman Win!");
                    GameManager.Instance.TriggerRoundEnd("Fisherman Win!");
                    if (!GS.Instance.isMasterClient)
                    {
                        CallSetOldMaster();
                    }
                }
            }
            WormSpawner.Instance.canSpawn = isCanMove = false;

            // Optional: stop all fishing actions
            leftHook = null;
            rightHook = null;
            isCasting = false;
            return;
        }


        //When Worm is over fisher man is loss and fishes are wins
        if (worms <= 0)
        {
            if (GameManager.Instance != null && GameManager.Instance.gameOverText != null)
            {
                OnCryingAnimation(true);
                GameManager.Instance.ShowGameOver("Fisherman Lose!\nFishes Win!");
                GameManager.Instance.TriggerRoundEnd("Fisherman Lose!\nFishes Win!");
            }

            WormSpawner.Instance.canSpawn = isCanMove = false;

            GameManager.Instance.WinFish_Mirror();

            // Optional: stop all fishing actions
            leftHook = null;
            rightHook = null;
            isCasting = false;

        }
    }

    public void CallSetOldMaster()
    {
        photonView.RPC(nameof(SetOldMaster), RpcTarget.All);
    }


    public void SetTriggerSync(string triggerName)
    {
        if (animator != null) animator.SetTrigger(triggerName);
        if (GS.Instance.isLan)
        {
            if (fishermanController_Mirror != null) fishermanController_Mirror.CallSetTrigger_Mirror(triggerName);
        }
        else
        {
            if (PhotonNetwork.InRoom) photonView.RPC(nameof(RpcSetTrigger), RpcTarget.Others, triggerName);
        }
    }

    [PunRPC]
    public void RpcSetTrigger(string triggerName)
    {
        if (animator != null) animator.SetTrigger(triggerName);
    }

    public void ResetTriggerSync(string triggerName)
    {
        if (animator != null) animator.ResetTrigger(triggerName);
        if (GS.Instance.isLan)
        {
            if (fishermanController_Mirror != null) fishermanController_Mirror.CallResetTrigger_Mirror(triggerName);
        }
        else
        {
            if (PhotonNetwork.InRoom) photonView.RPC(nameof(RpcResetTrigger), RpcTarget.Others, triggerName);
        }
    }

    [PunRPC]
    public void RpcResetTrigger(string triggerName)
    {
        if (animator != null) animator.ResetTrigger(triggerName);
    }

    public void SetBoolSync(string boolName, bool value)
    {
        if (animator != null) animator.SetBool(boolName, value);
        if (GS.Instance.isLan)
        {
            if (fishermanController_Mirror != null) fishermanController_Mirror.CallSetBool_Mirror(boolName, value);
        }
        else
        {
            if (PhotonNetwork.InRoom) photonView.RPC(nameof(RpcSetBool), RpcTarget.Others, boolName, value);
        }
    }

    [PunRPC]
    public void RpcSetBool(string boolName, bool value)
    {
        if (animator != null) animator.SetBool(boolName, value);
    }

    /// <summary>
    /// Syncs the fisherman's facing direction (isLeft/isRight) to all remote clients
    /// so that hat cosmetics display on the correct side.
    /// </summary>
    public void SyncDirectionState()
    {
        if (GS.Instance.isLan)
        {
            if (fishermanController_Mirror != null)
                fishermanController_Mirror.CmdSetDirection(isLeft);
        }
        else
        {
            if (PhotonNetwork.InRoom)
                photonView.RPC(nameof(RpcSetDirection), RpcTarget.Others, isLeft);
        }
    }

    [PunRPC]
    public void RpcSetDirection(bool syncedIsLeft)
    {
        isLeft = syncedIsLeft;
        isRight = !syncedIsLeft;
    }

    [PunRPC]
    public void SetOldMaster()
    {
        if(GS.Instance.isMasterClient)
        {
            int myId = PhotonNetwork.LocalPlayer.ActorNumber;
            Debug.Log("✅ My Client ID = " + myId);
            photonView.RPC(nameof(ChangeHostById), RpcTarget.MasterClient, myId);
        }
    }

    public void GetIdAndChangeHost()
    {
        int myId = PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log("✅ My Client ID = " + myId);

        photonView.RPC(nameof(ChangeHostById), RpcTarget.MasterClient, myId);
    }


    [PunRPC]
    public void ChangeHostById(int clientId)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("❌ Sirf current MasterClient hi host change kar sakta hai!");
            return;
        }

        Player targetPlayer = null;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == clientId)
            {
                targetPlayer = p;
                break;
            }
        }

        if (targetPlayer != null)
        {
            Debug.Log("✅ Host changed to Player with ID: " + clientId);
            PhotonNetwork.SetMasterClient(targetPlayer);
        }
        else
        {
            Debug.LogWarning("❌ Client ID not found: " + clientId);
        }
    }

    public void OnFishGoatAnimation(bool res)
    {
        if (isRight)
        {
            Debug.Log("OnFishGoatAnimation called =" + res);
            SetBoolSync("fishGotFacing_r", res);
        }
        else if (isLeft)
        {
            Debug.Log("OnFishGoatAnimation called =" + res);
            SetBoolSync("fishGotFacing_l", res);
        }
    }
    public void OnCryingAnimation(bool res)
    {
        if (isRight)
        {
            SetBoolSync("isCrying_r", res);
        }
        else if (isLeft)
        {
            SetBoolSync("isCrying_l", res);
        }
    }

    public void PlayWinAnimation()
    {
        isCanMove = false;
        isCanCast = false;
        isCasting = false;

        SetBoolSync("isCrying_r", false);
        SetBoolSync("isCrying_l", false);
        SetBoolSync("isFighting_r", false);
        SetBoolSync("isFighting_l", false);
        SetBoolSync("fishing_r", false);
        SetBoolSync("fishing_l", false);
        SetBoolSync("idel_r", false);
        SetBoolSync("idel_l", false);
        SetBoolSync("moveForward_l", false);
        SetBoolSync("moveBackward_l", false);
        SetBoolSync("moveReverceForward_r", false);
        SetBoolSync("moveReverceBackward_r", false);

        if (isRight)
        {
            SetBoolSync("isWin_r", true);
        }
        else
        {
            SetBoolSync("isWin_l", true);
        }
    }

    public void OnFightAnimation(bool res)
    {
        Debug.Log("OnFightAnimation called =" + res);
        if (isRight)
        {
            SetBoolSync("isFighting_r", res);
        }
        else if (isLeft)
        {
            SetBoolSync("isFighting_l", res);
        }
    }

    [PunRPC]
    public void RpcSetFishermanCosmetics(string hatName, string hairName)
    {
        CosmeticRuntimeApplier.ApplyFishermanCosmeticsByName(gameObject, hatName, hairName);
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = info.photonView != null ? info.photonView.InstantiationData : null;
        if (data == null || data.Length == 0)
        {
            return;
        }

        string hatName = data[0] as string;
        string hairName = data.Length > 1 ? data[1] as string : string.Empty;
        CosmeticRuntimeApplier.ApplyFishermanCosmeticsByName(gameObject, hatName, hairName);
    }

    internal void OnReeling()
    {
        if (isRight)
        {
            SetTriggerSync("isReeling_r");
        }
        else if (isLeft)
        {
            SetTriggerSync("isReeling_l");
        }
    }

    internal void PlaySFX(AudioClip playClip)
    {
        fisherManSounds.clip = playClip;
        GS.Instance.SetSFXVolume(fisherManSounds);
        fisherManSounds.Play();
    }

  
}
