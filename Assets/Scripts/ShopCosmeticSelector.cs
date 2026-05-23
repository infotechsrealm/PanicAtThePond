using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles runtime image swapping for cosmetic items inside the shop.
/// Changes the background element image (e.g., BoxSelected vs BoxUnselected)
/// when the corresponding cosmetic item is clicked.
/// </summary>
public class ShopCosmeticSelector : MonoBehaviour
{
    [Serializable]
    public class CosmeticElement
    {
        [Tooltip("The parent/background element GameObject whose Image component will be swapped (e.g., Elements Yellow hat).")]
        public GameObject elementObject;

        [Tooltip("The cosmetic item button or interactive object inside this element (e.g., Yellow hat).")]
        public Button cosmeticButton;

        [HideInInspector]
        public Image elementImage;
    }

    [Header("Cosmetic Elements Configuration")]
    [SerializeField] private List<CosmeticElement> cosmeticElements = new List<CosmeticElement>();

    [Header("Sprite Selection Assets")]
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite unselectedSprite;

    private CosmeticElement currentlySelectedElement;

    private void Awake()
    {
        InitializeVariables();
    }

    /// <summary>
    /// Structure Part 1: Initialize variables
    /// Caches the Image components and binds click event listeners to the buttons dynamically.
    /// </summary>
    private void InitializeVariables()
    {
        foreach (var element in cosmeticElements)
        {
            if (element.elementObject != null)
            {
                element.elementImage = element.elementObject.GetComponent<Image>();
                if (element.elementImage == null)
                {
                    Debug.LogWarning($"[ShopCosmeticSelector] No Image component found on {element.elementObject.name}.");
                }
            }

            if (element.cosmeticButton != null)
            {
                // Capture the element in a local variable for the lambda closure
                var localElement = element;
                element.cosmeticButton.onClick.AddListener(() => OnCosmeticSelected(localElement));
            }
            else if (element.elementObject != null)
            {
                // Fallback: Check if the element itself has a Button component
                Button btn = element.elementObject.GetComponent<Button>();
                if (btn == null)
                {
                    // Fallback: Check children for Button component
                    btn = element.elementObject.GetComponentInChildren<Button>(true);
                }

                if (btn != null)
                {
                    element.cosmeticButton = btn;
                    var localElement = element;
                    btn.onClick.AddListener(() => OnCosmeticSelected(localElement));
                }
            }
        }
    }

    /// <summary>
    /// Structure Part 2: Define selection logic
    /// Handles selected state changes by reverting previous selection and applying new selection.
    /// </summary>
    /// <param name="selectedElement">The cosmetic element that was clicked.</param>
    private void OnCosmeticSelected(CosmeticElement selectedElement)
    {
        if (selectedElement == null) return;

        Debug.Log($"[ShopCosmeticSelector] Selected cosmetic item: {selectedElement.elementObject.name}");

        // When another cosmetic item is clicked, reassign the previously selected element's image source to BoxUnselected
        if (currentlySelectedElement != null && currentlySelectedElement != selectedElement)
        {
            UpdateImageSource(currentlySelectedElement, unselectedSprite);
        }

        // When a user clicks on a cosmetic item, update the image source of the corresponding element to BoxSelected
        currentlySelectedElement = selectedElement;
        UpdateImageSource(currentlySelectedElement, selectedSprite);
    }

    /// <summary>
    /// Structure Part 3: Update image source
    /// Directly modifies the Image sprite of the background element, avoiding direct modification of the cosmetic item itself.
    /// </summary>
    /// <param name="element">The cosmetic element to update.</param>
    /// <param name="newSprite">The sprite to apply.</param>
    private void UpdateImageSource(CosmeticElement element, Sprite newSprite)
    {
        if (element != null && element.elementImage != null && newSprite != null)
        {
            element.elementImage.sprite = newSprite;
            element.elementImage.preserveAspect = false;
            Debug.Log($"[ShopCosmeticSelector] Changed element image of '{element.elementObject.name}' to '{newSprite.name}'");
        }
    }
}
