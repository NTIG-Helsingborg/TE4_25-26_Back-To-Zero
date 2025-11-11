using UnityEngine;

public class ShopButtonToggles : MonoBehaviour
{
    public void OpenItemShop()
    {
        if(Merchant.currentShopKeeper != null)
        {
            Merchant.currentShopKeeper.OpenItemShop();
        }
    }
    public void OpenArtifactShop()
    {
        if(Merchant.currentShopKeeper != null)
        {
            Merchant.currentShopKeeper.OpenArtifactShop();
        }
    }
    public void OpenAbilityShop()
    {
        if(Merchant.currentShopKeeper != null)
        {
            Merchant.currentShopKeeper.OpenAbilityShop();
        }
    }
}