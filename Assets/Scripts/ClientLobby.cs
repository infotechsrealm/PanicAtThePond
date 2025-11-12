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
                CoustomeRoomManager.Instance.CallLeaveRoom();
            }
            else
            {
                Debug.Log("Leaving room...");
                GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);
                gameObject.SetActive(false);
                PhotonNetwork.LeaveRoom();
            }
        }
        else
        {
           

            LANDiscoveryMenu lanDiscoveryMenu = LANDiscoveryMenu.Instance;

            lanDiscoveryMenu.networkDiscovery.StopDiscovery();

            if (NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopClient();
            }

            /*   var transport = (TelepathyTransport)NetworkManager.singleton.transport;
            transport.Shutdown();*/

            lanDiscoveryMenu.DiscoveredServerInfo.port = 0;
            lanDiscoveryMenu.DiscoveredServerInfo.baseBroadcastPort = 0;

            lanDiscoveryMenu.isConnected = false;

            lanDiscoveryMenu.CallDiscoverAllLANHosts_Unlimited();

            gameObject.SetActive(false);
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room successfully!");
    }
}
