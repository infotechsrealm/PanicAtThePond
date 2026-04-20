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
    }

    public void Close_ScoreUI()
    {
        scoreUI.SetActive(false);
    }
}