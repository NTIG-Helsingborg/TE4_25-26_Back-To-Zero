using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonStart : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string sceneToLoad = "sigma scene";
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite hoverSprite;
    
    private Image buttonImage;

    void Awake()
    {
        buttonImage = GetComponent<Image>();
        if (buttonImage != null && normalSprite != null)
        {
            buttonImage.sprite = normalSprite;
        }
    }

    public void LoadScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonImage != null && hoverSprite != null)
        {
            buttonImage.sprite = hoverSprite;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonImage != null && normalSprite != null)
        {
            buttonImage.sprite = normalSprite;
        }
    }
}
