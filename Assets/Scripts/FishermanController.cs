using Mirror;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class FishermanController : MonoBehaviourPunCallbacks
{

    public static FishermanController Instance;

    public FishermanController_Mirror fishermanController_Mirror;
    public InputActionReference inputAction;

    public NetworkTransformUnreliable networkTransformUnreliable;

    public GameObject hookPrefab;

    [Header("Movement")]
    public float moveSpeed;

    [Header("Rod Selection")]
    public Transform leftRod;
    public Transform rightRod;


    public float meterSpeed = 2f;
    public float maxCastDistance = 10f;

    public int catchadFish = 0;

    [Header("Horizontal Bounds")]
    public float minX = -8f;
    public float maxX = 8f;

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
                gameManager.fisherManObjects.SetActive(true);
                gameManager.UpdateUI(worms);
            }

            gameManager.LoadPreloderOnOff(false);

            if(GS.Instance.isLan)
            {
                GameManager.Instance.fisherManIsSpawned = true;
            }
            else
            {
                gameManager.CallFisherManSpawnedRPC(true);
            }


            // Start spawning logic
            

            StartCoroutine(PlayCricketRandomly());

            if(GS.Instance.isLan)
            {
                if(GameManager.Instance.isFisherMan)
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

        //Everyone can see everyone.
        if (GS.Instance.AllVisible)
        {
            if (PhotonNetwork.IsMasterClient || GS.Instance.IsMirrorMasterClient )
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
            if (PhotonNetwork.IsMasterClient || GS.Instance.IsMirrorMasterClient)
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

        //Fish can see the fisherman, but he can’t see them.
        if (GS.Instance.MurkyWaters)
        {
            if (PhotonNetwork.IsMasterClient || GS.Instance.IsMirrorMasterClient)
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

        //Fisherman can see fish, but fish can’t see him.
        if (GS.Instance.ClearWaters)
        {
            if (PhotonNetwork.IsMasterClient || GS.Instance.IsMirrorMasterClient)
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
            GS.Instance.SetVolume(cricketChirping);

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

        HandleCasting();
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
            Debug.Log(move);
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
                    GS.Instance.SetVolume(boatMoveSound);
                    boatMoveSound.Play();
                }

                isMoving = true;
                isIdel = false;

                // Drop rod when moving
                currentRod = null;

                // Reset fishing triggers (always do this once)
                animator.ResetTrigger("leftOurToPole_l");
                animator.ResetTrigger("rightOurToPole_r");

                if (isLeft)
                {
                    // Cancel fishing
                    animator.SetBool("fishing_l", false);

                    // Movement (mutually exclusive)
                    animator.SetBool("moveForward_l", moveInput < 0);
                    animator.SetBool("moveBackward_l", moveInput > 0);
                }
                else if (isRight)
                {
                    animator.SetBool("fishing_r", false);
                    animator.SetBool("moveReverceForward_r", moveInput > 0);
                    animator.SetBool("moveReverceBackward_r", moveInput < 0);
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
                    animator.SetBool("moveForward_l", false);
                    animator.SetBool("moveBackward_l", false);
                }
                else if (isRight)
                {
                    animator.SetBool("moveReverceForward_r", false);
                    animator.SetBool("moveReverceBackward_r", false);
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
                Debug.Log("moveInputY" + moveInputY); 
            }
            else
            {
                return;
            }
        }
        else
        {
            moveInputY = inputAction.action.ReadValue<Vector2>().x;
        }

        if (moveInputY == 1)
        {
            //Don't Change Line 
            isLeft = true;
            isRight = false;

            if (currentRod != leftRod)
            {
                animator.SetBool("idel_r", false);
                animator.SetBool("idel_l", true);
                animator.SetBool("fishing_l", true);
                animator.SetBool("fishing_r", false);
                animator.SetTrigger("leftOurToPole_l");
                SelectRoad(leftRod);
            }
        }
        else if (moveInputY == -1)
        {
            //Don't Change Line 
            isLeft = false;
            isRight = true;

            if (currentRod != rightRod)
            {
                animator.SetBool("idel_l", false);
                animator.SetBool("idel_r", true);
                animator.SetBool("fishing_l", false);
                animator.SetBool("fishing_r", true);
                animator.SetTrigger("rightOurToPole_r");
                SelectRoad(rightRod);
            }
        }
    }
    void HandleCasting()
    {
        if (!isCasting && Keyboard.current.xKey.isPressed && Keyboard.current.vKey.isPressed)
        {
            if (currentRod != null)
            {
                if (GameManager.Instance.messageText.text != "")
                {
                    GameManager.Instance.messageText.text = "";
                }

                if ((leftHook != null) || (rightHook != null))
                {
                    Debug.Log("Rod already has a hook!");
                    return;
                }

                isCasting = true;
                StartCoroutine(CastMeterRoutine());
            }
            else
            {
                GameManager.Instance.messageText.text = "Please select a rod first using the 'W' & 'S' key.";
                return;
            }
        }

        if (worms > 0)
        {
            if (isCasting && Keyboard.current.xKey.wasReleasedThisFrame || isCasting && Keyboard.current.vKey.wasReleasedThisFrame)
            {
                StartCoroutine(ReleaseCast());
            }
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

    IEnumerator ReleaseCast()
    {
        Debug.Log("ReleaseCast");
        isCasting = false;
        isCanMove = false;

        PlaySFX(throwWorm);

        StopCoroutine(CastMeterRoutine());


        if (currentRod == leftRod)
        {
            animator.SetTrigger("casting_l");
        }
        else
        {
            animator.SetTrigger("casting_r");
        }

        yield return new WaitForSeconds(0.5f);

        Hook hook = new Hook();
        if(GS.Instance.isLan)
        {
            GameObject temphook = Instantiate(hookPrefab, currentRod.position, Quaternion.identity);
            NetworkServer.Spawn(temphook.gameObject);
            hook = temphook.GetComponent<Hook>();
        }
        else
        {
             hook = PhotonNetwork.Instantiate(hookPrefab.name, currentRod.position, Quaternion.identity).GetComponent<Hook>();
        }

        if (hook != null)
        {
            if (GS.Instance.isLan)
            {
                fishermanController_Mirror.TryToSetJunkRod(hook.GetComponent<NetworkIdentity>(), currentRod.position);

               // hook.rodTip = currentRod.position;

                // Automatic worm attach
                hook.AttachWorm();

                // Launch hook based on meter value
                float castDistance = castingMeter.value * maxCastDistance;
                hook.LaunchDownWithDistance(castDistance);

            }
            else
            {
                int hookID = hook.GetComponent<PhotonView>().ViewID;

                // Send RPC to all clients to set rodTip
                photonView.RPC(nameof(SetupHookRodRPC), RpcTarget.AllBuffered, hookID, currentRod.position);

                hook.rodTip = currentRod.position;

                // Automatic worm attach
                hook.AttachWorm();

                // Launch hook based on meter value
                float castDistance = castingMeter.value * maxCastDistance;
                hook.LaunchDownWithDistance(castDistance);
            }

        }
        else
        {
            Debug.Log("Hook is null");
        }

        // Track hook per rod
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

        castingMeter.value = 0;
    }


    [PunRPC]
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
    }

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
            if (catchadFish > 0)
            {
                if (GameManager.Instance != null && GameManager.Instance.gameOverText != null)
                {
                    if (isRight)
                    {
                        animator.SetBool("isWin_r", true);
                    }
                    else if (isLeft)
                    {
                        animator.SetBool("isWin_l", true);
                    }
                    else
                    {
                        animator.SetBool("isWin_r", true);
                    }

                    Debug.Log("Fisherman Win!");
                    GameManager.Instance.ShowGameOver("Fisherman Win!");
                    GameManager.Instance.CallCoverBGDisableRPC();
                    WormSpawner.Instance.EnableWormDaceAnimation();

                    if(!GS.Instance.isMasterClient)
                    {
                        CallSetOldMaster();
                    }
                }
            }
            else
            {
                if (isRight)
                {
                    animator.SetBool("isCrying_r", true);
                }
                else if (isLeft)
                {
                    animator.SetBool("isCrying_l", true);
                }
                else
                {
                    animator.SetBool("isWin_r", true);
                }
                GameManager.Instance.ShowGameOver("GameOver!");
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
            }

            WormSpawner.Instance.canSpawn = isCanMove = false;
            for (int i = 0; i < GameManager.Instance.allFishes.Count; i++)
            {
                if (GameManager.Instance.allFishes[i] != null)
                {
                    GameManager.Instance.allFishes[i].CallWinFishRPC();
                }
            }

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
            animator.SetBool("fishGotFacing_r", res);
        }
        else if (isLeft)
        {
            animator.SetBool("fishGotFacing_l", res);
        }
    }
    public void OnCryingAnimation(bool res)
    {
        if (isRight)
        {
            animator.SetBool("isCrying_r", res);
        }
        else if (isLeft)
        {
            animator.SetBool("isCrying_l", res);
        }
    }

    public void OnFightAnimation(bool res)
    {
        Debug.Log("OnFightAnimation called =" + res);
        if (isRight)
        {
            animator.SetBool("isFighting_r", res);
        }
        else if (isLeft)
        {
            animator.SetBool("isFighting_l", res);
        }
    }

    internal void OnReeling()
    {
        if (isRight)
        {
            animator.SetTrigger("isReeling_r");
        }
        else if (isLeft)
        {
            animator.SetTrigger("isReeling_l");
        }
    }

    internal void PlaySFX(AudioClip playClip)
    {
        fisherManSounds.clip = playClip;
        GS.Instance.SetVolume(fisherManSounds);
        fisherManSounds.Play();
    }

    
}