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
            Debug.Log("hook is generated");
            transform.localScale = Vector3.zero;
        }

        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;

    }

    void Update()
    {
        if (transform.position.x < fishermanController.transform.position.x)
        {
            rodTip = fishermanController.leftRod; // ya jo bhi tip ka point ho
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

        lineRenderer.SetPosition(0, rodTip.position);
        lineRenderer.SetPosition(1, transform.position);

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
            GS.Instance.SetVolume(hookBack);
            hookBack.Play();
            isReturning = true;
            Vector3 target = rodTip.position;

            // Detach worm from hook so it stays in scene
            if (wormInstance != null)
            {
                if (GS.Instance.isLan)
                {
                    GameManager.Instance.myFish.fishController_Mirror.DropWorm(wormInstance.GetComponent<NetworkIdentity>());
                    hasWorm = false;
                }
                else
                {
                    PhotonView wormPV = wormInstance.GetComponent<PhotonView>();
                    photonView.RPC(nameof(DropWormRpc), RpcTarget.AllBuffered, wormPV.ViewID);
                    wormInstance.transform.parent = null; // worm ko hook se alag kar do
                    wormInstance = null; // reference clear
                    hasWorm = false;
                }
            }

            FishermanController fc = FishermanController.Instance;
            if (wormParent.GetComponentInChildren<JunkManager>() != null)
            {
                fc.OnCryingAnimation(true);
            }
            else
            {
                fc.OnFishGoatAnimation(true);
            }

            FishController[] fishes = GetComponentsInChildren<FishController>();

            while (Vector3.Distance(transform.position, target) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, dropSpeed * 1.5f * Time.deltaTime);
                yield return null;
            }

            if (wormParent.childCount != 0)
            {
                fc.PlaySFX(fishCatched);
                if (wormParent.GetChild(0).GetComponent<FishController>())
                {
                    fc.PlaySFX(smallVictory);
                }
            }
            isReturning = false;

            transform.localScale = Vector3.zero;
            fc.isCanMove = true;
            fc.isCanCast = true;
            fc.OnFishGoatAnimation(false);
            fc.OnCryingAnimation(false);

            fc.OnReeling();
            if (GameManager.Instance.isFisherMan)
            {
                fc.ClearHookReference(this.gameObject);
                fc.CheckWorms();
            }

            foreach (FishController f in fishes)
            {
                f.transform.localScale = Vector3.zero;
            }

            if(!GS.Instance.isLan)
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
        //hook_Mirror.SpawnWorm();
        AttachWorm();
    }
}
