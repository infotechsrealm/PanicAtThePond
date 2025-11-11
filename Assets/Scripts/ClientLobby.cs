using Mirror;
using Photon.Pun;
using System.Collections;
using UnityEngine;

public class ClientLobby : MonoBehaviourPunCallbacks
{
    public PlayerTableManager playerTableManager;

    private void OnEnable()
    {
        playerTableManager.UpdatePlayerTable();
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
                GS.Instance.GeneratePreloder(DashManager.instance.prefabPanret.transform);
                gameObject.SetActive(false);
                PhotonNetwork.LeaveRoom();
            }
        }
        else
        {
            LANDiscoveryMenu.Instance.networkDiscovery.StopDiscovery();

            if (NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopClient();
            }

         /*   var transport = (TelepathyTransport)NetworkManager.singleton.transport;
            transport.Shutdown();*/

           LANDiscoveryMenu.Instance.isConnected = false;

            LANDiscoveryMenu.Instance.CallDiscoverAllLANHosts_Unlimited();

            gameObject.SetActive(false);
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room successfully!");
    }
}
