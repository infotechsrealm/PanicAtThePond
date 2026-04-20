using Mirror;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HostLobby : MonoBehaviourPunCallbacks
{
    private const string FishermanWinFieldName = "FisherManWin_InputField";
    private const string FishermanFishFieldName = "FisherMan_Fish_InputField";
    private const string FishWinFieldName = "FishWinInputField";
    private const string FishEatWormFieldName = "Fish_Fish_InputField";
    private const string FishSurviveFieldName = "Fish_Fish_WormPoints_InputField";
    private const string SpacebarJamMinFieldName = "SpaceBar_Jam_Min_InputField";
    private const string SpacebarJamMaxFieldName = "SpaceBar_Jam_Max_InputField";
    private const string HungerWormRateFieldName = "Hunger Worm Rate_InputField";
    private const string GoldenFishSpeedFieldName = "GoldenFish_Speed_InputField";
    private const string GoldenFishBonusFieldName = "Bonuses(Golden Fish)InputField";

    public PlayerTableManager playerTableManager;
    public Button backButton, controlsButton, hintButton, pauseButton, ScoreSystem, BackScore;
    public GameObject hintUI, controlUI, pauseUI, ScoreUI;

    private readonly List<TMP_InputField> scoreInputs = new List<TMP_InputField>();
    private bool scoreInputsInitialized;
    private bool suppressScoreInputCallbacks;

    private TMP_InputField fishermanWinInput;
    private TMP_InputField fishermanCatchFishInput;
    private TMP_InputField fishermanBucketWormInput;
    private TMP_InputField fishWinInput;
    private TMP_InputField fishEatWormInput;
    private TMP_InputField fishSurviveInput;
    private TMP_InputField spacebarJamMinInput;
    private TMP_InputField spacebarJamMaxInput;
    private TMP_InputField hungerWormRateInput;
    private TMP_InputField goldenFishSpeedInput;
    private TMP_InputField goldenFishBonusInput;

    private void Start()
    {
        controlsButton.onClick.AddListener(OnControlPressed);
        hintButton.onClick.AddListener(OnHintPressed);
        pauseButton.onClick.AddListener(Pause);
        ScoreSystem.onClick.AddListener(Open_Score);
        BackScore.onClick.AddListener(Close_Score);
        RefreshScoreSystemUIFromState();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        BackManager.instance.RegisterScreen(pauseButton);
        playerTableManager.UpdatePlayerTable();
        RefreshScoreSystemUIFromState();
    }

    public void Open_Score()
    {
        ScoreUI.SetActive(true);
        RefreshScoreSystemUIFromState();
    }

    public void Close_Score()
    {
        SaveScoreSystemSettingsFromInputs();
        ScoreUI.SetActive(false);
    }

    private void OnControlPressed()
    {
        controlUI.SetActive(true);
    }

    private void OnHintPressed()
    {
        hintUI.SetActive(true);
    }

    private void Pause()
    {
        pauseUI.SetActive(true);
    }

    public void Close()
    {
        BackManager.instance.UnregisterScreen();

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                CoustomeRoomManager.Instance.CallLeaveRoom();
            }
            else
            {
                Debug.Log("Leaving room...");
                GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);
                gameObject.SetActive(false);
                PhotonNetwork.LeaveRoom();
            }
        }
        else
        {
            LANDiscoveryMenu.Instance.networkDiscovery.StopDiscovery();

            if (NetworkServer.active && NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkServer.active)
            {
                NetworkManager.singleton.StopServer();
            }

            gameObject.SetActive(false);
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room successfully!");
    }

    public void RefreshScoreSystemUIFromState()
    {
        InitializeScoreSystemInputs();
        ApplyScoreSystemSettingsToInputs();
        UpdateScoreInputInteractivity();
    }

    private void InitializeScoreSystemInputs()
    {
        if (scoreInputsInitialized || ScoreUI == null)
        {
            return;
        }

        List<TMP_InputField> allInputs = ScoreUI.GetComponentsInChildren<TMP_InputField>(true).ToList();

        fishermanWinInput = FindInput(allInputs, FishermanWinFieldName);

        List<TMP_InputField> fishermanFishInputs = FindInputs(allInputs, FishermanFishFieldName)
            .OrderBy(GetInputXPosition)
            .ToList();
        fishermanCatchFishInput = fishermanFishInputs.ElementAtOrDefault(0);
        fishermanBucketWormInput = fishermanFishInputs.ElementAtOrDefault(1);

        fishWinInput = FindInput(allInputs, FishWinFieldName);
        fishEatWormInput = FindInput(allInputs, FishEatWormFieldName);
        fishSurviveInput = FindInput(allInputs, FishSurviveFieldName);
        spacebarJamMinInput = FindInput(allInputs, SpacebarJamMinFieldName);
        spacebarJamMaxInput = FindInput(allInputs, SpacebarJamMaxFieldName);
        hungerWormRateInput = FindInput(allInputs, HungerWormRateFieldName);
        goldenFishSpeedInput = FindInput(allInputs, GoldenFishSpeedFieldName);
        goldenFishBonusInput = FindInput(allInputs, GoldenFishBonusFieldName);

        scoreInputs.Clear();
        TryRegisterInput(fishermanWinInput);
        TryRegisterInput(fishermanCatchFishInput);
        TryRegisterInput(fishermanBucketWormInput);
        TryRegisterInput(fishWinInput);
        TryRegisterInput(fishEatWormInput);
        TryRegisterInput(fishSurviveInput);
        TryRegisterInput(spacebarJamMinInput);
        TryRegisterInput(spacebarJamMaxInput);
        TryRegisterInput(hungerWormRateInput);
        TryRegisterInput(goldenFishSpeedInput);
        TryRegisterInput(goldenFishBonusInput);

        scoreInputsInitialized = true;
    }

    private void SaveScoreSystemSettingsFromInputs()
    {
        if (suppressScoreInputCallbacks || GS.Instance == null)
        {
            return;
        }

        if (GS.Instance.scoreSystemSettings == null)
        {
            GS.Instance.scoreSystemSettings = new ScoreSystemSettings();
        }

        GS.Instance.scoreSystemSettings.fishermanWinPoints = ReadInputValue(fishermanWinInput);
        GS.Instance.scoreSystemSettings.fishermanCatchFishPoints = ReadInputValue(fishermanCatchFishInput);
        GS.Instance.scoreSystemSettings.fishermanBucketWormPoints = ReadInputValue(fishermanBucketWormInput);
        GS.Instance.scoreSystemSettings.fishWinPoints = ReadInputValue(fishWinInput);
        GS.Instance.scoreSystemSettings.fishEatWormPoints = ReadInputValue(fishEatWormInput);
        GS.Instance.scoreSystemSettings.fishSurvivePoints = ReadInputValue(fishSurviveInput);
        GS.Instance.scoreSystemSettings.spacebarJamMin = ReadInputValue(spacebarJamMinInput);
        GS.Instance.scoreSystemSettings.spacebarJamMax = ReadInputValue(spacebarJamMaxInput);
        GS.Instance.scoreSystemSettings.hungerWormRateAmount = ReadInputValue(hungerWormRateInput);
        GS.Instance.scoreSystemSettings.goldenFishSpeed = ReadInputValue(goldenFishSpeedInput);
        GS.Instance.scoreSystemSettings.goldenFishBonusPoints = ReadInputValue(goldenFishBonusInput);

        GS.Instance.BroadcastScoreSystemSettingsIfHost();
    }

    private void ApplyScoreSystemSettingsToInputs()
    {
        if (GS.Instance == null || GS.Instance.scoreSystemSettings == null)
        {
            return;
        }

        suppressScoreInputCallbacks = true;

        WriteInputValue(fishermanWinInput, GS.Instance.scoreSystemSettings.fishermanWinPoints);
        WriteInputValue(fishermanCatchFishInput, GS.Instance.scoreSystemSettings.fishermanCatchFishPoints);
        WriteInputValue(fishermanBucketWormInput, GS.Instance.scoreSystemSettings.fishermanBucketWormPoints);
        WriteInputValue(fishWinInput, GS.Instance.scoreSystemSettings.fishWinPoints);
        WriteInputValue(fishEatWormInput, GS.Instance.scoreSystemSettings.fishEatWormPoints);
        WriteInputValue(fishSurviveInput, GS.Instance.scoreSystemSettings.fishSurvivePoints);
        WriteInputValue(spacebarJamMinInput, GS.Instance.scoreSystemSettings.spacebarJamMin);
        WriteInputValue(spacebarJamMaxInput, GS.Instance.scoreSystemSettings.spacebarJamMax);
        WriteInputValue(hungerWormRateInput, GS.Instance.scoreSystemSettings.hungerWormRateAmount);
        WriteInputValue(goldenFishSpeedInput, GS.Instance.scoreSystemSettings.goldenFishSpeed);
        WriteInputValue(goldenFishBonusInput, GS.Instance.scoreSystemSettings.goldenFishBonusPoints);

        suppressScoreInputCallbacks = false;
    }

    private void UpdateScoreInputInteractivity()
    {
        bool canEdit = CanEditScoreSystemSettings();

        foreach (TMP_InputField input in scoreInputs)
        {
            if (input != null)
            {
                input.interactable = canEdit;
            }
        }
    }

    private bool CanEditScoreSystemSettings()
    {
        if (GS.Instance == null)
        {
            return true;
        }

        if (GS.Instance.isLan)
        {
            return NetworkServer.active || !NetworkClient.isConnected || GS.Instance.IsMirrorMasterClient;
        }

        return !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
    }

    private void TryRegisterInput(TMP_InputField input)
    {
        if (input == null || scoreInputs.Contains(input))
        {
            return;
        }

        input.onValueChanged.RemoveListener(OnScoreInputValueChanged);
        input.onValueChanged.AddListener(OnScoreInputValueChanged);
        scoreInputs.Add(input);
    }

    private void OnScoreInputValueChanged(string _)
    {
        SaveScoreSystemSettingsFromInputs();
    }

    private static TMP_InputField FindInput(IEnumerable<TMP_InputField> inputs, string targetName)
    {
        return FindInputs(inputs, targetName).FirstOrDefault();
    }

    private static IEnumerable<TMP_InputField> FindInputs(IEnumerable<TMP_InputField> inputs, string targetName)
    {
        string normalizedTargetName = NormalizeInputName(targetName);
        return inputs.Where(input => NormalizeInputName(input.name) == normalizedTargetName);
    }

    private static string NormalizeInputName(string inputName)
    {
        return string.IsNullOrWhiteSpace(inputName) ? string.Empty : inputName.Trim();
    }

    private static float GetInputXPosition(TMP_InputField input)
    {
        RectTransform rectTransform = input.transform as RectTransform;
        return rectTransform != null ? rectTransform.anchoredPosition.x : 0f;
    }

    private static string ReadInputValue(TMP_InputField input)
    {
        return input == null ? string.Empty : input.text.Trim();
    }

    private static void WriteInputValue(TMP_InputField input, string value)
    {
        if (input == null)
        {
            return;
        }

        string safeValue = value ?? string.Empty;
        if (input.text != safeValue)
        {
            input.text = safeValue;
        }
    }
}
