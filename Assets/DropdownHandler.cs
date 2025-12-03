using UnityEngine;
using UnityEngine.UI;

public class DropdownHandler : MonoBehaviour
{
    [Header("Assign UI Elements")]
    public Dropdown waterDropdown;     // Legacy Dropdown
    public Text modeTitleText;         // BIG TEXT (e.g., CLEAR WATERS)
    public Text descriptionText;       // Right side description text


    void Start()
    {
        if (waterDropdown == null)
        {
            Debug.LogError("❌ Dropdown not assigned!");
            return;
        }

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

    void OnDropdownChanged(int index)
    {
        string selectedOption = waterDropdown.options[index].text;

        Debug.Log("🌊 Selected Water Mode: " + selectedOption);

        // (Optional) handle with switch
                    //SetVisiblity();
        switch (selectedOption)
        {
            case "ClearWater":
                {
                    GS.Instance.ClearWaters = true;
                    modeTitleText.text = "CLEAR WATERS";
                    descriptionText.text = "nBoth sides can see each other.";
                    Debug.Log("✔ ClearWater selected");
                    break;
                }

            case "MurkyWater":
                {
                    GS.Instance.MurkyWaters = true;

                    modeTitleText.text = "MURKY WATERS";
                    descriptionText.text = "Neither side can see each other.";
                    Debug.Log("✔ MurkyWater selected");
                    break;
                }

            case "DeepWater":
                {
                    GS.Instance.DeepWaters = true;

                    modeTitleText.text = "DEEP WATERS";
                    descriptionText.text = "Fisherman can’t see fish; fish can see him.";
                    Debug.Log("✔ DeepWater selected");
                    break;
                }

            case "ReflectiveWater":
                {
                    GS.Instance.ReflectiveWater = true;
                    modeTitleText.text = "REFLECTIVE WATERS";
                    descriptionText.text = "Fisherman can see fish; fish can’t see him.";
                    Debug.Log("✔ ReflectiveWater selected");
                    break;
                }
        }
    }
 
    public void SetVisiblity()
    {
         GS gsObj = GS.Instance;
        gsObj.DeepWaters = false;
        gsObj.MurkyWaters = false;
        gsObj.ClearWaters = false;
        gsObj.ReflectiveWater = false;
    }
   
}