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
        else if(GS.Instance.isLan && GS.Instance.IsMirrorMasterClient)
        {
            playAgainBtn.SetActive(true);
        }
        else
        {
            playAgainBtn.SetActive(false);
        }
        PhotonNetwork.AutomaticallySyncScene = true;

    }

    private void Start()
    {
    }
    public void PlayAgain()
    {
        if (GS.Instance.isLan)
        {
            
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

    public void Restart()
    {
        if (!GameManager.Instance.gameObject.activeSelf)
        {
            GameManager.Instance.gameObject.SetActive(true);
        }

        GameManager.Instance.RestartGame();
    }


}
