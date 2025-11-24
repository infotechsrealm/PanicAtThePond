using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class SteamIntegration : MonoBehaviour
{
    public RawImage avatarImage;
    public Text nameText;

    private void Start()
    {
        try
        {
            SteamClient.Init(480);
            nameText.text = SteamClient.Name;
            LoadAvatar();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    private void Update()
    {
        SteamClient.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        SteamClient.Shutdown();
    }

    async void LoadAvatar()
    {
        var avatar = await SteamFriends.GetLargeAvatarAsync(SteamClient.SteamId);

        if (avatar == null)
        {
            Debug.LogWarning("⚠ No avatar found.");
            return;
        }

        int width = (int)avatar.Value.Width;
        int height = (int)avatar.Value.Height;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.LoadRawTextureData(avatar.Value.Data);
        tex.Apply();

        // 🔥 FIX: Flip vertically
        tex = FlipTextureVertically(tex);

        avatarImage.texture = tex;
    }

    Texture2D FlipTextureVertically(Texture2D original)
    {
        Texture2D flipped = new Texture2D(original.width, original.height, original.format, false);

        for (int y = 0; y < original.height; y++)
        {
            Color[] row = original.GetPixels(0, y, original.width, 1);
            flipped.SetPixels(0, original.height - 1 - y, original.width, 1, row);
        }

        flipped.Apply();
        return flipped;
    }
}