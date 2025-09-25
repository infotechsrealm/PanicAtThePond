using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager instance;

    public Text miniGameText;      // UI text to show sequence
    public Text timerText;         // UI text to show countdown
    public GameObject miniGamePanel;

    private string currentSequence;
    private int progress;
    private bool active = false;

    private float timeLimit = 5f;
    private float timeRemaining;

    internal GameObject catchedFish;

    void Awake()
    {
        instance = this;
    }

    public void StartMiniGame()
    {
        FishermanController.instance.isCasting = HungerSystem.instance.canDecrease =  FishController.instance.canMove = false;

        active = true;
        progress = 0;

        // Random sequence A–Z
        currentSequence = "";
        for (int i = 0; i < 3; i++)  // length 3
        {
            char randomChar = (char)('A' + Random.Range(0, 26));
            currentSequence += randomChar;
        }

        // show UI
        miniGamePanel.transform.localScale = Vector3.one;
        UpdateMiniGameText();

        // timer
        timeRemaining = timeLimit;
        StartCoroutine(UpdateTimer());
    }

    void Update()
    {
        if (!active) return;

        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(currentSequence[progress].ToString().ToLower()))
            {
                progress++;
                UpdateMiniGameText(); // refresh UI colors

                if (progress >= currentSequence.Length)
                {
                    Success();
                }
            }
            else
            {
                Fail();
            }
        }
    }

    void UpdateMiniGameText()
    {
        string display = "";
        for (int i = 0; i < currentSequence.Length; i++)
        {
            if (i < progress)
                display += $"<color=green>{currentSequence[i]}</color> ";
            else
                display += $"{currentSequence[i]} ";
        }
        miniGameText.text = "Press: " + display;
    }

    IEnumerator UpdateTimer()
    {
        while (active && timeRemaining > 0)
        {
            if (timerText != null)
                timerText.text = "Time: " + Mathf.Ceil(timeRemaining).ToString();

            timeRemaining -= Time.deltaTime;
            yield return null;
        }

        if (active)
            Fail();
    }

    void Success()
    {
        FishermanController.instance.isCanMove =  HungerSystem.instance.canDecrease = FishController.instance.canMove = true;
        HungerSystem.instance.AddHunger(75f);

        active = false;
        miniGamePanel.transform.localScale = Vector3.zero;
        Destroy(catchedFish);
        Hook.instance.LoadReturnToRod();
        Debug.Log("Mini-game Success! Fish escaped with worm!");
        if (timerText != null) timerText.text = "";
    }
        
    void Fail()
    {


        active = false;
        miniGamePanel.transform.localScale = Vector3.zero;
        Destroy(catchedFish);
        Hook.instance.LoadReturnToRod();
        MashPhaseManager.instance.StartMashPhase();
        Debug.Log("Mini-game Failed! Fisherman caught the fish!");
        if (timerText != null) timerText.text = "";
    }
}
