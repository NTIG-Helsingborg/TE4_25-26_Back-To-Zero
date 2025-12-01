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
        public float triggerDelay = 0f;
        public bool triggerOnce = true; // If true, only triggers once
        public bool startDarkened = false; // If true and trigger is OnStart, starts already darkened
        
        public bool playAfterEntry = false; // If true, plays after another entry completes
        [HideInInspector]
        public int playAfterEntryIndex = 0; // Index of entry to play after (handled by custom drawer)
    }
    
    [Header("Intermission Entries")]
    [SerializeField] private IntermissionEntry[] intermissionEntries = new IntermissionEntry[0];
    
    [Header("Background Darkening")]
    [SerializeField] private bool enableDarkening = true;
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
    private Dictionary<int, bool> entryTriggered = new Dictionary<int, bool>(); // Track which entries have triggered
    private Dictionary<int, bool> entryCompleted = new Dictionary<int, bool>(); // Track which entries have completed
    private bool canvasWasEnabled = false;
    private CanvasGroup canvasGroup = null;
    private float originalCanvasAlpha = 1f;
    
    void Start()
    {
        // Auto-find text display if not assigned
        if (textDisplay == null)
        {
            textDisplay = GetComponent<TextMeshProUGUI>();
            if (textDisplay == null)
            {
                Debug.LogWarning("IntermissionTextDisplay: No TextMeshProUGUI found. Please assign one in the inspector.");
            }
        }
        
        // Auto-find dark overlay if not assigned
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
        
        // Initialize overlay if it exists
        if (darkOverlay != null)
        {
            darkOverlay.gameObject.SetActive(true);
            darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f);
            darkOverlay.transform.SetAsFirstSibling();
        }
        
        if (textDisplay != null)
        {
            textDisplay.gameObject.SetActive(true);
            textDisplay.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
            textDisplay.fontSize = fontSize;
            textDisplay.transform.SetAsLastSibling();
        }
        
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
        
        // Check if any OnStart entry wants to start darkened
        bool shouldStartDarkened = false;
        if (intermissionEntries != null)
        {
            foreach (var entry in intermissionEntries)
            {
                if (entry != null && entry.triggerType == TriggerType.OnStart && entry.startDarkened)
                {
                    shouldStartDarkened = true;
                    break;
                }
            }
        }
        
        if (shouldStartDarkened && enableDarkening)
        {
            if (darkOverlay != null)
            {
                darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 1f);
            }
            
            if (canvas != null)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
                canvas.gameObject.SetActive(false);
            }
        }
        
        CheckTriggers(TriggerType.OnStart, null);
    }
    
    void OnEnable()
    {
        CheckTriggers(TriggerType.OnEnable, null);
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        CheckTriggers(TriggerType.OnCollisionEnter, collision.gameObject);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        CheckTriggers(TriggerType.OnTriggerEnter, other.gameObject);
    }
    
    void OnCollisionExit2D(Collision2D collision)
    {
        CheckTriggers(TriggerType.OnCollisionExit, collision.gameObject);
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        CheckTriggers(TriggerType.OnTriggerExit, other.gameObject);
    }
    
    private void CheckTriggers(TriggerType triggerType, GameObject other)
    {
        if (intermissionEntries == null) return;
        
        for (int i = 0; i < intermissionEntries.Length; i++)
        {
            var entry = intermissionEntries[i];
            if (entry == null) continue;
            
            // Skip if this entry plays after another entry
            if (entry.playAfterEntry) continue;
            
            if (entry.triggerType != triggerType) continue;
            
            if (entry.triggerOnce && entryTriggered.ContainsKey(i) && entryTriggered[i])
            {
                continue;
            }
            
            if (other != null && !ShouldTrigger(entry, other))
            {
                continue;
            }
            
            StartCoroutine(TriggerEntryWithDelay(i, entry));
        }
    }
    
    private bool ShouldTrigger(IntermissionEntry entry, GameObject other)
    {
        if (entry.triggerLayer != -1 && (entry.triggerLayer.value & (1 << other.layer)) == 0)
        {
            return false;
        }
        
        return true;
    }
    
    private IEnumerator TriggerEntryWithDelay(int index, IntermissionEntry entry)
    {
        // Mark as triggered if triggerOnce is enabled
        if (entry.triggerOnce)
        {
            entryTriggered[index] = true;
        }
        
        if (entry.triggerDelay > 0f)
        {
            yield return new WaitForSeconds(entry.triggerDelay);
        }
        
        if (!string.IsNullOrEmpty(entry.text))
        {
            bool alreadyDarkened = entry.triggerType == TriggerType.OnStart && entry.startDarkened;
            bool hasMoreEntries = HasMoreEntriesAfter(index);
            yield return StartCoroutine(DisplayEntry(index, entry.text, alreadyDarkened, hasMoreEntries));
        }
        
        entryCompleted[index] = true;
        yield return StartCoroutine(CheckPlayAfterEntries(index));
    }
    
    private bool HasMoreEntriesAfter(int currentIndex)
    {
        if (intermissionEntries == null) return false;
        
        for (int i = 0; i < intermissionEntries.Length; i++)
        {
            var entry = intermissionEntries[i];
            if (entry == null) continue;
            
            // Check if this entry should play after the current entry
            if (entry.playAfterEntry && entry.playAfterEntryIndex == currentIndex)
            {
                // Check if already triggered (if triggerOnce is true)
                if (entry.triggerOnce && entryTriggered.ContainsKey(i) && entryTriggered[i])
                {
                    continue;
                }
                
                if (!string.IsNullOrEmpty(entry.text))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private IEnumerator CheckPlayAfterEntries(int completedIndex)
    {
        if (intermissionEntries == null) yield break;
        
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
                
                // Queue the entry to play after current display completes
                if (!string.IsNullOrEmpty(entry.text))
                {
                    // Mark as triggered if triggerOnce is enabled
                    if (entry.triggerOnce)
                    {
                        entryTriggered[i] = true;
                    }
                    
                    if (entry.triggerDelay > 0f)
                    {
                        yield return new WaitForSecondsRealtime(entry.triggerDelay);
                    }
                    
                    yield return null;
                    
                    if (textDisplay != null && textDisplay.color.a > 0.01f)
                    {
                        textDisplay.color = new Color(textDisplay.color.r, textDisplay.color.g, textDisplay.color.b, 0f);
                    }
                    
                    bool alreadyDarkened = true;
                    bool hasMoreEntries = HasMoreEntriesAfter(i);
                    yield return StartCoroutine(DisplayEntry(i, entry.text, alreadyDarkened, hasMoreEntries));
                    
                    entryCompleted[i] = true;
                    yield return StartCoroutine(CheckPlayAfterEntries(i));
                }
            }
        }
    }
    
    private IEnumerator DisplayEntry(int index, string message, bool alreadyDarkened = false, bool hasMoreEntries = false)
    {
        if (textDisplay == null) yield break;
        
        // Freeze game if enabled
        if (freezeGame)
        {
            Time.timeScale = 0f;
        }
        
        textDisplay.gameObject.SetActive(true);
        textDisplay.text = message;
        textDisplay.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
        textDisplay.fontSize = fontSize;
        textDisplay.transform.SetAsLastSibling();
        
        if (enableDarkening && !alreadyDarkened && canvas != null)
        {
            if (!canvasWasEnabled)
            {
                canvasWasEnabled = canvas.gameObject.activeSelf;
                if (canvasGroup != null)
                {
                    originalCanvasAlpha = canvasGroup.alpha;
                }
            }
            
            if (canvasGroup != null)
            {
                StartCoroutine(FadeOutCanvas());
            }
            else
            {
                canvas.gameObject.SetActive(false);
            }
        }
        
        if (enableDarkening && darkOverlay != null)
        {
            darkOverlay.gameObject.SetActive(true);
            darkOverlay.transform.SetAsFirstSibling();
        }
        
        yield return StartCoroutine(FadeInWithOverlay(alreadyDarkened));
        yield return new WaitForSecondsRealtime(displayDuration);
        
        if (freezeGame)
        {
            Time.timeScale = 1f;
        }
        
        yield return StartCoroutine(FadeOutWithOverlay(hasMoreEntries));
        textDisplay.gameObject.SetActive(false);
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
        if (textDisplay == null) yield break;
        
        // Freeze game if enabled
        if (freezeGame)
        {
            Time.timeScale = 0f;
        }
        
        textDisplay.gameObject.SetActive(true);
        textDisplay.text = message;
        textDisplay.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
        textDisplay.fontSize = fontSize;
        textDisplay.transform.SetAsLastSibling();
        
        if (enableDarkening && !alreadyDarkened && canvas != null)
        {
            if (!canvasWasEnabled && canvas.gameObject.activeSelf)
            {
                canvasWasEnabled = true;
                if (canvasGroup != null)
                {
                    originalCanvasAlpha = canvasGroup.alpha;
                }
            }
            
            if (canvasGroup != null)
            {
                StartCoroutine(FadeOutCanvas());
            }
            else
            {
                canvas.gameObject.SetActive(false);
            }
        }
        
        if (enableDarkening && darkOverlay != null)
        {
            darkOverlay.gameObject.SetActive(true);
            darkOverlay.transform.SetAsFirstSibling();
        }
        
        yield return StartCoroutine(FadeInWithOverlay(alreadyDarkened));
        yield return new WaitForSecondsRealtime(displayDuration);
        
        if (freezeGame)
        {
            Time.timeScale = 1f;
        }
        
        yield return StartCoroutine(FadeOutWithOverlay());
        textDisplay.gameObject.SetActive(false);
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
            StartCoroutine(TriggerEntryWithDelay(index, entry));
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
    
    private IEnumerator FadeInWithOverlay(bool alreadyDarkened = false)
    {
        // Determine which effects need to fade in
        bool needsTextFade = enableTextFadeIn;
        bool needsOverlayFade = enableDarkening && darkOverlay != null && !alreadyDarkened;
        
        // If neither needs fading, skip the coroutine
        if (!needsTextFade && !needsOverlayFade)
        {
            // Set final values immediately
            if (textDisplay != null)
            {
                textDisplay.color = textColor;
            }
            if (enableDarkening && darkOverlay != null)
            {
                darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 1f);
            }
            yield break;
        }
        
        // Calculate the maximum duration needed (adjusted by speed multipliers)
        float maxDuration = 0f;
        float adjustedTextFadeInDuration = needsTextFade ? textFadeInDuration / textFadeSpeed : 0f;
        float adjustedOverlayFadeInDuration = needsOverlayFade ? overlayFadeInDuration / overlayFadeSpeed : 0f;
        if (needsTextFade) maxDuration = Mathf.Max(maxDuration, adjustedTextFadeInDuration);
        if (needsOverlayFade) maxDuration = Mathf.Max(maxDuration, adjustedOverlayFadeInDuration);
        
        Color startTextColor = new Color(textColor.r, textColor.g, textColor.b, 0f);
        Color targetTextColor = textColor;
        
        if (textDisplay != null)
        {
            if (needsTextFade)
            {
                textDisplay.color = startTextColor;
            }
            else
            {
                textDisplay.color = targetTextColor;
            }
        }
        
        float startOverlayAlpha = alreadyDarkened ? 1f : 0f;
        float targetOverlayAlpha = 1f;
        
        if (darkOverlay != null && !alreadyDarkened)
        {
            startOverlayAlpha = darkOverlay.color.a;
        }
        
        if (needsOverlayFade && darkOverlay != null)
        {
            darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, startOverlayAlpha);
        }
        else if (enableDarkening && darkOverlay != null)
        {
            darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, targetOverlayAlpha);
        }
        
        float elapsed = 0f;
        
        while (elapsed < maxDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            
            if (needsTextFade && textDisplay != null)
            {
                float rawTextT = Mathf.Clamp01(elapsed / adjustedTextFadeInDuration);
                float textT = rawTextT * rawTextT * (3f - 2f * rawTextT);
                textDisplay.color = Color.Lerp(startTextColor, targetTextColor, textT);
            }
            
            if (needsOverlayFade && darkOverlay != null)
            {
                float rawOverlayT = Mathf.Clamp01(elapsed / adjustedOverlayFadeInDuration);
                float overlayT = rawOverlayT * rawOverlayT * (3f - 2f * rawOverlayT);
                float currentAlpha = Mathf.Lerp(startOverlayAlpha, targetOverlayAlpha, overlayT);
                darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, currentAlpha);
            }
            
            yield return null;
        }
        if (textDisplay != null)
        {
            textDisplay.color = targetTextColor;
        }
        
        if (enableDarkening && darkOverlay != null)
        {
            darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, targetOverlayAlpha);
        }
    }
    
    private IEnumerator FadeOutWithOverlay(bool keepOverlayDark = false)
    {
        // Calculate adjusted durations based on speed multipliers
        float adjustedTextFadeOutDuration = textFadeOutDuration / textFadeSpeed;
        float adjustedOverlayFadeOutDuration = overlayFadeOutDuration / overlayFadeSpeed;
        
        if (textDisplay != null)
        {
            Color startTextColor = textDisplay.color;
            Color targetTextColor = new Color(startTextColor.r, startTextColor.g, startTextColor.b, 0f);
            
            float textElapsed = 0f;
            while (textElapsed < adjustedTextFadeOutDuration)
            {
                textElapsed += Time.unscaledDeltaTime;
                float rawT = Mathf.Clamp01(textElapsed / adjustedTextFadeOutDuration);
                float t = rawT * rawT * (3f - 2f * rawT);
                textDisplay.color = Color.Lerp(startTextColor, targetTextColor, t);
                yield return null;
            }
            
            textDisplay.color = targetTextColor;
            
            if (keepOverlayDark)
            {
                yield return null;
            }
        }
        
        if (!keepOverlayDark && enableDarkening && darkOverlay != null)
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
                float t = rawT * rawT * (3f - 2f * rawT);
                float currentAlpha = Mathf.Lerp(startOverlayAlpha, targetOverlayAlpha, t);
                darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, currentAlpha);
                
                if (!canvasStarted && overlayElapsed >= overlayFadeHalfway && canvas != null && (canvasWasEnabled || originalCanvasAlpha > 0f))
                {
                    canvasRestoreCoroutine = StartCoroutine(FadeInCanvas());
                    canvasStarted = true;
                }
                
                yield return null;
            }
            
            darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, targetOverlayAlpha);
            
            if (canvasRestoreCoroutine != null)
            {
                yield return canvasRestoreCoroutine;
            }
        }
        else if (keepOverlayDark && enableDarkening && darkOverlay != null)
        {
            darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 1f);
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
        canvas.gameObject.SetActive(false);
    }
    
    private IEnumerator FadeInCanvas()
    {
        if (canvas == null || canvasGroup == null) yield break;
        
        canvas.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        
        float elapsed = 0f;
        
        while (elapsed < canvasFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / canvasFadeDuration;
            canvasGroup.alpha = Mathf.Lerp(0f, originalCanvasAlpha, t);
            yield return null;
        }
        
        canvasGroup.alpha = originalCanvasAlpha;
    }
    
    /// <summary>
    /// Clear the message queue and stop current display
    /// </summary>
    public void ClearQueue()
    {
        messageQueue.Clear();
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
            currentDisplayCoroutine = null;
        }
        
        if (freezeGame)
        {
            Time.timeScale = 1f;
        }
        
        if (textDisplay != null)
        {
            textDisplay.gameObject.SetActive(false);
        }
        
        if (enableDarkening && darkOverlay != null)
        {
            darkOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f);
        }
        
        if (enableDarkening && canvas != null && canvasWasEnabled)
        {
            canvas.gameObject.SetActive(true);
        }
        
        isDisplaying = false;
    }
}



