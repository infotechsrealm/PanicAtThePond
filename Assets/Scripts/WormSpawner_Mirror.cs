using Mirror;
using UnityEngine;

public class WormSpawner_Mirror : NetworkBehaviour
{
    public static WormSpawner_Mirror Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void DestroyWorm_Mirror(GameObject target)
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
