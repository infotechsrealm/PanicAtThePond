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
    private const string TroutSpeedFieldName = "Trout_Speed_InputField";
    private const string GoldenFishBonusFieldName = "Bonuses(Golden Fish)InputField";
    private const string FishTimerFieldName = "FishTimer_InputField";
    private const string HungerDepletionRateFieldName = "DepletionHungerRate_InputField";
    private static readonly Vector2 FishTimerInputOffset = new Vector2(0f, -70f);
    private static readonly Vector2 FishTimerLabelOffset = new Vector2(-115f, 0f);
    private static readonly Vector2 HungerDepletionInputOffset = new Vector2(155f, 0f);
    private static readonly Vector2 HungerDepletionLabelOffset = new Vector2(0f, 55f);
    private static readonly Vector2 TroutSpeedInputPosition = new Vector2(310f, -318f);
    private static readonly Vector2 TroutSpeedInputSize = new Vector2(49.4f, 42.2f);
    private static readonly Vector2 DepletionLabelPosition = new Vector2(163f, -270f);
    private static readonly Vector2 DepletionLabelSize = new Vector2(180f, 56f);
    private static readonly Vector2 ScoreResetButtonPosition = new Vector2(290f, 214f);

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
    private TMP_InputField fishTimerInput;
    private TMP_InputField hungerWormRateInput;
    private TMP_InputField hungerDepletionRateInput;
    private TMP_InputField goldenFishSpeedInput;
    private TMP_InputField troutSpeedInput;
    private TMP_InputField goldenFishBonusInput;
    private Button scoreResetButton;
    private bool scoreLayoutAdjusted;

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
        ResetScoreSystemAfterLeavingLobby();

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
        ResetScoreSystemAfterLeavingLobby();
        Debug.Log("Left room successfully!");
    }

    private static void ResetScoreSystemAfterLeavingLobby()
    {
        if (GS.Instance != null)
        {
            GS.Instance.ResetScoreSystemSettings();
        }
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
        fishTimerInput = FindInput(allInputs, FishTimerFieldName);
        hungerWormRateInput = FindInput(allInputs, HungerWormRateFieldName);
        hungerDepletionRateInput = FindInput(allInputs, HungerDepletionRateFieldName);
        goldenFishSpeedInput = FindInput(allInputs, GoldenFishSpeedFieldName);
        troutSpeedInput = FindInput(allInputs, TroutSpeedFieldName);
        goldenFishBonusInput = FindInput(allInputs, GoldenFishBonusFieldName);
        CreateAdditionalDebugInputs(allInputs);

        scoreInputs.Clear();
        TryRegisterInput(fishermanWinInput);
        TryRegisterInput(fishermanCatchFishInput);
        TryRegisterInput(fishermanBucketWormInput);
        TryRegisterInput(fishWinInput);
        TryRegisterInput(fishEatWormInput);
        TryRegisterInput(fishSurviveInput);
        TryRegisterInput(spacebarJamMinInput);
        TryRegisterInput(spacebarJamMaxInput);
        TryRegisterInput(fishTimerInput);
        TryRegisterInput(hungerWormRateInput);
        TryRegisterInput(hungerDepletionRateInput);
        TryRegisterInput(goldenFishSpeedInput);
        TryRegisterInput(troutSpeedInput);
        TryRegisterInput(goldenFishBonusInput);

        CreateScoreResetButton();
        AdjustScoreSystemDebugLayout();
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
        GS.Instance.scoreSystemSettings.fishTimerSeconds = ReadInputValue(fishTimerInput);
        GS.Instance.scoreSystemSettings.hungerWormRateAmount = ReadInputValue(hungerWormRateInput);
        GS.Instance.scoreSystemSettings.hungerDepletionRate = ReadInputValue(hungerDepletionRateInput);
        GS.Instance.scoreSystemSettings.goldenFishSpeed = ReadInputValue(goldenFishSpeedInput);
        GS.Instance.scoreSystemSettings.troutSpeed = ReadInputValue(troutSpeedInput);
        GS.Instance.scoreSystemSettings.goldenFishBonusPoints = ReadInputValue(goldenFishBonusInput);
        GS.Instance.scoreSystemSettings.FillBlankValuesWithDefaults();

        GS.Instance.BroadcastScoreSystemSettingsIfHost();
    }

    private void ApplyScoreSystemSettingsToInputs()
    {
        if (GS.Instance == null || GS.Instance.scoreSystemSettings == null)
        {
            return;
        }

        GS.Instance.scoreSystemSettings.FillBlankValuesWithDefaults();

        suppressScoreInputCallbacks = true;

        WriteInputValue(fishermanWinInput, GS.Instance.scoreSystemSettings.fishermanWinPoints);
        WriteInputValue(fishermanCatchFishInput, GS.Instance.scoreSystemSettings.fishermanCatchFishPoints);
        WriteInputValue(fishermanBucketWormInput, GS.Instance.scoreSystemSettings.fishermanBucketWormPoints);
        WriteInputValue(fishWinInput, GS.Instance.scoreSystemSettings.fishWinPoints);
        WriteInputValue(fishEatWormInput, GS.Instance.scoreSystemSettings.fishEatWormPoints);
        WriteInputValue(fishSurviveInput, GS.Instance.scoreSystemSettings.fishSurvivePoints);
        WriteInputValue(spacebarJamMinInput, GS.Instance.scoreSystemSettings.spacebarJamMin);
        WriteInputValue(spacebarJamMaxInput, GS.Instance.scoreSystemSettings.spacebarJamMax);
        WriteInputValue(fishTimerInput, GS.Instance.scoreSystemSettings.fishTimerSeconds);
        WriteInputValue(hungerWormRateInput, GS.Instance.scoreSystemSettings.hungerWormRateAmount);
        WriteInputValue(hungerDepletionRateInput, GS.Instance.scoreSystemSettings.hungerDepletionRate);
        WriteInputValue(goldenFishSpeedInput, GS.Instance.scoreSystemSettings.goldenFishSpeed);
        WriteInputValue(troutSpeedInput, GS.Instance.scoreSystemSettings.troutSpeed);
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

        if (scoreResetButton != null)
        {
            scoreResetButton.interactable = canEdit;
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

    private void ResetScoreSystemToDefaults()
    {
        if (GS.Instance == null)
        {
            return;
        }

        if (GS.Instance.scoreSystemSettings == null)
        {
            GS.Instance.scoreSystemSettings = new ScoreSystemSettings();
        }

        GS.Instance.scoreSystemSettings.Reset();
        ApplyScoreSystemSettingsToInputs();
        GS.Instance.BroadcastScoreSystemSettingsIfHost();
    }

    private void CreateScoreResetButton()
    {
        if (ScoreUI == null || scoreResetButton != null)
        {
            return;
        }

        TextMeshProUGUI title = ScoreUI.GetComponentsInChildren<TextMeshProUGUI>(true)
            .FirstOrDefault(text => NormalizeInputName(text.text).Equals("score system", System.StringComparison.OrdinalIgnoreCase));

        Transform parent = title != null ? title.transform.parent : ScoreUI.transform;
        GameObject resetObject = new GameObject("ScoreSystem_ResetButton", typeof(RectTransform), typeof(Image), typeof(Button));
        resetObject.transform.SetParent(parent, false);

        RectTransform resetRect = resetObject.GetComponent<RectTransform>();
        resetRect.anchorMin = new Vector2(0.5f, 0.5f);
        resetRect.anchorMax = new Vector2(0.5f, 0.5f);
        resetRect.sizeDelta = new Vector2(72.5f, 20.7f);
        resetRect.anchoredPosition = ScoreResetButtonPosition;
        resetRect.localScale = Vector3.one * 1.9f;

        Image resetImage = resetObject.GetComponent<Image>();
        resetImage.color = new Color(0.05f, 0.45f, 0.55f, 0.85f);

        scoreResetButton = resetObject.GetComponent<Button>();
        scoreResetButton.targetGraphic = resetImage;
        scoreResetButton.onClick.RemoveAllListeners();
        scoreResetButton.onClick.AddListener(ResetScoreSystemToDefaults);

        GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(resetObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "Reset";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 16f;
        label.color = Color.white;
        if (title != null && title.font != null)
        {
            label.font = title.font;
        }
    }

    private void AdjustScoreSystemDebugLayout()
    {
        if (ScoreUI == null || scoreLayoutAdjusted)
        {
            return;
        }

        ExpandClickableInput(fishWinInput);
        ExpandClickableInput(fishEatWormInput);
        ExpandClickableInput(fishSurviveInput);

        TextMeshProUGUI[] labels = ScoreUI.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
        {
            if (label == null)
            {
                continue;
            }

            string normalizedText = NormalizeInputName(label.text);
            if (normalizedText == "Spacebar Jam" ||
                normalizedText == "Fish Timer" ||
                normalizedText == "Hunger Worm Rate")
            {
                MoveLabelLeft(label, 22f);
            }
        }

        TextMeshProUGUI fishTimerLabel = labels.FirstOrDefault(label => NormalizeInputName(label.text) == "Fish Timer");
        if (fishTimerLabel != null)
        {
            fishTimerLabel.text = "Fish Timer\nTime / Depletion";
            AlignLabelWithInput(fishTimerLabel, fishTimerInput, FishTimerLabelOffset);
        }

        ApplyScoreDebugExactRects();
        scoreLayoutAdjusted = true;
    }

    private void CreateAdditionalDebugInputs(List<TMP_InputField> allInputs)
    {
        if (fishTimerInput == null)
        {
            fishTimerInput = CreateScoreInput(FishTimerFieldName, spacebarJamMaxInput, FishTimerInputOffset, ScoreSystemSettings.DefaultFishTimerSeconds.ToString(), allInputs);
        }

        if (hungerDepletionRateInput == null)
        {
            hungerDepletionRateInput = CreateScoreInput(HungerDepletionRateFieldName, goldenFishBonusInput, HungerDepletionInputOffset, ScoreSystemSettings.DefaultHungerDepletionRate.ToString(), allInputs);
            GameObject depLabelObj = CreateScoreLabel("Depletion\nHunger Rate:", hungerDepletionRateInput, HungerDepletionLabelOffset);
            if (depLabelObj != null) depLabelObj.name = "Depletion";
        }

        if (troutSpeedInput == null && goldenFishSpeedInput != null)
        {
            troutSpeedInput = CreateScoreInput(TroutSpeedFieldName, goldenFishSpeedInput, new Vector2(-160f, 0f), ScoreSystemSettings.DefaultTroutSpeed.ToString(), allInputs);
        }

        ApplyScoreDebugExactRects();
    }

    private TMP_InputField CreateScoreInput(string objectName, TMP_InputField template, Vector2 offsetFromTemplate, string defaultValue, List<TMP_InputField> allInputs)
    {
        if (template == null)
        {
            return null;
        }

        GameObject inputObject = Instantiate(template.gameObject, template.transform.parent);
        inputObject.name = objectName;

        RectTransform inputRect = inputObject.transform as RectTransform;
        RectTransform templateRect = template.transform as RectTransform;
        if (inputRect != null && templateRect != null)
        {
            inputRect.anchorMin = templateRect.anchorMin;
            inputRect.anchorMax = templateRect.anchorMax;
            inputRect.pivot = templateRect.pivot;
            inputRect.sizeDelta = templateRect.sizeDelta;
            inputRect.anchoredPosition = templateRect.anchoredPosition + offsetFromTemplate;
        }

        TMP_InputField input = inputObject.GetComponent<TMP_InputField>();
        input.onValueChanged.RemoveAllListeners();
        input.text = defaultValue;
        input.textComponent.text = defaultValue;
        allInputs.Add(input);
        return input;
    }

    private GameObject CreateScoreLabel(string text, TMP_InputField anchorInput, Vector2 offsetFromInput)
    {
        if (anchorInput == null || anchorInput.transform.parent.Find(text) != null)
        {
            return null;
        }

        TextMeshProUGUI templateLabel = ScoreUI.GetComponentsInChildren<TextMeshProUGUI>(true)
            .FirstOrDefault(label => NormalizeInputName(label.text) == "Hunger Worm Rate");
        GameObject labelObject = new GameObject(text, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(anchorInput.transform.parent, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        RectTransform inputRect = anchorInput.transform as RectTransform;
        if (inputRect != null)
        {
            labelRect.anchorMin = inputRect.anchorMin;
            labelRect.anchorMax = inputRect.anchorMax;
            labelRect.pivot = inputRect.pivot;
            labelRect.sizeDelta = new Vector2(180f, 56f);
            labelRect.anchoredPosition = inputRect.anchoredPosition + offsetFromInput;
        }

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = templateLabel != null ? templateLabel.fontSize * 0.78f : 18f;
        label.color = templateLabel != null ? templateLabel.color : Color.cyan;
        label.alignment = TextAlignmentOptions.Left;
        if (templateLabel != null && templateLabel.font != null)
        {
            label.font = templateLabel.font;
        }

        return labelObject;
    }

    private static void AlignLabelWithInput(TextMeshProUGUI label, TMP_InputField input, Vector2 offsetFromInput)
    {
        RectTransform labelRect = label != null ? label.transform as RectTransform : null;
        RectTransform inputRect = input != null ? input.transform as RectTransform : null;
        if (labelRect == null || inputRect == null)
        {
            return;
        }

        if (labelRect.parent != inputRect.parent)
        {
            labelRect.SetParent(inputRect.parent, false);
        }

        labelRect.anchorMin = inputRect.anchorMin;
        labelRect.anchorMax = inputRect.anchorMax;
        labelRect.pivot = inputRect.pivot;
        labelRect.anchoredPosition = inputRect.anchoredPosition + offsetFromInput;
        labelRect.sizeDelta = new Vector2(190f, 56f);
        label.alignment = TextAlignmentOptions.Center;
    }

    private static void ExpandClickableInput(TMP_InputField input)
    {
        if (input == null)
        {
            return;
        }

        RectTransform rectTransform = input.transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x + 16f, rectTransform.sizeDelta.y + 8f);
        }
    }

    private static void MoveLabelLeft(TextMeshProUGUI label, float amount)
    {
        RectTransform rectTransform = label.transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition += Vector2.left * amount;
        }
    }

    private void ApplyScoreDebugExactRects()
    {
        SetRect(troutSpeedInput != null ? troutSpeedInput.transform as RectTransform : null, TroutSpeedInputPosition, TroutSpeedInputSize);

        TextMeshProUGUI depletionLabel = ScoreUI != null
            ? ScoreUI.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(label => label.name == "Depletion")
            : null;
        SetRect(depletionLabel != null ? depletionLabel.transform as RectTransform : null, DepletionLabelPosition, DepletionLabelSize);
    }

    private static void SetRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
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
