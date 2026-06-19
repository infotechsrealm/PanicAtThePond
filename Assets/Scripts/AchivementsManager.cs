using UnityEngine;
using UnityEngine.UI;

public class AchivementsManager : MonoBehaviour
{
    public Button backButton;

    [HideInInspector]
    public GameObject settingsPanel;

    private void Start()
    {
        backButton.onClick.AddListener(OnBackPressed);

    }

    private void OnEnable()
    {
        BackManager.instance.RegisterScreen(backButton);

    }

    private void OnBackPressed()
    {
        BackManager.instance.UnregisterScreen();
        gameObject.SetActive(false);
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }
}
