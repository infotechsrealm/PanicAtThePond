using Steamworks;
using UnityEngine;

using PanicAtThePond.Managers;
using PanicAtThePond.Controllers;
using PanicAtThePond.Gameplay;
using PanicAtThePond.UI;
using PanicAtThePond.Shop;
using PanicAtThePond.Data;

namespace PanicAtThePond.Utilities
{
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

}