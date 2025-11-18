using UnityEngine;

public class Ability : ScriptableObject
{
    public new string name;
    public float cooldownTime;
    public float activeTime;
    
    [Tooltip("The keybind for this ability. Can be overridden in AbilitySetter.")]
    public KeyCode keybind = KeyCode.None;

    public virtual void Activate(){

    }
}
