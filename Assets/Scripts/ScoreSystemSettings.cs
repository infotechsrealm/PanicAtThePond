using ExitGames.Client.Photon;
using UnityEngine;

[System.Serializable]
public class ScoreSystemSettings
{
    public const string FishermanWinPointsKey = "ss_fisherman_win";
    public const string FishermanCatchFishPointsKey = "ss_fisherman_catch";
    public const string FishermanBucketWormPointsKey = "ss_fisherman_bucket";
    public const string FishWinPointsKey = "ss_fish_win";
    public const string FishEatWormPointsKey = "ss_fish_worm";
    public const string FishSurvivePointsKey = "ss_fish_survive";
    public const string GoldenFishBonusPointsKey = "ss_golden_bonus";
    public const string SpacebarJamMinKey = "ss_mash_min";
    public const string SpacebarJamMaxKey = "ss_mash_max";
    public const string HungerWormRateKey = "ss_hunger_worm";
    public const string GoldenFishSpeedKey = "ss_golden_speed";

    public string fishermanWinPoints = string.Empty;
    public string fishermanCatchFishPoints = string.Empty;
    public string fishermanBucketWormPoints = string.Empty;
    public string fishWinPoints = string.Empty;
    public string fishEatWormPoints = string.Empty;
    public string fishSurvivePoints = string.Empty;
    public string goldenFishBonusPoints = string.Empty;
    public string spacebarJamMin = string.Empty;
    public string spacebarJamMax = string.Empty;
    public string hungerWormRateAmount = string.Empty;
    public string goldenFishSpeed = string.Empty;

    public void Reset()
    {
        fishermanWinPoints = string.Empty;
        fishermanCatchFishPoints = string.Empty;
        fishermanBucketWormPoints = string.Empty;
        fishWinPoints = string.Empty;
        fishEatWormPoints = string.Empty;
        fishSurvivePoints = string.Empty;
        goldenFishBonusPoints = string.Empty;
        spacebarJamMin = string.Empty;
        spacebarJamMax = string.Empty;
        hungerWormRateAmount = string.Empty;
        goldenFishSpeed = string.Empty;
    }

    public void CopyFrom(ScoreSystemSettings other)
    {
        if (other == null)
        {
            Reset();
            return;
        }

        fishermanWinPoints = other.fishermanWinPoints ?? string.Empty;
        fishermanCatchFishPoints = other.fishermanCatchFishPoints ?? string.Empty;
        fishermanBucketWormPoints = other.fishermanBucketWormPoints ?? string.Empty;
        fishWinPoints = other.fishWinPoints ?? string.Empty;
        fishEatWormPoints = other.fishEatWormPoints ?? string.Empty;
        fishSurvivePoints = other.fishSurvivePoints ?? string.Empty;
        goldenFishBonusPoints = other.goldenFishBonusPoints ?? string.Empty;
        spacebarJamMin = other.spacebarJamMin ?? string.Empty;
        spacebarJamMax = other.spacebarJamMax ?? string.Empty;
        hungerWormRateAmount = other.hungerWormRateAmount ?? string.Empty;
        goldenFishSpeed = other.goldenFishSpeed ?? string.Empty;
    }

    public Hashtable ToPhotonProperties()
    {
        return new Hashtable
        {
            [FishermanWinPointsKey] = fishermanWinPoints ?? string.Empty,
            [FishermanCatchFishPointsKey] = fishermanCatchFishPoints ?? string.Empty,
            [FishermanBucketWormPointsKey] = fishermanBucketWormPoints ?? string.Empty,
            [FishWinPointsKey] = fishWinPoints ?? string.Empty,
            [FishEatWormPointsKey] = fishEatWormPoints ?? string.Empty,
            [FishSurvivePointsKey] = fishSurvivePoints ?? string.Empty,
            [GoldenFishBonusPointsKey] = goldenFishBonusPoints ?? string.Empty,
            [SpacebarJamMinKey] = spacebarJamMin ?? string.Empty,
            [SpacebarJamMaxKey] = spacebarJamMax ?? string.Empty,
            [HungerWormRateKey] = hungerWormRateAmount ?? string.Empty,
            [GoldenFishSpeedKey] = goldenFishSpeed ?? string.Empty
        };
    }

    public void ApplyPhotonProperties(Hashtable properties)
    {
        if (properties == null)
        {
            return;
        }

        fishermanWinPoints = GetStringValue(properties, FishermanWinPointsKey);
        fishermanCatchFishPoints = GetStringValue(properties, FishermanCatchFishPointsKey);
        fishermanBucketWormPoints = GetStringValue(properties, FishermanBucketWormPointsKey);
        fishWinPoints = GetStringValue(properties, FishWinPointsKey);
        fishEatWormPoints = GetStringValue(properties, FishEatWormPointsKey);
        fishSurvivePoints = GetStringValue(properties, FishSurvivePointsKey);
        goldenFishBonusPoints = GetStringValue(properties, GoldenFishBonusPointsKey);
        spacebarJamMin = GetStringValue(properties, SpacebarJamMinKey);
        spacebarJamMax = GetStringValue(properties, SpacebarJamMaxKey);
        hungerWormRateAmount = GetStringValue(properties, HungerWormRateKey);
        goldenFishSpeed = GetStringValue(properties, GoldenFishSpeedKey);
    }

    public int GetFishermanWinPoints()
    {
        return ParseIntOrDefault(fishermanWinPoints, 15, 0, 999);
    }

    public int GetFishermanCatchFishPoints()
    {
        return ParseIntOrDefault(fishermanCatchFishPoints, 3, 0, 999);
    }

    public int GetFishermanBucketWormPoints()
    {
        return ParseIntOrDefault(fishermanBucketWormPoints, 1, 0, 999);
    }

    public int GetFishWinPoints()
    {
        return ParseIntOrDefault(fishWinPoints, 10, 0, 999);
    }

    public int GetFishEatWormPoints()
    {
        return ParseIntOrDefault(fishEatWormPoints, 1, 0, 999);
    }

    public int GetFishSurvivePoints()
    {
        return ParseIntOrDefault(fishSurvivePoints, 5, 0, 999);
    }

    public int GetGoldenFishBonusPoints()
    {
        return ParseIntOrDefault(goldenFishBonusPoints, 0, 0, 999);
    }

    public float GetSpacebarJamMin()
    {
        ResolveMashRange(out float minValue, out _);
        return minValue;
    }

    public float GetSpacebarJamMax()
    {
        ResolveMashRange(out _, out float maxValue);
        return maxValue;
    }

    public float GetHungerWormRateAmount()
    {
        return ParseFloatOrDefault(hungerWormRateAmount, 15f, 0f, 100f);
    }

    public bool TryGetGoldenFishSpeed(out float configuredSpeed)
    {
        if (float.TryParse(goldenFishSpeed, out configuredSpeed))
        {
            configuredSpeed = Mathf.Clamp(configuredSpeed, 0.1f, 100f);
            return true;
        }

        configuredSpeed = 0f;
        return false;
    }

    private void ResolveMashRange(out float minValue, out float maxValue)
    {
        minValue = ParseFloatOrDefault(spacebarJamMin, 30f, 1f, 100f);
        maxValue = ParseFloatOrDefault(spacebarJamMax, 70f, 1f, 100f);

        if (maxValue < minValue)
        {
            float swap = minValue;
            minValue = maxValue;
            maxValue = swap;
        }
    }

    private static int ParseIntOrDefault(string rawValue, int fallbackValue, int minValue, int maxValue)
    {
        if (!int.TryParse(rawValue, out int parsedValue))
        {
            parsedValue = fallbackValue;
        }

        return Mathf.Clamp(parsedValue, minValue, maxValue);
    }

    private static float ParseFloatOrDefault(string rawValue, float fallbackValue, float minValue, float maxValue)
    {
        if (!float.TryParse(rawValue, out float parsedValue))
        {
            parsedValue = fallbackValue;
        }

        return Mathf.Clamp(parsedValue, minValue, maxValue);
    }

    private static string GetStringValue(Hashtable properties, string key)
    {
        if (properties.TryGetValue(key, out object value) && value != null)
        {
            return value.ToString();
        }

        return string.Empty;
    }
}
