using UnityEngine;
using UnityEngine.UI;

public class PlayerUltimateBar : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.8f, 0f);
    [SerializeField] private Image fillImage;
    private PlayerHandler handler;

    void Awake()
    {
        if (!target)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) target = player.transform;
        }
        if (target) handler = target.GetComponent<PlayerHandler>();
        if (handler != null) handler.OnUltimateChanged += OnUltChanged;
        Refresh();
    }

    void OnDestroy()
    {
        if (handler != null) handler.OnUltimateChanged -= OnUltChanged;
    }

    void LateUpdate()
    {
        if (target) transform.position = target.position + offset;
        if (Camera.main) transform.rotation = Camera.main.transform.rotation;
    }

    void OnUltChanged(float norm) => SetFill(norm);

    void Refresh()
    {
        if (handler != null) SetFill(handler.UltimateNormalized);
    }

    void SetFill(float v)
    {
        if (fillImage) fillImage.fillAmount = Mathf.Clamp01(v);
    }
}