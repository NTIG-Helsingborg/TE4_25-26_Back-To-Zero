using UnityEngine;

/// <summary>
/// Example script showing how to set up simple tutorial intermissions
/// Attach this to your IntermissionTextDisplay GameObject and configure the entries
/// </summary>
public class SimpleTutorialExample : MonoBehaviour
{
    [Header("Tutorial Setup Example")]
    [TextArea(5, 10)]
    [SerializeField] private string instructions = 
        "SIMPLE TUTORIAL SETUP:\n\n" +
        "1. Find your IntermissionTextDisplay GameObject\n" +
        "2. Add tutorial entries to the Intermission Entries array\n" +
        "3. For each entry:\n" +
        "   - Set text message\n" +
        "   - Set trigger (OnStart, OnTriggerEnter, etc.)\n" +
        "   - Optional: Drag GameObject to 'Highlight Object'\n" +
        "   - Optional: Check 'Open Inventory' to auto-open inventory\n" +
        "   - Optional: Check 'Close Inventory After' to auto-close\n\n" +
        "EXAMPLE TUTORIAL FLOW:\n\n" +
        "Entry 0: Welcome\n" +
        "  Text: 'Your HP is your Mana! Abilities cost health.'\n" +
        "  Trigger: OnStart\n" +
        "  Open Inventory: false\n\n" +
        "Entry 1: Inventory Guide  \n" +
        "  Text: 'This is your inventory. Drag abilities to slots.'\n" +
        "  Trigger: OnPreviousComplete (or manual)\n" +
        "  Open Inventory: TRUE\n" +
        "  Highlight Object: Drag your InventoryCanvas here\n\n" +
        "Entry 2: Harvest Explanation\n" +
        "  Text: 'Use Harvest (Right Click) on enemies to restore HP!'\n" +
        "  Trigger: OnPreviousComplete\n" +
        "  Close Inventory After: TRUE\n" +
        "  Highlight Object: Drag Harvest ability slot\n\n" +
        "That's it! The existing intermission system handles everything.";
}

