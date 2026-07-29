using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime-built Sal-T shop front (PDF 1.1.8 "Functionality Of Store").
/// Renders the rotation payload held by SaltShopClientState (which the session authority pushed
/// from shop_config.json) — no prices or item picks are derived locally:
///  - up to <see cref="SaltShopState.visibleSlots"/> still-locked hats laid out on the background
///    shelves, price + coin above each, a padlock on the hat while it is locked;
///  - clicking a locked hat opens the BUY? popup (price, YES / NO) over that hat;
///  - hats the local player already unlocked are skipped, so the shelf is refilled from the rest of
///    the rotation and only shows fewer cells when fewer locked hats remain;
///  - BACK sign returns to the previous page, CLOSE sign returns to the lobby.
/// Attached to the SaltShopPanel by ShopManager at runtime, so no scene rewiring is required.
///
/// COORDINATE SPACE — this is the fix for content spilling off the right of the screen.
/// The scene's "Sal -t Image BackGround" panel is 1265x611 units scaled ~1.8x and pushed +182 to
/// the right, so it measures ~2263x1099 canvas units and its right ~15% sits outside the 1920x1080
/// canvas. Anything anchored in *panel* space past ~0.85 is therefore invisible. So the overlay is
/// built under the ROOT CANVAS at scale 1 instead, and every anchor below is screen-normalized
/// against the 1920x1080 reference resolution — the same space the PDF mockup is drawn in.
/// </summary>
using PanicAtThePond.Managers;
using PanicAtThePond.Controllers;
using PanicAtThePond.Gameplay;
using PanicAtThePond.UI;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.Shop
{
public class SaltShopUI : MonoBehaviour
{
    private const string CoinSpriteResource = "ShopUI/SaltShop/coin";
    private const string LockSpriteResource = "ShopUI/SaltShop/lock";
    private const string BackSignSpriteResource = "ShopUI/SaltShop/back_sign";
    private const string CloseSignSpriteResource = "ShopUI/SaltShop/close_sign";
    private const string Frame1SpriteResource = "ShopUI/SaltShop/picture_frame_1";
    private const string Frame2SpriteResource = "ShopUI/SaltShop/picture_frame_2";

    /// <summary>Reference resolution of the scene's CanvasScaler; also the PDF mockup's resolution.</summary>
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    [Header("Signs (screen-normalized anchor, size in 1920x1080 units)")]
    [Tooltip("Sits directly under the 'sal-T shop' sign painted into the background art.")]
    public Vector2 BackSignAnchor = new Vector2(0.072f, 0.636f);
    [SerializeField] private Vector2 CloseSignAnchor = new Vector2(0.208f, 0.636f);
    [Tooltip("Height of the hanging sign sprites; width follows each sprite's own aspect ratio.")]
    public float SignHeight = 150f;

    [Header("Picture frames (PDF: 'Add in the 2 picture frames')")]
    public Vector2 PictureFrame1Anchor = new Vector2(0.615f, 0.845f);
    [SerializeField] private Vector2 PictureFrame2Anchor = new Vector2(0.775f, 0.845f);
    public float PictureFrameHeight = 220f;

    [Header("Coin balance (top right)")]
    public Vector2 CoinIconAnchor = new Vector2(0.862f, 0.938f);
    [SerializeField] private Vector2 CoinAmountAnchor = new Vector2(0.928f, 0.938f);
    public float CoinIconSize = 64f;

    [Header("Shelf slots — anchor is the CENTRE OF THE HAT on the shelf")]
    // Two hats on the middle shelf, one on the lower shelf, matching the mockup's arrangement and
    // kept inside the part of the shop art that is actually on screen.
    public Vector2[] SlotAnchors =
    {
        new Vector2(0.630f, 0.415f),
        new Vector2(0.860f, 0.415f),
        new Vector2(0.755f, 0.135f)
    };

    [Header("Shelf item metrics (1920x1080 units)")]
    [Tooltip("On-screen height of the hat ARTWORK itself, ignoring the transparent padding in its " +
             "icon PNG. The FisherMan_Hat_* icons are 64x64 files whose art fills only 19-38% of the " +
             "frame, so sizing by the file would draw them far smaller than the fish hats.")]
    [SerializeField] private float HatContentHeight = 105f;
    [Tooltip("On-screen height of the skull padlock artwork. This is what the player clicks to buy.")]
    public float LockContentHeight = 72f;
    [Tooltip("Offset of the coin icon from the hat centre.")]
    public Vector2 ItemCoinOffset = new Vector2(-95f, 120f);
    [Tooltip("Offset of the price label from the hat centre (label is left-aligned).")]
    public Vector2 ItemPriceOffset = new Vector2(15f, 120f);
    [SerializeField] private float ItemCoinSize = 60f;
    public float ItemPriceFontSize = 54f;

    [Header("Buy popup")]
    public Vector2 BuyPopupSize = new Vector2(500f, 430f);

    private ShopManager shopManager;
    private RectTransform canvasRect;
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
        SetOverlayVisible(true);
        CosmeticUnlocks.SyncFromPlayFab(Refresh);
        RefreshCoinBalance();
    }

    private void OnEnable()
    {
        SaltShopClientState.OnShopStateChanged += Refresh;
        CosmeticUnlocks.OnUnlocksChanged += Refresh;
        if (built)
        {
            SetOverlayVisible(true);
            Refresh();
            RefreshCoinBalance();
        }
    }

    private void OnDisable()
    {
        SaltShopClientState.OnShopStateChanged -= Refresh;
        CosmeticUnlocks.OnUnlocksChanged -= Refresh;
        HideBuyPopup();
        SetOverlayVisible(false);
    }

    private void OnDestroy()
    {
        if (overlayRoot != null)
        {
            Destroy(overlayRoot.gameObject);
        }
    }

    private void SetOverlayVisible(bool visible)
    {
        if (overlayRoot == null)
        {
            return;
        }

        overlayRoot.gameObject.SetActive(visible);
        if (visible)
        {
            // Keep the shop front above every other page on the canvas while it is open.
            overlayRoot.SetAsLastSibling();
        }
    }

    // ---------- construction ----------

    private void BuildOnce()
    {
        if (built)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas == null)
        {
            Debug.LogError("[SaltShopUI] No Canvas above the Sal-T shop panel — cannot build the shop front.");
            return;
        }
        canvasRect = (RectTransform)canvas.rootCanvas.transform;

        built = true;

        // Built under the canvas (NOT under the scaled/offset background panel) so that a
        // screen-normalized anchor of 0.86 really is 86% across the screen.
        overlayRoot = CreateStretched("SaltShop Overlay", canvasRect);
        overlayRoot.localScale = Vector3.one;

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

    /// <summary>
    /// Creates a child positioned by a screen-normalized anchor. <paramref name="size"/> is in
    /// 1920x1080 reference units, which is what the overlay's own rect is measured in.
    /// </summary>
    private RectTransform CreateAnchored(string name, RectTransform parent, Vector2 normalizedAnchor, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = normalizedAnchor;
        rect.anchorMax = normalizedAnchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        return rect;
    }

    /// <summary>
    /// Box that fits <paramref name="sprite"/> at exactly <paramref name="height"/> reference units.
    /// Image.preserveAspect only ever shrinks the graphic inside its rect, so sizing the rect to the
    /// sprite's own aspect is what stops the signs/hats rendering smaller than they are configured.
    /// </summary>
    private static Vector2 SizeForSprite(Sprite sprite, float height, float fallbackAspect)
    {
        float aspect = fallbackAspect;
        if (sprite != null && sprite.rect.height > 0f)
        {
            aspect = sprite.rect.width / sprite.rect.height;
        }
        return new Vector2(height * aspect, height);
    }

    /// <summary>
    /// Works out the rect size (and the nudge needed to re-centre it) so that <paramref name="sprite"/>'s
    /// VISIBLE artwork is drawn at exactly <paramref name="contentHeight"/> reference units.
    ///
    /// Image always draws the sprite's whole rect — transparent padding included — so an icon whose
    /// art fills a fifth of its PNG renders a fifth of the size for the same rect. Sprite.bounds is
    /// the tight (trimmed) mesh, which is available without the texture being read/write enabled, so
    /// it tells us how much of the file is actually art. Falls back to a plain aspect fit when the
    /// sprite is imported Full Rect and the bounds are therefore untrimmed.
    /// </summary>
    private static readonly Dictionary<Sprite, Rect> OpaqueBoundsCache = new Dictionary<Sprite, Rect>();

    /// <summary>
    /// Bounding box of the non-transparent pixels of <paramref name="sprite"/>, in sprite-rect pixel
    /// coordinates (origin bottom-left of sprite.rect). Cached — this reads the texture once per
    /// sprite. Requires "Read/Write Enabled" on the icon's importer; returns the full rect when the
    /// texture is not readable, which simply falls back to the old whole-file sizing.
    /// </summary>
    private static Rect GetOpaqueBounds(Sprite sprite)
    {
        Rect cached;
        if (OpaqueBoundsCache.TryGetValue(sprite, out cached))
        {
            return cached;
        }

        Rect full = new Rect(0f, 0f, sprite.rect.width, sprite.rect.height);
        Rect result = full;

        Texture2D texture = sprite.texture;
        if (texture != null && texture.isReadable)
        {
            int x0 = Mathf.FloorToInt(sprite.rect.x);
            int y0 = Mathf.FloorToInt(sprite.rect.y);
            int w = Mathf.FloorToInt(sprite.rect.width);
            int h = Mathf.FloorToInt(sprite.rect.height);

            try
            {
                Color32[] pixels = texture.GetPixels32();
                int texWidth = texture.width;
                int minX = w, minY = h, maxX = -1, maxY = -1;

                for (int y = 0; y < h; y++)
                {
                    int row = (y0 + y) * texWidth + x0;
                    for (int x = 0; x < w; x++)
                    {
                        if (pixels[row + x].a > 8)
                        {
                            if (x < minX) { minX = x; }
                            if (x > maxX) { maxX = x; }
                            if (y < minY) { minY = y; }
                            if (y > maxY) { maxY = y; }
                        }
                    }
                }

                if (maxX >= minX && maxY >= minY)
                {
                    result = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaltShopUI] Could not read '{sprite.name}' to trim it: {e.Message}");
            }
        }
        else if (texture != null)
        {
            Debug.LogWarning($"[SaltShopUI] Icon '{sprite.name}' is not Read/Write Enabled, so its " +
                             "transparent padding cannot be trimmed and it may render undersized.");
        }

        OpaqueBoundsCache[sprite] = result;
        return result;
    }

    private static bool TryGetContentFit(Sprite sprite, float contentHeight, out Vector2 size, out Vector2 offset)
    {
        size = Vector2.zero;
        offset = Vector2.zero;

        if (sprite == null || sprite.rect.width <= 0f || sprite.rect.height <= 0f)
        {
            return false;
        }

        Rect content = GetOpaqueBounds(sprite);
        if (content.width <= 0f || content.height <= 0f)
        {
            return false;
        }

        float fullWidth = sprite.rect.width;
        float fullHeight = sprite.rect.height;
        float scale = contentHeight / content.height;
        size = new Vector2(fullWidth * scale, fullHeight * scale);

        // Image centres the WHOLE sprite rect on the RectTransform, so shift by the gap between the
        // artwork's centre and the file's centre to put the art — not the padding — on the anchor.
        Vector2 artCentre = new Vector2(content.x + content.width * 0.5f, content.y + content.height * 0.5f);
        Vector2 rectCentre = new Vector2(fullWidth * 0.5f, fullHeight * 0.5f);
        offset = -(artCentre - rectCentre) * scale;
        return true;
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
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private void BuildPictureFrames()
    {
        // PDF: "Add in the 2 picture frames" on the wall above the top shelf. They only appear when
        // the art exists in Resources/ShopUI/SaltShop — dropping the PNGs in is enough.
        BuildPictureFrame("Picture Frame 1", Frame1SpriteResource, PictureFrame1Anchor);
        BuildPictureFrame("Picture Frame 2", Frame2SpriteResource, PictureFrame2Anchor);
    }

    private void BuildPictureFrame(string name, string resource, Vector2 anchor)
    {
        Sprite sprite = Resources.Load<Sprite>(resource);
        if (sprite == null)
        {
            return;
        }

        RectTransform rect = CreateAnchored(name, overlayRoot, ClampAnchor(anchor, SizeForSprite(sprite, PictureFrameHeight, 1f)),
            SizeForSprite(sprite, PictureFrameHeight, 1f));
        AddImage(rect, sprite, Color.white).raycastTarget = false;
    }

    private void BuildSigns()
    {
        Sprite backSprite = Resources.Load<Sprite>(BackSignSpriteResource);
        Sprite closeSprite = Resources.Load<Sprite>(CloseSignSpriteResource);

        // Hang under the "sal-T shop" sign painted into the background art, as in the mockup.
        CreateSignButton("Back Sign", BackSignAnchor, backSprite, "BACK", OnBackSign);
        CreateSignButton("Close Sign", CloseSignAnchor, closeSprite, "CLOSE", OnCloseSign);
    }

    private void CreateSignButton(string name, Vector2 anchor, Sprite sprite, string fallbackLabel, UnityEngine.Events.UnityAction onClick)
    {
        Vector2 size = SizeForSprite(sprite, SignHeight, 320f / 210f);
        RectTransform rect = CreateAnchored(name, overlayRoot, ClampAnchor(anchor, size), size);
        Image image = AddImage(rect, sprite, new Color(0.45f, 0.27f, 0.13f, 0.95f));
        image.raycastTarget = true;

        if (sprite == null)
        {
            RectTransform labelRect = CreateStretched("Label", rect);
            AddText(labelRect, fallbackLabel, 40f, new Color(0.95f, 0.87f, 0.7f), TextAlignmentOptions.Center);
        }

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
    }

    private void BuildCoinBalance()
    {
        Sprite coinSprite = Resources.Load<Sprite>(CoinSpriteResource);
        Vector2 coinSize = new Vector2(CoinIconSize, CoinIconSize);
        RectTransform coinRect = CreateAnchored("Coin Icon", overlayRoot, ClampAnchor(CoinIconAnchor, coinSize), coinSize);
        AddImage(coinRect, coinSprite, new Color(1f, 0.8f, 0.1f)).raycastTarget = false;

        Vector2 textSize = new Vector2(180f, 70f);
        RectTransform textRect = CreateAnchored("Coin Amount", overlayRoot, ClampAnchor(CoinAmountAnchor, textSize), textSize);
        coinBalanceText = AddText(textRect, "...", 60f, Color.white, TextAlignmentOptions.Left);
    }

    private void BuildBuyPopup()
    {
        // BUY? popup from the PDF mockup: white card over the hat with price, YES / NO.
        RectTransform popupRect = CreateAnchored("Buy Popup", overlayRoot, new Vector2(0.5f, 0.5f), BuyPopupSize);
        buyPopup = popupRect.gameObject;
        Image background = buyPopup.AddComponent<Image>();
        background.color = Color.white;

        RectTransform titleRect = CreateAnchored("Title", popupRect, new Vector2(0.5f, 0.85f), new Vector2(BuyPopupSize.x - 60f, 80f));
        AddText(titleRect, "BUY?", 62f, new Color(0.15f, 0.15f, 0.15f), TextAlignmentOptions.Center);

        Sprite coinSprite = Resources.Load<Sprite>(CoinSpriteResource);
        RectTransform coinRect = CreateAnchored("Coin", popupRect, new Vector2(0.31f, 0.53f), new Vector2(72f, 72f));
        AddImage(coinRect, coinSprite, new Color(1f, 0.8f, 0.1f)).raycastTarget = false;

        RectTransform priceRect = CreateAnchored("Price", popupRect, new Vector2(0.63f, 0.53f), new Vector2(200f, 80f));
        buyPriceText = AddText(priceRect, "0", 60f, new Color(0.15f, 0.15f, 0.15f), TextAlignmentOptions.Left);

        RectTransform statusRect = CreateAnchored("Status", popupRect, new Vector2(0.5f, 0.33f), new Vector2(BuyPopupSize.x - 40f, 44f));
        buyStatusText = AddText(statusRect, string.Empty, 30f, new Color(0.8f, 0.1f, 0.1f), TextAlignmentOptions.Center);

        CreatePopupButton(popupRect, "Yes Button", new Vector2(0.28f, 0.13f), "YES", new Color(0.1f, 0.65f, 0.25f), OnConfirmPurchase);
        CreatePopupButton(popupRect, "No Button", new Vector2(0.72f, 0.13f), "NO", new Color(0.85f, 0.15f, 0.15f), HideBuyPopup);

        buyPopup.SetActive(false);
    }

    private void CreatePopupButton(RectTransform parent, string name, Vector2 anchor, string label, Color textColor, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = CreateAnchored(name, parent, anchor, new Vector2(150f, 76f));
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.01f); // invisible but clickable, text carries the look

        RectTransform labelRect = CreateStretched("Label", rect);
        AddText(labelRect, label, 52f, textColor, TextAlignmentOptions.Center);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
    }

    // ---------- layout helpers ----------

    /// <summary>
    /// Keeps a rect of <paramref name="size"/> fully inside the canvas. This is the guard that stops
    /// prices and coins being drawn past the right edge of the window.
    /// </summary>
    private static Vector2 ClampAnchor(Vector2 anchor, Vector2 size)
    {
        float halfX = Mathf.Min(0.5f, (size.x * 0.5f) / ReferenceResolution.x);
        float halfY = Mathf.Min(0.5f, (size.y * 0.5f) / ReferenceResolution.y);
        return new Vector2(Mathf.Clamp(anchor.x, halfX, 1f - halfX), Mathf.Clamp(anchor.y, halfY, 1f - halfY));
    }

    /// <summary>Converts an offset in reference units into a screen-normalized anchor delta.</summary>
    private static Vector2 AnchorPlusOffset(Vector2 anchor, Vector2 offset)
    {
        return new Vector2(anchor.x + offset.x / ReferenceResolution.x, anchor.y + offset.y / ReferenceResolution.y);
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

        // PDF: the shop offers hats the player does NOT already own. Walk the authority's ordered
        // rotation and take the first still-locked hats, so owning today's first pick refills the
        // slot from the rest of the rotation instead of leaving a hole. Only when fewer locked hats
        // than slots remain do fewer cells show.
        int slots = Mathf.Clamp(state.visibleSlots, 1, SlotAnchors.Length);
        List<SaltShopState.ShopItem> visible = new List<SaltShopState.ShopItem>();
        foreach (SaltShopState.ShopItem item in state.items)
        {
            if (visible.Count >= slots)
            {
                break;
            }
            if (item != null && !CosmeticUnlocks.IsUnlocked(item.id))
            {
                visible.Add(item);
            }
        }

        if (visible.Count == 0)
        {
            ShowShelfMessage("SOLD OUT");
            return;
        }

        for (int i = 0; i < visible.Count; i++)
        {
            BuildItemCell(visible[i], SlotAnchors[i]);
        }
    }

    private void ShowShelfMessage(string message)
    {
        Vector2 size = new Vector2(520f, 80f);
        RectTransform rect = CreateAnchored("Shelf Message", itemsRoot, ClampAnchor(new Vector2(0.74f, 0.42f), size), size);
        AddText(rect, message, 46f, Color.white, TextAlignmentOptions.Center);
    }

    private void BuildItemCell(SaltShopState.ShopItem item, Vector2 slotAnchor)
    {
        Sprite coinSprite = Resources.Load<Sprite>(CoinSpriteResource);
        Sprite hatSprite = string.IsNullOrEmpty(item.iconResource) ? null : Resources.Load<Sprite>(item.iconResource);
        if (hatSprite == null)
        {
            Debug.LogWarning($"[SaltShopUI] Shop item '{item.id}' has no icon — " +
                             $"Resources.Load<Sprite>(\"{item.iconResource}\") returned null. " +
                             "Check the 'iconResource' path in StreamingAssets/shop_config.json.");
        }
        Sprite lockSprite = Resources.Load<Sprite>(LockSpriteResource);

        // Hat sits ON the shelf at the slot anchor; the price row floats above it, as in the mockup.
        // Sized by its visible artwork so every hat reads the same size regardless of icon padding.
        Vector2 hatSize;
        Vector2 hatOffset;
        if (!TryGetContentFit(hatSprite, HatContentHeight, out hatSize, out hatOffset))
        {
            hatSize = SizeForSprite(hatSprite, HatContentHeight, 1.5f);
            hatOffset = Vector2.zero;
        }
        RectTransform hatRect = CreateAnchored($"Shop Item {item.id}", itemsRoot, slotAnchor, hatSize);
        hatRect.anchoredPosition = hatOffset;
        Image hatImage = AddImage(hatRect, hatSprite, new Color(0.3f, 0.3f, 0.3f));
        // The hat's rect is mostly transparent padding, so it must not swallow clicks — the PDF has
        // the player clicking the padlock ("When the player clicks the lock it will display a buy menu").
        hatImage.raycastTarget = false;

        // Padlock over the hat while it is locked — this is the buy button.
        Vector2 lockSize;
        Vector2 lockOffset;
        if (!TryGetContentFit(lockSprite, LockContentHeight, out lockSize, out lockOffset))
        {
            lockSize = SizeForSprite(lockSprite, LockContentHeight, 185f / 280f);
            lockOffset = Vector2.zero;
        }
        RectTransform lockRect = CreateAnchored("Lock", itemsRoot, ClampAnchor(slotAnchor, lockSize), lockSize);
        lockRect.anchoredPosition = lockOffset;
        Image lockImage = AddImage(lockRect, lockSprite, new Color(0.1f, 0.1f, 0.1f, 0.85f));
        lockImage.raycastTarget = true;

        Vector2 coinSize = new Vector2(ItemCoinSize, ItemCoinSize);
        RectTransform coinRect = CreateAnchored("Coin", itemsRoot,
            ClampAnchor(AnchorPlusOffset(slotAnchor, ItemCoinOffset), coinSize), coinSize);
        AddImage(coinRect, coinSprite, new Color(1f, 0.8f, 0.1f)).raycastTarget = false;

        Vector2 priceSize = new Vector2(190f, 70f);
        RectTransform priceRect = CreateAnchored("Price", itemsRoot,
            ClampAnchor(AnchorPlusOffset(slotAnchor, ItemPriceOffset), priceSize), priceSize);
        AddText(priceRect, item.price.ToString(), ItemPriceFontSize, Color.white, TextAlignmentOptions.Left);

        Button button = lockRect.gameObject.AddComponent<Button>();
        button.targetGraphic = lockImage;
        button.onClick.AddListener(() => ShowBuyPopup(item, slotAnchor));
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

    /// <summary>
    /// PDF: "it will display a buy menu, over the hat they are about to buy". Centres the card on
    /// the hat, biased slightly upward, then clamps so it can never leave the window.
    /// </summary>
    private void PositionBuyPopupOver(Vector2 slotAnchor)
    {
        RectTransform popupRect = (RectTransform)buyPopup.transform;
        Vector2 anchor = AnchorPlusOffset(slotAnchor, new Vector2(0f, BuyPopupSize.y * 0.25f));
        anchor = ClampAnchor(anchor, BuyPopupSize);

        popupRect.anchorMin = anchor;
        popupRect.anchorMax = anchor;
        popupRect.anchoredPosition = Vector2.zero;
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

                if (coinBalanceText != null)
                {
                    coinBalanceText.text = newBalance.ToString();
                }
                HideBuyPopup();
                // Unlock last: it raises OnUnlocksChanged, which refreshes this shelf (and the
                // customization screen's padlocks) once the popup is already closed.
                CosmeticUnlocks.Unlock(item.id);
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

    /// <summary>
    /// Fills in the coin balance. The shop can be opened before PlayFab has finished logging in, in
    /// which case the old early-return left the placeholder "..." on screen forever, so wait for the
    /// login the same way ShopManager.FetchCoinsForShop does.
    /// </summary>
    private void RefreshCoinBalance()
    {
        if (coinBalanceText == null || !isActiveAndEnabled)
        {
            return;
        }

        StopCoroutine(nameof(RefreshCoinBalanceWhenReady));
        StartCoroutine(nameof(RefreshCoinBalanceWhenReady));
    }

    private System.Collections.IEnumerator RefreshCoinBalanceWhenReady()
    {
        const float timeout = 15f;
        float elapsed = 0f;

        while (elapsed < timeout && (PlayFabManager.Instance == null || !PlayFabManager.Instance.IsLoggedIn))
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (PlayFabManager.Instance == null || !PlayFabManager.Instance.IsLoggedIn)
        {
            if (coinBalanceText != null)
            {
                coinBalanceText.text = "0";
            }
            Debug.LogWarning("[SaltShopUI] PlayFab never logged in — showing 0 coins.");
            yield break;
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

}