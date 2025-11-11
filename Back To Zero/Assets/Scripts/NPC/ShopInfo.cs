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

    private void Awake()
    {
    infoPanelReact = GetComponent<RectTransform>();

    }

    public void ShowItemInfo(ItemSO itemSO)
    {
        itemNameText.text = itemSO.itemName;
        itemDescriptionText.text = itemSO.itemDescription;
        infoPanel.alpha = 1;
    }
    public void HideItemInfo()
    {
        infoPanel.alpha = 0;
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
