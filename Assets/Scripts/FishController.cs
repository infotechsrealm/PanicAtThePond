using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;

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
    public bool canMove = true;

    public static FishController instance;

    [Header("Floating on Death")]
    private bool isDead = false;
    public float floatSpeed = 2f;

    public Transform junkHolder;
    private GameObject carriedJunk;

    // Event for fish death
    public static event System.Action<FishController> OnFishDied;

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
    }


    void FixedUpdate()
    {

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
        // transform.position = clampedPos;
        transform.position = Vector3.Lerp(transform.position, clampedPos, Time.deltaTime * 10);

        // Check hunger
        if (!isDead && HungerSystem.instance != null && HungerSystem.instance.hungerBar.value <= 0)
        {
            isDead = true;
            canMove = false;
            rb.linearVelocity = Vector2.zero;
            StartCoroutine(FloatToSurface());
        }
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

        if (GameManager.instance != null && GameManager.instance.gameOverText != null)
        {
            GameManager.instance.ShowGameOver("Fish Dead!\nFisherman Wins!");
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
        if (!photonView.IsMine)
        {
            return;
        }

        if (other.CompareTag("HookWorm"))
        {
            other.gameObject.GetComponent<PolygonCollider2D>().enabled = false;

            if (carriedJunk != null)
            {
                DropJunkToHook(other.gameObject);
                return;
            }
            MiniGameManager.instance.StartMiniGame();
            MiniGameManager.instance.catchedFish = other.gameObject;
        }

        if (other.CompareTag("GoldTrout"))
        {

            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(other.gameObject);
                GameManager.instance.SpawnFisherman();
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                // Tell the master client to destroy the object via an RPC
                photonView.RPC("DestroyWormRPC", RpcTarget.MasterClient, other.GetComponent<PhotonView>().ViewID);
                GameManager.instance.SpawnFisherman();
                PhotonNetwork.Destroy(gameObject);
            }
        }

        if (other.CompareTag("Worm"))
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(other.gameObject);
                HungerSystem.instance.AddHunger(25f);
            }
            else
            {
                // Tell the master client to destroy the object via an RPC
                photonView.RPC("DestroyWormRPC", RpcTarget.MasterClient, other.GetComponent<PhotonView>().ViewID);
                HungerSystem.instance.AddHunger(25f);
            }
        }

        if (other.CompareTag("Junk") && carriedJunk == null)
        {
            carriedJunk = other.gameObject;
            carriedJunk.transform.SetParent(junkHolder);
            carriedJunk.transform.localPosition = Vector3.zero;
        }
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


    void DropJunkToHook(GameObject Fish)
    {
        Transform wormParent = Hook.instance.wormParent;

        if (wormParent != null)
        {
            HungerSystem.instance.canDecrease = canMove = true;
            HungerSystem.instance.AddHunger(75f);
            carriedJunk.transform.SetParent(wormParent);
            carriedJunk.GetComponent<PolygonCollider2D>().enabled = false;
            carriedJunk.transform.localPosition = Vector3.zero;
            Debug.Log("Fish dropped junk on hook! Fisherman pranked!");
            Hook.instance.LoadReturnToRod();
            Destroy(Fish);
        }
        else
        {
            Debug.LogWarning("wormParent not found inside Hook!");
        }

        Hook.instance.LoadReturnToRod();

        carriedJunk = null;
    }
  

}
