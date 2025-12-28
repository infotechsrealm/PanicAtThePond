using UnityEngine;
using UnityEngine.UI;
using Steamworks;

public class RoomFilterManager : MonoBehaviour
{
    public InputField searchField;
    public Transform roomListParent;
    public Dropdown regionDropdown; // Add this in Inspector
    public Toggle friendsOnlyToggle; // Add this in Inspector

    private string currentRegionFilter = ""; // Empty = All Regions
    private bool friendsOnlyFilter = false; // False = Show all rooms

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

        // Setup friends only toggle if assigned
        if (friendsOnlyToggle != null)
        {
            friendsOnlyToggle.onValueChanged.AddListener(OnFriendsOnlyToggleChanged);
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

    void OnFriendsOnlyToggleChanged(bool isOn)
    {
        friendsOnlyFilter = isOn;
        Debug.Log($"[RoomFilter] Friends Only filter: {(isOn ? "ON" : "OFF")}");
        FilterRooms();
    }

    bool IsSteamFriend(string userId)
    {
        // Check if Steam is initialized
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("[RoomFilter] Steam not initialized, cannot check friends");
            return false;
        }

        // Try to parse the userId as a Steam ID
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        // Photon's UserId for Steam is the Steam ID as a string
        if (ulong.TryParse(userId, out ulong steamId))
        {
            CSteamID friendSteamId = new CSteamID(steamId);
            
            // Check if this Steam ID is in our friends list
            int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
            
            for (int i = 0; i < friendCount; i++)
            {
                CSteamID friendId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                if (friendId == friendSteamId)
                {
                    return true;
                }
            }
        }

        return false;
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
                }
            }
            // Otherwise check LAN room
            else if (!string.IsNullOrEmpty(roomRowPrefab.lanRoomInfo.regionName))
            {
                regionName = roomRowPrefab.lanRoomInfo.regionName.ToLower();
            }

            // ---------- FILTER LOGIC ----------
            bool matchesSearch = string.IsNullOrEmpty(searchText) || 
                                 roomName.Contains(searchText) || 
                                 regionName.Contains(searchText);
            
            bool matchesRegion = string.IsNullOrEmpty(currentRegionFilter) || 
                                 regionName == currentRegionFilter;

            // Friends Only filter
            bool matchesFriends = true; // Default: show all rooms
            if (friendsOnlyFilter && roomRowPrefab.photonRoomInfo != null)
            {
                // Get the room creator's UserId (Steam ID)
                string creatorUserId = roomRowPrefab.photonRoomInfo.masterClientId;
                
                // Check if creator is a Steam friend
                matchesFriends = IsSteamFriend(creatorUserId);
                
                Debug.Log($"[RoomFilter] Room '{roomName}' creator ID: {creatorUserId}, isFriend: {matchesFriends}");
            }

            // Show room only if it matches ALL filters
            bool shouldShow = matchesSearch && matchesRegion && matchesFriends;
            row.SetActive(shouldShow);
        }
    }
}
