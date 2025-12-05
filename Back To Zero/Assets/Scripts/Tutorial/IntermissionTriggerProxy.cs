using UnityEngine;

/// <summary>
/// Place this on a child object with a Collider/Rigidbody to forward trigger events 
/// to the IntermissionTextDisplay manager.
/// </summary>
public class IntermissionTriggerProxy : MonoBehaviour
{
    [Tooltip("Reference to the manager. If null, will try to find it in parent or scene.")]
    [SerializeField] private IntermissionTextDisplay manager;
    
    [Tooltip("Only trigger if the object entering has this tag (e.g. 'Player')")]
    [SerializeField] private string targetTag = "Player";
    
    [HideInInspector]
    public int entryIndex = 0;

    private void Start()
    {
        if (manager == null)
        {
            manager = GetComponentInParent<IntermissionTextDisplay>();
            if (manager == null)
            {
                manager = FindObjectOfType<IntermissionTextDisplay>();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag)) return;
        
        if (manager != null)
        {
            Debug.Log($"[IntermissionTriggerProxy] Forwarding OnTriggerEnter2D from {gameObject.name} to Entry {entryIndex}");
            manager.TryTriggerEntry(entryIndex);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!string.IsNullOrEmpty(targetTag) && !collision.gameObject.CompareTag(targetTag)) return;

        if (manager != null)
        {
            Debug.Log($"[IntermissionTriggerProxy] Forwarding OnCollisionEnter2D from {gameObject.name} to Entry {entryIndex}");
            manager.TryTriggerEntry(entryIndex);
        }
    }
}
