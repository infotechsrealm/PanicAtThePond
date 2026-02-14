using System.Collections.Generic;
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
    public string regionName;

    public List<Text> fadText = new List<Text>();

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
    public Image regionIcon; // Reference to the "earth" GameObject's Image component

    // Store Photon room info for filtering
    public Photon.Realtime.RoomInfo photonRoomInfo;


    public void SelectRoom()
    {
        if (GS.Instance.isLan)
        {
            if (RoomTableManager.instance.SelectedButton != null)
            {
                RoomTableManager.instance.SelectedButton.interactable = true;
                RoomTableManager.instance.SelectedButton.GetComponentInParent<RoomRowPrefab>().FadText(false);
            }

            FadText(true);
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
                RoomTableManager.instance.SelectedButton.GetComponentInParent<RoomRowPrefab>().FadText(false);

            }

            FadText(true);
            btn.interactable = false;

            RoomTableManager.instance.SelectedButton = btn;

            CoustomeRoomManager.Instance.joinRoomName = roomName;

        }
    }

    public void FadText(bool isFad)
    {
        if (isFad)
        {

            foreach (Text t in lanRoomInfo.fadText)
            {
                t.CrossFadeAlpha(0.4f, 0.1f, false);
            }
        }
        else
        {
            foreach (Text t in lanRoomInfo.fadText)
            {
                t.CrossFadeAlpha(1f, 0.1f, false);
            }
        }
    }

    public void SetRegionIcon(Sprite icon)
    {
        if (regionIcon != null && icon != null)
        {
            regionIcon.sprite = icon;
            Debug.Log($"[RoomRowPrefab] Region icon sprite set to: {icon.name}");
        }
        else
        {
            if (regionIcon == null) Debug.LogWarning("[RoomRowPrefab] regionIcon Image component is null! Assign it in the prefab.");
            if (icon == null) Debug.LogWarning("[RoomRowPrefab] Trying to set null sprite");
        }
    }
}
