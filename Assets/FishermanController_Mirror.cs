using Mirror;
using UnityEngine;

public class FishermanController_Mirror : NetworkBehaviour
{
    public FishermanController FishermanController;

    public void TryToSetJunkRod(NetworkIdentity hookIDidentity , Vector3 curruntRod)
    {
        if (hookIDidentity != null)
        {
            CmdSetJunkRod(hookIDidentity.netId , curruntRod);
        }
    }

    [Command]
    void CmdSetJunkRod(uint hookID , Vector3 curruntRod)
    {
        Debug.Log("CmdSetJunkRod called" + curruntRod);
        if (NetworkServer.spawned.TryGetValue(hookID, out NetworkIdentity hookIDidentity))
        {
            GameObject hookobj = hookIDidentity.gameObject;

            Hook hook = hookobj.GetComponent<Hook>();

            if (hook != null)
            {
                hook.rodTip = curruntRod;
            }

            RpcSetJunkRod(hookID, curruntRod);
        }
    }

    [ClientRpc]
    void RpcSetJunkRod(uint hookID, Vector3 curruntRod)
    {
        Debug.Log("RpcSetJunkRod called" + curruntRod);

        if (NetworkClient.spawned.TryGetValue(hookID, out NetworkIdentity hookIDidentity))
        {
            GameObject hookobj = hookIDidentity.gameObject;

            Hook hook = hookobj.GetComponent<Hook>();

            if (hook != null)
            {
                hook.rodTip = curruntRod;
            }
        }
    }
}