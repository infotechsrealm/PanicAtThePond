using Steamworks;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    private int CoinCount = 0;
    private int MaxCoins = 2;

    public void CollectCoin()
    {
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.AddCurrency(1);
        }

        if (SteamManager.Initialized)
        {
            // Update coin stats
            SteamUserStats.GetStat("LEVEL_01_COIN_COUNT", out CoinCount);
            CoinCount++;
            SteamUserStats.SetStat("LEVEL_01_COIN_COUNT", CoinCount);
            SteamUserStats.StoreStats();

            // Check achievement condition
            if (CoinCount >= MaxCoins)
            {
                SteamUserStats.GetAchievement("LEVEL_01_ALL_COINS", out bool achievementCompleted);

                if (!achievementCompleted)
                {
                    SteamUserStats.SetAchievement("LEVEL_01_ALL_COINS");
                    SteamUserStats.StoreStats();
                }
            }
        }
    }
}
