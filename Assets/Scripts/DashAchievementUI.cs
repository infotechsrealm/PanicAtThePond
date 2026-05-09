using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Steamworks;

public class DashAchievementUI : MonoBehaviour

{
    [Header("UI References")]
    public GameObject achievementPanel;
    public Transform container;
    public GameObject achievementPlaceholderPrefab;

    [Header("Achievement List")]
    public List<string> achievementIds = new List<string> 
    { 
        "SOLO_ARTIST", "SURVIVOR", "EARTH_PRAISER", 
        "WHAT_A_SNACK", "FISH_SLAYER", "WE_COME_IN_SWARMS", "GULPER" 
    };

    private void Start()
    {
        if (achievementPanel != null)
            achievementPanel.SetActive(false);
        
        InitializePlaceholders();
    }

    public void ToggleAchievements()
    {
        if (achievementPanel != null)
        {
            achievementPanel.SetActive(!achievementPanel.activeSelf);
            if (achievementPanel.activeSelf) RefreshAchievements();
        }
    }

    private void InitializePlaceholders()
    {
        foreach (string id in achievementIds)
        {
            GameObject go = Instantiate(achievementPlaceholderPrefab, container);
            go.name = "Placeholder_" + id;
            // Placeholder logic: simple text/image identifying the achievement
            Text t = go.GetComponentInChildren<Text>();
            if (t != null) t.text = id;
        }
    }

    public void RefreshAchievements()
    {
        foreach (Transform child in container)
        {
            string id = child.name.Replace("Placeholder_", "");
            bool unlocked = IsUnlocked(id);
            
            // Visual feedback for placeholders (e.g., color change)
            Image img = child.GetComponent<Image>();
            if (img != null)
                img.color = unlocked ? Color.white : Color.gray;
        }
    }

    private bool IsUnlocked(string id)
    {
        if (PlayerPrefs.GetInt("Achievement_" + id, 0) == 1) return true;
        if (SteamManager.Initialized)
        {
            SteamUserStats.GetAchievement(id, out bool achieved);
            if (achieved) return true;
        }
        return false;
    }
}
