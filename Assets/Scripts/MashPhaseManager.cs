using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class MashPhaseManager : MonoBehaviourPunCallbacks
{
    public static MashPhaseManager instance;

    [Header("UI")]
    public GameObject mashPanel;
    public Slider mashSlider;   // 0 = escape (fish), 1 = capture (fisherman)
    public Text mashText;

    [Header("Settings")]
    public float mashSpeed = 0.01f; 
    public float decayRate = 0.002f; 

    private bool active = false;

    void Awake()
    {
        instance = this;
    }


    /*public void CallMashPhaseRPC()
    {
        photonView.RPC("StartMashPhase", RpcTarget.MasterClient);
    }*/

    public void StartMashPhase()
    {
        Debug.Log("afsjguafibsjdfhkjskdkdj");
        if (mashPanel != null)
        {
            mashPanel.SetActive(true);
        }
        if (mashSlider != null)
        {
            mashSlider.value = 0f;
        }

        active = true;
        mashText.text = "MASH SPACE BAR!";

        if(PhotonNetwork.IsMasterClient)
        {
            WormSpawner.instance.canSpawn = false;
            JunkSpawner.instance.canSpawn = false;
            FishController.instance.canMove = false;
        }


       // CallMashPhaseRPC();
    }

    void Update()
    {
        if (!active) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            mashSlider.value += mashSpeed * Time.deltaTime * 60;
        }

        // Check end conditions
        if (mashSlider.value <= 0f)
        {
            EndMashPhase(true);  // Fish escaped
        }
        else if (mashSlider.value >= 1f)
        {
            EndMashPhase(false); // Fisherman caught
        }
    }

    void EndMashPhase(bool fishWon)
    {

        active = false;
        if (mashPanel != null) mashPanel.SetActive(false);

        JunkSpawner.instance.canSpawn = WormSpawner.instance.canSpawn = FishermanController.instance.isCanMove = HungerSystem.instance.canDecrease = FishController.instance.canMove = true;
       
        if (fishWon)
        {
            HungerSystem.instance.AddHunger(75f);
            Debug.Log("Fish won the mash phase! Escaped hook.");

        }
        else
        {
            Debug.Log("Fisherman won the mash phase! Caught fish.");
        }
    }
}
