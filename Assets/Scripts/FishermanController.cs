using Mirror;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    public FishController fishController;


    [Header("Movement")]
    public float moveSpeed;

    [Header("Rod Selection")]
    public Transform leftRod;
    public Transform rightRod;

    private static readonly Vector3 DefaultLeftRodLocalPosition = new Vector3(-1f, 0.675f, 0f);
    private static readonly Vector3 DefaultRightRodLocalPosition = new Vector3(1f, 0.675f, 0f);
    private static readonly Vector3 DefaultLeftRodLocalScale = new Vector3(1f, -1f, -1f);


    public float meterSpeed = 2f;
    public float maxCastDistance = 10f;

    public int catchadFish = 0;

    [Header("Horizontal Bounds")]
    public float minX = -8f;
    public float maxX = 8f;

    public Animator animator;

    [Header("Animator Parameters")]
    [SerializeField] private string fightingLeftParam = "isFighting_l";
    [SerializeField] private string fightingRightParam = "isFighting_r";
    [SerializeField] private string fishingLeftParam = "fishing_l";
    [SerializeField] private string fishingRightParam = "fishing_r";
    [SerializeField] private string idleLeftParam = "idel_l";
    [SerializeField] private string idleRightParam = "idel_r";
    [SerializeField] private string castingLeftTrigger = "casting_l";
    [SerializeField] private string castingRightTrigger = "casting_r";

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
    private FishermanChildAnimatorSync childAnimatorSync;
    private int lastRodInputDirection = 1; // 1=left rod (W/up), -1=right rod (S/down)
    private readonly Dictionary<string, bool> animatorBoolCache = new Dictionary<string, bool>();
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

        // Reset any stuck animation states from previous games
        childAnimatorSync = GetComponent<FishermanChildAnimatorSync>();
        ResetAnimationStates();
        ResolveRodReferences();
        EnsureFishermanRenderersVisible();

        fishController = GameManager.Instance.myFish;

        if (GS.Instance.isLan)
        {
            if (GameManager.Instance.isFisherMan)
            {
                string hat = PlayerPrefs.GetString(CosmeticRuntimeApplier.SelectedFishermanHatPrefKey, string.Empty);
                string hair = PlayerPrefs.GetString(CosmeticRuntimeApplier.SelectedFishermanHairPrefKey, string.Empty);
                if (fishermanController_Mirror != null) fishermanController_Mirror.CmdSetCosmetics(hat, hair);
            }
        }
        else
        {
            if (GameManager.Instance.isFisherMan && photonView.IsMine)
            {
                string hat = PlayerPrefs.GetString(CosmeticRuntimeApplier.SelectedFishermanHatPrefKey, string.Empty);
                string hair = PlayerPrefs.GetString(CosmeticRuntimeApplier.SelectedFishermanHairPrefKey, string.Empty);
                photonView.RPC(nameof(RpcSetFishermanCosmetics), RpcTarget.AllBuffered, hat, hair);
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
            HandleRodSelection();
            FisherManMovement();
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

                // Reset fishing triggers (always do this once)
                animator.ResetTrigger("leftOurToPole_l");
                animator.ResetTrigger("rightOurToPole_r");

                if (isLeft)
                {
                    // Cancel fishing
                    SetAnimatorBool("fishing_l", false);

                    // Movement (mutually exclusive)
                    SetAnimatorBool("moveForward_l", moveInput < 0);
                    SetAnimatorBool("moveBackward_l", moveInput > 0);
                }
                else if (isRight)
                {
                    SetAnimatorBool("fishing_r", false);
                    SetAnimatorBool("moveReverceForward_r", moveInput > 0);
                    SetAnimatorBool("moveReverceBackward_r", moveInput < 0);
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
                    SetAnimatorBool("moveForward_l", false);
                    SetAnimatorBool("moveBackward_l", false);
                }
                else if (isRight)
                {
                    SetAnimatorBool("moveReverceForward_r", false);
                    SetAnimatorBool("moveReverceBackward_r", false);
                }

                RestoreSelectedRodPose();
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
        if (isCasting || !isCanCast || !isCanMove) return;

        float moveInputY;
        if (GS.Instance.isLan)
        {
            if (GameManager.Instance.isFisherMan)
            {
                // move = inputAction.action.ReadValue<Vector2>();
                float vertical = Input.GetAxisRaw("Vertical");

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
            float vertical = Input.GetAxisRaw("Vertical");

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

        bool upPressed = Keyboard.current != null && (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed);
        bool downPressed = Keyboard.current != null && (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed);

        if (upPressed && !downPressed)
        {
            moveInputY = 1f;
            lastRodInputDirection = 1;
        }
        else if (downPressed && !upPressed)
        {
            moveInputY = -1f;
            lastRodInputDirection = -1;
        }
        else if (upPressed && downPressed)
        {
            moveInputY = lastRodInputDirection;
        }

        if (moveInputY == 1)
        {
            //Don't Change Line 
            isLeft = true;
            isRight = false;

            bool changedRod = currentRod != leftRod;
            SetAnimatorBool("idel_r", false);
            SetAnimatorBool("idel_l", true);
            SetAnimatorBool("fishing_l", false);
            SetAnimatorBool("fishing_r", false);

            if (changedRod)
            {
                SetAnimatorTrigger("leftOurToPole_l");
            }

            SelectRoad(leftRod);
            EnsureRodChildVisible();
        }
        else if (moveInputY == -1)
        {
            //Don't Change Line 
            isLeft = false;
            isRight = true;

            bool changedRod = currentRod != rightRod;
            SetAnimatorBool("idel_l", false);
            SetAnimatorBool("idel_r", true);
            SetAnimatorBool("fishing_l", false);
            SetAnimatorBool("fishing_r", false);

            if (changedRod)
            {
                SetAnimatorTrigger("rightOurToPole_r");
            }

            SelectRoad(rightRod);
            EnsureRodChildVisible();
        }
    }

    private void RestoreSelectedRodPose()
    {
        if (animator == null || currentRod == null)
        {
            return;
        }

        if (currentRod == leftRod)
        {
            isLeft = true;
            isRight = false;
            SetAnimatorBool("idel_r", false);
            SetAnimatorBool("idel_l", true);
            SetAnimatorBool("fishing_l", leftHook != null);
            SetAnimatorBool("fishing_r", false);
        }
        else if (currentRod == rightRod)
        {
            isLeft = false;
            isRight = true;
            SetAnimatorBool("idel_l", false);
            SetAnimatorBool("idel_r", true);
            SetAnimatorBool("fishing_l", false);
            SetAnimatorBool("fishing_r", rightHook != null);
        }
    }

    private void ResolveRodReferences()
    {
        leftRod = ResolveRodReference(leftRod, "Left Rod", DefaultLeftRodLocalPosition, DefaultLeftRodLocalScale);
        rightRod = ResolveRodReference(rightRod, "Right Rod", DefaultRightRodLocalPosition, Vector3.one);
    }

    private Transform ResolveRodReference(Transform existingRod, string rodName, Vector3 localPosition, Vector3 localScale)
    {
        if (existingRod != null)
        {
            return existingRod;
        }

        Transform found = transform.Find(rodName);
        if (found == null)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (string.Equals(child.name, rodName, System.StringComparison.OrdinalIgnoreCase))
                {
                    found = child;
                    break;
                }
            }
        }

        if (found == null)
        {
            GameObject rodObject = new GameObject(rodName);
            rodObject.layer = gameObject.layer;
            found = rodObject.transform;
            found.SetParent(transform, false);
            found.localPosition = localPosition;
            found.localRotation = Quaternion.identity;
            found.localScale = localScale;
        }

        return found;
    }

    public void EnsureFishermanRenderersVisible()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = renderers[i];
            if (spriteRenderer == null)
            {
                continue;
            }

            spriteRenderer.enabled = true;
            Color color = spriteRenderer.color;
            spriteRenderer.color = new Color(color.r, color.g, color.b, 1f);
            spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 15);
        }
    }

    /// <summary>
    /// Ensures the "road" (rod) child GameObject's SpriteRenderer is enabled and visible.
    /// Called during casting and fishing states so the rod appears in the fisherman's hand.
    /// </summary>
    public void EnsureRodChildVisible()
    {
        Transform roadChild = transform.Find("road");
        if (roadChild == null)
        {
            // Case-insensitive fallback
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (string.Equals(child.name, "road", System.StringComparison.OrdinalIgnoreCase))
                {
                    roadChild = child;
                    break;
                }
            }
        }

        if (roadChild == null)
        {
            roadChild = transform.Find("Rod");
        }

        if (roadChild != null)
        {
            roadChild.gameObject.SetActive(true);
            SpriteRenderer[] renderers = roadChild.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer sr = renderers[i];
                if (sr == null) continue;
                sr.enabled = true;
                Color c = sr.color;
                sr.color = new Color(c.r, c.g, c.b, 1f);
                sr.sortingOrder = Mathf.Max(sr.sortingOrder, 16);
            }

            Animator[] animators = roadChild.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator a = animators[i];
                if (a == null) continue;
                a.enabled = true;
            }
        }
    }
    bool castReleased = false;

    void FisherManCastingFish()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        bool xHeld = Keyboard.current.xKey.isPressed;
        bool vHeld = Keyboard.current.vKey.isPressed;
        bool castPressed = Keyboard.current.xKey.wasPressedThisFrame || Keyboard.current.vKey.wasPressedThisFrame || (xHeld && vHeld);

        // CAST START
        if (!isCasting && castPressed)
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

            // Immediately set fishing/casting pose so rod is visible in hand during meter charging
            if (currentRod == leftRod)
            {
                SetAnimatorBool("idel_l", false);
                SetAnimatorBool("fishing_l", true);
                SetAnimatorBool("fishing_r", false);
            }
            else
            {
                SetAnimatorBool("idel_r", false);
                SetAnimatorBool("fishing_l", false);
                SetAnimatorBool("fishing_r", true);
            }
            EnsureRodChildVisible();
            EnsureFishermanRenderersVisible();

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
        isCanMove = false;

        PlaySFX(throwWorm);

        StopCoroutine(CastMeterRoutine());

        // Ensure rod is visible during the cast animation
        EnsureRodChildVisible();

        if (currentRod == leftRod)
        {
            SetAnimatorBool(idleLeftParam, false);
            SetAnimatorBool(fishingLeftParam, true);
            SetAnimatorBool(fishingRightParam, false);
            SetAnimatorTrigger(castingLeftTrigger);
        }
        else
        {
            SetAnimatorBool(idleRightParam, false);
            SetAnimatorBool(fishingLeftParam, false);
            SetAnimatorBool(fishingRightParam, true);
            SetAnimatorTrigger(castingRightTrigger);
        }

        // Keep isCasting true briefly after trigger to ensure animation is detected
        yield return new WaitForSeconds(0.1f);
        isCasting = false;

        yield return new WaitForSeconds(0.4f);

        Hook hook;
        hook = null;


        if (GS.Instance.isLan)
        {
            hook = fishermanController_Mirror.hook;
            hook.TryToSetJunkRod(currentRod.position);
            fishermanController_Mirror.hook.hook_Mirror.RpcSetJunkRod(currentRod.position);
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

        // Re-enable all fisherman sprites after hook returns (fixes fisherman not visible)
        EnsureFishermanRenderersVisible();
        EnsureRodChildVisible();
        if (GS.Instance != null && GS.Instance.isLan && fishermanController_Mirror != null && fishermanController_Mirror.isServer)
        {
            fishermanController_Mirror.RpcEnsureVisibility();
        }

        if (leftHook == null && rightHook == null && !isCasting)
        {
            RestoreSelectedRodPose();
        }
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
            SetAnimatorBool("fishGotFacing_r", res);
        }
        else if (isLeft)
        {
            Debug.Log("OnFishGoatAnimation called =" + res);
            SetAnimatorBool("fishGotFacing_l", res);
        }
    }
    public void OnCryingAnimation(bool res)
    {
        if (isRight)
        {
            SetAnimatorBool("isCrying_r", res);
        }
        else if (isLeft)
        {
            SetAnimatorBool("isCrying_l", res);
        }
    }

    // Call this at game start or when fisherman resets to clear stuck animations
    public void ResetAnimationStates()
    {
        // Try to get animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            Debug.Log("Animator was null, got via GetComponent: " + (animator != null));
        }

        if (animator == null)
        {
            // Try to find FishermanChildAnimatorSync and get its rootAnimator
            FishermanChildAnimatorSync sync = GetComponent<FishermanChildAnimatorSync>();
            if (sync != null && sync.rootAnimator != null)
            {
                animator = sync.rootAnimator;
                Debug.Log("[FishermanController] Assigned animator from FishermanChildAnimatorSync.rootAnimator");
            }
        }

        if (animator == null)
        {
            // Try to find "chest" child Animator
            Transform chestTransform = transform.Find("chest");
            if (chestTransform != null)
            {
                animator = chestTransform.GetComponent<Animator>();
                Debug.Log("[FishermanController] Assigned chest animator as primary animator.");
            }
        }

        if (animator == null)
        {
            Debug.LogError("Animator not found on FishermanController! Cannot reset animation states.");
            return;
        }

        // Reset all animation bools to false
        animator.SetBool("isCrying_l", false);
        animator.SetBool("isCrying_r", false);
        animator.SetBool("isFighting_l", false);
        animator.SetBool("isFighting_r", false);
        animator.SetBool("fishGotFacing_l", false);
        animator.SetBool("fishGotFacing_r", false);
        animator.SetBool("isWin_l", false);
        animator.SetBool("isWin_r", false);
        animator.SetBool("fishing_l", false);
        animator.SetBool("fishing_r", false);
        animator.SetBool("idel_l", false);
        animator.SetBool("idel_r", false);

        // Also reset movement bools
        animator.SetBool("moveForward_l", false);
        animator.SetBool("moveBackward_l", false);
        animator.SetBool("moveReverceForward_r", false);
        animator.SetBool("moveReverceBackward_r", false);

        Debug.Log("Fisherman animation states reset - all bools set to false");
    }

    public void PlayWinAnimation()
    {
        isCanMove = false;
        isCanCast = false;
        isCasting = false;

        animator.SetBool("isCrying_r", false);
        animator.SetBool("isCrying_l", false);
        animator.SetBool("isFighting_r", false);
        animator.SetBool("isFighting_l", false);

        if (isRight)
        {
            SetAnimatorBool("isWin_r", true);
        }
        else
        {
            SetAnimatorBool("isWin_l", true);
        }
    }

    public void OnFightAnimation(bool res)
    {
        Debug.Log("OnFightAnimation called =" + res);

        if (res)
        {
            // Lock fisherman during fighting — prevent movement and casting interference
            isCanMove = false;
            isCanCast = false;
            isCasting = false;

            // Ensure the rod is visible during fighting
            EnsureRodChildVisible();
            EnsureFishermanRenderersVisible();
            if (GS.Instance != null && GS.Instance.isLan && fishermanController_Mirror != null && fishermanController_Mirror.isServer)
            {
                fishermanController_Mirror.RpcEnsureVisibility();
            }
        }
        else
        {
            // Restore movement and casting after fight ends
            isCanMove = true;
            isCanCast = true;
        }

        if (isRight)
        {
            SetAnimatorBool(fightingRightParam, res);
            if (res)
            {
                SetAnimatorBool(idleRightParam, false);
                SetAnimatorBool(fishingLeftParam, false);
                SetAnimatorBool(fishingRightParam, true); // Keep rod pose during fight
            }
        }
        else if (isLeft)
        {
            SetAnimatorBool(fightingLeftParam, res);
            if (res)
            {
                SetAnimatorBool(idleLeftParam, false);
                SetAnimatorBool(fishingRightParam, false);
                SetAnimatorBool(fishingLeftParam, true); // Keep rod pose during fight
            }
        }

        if (!res)
        {
            RestoreSelectedRodPose();
        }
    }

    [PunRPC]
    public void RpcSetFishermanCosmetics(string hatName, string hairName)
    {
        CosmeticRuntimeApplier.ApplyFishermanCosmeticsByName(gameObject, hatName, hairName);
    }

    internal void OnReeling()
    {
        if (isRight)
        {
            SetAnimatorTrigger("isReeling_r");
        }
        else if (isLeft)
        {
            SetAnimatorTrigger("isReeling_l");
        }
    }

    internal void PlaySFX(AudioClip playClip)
    {
        fisherManSounds.clip = playClip;
        GS.Instance.SetSFXVolume(fisherManSounds);
        fisherManSounds.Play();
    }

    private void SetAnimatorBool(string parameterName, bool value)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(parameterName, value);
        if (childAnimatorSync != null)
        {
            childAnimatorSync.ApplyBool(parameterName, value);
        }

        if (GS.Instance != null && GS.Instance.isLan && fishermanController_Mirror != null && fishermanController_Mirror.isServer)
        {
            fishermanController_Mirror.RpcSetAnimatorBool(parameterName, value);
        }
        else if (ShouldSyncAnimatorParameter()
            && (!animatorBoolCache.TryGetValue(parameterName, out bool cachedValue) || cachedValue != value))
        {
            animatorBoolCache[parameterName] = value;
            photonView.RPC(nameof(RpcSetAnimatorBool), RpcTarget.Others, parameterName, value);
        }
    }

    private void SetAnimatorTrigger(string parameterName)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetTrigger(parameterName);
        if (childAnimatorSync != null)
        {
            childAnimatorSync.ApplyTrigger(parameterName);
        }

        if (GS.Instance != null && GS.Instance.isLan && fishermanController_Mirror != null && fishermanController_Mirror.isServer)
        {
            fishermanController_Mirror.RpcSetAnimatorTrigger(parameterName);
        }
        else if (ShouldSyncAnimatorParameter())
        {
            photonView.RPC(nameof(RpcSetAnimatorTrigger), RpcTarget.Others, parameterName);
        }
    }

    private bool ShouldSyncAnimatorParameter()
    {
        return GS.Instance != null
            && !GS.Instance.isLan
            && PhotonNetwork.InRoom
            && photonView != null
            && GameManager.Instance != null
            && (PhotonNetwork.IsMasterClient || GameManager.Instance.isFisherMan || photonView.IsMine);
    }

    [PunRPC]
    private void RpcSetAnimatorBool(string parameterName, bool value)
    {
        if (animator == null)
        {
            ResetAnimationStates();
        }

        if (animator != null)
        {
            animator.SetBool(parameterName, value);
            if (childAnimatorSync == null)
            {
                childAnimatorSync = GetComponent<FishermanChildAnimatorSync>();
            }
            if (childAnimatorSync != null)
            {
                childAnimatorSync.ApplyBool(parameterName, value);
            }
            animatorBoolCache[parameterName] = value;
        }
    }

    [PunRPC]
    private void RpcSetAnimatorTrigger(string parameterName)
    {
        if (animator == null)
        {
            ResetAnimationStates();
        }

        if (animator != null)
        {
            animator.SetTrigger(parameterName);
            if (childAnimatorSync == null)
            {
                childAnimatorSync = GetComponent<FishermanChildAnimatorSync>();
            }
            if (childAnimatorSync != null)
            {
                childAnimatorSync.ApplyTrigger(parameterName);
            }
        }
    }

  
}
