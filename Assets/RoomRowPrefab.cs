using UnityEngine;
using UnityEngine.UI;

public class RoomRowPrefab : MonoBehaviour
{

    public Text roomName;
    public Button btn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SelectRoom()
    {
        if(RoomTableManager.instance.SelectedButton!=null)
        {
            RoomTableManager.instance.SelectedButton.interactable = true;
        }
        btn.interactable = false;
        RoomTableManager.instance.SelectedButton = btn;
        CoustomeRoomManager.Instence.joinRoomName = roomName;
    }
}
