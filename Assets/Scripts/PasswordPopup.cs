using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class PasswordPopup : MonoBehaviour
{
    public Text passwordInput, passwordInputError;
    private RoomInfo targetRoom;
    public string correctPassword;


    public static PasswordPopup Instance;
    private void Awake()
    {
        Instance = this;
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
            GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);
            if(GS.Instance.isLan)
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
