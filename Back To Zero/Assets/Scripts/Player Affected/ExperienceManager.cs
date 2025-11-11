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
    [SerializeField] GameObject levelUpMenu;
    [SerializeField] GameObject[] rewardPanels;

    Button[] rewardButtons;

    int pendingLevelUps;
    bool levelUpMenuOpen;

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
        UpdateLevel();
        UpdateInterface();
        ConfigureLevelUpMenu();
        HideLevelUpMenu();
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
        bool leveledUp = false;

        while (totalExperience >= nextLevelsExperience)
        {
            currentLevel++;
            pendingLevelUps++;
            UpdateLevel();
            leveledUp = true;
        }

        if (leveledUp)
        {
            TryOpenLevelUpMenu();
        }
    }

    void UpdateLevel()
    {
        previousLevelsExperience = (int)experienceCurve.Evaluate(currentLevel);
        nextLevelsExperience = (int)experienceCurve.Evaluate(currentLevel + 1);

        if (nextLevelsExperience <= previousLevelsExperience)
        {
            nextLevelsExperience = previousLevelsExperience + 1;
        }
    }

    void UpdateInterface()
    {
        int start = totalExperience - previousLevelsExperience;
        int end = nextLevelsExperience - previousLevelsExperience;

        if (start < 0)
        {
            start = 0;
        }

        levelText.text = currentLevel.ToString();
        experienceText.text = start + " exp / " + end + " exp";
        experienceFill.fillAmount = end > 0 ? (float)start / end : 0f;
    }

    void ConfigureLevelUpMenu()
    {
        if (rewardPanels == null || rewardPanels.Length == 0)
        {
            return;
        }

        if (rewardButtons == null || rewardButtons.Length != rewardPanels.Length)
        {
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
            button.onClick.AddListener(() => HandleRewardSelection(index));
        }
    }

    void TryOpenLevelUpMenu()
    {
        if (pendingLevelUps <= 0 || levelUpMenuOpen)
        {
            return;
        }

        if (levelUpMenu != null)
        {
            levelUpMenu.SetActive(true);
        }

        levelUpMenuOpen = true;
    }

    void HandleRewardSelection(int rewardIndex)
    {
        if (!levelUpMenuOpen)
        {
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

    void HideLevelUpMenu()
    {
        if (levelUpMenu != null)
        {
            levelUpMenu.SetActive(false);
        }

        levelUpMenuOpen = false;
    }
}