using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class LocalPlayManager : MonoBehaviour
{
    public Button backButton,BuyButton,BuyPanelBackButton;
    public GameObject[] Fish_Sprite,BuyFishInfoText;
    public Image SecondFish,LockImg;
    public Button Left_BTN,Right_BTN;
    public GameObject[] InfoText;
    public GameObject LockItem,BuyPanel;
    public GameObject FishermanDisplayObject;

    [Header("Fish Voyage Diagram")]
    public Color lockedDiagramFishColor = new Color(0.14150941f, 0.13950692f, 0.13950692f, 1f);
    public Color unlockedDiagramFishColor = Color.white;

    private int Next_Fish;

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

    private static readonly string[] TroutAchievementIds =
    {
        "GULPER",
        "WHAT_A_SNACK",
        "SOLO_ARTIST"
    };

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
        HideFishermanDisplay();

        int nextUnlockedFish = FindUnlockedFish(Next_Fish, 1);
        if (nextUnlockedFish < 0)
        {
            RefreshArrowButtons();
            return;
        }

        Next_Fish = nextUnlockedFish;
        ShowSelectedFish();
    }

    public void Tap_PreviosButton()
    {
        HideFishermanDisplay();

        int previousUnlockedFish = FindUnlockedFish(Next_Fish, -1);
        if (previousUnlockedFish < 0)
        {
            RefreshArrowButtons();
            return;
        }

        Next_Fish = previousUnlockedFish;
        ShowSelectedFish();
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
        PlayerPrefs.SetString(SelectedFishPrefabPrefKey, GetSelectedFishPrefabName());
        PlayerPrefs.Save();
    }

    private void RefreshArrowButtons()
    {
        SetButtonInteractable(Left_BTN, FindUnlockedFish(Next_Fish, -1) >= 0);
        SetButtonInteractable(Right_BTN, FindUnlockedFish(Next_Fish, 1) >= 0);
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

        return FishPrefabNames[selectedFish];
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
}
