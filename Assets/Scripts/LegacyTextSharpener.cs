using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LegacyTextSharpener : MonoBehaviour
{
    private const string OverlayName = "__SharpTMP";
    private const string FontAssetName = "BebasNeue Book SDF";

    private static TMP_FontAsset cachedFontAsset;

    private Text sourceText;
    private TextMeshProUGUI overlayText;
    private float visibleAlpha = 1f;
    private bool forceBold;

    public static void EnsureSceneTextIsSharp()
    {
        Canvas.ForceUpdateCanvases();
        CacheFontAsset();

        Text[] sceneTexts = Resources.FindObjectsOfTypeAll<Text>();

        for (int i = 0; i < sceneTexts.Length; i++)
        {
            Text text = sceneTexts[i];
            if (text == null || !IsLoadedSceneObject(text.gameObject))
            {
                continue;
            }

            if (IsDropdownTemplateText(text))
            {
                continue;
            }

            EnsureOverlay(text);
        }

        EnsureDropdownWatchers();
    }

    // Sharpen every Legacy Text under a subtree (used for runtime-spawned UI such
    // as an open Dropdown List, whose option items don't exist at scene-load time).
    public static void SharpenSubtree(Transform root, bool forceBold = false)
    {
        if (root == null)
        {
            return;
        }

        CacheFontAsset();

        Text[] texts = root.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            Text text = texts[i];
            if (text == null || !text.gameObject.activeInHierarchy)
            {
                continue;
            }

            EnsureOverlay(text, forceBold);
        }
    }

    // Dropdowns build their option list at runtime, so attach a tiny watcher to each
    // one that sharpens the "Dropdown List" items the moment the list opens.
    private static void EnsureDropdownWatchers()
    {
        Dropdown[] dropdowns = Resources.FindObjectsOfTypeAll<Dropdown>();
        for (int i = 0; i < dropdowns.Length; i++)
        {
            Dropdown dropdown = dropdowns[i];
            if (dropdown == null || !IsLoadedSceneObject(dropdown.gameObject))
            {
                continue;
            }

            if (dropdown.GetComponent<DropdownListSharpener>() == null)
            {
                dropdown.gameObject.AddComponent<DropdownListSharpener>();
            }
        }
    }

    private static bool IsLoadedSceneObject(GameObject target)
    {
        Scene scene = target.scene;
        return scene.IsValid() && scene.isLoaded;
    }

    // Labels inside a Dropdown's Template are cloned once per option at runtime.
    // Overlaying them freezes every option at the template default ("Option A")
    // and hides the real label, so leave those to the Dropdown to render itself.
    private static bool IsDropdownTemplateText(Text text)
    {
        Dropdown dropdown = text.GetComponentInParent<Dropdown>(true);
        return dropdown != null
            && dropdown.template != null
            && text.transform.IsChildOf(dropdown.template);
    }

    private static void CacheFontAsset()
    {
        if (cachedFontAsset != null)
        {
            return;
        }

        TMP_FontAsset[] fontAssets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();

        // Prefer the exact font asset the project uses: "BebasNeue Book SDF".
        for (int i = 0; i < fontAssets.Length; i++)
        {
            if (fontAssets[i] != null && fontAssets[i].name == FontAssetName)
            {
                cachedFontAsset = fontAssets[i];
                return;
            }
        }

        // Fallback: any BebasNeue SDF font already loaded in memory.
        for (int i = 0; i < fontAssets.Length; i++)
        {
            if (fontAssets[i] != null && fontAssets[i].name.Contains("BebasNeue"))
            {
                cachedFontAsset = fontAssets[i];
                return;
            }
        }

        // Not loaded yet: leave cache empty so a later call can still find it,
        // and use the TMP default for this pass (handled in SyncNow).
        Debug.LogWarning("[LegacyTextSharpener] Font asset '" + FontAssetName +
            "' is not loaded yet; using TMP default for now.");
    }

    private static void EnsureOverlay(Text source, bool forceBold = false)
    {
        LegacyTextSharpener existing = source.GetComponentInChildren<LegacyTextSharpener>(true);
        if (existing != null && existing.sourceText == source)
        {
            existing.forceBold = existing.forceBold || forceBold;
            existing.SyncNow();
            return;
        }

        GameObject overlayObject = new GameObject(OverlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LegacyTextSharpener));
        overlayObject.layer = source.gameObject.layer;
        overlayObject.transform.SetParent(source.transform, false);
        overlayObject.transform.SetAsLastSibling();

        RectTransform rect = overlayObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = source.rectTransform.pivot;

        LegacyTextSharpener sharpener = overlayObject.GetComponent<LegacyTextSharpener>();
        sharpener.sourceText = source;
        sharpener.overlayText = overlayObject.GetComponent<TextMeshProUGUI>();
        sharpener.forceBold = forceBold;
        sharpener.SyncNow();
    }

    private void LateUpdate()
    {
        SyncNow();
    }

    private void SyncNow()
    {
        if (sourceText == null || overlayText == null)
        {
            return;
        }

        Color sourceColor = sourceText.color;
        if (sourceColor.a > 0f)
        {
            visibleAlpha = sourceColor.a;
        }

        overlayText.text = sourceText.text;
        overlayText.color = new Color(sourceColor.r, sourceColor.g, sourceColor.b, visibleAlpha);
        overlayText.raycastTarget = false;
        overlayText.maskable = sourceText.maskable;
        overlayText.richText = sourceText.supportRichText;
        overlayText.font = cachedFontAsset != null ? cachedFontAsset : TMP_Settings.defaultFontAsset;
        overlayText.fontSize = sourceText.fontSize;
        overlayText.enableAutoSizing = sourceText.resizeTextForBestFit;
        overlayText.fontSizeMin = sourceText.resizeTextMinSize;
        overlayText.fontSizeMax = sourceText.resizeTextMaxSize;
        overlayText.alignment = GetAlignment(sourceText.alignment);
        FontStyles fontStyle = GetFontStyle(sourceText.fontStyle);
        overlayText.fontStyle = forceBold ? fontStyle | FontStyles.Bold : fontStyle;
        overlayText.textWrappingMode = sourceText.horizontalOverflow == HorizontalWrapMode.Wrap
            ? TextWrappingModes.Normal
            : TextWrappingModes.NoWrap;
        overlayText.overflowMode = sourceText.verticalOverflow == VerticalWrapMode.Overflow
            ? TextOverflowModes.Overflow
            : TextOverflowModes.Truncate;
        overlayText.enableWordWrapping = sourceText.horizontalOverflow == HorizontalWrapMode.Wrap;
        overlayText.extraPadding = true;

        sourceText.color = new Color(sourceColor.r, sourceColor.g, sourceColor.b, 0f);
    }

    private static TextAlignmentOptions GetAlignment(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft:
                return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter:
                return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight:
                return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft:
                return TextAlignmentOptions.MidlineLeft;
            case TextAnchor.MiddleRight:
                return TextAlignmentOptions.MidlineRight;
            case TextAnchor.LowerLeft:
                return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter:
                return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight:
                return TextAlignmentOptions.BottomRight;
            default:
                return TextAlignmentOptions.Center;
        }
    }

    private static FontStyles GetFontStyle(FontStyle style)
    {
        switch (style)
        {
            case FontStyle.Bold:
                return FontStyles.Bold;
            case FontStyle.Italic:
                return FontStyles.Italic;
            case FontStyle.BoldAndItalic:
                return FontStyles.Bold | FontStyles.Italic;
            default:
                return FontStyles.Normal;
        }
    }
}

// Sits on each Dropdown and sharpens the runtime-created "Dropdown List" option
// items as soon as the list opens (their labels don't exist until then).
public class DropdownListSharpener : MonoBehaviour
{
    private const string ListName = "Dropdown List";

    private Transform sharpenedList;

    private void Update()
    {
        Transform list = transform.Find(ListName);
        if (list == null)
        {
            sharpenedList = null;
            return;
        }

        if (sharpenedList == list)
        {
            return;
        }

        sharpenedList = list;
        bool forceBold = GetComponentInParent<HostLobby>(true) != null;
        LegacyTextSharpener.SharpenSubtree(list, forceBold);
    }
}
