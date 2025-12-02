using UnityEngine;

public class SlashAnimationEvents : MonoBehaviour
{
    [SerializeField] private MeleeHitbox hitbox;           // assign the Hitbox child in Inspector
    [SerializeField] private float defaultHitDuration = 0.12f;

    private GameObject owner;
    private LayerMask layers;
    private int damage;
    private float knockback;
    private bool configured;

    private void Awake()
    {
        if (!hitbox) hitbox = GetComponentInChildren<MeleeHitbox>(true);
        if (hitbox) hitbox.gameObject.SetActive(false);
    }

    // Called by the ability after Instantiate to pass runtime values
    public void Setup(GameObject owner, float damage, float knockback, LayerMask layers)
    {
        this.owner = owner;
        this.damage = Mathf.RoundToInt(damage);
        this.knockback = knockback;
        this.layers = layers;
        configured = true;

        if (!hitbox) hitbox = GetComponentInChildren<MeleeHitbox>(true);
        if (hitbox) hitbox.gameObject.SetActive(false);
    }

    // Animation Event: no-arg version
    public void HitStart()
    {
        BeginHitWindow(defaultHitDuration);
    }

    // Animation Event: use a float parameter for custom duration per frame/event
    public void HitStartWithDuration(float duration)
    {
        BeginHitWindow(Mathf.Max(0.01f, duration));
    }

    // Optional Animation Event to immediately end/cancel the hit window
    public void HitEnd()
    {
        if (hitbox) hitbox.gameObject.SetActive(false);
    }

    private void BeginHitWindow(float duration)
    {
        if (!configured || !hitbox || owner == null) return;

        // Enable the hitbox object and initialize it for a short window.
        // MeleeHitbox.Initialize will auto-destroy the Hitbox child after 'duration'.
        hitbox.gameObject.SetActive(true);
        hitbox.Initialize(damage, knockback, duration, owner, layers);
    }
}