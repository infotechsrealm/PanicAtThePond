using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class GenerateFishAnimations
{
    [MenuItem("Tools/Generate Fish Animations")]
    public static void Execute()
    {
        string spritesPath = "Assets/UI/ShopUI/ExtractedPDFSprites";
        string outputControllerPath = "Assets/Resources/FishControllers";
        string outputClipPath = "Assets/Resources/FishControllers/Clips";
        
        string baseBassControllerPath = "Assets/Animations/Fish Animations/Fish 1/Fish 1.controller";
        string baseTroutControllerPath = "Assets/Animations/Fish Animations/Fish 2/Fish 2.controller";

        if (!AssetDatabase.IsValidFolder(outputControllerPath))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "FishControllers");
        }
        if (!AssetDatabase.IsValidFolder(outputClipPath))
        {
            AssetDatabase.CreateFolder("Assets/Resources/FishControllers", "Clips");
        }

        AssetDatabase.CopyAsset(baseBassControllerPath, $"{outputControllerPath}/Fish 1 Default.controller");
        AssetDatabase.CopyAsset(baseTroutControllerPath, $"{outputControllerPath}/Fish 2 Default.controller");

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { spritesPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) continue;

            string spriteName = sprite.name.ToLowerInvariant();
            if (spriteName.StartsWith("fisherman")) continue; // Ignore fisherman sprites

            bool isBass = spriteName.StartsWith("bass");
            bool isTrout = spriteName.StartsWith("trout");
            
            if (!isBass && !isTrout) continue;

            string baseController = isBass ? baseBassControllerPath : baseTroutControllerPath;

            // Create static clip
            AnimationClip clip = new AnimationClip();
            clip.frameRate = 60;
            AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

            EditorCurveBinding spriteBinding = new EditorCurveBinding();
            spriteBinding.type = typeof(SpriteRenderer);
            spriteBinding.path = "";
            spriteBinding.propertyName = "m_Sprite";

            ObjectReferenceKeyframe[] spriteKeyFrames = new ObjectReferenceKeyframe[2];
            spriteKeyFrames[0] = new ObjectReferenceKeyframe { time = 0, value = sprite };
            spriteKeyFrames[1] = new ObjectReferenceKeyframe { time = 1f, value = sprite };

            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeyFrames);

            string clipSavePath = $"{outputClipPath}/{sprite.name}_Static.anim";
            AssetDatabase.CreateAsset(clip, clipSavePath);
            AnimationClip savedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipSavePath);

            // Duplicate controller
            string newControllerPath = $"{outputControllerPath}/{sprite.name}.controller";
            AssetDatabase.CopyAsset(baseController, newControllerPath);

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(newControllerPath);

            // Replace all motions in all states
            foreach (var layer in controller.layers)
            {
                ReplaceMotionsInStateMachine(layer.stateMachine, savedClip);
            }

            EditorUtility.SetDirty(controller);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Fish animations generated successfully.");
    }

    private static void ReplaceMotionsInStateMachine(AnimatorStateMachine sm, AnimationClip newClip)
    {
        foreach (var state in sm.states)
        {
            if (state.state.motion != null && state.state.motion is AnimationClip)
            {
                state.state.motion = newClip;
            }
        }

        foreach (var subSm in sm.stateMachines)
        {
            ReplaceMotionsInStateMachine(subSm.stateMachine, newClip);
        }
    }
}
