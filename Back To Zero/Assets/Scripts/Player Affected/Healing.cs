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
    [Tooltip("Item name to consume when healing via input.")]
    [SerializeField] private string healingItemName = "Potion";
    [Tooltip("Item definition providing heal amount when using the input action.")]
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
        int amount = ResolveHealAmount(itemDefinition, fallbackHealAmount);
        return TryStartHeal(amount, consumeAfterCast: false);
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
        pendingItemName = consumeAfterCast ? healingItemName : null;
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

        bool shouldConsume = consumeItemOnHeal &&
                             inventoryManager != null &&
                             !string.IsNullOrEmpty(healingItemName);

        if (shouldConsume && inventoryManager.GetItemCount(healingItemName) <= 0)
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