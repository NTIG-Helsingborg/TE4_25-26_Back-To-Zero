using UnityEngine;
using System;
using System.Collections.Generic;

public class Merchant : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Root GameObject of the shop UI (Canvas or panel) to toggle.")]
    [SerializeField] private GameObject shopRoot;
    [SerializeField] private ShopManager shopManager;

    [Header("Player Detection")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private Transform playerTransform;

    [SerializeField] private List<ShopItems> shopItems;
    [SerializeField] private List<ShopItems> shopArtifacts;
    [SerializeField] private List<ShopItems> shopAbilities;
    public static event Action<ShopManager, bool> OnShopStateChanged;

    public static Merchant currentShopKeeper;


    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.B;

    private bool shopOpen = false;

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        if (shopRoot != null)
        {
            shopRoot.SetActive(false);
        }
    }

    void Update()
    {
        if (playerTransform == null || shopRoot == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool inRange = dist <= interactionRange;

        if (inRange && Input.GetKeyDown(toggleKey))
        {
            ToggleShop();
        }

        if (shopOpen && !inRange)
        {
            CloseShop();
        }
    }

    private void ToggleShop()
    {
        if (shopOpen) CloseShop();
        else OpenShop();
    }

    private void OpenShop()
    {
        if (shopRoot == null) return;
        
        // Close inventory if it's open
        InventoryManager inventoryManager = GameObject.Find("Player")?.GetComponent<InventoryManager>();
        if (inventoryManager != null)
        {
            // This will trigger before we set shopOpen, so inventory will close
            OnShopStateChanged?.Invoke(shopManager, true);
        }
        
        shopRoot.SetActive(true);
        shopOpen = true;
        Time.timeScale = 0f;
        currentShopKeeper = this;
        
        if (shopManager != null)
        {
            shopManager.PopulateShopItems(shopItems);
        }
    }

    private void CloseShop()
    {
        if (shopRoot == null) return;
        shopRoot.SetActive(false);
        shopOpen = false;
        Time.timeScale = 1f;
        OnShopStateChanged?.Invoke(shopManager, false);
        currentShopKeeper = null;
    }

    public void OnCloseButtonClicked()
    {
        CloseShop();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }

    public void OpenMiscShop()
    {
        shopManager.PopulateShopItems(shopItems);
    }
    
    public void OpenItemShop()
    {
        shopManager.PopulateShopItems(shopItems);
    }
    
    public void OpenArtifactShop()
    {
         shopManager.PopulateShopItems(shopArtifacts);
    }
    
    public void OpenAbilityShop()
    {
        shopManager.PopulateShopItems(shopAbilities);
    }



}
