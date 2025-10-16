using Photon.Pun;
using System.Collections;
using UnityEngine;

public class GoldenFishAI : MonoBehaviourPunCallbacks
{
    [Header("Movement Settings")]
    public float minSpeed = 1.5f;            // minimum swim speed
    public float maxSpeed = 3f;              // maximum swim speed
    public float minChangeTime = 1f;         // min time before random direction change
    public float maxChangeTime = 3f;         // max time before random direction change

    [Header("Movement Bounds")]
    public Vector2 minBounds = new Vector2(-8f, -4f);
    public Vector2 maxBounds = new Vector2(8f, 4f);

    private Vector2 moveDirection;
    private float moveSpeed;
    private float originalScaleX;
    private float originalScaleY;
    private bool isPaused = false;

    public Animator animator;


    bool reachedCenter = false;
    Vector2 centerPos = Vector2.zero;
    void Start()
    {

        if (photonView.IsMine)
        {
            originalScaleX = transform.localScale.x;
            originalScaleY = transform.localScale.y;

            PickNewDirectionAndSpeed(); // initial direction and speed
            StartCoroutine(RandomDirectionRoutine());

            centerPos = new Vector2(Random.Range(minBounds.x, maxBounds.x), Random.Range(minBounds.y, maxBounds.y));

            if (transform.position.x < 0)
            {
                transform.localScale = new Vector3(-originalScaleX, originalScaleY, 1);

            }
            else if (transform.position.x > 0)
            {

                transform.localScale = new Vector3(originalScaleX, originalScaleY, 1);
            }
        }
    }


    void Update()
    {
        if (photonView.IsMine)
        {
            if (animator != null)
                animator.SetBool("isMove", !isPaused);

            if (isPaused) return;

            if (!reachedCenter)
            {

                // Move toward center first
                transform.position = Vector2.MoveTowards(transform.position, centerPos, moveSpeed * Time.deltaTime);

                if (Vector2.Distance(transform.position, centerPos) < 0.1f)
                {
                    reachedCenter = true;       // reached center, now start random movement
                    PickNewDirectionAndSpeed(); // pick first random direction
                    StartCoroutine(RandomDirectionRoutine());
                }
            }
            else
            {
                // Normal random movement logic here...

                Vector3 pos = transform.position;
                pos += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);

                bool hitXBound = false;
                bool hitYBound = false;

                // check X bounds
                if (pos.x <= minBounds.x || pos.x >= maxBounds.x)
                {
                    pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
                    hitXBound = true;
                }

                // check Y bounds
                if (pos.y <= minBounds.y || pos.y >= maxBounds.y)
                {
                    pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
                    hitYBound = true;
                }



                transform.position = pos;

                // Flip sprite based on direction
                if (moveDirection.x < 0)
                    transform.localScale = new Vector3(originalScaleX, originalScaleY, 1);
                else if (moveDirection.x > 0)
                    transform.localScale = new Vector3(-originalScaleX, originalScaleY, 1);

                // If hit bounds, pause then reverse direction + random speed
                if ((hitXBound || hitYBound) && !isPaused)
                {
                    StartCoroutine(PauseAndTurn(hitXBound, hitYBound, false));
                }
            }
        }
    }
    IEnumerator RandomDirectionRoutine()
    {
        while (true)
        {
            // wait random time before changing direction mid-swim
            yield return new WaitForSeconds(Random.Range(minChangeTime, maxChangeTime));

            // pause then pick random direction & speed
            yield return StartCoroutine(PauseAndTurn(true, true, true));
        }
    }

    void PickNewDirectionAndSpeed()
    {
        float randomX = Random.Range(-1f, 1f);
        float randomY = Random.Range(-0.5f, 0.5f);
        moveDirection = new Vector2(randomX, randomY).normalized;

        moveSpeed = Random.Range(minSpeed, maxSpeed); // pick random speed
    }

    IEnumerator PauseAndTurn(bool changeX, bool changeY, bool randomDirection = false)
    {
        if (isPaused) yield break;

        isPaused = true;

        // 🕒 short random pause before changing direction
        float waitTime = Random.Range(0f, 3f);
        yield return new WaitForSeconds(waitTime);

        if (randomDirection)
        {
            // pick random direction & speed
            PickNewDirectionAndSpeed();
        }
        else
        {
            // reverse direction after hitting bounds
            if (changeX) moveDirection.x *= -1;
            if (changeY) moveDirection.y *= -1;

            // also randomize speed after hitting bound
            moveSpeed = Random.Range(minSpeed, maxSpeed);
        }

        isPaused = false;
    }
}
