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
        if (rewardPanels == null || rewardPanels.Length == 0)
        {
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
                rewardButtons[i] = null;
                continue;
            }

            Button button = panel.GetComponent<Button>();

            if (button == null)
            {
                button = panel.GetComponentInChildren<Button>(true);
            }

            if (button == null)
            {
                rewardButtons[i] = null;
                Debug.LogWarning($"Reward panel at index {i} does not contain a Button component.");
                continue;
            }

            rewardButtons[i] = button;
            
            // Remove existing listeners to prevent duplicates
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => HandleRewardSelection(index));
        }
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