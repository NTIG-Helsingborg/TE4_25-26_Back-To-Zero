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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ExperienceManager instances detected. Using the most recent one.");
        }

        Instance = this;
    }

    void Start()
    {
        // Validate experience curve
        if (experienceCurve == null || experienceCurve.keys.Length == 0)
        {
            Debug.LogError("ExperienceManager: experienceCurve is not set! Creating a default curve.");
            experienceCurve = AnimationCurve.EaseInOut(0, 0, 10, 1000);
        }
        
        // Reset to level 0/1 if something went wrong
        if (currentLevel > 50)
        {
            Debug.LogWarning($"ExperienceManager: Current level is {currentLevel} which seems wrong. Resetting to 1.");
            currentLevel = 1;
            totalExperience = 0;
        }
        
        // Start at level 1 instead of 0
        if (currentLevel == 0)
        {
            currentLevel = 1;
        }
        
        UpdateLevel();
        UpdateInterface();
    }

    public void AddExperience(int amount)
    {
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
        int maxIterations = 10; // Reasonable max level ups per kill
        int iterations = 0;
        
        while (totalExperience >= nextLevelsExperience && iterations < maxIterations)
        {
            currentLevel++;
            int oldNext = nextLevelsExperience;
            UpdateLevel();

            // Check if nextLevelsExperience actually increased meaningfully
            if (nextLevelsExperience <= oldNext)
            {
                Debug.LogError($"ExperienceManager: Experience curve issue at level {currentLevel}! Next level XP ({nextLevelsExperience}) must be > current level XP ({oldNext}). FIX YOUR CURVE!");
                // Force a reasonable increase to prevent infinite loop
                nextLevelsExperience = oldNext + 100;
                break;
            }

            Debug.Log($"Level Up! Now level {currentLevel}");
            // Start level up sequence... Possibly vfx?
            
            iterations++;
        }
        
        if (iterations >= maxIterations)
        {
            Debug.LogError($"ExperienceManager: Leveled up {maxIterations} times in one frame! Your experienceCurve is configured incorrectly. Please fix it in the Inspector.");
        }
    }

    void UpdateLevel()
    {
        previousLevelsExperience = (int)experienceCurve.Evaluate(currentLevel);
        nextLevelsExperience = (int)experienceCurve.Evaluate(currentLevel + 1);

        if (nextLevelsExperience <= previousLevelsExperience)
        {
            Debug.LogWarning($"Experience curve issue: Level {currentLevel} -> {currentLevel + 1} doesn't increase! ({previousLevelsExperience} -> {nextLevelsExperience})");
            nextLevelsExperience = previousLevelsExperience + 100;
        }
    }

    void UpdateInterface()
    {
        // Calculate progress towards next level
        int currentLevelProgress = totalExperience - previousLevelsExperience;
        int xpNeededForNextLevel = nextLevelsExperience - previousLevelsExperience;

        // Clamp values to be valid
        if (currentLevelProgress < 0)
        {
            currentLevelProgress = 0;
        }

        if (currentLevelProgress > xpNeededForNextLevel)
        {
            currentLevelProgress = xpNeededForNextLevel;
        }

        // Update UI elements
        if (levelText != null)
        {
            levelText.text = currentLevel.ToString();
        }
        
        if (experienceText != null)
        {
            experienceText.text = $"{currentLevelProgress} exp / {xpNeededForNextLevel} exp";
        }
        
        if (experienceFill != null)
        {
            float fillAmount = xpNeededForNextLevel > 0 ? (float)currentLevelProgress / xpNeededForNextLevel : 0f;
            fillAmount = Mathf.Clamp01(fillAmount);
            experienceFill.fillAmount = fillAmount;
        }
    }
}