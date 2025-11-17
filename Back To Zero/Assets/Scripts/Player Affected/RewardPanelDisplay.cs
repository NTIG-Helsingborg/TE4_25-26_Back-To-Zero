using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component to display reward/boon information on a UI panel
/// Attach this to each reward panel GameObject
/// </summary>
public class RewardPanelDisplay : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text component for reward name")]
    [SerializeField] private TextMeshProUGUI nameText;
    
    [Tooltip("Text component for reward description")]
    [SerializeField] private TextMeshProUGUI descriptionText;
    
    [Tooltip("Text component for stat changes")]
    [SerializeField] private TextMeshProUGUI statChangesText;
    
    [Tooltip("Text component for rarity label")]
    [SerializeField] private TextMeshProUGUI rarityText;
    
    [Tooltip("Image component for background/border (will be colored by rarity)")]
    [SerializeField] private Image backgroundImage;
    
    [Tooltip("Image component for icon (optional)")]
    [SerializeField] private Image iconImage;

    private RewardSO currentReward;

    /// <summary>
    /// Display a reward on this panel
    /// </summary>
    public void DisplayReward(RewardSO reward)
    {
        currentReward = reward;

        if (reward == null)
        {
            Debug.LogWarning("RewardPanelDisplay: Cannot display null reward!");
            return;
        }

        // Set name
        if (nameText != null)
        {
            nameText.text = reward.rewardName;
        }

        // Set description
        if (descriptionText != null)
        {
            descriptionText.text = reward.description;
        }

        // Set stat changes
        if (statChangesText != null)
        {
            statChangesText.text = reward.GetStatChangesDescription();
            
            // Color code: green for buffs, red for debuffs
            if (reward.HasDebuffs())
            {
                statChangesText.color = new Color(1f, 0.5f, 0.5f); // Light red
            }
            else
            {
                statChangesText.color = new Color(0.5f, 1f, 0.5f); // Light green
            }
        }

        // Set rarity
        if (rarityText != null)
        {
            rarityText.text = RewardRarityHelper.GetRarityName(reward.rarity);
            rarityText.color = RewardRarityHelper.GetRarityColor(reward.rarity);
        }

        // Set background color based on rarity
        if (backgroundImage != null)
        {
            Color rarityColor = RewardRarityHelper.GetRarityColor(reward.rarity);
            // Make it semi-transparent for background
            rarityColor.a = 0.3f;
            backgroundImage.color = rarityColor;
        }
    }

    /// <summary>
    /// Clear the panel display
    /// </summary>
    public void ClearDisplay()
    {
        if (nameText != null) nameText.text = "";
        if (descriptionText != null) descriptionText.text = "";
        if (statChangesText != null) statChangesText.text = "";
        if (rarityText != null) rarityText.text = "";
        if (backgroundImage != null) backgroundImage.color = Color.clear;
        currentReward = null;
    }

    /// <summary>
    /// Get the currently displayed reward
    /// </summary>
    public RewardSO GetCurrentReward()
    {
        return currentReward;
    }
}

