using Steamworks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AchievementCellManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Achievement")]
    public string achievementId;
    public string info = "Achievement Info";

    [Header("Locked Visual")]
    public Color lockedImageColor = new Color(0.35f, 0.35f, 0.35f, 0.65f);
    public Color lockedTextColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private Image[] images;
    private Color[] originalImageColors;
    private Text[] texts;
    private Color[] originalTextColors;

    private void Awake()
    {
        CacheVisuals();
        ResolveAchievementId();
    }

    private void OnEnable()
    {
        RefreshVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        HoverTooltipManager.instance.ShowTooltip(info, transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HoverTooltipManager.instance.HideTooltip();
    }

    public void RefreshVisualState()
    {
        CacheVisuals();
        ResolveAchievementId();

        bool unlocked = IsUnlocked();

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null)
            {
                images[i].color = unlocked ? originalImageColors[i] : lockedImageColor;
            }
        }

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
            {
                texts[i].color = unlocked ? originalTextColors[i] : lockedTextColor;
            }
        }
    }

    private void CacheVisuals()
    {
        if (images == null || originalImageColors == null)
        {
            images = GetComponentsInChildren<Image>(true);
            originalImageColors = new Color[images.Length];
            for (int i = 0; i < images.Length; i++)
            {
                originalImageColors[i] = images[i] != null ? images[i].color : Color.white;
            }
        }

        if (texts == null || originalTextColors == null)
        {
            texts = GetComponentsInChildren<Text>(true);
            originalTextColors = new Color[texts.Length];
            for (int i = 0; i < texts.Length; i++)
            {
                originalTextColors[i] = texts[i] != null ? texts[i].color : Color.white;
            }
        }
    }

    private void ResolveAchievementId()
    {
        if (!string.IsNullOrWhiteSpace(achievementId))
        {
            achievementId = achievementId.Trim();
            return;
        }

        string objectName = gameObject.name;
        if (objectName.Contains("SoloArtist")) achievementId = "SOLO_ARTIST";
        else if (objectName.Contains("Survivor")) achievementId = "SURVIVOR";
        else if (objectName.Contains("EarthPraiser")) achievementId = "EARTH_PRAISER";
        else if (objectName.Contains("WhatASnack")) achievementId = "WHAT_A_SNACK";
        else if (objectName.Contains("FishSlayer")) achievementId = "FISH_SLAYER";
        else if (objectName.Contains("WeComeInSwarms")) achievementId = "WE_COME_IN_SWARMS";
        else if (objectName.Contains("Gulper")) achievementId = "GULPER";
    }

    private bool IsUnlocked()
    {
        if (string.IsNullOrWhiteSpace(achievementId))
        {
            return false;
        }

        if (PlayerPrefs.GetInt("Achievement_" + achievementId, 0) == 1)
        {
            return true;
        }

        if (!SteamManager.Initialized)
        {
            return false;
        }

        bool unlocked = SteamUserStats.GetAchievement(achievementId, out bool achieved) && achieved;
        if (unlocked)
        {
            PlayerPrefs.SetInt("Achievement_" + achievementId, 1);
            PlayerPrefs.Save();
        }

        return unlocked;
    }
}
