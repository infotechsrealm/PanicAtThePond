using UnityEngine;
using UnityEngine.UI;

public class LocalPlayManager : MonoBehaviour
{
    public Button backButton;

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
}
