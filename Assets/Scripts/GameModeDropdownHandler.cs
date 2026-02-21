using Mirror;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class GameModeDropdownHandler : MonoBehaviourPunCallbacks
{
    [Header("Assign UI Elements")]
    public Dropdown gameModeDropdown; 
    public Text gameModeTitleText;
    public Text gameModeDescriptionText;

    public static GameModeDropdownHandler Instance;
    private void Awake()
    {
        Instance = this;

        // Clear old options (optional)
        gameModeDropdown.ClearOptions();

        // Add 4 options
        gameModeDropdown.AddOptions(new System.Collections.Generic.List<string>()
        {
            "Quick Survivalist",
            "Quick Cast",
            "Deep Sea Fishing"
        });

        // Listener add
        gameModeDropdown.onValueChanged.AddListener(OnGameModeChanged);

    }

    public int gameModeDropDownIndex = 0;
    public void OnGameModeChanged(int index)
    {
        string selectedOption = gameModeDropdown.options[index].text;

        switch (selectedOption)
        {
            case "Quick Survivalist":
                {
                    gameModeDropDownIndex = 0;
                    if (GS.Instance != null) GS.Instance.currentGameMode = 0;
                    gameModeTitleText.text = "Quick Survivalist";
                    gameModeDescriptionText.text = "A single chaotic round where Fish and Fisherman clash. Win or lose-no points.";
                    break;
                }

            case "Quick Cast":
                {
                    gameModeDropDownIndex = 1;
                    if (GS.Instance != null) GS.Instance.currentGameMode = 1;
                    gameModeTitleText.text = "Quick Cast";
                    gameModeDescriptionText.text = "Compete in 1 rounds and snag some points.";
                    break;
                }

            case "Deep Sea Fishing":
                {
                    gameModeDropDownIndex = 2;
                    if (GS.Instance != null) GS.Instance.currentGameMode = 2;
                    gameModeTitleText.text = "Deep Sea Fishing";
                    gameModeDescriptionText.text = "Compete across 5 rounds and catch a bunch of points.";
                    break;
                }
        }

    }
}