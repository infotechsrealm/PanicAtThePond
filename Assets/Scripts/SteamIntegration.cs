using UnityEngine;
using Steamworks;
using UnityEngine.UI;

public class SteamIntegration : MonoBehaviour
{
    public RawImage avatarImage;
    public Text nameText;

    private void Start()
    {

        // DO NOT initialize Steam here!
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam is not initialized!");
            return;
        }

        Debug.Log("Steam Init = " + SteamManager.Initialized);
        nameText.text = SteamFriends.GetPersonaName();
        LoadAvatar();
    }

    private void Update()
    {
        if (SteamManager.Initialized)
            SteamAPI.RunCallbacks();
    }

    

    void LoadAvatar()
    {
        CSteamID userID = SteamUser.GetSteamID();
        int avatarInt = SteamFriends.GetLargeFriendAvatar(userID);

        if (avatarInt == -1)
        {
            Debug.Log("Avatar not ready yet");
            return;
        }

        uint width, height;
        if (!SteamUtils.GetImageSize(avatarInt, out width, out height))
        {
            Debug.LogError("Failed to get avatar size");
            return;
        }

        byte[] image = new byte[width * height * 4];
        if (!SteamUtils.GetImageRGBA(avatarInt, image, (int)(width * height * 4)))
        {
            Debug.LogError("Failed to get avatar RGBA");
            return;
        }

        Texture2D tex = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
        tex.LoadRawTextureData(image);
        tex.Apply();

        // Flip fix here
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