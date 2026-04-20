using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class AutoBlockRaycastOnInputClick : MonoBehaviour
{
    [SerializeField] private TMP_InputField[] tmpInputFields;
    [SerializeField] private InputField[] standardInputFields;
    [SerializeField] private bool debugMode = true;
    
    private Graphic[] allGraphics;
    private CanvasGroup[] allCanvasGroups;
    private Dictionary<Graphic, bool> originalRaycastState = new Dictionary<Graphic, bool>();
    private Dictionary<CanvasGroup, bool> originalBlocksRaycastState = new Dictionary<CanvasGroup, bool>();
    
    private GameObject currentlySelectedInputField = null;
    
    void Start()
    {
        // Cache all graphics in scene
        allGraphics = FindObjectsByType<Graphic>(FindObjectsSortMode.None);
        allCanvasGroups = FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None);
        
        // Store original states
        foreach (Graphic g in allGraphics)
        {
            originalRaycastState[g] = g.raycastTarget;
        }
        
        foreach (CanvasGroup cg in allCanvasGroups)
        {
            originalBlocksRaycastState[cg] = cg.blocksRaycasts;
        }
        
        // Add listeners to all input fields
        AddListenersToInputFields();
        
        if (debugMode)
            Debug.Log("✓ AutoBlockRaycastOnInputClick initialized");
    }
    
    void Update()
    {
        // Check if an input field is currently selected
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
        {
            GameObject selected = eventSystem.currentSelectedGameObject;
            
            // Check if it's a TMP_InputField
            TMP_InputField tmpInput = selected.GetComponent<TMP_InputField>();
            if (tmpInput != null && currentlySelectedInputField != selected)
            {
                OnInputFieldSelected(selected);
                currentlySelectedInputField = selected;
            }
            
            // Check if it's a standard InputField
            InputField stdInput = selected.GetComponent<InputField>();
            if (stdInput != null && currentlySelectedInputField != selected)
            {
                OnInputFieldSelected(selected);
                currentlySelectedInputField = selected;
            }
        }
        else
        {
            // No input field selected
            if (currentlySelectedInputField != null)
            {
                OnInputFieldDeselected();
                currentlySelectedInputField = null;
            }
        }
    }
    
    void AddListenersToInputFields()
    {
        // TMP_InputField listeners
        if (tmpInputFields != null)
        {
            foreach (TMP_InputField inputField in tmpInputFields)
            {
                if (inputField == null) continue;
                
                // On end edit (user finishes typing)
                inputField.onEndEdit.AddListener((value) => OnInputFieldDeselected());
            }
        }
        
        // Standard InputField listeners
        if (standardInputFields != null)
        {
            foreach (InputField inputField in standardInputFields)
            {
                if (inputField == null) continue;
                
                // On end edit (user finishes typing)
                inputField.onEndEdit.AddListener((value) => OnInputFieldDeselected());
            }
        }
        
        if (debugMode)
            Debug.Log("✓ Input field listeners added");
    }
    
    // Called when input field is clicked/selected
    private void OnInputFieldSelected(GameObject selectedInputField)
    {
        if (selectedInputField == null) return;
        
        if (debugMode)
            Debug.Log($"\n=== INPUT FIELD SELECTED: {selectedInputField.name} ===");
        
        // Disable raycast on everything EXCEPT the selected input field
        DisableRaycastExcept(selectedInputField);
    }
    
    // Called when user clicks elsewhere (deselects input field)
    private void OnInputFieldDeselected()
    {
        if (debugMode)
            Debug.Log("\n=== INPUT FIELD DESELECTED - Restoring Raycasts ===");
        
        // Restore original states
        RestoreAllRaycasts();
    }
    
    // Disable raycast on all objects except the selected input field
    private void DisableRaycastExcept(GameObject selectedInputField)
    {
        if (debugMode)
            Debug.Log($"Disabling raycast on all objects except: {selectedInputField.name}");
        
        int disabledCount = 0;
        int blockedCanvasGroups = 0;
        
        // Disable raycast target on graphics
        foreach (Graphic g in allGraphics)
        {
            if (g == null) continue;
            
            // SKIP the selected input field and its children
            if (IsPartOfInputField(g.gameObject, selectedInputField))
            {
                continue; // Keep this enabled
            }
            
            // SKIP buttons and interactive elements
            if (g.GetComponent<Button>() != null)
            {
                continue; // Keep buttons enabled
            }
            
            // SKIP other input fields
            if (g.GetComponent<TMP_InputField>() != null)
            {
                continue;
            }
            if (g.GetComponent<InputField>() != null)
            {
                continue;
            }
            
            // DISABLE raycast on everything else
            if (g.raycastTarget)
            {
                g.raycastTarget = false;
                disabledCount++;
                
                if (debugMode)
                    Debug.Log($"  ✓ Disabled Raycast: {g.gameObject.name} ({g.GetType().Name})");
            }
        }
        
        // Disable BlocksRaycasts on CanvasGroups
        foreach (CanvasGroup cg in allCanvasGroups)
        {
            if (cg == null) continue;
            
            // Skip if this canvas group is part of the selected input
            if (IsPartOfInputField(cg.gameObject, selectedInputField))
            {
                continue;
            }
            
            if (cg.blocksRaycasts)
            {
                cg.blocksRaycasts = false;
                blockedCanvasGroups++;
                
                if (debugMode)
                    Debug.Log($"  ✓ Disabled CanvasGroup BlocksRaycasts: {cg.gameObject.name}");
            }
        }
        
        if (debugMode)
            Debug.Log($"\n=== BLOCKING COMPLETE ===\n" +
                      $"Disabled {disabledCount} Graphic Raycast Targets\n" +
                      $"Disabled {blockedCanvasGroups} CanvasGroup BlocksRaycasts\n" +
                      $"Input field '{selectedInputField.name}' is now exclusive focus\n");
    }
    
    // Restore all raycasts to original state
    private void RestoreAllRaycasts()
    {
        int restoredGraphics = 0;
        int restoredCanvasGroups = 0;
        
        // Restore graphics
        foreach (Graphic g in allGraphics)
        {
            if (g == null) continue;
            
            if (originalRaycastState.ContainsKey(g))
            {
                if (g.raycastTarget != originalRaycastState[g])
                {
                    g.raycastTarget = originalRaycastState[g];
                    restoredGraphics++;
                }
            }
        }
        
        // Restore CanvasGroups
        foreach (CanvasGroup cg in allCanvasGroups)
        {
            if (cg == null) continue;
            
            if (originalBlocksRaycastState.ContainsKey(cg))
            {
                if (cg.blocksRaycasts != originalBlocksRaycastState[cg])
                {
                    cg.blocksRaycasts = originalBlocksRaycastState[cg];
                    restoredCanvasGroups++;
                }
            }
        }
        
        if (debugMode)
            Debug.Log($"Restored {restoredGraphics} Graphics and {restoredCanvasGroups} CanvasGroups\n");
    }
    
    // Check if a game object is part of an input field (including children)
    private bool IsPartOfInputField(GameObject obj, GameObject inputField)
    {
        // Check if obj is the input field itself
        if (obj == inputField) return true;
        
        // Check if obj is a child of the input field
        Transform current = obj.transform;
        while (current != null)
        {
            if (current.gameObject == inputField)
            {
                return true;
            }
            current = current.parent;
        }
        
        return false;
    }
    
    // Manual method to check which objects are blocking
    public void DebugBlockingObjects(GameObject inputField)
    {
        Debug.Log($"\n=== OBJECTS THAT WOULD BE BLOCKED FOR: {inputField.name} ===\n");
        
        int count = 0;
        foreach (Graphic g in allGraphics)
        {
            if (g == null) continue;
            
            // Skip input field itself and children
            if (IsPartOfInputField(g.gameObject, inputField))
                continue;
            
            // Skip buttons
            if (g.GetComponent<Button>() != null)
                continue;
            
            // Skip other inputs
            if (g.GetComponent<TMP_InputField>() != null || g.GetComponent<InputField>() != null)
                continue;
            
            if (g.raycastTarget)
            {
                count++;
                Debug.Log($"[{count}] {g.gameObject.name} ({g.GetType().Name})");
            }
        }
        
        Debug.Log($"\nTotal objects that will be blocked: {count}");
    }
    
    // Auto-populate input field arrays
    public void AutoPopulateInputFields()
    {
        tmpInputFields = FindObjectsByType<TMP_InputField>(FindObjectsSortMode.None);
        standardInputFields = FindObjectsByType<InputField>(FindObjectsSortMode.None);
        
        Debug.Log($"Auto-populated {tmpInputFields.Length} TMP_InputFields and {standardInputFields.Length} standard InputFields");
        
        // Reinitialize
        Start();
    }
}