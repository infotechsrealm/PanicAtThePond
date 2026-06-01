using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Synchronizes state and frame playback time of all child animators with the root Animator.
/// This allows modular 2D body parts to animate in perfect sync without duplicating parameters/transitions.
/// </summary>
public class FishermanChildAnimatorSync : MonoBehaviour
{
    [Header("References")]
    public Animator rootAnimator;
    private List<Animator> childAnimators = new List<Animator>();
    private int lastStateHash = 0;

    private void Start()
    {
        // 1. Ensure root animator is assigned or fallback to chest
        if (rootAnimator == null)
        {
            rootAnimator = GetComponent<Animator>();
        }

        if (rootAnimator == null)
        {
            // Find "chest" child Animator to act as root/driver animator
            Transform chestTransform = transform.Find("chest");
            if (chestTransform != null)
            {
                rootAnimator = chestTransform.GetComponent<Animator>();
                Debug.Log("[FishermanChildAnimatorSync] Found and assigned child 'chest' as rootAnimator.");
            }
        }

        // 2. Ensure rootAnimator has the driver controller assigned
        if (rootAnimator == null)
        {
            rootAnimator = gameObject.AddComponent<Animator>();
        }

        if (rootAnimator.runtimeAnimatorController == null)
        {
            var controller = Resources.Load<RuntimeAnimatorController>("Fisherman created/Cheast anim/Cheast Animator");
            if (controller != null)
            {
                rootAnimator.runtimeAnimatorController = controller;
                Debug.Log("[FishermanChildAnimatorSync] Assigned Cheast Animator controller to root Animator at runtime.");
            }
            else
            {
                Debug.LogWarning("[FishermanChildAnimatorSync] Failed to load Cheast Animator at runtime.");
            }
        }

        // 3. Keep FishermanController.animator field in sync with our rootAnimator
        var controllerScript = GetComponent<FishermanController>();
        if (controllerScript != null && controllerScript.animator == null)
        {
            controllerScript.animator = rootAnimator;
            Debug.Log("[FishermanChildAnimatorSync] Assigned root Animator to FishermanController at runtime.");
        }

        // 4. Configure other modular child animators dynamically at runtime
        ConfigureChildControllersAtRuntime();

        // 5. Register child animators
        FindChildAnimators();
    }

    private void ConfigureChildControllersAtRuntime()
    {
        ConfigureChildAtRuntime("chest", "Fisherman created/Cheast anim/Cheast Animator");
        ConfigureChildAtRuntime("head", "Fisherman created/Face/Face");
        ConfigureChildAtRuntime("hand", "Fisherman created/hand anim/Hand Aniamator");
        ConfigureChildAtRuntime("oar", "Fisherman created/oar/Oar");
        ConfigureChildAtRuntime("road", "Fisherman created/Road/Road");
        ConfigureChildAtRuntime("boat", "Fisherman created/Boat/BotAnimator");
    }

    private void ConfigureChildAtRuntime(string childName, string resourcePath)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            // Case-insensitive search
            for (int i = 0; i < transform.childCount; i++)
            {
                var t = transform.GetChild(i);
                if (t.name.ToLowerInvariant() == childName.ToLowerInvariant())
                {
                    child = t;
                    break;
                }
            }
        }

        if (child != null)
        {
            Animator anim = child.GetComponent<Animator>();
            if (anim == null)
            {
                anim = child.gameObject.AddComponent<Animator>();
            }

            if (anim.runtimeAnimatorController == null)
            {
                var controller = Resources.Load<RuntimeAnimatorController>(resourcePath);
                if (controller != null)
                {
                    anim.runtimeAnimatorController = controller;
                    Debug.Log($"[FishermanChildAnimatorSync] Runtime assigned '{resourcePath}' to '{child.name}'.");
                }
                else
                {
                    Debug.LogWarning($"[FishermanChildAnimatorSync] Failed to load runtime controller at '{resourcePath}'.");
                }
            }
        }
    }

    /// <summary>
    /// Finds and registers all Animator components on child GameObjects.
    /// Excludes the root Animator itself.
    /// </summary>
    public void FindChildAnimators()
    {
        childAnimators.Clear();
        Animator[] anims = GetComponentsInChildren<Animator>(true);
        foreach (Animator a in anims)
        {
            if (a != null && a != rootAnimator && a.gameObject != gameObject)
            {
                childAnimators.Add(a);
            }
        }
        Debug.Log($"[FishermanChildAnimatorSync] Found {childAnimators.Count} child animators.");
    }

    private void LateUpdate()
    {
        if (rootAnimator == null || childAnimators.Count == 0)
            return;

        // Get the current state of the first layer of the root animator
        AnimatorStateInfo rootState = rootAnimator.GetCurrentAnimatorStateInfo(0);
        int currentStateHash = rootState.shortNameHash;

        // If the state changed on the root, immediately transition all active child animators
        if (currentStateHash != lastStateHash)
        {
            lastStateHash = currentStateHash;
            for (int i = 0; i < childAnimators.Count; i++)
            {
                Animator child = childAnimators[i];
                if (child != null && child.isActiveAndEnabled && child.runtimeAnimatorController != null)
                {
                    child.Play(currentStateHash, 0, 0f);
                }
            }
        }
        else
        {
            // Sync playback time to align sprite animation frames precisely
            float rootTime = rootState.normalizedTime;
            for (int i = 0; i < childAnimators.Count; i++)
            {
                Animator child = childAnimators[i];
                if (child != null && child.isActiveAndEnabled && child.runtimeAnimatorController != null)
                {
                    AnimatorStateInfo childState = child.GetCurrentAnimatorStateInfo(0);
                    if (childState.shortNameHash == currentStateHash)
                    {
                        // Snap if they drift apart (e.g. on state entry or framerate fluctuations)
                        if (Mathf.Abs(childState.normalizedTime - rootTime) > 0.02f)
                        {
                            child.Play(currentStateHash, 0, rootTime);
                        }
                    }
                    else
                    {
                        // If for some reason the state didn't transition, force it now
                        child.Play(currentStateHash, 0, rootTime);
                    }
                }
            }
        }
    }
}
