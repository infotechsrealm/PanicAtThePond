using Mirror;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq; // for sorting
using UnityEngine;
using UnityEngine.UI;

public class PlayerTableManager : MonoBehaviourPunCallbacks
{
    public Transform playerTablePanel;     // Parent panel (Vertical Layout)
    public GameObject playerRowPrefab;     // Prefab with 2 Texts (ID + Nickname)

    private Dictionary<int, GameObject> playerRows = new Dictionary<int, GameObject>();

    public static PlayerTableManager instance;
    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        UpdatePlayerTableUI();
    }

    public void UpdatePlayerTableUI()
    {
        if (CreateJoinManager.Instence.LAN.isOn)
        {
            UpdatePlayerTableUI2();
        }
        else
        {
            UpdatePlayerTableUI1();
        }
    }

    public void UpdatePlayerTableUI1()
    {
        Debug.Log("UpdatePlayerTableUI Called");

        // Clear old entries
        foreach (Transform child in playerTablePanel)
            Destroy(child.gameObject);

        playerRows.Clear();

        // Sort players by ActorNumber
        Player[] sortedPlayers = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();

        // Rebuild table
        for (int i = 0; i < sortedPlayers.Length; i++)
        {
            Player player = sortedPlayers[i];
            GameObject row = Instantiate(playerRowPrefab, playerTablePanel);

            Text[] texts = row.GetComponentsInChildren<Text>();
            if (texts.Length >= 3)
            {
                texts[0].text = (i + 1).ToString();   // Sequential number
                texts[1].text = player.NickName;      // Nickname
                texts[2].text = $"{PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
            }

            playerRows[player.ActorNumber] = row;
        }
        if (PhotonNetwork.CurrentRoom.Players.Count < 2)
        {
            CoustomeRoomManager.Instence.startButton.interactable = false;
        }
    }

    public void UpdatePlayerTableUI2()
    {
        Debug.Log("UpdateLobbyUI Called");

        // Clear old entries
        foreach (Transform child in playerTablePanel)
            Destroy(child.gameObject);

        playerRows.Clear();

        // Mirror connections list (identity null हो सकती है, कोई दिक्कत नहीं)
        var players = NetworkServer.connections.Values
            .Where(conn => conn != null)
            .OrderBy(conn => conn.connectionId)
            .ToArray();

        Debug.Log("Connected Players Count = " + players.Length);

        // Rebuild table
        for (int i = 0; i < players.Length; i++)
        {
            GameObject row = Instantiate(playerRowPrefab, playerTablePanel);

            Text[] texts = row.GetComponentsInChildren<Text>();
            if (texts.Length >= 3)
            {
                texts[0].text = (i + 1).ToString();           // Serial
                texts[1].text = "Player " + (i + 1);          // Nickname placeholder
                texts[2].text = $"{players.Length}/2";         // Current / Max Players
            }

            playerRows[i] = row.gameObject;
        }

        // Disable start if less than 2 players
        CoustomeRoomManager.Instence.startButton.interactable = (players.Length >= 2);
    }

}