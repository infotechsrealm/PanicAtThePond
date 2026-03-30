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

    public override void OnEnable()
    {
        base.OnEnable();
        UpdatePlayerTable();
    }

    public void UpdatePlayerTable()
    {
      //  Debug.Log("UpdatePlayerTable called");
        if (GS.Instance.isLan)
        {
           StartCoroutine(UpdateLANPlayerTableUI());
        }
        else
        {
            RebuildTable();
            UpdatePlayerTableUI();
        }
    }

    public void UpdatePlayerTableUI()
    {
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
            texts[0].text = player.NickName;

            /* if (texts.Length >= 3)
             {
                 texts[0].text = (i + 1).ToString();   // Sequential number
                 texts[1].text = player.NickName;      // Nickname
                 texts[2].text = $"{PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
             }*/

            playerRows[player.ActorNumber] = row;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            Debug.Log(PhotonNetwork.CurrentRoom.PlayerCount + "start Button Enable" + PhotonNetwork.CurrentRoom.MaxPlayers);

            CoustomeRoomManager.Instance.startButton.interactable = true;
        }

        int curruntPlayer = 2 - PhotonNetwork.CurrentRoom.PlayerCount;
        //int curruntPlayer = PhotonNetwork.CurrentRoom.MaxPlayers - PhotonNetwork.CurrentRoom.PlayerCount;
        if (curruntPlayer < 1)
        {
            CreateJoinManager.Instance.requirePlayerText.text = "";
        }
        else
        {
            CreateJoinManager.Instance.requirePlayerText.text = curruntPlayer + " Players Required.";
        }
    }

    void RebuildTable()
    {
        /*// sorted list
        var sortedPlayers = PlayerManager_Mirror.players.OrderBy(p => p.connectionId).ToList();

        // rows clear करना
        foreach (Transform child in playerTablePanel)
            Destroy(child.gameObject);

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            var conn = sortedPlayers[i];

            GameObject row = Instantiate(playerRowPrefab, playerTablePanel);
            Text[] texts = row.GetComponentsInChildren<Text>();

            if (texts.Length >= 3)
            {
                texts[0].text = (i + 1).ToString();                // Rank
                texts[1].text = $"Player {conn.connectionId}";     // Nickname (custom)
                texts[2].text = $"/4";
            }
        }*/
    }


    IEnumerator UpdateLANPlayerTableUI()
    {
        yield return new WaitForSeconds(1f);

        foreach (Transform child in playerTablePanel)
            Destroy(child.gameObject);

        playerRows.Clear();
        for (int i = 0; i < players.Count; i++)
        {
            string player = players[i];
            GameObject row = Instantiate(playerRowPrefab, playerTablePanel);

            Text[] texts = row.GetComponentsInChildren<Text>();
            texts[0].text = player;
          /*  if (texts.Length >= 3)
            {
                texts[0].text = (i + 1).ToString();   // Sequential number
                texts[1].text = player;      // Nickname
                texts[2].text = $"{players.Count}/{LANDiscoveryMenu.Instance.DiscoveredServerInfo.maxPlayers}";
            }*/

        }
        GS.Instance.DestroyPreloder();

        if(players.Count >= 2)
        {
            CoustomeRoomManager.Instance.startButton.interactable = true;

        }
        else
        {
            CoustomeRoomManager.Instance.startButton.interactable = false;
        }

        //int curruntPlayer = LANDiscoveryMenu.Instance.DiscoveredServerInfo.maxPlayers - players.Count;
        int curruntPlayer = 2 - players.Count;
        if (curruntPlayer < 1)
        {
            CreateJoinManager.Instance.requirePlayerText.text = "";
        }
        else
        {
            CreateJoinManager.Instance.requirePlayerText.text = curruntPlayer + " Players Required.";
        }

    }

    public void ResetTable()
    {
        foreach (Transform child in playerTablePanel)
            Destroy(child.gameObject);
    }
}