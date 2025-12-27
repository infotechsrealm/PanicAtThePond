using UnityEngine;
using UnityEngine.UI;

public class RoomFilterManager : MonoBehaviour
{
    public InputField searchField;
    public Transform roomListParent;

    void Start()
    {
        searchField.onValueChanged.AddListener(FilterRooms);
    }

    void FilterRooms(string searchText)
    {
        searchText = searchText.ToLower();

        for (int i = 0; i < roomListParent.childCount; i++)
        {
            GameObject row = roomListParent.GetChild(i).gameObject;

            RoomRowPrefab roomRowPrefab = row.GetComponentInChildren<RoomRowPrefab>();
            if (roomRowPrefab == null)
                continue;

            string name = roomRowPrefab.roomName.text.ToLower();
            
            // Get region name for filtering
            string regionName = "";
            if (!string.IsNullOrEmpty(roomRowPrefab.lanRoomInfo.regionName))
            {
                regionName = roomRowPrefab.lanRoomInfo.regionName.ToLower();
            }

            // ---------- FILTER ----------
            if (string.IsNullOrEmpty(searchText))
            {
                row.SetActive(true);
            }
            else if (name.Contains(searchText) || regionName.Contains(searchText))   // PARTIAL MATCH FILTER (name OR region)
            {
                row.SetActive(true);
            }
            else
            {
                row.SetActive(false);
            }
        }
    }
}
