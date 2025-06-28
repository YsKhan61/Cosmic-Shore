using CosmicShore.Game;
using CosmicShore.Game.IO;
using CosmicShore.Game.Projectiles;
using CosmicShore.Models;
using CosmicShore.Models.Enums;
using CosmicShore.Utilities;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CosmicShore.Core
{
    /// <summary>
    /// Base behaviour for ship implementations.  Logic shared between
    /// <see cref="Ship"/> and <see cref="NetworkShip"/> should live here.
    /// </summary>
    [RequireComponent(typeof(ShipStatus))]
    public abstract class R_ShipBase : NetworkBehaviour, IShip
    {
        public event Action<IShipStatus> OnShipInitialized;

        [Header("Ship Meta")]
        [SerializeField] protected string _name;
        [SerializeField] protected ShipTypes _shipType;

        [Header("Ship Components")]
        [SerializeField] protected Skimmer nearFieldSkimmer;
        [SerializeField] protected GameObject orientationHandle;
        [SerializeField] protected List<GameObject> shipGeometries;

        [Header("Optional Ship Components")]
        [SerializeField] protected GameObject AOEPrefab;
        [SerializeField] protected Skimmer farFieldSkimmer;

        [Header("Configuration")]
        [SerializeField] protected int resourceIndex = 0;
        [SerializeField] protected int ammoResourceIndex = 0;
        [SerializeField] protected int boostResourceIndex = 0;
        [SerializeField] protected float boostMultiplier = 4f;
        [SerializeField] protected bool bottomEdgeButtons = false;
        [SerializeField] protected float Inertia = 70f;


        [Serializable]
        public struct ElementStat
        {
            public string StatName;
            public Element Element;

            public ElementStat(string statName, Element element)
            {
                StatName = statName;
                Element = element;
            }
        }

        [Header("Elemental Stats")]
        [SerializeField] protected List<ElementStat> ElementStats = new();

        [Header("Event Channels")]
        [SerializeField] protected BoolEventChannelSO onBottomEdgeButtonsEnabled;

        [Header("Refactored Components")]
        [SerializeField] protected R_ShipActionHandler actionHandler;
        [SerializeField] protected R_ShipImpactHandler impactHandler;
        [SerializeField] protected R_ShipCustomization customization;

        protected Material shipMaterial;
        protected const float speedModifierDuration = 2f;

        protected IShipStatus _shipStatus;
        public IShipStatus ShipStatus
        {
            get
            {
                _shipStatus ??= GetComponent<ShipStatus>();
                _shipStatus.Name = _name;
                _shipStatus.ShipType = _shipType;
                _shipStatus.BoostMultiplier = boostMultiplier;
                return _shipStatus;
            }
        }

        public Transform Transform => transform;

        public IInputStatus InputStatus => ShipStatus.InputController.InputStatus;

        protected void SetTeamToShipStatusAndSkimmers(Teams team)
        {
            ShipStatus.Team = team;
            if (nearFieldSkimmer != null) nearFieldSkimmer.Team = team;
            if (farFieldSkimmer != null) farFieldSkimmer.Team = team;
        }

        protected void SetPlayerToShipStatusAndSkimmers(IPlayer player)
        {
            ShipStatus.Player = player;
            if (nearFieldSkimmer != null) nearFieldSkimmer.Player = player;
            if (farFieldSkimmer != null) farFieldSkimmer.Player = player;
        }

        protected void InitializeShipGeometries() => ShipHelper.InitializeShipGeometries(this, shipGeometries);

        public abstract void Initialize(IPlayer player);

        public virtual void Teleport(Transform targetTransform) =>
            ShipHelper.Teleport(transform, targetTransform);

        public virtual void SetResourceLevels(ResourceCollection resources) =>
            ShipStatus.ResourceSystem.InitializeElementLevels(resources);

        public virtual void SetShipUp(float angle) =>
            orientationHandle.transform.localRotation = Quaternion.Euler(0, 0, angle);

        public virtual void DisableSkimmer()
        {
            nearFieldSkimmer?.gameObject.SetActive(false);
            farFieldSkimmer?.gameObject.SetActive(false);
        }

        public virtual void SetBoostMultiplier(float multiplier) => boostMultiplier = multiplier;

        public virtual void ToggleGameObject(bool toggle) => gameObject.SetActive(toggle);

        public virtual void SetShipMaterial(Material material)
        {
            shipMaterial = material;
            if (customization != null)
                customization.SetShipMaterial(material);
            else
                ShipHelper.ApplyShipMaterial(shipMaterial, shipGeometries);
        }

        public virtual void SetBlockSilhouettePrefab(GameObject prefab)
        {
            if (customization != null)
                customization.SetBlockSilhouettePrefab(prefab);
            else
                ShipStatus.Silhouette?.SetBlockPrefab(prefab);
        }

        public virtual void SetAOEExplosionMaterial(Material material)
        {
            if (customization != null)
                customization.SetAOEExplosionMaterial(material);
            else
                ShipStatus.AOEExplosionMaterial = material;
        }

        public virtual void SetAOEConicExplosionMaterial(Material material)
        {
            if (customization != null)
                customization.SetAOEConicExplosionMaterial(material);
            else
                ShipStatus.AOEConicExplosionMaterial = material;
        }

        public virtual void SetSkimmerMaterial(Material material)
        {
            if (customization != null)
                customization.SetSkimmerMaterial(material);
            else
                ShipStatus.SkimmerMaterial = material;
        }

        public virtual void AssignCaptain(SO_Captain captain)
        {
            ShipStatus.Captain = captain;
            SetResourceLevels(captain.InitialResourceLevels);
        }

        public virtual void BindElementalFloat(string name, Element element)
        {
            if (ElementStats.TrueForAll(es => es.StatName != name))
                ElementStats.Add(new ElementStat(name, element));
        }

        protected void Attach(TrailBlock trailBlock)
        {
            if (trailBlock && trailBlock.Trail != null)
            {
                ShipStatus.Attached = true;
                ShipStatus.AttachedTrailBlock = trailBlock;
            }
        }

        protected void InvokeShipInitializedEvent() => OnShipInitialized?.Invoke(ShipStatus);

        public void PerformShipControllerActions(InputEvents controlType)
        {
            actionHandler.PerformShipControllerActions(controlType);
        }

        public void StopShipControllerActions(InputEvents controlType)
        {
            actionHandler.StopShipControllerActions(controlType);
        }

        public void PerformCrystalImpactEffects(CrystalProperties crystalProperties) =>
            impactHandler.PerformCrystalImpactEffects(crystalProperties);

        public void PerformTrailBlockImpactEffects(TrailBlockProperties trailBlockProperties) =>
            impactHandler.PerformTrailBlockImpactEffects(trailBlockProperties);
    }

    [Serializable]
    public struct InputEventShipActionMapping
    {
        public InputEvents InputEvent;
        public List<ShipAction> ShipActions;
    }

    [Serializable]
    public struct ResourceEventShipActionMapping
    {
        public ResourceEvents ResourceEvent;
        public List<ShipAction> ClassActions;
    }
}
