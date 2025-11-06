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

    public LANRoomInfo()
    {
        roomName = "";
        port = 0;
        baseBroadcastPort = 0;
        roomPassword = "";
    }
}
public class RoomRowPrefab : MonoBehaviour
{
    public LANRoomInfo lanRoomInfo;
    public Text roomName;
    public Button btn;

    public bool isLAN = true;

    public void SelectRoom()
    {
        if (isLAN)
        {
            if (RoomTableManager.instance.SelectedButton != null)
            {
                RoomTableManager.instance.SelectedButton.interactable = true;
            }
            btn.interactable = false;
            RoomTableManager.instance.SelectedButton = btn;

            LANDiscoveryMenu lANDiscoveryMenu = LANDiscoveryMenu.Instance;

            lANDiscoveryMenu.DiscoveredServerInfo.serverName = lanRoomInfo.roomName;
            lANDiscoveryMenu.DiscoveredServerInfo.port = lanRoomInfo.port;
            lANDiscoveryMenu.DiscoveredServerInfo.baseBroadcastPort = lanRoomInfo.baseBroadcastPort;
            lANDiscoveryMenu.DiscoveredServerInfo.roomPassword = lanRoomInfo.roomPassword;

        }
        else
        {
            if (RoomTableManager.instance.SelectedButton != null)
            {
                RoomTableManager.instance.SelectedButton.interactable = true;
            }
            btn.interactable = false;

            RoomTableManager.instance.SelectedButton = btn;

            CoustomeRoomManager.Instence.joinRoomName = roomName;
        }
    }
}
