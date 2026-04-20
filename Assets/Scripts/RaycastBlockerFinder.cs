using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RaycastBlockerFinder : MonoBehaviour
{
    void Start()
    {
        FindAllRaycastBlockers();
    }
    
    public void FindAllRaycastBlockers()
    {
        Debug.Log("\n========== RAYCAST BLOCKER DETECTION ==========\n");
        
        // Find all Graphic components with raycastTarget enabled
        Graphic[] allGraphics = FindObjectsByType<Graphic>(FindObjectsSortMode.None);
        List<Graphic> raycastTargets = new List<Graphic>();
        
        foreach (Graphic g in allGraphics)
        {
            if (g.raycastTarget && g.gameObject.activeInHierarchy)
            {
                raycastTargets.Add(g);
            }
        }
        
        Debug.Log($"Total Raycast Targets Found: {raycastTargets.Count}\n");
        
        // Sort by canvas depth (render order)
        Debug.Log("--- RAYCAST TARGETS (in render order, last = top) ---\n");
        
        for (int i = 0; i < raycastTargets.Count; i++)
        {
            Graphic g = raycastTargets[i];
            Canvas canvas = g.GetComponentInParent<Canvas>();
            CanvasGroup cg = g.GetComponentInParent<CanvasGroup>();
            
            string canvasInfo = canvas != null ? $"Canvas: {canvas.name}, SortOrder: {canvas.sortingOrder}" : "No Canvas";
            string cgInfo = cg != null ? $"CanvasGroup (Blocks: {cg.blocksRaycasts}, Alpha: {cg.alpha})" : "No CanvasGroup";
            
            Debug.Log($"[{i}] {g.name} | Type: {g.GetType().Name}");
            Debug.Log($"     {canvasInfo}");
            Debug.Log($"     {cgInfo}");
            Debug.Log($"     Alpha: {g.color.a}, Active: {g.gameObject.activeInHierarchy}\n");
        }
        
        // Find Input Fields and check what's blocking them
        Debug.Log("\n--- INPUT FIELDS AND THEIR BLOCKERS ---\n");
        InputField[] allInputFields = FindObjectsByType<InputField>(FindObjectsSortMode.None);
        
        foreach (InputField inputField in allInputFields)
        {
            RectTransform inputRect = inputField.GetComponent<RectTransform>();
            Graphic inputGraphic = inputField.GetComponent<Graphic>();
            
            Debug.Log($"INPUT FIELD: {inputField.name}");
            Debug.Log($"  World Pos: {inputRect.position}");
            Debug.Log($"  Size: {inputRect.rect.size}");
            Debug.Log($"  Raycast Target: {(inputGraphic != null ? inputGraphic.raycastTarget : "N/A")}");
            
            // Check what's in front of it
            List<Graphic> blockingGraphics = new List<Graphic>();
            
            foreach (Graphic g in raycastTargets)
            {
                if (g == inputGraphic) continue; // Skip self
                
                Canvas inputCanvas = inputField.GetComponentInParent<Canvas>();
                Canvas gCanvas = g.GetComponentInParent<Canvas>();
                
                // Simple check: if graphic is after input in hierarchy and raycast enabled
                if (IsGraphicBlockingInput(g, inputField))
                {
                    blockingGraphics.Add(g);
                }
            }
            
            if (blockingGraphics.Count > 0)
            {
                Debug.LogWarning($"  ⚠️ BLOCKED BY {blockingGraphics.Count} OBJECTS:");
                foreach (Graphic g in blockingGraphics)
                {
                    Debug.LogWarning($"     - {g.name} ({g.GetType().Name})");
                }
            }
            else
            {
                Debug.Log($"  ✓ No blocking detected");
            }
            
            Debug.Log("");
        }
        
        // Panel detection
        Debug.Log("\n--- PANELS WITH RAYCAST TARGET ENABLED ---\n");
        Image[] allImages = FindObjectsByType<Image>(FindObjectsSortMode.None);
        
        foreach (Image img in allImages)
        {
            if (img.raycastTarget && img.gameObject.activeInHierarchy)
            {
                RectTransform rect = img.GetComponent<RectTransform>();
                Canvas canvas = img.GetComponentInParent<Canvas>();
                
                Debug.Log($"Panel: {img.gameObject.name}");
                Debug.Log($"  Canvas: {(canvas != null ? canvas.name : "None")}");
                Debug.Log($"  Size: {rect.rect.size}");
                Debug.Log($"  Color Alpha: {img.color.a}");
                Debug.Log($"  Parent: {img.transform.parent.name}\n");
            }
        }
    }
    
    private bool IsGraphicBlockingInput(Graphic graphic, InputField inputField)
    {
        // Check if graphic has raycast enabled
        if (!graphic.raycastTarget) return false;
        
        // Check if graphic is in same canvas or parent canvas
        Canvas gCanvas = graphic.GetComponentInParent<Canvas>();
        Canvas inputCanvas = inputField.GetComponentInParent<Canvas>();
        
        if (gCanvas == null || inputCanvas == null) return false;
        if (gCanvas != inputCanvas) return false;
        
        // Check visibility
        Image img = graphic as Image;
        if (img != null && img.color.a < 0.1f) return false; // Nearly transparent
        
        return true;
    }
}