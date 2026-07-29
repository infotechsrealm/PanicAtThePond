using Mirror;
using UnityEngine;

using PanicAtThePond.Managers;
using PanicAtThePond.Controllers;
using PanicAtThePond.UI;
using PanicAtThePond.Shop;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.Gameplay
{
public class WormManager_Mirror : NetworkBehaviour

{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("=== WormManager_Mirror CALLED ===");
        Debug.Log("isServer: " + isServer);
        Debug.Log("isClient: " + isClient);
        Debug.Log("isLocalPlayer: " + isLocalPlayer);
        Debug.Log("connectionToClient: " + connectionToClient);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

}