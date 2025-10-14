using UnityEngine;

public class JoinPanel : MonoBehaviour
{

    public RoomTableManager roomTableManager;


    private void OnEnable()
    {
        roomTableManager.UpdateRoomTableUI();
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
        gameObject.SetActive(false);
    }
}
