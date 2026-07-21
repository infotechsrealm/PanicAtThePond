using System;
using UnityEngine;

/// <summary>
/// Client-side holder of the current Sal-T shop. The UI reads ONLY from here.
///
/// Sources, in priority order:
///  1. A payload pushed by the session authority (Mirror host via SaltShopMessage,
///     Photon master client via the room property) — always wins while connected.
///  2. When no server payload has arrived (main menu / offline), the state falls back to the
///     locally resolved deterministic rotation, which matches what any authority would send
///     for the same UTC window.
/// </summary>
public static class SaltShopClientState
{
    public const string PhotonRoomPropertyKey = "saltShopState";

    private static SaltShopState current;
    private static bool currentCameFromServer;

    /// <summary>Raised whenever the shop state changes so open UI can rebuild.</summary>
    public static event Action OnShopStateChanged;

    /// <summary>Applies a payload received from the session authority. Returns true when accepted.</summary>
    public static bool ApplyServerState(string json)
    {
        SaltShopState state = SaltShopState.FromJson(json);
        if (state == null)
        {
            Debug.LogWarning("[SaltShopClientState] Rejected malformed server shop payload.");
            return false;
        }

        current = state;
        currentCameFromServer = true;
        Debug.Log($"[SaltShopClientState] Received server shop rotation ({state.items.Count} items, ends {state.WindowEndUtc:u}).");
        OnShopStateChanged?.Invoke();
        return true;
    }

    /// <summary>Called on disconnect so a stale session payload cannot outlive the session.</summary>
    public static void ClearServerState()
    {
        if (!currentCameFromServer)
        {
            return;
        }

        current = null;
        currentCameFromServer = false;
        OnShopStateChanged?.Invoke();
    }

    /// <summary>
    /// Current shop state for display, or null while a networked client is still waiting for the
    /// authority's payload. A server payload is kept verbatim until the server replaces it or we
    /// disconnect; the local deterministic rotation is only ever used when there is no authority
    /// to defer to (single player / main menu), never to second-guess a server we are joined to.
    /// </summary>
    public static SaltShopState GetCurrent()
    {
        if (currentCameFromServer)
        {
            return current;
        }

        // Joined to someone else's session but their payload has not landed yet: show nothing
        // rather than a locally generated shop, so prices/items can never disagree with the host.
        if (IsAwaitingRemoteAuthority())
        {
            return null;
        }

        if (current != null && DateTime.UtcNow >= current.WindowEndUtc)
        {
            current = null; // window expired — recompute below
        }

        if (current == null)
        {
            current = SaltShopService.ResolveCurrentShop();
        }

        return current;
    }

    /// <summary>
    /// True when this peer is a non-authoritative member of a live session, so the shop must come
    /// down the wire. Mirror: a client that is not also the host. Photon: a non-master client.
    /// </summary>
    private static bool IsAwaitingRemoteAuthority()
    {
        if (Mirror.NetworkClient.active && !Mirror.NetworkServer.active)
        {
            return true;
        }

        if (Photon.Pun.PhotonNetwork.InRoom && !Photon.Pun.PhotonNetwork.IsMasterClient)
        {
            return true;
        }

        return false;
    }

    public static bool HasServerState => currentCameFromServer;
}
