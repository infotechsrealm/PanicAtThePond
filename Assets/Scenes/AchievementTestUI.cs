using UnityEngine;
using Steamworks;

public class AchievementTestUI : MonoBehaviour
{
    private void Start()
    {
        if (SteamManager.Initialized)
            Debug.Log("✔ Steam Initialized - Ready for Achievements");
        else
            Debug.LogError("❌ Steam not initialized!");

        SteamUserStats.ResetAllStats(true);
        SteamUserStats.StoreStats();
    }

    public void Unlock_AchWinOne() => Unlock("ACH_WIN_ONE_GAME");
    public void Unlock_AchWin100() => Unlock("ACH_WIN_100_GAMES");
    public void Unlock_AchTravelSingle() => Unlock("ACH_TRAVEL_FAR_SINGLE");
    public void Unlock_AchTravelAccum() => Unlock("ACH_TRAVEL_FAR_ACCUM");

    void Unlock(string id)
    {
        if (!SteamManager.Initialized) return;
        SteamUserStats.SetAchievement(id);
        SteamUserStats.StoreStats();
        Debug.Log("✔ ACHIEVEMENT UNLOCKED: " + id);
    }

    public void ResetAll()
    {
        if (!SteamManager.Initialized) return;
        SteamUserStats.ResetAllStats(true);
        SteamUserStats.StoreStats();
        Debug.Log("🔄 All Achievements + Stats Reset.");
    }


    public void UnlockMyAchievement()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam not initialized!");
            return;
        }

        SteamUserStats.SetAchievement("LEVEL_01_ALL_COINS");
        SteamUserStats.StoreStats();

        Debug.Log("Achievement unlocked!");
    }

}
