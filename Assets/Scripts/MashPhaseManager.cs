using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MashPhaseManager : NetworkBehaviour
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

    
    IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);

        GameManager gm = GameManager.Instance;

        mashPanel = gm.mashPanel;
        mashSlider = gm.mashSlider;
        mashText = gm.mashText;
    }
    public void StartMashPhase()
    {
        if (IsServer)
        {
            ExecuteFunctionLocal();
            ExecuteFunctionObserversRpc();
        }
        else
        {
            CallUniversalServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CallUniversalServerRpc()
    {
        ExecuteFunctionLocal();

        ExecuteFunctionObserversRpc();
    }

    [ObserversRpc]
    private void ExecuteFunctionObserversRpc()
    {
        ExecuteFunctionLocal();
    }

    private void ExecuteFunctionLocal()
    {
        if (FishermanController.instance.isfisherMan || GameManager.Instance.myFish.isCatchedFish)
        {
            JunkSpawner.instance.canSpawn = WormSpawner.instance.canSpawn = FishermanController.instance.isCanMove = HungerSystem.instance.canDecrease = false;

            if (mashPanel != null) mashPanel.SetActive(true);
            if (mashSlider != null) mashSlider.value = 0f;

            active = true;
            mashText.text = "MASH SPACE BAR!";
        }
    }

    void Update()
    {
        if (IsServer)
        {
            Debug.Log("i m Server");
        }
        if (!active) return;



        if (Input.GetKeyDown(KeyCode.Space))
        {
            mashSlider.value += mashSpeed * Time.deltaTime * 60;
        }

        if (mashSlider.value >= 1f)
        {
            mashSlider.value = 0;
            if (FishermanController.instance.isfisherMan)
            {
                EndMashPhase(true); // Fisherman caught
            }
            else
            {
                EndMashPhase(false); // Fisherman caught
            }
        }
    }

    // This is what you'll call from anywhere
    public void EndMashPhase(bool fishermanWon)
    {
        if (IsServer)
        {
            EndMashPhaseObserversRpc(fishermanWon);
        }
        else
        {
            // Client requests server to handle
            EndMashPhaseServerRpc(fishermanWon);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void EndMashPhaseServerRpc(bool fishermanWon)
    {
        // Only broadcast (server will also receive via RPC)
        EndMashPhaseObserversRpc(fishermanWon);
    }

    [ObserversRpc]
    private void EndMashPhaseObserversRpc(bool fishermanWon)
    {
        EndMashPhaseLocal(fishermanWon);
    }

    private void EndMashPhaseLocal(bool fishermanWon)
    {
        Debug.Log("EndMashPhaseLocal Called");
        active = false;
        if (mashPanel != null) mashPanel.SetActive(false);

        if (fishermanWon)
        {
            FishermanController.instance.catchadFishes++;
            Debug.Log("Fish won the mash phase! Escaped hook.");
            Transform myfish2 = GameObject.FindGameObjectWithTag("CatchdFish").GetComponent<Transform>();
            myfish2.transform.SetParent(Hook.instance.wormParent.transform);
            myfish2.transform.localPosition = Vector3.zero;
            myfish2.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
              
            JunkSpawner.instance.canSpawn =
            WormSpawner.instance.canSpawn =
            FishermanController.instance.isCanMove = true;
        }
        else
        {
            JunkSpawner.instance.canSpawn =
           WormSpawner.instance.canSpawn =
           FishermanController.instance.isCanMove =
           HungerSystem.instance.canDecrease =
           GameManager.Instance.myFish.canMove = true;
            HungerSystem.instance.AddHunger(75f);
            Debug.Log("Fisherman won the mash phase! Caught fish.");

            for (int i = 0; i < GameManager.Instance.AllFishPlayers.Count; i++)
            {
                if (GameManager.Instance.AllFishPlayers[i] != null)
                {
                    GameManager.Instance.AllFishPlayers[i].ChangeTag("Fish");
                }
                else
                {
                    Debug.Log("FishController is null");
                }
            }
        }

        if (IsServer)
        {
            Hook.instance.LoadReturnToRod();
        }
    }
}
