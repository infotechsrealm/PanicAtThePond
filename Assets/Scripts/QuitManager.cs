using UnityEngine;
using UnityEngine.UI;

public class QuitManager : MonoBehaviour
{
    public Button backButton;

    private void Start()
    {
    }

    private void OnEnable()
    {
        BackManager.instance.RegisterScreen(backButton);
    }
    // Update is called once per frame
    void Update()
    {
        
    }


    public void Yes()
    {
        Application.Quit();
    }

    public void Cancle()
    {
        BackManager.instance.UnregisterScreen();

        gameObject.SetActive(false);
    }
}
