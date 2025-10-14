using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class PasswordPopup : MonoBehaviour
{
    public Text passwordInput;
    private RoomInfo targetRoom;
    private string correctPassword;


    public static PasswordPopup instence;
    private void Awake()
    {
        instence = this;
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
            Debug.Log("Password correct! Joining room...");
            PhotonNetwork.JoinRoom(targetRoom.Name);
            if (Preloader.instance == null)
                Instantiate(GS.instance.preloder, DashManager.instance.prefabPanret.transform);
        }
        else
        {
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
