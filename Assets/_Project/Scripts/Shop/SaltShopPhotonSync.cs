using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Photon side of the server-authoritative Sal-T shop.
/// The master client resolves the rotation from shop_config.json and publishes it as a room
/// property; every client (including late joiners) reads that property and displays it verbatim.
/// Installs itself at startup so no scene wiring is needed.
/// </summary>
using PanicAtThePond.Managers;
using PanicAtThePond.Controllers;
using PanicAtThePond.Gameplay;
using PanicAtThePond.UI;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.Shop
{
public class SaltShopPhotonSync : MonoBehaviourPunCallbacks
{
    private static SaltShopPhotonSync instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("SaltShopPhotonSync");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SaltShopPhotonSync>();
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PublishShopState();
        }
        else
        {
            ApplyRoomShopState();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // If the authority left, the new master re-publishes so the room always has a valid state.
        if (PhotonNetwork.LocalPlayer != null && newMasterClient != null &&
            PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
        {
            PublishShopState();
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged != null && propertiesThatChanged.ContainsKey(SaltShopClientState.PhotonRoomPropertyKey))
        {
            ApplyRoomShopState();
        }
    }

    public override void OnLeftRoom()
    {
        SaltShopClientState.ClearServerState();
    }

    private static void PublishShopState()
    {
        SaltShopState state = SaltShopService.ResolveCurrentShop();
        if (state == null)
        {
            Debug.LogError("[SaltShopPhotonSync] Could not resolve shop state from shop_config.json.");
            return;
        }

        string json = state.ToJson();
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            { SaltShopClientState.PhotonRoomPropertyKey, json }
        });

        // The master is also a client of this state — apply locally right away.
        SaltShopClientState.ApplyServerState(json);
    }

    private static void ApplyRoomShopState()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(SaltShopClientState.PhotonRoomPropertyKey, out object value) &&
            value is string json)
        {
            SaltShopClientState.ApplyServerState(json);
        }
    }
}

}