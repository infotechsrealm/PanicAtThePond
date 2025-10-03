using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WormSpawner : MonoBehaviourPunCallbacks
{
    public GameObject wormPrefab, goldWormPrefab;
    public float spawnInterval = 3f;
    public float xRange = 8f;
    public float yRange = 4f;

    internal bool canSpawn = true;

    public static WormSpawner instance;


    private List<GameObject> activeWorms = new List<GameObject>(); // track all junks


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

            GameObject worm =  PhotonNetwork.Instantiate("Worm", pos, Quaternion.identity).gameObject;
            if(FishermanController.instance!=null)
            {
                worm.GetComponent<AudioSource>().mute = true;
            }
            activeWorms.Add(worm);

        }

        yield return new WaitForSeconds(Random.Range(3f, 7f));
        StartCoroutine(SpawnWorm());

    }

    void SpawnGoldWorm()
    {
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
        canSpawn = false;

        if (!PhotonNetwork.IsMasterClient)
        {
            // सिर्फ MasterClient ही destroy करेगा
            Debug.LogWarning("⚠️ Only MasterClient can destroy worms!");
            return;
        }

        foreach (GameObject worm in activeWorms)
        {
            if (worm != null)
            {
                PhotonView pv = worm.GetComponent<PhotonView>();
                if (pv != null)
                {
                    PhotonNetwork.Destroy(worm); // ✅ direct object pass करो, दुबारा Find मत करो
                }
            }
        }

        activeWorms.Clear();
    }


    void OnDestroy()
    {
        activeWorms.Remove(this.gameObject);
    }
}
