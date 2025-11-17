using UnityEngine;

public class Ability : ScriptableObject
{
    public new string name;
    public float cooldownTime;
    public float activeTime;
    public virtual bool CanActivate() => true;
    public virtual void Activate(){

    }
}
