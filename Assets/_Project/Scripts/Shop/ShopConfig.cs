using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Data model + loader for StreamingAssets/shop_config.json — the single balancing file that
/// controls every hat item, its price and the pool of items available for the daily rotation.
/// Only the authority (Mirror host / Photon master client / offline fallback) reads this file;
/// regular clients receive the resolved shop state over the network (see SaltShopClientState).
/// </summary>
using PanicAtThePond.Managers;
using PanicAtThePond.Controllers;
using PanicAtThePond.Gameplay;
using PanicAtThePond.UI;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.Shop
{
[Serializable]
public class ShopConfig
{
    [Serializable]
    public class HatEntry
    {
        public string id;
        public string displayName;
        public string category;
        public int price;
        public string iconResource;
        public bool inRotation;
        public bool unlockedByDefault;
    }

    [SerializeField] private int configVersion = 1;
    public string currency = "WC";
    public int rotationSlots = 3;
    public int rotationIntervalHours = 24;
    public int rotationSeedSalt = 0;
    public List<HatEntry> hats = new List<HatEntry>();

    public const string FileName = "shop_config.json";

    private static ShopConfig cached;

    public static string ConfigFilePath => Path.Combine(Application.streamingAssetsPath, FileName);

    /// <summary>
    /// Loads and caches the config from StreamingAssets. Returns null when the file is missing
    /// or unparsable so callers can fail loudly instead of inventing prices client-side.
    /// </summary>
    public static ShopConfig Load()
    {
        if (cached != null)
        {
            return cached;
        }

        try
        {
            string path = ConfigFilePath;
            if (!File.Exists(path))
            {
                Debug.LogError($"[ShopConfig] Missing shop config at: {path}");
                return null;
            }

            string json = File.ReadAllText(path);
            ShopConfig config = JsonUtility.FromJson<ShopConfig>(json);
            if (config == null || config.hats == null || config.hats.Count == 0)
            {
                Debug.LogError("[ShopConfig] shop_config.json parsed empty — check the JSON structure.");
                return null;
            }

            config.rotationSlots = Mathf.Max(1, config.rotationSlots);
            config.rotationIntervalHours = Mathf.Max(1, config.rotationIntervalHours);
            cached = config;
            Debug.Log($"[ShopConfig] Loaded {config.hats.Count} hats, {config.rotationSlots} rotation slots, {config.rotationIntervalHours}h interval.");
            return cached;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ShopConfig] Failed to load shop config: {e.Message}");
            return null;
        }
    }

    /// <summary>Drops the cache so edits to the JSON are picked up without restarting.</summary>
    public static void Reload()
    {
        cached = null;
        Load();
    }

    public HatEntry FindHat(string hatId)
    {
        if (string.IsNullOrEmpty(hatId))
        {
            return null;
        }

        for (int i = 0; i < hats.Count; i++)
        {
            if (hats[i] != null && hats[i].id == hatId)
            {
                return hats[i];
            }
        }
        return null;
    }
}

}