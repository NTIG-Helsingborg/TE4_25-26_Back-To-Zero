using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple tutorial manager that listens to intermissions and shows/hides UI elements
/// Just add this to a GameObject and configure which intermissions control which UI
/// </summary>
public class SimpleTutorialManager : MonoBehaviour
{
    [System.Serializable]
    public class TutorialStep
    {
        [Tooltip("Which intermission entry index triggers this (0, 1, 2...)")]
        public int intermissionEntryIndex = 0;
        
        [Tooltip("UI elements to SHOW when this intermission plays")]
        public GameObject[] uiToShow;
        
        [Tooltip("UI elements to HIDE when this intermission plays")]
        public GameObject[] uiToHide;
        
        [Tooltip("UI elements to HIGHLIGHT (appear above dark overlay with outline)")]
        public GameObject[] uiToHighlight;
        
        [Tooltip("Open inventory during this intermission?")]
        public bool openInventory = false;
    }
    
    [Header("References")]
    [Tooltip("The IntermissionTextDisplay component to listen to")]
    [SerializeField] private IntermissionTextDisplay intermissionDisplay;
    
    [SerializeField] private GameObject inventoryCanvas;
    
    [Header("Tutorial Steps")]
    [Tooltip("Configure what UI to show/hide for each intermission entry")]
    [SerializeField] private TutorialStep[] tutorialSteps;
    
    [Header("Highlight Settings")]
    [Tooltip("Color of the outline around highlighted objects")]
    [SerializeField] private Color highlightOutlineColor = Color.yellow;
    
    [Tooltip("Size of the highlight outline")]
    [SerializeField] private Vector2 highlightOutlineSize = new Vector2(5, 5);
    
    private Dictionary<string, TutorialStep> stepsByName = new Dictionary<string, TutorialStep>();
    private TutorialStep currentStep;
    private List<HighlightData> activeHighlights = new List<HighlightData>();
    
    private class HighlightData
    {
        public GameObject target;
        public Canvas canvas;
        public int originalSortOrder;
        public UnityEngine.UI.Outline outline;
    }
    
    void Start()
    {
        // Auto-find inventory if not set
        if (inventoryCanvas == null)
        {
            inventoryCanvas = GameObject.Find("InventoryCanvas 1");
        }
        
        // Auto-find intermission display if not set
        if (intermissionDisplay == null)
        {
            intermissionDisplay = GetComponent<IntermissionTextDisplay>();
            if (intermissionDisplay == null)
            {
                intermissionDisplay = FindObjectOfType<IntermissionTextDisplay>();
            }
        }
        
        Debug.Log($"[SimpleTutorialManager] Initialized with {(tutorialSteps != null ? tutorialSteps.Length : 0)} tutorial steps");
    }
    
    /// <summary>
    /// Call this when an intermission entry starts
    /// </summary>
    public void OnIntermissionEntryStart(int entryIndex)
    {
        if (tutorialSteps == null) return;
        
        // Find matching tutorial step
        foreach (var step in tutorialSteps)
        {
            if (step.intermissionEntryIndex == entryIndex)
            {
                currentStep = step;
                ApplyStep(step);
                break;
            }
        }
    }
    
    /// <summary>
    /// Call this when an intermission entry ends
    /// </summary>
    public void OnIntermissionEntryEnd(int entryIndex)
    {
        if (currentStep != null && currentStep.intermissionEntryIndex == entryIndex)
        {
            RevertStep(currentStep);
            currentStep = null;
        }
    }
    
    private void ApplyStep(TutorialStep step)
    {
        // Show UI elements
        if (step.uiToShow != null)
        {
            foreach (var ui in step.uiToShow)
            {
                if (ui != null && !ui.activeSelf)
                {
                    ui.SetActive(true);
                    Debug.Log($"[SimpleTutorialManager] Showed: {ui.name}");
                }
            }
        }
        
        // Hide UI elements
        if (step.uiToHide != null)
        {
            foreach (var ui in step.uiToHide)
            {
                if (ui != null && ui.activeSelf)
                {
                    ui.SetActive(false);
                    Debug.Log($"[SimpleTutorialManager] Hid: {ui.name}");
                }
            }
        }
        
        // Highlight UI elements (bring to front with outline)
        if (step.uiToHighlight != null)
        {
            foreach (var ui in step.uiToHighlight)
            {
                if (ui != null)
                {
                    HighlightObject(ui);
                }
            }
        }
        
        // Open inventory
        if (step.openInventory && inventoryCanvas != null)
        {
            if (!inventoryCanvas.activeSelf)
            {
                inventoryCanvas.SetActive(true);
                Debug.Log("[SimpleTutorialManager] Opened inventory");
            }
        }
    }
    
    private void RevertStep(TutorialStep step)
    {
        // Remove all highlights
        RemoveAllHighlights();
        
        // Hide what we showed
        if (step.uiToShow != null)
        {
            foreach (var ui in step.uiToShow)
            {
                if (ui != null && ui.activeSelf)
                {
                    ui.SetActive(false);
                }
            }
        }
        
        // Show what we hid
        if (step.uiToHide != null)
        {
            foreach (var ui in step.uiToHide)
            {
                if (ui != null && !ui.activeSelf)
                {
                    ui.SetActive(true);
                }
            }
        }
        
        // Close inventory if we opened it
        if (step.openInventory && inventoryCanvas != null)
        {
            if (inventoryCanvas.activeSelf)
            {
                inventoryCanvas.SetActive(false);
                Debug.Log("[SimpleTutorialManager] Closed inventory");
            }
        }
    }
    
    /// <summary>
    /// Highlights a UI object by bringing it above the dark overlay with an outline
    /// </summary>
    private void HighlightObject(GameObject targetObject)
    {
        if (targetObject == null) return;
        
        HighlightData data = new HighlightData();
        data.target = targetObject;
        
        // Find the canvas (either on object or parent)
        Canvas canvas = targetObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = targetObject.GetComponentInParent<Canvas>();
        }
        
        if (canvas != null)
        {
            data.canvas = canvas;
            data.originalSortOrder = canvas.sortingOrder;
            // Set to 999 to appear above dark overlay (which is usually ~100)
            canvas.sortingOrder = 999;
            Debug.Log($"[SimpleTutorialManager] Set {targetObject.name} canvas to sort order 999");
        }
        
        // Add yellow outline
        UnityEngine.UI.Outline outline = targetObject.GetComponent<UnityEngine.UI.Outline>();
        if (outline == null)
        {
            outline = targetObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = highlightOutlineColor;
            outline.effectDistance = highlightOutlineSize;
            data.outline = outline;
            Debug.Log($"[SimpleTutorialManager] Added outline to {targetObject.name}");
        }
        
        activeHighlights.Add(data);
    }
    
    /// <summary>
    /// Removes all active highlights
    /// </summary>
    private void RemoveAllHighlights()
    {
        foreach (var data in activeHighlights)
        {
            if (data.canvas != null)
            {
                data.canvas.sortingOrder = data.originalSortOrder;
            }
            
            if (data.outline != null)
            {
                Destroy(data.outline);
            }
        }
        
        activeHighlights.Clear();
        Debug.Log("[SimpleTutorialManager] Removed all highlights");
    }
}

