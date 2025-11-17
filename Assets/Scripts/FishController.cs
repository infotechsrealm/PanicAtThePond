using Mirror;
using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class FishController : MonoBehaviourPunCallbacks 
{
    public static FishController Instance;

    public PhotonTransformViewClassic photonTransformViewClassic;

    public FishController_Mirror fishController_Mirror;

    public FishermanController fishermanController;

    [SerializeField]
    internal InputActionReference inputAction;
  
    [Header("Fish Stats")]
    public int hunger = 100;
    public float speed = 5f;

    public Vector2 minBounds = new Vector2(-8f, -4f);
    public Vector2 maxBounds = new Vector2(8f, 4f);

    public float floatSpeed = 2f;
    public Transform junkHolder;
    public int myPlayerID;

    public AudioSource 
           audioSource,
           audioSourceForFishMove;

    public AudioClip fishEatWarmSound;
    public AudioClip fishCameToSurfaceSound;

    public Animator animator;

    internal float originalScaleX;
    internal float originalScaleY;
    public bool canMove = true;
    internal bool catchadeFish = false;
    internal GameObject carriedJunk;

    [Header("Floating on Death")]
    private bool isDead = false;
   internal bool isFisherMan= false;
    private Rigidbody2D rb;

    public NetworkIdentity mirrorIdentity;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        originalScaleX = transform.localScale.x;
        originalScaleY = transform.localScale.y;
        rb = GetComponent<Rigidbody2D>();
    }
    void Start()
    {
        if (GS.Instance.isLan)
        {
            if (mirrorIdentity != null && mirrorIdentity.isLocalPlayer)
            {
                GameManager.Instance.myFish = this;
            }
        }
        else
        {
            if (photonView.IsMine)
            {
                GameManager.Instance.myFish = this;
            }
        }
            GameManager.Instance.allFishes.Add(this);
    }

    void FixedUpdate()
    {
        if(isFisherMan)
        {
            return;
        }

        if (GS.Instance.isLan)
        {
            if (!mirrorIdentity.isLocalPlayer)
            {
                return;
            }
        }
        else
        {
            if (!photonView.IsMine)
            {
                return;
            }
        }
        
        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 move;
        if (GS.Instance.isLan)
        {
            if(mirrorIdentity.isLocalPlayer)
            {
                // move = inputAction.action.ReadValue<Vector2>();
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");

                 move = new Vector2(horizontal, vertical);
            }
            else
            {
                return;
            }
        }
        else
        {
             move = inputAction.action.ReadValue<Vector2>();
        }

        float moveX = move.x;
        float moveY = move.y;

        if (moveX != 0 || moveY != 0)
        {
            if (!audioSourceForFishMove.isPlaying)
            {
                GS.Instance.SetVolume(audioSourceForFishMove);
                audioSourceForFishMove.Play();
            }

            if (!animator.GetBool("isMove"))
            {
                animator.SetBool("isMove", true);
            }
        }
        else
        {
            if (audioSourceForFishMove.isPlaying)
            {
                GS.Instance.SetVolume(audioSourceForFishMove);
                audioSourceForFishMove.Pause();
            }

            if (animator.GetBool("isMove"))
            {
                animator.SetBool("isMove", false);
            }
        }

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
        transform.position = Vector3.Lerp(transform.position, clampedPos, Time.deltaTime * 10);

        // Check hunger
        if (!isDead && HungerSystem.Instance != null && HungerSystem.Instance.hungerBar.value <= 0)
        {
            StartCoroutine(FloatToSurface());
        }

        if (carriedJunk != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame || Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("Fish is leve the junk");
                if (GS.Instance.isLan)
                {
                    fishController_Mirror.TryLeaveJunk(carriedJunk.GetComponent<NetworkIdentity>());
                    carriedJunk = null;
                }
                else
                {
                    int junkId = carriedJunk.GetComponent<PhotonView>().ViewID;
                    photonView.RPC(nameof(LeaveJunk), RpcTarget.AllBuffered, junkId);
                    PhotonNetwork.SendAllOutgoingCommands();
                    carriedJunk = null;
                }
            }
        }
    }

    private IEnumerator FloatToSurface()
    {
        GameManager.Instance.CallLessPlayerCountRPC();

        animator.SetBool("isFight", false);
        animator.SetBool("isDead", true);

        canMove = false;
        isDead = true;

        rb.linearVelocity = Vector2.zero;
        transform.GetComponent<PolygonCollider2D>().enabled = false;

        if (carriedJunk != null)
        {
            int viewId = carriedJunk.GetComponent<PhotonView>().ViewID;
            photonView.RPC(nameof(LeaveJunk), RpcTarget.OthersBuffered, viewId);
        }

        float targetY = maxBounds.y; // Surface
        HungerSystem.Instance.canDecrease = false;

        if (GameManager.Instance != null && GameManager.Instance.gameOverText != null)
        {
            GameManager.Instance.ShowGameOver("You lose!");
        }

        while (transform.position.y < targetY)
        {
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;
            yield return null;
        }

        PlaySFX(fishCameToSurfaceSound);

        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);

    }

    //when Fish is Die and User press space key.
    [PunRPC]
    public void LeaveJunk(int junkId)
    {
        GameObject junk = PhotonView.Find(junkId).gameObject;
        junk.GetComponent<JunkManager>().LeaveByFish();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (photonView.IsMine || mirrorIdentity.isLocalPlayer)
        {
            if (audioSourceForFishMove.isPlaying)
            {
                GS.Instance.SetVolume(audioSourceForFishMove);
                audioSourceForFishMove.Pause();
            }

            if (other.CompareTag("HookWorm"))
            {
                PlaySFX(fishEatWarmSound);
                animator.SetTrigger("isEat");
                other.gameObject.GetComponent<PolygonCollider2D>().enabled = false;
                photonView.RPC(nameof(DestroyWormRPC), RpcTarget.MasterClient, other.GetComponent<PhotonView>().ViewID);
                PhotonNetwork.SendAllOutgoingCommands();

                if (carriedJunk != null)
                {
                    DropJunkToHook();
                    return;
                }

                animator.SetBool("isFight", true);
                animator.SetBool("isMove", false);
                catchadeFish = true;
                MiniGameManager.Instance.StartMiniGame();

            }

            if (other.CompareTag("GoldTrout"))
            {
                animator.SetTrigger("isEat");
                GameManager.Instance.isFisherMan = true;
                PlaySFX(fishEatWarmSound);
                GameManager.Instance.goldWormEatByFish = true;
                GS.Instance.SetVolume(audioSource);
                audioSource.Play();
                other.gameObject.transform.localScale = Vector3.zero;
                other.gameObject.GetComponent<PolygonCollider2D>().enabled = false;
                StartCoroutine(GenerateFisherMan(other.gameObject));
            }

            if (other.CompareTag("Worm"))
            {
                PlaySFX(fishEatWarmSound);
                animator.SetTrigger("isEat");

                if (GS.Instance.isLan)
                {
                    fishController_Mirror.Destroy_Mirror(other.gameObject);
                    HungerSystem.Instance.AddHunger(25f);
                }
                else
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        PhotonNetwork.Destroy(other.gameObject);
                        HungerSystem.Instance.AddHunger(25f);
                    }
                    else
                    {
                        photonView.RPC(nameof(DestroyWormRPC), RpcTarget.MasterClient, other.GetComponent<PhotonView>().ViewID);
                        PhotonNetwork.SendAllOutgoingCommands();
                        HungerSystem.Instance.AddHunger(25f);
                    }
                }
            }

            if (other.CompareTag("Junk") && carriedJunk == null)
            {
                Debug.Log("collide with junk");

                animator.SetTrigger("isEat");
                JunkManager junk = other.GetComponent<JunkManager>();
                carriedJunk = junk.gameObject;

                if (GS.Instance.isLan)
                {
                    Debug.Log("is master Client = " + GS.Instance.IsMirrorMasterClient);
                    junk.CallFreezeObjectRPC();
                    fishController_Mirror.TryPickupJunk(carriedJunk.GetComponent<NetworkIdentity>());
                }
                else
                {
                    carriedJunk.transform.SetParent(junkHolder);
                    carriedJunk.transform.localPosition = Vector3.zero;
                    carriedJunk.GetComponent<PolygonCollider2D>().enabled = false;

                    junk.CallFreezeObjectRPC();
                    int viewId = other.GetComponent<PhotonView>().ViewID;
                    photonView.RPC(nameof(SetJunkInFish), RpcTarget.OthersBuffered, viewId);
                    PhotonNetwork.SendAllOutgoingCommands();
                }
            }
        }
    }


    [PunRPC]
    void SetJunkInFish(int viewId)
    {
        carriedJunk = PhotonView.Find(viewId).gameObject;
        carriedJunk.GetComponent<PolygonCollider2D>().enabled = false;
        carriedJunk.transform.SetParent(junkHolder);
        carriedJunk.transform.localPosition = Vector3.zero;
    }

    IEnumerator GenerateFisherMan(GameObject other)
    {
        yield return new WaitForSeconds(0.25f);

        if (GS.Instance.isLan)
        {
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn.identity != null)
                {
                    Debug.Log("RemovePlayerForConnection ");
                    NetworkServer.RemovePlayerForConnection(conn, true);
                }
            }

            Debug.Log("GenerateFisherMan");
            fishController_Mirror.Destroy_Mirror(other);
            transform.localScale =Vector3.zero;
            isFisherMan = true;
            //fishController_Mirror.Destroy_Mirror(gameObject);
            //GameManager.Instance.LoadSpawnFisherman();
            //WormSpawner.Instance.DestroyAllWorms();
            //FishController_Mirror.Instance.Destroy_Mirror(gameObject);


        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                WormSpawner.Instance.DestroyAllWorms();
                PhotonNetwork.Destroy(other.gameObject);
                GameManager.Instance.LoadSpawnFisherman();
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                photonView.RPC(nameof(LoadDestroyAllWormsRPC), RpcTarget.MasterClient);
                photonView.RPC(nameof(DestroyWormRPC), RpcTarget.MasterClient, other.GetComponent<PhotonView>().ViewID);
                GameManager.Instance.LoadGetIdAndChangeHost();
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    [PunRPC]
    public void LoadDestroyAllWormsRPC()
    {
        WormSpawner.Instance.DestroyAllWorms();
    }

    [PunRPC]
    void DestroyWormRPC(int viewID)
    {
        PhotonView pv = PhotonView.Find(viewID);
        if (pv != null)
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.Destroy(pv.gameObject);
        }
    }

    void DropJunkToHook()
    {
        Hook hook = Hook.Instance;

        if (hook != null)
        {
            HungerSystem.Instance.AddHunger(75f);

            carriedJunk.transform.SetParent(hook.wormParent);
            carriedJunk.transform.localPosition = Vector3.zero;

            int viewId = hook.GetComponent<PhotonView>().ViewID;
            photonView.RPC(nameof(SetJunkInHook), RpcTarget.OthersBuffered, viewId);
            PhotonNetwork.SendAllOutgoingCommands();

        }
        hook.CallRpcToReturnRod();
        carriedJunk = null;
    }

    [PunRPC]
    void SetJunkInHook(int viewId)
    {
        Hook hook = PhotonView.Find(viewId).GetComponent<Hook>();
        carriedJunk.GetComponent<PolygonCollider2D>().enabled = false;
        carriedJunk.transform.SetParent(hook.wormParent);
        carriedJunk.transform.localPosition = Vector3.zero;
        carriedJunk = null;
    }


    //When Fish Loss in MashPhase.
    public void PutFishInHook()
    {
        int fishViewID = GameManager.Instance.myFish.GetComponent<PhotonView>().ViewID;
        int hookViewID = Hook.Instance.GetComponent<PhotonView>().ViewID;
        photonView.RPC(nameof(PutFishInHookRPC), RpcTarget.All, fishViewID, hookViewID);
        PhotonNetwork.SendAllOutgoingCommands();
        if (photonView.IsMine)
        {
            if (GameManager.Instance != null && GameManager.Instance.gameOverText != null)
            {
                GameManager.Instance.ShowGameOver("You lose!");
                canMove = false;
                isDead = true;
                HungerSystem.Instance.canDecrease = false;
            }
        }
    }
  

    [PunRPC]
    public void PutFishInHookRPC(int fishId, int hookId)
    {
        //Do not Change Linr.
        GameObject fish = PhotonView.Find(fishId).gameObject;
        fish.GetComponent<PhotonTransformViewClassic>().enabled = false;
        Hook hook = PhotonView.Find(hookId).GetComponent<Hook>();
        Transform fishParent = hook.wormParent;
        fish.transform.GetComponent<PolygonCollider2D>().enabled = false;
        fish.transform.SetParent(fishParent);
        fish.transform.eulerAngles = new Vector3(0f, 0f, -90f);
        fish.transform.localPosition = Vector3.zero;
        if (catchadeFish)
        {
            hook.CallRpcToReturnRod();
        }
        PhotonNetwork.SendAllOutgoingCommands();
    }

    //call this function in GameManager
    public void CallWinFishRPC()
    {
        photonView.RPC(nameof(WinFish), RpcTarget.Others);
    }

    //call this function in GameManager
    public void CallAllWinFishRPC()
    {
        photonView.RPC(nameof(WinFish), RpcTarget.All);
    }

    [PunRPC]
    public void WinFish()
    {
        if (photonView.IsMine && !isDead)
        {
            GameManager.Instance.ShowGameOver("You win!");
            canMove = false;
            HungerSystem.Instance.canDecrease = false;
            GetComponent<PolygonCollider2D>().enabled = false;
            isDead = true;
            animator.SetBool("isJoyful", true);
        }
    }

    //When Hook is Destroy Call This Function
    public void DestroyCatchFish()
    {
        if (catchadeFish)
        {
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
    void PlaySFX(AudioClip playClip)
    {
        audioSource.clip = playClip;
        GS.Instance.SetVolume(audioSource);
        audioSource.Play();
    }

    //When Game is paused Fish movement is stoped and Fad it.
    public void CallGamePauseRPC(bool isPause)
    {
        int fishViewID = GameManager.Instance.myFish.GetComponent<PhotonView>().ViewID;
        photonView.RPC(nameof(GamePause), RpcTarget.AllBuffered, isPause, fishViewID);
        PhotonNetwork.SendAllOutgoingCommands();
    }

    [PunRPC]
    public void GamePause(bool isPause, int fishID)
    {
        int thisFishID = GetComponent<PhotonView>().ViewID;

        if (thisFishID == fishID)
        {
            if (isPause)
            {
                transform.GetComponent<PolygonCollider2D>().enabled = false;
                var sr = GetComponent<SpriteRenderer>();
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.5f);
                canMove = false;
            }
            else
            {
                transform.GetComponent<PolygonCollider2D>().enabled = true;
                var sr = GetComponent<SpriteRenderer>();
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
                canMove = true;
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (GS.Instance.isLan)
        {

        }
        else
        {
            if (!isDead)
            {
                GameManager.Instance.CallLessPlayerCountRPC();
                PhotonNetwork.SendAllOutgoingCommands(); // send it now
            }
        }
    }
}
