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
    public float avoidDistance = 2.5f; // Increased detection range - fish will start fleeing earlier
    public float panicSpeedMultiplier = 1.5f; // Increased escape speed multiplier
    public float maxEscapeSpeed = 5.0f;         // Increased max escape speed
    public float minEscapeSpeed = 3.0f; // Increased min escape speed

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

    // Smooth movement variables to prevent jittering
    Vector2 smoothDirection = Vector2.zero;
    public float directionSmoothTime = 0.15f;
    Vector2 directionVelocity = Vector2.zero;
    Vector2 smoothPushVelocity = Vector2.zero;
    Vector2 pushVelocity = Vector2.zero; // Separate velocity ref for push smoothing
    public float pushSmoothTime = 0.1f;
    
    // Update sharks list periodically to track all players
    float lastSharkUpdateTime = 0f;
    public float sharkUpdateInterval = 0.2f; // Update every 0.2 seconds

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

        // Initialize sharks list - will be updated dynamically in Update()
        UpdateSharksList();

        PickNewDirectionAndSpeed();
        currentSpeed = moveSpeed;
        smoothDirection = moveDirection;

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

        // ---------- UPDATE SHARKS LIST DYNAMICALLY ----------
        // Update sharks list periodically to track all players (host and client)
        if (Time.time - lastSharkUpdateTime >= sharkUpdateInterval)
        {
            UpdateSharksList();
            lastSharkUpdateTime = Time.time;
        }

        // ---------- DANGER CHECK & DIRECT FLEE DIRECTION ----------
        bool danger = false;
        Vector2 fleeDirection = Vector2.zero;
        float closestDistance = float.MaxValue;
        int nearbyPlayerCount = 0;
        
        // Check ALL players to find the closest one (even if outside avoidDistance)
        foreach (Transform s in sharks)
        {
            if (s == null) continue;
            float distance = Vector2.Distance(transform.position, s.position);
            
            // Track closest player distance for clearing escape lock
            if (distance < closestDistance)
                closestDistance = distance;
            
            if (distance < avoidDistance)
            {
                danger = true;
                nearbyPlayerCount++;
                
                // Calculate direction AWAY from this player (flee direction)
                Vector2 toPlayer = ((Vector2)s.position - (Vector2)transform.position);
                if (toPlayer.magnitude > 0.01f)
                {
                    Vector2 awayFromPlayer = -toPlayer.normalized;
                    // Weight by distance - closer players have stronger influence
                    float weight = 1f / (distance + 0.1f);
                    fleeDirection += awayFromPlayer * weight;
                }
            }
        }
        
        // Normalize the combined flee direction - ensure it always points AWAY from players
        if (nearbyPlayerCount > 0)
        {
            if (fleeDirection.magnitude > 0.01f)
            {
                fleeDirection.Normalize();
            }
            else
            {
                // Fallback: if somehow we have no valid direction, calculate away from closest player
                Transform closestPlayer = null;
                float minDist = float.MaxValue;
                foreach (Transform s in sharks)
                {
                    if (s == null) continue;
                    float d = Vector2.Distance(transform.position, s.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        closestPlayer = s;
                    }
                }
                if (closestPlayer != null)
                {
                    Vector2 toPlayer = ((Vector2)closestPlayer.position - (Vector2)transform.position);
                    if (toPlayer.magnitude > 0.01f)
                    {
                        fleeDirection = -toPlayer.normalized; // AWAY from player
                    }
                }
            }
        }

        // ---------- ESCAPE LOCK ----------
        if (danger)
        {
            if (!escapeLocked)
            {
                // Calculate escape target that moves directly away from players
                lockedEscapeTarget = GetDirectFleeTarget(fleeDirection, closestDistance);
                escapeLocked = true;
            }
            else
            {
                // Update escape target while in danger to continuously move away
                lockedEscapeTarget = GetDirectFleeTarget(fleeDirection, closestDistance);
            }
        }
        else
        {
            // Only clear escape lock if we're far enough from danger
            if (closestDistance > avoidDistance * 1.5f)
            {
                escapeLocked = false;
            }
        }

        // ---------- TARGET ----------
        Vector2 target;
        if (escapeLocked)
        {
            // When escaping, use the locked escape target (calculated to move away from players)
            target = lockedEscapeTarget;
        }
        else
        {
            target = !reachedCenter ? centerPos : (Vector2)transform.position + moveDirection;
        }

        // ---------- DIRECTION (SMOOTH INTERPOLATION) ----------
        Vector2 targetDir = (target - (Vector2)transform.position);
        if (targetDir.magnitude > 0.001f)
            targetDir.Normalize();

        // Smooth direction changes to prevent jittering
        smoothDirection = Vector2.SmoothDamp(smoothDirection, targetDir, ref directionVelocity, directionSmoothTime);
        
        // Use smoothed direction for movement
        Vector2 dir = smoothDirection;

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

        // ---------- SMOOTH PUSH AWAY FROM PLAYERS (PREVENTS JITTERING) ----------
        // Only apply push when very close - main escape is handled by direction logic above
        Vector2 totalPush = Vector2.zero;
        int pushCount = 0;
        float pushThreshold = 1.5f; // Slightly larger push threshold for safety buffer
        
        foreach (Transform s in sharks)
        {
            if (s == null) continue;
            float d = Vector2.Distance(transform.position, s.position);
            if (d < pushThreshold && d > 0.01f) // Avoid division by zero
            {
                // Direction AWAY from player (gold fish position - player position)
                Vector2 pushDir = ((Vector2)transform.position - (Vector2)s.position).normalized;
                // Scale push strength by distance (stronger when closer)
                float pushStrength = (pushThreshold - d) / pushThreshold;
                // Stronger push when very close to ensure separation
                totalPush += pushDir * pushStrength * 0.25f;
                pushCount++;
            }
        }
        
        // Smooth the push velocity to prevent jittering
        if (pushCount > 0)
        {
            Vector2 averagePush = totalPush / pushCount;
            smoothPushVelocity = Vector2.SmoothDamp(smoothPushVelocity, averagePush, ref pushVelocity, pushSmoothTime);
            pos += (Vector3)smoothPushVelocity;
        }
        else
        {
            // Gradually reduce push velocity when no players are nearby
            smoothPushVelocity = Vector2.Lerp(smoothPushVelocity, Vector2.zero, pushSmoothTime * 2f);
            pos += (Vector3)smoothPushVelocity;
        }

        pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
        pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
        transform.position = pos;

        // ---------- CENTER REACHED ----------
        if (!escapeLocked && !reachedCenter &&
            Vector2.Distance(transform.position, centerPos) < 0.15f)
        {
            reachedCenter = true;
        }

        // ---------- ESCAPE TARGET DONE ----------
        if (escapeLocked &&
            Vector2.Distance(transform.position, lockedEscapeTarget) < targetReachDistance)
        {
            escapeLocked = false;
        }
    }

    Vector2 GetDirectFleeTarget(Vector2 fleeDirection, float closestPlayerDistance)
    {
        // Calculate a target point that moves directly away from players
        float fleeDistance = Mathf.Max(3f, avoidDistance * 1.5f);
        
        // If we're very close to a player, flee more aggressively
        if (closestPlayerDistance < avoidDistance * 0.6f)
        {
            fleeDistance = Mathf.Max(4f, avoidDistance * 2f);
        }
        
        Vector2 targetPos = (Vector2)transform.position + fleeDirection * fleeDistance;
        
        // Ensure target is within bounds, but prioritize fleeing direction
        targetPos.x = Mathf.Clamp(targetPos.x, minBounds.x, maxBounds.x);
        targetPos.y = Mathf.Clamp(targetPos.y, minBounds.y, maxBounds.y);
        
        // If we'd hit a boundary, try to find a better angle
        bool nearBoundary =
            targetPos.x <= minBounds.x + boundaryMargin ||
            targetPos.x >= maxBounds.x - boundaryMargin ||
            targetPos.y <= minBounds.y + boundaryMargin ||
            targetPos.y >= maxBounds.y - boundaryMargin;

        if (nearBoundary)
        {
            // When near boundary, find the safest direction that still moves away from players
            return FindSafestSectorPosition();
        }

        return targetPos;
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

    // Update sharks list from GameManager to track all players (host and client)
    void UpdateSharksList()
    {
        if (GameManager.Instance != null && GameManager.Instance.allFishes != null)
        {
            sharks = GameManager.Instance.allFishes
                .Where(f => f != null && f.transform != null) // Filter out null references
                .Select(f => f.transform)
                .ToArray();
        }
    }
}
