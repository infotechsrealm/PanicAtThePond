using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class SteamTest : MonoBehaviour
{
    public RawImage avatarImage;
    public Text name;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        if (SteamAPI.Init())
        {
            Debug.Log("✅ Steam initialized successfully!");
            Debug.Log("🎮 Steam Name: " + SteamFriends.GetPersonaName());
            name.text = SteamFriends.GetPersonaName();
            SetSteamAvatar();
        }
        else
        {
            Debug.LogError("❌ Steam initialization failed!");
        }

    }
    // Update is called once per frame
    void Update()
    {
        
    }

    void SetSteamAvatar()
    {
        CSteamID steamID = SteamUser.GetSteamID();
        int avatarInt = SteamFriends.GetLargeFriendAvatar(steamID); // Medium = 64x64

        if (avatarInt == 0)
        {
            Debug.LogWarning("⚠️ Avatar not found yet!");
            return;
        }

        uint width, height;
        bool gotSize = SteamUtils.GetImageSize(avatarInt, out width, out height);
        if (!gotSize || width == 0 || height == 0)
        {
            Debug.LogWarning("⚠️ Failed to get avatar size!");
            return;
        }

        byte[] imageBuffer = new byte[width * height * 4];
        bool gotImage = SteamUtils.GetImageRGBA(avatarInt, imageBuffer, imageBuffer.Length);
        if (!gotImage)
        {
            Debug.LogWarning("⚠️ Failed to get avatar image!");
            return;
        }

        // Create texture and load data
        Texture2D tex = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
        tex.LoadRawTextureData(imageBuffer);
        tex.Apply();

        // 🔁 Flip the image vertically (Steam returns it upside down)
        tex = FlipTextureVertically(tex);

        // ✅ Fix pixel cracks (disable linear filtering)
        tex.filterMode = FilterMode.Point;  // crisp pixels
        tex.wrapMode = TextureWrapMode.Clamp;

        avatarImage.texture = tex;
    }

    Texture2D FlipTextureVertically(Texture2D original)
    {
        Texture2D flipped = new Texture2D(original.width, original.height, original.format, false);

        for (int y = 0; y < original.height; y++)
        {
            flipped.SetPixels(0, y, original.width, 1, original.GetPixels(0, original.height - 1 - y, original.width, 1));
        }

        flipped.Apply();
        return flipped;
    }
}
