using UnityEngine;
using UnityEditor;
using System.IO;

public class ExportAseprite
{
    [MenuItem("Tools/Export Black Hair Aseprite")]
    public static void Export()
    {
        string assetPath = "Assets/UI/Game UI/Fisherman/FishermansAnimations-Head-BlackHair-Sheet.aseprite";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex != null)
        {
            // Create a temporary RenderTexture to read pixels from the non-readable texture
            RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            Texture2D readableTex = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, false);
            readableTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            readableTex.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            byte[] bytes = readableTex.EncodeToPNG();
            string outputPath = "Assets/UI/Game UI/Fisherman/FishermansAnimations-Head-BlackHair-Sheet.png";
            File.WriteAllBytes(outputPath, bytes);
            Debug.Log("Successfully exported Aseprite texture to PNG at: " + outputPath);
        }
        else
        {
            Debug.LogError("Failed to load Aseprite texture at: " + assetPath);
        }
    }
}
