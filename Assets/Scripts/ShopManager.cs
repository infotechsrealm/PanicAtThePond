using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ShopManager : MonoBehaviour
{
    public Button HatButton, RoadButton,CheastButton, BackHatButton, BackCheastButton;
    public GameObject ShopItemPanel, HatItemsPanel, FishVoyageDiagram, cheastPanel, RoadPanel;
    public TextMeshProUGUI ShopCoinText;

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
    public string fishingshopFramesResourceFolder = "Fishingshop2Frames";
    public float fishingshopFramesPerSecond = 10f;

    private bool isFishFishermanDropdownOpen;
    private RectTransform fishFishermanDropdownClickArea;
    private LocalPlayManager displayLocalPlayManager;

    private void Awake()
    {
        if (HatButton != null)
        {
            HatButton.onClick.AddListener(HatShopUI);
        }

        if (RoadButton != null)
        {
            RoadButton.onClick.AddListener(RoadShopUI);
        }

        if (BackHatButton != null)
        {
            BackHatButton.onClick.AddListener(BackHatPanelUI);
        }

        if (CheastButton != null)
        {
            CheastButton.onClick.AddListener(CheastShopUI);
        }

        if (BackCheastButton != null)
        {
            BackCheastButton.onClick.AddListener(BackCheastPanelUI);
        }

        ResolveOptionalReferences();
        RegisterOptionalButtons();
    }

    private void Start()
    {
        ResolveOptionalReferences();

        if (FishVoyageDiagram == null && ShopItemPanel != null)
        {
            Transform diagram = FindChildByName(ShopItemPanel.transform.root, "Fish Voyage Diagram");
            if (diagram != null)
            {
                FishVoyageDiagram = diagram.gameObject;
            }
        }

        if (FishVoyageDiagram != null)
        {
            FishVoyageDiagram.SetActive(false);
        }

        CloseFishFishermanDropdown();
        SelectFishDisplay();
        if (SaltShopPanel != null)
        {
            SaltShopPanel.SetActive(false);
        }

        SetupAnimatedShopGif();
        StartCoroutine(FetchCoinsForShop());
    }

    private void Update()
    {
        if (FishFishermanDropdownButton != null || fishFishermanDropdownClickArea == null)
        {
            return;
        }

        if (!fishFishermanDropdownClickArea.gameObject.activeInHierarchy || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (RectTransformUtility.RectangleContainsScreenPoint(
            fishFishermanDropdownClickArea,
            Input.mousePosition,
            GetDropdownClickCamera()))
        {
            ToggleFishFishermanDropdown();
        }
    }

    private IEnumerator FetchCoinsForShop()
    {
        if (PlayFabManager.Instance != null && ShopCoinText != null)
        {
            // Wait until PlayFab is fully logged in
            while (!PlayFabManager.Instance.IsLoggedIn)
            {
                yield return null; // wait to next frame
            }

            PlayFabManager.Instance.GetCurrency(amount =>
            {
                ShopCoinText.text = amount.ToString();
            });
        }
    }

    public void BackCheastPanelUI()
    {
        if (cheastPanel != null)
        {
            cheastPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (HatButton != null)
        {
            HatButton.onClick.RemoveListener(HatShopUI);
        }

        if (RoadButton != null)
        {
            RoadButton.onClick.RemoveListener(RoadShopUI);
        }

        if (BackHatButton != null)
        {
            BackHatButton.onClick.RemoveListener(BackHatPanelUI);
        }

        if (CheastButton != null)
        {
            CheastButton.onClick.RemoveListener(CheastShopUI);
        }

        if (BackCheastButton != null)
        {
            BackCheastButton.onClick.RemoveListener(BackCheastPanelUI);
        }

        if (FishFishermanDropdownButton != null)
        {
            FishFishermanDropdownButton.onClick.RemoveListener(ToggleFishFishermanDropdown);
        }

        if (FishOptionButton != null)
        {
            FishOptionButton.onClick.RemoveListener(SelectFishDisplay);
        }

        if (FishermanOptionButton != null)
        {
            FishermanOptionButton.onClick.RemoveListener(SelectFishermanDisplay);
        }

        if (DisplayHatButton != null)
        {
            DisplayHatButton.onClick.RemoveListener(SelectHatDisplay);
        }

        if (SaltShopButton != null)
        {
            SaltShopButton.onClick.RemoveListener(OpenSaltShop);
        }

        if (SaltShopBackButton != null)
        {
            SaltShopBackButton.onClick.RemoveListener(CloseSaltShop);
        }
    }

    public void CheastShopUI()
    {
        if (ShopItemPanel != null)
        {
            ShopItemPanel.SetActive(true);
        }

        if (HatItemsPanel != null)
        {
            HatItemsPanel.SetActive(false);
        }

        if (FishVoyageDiagram != null)
        {
            FishVoyageDiagram.SetActive(false);
        }

        if (cheastPanel != null)
        {
            cheastPanel.SetActive(true);
        }
    }

    public void HatShopUI()
    {
        if (ShopItemPanel != null)
        {
            ShopItemPanel.SetActive(true);
        }

        if (HatItemsPanel != null)
        {
            HatItemsPanel.SetActive(true);
        }

        if (FishVoyageDiagram != null)
        {
            FishVoyageDiagram.SetActive(false);
        }
        if (cheastPanel != null)
        {
            cheastPanel.SetActive(false);
        }
        if (RoadPanel != null)
        {
            RoadPanel.SetActive(false);
        }
    }

    public void RoadShopUI()
    {
        if (ShopItemPanel != null)
        {
            ShopItemPanel.SetActive(true);
        }

        if (HatItemsPanel != null)
        {
            HatItemsPanel.SetActive(false);
        }

        if (FishVoyageDiagram != null)
        {
            FishVoyageDiagram.SetActive(true);
        }

        if (RoadPanel != null)
        {
            RoadPanel.SetActive(true);
        }
    }

    public void BackHatPanelUI()
    {
        if (HatItemsPanel != null)
        {
            HatItemsPanel.SetActive(false);
        }

        if (ShopItemPanel != null)
        {
            ShopItemPanel.SetActive(false);
        }

        if (FishVoyageDiagram != null)
        {
            FishVoyageDiagram.SetActive(false);
        }

        if (RoadPanel != null)
        {
            RoadPanel.SetActive(false);
        }
    }

    public void ToggleFishFishermanDropdown()
    {
        SetFishFishermanDropdownOpen(!isFishFishermanDropdownOpen);
    }

    public void SelectFishDisplay()
    {
        SetDisplayMode("Fish", true, false, false);
    }

    public void SelectFishermanDisplay()
    {
        SetDisplayMode("Fisherman", false, true, false);
    }

    public void SelectHatDisplay()
    {
        SetActiveIfNotNull(HatDisplayObject, true);
        CloseFishFishermanDropdown();
    }

    public void HideFishFishermanDisplay()
    {
        SetFishDisplayVisible(false);
        SetActiveIfNotNull(FishermanDisplayObject, false);
        CloseFishFishermanDropdown();
    }

    public void OpenSaltShop()
    {
        if (ShopItemPanel != null)
        {
            ShopItemPanel.SetActive(true);
        }

        if (SaltShopPanel != null)
        {
            SaltShopPanel.SetActive(true);
        }

        CloseFishFishermanDropdown();
    }

    public void CloseSaltShop()
    {
        if (SaltShopPanel != null)
        {
            SaltShopPanel.SetActive(false);
        }

        if (ShopItemPanel != null)
        {
            ShopItemPanel.SetActive(false);
        }
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
        GameObject[] fishDisplays = GetFishDisplayObjects();
        if (fishDisplays == null || fishDisplays.Length == 0)
        {
            SetActiveIfNotNull(FishDisplayObject, visible);
            return;
        }

        for (int i = 0; i < fishDisplays.Length; i++)
        {
            SetActiveIfNotNull(fishDisplays[i], false);
        }

        if (!visible)
        {
            return;
        }

        int selectedFish = Mathf.Clamp(PlayerPrefs.GetInt(LocalPlayManager.SelectedFishPrefKey, 0), 0, fishDisplays.Length - 1);
        GameObject selectedFishDisplay = fishDisplays[selectedFish] != null ? fishDisplays[selectedFish] : FishDisplayObject;
        SetActiveIfNotNull(selectedFishDisplay, true);
    }

    private GameObject[] GetFishDisplayObjects()
    {
        if (FishDisplayObjects != null && FishDisplayObjects.Length > 0)
        {
            return FishDisplayObjects;
        }

        if (displayLocalPlayManager != null && displayLocalPlayManager.Fish_Sprite != null && displayLocalPlayManager.Fish_Sprite.Length > 0)
        {
            FishDisplayObjects = displayLocalPlayManager.Fish_Sprite;
            return FishDisplayObjects;
        }

        return FishDisplayObject != null ? new[] { FishDisplayObject } : null;
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

    private void SetupAnimatedShopGif()
    {
        if (!autoAnimateFishingshopGif)
        {
            return;
        }

        Transform searchRoot = ShopItemPanel != null ? ShopItemPanel.transform.root : transform.root;
        if (searchRoot == null)
        {
            return;
        }

        Image[] images = searchRoot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image.sprite == null || !image.sprite.name.StartsWith("Fishingshop2"))
            {
                continue;
            }

            UIImageFrameAnimator animator = image.GetComponent<UIImageFrameAnimator>();
            if (animator == null)
            {
                animator = image.gameObject.AddComponent<UIImageFrameAnimator>();
            }

            animator.resourcesFolder = fishingshopFramesResourceFolder;
            animator.framesPerSecond = fishingshopFramesPerSecond;
            animator.loop = true;
            animator.playOnEnable = true;
            animator.Play();
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

    private static void SetActiveIfNotNull(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void RegisterOptionalButtons()
    {
        if (FishFishermanDropdownButton != null)
        {
            FishFishermanDropdownButton.onClick.AddListener(ToggleFishFishermanDropdown);
        }

        if (FishOptionButton != null)
        {
            FishOptionButton.onClick.AddListener(SelectFishDisplay);
        }

        if (FishermanOptionButton != null)
        {
            FishermanOptionButton.onClick.AddListener(SelectFishermanDisplay);
        }

        if (DisplayHatButton != null)
        {
            DisplayHatButton.onClick.AddListener(SelectHatDisplay);
        }

        if (SaltShopButton != null)
        {
            SaltShopButton.onClick.AddListener(OpenSaltShop);
        }

        if (SaltShopBackButton != null)
        {
            SaltShopBackButton.onClick.AddListener(CloseSaltShop);
        }
    }

    private void ResolveOptionalReferences()
    {
        Transform searchRoot = ShopItemPanel != null ? ShopItemPanel.transform.root : transform.root;
        Transform shopRoot = ResolveDisplayShopRoot(searchRoot);
        displayLocalPlayManager = shopRoot != null ? shopRoot.GetComponent<LocalPlayManager>() : null;
        GameObject dropdownRoot = FindFirstChildByNames(shopRoot, "Dropdown", "Fish&Fisherman DD", "Fish & FisherMan DD", "FishFishermanDropdown");

        if (IsInvalidDropdownList(FishFishermanDropdownList))
        {
            Transform listSearchRoot = dropdownRoot != null ? dropdownRoot.transform : shopRoot;
            FishFishermanDropdownList = FindFirstChildByNames(listSearchRoot, "DD list", "DD List", "Dropdown List");
        }

        if (dropdownRoot != null)
        {
            DisableIncompleteTMPDropdown(dropdownRoot);
        }

        if (IsInvalidMainDropdownButton(FishFishermanDropdownButton))
        {
            FishFishermanDropdownButton = FindExistingDropdownToggleButton(shopRoot, dropdownRoot);
        }

        fishFishermanDropdownClickArea = FishFishermanDropdownButton != null
            ? FishFishermanDropdownButton.GetComponent<RectTransform>()
            : dropdownRoot != null ? dropdownRoot.GetComponent<RectTransform>() : null;

        Transform optionRoot = FishFishermanDropdownList != null ? FishFishermanDropdownList.transform : shopRoot;

        if (IsInvalidOptionButton(FishOptionButton, "Fish"))
        {
            FishOptionButton = GetComponentFromNames<Button>(optionRoot, "Fish Button ", "Fish Button", "FishButton");
        }

        if (IsInvalidOptionButton(FishermanOptionButton, "Fisher"))
        {
            FishermanOptionButton = GetComponentFromNames<Button>(optionRoot, "FisherMan Button", "Fisherman Button", "FisherManButton", "FishermanButton");
        }

        if (FishFishermanDropdownArrow == null)
        {
            Transform arrowSearchRoot = dropdownRoot != null ? dropdownRoot.transform : shopRoot;
            GameObject arrow = FindFirstChildByNames(arrowSearchRoot, "Arrow");
            FishFishermanDropdownArrow = arrow != null ? arrow.transform : null;
        }

        if (FishFishermanDropdownText == null)
        {
            GameObject label = FindFirstChildByNames(shopRoot, "Label", "SelectedItemText");
            FishFishermanDropdownText = label != null ? label.GetComponentInChildren<Text>(true) : null;
        }

        if (FishFishermanDropdownTMPText == null)
        {
            GameObject label = FindFirstChildByNames(shopRoot, "Label", "SelectedItemText");
            FishFishermanDropdownTMPText = label != null ? label.GetComponentInChildren<TMP_Text>(true) : null;
        }

        if (FishDisplayObject == null)
        {
            FishDisplayObject = FindFirstChildByNames(shopRoot, "Fish 1", "SelectedItem");
        }

        if ((FishDisplayObjects == null || FishDisplayObjects.Length == 0) && displayLocalPlayManager != null)
        {
            FishDisplayObjects = displayLocalPlayManager.Fish_Sprite;
        }

        if (FishermanDisplayObject == null)
        {
            FishermanDisplayObject = FindFirstChildByNames(shopRoot, "FisherMan", "Fisherman");
        }

        if (DisplayHatButton == null)
        {
            Transform directHatButton = FindDirectChildByName(shopRoot, "Hat");
            DisplayHatButton = directHatButton != null
                ? directHatButton.GetComponent<Button>()
                : GetComponentFromNames<Button>(shopRoot, "Hat Button", "Display Hat Button", "DisplayHatButton");
        }

        if (HatDisplayObject == null)
        {
            Transform directHatImage = FindDirectChildByName(shopRoot, "Hatimage");
            if (directHatImage == null)
            {
                directHatImage = FindDirectChildByName(shopRoot, "Hat Image");
            }

            if (directHatImage == null)
            {
                directHatImage = FindDirectChildByName(shopRoot, "DisplayHatImage");
            }

            HatDisplayObject = directHatImage != null ? directHatImage.gameObject : null;
        }

        if (SaltShopButton == null)
        {
            SaltShopButton = GetComponentFromNames<Button>(shopRoot, "Sal - TButton", "Sal -TButton", "Sal-TButton", "SaltShopButton");
        }

        if (SaltShopPanel == null)
        {
            SaltShopPanel = FindFirstChildByNames(searchRoot, "Sal -t Image BackGround", "Sal-t Image BackGround", "Salt Image BackGround", "SaltShopPanel");
        }

        if (SaltShopBackButton == null && SaltShopPanel != null)
        {
            SaltShopBackButton = GetComponentFromNames<Button>(SaltShopPanel.transform, "Back", "Back Button");
        }
    }

    private static Transform ResolveDisplayShopRoot(Transform searchRoot)
    {
        if (searchRoot == null)
        {
            return null;
        }

        LocalPlayManager localPlayManager = searchRoot.GetComponentInChildren<LocalPlayManager>(true);
        if (localPlayManager != null)
        {
            return localPlayManager.transform;
        }

        Transform shop = FindChildByName(searchRoot, "Shop");
        return shop != null ? shop : searchRoot;
    }

    private static T GetComponentFromNames<T>(Transform root, params string[] names) where T : Component
    {
        GameObject match = FindFirstChildByNames(root, names);
        return match != null ? match.GetComponent<T>() : null;
    }

    private static bool IsInvalidDropdownList(GameObject dropdownList)
    {
        if (dropdownList == null)
        {
            return true;
        }

        return !dropdownList.name.ToLowerInvariant().Contains("list");
    }

    private bool IsInvalidMainDropdownButton(Button button)
    {
        if (button == null)
        {
            return true;
        }

        return FishFishermanDropdownList != null && button.transform.IsChildOf(FishFishermanDropdownList.transform);
    }

    private Button FindExistingDropdownToggleButton(Transform shopRoot, GameObject dropdownRoot)
    {
        Button button = dropdownRoot != null ? dropdownRoot.GetComponent<Button>() : null;
        if (button != null)
        {
            return button;
        }

        button = GetComponentFromNames<Button>(
            shopRoot,
            "Dropdown Button",
            "FishFishermanDropdownButton",
            "Fish&Fisherman Dropdown Button",
            "Fish & Fisherman Dropdown Button");

        if (button != null && !IsInvalidMainDropdownButton(button))
        {
            return button;
        }

        return null;
    }

    private static bool IsInvalidOptionButton(Button button, string expectedNamePart)
    {
        if (button == null)
        {
            return true;
        }

        return button.name.IndexOf(expectedNamePart, System.StringComparison.OrdinalIgnoreCase) < 0;
    }

    private static void DisableIncompleteTMPDropdown(GameObject dropdownRoot)
    {
        TMP_Dropdown tmpDropdown = dropdownRoot.GetComponent<TMP_Dropdown>();
        if (tmpDropdown != null && tmpDropdown.template == null)
        {
            tmpDropdown.enabled = false;
        }
    }

    private Camera GetDropdownClickCamera()
    {
        if (fishFishermanDropdownClickArea == null)
        {
            return null;
        }

        Canvas canvas = fishFishermanDropdownClickArea.GetComponentInParent<Canvas>();
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    }

    private static GameObject FindFirstChildByNames(Transform root, params string[] names)
    {
        if (root == null || names == null)
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

    private static Transform FindDirectChildByName(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
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
}
