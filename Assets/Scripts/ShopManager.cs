using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ShopManager : MonoBehaviour
{
    public Button HatButton, RoadButton,CheastButton, BackHatButton, BackCheastButton;
    public GameObject ShopItemPanel, HatItemsPanel, FishVoyageDiagram, cheastPanel, RoadPanel;
    public TextMeshProUGUI ShopCoinText;

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
