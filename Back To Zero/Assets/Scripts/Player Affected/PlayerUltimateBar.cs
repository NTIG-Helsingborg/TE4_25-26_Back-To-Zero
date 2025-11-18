using UnityEngine;
using UnityEngine.UI;

public class PlayerUltimateBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;      // assign your BloodBar Image here
    [SerializeField] private bool faceCamera = true;

    private PlayerHandler handler;

    void Awake()
    {
        if (handler == null)
            handler = GetComponentInParent<PlayerHandler>(); // Player is the parent

        if (fillImage == null)
            fillImage = GetComponentInChildren<Image>(true); // fallback

        if (handler != null)
            handler.OnUltimateChanged += OnUltChanged;

        Refresh();
    }

    void OnDestroy()
    {
        if (handler != null)
            handler.OnUltimateChanged -= OnUltChanged;
    }

    void LateUpdate()
    {
        if (faceCamera && Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;
    }

    private void OnUltChanged(float norm) => SetFill(norm);

    private void Refresh()
    {
        if (handler != null) SetFill(handler.UltimateNormalized);
    }

    private void SetFill(float v)
    {
        if (fillImage) fillImage.fillAmount = Mathf.Clamp01(v);
    }
}