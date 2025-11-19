using UnityEngine;
using UnityEngine.UI;

public class PlayerUltimateBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private bool colorWhenFull = true;
    [SerializeField] private Color fullColor = Color.red;
    [SerializeField] private Color normalColor = Color.white;

    private PlayerHandler handler;

    void Awake()
    {
        if (!fillImage)
            fillImage = GetComponentInChildren<Image>();

        FindPlayerHandler();
        Subscribe();
        Refresh();
    }

    void OnEnable()
    {
        if (handler == null) FindPlayerHandler();
        Subscribe();
        Refresh();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void FindPlayerHandler()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player) handler = player.GetComponent<PlayerHandler>();
    }

    void Subscribe()
    {
        if (handler != null)
            handler.OnUltimateChanged += OnUltChanged;
    }

    void Unsubscribe()
    {
        if (handler != null)
            handler.OnUltimateChanged -= OnUltChanged;
    }

    void OnUltChanged(float norm) => SetFill(norm);

    void Refresh()
    {
        if (handler != null)
            SetFill(handler.UltimateNormalized);
        else
            SetFill(0f);
    }

    void SetFill(float v)
    {
        if (!fillImage) return;
        float clamped = Mathf.Clamp01(v);
        fillImage.fillAmount = clamped;

        if (colorWhenFull)
            fillImage.color = (clamped >= 0.999f) ? fullColor : normalColor;
    }
}