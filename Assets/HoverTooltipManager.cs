using UnityEngine;
using UnityEngine.UI;

public class HoverTooltipManager : MonoBehaviour
{
    public static HoverTooltipManager instance;

    public GameObject tooltipObject;
    public Text tooltipText;

    private Canvas canvas;


   public float offsetX = 0f;
   public float offsetY = -100f;
    void Awake()
    {
        instance = this;
        canvas = GetComponentInParent<Canvas>();
        tooltipObject.SetActive(false);
    }

  
    public void ShowTooltip(string info, RectTransform target)
    {
        tooltipText.text = info;
        tooltipObject.SetActive(true);

        // Target position → tooltip offset
        Vector2 targetPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, target.position),
            canvas.worldCamera,
            out targetPos
        );

        // Offset (right + down)
        

        tooltipObject.GetComponent<RectTransform>().anchoredPosition =
            targetPos + new Vector2(offsetX, offsetY);
    }

    public void ShowTooltip(string info)
    {
        tooltipText.text = info;
        tooltipObject.SetActive(true);
    }

    public void HideTooltip()
    {
        tooltipObject.SetActive(false);
    }
}
