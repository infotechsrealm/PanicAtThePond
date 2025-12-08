
/*sharks = GameManager.Instance.allFishes
          .Select(f => f.transform)
          .ToArray();*/

using Mirror;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GoldenFishAI : MonoBehaviourPunCallbacks
{
    [Header("Movement Settings")]
    public float minSpeed = 1.5f;
    public float maxSpeed = 3f;
    public float turnSmoothness = 0.15f;   // smoother turning

    [Header("Shark Panic Settings")]
    public float avoidDistance = 2.5f;
    public float avoidanceStrength = 1.2f;
    public float panicSpeedMultiplier = 1.8f; // fish moves faster in panic

    [Header("Movement Bounds")]
    public Vector2 minBounds = new Vector2(-8f, -4f);
    public Vector2 maxBounds = new Vector2(8f, 4f);

    public Transform[] sharks;
    public Animator animator;

    private Vector2 moveDirection;
    private float moveSpeed;
    private float originalScaleX, originalScaleY;

    bool reachedCenter = false;
    Vector2 centerPos;

    Vector2 avoidanceVector = Vector2.zero;
    float currentSpeed;

    void Start()
    {
        if (GS.Instance.isLan)
        {
            if (!GS.Instance.IsMirrorMasterClient) return;
        }
        else
        {
            if (!photonView.IsMine) return;
        }

        originalScaleX = transform.localScale.x;
        originalScaleY = transform.localScale.y;

        PickNewDirectionAndSpeed();
        currentSpeed = moveSpeed;

        StartCoroutine(RandomDirectionRoutine());

        centerPos = new Vector2(Random.Range(minBounds.x, maxBounds.x), Random.Range(minBounds.y, maxBounds.y));

        sharks = GameManager.Instance.allFishes
          .Select(f => f.transform)
          .ToArray();
    }

    void Update()
    {
        if (GS.Instance.isLan)
        {
            if (!GS.Instance.IsMirrorMasterClient) return;
        }
        else
        {
            if (!photonView.IsMine) return;
        }

        if (animator != null)
            animator.SetBool("isMove", true);

        // Calculate avoidance blended into movement
        CalculateSharkAvoidance();

        // Speed boost if shark is near
        ApplyDynamicSpeed();

        if (!reachedCenter)
        {
            // Direction toward center
            Vector2 toCenter = (centerPos - (Vector2)transform.position).normalized;

            // ⭐ Flip based on x-direction (THIS FIXES THE UPSIDE FLIP BUG)
            if (toCenter.x < 0)
                transform.localScale = new Vector3(originalScaleX, originalScaleY, 1);
            else
                transform.localScale = new Vector3(-originalScaleX, originalScaleY, 1);

            // Move the fish
            transform.position = Vector2.MoveTowards(transform.position, centerPos, currentSpeed * Time.deltaTime);

            // Check if arrived
            if (Vector2.Distance(transform.position, centerPos) < 0.1f)
            {
                reachedCenter = true;
            }

            return; // DO NOT REMOVE
        }


        MoveFish();
    }

    // ------------------------ Natural Movement ------------------------
    void MoveFish()
    {
        Vector3 pos = transform.position;

        // Blended final direction
        Vector2 finalDir = moveDirection + avoidanceVector;

        // Smooth turning
        finalDir = Vector2.Lerp(moveDirection, finalDir.normalized, turnSmoothness);

        pos += (Vector3)(finalDir * currentSpeed * Time.deltaTime);

        bool hitX = false, hitY = false;

        if (pos.x <= minBounds.x || pos.x >= maxBounds.x)
        {
            pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
            hitX = true;
        }

        if (pos.y <= minBounds.y || pos.y >= maxBounds.y)
        {
            pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
            hitY = true;
        }

        transform.position = pos;

        // Smooth flip
        if (finalDir.x < 0)
            transform.localScale =  new Vector3(originalScaleX, originalScaleY, 1);
        else
            transform.localScale = new Vector3(-originalScaleX, originalScaleY, 1);

        // Random movement bounce
        if (hitX || hitY)
            PickNewDirectionAndSpeed();
    }

    // ------------------------ Smooth Speed Boost ------------------------
    void ApplyDynamicSpeed()
    {
        // Shark नहीं है → normal speed
        if (avoidanceVector == Vector2.zero)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, moveSpeed, 0.05f);
        }
        else
        {
            // Shark पास → panic speed boost
            float boosted = moveSpeed * panicSpeedMultiplier;
            currentSpeed = Mathf.Lerp(currentSpeed, boosted, 0.1f);
        }
    }

    // ------------------------ Shark Avoidance ------------------------
    void CalculateSharkAvoidance()
    {
        avoidanceVector = Vector2.zero;

        if (sharks == null || sharks.Length == 0)
            return;

        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (Transform s in sharks)
        {
            if (s == null) continue;

            float d = Vector2.Distance(transform.position, s.position);

            if (d < minDist)
            {
                minDist = d;
                nearest = s;
            }
        }

        if (nearest == null || minDist > avoidDistance)
            return;

        // जितना पास shark आए → उतना strong avoidance
        float t = 1f - (minDist / avoidDistance);

        avoidanceVector =
            ((Vector2)(transform.position - nearest.position)).normalized *
            t * avoidanceStrength;
    }

    // ------------------------ Random Direction ------------------------
    IEnumerator RandomDirectionRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(1, 3));
            PickNewDirectionAndSpeed();
        }
    }

    void PickNewDirectionAndSpeed()
    {
        moveDirection = new Vector2(Random.Range(-1f, 1f),
                                    Random.Range(-0.5f, 0.5f)).normalized;

        moveSpeed = Random.Range(minSpeed, maxSpeed);
    }
}
