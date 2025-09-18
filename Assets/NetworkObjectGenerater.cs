using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;

public class NetworkObjectGenerater : NetworkBehaviour
{
    public GameObject MashPhase;

    public Text clientText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(IsServer)
        {
            SpawnWormmashPhase();
                clientText.text  = "ClientId = " +  Owner.ClientId;
        }
    }

    void SpawnWormmashPhase()
    {
        GameObject mashPhase = Instantiate(MashPhase);
        Spawn(mashPhase);
    }
}
