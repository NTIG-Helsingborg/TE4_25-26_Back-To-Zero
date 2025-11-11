using TMPro;
using UnityEngine;

public class MiniMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] InventoryManager inventoryManager;
    [SerializeField] TextMeshProUGUI moneyText;
    [SerializeField] TextMeshProUGUI crystalsText;

    [Header("Item Names")]
    [SerializeField] string moneyItemName = "Coin";
    [SerializeField] string crystalItemName = "Crystal";

    int lastMoneyAmount = -1;
    int lastCrystalAmount = -1;

    void Awake()
    {
        TryAssignInventoryManager();
    }

    void OnEnable()
    {
        RefreshDisplay(forceRefresh: true);
    }

    void Update()
    {
        RefreshDisplay();
    }

    void TryAssignInventoryManager()
    {
        if (inventoryManager != null)
        {
            return;
        }

        inventoryManager = FindFirstObjectByType<InventoryManager>();

#if UNITY_EDITOR
        if (inventoryManager == null)
        {
            Debug.LogWarning($"{nameof(MiniMenu)}: InventoryManager reference not set and could not be found in the scene.");
        }
#endif
    }

    void RefreshDisplay(bool forceRefresh = false)
    {
        if (inventoryManager == null)
        {
            TryAssignInventoryManager();
        }

        int moneyAmount = inventoryManager != null ? inventoryManager.GetItemCount(moneyItemName) : 0;
        int crystalAmount = inventoryManager != null ? inventoryManager.GetItemCount(crystalItemName) : 0;

        if (forceRefresh || moneyAmount != lastMoneyAmount)
        {
            lastMoneyAmount = moneyAmount;
            if (moneyText != null)
            {
                moneyText.text = moneyAmount.ToString();
            }
        }

        if (forceRefresh || crystalAmount != lastCrystalAmount)
        {
            lastCrystalAmount = crystalAmount;
            if (crystalsText != null)
            {
                crystalsText.text = crystalAmount.ToString();
            }
        }
    }
}
