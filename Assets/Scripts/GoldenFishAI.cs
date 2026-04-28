using Mirror;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GoldenFishAI : MonoBehaviourPunCallbacks
{
    [Header("Movement Settings")]
    public float minSpeed = 8.0f;  // 🔼 EXTREMELY fast base speed (increased from 5)
    public float maxSpeed = 12.0f;   // 🔼 EXTREMELY fast max speed (increased from 7)
    public float speedSmooth = 0.1f; // Snappier speed transitions (decreased from 0.15)

    [Header("Hard Escape Settings")]
    public float avoidDistance = 11.0f; // 🔼 Detects players from much further away (increased from 7.5)
    public float panicSpeedMultiplier = 4.0f; // 🔼 Burst speed multiplier (increased from 3.5)
    public float maxEscapeSpeed = 20.0f;         // 🔼 Super fast escape cap (increased from 12.5)
    public float minEscapeSpeed = 15.0f; // 🔼 High minimum escape speed (increased from 10)

    [Header("Movement Bounds")]
    public Vector2 minBounds = new Vector2(-8f, -4f);
    public Vector2 maxBounds = new Vector2(8f, 0f);
    public float boundaryMargin = 1.0f; // 🔼 Reduced margin so it doesn't trigger in center (was 2.5)

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
    public float flipDeadZone = 0.3f; // 🔥 Increased deadzone to prevent jitter at high speeds

    // Smooth movement variables to prevent jittering
    Vector2 smoothDirection = Vector2.zero;
    public float directionSmoothTime = 0.15f; // Smoother, less robotic turns (was 0.05)
    Vector2 directionVelocity = Vector2.zero;
    Vector2 smoothPushVelocity = Vector2.zero;
    Vector2 pushVelocity = Vector2.zero; // Separate velocity ref for push smoothing
    public float pushSmoothTime = 0.03f; // 🔼 EXTREMELY fast push reactions
    
    // Update sharks list periodically to track all players
    float lastSharkUpdateTime = 0f;
    public float sharkUpdateInterval = 0.05f; // 🔼 Update very frequently - tracks players extremely well
    
    // Unpredictable escape behavior
    float lastEscapeDirectionChange = 0f;
    public float escapeDirectionChangeInterval = 0.3f; // Less spasmodic escape changes (was 0.12)
    Vector2 currentEscapeDirection = Vector2.zero;
    
    // Persistent alert system - goldfish stays alert for longer
    float lastDangerTime = 0f;
    public float alertDuration = 20.0f; // Stay alert for 20 seconds (increased from 15)
    bool isAlert = false;

    // Fatigue system - goldfish gets tired over time
    float spawnTime;
    public float timeToFullFatigue = 120f; // 2 minutes to reach minimum speed
    public float maxFatigueMultiplier = 0.4f; // Speed drops to 40% at full fatigue

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
        spawnTime = Time.time; // Track when the fish spawned

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

        // Calculate fatigue multiplier
        float aliveTime = Time.time - spawnTime;
        float fatigueProgress = Mathf.Clamp01(aliveTime / timeToFullFatigue);
        // Multiplier goes from 1.0 down to maxFatigueMultiplier (e.g. 0.4)
        float currentFatigueMultiplier = Mathf.Lerp(1.0f, maxFatigueMultiplier, fatigueProgress);

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
            
            // Effective avoid distance is reduced by fatigue
            float currentAvoidDistance = avoidDistance * currentFatigueMultiplier;

            if (distance < currentAvoidDistance)
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
        float currentPanicMultiplier = panicSpeedMultiplier * currentFatigueMultiplier;
        float currentMaxEscape = maxEscapeSpeed * currentFatigueMultiplier;
        float currentMinEscape = minEscapeSpeed * currentFatigueMultiplier;
        
        float currentTargetMoveSpeed = moveSpeed * currentFatigueMultiplier;

        float targetSpeed = (escapeLocked || isAlert)
            ? Mathf.Clamp(currentTargetMoveSpeed * currentPanicMultiplier, currentMinEscape, currentMaxEscape)
            : currentTargetMoveSpeed;

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
        // Increased threshold prevents "vibrating" around target due to high speed overshooting
        if (!escapeLocked && !reachedCenter &&
            Vector2.Distance(transform.position, centerPos) < 1.0f) // 🔼 Much larger acceptance radius (was 0.15)
        {
            reachedCenter = true;
            // Immediately pick a new direction when center reached to keep moving
            PickNewDirectionAndSpeed();
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
        int sectors = 16; // Increased sectors for better precision
        float radius = Mathf.Min(
            maxBounds.x - minBounds.x,
            maxBounds.y - minBounds.y
        ) * 0.5f;

        Vector2 origin = transform.position;
        Vector2 bestPos = origin;
        float bestScore = -99999f;
        bool foundSafe = false;

        // Fallbacks for when all sectors are "blocked" (player is within 8 units)
        Vector2 bestBlockedPos = origin;
        float bestBlockedScore = -99999f;

        for (int i = 0; i < sectors; i++)
        {
            float angle = (360f / sectors) * i * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            Vector2 candidate = origin + dir * radius;
            candidate.x = Mathf.Clamp(candidate.x, minBounds.x, maxBounds.x);
            candidate.y = Mathf.Clamp(candidate.y, minBounds.y, maxBounds.y);

            float score = 0f;
            bool blocked = false;

            // Check distance to all sharks
            foreach (Transform s in sharks)
            {
                if (s == null) continue;
                float d = Vector2.Distance(candidate, s.position);
                
                // Block if very close, but still calculate score
                if (d < 8.0f) 
                {
                    blocked = true;
                }
                
                // Score favors distance from sharks
                // Add weighted score: closer sharks have much more impact
                score += d + (100f / (d + 0.1f)) * -1.0f; 
            }

            // --- 🔼 CORNER/WALL AVOIDANCE PENALTY ---
            // Calculate how close this candidate position is to any wall
            float distToEdgeX = Mathf.Min(Mathf.Abs(candidate.x - minBounds.x), Mathf.Abs(candidate.x - maxBounds.x));
            float distToEdgeY = Mathf.Min(Mathf.Abs(candidate.y - minBounds.y), Mathf.Abs(candidate.y - maxBounds.y));
            float distToEdge = Mathf.Min(distToEdgeX, distToEdgeY);

            // Apply penalty if near edge
            // penalize "corner" spots where both X and Y are close to edge
            if (distToEdge < 1.0f) // Reduced threshold (was 1.5)
            {
                score -= 200f; // Reduced soft penalty (was 500)
            }
            if (distToEdge < 0.5f) // Reduced threshold (was 0.8)
            {
                score -= 1000f; // Penalty for being right against the wall (was 2000)
                blocked = true; // Treats wall hugging as "blocked" unless absolutely necessary
            }

            // Also penalize if the direction is pointing TOWARDS the nearest wall when we are already in the margin
            Vector2 currentPos = transform.position;
            bool currentlyNearWall = 
                currentPos.x < minBounds.x + boundaryMargin || currentPos.x > maxBounds.x - boundaryMargin ||
                currentPos.y < minBounds.y + boundaryMargin || currentPos.y > maxBounds.y - boundaryMargin;
            
            if (currentlyNearWall)
            {
                // If we are near a wall, encourage moving towards center
                // Simple dot product check: does this direction point towards center?
                Vector2 toCenter = (Vector2.zero - currentPos).normalized; // Assuming 0,0 is roughly center, or use (min+max)/2
                Vector2 centerPoint = (minBounds + maxBounds) * 0.5f;
                toCenter = (centerPoint - currentPos).normalized;
                
                float dot = Vector2.Dot(dir, toCenter);
                if (dot > 0)
                {
                    score += dot * 200f; // Bonus for moving towards center
                }
                else
                {
                    score -= 500f; // Penalty for moving further outward
                }
            }

            if (!blocked)
            {
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPos = candidate;
                    foundSafe = true;
                }
            }
            else
            {
                // Track the "least bad" option among blocked sectors
                if (score > bestBlockedScore)
                {
                    bestBlockedScore = score;
                    bestBlockedPos = candidate;
                }
            }
        }

        // If we found a safe unblocked spot, use it
        if (foundSafe)
        {
            return bestPos;
        }

        // Otherwise, use the best blocked spot (furthest from sharks among the blocked ones)
        if (bestBlockedPos != origin)
        {
             return bestBlockedPos;
        }

        // Absolute fail-safe: if stuck at origin, pick a random valid point in bounds
        // This prevents the fish from completely freezing
        Vector2 randomFallback = new Vector2(
            Random.Range(minBounds.x, maxBounds.x),
            Random.Range(minBounds.y, maxBounds.y)
        );
        return randomFallback;
    }

    IEnumerator RandomDirectionRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(1.5f, 3.0f)); // 🔼 Longer, smoother movement arcs (was 0.3-0.7)
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
