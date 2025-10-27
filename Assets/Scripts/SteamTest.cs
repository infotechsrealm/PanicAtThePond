using Steamworks;
using UnityEngine;

public class SteamTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        if (SteamAPI.Init())
        {
            Debug.Log("✅ Steam initialized successfully!");
            Debug.Log("🎮 Steam Name: " + SteamFriends.GetPersonaName());
            int avatarId = SteamFriends.GetSmallFriendAvatar(SteamUser.GetSteamID());
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
}
