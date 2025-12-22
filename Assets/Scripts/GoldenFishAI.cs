using Mirror;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GoldenFishAI : MonoBehaviourPunCallbacks
{
    [Header("Movement Settings")]
    public float minSpeed = 5.0f;  // 🔼 EXTREMELY fast base speed (reduced slightly)
    public float maxSpeed = 7.0f;   // 🔼 EXTREMELY fast max speed (reduced slightly)
    public float speedSmooth = 0.15f; // Faster speed transitions

    [Header("Hard Escape Settings")]
    public float avoidDistance = 7.5f; // 🔼 EXTREMELY large detection range - fish detects players from very far away
    public float panicSpeedMultiplier = 3.5f; // 🔼 EXTREMELY fast escape multiplier
    public float maxEscapeSpeed = 12.5f;         // 🔼 EXTREMELY fast max escape speed (reduced slightly)
    public float minEscapeSpeed = 10.0f; // 🔼 EXTREMELY fast min escape speed (reduced slightly)

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
    public float directionSmoothTime = 0.04f; // 🔼 EXTREMELY fast direction changes - very reactive
    Vector2 directionVelocity = Vector2.zero;
    Vector2 smoothPushVelocity = Vector2.zero;
    Vector2 pushVelocity = Vector2.zero; // Separate velocity ref for push smoothing
    public float pushSmoothTime = 0.03f; // 🔼 EXTREMELY fast push reactions
    
    // Update sharks list periodically to track all players
    float lastSharkUpdateTime = 0f;
    public float sharkUpdateInterval = 0.05f; // 🔼 Update very frequently - tracks players extremely well
    
    // Unpredictable escape behavior
    float lastEscapeDirectionChange = 0f;
    public float escapeDirectionChangeInterval = 0.2f; // Change escape direction frequently
    Vector2 currentEscapeDirection = Vector2.zero;
    
    // Persistent alert system - goldfish stays alert for longer
    float lastDangerTime = 0f;
    public float alertDuration = 15.0f; // Stay alert for 15 seconds after last danger - much longer!
    bool isAlert = false;

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

        // ---------- ESCAPE LOCK & PERSISTENT ALERT SYSTEM ----------
        if (danger)
        {
            lastDangerTime = Time.time;
            isAlert = true;
            
            // Add unpredictable direction changes during escape
            if (Time.time - lastEscapeDirectionChange >= escapeDirectionChangeInterval)
            {
                // Occasionally add a random perpendicular component to make escape unpredictable
                if (fleeDirection.magnitude > 0.01f)
                {
                    Vector2 perpendicular = new Vector2(-fleeDirection.y, fleeDirection.x);
                    if (Random.value < 0.5f) perpendicular = -perpendicular; // Random left or right
                    fleeDirection = (fleeDirection + perpendicular * Random.Range(0.3f, 0.7f)).normalized;
                }
                lastEscapeDirectionChange = Time.time;
            }
            
            if (!escapeLocked)
            {
                // Calculate escape target that moves directly away from players
                lockedEscapeTarget = GetDirectFleeTarget(fleeDirection, closestDistance);
                escapeLocked = true;
            }
            else
            {
                // Update escape target while in danger to continuously move away (with unpredictability)
                lockedEscapeTarget = GetDirectFleeTarget(fleeDirection, closestDistance);
            }
        }
        else
        {
            // Check if still in alert period (stays alert for longer even after danger passes)
            float timeSinceLastDanger = Time.time - lastDangerTime;
            if (timeSinceLastDanger < alertDuration)
            {
                // Still in alert period - maintain escape behavior
                isAlert = true;
                
                // Even when not in immediate danger, if alert, still try to maintain distance
                if (closestDistance < avoidDistance * 2.5f) // Extended alert range
                {
                    // Continue escaping even if technically "safe"
                    if (!escapeLocked)
                    {
                        // Calculate escape direction away from closest player
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
                                Vector2 awayFromPlayer = -toPlayer.normalized;
                                lockedEscapeTarget = GetDirectFleeTarget(awayFromPlayer, minDist);
                                escapeLocked = true;
                            }
                        }
                    }
                    else
                    {
                        // Update escape target to continue moving away
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
                                Vector2 awayFromPlayer = -toPlayer.normalized;
                                lockedEscapeTarget = GetDirectFleeTarget(awayFromPlayer, minDist);
                            }
                        }
                    }
                }
            }
            else
            {
                isAlert = false;
            }
            
            // Only clear escape lock if we're VERY far from danger AND alert period has passed
            if (closestDistance > avoidDistance * 4.5f && !isAlert) // 🔼 Requires EXTREMELY more distance AND no alert
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
        // Maintain high speed even when alert (not just when escape locked)
        float targetSpeed = (escapeLocked || isAlert)
            ? Mathf.Clamp(moveSpeed * panicSpeedMultiplier, minEscapeSpeed, maxEscapeSpeed)
            : moveSpeed;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedSmooth);

        // ---------- MOVE ----------
        Vector3 pos = transform.position + (Vector3)(dir * currentSpeed * Time.deltaTime);

        // ---------- SMOOTH PUSH AWAY FROM PLAYERS (PREVENTS JITTERING) ----------
        // Only apply push when very close - main escape is handled by direction logic above
        Vector2 totalPush = Vector2.zero;
        int pushCount = 0;
        float pushThreshold = 4.0f; // 🔼 EXTREMELY large push threshold - reacts very early
        
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
                // 🔼 EXTREMELY strong push when very close to ensure maximum separation
                totalPush += pushDir * pushStrength * 1.2f; // Much stronger push
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
        // Only clear escape target if we've reached it AND we're not in alert mode
        if (escapeLocked &&
            Vector2.Distance(transform.position, lockedEscapeTarget) < targetReachDistance)
        {
            // Don't clear escape lock if still alert - immediately pick new escape target
            if (isAlert)
            {
                // Find new escape target immediately
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
                        Vector2 awayFromPlayer = -toPlayer.normalized;
                        lockedEscapeTarget = GetDirectFleeTarget(awayFromPlayer, minDist);
                    }
                }
            }
            else
            {
                escapeLocked = false;
            }
        }
    }

    Vector2 GetDirectFleeTarget(Vector2 fleeDirection, float closestPlayerDistance)
    {
        // Calculate a target point that moves directly away from players
        float fleeDistance = Mathf.Max(10f, avoidDistance * 3.0f); // 🔼 EXTREMELY longer flee distances
        
        // If we're very close to a player, flee more aggressively
        if (closestPlayerDistance < avoidDistance * 0.6f)
        {
            fleeDistance = Mathf.Max(15f, avoidDistance * 4.5f); // 🔼 EXTREMELY longer flee when very close
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
                if (d < 8.0f) // 🔼 EXTREMELY larger avoidance radius when finding safe positions
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
            yield return new WaitForSeconds(Random.Range(0.3f, 0.7f)); // 🔼 EXTREMELY frequent direction changes - very unpredictable
            // Even when escape locked or alert, occasionally change direction to be unpredictable
            if (!escapeLocked || (isAlert && Random.value < 0.4f)) // 40% chance to change direction even when alert
                PickNewDirectionAndSpeed();
        }
    }

    void PickNewDirectionAndSpeed()
    {
        // More erratic movement patterns - harder to predict
        moveDirection = new Vector2(
            Random.Range(-1f, 1f),
            Random.Range(-0.8f, 0.8f) // More vertical movement variation
        ).normalized;

        moveSpeed = Random.Range(minSpeed, maxSpeed);
        
        // Occasionally add sudden speed bursts
        if (Random.value < 0.3f) // 30% chance
        {
            moveSpeed *= 1.4f; // Sudden speed boost
        }
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
