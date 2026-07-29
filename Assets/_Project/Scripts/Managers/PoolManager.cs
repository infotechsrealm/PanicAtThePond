using System.Collections.Generic;
using UnityEngine;
using PanicAtThePond.Interfaces;

namespace PanicAtThePond.Managers
{
    /// <summary>
    /// Single authority for pooled object lifecycle. Callers use <see cref="Get"/> and
    /// <see cref="Release"/> instead of <c>Instantiate</c> / <c>Destroy</c>.
    ///
    /// NOTE: this pool is for LOCAL, non-networked objects only. The spawns currently in this project
    /// go through <c>PhotonNetwork.Instantiate</c> and <c>NetworkServer.Spawn</c>, which own their own
    /// object lifecycle and view/net IDs — pooling those requires a networking redesign and is
    /// deliberately out of scope here.
    /// </summary>
    [DisallowMultipleComponent]
    public class PoolManager : MonoBehaviour
    {
        private const int DEFAULT_PREWARM_COUNT = 0;

        [SerializeField] private bool _keepAliveAcrossScenes = true;
        [SerializeField] private Transform _poolRoot;

        private readonly Dictionary<GameObject, Queue<GameObject>> _available = new Dictionary<GameObject, Queue<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new Dictionary<GameObject, GameObject>();

        /// <summary>Singleton access point. Null until the manager's <c>Awake</c> has run.</summary>
        public static PoolManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_poolRoot == null)
            {
                _poolRoot = transform;
            }

            if (_keepAliveAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            Cleanup();
        }

        /// <summary>
        /// Creates <paramref name="count"/> inactive instances of <paramref name="prefab"/> up front so
        /// the first <see cref="Get"/> calls do not allocate.
        /// </summary>
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0)
            {
                return;
            }

            Queue<GameObject> queue = GetQueue(prefab);
            for (int i = 0; i < count; i++)
            {
                GameObject instance = CreateInstance(prefab);
                instance.SetActive(false);
                queue.Enqueue(instance);
            }
        }

        /// <summary>
        /// Returns a live instance of <paramref name="prefab"/>, reusing a pooled one when available.
        /// Any <see cref="IPoolable"/> components on it receive <c>OnSpawn</c>.
        /// </summary>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogError("[PoolManager] Get called with a null prefab.");
                return null;
            }

            Queue<GameObject> queue = GetQueue(prefab);
            GameObject instance = null;

            while (queue.Count > 0 && instance == null)
            {
                instance = queue.Dequeue(); // may be null if it was destroyed by a scene unload
            }

            if (instance == null)
            {
                instance = CreateInstance(prefab);
            }

            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);

            NotifySpawned(instance);
            return instance;
        }

        /// <summary>
        /// Deactivates <paramref name="instance"/> and returns it to its pool. Instances not created by
        /// this manager are destroyed instead, so a stray call cannot corrupt the pool.
        /// </summary>
        public void Release(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!_instanceToPrefab.TryGetValue(instance, out GameObject prefab))
            {
                Debug.LogWarning($"[PoolManager] '{instance.name}' was not created by this pool — destroying it instead.");
                Destroy(instance);
                return;
            }

            NotifyDespawned(instance);

            instance.SetActive(false);
            instance.transform.SetParent(_poolRoot, false);
            GetQueue(prefab).Enqueue(instance);
        }

        /// <summary>Destroys every pooled instance and clears all bookkeeping.</summary>
        public void Cleanup()
        {
            foreach (KeyValuePair<GameObject, Queue<GameObject>> pair in _available)
            {
                while (pair.Value.Count > 0)
                {
                    GameObject instance = pair.Value.Dequeue();
                    if (instance != null)
                    {
                        Destroy(instance);
                    }
                }
            }

            _available.Clear();
            _instanceToPrefab.Clear();
        }

        private Queue<GameObject> GetQueue(GameObject prefab)
        {
            if (!_available.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                _available.Add(prefab, queue);
            }

            return queue;
        }

        private GameObject CreateInstance(GameObject prefab)
        {
            GameObject instance = Instantiate(prefab, _poolRoot);
            _instanceToPrefab[instance] = prefab;
            return instance;
        }

        private static void NotifySpawned(GameObject instance)
        {
            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnSpawn();
            }
        }

        private static void NotifyDespawned(GameObject instance)
        {
            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnDespawn();
            }
        }
    }
}
