using FishNet.Object;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Hook : NetworkBehaviour
{
    public Transform rodTip;
    public LineRenderer lineRenderer;
    public float dropSpeed = 3f;

    public GameObject wormPrefab;
    public Transform wormParent;
    internal GameObject wormInstance;

    private bool hasWorm = false;
    private bool isReturning = false;
    internal bool isGoing = true;

    public float minDistance = 2f;   // Minimum hook drop distance
    public float maxDistance = 15f;  // Maximum hook drop distance

    public static Hook instance;

    public Vector3 pos;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        pos = transform.position;
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

        if(IsServer)
        {
            AttachWorm();
        }
    }

    void Update()
    {
        ShowRope();
        if (Input.GetMouseButtonDown(1) && !isReturning && !isGoing && FishermanController.instance.isfisherMan) // 1 = right mouse button
        {
            LoadReturnToRod();
        }
    }
    public void ShowRope()
    {

        if (rodTip != null)
        {
            lineRenderer.SetPosition(0, rodTip.position);
            lineRenderer.SetPosition(1, transform.position);
        }
        else
        {
            lineRenderer.SetPosition(0, pos);
            lineRenderer.SetPosition(1, transform.position);
        }
    }

    public void AttachWorm()
    {
        if (wormPrefab != null && !hasWorm && wormParent != null)
        {
            GameObject worm = Instantiate(wormPrefab, wormParent.position, Quaternion.identity, wormParent);
            worm.transform.localPosition = Vector3.zero;
            worm.GetComponent<PolygonCollider2D>().enabled = false;
            hasWorm = true;
            wormInstance = worm;
            if (IsServer)
                Spawn(worm);

            worm.transform.SetParent(wormParent, false);
        }
    }

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

        isGoing = false;

        if (wormInstance != null)
        {
          wormInstance.GetComponent<PolygonCollider2D>().enabled = true;
        }
        EnableClientColider();
    }


    [ObserversRpc]
    public void EnableClientColider()
    {
        Debug.Log("EnableClientColider");
        if (wormInstance != null)
        {
            wormInstance.GetComponent<PolygonCollider2D>().enabled = true;
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void LoadReturnToRod()
    {
        StartCoroutine(ReturnToRod());  
    }
    public void DropWorm()
    {
        if (IsServer)
        {
            ExecuteFunctionLocal();
            ExecuteFunctionObserversRpc();
        }
        else
        {
            CallUniversalServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CallUniversalServerRpc()
    {
        ExecuteFunctionLocal();

        ExecuteFunctionObserversRpc();
    }

    [ObserversRpc] 
    private void ExecuteFunctionObserversRpc()
    {
        ExecuteFunctionLocal();
    }

    private void ExecuteFunctionLocal()
    {
        if (wormInstance != null)
        {
            wormInstance.tag = "DropedWorm";
            wormInstance.transform.parent = null; // worm ko hook se alag kar do
            wormInstance = null; // reference clear
            hasWorm = false;
        }
    }

    private IEnumerator ReturnToRod()
    {
        isReturning = true;
        Vector3 target = rodTip.position;

        // Detach worm from hook so it stays in scene
        DropWorm();

        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, dropSpeed * 1.5f * Time.deltaTime);
            yield return null;
        }

        MashPhaseManager.instance.mashPanel.SetActive(false);

        if(IsServer)
        {
            Debug.Log("Called Srver Side");
        }
        else
        {
            Debug.Log("Called client Side");

        }


        for (int i = 0; i < GameManager.Instance.AllFishPlayers.Count; i++)
        {
            if (GameManager.Instance.AllFishPlayers[i] != null)
            {
                GameManager.Instance.AllFishPlayers[i].CatchByFishermanLocal();
            }
        }

        Destroy(gameObject); 
    }


    // ✅ Updated to avoid obsolete warning
    void OnDestroy()
    {

        FishermanController fc = FindFirstObjectByType<FishermanController>();
        if (fc.isfisherMan)
        {
            fc.ClearHookReference(this.gameObject);
            fc.CheckWorms();
            fc.isCanMove = true;
        }
    }

    private void ExecuteShowGameOver()
    {
        ShowGameOver();
    }

    // 🔹 अगर client से call किया तो पहले server पर जाएगा
    [ServerRpc(RequireOwnership = false)]
    private void ShowGameOverServerRpc()
    {
        ExecuteShowGameOver();          // server पर चलेगा
        ShowGameOverObserversRpc();     // फिर सब clients पर चलेगा
    }

    // 🔹 server सभी clients को बोलेगा चलाने के लिए
    [ObserversRpc]
    private void ShowGameOverObserversRpc()
    {
        ExecuteShowGameOver();
    }

    // 🔹 Public wrapper → चाहे server call करे या client
    public void CallShowGameOver()
    {
        if (IsServer)
        {
            ExecuteShowGameOver();          // server पर तुरंत
            ShowGameOverObserversRpc();     // clients पर
        }
        else
        {
            ShowGameOverServerRpc();        // client server को बोलेगा
        }
    }


    public void ShowGameOver()
    {
        for (int i = 0; i < GameManager.Instance.AllFishPlayers.Count; i++)
        {
            if (GameManager.Instance.AllFishPlayers[i] != null)
            {
                GameManager.Instance.AllFishPlayers[i].CatchByFishermanLocal();
            }
        }
    }

}
