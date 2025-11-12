using UnityEngine;
using UnityEngine.UI;

//this class is used to store information about discovered LAN servers
[System.Serializable]
public class LANRoomInfo
{
    public string roomName;
    public int port;
    public int baseBroadcastPort;
    public string roomPassword;
    public int connectedPlayers;
    public int maxPlayers;

    public LANRoomInfo()
    {
        roomName = "";
        port = 0;
        baseBroadcastPort = 0;
        roomPassword = "";
        connectedPlayers = 0;
        maxPlayers = 0;
    }
}
public class RoomRowPrefab : MonoBehaviour
{
    public LANRoomInfo lanRoomInfo;
    public Text roomName;
    public Button btn;


    public void SelectRoom()
    {
        if (GS.Instance.isLan)
        {
            if (RoomTableManager.instance.SelectedButton != null)
            {
                RoomTableManager.instance.SelectedButton.interactable = true;
            }
            btn.interactable = false;
            RoomTableManager.instance.SelectedButton = btn;

            LANDiscoveryMenu LANDiscoveryMenu = LANDiscoveryMenu.Instance;

            LANDiscoveryMenu.DiscoveredServerInfo.roomName = lanRoomInfo.roomName;
            LANDiscoveryMenu.DiscoveredServerInfo.port = lanRoomInfo.port;
            LANDiscoveryMenu.DiscoveredServerInfo.baseBroadcastPort = lanRoomInfo.baseBroadcastPort;
            LANDiscoveryMenu.DiscoveredServerInfo.roomPassword = lanRoomInfo.roomPassword;
            LANDiscoveryMenu.DiscoveredServerInfo.maxPlayers = lanRoomInfo.maxPlayers;

        }
        else
        {
            if (RoomTableManager.instance.SelectedButton != null)
            {
                RoomTableManager.instance.SelectedButton.interactable = true;
            }
            btn.interactable = false;

            RoomTableManager.instance.SelectedButton = btn;

            CoustomeRoomManager.Instance.joinRoomName = roomName;



        }
    }
}
