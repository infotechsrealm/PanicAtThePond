using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System;

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
        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Successful PlayFab Login!");
        IsLoggedIn = true;
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
}
