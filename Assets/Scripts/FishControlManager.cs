using UnityEngine;
using UnityEngine.UI;

public class FishControlManager : MonoBehaviour
{
    public Button backButton;

    [HideInInspector]
    public GameObject controlsPanel;

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
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(true);
        }
    }
}
