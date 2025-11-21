using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class JoinPanel : MonoBehaviour
{

    public RoomTableManager roomTableManager;

    public Button joinRandomBtn;
    private void OnEnable()
    {
        roomTableManager.UpdateRoomTable();
        if (GS.Instance.isLan)
        {
            joinRandomBtn.interactable = false;
        }
        else
        {
            joinRandomBtn.interactable = true;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
    }

   

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Close()
    {
        if (GS.Instance.isLan)
        {
            if (LANDiscoveryMenu.Instance != null)
            {
                LANDiscoveryMenu.Instance.StopRoomFindCoroutine();
            }
        }

        gameObject.SetActive(false);
    }
}
