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
}
