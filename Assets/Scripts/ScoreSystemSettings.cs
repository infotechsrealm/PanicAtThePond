using ExitGames.Client.Photon;
using UnityEngine;

[System.Serializable]
public class ScoreSystemSettings
{
    public const int DefaultFishermanWinPoints = 15;
    public const int DefaultFishermanCatchFishPoints = 3;
    public const int DefaultFishermanBucketWormPoints = 1;
    public const int DefaultFishWinPoints = 10;
    public const int DefaultFishEatWormPoints = 1;
    public const int DefaultFishSurvivePoints = 5;
    public const int DefaultGoldenFishBonusPoints = 0;
    public const float DefaultSpacebarJamMin = 30f;
    public const float DefaultSpacebarJamMax = 70f;
    public const float DefaultFishTimerSeconds = 3f;
    public const float DefaultHungerWormRateAmount = 15f;
    public const float DefaultHungerDepletionRate = 1f;
    public const float DefaultTroutSpeed = 3f;
    public const float DefaultGoldenFishSpeed = 3f;

    public const string FishermanWinPointsKey = "ss_fisherman_win";
    public const string FishermanCatchFishPointsKey = "ss_fisherman_catch";
    public const string FishermanBucketWormPointsKey = "ss_fisherman_bucket";
    public const string FishWinPointsKey = "ss_fish_win";
    public const string FishEatWormPointsKey = "ss_fish_worm";
    public const string FishSurvivePointsKey = "ss_fish_survive";
    public const string GoldenFishBonusPointsKey = "ss_golden_bonus";
    public const string SpacebarJamMinKey = "ss_mash_min";
    public const string SpacebarJamMaxKey = "ss_mash_max";
    public const string FishTimerSecondsKey = "ss_fish_timer";
    public const string HungerWormRateKey = "ss_hunger_worm";
    public const string HungerDepletionRateKey = "ss_hunger_depletion";
    public const string GoldenFishSpeedKey = "ss_golden_speed";
    public const string TroutSpeedKey = "ss_trout_speed";

    public string fishermanWinPoints = DefaultFishermanWinPoints.ToString();
    public string fishermanCatchFishPoints = DefaultFishermanCatchFishPoints.ToString();
    public string fishermanBucketWormPoints = DefaultFishermanBucketWormPoints.ToString();
    public string fishWinPoints = DefaultFishWinPoints.ToString();
    public string fishEatWormPoints = DefaultFishEatWormPoints.ToString();
    public string fishSurvivePoints = DefaultFishSurvivePoints.ToString();
    public string goldenFishBonusPoints = DefaultGoldenFishBonusPoints.ToString();
    public string spacebarJamMin = DefaultSpacebarJamMin.ToString();
    public string spacebarJamMax = DefaultSpacebarJamMax.ToString();
    public string fishTimerSeconds = DefaultFishTimerSeconds.ToString();
    public string hungerWormRateAmount = DefaultHungerWormRateAmount.ToString();
    public string hungerDepletionRate = DefaultHungerDepletionRate.ToString();
    public string goldenFishSpeed = DefaultGoldenFishSpeed.ToString();
    public string troutSpeed = DefaultTroutSpeed.ToString();

    public void Reset()
    {
        fishermanWinPoints = DefaultFishermanWinPoints.ToString();
        fishermanCatchFishPoints = DefaultFishermanCatchFishPoints.ToString();
        fishermanBucketWormPoints = DefaultFishermanBucketWormPoints.ToString();
        fishWinPoints = DefaultFishWinPoints.ToString();
        fishEatWormPoints = DefaultFishEatWormPoints.ToString();
        fishSurvivePoints = DefaultFishSurvivePoints.ToString();
        goldenFishBonusPoints = DefaultGoldenFishBonusPoints.ToString();
        spacebarJamMin = DefaultSpacebarJamMin.ToString();
        spacebarJamMax = DefaultSpacebarJamMax.ToString();
        fishTimerSeconds = DefaultFishTimerSeconds.ToString();
        hungerWormRateAmount = DefaultHungerWormRateAmount.ToString();
        hungerDepletionRate = DefaultHungerDepletionRate.ToString();
        goldenFishSpeed = DefaultGoldenFishSpeed.ToString();
        troutSpeed = DefaultTroutSpeed.ToString();
    }

    public void FillBlankValuesWithDefaults()
    {
        fishermanWinPoints = DefaultIfBlank(fishermanWinPoints, DefaultFishermanWinPoints.ToString());
        fishermanCatchFishPoints = DefaultIfBlank(fishermanCatchFishPoints, DefaultFishermanCatchFishPoints.ToString());
        fishermanBucketWormPoints = DefaultIfBlank(fishermanBucketWormPoints, DefaultFishermanBucketWormPoints.ToString());
        fishWinPoints = DefaultIfBlank(fishWinPoints, DefaultFishWinPoints.ToString());
        fishEatWormPoints = DefaultIfBlank(fishEatWormPoints, DefaultFishEatWormPoints.ToString());
        fishSurvivePoints = DefaultIfBlank(fishSurvivePoints, DefaultFishSurvivePoints.ToString());
        goldenFishBonusPoints = DefaultIfBlank(goldenFishBonusPoints, DefaultGoldenFishBonusPoints.ToString());
        spacebarJamMin = DefaultIfBlank(spacebarJamMin, DefaultSpacebarJamMin.ToString());
        spacebarJamMax = DefaultIfBlank(spacebarJamMax, DefaultSpacebarJamMax.ToString());
        fishTimerSeconds = DefaultIfBlank(fishTimerSeconds, DefaultFishTimerSeconds.ToString());
        hungerWormRateAmount = DefaultIfBlank(hungerWormRateAmount, DefaultHungerWormRateAmount.ToString());
        hungerDepletionRate = DefaultIfBlank(hungerDepletionRate, DefaultHungerDepletionRate.ToString());
        goldenFishSpeed = DefaultIfBlank(goldenFishSpeed, DefaultGoldenFishSpeed.ToString());
        troutSpeed = DefaultIfBlank(troutSpeed, DefaultTroutSpeed.ToString());
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
        fishTimerSeconds = other.fishTimerSeconds ?? string.Empty;
        hungerWormRateAmount = other.hungerWormRateAmount ?? string.Empty;
        hungerDepletionRate = other.hungerDepletionRate ?? string.Empty;
        goldenFishSpeed = other.goldenFishSpeed ?? string.Empty;
        troutSpeed = other.troutSpeed ?? string.Empty;
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
            [FishTimerSecondsKey] = fishTimerSeconds ?? string.Empty,
            [HungerWormRateKey] = hungerWormRateAmount ?? string.Empty,
            [HungerDepletionRateKey] = hungerDepletionRate ?? string.Empty,
            [GoldenFishSpeedKey] = goldenFishSpeed ?? string.Empty,
            [TroutSpeedKey] = troutSpeed ?? string.Empty
        };
    }

    public void ApplyPhotonProperties(Hashtable properties)
    {
        if (properties == null)
        {
            return;
        }

        fishermanWinPoints = GetStringValue(properties, FishermanWinPointsKey, DefaultFishermanWinPoints.ToString());
        fishermanCatchFishPoints = GetStringValue(properties, FishermanCatchFishPointsKey, DefaultFishermanCatchFishPoints.ToString());
        fishermanBucketWormPoints = GetStringValue(properties, FishermanBucketWormPointsKey, DefaultFishermanBucketWormPoints.ToString());
        fishWinPoints = GetStringValue(properties, FishWinPointsKey, DefaultFishWinPoints.ToString());
        fishEatWormPoints = GetStringValue(properties, FishEatWormPointsKey, DefaultFishEatWormPoints.ToString());
        fishSurvivePoints = GetStringValue(properties, FishSurvivePointsKey, DefaultFishSurvivePoints.ToString());
        goldenFishBonusPoints = GetStringValue(properties, GoldenFishBonusPointsKey, DefaultGoldenFishBonusPoints.ToString());
        spacebarJamMin = GetStringValue(properties, SpacebarJamMinKey, DefaultSpacebarJamMin.ToString());
        spacebarJamMax = GetStringValue(properties, SpacebarJamMaxKey, DefaultSpacebarJamMax.ToString());
        fishTimerSeconds = GetStringValue(properties, FishTimerSecondsKey, DefaultFishTimerSeconds.ToString());
        hungerWormRateAmount = GetStringValue(properties, HungerWormRateKey, DefaultHungerWormRateAmount.ToString());
        hungerDepletionRate = GetStringValue(properties, HungerDepletionRateKey, DefaultHungerDepletionRate.ToString());
        goldenFishSpeed = GetStringValue(properties, GoldenFishSpeedKey, DefaultGoldenFishSpeed.ToString());
        troutSpeed = GetStringValue(properties, TroutSpeedKey, DefaultTroutSpeed.ToString());
    }

    public int GetFishermanWinPoints()
    {
        return ParseIntOrDefault(fishermanWinPoints, DefaultFishermanWinPoints, 0, 999);
    }

    public int GetFishermanCatchFishPoints()
    {
        return ParseIntOrDefault(fishermanCatchFishPoints, DefaultFishermanCatchFishPoints, 0, 999);
    }

    public int GetFishermanBucketWormPoints()
    {
        return ParseIntOrDefault(fishermanBucketWormPoints, DefaultFishermanBucketWormPoints, 0, 999);
    }

    public int GetFishWinPoints()
    {
        return ParseIntOrDefault(fishWinPoints, DefaultFishWinPoints, 0, 999);
    }

    public int GetFishEatWormPoints()
    {
        return ParseIntOrDefault(fishEatWormPoints, DefaultFishEatWormPoints, 0, 999);
    }

    public int GetFishSurvivePoints()
    {
        return ParseIntOrDefault(fishSurvivePoints, DefaultFishSurvivePoints, 0, 999);
    }

    public int GetGoldenFishBonusPoints()
    {
        return ParseIntOrDefault(goldenFishBonusPoints, DefaultGoldenFishBonusPoints, 0, 999);
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
        return ParseFloatOrDefault(hungerWormRateAmount, DefaultHungerWormRateAmount, 0f, 100f);
    }

    public float GetFishTimerSeconds()
    {
        return ParseFloatOrDefault(fishTimerSeconds, DefaultFishTimerSeconds, 0.5f, 60f);
    }

    public float GetHungerDepletionRate()
    {
        return ParseFloatOrDefault(hungerDepletionRate, DefaultHungerDepletionRate, 0f, 100f);
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

    public float GetTroutSpeed()
    {
        return ParseFloatOrDefault(troutSpeed, DefaultTroutSpeed, 0.1f, 100f);
    }

    public float GetGoldenFishSpeed()
    {
        return ParseFloatOrDefault(goldenFishSpeed, DefaultGoldenFishSpeed, 0.1f, 100f);
    }

    private void ResolveMashRange(out float minValue, out float maxValue)
    {
        minValue = ParseFloatOrDefault(spacebarJamMin, DefaultSpacebarJamMin, 1f, 100f);
        maxValue = ParseFloatOrDefault(spacebarJamMax, DefaultSpacebarJamMax, 1f, 100f);

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

    private static string GetStringValue(Hashtable properties, string key, string fallbackValue)
    {
        if (properties.TryGetValue(key, out object value) && value != null)
        {
            return value.ToString();
        }

        return fallbackValue;
    }

    private static string DefaultIfBlank(string value, string fallbackValue)
    {
        return string.IsNullOrWhiteSpace(value) ? fallbackValue : value;
    }
}
