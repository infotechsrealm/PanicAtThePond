using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public Button HatButton, KeyButton, BackHatButton;
    public GameObject ShopItemPanel, HatItemsPanel;

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
    }

    public void KeyShopUI()
    {
        //
    }

    public void BackHatPanelUI()
    {
        if (HatItemsPanel != null)
        {
            HatItemsPanel.SetActive(false);
            ShopItemPanel.SetActive(false);
        }
    }
}
