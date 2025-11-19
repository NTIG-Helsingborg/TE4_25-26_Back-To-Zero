using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExperienceManager : MonoBehaviour
{
    public static ExperienceManager Instance { get; private set; }

    [Header("Experience")]
    [SerializeField] AnimationCurve experienceCurve;

    int currentLevel;
    int totalExperience;
    int previousLevelsExperience;
    int nextLevelsExperience;

    [Header("Interface")]
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] TextMeshProUGUI experienceText;
    [SerializeField] Image experienceFill;

    [Header("Level Up UI")]
    [SerializeField] GameObject levelUpCanvas;
    [SerializeField] GameObject levelUpMenu;
    [SerializeField] GameObject[] rewardPanels;

    Button[] rewardButtons;

    int pendingLevelUps;
    bool levelUpMenuOpen;
    int lastDisplayedLevel = -1;

    // Safety: Prevent infinite loops
    private const int MAX_LEVEL_UPS_PER_FRAME = 100;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ExperienceManager instances detected. Using the most recent one.");
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        // Initialize with safe defaults if curve is missing
        if (experienceCurve == null)
        {
            Debug.LogError("ExperienceManager: experienceCurve is not assigned! Creating default curve.");
            experienceCurve = AnimationCurve.Linear(0, 0, 100, 10000);
        }

        UpdateLevel();
        lastDisplayedLevel = currentLevel;
        UpdateInterface();
        ConfigureLevelUpMenu();
        HideLevelUpMenu();
    }

    public void AddExperience(int amount)
    {
        // Safety check: ensure instance is valid
        if (Instance == null || Instance != this)
        {
            Debug.LogWarning("ExperienceManager: AddExperience called but instance is null or invalid.");
            return;
        }

        if (amount <= 0)
        {
            return;
        }

        totalExperience += amount;
        CheckForLevelUp();
        UpdateInterface();
    }

    void CheckForLevelUp()
    {
        if (experienceCurve == null)
        {
            Debug.LogError("ExperienceManager: Cannot check for level up - experienceCurve is null!");
            return;
        }

        bool leveledUp = false;
        int levelUpCount = 0;

        // Safety: Prevent infinite loops
        while (totalExperience >= nextLevelsExperience && levelUpCount < MAX_LEVEL_UPS_PER_FRAME)
        {
            currentLevel++;
            pendingLevelUps++;
            UpdateLevel();
            leveledUp = true;
            levelUpCount++;

            // Additional safety: if nextLevelsExperience didn't increase, break to prevent infinite loop
            if (nextLevelsExperience <= previousLevelsExperience)
            {
                Debug.LogWarning($"ExperienceManager: Experience curve may be invalid at level {currentLevel}. Breaking level up loop.");
                break;
            }
        }

        if (levelUpCount >= MAX_LEVEL_UPS_PER_FRAME)
        {
            Debug.LogWarning($"ExperienceManager: Reached maximum level ups per frame ({MAX_LEVEL_UPS_PER_FRAME}). Some level ups may be delayed.");
        }

        if (leveledUp)
        {
            TryOpenLevelUpMenu();
        }
    }

    void UpdateLevel()
    {
        if (experienceCurve == null)
        {
            Debug.LogError("ExperienceManager: Cannot update level - experienceCurve is null!");
            return;
        }

        previousLevelsExperience = (int)experienceCurve.Evaluate(currentLevel);
        nextLevelsExperience = (int)experienceCurve.Evaluate(currentLevel + 1);

        // Ensure experience requirement always increases
        if (nextLevelsExperience <= previousLevelsExperience)
        {
            nextLevelsExperience = previousLevelsExperience + 1;
        }
    }

    void UpdateInterface()
    {
        // Safety: Check for null UI elements before accessing
        if (levelText == null && experienceText == null && experienceFill == null)
        {
            // UI elements not assigned - this is okay, just skip UI update
            return;
        }

        int start = totalExperience - previousLevelsExperience;
        int end = nextLevelsExperience - previousLevelsExperience;

        if (start < 0)
        {
            start = 0;
        }

        // Safe UI updates with null checks
        if (levelText != null)
        {
            levelText.text = currentLevel.ToString();
        }

        if (experienceText != null)
        {
            experienceText.text = start + " exp / " + end + " exp";
        }

        if (experienceFill != null)
        {
            experienceFill.fillAmount = end > 0 ? (float)start / end : 0f;
        }

        if (lastDisplayedLevel != currentLevel)
        {
            lastDisplayedLevel = currentLevel;
            ShowLevelUpCanvas();
        }
    }

    void ConfigureLevelUpMenu()
    {
        Debug.Log($"ExperienceManager: ConfigureLevelUpMenu called. rewardPanels is null: {rewardPanels == null}, Length: {(rewardPanels != null ? rewardPanels.Length : 0)}");
        
        if (rewardPanels == null || rewardPanels.Length == 0)
        {
            Debug.LogWarning("ExperienceManager: rewardPanels is null or empty. Cannot configure level up menu.");
            return;
        }

        if (rewardButtons == null || rewardButtons.Length != rewardPanels.Length)
        {
            // Clear old listeners if reconfiguring
            if (rewardButtons != null)
            {
                ClearRewardButtonListeners();
            }
            rewardButtons = new Button[rewardPanels.Length];
        }

        for (int i = 0; i < rewardPanels.Length; i++)
        {
            int index = i;
            GameObject panel = rewardPanels[i];

            if (panel == null)
            {
                Debug.LogWarning($"ExperienceManager: Reward panel at index {i} is null.");
                rewardButtons[i] = null;
                continue;
            }

            Debug.Log($"ExperienceManager: Configuring button for panel {i}: {panel.name}");

            Button button = panel.GetComponent<Button>();

            // If not found on panel, search children
            if (button == null)
            {
                Debug.Log($"ExperienceManager: No Button on {panel.name}, searching children...");
                button = panel.GetComponentInChildren<Button>(true);
            }

            // If still not found, try to find Button GameObject by name pattern (Button1, Button2, Button3)
            if (button == null)
            {
                Debug.Log($"ExperienceManager: No Button in children of {panel.name}, searching for Button{i + 1} GameObject...");
                Transform parent = panel.transform.parent;
                if (parent != null)
                {
                    Transform buttonTransform = parent.Find($"Button{i + 1}");
                    if (buttonTransform != null)
                    {
                        button = buttonTransform.GetComponent<Button>();
                        if (button != null)
                        {
                            Debug.Log($"ExperienceManager: Found Button component on Button{i + 1} GameObject.");
                        }
                    }
                }
            }

            // If still not found, search all siblings
            if (button == null)
            {
                Debug.Log($"ExperienceManager: Searching siblings of {panel.name}...");
                Transform parent = panel.transform.parent;
                if (parent != null)
                {
                    for (int j = 0; j < parent.childCount; j++)
                    {
                        Transform sibling = parent.GetChild(j);
                        if (sibling.name.Contains($"Button{i + 1}") || 
                            (i == 0 && sibling.name == "Button1") ||
                            (i == 1 && sibling.name == "Button2") ||
                            (i == 2 && sibling.name == "Button3"))
                        {
                            button = sibling.GetComponent<Button>();
                            if (button != null)
                            {
                                Debug.Log($"ExperienceManager: Found Button component on sibling {sibling.name}.");
                                break;
                            }
                        }
                    }
                }
            }

            // If still not found, search in parent's parent (ButtonWraper might be a sibling of LevelUpMenu)
            if (button == null)
            {
                Debug.Log($"ExperienceManager: Searching parent's parent for Button{i + 1}...");
                Transform parent = panel.transform.parent;
                if (parent != null && parent.parent != null)
                {
                    Transform grandParent = parent.parent;
                    // Search all children of grandparent
                    for (int j = 0; j < grandParent.childCount; j++)
                    {
                        Transform child = grandParent.GetChild(j);
                        // Check if this is ButtonWraper or contains Button GameObjects
                        Transform buttonTransform = child.Find($"Button{i + 1}");
                        if (buttonTransform != null)
                        {
                            button = buttonTransform.GetComponent<Button>();
                            if (button != null)
                            {
                                Debug.Log($"ExperienceManager: Found Button component on Button{i + 1} in {child.name}.");
                                break;
                            }
                        }
                        // Also check direct children
                        if (child.name == $"Button{i + 1}" || 
                            (i == 0 && child.name == "Button1") ||
                            (i == 1 && child.name == "Button2") ||
                            (i == 2 && child.name == "Button3"))
                        {
                            button = child.GetComponent<Button>();
                            if (button != null)
                            {
                                Debug.Log($"ExperienceManager: Found Button component directly on {child.name}.");
                                break;
                            }
                        }
                    }
                }
            }

            // Last resort: search entire levelUpMenu hierarchy
            if (button == null && levelUpMenu != null)
            {
                Debug.Log($"ExperienceManager: Searching entire levelUpMenu hierarchy for Button{i + 1}...");
                Transform[] allChildren = levelUpMenu.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in allChildren)
                {
                    if (child.name == $"Button{i + 1}" || 
                        (i == 0 && child.name == "Button1") ||
                        (i == 1 && child.name == "Button2") ||
                        (i == 2 && child.name == "Button3"))
                    {
                        button = child.GetComponent<Button>();
                        if (button != null)
                        {
                            Debug.Log($"ExperienceManager: Found Button component on {child.name} in levelUpMenu hierarchy.");
                            break;
                        }
                    }
                }
            }

            // Final resort: search entire levelUpCanvas hierarchy (ButtonWraper might be at canvas level)
            if (button == null && levelUpCanvas != null)
            {
                Debug.Log($"ExperienceManager: Searching entire levelUpCanvas hierarchy for Button{i + 1}...");
                Transform[] allChildren = levelUpCanvas.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in allChildren)
                {
                    if (child.name == $"Button{i + 1}" || 
                        (i == 0 && child.name == "Button1") ||
                        (i == 1 && child.name == "Button2") ||
                        (i == 2 && child.name == "Button3"))
                    {
                        button = child.GetComponent<Button>();
                        if (button != null)
                        {
                            Debug.Log($"ExperienceManager: Found Button component on {child.name} in levelUpCanvas hierarchy.");
                            break;
                        }
                    }
                }
            }

            if (button == null)
            {
                rewardButtons[i] = null;
                Debug.LogError($"ExperienceManager: Reward panel at index {i} ({panel.name}) does not contain a Button component and no matching Button GameObject found.");
                continue;
            }

            Debug.Log($"ExperienceManager: Found button on {button.gameObject.name}. Adding listener for index {index}.");
            rewardButtons[i] = button;
            
            // Remove existing listeners to prevent duplicates
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => HandleRewardSelection(index));
            
            Debug.Log($"ExperienceManager: Button listener added. onClick listener count: {button.onClick.GetPersistentEventCount()}");
        }
        
        Debug.Log($"ExperienceManager: ConfigureLevelUpMenu completed. Configured {rewardPanels.Length} buttons.");
    }

    void ClearRewardButtonListeners()
    {
        if (rewardButtons == null)
            return;

        for (int i = 0; i < rewardButtons.Length; i++)
        {
            if (rewardButtons[i] != null)
            {
                rewardButtons[i].onClick.RemoveAllListeners();
            }
        }
    }

    void TryOpenLevelUpMenu()
    {
        if (pendingLevelUps <= 0 || levelUpMenuOpen)
        {
            return;
        }

        // Ensure buttons are configured before opening menu
        ConfigureLevelUpMenu();

        ShowLevelUpCanvas();

        if (levelUpMenu != null)
        {
            levelUpMenu.SetActive(true);
        }

        levelUpMenuOpen = true;
        
        // Pause the game when level up menu opens
        Time.timeScale = 0f;
    }

    void HandleRewardSelection(int rewardIndex)
    {
        if (!levelUpMenuOpen)
        {
            return;
        }

        // Safety: Validate reward index
        if (rewardIndex < 0 || (rewardPanels != null && rewardIndex >= rewardPanels.Length))
        {
            Debug.LogWarning($"ExperienceManager: Invalid reward index {rewardIndex}. Ignoring selection.");
            return;
        }

        GrantReward(rewardIndex);
        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
        HideLevelUpMenu();

        if (pendingLevelUps > 0)
        {
            TryOpenLevelUpMenu();
        }
    }

    void GrantReward(int rewardIndex)
    {
        Debug.Log($"Reward {rewardIndex + 1} selected. (Reward effect not yet implemented.)");
    }

    void ShowLevelUpCanvas()
    {
        if (levelUpCanvas != null)
        {
            levelUpCanvas.SetActive(true);
        }
    }

    void HideLevelUpMenu()
    {
        if (levelUpCanvas != null)
        {
            levelUpCanvas.SetActive(false);
        }

        if (levelUpMenu != null)
        {
            levelUpMenu.SetActive(false);
        }

        levelUpMenuOpen = false;
        
        // Unpause the game when level up menu closes (only if no more pending level ups)
        if (pendingLevelUps <= 0)
        {
            Time.timeScale = 1f;
        }
    }
}