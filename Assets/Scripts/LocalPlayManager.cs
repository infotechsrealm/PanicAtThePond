using ExitGames.Client.Photon.StructWrapping;
using UnityEngine;
using UnityEngine.UI;

public class LocalPlayManager : MonoBehaviour
{
    public Button backButton;
    public GameObject[] Fish_Sprite;
    public Button Left_BTN,Right_BTN;
    private int Next_Fish;

    private void Start()
    {
        Next_Fish = 0;
        Fish_Sprite[1].SetActive(false);
        Left_BTN.GetComponent<Button>().interactable = false;
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
        }
    }
}
