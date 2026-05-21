using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FishermanSetupTool : EditorWindow
{
    [MenuItem("Tools/Setup New Fisherman")]
    public static void ShowWindow()
    {
        GetWindow<FishermanSetupTool>("New Fisherman Setup");
    }

    private GameObject basePrefab;
    private DefaultAsset animationsFolder;
    private Sprite redHairSprite;

    private void OnGUI()
    {
        GUILayout.Label("New Fisherman Setup Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        basePrefab = (GameObject)EditorGUILayout.ObjectField("Base Fisherman Prefab", basePrefab, typeof(GameObject), false);
        animationsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Used Animations Folder", animationsFolder, typeof(DefaultAsset), false);
        redHairSprite = (Sprite)EditorGUILayout.ObjectField("Red Hair Sprite (Optional)", redHairSprite, typeof(Sprite), false);

        GUILayout.Space(20);
        if (GUILayout.Button("Create New Fisherman from Scratch", GUILayout.Height(40)))
        {
            CreateNewFisherman();
        }
    }

    private void CreateNewFisherman()
    {
        if (basePrefab == null || animationsFolder == null)
        {
            Debug.LogError("Please assign the base prefab and the UsedAnimations folder.");
            return;
        }

        string newPrefabPath = "Assets/Resources/NewFisherMan.prefab";

        // 1. Duplicate Base Prefab
        bool success;
        GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(basePrefab, newPrefabPath, out success);
        if (!success)
        {
            Debug.LogError("Failed to create the new prefab. Does one already exist at " + newPrefabPath + "?");
            return;
        }

        // 2. Load New Animations
        string folderPath = AssetDatabase.GetAssetPath(animationsFolder);
        string[] animGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });
        
        List<AnimationClip> newClips = new List<AnimationClip>();
        foreach (string guid in animGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            newClips.Add(AssetDatabase.LoadAssetAtPath<AnimationClip>(path));
        }

        // 3. Create Animator Override Controller
        Animator animator = newPrefab.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            RuntimeAnimatorController baseController = animator.runtimeAnimatorController;
            
            // If the base is already an override, get its base
            if (baseController is AnimatorOverrideController)
            {
                baseController = ((AnimatorOverrideController)baseController).runtimeAnimatorController;
            }

            AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);
            string overridePath = "Assets/Animations/Fisher Man Animations/NewFishermanOverride.overrideController";
            
            // Automatically map old clips to the new ones based on naming
            var clipOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            foreach (AnimationClip oldClip in baseController.animationClips)
            {
                // Normalize names for comparison (remove spaces, lowercase)
                string oldName = oldClip.name.ToLower().Replace(" ", "").Replace("_", "");
                AnimationClip replacement = null;
                
                foreach (AnimationClip newClip in newClips)
                {
                    string newName = newClip.name.ToLower().Replace(" ", "").Replace("_", "");
                    
                    // Match logic: e.g. "Idel_Left" matches "Idel Left.anim"
                    if (newName == oldName || newName.Contains(oldName) || oldName.Contains(newName))
                    {
                        replacement = newClip;
                        break;
                    }
                }
                
                if (replacement != null)
                {
                    clipOverrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(oldClip, replacement));
                }
            }
            
            overrideController.ApplyOverrides(clipOverrides);
            AssetDatabase.CreateAsset(overrideController, overridePath);
            animator.runtimeAnimatorController = overrideController;
        }

        // 4. Try to replace hat with red hair directly on SpriteRenderers (if they are structured as children)
        if (redHairSprite != null)
        {
            SpriteRenderer[] renderers = newPrefab.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in renderers)
            {
                string objName = sr.gameObject.name.ToLower();
                if (objName.Contains("hat") || objName.Contains("hair"))
                {
                    sr.sprite = redHairSprite;
                }
            }
        }

        PrefabUtility.SavePrefabAsset(newPrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Successfully created New Fisherman prefab at: " + newPrefabPath);
        Selection.activeObject = newPrefab;
    }
}
