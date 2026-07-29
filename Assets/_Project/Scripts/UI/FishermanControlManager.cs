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
public class FishermanControlManager : MonoBehaviour
{
    public Button backButton;

    [HideInInspector]
    public GameObject controlsPanel;

    private void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(BackButton);
            backButton.onClick.AddListener(BackButton);
        }
    }

    private void OnEnable()
    {
        BackManager.EnsureInstance().RegisterScreen(backButton);
    }

    public void BackButton()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        if (BackManager.instance != null)
        {
            BackManager.instance.UnregisterScreen();
        }

        gameObject.SetActive(false);
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(true);
        }
    }
}

}