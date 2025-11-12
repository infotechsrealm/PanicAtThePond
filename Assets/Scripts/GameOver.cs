using Photon.Pun;
using UnityEngine;

public class GameOver : MonoBehaviourPunCallbacks
{
    public static GameOver Instance;

    public GameObject playAgainBtn;

    private void Awake()
    {
        Instance = this;
        if (GS.Instance.isMasterClient && PhotonNetwork.IsMasterClient)
        {
            playAgainBtn.SetActive(true);
        }
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void PlayAgain()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            PhotonNetwork.LoadLevel("Dash");
        }
        else
        {
            GameManager.Instance.RestartGame();
        }
    }

    public void Restart()
    {
        GameManager.Instance.RestartGame();
    }


}
