using Mirror;
using UnityEngine;

public class GoldenFishAI_Mirror : NetworkBehaviour
{
    public static GoldenFishAI_Mirror Instance;

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        Debug.Log("=== GoldenFishAI_Mirror CALLED ===");
        Debug.Log("isServer: " + isServer);
        Debug.Log("isClient: " + isClient);
        Debug.Log("isLocalPlayer: " + isLocalPlayer);
        Debug.Log("connectionToClient: " + connectionToClient);
    }
    public void Destroy_Mirror(GameObject target)
    {
        if (NetworkServer.active)
        {
            NetworkServer.Destroy(target);
        }
        else if (NetworkClient.active)
        {
            CmdRequestDestroy(target);
        }

    }

    [Command]
    void CmdRequestDestroy(GameObject target)
    {
        NetworkServer.Destroy(target);
    }
}
