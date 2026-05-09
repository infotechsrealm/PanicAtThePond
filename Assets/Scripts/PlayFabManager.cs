using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;

public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance;
    
    public bool IsLoggedIn = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Login()
    {
        string customId = SystemInfo.deviceUniqueIdentifier;
        
        // If testing in the Unity Editor at the same time as a standalone build on the same PC, 
        // make the Editor act as a different player to prevent PlayFab account collisions.
#if UNITY_EDITOR
        customId += "_EDITOR";
#endif

        var request = new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Successful PlayFab Login!");
        IsLoggedIn = true;
        SyncLocalAchievements();
    }

    private void SyncLocalAchievements()
    {
        string[] allAchievements = { "SOLO_ARTIST", "SURVIVOR", "EARTH_PRAISER", "WHAT_A_SNACK", "FISH_SLAYER", "WE_COME_IN_SWARMS", "GULPER" };
        var dataToSync = new Dictionary<string, string>();

        foreach (var ach in allAchievements)
        {
            if (PlayerPrefs.GetInt("Achievement_" + ach, 0) == 1)
            {
                dataToSync["Achievement_" + ach] = "1";
            }
        }

        if (dataToSync.Count > 0)
        {
            var request = new UpdateUserDataRequest
            {
                Data = dataToSync
            };

            PlayFabClientAPI.UpdateUserData(request, res =>
            {
                Debug.Log($"Successfully synced {dataToSync.Count} local achievements to PlayFab on login.");
            }, err =>
            {
                Debug.LogError("Error syncing local achievements on login: " + err.GenerateErrorReport());
            });
        }
    }

    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogWarning("Something went wrong with your PlayFab login API call.");
        Debug.LogError(error.GenerateErrorReport());
    }

    public void AddCurrency(int amount, Action<int> onSuccess = null)
    {
        var request = new AddUserVirtualCurrencyRequest
        {
            VirtualCurrency = "WC",
            Amount = amount
        };

        PlayFabClientAPI.AddUserVirtualCurrency(request, result =>
        {
            Debug.Log($"Successfully awarded {amount} Worm Coins! New balance: {result.Balance}");
            onSuccess?.Invoke(result.Balance);
        }, error =>
        {
            Debug.LogError("Error awarding Worm Coins: " + error.GenerateErrorReport());
        });
    }

    public void GetCurrency(Action<int> onSuccess)
    {
        if (!IsLoggedIn)
        {
            Debug.LogWarning("GetCurrency called before login finished. Skipping or handle retry.");
            return;
        }

        var request = new GetUserInventoryRequest();

        PlayFabClientAPI.GetUserInventory(request, result =>
        {
            Debug.Log("Fetched PlayFab Inventory!");
            if (result.VirtualCurrency.TryGetValue("WC", out int coinsAmount))
            {
                onSuccess?.Invoke(coinsAmount);
            }
            else
            {
                onSuccess?.Invoke(0); // If WC currency not found, return 0
            }
        }, error =>
        {
            Debug.LogError("Error fetching PlayFab inventory: " + error.GenerateErrorReport());
        });
    }

    public void UpdateAchievementData(string achievementId, string value, Action<bool> onComplete = null)
    {
        if (!IsLoggedIn)
        {
            Debug.LogWarning("UpdateAchievementData called before login finished.");
            onComplete?.Invoke(false);
            return;
        }

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "Achievement_" + achievementId, value }
            }
        };

        PlayFabClientAPI.UpdateUserData(request, result =>
        {
            Debug.Log($"Successfully synced achievement {achievementId} to PlayFab");
            onComplete?.Invoke(true);
        }, error =>
        {
            Debug.LogError("Error syncing achievement to PlayFab: " + error.GenerateErrorReport());
            onComplete?.Invoke(false);
        });
    }

    // ---- COSMETICS & INVENTORY ----

    public void GetInventory(Action<List<ItemInstance>> onSuccess)
    {
        if (!IsLoggedIn)
        {
            Debug.LogWarning("GetInventory called before login finished.");
            return;
        }

        var request = new GetUserInventoryRequest();

        PlayFabClientAPI.GetUserInventory(request, result =>
        {
            Debug.Log("Fetched PlayFab Inventory items!");
            onSuccess?.Invoke(result.Inventory);
        }, error =>
        {
            Debug.LogError("Error fetching PlayFab inventory: " + error.GenerateErrorReport());
        });
    }

    // Option 1: Using PlayFab Economy (Catalog & Inventory)
    public void PurchaseCosmetic(string itemId, int price, Action onPurchaseSuccess, Action<string> onPurchaseFailure)
    {
        if (!IsLoggedIn)
        {
            Debug.LogWarning("PurchaseCosmetic called before login finished.");
            onPurchaseFailure?.Invoke("Not logged in");
            return;
        }

        var request = new PurchaseItemRequest
        {
            ItemId = itemId,
            VirtualCurrency = "WC",
            Price = price
        };

        PlayFabClientAPI.PurchaseItem(request, result =>
        {
            Debug.Log($"Successfully purchased {itemId} from PlayFab Economy!");
            onPurchaseSuccess?.Invoke();
        }, error =>
        {
            Debug.LogError("Error purchasing item: " + error.GenerateErrorReport());
            onPurchaseFailure?.Invoke(error.ErrorMessage);
        });
    }

    // Option 2: Using PlayFab Player Data (Title) to store cosmetics
    public void UpdateCosmeticData(string cosmeticId, Action<bool> onComplete = null)
    {
        if (!IsLoggedIn)
        {
            Debug.LogWarning("UpdateCosmeticData called before login finished.");
            onComplete?.Invoke(false);
            return;
        }

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "Cosmetic_" + cosmeticId, "Unlocked" }
            }
        };

        PlayFabClientAPI.UpdateUserData(request, result =>
        {
            Debug.Log($"Successfully synced cosmetic {cosmeticId} to PlayFab User Data");
            onComplete?.Invoke(true);
        }, error =>
        {
            Debug.LogError("Error syncing cosmetic to PlayFab: " + error.GenerateErrorReport());
            onComplete?.Invoke(false);
        });
    }
}
