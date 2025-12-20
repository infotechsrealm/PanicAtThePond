using UnityEngine;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public Button backButton;
    public GameObject settingUI;
    private void Start()
    {
    }

    private void OnEnable()
    {
        BackManager.instance.RegisterScreen(backButton);
    }

    public void BackButton()
    {
        BackManager.instance.UnregisterScreen();
        gameObject.SetActive(false);
    }

    public void Resume()
    {

    }

    public void Setting()
    {
        settingUI.SetActive(true);
    }

}
