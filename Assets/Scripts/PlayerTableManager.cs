using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // for sorting
using UnityEngine;
using UnityEngine.UI;

public class PlayerTableManager : MonoBehaviourPunCallbacks
{
    public Transform playerTablePanel;     // Parent panel (Vertical Layout)
    public GameObject playerRowPrefab;     // Prefab with 2 Texts (ID + Nickname)

    private Dictionary<int, GameObject> playerRows = new Dictionary<int, GameObject>();

    public static PlayerTableManager Instance;


    public List<string> players = new List<string>();

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        UpdatePlayerTable();
    }

    public void UpdatePlayerTable()
    {
        if (GS.Instance.isLan)
        {
           StartCoroutine(UpdateLANPlayerTableUI());
        }
        else
        {
            UpdatePlayerTableUI();
        }
    }

    public void UpdatePlayerTableUI()
    {
        Debug.Log("UpdatePlayerTableUI Called");

        // Clear old entries
        foreach (Transform child in playerTablePanel)
            Destroy(child.gameObject);

        playerRows.Clear();

        // Sort players by ActorNumber
        Player[] sortedPlayers = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();

        int x = PhotonNetwork.PlayerList.Count();

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
        if (PhotonNetwork.CurrentRoom.PlayerCount < PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            CoustomeRoomManager.Instance.startButton.interactable = true;
        }
      
    }



    IEnumerator  UpdateLANPlayerTableUI()
    {
        yield return new WaitForSeconds(1f);

        foreach (Transform child in playerTablePanel)
            Destroy(child.gameObject);

        playerRows.Clear();
        Debug.Log("UpdateLANPlayerTableUI Called");
        for (int i = 0; i < players.Count; i++)
        {
            string player = players[i];
            GameObject row = Instantiate(playerRowPrefab, playerTablePanel);

            Text[] texts = row.GetComponentsInChildren<Text>();
            if (texts.Length >= 3)
            {
                texts[0].text = (i + 1).ToString();   // Sequential number
                texts[1].text = player;      // Nickname
                texts[2].text = $"{players.Count}/{LANDiscoveryMenu.Instance.DiscoveredServerInfo.maxPlayers}";
            }

        }
        GS.Instance.DestroyPreloder();

        if(players.Count >= LANDiscoveryMenu.Instance.DiscoveredServerInfo.maxPlayers)
        {
            CoustomeRoomManager.Instance.startButton.interactable = true;

        }
        else
        {
            CoustomeRoomManager.Instance.startButton.interactable = false;
        }

    }


}