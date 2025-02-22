using CosmicShore.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace CosmicShore
{
    public class Silhouette : MonoBehaviour
    {
        [SerializeField] Vector3 sihouetteScale = Vector3.one;
        [SerializeField] List<GameObject> silhouetteParts = new();

        #region Optional configuration 
        // trails //
        [SerializeField] TrailSpawner trailSpawner;

        // drifting //
        [SerializeField] DriftTrailAction driftTrailAction;

        // jaws //
        [SerializeField] GameObject topJaw;
        [SerializeField] GameObject bottomJaw;
        [SerializeField] int JawResourceIndex;

        [SerializeField] bool swingBlocks;
        #endregion

        IShip _ship;
        GameObject blockPrefab; // Prefab for the blockImages
        GameObject[,] blockPool;
        Game.UI.MiniGameHUD hud;
        GameObject silhouetteContainer;
        Transform trailDisplayContainer;

        int poolSize;
        float _worldToUIScale = 2;
        float _imageScale = .02f;

        private void OnEnable()
        {
            if (driftTrailAction) driftTrailAction.OnChangeDriftAltitude += calculateDriftAngle;
            if (trailSpawner) trailSpawner.OnBlockCreated += HandleBlockCreation;
        }

        private void OnDisable()
        {
            if (driftTrailAction) driftTrailAction.OnChangeDriftAltitude -= calculateDriftAngle;
            if (topJaw && _ship != null) _ship.ResourceSystem.Resources[JawResourceIndex].OnResourceChange -= calculateBlastAngle;
            if (trailSpawner) trailSpawner.OnBlockCreated -= HandleBlockCreation;
        }

        // TODO - Call this method from Ship
        public void Initialize(IShip ship)
        {
            _ship = ship;
            if (!_ship.AIPilot.AutoPilotEnabled && _ship.Player.GameCanvas != null)
            {
                hud = _ship.Player.GameCanvas.MiniGameHUD;
                silhouetteContainer = hud.SetSilhouetteActive(!ship.AIPilot.AutoPilotEnabled && ship.Player.IsActive);
                trailDisplayContainer = hud.SetTrailDisplayActive(!ship.AIPilot.AutoPilotEnabled).transform;
                foreach (var part in silhouetteParts)
                {
                    part.transform.SetParent(silhouetteContainer.transform, false);
                    part.SetActive(true);
                }
            }

            if (topJaw) _ship.ResourceSystem.Resources[JawResourceIndex].OnResourceChange += calculateBlastAngle;
        }

        public void SetBlockPrefab(GameObject block)
        {
            blockPrefab = block;
        }

        float dotProduct = .9999f;
        bool flip;

        private void calculateDriftAngle(float dotProduct)
        {
            ShipStatus status = _ship.ShipStatus;
            flip = Vector3.Dot(transform.up, status.Course) > 0;
            foreach (var part in silhouetteParts) { part.gameObject.SetActive(!_ship.AIPilot.AutoPilotEnabled && _ship.Player.IsActive); } // TODO: why?
            silhouetteContainer.transform.localRotation = Quaternion.Euler(0, 0, (flip ? -1f : 1f) * Mathf.Acos(dotProduct-.0001f) * Mathf.Rad2Deg);

            this.dotProduct = dotProduct;// Acos hates 1
        }

        private void calculateBlastAngle(float currentAmmo)
        {
            foreach (var part in silhouetteParts) { part.gameObject.SetActive(true);}
            topJaw.transform.localRotation = Quaternion.Euler(0, 0, 21 * currentAmmo);
            topJaw.GetComponent<Image>().color = currentAmmo > .98 ? Color.green : Color.white;

            
            bottomJaw.transform.localRotation = Quaternion.Euler(0, 0, -21 * currentAmmo);
            bottomJaw.GetComponent<Image>().color = currentAmmo > .98 ? Color.green : Color.white ;

        }

        private void HandleBlockCreation(float xShift, float wavelength, float scaleX, float scaleY, float scaleZ)
        {
            if (!_ship.AIPilot.AutoPilotEnabled)
            {
                if (poolSize < 1)
                {
                    if (swingBlocks) poolSize = Mathf.CeilToInt(((RectTransform)trailDisplayContainer).rect.width / (trailSpawner.MinWaveLength * _worldToUIScale));
                    else poolSize = Mathf.CeilToInt(((RectTransform)trailDisplayContainer).rect.width / (trailSpawner.MinWaveLength * _worldToUIScale * scaleY));
                    InitializeBlockPool();
                }
                if (swingBlocks) UpdateBlockPool(xShift * (scaleY / 2) * _worldToUIScale, wavelength * _worldToUIScale, scaleX * scaleY * _imageScale, scaleZ * _imageScale);
                else UpdateBlockPool(xShift * _worldToUIScale * scaleY, wavelength * _worldToUIScale * scaleY, scaleX * scaleY * _imageScale, scaleZ * scaleY * _imageScale); // VPS per unit speed is proportional to display area because gap doesn't matter and (x*y) * (z*y) / (wavelength*y) is proportional to volume/wavelength.
            }
        }

        private void InitializeBlockPool()
        {
            blockPool = new GameObject[poolSize, 2]; // Two blocks per column
            for (int i = 0; i < poolSize; i++)
            {
                GameObject tempContainer = new GameObject();
                tempContainer.AddComponent<RectTransform>();
                tempContainer.transform.SetParent(trailDisplayContainer, false);
                for (int j = 0; j < 2; j++)
                {
                    GameObject newBlock = Instantiate(blockPrefab, silhouetteContainer.transform);
                    newBlock.transform.SetParent(tempContainer.transform, false);
                    Debug.Log($"silhouette: {trailSpawner.MinWaveLength}");
                    newBlock.transform.parent.localPosition = new Vector3(-i * trailSpawner.MinWaveLength * _worldToUIScale +
                        (((RectTransform)trailDisplayContainer).rect.width/2), 0, 0);
                    newBlock.transform.localPosition = new Vector3(0, j * 2 * trailSpawner.Gap - trailSpawner.Gap, 0);
                    newBlock.transform.localScale = Vector3.zero;
                    newBlock.SetActive(true);
                    blockPool[i, j] = newBlock;
                }
            }
        }



        private void UpdateBlockPool(float xShift, float wavelength, float scaleX, float scaleZ)
        {
            if (!_ship.AIPilot.AutoPilotEnabled)
            {
                for (int j = 0; j < 2; j++)
                {
                    blockPool[0, j].transform.localScale = new Vector3(scaleZ, j * 2 * scaleX - scaleX, 1);
                    blockPool[0, j].transform.parent.localPosition = new Vector3(((RectTransform)trailDisplayContainer).rect.width / 2, 0, 0);
                    blockPool[0, j].transform.localPosition = new Vector3(0, j * 2 * xShift - xShift, 0);
                }
                if (driftTrailAction)
                {
                    blockPool[0, 0].transform.parent.localRotation = Quaternion.Euler(0, 0, (flip ? -1f : 1f) * Mathf.Acos(dotProduct - .0001f) * Mathf.Rad2Deg);
                }
                for (int i = poolSize - 1; i > 0; i--)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        blockPool[i, j].transform.localScale = blockPool[i - 1, j].transform.localScale;
                        blockPool[i, j].transform.parent.localPosition = new Vector3(-i * wavelength +
                            (((RectTransform)trailDisplayContainer).rect.width / 2), 0, 0);
                        blockPool[i, j].transform.localPosition = blockPool[i - 1, j].transform.localPosition;
                    }

                    bool underCurrentPoolSize = i < Mathf.CeilToInt(((RectTransform)trailDisplayContainer).rect.width / wavelength);
                    blockPool[i, 1].transform.parent.gameObject.SetActive(underCurrentPoolSize);

                    if (driftTrailAction)
                    {
                        if (underCurrentPoolSize) blockPool[i, 0].transform.parent.localRotation = blockPool[i - 1, 0].transform.parent.localRotation;
                    }
                }
            }
        }

        public void Clear()
        {
            foreach (Transform t in trailDisplayContainer.transform) { t.gameObject.SetActive(false); }
            foreach (Transform t in silhouetteContainer.transform) { t.gameObject.SetActive(false); }
        }
    }
}
