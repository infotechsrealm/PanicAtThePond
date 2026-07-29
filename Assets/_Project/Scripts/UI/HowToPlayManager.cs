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
}