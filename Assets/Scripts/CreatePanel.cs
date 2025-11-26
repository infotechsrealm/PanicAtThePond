using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class CreatePanel : MonoBehaviour
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
