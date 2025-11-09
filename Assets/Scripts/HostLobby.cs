using Mirror;
using Mirror.Discovery;
using Photon.Pun;
using UnityEngine;

public class HostLobby : MonoBehaviourPunCallbacks 
{

    public PlayerTableManager playerTableManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void OnEnable()
    {
        playerTableManager.UpdatePlayerTable();
    }
    public void Close()
    {
        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
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
            

            // 1️⃣ पहले discovery बंद करो ताकि broadcast रुक जाए
            LANDiscoveryMenu.Instance.networkDiscovery.StopDiscovery();

            // 2️⃣ फिर host बंद करो (इससे server + client दोनों बंद हो जाते हैं)
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkServer.active)
            {
                NetworkManager.singleton.StopServer();
            }
          

            // 3️⃣ transport बंद करो ताकि socket release हो जाए
            var transport = (TelepathyTransport)NetworkManager.singleton.transport;
            transport.Shutdown();


            gameObject.SetActive(false);
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room successfully!");
    }
}
