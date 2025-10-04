using Photon.Chat.Demo;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FishermanController : MonoBehaviourPunCallbacks
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Rod Selection")]
    public Transform leftRod;
    public Transform rightRod;

    public Transform currentRod;

    [Header("Casting")]
    public KeyCode castKey1 = KeyCode.X;
    public KeyCode castKey2 = KeyCode.V;
    public Slider castingMeter;       // UI Slider (0-1)
    public float meterSpeed = 2f;
    public float maxCastDistance = 10f; // max distance hook can go

    [Header("Worms")]
    internal int worms;
    public int catchadFish = 0;

    internal bool isCasting = false;
    internal bool isCanMove = true;
    internal bool isMoving = false;
    internal bool isFisherMan = false;
    internal bool isCanCast = true;
    internal bool isIdel = false, isRight = false, isLeft = true;
    private bool meterIncreasing = true;


    [HideInInspector] public GameObject leftHook = null;
    [HideInInspector] public GameObject rightHook = null;

    [Header("Horizontal Bounds")]
    public float minX = -8f;
    public float maxX = 8f;

    public Animator animator;

    public static FishermanController instance;

    public int catchadeFishID;

    public float valc = 0;

    //this audio is play random time
    public AudioSource cricketChirping;

    public AudioSource fisherManSounds;

    public AudioSource boatMoveSound;

    public AudioClip throwWorm;
    public AudioClip stopBoat;

    private void Awake()
    {
        if (instance == null)
            instance = this;

    }
    void Start()
    {

        if (PhotonNetwork.IsMasterClient)
        {
            if (castingMeter != null)
            {
                castingMeter.value = 0;
            }
            castingMeter = GameManager.instance.castingMeter;
            worms = GameManager.instance.fishermanWorms;
            GameManager.instance.hungerBar.SetActive(false);
            GameManager.instance.fisherManObjects.SetActive(true);
            GameManager.instance.LoadPreloderOnOff(false);
            JunkSpawner.instance.canSpawn = true;
            WormSpawner.instance.canSpawn = true;
            JunkSpawner.instance.LoadSpawnJunk();
            WormSpawner.instance.LoadSpawnWorm();
            GameManager.instance.UpdateUI(worms);

            StartCoroutine(PlayCricketRandomly());
            GameManager.instance.CallFisherManSpawnedRPC(true);

        }

    }

    IEnumerator PlayCricketRandomly()
    {
        while (true)
        {
            // Random wait before playing sound (20–30 seconds)
            float waitBeforePlay = Random.Range(20f, 30f);
            yield return new WaitForSeconds(waitBeforePlay);

            // Play the sound
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
        if (PhotonNetwork.IsMasterClient)
        {
            if (isCanCast)
            {
                if (isCanMove)
                {
                    FisherManMovement();
                    if (!isMoving)
                    {
                        HandleRodSelection();
                    }
                }
                HandleCasting();
            }
        }
    }
    void FisherManMovement()
    {
        if (leftHook == null && rightHook == null && !isCasting)
        {
            float moveInput = Input.GetAxisRaw("Horizontal");
            Vector3 move = new Vector3(moveInput * moveSpeed * Time.deltaTime, 0, 0);
            transform.position += move;

            // Clamp only X position
            Vector3 clampedPos = transform.position;
            clampedPos.x = Mathf.Clamp(clampedPos.x, minX, maxX);
            transform.position = clampedPos;

            isMoving = false;

            Debug.Log("moveInput = "+moveInput);

            if (moveInput != 0)
            {
                if (!boatMoveSound.isPlaying)
                {
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
        if (Input.GetKeyDown(KeyCode.W))
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
        else if (Input.GetKeyDown(KeyCode.S))
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
        if (!isCasting && Input.GetKey(castKey1) && Input.GetKey(castKey2))
        {
            if (currentRod != null)
            {
                if (GameManager.instance.messageText.text != "")
                {
                    GameManager.instance.messageText.text = "";
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
                GameManager.instance.messageText.text = "Please select a rod first using the 'W' or 'S' key.";
                Debug.Log("Please select a rod first using the 'W' or 'S' key.");
                return;
            }

            // Release → cast hook with meter value

        }

        if (worms > 0)
        {
            if (isCasting && (!Input.GetKey(castKey1) || !Input.GetKey(castKey2)))
            {
                ReleaseCast();
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

    void ReleaseCast()
    {
        isCasting = false;
        isCanMove = false;

        PlaySFX(throwWorm);

        StopCoroutine(CastMeterRoutine());

        Hook hook = PhotonNetwork.Instantiate("hookPrefab", currentRod.position, Quaternion.identity).GetComponent<Hook>();

        if (currentRod == leftRod)
        {
            animator.SetTrigger("casting_l");
        }
        else
        {
            animator.SetTrigger("casting_r");
        }

        if (hook != null)
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
            GameManager.instance.UpdateUI(worms);
            Debug.Log("Worm used! Remaining: " + worms);
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
        int curruntPlayer = PhotonNetwork.CurrentRoom.PlayerCount;
        Debug.Log("catchadFish = "+ catchadFish + " GameManager.instance.totalPlayers = " + GameManager.instance.totalPlayers );
        //when fisherma target achived , he win 
        if (catchadFish >= (GameManager.instance.totalPlayers - 1))
        {
            if (catchadFish > 0)
            {
                if (GameManager.instance != null && GameManager.instance.gameOverText != null)
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
                    GameManager.instance.ShowGameOver("Fisherman Win!");
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
                GameManager.instance.ShowGameOver("GameOver!");
            }
            WormSpawner.instance.canSpawn = isCanMove = false;

            // Optional: stop all fishing actions
            leftHook = null;
            rightHook = null;
            isCasting = false;
            return;
        }
        

        //When Worm is over fisher man is loss and fishes are wins
        if (worms <= 0)
        {
            if (GameManager.instance != null && GameManager.instance.gameOverText != null)
            {
                if (isRight)
                {
                    animator.SetBool("isCrying_r", true);
                }
                else if (isLeft)
                {
                    animator.SetBool("isCrying_l", true);
                }
                GameManager.instance.ShowGameOver("Fisherman Lose!\nFishes Win!");
            }

            WormSpawner.instance.canSpawn = isCanMove = false;
            for (int i = 0; i < GameManager.instance.allFishes.Count; i++)
            {
                if (GameManager.instance.allFishes[i] != null)
                {
                    GameManager.instance.allFishes[i].CallWinFishRPC();
                }
            }

            // Optional: stop all fishing actions
            leftHook = null;
            rightHook = null;
            isCasting = false;

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
            PhotonNetwork.SetMasterClient(targetPlayer);
            Debug.Log("✅ Host changed to Player with ID: " + clientId);
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

    public void OnFightAnimation(bool res)
    {
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
        fisherManSounds.Play();
    }

}
