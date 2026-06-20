using Mirror;
using UnityEngine;

public class Hook_Mirror : NetworkBehaviour
{

    public Hook hook;

    private void Awake()
    {
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("=== Hook_Mirror CALLED ===");
        Debug.Log("isServer: " + isServer);
        Debug.Log("isClient: " + isClient);
        Debug.Log("isLocalPlayer: " + isLocalPlayer);
        Debug.Log("connectionToClient: " + connectionToClient);
    }

    // Update is called once per frame
    void Update()
    {

    }

    [ClientRpc]
    public void RpcSetJunkRod(Vector3 currentRod)
    {
        if (hook != null)
        {
            hook.transform.position = currentRod;
            hook.transform.localScale = Vector3.one;
        }
    }

    // Mirror RPC to sync the rod tip to all clients so the LineRenderer draws correctly
    [Command]
    public void CmdSetRodTip(NetworkIdentity rodIdentity)
    {
        RpcSetRodTip(rodIdentity);
    }

    [ClientRpc]
    public void RpcSetRodTip(NetworkIdentity rodIdentity)
    {
        if (hook != null)
        {
            hook.rodTip = rodIdentity != null ? rodIdentity.transform : null;
        }
    }

    [Command]
    public void CmdSpawnAndAttachWorm()
    {
        if (hook != null && hook.wormPrefab != null && hook.wormParent != null)
        {
            GameObject wormInst = Instantiate(hook.wormPrefab, hook.wormParent.position, Quaternion.identity);
            NetworkServer.Spawn(wormInst, connectionToClient);
            RpcAttachWorm(wormInst.GetComponent<NetworkIdentity>());
        }
    }

    [ClientRpc]
    public void RpcAttachWorm(NetworkIdentity wormIdentity)
    {
        if (hook != null && wormIdentity != null)
        {
            hook.wormInstance = wormIdentity.gameObject;
            Transform worm = hook.wormInstance.transform;
            worm.SetParent(hook.wormParent, false);
            worm.localPosition = Vector3.zero;
            worm.localScale = Vector3.one;
            hook.hasWorm = true;
            WormSpawner.Instance.activeWorms.Add(hook.wormInstance);
        }
    }
}
