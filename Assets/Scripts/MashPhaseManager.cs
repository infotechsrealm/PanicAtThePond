using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MashPhaseManager : MonoBehaviourPunCallbacks
{
    public static MashPhaseManager Instance;

   

    [Header("UI")]
    public GameObject mashPanel;
    public Slider mashSlider;   // 0 = escape (fish), 1 = capture (fisherman)
    public Text mashText;

    [Header("Settings")]
    public float mashSpeed = 0.01f; 
    public float decayRate = 0.002f;

    internal bool active = false;

    void Awake()
    {
        Instance = this;
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
            FishermanController.Instance.OnFightAnimation(true);
            WormSpawner.Instance.canSpawn = false;
            JunkSpawner.Instance.canSpawn = false;
            FishermanController.Instance.isCanCast = false;
        }
    }

    public void CallMashPhase_Mirror()
    {
        if(GameManager.Instance.myFish.isFisherMan)
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
            FishermanController.Instance.OnFightAnimation(true);
            WormSpawner.Instance.canSpawn = false;
            JunkSpawner.Instance.canSpawn = false;
            FishermanController.Instance.isCanCast = false;
        }
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


        if (PhotonNetwork.IsMasterClient || GameManager.Instance.myFish.isFisherMan)
        {
         
            FishermanController.Instance.isCanCast = false;
            if (GS.Instance.isLan)
            {
                GameManager.Instance.myFish.fishController_Mirror.CallMashPhase();
            }
        }
        else
        {
            if (GS.Instance.isLan)
            {
                GameManager.Instance.myFish.fishController_Mirror.CallMashPhase();
            }
            else
            {
                photonView.RPC(nameof(CallMashPhaseRPC), RpcTarget.MasterClient);
            }
        }
    }


    void Update()
    {
        if (!active) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            mashSlider.value += mashSpeed * Time.deltaTime * 60;
        }

        // Check end conditions
        if (mashSlider.value >= 1f)
        {
            if (GS.Instance.isLan)
            {
                if(GameManager.Instance.myFish.isFisherMan)
                {
                    EndMashPhase_Mirror(true);
                }
                else
                {
                    EndMashPhase_Mirror(false);
                }

                GameManager.Instance.myFish.fishController_Mirror.CallDisableMashPhase();
            }
            else
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    photonView.RPC(nameof(EndMashPhase), RpcTarget.All, true);
                }
                else
                {
                    photonView.RPC(nameof(EndMashPhase), RpcTarget.All, false);
                }
            }
        }
    }


    public void DisableMashPhase()
    {
        if (mashPanel != null) mashPanel.SetActive(false);
        FindCatchadFish_Mirror();
    }

    public void EndMashPhase_Mirror(bool fisherManIsWin)
    {
        FishermanController fishermanController = FishermanController.Instance;

        if(GameManager.Instance.isFisherMan)
        {
            fishermanController.isCanCast = true;
        }

        if (fisherManIsWin)
        {
            if (fishermanController != null)
            {
                fishermanController.catchadFish++;
            }

            fishermanController.OnFightAnimation(false);

        }
        else
        {
            if (!GameManager.Instance.myFish.isFisherMan)
            {
                HungerSystem.Instance.AddHunger(75f);
                GameManager.Instance.myFish.canMove = true;
                GameManager.Instance.myFish.animator.SetBool("isFight", false);
                GameManager.Instance.myFish.catchadeFish = false;
                Debug.Log("Fisherman won the mash phase! Caught fish.");
            }

            GameManager.Instance.myFish.fishController_Mirror.ReturnRoadOfHook();

        }

        active = false;

    }

    [PunRPC]
    public void EndMashPhase(bool fisherManIsWin)
    {
        WormSpawner.Instance.canSpawn = true;
        JunkSpawner.Instance.canSpawn = true;
        FishermanController fishermanController =  FishermanController.Instance;
        fishermanController.isCanCast = true; 
        if (mashPanel != null) mashPanel.SetActive(false);

        if (fisherManIsWin)
        {
            if (fishermanController != null)
            {
                fishermanController.catchadFish++;
            }
            if (PhotonNetwork.IsMasterClient)
            {
                fishermanController.OnFightAnimation(false);
            }
            photonView.RPC(nameof(FindCatchadFish), RpcTarget.Others);
        }
        else
        {
            if (GameManager.Instance.myFish)
            {
                GameManager.Instance.myFish.catchadeFish = false;
                HungerSystem.Instance.AddHunger(75f);
                GameManager.Instance.myFish.canMove = true;
                GameManager.Instance.myFish.animator.SetBool("isFight", false);
                Debug.Log("Fisherman won the mash phase! Caught fish.");
            }
            Hook.Instance.CallRpcToReturnRod();
        }

        active = false;

    }

    [PunRPC]
    public void FindCatchadFish()
    {
        FishController fish = GameManager.Instance.myFish;
        if (fish != null)
        {
            if (fish.catchadeFish)
            {
                fish.PutFishInHook();
            }
        }
    }

    public void FindCatchadFish_Mirror()
    {
        FishController fish = GameManager.Instance.myFish;
        if (fish != null)
        {
            if (fish.catchadeFish)
            {
                Debug.Log("Im cetchade fish");
                fish.PutFishInHook();
            }
        }
    }




}
