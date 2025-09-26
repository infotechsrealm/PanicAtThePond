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


    [PunRPC]
    public void CallMashPhaseRPC()
    {
        active = true;

        if (mashPanel != null)
        {
            mashPanel.SetActive(true);
        }

        if (mashSlider != null)
        {
            mashSlider.value = 0f;
        }

        mashText.text = "MASH SPACE BAR!";

        if (PhotonNetwork.IsMasterClient)
        {
            WormSpawner.instance.canSpawn = false;
            JunkSpawner.instance.canSpawn = false;
            FishermanController.instance.isCanCast = false;
        }
    }

    public void StartMashPhase()
    {
        active = true;

        if (mashPanel != null)
        {
            mashPanel.SetActive(true);
        }

        if (mashSlider != null)
        {
            mashSlider.value = 0f;
        }

        mashText.text = "MASH SPACE BAR!";

        if (PhotonNetwork.IsMasterClient)
        {
            WormSpawner.instance.canSpawn = false;
            JunkSpawner.instance.canSpawn = false;
            FishermanController.instance.isCanCast = false;
        }
        else
        {
            photonView.RPC(nameof(CallMashPhaseRPC), RpcTarget.MasterClient);
           
        }
    }


    void Update()
    {
        if (!active) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            mashSlider.value += mashSpeed * Time.deltaTime * 60;
        }

        // Check end conditions
        if (mashSlider.value >= 1f)
        {

            if (PhotonNetwork.IsMasterClient)
            {
                FishermanController.instance.catchadFish++;
                photonView.RPC(nameof(EndMashPhase), RpcTarget.All,true);
            }
            else
            {
                if (GameManager.instance.myFish)
                {
                    photonView.RPC(nameof(EndMashPhase), RpcTarget.All,false);
                }

            }
        }
    }

    [PunRPC]
    public void EndMashPhase(bool fisherManIsWin)
    {
        WormSpawner.instance.canSpawn = true;
        JunkSpawner.instance.canSpawn = true;
        FishermanController.instance.isCanCast = true;
        if (mashPanel != null) mashPanel.SetActive(false);

        if (fisherManIsWin)
        {
            photonView.RPC(nameof(FindCatchadFish), RpcTarget.Others);
            Debug.Log("Fish won the mash phase! Escaped hook.");
        }
        else
        {
            HungerSystem.instance.AddHunger(75f);
            GameManager.instance.myFish.canMove = true;
            GameManager.instance.myFish.catchadeFish = false;
            Debug.Log("Fisherman won the mash phase! Caught fish.");

            if (PhotonNetwork.IsMasterClient)
            {
                Hook.instance.CallRpcToReturnRod();
            }
        }

        active = false;

    }

    [PunRPC]
    public void FindCatchadFish()
    {
        FishController fish = GameManager.instance.myFish;
        if (fish != null)
        {
            if (fish.catchadeFish)
            {
                fish.PutFishInHookRPC();
            }
        }
    }

   

}
