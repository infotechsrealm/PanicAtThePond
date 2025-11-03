using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class PasswordPopup : MonoBehaviour
{
    public Text passwordInput, passwordInputError;
    private RoomInfo targetRoom;
    private string correctPassword;


    public static PasswordPopup instence;
    private void Awake()
    {
        instence = this;
    }

    private void Start()
    {
        if (Preloader.instance != null)
        {
            Destroy(Preloader.instance.gameObject);
        }
    }
    public void Init(RoomInfo room, string correctPwd)
    {
        targetRoom = room;
        correctPassword = correctPwd;
    }

    public void OnJoinClicked()
    {
        if (passwordInput.text == correctPassword)
        {
            passwordInputError.text = "";

            Debug.Log("Password correct! Joining room...");
            PhotonNetwork.JoinRoom(targetRoom.Name);
            if (Preloader.instance == null)
                Instantiate(GS.Instance.preloder, DashManager.instance.prefabPanret.transform);
        }
        else
        {
            passwordInputError.text = "Please enter a valid password.";
            Debug.LogWarning("Wrong password!");
            // Optionally show error text on UI
        }
    }

    public void OnCancelClicked()
    {
        if (Preloader.instance != null)
        {
            Destroy(Preloader.instance.gameObject);
        }
        Destroy(gameObject);
    }
}
