using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable snapshot of the current Sal-T shop rotation. Produced by the authority
/// (host/master client) from shop_config.json and shipped to clients as JSON — clients render
/// exactly this payload and never derive prices or item picks locally.
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
public class SaltShopState
{
    [Serializable]
    public class ShopItem
    {
        public string id;
        public string displayName;
        public string category;
        public int price;
        public string iconResource;
    }

    public long windowStartUtcTicks;
    public long windowEndUtcTicks;

    /// <summary>
    /// The whole rotation pool for this window, in the authority's deterministic order.
    /// It is NOT the shelf contents: the shelf shows the first <see cref="visibleSlots"/> entries
    /// that the *local* player has not unlocked yet (PDF: "new hats that they don't have unlocked").
    /// Unlocks are per-player, so this per-player filter has to happen client-side — but the order
    /// and the prices still come from the authority, so two players never see different prices.
    /// </summary>
    public List<ShopItem> items = new List<ShopItem>();

    /// <summary>How many shelf slots the shop front may fill (shop_config.json "rotationSlots").</summary>
    public int visibleSlots = 3;

    public DateTime WindowEndUtc => new DateTime(windowEndUtcTicks, DateTimeKind.Utc);

    public string ToJson() => JsonUtility.ToJson(this);

    public static SaltShopState FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            SaltShopState state = JsonUtility.FromJson<SaltShopState>(json);
            if (state == null || state.items == null)
            {
                return null;
            }

            // Payloads written before visibleSlots existed deserialize it as 0; fall back to the
            // item count so those rooms still fill the shelf instead of showing nothing.
            state.visibleSlots = state.visibleSlots > 0 ? state.visibleSlots : Mathf.Max(1, state.items.Count);
            return state;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaltShopState] Failed to parse shop state: {e.Message}");
            return null;
        }
    }
}

/// <summary>
/// Server-side shop logic. Reads shop_config.json and resolves the rotation for the current
/// 24-hour window: N random hats from the rotation pool, locked for the whole window.
///
/// The pick is seeded by the UTC window index (unix-time / interval) + a salt from the JSON, so
/// every authority computes the identical rotation for the same real-world day. In a multiplayer
/// session the host/master broadcasts its resolved state and clients display only that payload;
/// offline the same deterministic pick keeps the shop consistent with what servers show.
/// </summary>
public static class SaltShopService
{
    private static readonly DateTime UnixEpochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Computes the locked shop rotation for the window containing <paramref name="utcNow"/>.</summary>
    public static SaltShopState ResolveCurrentShop(DateTime utcNow)
    {
        ShopConfig config = ShopConfig.Load();
        if (config == null)
        {
            return null;
        }

        double intervalSeconds = config.rotationIntervalHours * 3600.0;
        double secondsSinceEpoch = (utcNow - UnixEpochUtc).TotalSeconds;
        long windowIndex = (long)Math.Floor(secondsSinceEpoch / intervalSeconds);

        DateTime windowStart = UnixEpochUtc.AddSeconds(windowIndex * intervalSeconds);
        DateTime windowEnd = windowStart.AddSeconds(intervalSeconds);

        List<ShopConfig.HatEntry> pool = new List<ShopConfig.HatEntry>();
        foreach (ShopConfig.HatEntry hat in config.hats)
        {
            if (hat != null && hat.inRotation && !hat.unlockedByDefault)
            {
                pool.Add(hat);
            }
        }

        // Deterministic shuffle: same day + same config ⇒ same rotation everywhere.
        int seed = unchecked((int)(windowIndex * 31 + config.rotationSeedSalt));
        System.Random rng = new System.Random(seed);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        SaltShopState state = new SaltShopState
        {
            windowStartUtcTicks = windowStart.Ticks,
            windowEndUtcTicks = windowEnd.Ticks,
            visibleSlots = config.rotationSlots
        };

        // Ship the WHOLE shuffled pool, not just the first N. Each client then takes the first N
        // entries it has not unlocked, so a player who already owns today's first pick still gets a
        // full shelf, and a player who owns all but two sees exactly those two (PDF 1.1.8).
        for (int i = 0; i < pool.Count; i++)
        {
            state.items.Add(new SaltShopState.ShopItem
            {
                id = pool[i].id,
                displayName = pool[i].displayName,
                category = pool[i].category,
                price = pool[i].price,
                iconResource = pool[i].iconResource
            });
        }

        return state;
    }

    public static SaltShopState ResolveCurrentShop() => ResolveCurrentShop(DateTime.UtcNow);
}

}