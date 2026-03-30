using UnityEngine;
using UnityEngine.UI;
public class HowToPlayManager : MonoBehaviour
{
    public Button backButton;

    private void Start()
    {
    }

    private void OnEnable()
    {
        BackManager.instance.RegisterScreen(backButton);
    }

    public void Close()
    {
        BackManager.instance.UnregisterScreen();
        Destroy(gameObject);
    }
}