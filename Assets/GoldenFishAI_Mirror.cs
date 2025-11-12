using Mirror;
using UnityEngine;

public class GoldenFishAI_Mirror : NetworkBehaviour
{
    public static GoldenFishAI_Mirror Instance;

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
