using UnityEngine;
using UnityEngine.UI;

public class HungerSystem : MonoBehaviour
{
    public static HungerSystem instance;

    public Slider hungerBar;
    public float maxHunger = 100f;
    public float hungerDecreaseSpeed = 5f;

    private float currentHunger;

    public bool canDecrease;

    void Awake()
    {
        instance = this;
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

        if (GameManager.instance != null)
        {
            currentHunger -= hungerDecreaseSpeed * Time.deltaTime;
            currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);

            hungerBar.value = currentHunger;

            if (currentHunger <= 0)
            {
                WormSpawner.instance.StopSpawning();
                JunkSpawner.instance.StopSpawning();
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
}
