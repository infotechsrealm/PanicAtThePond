using FishNet.Connection;
using FishNet.Object;
using System.Collections;
using UnityEngine;

public class FishController : NetworkBehaviour
{
    [Header("Fish Stats")]
    public int hunger = 100;
    public float speed = 5f;

    public Vector2 minBounds = new Vector2(-8f, -4f);
    public Vector2 maxBounds = new Vector2(8f, 4f);

    internal float originalScaleX;
    internal float originalScaleY;

    private Rigidbody2D rb;
    public bool canMove = true;

    public static FishController instance;

    [Header("Floating on Death")]
    private bool isDead = false;
    public float floatSpeed = 2f;

    public bool isCatchedFish = false;

    public Transform junkHolder;   
    public GameObject carriedJunk;

    public GameObject CatchedWorm;

    // Event for fish death
    public static event System.Action<FishController> OnFishDied;

    public PolygonCollider2D myColider;

    public AdvancedHostMigration advancedHostMigration;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        originalScaleX = transform.localScale.x;
        originalScaleY = transform.localScale.y;

       
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        StartCoroutine(SetObjectainGamemanager());
    }

    public GameObject localPlayer; // आपके local clone का reference

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner) // ये मेरा object है
        {
            localPlayer = this.gameObject;
            Debug.Log("Local player spawned: " + gameObject.name);

        }
    }
        
    void Update()
    {
        

        if (!IsOwner)
            return;

        

        if (Input.GetKeyDown(KeyCode.Q))
        {
            AdvancedHostMigration migrationManager = FindObjectOfType<AdvancedHostMigration>();
            if (migrationManager != null)
            {
                Debug.Log("fgasgdsytaSUDSA");
                migrationManager.BecomeNewHost();
            }
            else
            {
                Debug.LogError("nuljjasduyssudsdiysd");
            }
        }

        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
           //AutoFishMove();
            return;
        }

        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        rb.linearVelocity = new Vector2(moveX, moveY) * speed;

        // Flip fish based on direction
        if (moveX < 0)
            transform.localScale = new Vector3(originalScaleX, originalScaleY, 1);
        else if (moveX > 0)
            transform.localScale = new Vector3(-originalScaleX, originalScaleY, 1);

        // Clamp position
        Vector3 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, minBounds.x, maxBounds.x);
        clampedPos.y = Mathf.Clamp(clampedPos.y, minBounds.y, maxBounds.y);
        transform.position = clampedPos;

        // Check hunger
        if (!isDead && HungerSystem.instance.hungerBar.value <= 0)
        {
            isDead = true;
            canMove = false;
            rb.linearVelocity = Vector2.zero;
            StartCoroutine(FloatToSurface());
            StopGame();
        }
    }

    IEnumerator SetObjectainGamemanager()
    {
        yield return new WaitForSeconds(2f);
        if (GameManager.Instance != null)
        {
            if (IsOwner)
            {
                if (GameManager.Instance.myFish == null)
                {
                    GameManager.Instance.myFish = this;
                }
            }

            GameManager.Instance.AllFishPlayers.Add(this);
        }
    }
 

    [ServerRpc(RequireOwnership = false)]
    private void StopGame()
    {
        JunkSpawner.instance.canSpawn = false;
        WormSpawner.instance.canSpawn = false;
        FishermanController.instance.isCanMove = false;
        HungerSystem.instance.canDecrease = false;
    }



    private IEnumerator FloatToSurface()
    {

        float targetY = maxBounds.y; // Surface
        while (transform.position.y < targetY)
        {
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;
            yield return null;
        }
        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        Debug.Log(name + " has floated to the surface (dead).");

        // Trigger event
        OnFishDied?.Invoke(this);

        if (GameManager.Instance != null && GameManager.Instance.gameOverText != null)
        {
            canMove = false;
            GameManager gm = GameManager.Instance;
            myColider.enabled = false;
            gm.gameOverPanel.SetActive(true);
            gm.ShowGameOverMessage("You Lose!");
        }
    }

    // Optional: Auto swim logic when player control is off
    public float Autospeed = 3f;
    internal Vector3 direction = Vector3.left;
    public void AutoFishMove()
    {
        if (transform.position.x > 7.5)
            direction = Vector3.left;
        else if (transform.position.x < -7.5)
            direction = Vector3.right;

        transform.localScale = (direction.x < 0) ? new Vector3(originalScaleX, originalScaleY, 1)
                                                 : new Vector3(-originalScaleX, originalScaleY, 1);


        transform.position += direction * Autospeed * Time.deltaTime;
        transform.position = new Vector3(transform.position.x, -2f, transform.position.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Worm"))
        {

            if (carriedJunk != null)
            {
                DropJunkToHook(other.gameObject);
                return;
            }

            gameObject.tag = "CatchdFish";

            if(IsOwner)
            {
                isCatchedFish = true;
            }
            other.gameObject.GetComponent<PolygonCollider2D>().enabled = false;

            canMove = false;

            Destroy(other.gameObject);


            if (IsOwner)
            {
                MiniGameManager.instance.StartMiniGame();
            }

            CatchedWorm = other.gameObject;
        }

        if (other.CompareTag("GoldTrout"))
        {
            if (IsServer)
            {
                SpawnFisherman();
                DestroyFish(other.gameObject);
            }
            else
            {
                RequestSpawnFishermanServerRpc(other.gameObject);
            }

            if(IsOwner)
            {
                GameManager.Instance.LoadMakeFisherMan();
            }
        }

        if (other.CompareTag("DropedWorm"))
        {
            HungerSystem.instance.AddHunger(75f);
            Destroy(other.gameObject);
        }

        if (other.CompareTag("Worm2"))
        {
            HungerSystem.instance.AddHunger(25f);
            Destroy(other.gameObject);
        }

        if (carriedJunk == null)
        {
            if (other.CompareTag("Junk"))
            {   
                carriedJunk = other.gameObject;
                carriedJunk.GetComponent<PolygonCollider2D>().enabled = false;
                carriedJunk.transform.SetParent(junkHolder);
                carriedJunk.transform.localPosition = Vector3.zero;
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void DestrouWorm()
    {
        Destroy(CatchedWorm);
    }
    public void DestroyFish(GameObject Worm)
    {
        Debug.Log("DestroyFish");   
        Destroy(Worm);
        Destroy(gameObject);
    }

    [ServerRpc]
    public void RequestSpawnFishermanServerRpc(GameObject worm)
    {
        SpawnFisherman(); // server पर call, object complete work करेगा
        DestroyFish(worm);
    }

    public void SpawnFisherman()
    {
        Debug.Log("SpawnFisherman");

        JunkSpawner.instance.canSpawn = true;
        JunkSpawner.instance.LoadSpaenLoop();
        var fishermanObj = Instantiate(GameManager.Instance.fishermanPrefab,
            new Vector3(0f, 8.75f, 0f), Quaternion.identity);
        Spawn(fishermanObj.gameObject);
    }

    [ServerRpc]
    public void SetFisherMan(FishermanController fm)
    {
        Debug.Log("SetFisherMan"); 
        GameManager.Instance.fisherman = fm;    
    }

    public void DropJunkToHook(GameObject worm)
    {
        if (IsServer)
        {
            DropJunkToHookLocal(worm);
            DropJunkToHookObserversRpc(worm);
        }
        else if (IsOwner)
        {
            DropJunkToHookServerRpc(worm);
        }
    }

    private void DropJunkToHookLocal(GameObject worm)
    {
        Transform wormParent = Hook.instance.wormParent;

        if (wormParent != null)
        {
            HungerSystem.instance.canDecrease = canMove = true;

            HungerSystem.instance.AddHunger(75f);

            carriedJunk.transform.SetParent(wormParent);
            carriedJunk.GetComponent<PolygonCollider2D>().enabled = false;
            carriedJunk.transform.localPosition = Vector3.zero;
            Destroy(worm);
            Debug.Log("Fish dropped junk on hook! Fisherman pranked!");
        }
        else
        {
            Debug.LogWarning("wormParent not found inside Hook!");
        }

        if (IsServer)
        {
            Hook.instance.LoadReturnToRod();
        }

        carriedJunk = null;
    }

    [ServerRpc(RequireOwnership = true)]
    private void DropJunkToHookServerRpc(GameObject worm)
    {
        DropJunkToHookLocal(worm);
        DropJunkToHookObserversRpc(worm);
    }

    [ObserversRpc]
    private void DropJunkToHookObserversRpc(GameObject worm)
    {
        if (IsServer) return; // Server already executed
        DropJunkToHookLocal(worm);
    }


    //For tag Change 
    public void ChangeTag(string newTag)
    {
        if (gameObject.tag == "CatchdFish")
        {
            if (IsServer)
            {
                ChangeTagLocal(newTag);
                ChangeTagObserversRpc(newTag);
            }
            else if (IsOwner)
            {
                ChangeTagServerRpc(newTag);
            }
        }
    }

    private void ChangeTagLocal(string newTag)
    {
        gameObject.tag = newTag;
        isCatchedFish = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeTagServerRpc(string newTag)
    {
        // Server पर tag change
        ChangeTagLocal(newTag);

        // बाकी clients को inform
        ChangeTagObserversRpc(newTag);
    }

    [ObserversRpc]
    private void ChangeTagObserversRpc(string newTag)
    {
        if (IsServer) return; // server पहले ही बदल चुका है
        ChangeTagLocal(newTag);
    }


    public void ShowGameOver(bool isFishWin)
    {
        if (IsServer)
        {
            ShowGameOverLocal(isFishWin);
            ShowGameOverObserversRpc(isFishWin);
        }
        else
        {
            ShowGameOverServerRpc(isFishWin);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ShowGameOverServerRpc(bool isFishWin)
    {
        ShowGameOverLocal(isFishWin);           // server पर execute
        ShowGameOverObserversRpc(isFishWin);    // सभी clients को broadcast
    }

    [ObserversRpc]
    private void ShowGameOverObserversRpc(bool isFishWin)
    {
        if (IsServer) return; // server पहले ही execute कर चुका है
        ShowGameOverLocal(isFishWin);
    }
    private void ShowGameOverLocal(bool isFishWin)
    {
        if (IsOwner)
        {
            canMove = false;
            GameManager gm = GameManager.Instance;
            myColider.enabled = false;
            gm.gameOverPanel.SetActive(true);
            if (isFishWin)
            {
                gm.ShowGameOverMessage("You Win!");
            }
            else
            {
                gm.ShowGameOverMessage("You Lose!");
            }
        }
    }



    public void CatchByFisherman()
    {
        if (IsServer)
        {
            CatchByFishermanObserversRpc();
        }
        else
        {
            CatchByFishermanServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CatchByFishermanServerRpc()
    {
        CatchByFishermanObserversRpc();
    }

    [ObserversRpc]
    private void CatchByFishermanObserversRpc()
    {
        CatchByFishermanLocal();
    }

    //When fish is hooked and catchedf by Fisherma , that time called this 
    public void CatchByFishermanLocal()
    {
        if (IsOwner)
        {
            if (gameObject.tag == "CatchdFish")
            {
                canMove = false;
                GameManager gm = GameManager.Instance;
                myColider.enabled = false;
                gm.gameOverPanel.SetActive(true);
                gm.ShowGameOverMessage("You Lose!");
            }
        }
    }
}
