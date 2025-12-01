using Steamworks;
using UnityEngine;

public class ResetStatsAchievements : MonoBehaviour
{
    [SerializeField]
    private bool ResetStatsOnGameStart = false;

    [SerializeField]
    private bool AlsoResetAchievements = false;

    private void Start()
    {
        if (SteamManager.Initialized)
        {
            if (ResetStatsOnGameStart)
            {
                SteamUserStats.ResetAllStats(AlsoResetAchievements);
            }
        }
    }
}
