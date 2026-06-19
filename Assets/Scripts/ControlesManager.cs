using Mirror.BouncyCastle.Asn1.Crmf;
using UnityEngine;
using UnityEngine.UI;

public class ControlesManager : MonoBehaviour
{

    public Button backButton,fishButton,fishermanButton, BaackFishermanControleui;
    public GameObject fishControlUI, FishermanControlUI;

    [HideInInspector]
    public GameObject settingsPanel;

    private void Start()
    {
        LegacyTextSharpener.EnsureSceneTextIsSharp();
        backButton.onClick.AddListener(OnBackPressed);
        fishButton.onClick.AddListener(onFishControlPressed);
        fishermanButton.onClick.AddListener(onFishermanControlPressed);
        if (BaackFishermanControleui != null)
        {
            BaackFishermanControleui.onClick.AddListener(OnBackPressedFishermanControlUI);
        }

    }

    private void OnEnable()
    {
        BackManager.instance.RegisterScreen(backButton);
        LegacyTextSharpener.EnsureSceneTextIsSharp();

    }

    private void OnBackPressed()
    {
        BackManager.instance.UnregisterScreen();
        gameObject.SetActive(false);
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public  void OnBackPressedFishermanControlUI()
    {
        if (FishermanControlUI == null)
        {
            return;
        }

        FishermanControlManager fishermanControlManager = FishermanControlUI.GetComponent<FishermanControlManager>();
        if (fishermanControlManager != null)
        {
            fishermanControlManager.BackButton();
            return;
        }

        FishermanControlUI.SetActive(false);
        gameObject.SetActive(true);
    }

    private void onFishControlPressed()
    {
        if (fishControlUI != null)
        {
            fishControlUI.SetActive(true);
            FishControlManager fishControlManager = fishControlUI.GetComponent<FishControlManager>();
            if (fishControlManager != null)
            {
                fishControlManager.controlsPanel = gameObject;
            }
            BringToFrontWithinCanvas(fishControlUI.transform);
            gameObject.SetActive(false);
        }
    }
    private void onFishermanControlPressed()
    {
        if (FishermanControlUI != null)
        {
            FishermanControlUI.SetActive(true);
            FishermanControlManager fishermanControlManager = FishermanControlUI.GetComponent<FishermanControlManager>();
            if (fishermanControlManager != null)
            {
                fishermanControlManager.controlsPanel = gameObject;
            }
            BringToFrontWithinCanvas(FishermanControlUI.transform);
            gameObject.SetActive(false);
        }
    }

    private void ShowPanelInFront(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        panel.SetActive(true);
        BringToFrontWithinCanvas(panel.transform);
    }

    private void BringToFrontWithinCanvas(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            current.SetAsLastSibling();

            Transform parent = current.parent;
            if (parent == null || parent.GetComponent<Canvas>() != null)
            {
                break;
            }

            current = parent;
        }
    }
}
