using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Synchronizes state and frame playback time of all child animators with the root Animator.
/// This allows modular 2D body parts to animate in perfect sync without duplicating parameters/transitions.
/// </summary>
using PanicAtThePond.Managers;
using PanicAtThePond.Gameplay;
using PanicAtThePond.UI;
using PanicAtThePond.Shop;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.Controllers
{
public class FishermanChildAnimatorSync : MonoBehaviour
{
    [Header("References")]
    public Animator rootAnimator;

    [Header("Playback")]
    [SerializeField, Range(0.1f, 2f)] private float playbackSpeed = 1f;

    [Header("Oar Alignment")]
    [SerializeField] private Vector3 leftFacingOarHandOffset = new Vector3(-0.055f, 0.075f, 0f);
    [SerializeField] private Vector3 rightFacingOarHandOffset = new Vector3(0.055f, 0.075f, 0f);

    private readonly List<Animator> childAnimators = new List<Animator>();
    private readonly Dictionary<Animator, HashSet<int>> animatorParameterCache = new Dictionary<Animator, HashSet<int>>();
    private Transform oarTransform;
    private Vector3 oarBaseLocalPosition;

    // State names for oar hand alignment
    private static readonly string[] LeftFacingOarStateNames = new string[]
    {
        "Move Forward", "Move Backwards", "Oar To Left Pole", "Left Pole To Oar"
    };

    private static readonly string[] RightFacingOarStateNames = new string[]
    {
        "Move Reverse Forward", "Move Reverse Backwards", "Oar To Right Pole", "Right Pole To Oar"
    };

    // Pre-compute state hashes for fast comparison
    private static readonly int[] LeftFacingOarStateHashes;
    private static readonly int[] RightFacingOarStateHashes;

    static FishermanChildAnimatorSync()
    {
        LeftFacingOarStateHashes = new int[LeftFacingOarStateNames.Length];
        for (int i = 0; i < LeftFacingOarStateNames.Length; i++)
            LeftFacingOarStateHashes[i] = Animator.StringToHash(LeftFacingOarStateNames[i]);

        RightFacingOarStateHashes = new int[RightFacingOarStateNames.Length];
        for (int i = 0; i < RightFacingOarStateNames.Length; i++)
            RightFacingOarStateHashes[i] = Animator.StringToHash(RightFacingOarStateNames[i]);
    }

    private void Start()
    {
        // 1. Ensure root animator is assigned
        if (rootAnimator == null)
        {
            rootAnimator = GetComponent<Animator>();
        }

        if (rootAnimator == null)
        {
            Transform chestTransform = transform.Find("chest");
            if (chestTransform != null)
            {
                rootAnimator = chestTransform.GetComponent<Animator>();
            }
        }

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
            }
        }

        // 2. Sync with FishermanController
        var controllerScript = GetComponent<FishermanController>();
        if (controllerScript != null && controllerScript.animator == null)
        {
            controllerScript.animator = rootAnimator;
        }

        // 3. Configure child animators at runtime
        ConfigureChildControllersAtRuntime();

        // 4. Register child animators
        FindChildAnimators();
        CacheOarTransform();

        // 5. Sync initial state
        ForceInitialStateSync();

        ApplyPlaybackSpeed();

        // Log all child animator states for debugging
        Debug.Log($"[FishermanChildAnimatorSync] Initialized with {childAnimators.Count} child animators.");
        Debug.Log($"[FishermanChildAnimatorSync] Root animator: {(rootAnimator != null ? rootAnimator.gameObject.name : "NULL")}, controller: {(rootAnimator?.runtimeAnimatorController?.name ?? "NULL")}");

        // Log each child's controller info
        for (int i = 0; i < childAnimators.Count; i++)
        {
            var child = childAnimators[i];
            if (child != null)
            {
                Debug.Log($"[FishermanChildAnimatorSync] Child {i}: {child.gameObject.name}, controller: {(child.runtimeAnimatorController?.name ?? "NULL")}, state: {child.GetCurrentAnimatorStateInfo(0).shortNameHash}");
            }
        }
    }

    /// <summary>
    /// Forces all child animators to match the root's initial state.
    /// Uses name-based lookup to handle different state hashes across controllers.
    /// </summary>
    private void ForceInitialStateSync()
    {
        if (rootAnimator == null || childAnimators.Count == 0)
            return;

        rootAnimator.Update(0f);
        AnimatorStateInfo rootState = rootAnimator.GetCurrentAnimatorStateInfo(0);

        // Get root's current state name
        int rootStateHash = rootState.shortNameHash;
        if (rootStateHash == 0)
        {
            // Use default state - try to find "Idel Left"
            rootStateHash = Animator.StringToHash("Idel Left");
        }

        // Play on root first to ensure it's in a valid state
        if (rootState.shortNameHash == 0)
        {
            rootAnimator.Play(rootStateHash, 0, 0f);
            rootAnimator.Update(0f);
            rootState = rootAnimator.GetCurrentAnimatorStateInfo(0);
        }

        // Now sync all children using the SAME state name hash computation
        foreach (Animator child in childAnimators)
        {
            if (child == null || !child.isActiveAndEnabled)
                continue;

            child.Play(rootStateHash, 0, 0f);
            child.Update(0f);
        }

        Debug.Log($"[FishermanChildAnimatorSync] Initial sync to state hash: {rootStateHash}");
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
                }
            }
        }
    }

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
    }

    private void LateUpdate()
    {
        if (rootAnimator == null || childAnimators.Count == 0)
            return;

        ApplyPlaybackSpeed();

        // Sync child animators with root
        SyncChildStates();

        // Apply oar hand alignment
        AnimatorStateInfo rootState = rootAnimator.GetCurrentAnimatorStateInfo(0);
        ApplyOarHandAlignment(rootState.shortNameHash);
    }

    /// <summary>
    /// NOTE: Force state synchronization is DISABLED because different controllers
    /// generate DIFFERENT hash values for the same state name.
    ///
    /// Instead, all animation sync happens via the parameter system (ApplyBool/ApplyTrigger).
    /// When FishermanController sets a parameter, it sets it on ALL animators (root + children)
    /// so they all transition together based on their shared transition rules.
    /// </summary>
    private void SyncChildStates()
    {
        // DO NOT force sync states here - it doesn't work across different animator controllers
        // The ApplyBool/ApplyTrigger methods already sync all animators via shared parameters
    }

    private void ApplyPlaybackSpeed()
    {
        float speed = Mathf.Max(0.1f, playbackSpeed);

        if (rootAnimator != null)
            rootAnimator.speed = speed;

        for (int i = 0; i < childAnimators.Count; i++)
        {
            Animator child = childAnimators[i];
            if (child != null)
                child.speed = speed;
        }
    }

    /// <summary>
    /// Applies a boolean parameter to all child animators that have this parameter.
    /// Called by FishermanController when setting animation states.
    /// </summary>
    public void ApplyBool(string parameterName, bool value)
    {
        if (string.IsNullOrEmpty(parameterName))
            return;

        int paramHash = Animator.StringToHash(parameterName);

        // Apply to all child animators
        for (int i = 0; i < childAnimators.Count; i++)
        {
            Animator child = childAnimators[i];
            if (child == null || child == rootAnimator || !child.isActiveAndEnabled)
                continue;

            // Check if animator has this parameter
            if (HasParameter(child, paramHash, AnimatorControllerParameterType.Bool))
            {
                child.SetBool(parameterName, value);
            }
        }
    }

    /// <summary>
    /// Applies a trigger parameter to all child animators that have this parameter.
    /// Called by FishermanController when triggering animation transitions.
    /// </summary>
    public void ApplyTrigger(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return;

        int paramHash = Animator.StringToHash(parameterName);

        // Apply to all child animators
        for (int i = 0; i < childAnimators.Count; i++)
        {
            Animator child = childAnimators[i];
            if (child == null || child == rootAnimator || !child.isActiveAndEnabled)
                continue;

            // Check if animator has this parameter
            if (HasParameter(child, paramHash, AnimatorControllerParameterType.Trigger))
            {
                child.SetTrigger(parameterName);
            }
        }
    }

    private bool HasParameter(Animator animator, int paramHash, AnimatorControllerParameterType type)
    {
        if (animator == null)
            return false;

        if (!animatorParameterCache.TryGetValue(animator, out HashSet<int> hashes))
        {
            hashes = new HashSet<int>();
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter p = parameters[i];
                hashes.Add(Animator.StringToHash(p.name));
            }
            animatorParameterCache[animator] = hashes;
        }

        // Check by name hash (we need to find param by name, not by hash)
        foreach (var p in animator.parameters)
        {
            if (p.type == type && Animator.StringToHash(p.name) == paramHash)
                return true;
        }
        return false;
    }

    private void CacheOarTransform()
    {
        oarTransform = transform.Find("oar");
        if (oarTransform == null)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (string.Equals(child.name, "oar", System.StringComparison.OrdinalIgnoreCase))
                {
                    oarTransform = child;
                    break;
                }
            }
        }

        if (oarTransform != null)
            oarBaseLocalPosition = oarTransform.localPosition;
    }

    private void ApplyOarHandAlignment(int rootShortNameHash)
    {
        if (oarTransform == null)
            return;

        if (ContainsState(LeftFacingOarStateHashes, rootShortNameHash))
        {
            oarTransform.localPosition = oarBaseLocalPosition + leftFacingOarHandOffset;
        }
        else if (ContainsState(RightFacingOarStateHashes, rootShortNameHash))
        {
            oarTransform.localPosition = oarBaseLocalPosition + rightFacingOarHandOffset;
        }
        else
        {
            oarTransform.localPosition = oarBaseLocalPosition;
        }
    }

    private static bool ContainsState(int[] stateHashes, int stateHash)
    {
        for (int i = 0; i < stateHashes.Length; i++)
        {
            if (stateHashes[i] == stateHash)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Debug method to log current animation states of all animators.
    /// </summary>
    public void DebugLogStates()
    {
        if (rootAnimator != null)
        {
            AnimatorStateInfo rootState = rootAnimator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[Sync Debug] Root state: {rootState.shortNameHash}, layer: {rootAnimator.GetLayerName(0)}");
        }

        for (int i = 0; i < childAnimators.Count; i++)
        {
            Animator child = childAnimators[i];
            if (child != null)
            {
                AnimatorStateInfo childState = child.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"[Sync Debug] Child {i} state: {childState.shortNameHash}, layer: {child.GetLayerName(0)}");
            }
        }
    }
}
}