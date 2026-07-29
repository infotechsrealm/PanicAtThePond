using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper script to load sprite sheets from their current location
/// Works without needing to move files to Resources folder
/// </summary>
using PanicAtThePond.Managers;
using PanicAtThePond.Controllers;
using PanicAtThePond.Gameplay;
using PanicAtThePond.UI;
using PanicAtThePond.Shop;
using PanicAtThePond.Data;

namespace PanicAtThePond.Utilities
{
public class FishermanSpriteLoader
{
    private const string SPRITES_FOLDER = "Assets/Animations/Fisher Man Animations/Sprite Sheets/";

    /// <summary>
    /// Load a sprite sheet by name (without extension)
    /// </summary>
    public static Texture2D LoadSpriteSheet(string sheetName)
    {
        #if UNITY_EDITOR
        // In editor, load directly from Assets folder
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
            SPRITES_FOLDER + "FishermansAnimations-" + sheetName + "_Sheet.png"
        );

        if (texture != null)
        {
            Debug.Log($"✓ Loaded: {sheetName}");
            return texture;
        }
        #endif

        // Fallback to Resources
        Texture2D fallback = Resources.Load<Texture2D>("Sprites/FishermansAnimations-" + sheetName + "_Sheet");
        if (fallback != null)
        {
            return fallback;
        }

        Debug.LogError($"❌ Could not load sprite sheet: {sheetName}");
        return null;
    }

    /// <summary>
    /// Get all available sprite sheets
    /// </summary>
    public static Dictionary<string, Texture2D> LoadAllSheets()
    {
        Dictionary<string, Texture2D> sheets = new Dictionary<string, Texture2D>();

        string[] names = { "GreenBody", "Arms", "Boat", "Rods", "Oars" };

        foreach (string name in names)
        {
            Texture2D sheet = LoadSpriteSheet(name);
            if (sheet != null)
            {
                sheets[name] = sheet;
            }
        }

        return sheets;
    }
}

}