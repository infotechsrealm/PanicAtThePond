using Photon.Pun;
using UnityEngine;

public class ClientLobby : MonoBehaviourPunCallbacks
{
    public PlayerTableManager playerTableManager;

    private void OnEnable()
    {
        playerTableManager.UpdatePlayerTableUI();
    }

    public void Close()
    {
        if (PhotonNetwork.InRoom)
        {
            if(PhotonNetwork.IsMasterClient)
            {
                CoustomeRoomManager.Instence.CallLeaveRoom();
            }
            else
            {
                Debug.Log("Leaving room...");
                if (Preloader.instance == null)
                {
                    Instantiate(GS.Instance.preloder, DashManager.instance.prefabPanret.transform);
                }

                gameObject.SetActive(false);

                PhotonNetwork.LeaveRoom();
            }

        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room successfully!");
    }

}
