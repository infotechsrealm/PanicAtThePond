using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JunkSpawner : MonoBehaviour
{
    public GameObject[] junkPrefabs; 
    public float minSpawnDelay = 8f;   // min wait
    public float maxSpawnDelay = 12f;   // max wait
    internal float xRange = -9f;

    internal bool canSpawn = false;
    public static JunkSpawner instance;

    private List<GameObject> activeJunks = new List<GameObject>(); // track all junks

    public Transform posRefrence;
    public float moveSpeed = 3f;



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
                int randomNo = Random.Range(0, 2);
                float x = randomNo == 0 ? xRange : -xRange;

                Vector2 pos = new Vector2(x, posRefrence.position.y);

                GameObject prefab = junkPrefabs[Random.Range(0, junkPrefabs.Length)];

                GameObject newJunk = PhotonNetwork.Instantiate(prefab.name, pos, Quaternion.identity);

                float randomXForce = Random.Range(0f, 2f);
                randomXForce = randomNo == 0 ? randomXForce : -randomXForce;
                Vector2 force = new Vector2(randomXForce, 0) * moveSpeed;

                Rigidbody2D rb = newJunk.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = force; // give motion in x
                }

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




