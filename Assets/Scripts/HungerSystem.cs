using UnityEngine;
using UnityEngine.UI;

public class HungerSystem : MonoBehaviour
{
    public static HungerSystem Instance;

    public Slider hungerBar;
    public float maxHunger = 100f;
    public float hungerDecreaseSpeed = 5f;

    private float currentHunger;

    public bool canDecrease;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ApplyConfiguredDepletionRate();
        canDecrease = true;
        currentHunger = maxHunger;
        hungerBar.maxValue = maxHunger;
        hungerBar.value = currentHunger;
    }

    private void ApplyConfiguredDepletionRate()
    {
        if (GS.Instance != null && GS.Instance.scoreSystemSettings != null)
        {
            hungerDecreaseSpeed = GS.Instance.scoreSystemSettings.GetHungerDepletionRate();
        }
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
                WormSpawner.Instance.StopSpawning();
                JunkSpawner.Instance.StopSpawning();
            }
        }
    }

    public void AddHunger(float amount)
    {
        Debug.Log("hungerBar.value = "+ hungerBar.value +"asa"+ amount);
        Debug.Log("hungerBar incressval = "+ (hungerBar.value * amount) / 100);

        currentHunger += (hungerBar.value*amount)/100;
        currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
        hungerBar.value = currentHunger;
    }

    public void ResetHungerToFull(bool allowDepletion)
    {
        currentHunger = maxHunger;
        if (hungerBar != null)
        {
            hungerBar.maxValue = maxHunger;
            hungerBar.value = currentHunger;
        }

        canDecrease = allowDepletion;
    }
}
