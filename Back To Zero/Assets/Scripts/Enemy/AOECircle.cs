using UnityEngine;

public class AOECircle : MonoBehaviour
{
    [Header("Visual Settings")]
    public bool useCustomSprite = false;
    public Sprite customAOESprite;
    public bool useCustomDamageSprite = false;
    public Sprite customDamageSprite;
    public int resolution = 256; public bool useGradient = true; public float gradientWidth = 0.1f; public int sortingOrder = -10;
    [Header("Color Settings")]
    public bool useCustomDamageColor = false;
    public Color customDamageColor = Color.white;
    public Color warningColor = new Color(1f, 0f, 0f, 0.3f); 
    public Color damageColor = new Color(0.8f, 0f, 0f, 0.6f);

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private EntityDamage entityDamage;

    void Start()
    {
        // Collider
        circleCollider = GetComponentInChildren<CircleCollider2D>();
        if (circleCollider == null) circleCollider = gameObject.AddComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.radius = 0.5f;

        // Rigidbody
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        // Sprite
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // Use custom sprite or create procedural circle
        if (useCustomSprite && customAOESprite != null)
        {
            spriteRenderer.sprite = customAOESprite;
        }
        else if (spriteRenderer.sprite == null)
        {
            CreateCircleSprite(spriteRenderer);
        }
        
        spriteRenderer.sortingOrder = sortingOrder;

        // Damage
        entityDamage = GetComponentInChildren<EntityDamage>(true);

        // Start in warning mode: no damage, no collision
        SetWarningMode();
    }

    public void SetWarningMode()
    {
        if (spriteRenderer != null) spriteRenderer.color = warningColor;
        if (entityDamage != null) entityDamage.enabled = false;
        if (circleCollider != null) circleCollider.enabled = false;
    }

    public void SetDamageMode()
    {
        if (spriteRenderer != null)
        {
            // Change sprite if custom damage sprite is provided
            if (useCustomDamageSprite && customDamageSprite != null)
            {
                spriteRenderer.sprite = customDamageSprite;
            }
            
            // Change color
            spriteRenderer.color = useCustomDamageColor ? customDamageColor : damageColor;
        }
        if (entityDamage != null) entityDamage.enabled = true;
        if (circleCollider != null) circleCollider.enabled = true;
    }

    void CreateCircleSprite(SpriteRenderer sr)
    {
        var texture = new Texture2D(resolution, resolution) { filterMode = FilterMode.Bilinear };
        var pixels = new Color[resolution * resolution];
        Vector2 center = new(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f, gradientStart = radius * (1f - gradientWidth);

        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            Vector2 p = new(x + 0.5f, y + 0.5f);
            float d = Vector2.Distance(p, center);
            if (d <= radius)
            {
                float a = 1f;
                if (useGradient && d >= gradientStart)
                {
                    float t = (d - gradientStart) / (radius - gradientStart);
                    a = 1f - t;
                }
                else if (d >= radius - 1f)
                {
                    a = radius - d;
                }
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, a);
            }
            else pixels[y * resolution + x] = Color.clear;
        }

        texture.SetPixels(pixels);
        texture.Apply();

        sr.sprite = Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution / 2f);
    }
}
