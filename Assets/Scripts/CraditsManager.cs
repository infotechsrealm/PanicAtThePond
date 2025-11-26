using UnityEngine;
using UnityEngine.UI;

public class CraditsManager : MonoBehaviour
{
    public Button backButton;

    private void Start()
    {
    }

    private void OnEnable()
    {
        BackManager.instance.RegisterScreen(backButton);
    }

    public void Back()
    {
        BackManager.instance.UnregisterScreen();
        gameObject.SetActive(false);
    }
}
