using CosmicShore.Utility.ClassExtensions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace CosmicShore.Core
{
    public abstract class PoolManagerBase : MonoBehaviour
    {
        [System.Serializable]
        public class Pool
        {
            public GameObject prefab;
            public string tag;
            public int size;

            public Pool(GameObject prefab, int size, string tag = null)
            {
                this.prefab = prefab;
                this.size = size;
                this.tag = string.IsNullOrEmpty(tag) ? prefab.tag : tag;
            }
        }

        [FormerlySerializedAs("pools")]
        [SerializeField] protected List<Pool> _configDatas;
        protected Dictionary<string, Queue<GameObject>> _poolDictionary;

        /// <summary>
        /// When a pool is empty and a spawn is requested we create this many
        /// objects so that the pool always has at least one available item
        /// after the spawn completes.
        /// </summary>
        protected const int _objectsCreatedWhenEmpty = 2;

        #region Initialization

        public virtual void Start()
        {
            InitializePoolDictionary();
        }

        protected virtual void InitializePoolDictionary()
        {
            _poolDictionary = new Dictionary<string, Queue<GameObject>>();

            if (_configDatas == null || _configDatas.Count == 0)
            {
                Debug.LogError("Pool config data is empty!");
                enabled = false;
                return;
            }

            foreach (var config in _configDatas)
            {
                CreateNewPool(config.prefab, config.size, config.tag);
            }
        }

        public virtual void CreatePoolDictionary() => InitializePoolDictionary();

        #endregion

        #region Pool Creation & Expansion

        public virtual void AddConfigData(GameObject prefab, int size, string tag = null)
        {
            _configDatas ??= new List<Pool>();
            _poolDictionary ??= new Dictionary<string, Queue<GameObject>>();

            _configDatas.Add(new Pool(prefab, size, tag));
            CreateNewPool(prefab, size, tag);
        }

        protected virtual void CreateNewPool(GameObject prefab, int size, string tag = null)
        {
            string poolTag = tag ?? prefab.tag;

            if (_poolDictionary.ContainsKey(poolTag))
            {
                Debug.LogWarning($"Pool with tag '{poolTag}' already exists.");
                return;
            }

            Queue<GameObject> objectPool = new Queue<GameObject>();
            _poolDictionary.Add(poolTag, objectPool);

            for (int i = 0; i < size; i++)
            {
                CreatePoolObject(prefab, poolTag);
            }
        }

        protected virtual GameObject CreatePoolObject(GameObject prefab, string tag)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.tag = tag;
            obj.SetActive(false);
            _poolDictionary[tag].Enqueue(obj);
            return obj;
        }

        #endregion

        #region Spawn & Return

        public virtual GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if (!_poolDictionary.ContainsKey(tag))
            {
                DebugExtensions.LogColored($"Pool with tag '{tag}' not found. Creating new pool...", Color.red);

                if (TryGetPrefabByTag(tag, out GameObject prefab))
                {
                    CreateNewPool(prefab, 0, tag);
                }
                else
                {
                    Debug.LogError($"No prefab found with tag '{tag}' to create pool.");
                    return null;
                }
            }

            if (_poolDictionary[tag].Count == 0)
            {
                if (TryGetPrefabByTag(tag, out GameObject prefab))
                {
                    DebugExtensions.LogColored($"Pool '{tag}' is empty. Expanding...", Color.red);
                    for (int i = 0; i < _objectsCreatedWhenEmpty; i++)
                    {
                        CreatePoolObject(prefab, tag);
                    }
                }
            }

            GameObject objToSpawn = _poolDictionary[tag].Dequeue();
            objToSpawn.transform.SetPositionAndRotation(position, rotation);
            objToSpawn.SetActive(true);
            return objToSpawn;
        }

        public virtual void ReturnToPool(GameObject obj, string tag)
        {
            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogError($"Trying to return object to non-existent pool with tag '{tag}'.");
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            _poolDictionary[tag].Enqueue(obj);
        }

        #endregion

        #region Helpers

        protected virtual bool TryGetPrefabByTag(string tag, out GameObject prefab)
        {
            foreach (var config in _configDatas)
            {
                if (config.prefab != null && config.tag == tag)
                {
                    prefab = config.prefab;
                    return true;
                }
            }

            prefab = null;
            return false;
        }

        protected virtual int GetPoolSize(string tag)
        {
            if (_poolDictionary.TryGetValue(tag, out var queue))
            {
                return queue.Count;
            }

            Debug.LogWarning($"Pool size query failed. No pool found with tag '{tag}'.");
            return 0;
        }

        #endregion
    }
}