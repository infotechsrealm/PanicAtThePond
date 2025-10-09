using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq; // for sorting

public class PlayerTableManager : MonoBehaviourPunCallbacks
{
    public Transform playerTablePanel;     // Parent panel (Vertical Layout)
    public GameObject playerRowPrefab;     // Prefab with 2 Texts (ID + Nickname)

    private Dictionary<int, GameObject> playerRows = new Dictionary<int, GameObject>();

    void Start()
    {
        UpdateTable();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateTable();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateTable();
    }

    void UpdateTable()
    {
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
    }

}
