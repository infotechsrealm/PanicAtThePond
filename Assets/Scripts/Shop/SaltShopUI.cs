using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime-built Sal-T shop front (PDF 1.1.8 "Functionality Of Store").
/// Renders ONLY the rotation payload held by SaltShopClientState (which the session authority
/// pushed from shop_config.json) — no prices or item picks are derived locally:
///  - up to 3 rotation hats laid out on the background shelves, price + coin above each,
///    a padlock on the hat while it is locked;
///  - clicking a locked hat opens the BUY? popup (price, YES / NO) over the shop;
///  - hats the local player already unlocked are hidden, so fewer than 3 slots may show;
///  - BACK sign returns to the previous page, CLOSE sign returns to the lobby.
/// Attached to the SaltShopPanel by ShopManager at runtime, so no scene rewiring is required.
/// </summary>
public class SaltShopUI : MonoBehaviour
{
    private const string CoinSpriteResource = "ShopUI/SaltShop/coin";
    private const string LockSpriteResource = "ShopUI/SaltShop/lock";
    private const string BackSignSpriteResource = "ShopUI/SaltShop/back_sign";
    private const string CloseSignSpriteResource = "ShopUI/SaltShop/close_sign";
    private const string Frame1SpriteResource = "ShopUI/SaltShop/picture_frame_1";
    private const string Frame2SpriteResource = "ShopUI/SaltShop/picture_frame_2";

    // Slot anchors matching the PDF mockup: two hats on the middle shelf, one on the lower shelf.
    private static readonly Vector2[] SlotAnchors =
    {
        new Vector2(0.585f, 0.47f),
        new Vector2(0.835f, 0.47f),
        new Vector2(0.71f, 0.18f)
    };

    private ShopManager shopManager;
    private RectTransform overlayRoot;
    private RectTransform itemsRoot;
    private GameObject buyPopup;
    private TextMeshProUGUI buyPriceText;
    private TextMeshProUGUI buyStatusText;
    private TextMeshProUGUI coinBalanceText;
    private SaltShopState.ShopItem pendingPurchase;
    private bool purchaseInFlight;
    private bool built;

    public void Open(ShopManager owner)
    {
        shopManager = owner;
        BuildOnce();
        CosmeticUnlocks.SyncFromPlayFab(Refresh);
        RefreshCoinBalance();
    }

    private void OnEnable()
    {
        SaltShopClientState.OnShopStateChanged += Refresh;
        if (built)
        {
            Refresh();
            RefreshCoinBalance();
        }
    }

    private void OnDisable()
    {
        SaltShopClientState.OnShopStateChanged -= Refresh;
        HideBuyPopup();
    }

    // ---------- construction ----------

    private void BuildOnce()
    {
        if (built)
        {
            return;
        }
        built = true;

        overlayRoot = CreateStretched("SaltShop Overlay", (RectTransform)transform);
        BuildPictureFrames();
        BuildSigns();
        BuildCoinBalance();
        itemsRoot = CreateStretched("SaltShop Items", overlayRoot);
        BuildBuyPopup();
    }

    private RectTransform CreateStretched(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private RectTransform CreateAnchored(string name, RectTransform parent, Vector2 normalizedAnchor, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = normalizedAnchor;
        rect.anchorMax = normalizedAnchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        return rect;
    }

    private Image AddImage(RectTransform rect, Sprite sprite, Color fallbackColor)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        if (sprite != null)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
        }
        else
        {
            image.color = fallbackColor;
        }
        return image;
    }

    private TextMeshProUGUI AddText(RectTransform rect, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private void BuildPictureFrames()
    {
        // PDF: "Add in the 2 picture frames" on the top shelf. They only appear when the art
        // exists in Resources/ShopUI/SaltShop — dropping the PNGs in is enough.
        Sprite frame1 = Resources.Load<Sprite>(Frame1SpriteResource);
        Sprite frame2 = Resources.Load<Sprite>(Frame2SpriteResource);

        if (frame1 != null)
        {
            RectTransform rect = CreateAnchored("Picture Frame 1", overlayRoot, new Vector2(0.5f, 0.87f), new Vector2(90f, 90f));
            AddImage(rect, frame1, Color.white).raycastTarget = false;
        }

        if (frame2 != null)
        {
            RectTransform rect = CreateAnchored("Picture Frame 2", overlayRoot, new Vector2(0.72f, 0.87f), new Vector2(90f, 90f));
            AddImage(rect, frame2, Color.white).raycastTarget = false;
        }
    }

    private void BuildSigns()
    {
        Sprite backSprite = Resources.Load<Sprite>(BackSignSpriteResource);
        Sprite closeSprite = Resources.Load<Sprite>(CloseSignSpriteResource);

        // Positioned under the painted "sal-T shop" sign in the background art, like the mockup.
        CreateSignButton("Back Sign", new Vector2(0.075f, 0.62f), backSprite, "BACK", OnBackSign);
        CreateSignButton("Close Sign", new Vector2(0.21f, 0.62f), closeSprite, "CLOSE", OnCloseSign);
    }

    private void CreateSignButton(string name, Vector2 anchor, Sprite sprite, string fallbackLabel, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = CreateAnchored(name, overlayRoot, anchor, new Vector2(110f, 48f));
        Image image = AddImage(rect, sprite, new Color(0.45f, 0.27f, 0.13f, 0.95f));
        image.raycastTarget = true;

        if (sprite == null)
        {
            RectTransform labelRect = CreateStretched("Label", rect);
            AddText(labelRect, fallbackLabel, 22f, new Color(0.95f, 0.87f, 0.7f), TextAlignmentOptions.Center);
        }

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
    }

    private void BuildCoinBalance()
    {
        RectTransform row = CreateAnchored("Coin Balance", overlayRoot, new Vector2(0.9f, 0.93f), new Vector2(190f, 44f));

        Sprite coinSprite = Resources.Load<Sprite>(CoinSpriteResource);
        RectTransform coinRect = CreateAnchored("Coin Icon", row, new Vector2(0.12f, 0.5f), new Vector2(38f, 38f));
        AddImage(coinRect, coinSprite, new Color(1f, 0.8f, 0.1f)).raycastTarget = false;

        RectTransform textRect = CreateAnchored("Amount", row, new Vector2(0.62f, 0.5f), new Vector2(130f, 44f));
        coinBalanceText = AddText(textRect, "...", 30f, Color.white, TextAlignmentOptions.Left);
    }

    private void BuildBuyPopup()
    {
        // BUY? popup from the PDF mockup: white card over the shop with price, YES / NO.
        RectTransform popupRect = CreateAnchored("Buy Popup", overlayRoot, new Vector2(0.5f, 0.5f), new Vector2(260f, 220f));
        buyPopup = popupRect.gameObject;
        Image background = buyPopup.AddComponent<Image>();
        background.color = Color.white;

        RectTransform titleRect = CreateAnchored("Title", popupRect, new Vector2(0.5f, 0.85f), new Vector2(220f, 40f));
        AddText(titleRect, "BUY?", 32f, new Color(0.15f, 0.15f, 0.15f), TextAlignmentOptions.Center);

        Sprite coinSprite = Resources.Load<Sprite>(CoinSpriteResource);
        RectTransform coinRect = CreateAnchored("Coin", popupRect, new Vector2(0.3f, 0.58f), new Vector2(36f, 36f));
        AddImage(coinRect, coinSprite, new Color(1f, 0.8f, 0.1f)).raycastTarget = false;

        RectTransform priceRect = CreateAnchored("Price", popupRect, new Vector2(0.62f, 0.58f), new Vector2(120f, 40f));
        buyPriceText = AddText(priceRect, "0", 30f, new Color(0.15f, 0.15f, 0.15f), TextAlignmentOptions.Left);

        RectTransform statusRect = CreateAnchored("Status", popupRect, new Vector2(0.5f, 0.38f), new Vector2(240f, 30f));
        buyStatusText = AddText(statusRect, string.Empty, 18f, new Color(0.8f, 0.1f, 0.1f), TextAlignmentOptions.Center);

        CreatePopupButton(popupRect, "Yes Button", new Vector2(0.28f, 0.15f), "YES", new Color(0.1f, 0.65f, 0.25f), OnConfirmPurchase);
        CreatePopupButton(popupRect, "No Button", new Vector2(0.72f, 0.15f), "NO", new Color(0.85f, 0.15f, 0.15f), HideBuyPopup);

        buyPopup.SetActive(false);
    }

    private void CreatePopupButton(RectTransform parent, string name, Vector2 anchor, string label, Color textColor, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = CreateAnchored(name, parent, anchor, new Vector2(90f, 44f));
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.01f); // invisible but clickable, text carries the look

        RectTransform labelRect = CreateStretched("Label", rect);
        AddText(labelRect, label, 28f, textColor, TextAlignmentOptions.Center);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
    }

    // ---------- shop content ----------

    /// <summary>Rebuilds the shelf items from the current server-provided state.</summary>
    public void Refresh()
    {
        if (!built || itemsRoot == null)
        {
            return;
        }

        for (int i = itemsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(itemsRoot.GetChild(i).gameObject);
        }

        SaltShopState state = SaltShopClientState.GetCurrent();
        if (state == null)
        {
            // Either we are a client still waiting on the host's rotation, or the config is broken.
            // Either way the shelves stay empty rather than showing locally invented prices.
            ShowShelfMessage("...");
            return;
        }

        // Per the PDF: already-unlocked hats are not offered again — if fewer remain, fewer show.
        List<SaltShopState.ShopItem> visible = new List<SaltShopState.ShopItem>();
        foreach (SaltShopState.ShopItem item in state.items)
        {
            if (item != null && !CosmeticUnlocks.IsUnlocked(item.id))
            {
                visible.Add(item);
            }
        }

        for (int i = 0; i < visible.Count && i < SlotAnchors.Length; i++)
        {
            BuildItemCell(visible[i], SlotAnchors[i]);
        }
    }

    /// <summary>
    /// Moves the BUY? card so it sits over the hat being bought, as the PDF mockup shows, while
    /// keeping it fully inside the shop panel when the hat sits near an edge.
    /// </summary>
    private void PositionBuyPopupOver(Vector2 slotAnchor)
    {
        RectTransform popupRect = (RectTransform)buyPopup.transform;
        RectTransform parentRect = (RectTransform)popupRect.parent;

        float halfWidthNorm = parentRect.rect.width > 0f
            ? (popupRect.sizeDelta.x * 0.5f) / parentRect.rect.width
            : 0.15f;
        float halfHeightNorm = parentRect.rect.height > 0f
            ? (popupRect.sizeDelta.y * 0.5f) / parentRect.rect.height
            : 0.2f;

        float x = Mathf.Clamp(slotAnchor.x, halfWidthNorm, 1f - halfWidthNorm);
        float y = Mathf.Clamp(slotAnchor.y + halfHeightNorm * 0.5f, halfHeightNorm, 1f - halfHeightNorm);

        popupRect.anchorMin = new Vector2(x, y);
        popupRect.anchorMax = new Vector2(x, y);
        popupRect.anchoredPosition = Vector2.zero;
    }

    private void ShowShelfMessage(string message)
    {
        RectTransform rect = CreateAnchored("Shelf Message", itemsRoot, new Vector2(0.71f, 0.4f), new Vector2(320f, 50f));
        AddText(rect, message, 26f, Color.white, TextAlignmentOptions.Center);
    }

    private void BuildItemCell(SaltShopState.ShopItem item, Vector2 anchor)
    {
        RectTransform cell = CreateAnchored($"Shop Item {item.id}", itemsRoot, anchor, new Vector2(170f, 150f));

        // Price row: coin icon + amount, above the hat like the mockup.
        Sprite coinSprite = Resources.Load<Sprite>(CoinSpriteResource);
        RectTransform coinRect = CreateAnchored("Coin", cell, new Vector2(0.28f, 0.82f), new Vector2(34f, 34f));
        AddImage(coinRect, coinSprite, new Color(1f, 0.8f, 0.1f)).raycastTarget = false;

        RectTransform priceRect = CreateAnchored("Price", cell, new Vector2(0.62f, 0.82f), new Vector2(100f, 40f));
        AddText(priceRect, item.price.ToString(), 28f, Color.white, TextAlignmentOptions.Left);

        // Hat icon with the padlock over it while locked.
        Sprite hatSprite = string.IsNullOrEmpty(item.iconResource) ? null : Resources.Load<Sprite>(item.iconResource);
        RectTransform hatRect = CreateAnchored("Hat", cell, new Vector2(0.5f, 0.32f), new Vector2(72f, 72f));
        Image hatImage = AddImage(hatRect, hatSprite, new Color(0.3f, 0.3f, 0.3f));
        hatImage.raycastTarget = true;

        Sprite lockSprite = Resources.Load<Sprite>(LockSpriteResource);
        RectTransform lockRect = CreateAnchored("Lock", hatRect, new Vector2(0.5f, 0.5f), new Vector2(40f, 40f));
        AddImage(lockRect, lockSprite, new Color(0.1f, 0.1f, 0.1f, 0.85f)).raycastTarget = false;

        Button button = hatRect.gameObject.AddComponent<Button>();
        button.targetGraphic = hatImage;
        button.onClick.AddListener(() => ShowBuyPopup(item, anchor));
    }

    // ---------- purchase flow ----------

    private void ShowBuyPopup(SaltShopState.ShopItem item, Vector2 slotAnchor)
    {
        pendingPurchase = item;
        purchaseInFlight = false;
        if (buyPriceText != null)
        {
            buyPriceText.text = item.price.ToString();
        }
        if (buyStatusText != null)
        {
            buyStatusText.text = string.Empty;
        }
        buyPopup.SetActive(true);
        PositionBuyPopupOver(slotAnchor);
        buyPopup.transform.SetAsLastSibling();
    }

    private void HideBuyPopup()
    {
        pendingPurchase = null;
        purchaseInFlight = false;
        if (buyPopup != null)
        {
            buyPopup.SetActive(false);
        }
    }

    private void OnConfirmPurchase()
    {
        if (pendingPurchase == null || purchaseInFlight)
        {
            return;
        }

        if (PlayFabManager.Instance == null || !PlayFabManager.Instance.IsLoggedIn)
        {
            SetBuyStatus("NOT CONNECTED");
            return;
        }

        SaltShopState.ShopItem item = pendingPurchase;
        purchaseInFlight = true;
        SetBuyStatus("...");

        PlayFabManager.Instance.GetCurrency(balance =>
        {
            if (this == null || pendingPurchase != item)
            {
                return;
            }

            if (balance < item.price)
            {
                purchaseInFlight = false;
                SetBuyStatus("NOT ENOUGH COINS");
                return;
            }

            PlayFabManager.Instance.SubtractCurrency(item.price, newBalance =>
            {
                if (this == null)
                {
                    return;
                }

                CosmeticUnlocks.Unlock(item.id);
                if (coinBalanceText != null)
                {
                    coinBalanceText.text = newBalance.ToString();
                }
                HideBuyPopup();
                Refresh();
                Debug.Log($"[SaltShopUI] Purchased '{item.id}' for {item.price}.");
            }, error =>
            {
                if (this == null)
                {
                    return;
                }

                purchaseInFlight = false;
                SetBuyStatus("PURCHASE FAILED");
            });
        });
    }

    private void SetBuyStatus(string message)
    {
        if (buyStatusText != null)
        {
            buyStatusText.text = message;
        }
    }

    private void RefreshCoinBalance()
    {
        if (coinBalanceText == null || PlayFabManager.Instance == null || !PlayFabManager.Instance.IsLoggedIn)
        {
            return;
        }

        PlayFabManager.Instance.GetCurrency(amount =>
        {
            if (this != null && coinBalanceText != null)
            {
                coinBalanceText.text = amount.ToString();
            }
        });
    }

    // ---------- navigation ----------

    private void OnBackSign()
    {
        HideBuyPopup();
        if (shopManager != null)
        {
            shopManager.BackFromSaltShop();
        }
    }

    private void OnCloseSign()
    {
        HideBuyPopup();
        if (shopManager != null)
        {
            shopManager.CloseSaltShop();
        }
    }
}
