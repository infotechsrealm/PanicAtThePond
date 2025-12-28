using UnityEngine;
using UnityEngine.UI;

public class RoomFilterManager : MonoBehaviour
{
    public InputField searchField;
    public Transform roomListParent;
    public Dropdown regionDropdown; // Add this in Inspector

    private string currentRegionFilter = ""; // Empty = All Regions

    void Start()
    {
        searchField.onValueChanged.AddListener(OnSearchChanged);
        
        // Setup region dropdown if assigned
        if (regionDropdown != null)
        {
            regionDropdown.ClearOptions();
            regionDropdown.AddOptions(new System.Collections.Generic.List<string>()
            {
                "All Regions",
                "Europe",
                "North America",
                "Oceania"
            });
            regionDropdown.onValueChanged.AddListener(OnRegionDropdownChanged);
        }
    }

    void OnSearchChanged(string searchText)
    {
        FilterRooms();
    }

    void OnRegionDropdownChanged(int index)
    {
        // Map dropdown index to region code
        switch (index)
        {
            case 0: currentRegionFilter = ""; break; // All Regions
            case 1: currentRegionFilter = "eu"; break; // Europe
            case 2: currentRegionFilter = "us"; break; // North America
            case 3: currentRegionFilter = "au"; break; // Oceania
        }
        
        Debug.Log($"[RoomFilter] Region filter changed to: {(string.IsNullOrEmpty(currentRegionFilter) ? "All Regions" : currentRegionFilter)}");
        FilterRooms();
    }

    void FilterRooms()
    {
        string searchText = searchField.text.ToLower();

        for (int i = 0; i < roomListParent.childCount; i++)
        {
            GameObject row = roomListParent.GetChild(i).gameObject;

            RoomRowPrefab roomRowPrefab = row.GetComponentInChildren<RoomRowPrefab>();
            if (roomRowPrefab == null)
                continue;

            string roomName = roomRowPrefab.roomName.text.ToLower();
            
            // Get region name for filtering
            string regionName = "";
            
            // Check if it's a Photon room (has photonRoomInfo)
            if (roomRowPrefab.photonRoomInfo != null)
            {
                // Get region from Photon room custom properties
                if (roomRowPrefab.photonRoomInfo.CustomProperties.TryGetValue("region", out object regionObj))
                {
                    regionName = (regionObj as string)?.ToLower();
                    Debug.Log($"[RoomFilter] Photon room '{roomName}' has region: '{regionName}'");
                }
            }
            // Otherwise check LAN room
            else if (!string.IsNullOrEmpty(roomRowPrefab.lanRoomInfo.regionName))
            {
                regionName = roomRowPrefab.lanRoomInfo.regionName.ToLower();
                Debug.Log($"[RoomFilter] LAN room '{roomName}' has region: '{regionName}'");
            }

            // ---------- FILTER LOGIC ----------
            bool matchesSearch = string.IsNullOrEmpty(searchText) || 
                                 roomName.Contains(searchText) || 
                                 regionName.Contains(searchText);
            
            bool matchesRegion = string.IsNullOrEmpty(currentRegionFilter) || 
                                 regionName == currentRegionFilter;

            Debug.Log($"[RoomFilter] Room '{roomName}': region='{regionName}', filter='{currentRegionFilter}', matchesRegion={matchesRegion}, visible={matchesSearch && matchesRegion}");

            // Show room only if it matches BOTH search AND region filter
            row.SetActive(matchesSearch && matchesRegion);
        }
    }
}
