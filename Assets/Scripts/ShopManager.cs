using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ShopManager : MonoBehaviour
{
    public Button HatButton, KeyButton, BackHatButton;
    public GameObject ShopItemPanel, HatItemsPanel, FishVoyageDiagram;
    public TextMeshProUGUI ShopCoinText;

    private void Awake()
    {
        if (HatButton != null)
        {
            HatButton.onClick.AddListener(HatShopUI);
        }

        if (KeyButton != null)
        {
            KeyButton.onClick.AddListener(KeyShopUI);
        }

        if (BackHatButton != null)
        {
            BackHatButton.onClick.AddListener(BackHatPanelUI);
        }
    }

    private void Start()
    {
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

        StartCoroutine(FetchCoinsForShop());
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

    private void OnDestroy()
    {
        if (HatButton != null)
        {
            HatButton.onClick.RemoveListener(HatShopUI);
        }

        if (KeyButton != null)
        {
            KeyButton.onClick.RemoveListener(KeyShopUI);
        }

        if (BackHatButton != null)
        {
            BackHatButton.onClick.RemoveListener(BackHatPanelUI);
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
    }

    public void KeyShopUI()
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
