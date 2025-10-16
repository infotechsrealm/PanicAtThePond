using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WormSpawner : MonoBehaviourPunCallbacks
{
    public GameObject wormPrefab, goldFish;
    public float spawnInterval = 3f;
    public float xRange = 8f;
    public float yRange = 4f;

    internal bool canSpawn = true;

    public static WormSpawner instance;


    internal List<GameObject> activeWorms = new List<GameObject>(); // track all junks


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
            Invoke(nameof(SpawnGoldWorm),Random.Range(5,10));
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
            for (int i = 0; i < activeWorms.Count; i++)
            {
                if (activeWorms[i] == null)
                {
                    activeWorms.Remove(activeWorms[i]);
                }
            }
            if (activeWorms.Count < 5)
            {
                float x = Random.Range(-xRange, xRange);
                float y = Random.Range(-yRange, 1);
                Vector2 pos = new Vector2(x, y);

                GameObject worm = PhotonNetwork.Instantiate(wormPrefab.name, pos, Quaternion.identity).gameObject;
                if (FishermanController.Instence != null)
                {
                    worm.GetComponent<AudioSource>().mute = true;
                }
                activeWorms.Add(worm);
            }
        }

        yield return new WaitForSeconds(Random.Range(5f, 10f));
        StartCoroutine(SpawnWorm());

    }

    void SpawnGoldWorm()
    {
        int r = Random.Range(0, 2);
        float x = r == 0 ? -10f : 10f;

        float y = Random.Range(-yRange, 1);
        Vector2 pos = new Vector2(x, y);

        PhotonNetwork.Instantiate(goldFish.name, pos, Quaternion.identity);
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


    public void EnableWormDaceAnimation()
    {
        for (int i = 0; i < activeWorms.Count; i++)
        {
            if (activeWorms[i] != null)
            {
                activeWorms[i].GetComponent<WormManager>().OnDanceAnimation();
            }
        }
    }
}
