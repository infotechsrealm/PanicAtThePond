using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class LocalPlayManager : MonoBehaviour
{
    public Button backButton,BuyButton,BuyPanelBackButton;
    public GameObject[] Fish_Sprite,BuyFishInfoText;
    public Image SecondFish,LockImg;
    public Button Left_BTN,Right_BTN;
    public GameObject[] InfoText;
    public GameObject LockItem,BuyPanel;
    public GameObject FishermanDisplayObject;
    public GameObject fishermanYellowHat;

    [Header("Fish Voyage Diagram")]
    public Color lockedDiagramFishColor = new Color(0.14150941f, 0.13950692f, 0.13950692f, 1f);
    public Color unlockedDiagramFishColor = Color.white;

    private int Next_Fish;
    private Sprite[] originalFishSprites;

    [Header("Hat Dropdown UI")]
    public Button HatButton;
    public GameObject DD_List_HatButton;
    public Button FishOptionButton;
    public Button FishSpeciesOptionButton;
    public TMP_Text HatButtonText;
    public Text HatButtonLegacyText;

    private enum CyclingMode
    {
        FishHats,
        FishSpecies
    }
    private CyclingMode currentCyclingMode = CyclingMode.FishHats;

    public const string SelectedFishPrefKey = "SelectedFish";
    public const string TroutUnlockedPrefKey = "FishUnlocked_Trout";
    public const string SelectedFishPrefabPrefKey = "SelectedFishPrefab";
    public const string DefaultFishPrefabName = "Fish";
    public const string TroutFishPrefabName = "Fish 2";

    private static readonly string[] FishPrefabNames =
    {
        DefaultFishPrefabName,
        TroutFishPrefabName
    };

    private static readonly float[] FishPrefabScales =
    {
        1f,
        3.3f
    };

    private static readonly string[] TroutAchievementIds =
    {
        "GULPER",
        "WHAT_A_SNACK",
        "SOLO_ARTIST"
    };

    private void Awake()
    {
        CacheOriginalSprites();
    }

    private void Start()
    {
        Next_Fish = Mathf.Clamp(PlayerPrefs.GetInt(SelectedFishPrefKey, 0), 0, FishPrefabNames.Length - 1);
        RefreshFishUnlocks();
        if (!IsFishUnlocked(Next_Fish))
        {
            Next_Fish = GetFirstUnlockedFishIndex();
        }

        if (BuyButton != null)
        {
            BuyButton.gameObject.SetActive(false);
            BuyButton.onClick.RemoveListener(BuyPanelON);
            BuyButton.onClick.AddListener(BuyPanelON);
        }

        if (BuyPanelBackButton != null)
        {
            BuyPanelBackButton.onClick.RemoveListener(BuyPanleClose);
            BuyPanelBackButton.onClick.AddListener(BuyPanleClose);
        }

        if (HatButton != null)
        {
            HatButton.onClick.RemoveListener(ToggleHatDropdown);
            HatButton.onClick.AddListener(ToggleHatDropdown);
        }

        if (FishOptionButton != null)
        {
            FishOptionButton.onClick.RemoveListener(SelectFishMode);
            FishOptionButton.onClick.AddListener(SelectFishMode);
        }

        if (FishSpeciesOptionButton != null)
        {
            FishSpeciesOptionButton.onClick.RemoveListener(SelectFishSpeciesMode);
            FishSpeciesOptionButton.onClick.AddListener(SelectFishSpeciesMode);
        }

        if (DD_List_HatButton != null)
        {
            DD_List_HatButton.SetActive(false);
        }

        if (HatButtonText != null)
        {
            HatButtonText.text = currentCyclingMode == CyclingMode.FishHats ? "FISH" : "FISH SPECIES";
        }
        if (HatButtonLegacyText != null)
        {
            HatButtonLegacyText.text = currentCyclingMode == CyclingMode.FishHats ? "FISH" : "FISH SPECIES";
        }

        RegisterArrowButtons();
        ResolveDisplayObjects();

        if (BuyPanel != null)
        {
            BuyPanel.SetActive(false);
        }

        ShowSelectedFish();
        KeepArrowButtonsOnScreen();
    }


    private void OnDestroy()
    {
        if (BuyButton != null)
        {
            BuyButton.onClick.RemoveListener(BuyPanelON);
        }

        if (BuyPanelBackButton != null)
        {
            BuyPanelBackButton.onClick.RemoveListener(BuyPanleClose);
        }

        if (HatButton != null)
        {
            HatButton.onClick.RemoveListener(ToggleHatDropdown);
        }

        if (FishOptionButton != null)
        {
            FishOptionButton.onClick.RemoveListener(SelectFishMode);
        }

        if (FishSpeciesOptionButton != null)
        {
            FishSpeciesOptionButton.onClick.RemoveListener(SelectFishSpeciesMode);
        }

        if (Right_BTN != null)
        {
            Right_BTN.onClick.RemoveListener(Tap_NextButton);
        }

        if (Left_BTN != null)
        {
            Left_BTN.onClick.RemoveListener(Tap_PreviosButton);
        }
    }


    private void OnEnable()
    {
        if (BackManager.instance != null && backButton != null)
        {
            BackManager.instance.RegisterScreen(backButton);
        }

        RefreshFishUnlocks();
        ResolveDisplayObjects();
        if (!IsFishUnlocked(Next_Fish))
        {
            Next_Fish = GetFirstUnlockedFishIndex();
        }

        ShowSelectedFish();
        KeepArrowButtonsOnScreen();
    }

    public void BackButton()
    {
        if (BackManager.instance != null)
        {
            BackManager.instance.UnregisterScreen();
        }

        gameObject.SetActive(false);
    }

    public void Tap_NextButton()
    {
        if (TryCycleShopSelection(1))
        {
            return;
        }

        if (currentCyclingMode == CyclingMode.FishHats)
        {
            CycleActiveFishHat(1);
        }
        else
        {
            CycleActiveFish(1);
        }
    }

    public void Tap_PreviosButton()
    {
        if (TryCycleShopSelection(-1))
        {
            return;
        }

        if (currentCyclingMode == CyclingMode.FishHats)
        {
            CycleActiveFishHat(-1);
        }
        else
        {
            CycleActiveFish(-1);
        }
    }


    private bool TryCycleShopSelection(int direction)
    {
        ShopManager shop = FindShopManager();
        if (shop != null && shop.IsShopOpen())
        {
            return shop.CycleActiveDisplaySelection(direction, this);
        }

        return false;
    }

    private static ShopManager FindShopManager()
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop == null)
        {
            ShopManager[] shops = FindObjectsByType<ShopManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            shop = shops != null && shops.Length > 0 ? shops[0] : null;
        }

        if (shop != null)
        {
            if (!shop.gameObject.activeSelf)
            {
                shop.gameObject.SetActive(true);
            }

            if (!shop.enabled)
            {
                shop.enabled = true;
            }
        }

        return shop;
    }

    public void ApplyFishSelectionFromShop(int fishIndex)
    {
        Next_Fish = Mathf.Clamp(fishIndex, 0, FishPrefabNames.Length - 1);
        ShowSelectedFish();
    }

    public bool CycleActiveFish(int direction)
    {
        HideFishermanDisplay();

        int nextUnlocked = FindUnlockedFish(Next_Fish, direction);
        if (nextUnlocked < 0)
        {
            int startCheck = (direction > 0) ? 0 : FishPrefabNames.Length - 1;
            if (IsFishUnlocked(startCheck))
            {
                nextUnlocked = startCheck;
            }
            else
            {
                nextUnlocked = FindUnlockedFish(startCheck, direction);
            }
        }

        if (nextUnlocked >= 0 && nextUnlocked != Next_Fish)
        {
            Next_Fish = nextUnlocked;
            ShowSelectedFish();
            return true;
        }

        return false;
    }

    private void ShowSelectedFish()
    {
        RefreshFishUnlocks();
        bool troutUnlocked = IsFishUnlocked(1);
        RefreshFishVoyageDiagram(troutUnlocked);

        if (!IsFishUnlocked(Next_Fish))
        {
            Next_Fish = GetFirstUnlockedFishIndex();
        }

        if (Fish_Sprite != null)
        {
            for (int i = 0; i < Fish_Sprite.Length; i++)
            {
                if (Fish_Sprite[i] != null)
                {
                    Fish_Sprite[i].SetActive(i == Next_Fish);
                    if (i == Next_Fish)
                    {
                        Sprite compSprite = GetCompositeSpriteForSelectedHat(i);
                        SetPreviewFishSprite(Fish_Sprite[i], compSprite);
                    }
                    else
                    {
                        CacheOriginalSprites();
                        if (originalFishSprites != null && i < originalFishSprites.Length)
                        {
                            SetPreviewFishSprite(Fish_Sprite[i], originalFishSprites[i]);
                        }
                    }
                }
            }
        }


        if (BuyFishInfoText != null)
        {
            for (int i = 0; i < BuyFishInfoText.Length; i++)
            {
                if (BuyFishInfoText[i] != null)
                {
                    BuyFishInfoText[i].SetActive(false);
                }
            }
        }

        bool selectedFishUnlocked = IsFishUnlocked(Next_Fish);
        if (LockImg != null)
        {
            LockImg.gameObject.SetActive(!selectedFishUnlocked);
        }

        if (LockItem != null)
        {
            LockItem.SetActive(!selectedFishUnlocked);
        }

        if (BuyButton != null)
        {
            BuyButton.gameObject.SetActive(false);
        }

        if (BuyPanel != null)
        {
            BuyPanel.SetActive(false);
        }

        RefreshArrowButtons();

        PlayerPrefs.SetInt(SelectedFishPrefKey, Next_Fish);
        PlayerPrefs.SetString(SelectedFishPrefabPrefKey, GetFishPrefabNameForSelection(Next_Fish));
        PlayerPrefs.Save();
    }

    private void RefreshArrowButtons()
    {
        SetButtonInteractable(Left_BTN, true);
        SetButtonInteractable(Right_BTN, true);
    }

    private void RegisterArrowButtons()
    {
        if (Right_BTN != null)
        {
            Right_BTN.onClick.RemoveListener(Tap_NextButton);
            Right_BTN.onClick.AddListener(Tap_NextButton);
        }

        if (Left_BTN != null)
        {
            Left_BTN.onClick.RemoveListener(Tap_PreviosButton);
            Left_BTN.onClick.AddListener(Tap_PreviosButton);
        }
    }

    private void HideFishermanDisplay()
    {
        if (FishermanDisplayObject != null)
        {
            FishermanDisplayObject.SetActive(false);
        }

        if (fishermanYellowHat != null)
        {
            fishermanYellowHat.SetActive(false);
        }
    }

    private void ResolveDisplayObjects()
    {
        if (FishermanDisplayObject == null)
        {
            Transform fisherman = FindChildByName(transform, "FisherMan");
            if (fisherman == null)
            {
                fisherman = FindChildByName(transform, "Fisherman");
            }

            FishermanDisplayObject = fisherman != null ? fisherman.gameObject : null;
        }

        if (fishermanYellowHat == null)
        {
            Transform yellowHat = FindChildByName(transform, "FisherMan yellow hat");
            if (yellowHat == null)
            {
                yellowHat = FindChildByName(transform, "Fisherman yellow hat");
            }

            fishermanYellowHat = yellowHat != null ? yellowHat.gameObject : null;
        }
    }

    private void RefreshFishVoyageDiagram(bool troutUnlocked)
    {
        SetActiveInfoText(0, troutUnlocked);
        SetActiveInfoText(1, troutUnlocked);
        SetActiveInfoText(2, !troutUnlocked);
        SetActiveInfoText(3, !troutUnlocked);

        if (SecondFish != null)
        {
            SecondFish.color = troutUnlocked ? GetUnlockedDiagramFishColor() : GetLockedDiagramFishColor();
        }
    }

    private Color GetLockedDiagramFishColor()
    {
        return lockedDiagramFishColor.a > 0f ? lockedDiagramFishColor : new Color(0.14150941f, 0.13950692f, 0.13950692f, 1f);
    }

    private Color GetUnlockedDiagramFishColor()
    {
        return unlockedDiagramFishColor.a > 0f ? unlockedDiagramFishColor : Color.white;
    }

    private void SetActiveInfoText(int index, bool active)
    {
        if (InfoText == null || index < 0 || index >= InfoText.Length || InfoText[index] == null)
        {
            return;
        }

        InfoText[index].SetActive(active);
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private int FindUnlockedFish(int startIndex, int direction)
    {
        int index = startIndex + direction;
        while (index >= 0 && index < FishPrefabNames.Length)
        {
            if (IsFishUnlocked(index))
            {
                return index;
            }

            index += direction;
        }

        return -1;
    }

    private int GetFirstUnlockedFishIndex()
    {
        for (int i = 0; i < FishPrefabNames.Length; i++)
        {
            if (IsFishUnlocked(i))
            {
                return i;
            }
        }

        return 0;
    }

    private static bool IsFishUnlocked(int fishIndex)
    {
        if (fishIndex == 0)
        {
            return true;
        }

        if (fishIndex == 1)
        {
            return PlayerPrefs.GetInt(TroutUnlockedPrefKey, 0) == 1;
        }

        return false;
    }

    private static void RefreshFishUnlocks()
    {
        if (PlayerPrefs.GetInt(TroutUnlockedPrefKey, 0) == 1)
        {
            return;
        }

        if (AreAllTroutAchievementsUnlocked())
        {
            PlayerPrefs.SetInt(TroutUnlockedPrefKey, 1);
            PlayerPrefs.Save();
        }
    }

    private static bool AreAllTroutAchievementsUnlocked()
    {
        for (int i = 0; i < TroutAchievementIds.Length; i++)
        {
            if (!IsAchievementUnlocked(TroutAchievementIds[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAchievementUnlocked(string achievementId)
    {
        if (PlayerPrefs.GetInt("Achievement_" + achievementId, 0) == 1)
        {
            return true;
        }

        if (!SteamManager.Initialized)
        {
            return false;
        }

        return SteamUserStats.GetAchievement(achievementId, out bool achieved) && achieved;
    }

    public static string GetSelectedFishPrefabName()
    {
        RefreshFishUnlocks();

        int selectedFish = Mathf.Clamp(PlayerPrefs.GetInt(SelectedFishPrefKey, 0), 0, FishPrefabNames.Length - 1);
        if (!IsFishUnlocked(selectedFish))
        {
            selectedFish = 0;
        }

        return GetFishPrefabNameForSelection(selectedFish);
    }

    public static float GetSelectedFishScale()
    {
        RefreshFishUnlocks();

        int selectedFish = Mathf.Clamp(PlayerPrefs.GetInt(SelectedFishPrefKey, 0), 0, FishPrefabScales.Length - 1);
        if (!IsFishUnlocked(selectedFish))
        {
            selectedFish = 0;
        }

        return GetFishScaleForSelection(selectedFish);
    }

    public static string GetFishPrefabNameForSelection(int selectedFish)
    {
        int fishIndex = Mathf.Clamp(selectedFish, 0, FishPrefabNames.Length - 1);
        return FishPrefabNames[fishIndex];
    }

    public static float GetFishScaleForSelection(int selectedFish)
    {
        int fishIndex = Mathf.Clamp(selectedFish, 0, FishPrefabScales.Length - 1);
        return FishPrefabScales[fishIndex];
    }

    private void KeepArrowButtonsOnScreen()
    {
        KeepButtonInsideParent(Left_BTN);
        KeepButtonInsideParent(Right_BTN);
    }

    private static void KeepButtonInsideParent(Button button)
    {
        RectTransform rect = button != null ? button.transform as RectTransform : null;
        RectTransform parent = rect != null ? rect.parent as RectTransform : null;
        if (rect == null || parent == null)
        {
            return;
        }

        float halfWidth = parent.rect.width * 0.5f;
        float halfButtonWidth = rect.rect.width * 0.5f;
        float margin = 12f;
        Vector2 position = rect.anchoredPosition;
        position.x = Mathf.Clamp(position.x, -halfWidth + halfButtonWidth + margin, halfWidth - halfButtonWidth - margin);
        rect.anchoredPosition = position;
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

    public void BuyPanelON()
    {
        if (BuyPanel != null)
        {
            BuyPanel.SetActive(true);
        }
    }

    public void BuyPanleClose()
    {
        if (BuyPanel != null)
        {
            BuyPanel.SetActive(false);
        }
    }

    private void CacheOriginalSprites()
    {
        if (originalFishSprites != null) return;

        originalFishSprites = new Sprite[Fish_Sprite.Length];
        for (int i = 0; i < Fish_Sprite.Length; i++)
        {
            if (Fish_Sprite[i] != null)
            {
                SpriteRenderer sr = Fish_Sprite[i].GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    originalFishSprites[i] = sr.sprite;
                }
                else
                {
                    Image img = Fish_Sprite[i].GetComponent<Image>();
                    if (img != null)
                    {
                        originalFishSprites[i] = img.sprite;
                    }
                }
            }
        }
    }

    private void SetPreviewFishSprite(GameObject fishDisplay, Sprite sprite)
    {
        if (fishDisplay == null || sprite == null) return;

        SpriteRenderer sr = fishDisplay.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = sprite;
        }
        else
        {
            Image img = fishDisplay.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = sprite;
            }
        }
    }

    public void RestoreOriginalFishSprites()
    {
        CacheOriginalSprites();
        if (originalFishSprites != null && Fish_Sprite != null)
        {
            for (int i = 0; i < Fish_Sprite.Length; i++)
            {
                if (Fish_Sprite[i] != null && i < originalFishSprites.Length)
                {
                    SetPreviewFishSprite(Fish_Sprite[i], originalFishSprites[i]);
                }
            }
        }
    }

    private List<Sprite> GetCompositePreviewSprites(int fishIndex)
    {
        List<Sprite> list = new List<Sprite>();
        
        CacheOriginalSprites();
        if (originalFishSprites != null && fishIndex >= 0 && fishIndex < originalFishSprites.Length)
        {
            list.Add(originalFishSprites[fishIndex]);
        }
        else
        {
            list.Add(null);
        }

        Sprite[] compositeSprites = Resources.LoadAll<Sprite>("ShopUI/Fish preview");
        if (compositeSprites != null)
        {
            foreach (Sprite sprite in compositeSprites)
            {
                if (sprite == null) continue;

                string name = sprite.name.ToLowerInvariant();
                if (fishIndex == 0) // Bass
                {
                    if (name.Contains("fish") && !name.Contains("trout"))
                    {
                        list.Add(sprite);
                    }
                }
                else if (fishIndex == 1) // Trout
                {
                    if (name.Contains("trout"))
                    {
                        list.Add(sprite);
                    }
                }
            }
        }
        
        return list;
    }

    private Sprite GetCompositeSpriteForSelectedHat(int fishIndex)
    {
        Sprite currentHat = CosmeticRuntimeApplier.GetSelectedFishHat();
        if (currentHat == null)
        {
            CacheOriginalSprites();
            if (originalFishSprites != null && fishIndex >= 0 && fishIndex < originalFishSprites.Length)
            {
                return originalFishSprites[fishIndex];
            }
            return null;
        }

        string hatNorm = NormalizeSpriteName(currentHat.name);
        List<Sprite> compositeList = GetCompositePreviewSprites(fishIndex);
        foreach (Sprite compSprite in compositeList)
        {
            if (compSprite == null) continue;

            string hatName = GetHatNameFromCompositeSpriteName(compSprite.name);
            if (!string.IsNullOrEmpty(hatName) && NormalizeSpriteName(hatName) == hatNorm)
            {
                return compSprite;
            }
        }

        CacheOriginalSprites();
        if (originalFishSprites != null && fishIndex >= 0 && fishIndex < originalFishSprites.Length)
        {
            return originalFishSprites[fishIndex];
        }
        return null;
    }

    private string GetHatNameFromCompositeSpriteName(string compositeName)
    {
        string normName = compositeName.ToLowerInvariant();
        if (normName.Contains("yellow"))
        {
            return "FisherMan_Hat_-Default_-_Fishing_Hat";
        }
        if (normName.Contains("polish"))
        {
            return "beret";
        }
        if (normName.Contains("black"))
        {
            return "hat2";
        }
        if (normName.Contains("boat"))
        {
            return "paper_boat";
        }
        if (normName.Contains("cap"))
        {
            return "cap";
        }
        if (normName.Contains("orange"))
        {
            return "hat";
        }
        return ""; // No hat
    }

    private void CycleActiveFishHat(int direction)
    {
        List<Sprite> availableSprites = GetCompositePreviewSprites(Next_Fish);
        if (availableSprites.Count == 0) return;

        Sprite currentShowSprite = null;
        if (Fish_Sprite != null && Next_Fish >= 0 && Next_Fish < Fish_Sprite.Length && Fish_Sprite[Next_Fish] != null)
        {
            SpriteRenderer sr = Fish_Sprite[Next_Fish].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                currentShowSprite = sr.sprite;
            }
            else
            {
                Image img = Fish_Sprite[Next_Fish].GetComponent<Image>();
                if (img != null)
                {
                    currentShowSprite = img.sprite;
                }
            }
        }

        int currentIndex = 0;
        if (currentShowSprite != null)
        {
            currentIndex = availableSprites.IndexOf(currentShowSprite);
        }

        if (currentIndex < 0) currentIndex = 0;

        int nextIndex = (currentIndex + direction + availableSprites.Count) % availableSprites.Count;
        Sprite nextSprite = availableSprites[nextIndex];

        if (Fish_Sprite != null && Next_Fish >= 0 && Next_Fish < Fish_Sprite.Length && Fish_Sprite[Next_Fish] != null)
        {
            SetPreviewFishSprite(Fish_Sprite[Next_Fish], nextSprite);
        }

        string hatName = nextSprite != null ? GetHatNameFromCompositeSpriteName(nextSprite.name) : "";
        Sprite hatSprite = string.IsNullOrEmpty(hatName) ? null : CosmeticRuntimeApplier.GetSpriteByName(hatName);
        CosmeticRuntimeApplier.SelectFishHat(hatSprite);
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

    public void ToggleHatDropdown()
    {
        if (DD_List_HatButton != null)
        {
            DD_List_HatButton.SetActive(!DD_List_HatButton.activeSelf);
        }
    }

    public void SelectFishMode()
    {
        currentCyclingMode = CyclingMode.FishHats;
        if (DD_List_HatButton != null)
        {
            DD_List_HatButton.SetActive(false);
        }
        
        if (HatButtonText != null)
        {
            HatButtonText.text = "FISH";
        }
        if (HatButtonLegacyText != null)
        {
            HatButtonLegacyText.text = "FISH";
        }
    }

    public void SelectFishSpeciesMode()
    {
        currentCyclingMode = CyclingMode.FishSpecies;
        if (DD_List_HatButton != null)
        {
            DD_List_HatButton.SetActive(false);
        }
        
        if (HatButtonText != null)
        {
            HatButtonText.text = "FISH SPECIES";
        }
        if (HatButtonLegacyText != null)
        {
            HatButtonLegacyText.text = "FISH SPECIES";
        }
    }
}

