using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tutorial manager with dedicated canvas - UI elements are moved to a separate tutorial canvas
/// that is never touched by intermission, eliminating all timing and alpha conflicts
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
    private Coroutine currentStepCoroutine;
    private Dictionary<GameObject, bool> originalUIStates = new Dictionary<GameObject, bool>(); // Track original active states
    private Dictionary<GameObject, Transform> originalParents = new Dictionary<GameObject, Transform>(); // Track original parents
    
    // Dedicated tutorial canvas - separate from game canvas, never touched by intermission
    private Canvas tutorialCanvas;
    private const int TUTORIAL_CANVAS_SORT_ORDER = 1000;
    
    private class HighlightData
    {
        public GameObject target;
        public Canvas canvas;
        public bool canvasWasAdded;
        public UnityEngine.UI.GraphicRaycaster raycaster;
        public bool raycasterWasAdded;
        public int originalSortOrder;
        public bool originalOverrideSorting;
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
        
        // Create or find dedicated tutorial canvas
        CreateTutorialCanvas();
        
        Debug.Log($"[SimpleTutorialManager] Initialized with {(tutorialSteps != null ? tutorialSteps.Length : 0)} tutorial steps");
    }
    
    /// <summary>
    /// Creates a dedicated tutorial canvas that sits above everything and is never touched by intermission
    /// </summary>
    private void CreateTutorialCanvas()
    {
        // Try to find existing tutorial canvas first
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas c in allCanvases)
        {
            if (c.gameObject.name == "TutorialCanvas")
            {
                tutorialCanvas = c;
                Debug.Log("[SimpleTutorialManager] Found existing TutorialCanvas");
                break;
            }
        }
        
        // Create one if not found
        if (tutorialCanvas == null)
        {
            GameObject canvasObj = new GameObject("TutorialCanvas");
            tutorialCanvas = canvasObj.AddComponent<Canvas>();
            tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tutorialCanvas.sortingOrder = TUTORIAL_CANVAS_SORT_ORDER;
            
            // Add raycaster for interaction
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Add CanvasScaler for resolution independence
            UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            Debug.Log("[SimpleTutorialManager] Created new TutorialCanvas with sort order " + TUTORIAL_CANVAS_SORT_ORDER);
        }
        
        // Ensure it has the correct sort order
        tutorialCanvas.sortingOrder = TUTORIAL_CANVAS_SORT_ORDER;
        tutorialCanvas.gameObject.SetActive(true);
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
                // Cancel any pending step application
                if (currentStepCoroutine != null)
                {
                    StopCoroutine(currentStepCoroutine);
                }
                
                currentStep = step;
                // Apply step with delay so intermission overlay appears first
                currentStepCoroutine = StartCoroutine(ApplyStepWithDelay(step, 0.1f));
                break;
            }
        }
    }
    
    /// <summary>
    /// Call this when an intermission entry ends
    /// </summary>
    public void OnIntermissionEntryEnd(int entryIndex)
    {
        Debug.Log($"[SimpleTutorialManager] OnIntermissionEntryEnd called for entry {entryIndex}");
        
        if (currentStep != null && currentStep.intermissionEntryIndex == entryIndex)
        {
            Debug.Log($"[SimpleTutorialManager] Reverting step for entry {entryIndex}");
            
            // If step is still waiting to be applied (coroutine active), just cancel it
            if (currentStepCoroutine != null)
            {
                Debug.Log($"[SimpleTutorialManager] Step not applied yet - canceling application (no flash)");
                StopCoroutine(currentStepCoroutine);
                currentStepCoroutine = null;
                currentStep = null;
                
                // Still force-hide intermission
                if (intermissionDisplay != null)
                {
                    intermissionDisplay.ForceHideInstant();
                }
                return;
            }
            
            // CRITICAL 3-STEP ORDER:
            // 1. Hide tutorial UI instantly (on TutorialCanvas)
            // 2. Restore canvas alpha to full (so UI will be visible)
            // 3. Move UI back and show it (now visible because canvas alpha is restored)
            
            // Step 1: Hide UI on TutorialCanvas
            HideUIOnTutorialCanvas();
            
            // Step 2: Restore canvas FIRST so UI will be visible when moved
            if (intermissionDisplay != null)
            {
                Debug.Log($"[SimpleTutorialManager] Restoring canvas BEFORE showing UI");
                intermissionDisplay.ForceHideInstant();
            }
            
            // Step 3: Move UI back and restore visibility
            MoveUIBackAndRestore(currentStep);
            currentStep = null;
        }
        else
        {
            Debug.Log($"[SimpleTutorialManager] No matching step to revert (currentStep={(currentStep != null ? currentStep.intermissionEntryIndex.ToString() : "null")})");
        }
    }
    
    /// <summary>
    /// Hide all UI that was moved to TutorialCanvas
    /// </summary>
    private void HideUIOnTutorialCanvas()
    {
        foreach (var kvp in originalParents)
        {
            GameObject ui = kvp.Key;
            if (ui != null && ui.activeSelf)
            {
                ui.SetActive(false);
                Debug.Log($"[SimpleTutorialManager] Pre-hiding {ui.name}");
            }
        }
        RemoveAllHighlights();
    }
    
    /// <summary>
    /// Move UI back to original parents and restore visibility
    /// </summary>
    private void MoveUIBackAndRestore(TutorialStep step)
    {
        // Move back to original parents
        foreach (var kvp in originalParents)
        {
            GameObject ui = kvp.Key;
            Transform originalParent = kvp.Value;
            
            if (ui != null && originalParent != null)
            {
                ui.transform.SetParent(originalParent, true);
                Debug.Log($"[SimpleTutorialManager] Moved {ui.name} back to {originalParent.name}");
            }
        }
        
        // Restore visibility (canvas is now at full alpha)
        foreach (var kvp in originalUIStates)
        {
            GameObject ui = kvp.Key;
            bool originalState = kvp.Value;
            
            if (ui != null && ui.activeSelf != originalState)
            {
                ui.SetActive(originalState);
                Debug.Log($"[SimpleTutorialManager] Restored {ui.name} to {(originalState ? "visible" : "hidden")}");
            }
        }
        
        originalUIStates.Clear();
        originalParents.Clear();
    }
    
    /// <summary>
    /// Apply step with a small delay to ensure intermission overlay appears first
    /// </summary>
    private System.Collections.IEnumerator ApplyStepWithDelay(TutorialStep step, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ApplyStep(step);
        currentStepCoroutine = null; // Clear reference after applying
    }
    
    private void ApplyStep(TutorialStep step)
    {
        // Clear any previous state tracking
        originalUIStates.Clear();
        originalParents.Clear();
        
        // VALIDATION: Detect UI elements in both show and hide lists
        if (step.uiToShow != null && step.uiToHide != null)
        {
            foreach (var showUI in step.uiToShow)
            {
                if (showUI != null)
                {
                    foreach (var hideUI in step.uiToHide)
                    {
                        if (hideUI == showUI)
                        {
                            Debug.LogError($"[SimpleTutorialManager] CONFLICT: '{showUI.name}' is in BOTH uiToShow and uiToHide! This will cause issues. Prioritizing SHOW over HIDE.");
                        }
                    }
                }
            }
        }
        
        // Show UI elements - move to tutorial canvas
        if (step.uiToShow != null)
        {
            foreach (var ui in step.uiToShow)
            {
                if (ui != null)
                {
                    // Track original state and parent
                    if (!originalUIStates.ContainsKey(ui))
                    {
                        originalUIStates[ui] = ui.activeSelf;
                        originalParents[ui] = ui.transform.parent;
                    }
                    
                    // Move to tutorial canvas
                    ui.transform.SetParent(tutorialCanvas.transform, true);
                    
                    if (!ui.activeSelf)
                    {
                        ui.SetActive(true);
                    }
                    
                    Debug.Log($"[SimpleTutorialManager] Moved {ui.name} to TutorialCanvas and showed");
                }
            }
        }
        
        // Hide UI elements (skip if already in show list)
        if (step.uiToHide != null)
        {
            foreach (var ui in step.uiToHide)
            {
                if (ui != null)
                {
                    // Skip if this UI is in the show list
                    bool isInShowList = false;
                    if (step.uiToShow != null)
                    {
                        foreach (var showUI in step.uiToShow)
                        {
                            if (showUI == ui)
                            {
                                isInShowList = true;
                                Debug.LogWarning($"[SimpleTutorialManager] Skipping hide for '{ui.name}' - already in show list");
                                break;
                            }
                        }
                    }
                    
                    if (isInShowList) continue;
                    
                    if (!originalUIStates.ContainsKey(ui))
                    {
                        originalUIStates[ui] = ui.activeSelf;
                    }
                    
                    if (ui.activeSelf)
                    {
                        ui.SetActive(false);
                        Debug.Log($"[SimpleTutorialManager] Hid: {ui.name}");
                    }
                }
            }
        }
        
        // Highlight UI elements - move to tutorial canvas
        if (step.uiToHighlight != null)
        {
            foreach (var ui in step.uiToHighlight)
            {
                if (ui != null)
                {
                    // Track original parent if not already tracked
                    if (!originalParents.ContainsKey(ui))
                    {
                        originalParents[ui] = ui.transform.parent;
                    }
                    
                    // Move to tutorial canvas
                    ui.transform.SetParent(tutorialCanvas.transform, true);
                    
                    HighlightObject(ui);
                    Debug.Log($"[SimpleTutorialManager] Moved {ui.name} to TutorialCanvas and highlighted");
                }
            }
        }
        
        // Open inventory
        if (step.openInventory && inventoryCanvas != null)
        {
            if (!originalUIStates.ContainsKey(inventoryCanvas))
            {
                originalUIStates[inventoryCanvas] = inventoryCanvas.activeSelf;
            }
            
            if (!inventoryCanvas.activeSelf)
            {
                inventoryCanvas.SetActive(true);
                Debug.Log("[SimpleTutorialManager] Opened inventory");
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
        
        // Check if object already has a Canvas
        Canvas canvas = targetObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            // Add Canvas if missing
            canvas = targetObject.AddComponent<Canvas>();
            data.canvasWasAdded = true;
        }
        
        data.canvas = canvas;
        data.originalOverrideSorting = canvas.overrideSorting;
        data.originalSortOrder = canvas.sortingOrder;
        
        // Configure Canvas to sit above everything
        canvas.overrideSorting = true;
        canvas.sortingOrder = 30000; // Very high value to ensure it's on top
        
        // Check for GraphicRaycaster (needed for interaction on new Canvas)
        UnityEngine.UI.GraphicRaycaster raycaster = targetObject.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = targetObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            data.raycasterWasAdded = true;
        }
        data.raycaster = raycaster;
        
        Debug.Log($"[SimpleTutorialManager] Highlighted {targetObject.name} (Canvas added: {data.canvasWasAdded})");
        
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
            if (data.target != null)
            {
                // Remove Outline first
                if (data.outline != null)
                {
                    Destroy(data.outline);
                }
                
                // Destroy Raycaster BEFORE Canvas (dependency order)
                if (data.raycasterWasAdded && data.raycaster != null)
                {
                    Destroy(data.raycaster);
                }
                
                // Now safe to remove Canvas
                if (data.canvasWasAdded && data.canvas != null)
                {
                    Destroy(data.canvas);
                }
                else if (data.canvas != null)
                {
                    // Restore original settings if we didn't add the canvas
                    data.canvas.overrideSorting = data.originalOverrideSorting;
                    data.canvas.sortingOrder = data.originalSortOrder;
                }
            }
        }
        
        activeHighlights.Clear();
        Debug.Log("[SimpleTutorialManager] Removed all highlights");
    }
}
