using Photon.Pun;
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
public class CreatePanel : MonoBehaviour
{

    public Button backButton;

    [SerializeField] private InputField roomNameInput;

    private void Start()
    {
        roomNameInput.text = "FISHFOOD";
    }
    private void OnEnable()
    {
        BackManager.instance.RegisterScreen(backButton);

    }
    public void Close()
    {
        BackManager.instance.UnregisterScreen();

        if (GS.Instance.isLan)
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
        }
        gameObject.SetActive(false);
    }
}

}