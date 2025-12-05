using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class IntermissionTextDisplay : MonoBehaviour
{
    [Header("Text Display")]
    [SerializeField] private TextMeshProUGUI textDisplay;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float fontSize = 48f;
    
    [Header("Text Fade Settings")]
    [SerializeField] private bool enableTextFadeIn = true;
    [SerializeField] private float textFadeInDuration = 1f;
    [SerializeField] private float textFadeOutDuration = 0.5f;
    [SerializeField] [Range(0.1f, 5f)] private float textFadeSpeed = 1f; // Speed multiplier (1.0 = normal, <1.0 = slower, >1.0 = faster)
    
    [Header("Timing Settings")]
    [SerializeField] private float displayDuration = 3f;
    
    public enum TriggerType
    {
        None,               // Manual only (via code)
        OnStart,            // When scene starts
        OnCollisionEnter,   // When object collides (requires Collider)
        OnTriggerEnter,     // When trigger is entered (requires Collider + IsTrigger)
        OnCollisionExit,    // When collision ends
        OnTriggerExit,      // When trigger exits
        OnEnable,           // When GameObject becomes active
        AfterEntry          // Play after another entry completes
    }
    
    [System.Serializable]
    public class IntermissionEntry
    {
        [TextArea(2, 5)]
        public string text = "";
        
        public TriggerType triggerType = TriggerType.None;
        public LayerMask triggerLayer = -1; // Which layers can trigger (all by default)
        public GameObject specificColliderObject = null; // Specific GameObject with collider to check for OnCollisionEnter/Exit
        public float triggerDelay = 0f;
        public bool triggerOnce = true; // If true, only triggers once
        public bool startDarkened = false; // If true and trigger is OnStart, starts already darkened
        
        public float customDisplayDuration = -1f; // Custom duration for this entry (-1 = use global setting)
        public bool waitForInput = false; // If true, wait for key press instead of auto-dismissing
        public KeyCode dismissKey = KeyCode.Space; // Key to dismiss intermission (only if waitForInput is true)
        
        public bool enableText = true; // Show text for this entry
        public bool enableOverlay = true; // Show dark overlay for this entry
        public bool enableDarkening = true; // Enable darkening effects (overlay + canvas fade) for this entry
        
        [Range(0f, 1f)]
        public float overlayOpacity = 1f; // Target opacity for the overlay
        
        public bool forceStart = false; // If true, interrupts any currently playing intermission
        
        public bool snappyCanvas = false; // If true, skips fade-in animations
        
        public bool playAfterEntry = false; // If true, plays after another entry completes
        [HideInInspector]
        public int playAfterEntryIndex = 0; // Index of entry to play after (handled by custom drawer)
    }
    
    [Header("Intermission Entries")]
    [SerializeField] private IntermissionEntry[] intermissionEntries = new IntermissionEntry[0];
    
    [Header("Background Darkening")]
    [SerializeField] private Image darkOverlay; // Full-screen UI Image overlay for darkening
        [SerializeField] private Color overlayColor = Color.black;
        [SerializeField] private float overlayFadeInDuration = 1f;
        [SerializeField] private float overlayFadeOutDuration = 0.5f;
    [SerializeField] [Range(0.1f, 5f)] private float overlayFadeSpeed = 1f; // Speed multiplier (1.0 = normal, <1.0 = slower, >1.0 = faster)
    [SerializeField] private Canvas canvas; // Canvas to hide during intermission (optional - hides game UI)
        [SerializeField] private float canvasFadeDuration = 0.5f;
    
    [Header("Game Freeze")]
    [SerializeField] private bool freezeGame = true; // Freeze game during intermission
    
    private Queue<string> messageQueue = new Queue<string>();
    private bool isDisplaying = false;
    private Coroutine currentDisplayCoroutine;
    private Coroutine activeTriggerCoroutine; // Track the active trigger coroutine
    private int currentActiveEntryIndex = -1; // Track the currently active entry index
    private Dictionary<int, bool> entryTriggered = new Dictionary<int, bool>(); // Track which entries have triggered
    private Dictionary<int, bool> entryCompleted = new Dictionary<int, bool>(); // Track which entries have completed
    private bool canvasWasEnabled = false;
    private CanvasGroup canvasGroup = null;
    private float originalCanvasAlpha = 1f;
    
    void Start()
    {
        InitializeTextDisplay();
        InitializeDarkOverlay();
        InitializeCanvas();
        HandleStartDarkened();
        CheckTriggers(TriggerType.OnStart, null, null);
    }
    
    /// <summary>
    /// Initializes the text display component, auto-finding it if not assigned
    /// </summary>
    private void InitializeTextDisplay()
    {
        if (textDisplay == null)
        {
            textDisplay = GetComponent<TextMeshProUGUI>();
            if (textDisplay == null)
            {
                Debug.LogWarning("IntermissionTextDisplay: No TextMeshProUGUI found. Please assign one in the inspector.");
                return;
            }
        }
        
        textDisplay.gameObject.SetActive(true);
        textDisplay.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
        textDisplay.fontSize = fontSize;
        textDisplay.transform.SetAsLastSibling();
    }
    
    /// <summary>
    /// Initializes the dark overlay, auto-finding it if not assigned
    /// </summary>
    private void InitializeDarkOverlay()
    {
        if (darkOverlay == null)
        {
            // Try to find it as a sibling or child of text display
            if (textDisplay != null)
            {
                darkOverlay = textDisplay.transform.parent?.GetComponentInChildren<Image>();
            }
            
            // If still not found, search all canvases
            if (darkOverlay == null)
            {
                Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (Canvas c in allCanvases)
                {
                    Image overlay = c.GetComponentInChildren<Image>();
                    if (overlay != null && overlay != textDisplay?.GetComponent<Image>())
                    {
                        darkOverlay = overlay;
                        break;
                    }
                }
            }
        }
        
        if (darkOverlay != null)
        {
            darkOverlay.gameObject.SetActive(true);
            SetOverlayColor(0f);
            darkOverlay.transform.SetAsFirstSibling();
        }
    }
    
    /// <summary>
    /// Sets the overlay color alpha
    /// </summary>
    private void SetOverlayColor(float alpha)
    {
        if (darkOverlay != null)
        {
            darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, alpha);
        }
    }
    
    /// <summary>
    /// Smooth step interpolation function for easing
    /// </summary>
    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }
    
    /// <summary>
    /// Initializes the canvas and canvas group
    /// </summary>
    private void InitializeCanvas()
    {
        if (canvas != null)
        {
            canvasGroup = canvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            }
            canvasWasEnabled = canvas.gameObject.activeSelf;
            originalCanvasAlpha = canvasGroup.alpha;
        }
    }
    
    /// <summary>
    /// Handles starting darkened if any OnStart entry requires it
    /// </summary>
    private void HandleStartDarkened()
    {
        IntermissionEntry startEntry = GetStartDarkenedEntry();
        
        if (startEntry != null)
        {
            SetOverlayColor(startEntry.overlayOpacity);
            
            if (canvas != null && canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvas.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Checks if any OnStart entry wants to start darkened and returns it
    /// </summary>
    private IntermissionEntry GetStartDarkenedEntry()
    {
        if (intermissionEntries == null) return null;
        
        foreach (var entry in intermissionEntries)
        {
            if (entry != null && entry.triggerType == TriggerType.OnStart && entry.startDarkened)
            {
                return entry;
            }
        }
        return null;
    }
    
    void OnEnable()
    {
        CheckTriggers(TriggerType.OnEnable, null, null);
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[IntermissionTextDisplay] OnCollisionEnter2D detected: GameObject='{collision.gameObject.name}', Layer={collision.gameObject.layer}");
        CheckTriggers(TriggerType.OnCollisionEnter, collision.gameObject, collision.collider);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[IntermissionTextDisplay] OnTriggerEnter2D detected: GameObject='{other.gameObject.name}', Layer={other.gameObject.layer}, Collider='{other.name}'");
        CheckTriggers(TriggerType.OnTriggerEnter, other.gameObject, other);
    }
    
    void OnCollisionExit2D(Collision2D collision)
    {
        CheckTriggers(TriggerType.OnCollisionExit, collision.gameObject, collision.collider);
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        CheckTriggers(TriggerType.OnTriggerExit, other.gameObject, other);
    }
    
    private void CheckTriggers(TriggerType triggerType, GameObject other, Collider2D collider = null)
    {
        if (intermissionEntries == null)
        {
            Debug.LogWarning($"[IntermissionTextDisplay] CheckTriggers: intermissionEntries is null for triggerType={triggerType}");
            return;
        }
        
        // Spam protection: Don't allow new triggers if a display is already active
        // UNLESS forceStart is enabled for any of the triggered entries
        bool isForced = false;
        for (int i = 0; i < intermissionEntries.Length; i++)
        {
            var entry = intermissionEntries[i];
            if (entry != null && entry.triggerType == triggerType && entry.forceStart)
            {
                // Basic check - doesn't verify all conditions yet, but good enough to bypass the early spam check
                isForced = true;
                break;
            }
        }
        
        if (isDisplaying && !isForced)
        {
            Debug.Log($"[IntermissionTextDisplay] CheckTriggers: Spam protection - Already displaying, ignoring triggerType={triggerType}");
            return;
        }
        
        Debug.Log($"[IntermissionTextDisplay] CheckTriggers: Checking triggerType={triggerType}, EntryCount={intermissionEntries.Length}, Other={(other != null ? other.name : "null")}");
        
        for (int i = 0; i < intermissionEntries.Length; i++)
        {
            var entry = intermissionEntries[i];
            if (entry == null)
            {
                Debug.LogWarning($"[IntermissionTextDisplay] CheckTriggers: Entry[{i}] is null");
                continue;
            }
            
            // Skip if this entry plays after another entry
            if (entry.playAfterEntry)
            {
                Debug.Log($"[IntermissionTextDisplay] CheckTriggers: Entry[{i}] skipped - plays after another entry");
                continue;
            }
            
            if (entry.triggerType != triggerType)
            {
                Debug.Log($"[IntermissionTextDisplay] CheckTriggers: Entry[{i}] skipped - triggerType mismatch (entry={entry.triggerType}, checking={triggerType})");
                continue;
            }
            
            if (!CanTriggerEntry(i, entry))
            {
                Debug.Log($"[IntermissionTextDisplay] CheckTriggers: Entry[{i}] skipped - already triggered (triggerOnce={entry.triggerOnce})");
                continue;
            }
            
            if (other != null && !ShouldTrigger(entry, other, collider))
            {
                Debug.Log($"[IntermissionTextDisplay] CheckTriggers: Entry[{i}] skipped - ShouldTrigger returned false");
                continue;
            }
            
            Debug.Log($"[IntermissionTextDisplay] CheckTriggers: Entry[{i}] TRIGGERED! Text='{entry.text}', TriggerType={entry.triggerType}");
            
            // Store the coroutine so we can track it
            activeTriggerCoroutine = StartCoroutine(TriggerEntryWithDelay(i, entry));
        }
    }
    
    private bool ShouldTrigger(IntermissionEntry entry, GameObject other, Collider2D collider = null)
    {
        // Check layer mask
        // -1 means "Everything" (all layers allowed)
        // 0 means "Nothing" (no layers allowed - should reject)
        // Any other value means specific layers
        if (entry.triggerLayer.value == 0)
        {
            Debug.Log($"[IntermissionTextDisplay] ShouldTrigger: Layer check failed - Entry layerMask=0 (Nothing), rejecting all");
            return false;
        }
        else if (entry.triggerLayer.value != -1 && (entry.triggerLayer.value & (1 << other.layer)) == 0)
        {
            Debug.Log($"[IntermissionTextDisplay] ShouldTrigger: Layer check failed - Entry layerMask={entry.triggerLayer.value}, Other layer={other.layer} (bit={1 << other.layer})");
            return false;
        }
        
        // Check specific collider GameObject for OnCollisionEnter/Exit and OnTriggerEnter/Exit
        // If specificColliderObject is set, only trigger when THAT object's collider enters/exits
        if ((entry.triggerType == TriggerType.OnCollisionEnter || entry.triggerType == TriggerType.OnCollisionExit ||
             entry.triggerType == TriggerType.OnTriggerEnter || entry.triggerType == TriggerType.OnTriggerExit) 
            && entry.specificColliderObject != null)
        {
            Debug.Log($"[IntermissionTextDisplay] ShouldTrigger: Checking specific collider - Target='{entry.specificColliderObject.name}', Other='{other.name}'");
            
            // Check if the colliding GameObject IS the target GameObject (or has the collider)
            if (other == entry.specificColliderObject)
            {
                Debug.Log($"[IntermissionTextDisplay] ShouldTrigger: GameObject match - Other is the target GameObject");
                return true; // The target GameObject itself is colliding, allow it
            }
            
            // Check if the collider belongs to the target GameObject
            if (collider != null)
            {
                // Get all colliders from the specified GameObject
                Collider2D[] targetColliders = entry.specificColliderObject.GetComponents<Collider2D>();
                bool matches = false;
                foreach (Collider2D targetCollider in targetColliders)
                {
                    if (collider == targetCollider)
                    {
                        matches = true;
                        Debug.Log($"[IntermissionTextDisplay] ShouldTrigger: Collider match found - '{collider.name}' belongs to target GameObject");
                        break;
                    }
                }
                if (!matches)
                {
                    Debug.Log($"[IntermissionTextDisplay] ShouldTrigger: Collider mismatch - collider '{collider.name}' (from '{other.name}') does not belong to target GameObject '{entry.specificColliderObject.name}'. Leave Collider Object empty to trigger on any collision.");
                    return false;
                }
            }
            else
            {
                Debug.Log($"[IntermissionTextDisplay] ShouldTrigger: Collider is null, cannot verify match");
                return false;
            }
        }
        
        Debug.Log($"[IntermissionTextDisplay] ShouldTrigger: All checks passed");
        return true;
    }
    
    private IEnumerator TriggerEntryWithDelay(int index, IntermissionEntry entry)
    {
        Debug.Log($"[IntermissionTextDisplay] TriggerEntryWithDelay: Entry[{index}] - Text='{entry.text}', TriggerType={entry.triggerType}, Delay={entry.triggerDelay}, EnableText={entry.enableText}, EnableOverlay={entry.enableOverlay}, EnableDarkening={entry.enableDarkening}, ForceStart={entry.forceStart}");
        
        // Handle Force Start
        if (entry.forceStart && isDisplaying)
        {
            Debug.Log($"[IntermissionTextDisplay] TriggerEntryWithDelay: Force Start enabled - interrupting current display");
            StopCurrentDisplay(true); // Pass true to indicate we are starting a new one
        }
        
        // Mark as triggered if triggerOnce is enabled
        if (entry.triggerOnce)
        {
            entryTriggered[index] = true;
            Debug.Log($"[IntermissionTextDisplay] TriggerEntryWithDelay: Entry[{index}] marked as triggered (triggerOnce=true)");
        }
        
        if (entry.triggerDelay > 0f)
        {
            Debug.Log($"[IntermissionTextDisplay] TriggerEntryWithDelay: Waiting {entry.triggerDelay} seconds before displaying Entry[{index}]");
            // CRITICAL FIX: Use WaitForSecondsRealtime to avoid freezing if timeScale is 0
            yield return new WaitForSecondsRealtime(entry.triggerDelay);
            
            // Check spam protection again after delay (another display might have started)
            if (isDisplaying)
            {
                Debug.Log($"[IntermissionTextDisplay] TriggerEntryWithDelay: Spam protection - Display started during delay, cancelling Entry[{index}]");
                yield break;
            }
        }
        
        currentActiveEntryIndex = index;
        
        // Notify tutorial manager that intermission entry is starting
        SimpleTutorialManager tutorialManager = FindObjectOfType<SimpleTutorialManager>();
        if (tutorialManager != null)
        {
            tutorialManager.OnIntermissionEntryStart(index);
        }
        
        if (!string.IsNullOrEmpty(entry.text))
        {
            bool alreadyDarkened = entry.triggerType == TriggerType.OnStart && entry.startDarkened;
            bool hasMoreEntries = HasMoreEntriesAfter(index);
            Debug.Log($"[IntermissionTextDisplay] TriggerEntryWithDelay: Displaying Entry[{index}] - AlreadyDarkened={alreadyDarkened}, HasMoreEntries={hasMoreEntries}");
            yield return StartCoroutine(DisplayEntry(index, entry, alreadyDarkened, hasMoreEntries));
        }
        else
        {
            Debug.LogWarning($"[IntermissionTextDisplay] TriggerEntryWithDelay: Entry[{index}] has empty text, skipping display");
        }
        
        // Notify tutorial manager that intermission entry is ending
        if (tutorialManager != null)
        {
            tutorialManager.OnIntermissionEntryEnd(index);
        }
        
        entryCompleted[index] = true;
        yield return StartCoroutine(CheckPlayAfterEntries(index));
        
        currentActiveEntryIndex = -1;
        activeTriggerCoroutine = null;
    }
    
    private bool HasMoreEntriesAfter(int currentIndex)
    {
        return FindNextEntryIndex(currentIndex) >= 0;
    }
    
    /// <summary>
    /// Finds the index of the next entry that should play after the given index, or -1 if none found
    /// </summary>
    private int FindNextEntryIndex(int completedIndex)
    {
        if (intermissionEntries == null) return -1;
        
        for (int i = 0; i < intermissionEntries.Length; i++)
        {
            var entry = intermissionEntries[i];
            if (entry == null) continue;
            
            // Check if this entry should play after the completed entry
            if (entry.playAfterEntry && entry.playAfterEntryIndex == completedIndex)
            {
                // Check if already triggered (if triggerOnce is true)
                if (entry.triggerOnce && entryTriggered.ContainsKey(i) && entryTriggered[i])
                {
                    continue;
                }
                
                if (!string.IsNullOrEmpty(entry.text))
                {
                    return i;
                }
            }
        }
        
        return -1;
    }
    
    /// <summary>
    /// Checks if an entry can be triggered (not already triggered if triggerOnce is enabled)
    /// </summary>
    private bool CanTriggerEntry(int index, IntermissionEntry entry)
    {
        return !entry.triggerOnce || !entryTriggered.ContainsKey(index) || !entryTriggered[index];
    }
    
    private IEnumerator CheckPlayAfterEntries(int completedIndex)
    {
        int nextIndex = FindNextEntryIndex(completedIndex);
        
        while (nextIndex >= 0)
        {
            var entry = intermissionEntries[nextIndex];
            currentActiveEntryIndex = nextIndex;
            
            // Mark as triggered if triggerOnce is enabled
            if (entry.triggerOnce)
            {
                entryTriggered[nextIndex] = true;
            }
            
            if (entry.triggerDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(entry.triggerDelay);
            }
            
            yield return null;
            
            // Clear text if still visible
            if (textDisplay != null && textDisplay.color.a > 0.01f)
            {
                textDisplay.color = new Color(textDisplay.color.r, textDisplay.color.g, textDisplay.color.b, 0f);
            }
            
            bool alreadyDarkened = true;
            bool hasMoreEntries = HasMoreEntriesAfter(nextIndex);
            yield return StartCoroutine(DisplayEntry(nextIndex, entry, alreadyDarkened, hasMoreEntries));
            
            entryCompleted[nextIndex] = true;
            
            // Check for next entry
            nextIndex = FindNextEntryIndex(nextIndex);
        }
        
        // Clear displaying flag when all chained entries are complete
        isDisplaying = false;
        currentActiveEntryIndex = -1;
        Debug.Log($"[IntermissionTextDisplay] CheckPlayAfterEntries: All chained entries complete, isDisplaying={isDisplaying}");
    }
    
    private IEnumerator DisplayEntry(int index, IntermissionEntry entry, bool alreadyDarkened = false, bool hasMoreEntries = false)
    {
        // Use per-entry settings
        bool useText = entry.enableText;
        bool useOverlay = entry.enableOverlay;
        bool useDarkening = entry.enableDarkening; // Only the per-entry setting matters now
        float opacity = entry.overlayOpacity;
        bool snappy = entry.snappyCanvas;
        float duration = entry.customDisplayDuration >= 0 ? entry.customDisplayDuration : displayDuration;
        bool waitInput = entry.waitForInput;
        KeyCode key = entry.dismissKey;
        
        yield return StartCoroutine(DisplayTextInternal(entry.text, alreadyDarkened, hasMoreEntries, useText, useOverlay, useDarkening, opacity, snappy, duration, waitInput, key));
    }
    
    /// <summary>
    /// Show an intermission message. If a message is already displaying, it will be queued.
    /// </summary>
    public void ShowIntermission(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("IntermissionTextDisplay: Cannot show empty message.");
            return;
        }
        
        messageQueue.Enqueue(message);
        
        if (!isDisplaying && currentDisplayCoroutine == null)
        {
            currentDisplayCoroutine = StartCoroutine(ProcessMessageQueue());
        }
    }
    
    private IEnumerator ProcessMessageQueue()
    {
        isDisplaying = true;
        
        while (messageQueue.Count > 0)
        {
            string message = messageQueue.Dequeue();
            yield return StartCoroutine(DisplayMessage(message));
        }
        
        isDisplaying = false;
        currentDisplayCoroutine = null;
    }
    
    private IEnumerator DisplayMessage(string message, bool alreadyDarkened = false)
    {
        // Use default settings for manual message display (darkening enabled by default)
        yield return StartCoroutine(DisplayTextInternal(message, alreadyDarkened, false, true, true, true, 1f, false, displayDuration, false, KeyCode.Space));
    }
    
    /// <summary>
    /// Internal method that handles the actual text display logic - used by both DisplayEntry and DisplayMessage
    /// </summary>
    private IEnumerator DisplayTextInternal(string message, bool alreadyDarkened, bool hasMoreEntries, 
        bool useText, bool useOverlay, bool useDarkening, float targetOverlayOpacity, bool snappyCanvas,
        float duration, bool waitForInput, KeyCode dismissKey)
    {
        // Set displaying flag to prevent spam
        isDisplaying = true;
        
        Debug.Log($"[IntermissionTextDisplay] DisplayTextInternal: Starting - Message='{message}', UseText={useText}, UseOverlay={useOverlay}, UseDarkening={useDarkening}, AlreadyDarkened={alreadyDarkened}, Opacity={targetOverlayOpacity}, Snappy={snappyCanvas}, Duration={duration}, WaitForInput={waitForInput}");
        
        if (textDisplay == null && useText)
        {
            Debug.LogError($"[IntermissionTextDisplay] DisplayTextInternal: textDisplay is null but useText=true! Cannot display text.");
            isDisplaying = false;
            yield break;
        }
        
        if (textDisplay != null)
        {
            Debug.Log($"[IntermissionTextDisplay] DisplayTextInternal: textDisplay found - Active={textDisplay.gameObject.activeSelf}, Enabled={textDisplay.enabled}");
        }
        
        FreezeGame();
        SetupTextDisplay(message, useText);
        SetupCanvasDarkening(useDarkening, alreadyDarkened, snappyCanvas);
        SetupOverlay(useOverlay, useDarkening, alreadyDarkened);
        
        yield return StartCoroutine(FadeInWithOverlay(alreadyDarkened, useText, useOverlay, useDarkening, targetOverlayOpacity, snappyCanvas));
        Debug.Log($"[IntermissionTextDisplay] DisplayTextInternal: Fade in complete");
        
        // Wait for specified duration or input
        if (waitForInput)
        {
            Debug.Log($"[IntermissionTextDisplay] DisplayTextInternal: Waiting for {dismissKey} key press...");
            while (!Input.GetKeyDown(dismissKey))
            {
                yield return null;
            }
            Debug.Log($"[IntermissionTextDisplay] DisplayTextInternal: {dismissKey} pressed, dismissing");
        }
        else
        {
            Debug.Log($"[IntermissionTextDisplay] DisplayTextInternal: Waiting {duration} seconds");
            yield return new WaitForSecondsRealtime(duration);
        }
        
        UnfreezeGame();
        yield return StartCoroutine(FadeOutWithOverlay(hasMoreEntries, useText, useOverlay, useDarkening));
        
        CleanupTextDisplay(useText);
        
        // Clear displaying flag when done (unless there are more entries queued)
        if (!hasMoreEntries)
        {
            isDisplaying = false;
        }
        
        Debug.Log($"[IntermissionTextDisplay] DisplayTextInternal: Complete, isDisplaying={isDisplaying}");
    }
    
    /// <summary>
    /// Sets up the text display component
    /// </summary>
    private void SetupTextDisplay(string message, bool useText)
    {
        if (useText && textDisplay != null)
        {
            Debug.Log($"[IntermissionTextDisplay] SetupTextDisplay: Setting up text - Message='{message}', Color={textColor}, FontSize={fontSize}");
            textDisplay.gameObject.SetActive(true);
            textDisplay.text = message;
            textDisplay.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
            textDisplay.fontSize = fontSize;
            textDisplay.transform.SetAsLastSibling();
            Debug.Log($"[IntermissionTextDisplay] SetupTextDisplay: Text set - Active={textDisplay.gameObject.activeSelf}, CurrentAlpha={textDisplay.color.a}, Text='{textDisplay.text}'");
        }
        else if (useText && textDisplay == null)
        {
            Debug.LogError($"[IntermissionTextDisplay] SetupTextDisplay: useText=true but textDisplay is null!");
        }
        else
        {
            Debug.Log($"[IntermissionTextDisplay] SetupTextDisplay: Skipped - useText={useText}");
        }
    }
    
    /// <summary>
    /// Cleans up the text display component
    /// </summary>
    private void CleanupTextDisplay(bool useText)
    {
        if (useText && textDisplay != null)
        {
            textDisplay.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Sets up canvas darkening effects
    /// </summary>
    private void SetupCanvasDarkening(bool useDarkening, bool alreadyDarkened, bool snappyCanvas = false)
    {
        if (useDarkening && !alreadyDarkened && canvas != null)
        {
            if (!canvasWasEnabled)
            {
                canvasWasEnabled = canvas.gameObject.activeSelf;
                if (canvasGroup != null)
                {
                    originalCanvasAlpha = canvasGroup.alpha;
                }
            }
            
            // CRITICAL: Don't use SetActive(false) - it prevents tutorial from showing child UI!
            // Always use CanvasGroup.alpha instead
            if (canvasGroup != null)
            {
                if (snappyCanvas)
                {
                    // Snap immediately to 0 alpha
                    canvasGroup.alpha = 0f;
                }
                else
                {
                    // Fade out gradually
                    StartCoroutine(FadeOutCanvas());
                }
            }
            else
            {
                // No CanvasGroup - add one on the fly
                Debug.LogWarning("[IntermissionTextDisplay] Canvas has no CanvasGroup! Adding one dynamically.");
                canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
                originalCanvasAlpha = 1f;
                
                if (snappyCanvas)
                {
                    canvasGroup.alpha = 0f;
                }
                else
                {
                    StartCoroutine(FadeOutCanvas());
                }
            }
        }
        else if (!useDarkening && alreadyDarkened && canvas != null)
        {
            // If we're already darkened but this entry doesn't want darkening,
            // restore the canvas
            Debug.Log($"[IntermissionTextDisplay] SetupCanvasDarkening: Entry doesn't want darkening but canvas is hidden, restoring");
            StartCoroutine(FadeInCanvas());
        }
    }
    
    /// <summary>
    /// Sets up the dark overlay
    /// </summary>
    private void SetupOverlay(bool useOverlay, bool useDarkening, bool alreadyDarkened)
    {
        if (darkOverlay == null) return;
        
        if (useOverlay && useDarkening)
        {
            darkOverlay.gameObject.SetActive(true);
            darkOverlay.transform.SetAsFirstSibling();
            Debug.Log($"[IntermissionTextDisplay] SetupOverlay: Overlay enabled for this entry");
        }
        else if (alreadyDarkened && (!useOverlay || !useDarkening))
        {
            // If we're already darkened but this entry doesn't want overlay/darkening,
            // fade out the existing overlay immediately
            Debug.Log($"[IntermissionTextDisplay] SetupOverlay: Entry doesn't want darkening but overlay is active, fading out");
            StartCoroutine(FadeOutExistingOverlay());
        }
    }
    
    /// <summary>
    /// Fades out an existing overlay when transitioning to an entry that doesn't want it
    /// </summary>
    private IEnumerator FadeOutExistingOverlay()
    {
        if (darkOverlay == null) yield break;
        
        float startAlpha = darkOverlay.color.a;
        float targetAlpha = 0f;
        float adjustedDuration = overlayFadeOutDuration / overlayFadeSpeed;
        float elapsed = 0f;
        
        while (elapsed < adjustedDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float rawT = Mathf.Clamp01(elapsed / adjustedDuration);
            float t = SmoothStep(rawT);
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetOverlayColor(currentAlpha);
            yield return null;
        }
        
        SetOverlayColor(targetAlpha);
        Debug.Log($"[IntermissionTextDisplay] FadeOutExistingOverlay: Complete");
    }
    
    /// <summary>
    /// Freezes the game if enabled
    /// </summary>
    private void FreezeGame()
    {
        if (freezeGame)
        {
            Time.timeScale = 0f;
        }
    }
    
    /// <summary>
    /// Unfreezes the game if it was frozen
    /// </summary>
    private void UnfreezeGame()
    {
        if (freezeGame)
        {
            Time.timeScale = 1f;
        }
    }
    
    /// <summary>
    /// Manually trigger a specific entry by index
    /// </summary>
    public void TriggerEntryManually(int index)
    {
        if (intermissionEntries == null || index < 0 || index >= intermissionEntries.Length)
        {
            Debug.LogWarning($"IntermissionTextDisplay: Invalid entry index {index}");
            return;
        }
        
        var entry = intermissionEntries[index];
        if (entry != null && !string.IsNullOrEmpty(entry.text))
        {
            activeTriggerCoroutine = StartCoroutine(TriggerEntryWithDelay(index, entry));
        }
    }
    
    /// <summary>
    /// Reset trigger state for a specific entry so it can trigger again
    /// </summary>
    public void ResetTrigger(int entryIndex)
    {
        if (entryTriggered.ContainsKey(entryIndex))
        {
            entryTriggered[entryIndex] = false;
        }
        if (entryCompleted.ContainsKey(entryIndex))
        {
            entryCompleted[entryIndex] = false;
        }
    }
    
    /// <summary>
    /// Reset all trigger states
    /// </summary>
    public void ResetAllTriggers()
    {
        entryTriggered.Clear();
        entryCompleted.Clear();
    }
    
    private IEnumerator FadeInWithOverlay(bool alreadyDarkened = false, bool useText = true, bool useOverlay = true, bool useDarkening = true, float targetOverlayOpacity = 1f, bool snappyCanvas = false)
    {
        Debug.Log($"[IntermissionTextDisplay] FadeInWithOverlay: Starting - UseText={useText}, UseOverlay={useOverlay}, UseDarkening={useDarkening}, AlreadyDarkened={alreadyDarkened}, EnableTextFadeIn={enableTextFadeIn}, TargetOpacity={targetOverlayOpacity}, Snappy={snappyCanvas}");
        
        // Determine which effects need to fade in
        bool needsTextFade = useText && enableTextFadeIn && textDisplay != null;
        
        // Check if overlay needs fading (either it's not dark yet, or the opacity is different)
        float currentOverlayAlpha = darkOverlay != null ? darkOverlay.color.a : 0f;
        bool opacityChanged = Mathf.Abs(currentOverlayAlpha - targetOverlayOpacity) > 0.01f;
        bool needsOverlayFade = useOverlay && useDarkening && darkOverlay != null && (!alreadyDarkened || opacityChanged);
        
        Debug.Log($"[IntermissionTextDisplay] FadeInWithOverlay: NeedsTextFade={needsTextFade}, NeedsOverlayFade={needsOverlayFade}, OpacityChanged={opacityChanged}");
        
        // CRITICAL FIX: If text should be shown but fade is disabled, show it immediately
        if (useText && textDisplay != null && !enableTextFadeIn)
        {
            Debug.Log($"[IntermissionTextDisplay] FadeInWithOverlay: Text fade disabled, setting text color immediately to visible");
            textDisplay.color = textColor;
        }
        
        // Snappy Canvas: Skip fading and set everything immediately
        if (snappyCanvas)
        {
            Debug.Log($"[IntermissionTextDisplay] FadeInWithOverlay: Snappy Canvas enabled - skipping fade");
            
            if (useText && textDisplay != null)
            {
                textDisplay.color = textColor;
            }
            
            if (useOverlay && useDarkening && darkOverlay != null)
            {
                SetOverlayColor(targetOverlayOpacity);
            }
            
            yield break;
        }
        
        // If neither needs fading, skip the coroutine
        if (!needsTextFade && !needsOverlayFade)
        {
            Debug.Log($"[IntermissionTextDisplay] FadeInWithOverlay: Early exit - no fading needed");
            // Set final values immediately
            if (needsTextFade && textDisplay != null)
            {
                textDisplay.color = textColor;
                Debug.Log($"[IntermissionTextDisplay] FadeInWithOverlay: Set text color immediately (needsTextFade=true)");
            }
            if (needsOverlayFade)
            {
                SetOverlayColor(targetOverlayOpacity);
                Debug.Log($"[IntermissionTextDisplay] FadeInWithOverlay: Set overlay color immediately");
            }
            yield break;
        }
        
        // Calculate the maximum duration needed (adjusted by speed multipliers)
        float maxDuration = 0f;
        float adjustedTextFadeInDuration = needsTextFade ? textFadeInDuration / textFadeSpeed : 0f;
        float adjustedOverlayFadeInDuration = needsOverlayFade ? overlayFadeInDuration / overlayFadeSpeed : 0f;
        if (needsTextFade) maxDuration = Mathf.Max(maxDuration, adjustedTextFadeInDuration);
        if (needsOverlayFade) maxDuration = Mathf.Max(maxDuration, adjustedOverlayFadeInDuration);
        
        Debug.Log($"[IntermissionTextDisplay] FadeInWithOverlay: MaxDuration={maxDuration}, TextFadeDuration={adjustedTextFadeInDuration}, OverlayFadeDuration={adjustedOverlayFadeInDuration}");
        
        Color startTextColor = new Color(textColor.r, textColor.g, textColor.b, 0f);
        Color targetTextColor = textColor;
        
        if (needsTextFade && textDisplay != null)
        {
            if (enableTextFadeIn)
            {
                textDisplay.color = startTextColor;
                Debug.Log($"[IntermissionTextDisplay] FadeInWithOverlay: Set start text color (alpha=0)");
            }
            else
            {
                textDisplay.color = targetTextColor;
                Debug.Log($"[IntermissionTextDisplay] FadeInWithOverlay: Set target text color immediately (fade disabled)");
            }
        }
        
        float startOverlayAlpha = 0f;
        float targetOverlayAlpha = targetOverlayOpacity;
        
        if (darkOverlay != null)
        {
            startOverlayAlpha = darkOverlay.color.a;
        }
        
        if (needsOverlayFade)
        {
            SetOverlayColor(startOverlayAlpha);
        }
        else if (useOverlay && useDarkening)
        {
            SetOverlayColor(targetOverlayAlpha);
        }
        
        float elapsed = 0f;
        
        while (elapsed < maxDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            
            if (needsTextFade && textDisplay != null)
            {
                float rawTextT = Mathf.Clamp01(elapsed / adjustedTextFadeInDuration);
                float textT = SmoothStep(rawTextT);
                textDisplay.color = Color.Lerp(startTextColor, targetTextColor, textT);
            }
            
            if (needsOverlayFade)
            {
                float rawOverlayT = Mathf.Clamp01(elapsed / adjustedOverlayFadeInDuration);
                float overlayT = SmoothStep(rawOverlayT);
                float currentAlpha = Mathf.Lerp(startOverlayAlpha, targetOverlayAlpha, overlayT);
                SetOverlayColor(currentAlpha);
            }
            
            yield return null;
        }
        if (needsTextFade && textDisplay != null)
        {
            textDisplay.color = targetTextColor;
            Debug.Log($"[IntermissionTextDisplay] FadeInWithOverlay: Text fade complete - FinalAlpha={textDisplay.color.a}");
        }
        
        if (needsOverlayFade)
        {
            SetOverlayColor(targetOverlayAlpha);
            Debug.Log($"[IntermissionTextDisplay] FadeInWithOverlay: Overlay fade complete");
        }
        
        Debug.Log($"[IntermissionTextDisplay] FadeInWithOverlay: Complete - TextAlpha={(textDisplay != null ? textDisplay.color.a.ToString() : "N/A")}");
    }
    
    private IEnumerator FadeOutWithOverlay(bool keepOverlayDark = false, bool useText = true, bool useOverlay = true, bool useDarkening = true)
    {
        // Calculate adjusted durations based on speed multipliers
        float adjustedTextFadeOutDuration = textFadeOutDuration / textFadeSpeed;
        float adjustedOverlayFadeOutDuration = overlayFadeOutDuration / overlayFadeSpeed;
        
        if (useText && textDisplay != null)
        {
            Color startTextColor = textDisplay.color;
            Color targetTextColor = new Color(startTextColor.r, startTextColor.g, startTextColor.b, 0f);
            
            float textElapsed = 0f;
            while (textElapsed < adjustedTextFadeOutDuration)
            {
                textElapsed += Time.unscaledDeltaTime;
                float rawT = Mathf.Clamp01(textElapsed / adjustedTextFadeOutDuration);
                float t = SmoothStep(rawT);
                textDisplay.color = Color.Lerp(startTextColor, targetTextColor, t);
                yield return null;
            }
            
            textDisplay.color = targetTextColor;
            
            if (keepOverlayDark)
            {
                yield return null;
            }
        }
        
        if (!keepOverlayDark && useOverlay && useDarkening && darkOverlay != null)
        {
            float startOverlayAlpha = darkOverlay.color.a;
            float targetOverlayAlpha = 0f;
            
            float overlayFadeHalfway = adjustedOverlayFadeOutDuration * 0.5f;
            float overlayElapsed = 0f;
            Coroutine canvasRestoreCoroutine = null;
            bool canvasStarted = false;
            
            while (overlayElapsed < adjustedOverlayFadeOutDuration)
            {
                overlayElapsed += Time.unscaledDeltaTime;
                float rawT = Mathf.Clamp01(overlayElapsed / adjustedOverlayFadeOutDuration);
                float t = SmoothStep(rawT);
                float currentAlpha = Mathf.Lerp(startOverlayAlpha, targetOverlayAlpha, t);
                SetOverlayColor(currentAlpha);
                
                if (!canvasStarted && overlayElapsed >= overlayFadeHalfway && canvas != null && (canvasWasEnabled || originalCanvasAlpha > 0f))
                {
                    canvasRestoreCoroutine = StartCoroutine(FadeInCanvas());
                    canvasStarted = true;
                }
                
                yield return null;
            }
            
            SetOverlayColor(targetOverlayAlpha);
            
            if (canvasRestoreCoroutine != null)
            {
                yield return canvasRestoreCoroutine;
            }
        }
        else if (keepOverlayDark && useOverlay && useDarkening)
        {
            // Keep current opacity (don't force to 1f)
            // This allows chaining entries with different opacities to transition smoothly
        }
    }
    
    private IEnumerator FadeOutCanvas()
    {
        if (canvas == null || canvasGroup == null) yield break;
        
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsed < canvasFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / canvasFadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        // Don't disable canvas - tutorial needs to show child UI!
    }
    
    private IEnumerator FadeInCanvas()
    {
        if (canvas == null || canvasGroup == null) yield break;
        
        canvas.gameObject.SetActive(true);
        
        // Start from current alpha instead of forcing to 0
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        
        while (elapsed < canvasFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / canvasFadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, originalCanvasAlpha, t);
            yield return null;
        }
        
        canvasGroup.alpha = originalCanvasAlpha;
    }
    
    /// <summary>
    /// Stops the currently playing intermission immediately
    /// </summary>
    public void StopCurrentDisplay(bool startingNew = false)
    {
        // CRITICAL FIX: Do NOT use StopAllCoroutines() as it kills the NEW trigger coroutine
        // that called this method!
        
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
            currentDisplayCoroutine = null;
        }
        
        // Only stop the active trigger coroutine if we are NOT starting a new one
        // (If we are starting a new one, this variable might already hold the new coroutine)
        if (!startingNew && activeTriggerCoroutine != null)
        {
            StopCoroutine(activeTriggerCoroutine);
            activeTriggerCoroutine = null;
        }
        
        // Ensure game is unfrozen
        UnfreezeGame();
        
        // Notify tutorial manager to clean up
        if (currentActiveEntryIndex >= 0)
        {
            SimpleTutorialManager tutorialManager = FindObjectOfType<SimpleTutorialManager>();
            if (tutorialManager != null)
            {
                tutorialManager.OnIntermissionEntryEnd(currentActiveEntryIndex);
            }
            currentActiveEntryIndex = -1;
        }
        
        // Force instant cleanup of UI
        ForceHideInstant();
        
        isDisplaying = false;
        Debug.Log($"[IntermissionTextDisplay] StopCurrentDisplay: Interrupted current display (StartingNew={startingNew})");
    }

    /// <summary>
    /// Triggers any entries associated with the given collider object
    /// </summary>
    public void TriggerByObject(GameObject obj)
    {
        if (intermissionEntries == null) return;
        
        for (int i = 0; i < intermissionEntries.Length; i++)
        {
            var entry = intermissionEntries[i];
            if (entry != null && entry.specificColliderObject == obj)
            {
                TryTriggerEntry(i);
            }
        }
    }
    
    /// <summary>
    /// Tries to trigger an entry, respecting its conditions (TriggerOnce, etc.)
    /// </summary>
    public void TryTriggerEntry(int index)
    {
        if (intermissionEntries == null || index < 0 || index >= intermissionEntries.Length) return;
        
        var entry = intermissionEntries[index];
        if (entry == null) return;
        
        // Check if allowed to trigger
        if (!CanTriggerEntry(index, entry))
        {
            Debug.Log($"[IntermissionTextDisplay] TryTriggerEntry: Entry[{index}] skipped - already triggered");
            return;
        }
        
        // Check spam protection (unless forced)
        if (isDisplaying && !entry.forceStart)
        {
            Debug.Log($"[IntermissionTextDisplay] TryTriggerEntry: Entry[{index}] skipped - display active");
            return;
        }
        
        activeTriggerCoroutine = StartCoroutine(TriggerEntryWithDelay(index, entry));
    }

    /// <summary>
    /// Clear the message queue and stop current display
    /// </summary>
    public void ClearQueue()
    {
        messageQueue.Clear();
        StopCurrentDisplay(false);
    }
    
    /// <summary>
    /// FORCE instant hide - skips all fades and immediately restores canvas
    /// Called by SimpleTutorialManager when tutorial needs immediate cleanup
    /// </summary>
    public void ForceHideInstant()
    {
        Debug.Log("[IntermissionTextDisplay] ForceHideInstant: Instant cleanup requested");
        
        // Hide text immediately - but ONLY the text GameObject itself, not parents
        if (textDisplay != null)
        {
            textDisplay.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
            textDisplay.gameObject.SetActive(false);
            Debug.Log($"[IntermissionTextDisplay] Hidden textDisplay: {textDisplay.name}");
        }
        
        // Hide overlay immediately - but ONLY the overlay GameObject itself, not parents
        if (darkOverlay != null)
        {
            // Set alpha to 0
            Color overlayColor = darkOverlay.color;
            overlayColor.a = 0f;
            darkOverlay.color = overlayColor;
            
            // Disable the Image component
            darkOverlay.enabled = false;
            
            // Hide the GameObject
            darkOverlay.gameObject.SetActive(false);
            
            Debug.Log($"[IntermissionTextDisplay] Hidden darkOverlay: {darkOverlay.name}");
        }
        
        // Restore canvas immediately
        if (canvas != null && canvasGroup != null)
        {
            canvasGroup.alpha = originalCanvasAlpha;
            Debug.Log($"[IntermissionTextDisplay] Canvas alpha restored to {originalCanvasAlpha}");
        }
        
        // Reset state
        isDisplaying = false;
        
        Debug.Log("[IntermissionTextDisplay] ForceHideInstant: Complete");
    }
}
