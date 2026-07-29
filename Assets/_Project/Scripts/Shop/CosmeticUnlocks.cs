using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks which shop hats the local player owns. Unlocks are mirrored to PlayFab user data
/// (key "Cosmetic_&lt;id&gt;" = "Unlocked", same scheme PlayFabManager.UpdateCosmeticData already
/// uses) and cached in PlayerPrefs so the shop still knows ownership while offline.
/// </summary>
using PanicAtThePond.Managers;
using PanicAtThePond.Controllers;
using PanicAtThePond.Gameplay;
using PanicAtThePond.UI;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.Shop
{
public static class CosmeticUnlocks
{
    private const string LocalPrefPrefix = "HatUnlocked_";
    private const string PlayFabKeyPrefix = "Cosmetic_";

    private static bool playFabSynced;

    /// <summary>Raised after a hat is unlocked so open UI (shop cells, lock overlays) can refresh.</summary>
    public static event Action OnUnlocksChanged;

    public static bool IsUnlocked(string hatId)
    {
        if (string.IsNullOrEmpty(hatId))
        {
            return false;
        }

        ShopConfig config = ShopConfig.Load();
        ShopConfig.HatEntry entry = config != null ? config.FindHat(hatId) : null;
        if (entry != null && entry.unlockedByDefault)
        {
            return true;
        }

        return PlayerPrefs.GetInt(LocalPrefPrefix + hatId, 0) == 1;
    }

    /// <summary>Marks a hat owned locally and pushes the unlock to PlayFab.</summary>
    public static void Unlock(string hatId)
    {
        if (string.IsNullOrEmpty(hatId))
        {
            return;
        }

        PlayerPrefs.SetInt(LocalPrefPrefix + hatId, 1);
        PlayerPrefs.Save();

        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.UpdateCosmeticData(hatId);
        }

        OnUnlocksChanged?.Invoke();
    }

    /// <summary>
    /// Pulls unlocks stored on PlayFab into the local cache (once per session) so purchases made
    /// on another install of the same account are honoured. Safe to call repeatedly.
    /// </summary>
    public static void SyncFromPlayFab(Action onDone = null)
    {
        if (playFabSynced || PlayFabManager.Instance == null || !PlayFabManager.Instance.IsLoggedIn)
        {
            onDone?.Invoke();
            return;
        }

        PlayFabManager.Instance.GetUserData(data =>
        {
            if (data != null)
            {
                foreach (KeyValuePair<string, string> pair in data)
                {
                    if (pair.Key.StartsWith(PlayFabKeyPrefix, StringComparison.Ordinal) &&
                        pair.Value == "Unlocked")
                    {
                        string hatId = pair.Key.Substring(PlayFabKeyPrefix.Length);
                        PlayerPrefs.SetInt(LocalPrefPrefix + hatId, 1);
                    }
                }
                PlayerPrefs.Save();
                playFabSynced = true;
            }
            onDone?.Invoke();
        });
    }
}

}