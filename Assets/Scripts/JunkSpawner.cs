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

    internal bool canSpawn = true;
    public static JunkSpawner instance;

    private List<GameObject> activeJunks = new List<GameObject>(); // track all junks

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
            StartCoroutine(SpawnLoop());
        }
    }

    IEnumerator SpawnLoop()
    {
        while (canSpawn)
        {
            if (activeJunks.Count < 2)
            {
                float x = Random.Range(-xRange, xRange);
                float y = yRange;
                Vector2 pos = new Vector2(x, y);

                GameObject prefab = junkPrefabs[Random.Range(0, junkPrefabs.Length)];
                GameObject newJunk = Instantiate(prefab, pos, Quaternion.identity);

                activeJunks.Add(newJunk);

                Junk junkScript = newJunk.AddComponent<Junk>();
                junkScript.onDestroyed = () => { activeJunks.Remove(newJunk); };
            }

            float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(delay);
        }
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
}

// Helper script
public class Junk : MonoBehaviour
{
    public System.Action onDestroyed;

    void OnDestroy()
    {
        if (onDestroyed != null)
        {
            onDestroyed.Invoke();
        }
    }
}
