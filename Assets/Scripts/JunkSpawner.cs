using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JunkSpawner : MonoBehaviour
{
    public GameObject[] junkPrefabs; 
    public float minSpawnDelay = 8f;   // min wait
    public float maxSpawnDelay = 12f;   // max wait
    public float xRange = 8f;
    public float yRange = 4f;

    internal bool canSpawn = false;
    public static JunkSpawner instance;

    private List<GameObject> activeJunks = new List<GameObject>(); // track all junks

    public Transform posRefrence;

   

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {

    }

    public void LoadSpawnJunk()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(SpawnJunk());
        }
    }
    IEnumerator SpawnJunk()
    {
        if (canSpawn)
        {
            for (int i = 0; i < activeJunks.Count; i++)
            {
                if (activeJunks[i] == null)
                {
                    activeJunks.Remove(activeJunks[i]);
                }
            }
            if (activeJunks.Count < 2)
            {
                float x = Random.Range(-xRange, xRange);
                float y = yRange;
                Vector2 pos = new Vector2(x, posRefrence.position.y);

                GameObject prefab = junkPrefabs[Random.Range(0, junkPrefabs.Length)];

                GameObject newJunk = PhotonNetwork.Instantiate(prefab.name, pos, Quaternion.identity);


                activeJunks.Add(newJunk);
            }
        }
        float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
        yield return new WaitForSeconds(delay);
        StartCoroutine(SpawnJunk());
    }

    public void StopSpawning()  
    {
        canSpawn = false;
    }

    public void DestroyAllJunks()
    {
        foreach (GameObject junk in activeJunks)
        {
            if (junk != null) Destroy(junk);
        }
        activeJunks.Clear();
    }

    void OnDestroy()
    {
        activeJunks.Remove(this.gameObject);
    }
}




