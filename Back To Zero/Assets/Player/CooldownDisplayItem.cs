using UnityEngine;
using TMPro;

public class CooldownDisplayItem : MonoBehaviour
{
    [SerializeField] private TMP_Text keybindText;
    
    public TMP_Text GetKeybindText()
    {
        if (keybindText == null)
            keybindText = GetComponentInChildren<TMP_Text>();
        return keybindText;
    }
}

