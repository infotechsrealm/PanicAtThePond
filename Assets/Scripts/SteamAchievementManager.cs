using UnityEngine;
using Steamworks;
public class SteamAchievementManager : MonoBehaviour
{

    public SteamAchievementManager achievementManager;
    public void UnlockAchievement(string achievementApiName)
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("Steam Manager is not initialized. Cannot unlock achieve");
            return;
        }
        // Set the achievement
        SteamUserStats.SetAchievement(achievementApiName);
        // Store the change on Steam's servers
        SteamUserStats.StoreStats();
        Debug.Log("Unlocked achievement: " + achievementApiName);
    }


    //Fisherman Achievements
    void CheckForFishSlayerAchievement(int fishCaughtThisRound)
    {
        achievementManager.UnlockAchievement("ACH_FISH_SLAYER");
    }


    //Fish Achievements 
    void CheckForWhatASnackAchievement()
    {
        achievementManager.UnlockAchievement("ACH_WHAT_A_SNACK");
    }
    void CheckForSurvivorAchievement()
    {
        achievementManager.UnlockAchievement("ACH_SURVIVOR");
    }

    void CheckForWeComeInSwarmsAchievement(int totalPlayers)
    {
        if (totalPlayers == 7)
        {
            achievementManager.UnlockAchievement("ACH_WE_COME_IN_SWARMS");
        }
    }

    void CheckForSoloArtistAchievement()
    {
        achievementManager.UnlockAchievement("ACH_SOLO_ARTIST");
    }


    void CheckForGulperAchievement(int wormsEatenThisRound)
    {
        achievementManager.UnlockAchievement("ACH_GULPER");
    }

}