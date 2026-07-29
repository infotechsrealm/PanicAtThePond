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

}