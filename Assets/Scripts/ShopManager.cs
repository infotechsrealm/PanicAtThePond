using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ShopManager : MonoBehaviour
{
    [Serializable]
    public class CosmeticPreviewRule
    {
        public string CosmeticName;
        public string PreviewSpriteName;
    }

    [Header("Main Shop Buttons")]
    public Button HatButton;
    public Button RoadButton;
    public Button CheastButton;
    public Button BackHatButton;
    public Button BackCheastButton;

    [Header("Fish/Fisherman Buttons")]
    public Button FishButtonCosmetic;
    public Button FisherManButtonCostmetic;
    public Button Close_FishShopUi;
    public Button Close_FishermanShopUI;
    public Button close_Fish_FishermanCosmeticPanelButton;

    [Header("Shop Panels")]
    public GameObject ShopItemPanel;
    public GameObject HatItemsPanel;
    public GameObject FishVoyageDiagram;
    public GameObject cheastPanel;
    public GameObject RoadPanel;
    public TextMeshProUGUI ShopCoinText;

    [Header("Fish/Fisherman Cosmetic Panels")]
    public GameObject FishFishermanCosmeticPanel;
    public GameObject FishCosmeticPanel;
    public GameObject FishermanCosmeticPanel;

    [Header("Cosmetic Category Buttons")]
    public Button FishFaceButton;
    public Button FishermanFaceButton;
    public Button FishermanHatButton;

    [Header("Fisherman Cosmetic Categories")]
    public GameObject FishermanHatObject;
    public GameObject FishermanHairObject;

    [Header("Cosmetic Item Opacity")]
    public Transform FishCosmeticItemsRoot;
    public Transform FishermanCosmeticItemsRoot;
    [Range(0f, 1f)] public float SelectedItemOpacity = 1f;
    [Range(0f, 1f)] public float UnselectedItemOpacity = 0.35f;

    [Header("Cosmetic Cell Sprites")]
    public Sprite BoxSelectedSprite;
    public Sprite BoxUnselectedSprite;

    [Header("Fish/Fisherman Dropdown")]
    public Button FishFishermanDropdownButton;
    public GameObject FishFishermanDropdownList;
    public Button FishOptionButton;
    public Button FishermanOptionButton;
    public Transform FishFishermanDropdownArrow;
    public Text FishFishermanDropdownText;
    public TMP_Text FishFishermanDropdownTMPText;

    [Header("Hat Category Dropdown")]
    public Button HatDropdownButton;
    public GameObject HatDropdownList;
    public Button HatOptionButton;
    public Button FishSpeciesOptionButton;
    public Button HairOptionButton;
    public Button HatHairOptionButton;
    public Transform HatDropdownArrow;
    public Text HatDropdownText;
    public TMP_Text HatDropdownTMPText;
    public Image DiagramPreviewImage; // Shows "daigram preview.png" in bottom right when Fish Species selected

    [Header("Display Preview")]
    public GameObject ShopPreviewRoot;
    public GameObject FishDisplayObject;
    public GameObject[] FishDisplayObjects;
    public GameObject FishermanDisplayObject;
    public Button DisplayHatButton;
    public GameObject HatDisplayObject;

    [Header("Dynamic Hat Preview")]
    public bool UseDynamicHatPreviewSprites = true;
    public string FishPreviewAssetFolder = "Assets/UI/Game UI/Fish preview";
    public string FishermanPreviewAssetFolder = "Assets/UI/Game UI/Fishermna Preview";
    public List<CosmeticPreviewRule> FishPreviewRules = new List<CosmeticPreviewRule>();
    public List<CosmeticPreviewRule> FishermanPreviewRules = new List<CosmeticPreviewRule>();

    [Header("Sal-t Shop")]
    public Button SaltShopButton;
    public GameObject SaltShopPanel;
    public Button SaltShopBackButton;

    [Header("Animated Shop GIF")]
    public bool autoAnimateFishingshopGif = true;
    public Image SaltShopAnimatedImage;
    public string fishingshopFramesResourceFolder = "Fishingshop2Frames";
    public float fishingshopFramesPerSecond = 10f;

    private bool isFishFishermanDropdownOpen;
    private bool isHatDropdownOpen;
    private bool isFishSelected;
    private bool isFishermanSelected;
    private UIImageFrameAnimator saltShopAnimator;
    private readonly List<Button> fishCosmeticItemButtons = new List<Button>();
    private readonly List<Button> fishermanCosmeticItemButtons = new List<Button>();
    private readonly List<UnityAction> fishCosmeticItemActions = new List<UnityAction>();
    private readonly List<UnityAction> fishermanCosmeticItemActions = new List<UnityAction>();
    private readonly Dictionary<Image, Sprite> displayBaseSprites = new Dictionary<Image, Sprite>();
    private readonly Dictionary<string, Sprite>[] fishPreviewSpritesByCosmetic = new Dictionary<string, Sprite>[2];
    private readonly Dictionary<string, Sprite> fishermanPreviewSpritesByCosmetic = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, Sprite> previewSpritesByName = new Dictionary<string, Sprite>();
    private List<Sprite> orderedFishermanHatPreviewSprites;
    private bool previewMapsBuilt;
    private const string FishHatPreviewChildName = "Applied Fish Hat Cosmetic Preview";
    private const string FishDisplayModeHat = "Hat";
    private const string FishDisplayModeSpecies = "Fish Species";
    private const string FishermanDisplayModeHat = "Hat";
    private const string FishermanDisplayModeHair = "Hair";
    private const string FishermanHeadphonePreviewResourcePath = "ShopUI/Fisherman Preview/Fishermna headphone hat";
    private static readonly Color SelectedCellOutlineColor = new Color(0.12f, 0.55f, 1f, 1f);
    private static readonly Vector2 SelectedCellOutlineDistance = new Vector2(3f, -3f);
    private string selectedFishDisplayMode = FishDisplayModeHat;
    private string selectedFishermanDisplayMode = FishermanDisplayModeHat;
    private LocalPlayManager shopLocalPlayManager;
    private bool useLegacyHatDropdownLabel;

    private void Awake()
    {
        ResolvePanelReferences();
        ResolveCloseButton();
        ResolveCosmeticCategoryReferences();
        ResolveCosmeticItemRoots();
        ResolveShopPreviewReferences();
        ResolveFishDisplayObjects();
        ResolvePreviewAssetFolderPaths();
        ConfigureHatDropdownLabels();

        AddButtonListener(HatButton, HatShopUI);
        AddButtonListener(RoadButton, RoadShopUI);
        AddButtonListener(BackHatButton, BackHatPanelUI);
        AddButtonListener(CheastButton, CheastShopUI);
        AddButtonListener(BackCheastButton, BackCheastPanelUI);
        AddButtonListener(FishButtonCosmetic, FishShopUI);
        AddButtonListener(FisherManButtonCostmetic, FishermanShopUI);
        AddButtonListener(Close_FishShopUi, CloseFishShopUI);
        AddButtonListener(Close_FishermanShopUI, CloseFishermanShopUI);
        AddButtonListener(close_Fish_FishermanCosmeticPanelButton, CloseFishFishermanShopUI);
        AddButtonListener(FishFishermanDropdownButton, ToggleFishFishermanDropdown);
        AddButtonListener(FishOptionButton, SelectFishDisplay);
        AddButtonListener(FishermanOptionButton, SelectFishermanDisplay);
        AddButtonListener(DisplayHatButton, SelectHatDisplay);
        AddButtonListener(HatDropdownButton, ToggleHatDropdown);
        AddButtonListener(HatOptionButton, SelectHatOption);
        AddButtonListener(FishSpeciesOptionButton, SelectFishSpeciesOption);
        // Scene wiring: HairOptionButton = "hat Button Fisherman", HatHairOptionButton = "Hair Button Fisherman."
        AddButtonListener(HairOptionButton, SelectHatHairOption);
        AddButtonListener(HatHairOptionButton, SelectHairOption);
        AddButtonListener(FishFaceButton, FishShopUI);
        AddButtonListener(FishermanFaceButton, SelectFishermanHairCategory);
        AddButtonListener(FishermanHatButton, SelectFishermanHatCategory);
        AddButtonListener(SaltShopButton, OpenSaltShop);
        AddButtonListener(SaltShopBackButton, CloseSaltShop);

        RegisterCosmeticItemButtons();
    }

    private void Start()
    {
        SetActiveIfNotNull(FishVoyageDiagram, false);
        SetActiveIfNotNull(FishFishermanDropdownList, false);
        SetActiveIfNotNull(HatDropdownList, false);
        SetDisplayControlLabel(selectedFishDisplayMode);
        SetupAnimatedSaltShopGif();
        SetActiveIfNotNull(SaltShopPanel, false);
        StartCoroutine(FetchCoinsForShop());
        LoadDiagramPreviewSprite();
    }

    private void OnDestroy()
    {
        RemoveButtonListener(HatButton, HatShopUI);
        RemoveButtonListener(RoadButton, RoadShopUI);
        RemoveButtonListener(BackHatButton, BackHatPanelUI);
        RemoveButtonListener(CheastButton, CheastShopUI);
        RemoveButtonListener(BackCheastButton, BackCheastPanelUI);
        RemoveButtonListener(FishButtonCosmetic, FishShopUI);
        RemoveButtonListener(FisherManButtonCostmetic, FishermanShopUI);
        RemoveButtonListener(Close_FishShopUi, CloseFishShopUI);
        RemoveButtonListener(Close_FishermanShopUI, CloseFishermanShopUI);
        RemoveButtonListener(close_Fish_FishermanCosmeticPanelButton, CloseFishFishermanShopUI);
        RemoveButtonListener(FishFishermanDropdownButton, ToggleFishFishermanDropdown);
        RemoveButtonListener(FishOptionButton, SelectFishDisplay);
        RemoveButtonListener(FishermanOptionButton, SelectFishermanDisplay);
        RemoveButtonListener(DisplayHatButton, SelectHatDisplay);
        RemoveButtonListener(HatDropdownButton, ToggleHatDropdown);
        RemoveButtonListener(HatOptionButton, SelectHatOption);
        RemoveButtonListener(FishSpeciesOptionButton, SelectFishSpeciesOption);
        RemoveButtonListener(HairOptionButton, SelectHatHairOption);
        RemoveButtonListener(HatHairOptionButton, SelectHairOption);
        RemoveButtonListener(FishFaceButton, FishShopUI);
        RemoveButtonListener(FishermanFaceButton, SelectFishermanHairCategory);
        RemoveButtonListener(FishermanHatButton, SelectFishermanHatCategory);
        RemoveButtonListener(SaltShopButton, OpenSaltShop);
        RemoveButtonListener(SaltShopBackButton, CloseSaltShop);
        RemoveCosmeticItemButtonListeners(fishCosmeticItemButtons, fishCosmeticItemActions);
        RemoveCosmeticItemButtonListeners(fishermanCosmeticItemButtons, fishermanCosmeticItemActions);
    }

    private IEnumerator FetchCoinsForShop()
    {
        if (PlayFabManager.Instance == null || ShopCoinText == null)
        {
            yield break;
        }

        while (!PlayFabManager.Instance.IsLoggedIn)
        {
            yield return null;
        }

        PlayFabManager.Instance.GetCurrency(amount =>
        {
            ShopCoinText.text = amount.ToString();
        });
    }

    public void HatShopUI()
    {
        OpenShopItemPanel();
        SetActiveIfNotNull(HatItemsPanel, true);
        SetActiveIfNotNull(FishVoyageDiagram, false);
        SetActiveIfNotNull(cheastPanel, false);
        SetActiveIfNotNull(RoadPanel, false);
        SetActiveIfNotNull(FishFishermanCosmeticPanel, true);
        SetActiveIfNotNull(FishCosmeticPanel, false);
        SetActiveIfNotNull(FishermanCosmeticPanel, false);
    }

    public void RoadShopUI()
    {
        OpenShopItemPanel();
        SetActiveIfNotNull(HatItemsPanel, false);
        SetActiveIfNotNull(FishVoyageDiagram, true);
        SetActiveIfNotNull(RoadPanel, true);
        SetActiveIfNotNull(cheastPanel, false);
        CloseFishFishermanShopUI();
    }

    public void CheastShopUI()
    {
        OpenShopItemPanel();
        SetActiveIfNotNull(HatItemsPanel, false);
        SetActiveIfNotNull(FishVoyageDiagram, false);
        SetActiveIfNotNull(RoadPanel, false);
        SetActiveIfNotNull(cheastPanel, true);
        CloseFishFishermanShopUI();
    }

    public void FishShopUI()
    {
        OpenShopItemPanel();
        SetActiveIfNotNull(HatItemsPanel, false);
        SetActiveIfNotNull(cheastPanel, false);
        SetActiveIfNotNull(RoadPanel, false);
        SetActiveIfNotNull(FishFishermanCosmeticPanel, true);
        SetActiveIfNotNull(FishCosmeticPanel, true);
        SetActiveIfNotNull(FishermanCosmeticPanel, false);
        SelectFishDisplay();
    }

    public void FishermanShopUI()
    {
        OpenShopItemPanel();
        SetActiveIfNotNull(HatItemsPanel, false);
        SetActiveIfNotNull(FishVoyageDiagram, false);
        SetActiveIfNotNull(cheastPanel, false);
        SetActiveIfNotNull(RoadPanel, false);
        SetActiveIfNotNull(FishFishermanCosmeticPanel, true);
        SetActiveIfNotNull(FishCosmeticPanel, false);
        SetActiveIfNotNull(FishermanCosmeticPanel, true);
        SelectFishermanDisplay();
    }

    public void fishermanShopUI()
    {
        FishermanShopUI();
    }

    public void BackHatPanelUI()
    {
        SetActiveIfNotNull(HatItemsPanel, false);
        SetActiveIfNotNull(ShopItemPanel, false);
        SetActiveIfNotNull(FishVoyageDiagram, false);
        SetActiveIfNotNull(RoadPanel, false);
        CloseFishFishermanShopUI();
    }

    public void BackCheastPanelUI()
    {
        SetActiveIfNotNull(cheastPanel, false);
    }

    public void CloseFishShopUI()
    {
        SetActiveIfNotNull(FishCosmeticPanel, false);
    }

    public void CloseFishermanShopUI()
    {
        SetActiveIfNotNull(FishermanCosmeticPanel, false);
        SetActiveIfNotNull(FishermanHairObject, false);
        SetActiveIfNotNull(FishermanHatObject, false);
    }

    public void CloseFishFishermanShopUI()
    {
        isFishSelected = false;
        isFishermanSelected = false;
        SetActiveIfNotNull(FishFishermanCosmeticPanel, false);
        SetActiveIfNotNull(FishCosmeticPanel, false);
        SetActiveIfNotNull(FishermanCosmeticPanel, false);
        SetActiveIfNotNull(FishermanHairObject, false);
        SetActiveIfNotNull(FishermanHatObject, false);
        CloseFishFishermanDropdown();
        CloseHatDropdown();
        SetActiveIfNotNull(ShopItemPanel, false);
    }

    public void CloseFish_FishermanShopUI()
    {
        CloseFishFishermanShopUI();
    }

    public void ToggleFishFishermanDropdown()
    {
        SetFishFishermanDropdownOpen(!isFishFishermanDropdownOpen);
    }

    public void ToggleHatDropdown()
    {
        UpdateDisplayedDropdownOptions();
        SetHatDropdownOpen(!isHatDropdownOpen);
    }

    public void SelectFishDisplay()
    {
        EnsureShopPreviewRootActive();
        isFishSelected = true;
        isFishermanSelected = false;
        SetDisplayMode("Fish", true, false, IsFishHatModeSelected());
        ClearFishermanHatFromDisplay();
        HideFishVoyageDiagramPreview();
        UpdateDisplayedDropdownOptions();
        ApplySelectedFishDisplayMode();
        CloseFishFishermanDropdown();
    }

    public void SelectFishermanDisplay()
    {
        EnsureShopPreviewRootActive();
        isFishermanSelected = true;
        isFishSelected = false;
        SetDisplayMode("Fisherman", false, true, IsFishermanHatModeSelected());
        ClearFishHatFromDisplay();
        HideFishVoyageDiagramPreview();
        UpdateDisplayedDropdownOptions();
        ApplySelectedFishermanDisplayMode();
        CloseFishFishermanDropdown();
    }

    public void SelectHatDisplay()
    {
        CloseFishFishermanDropdown();
        CloseHatDropdown();
    }

    public void SelectFishermanHairCategory()
    {
        selectedFishermanDisplayMode = FishermanDisplayModeHair;
        SetActiveIfNotNull(FishermanHairObject, true);
        SetActiveIfNotNull(FishermanHatObject, false);
        SetActiveIfNotNull(FishermanCosmeticPanel, true);
        SetActiveIfNotNull(FishCosmeticPanel, false);
        CloseHatDropdown();
    }

    public void SelectFishermanHatCategory()
    {
        selectedFishermanDisplayMode = FishermanDisplayModeHat;
        SetActiveIfNotNull(FishermanHatObject, true);
        SetActiveIfNotNull(FishermanHairObject, false);
        SetActiveIfNotNull(FishermanCosmeticPanel, true);
        SetActiveIfNotNull(FishCosmeticPanel, false);
        CloseHatDropdown();
    }

    public void HideFishFishermanDisplay()
    {
        SetDisplayMode(string.Empty, false, false, false);
    }

    public void OpenSaltShop()
    {
        OpenShopItemPanel();
        SetActiveIfNotNull(SaltShopPanel, true);
        SetActiveIfNotNull(FishFishermanCosmeticPanel, false);
        SetActiveIfNotNull(FishCosmeticPanel, false);
        SetActiveIfNotNull(FishermanCosmeticPanel, false);
        SetActiveIfNotNull(FishermanHairObject, false);
        SetActiveIfNotNull(FishermanHatObject, false);
        CloseFishFishermanDropdown();
        PlaySaltShopGif();
    }

    public void CloseSaltShop()
    {
        SetActiveIfNotNull(SaltShopPanel, false);
        SetActiveIfNotNull(ShopItemPanel, false);
    }

    private void OpenShopItemPanel()
    {
        SetActiveIfNotNull(ShopItemPanel, true);
        EnsureShopPreviewRootActive();
    }

    private void EnsureShopPreviewRootActive()
    {
        SetActiveIfNotNull(ShopPreviewRoot, true);
    }

    private void ResolveShopPreviewReferences()
    {
        if (ShopPreviewRoot == null)
        {
            ShopPreviewRoot = FindGameObjectByNames(transform.root, "Shop");
        }

        if (ShopPreviewRoot != null && shopLocalPlayManager == null)
        {
            shopLocalPlayManager = ShopPreviewRoot.GetComponent<LocalPlayManager>();
        }

        if (FishermanDisplayObject == null && ShopPreviewRoot != null)
        {
            FishermanDisplayObject = FindGameObjectByNames(
                ShopPreviewRoot.transform,
                "FisherMan yellow hat",
                "Fisherman yellow hat",
                "Fisherman Display",
                "FishermanDisplay");
        }
    }

    private void ConfigureHatDropdownLabels()
    {
        useLegacyHatDropdownLabel = HatDropdownTMPText == null && HatDropdownText != null;

        if (HatDropdownTMPText != null && HatDropdownText != null)
        {
            HatDropdownText.gameObject.SetActive(false);
            useLegacyHatDropdownLabel = false;
        }
    }

    private void SetDisplayMode(string label, bool showFish, bool showFisherman, bool showHat)
    {
        SetFishDisplayVisible(showFish);
        SetActiveIfNotNull(FishermanDisplayObject, showFisherman);
        SetActiveIfNotNull(HatDisplayObject, showHat && !UseDynamicHatPreviewSprites);
        SetDropdownLabel(label);
        CloseFishFishermanDropdown();
    }

    private void ApplySelectedFishDisplayMode()
    {
        SetDisplayControlLabel(selectedFishDisplayMode);
        bool showSpecies = IsFishSpeciesModeSelected();
        SetActiveIfNotNull(HatDisplayObject, !showSpecies && !UseDynamicHatPreviewSprites);

        if (showSpecies)
        {
            ClearFishHatFromDisplay();
            RefreshSelectedFishDisplay();
            ShowFishVoyageDiagramPreview();
            return;
        }

        ApplySavedFishHatToDisplay();
        RefreshBottomRightPreview();
    }

    private void ApplySelectedFishermanDisplayMode()
    {
        SetActiveIfNotNull(FishVoyageDiagram, false);
        SetDisplayControlLabel(selectedFishermanDisplayMode);
        SetActiveIfNotNull(HatDisplayObject, IsFishermanHatModeSelected() && !UseDynamicHatPreviewSprites);

        if (IsFishermanHatModeSelected())
        {
            SetActiveIfNotNull(FishermanHatObject, true);
            SetActiveIfNotNull(FishermanHairObject, false);
            SetActiveIfNotNull(FishermanCosmeticPanel, true);
            SetActiveIfNotNull(FishCosmeticPanel, false);
        }
        else
        {
            SetActiveIfNotNull(FishermanHairObject, true);
            SetActiveIfNotNull(FishermanHatObject, false);
            SetActiveIfNotNull(FishermanCosmeticPanel, true);
            SetActiveIfNotNull(FishCosmeticPanel, false);
        }

        ApplySavedFishermanDisplayModeSprite();
        RefreshBottomRightPreview();
    }

    private void ApplySavedFishermanDisplayModeSprite()
    {
        Sprite selectedSprite = IsFishermanHatModeSelected()
            ? CosmeticRuntimeApplier.GetSelectedFishermanHat()
            : CosmeticRuntimeApplier.GetSelectedFishermanHair();

        if (selectedSprite == null)
        {
            if (IsFishermanHatModeSelected() && TryShowDefaultFishermanHatPreview())
            {
                return;
            }

            ClearFishermanHatFromDisplay();
            return;
        }

        ApplySelectedFishermanHatToDisplay(selectedSprite);
    }

    private bool TryShowDefaultFishermanHatPreview()
    {
        List<Sprite> previewSprites = GetOrderedFishermanHatPreviewSprites();
        if (previewSprites.Count == 0 || FishermanDisplayObject == null)
        {
            return false;
        }

        Image fishermanImage = FishermanDisplayObject.GetComponent<Image>();
        if (fishermanImage == null)
        {
            return false;
        }

        fishermanImage.sprite = previewSprites[0];
        fishermanImage.preserveAspect = true;
        RefreshBottomRightPreview();
        return true;
    }

    private void RefreshSelectedFishDisplay()
    {
        ResolveFishDisplayObjects();
        if (FishDisplayObjects != null && FishDisplayObjects.Length > 0)
        {
            int selectedFish = Mathf.Clamp(PlayerPrefs.GetInt(LocalPlayManager.SelectedFishPrefKey, 0), 0, FishDisplayObjects.Length - 1);
            for (int i = 0; i < FishDisplayObjects.Length; i++)
            {
                SetActiveIfNotNull(FishDisplayObjects[i], i == selectedFish);
            }
        }
    }

    private bool IsFishHatModeSelected()
    {
        return selectedFishDisplayMode == FishDisplayModeHat;
    }

    private bool IsFishSpeciesModeSelected()
    {
        return selectedFishDisplayMode == FishDisplayModeSpecies;
    }

    private bool IsFishermanHatModeSelected()
    {
        return selectedFishermanDisplayMode == FishermanDisplayModeHat;
    }

    private void HideFishermanDisplayPreview()
    {
        if (FishermanDisplayObject == null || !FishermanDisplayObject.activeSelf)
        {
            return;
        }

        ClearFishermanHatFromDisplay();
        SetActiveIfNotNull(FishermanDisplayObject, false);
    }

    private void SetFishDisplayVisible(bool visible)
    {
        ResolveFishDisplayObjects();

        if (FishDisplayObjects != null && FishDisplayObjects.Length > 0)
        {
            int selectedFish = PlayerPrefs.GetInt("SelectedFish", 0);
            for (int i = 0; i < FishDisplayObjects.Length; i++)
            {
                SetActiveIfNotNull(FishDisplayObjects[i], visible && (i == selectedFish));
            }

            return;
        }

        SetActiveIfNotNull(FishDisplayObject, visible);
    }

    private void ResolveFishDisplayObjects()
    {
        if (FishDisplayObject == null && ShopPreviewRoot != null)
        {
            FishDisplayObject = FindGameObjectByNames(ShopPreviewRoot.transform, "Fish 1", "Fish1", "Fish");
        }

        if (FishDisplayObjects != null && FishDisplayObjects.Length > 1)
        {
            return;
        }

        Transform displayRoot = FishDisplayObject != null ? FishDisplayObject.transform.parent : null;
        if (displayRoot == null && ShopPreviewRoot != null)
        {
            displayRoot = ShopPreviewRoot.transform;
        }

        if (displayRoot == null)
        {
            return;
        }

        GameObject fishOne = FishDisplayObject != null
            ? FishDisplayObject
            : FindGameObjectByNames(displayRoot, "Fish 1", "Fish1", "Fish");
        GameObject fishTwo = FindGameObjectByNames(displayRoot, "Fish 2", "Fish2");
        if (fishOne == null && fishTwo == null)
        {
            return;
        }

        if (fishTwo != null)
        {
            FishDisplayObjects = new GameObject[]
            {
                fishOne != null ? fishOne : fishTwo,
                fishTwo
            };
        }
        else if (fishOne != null)
        {
            FishDisplayObjects = new GameObject[] { fishOne };
        }
    }

    private void SetFishFishermanDropdownOpen(bool open)
    {
        isFishFishermanDropdownOpen = open;
        SetActiveIfNotNull(FishFishermanDropdownList, open);

        if (FishFishermanDropdownArrow != null)
        {
            Vector3 angles = FishFishermanDropdownArrow.localEulerAngles;
            angles.z = open ? 180f : 0f;
            FishFishermanDropdownArrow.localEulerAngles = angles;
        }
    }

    private void CloseFishFishermanDropdown()
    {
        SetFishFishermanDropdownOpen(false);
        CloseHatDropdown();
    }

    private void CloseHatDropdown()
    {
        SetHatDropdownOpen(false);
    }

    private void SetHatDropdownOpen(bool open)
    {
        isHatDropdownOpen = open;
        SetActiveIfNotNull(HatDropdownList, open);

        if (HatDropdownArrow != null)
        {
            Vector3 angles = HatDropdownArrow.localEulerAngles;
            angles.z = open ? 180f : 0f;
            HatDropdownArrow.localEulerAngles = angles;
        }
    }

    // Duplicate of line 337 - removed

    public void SelectHatOption()
    {
        if (isFishermanSelected)
        {
            return;
        }

        selectedFishDisplayMode = FishDisplayModeHat;
        CloseHatDropdown();
        SetHatDropdownLabel(FishDisplayModeHat);
        SetDisplayControlLabel(FishDisplayModeHat);
        SetActiveIfNotNull(FishCosmeticPanel, true);
        ApplySelectedFishDisplayMode();
    }

    public void SelectFishSpeciesOption()
    {
        if (isFishermanSelected)
        {
            return;
        }

        EnsureShopPreviewRootActive();
        selectedFishDisplayMode = FishDisplayModeSpecies;
        CloseHatDropdown();
        SetHatDropdownLabel(FishDisplayModeSpecies);
        SetDisplayControlLabel(FishDisplayModeSpecies);
        ApplySelectedFishDisplayMode();
    }

    public void SelectHairOption()
    {
        if (!isFishermanSelected)
        {
            return;
        }

        selectedFishermanDisplayMode = FishermanDisplayModeHair;
        CloseHatDropdown();
        SetHatDropdownLabel(FishermanDisplayModeHair);
        SetDisplayControlLabel(FishermanDisplayModeHair);
        ApplySelectedFishermanDisplayMode();
    }

    public void SelectHatHairOption()
    {
        if (!isFishermanSelected)
        {
            return;
        }

        selectedFishermanDisplayMode = FishermanDisplayModeHat;
        CloseHatDropdown();
        SetHatDropdownLabel(FishermanDisplayModeHat);
        SetDisplayControlLabel(FishermanDisplayModeHat);
        ApplySelectedFishermanDisplayMode();
    }

    // Duplicate removed - kept only at line 619

    private void SetHatDropdownLabel(string label)
    {
        if (useLegacyHatDropdownLabel)
        {
            if (HatDropdownText != null)
            {
                HatDropdownText.text = label;
            }
        }
        else if (HatDropdownTMPText != null)
        {
            HatDropdownTMPText.text = label;
        }
        else if (HatDropdownText != null)
        {
            HatDropdownText.text = label;
        }
    }

    private void ShowFishVoyageDiagramPreview()
    {
        if (DiagramPreviewImage != null)
        {
            if (DiagramPreviewImage.sprite == null)
            {
                LoadDiagramPreviewSprite();
            }

            DiagramPreviewImage.enabled = true;
            DiagramPreviewImage.gameObject.SetActive(true);
            DiagramPreviewImage.transform.SetAsLastSibling();
        }

        if (HatDisplayObject != null && HatDisplayObject != DiagramPreviewImage?.gameObject)
        {
            HatDisplayObject.SetActive(false);
        }
    }

    private void HideFishVoyageDiagramPreview()
    {
        if (DiagramPreviewImage != null)
        {
            DiagramPreviewImage.gameObject.SetActive(false);
        }
    }

    private bool IsHatIconPreviewMode()
    {
        if (isFishSelected && IsFishHatModeSelected())
        {
            return true;
        }

        return isFishermanSelected && IsFishermanHatModeSelected();
    }

    private void RefreshBottomRightPreview()
    {
        if (IsFishSpeciesModeSelected() && isFishSelected)
        {
            ShowFishVoyageDiagramPreview();
            return;
        }

        if (IsHatIconPreviewMode())
        {
            ShowCurrentHatIconPreview();
            return;
        }

        HideBottomRightPreview();
    }

    private void ShowCurrentHatIconPreview()
    {
        if (DiagramPreviewImage == null)
        {
            return;
        }

        Sprite hatIcon = isFishermanSelected
            ? CosmeticRuntimeApplier.GetSelectedFishermanHat()
            : CosmeticRuntimeApplier.GetSelectedFishHat();

        if (hatIcon == null)
        {
            HideBottomRightPreview();
            return;
        }

        DiagramPreviewImage.sprite = hatIcon;
        DiagramPreviewImage.preserveAspect = true;
        DiagramPreviewImage.enabled = true;
        DiagramPreviewImage.gameObject.SetActive(true);
        DiagramPreviewImage.transform.SetAsLastSibling();
    }

    private void HideBottomRightPreview()
    {
        HideFishVoyageDiagramPreview();
    }

    private void LoadDiagramPreviewSprite()
    {
        if (DiagramPreviewImage == null)
        {
            return;
        }

        Sprite diagramSprite = Resources.Load<Sprite>("ShopUI/daigram preview");
        if (diagramSprite == null)
        {
            diagramSprite = Resources.Load<Sprite>("ShopUI/diagram preview");
        }

        if (diagramSprite == null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>("ShopUI");
            for (int i = 0; i < sprites.Length; i++)
            {
                string spriteName = NormalizeSpriteName(sprites[i].name);
                if (spriteName.Contains("diagram") || spriteName.Contains("daigram") || spriteName.Contains("voyage"))
                {
                    diagramSprite = sprites[i];
                    break;
                }
            }
        }

        if (diagramSprite != null)
        {
            DiagramPreviewImage.sprite = diagramSprite;
            DiagramPreviewImage.preserveAspect = true;
        }
    }

    private void UpdateDisplayedDropdownOptions()
    {
        if (isFishermanSelected)
        {
            // Hide fish-only options; show fisherman Hat + Hair (last two buttons in the dropdown list).
            SetActiveIfNotNull(HatOptionButton?.gameObject, false);
            SetActiveIfNotNull(FishSpeciesOptionButton?.gameObject, false);
            SetActiveIfNotNull(HairOptionButton?.gameObject, true);
            SetActiveIfNotNull(HatHairOptionButton?.gameObject, true);
        }
        else if (isFishSelected)
        {
            SetActiveIfNotNull(HatOptionButton?.gameObject, true);
            SetActiveIfNotNull(FishSpeciesOptionButton?.gameObject, true);
            SetActiveIfNotNull(HairOptionButton?.gameObject, false);
            SetActiveIfNotNull(HatHairOptionButton?.gameObject, false);
        }
        else
        {
            SetActiveIfNotNull(HatOptionButton?.gameObject, false);
            SetActiveIfNotNull(FishSpeciesOptionButton?.gameObject, false);
            SetActiveIfNotNull(HairOptionButton?.gameObject, false);
            SetActiveIfNotNull(HatHairOptionButton?.gameObject, false);
        }
    }

    private void SetDropdownLabel(string label)
    {
        if (FishFishermanDropdownText != null)
        {
            FishFishermanDropdownText.text = label;
        }

        if (FishFishermanDropdownTMPText != null)
        {
            FishFishermanDropdownTMPText.text = label;
        }
    }

    private void SetDisplayControlLabel(string label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return;
        }

        SetHatDropdownLabel(label);
    }

    private void SetupAnimatedSaltShopGif()
    {
        if (!autoAnimateFishingshopGif || SaltShopPanel == null)
        {
            return;
        }

        if (SaltShopAnimatedImage == null)
        {
            SaltShopAnimatedImage = FindAnimatedShopImage(SaltShopPanel.transform);
        }

        if (SaltShopAnimatedImage == null)
        {
            return;
        }

        saltShopAnimator = SaltShopAnimatedImage.GetComponent<UIImageFrameAnimator>();
        if (saltShopAnimator == null)
        {
            saltShopAnimator = SaltShopAnimatedImage.gameObject.AddComponent<UIImageFrameAnimator>();
        }

        saltShopAnimator.resourcesFolder = fishingshopFramesResourceFolder;
        saltShopAnimator.framesPerSecond = fishingshopFramesPerSecond;
        saltShopAnimator.loop = true;
        saltShopAnimator.playOnEnable = true;
    }

    private void PlaySaltShopGif()
    {
        if (!autoAnimateFishingshopGif)
        {
            return;
        }

        if (saltShopAnimator == null)
        {
            SetupAnimatedSaltShopGif();
        }

        if (saltShopAnimator != null)
        {
            saltShopAnimator.Play();
        }
    }

    private void ResolveCloseButton()
    {
        if (Close_FishShopUi == null && FishCosmeticPanel != null)
        {
            Close_FishShopUi = FindButtonByNames(FishCosmeticPanel.transform, "x", "X", "Close", "Close Button", "CloseButton");
        }

        if (Close_FishermanShopUI == null && FishermanCosmeticPanel != null)
        {
            Close_FishermanShopUI = FindButtonByNames(FishermanCosmeticPanel.transform, "x", "X", "Close", "Close Button", "CloseButton");
        }

        if (close_Fish_FishermanCosmeticPanelButton == null)
        {
            Transform root = FishFishermanCosmeticPanel != null ? FishFishermanCosmeticPanel.transform : transform.root;
            close_Fish_FishermanCosmeticPanelButton = FindButtonByNames(root, "Close", "Close Button", "CloseButton", "X", "x", "Back");
        }
    }

    private void ResolvePanelReferences()
    {
        Transform root = transform.root;

        if (FishFishermanCosmeticPanel == null)
        {
            FishFishermanCosmeticPanel = FindGameObjectByNames(
                root,
                "Fish/Fisherman Cosmetic Panel",
                "Fish Fisherman Cosmetic Panel",
                "FishFishermanCosmeticPanel");
        }

        if (FishCosmeticPanel == null)
        {
            FishCosmeticPanel = FindGameObjectByNames(root, "Fish Cosmetic", "Fish Cosmetic Panel", "FishCosmeticPanel");
        }

        if (FishermanCosmeticPanel == null)
        {
            FishermanCosmeticPanel = FindGameObjectByNames(
                root,
                "Fisherman Cosmetic",
                "FisherMan Cosmetic",
                "Fisherman Cosmetic Panel",
                "FishermanCosmeticPanel");
        }
    }

    private void ResolveCosmeticCategoryReferences()
    {
        Transform root = FishFishermanCosmeticPanel != null ? FishFishermanCosmeticPanel.transform : transform.root;

        if (FishFaceButton == null)
        {
            FishFaceButton = FindButtonByNames(root, "FishButton", "Fish Button");
        }

        if (FishermanFaceButton == null)
        {
            FishermanFaceButton = FindButtonByNames(root, "FishermanButton", "FisherMan Button", "Fisherman Button");
        }

        if (FishermanHatButton == null)
        {
            FishermanHatButton = FindButtonByNames(root, "hatButton", "HatButton", "Hat Button");
        }

        Transform fishermanItemsRoot = FishermanCosmeticPanel != null ? FishermanCosmeticPanel.transform : root;

        if (FishermanHatObject == null)
        {
            FishermanHatObject = FindGameObjectByNames(fishermanItemsRoot, "hat", "Hat");
        }

        if (FishermanHairObject == null)
        {
            FishermanHairObject = FindGameObjectByNames(fishermanItemsRoot, "hair", "Hair");
        }
    }

    private void ResolveCosmeticItemRoots()
    {
        if (FishCosmeticItemsRoot == null)
        {
            GameObject fishItemsRoot = FishCosmeticPanel != null
                ? FindGameObjectByNames(FishCosmeticPanel.transform, "fish Cosmetics", "Fish Cosmetics", "FishCosmetics")
                : FindGameObjectByNames(transform.root, "fish Cosmetics", "Fish Cosmetics", "FishCosmetics");
            FishCosmeticItemsRoot = fishItemsRoot != null ? fishItemsRoot.transform : null;
        }

        if (FishermanCosmeticItemsRoot == null)
        {
            GameObject fishermanItemsRoot = FishermanCosmeticPanel != null
                ? FindGameObjectByNames(FishermanCosmeticPanel.transform, "fisherman Cosmetics", "Fisherman Cosmetics", "FishermanCosmetics")
                : FindGameObjectByNames(transform.root, "fisherman Cosmetics", "Fisherman Cosmetics", "FishermanCosmetics");
            FishermanCosmeticItemsRoot = fishermanItemsRoot != null ? fishermanItemsRoot.transform : null;
        }
    }

    private void ResolvePreviewAssetFolderPaths()
    {
        if (string.IsNullOrEmpty(FishPreviewAssetFolder))
        {
            FishPreviewAssetFolder = "Assets/UI/Game UI/Fish preview";
        }

        if (string.IsNullOrEmpty(FishermanPreviewAssetFolder)
            || NormalizeSpriteName(FishermanPreviewAssetFolder) == NormalizeSpriteName("Assets/UI/Game UI"))
        {
            FishermanPreviewAssetFolder = "Assets/Resources/ShopUI/Fisherman Preview";
        }
    }

    private void RegisterCosmeticItemButtons()
    {
        RegisterCosmeticItemButtons(FishCosmeticItemsRoot, fishCosmeticItemButtons, fishCosmeticItemActions, false);
        RegisterCosmeticItemButtons(FishermanCosmeticItemsRoot, fishermanCosmeticItemButtons, fishermanCosmeticItemActions, true);

        ApplyItemOpacity(fishCosmeticItemButtons, null);
        ApplyItemOpacity(fishermanCosmeticItemButtons, null);
    }

    private void RegisterCosmeticItemButtons(Transform root, List<Button> buttons, List<UnityAction> actions, bool isFishermanCosmetic)
    {
        buttons.Clear();
        actions.Clear();

        if (root == null)
        {
            return;
        }

        Button[] foundButtons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < foundButtons.Length; i++)
        {
            Button button = foundButtons[i];
            if (button == null || IsCloseButton(button))
            {
                continue;
            }

            MakeButtonClickable(button);
            buttons.Add(button);
            UnityAction action = () => SelectCosmeticItem(buttons, button, isFishermanCosmetic);
            actions.Add(action);
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (!IsCosmeticItemImage(image) && !IsClearCosmeticItemImage(root, image))
            {
                continue;
            }

            Button button = image.GetComponent<Button>();
            if (button == null)
            {
                button = image.gameObject.AddComponent<Button>();
            }

            if (buttons.Contains(button))
            {
                continue;
            }

            MakeButtonClickable(button);
            buttons.Add(button);
            UnityAction action = () => SelectCosmeticItem(buttons, button, isFishermanCosmetic);
            actions.Add(action);
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }

    private void SelectCosmeticItem(List<Button> buttons, Button selectedButton, bool isFishermanCosmetic)
    {
        ApplyItemOpacity(buttons, selectedButton);

        if (!isFishermanCosmetic && IsClearFishCosmeticButton(selectedButton))
        {
            CosmeticRuntimeApplier.SelectFishHat(null);
            ClearFishHatFromDisplay();
            RefreshBottomRightPreview();
            return;
        }

        if (isFishermanCosmetic && IsClearFishermanCosmeticButton(selectedButton))
        {
            selectedFishermanDisplayMode = FishermanDisplayModeHat;
            SetDisplayControlLabel(selectedFishermanDisplayMode);
            CosmeticRuntimeApplier.SelectFishermanHat(null);
            ClearFishermanHatFromDisplay();
            RefreshBottomRightPreview();
            return;
        }

        Sprite selectedSprite = GetButtonSprite(selectedButton);
        if (selectedSprite == null)
        {
            return;
        }

        if (!isFishermanCosmetic)
        {
            selectedFishDisplayMode = FishDisplayModeHat;
            SetDisplayControlLabel(selectedFishDisplayMode);
            SetActiveIfNotNull(FishVoyageDiagram, false);
            CosmeticRuntimeApplier.SelectFishHat(selectedSprite);
            ApplySelectedFishHatToDisplay(selectedSprite);
            RefreshBottomRightPreview();
            return;
        }

        if (FishermanHairObject != null && selectedButton.transform.IsChildOf(FishermanHairObject.transform))
        {
            selectedFishermanDisplayMode = FishermanDisplayModeHair;
            SetDisplayControlLabel(selectedFishermanDisplayMode);
            CosmeticRuntimeApplier.SelectFishermanHair(selectedSprite);
            ApplySelectedFishermanHatToDisplay(selectedSprite);
            RefreshBottomRightPreview();
        }
        else
        {
            selectedFishermanDisplayMode = FishermanDisplayModeHat;
            SetDisplayControlLabel(selectedFishermanDisplayMode);
            CosmeticRuntimeApplier.SelectFishermanHat(selectedSprite);
            ApplySelectedFishermanHatToDisplay(selectedSprite);
            RefreshBottomRightPreview();
        }
    }

    private void ApplySavedFishHatToDisplay()
    {
        Sprite selectedSprite = CosmeticRuntimeApplier.GetSelectedFishHat();
        if (selectedSprite == null)
        {
            ClearFishHatFromDisplay();
            return;
        }

        ApplySelectedFishHatToDisplay(selectedSprite);
    }

    private void ApplySelectedFishHatToDisplay(Sprite selectedSprite)
    {
        if (selectedSprite == null)
        {
            return;
        }

        ResolveFishDisplayObjects();

        if (FishDisplayObjects != null && FishDisplayObjects.Length > 0)
        {
            for (int i = 0; i < FishDisplayObjects.Length; i++)
            {
                if (FishDisplayObjects[i] != null)
                {
                    ApplySelectedFishHatToDisplayObject(FishDisplayObjects[i], selectedSprite);
                }
            }

            return;
        }

        if (FishDisplayObject != null)
        {
            ApplySelectedFishHatToDisplayObject(FishDisplayObject, selectedSprite);
        }
    }

    private void ClearFishHatFromDisplay()
    {
        ResolveFishDisplayObjects();

        if (FishDisplayObjects != null && FishDisplayObjects.Length > 0)
        {
            for (int i = 0; i < FishDisplayObjects.Length; i++)
            {
                ClearFishHatFromDisplayObject(FishDisplayObjects[i]);
            }

            return;
        }

        ClearFishHatFromDisplayObject(FishDisplayObject);
    }

    private void ClearFishHatFromDisplayObject(GameObject fishDisplay)
    {
        if (fishDisplay == null)
        {
            return;
        }

        if (fishDisplay.GetComponent<SpriteRenderer>() != null)
        {
            CosmeticRuntimeApplier.RemoveFishHat(fishDisplay);
            return;
        }

        RectTransform fishRect = fishDisplay.GetComponent<RectTransform>();
        if (fishRect == null)
        {
            return;
        }

        Image fishImage = fishDisplay.GetComponent<Image>();
        RestoreDisplayBaseSprite(fishImage);

        Transform previewTransform = fishRect.Find(FishHatPreviewChildName);
        if (previewTransform == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(previewTransform.gameObject);
        }
        else
        {
            DestroyImmediate(previewTransform.gameObject);
        }
    }

    private void ApplySelectedFishHatToDisplayObject(GameObject fishDisplay, Sprite selectedSprite)
    {
        if (fishDisplay == null || selectedSprite == null)
        {
            return;
        }

        if (fishDisplay.GetComponent<SpriteRenderer>() != null)
        {
            CosmeticRuntimeApplier.ApplyFishHatByName(fishDisplay, selectedSprite.name);
            return;
        }

        RectTransform fishRect = fishDisplay.GetComponent<RectTransform>();
        Image fishImage = fishDisplay.GetComponent<Image>();
        if (fishRect == null || fishImage == null)
        {
            return;
        }

        CacheDisplayBaseSprite(fishImage);

        if (UseDynamicHatPreviewSprites && TryApplyCompositePreviewSprite(fishImage, selectedSprite, GetFishDisplayIndex(fishDisplay), false))
        {
            ClearFishHatPreviewChild(fishRect);
            return;
        }

        Transform previewTransform = fishRect.Find(FishHatPreviewChildName);
        if (previewTransform == null)
        {
            GameObject previewObject = new GameObject(FishHatPreviewChildName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            previewTransform = previewObject.transform;
            previewTransform.SetParent(fishRect, false);
        }

        RectTransform previewRect = previewTransform as RectTransform;
        Image previewImage = previewTransform.GetComponent<Image>();
        previewImage.sprite = selectedSprite;
        previewImage.raycastTarget = false;
        previewImage.preserveAspect = true;

        previewRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewRect.pivot = new Vector2(0.5f, 0.5f);
        previewRect.anchoredPosition = GetFishHatPreviewPosition(selectedSprite);
        previewRect.localEulerAngles = new Vector3(0f, 0f, GetFishHatPreviewRotation(selectedSprite));
        previewRect.sizeDelta = GetFishHatPreviewSize(selectedSprite);
        previewRect.SetAsLastSibling();
    }

    private void ClearFishHatPreviewChild(RectTransform fishRect)
    {
        Transform previewTransform = fishRect != null ? fishRect.Find(FishHatPreviewChildName) : null;
        if (previewTransform == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(previewTransform.gameObject);
        }
        else
        {
            DestroyImmediate(previewTransform.gameObject);
        }
    }

    private Sprite GetCompositePreviewSprite(Sprite selectedSprite, int fishDisplayIndex, bool isFisherman)
    {
        if (selectedSprite == null)
        {
            return null;
        }

        BuildPreviewMapsIfNeeded();

        string cosmeticKey = NormalizeSpriteName(selectedSprite);
        Sprite previewSprite = ResolvePreviewSpriteFromRules(
            isFisherman ? FishermanPreviewRules : FishPreviewRules,
            cosmeticKey);

        if (previewSprite == null)
        {
            if (isFisherman)
            {
                fishermanPreviewSpritesByCosmetic.TryGetValue(cosmeticKey, out previewSprite);
                if (previewSprite == null)
                {
                    previewSprite = ResolveFishermanHatPreviewSprite(selectedSprite);
                }
            }
            else
            {
                int index = Mathf.Clamp(fishDisplayIndex, 0, fishPreviewSpritesByCosmetic.Length - 1);
                Dictionary<string, Sprite> fishMap = fishPreviewSpritesByCosmetic[index];
                if (fishMap != null)
                {
                    fishMap.TryGetValue(cosmeticKey, out previewSprite);
                }
            }
        }

        return previewSprite;
    }

    private bool TryApplyCompositePreviewSprite(Image targetImage, Sprite selectedSprite, int fishDisplayIndex, bool isFisherman)
    {
        if (targetImage == null || selectedSprite == null)
        {
            return false;
        }

        Sprite previewSprite = GetCompositePreviewSprite(selectedSprite, fishDisplayIndex, isFisherman);
        if (previewSprite == null)
        {
            return false;
        }

        targetImage.sprite = previewSprite;
        targetImage.preserveAspect = true;
        return true;
    }

    private Sprite ResolveFishermanHatPreviewSprite(Sprite cosmeticSprite)
    {
        if (cosmeticSprite == null)
        {
            return null;
        }

        string hatKey = GetFishermanCosmeticHatKey(cosmeticSprite.name);
        if (hatKey == "headphone")
        {
            Sprite headphonePreview = Resources.Load<Sprite>(FishermanHeadphonePreviewResourcePath);
            if (headphonePreview != null)
            {
                RegisterPreviewSprite(headphonePreview);
                return headphonePreview;
            }
        }

        Dictionary<string, Sprite> previewSpritesByHat = GetFishermanPreviewSpritesByHat();
        return ResolveFishermanPreviewSprite(cosmeticSprite, previewSpritesByHat);
    }

    private Sprite ResolvePreviewSpriteFromRules(List<CosmeticPreviewRule> rules, string cosmeticKey)
    {
        if (rules == null || string.IsNullOrEmpty(cosmeticKey))
        {
            return null;
        }

        for (int i = 0; i < rules.Count; i++)
        {
            CosmeticPreviewRule rule = rules[i];
            if (rule == null || NormalizeSpriteName(rule.CosmeticName) != cosmeticKey)
            {
                continue;
            }

            Sprite sprite = GetPreviewSpriteByName(rule.PreviewSpriteName);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    private Sprite GetPreviewSpriteByName(string spriteName)
    {
        BuildPreviewMapsIfNeeded();
        previewSpritesByName.TryGetValue(NormalizeSpriteName(spriteName), out Sprite sprite);
        return sprite;
    }

    private void ApplySavedFishermanHatToDisplay()
    {
        Sprite selectedHat = CosmeticRuntimeApplier.GetSelectedFishermanHat();
        Sprite selectedHair = CosmeticRuntimeApplier.GetSelectedFishermanHair();
        if (selectedHair != null)
        {
            ApplySelectedFishermanHatToDisplay(selectedHair);
            return;
        }

        if (selectedHat == null)
        {
            ClearFishermanHatFromDisplay();
            return;
        }

        ApplySelectedFishermanHatToDisplay(selectedHat);
    }

    private void ApplySelectedFishermanHatToDisplay(Sprite selectedSprite)
    {
        if (selectedSprite == null || FishermanDisplayObject == null)
        {
            return;
        }

        if (FishermanDisplayObject.GetComponent<SpriteRenderer>() != null)
        {
            CosmeticRuntimeApplier.ApplyFishermanCosmeticsByName(FishermanDisplayObject, selectedSprite.name, null);
            return;
        }

        Image fishermanImage = FishermanDisplayObject.GetComponent<Image>();
        if (fishermanImage == null)
        {
            return;
        }

        CacheDisplayBaseSprite(fishermanImage);

        bool isHairSelection = FishermanHairObject != null
            && selectedFishermanDisplayMode == FishermanDisplayModeHair;

        if (isHairSelection)
        {
            if (!TryApplyCompositePreviewSprite(fishermanImage, selectedSprite, 0, true))
            {
                ApplyFishermanRedHairPreview(fishermanImage);
            }

            return;
        }

        Sprite previewSprite = GetCompositePreviewSprite(selectedSprite, 0, true);
        if (previewSprite != null)
        {
            fishermanImage.sprite = previewSprite;
            fishermanImage.preserveAspect = true;
        }
        else if (GetFishermanCosmeticHatKey(selectedSprite.name) == "yellow")
        {
            RestoreDisplayBaseSprite(fishermanImage);
        }
        else
        {
            RestoreDisplayBaseSprite(fishermanImage);
        }
    }

    private void ClearFishermanHatFromDisplay()
    {
        if (FishermanDisplayObject == null)
        {
            return;
        }

        Image fishermanImage = FishermanDisplayObject.GetComponent<Image>();
        if (!ApplyFishermanRedHairPreview(fishermanImage))
        {
            RestoreDisplayBaseSprite(fishermanImage);
        }
    }

    private bool ApplyFishermanRedHairPreview(Image fishermanImage)
    {
        if (fishermanImage == null)
        {
            return false;
        }

        Sprite redHairSprite = GetPreviewSpriteByName("Fisherman Red hair");
        if (redHairSprite == null)
        {
            redHairSprite = GetPreviewSpriteByName("fisherman_red_hair");
        }

        if (redHairSprite == null)
        {
            return false;
        }

        fishermanImage.sprite = redHairSprite;
        fishermanImage.preserveAspect = true;
        return true;
    }

    private void BuildPreviewMapsIfNeeded()
    {
        if (previewMapsBuilt)
        {
            return;
        }

        previewMapsBuilt = true;
        fishPreviewSpritesByCosmetic[0] = BuildFishPreviewMap(false);
        fishPreviewSpritesByCosmetic[1] = BuildFishPreviewMap(true);
        BuildFishermanPreviewMap();
    }

    private Dictionary<string, Sprite> BuildFishPreviewMap(bool useTroutPreview)
    {
        Dictionary<string, Sprite> map = new Dictionary<string, Sprite>();
        List<Button> cosmeticButtons = GetOrderedCosmeticButtons(fishCosmeticItemButtons, FishCosmeticItemsRoot, null);
        Dictionary<string, Sprite> previewSpritesByHat = GetFishPreviewSpritesByHat(useTroutPreview);

        for (int i = 0; i < cosmeticButtons.Count; i++)
        {
            Sprite cosmeticSprite = GetButtonSprite(cosmeticButtons[i]);
            if (cosmeticSprite == null)
            {
                continue;
            }

            Sprite previewSprite = ResolveFishPreviewSprite(cosmeticSprite, previewSpritesByHat);
            if (previewSprite != null)
            {
                map[NormalizeSpriteName(cosmeticSprite)] = previewSprite;
            }
        }

        return map;
    }

    private void BuildFishermanPreviewMap()
    {
        fishermanPreviewSpritesByCosmetic.Clear();
        List<Button> cosmeticButtons = GetOrderedCosmeticButtons(fishermanCosmeticItemButtons, FishermanCosmeticItemsRoot, FishermanHatObject);
        List<Button> hairButtons = GetOrderedCosmeticButtons(fishermanCosmeticItemButtons, FishermanCosmeticItemsRoot, FishermanHairObject);
        Dictionary<string, Sprite> previewSpritesByHat = GetFishermanPreviewSpritesByHat();

        AddFishermanPreviewMappings(cosmeticButtons, previewSpritesByHat);
        AddFishermanPreviewMappings(hairButtons, previewSpritesByHat);
    }

    private void AddFishermanPreviewMappings(List<Button> cosmeticButtons, Dictionary<string, Sprite> previewSpritesByHat)
    {
        if (cosmeticButtons == null || previewSpritesByHat == null)
        {
            return;
        }

        for (int i = 0; i < cosmeticButtons.Count; i++)
        {
            Sprite cosmeticSprite = GetButtonSprite(cosmeticButtons[i]);
            if (cosmeticSprite == null)
            {
                continue;
            }

            Sprite previewSprite = ResolveFishermanPreviewSprite(cosmeticSprite, previewSpritesByHat);
            if (previewSprite != null)
            {
                fishermanPreviewSpritesByCosmetic[NormalizeSpriteName(cosmeticSprite)] = previewSprite;
            }
        }
    }

    private List<Button> GetOrderedCosmeticButtons(List<Button> sourceButtons, Transform itemRoot, GameObject requiredParent)
    {
        List<Button> orderedButtons = new List<Button>();
        if (sourceButtons == null)
        {
            return orderedButtons;
        }

        for (int i = 0; i < sourceButtons.Count; i++)
        {
            Button button = sourceButtons[i];
            if (button == null || IsClearFishCosmeticButton(button))
            {
                continue;
            }

            if (requiredParent != null && !button.transform.IsChildOf(requiredParent.transform))
            {
                continue;
            }

            if (itemRoot != null && !button.transform.IsChildOf(itemRoot))
            {
                continue;
            }

            if (GetButtonSprite(button) != null)
            {
                orderedButtons.Add(button);
            }
        }

        orderedButtons.Sort((a, b) => GetHierarchySortKey(a.transform).CompareTo(GetHierarchySortKey(b.transform)));
        return orderedButtons;
    }

    private static string GetHierarchySortKey(Transform transform)
    {
        if (transform == null)
        {
            return string.Empty;
        }

        List<int> indices = new List<int>();
        Transform current = transform;
        while (current != null)
        {
            indices.Add(current.GetSiblingIndex());
            current = current.parent;
        }

        indices.Reverse();
        return string.Join(".", indices.ConvertAll(index => index.ToString("D4")).ToArray());
    }

    private Dictionary<string, Sprite> GetFishPreviewSpritesByHat(bool useTroutPreview)
    {
        Dictionary<string, Sprite> spritesByHat = new Dictionary<string, Sprite>();
        List<Sprite> sprites = LoadPreviewSpritesFromFolder(FishPreviewAssetFolder, useTroutPreview ? "Trout" : "Fish");
        for (int i = 0; i < sprites.Count; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite == null)
            {
                continue;
            }

            RegisterPreviewSprite(sprite);
            string hatKey = GetFishPreviewHatKey(sprite.name);
            if (!string.IsNullOrEmpty(hatKey) && !spritesByHat.ContainsKey(hatKey))
            {
                spritesByHat.Add(hatKey, sprite);
            }
        }

        return spritesByHat;
    }

    private Sprite ResolveFishPreviewSprite(Sprite cosmeticSprite, Dictionary<string, Sprite> previewSpritesByHat)
    {
        if (cosmeticSprite == null || previewSpritesByHat == null)
        {
            return null;
        }

        string hatKey = GetFishCosmeticHatKey(cosmeticSprite.name);
        if (!string.IsNullOrEmpty(hatKey) && previewSpritesByHat.TryGetValue(hatKey, out Sprite previewSprite))
        {
            return previewSprite;
        }

        return null;
    }

    private Dictionary<string, Sprite> GetFishermanPreviewSpritesByHat()
    {
        Dictionary<string, Sprite> spritesByHat = new Dictionary<string, Sprite>();
        List<Sprite> sprites = LoadPreviewSpritesFromFolder(FishermanPreviewAssetFolder, string.Empty);
        for (int i = 0; i < sprites.Count; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite == null)
            {
                continue;
            }

            RegisterPreviewSprite(sprite);
            string hatKey = GetFishermanPreviewHatKey(sprite.name);
            if (!string.IsNullOrEmpty(hatKey) && !spritesByHat.ContainsKey(hatKey))
            {
                spritesByHat.Add(hatKey, sprite);
            }
        }

        return spritesByHat;
    }

    private Sprite ResolveFishermanPreviewSprite(Sprite cosmeticSprite, Dictionary<string, Sprite> previewSpritesByHat)
    {
        if (cosmeticSprite == null || previewSpritesByHat == null)
        {
            return null;
        }

        string hatKey = GetFishermanCosmeticHatKey(cosmeticSprite.name);
        if (!string.IsNullOrEmpty(hatKey) && previewSpritesByHat.TryGetValue(hatKey, out Sprite previewSprite))
        {
            return previewSprite;
        }

        return null;
    }

    private List<Sprite> FindScenePreviewSprites(string prefix)
    {
        List<Sprite> sprites = new List<Sprite>();
        string normalizedPrefix = NormalizeSpriteName(prefix);
        Image[] images = transform.root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            Sprite sprite = image != null ? image.sprite : null;
            if (sprite == null)
            {
                continue;
            }

            string imageName = NormalizeSpriteName(image.name);
            string spriteName = NormalizeSpriteName(sprite);
            if (IsNumberedPreviewName(imageName, normalizedPrefix) || IsNumberedPreviewName(spriteName, normalizedPrefix))
            {
                sprites.Add(sprite);
            }
        }

        return sprites;
    }

    private List<Sprite> LoadPreviewSpritesFromAsset(string assetPath)
    {
        List<Sprite> sprites = new List<Sprite>();
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(assetPath))
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    sprites.Add(sprite);
                    RegisterPreviewSprite(sprite);
                }
            }
        }
#endif
        return sprites;
    }

    private List<Sprite> LoadPreviewSpritesFromFolder(string folderPath, string filePrefix)
    {
        List<Sprite> sprites = new List<Sprite>();
        
        string resourcePath = "";
        if (!string.IsNullOrEmpty(folderPath) && folderPath.ToLowerInvariant().Contains("fisherman"))
        {
            resourcePath = "ShopUI/Fisherman Preview";
        }
        else
        {
            resourcePath = "ShopUI/Fish preview";
        }

        Sprite[] loadedSprites = Resources.LoadAll<Sprite>(resourcePath);
        if (loadedSprites != null)
        {
            string normalizedPrefix = NormalizeSpriteName(filePrefix);
            for (int i = 0; i < loadedSprites.Length; i++)
            {
                Sprite sprite = loadedSprites[i];
                if (sprite == null) continue;

                if (string.IsNullOrEmpty(normalizedPrefix) || NormalizeSpriteName(sprite.name).StartsWith(normalizedPrefix))
                {
                    sprites.Add(sprite);
                }
            }
        }
        
        return sprites;
    }


    private static string GetFishCosmeticHatKey(string cosmeticName)
    {
        string name = NormalizeSpriteName(cosmeticName);
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        if (name.Contains("paperboat") || name.Contains("boat"))
        {
            return "boat";
        }

        if (name.Contains("cap"))
        {
            return "cap";
        }

        if (name.Contains("beret") || name.Contains("polish"))
        {
            return "polish";
        }

        if (name.Contains("default") || name.Contains("fishing"))
        {
            return "yellow";
        }

        if (name.Contains("hat2") || name.Contains("black") || name.Contains("top"))
        {
            return "black";
        }

        if (name == "hat" || name.Contains("orange"))
        {
            return "orange";
        }

        return string.Empty;
    }

    private static string GetFishPreviewHatKey(string previewName)
    {
        string name = NormalizeSpriteName(previewName);
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        if (name.Contains("boat"))
        {
            return "boat";
        }

        if (name.Contains("cap"))
        {
            return "cap";
        }

        if (name.Contains("polish"))
        {
            return "polish";
        }

        if (name.Contains("yellow"))
        {
            return "yellow";
        }

        if (name.Contains("black"))
        {
            return "black";
        }

        if (name.Contains("orange"))
        {
            return "orange";
        }

        return string.Empty;
    }

    private static string GetFishermanCosmeticHatKey(string cosmeticName)
    {
        string name = NormalizeSpriteName(cosmeticName);
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        if (name.Contains("redhair"))
        {
            return "redhair";
        }

        if (name.Contains("bluecap"))
        {
            return "bluecap";
        }

        if (name.Contains("chef") || name.Contains("white") || name.Contains("soda"))
        {
            return "white";
        }

        if (name.Contains("fishhat") || name.Contains("frog"))
        {
            return "frog";
        }

        if (name.Contains("turtle"))
        {
            return "turtle";
        }

        if (name.Contains("ranger"))
        {
            return "green";
        }

        if (name.Contains("redcap"))
        {
            return "red";
        }

        if (name.Contains("headphone") || name.Contains("headphones"))
        {
            return "headphone";
        }

        if (name.Contains("default") || name.Contains("fishing"))
        {
            return "yellow";
        }

        return string.Empty;
    }

    private static string GetFishermanPreviewHatKey(string previewName)
    {
        string name = NormalizeSpriteName(previewName);
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        if (name.Contains("redhair"))
        {
            return "redhair";
        }

        if (name.Contains("bluecap"))
        {
            return "bluecap";
        }

        if (name.Contains("white") || name.Contains("soda"))
        {
            return "white";
        }

        if (name.Contains("griin"))
        {
            return "frog";
        }

        if (name.Contains("turtle"))
        {
            return "turtle";
        }

        if (name.Contains("green"))
        {
            return "green";
        }

        if (name.Contains("headphone") || name.Contains("headphones") || name.Contains("fishermnaheadphone"))
        {
            return "headphone";
        }

        if (name.Contains("redhat"))
        {
            return "red";
        }

        if (name.Contains("yellow"))
        {
            return "yellow";
        }

        return string.Empty;
    }

    private void RegisterPreviewSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        string key = NormalizeSpriteName(sprite);
        if (!previewSpritesByName.ContainsKey(key))
        {
            previewSpritesByName.Add(key, sprite);
        }
    }

    private static bool IsCharacterBasePreviewSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return true;
        }

        return sprite.rect.height > sprite.rect.width * 1.5f;
    }

    private static bool IsNumberedPreviewName(string normalizedName, string normalizedPrefix)
    {
        if (string.IsNullOrEmpty(normalizedName) || string.IsNullOrEmpty(normalizedPrefix))
        {
            return false;
        }

        if (!normalizedName.StartsWith(normalizedPrefix) || normalizedName.Length <= normalizedPrefix.Length)
        {
            return false;
        }

        char next = normalizedName[normalizedPrefix.Length];
        return next >= '0' && next <= '9';
    }

    private void CacheDisplayBaseSprite(Image image)
    {
        if (image != null && !displayBaseSprites.ContainsKey(image))
        {
            displayBaseSprites.Add(image, image.sprite);
        }
    }

    private void RestoreDisplayBaseSprite(Image image)
    {
        if (image == null)
        {
            return;
        }

        if (displayBaseSprites.TryGetValue(image, out Sprite baseSprite))
        {
            image.sprite = baseSprite;
            image.preserveAspect = true;
        }
    }

    private static Vector2 GetFishHatPreviewPosition(Sprite selectedSprite)
    {
        string name = NormalizeSpriteName(selectedSprite);
        switch (name)
        {
            case "beret": return new Vector2(-16f, 29f);
            case "fishermanhatdefaultfishinghat": return new Vector2(-8f, 29f);
            case "hat2": return new Vector2(-18f, 30f);
            case "cap": return new Vector2(-18f, 25f);
            case "paperboat": return new Vector2(-7f, 31f);
            default: return new Vector2(0f, 29f);
        }
    }

    private static Vector2 GetFishHatPreviewSize(Sprite selectedSprite)
    {
        string name = NormalizeSpriteName(selectedSprite);
        switch (name)
        {
            case "beret": return new Vector2(58f, 36f);
            case "fishermanhatdefaultfishinghat": return new Vector2(58f, 42f);
            case "paperboat": return new Vector2(52f, 34f);
            default: return new Vector2(54f, 38f);
        }
    }

    private static float GetFishHatPreviewRotation(Sprite selectedSprite)
    {
        string name = NormalizeSpriteName(selectedSprite);
        switch (name)
        {
            case "beret": return -8f;
            case "cap": return -15f;
            case "paperboat": return -15f;
            case "hat2": return 5f;
            default: return 0f;
        }
    }

    private static Sprite GetButtonSprite(Button button)
    {
        if (button == null)
        {
            return null;
        }

        Image[] images = button.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image.sprite == null || IsCellBackgroundImage(image, button))
            {
                continue;
            }

            return image.sprite;
        }

        Image directImage = button.GetComponent<Image>();
        return directImage != null && directImage.sprite != null && !IsCellBackgroundImage(directImage, button)
            ? directImage.sprite
            : null;
    }

    private static bool IsCellBackgroundImage(Image image, Button ownerButton)
    {
        if (image == null || image.sprite == null)
        {
            return true;
        }

        string imageName = NormalizeSpriteName(image.name);
        string spriteName = NormalizeSpriteName(image.sprite);
        if (spriteName.Contains("boxselected")
            || spriteName.Contains("boxunselected")
            || spriteName.Contains("background")
            || spriteName.Contains("button")
            || spriteName == "uisprite")
        {
            return true;
        }

        if (image.transform == ownerButton.transform)
        {
            return imageName.Contains("cell") || imageName.Contains("slot") || imageName.Contains("box");
        }

        return false;
    }

    private bool IsFishDisplayVisible()
    {
        if (!isFishSelected)
        {
            return false;
        }

        ResolveFishDisplayObjects();

        if (FishDisplayObjects != null && FishDisplayObjects.Length > 0)
        {
            for (int i = 0; i < FishDisplayObjects.Length; i++)
            {
                if (FishDisplayObjects[i] != null && FishDisplayObjects[i].activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        return FishDisplayObject != null && FishDisplayObject.activeInHierarchy;
    }

    private bool IsFishermanDisplayVisible()
    {
        return isFishermanSelected
            && FishermanDisplayObject != null
            && FishermanDisplayObject.activeInHierarchy;
    }

    private int GetFishDisplayIndex(GameObject fishDisplay)
    {
        ResolveFishDisplayObjects();

        if (FishDisplayObjects == null || fishDisplay == null)
        {
            return 0;
        }

        for (int i = 0; i < FishDisplayObjects.Length; i++)
        {
            if (FishDisplayObjects[i] == fishDisplay)
            {
                return i;
            }
        }

        return 0;
    }

    private bool IsClearFishCosmeticButton(Button button)
    {
        if (button == null)
        {
            return false;
        }

        string buttonName = button.name.ToLowerInvariant();
        if (buttonName.Contains("clear") || buttonName.Contains("none") || buttonName.Contains("empty") || buttonName.Contains("x icon"))
        {
            return true;
        }

        return FishCosmeticItemsRoot != null
            && button.transform.parent == FishCosmeticItemsRoot
            && button.transform.GetSiblingIndex() == 0;
    }

    private bool IsClearFishermanCosmeticButton(Button button)
    {
        if (button == null)
        {
            return false;
        }

        string buttonName = button.name.ToLowerInvariant();
        if (buttonName.Contains("clear") || buttonName.Contains("none") || buttonName.Contains("empty") || buttonName.Contains("x icon"))
        {
            return true;
        }

        if (FishermanHatObject != null
            && button.transform.parent == FishermanHatObject.transform
            && button.transform.GetSiblingIndex() == 0)
        {
            return true;
        }

        return FishermanCosmeticItemsRoot != null
            && button.transform.parent == FishermanCosmeticItemsRoot
            && button.transform.GetSiblingIndex() == 0;
    }

    private static void RemoveCosmeticItemButtonListeners(List<Button> buttons, List<UnityAction> actions)
    {
        int count = Mathf.Min(buttons.Count, actions.Count);
        for (int i = 0; i < count; i++)
        {
            if (buttons[i] != null && actions[i] != null)
            {
                buttons[i].onClick.RemoveListener(actions[i]);
            }
        }
    }

    private void ApplyItemOpacity(List<Button> buttons, Button selectedButton)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            float alpha = selectedButton != null && button == selectedButton ? SelectedItemOpacity : UnselectedItemOpacity;
            SetGraphicAlpha(button.gameObject, alpha);
            SetSelectedCellOutline(button, selectedButton != null && button == selectedButton);
        }
    }

    private void SetSelectedCellOutline(Button button, bool selected)
    {
        if (button == null || BoxSelectedSprite == null || BoxUnselectedSprite == null)
        {
            return;
        }

        Image targetImage = FindSelectionOutlineGraphic(button) as Image;
        if (targetImage != null)
        {
            Sprite previousSprite = targetImage.sprite;
            Sprite newSprite = selected ? BoxSelectedSprite : BoxUnselectedSprite;

            if (previousSprite != newSprite)
            {
                targetImage.sprite = newSprite;
                targetImage.preserveAspect = false;

                string targetName = targetImage.gameObject.name;
                string newSpriteName = newSprite.name;

                if (selected)
                {
                    Debug.Log($"[ShopManager] Selected cosmetic: '{button.name}'. Changed element image '{targetName}' to '{newSpriteName}' (BoxSelected).");
                }
                else
                {
                    Debug.Log($"[ShopManager] Unselected cosmetic: '{button.name}'. Reassigned element image '{targetName}' to '{newSpriteName}' (BoxUnselected).");
                }
            }
        }
    }

    private static Graphic FindSelectionOutlineGraphic(Button button)
    {
        if (button == null)
        {
            return null;
        }

        Transform current = button.transform;
        for (int i = 0; current != null && i < 4; i++)
        {
            Image image = current.GetComponent<Image>();
            if (image != null && (IsCellBackgroundImage(image, button) || IsNamedCellContainer(current)))
            {
                return image;
            }

            current = current.parent;
        }

        return button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Graphic>();
    }

    private static bool IsNamedCellContainer(Transform transform)
    {
        if (transform == null)
        {
            return false;
        }

        string name = NormalizeSpriteName(transform.name);
        return name.Contains("cell") || name.Contains("slot") || name.Contains("box");
    }

    private static void SetGraphicAlpha(GameObject root, float alpha)
    {
        if (root == null)
        {
            return;
        }

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null)
            {
                continue;
            }

            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }

    private static bool IsCloseButton(Button button)
    {
        string name = button.name.ToLowerInvariant();
        return name.Contains("close") || name == "x" || name.Contains("back");
    }

    private static bool IsCosmeticItemImage(Image image)
    {
        if (image == null || image.sprite == null)
        {
            return false;
        }

        string name = image.name.ToLowerInvariant();
        if (name.Contains("close") || name == "x" || name.Contains("button") || name.Contains("background"))
        {
            return false;
        }

        return name.Contains("cosmeteic") || name.Contains("cosmetic") || name.Contains("hat") || name.Contains("hair");
    }

    private static bool IsClearCosmeticItemImage(Transform root, Image image)
    {
        if (root == null || image == null)
        {
            return false;
        }

        string name = image.name.ToLowerInvariant();
        if (name.Contains("clear") || name.Contains("none") || name.Contains("empty") || name.Contains("x icon"))
        {
            return true;
        }

        return image.transform.parent == root && image.transform.GetSiblingIndex() == 0;
    }

    private static string NormalizeSpriteName(Sprite sprite)
    {
        if (sprite == null)
        {
            return string.Empty;
        }

        string name = sprite.name.ToLowerInvariant();
        if (name.EndsWith("_0"))
        {
            name = name.Substring(0, name.Length - 2);
        }

        return name
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);
    }

    private static string NormalizeSpriteName(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
        {
            return string.Empty;
        }

        string name = spriteName.ToLowerInvariant();
        if (name.EndsWith("_0"))
        {
            name = name.Substring(0, name.Length - 2);
        }

        return name
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);
    }

    private static int GetSpriteNumericSuffix(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
        {
            return 0;
        }

        int value = 0;
        int multiplier = 1;
        bool foundDigit = false;

        for (int i = spriteName.Length - 1; i >= 0; i--)
        {
            char c = spriteName[i];
            if (c < '0' || c > '9')
            {
                break;
            }

            foundDigit = true;
            value += (c - '0') * multiplier;
            multiplier *= 10;
        }

        return foundDigit ? value : 0;
    }

    private static Image FindAnimatedShopImage(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image != null && image.sprite != null && image.sprite.name.StartsWith("Fishingshop2"))
            {
                return image;
            }
        }

        return images.Length > 0 ? images[0] : null;
    }

    private static void MakeButtonClickable(Button button)
    {
        button.interactable = true;

        Graphic targetGraphic = button.targetGraphic;
        if (targetGraphic == null)
        {
            targetGraphic = button.GetComponent<Graphic>();
            button.targetGraphic = targetGraphic;
        }

        if (targetGraphic != null)
        {
            targetGraphic.raycastTarget = true;
        }

        Graphic[] graphics = button.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
            {
                graphics[i].raycastTarget = true;
            }
        }
    }

    private static GameObject FindGameObjectByNames(Transform root, params string[] names)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < names.Length; i++)
        {
            Transform match = FindChildByName(root, names[i]);
            if (match != null)
            {
                return match.gameObject;
            }
        }

        return null;
    }

    private static Button FindButtonByNames(Transform root, params string[] names)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < names.Length; i++)
        {
            Transform match = FindChildByName(root, names[i]);
            if (match == null)
            {
                continue;
            }

            Button button = match.GetComponent<Button>();
            if (button != null)
            {
                return button;
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindChildByName(root.GetChild(i), childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(action);
        }
    }

    private static void SetActiveIfNotNull(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    public bool CycleActiveDisplaySelection(int direction, LocalPlayManager localPlay)
    {
        if (!IsShopOpen())
        {
            return false;
        }

        LocalPlayManager playManager = localPlay != null ? localPlay : shopLocalPlayManager;

        if (IsFishDisplayVisible() && IsFishSpeciesModeSelected())
        {
            CycleFishSpecies(direction, playManager);
            return true;
        }

        if (IsFishDisplayVisible() && IsFishHatModeSelected())
        {
            CycleFishHat(direction, playManager);
            return true;
        }

        if (IsFishermanDisplayVisible() && IsFishermanHatModeSelected())
        {
            CycleFishermanHat(direction);
            return true;
        }

        if (IsFishermanDisplayVisible())
        {
            CycleFishermanHair(direction);
            return true;
        }

        return false;
    }

    public bool IsShopOpen()
    {
        if (ShopItemPanel != null && ShopItemPanel.activeSelf)
        {
            return true;
        }

        if (FishFishermanCosmeticPanel != null && FishFishermanCosmeticPanel.activeSelf)
        {
            return true;
        }

        return ShopPreviewRoot != null && ShopPreviewRoot.activeSelf;
    }

    public bool IsCyclingSpecies()
    {
        if (ShopItemPanel != null && ShopItemPanel.activeSelf)
        {
            if (IsFishDisplayVisible() && IsFishSpeciesModeSelected())
            {
                return true;
            }
        }
        return false;
    }

    private List<Button> GetActiveCosmeticButtons(bool isFisherman)
    {
        List<Button> result = new List<Button>();
        List<Button> source = isFisherman ? fishermanCosmeticItemButtons : fishCosmeticItemButtons;
        
        if (!isFisherman)
        {
            for (int i = 0; i < source.Count; i++)
            {
                Button btn = source[i];
                if (btn != null)
                {
                    bool isClear = IsClearFishCosmeticButton(btn);
                    bool hasSprite = GetButtonSprite(btn) != null;
                    if (isClear || hasSprite)
                    {
                        result.Add(btn);
                    }
                }
            }
        }
        else
        {
            GameObject requiredParent = (selectedFishermanDisplayMode == FishermanDisplayModeHat)
                ? FishermanHatObject
                : FishermanHairObject;
            
            for (int i = 0; i < source.Count; i++)
            {
                Button btn = source[i];
                if (btn != null)
                {
                    if (requiredParent == null || btn.transform.IsChildOf(requiredParent.transform))
                    {
                        bool isClear = IsClearFishermanCosmeticButton(btn);
                        bool hasSprite = GetButtonSprite(btn) != null;
                        if (isClear || hasSprite)
                        {
                            result.Add(btn);
                        }
                    }
                }
            }
        }
        
        // Sort the buttons based on their hierarchy order so they cycle in the exact visual grid order!
        result.Sort((a, b) => GetHierarchySortKey(a.transform).CompareTo(GetHierarchySortKey(b.transform)));
        return result;
    }

    private int GetSelectedButtonIndex(List<Button> activeButtons, bool isFisherman)
    {
        Sprite selectedSprite = null;
        if (!isFisherman)
        {
            selectedSprite = CosmeticRuntimeApplier.GetSelectedFishHat();
        }
        else
        {
            selectedSprite = (selectedFishermanDisplayMode == FishermanDisplayModeHat)
                ? CosmeticRuntimeApplier.GetSelectedFishermanHat()
                : CosmeticRuntimeApplier.GetSelectedFishermanHair();
        }

        for (int i = 0; i < activeButtons.Count; i++)
        {
            Button btn = activeButtons[i];
            if (selectedSprite == null)
            {
                if (isFisherman ? IsClearFishermanCosmeticButton(btn) : IsClearFishCosmeticButton(btn))
                {
                    return i;
                }
            }
            else
            {
                if (GetButtonSprite(btn) == selectedSprite)
                {
                    return i;
                }
            }
        }
        return 0;
    }

    public void CycleFishHat(int direction, LocalPlayManager localPlay)
    {
        List<Button> activeButtons = GetActiveCosmeticButtons(false);
        if (activeButtons.Count == 0)
        {
            return;
        }

        int currentIndex = GetSelectedButtonIndex(activeButtons, false);
        int nextIndex = (currentIndex + direction + activeButtons.Count) % activeButtons.Count;

        Button btn = activeButtons[nextIndex];
        SelectCosmeticItem(fishCosmeticItemButtons, btn, false);
    }

    public void CycleFishSpecies(int direction, LocalPlayManager localPlay)
    {
        EnsureShopPreviewRootActive();
        ResolveFishDisplayObjects();

        if (FishDisplayObjects == null || FishDisplayObjects.Length == 0)
        {
            return;
        }

        int currentFish = Mathf.Clamp(PlayerPrefs.GetInt(LocalPlayManager.SelectedFishPrefKey, 0), 0, FishDisplayObjects.Length - 1);
        int fishCount = FishDisplayObjects.Length;
        for (int step = 0; step < fishCount; step++)
        {
            int candidate = (currentFish + direction + fishCount) % fishCount;
            if (IsFishSpeciesUnlocked(candidate, localPlay))
            {
                if (candidate == currentFish)
                {
                    break;
                }

                currentFish = candidate;
                PlayerPrefs.SetInt(LocalPlayManager.SelectedFishPrefKey, currentFish);
                PlayerPrefs.Save();
                RefreshSelectedFishDisplay();
                SyncLocalPlayFishSelection(currentFish, localPlay);
                return;
            }

            currentFish = candidate;
        }
    }

    private static bool IsFishSpeciesUnlocked(int fishIndex, LocalPlayManager localPlay)
    {
        if (fishIndex <= 0)
        {
            return true;
        }

        if (fishIndex == 1)
        {
            return PlayerPrefs.GetInt(LocalPlayManager.TroutUnlockedPrefKey, 0) == 1;
        }

        return false;
    }

    private void SyncLocalPlayFishSelection(int fishIndex, LocalPlayManager localPlay)
    {
        if (localPlay == null)
        {
            return;
        }

        localPlay.ApplyFishSelectionFromShop(fishIndex);
    }

    public void CycleFishermanHat(int direction)
    {
        EnsureShopPreviewRootActive();

        if (TryCycleFishermanHatPreviewSprites(direction))
        {
            return;
        }

        List<Button> activeButtons = GetActiveCosmeticButtons(true);
        if (activeButtons.Count == 0)
        {
            return;
        }

        int currentIndex = GetSelectedButtonIndex(activeButtons, true);
        int nextIndex = (currentIndex + direction + activeButtons.Count) % activeButtons.Count;

        Button targetButton = activeButtons[nextIndex];
        SelectCosmeticItem(fishermanCosmeticItemButtons, targetButton, true);
    }

    public void CycleFishermanHair(int direction)
    {
        List<Button> activeButtons = GetActiveCosmeticButtons(true);
        if (activeButtons.Count == 0)
        {
            return;
        }

        int currentIndex = GetSelectedButtonIndex(activeButtons, true);
        int nextIndex = (currentIndex + direction + activeButtons.Count) % activeButtons.Count;

        Button targetButton = activeButtons[nextIndex];
        SelectCosmeticItem(fishermanCosmeticItemButtons, targetButton, true);
    }

    private List<Sprite> GetOrderedFishermanHatPreviewSprites()
    {
        if (orderedFishermanHatPreviewSprites != null)
        {
            return orderedFishermanHatPreviewSprites;
        }

        orderedFishermanHatPreviewSprites = new List<Sprite>();
        List<Sprite> loadedSprites = LoadPreviewSpritesFromFolder(FishermanPreviewAssetFolder, string.Empty);
        for (int i = 0; i < loadedSprites.Count; i++)
        {
            Sprite sprite = loadedSprites[i];
            if (sprite == null || IsFishermanHairPreviewSprite(sprite))
            {
                continue;
            }

            orderedFishermanHatPreviewSprites.Add(sprite);
        }

        orderedFishermanHatPreviewSprites.Sort((a, b) =>
            string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        return orderedFishermanHatPreviewSprites;
    }

    private static bool IsFishermanHairPreviewSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return false;
        }

        string name = NormalizeSpriteName(sprite.name);
        return name.Contains("redhair") || (name.Contains("red") && name.Contains("hair") && !name.Contains("hat"));
    }

    private bool TryCycleFishermanHatPreviewSprites(int direction)
    {
        if (FishermanDisplayObject == null)
        {
            return false;
        }

        Image fishermanImage = FishermanDisplayObject.GetComponent<Image>();
        if (fishermanImage == null)
        {
            return false;
        }

        List<Sprite> previewSprites = GetOrderedFishermanHatPreviewSprites();
        if (previewSprites.Count == 0)
        {
            return false;
        }

        int currentIndex = 0;
        Sprite currentSprite = fishermanImage.sprite;
        if (currentSprite != null)
        {
            string currentKey = NormalizeSpriteName(currentSprite);
            for (int i = 0; i < previewSprites.Count; i++)
            {
                if (NormalizeSpriteName(previewSprites[i]) == currentKey)
                {
                    currentIndex = i;
                    break;
                }
            }
        }

        int nextIndex = (currentIndex + direction + previewSprites.Count) % previewSprites.Count;
        Sprite nextPreview = previewSprites[nextIndex];
        if (nextPreview == null)
        {
            return false;
        }

        fishermanImage.sprite = nextPreview;
        fishermanImage.preserveAspect = true;

        Sprite cosmeticSprite = FindFishermanCosmeticSpriteForPreview(nextPreview);
        if (cosmeticSprite != null)
        {
            CosmeticRuntimeApplier.SelectFishermanHat(cosmeticSprite);
            List<Button> hatButtons = GetActiveCosmeticButtons(true);
            ApplyItemOpacity(hatButtons, FindCosmeticButtonForSprite(hatButtons, cosmeticSprite));
        }
        else
        {
            CosmeticRuntimeApplier.SelectFishermanHat(null);
        }

        RefreshBottomRightPreview();
        return true;
    }

    private Sprite FindFishermanCosmeticSpriteForPreview(Sprite previewSprite)
    {
        if (previewSprite == null)
        {
            return null;
        }

        BuildPreviewMapsIfNeeded();
        string previewKey = NormalizeSpriteName(previewSprite);

        foreach (KeyValuePair<string, Sprite> entry in fishermanPreviewSpritesByCosmetic)
        {
            if (entry.Value != null && NormalizeSpriteName(entry.Value) == previewKey)
            {
                return GetSpriteByNormalizedName(entry.Key);
            }
        }

        List<CosmeticPreviewRule> rules = FishermanPreviewRules;
        if (rules != null)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                CosmeticPreviewRule rule = rules[i];
                if (rule == null || NormalizeSpriteName(rule.PreviewSpriteName) != previewKey)
                {
                    continue;
                }

                Sprite cosmeticSprite = GetSpriteByNormalizedName(rule.CosmeticName);
                if (cosmeticSprite != null)
                {
                    return cosmeticSprite;
                }
            }
        }

        return null;
    }

    private Sprite GetSpriteByNormalizedName(string normalizedName)
    {
        List<Button> buttons = fishermanCosmeticItemButtons;
        for (int i = 0; i < buttons.Count; i++)
        {
            Sprite sprite = GetButtonSprite(buttons[i]);
            if (sprite != null && NormalizeSpriteName(sprite) == NormalizeSpriteName(normalizedName))
            {
                return sprite;
            }
        }

        return null;
    }

    private static Button FindCosmeticButtonForSprite(List<Button> buttons, Sprite sprite)
    {
        if (buttons == null || sprite == null)
        {
            return null;
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            Button button = buttons[i];
            if (button != null && GetButtonSprite(button) == sprite)
            {
                return button;
            }
        }

        return null;
    }
}

