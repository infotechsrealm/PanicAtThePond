using UnityEngine;
using UnityEngine.UI;

using PanicAtThePond.Managers;
using PanicAtThePond.Controllers;
using PanicAtThePond.Gameplay;
using PanicAtThePond.Shop;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.UI
{
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

}