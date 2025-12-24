using Mirror;
using Photon.Pun;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Hook : MonoBehaviourPunCallbacks
{
    public Transform rodTip;

    public Hook_Mirror hook_Mirror;

    public LineRenderer lineRenderer;
    public float dropSpeed = 3f;
    
    [Header("Rod Tip Offset")]
    public float rodTipOffset = 0.5f; // Distance from rod pivot to tip (adjust based on your rod sprite)
    public float horizontalOffset = 0.1f; // Horizontal offset to make line appear from rod string continuation (right rod and left rod fisherman view)
    public float leftRodHorizontalOffsetFish = 0.1f; // Horizontal offset for left rod from fish view
    public float leftRodVerticalOffset = 0.15f; // Vertical offset for left rod to compensate for negative scale (fish view)
    public float leftRodVerticalOffsetFisherman = 0.05f; // Vertical offset for left rod from fisherman view

    public GameObject wormPrefab;
    public Transform wormParent;
    public GameObject wormInstance;

    internal bool hasWorm = false;
    private bool isReturning = false,isComming = true;

    public float minDistance = 2f;   // Minimum hook drop distance
    public float maxDistance = 15f;  // Maximum hook drop distance

    public static Hook Instance;

    public AudioSource hookBack;

    //this clips for fisherman
    public AudioClip fishCatched;
    public AudioClip smallVictory;
    public FishermanController fishermanController;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        if(FishermanController.Instance !=null)
        {
            fishermanController = FishermanController.Instance;
        }
            fishermanController.fishermanController_Mirror.hook = this;

        if (GS.Instance.isLan)
        {
            transform.localScale = Vector3.zero;
        }

        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
    }

    void Update()
    {

        if (transform.position.x < fishermanController.transform.position.x)
        {
            rodTip = fishermanController.leftRod; 
        }
        else if (transform.position.x > fishermanController.transform.position.x)
        {
            rodTip = fishermanController.rightRod;
        }
        else
        {
            rodTip = transform;
        }
       

        if (rodTip == null || lineRenderer == null || transform.localScale == Vector3.zero)
        {
            lineRenderer.enabled = false;
            return;
        }


        lineRenderer.enabled = true;

        // Determine if this is left or right rod for horizontal offset
        bool isLeftRod = (rodTip == fishermanController.leftRod);
        
        // Calculate the actual rod tip position based on rotation and offset
        Vector3 actualRodTipPosition = GetRodTipPosition(rodTip, isLeftRod);
        lineRenderer.SetPosition(0, actualRodTipPosition);
        lineRenderer.SetPosition(1, transform.position);
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;

        if (Input.GetMouseButtonDown(1))
        {
            if (GameManager.Instance.isFisherMan)
            {
                if (!isComming && !isReturning && !MiniGameManager.Instance.active && !MashPhaseManager.Instance.active) // 1 = right mouse button
                {
                    LoadReturnToRod_Mirror();
                }
            }
        }

        if (PhotonNetwork.IsMasterClient )
        {
            if (Input.GetMouseButtonDown(1) && !isComming && !isReturning && !MiniGameManager.Instance.active && !MashPhaseManager.Instance.active) // 1 = right mouse button
            {
                LoadReturnToRod();
            }
        }
    }

    public void AttachWorm()
    {
        if (wormPrefab != null && !hasWorm && wormParent != null)
        {
            if (GS.Instance.isLan)
            {
                GameManager gm = GameManager.Instance;
                if (gm != null)
                {
                    wormInstance =  GameManager.Instance.myFish.fishController_Mirror.SetWormInJunk(GetComponent<NetworkIdentity>());
                }
            }
            else
            {
                wormInstance = PhotonNetwork.Instantiate(wormPrefab.name, wormParent.position, Quaternion.identity).gameObject;
                WormSpawner.Instance.activeWorms.Add(wormInstance);
                int wormID = wormInstance.GetComponent<PhotonView>().ViewID;
                photonView.RPC(nameof(SetupWormRPC), RpcTarget.AllBuffered, wormID);
            }
        }
    }

    [PunRPC]
    void SetupWormRPC(int wormID)
    {
        PhotonView wormView = PhotonView.Find(wormID);

        if (wormView != null)
        {
            Transform worm = wormView.transform;
            worm.SetParent(wormParent.transform, false);
            worm.localPosition = Vector3.zero;
            worm.localScale = Vector3.one;
            hasWorm = true;
        }
    }

    // Launch hook with variable distance
    public void LaunchDownWithDistance(float distance, Transform _rodip)
    {
        rodTip = _rodip;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        StartCoroutine(MoveDown(distance));
    }
    private IEnumerator MoveDown(float distance)
    {
        Vector3 target = transform.position + Vector3.down * distance;
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, dropSpeed * Time.deltaTime);
            yield return null;
        }

        if (GS.Instance.isLan)
        {
            GameManager.Instance.myFish.fishController_Mirror.EnableWormCollider(wormInstance.GetComponent<NetworkIdentity>());
        }
        else
        {
            PhotonView wormPV = wormInstance.GetComponent<PhotonView>();
            photonView.RPC(nameof(EnableWormColliderRPC), RpcTarget.AllBuffered, wormPV.ViewID, true);
        }

        isComming = false;
    }

    [PunRPC]
    void EnableWormColliderRPC(int wormID,bool enable)
    {
        PhotonView wormView = PhotonView.Find(wormID);
        if (wormView != null)
        {
            PolygonCollider2D col = wormView.GetComponent<PolygonCollider2D>();
            if (col != null)
                col.enabled = enable;
        }
    }

    public void CallRpcToReturnRod()
    {
        photonView.RPC(nameof(LoadReturnToRod), RpcTarget.MasterClient);
    }

    [PunRPC]
    public void LoadReturnToRod()
    {
        StartCoroutine(ReturnToRod());
    }

    public void LoadReturnToRod_Mirror()
    {
        StartCoroutine(ReturnToRod());
    }

    private IEnumerator ReturnToRod()
    {
        if (!isReturning && PhotonNetwork.IsMasterClient || !isReturning && GameManager.Instance.isFisherMan)
        {
            GS.Instance.SetSFXVolume(hookBack);
            hookBack.Play();
            isReturning = true;
            // Determine if this is left or right rod for horizontal offset
            bool isLeftRod = (rodTip == fishermanController.leftRod);
            Vector3 target = GetRodTipPosition(rodTip, isLeftRod);

            // Detach worm from hook so it stays in scene
                    hasWorm = false;
            if (wormInstance != null)
            {
                if (GS.Instance.isLan)
                {
                    GameManager.Instance.myFish.fishController_Mirror.DropWorm(wormInstance.GetComponent<NetworkIdentity>());
                }
                else
                {
                    PhotonView wormPV = wormInstance.GetComponent<PhotonView>();
                    photonView.RPC(nameof(DropWormRpc), RpcTarget.AllBuffered, wormPV.ViewID);
                    wormInstance.transform.parent = null; // worm ko hook se alag kar do
                    wormInstance = null; // reference clear
                }
            }

            if (wormParent.GetComponentInChildren<JunkManager>() != null)
            {
                fishermanController.OnCryingAnimation(true);
            }
            else
            {
                fishermanController.OnFishGoatAnimation(true);
            }

            FishController[] fishes = GetComponentsInChildren<FishController>();

            while (Vector3.Distance(transform.position, target) > 0.05f)
            {
               
                transform.position = Vector3.MoveTowards(transform.position, target, dropSpeed * 1.5f * Time.deltaTime);
                yield return null;
            }

            fishermanController.OnFishGoatAnimation(false);
            fishermanController.OnCryingAnimation(false);
            fishermanController.OnFightAnimation(false);

            if (wormParent.childCount != 0)
            {
                fishermanController.PlaySFX(fishCatched);
                if (wormParent.GetChild(0).GetComponent<FishController>())
                {
                    fishermanController.PlaySFX(smallVictory);
                }
            }
         
            isReturning = false;

            transform.localScale = Vector3.zero;
            fishermanController.isCanMove = true;
            fishermanController.isCanCast = true;


            fishermanController.OnReeling();
            if (GameManager.Instance.isFisherMan)
            {
                fishermanController.ClearHookReference(this.gameObject);
                fishermanController.CheckWorms();
            }

            foreach (FishController f in fishes)
            {
                f.transform.localScale = Vector3.zero;
            }

            if(GS.Instance.isLan)
            {
                if (GameManager.Instance.myFish.isFisherMan)
                {
                    if (wormParent.childCount > 0)   
                    {
                        Transform child = wormParent.GetChild(0);
                        string tag = child.tag;

                        Debug.Log("wormParent.GetChild(0).tag = " + tag);

                        if (tag == "Fish")
                        {
                            GameManager.Instance.myFish.fishController_Mirror
                                .DisableFish_Mirror(child.GetComponent<NetworkIdentity>());
                        }
                        else if (tag == "Junk")
                        {
                            GameManager.Instance.myFish.fishController_Mirror
                                .Destroy_Mirror(child.gameObject);
                        }
                    }
                    else
                    {
                        Debug.Log("wormParent EMPTY hai — koi child nahi!");
                    }
                }
            }
            else
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    [PunRPC]
    void DropWormRpc(int wormID)
    {
        PhotonView wormView = PhotonView.Find(wormID);
        if (wormView != null)
        {
            Transform col = wormView.GetComponent<Transform>();
            col.tag = "Worm";

            if (col != null)
                col.parent = null; 
        }
    }

    // ✅ Updated to avoid obsolete warning
    void OnDestroy()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            FishermanController fc = Object.FindFirstObjectByType<FishermanController>();
            if (fc != null)
            {
                fc.ClearHookReference(this.gameObject);
                fc.CheckWorms();
            }
        }
        else
        {
            FishController myFish = GameManager.Instance.myFish;
            if (myFish != null)
            {
                if (myFish.catchadeFish)
                {
                    myFish.DestroyCatchFish();
                }
            }
        }
    }

    public void TryToSetJunkRod(Vector3 curruntRod)
    {
        Debug.Log("TryToSetJunkRod called");
        transform.position = curruntRod;
        transform.localScale = Vector3.one;
        NetworkIdentity hookIDidentity = GetComponent<NetworkIdentity>();
        AttachWorm();
    }
    
    /// <summary>
    /// Calculates the actual position of the rod tip based on the rod's rotation and offset.
    /// This ensures the fishing line appears to come from the visual tip of the rod.
    /// </summary>
    private Vector3 GetRodTipPosition(Transform rod, bool isLeftRod)
    {
        if (rod == null) return Vector3.zero;
        
        // Check if we're viewing from fish side (not fisherman)
        bool isFishView = (GameManager.Instance != null && !GameManager.Instance.isFisherMan);
        
        // Apply horizontal (X-axis) offset
        Vector3 horizontalOffsetVector;
        if (isLeftRod)
        {
            // For left rod, use different horizontal offset based on view
            // Negative X direction (to the left)
            float horizontalOffsetValue = isFishView ? leftRodHorizontalOffsetFish : horizontalOffset;
            horizontalOffsetVector = new Vector3(-horizontalOffsetValue, 0f, 0f);
        }
        else
        {
            // For right rod, offset to the right (positive X)
            horizontalOffsetVector = new Vector3(horizontalOffset, 0f, 0f);
        }
        
        // For left rod, account for negative Y scale which affects visual tip position
        Vector3 verticalOffsetVector = Vector3.zero;
        if (isLeftRod)
        {
            // Left rod has negative Y scale (y: -1, z: -1), which flips the visual
            // From fish view, the tip appears at a different Y position due to the scale flip
            // Use different offset values for fisherman vs fish view
            float verticalOffsetValue = isFishView ? -leftRodVerticalOffset : leftRodVerticalOffsetFisherman;
            verticalOffsetVector = new Vector3(0f, verticalOffsetValue, 0f);
        }
        
        // Return the rod's position plus offsets
        return rod.position + horizontalOffsetVector + verticalOffsetVector;
    }
   
}