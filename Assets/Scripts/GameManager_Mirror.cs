using Mirror;
using UnityEngine;

public class GameManager_Mirror : NetworkBehaviour
{
    public static GameManager_Mirror Instance;
    public GameObject fishermanPrefab;

    void Awake()
    {
        Instance = this;
    }

    // 🔹 Universal function — client ya host dono call kar sakte hain
    public void RequestSpawnFisherman()
    {
        if (isLocalPlayer)
        {
            CmdSpawnFishermanOnServer();
        }
    }

    [Command]
    void CmdSpawnFishermanOnServer()
    {
        Vector3 spawnPos = new Vector3(Random.Range(-5, 5), Random.Range(-3, 3), 0);
        GameObject fish = Instantiate(fishermanPrefab, spawnPos, Quaternion.identity);
        NetworkServer.Spawn(fish, connectionToClient); // 🔹 gives authority to caller client
    }



}
