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
    public float maxSpeed = 2.6f;   // 🔽 reduced max speed
    public float speedSmooth = 0.08f;

    [Header("Hard Escape Settings")]
    public float avoidDistance = 1f;
    public float panicSpeedMultiplier = 1.35f; // 🔽 reduced
    public float maxEscapeSpeed = 4.2f;         // 🔽 reduced
    public float minEscapeSpeed = 2.4f;

    [Header("Movement Bounds")]
    public Vector2 minBounds = new Vector2(-8f, -4f);
    public Vector2 maxBounds = new Vector2(8f, 0f);
    public float boundaryMargin = 1.2f;

    public Transform[] sharks;
    public Animator animator;

    Vector2 moveDirection;
    float moveSpeed;
    float currentSpeed;

    float originalScaleX, originalScaleY;

    bool reachedCenter = false;
    Vector2 centerPos;

    // HARD LOCK ESCAPE
    bool escapeLocked = false;
    Vector2 lockedEscapeTarget;
    public float targetReachDistance = 0.45f;

    // flip stability
    float lastFlipX = 0f;
    public float flipDeadZone = 0.15f; // 🔥 jitter killer

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

        sharks = GameManager.Instance.allFishes
            .Select(f => f.transform)
            .ToArray();

        PickNewDirectionAndSpeed();
        currentSpeed = moveSpeed;

        centerPos = FindSafestSectorPosition();
        StartCoroutine(RandomDirectionRoutine());
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

        // ---------- DANGER CHECK ----------
        bool danger = false;
        foreach (Transform s in sharks)
        {
            if (s == null) continue;
            if (Vector2.Distance(transform.position, s.position) < avoidDistance)
            {
                danger = true;
                break;
            }
        }

        // ---------- ESCAPE LOCK ----------
        if (danger)
        {
            if (!escapeLocked)
            {
                lockedEscapeTarget = GetSmartEscapeTarget();
                escapeLocked = true;
            }
        }
        else
        {
            escapeLocked = false;
        }

        // ---------- TARGET ----------
        Vector2 target =
            escapeLocked ? lockedEscapeTarget :
            (!reachedCenter ? centerPos : (Vector2)transform.position + moveDirection);

        // ---------- DIRECTION ----------
        Vector2 dir = (target - (Vector2)transform.position);
        if (dir.magnitude > 0.001f)
            dir.Normalize();

        // ---------- STABLE FLIP (NO JITTER) ----------
        if (Mathf.Abs(dir.x) > flipDeadZone)
        {
            if (dir.x < 0 && lastFlipX >= 0)
                transform.localScale = new Vector3(originalScaleX, originalScaleY, 1);
            else if (dir.x > 0 && lastFlipX <= 0)
                transform.localScale = new Vector3(-originalScaleX, originalScaleY, 1);

            lastFlipX = dir.x;
        }

        // ---------- SPEED (SMOOTH & LIMITED) ----------
        float targetSpeed = escapeLocked
            ? Mathf.Clamp(moveSpeed * panicSpeedMultiplier, minEscapeSpeed, maxEscapeSpeed)
            : moveSpeed;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedSmooth);

        // ---------- MOVE ----------
        Vector3 pos = transform.position + (Vector3)(dir * currentSpeed * Time.deltaTime);

        pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
        pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
        transform.position = pos;

        // ---------- CENTER REACHED ----------
        if (!escapeLocked && !reachedCenter &&
            Vector2.Distance(transform.position, centerPos) < 0.15f)
        {
            reachedCenter = true;
        }

        // ---------- HARD NO-TOUCH ----------
        foreach (Transform s in sharks)
        {
            if (s == null) continue;
            float d = Vector2.Distance(transform.position, s.position);
            if (d < 1.3f)
            {
                Vector2 push = ((Vector2)transform.position - (Vector2)s.position).normalized;
                transform.position += (Vector3)(push * 0.18f);
            }
        }

        // ---------- ESCAPE TARGET DONE ----------
        if (escapeLocked &&
            Vector2.Distance(transform.position, lockedEscapeTarget) < targetReachDistance)
        {
            escapeLocked = false;
        }
    }

    Vector2 GetSmartEscapeTarget()
    {
        bool nearBoundary =
            transform.position.x < minBounds.x + boundaryMargin ||
            transform.position.x > maxBounds.x - boundaryMargin ||
            transform.position.y < minBounds.y + boundaryMargin ||
            transform.position.y > maxBounds.y - boundaryMargin;

        if (nearBoundary)
        {
            return Vector2.Lerp(
                transform.position,
                (minBounds + maxBounds) * 0.5f,
                0.6f
            );
        }

        return FindSafestSectorPosition();
    }

    Vector2 FindSafestSectorPosition()
    {
        int sectors = 12;
        float radius = Mathf.Min(
            maxBounds.x - minBounds.x,
            maxBounds.y - minBounds.y
        ) * 0.45f;

        Vector2 origin = transform.position;
        Vector2 bestPos = origin;
        float bestScore = -99999f;

        for (int i = 0; i < sectors; i++)
        {
            float angle = (360f / sectors) * i * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            Vector2 candidate = origin + dir * radius;
            candidate.x = Mathf.Clamp(candidate.x, minBounds.x, maxBounds.x);
            candidate.y = Mathf.Clamp(candidate.y, minBounds.y, maxBounds.y);

            float score = 0f;
            bool blocked = false;

            foreach (Transform s in sharks)
            {
                if (s == null) continue;
                float d = Vector2.Distance(candidate, s.position);
                if (d < 2.8f)
                {
                    blocked = true;
                    break;
                }
                score += d;
            }

            if (!blocked && score > bestScore)
            {
                bestScore = score;
                bestPos = candidate;
            }
        }

        return bestPos;
    }

    IEnumerator RandomDirectionRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(1.8f, 3.2f));
            if (!escapeLocked)
                PickNewDirectionAndSpeed();
        }
    }

    void PickNewDirectionAndSpeed()
    {
        moveDirection = new Vector2(
            Random.Range(-1f, 1f),
            Random.Range(-0.5f, 0.5f)
        ).normalized;

        moveSpeed = Random.Range(minSpeed, maxSpeed);
    }
}
