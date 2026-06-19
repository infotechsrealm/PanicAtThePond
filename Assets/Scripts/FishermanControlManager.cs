using UnityEngine;
using UnityEngine.UI;

public class FishermanControlManager : MonoBehaviour
{
    public Button backButton;

    [HideInInspector]
    public GameObject controlsPanel;

    private void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(BackButton);
            backButton.onClick.AddListener(BackButton);
        }
    }

    private void OnEnable()
    {
        BackManager.EnsureInstance().RegisterScreen(backButton);
    }

    public void BackButton()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        if (BackManager.instance != null)
        {
            BackManager.instance.UnregisterScreen();
        }

        gameObject.SetActive(false);
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(true);
        }
    }
}
