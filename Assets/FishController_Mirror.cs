using UnityEngine;
using Mirror;

public class FishController_Mirror : NetworkBehaviour
{
    public static FishController_Mirror Instance;

    private void Awake()
    {
        Instance = this;
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
