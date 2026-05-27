using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class ExportAsepriteToPNG
{
    static ExportAsepriteToPNG()
    {
        EditorApplication.delayCall += Execute;
    }

    static void Execute()
    {
        string asepritePath = "Assets/UI/Game UI/Fisherman/FishermansAnimations-Head-BlackHair-Sheet.aseprite";
        string pngPath = "Assets/UI/Game UI/Fisherman/FishermansAnimations-Head-BlackHair-Sheet.png";
        
        if (File.Exists(pngPath)) 
        {
            // Already exported
            return;
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(asepritePath);
        if (tex != null)
        {
            // We need to read pixels, but the texture might not be marked as readable.
            // So we blit it to a RenderTexture.
            RenderTexture tmp = RenderTexture.GetTemporary(
                tex.width, 
                tex.height, 
                0, 
                RenderTextureFormat.Default, 
                RenderTextureReadWrite.Linear);
            
            Graphics.Blit(tex, tmp);
            
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;
            
            Texture2D readableText = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            readableText.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            readableText.Apply();
            
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            byte[] bytes = readableText.EncodeToPNG();
            File.WriteAllBytes(pngPath, bytes);
            
            Debug.Log("Successfully exported Aseprite to PNG!");
            
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogError("Could not find Aseprite file at " + asepritePath);
        }
    }
}
