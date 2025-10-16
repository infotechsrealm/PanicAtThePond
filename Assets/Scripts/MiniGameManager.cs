using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MiniGameManager : MonoBehaviourPunCallbacks
{
    public static MiniGameManager instance;

    public Text miniGameText;      // UI text to show sequence
    public Text timerText;         // UI text to show countdown
    public GameObject miniGamePanel;

    private string currentSequence;
    private int progress;
    internal bool active = false;

    private float timeLimit = 5f;
    private float timeRemaining;


    void Awake()
    {
        instance = this;
    }

    public void StartMiniGame()
    {
        FishermanController.Instence.isCasting = HungerSystem.instance.canDecrease =  FishController.Instence.canMove = false;

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

    /*void Update()
    {
        if (!active) return;

        if (Keyboard.current.anyKey.wasPressedThisFrame)
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
    }*/

    void Update()
    {
        if (!active) return;

        if (Keyboard.current.anyKey.wasPressedThisFrame)
        {
            var expectedKey = currentSequence[progress].ToString().ToLower();

            // convert the expected character to a Key enum
            Key key = (Key)System.Enum.Parse(typeof(Key), expectedKey.ToUpper());

            if (Keyboard.current[key].wasPressedThisFrame)
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
        Debug.Log("Mini-game Success! Fish escaped with worm!");

        HungerSystem.instance.canDecrease = FishController.Instence.canMove = true;
        HungerSystem.instance.AddHunger(75f);
        FishController.Instence.animator.SetBool("isFight", false);
        active = false;
        miniGamePanel.transform.localScale = Vector3.zero;
        if (GameManager.instance.myFish != null)
        {
            GameManager.instance.myFish.catchadeFish = false;
        }
        Hook.instance.CallRpcToReturnRod();
        if (timerText != null) timerText.text = "";
    }
        
    void Fail()
    {
        Debug.Log("Mini-game Failed! Fisherman caught the fish!");

        active = false;
        miniGamePanel.transform.localScale = Vector3.zero;

        if (!PhotonNetwork.IsMasterClient)
        {
            MashPhaseManager.instance.StartMashPhase();
        }

        if (timerText != null) timerText.text = "";
    }

    [PunRPC]
    void DestroyWormRPC(int viewID)
    {
        PhotonView target = PhotonView.Find(viewID);
        if (target != null)
        {
            PhotonNetwork.Destroy(target.gameObject);
        }
    }
}
