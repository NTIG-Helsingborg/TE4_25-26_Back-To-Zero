using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CooldownDisplayItem : MonoBehaviour
{
    [SerializeField] private TMP_Text keybindText;
    [SerializeField] private Image abilityImage;
    
    public TMP_Text GetKeybindText()
    {
        if (keybindText == null)
            keybindText = GetComponentInChildren<TMP_Text>();
        return keybindText;
    }
    
    public Image GetAbilityImage()
    {
        if (abilityImage == null)
        {
            // Try to find Image named "AbilityImage" or similar
            Transform imageTransform = transform.Find("AbilityImage");
            if (imageTransform == null)
                imageTransform = transform.Find("Ability Image");
            if (imageTransform == null)
                imageTransform = transform.Find("Icon");
            
            if (imageTransform != null)
                abilityImage = imageTransform.GetComponent<Image>();
        }
        return abilityImage;
    }
}

