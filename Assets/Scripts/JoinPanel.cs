using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class JoinPanel : MonoBehaviour
{
    public RoomTableManager roomTableManager;

    public Button joinRandomBtn;

    private void OnEnable()
    {
        BackManager.instance.RegisterScreen(backButton);
        roomTableManager.UpdateRoomTable();
        if (GS.Instance.isLan)
        {
            joinRandomBtn.interactable = false;
        }
        else
        {
            joinRandomBtn.interactable = true;
        }
    }

    private void OnDisable()
    {
        roomTableManager.ResetTable();
    }

    public Button backButton;

    private void Start()
    {
    }



    // Update is called once per frame
    void Update()
    {
        
    }

    public void Close()
    {
        BackManager.instance.UnregisterScreen();

        if (GS.Instance.isLan)
        {
            if (LANDiscoveryMenu.Instance != null)
            {
                LANDiscoveryMenu.Instance.StopRoomFindCoroutine();
            }
        }
        else
        {

            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
        }



        gameObject.SetActive(false);
    }
}
