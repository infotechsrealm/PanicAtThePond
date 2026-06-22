using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HoverTooltipManager : MonoBehaviour
{
    public static HoverTooltipManager instance;

    public GameObject tooltipObject;
    public Text tooltipText;

    private Canvas canvas;


   public float offsetX = 0f;
   public float offsetY = -100f;
    private Vector2 defaultSizeDelta;
    private Vector3 defaultLocalScale;

    void Awake()
    {
        instance = this;
        canvas = GetComponentInParent<Canvas>();
        if (tooltipObject != null)
        {
            RectTransform rt = tooltipObject.GetComponent<RectTransform>();
            defaultSizeDelta = rt.sizeDelta;
            defaultLocalScale = rt.localScale;
            tooltipObject.SetActive(false);
        }
    }

  
    public void ShowTooltip(string info, RectTransform target)
    {
        tooltipText.text = info;
        tooltipObject.SetActive(true);

        RectTransform rt = tooltipObject.GetComponent<RectTransform>();

        if (info.Contains("Game Terms") || (target != null && target.name.ToLowerInvariant().Contains("mag")))
        {
            if (SceneManager.GetActiveScene().name == "Dash")
            {
                rt.anchoredPosition = new Vector2(90f, -290f);
                rt.sizeDelta = new Vector2(400f, 130f);
                rt.localScale = new Vector3(1.593115f, 1.593115f, 1f);
            }
            else
            {
                rt.anchoredPosition = new Vector2(58.6f, -140f);
                rt.sizeDelta = new Vector2(458f, 215f);
                rt.localScale = new Vector3(0.7f, 0.7f, 0.43939f);
            }
            
            tooltipText.text = "Game Terms\nGamemode The type of game you choose to play (e.g., Quick Cast,\nDeep Sea Fishing, Survival).\nMatch A full play session of a selected gamemode, made up of\nmultiple rounds. The match ends when the number of rounds have\nfully been played through.\nRound A single segment of gameplay within a match. Players\ncompete in those rounds";
            tooltipText.fontStyle = FontStyle.Bold;
        }
        else
        {
            // Target position → tooltip offset
            Vector2 targetPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, target.position),
                canvas.worldCamera,
                out targetPos
            );

            rt.anchoredPosition = targetPos + new Vector2(offsetX, offsetY);
            rt.sizeDelta = defaultSizeDelta;
            rt.localScale = defaultLocalScale;
            tooltipText.fontStyle = FontStyle.Bold;
        }
    }

    public void ShowTooltip(string info)
    {
        tooltipText.text = info;
        tooltipObject.SetActive(true);

        RectTransform rt = tooltipObject.GetComponent<RectTransform>();

        if (info.Contains("Game Terms"))
        {
            if (SceneManager.GetActiveScene().name == "Dash")
            {
                rt.anchoredPosition = new Vector2(90f, -290f);
                rt.sizeDelta = new Vector2(400f, 130f);
                rt.localScale = new Vector3(1.593115f, 1.593115f, 1f);
            }
            else
            {
                rt.anchoredPosition = new Vector2(58.6f, -140f);
                rt.sizeDelta = new Vector2(458f, 215f);
                rt.localScale = new Vector3(0.7f, 0.7f, 0.43939f);
            }
            
            tooltipText.text = "Game Terms\nGamemode The type of game you choose to play (e.g., Quick Cast,\nDeep Sea Fishing, Survival).\nMatch A full play session of a selected gamemode, made up of\nmultiple rounds. The match ends when the number of rounds have\nfully been played through.\nRound A single segment of gameplay within a match. Players\ncompete in those rounds";
            tooltipText.fontStyle = FontStyle.Bold;
        }
        else
        {
            // Reset to normal values just in case
            rt.sizeDelta = defaultSizeDelta;
            rt.localScale = defaultLocalScale;
            tooltipText.fontStyle = FontStyle.Bold;
        }
    }

    public void HideTooltip()
    {
        tooltipObject.SetActive(false);
    }
}
