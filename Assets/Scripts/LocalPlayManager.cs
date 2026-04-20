using ExitGames.Client.Photon.StructWrapping;
using UnityEngine;
using UnityEngine.UI;

public class LocalPlayManager : MonoBehaviour
{
    public Button backButton,BuyButton,BuyPanelBackButton;
    public GameObject[] Fish_Sprite,BuyFishInfoText;
    public Image SecondFish,LockImg;
    public Button Left_BTN,Right_BTN;
    public GameObject[] InfoText;
    public GameObject LockItem,BuyPanel;
    private int Next_Fish;

    private void Start()
    {
        Next_Fish = 0;
       // PlayerPrefs.SetInt("SelectedFish", 0);
        Fish_Sprite[1].SetActive(false);
        Left_BTN.GetComponent<Button>().interactable = false;
        BuyButton.onClick.AddListener(BuyPanelON);
        BuyPanelBackButton.onClick.AddListener(BuyPanleClose);
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

    public void Tap_NextButton()
    {
        Next_Fish ++;
        if(Next_Fish == 1)
        {
            Fish_Sprite[0].SetActive(false);
            Fish_Sprite[1].SetActive(true);
            Left_BTN.GetComponent<Button>().interactable = true;
            Right_BTN.GetComponent<Button>().interactable = false;
            BuyButton.gameObject.SetActive(true);
            BuyFishInfoText[0].SetActive(true);
            LockImg.gameObject.SetActive(true);
            LockItem.gameObject.SetActive(true);
            //InfoText[0].SetActive(true);InfoText[1].SetActive(true);
            //InfoText[2].SetActive(false);InfoText[3].SetActive(false);
            //SecondFish.color = Color.white;
            //PlayerPrefs.SetInt("SelectedFish", );
        }
    }
    public void Tap_PreviosButton()
    {
        Next_Fish --;
        if(Next_Fish == 0)
        {
            Fish_Sprite[0].SetActive(true);
            Fish_Sprite[1].SetActive(false);
            Left_BTN.GetComponent<Button>().interactable = false;
            Right_BTN.GetComponent<Button>().interactable = true;
            BuyButton.gameObject.SetActive(false);
            BuyFishInfoText[0].SetActive(false);
            LockImg.gameObject.SetActive(false);
            LockItem.gameObject.SetActive(false);
            //InfoText[0].SetActive(false);InfoText[1].SetActive(false);
            //InfoText[2].SetActive(true);InfoText[3].SetActive(true);
            //SecondFish.color = Color.black;
            //PlayerPrefs.SetInt("SelectedFish", 0);
        }
    }
    public void BuyPanelON()
    {
        BuyPanel.SetActive(true);
    }
    public void BuyPanleClose()
    {
        BuyPanel.SetActive(false);
    }
}
