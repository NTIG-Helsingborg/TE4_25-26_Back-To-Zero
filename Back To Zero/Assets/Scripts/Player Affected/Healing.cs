using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Dark-Souls style healing behaviour, supporting both inventory usage and direct input.
/// Applies a windup, slows the player, then restores health when the cast completes.
/// </summary>
public class Healing : MonoBehaviour
{
    [Header("Input (Optional)")]
    [Tooltip("Input action that triggers the heal sequence directly.")]
    [SerializeField] private InputActionReference healAction;
    [Tooltip("Inventory manager used to check or consume items when healing via input.")]
    [SerializeField] private InventoryManager inventoryManager;
    [Tooltip("Item name to consume when healing via input. Auto-populated from Healing Item Definition if assigned.")]
    [SerializeField] private string healingItemName = "Potion";
    [Tooltip("Item definition providing heal amount when using the input action. If assigned, item name will be auto-used from this.")]
    [SerializeField] private ItemSO healingItemDefinition;
    [Tooltip("Should the input-triggered heal consume an item once the heal completes?")]
    [SerializeField] private bool consumeItemOnHeal = true;

    [Header("Healing")]
    [Tooltip("Amount of health restored if the item definition does not provide one.")]
    [SerializeField] private int fallbackHealAmount = 30;
    [Tooltip("Delay before the heal is applied.")]
    [SerializeField] private float healWindupDuration = 1.35f;
    [Tooltip("Additional delay after the heal lands before restoring movement.")]
    [SerializeField] private float postHealRecovery = 0.2f;
    [Tooltip("Movement multiplier applied while healing.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float healingSlowMultiplier = 0.3f;

    private PlayerMove playerMove;
    private Health playerHealth;
    private Coroutine healRoutine;
    private float cachedMoveSpeed;
    private int pendingHealAmount;
    private bool pendingConsumeItem;
    private string pendingItemName;
    private bool isHealing;

    public bool IsHealing => isHealing;

    /// <summary>
    /// Gets the effective healing item name - uses ItemSO name if available, otherwise falls back to healingItemName string.
    /// </summary>
    private string GetEffectiveItemName()
    {
        if (healingItemDefinition != null && !string.IsNullOrEmpty(healingItemDefinition.itemName))
        {
            return healingItemDefinition.itemName;
        }
        return healingItemName;
    }

    private void Awake()
    {
        playerMove = GetComponent<PlayerMove>();
        playerHealth = GetComponent<Health>();
        if (inventoryManager == null)
        {
            inventoryManager = GetComponent<InventoryManager>();
        }
    }

    private void OnEnable()
    {
        if (healAction != null)
        {
            healAction.action.performed += OnHealPerformed;
            healAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (healAction != null)
        {
            healAction.action.performed -= OnHealPerformed;
            healAction.action.Disable();
        }

        if (healRoutine != null)
        {
            StopCoroutine(healRoutine);
            healRoutine = null;
        }

        ResetMovementSpeed();
        isHealing = false;
        pendingHealAmount = 0;
        pendingConsumeItem = false;
        pendingItemName = null;
    }

    /// <summary>
    /// Attempts to start the heal using data from the provided item definition.
    /// </summary>
    public bool TryStartHeal(ItemSO itemDefinition)
    {
        if (itemDefinition == null)
        {
            return false;
        }

        int amount = ResolveHealAmount(itemDefinition, fallbackHealAmount);
        
        // If this is being called from inventory usage, we should consume the item
        bool shouldConsume = itemDefinition.statToChange == ItemSO.StatToChange.Health &&
                            inventoryManager != null &&
                            !string.IsNullOrEmpty(itemDefinition.itemName);
        
        if (shouldConsume)
        {
            // Check if we have the item before starting heal
            if (inventoryManager.GetItemCount(itemDefinition.itemName) <= 0)
            {
                return false;
            }
            
            // Store the item name for consumption after heal completes
            pendingItemName = itemDefinition.itemName;
            pendingConsumeItem = true;
        }
        
        return TryStartHeal(amount, consumeAfterCast: shouldConsume);
    }

    /// <summary>
    /// Attempts to start the heal using a direct heal amount.
    /// </summary>
    public bool TryStartHeal(int healAmount, bool consumeAfterCast = false)
    {
        if (healAmount <= 0)
        {
            healAmount = fallbackHealAmount;
        }

        if (healAmount <= 0 || isHealing)
        {
            return false;
        }

        if (playerHealth == null)
        {
            Debug.LogWarning($"{nameof(Healing)}: Missing Health component.");
            return false;
        }

        if (playerHealth.IsFullHealth())
        {
            return false;
        }

        pendingHealAmount = healAmount;
        pendingConsumeItem = consumeAfterCast;
        // Only override pendingItemName if it's not already set (from TryStartHeal(ItemSO))
        if (consumeAfterCast && string.IsNullOrEmpty(pendingItemName))
        {
            pendingItemName = GetEffectiveItemName();
        }
        healRoutine = StartCoroutine(HealRoutine());
        return true;
    }

    private void OnHealPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        TryStartHealFromInput($"InputActionReference (phase={context.phase})");
    }

    public void OnHeal(InputValue value)
    {
        if (!value.isPressed)
        {
            return;
        }

        TryStartHealFromInput("PlayerInput.SendMessage");
    }

    private void TryStartHealFromInput(string debugSource = null)
    {
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(debugSource))
        {
            Debug.Log($"{nameof(Healing)}: Heal input triggered via {debugSource}.");
        }
#endif

        if (isHealing || playerHealth == null)
        {
            return;
        }

        ItemSO definitionToUse = healingItemDefinition;
        int healAmount = ResolveHealAmount(definitionToUse, fallbackHealAmount);

        // Get the effective item name (from ItemSO if available, otherwise from string field)
        string effectiveItemName = GetEffectiveItemName();
        
        bool shouldConsume = consumeItemOnHeal &&
                             inventoryManager != null &&
                             !string.IsNullOrEmpty(effectiveItemName);

        if (shouldConsume && inventoryManager.GetItemCount(effectiveItemName) <= 0)
        {
            return;
        }

        bool started = TryStartHeal(healAmount, shouldConsume);
        if (!started)
        {
            pendingConsumeItem = false;
            pendingItemName = null;
        }
    }

    private IEnumerator HealRoutine()
    {
        isHealing = true;
        CacheAndReduceMoveSpeed();

        if (healWindupDuration > 0f)
        {
            yield return new WaitForSeconds(healWindupDuration);
        }

        if (pendingHealAmount > 0)
        {
            playerHealth.Heal(pendingHealAmount);
        }

        if (pendingConsumeItem &&
            inventoryManager != null &&
            !string.IsNullOrEmpty(pendingItemName))
        {
            bool removed = inventoryManager.RemoveItem(pendingItemName, 1);
            if (!removed)
            {
                Debug.LogWarning($"{nameof(Healing)}: Failed to remove item '{pendingItemName}' after healing.");
            }
        }

        if (postHealRecovery > 0f)
        {
            yield return new WaitForSeconds(postHealRecovery);
        }

        ResetMovementSpeed();
        isHealing = false;
        pendingHealAmount = 0;
        pendingConsumeItem = false;
        pendingItemName = null;
        healRoutine = null;
    }

    private static int ResolveHealAmount(ItemSO itemDefinition, int fallbackAmount)
    {
        if (itemDefinition != null && itemDefinition.statToChange == ItemSO.StatToChange.Health)
        {
            int definedAmount = itemDefinition.amountToChangeStat;
            if (definedAmount > 0)
            {
                return definedAmount;
            }
        }

        return fallbackAmount;
    }

    private void CacheAndReduceMoveSpeed()
    {
        if (playerMove == null)
        {
            return;
        }

        cachedMoveSpeed = playerMove.MoveSpeed;
        playerMove.MoveSpeed = Mathf.Max(0f, cachedMoveSpeed * healingSlowMultiplier);
    }

    private void ResetMovementSpeed()
    {
        if (playerMove == null)
        {
            return;
        }

        if (cachedMoveSpeed > 0f)
        {
            playerMove.MoveSpeed = cachedMoveSpeed;
        }
    }
}