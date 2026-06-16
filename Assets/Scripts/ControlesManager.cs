using Mirror.BouncyCastle.Asn1.Crmf;
using UnityEngine;
using UnityEngine.UI;

public class ControlesManager : MonoBehaviour
{

    public Button backButton,fishButton,fishermanButton, BaackFishermanControleui;
    public GameObject fishControlUI, FishermanControlUI;

    private void Start()
    {
        backButton.onClick.AddListener(OnBackPressed);
        fishButton.onClick.AddListener(onFishControlPressed);
        fishermanButton.onClick.AddListener(onFishermanControlPressed);
        BaackFishermanControleui.onClick.AddListener(OnBackPressedFishermanControlUI);

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

    public  void OnBackPressedFishermanControlUI()
    {
        FishermanControlUI.SetActive(false);
    }

    private void onFishControlPressed()
    {
        fishControlUI.SetActive(true);
    }
    private void onFishermanControlPressed()
    {
        FishermanControlUI.SetActive(true);
    }
}
