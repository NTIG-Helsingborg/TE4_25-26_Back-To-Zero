using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Attach this to a UI Panel or the root Canvas that covers the inventory area.
// It will detect left mouse clicks that are NOT on an ItemSlot and trigger a full deselect
// (hides all selected shaders and swap panels).
// Usage: Add a transparent Image (set its color alpha ~0 and enable Raycast Target) that spans
// the inventory background, then add this component to it. Clicking anywhere on that background
// outside of item boxes will clear selections.
public class InventoryOutsideClickHandler : MonoBehaviour, IPointerClickHandler
{
    private InventoryManager inventoryManager;
    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

    private void Awake()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogWarning("InventoryOutsideClickHandler: No InventoryManager found in scene.");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // If clicked object (or its parents) contains an ItemSlot, do nothing.
        GameObject clicked = eventData.pointerCurrentRaycast.gameObject;
        if (clicked != null && (clicked.GetComponent<ItemSlot>() != null || clicked.GetComponentInParent<ItemSlot>() != null))
            return;

        // Full deselect across all slot groups.
        if (inventoryManager != null)
        {
            inventoryManager.DeselectAllSlots();
            inventoryManager.DeselectAllArtifactSlots();
            inventoryManager.DeselectAllAbilitySlots();
        }
    }
}
