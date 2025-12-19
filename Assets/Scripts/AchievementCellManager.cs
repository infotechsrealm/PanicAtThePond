using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AchievementCellManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string info = "Achievement Info";

    public void OnPointerEnter(PointerEventData eventData)
    {
        HoverTooltipManager.instance.ShowTooltip(info, transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HoverTooltipManager.instance.HideTooltip();
    }



}