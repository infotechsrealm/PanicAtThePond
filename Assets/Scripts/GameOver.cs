using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class GameOver : MonoBehaviourPunCallbacks
{
    public static GameOver Instance;

    public Button playAgainBtn,lobbyButton,CloseButton;

    private void Awake()
    {
        Instance = this;
        if (GS.Instance.isMasterClient && PhotonNetwork.IsMasterClient)
        {
            playAgainBtn.gameObject.SetActive(true);
        }
        else if (GS.Instance.isLan && GS.Instance.IsMirrorMasterClient)
        {
            playAgainBtn.gameObject.SetActive(false);
        }
        else
        {
            playAgainBtn.gameObject.SetActive(false);
        }
        PhotonNetwork.AutomaticallySyncScene = true;
    }


    private void Start()
    {
        playAgainBtn.onClick.AddListener(PlayAgain);
        lobbyButton.onClick.AddListener(Lobby);
        CloseButton.onClick.AddListener(Close);

    }
    public void PlayAgain()
    {
        SceneManager.LoadScene("Play");
       /* if (!GameManager.Instance.gameObject.activeSelf)
        {
            GameManager.Instance.gameObject.SetActive(true);
        }

        GameManager.Instance.RestartGame();*/
    }

    public void Lobby()
    {
        if (GS.Instance.isLan)
        {
            GameManager.Instance.RestartGame();
        }
        else
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
    }

    public void Close()
    {
        if (GS.Instance.isLan)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            PhotonNetwork.LeaveRoom();
        }
    }

}
