using Mirror;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class DropdownHandler : MonoBehaviourPunCallbacks
{
    [Header("Assign UI Elements")]
    public Dropdown waterDropdown;     // Legacy Dropdown
    public Text modeTitleText;         // BIG TEXT (e.g., CLEAR WATERS)
    public Text descriptionText;       // Right side description text

    public static DropdownHandler Instance;
    private void Awake()
    {
        Instance = this;


        // Clear old options (optional)
        waterDropdown.ClearOptions();

        // Add 4 options
        waterDropdown.AddOptions(new System.Collections.Generic.List<string>()
        {
            "ClearWater",
            "MurkyWater",
            "DeepWater",
            "ReflectiveWater"
        });

        // Listener add
        waterDropdown.onValueChanged.AddListener(OnDropdownChanged);
    }
    private void OnEnable()
    {
       GS.Instance.rerfeshDropDown();
    }
  
    private void Update()
    {
        if(GS.Instance.isLan)
        {
            if(!GS.Instance.IsMirrorMasterClient && GS.Instance.dropDownChangeAvalable)
            {
                GS.Instance.rerfeshDropDown();
            }
        }
    }

    public int dropDownIndex = 0;
    public void OnDropdownChanged(int index)
    {
        string selectedOption = waterDropdown.options[index].text;

        Debug.Log("🌊 Selected Water Mode: " + selectedOption);

        // (Optional) handle with switch
        ResatVisiblity();
        switch (selectedOption)
        {
            case "ClearWater":
                {
                    dropDownIndex = 0;
                    GS.Instance.ClearWaters = true;
                    modeTitleText.text = "CLEAR WATERS";
                    descriptionText.text = "Both sides can see each other.";
                    Debug.Log("✔ ClearWater selected");
                    break;
                }

            case "MurkyWater":
                {
                    dropDownIndex = 1;
                    GS.Instance.MurkyWaters = true;

                    modeTitleText.text = "MURKY WATERS";
                    descriptionText.text = "Neither side can see each other.";
                    Debug.Log("✔ MurkyWater selected");
                    break;
                }

            case "DeepWater":
                {
                    dropDownIndex = 2;
                    GS.Instance.DeepWaters = true;

                    modeTitleText.text = "DEEP WATERS";
                    descriptionText.text = "Fisherman can’t see fish; fish can see him.";
                    Debug.Log("✔ DeepWater selected");
                    break;
                }

            case "ReflectiveWater":
                {
                    dropDownIndex = 3;
                    GS.Instance.ReflectiveWater = true;
                    modeTitleText.text = "REFLECTIVE WATERS";
                    descriptionText.text = "Fisherman can see fish; fish can’t see him.";
                    Debug.Log("✔ ReflectiveWater selected");
                    break;
                }
        }

        GS gsObj = GS.Instance;
        if (gsObj.isLan)
        {
            if (gsObj.IsMirrorMasterClient)
            {
                CustomNetworkManager.Instence.CallBroadcastVisibility();
            }
        }
        else
        {
            if(PhotonNetwork.IsMasterClient)
            {
                CreateJoinManager.Instance.SetVissiblity_Photon_RPC();
            }
        }
    }

    

    public void ResatVisiblity()
    {
        GS gsObj = GS.Instance;
        gsObj.DeepWaters = false;
        gsObj.MurkyWaters = false;
        gsObj.ClearWaters = false;
        gsObj.ReflectiveWater = false;
    }


   /* public void refreshDropDown()
    {
        int index = 0;
        GS gsObj = GS.Instance;
        if (gsObj.ClearWaters)
        {
            index = 0;
        }
        else if (gsObj.MurkyWaters)
        {
            index = 1;
        }
        else if (gsObj.DeepWaters)
        {
            index = 2;
        }
        else if (gsObj.ReflectiveWater)
        {
            index = 3;
        }
            OnDropdownChanged(index);
        waterDropdown.value = index;   // dropdown option index set karega
        waterDropdown.RefreshShownValue();   // UI ko update karega
    }*/
   
}