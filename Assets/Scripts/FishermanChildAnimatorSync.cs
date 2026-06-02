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

    [Header("Playback")]
    [SerializeField, Range(0.1f, 2f)] private float playbackSpeed = 1f;

    [Header("Oar Alignment")]
    [SerializeField] private Vector3 leftFacingOarHandOffset = new Vector3(-0.055f, 0.075f, 0f);
    [SerializeField] private Vector3 rightFacingOarHandOffset = new Vector3(0.055f, 0.075f, 0f);

    private readonly List<Animator> childAnimators = new List<Animator>();
    private readonly Dictionary<Animator, HashSet<int>> animatorParameterCache = new Dictionary<Animator, HashSet<int>>();
    private Transform oarTransform;
    private Vector3 oarBaseLocalPosition;

    private static readonly int[] LeftFacingOarStates =
    {
        Animator.StringToHash("Move Forward"),
        Animator.StringToHash("Move Backwards"),
        Animator.StringToHash("Oar To Left Pole"),
        Animator.StringToHash("Left Pole To Oar")
    };

    private static readonly int[] RightFacingOarStates =
    {
        Animator.StringToHash("Move Reverse Forward"),
        Animator.StringToHash("Move Reverse Backwards"),
        Animator.StringToHash("Oar To Right Pole"),
        Animator.StringToHash("Right Pole To Oar")
    };

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
        CacheOarTransform();
        ApplyPlaybackSpeed();
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
        {
            return;
        }

        ApplyPlaybackSpeed();
        AnimatorStateInfo rootState = rootAnimator.GetCurrentAnimatorStateInfo(0);
        ApplyOarHandAlignment(rootState.shortNameHash);
    }

    private void ApplyPlaybackSpeed()
    {
        float speed = Mathf.Max(0.1f, playbackSpeed);

        if (rootAnimator != null)
        {
            rootAnimator.speed = speed;
        }

        for (int i = 0; i < childAnimators.Count; i++)
        {
            Animator child = childAnimators[i];
            if (child != null)
            {
                child.speed = speed;
            }
        }
    }

    public void ApplyBool(string parameterName, bool value)
    {
        for (int i = 0; i < childAnimators.Count; i++)
        {
            Animator child = childAnimators[i];
            if (child == null || child == rootAnimator || !child.isActiveAndEnabled)
            {
                continue;
            }

            if (HasParameter(child, parameterName, AnimatorControllerParameterType.Bool))
            {
                child.SetBool(parameterName, value);
            }
        }
    }

    public void ApplyTrigger(string parameterName)
    {
        for (int i = 0; i < childAnimators.Count; i++)
        {
            Animator child = childAnimators[i];
            if (child == null || child == rootAnimator || !child.isActiveAndEnabled)
            {
                continue;
            }

            if (HasParameter(child, parameterName, AnimatorControllerParameterType.Trigger))
            {
                child.SetTrigger(parameterName);
            }
        }
    }

    private bool HasParameter(Animator animator, string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
        {
            return false;
        }

        if (!animatorParameterCache.TryGetValue(animator, out HashSet<int> hashes))
        {
            hashes = new HashSet<int>();
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter p = parameters[i];
                hashes.Add(ComposeParameterKey(p.nameHash, p.type));
            }

            animatorParameterCache[animator] = hashes;
        }

        return hashes.Contains(ComposeParameterKey(Animator.StringToHash(parameterName), type));
    }

    private static int ComposeParameterKey(int nameHash, AnimatorControllerParameterType type)
    {
        return (nameHash * 31) ^ (int)type;
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
        {
            oarBaseLocalPosition = oarTransform.localPosition;
        }
    }

    private void ApplyOarHandAlignment(int rootShortNameHash)
    {
        if (oarTransform == null)
        {
            return;
        }

        if (ContainsState(LeftFacingOarStates, rootShortNameHash))
        {
            oarTransform.localPosition = oarBaseLocalPosition + leftFacingOarHandOffset;
            return;
        }

        if (ContainsState(RightFacingOarStates, rootShortNameHash))
        {
            oarTransform.localPosition = oarBaseLocalPosition + rightFacingOarHandOffset;
            return;
        }

        oarTransform.localPosition = oarBaseLocalPosition;
    }

    private static bool ContainsState(int[] stateHashes, int stateHash)
    {
        for (int i = 0; i < stateHashes.Length; i++)
        {
            if (stateHashes[i] == stateHash)
            {
                return true;
            }
        }

        return false;
    }
}
