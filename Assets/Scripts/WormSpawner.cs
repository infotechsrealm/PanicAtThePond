using Photon.Pun;
using System.Collections;
using UnityEngine;

public class WormSpawner : MonoBehaviour
{
    public GameObject wormPrefab, goldWormPrefab;
    public float spawnInterval = 3f;
    public float xRange = 8f;
    public float yRange = 4f;

    internal bool canSpawn = true;

    public static WormSpawner instance;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            LoadSpawnWorm();
            SpawnGoldWorm();
           //Invoke("SpawnGoldWorm",1f);
        }
    }


    public void LoadSpawnWorm()
    {
        StartCoroutine(SpawnWorm());
    }

    IEnumerator SpawnWorm()
    {
        if (canSpawn)
        {
            float x = Random.Range(-xRange, xRange);
            float y = Random.Range(-yRange, 1);
            Vector2 pos = new Vector2(x, y);

            PhotonNetwork.Instantiate("Worm", pos, Quaternion.identity);

        }
        yield return new WaitForSeconds(Random.Range(3f, 7f));
        StartCoroutine(SpawnWorm());

    }

    void SpawnGoldWorm()
    {

        Debug.Log("SpawnGoldWorm called");
        float x = Random.Range(-xRange, xRange);
        float y = Random.Range(-yRange, 1);
        Vector2 pos = new Vector2(x, y);

        PhotonNetwork.Instantiate("GoldWorm", pos, Quaternion.identity);
    }

    public void StopSpawning()
    {
        canSpawn = false;
    }

    public void DestroyAllWorms()
    {
        GameObject[] worms = GameObject.FindGameObjectsWithTag("Worm");
        foreach (GameObject worm in worms)
        {
            Destroy(worm);
        }

        GameObject[] worms2 = GameObject.FindGameObjectsWithTag("Worm2");
        foreach (GameObject worm in worms2)
        {
            Destroy(worm);
        }

        GameObject[] goldTrouts = GameObject.FindGameObjectsWithTag("GoldTrout");
        foreach (GameObject trout in goldTrouts)
        {
            Destroy(trout);
        }

    }
}
