using Photon.Pun;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Hook : MonoBehaviourPunCallbacks
{
    internal Vector3 rodTip;

    public LineRenderer lineRenderer;
    public float dropSpeed = 3f;

    public GameObject wormPrefab;
    public Transform wormParent;
    private GameObject wormInstance;

    private bool hasWorm = false;
    private bool isReturning = false,isComming = true;

    public float minDistance = 2f;   // Minimum hook drop distance
    public float maxDistance = 15f;  // Maximum hook drop distance

    public static Hook instance;


    public AudioSource hookBack;

    //this clips for fisherman
    public AudioClip fishCatched;
    public AudioClip smallVictory;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;
    }

    void Update()
    {
        if (rodTip == null || lineRenderer == null)
            return;

        lineRenderer.SetPosition(0, rodTip);
        lineRenderer.SetPosition(1, transform.position);

        if (PhotonNetwork.IsMasterClient)
        {
            if (Input.GetMouseButtonDown(1) && !isComming && !isReturning && !MiniGameManager.instance.active && !MashPhaseManager.instance.active) // 1 = right mouse button
            {
                LoadReturnToRod();
            }
        }
    }

    public void AttachWorm()
    {
        if (wormPrefab != null && !hasWorm && wormParent != null)
        {
            wormInstance = PhotonNetwork.Instantiate("HookWorm", wormParent.position, Quaternion.identity).gameObject;
            int wormID = wormInstance.GetComponent<PhotonView>().ViewID;
            photonView.RPC(nameof(SetupWormRPC), RpcTarget.AllBuffered, wormID);
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
            PolygonCollider2D col = worm.GetComponent<PolygonCollider2D>();

            if (col != null)
                col.enabled = false;

            hasWorm = true;
        }
    }

    // Launch hook with variable distance
    public void LaunchDownWithDistance(float distance)
    {
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
        PhotonView wormPV = wormInstance.GetComponent<PhotonView>();
        photonView.RPC(nameof(EnableWormColliderRPC), RpcTarget.AllBuffered, wormPV.ViewID,true);
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

    private IEnumerator ReturnToRod()
    {
        if (isReturning || !PhotonNetwork.IsMasterClient)
        {
            yield return null;
        }

        hookBack.Play();
        isReturning = true;
        Vector3 target = rodTip;

        // Detach worm from hook so it stays in scene
        if (wormInstance != null)
        {
            PhotonView wormPV = wormInstance.GetComponent<PhotonView>();
            photonView.RPC(nameof(DropWormRpc), RpcTarget.AllBuffered, wormPV.ViewID);
            wormInstance.transform.parent = null; // worm ko hook se alag kar do
            wormInstance = null; // reference clear
            hasWorm = false;
        }

        FishermanController fc = FishermanController.instance;
        /*if (wormParent.childCount != 0)
        {
            if (wormParent.GetChild(0).GetComponent<FishController>())
            {
                fc.OnFightAnimation(false);
            }
            fc.OnFishGoatAnimation(true);

        }
        else
        {
            fc.OnFightAnimation(false);
        }*/

        fc.OnFishGoatAnimation(true);


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

            fc.isCanMove = true;
        fc.isCanCast = true;
        fc.OnFishGoatAnimation(false);
        fc.OnReeling();

        foreach (FishController f in fishes)
        {
            f.transform.localScale = Vector3.zero;
        }

        PhotonNetwork.Destroy(gameObject);
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
            FishController myFish = GameManager.instance.myFish;
            if (myFish != null)
            {
                if (myFish.catchadeFish)
                {
                    myFish.DestroyCatchFish();
                }
            }
        }
    }


    public void DestroyParentWithChildren(GameObject parent)
    {
        PhotonNetwork.Destroy(parent);

        foreach (PhotonView pv in parent.GetComponentsInChildren<PhotonView>())
        {
            if (pv != null && pv.gameObject != parent)
            {
                PhotonNetwork.Destroy(pv.gameObject);
            }
        }
    }

}
