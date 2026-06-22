using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
    public Button ScoreSystemButton, BackButton;
    public GameObject scoreUI;
    public void Start()
    {
        ScoreSystemButton.onClick.AddListener(scoreui_Open);
        BackButton.onClick.AddListener(Close_ScoreUI);
    }

    public void scoreui_Open()
    {
        scoreUI.SetActive(true);
        FixResetButton();
    }

    private void FixResetButton()
    {
        if (scoreUI == null) return;
        
        Button[] btns = scoreUI.GetComponentsInChildren<Button>(true);
        foreach (Button b in btns)
        {
            bool isReset = b.name.ToLower().Contains("reset");
            if (!isReset)
            {
                Text t = b.GetComponentInChildren<Text>(true);
                if (t != null && t.text.ToLower().Contains("reset")) isReset = true;
            }
            
            if (isReset)
            {
                RectTransform rect = b.GetComponent<RectTransform>();
                rect.localScale = Vector3.one;
                rect.anchorMin = new Vector2(1, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(1, 1);
                rect.anchoredPosition = new Vector2(-50, -20);
                break;
            }
        }
    }

    public void Close_ScoreUI()
    {
        scoreUI.SetActive(false);
    }
}