using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class PasswordPopup : MonoBehaviour
{
    public Text passwordInput, passwordInputError;
    private RoomInfo targetRoom;
    public string correctPassword;


    public static PasswordPopup instence;
    private void Awake()
    {
        instence = this;
    }

    private void Start()
    {
        GS.Instance.DestroyPreloder();

    }
    public void Init(RoomInfo room, string correctPwd)
    {
        targetRoom = room;
        correctPassword = correctPwd;
    }

    public void OnJoinClicked()
    {
        if (passwordInput.text.ToString().Trim() == correctPassword.Trim())
        {
            passwordInputError.text = "";
            Debug.Log("Password correct! Joining room...");
            GS.Instance.GeneratePreloder(DashManager.instance.prefabPanret.transform);
            if(CreateJoinManager.Instence.LAN.isOn)
            {
                LANDiscoveryMenu.Instance.JoinRoom();
            }
            else
            {
                PhotonNetwork.JoinRoom(targetRoom.Name);
            }
            Destroy(gameObject);
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
        GS.Instance.DestroyPreloder();

        Destroy(gameObject);
    }
}
