using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ShopInfo : MonoBehaviour
{
    public CanvasGroup infoPanel;

    public TMP_Text itemNameText; 
    public TMP_Text itemDescriptionText;

    private RectTransform infoPanelReact;
    private bool isShowing = false;

    private void Awake()
    {
        // Ensure the info panel starts hidden
        if (infoPanel != null)
        {
            infoPanelReact = infoPanel.GetComponent<RectTransform>();
            infoPanel.alpha = 0;
            // Make sure the panel doesn't block raycasts when invisible
            infoPanel.blocksRaycasts = false;
            infoPanel.interactable = false;
        }
        else
        {
            Debug.LogError("ShopInfo: infoPanel CanvasGroup is not assigned!");
        }
    }
    
    private void Update()
    {
        // Continuously update position while showing
        if (isShowing && infoPanel != null && infoPanel.alpha > 0)
        {
            FollowMouse();
        }
    }

    public void ShowItemInfo(ItemSO itemSO)
    {
        if (infoPanel == null) return;
        
        itemNameText.text = itemSO.itemName;
        itemDescriptionText.text = itemSO.itemDescription;
        infoPanel.alpha = 1;
        isShowing = true;
        gameObject.SetActive(true);
        
        // Position the popup at the mouse when first shown
        FollowMouse();
    }
    
    // Overload for direct string input (used by ItemSlot)
    public void ShowItemInfo(string name, string description)
    {
        if (infoPanel == null) return;
        
        itemNameText.text = name;
        itemDescriptionText.text = description;
        infoPanel.alpha = 1;
        isShowing = true;
        gameObject.SetActive(true);
        
        // Position the popup at the mouse when first shown
        FollowMouse();
    }
    
    public void HideItemInfo()
    {
        if (infoPanel == null) return;
        
        infoPanel.alpha = 0;
        isShowing = false;
        itemNameText.text = "";
        itemDescriptionText.text = "";
    }
    public void FollowMouse()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 offset = new Vector3(10f, -10f, 0f);
        infoPanelReact.position = mousePosition + offset;
    }

    
}
