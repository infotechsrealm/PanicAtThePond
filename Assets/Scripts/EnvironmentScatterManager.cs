using System;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentScatterManager : MonoBehaviour
{
    [SerializeField] private string environmentRootName = "Environment";
    [SerializeField] private float horizontalJitter = 0.25f;
    [SerializeField] private float verticalJitter = 0.12f;
    [SerializeField] private float scaleJitterPercent = 0.08f;

    private bool layoutApplied;
    private readonly Dictionary<Transform, Vector3> originalLocalScales = new Dictionary<Transform, Vector3>();

    public void ApplyLayoutIfNeeded()
    {
        if (layoutApplied)
        {
            return;
        }

        layoutApplied = TryApplyLayout();
    }

    private bool TryApplyLayout()
    {
        GameObject environmentRoot = GameObject.Find(environmentRootName);
        if (environmentRoot == null)
        {
            return false;
        }

        List<Transform> organizedGroups = new List<Transform>();
        foreach (Transform group in environmentRoot.GetComponentsInChildren<Transform>(true))
        {
            List<Transform> plants = GetDirectPlantChildren(group);
            if (plants.Count >= 2)
            {
                organizedGroups.Add(group);
                ArrangePlantGroup(group, plants);
            }
        }

        if (organizedGroups.Count > 0)
        {
            return true;
        }

        List<Transform> fallbackPlants = GetAllPlants(environmentRoot.transform);
        if (fallbackPlants.Count >= 2)
        {
            ArrangePlantGroup(environmentRoot.transform, fallbackPlants);
            return true;
        }

        return false;
    }

    private List<Transform> GetDirectPlantChildren(Transform parent)
    {
        List<Transform> result = new List<Transform>();
        foreach (Transform child in parent)
        {
            if (IsPlant(child))
            {
                result.Add(child);
            }
        }

        result.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        return result;
    }

    private List<Transform> GetAllPlants(Transform root)
    {
        List<Transform> result = new List<Transform>();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != root && IsPlant(child))
            {
                result.Add(child);
            }
        }

        result.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        return result;
    }

    private bool IsPlant(Transform candidate)
    {
        return candidate.name.IndexOf("plant", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void ArrangePlantGroup(Transform group, List<Transform> plants)
    {
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < plants.Count; i++)
        {
            Vector3 localPosition = plants[i].localPosition;
            minX = Mathf.Min(minX, localPosition.x);
            maxX = Mathf.Max(maxX, localPosition.x);
            minY = Mathf.Min(minY, localPosition.y);
            maxY = Mathf.Max(maxY, localPosition.y);

            if (!originalLocalScales.ContainsKey(plants[i]))
            {
                originalLocalScales[plants[i]] = plants[i].localScale;
            }
        }

        if (Mathf.Approximately(minX, maxX))
        {
            minX -= 1f;
            maxX += 1f;
        }

        if (Mathf.Approximately(minY, maxY))
        {
            minY -= 0.15f;
            maxY += 0.15f;
        }

        System.Random random = new System.Random(BuildSeed(group.name));
        for (int i = 0; i < plants.Count; i++)
        {
            Transform plant = plants[i];
            float t = plants.Count == 1 ? 0.5f : i / (float)(plants.Count - 1);
            float baseX = Mathf.Lerp(minX, maxX, t);
            float centered = Mathf.Abs((t * 2f) - 1f);
            float baseY = Mathf.Lerp(maxY, minY, centered * 0.65f);

            float randomX = NextRange(random, -horizontalJitter, horizontalJitter);
            float randomY = NextRange(random, -verticalJitter, verticalJitter);
            float scaleMultiplier = 1f + NextRange(random, -scaleJitterPercent, scaleJitterPercent);

            Vector3 localPosition = plant.localPosition;
            plant.localPosition = new Vector3(baseX + randomX, baseY + randomY, localPosition.z);
            plant.localScale = originalLocalScales[plant] * scaleMultiplier;
        }
    }

    private int BuildSeed(string groupName)
    {
        int round = GS.Instance != null ? GS.Instance.currentRound : 1;
        int mode = GS.Instance != null ? GS.Instance.currentGameMode : 0;
        return (round * 397) ^ (mode * 53) ^ StableHash(groupName);
    }

    private float NextRange(System.Random random, float min, float max)
    {
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }

    private int StableHash(string value)
    {
        unchecked
        {
            int hash = 23;
            for (int i = 0; i < value.Length; i++)
            {
                hash = (hash * 31) + value[i];
            }

            return hash;
        }
    }
}
