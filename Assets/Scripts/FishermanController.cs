using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FishermanController : NetworkBehaviour
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
    public int worms ;

    [Header("Hook")]
    public GameObject hookPrefab;

    internal bool isCasting = false;
    internal bool isCanMove = true;
    internal bool isfisherMan = false;
    private bool meterIncreasing = true;

    [HideInInspector] public bool leftHook = false;
    [HideInInspector] public bool rightHook = false;

    [Header("Horizontal Bounds")]
    public float minX = -8f;
    public float maxX = 8f;

    public static FishermanController instance;

    public int catchadFishes = 0;

    public FishController catchedFish;
   
    private void Awake()
    {
        if (instance == null)
            instance = this;
    }
    void Start()
    {
        currentRod = leftRod;
        if (castingMeter != null)
            castingMeter.value = 0;
       
    }

    void Update()
    {
        if (isfisherMan)
        {
            HandleCasting();
            HandleMovement();
            HandleRodSelection();
        }
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner || IsClient)
        {
            GameManager.Instance.AssignFisherman(this);
        }
    }

    void HandleMovement()
    {
        if (leftHook == false && rightHook == false && !isCasting && isCanMove)
        {
            float moveInput = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(moveInput) > 0.01f)
            {
                Debug.Log($"Sending input to server: {moveInput}");
                SendMoveInputServerRpc(moveInput, Time.fixedDeltaTime);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendMoveInputServerRpc(float moveInput, float deltaTime)
    {
        Debug.Log($"[SERVER] Received input: {moveInput}");
        Vector3 move = new Vector3(moveInput * moveSpeed * deltaTime, 0, 0);
        transform.position += move;

        Vector3 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, minX, maxX);
        transform.position = clampedPos;
    }

    void HandleRodSelection()
    {
        if (Input.GetKeyDown(KeyCode.W))
            currentRod = leftRod;
        else if (Input.GetKeyDown(KeyCode.S))
            currentRod = rightRod;
    }

    void HandleCasting()
    {
        // X + V held down → start casting meter
        if (!isCasting && Input.GetKey(castKey1) && Input.GetKey(castKey2))
        {
            if (( leftHook != false) || (rightHook != false))
            {
                Debug.Log("Rod already has a hook!");
                return;
            }


            isCasting = true;
            StartCoroutine(CastMeterRoutine());

        }

        // Release → cast hook with meter value
        if (500 > 0)
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
        isCanMove = isCasting = false;
        StartCoroutine(CastMeterRoutine());

        if (hookPrefab != null && currentRod != null)
        {
            // Client -> Server ko request bheje
            float castDistance = castingMeter.value * maxCastDistance;
            ReleaseCastServerRpc(currentRod == leftRod, castDistance);

            // Track hook per rod
            if (currentRod == leftRod)
            {
                leftHook = true;
            }
            else
            {
                rightHook = true;
            }

            if (worms > 0)
            {
                worms--;
                GameManager.Instance.UpdateUI(worms);
                Debug.Log($"[SERVER] Worm used! Remaining: {worms}");
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReleaseCastServerRpc(bool isLeftRod, float castDistance)
    {
        Transform rod = isLeftRod ? leftRod : currentRod;

        GameObject hook = Instantiate(hookPrefab, rod.position, Quaternion.identity);
        hook.name = "Hook";

        Hook hookScript = hook.GetComponent<Hook>();
        if (hookScript != null)
        {
            hookScript.rodTip = rod;
            hookScript.LaunchDownWithDistance(castDistance);
        }

        // Server spawn karega
        Spawn(hook);

        // Worm count decrease
       
    }

    public void ClearHookReference(GameObject hook)
    {
        if (hook == leftHook) leftHook = false;
        if (hook == rightHook) rightHook = false;
    }

    [ObserversRpc]
    public void SetCurruntRodinHook(Transform tip)
    {
        Debug.Log("SetCurruntRodinHook");
        Hook.instance.rodTip = tip;
    }

    // Check worms and print lose message
    public void CheckWorms()
    {
        if(catchadFishes >= NetworkUI.instence.playersRequired-1)
        {
            if (isfisherMan)
            {
                GameManager gm = GameManager.Instance;
                for (int i = 0; i < gm.AllFishPlayers.Count; i++)
                {
                    gm.AllFishPlayers[i].ShowGameOver(false);
                }
                gm.gameOverPanel.SetActive(true);
                gm.ShowGameOverMessage("Fisherman Win!");
                WormSpawner.instance.canSpawn = isCanMove = HungerSystem.instance.canDecrease = FishController.instance.canMove = false;

                // Optional: stop all fishing actions
                leftHook = false;
                rightHook = false;
                isCasting = false;
            }
            return;
        }



        if (worms <= 0)
        {
            if (isfisherMan)
            {
                Debug.Log("zzzCheckWormsCheckWorms");
                GameManager gm = GameManager.Instance;

                for (int i = 0; i < gm.AllFishPlayers.Count; i++)
                {
                    gm.AllFishPlayers[i].ShowGameOver(true);
                }
                gm.gameOverPanel.SetActive(true);

                gm.ShowGameOverMessage("Fisherman Lose!");
                WormSpawner.instance.canSpawn = isCanMove = HungerSystem.instance.canDecrease = FishController.instance.canMove = false;
                // Optional: stop all fishing actions
                leftHook = false;
                rightHook = false;
                isCasting = false;
            }
        }

    }


}
