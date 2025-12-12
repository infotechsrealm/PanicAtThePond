using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class CreatePanel : MonoBehaviour
{

    public Button backButton;

    public InputField roomNameInput;

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
