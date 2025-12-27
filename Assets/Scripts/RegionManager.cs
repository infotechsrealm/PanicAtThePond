
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum RegionType
{
    Unknown,
    Europe,
    NorthAmerica,
    Oceania
}

public class RegionManager : MonoBehaviour
{
    public static RegionManager Instance;

    private Dictionary<RegionType, Sprite> regionIcons = new Dictionary<RegionType, Sprite>();
    
    // Mapping from raw strings (and prefixes) to RegionType
    private Dictionary<string, RegionType> stringToRegionMap = new Dictionary<string, RegionType>()
    {
        // Explicit
        { "europe", RegionType.Europe },
        { "north america", RegionType.NorthAmerica },
        { "oceania", RegionType.Oceania },
        
        // Codes / Mappings
        { "eu", RegionType.Europe },
        { "us", RegionType.NorthAmerica },
        { "ca", RegionType.NorthAmerica }, // Canada -> NorthAmerica mapping
        { "na", RegionType.NorthAmerica },
        { "au", RegionType.Oceania },
        { "asia", RegionType.Oceania }, 
        { "in", RegionType.Oceania }, // India -> Oceania as nearest
        { "jp", RegionType.Oceania }, // Japan -> Oceania
        { "kr", RegionType.Oceania }, // Korea -> Oceania
        { "sg", RegionType.Oceania }, // Singapore -> Oceania
        { "hk", RegionType.Oceania }, // Hong Kong -> Oceania
        // Prompt said: "Oceania__OCE", "Europe", "North America". 
        // "Canada should use the 'North America' region icon."
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadIcons();
            DontDestroyOnLoad(gameObject);
            Debug.Log("[RegionManager] RegionManager initialized successfully");
        }
        else
        {
            Destroy(gameObject);
            Debug.LogWarning("[RegionManager] Duplicate RegionManager destroyed");
        }
    }

    private void LoadIcons()
    {
        // Icons are assigned in the Inspector
        // They are located in Assets/Dash UI/Regions/
        // No need to move them to a Resources folder
    }

    // Assign these in the Inspector or finding them if possible. 
    // Since I can't drag-drop in this text interface, I will assume the user might need to assign them, 
    // OR I can try to load them via Resources if I tell the user to move them.
    // The prompt says: "Load region icon sprites from DashUI/regions at runtime (e.g., via Resources.LoadAll<Sprite>...)"
    // This implies they MIGHT be willing to move them or I should use Resources.LoadAll if that folder WAS a resources folder.
    // However, "Dash UI/Regions" is not "Resources". 
    // I'll stick to serialized fields for the sprites to be assigned in the prefab/scene.
    [Header("Region Icons (Assign in Inspector)")]
    public Sprite iconEurope;
    public Sprite iconNorthAmerica;
    public Sprite iconOceania;
    public Sprite iconUnknown; // Fallback

    private void Start()
    {
        // Populate dictionary from Inspector-assigned sprites
        regionIcons[RegionType.Europe] = iconEurope;
        regionIcons[RegionType.NorthAmerica] = iconNorthAmerica;
        regionIcons[RegionType.Oceania] = iconOceania;
        regionIcons[RegionType.Unknown] = iconUnknown;
    }

    public RegionType GetRegionFromString(string rawRegion)
    {
        if (string.IsNullOrEmpty(rawRegion)) return RegionType.Unknown;
        
        rawRegion = rawRegion.ToLowerInvariant();

        // Direct check
        if (stringToRegionMap.ContainsKey(rawRegion)) return stringToRegionMap[rawRegion];

        // Partial match / 'Nearest' logic
        if (rawRegion.Contains("eu")) return RegionType.Europe;
        if (rawRegion.Contains("us") || rawRegion.Contains("sa") || rawRegion.Contains("ca")) return RegionType.NorthAmerica; // sa = south america -> nearest NA?
        if (rawRegion.Contains("au") || rawRegion.Contains("jp") || rawRegion.Contains("asia")) return RegionType.Oceania;
        if (rawRegion.Contains("oceania")) return RegionType.Oceania;
        if (rawRegion.Contains("america")) return RegionType.NorthAmerica;

        return RegionType.Unknown;
    }

    public Sprite GetRegionIcon(RegionType region)
    {
        if (regionIcons.TryGetValue(region, out Sprite icon))
        {
            return icon;
        }
        return iconUnknown;
    }

    /// <summary>
    /// Detects the player's region based on system timezone (for LAN mode)
    /// </summary>
    public string GetLocalRegion()
    {
        try
        {
            // Get system timezone
            System.TimeZoneInfo localZone = System.TimeZoneInfo.Local;
            string timeZoneId = localZone.Id;
            
            Debug.Log($"[RegionManager] Detecting region from timezone: {timeZoneId}");
            
            // Map common timezone IDs to regions
            // Europe timezones
            if (timeZoneId.Contains("Europe") || 
                timeZoneId.Contains("GMT") || 
                timeZoneId.Contains("UTC") ||
                timeZoneId.Contains("W. Europe") ||
                timeZoneId.Contains("Central Europe") ||
                timeZoneId.Contains("E. Europe") ||
                timeZoneId.Contains("Romance") ||
                timeZoneId.Contains("Central European"))
            {
                return "eu";
            }
            
            // North America timezones
            if (timeZoneId.Contains("America") || 
                timeZoneId.Contains("US") || 
                timeZoneId.Contains("Canada") ||
                timeZoneId.Contains("Eastern Standard") ||
                timeZoneId.Contains("Central Standard") ||
                timeZoneId.Contains("Mountain Standard") ||
                timeZoneId.Contains("Pacific Standard"))
            {
                return "us";
            }
            
            // Oceania/Asia timezones
            if (timeZoneId.Contains("Australia") || 
                timeZoneId.Contains("Pacific") || 
                timeZoneId.Contains("New Zealand") ||
                timeZoneId.Contains("AUS") ||
                timeZoneId.Contains("Asia") ||
                timeZoneId.Contains("India") ||
                timeZoneId.Contains("China") ||
                timeZoneId.Contains("Japan") ||
                timeZoneId.Contains("Korea") ||
                timeZoneId.Contains("Singapore") ||
                timeZoneId.Contains("Hong Kong"))
            {
                return "au"; // Oceania
            }
            
            // Default fallback
            Debug.LogWarning($"[RegionManager] Unknown timezone '{timeZoneId}', defaulting to Europe");
            return "eu";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RegionManager] Error detecting region: {e.Message}");
            return "eu"; // Default to Europe on error
        }
    }
    
    public Sprite GetRegionIcon(string rawRegion)
    {
        return GetRegionIcon(GetRegionFromString(rawRegion));
    }
}
