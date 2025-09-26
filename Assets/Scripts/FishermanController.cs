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

    internal Transform currentRod;

    [Header("Casting")]
    public KeyCode castKey1 = KeyCode.X;
    public KeyCode castKey2 = KeyCode.V;
    public Slider castingMeter;       // UI Slider (0-1)
    public float meterSpeed = 2f;
    public float maxCastDistance = 10f; // max distance hook can go

    [Header("Worms")]
    internal int worms;
    internal int catchadFish = 0;

    internal bool isCasting = false;
    internal bool isCanMove = true;
    internal bool isFisherMan = false;
    internal bool isCanCast = true;
    private bool meterIncreasing = true;

    [HideInInspector] public GameObject leftHook = null;
    [HideInInspector] public GameObject rightHook = null;

    [Header("Horizontal Bounds")]
    public float minX = -8f;
    public float maxX = 8f;

    public static FishermanController instance;
   
    private void Awake()
    {
        if (instance == null)
            instance = this;
        
    }
    void Start()
    {

        if (PhotonNetwork.IsMasterClient)
        {
            currentRod = leftRod;
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
                    HandleMovement();
                }
                HandleRodSelection();
                HandleCasting();
            }
        }
    }
    void HandleMovement()
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
        }
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
            if ((leftHook != null) || (rightHook != null))
            {
                Debug.Log("Rod already has a hook!");
                return;
            }

            isCasting = true;
            StartCoroutine(CastMeterRoutine());
        }

        // Release → cast hook with meter value
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
        StopCoroutine(CastMeterRoutine());

        if (currentRod != null)
        {
            Hook hook = PhotonNetwork.Instantiate("hookPrefab", currentRod.position, Quaternion.identity).GetComponent<Hook>();

            if (hook != null)
            {
                int hookID = hook.GetComponent<PhotonView>().ViewID;

                // Send RPC to all clients to set rodTip
                photonView.RPC(nameof(SetupHookRodRPC), RpcTarget.AllBuffered, hookID,currentRod.position);

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
        }

        castingMeter.value = 0;
    }

    [PunRPC]
    void SetupHookRodRPC(int hookID,Vector3 curruntRod)
    {
        PhotonView hookView = PhotonView.Find(hookID);

        if (hookView != null)
        {
            Hook hook = hookView.GetComponent<Hook>();
            if (hook != null)
            {
                hook.rodTip = curruntRod;
                Debug.Log("Hook GEted");
            }
            else
            {
                Debug.Log("Hook null");
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

        if(catchadFish >= GameManager.instance.totalPlayers-1)
        {
            if (GameManager.instance != null && GameManager.instance.gameOverText != null)
            {
                GameManager.instance.ShowGameOver("Fisherman Win!");
            }
            WormSpawner.instance.canSpawn = isCanMove = false;

            // Optional: stop all fishing actions
            leftHook = null;
            rightHook = null;
            isCasting = false;
            Debug.Log("CheckWorms Clled");

            return;

        }

        if (worms <= 0)
        {
            if (GameManager.instance != null && GameManager.instance.gameOverText != null)
            {
                GameManager.instance.ShowGameOver("Fisherman Lose!\nFishes Win!");
            }
           WormSpawner.instance.canSpawn =  isCanMove = false;

            // Optional: stop all fishing actions
            leftHook = null;
            rightHook = null;
            isCasting = false;
            Debug.Log("CheckWorms Clled");

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

   

}
