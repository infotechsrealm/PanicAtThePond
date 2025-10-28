using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class LanPhotonTester : MonoBehaviourPunCallbacks
{
    [Header("LAN Settings")]
    public string masterServerIP = "192.168.29.9";  // <-- Host machine का IP डालो
    public int masterServerPort = 5055;             // Default Photon Server UDP port
    public string appVersion = "1.0";

    void Start()
    {
        Debug.Log("🔌 Connecting to Photon LAN Server... " + masterServerIP + ":" + masterServerPort);

        // पहले PhotonServerSettings में "Use Name Server" को disable कर दो
        // अगर करना भूल गए हो, तो ये manually local connect करेगा
        PhotonNetwork.ConnectToMaster(masterServerIP, masterServerPort, appVersion);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Connected to LAN Master Server!");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("⚠️ No existing rooms found, creating new one...");
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 8 };
        PhotonNetwork.CreateRoom("LANRoom", roomOptions);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("🟢 Created new room successfully: " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("🎮 Joined room successfully! Player count: " + PhotonNetwork.CurrentRoom.PlayerCount);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError("❌ Disconnected from server. Reason: " + cause);
    }
}
