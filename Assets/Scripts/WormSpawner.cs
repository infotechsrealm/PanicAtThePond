using Mirror;
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

    public static WormSpawner Instance;


    internal List<GameObject> activeWorms = new List<GameObject>(); // track all junks


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        if (GS.Instance.isLan)
        {
            if(GS.Instance.IsMirrorMasterClient)
            {
                LoadSpawnWorm();
                Invoke(nameof(SpawnGoldWorm), Random.Range(5, 10));

            }
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                LoadSpawnWorm();
                Invoke(nameof(SpawnGoldWorm), Random.Range(5, 10));
            }
        }
        canSpawn = true;
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

                if (GS.Instance.isLan)
                {
                    GameObject worm = Instantiate(wormPrefab, pos, Quaternion.identity);
                    NetworkServer.Spawn(worm);
                    activeWorms.Add(worm);
                }
                else
                {
                    GameObject worm = PhotonNetwork.Instantiate(wormPrefab.name, pos, Quaternion.identity).gameObject;
                    if (FishermanController.Instance != null)
                    {
                        worm.GetComponent<AudioSource>().mute = true;
                    }
                    activeWorms.Add(worm);
                }
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

        if (GS.Instance.isLan)
        {
            GameObject goldfish = Instantiate(goldFish, pos, Quaternion.identity);
            NetworkServer.Spawn(goldfish);
        }
        else
        {
            PhotonNetwork.Instantiate(goldFish.name, pos, Quaternion.identity);
        }
    }

    public void StopSpawning()
    {
        canSpawn = false;
    }

    public void DestroyAllWorms()
    {
        canSpawn = false;

        if (GS.Instance.isLan)
        {
            foreach (GameObject worm in activeWorms)
            {
                if (worm != null)
                {
                    WormSpawner_Mirror.Instance.DestroyWorm_Mirror(worm);
                }
            }
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                foreach (GameObject worm in activeWorms)
                {
                    if (worm != null)
                    {
                        PhotonView pv = worm.GetComponent<PhotonView>();
                        if (pv != null)
                        {
                            PhotonNetwork.Destroy(worm);
                        }
                    }
                }

                activeWorms.Clear();
                Debug.LogWarning("⚠️ Only MasterClient can destroy worms!");
            }
        }
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
