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
    private bool isReturning = false;

    public float minDistance = 2f;   // Minimum hook drop distance
    public float maxDistance = 15f;  // Maximum hook drop distance

    public static Hook instance;

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
            if (Input.GetMouseButtonDown(1) && !isReturning) // 1 = right mouse button
            {
                LoadReturnToRod();
            }
        }
    }

    public void AttachWorm()
    {
        if (wormPrefab != null && !hasWorm && wormParent != null)
        {
            //  wormInstance = Instantiate(wormPrefab, wormParent.position, Quaternion.identity, wormParent);
            wormInstance = PhotonNetwork.Instantiate("HookWorm", wormParent.position,Quaternion.identity).gameObject;

            int wormID = wormInstance.GetComponent<PhotonView>().ViewID;

              photonView.RPC("SetupWormRPC", RpcTarget.AllBuffered, wormID);

            /*  wormInstance.transform.SetParent(wormParent.transform,false);
              wormInstance.transform.localPosition = Vector3.zero;
              wormInstance.GetComponent<PolygonCollider2D>().enabled = false;
              hasWorm = true;*/
        }
    }

    [PunRPC]
    void SetupWormRPC(int wormID)
    {
        PhotonView wormView = PhotonView.Find(wormID);

        if (wormView != null)
        {
            Transform worm = wormView.transform;

            // Parent set karo
            worm.SetParent(wormParent.transform, false);

            // Local pos reset karo
            worm.localPosition = Vector3.zero;

            // Collider disable karo
            PolygonCollider2D col = worm.GetComponent<PolygonCollider2D>();
            if (col != null)
                col.enabled = false;

            // Bool set karo
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

        // Master client ya koi bhi client call kare
        PhotonView wormPV = wormInstance.GetComponent<PhotonView>();
        photonView.RPC("EnableWormColliderRPC", RpcTarget.AllBuffered, wormPV.ViewID,true);


       // wormInstance.GetComponent<PolygonCollider2D>().enabled = true;

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
        if (isReturning)
        {
            Debug.Log("ReturnToRod all rady returning");
            yield return null;
        }
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

        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, dropSpeed * 1.5f * Time.deltaTime);
            yield return null;
        }

        PhotonNetwork.Destroy(gameObject); // hook destroyd
    }

    [PunRPC]
    void DropWormRpc(int wormID)
    {
        PhotonView wormView = PhotonView.Find(wormID);
        if (wormView != null)
        {
            Transform col = wormView.GetComponent<Transform>();
            if (col != null)
                col.parent = null; // worm ko hook se alag kar do
        }
    }

    // ✅ Updated to avoid obsolete warning
    void OnDestroy()
    {
        FishermanController fc = Object.FindFirstObjectByType<FishermanController>();
        if (fc != null)
        {
            fc.ClearHookReference(this.gameObject);
            //fc.CheckWorms();
        }
    }

}
