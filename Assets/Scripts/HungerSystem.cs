using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class HungerSystem : MonoBehaviourPun  
{
    public static HungerSystem Instance;

    public Slider hungerBar;
    public float maxHunger = 100f;
    public float hungerDecreaseSpeed = 5f;
    public ScoreManager ScoreManager;
    public GameObject WinnerScreen,WinnerScreenn, ScoreScreen;

    private float currentHunger;
    public bool canDecrease;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        canDecrease = true;
        currentHunger = maxHunger;
        hungerBar.maxValue = maxHunger;
        hungerBar.value = currentHunger;
    }

    void Update()
    {
        if (!canDecrease) return;

        if (GameManager.Instance != null)
        {
            currentHunger -= hungerDecreaseSpeed * Time.deltaTime;
            currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
            hungerBar.value = currentHunger;

            if (currentHunger <= 0)
            {
                currentHunger = 0;
                canDecrease = false;

                // Stop spawners
                if (WormSpawner.Instance != null) WormSpawner.Instance.StopSpawning();
                if (JunkSpawner.Instance != null) JunkSpawner.Instance.StopSpawning();

                // Determine if we are the authority (Master Client or Server)
                bool isAuthority = false;
                if (GS.Instance != null && GS.Instance.isLan)
                {
                    isAuthority = GS.Instance.IsMirrorMasterClient;
                }
                else
                {
                    isAuthority = PhotonNetwork.IsMasterClient;
                }

                // Authoritative trigger for end of round
                if (isAuthority)
                {
                    GameManager.Instance.ShowGameOver("Fisherman Win! (Fish Starved)");
                    GameManager.Instance.TriggerRoundEnd("Fisherman Win! (Fish Starved)");
                }
                else if (GS.Instance != null && !GS.Instance.isLan && !PhotonNetwork.IsConnected)
                {
                    // Fallback for offline/singleplayer
                    GameManager.Instance.ShowGameOver("Fisherman Win! (Fish Starved)");
                }
            }
        }
    }

    public void AddHunger(float amount)
    {
        currentHunger += (hungerBar.value * amount) / 100;
        currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
        hungerBar.value = currentHunger;
    }
}