using UnityEngine;
using UnityEngine.UI;

public class FishControlManager : MonoBehaviour
{
    public Button backButton;

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
    }
}
