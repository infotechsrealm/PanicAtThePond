using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class ShopManager : MonoBehaviour
{
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

    [Header("Fish/Fisherman Dropdown")]
    public Button FishFishermanDropdownButton;
    public GameObject FishFishermanDropdownList;
    public Button FishOptionButton;
    public Button FishermanOptionButton;
    public Transform FishFishermanDropdownArrow;
    public Text FishFishermanDropdownText;
    public TMP_Text FishFishermanDropdownTMPText;

    [Header("Display Preview")]
    public GameObject FishDisplayObject;
    public GameObject[] FishDisplayObjects;
    public GameObject FishermanDisplayObject;
    public Button DisplayHatButton;
    public GameObject HatDisplayObject;

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
    private UIImageFrameAnimator saltShopAnimator;
    private readonly List<Button> fishCosmeticItemButtons = new List<Button>();
    private readonly List<Button> fishermanCosmeticItemButtons = new List<Button>();
    private readonly List<UnityAction> fishCosmeticItemActions = new List<UnityAction>();
    private readonly List<UnityAction> fishermanCosmeticItemActions = new List<UnityAction>();

    private void Awake()
    {
        ResolvePanelReferences();
        ResolveCloseButton();
        ResolveCosmeticCategoryReferences();
        ResolveCosmeticItemRoots();
        ResolveFishDisplayObjects();

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
        SetupAnimatedSaltShopGif();
        SetActiveIfNotNull(SaltShopPanel, false);
        StartCoroutine(FetchCoinsForShop());
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
        SetActiveIfNotNull(FishVoyageDiagram, false);
        SetActiveIfNotNull(cheastPanel, false);
        SetActiveIfNotNull(RoadPanel, false);
        SetActiveIfNotNull(FishFishermanCosmeticPanel, true);
        SetActiveIfNotNull(FishCosmeticPanel, true);
        SetActiveIfNotNull(FishermanCosmeticPanel, false);
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
        SelectFishermanHatCategory();
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
        SetActiveIfNotNull(FishFishermanCosmeticPanel, false);
        SetActiveIfNotNull(FishCosmeticPanel, false);
        SetActiveIfNotNull(FishermanCosmeticPanel, false);
        SetActiveIfNotNull(FishermanHairObject, false);
        SetActiveIfNotNull(FishermanHatObject, false);
        CloseFishFishermanDropdown();
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

    public void SelectFishDisplay()
    {
        bool isHatVisible = HatDisplayObject != null && HatDisplayObject.activeSelf;
        SetDisplayMode("Fish", true, false, isHatVisible);
    }

    public void SelectFishermanDisplay()
    {
        bool isHatVisible = HatDisplayObject != null && HatDisplayObject.activeSelf;
        SetDisplayMode("Fisherman", false, true, isHatVisible);
    }

    public void SelectHatDisplay()
    {
        if (HatDisplayObject != null) HatDisplayObject.SetActive(true);
        CloseFishFishermanDropdown();
    }

    public void SelectFishermanHairCategory()
    {
        SetActiveIfNotNull(FishermanHairObject, true);
        SetActiveIfNotNull(FishermanHatObject, false);
        SetActiveIfNotNull(FishermanCosmeticPanel, true);
        SetActiveIfNotNull(FishCosmeticPanel, false);
    }

    public void SelectFishermanHatCategory()
    {
        SetActiveIfNotNull(FishermanHatObject, true);
        SetActiveIfNotNull(FishermanHairObject, false);
        SetActiveIfNotNull(FishermanCosmeticPanel, true);
        SetActiveIfNotNull(FishCosmeticPanel, false);
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
    }

    private void SetDisplayMode(string label, bool showFish, bool showFisherman, bool showHat)
    {
        SetFishDisplayVisible(showFish);
        SetActiveIfNotNull(FishermanDisplayObject, showFisherman);
        SetActiveIfNotNull(HatDisplayObject, showHat);
        SetDropdownLabel(label);
        CloseFishFishermanDropdown();
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
        if (FishDisplayObjects != null && FishDisplayObjects.Length > 1)
        {
            return;
        }

        Transform displayRoot = FishDisplayObject != null ? FishDisplayObject.transform.parent : null;
        if (displayRoot == null)
        {
            return;
        }

        GameObject fishOne = FindGameObjectByNames(displayRoot, "Fish 1", "Fish1", "Fish");
        GameObject fishTwo = FindGameObjectByNames(displayRoot, "Fish 2", "Fish2");
        if (fishOne == null && fishTwo == null)
        {
            return;
        }

        FishDisplayObjects = new GameObject[]
        {
            fishOne != null ? fishOne : FishDisplayObject,
            fishTwo
        };
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
            if (!IsCosmeticItemImage(image))
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

        Sprite selectedSprite = GetButtonSprite(selectedButton);
        if (selectedSprite == null)
        {
            return;
        }

        if (!isFishermanCosmetic)
        {
            CosmeticRuntimeApplier.SelectFishHat(selectedSprite);
            return;
        }

        if (FishermanHairObject != null && selectedButton.transform.IsChildOf(FishermanHairObject.transform))
        {
            CosmeticRuntimeApplier.SelectFishermanHair(selectedSprite);
        }
        else
        {
            CosmeticRuntimeApplier.SelectFishermanHat(selectedSprite);
        }
    }

    private static Sprite GetButtonSprite(Button button)
    {
        Image image = button != null ? button.GetComponent<Image>() : null;
        if (image != null && image.sprite != null)
        {
            return image.sprite;
        }

        image = button != null ? button.GetComponentInChildren<Image>(true) : null;
        return image != null ? image.sprite : null;
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
        }
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
}
