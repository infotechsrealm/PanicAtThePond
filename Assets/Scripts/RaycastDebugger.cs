using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class RaycastDebugger : MonoBehaviour
{
    [SerializeField] private TMP_InputField[] tmpInputFields; // Array for TMP_InputField
    [SerializeField] private InputField[] standardInputFields; // Array for standard InputField
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool autoDebugOnClick = true;
    
    private GraphicRaycaster raycaster;
    private EventSystem eventSystem;
    
    void Start()
    {
        raycaster = FindFirstObjectByType<GraphicRaycaster>();
        eventSystem = FindFirstObjectByType<EventSystem>();
        
        if (raycaster == null)
            Debug.LogError("RaycastDebugger: No GraphicRaycaster found in scene!");
        if (eventSystem == null)
            Debug.LogError("RaycastDebugger: No EventSystem found in scene!");
        
        Debug.Log("RaycastDebugger initialized. Monitoring TMP_InputFields and InputFields.");
    }
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && autoDebugOnClick)
        {
            DebugAllInputFieldsUnderMouse();
        }
    }
    
    // Debug all input fields in arrays
    public void DebugAllInputFieldsInArrays()
    {
        Debug.Log("\n========== DEBUGGING ALL INPUT FIELDS IN ARRAYS ==========\n");
        
        // Debug TMP_InputFields
        if (tmpInputFields != null && tmpInputFields.Length > 0)
        {
            Debug.Log($"--- TMP_INPUT FIELDS ({tmpInputFields.Length} total) ---\n");
            for (int i = 0; i < tmpInputFields.Length; i++)
            {
                DebugSingleTMPInputField(tmpInputFields[i], i);
            }
        }
        else
        {
            Debug.LogWarning("No TMP_InputFields assigned in array!");
        }
        
        // Debug Standard InputFields
        if (standardInputFields != null && standardInputFields.Length > 0)
        {
            Debug.Log($"\n--- STANDARD INPUT FIELDS ({standardInputFields.Length} total) ---\n");
            for (int i = 0; i < standardInputFields.Length; i++)
            {
                DebugSingleInputField(standardInputFields[i], i);
            }
        }
        
        Debug.Log("\n========== END ARRAY DEBUG ==========\n");
    }
    
    // Debug single TMP_InputField
    private void DebugSingleTMPInputField(TMP_InputField inputField, int index)
    {
        if (inputField == null)
        {
            Debug.LogError($"[{index}] TMP_InputField is NULL!");
            return;
        }
        
        RectTransform inputRect = inputField.GetComponent<RectTransform>();
        Graphic inputGraphic = inputField.GetComponent<Graphic>();
        CanvasGroup inputCanvasGroup = inputField.GetComponentInParent<CanvasGroup>();
        Canvas inputCanvas = inputField.GetComponentInParent<Canvas>();
        
        Debug.Log($"[{index}] {inputField.name}");
        Debug.Log($"     Active: {inputField.gameObject.activeInHierarchy}");
        Debug.Log($"     Position: {inputRect.position}");
        Debug.Log($"     Size: {inputRect.rect.size}");
        Debug.Log($"     Raycast Target: {(inputGraphic != null ? inputGraphic.raycastTarget : "N/A")}");
        Debug.Log($"     Alpha: {(inputGraphic != null ? inputGraphic.color.a : "N/A")}");
        Debug.Log($"     Canvas: {(inputCanvas != null ? inputCanvas.name : "None")}");
        Debug.Log($"     CanvasGroup Blocks: {(inputCanvasGroup != null ? inputCanvasGroup.blocksRaycasts : "N/A")}");
        
        // Check for blockers
        CheckForBlockers(inputField);
        Debug.Log("");
    }
    
    // Debug single standard InputField
    private void DebugSingleInputField(InputField inputField, int index)
    {
        if (inputField == null)
        {
            Debug.LogError($"[{index}] InputField is NULL!");
            return;
        }
        
        RectTransform inputRect = inputField.GetComponent<RectTransform>();
        Graphic inputGraphic = inputField.GetComponent<Graphic>();
        CanvasGroup inputCanvasGroup = inputField.GetComponentInParent<CanvasGroup>();
        Canvas inputCanvas = inputField.GetComponentInParent<Canvas>();
        
        Debug.Log($"[{index}] {inputField.name}");
        Debug.Log($"     Active: {inputField.gameObject.activeInHierarchy}");
        Debug.Log($"     Position: {inputRect.position}");
        Debug.Log($"     Size: {inputRect.rect.size}");
        Debug.Log($"     Raycast Target: {(inputGraphic != null ? inputGraphic.raycastTarget : "N/A")}");
        Debug.Log($"     Alpha: {(inputGraphic != null ? inputGraphic.color.a : "N/A")}");
        Debug.Log($"     Canvas: {(inputCanvas != null ? inputCanvas.name : "None")}");
        Debug.Log($"     CanvasGroup Blocks: {(inputCanvasGroup != null ? inputCanvasGroup.blocksRaycasts : "N/A")}");
        
        // Check for blockers
        CheckForBlockers(inputField);
        Debug.Log("");
    }
    
    // Check what's blocking an input field
    private void CheckForBlockers(Selectable inputField)
    {
        List<string> blockers = new List<string>();
        
        Graphic[] allGraphics = FindObjectsByType<Graphic>(FindObjectsSortMode.None);
        Canvas inputCanvas = inputField.GetComponentInParent<Canvas>();
        
        foreach (Graphic g in allGraphics)
        {
            if (g == inputField.GetComponent<Graphic>()) continue;
            if (!g.raycastTarget) continue;
            if (!g.gameObject.activeInHierarchy) continue;
            
            Canvas gCanvas = g.GetComponentInParent<Canvas>();
            if (gCanvas != inputCanvas) continue;
            
            Image img = g as Image;
            if (img != null && img.color.a < 0.1f) continue;
            
            blockers.Add(g.gameObject.name);
        }
        
        if (blockers.Count > 0)
        {
            Debug.LogWarning($"     ⚠️ POTENTIAL BLOCKERS: {string.Join(", ", blockers)}");
        }
        else
        {
            Debug.Log($"     ✓ No blockers detected");
        }
    }
    
    // Debug all input fields under mouse click
    public void DebugAllInputFieldsUnderMouse()
    {
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };
        
        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);
        
        Debug.Log($"\n=== RAYCAST DEBUG AT MOUSE POSITION {Input.mousePosition} ===");
        Debug.Log($"Total Objects Hit: {results.Count}\n");
        
        if (results.Count == 0)
        {
            Debug.LogWarning("NO RAYCAST HITS! Area is completely blocked or off-screen.");
            return;
        }
        
        for (int i = 0; i < results.Count; i++)
        {
            RaycastResult result = results[i];
            Graphic graphic = result.gameObject.GetComponent<Graphic>();
            CanvasGroup canvasGroup = result.gameObject.GetComponentInParent<CanvasGroup>();
            
            Debug.Log($"[{i}] {result.gameObject.name}");
            Debug.Log($"    Type: {result.gameObject.GetComponent<Graphic>()?.GetType().Name ?? "Unknown"}");
            Debug.Log($"    Raycast Target: {(graphic != null ? graphic.raycastTarget : "N/A")}");
            Debug.Log($"    Alpha: {(graphic != null ? graphic.color.a : "N/A")}");
            Debug.Log($"    CanvasGroup Blocks: {(canvasGroup != null ? canvasGroup.blocksRaycasts : "N/A")}");
            Debug.Log("");
        }
    }
    
    // Auto-populate arrays from scene
    public void AutoPopulateArrays()
    {
        tmpInputFields = FindObjectsByType<TMP_InputField>(FindObjectsSortMode.None);
        standardInputFields = FindObjectsByType<InputField>(FindObjectsSortMode.None);
        
        Debug.Log($"Auto-populated {tmpInputFields.Length} TMP_InputFields and {standardInputFields.Length} standard InputFields");
    }
    
    // Find all input fields in scene (doesn't use arrays)
    public void FindAllInputFieldsInScene()
    {
        Debug.Log("\n========== ALL INPUT FIELDS IN SCENE ==========\n");
        
        TMP_InputField[] allTMP = FindObjectsByType<TMP_InputField>(FindObjectsSortMode.None);
        InputField[] allStandard = FindObjectsByType<InputField>(FindObjectsSortMode.None);
        
        Debug.Log($"TMP_InputFields found: {allTMP.Length}");
        for (int i = 0; i < allTMP.Length; i++)
        {
            Debug.Log($"  [{i}] {allTMP[i].name}");
        }
        
        Debug.Log($"\nStandard InputFields found: {allStandard.Length}");
        for (int i = 0; i < allStandard.Length; i++)
        {
            Debug.Log($"  [{i}] {allStandard[i].name}");
        }
        
        Debug.Log("\n========== END SCENE SCAN ==========\n");
    }
}