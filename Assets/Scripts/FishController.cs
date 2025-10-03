using Photon.Pun;
using System.Collections;
using UnityEngine;
using Photon.Realtime;

public class FishController : MonoBehaviourPunCallbacks
{
    [Header("Fish Stats")]
    public int hunger = 100;
    public float speed = 5f;

    public Vector2 minBounds = new Vector2(-8f, -4f);
    public Vector2 maxBounds = new Vector2(8f, 4f);

    internal float originalScaleX;
    internal float originalScaleY;

    private Rigidbody2D rb;

    internal bool canMove = true;
    internal bool catchadeFish = false;

    public static FishController instance;

    [Header("Floating on Death")]
    private bool isDead = false;
    public float floatSpeed = 2f;

    public Transform junkHolder;
    private GameObject carriedJunk;

    // Event for fish death
    public static event System.Action<FishController> OnFishDied;

    public int myPlayerID;

    public AudioSource audioSource;
    public AudioSource audioSourceForFishMove;

    public AudioClip fishEatWarmSound;
    public AudioClip fishCameToSurfaceSound;
    public Animator animator;
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
        int myId = PhotonNetwork.LocalPlayer.ActorNumber;

        if (photonView.IsMine)
        {
            GameManager.instance.myFish = this;
        }
        GameManager.instance.allFishes.Add(this);
    }


    void FixedUpdate()
    {
        if(PhotonNetwork.IsMasterClient)
        {
            Debug.Log("I M Master ---------------------------------");

        }
        if (catchadeFish)
        {
            Debug.Log("I M Catchade ---------------------------------");
        }

        if (!photonView.IsMine)
        {
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("✅ I am the MasterClient now!");
        }

        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }



        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        // Jab bhi koi key press ho (X ya Y), tab function call karo
        if (moveX != 0 || moveY != 0)
        {
            if(!audioSourceForFishMove.isPlaying)
            {
                audioSourceForFishMove.Play();
            }

            animator.SetBool("isMove", true);
        }
        else
        {
            if (audioSourceForFishMove.isPlaying)
            {
                audioSourceForFishMove.Pause();
            }
            animator.SetBool("isMove", false);
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
        // transform.position = clampedPos;
        transform.position = Vector3.Lerp(transform.position, clampedPos, Time.deltaTime * 10);

        // Check hunger
        if (!isDead && HungerSystem.instance != null && HungerSystem.instance.hungerBar.value <= 0)
        {
            isDead = true;
            canMove = false;
            rb.linearVelocity = Vector2.zero;
            transform.GetComponent<PolygonCollider2D>().enabled = false;
            StartCoroutine(FloatToSurface());
        }
    }

    private IEnumerator FloatToSurface()
    {
        animator.SetBool("isFight", false);
        animator.SetBool("isDead", true);
        canMove = false;
        float targetY = maxBounds.y; // Surface
        while (transform.position.y < targetY)
        {
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;
            yield return null;
        }

        PlaySFX(fishCameToSurfaceSound);


        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        // Trigger event
        OnFishDied?.Invoke(this);

        if (GameManager.instance != null && GameManager.instance.gameOverText != null)
        {
            GameManager.instance.ShowGameOver("You lose!");
        }
        HungerSystem.instance.canDecrease = false;
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
        if (photonView.IsMine)
        {
            if (audioSourceForFishMove.isPlaying)
            {
                audioSourceForFishMove.Pause();
            }

            if (other.CompareTag("HookWorm"))
            {

                PlaySFX(fishEatWarmSound);
                animator.SetTrigger("isEat");
                other.gameObject.GetComponent<PolygonCollider2D>().enabled = false;
                photonView.RPC(nameof(DestroyWormRPC), RpcTarget.MasterClient, other.GetComponent<PhotonView>().ViewID);
                if (carriedJunk != null)
                {
                    DropJunkToHook();
                    return;
                }
                animator.SetBool("isFight", true);
                animator.SetBool("isMove", false);
                catchadeFish = true;
                MiniGameManager.instance.StartMiniGame();
            }

            if (other.CompareTag("GoldTrout"))
            {
                PlaySFX(fishEatWarmSound);
                GameManager.instance.isFisherMan = true;
                audioSource.Play();
                StartCoroutine(ChangeHost(other.gameObject));

            }

            if (other.CompareTag("Worm"))
            {
                PlaySFX(fishEatWarmSound);

                animator.SetTrigger("isEat");
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(other.gameObject);
                    HungerSystem.instance.AddHunger(25f);
                }
                else
                {
                    photonView.RPC(nameof(DestroyWormRPC), RpcTarget.MasterClient, other.GetComponent<PhotonView>().ViewID);
                    HungerSystem.instance.AddHunger(25f);
                }
            }

            if (other.CompareTag("Junk") && carriedJunk == null)
            {
                animator.SetTrigger("isEat");
                JunkManager junk = other.GetComponent<JunkManager>();
                carriedJunk = junk.gameObject;
                junk.CallFreezeObjectRPC();
                carriedJunk.transform.SetParent(junkHolder);
                carriedJunk.transform.localPosition = Vector3.zero;
                carriedJunk.GetComponent<PolygonCollider2D>().enabled = false;
                int viewId = other.GetComponent<PhotonView>().ViewID;
                photonView.RPC(nameof(SetJunkInFish), RpcTarget.OthersBuffered, viewId);
            }
        }
    }

    IEnumerator ChangeHost(GameObject other)
    {
        yield return new WaitForSeconds(0.25f);
        if (PhotonNetwork.IsMasterClient)
        {
            WormSpawner.instance.DestroyAllWorms();
            PhotonNetwork.Destroy(other.gameObject);
            GameManager.instance.LoadSpawnFisherman();
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            photonView.RPC(nameof(LoadDestroyAllWormsRPC), RpcTarget.MasterClient);
            photonView.RPC(nameof(DestroyWormRPC), RpcTarget.MasterClient, other.GetComponent<PhotonView>().ViewID);
            GameManager.instance.LoadGetIdAndChangeHost();
            PhotonNetwork.Destroy(gameObject);
        }
    }

    [PunRPC]
    public void LoadDestroyAllWormsRPC()
    {
        WormSpawner.instance.DestroyAllWorms();
    }

    [PunRPC]
    void RequestDestroy(int viewId)
    {
        GameObject obj = PhotonView.Find(viewId).gameObject;
        PhotonNetwork.Destroy(obj);
    }

    [PunRPC]
    void SetJunkInFish(int viewId)
    {
        carriedJunk = PhotonView.Find(viewId).gameObject;
        carriedJunk.GetComponent<PolygonCollider2D>().enabled = false;
        carriedJunk.transform.SetParent(junkHolder);
        carriedJunk.transform.localPosition = Vector3.zero;
    }

    [PunRPC]
    void DestroyWormRPC(int viewID)
    {
        PhotonView target = PhotonView.Find(viewID);
        if (target != null)
        {
            PhotonNetwork.Destroy(target.gameObject);
        }
    }

    void DropJunkToHook()
    {
        Hook hook = Hook.instance;

        if (hook != null)
        {
            HungerSystem.instance.AddHunger(75f);

            carriedJunk.transform.SetParent(hook.wormParent);
            carriedJunk.transform.localPosition = Vector3.zero;

            int viewId = hook.GetComponent<PhotonView>().ViewID;
            photonView.RPC(nameof(SetJunkInHook), RpcTarget.OthersBuffered, viewId);

            Debug.Log("Fish dropped junk on hook! Fisherman pranked!");
        }
        else
        {
            Debug.LogWarning("wormParent not found inside Hook!");
        }

     
        hook.CallRpcToReturnRod();
        carriedJunk = null;
    }

    [PunRPC]
    void SetJunkInHook(int viewId)
    {
        Debug.Log("asmfhsffiadfakdhgasoasd");
        Hook hook = PhotonView.Find(viewId).GetComponent<Hook>();
        carriedJunk.GetComponent<PolygonCollider2D>().enabled = false;
        carriedJunk.transform.SetParent(hook.wormParent);
        carriedJunk.transform.localPosition = Vector3.zero;
        carriedJunk = null;
    }

    public void PutFishInHookRPC()
    {
        Debug.Log("PutFishInHookRPC callled");
        int fishViewID = GameManager.instance.myFish.GetComponent<PhotonView>().ViewID;
        int hookViewID = Hook.instance.GetComponent<PhotonView>().ViewID;
        photonView.RPC(nameof(PutFishInHook), RpcTarget.All, fishViewID, hookViewID);
        if (photonView.IsMine)
        {
            if (GameManager.instance != null && GameManager.instance.gameOverText != null)
            {
                GameManager.instance.ShowGameOver("You lose!");
                canMove = false;
                isDead = true;
                HungerSystem.instance.canDecrease = false;
            }
        }
    }

    [PunRPC]
    public void GetCatchadFishId()
    {
        if (catchadeFish)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    public void CallWinFishRPC()
    {
        photonView.RPC(nameof(WinFish), RpcTarget.Others);
    }

    public void CallAllWinFishRPC()
    {
        photonView.RPC(nameof(WinFish), RpcTarget.All);
    }

    [PunRPC]
    public void WinFish()
    {
        if (photonView.IsMine && !isDead)
        {
            GameManager.instance.ShowGameOver("You win!");
            canMove = false;
            HungerSystem.instance.canDecrease = false;
            GetComponent<PolygonCollider2D>().enabled = false;
            isDead = true;
            animator.SetBool("isJoyful", true);
        }
    }

    [PunRPC]
    public void PutFishInHook(int fishId,int hookId)
    {
        GameObject fish = PhotonView.Find(fishId).gameObject;
        Hook hook = PhotonView.Find(hookId).GetComponent<Hook>();
        Transform fishParent = hook.wormParent;
        fish.transform.GetComponent<PolygonCollider2D>().enabled = false;
        fish.transform.SetParent(fishParent);
        fish.transform.localPosition = Vector3.zero;
        fish.transform.eulerAngles = new Vector3(0f, 0f, -90f);
        fish.GetComponent<PhotonTransformViewClassic>().enabled = false;
        if (catchadeFish)
        {
            hook.CallRpcToReturnRod();          
        }
    }

    public void CallDestroyCatchFishRPC()
    {
        photonView.RPC(nameof(DestroyCatchFish), RpcTarget.All);
    }

    [PunRPC]
    public void DestroyCatchFish()
    {
        if(catchadeFish)
        {
            if (photonView.IsMine)
            {
                if (transform.localScale == Vector3.zero)
                {
                    PhotonNetwork.Destroy(gameObject);
                }
            }
        }
    }

    void PlaySFX(AudioClip playClip)
    {
        audioSource.clip = playClip;
        audioSource.Play();
    }
}
