using UnityEngine;

public class InventoryButtons : MonoBehaviour
{
    [SerializeField] private GameObject InventoryMenu;
    [SerializeField] private GameObject InventoryMenuEquipment;
    [SerializeField] private GameObject InventoryMenuAbility;

    public void OpenInventoryMenu()
    {
        if (InventoryMenu != null) InventoryMenu.SetActive(true);
        if (InventoryMenuEquipment != null) InventoryMenuEquipment.SetActive(false);
        if (InventoryMenuAbility != null) InventoryMenuAbility.SetActive(false);
    }

    public void OpenInventoryMenuEquipment()
    {
        if (InventoryMenu != null) InventoryMenu.SetActive(false);
        if (InventoryMenuEquipment != null) InventoryMenuEquipment.SetActive(true);
        if (InventoryMenuAbility != null) InventoryMenuAbility.SetActive(false);
    }

    public void OpenInventoryMenuAbility()
    {
        if (InventoryMenu != null) InventoryMenu.SetActive(false);
        if (InventoryMenuEquipment != null) InventoryMenuEquipment.SetActive(false);
        if (InventoryMenuAbility != null) InventoryMenuAbility.SetActive(true);
    }
}
