using Photon.Pun;
using UnityEngine;

public class ClientLobby : MonoBehaviourPunCallbacks
{

    public PlayerTableManager playerTableManager;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        playerTableManager.UpdateTable();
    }
    public void Close()
    {
        if (PhotonNetwork.InRoom)
        {
            if(PhotonNetwork.IsMasterClient)
            {
                CoustomeRoomManager.Instence.CallLeaveRoom();
            }
            else
            {
                Debug.Log("Leaving room...");
                if (Preloader.instance == null)
                {
                    Instantiate(GS.instance.preloder, DashManager.instance.prefabPanret.transform);
                }

                gameObject.SetActive(false);

                PhotonNetwork.LeaveRoom();
            }

        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room successfully!");
    }


}
