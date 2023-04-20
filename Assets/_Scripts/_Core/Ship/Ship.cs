using System.Collections;
using System.Collections.Generic;
using StarWriter.Core.Input;
using UnityEngine;

namespace StarWriter.Core
{
    [RequireComponent(typeof(ResourceSystem))]
    [RequireComponent(typeof(TrailSpawner))]
    [RequireComponent(typeof(ShipData))]
    public class Ship : MonoBehaviour
    {
        [HideInInspector] public CameraManager cameraManager;
        [HideInInspector] public InputController inputController;
        [HideInInspector] public ResourceSystem ResourceSystem;

        [Header("ship Meta")]
        [SerializeField] string Name;
        [SerializeField] public ShipTypes ShipType;

        [Header("ship Components")]
        [HideInInspector] public TrailSpawner TrailSpawner;
        [SerializeField] Skimmer nearFieldSkimmer;
        [SerializeField] GameObject OrientationHandle;
        [SerializeField] public List<GameObject> shipGeometries;
        [SerializeField] GameObject head;
        ShipController shipController;
        ShipData shipData;

        [Header("optional ship Components")]
        [SerializeField] GameObject AOEPrefab;
        [SerializeField] Skimmer farFieldSkimmer;

        [Header("Environment Interactions")]
        public List<CrystalImpactEffects> crystalImpactEffects;
        [ShowIf(CrystalImpactEffects.AreaOfEffectExplosion)] [SerializeField] float minExplosionScale = 50;
        [ShowIf(CrystalImpactEffects.AreaOfEffectExplosion)] [SerializeField] float maxExplosionScale = 400;

        [SerializeField] List<TrailBlockImpactEffects> trailBlockImpactEffects;
        [SerializeField] float blockChargeChange;

        [Header("Configuration")]
        public float boostMultiplier = 4f; // TODO: Move to ShipController
        public float boostFuelAmount = -.01f; 

        [SerializeField] List<ShipActionAbstractBase> maxSpeedStraightActions;
        [SerializeField] List<ShipActionAbstractBase> minSpeedStraightActions;
        [SerializeField] List<ShipActionAbstractBase> leftStickShipActions;
        [SerializeField] List<ShipActionAbstractBase> rightStickShipActions;
        [SerializeField] List<ShipActionAbstractBase> flipShipActions;
        [SerializeField] List<ShipActionAbstractBase> idleShipActions;
        Dictionary<InputEvents, List<ShipActionAbstractBase>> ShipControlActions;

        [Header("Passive Effects")]
        public List<ShipLevelEffects> LevelEffects;
        
        [ShowIf(ShipLevelEffects.ScaleGap)] [SerializeField] float minGap = 0;
        [ShowIf(ShipLevelEffects.ScaleGap)] [SerializeField] float maxGap = 0;
        [ShowIf(ShipLevelEffects.ScaleSkimmers)] [SerializeField] float minFarFieldSkimmerScale = 100;
        [ShowIf(ShipLevelEffects.ScaleSkimmers)] [SerializeField] float maxFarFieldSkimmerScale = 200;
        [SerializeField] float minNearFieldSkimmerScale = 15;
        [SerializeField] float maxNearFieldSkimmerScale = 100;

        // TODO: move these into GunShipController
        [ShowIf(ShipLevelEffects.ScaleProjectiles)] [SerializeField] float minProjectileScale = 1;
        [ShowIf(ShipLevelEffects.ScaleProjectiles)] [SerializeField] float maxProjectileScale = 10;
        [ShowIf(ShipLevelEffects.ScaleProjectileBlocks)] [SerializeField] Vector3 minProjectileBlockScale = new Vector3(1.5f, 1.5f, 3f);
        [ShowIf(ShipLevelEffects.ScaleProjectileBlocks)] [SerializeField] Vector3 maxProjectileBlockScale = new Vector3(1.5f, 1.5f, 30f);

        public List<ShipControlOverrides> ControlOverrides;
        [SerializeField] float closeCamDistance;
        [SerializeField] float farCamDistance;

        List<ShipSpeedModifier> SpeedModifiers = new();
        Material ShipMaterial;
        Material AOEExplosionMaterial;
        float speedModifierDuration = 2f;
        float speedModifierMax = 6f;
        float abilityStartTime;

        Teams team;
        public Teams Team 
        { 
            get => team; 
            set 
            { 
                team = value;
                if (nearFieldSkimmer != null) nearFieldSkimmer.team = value;
                if (farFieldSkimmer != null) farFieldSkimmer.team = value; 
            }
        }

        Player player;
        public Player Player 
        { 
            get => player;
            set
            {
                player = value;
                if (nearFieldSkimmer != null) nearFieldSkimmer.Player = value;
                if (farFieldSkimmer != null) farFieldSkimmer.Player = value;
            }
        }

        void Awake()
        {
            ResourceSystem = GetComponent<ResourceSystem>();
            shipController = GetComponent<ShipController>();
            TrailSpawner = GetComponent<TrailSpawner>();
            shipData = GetComponent<ShipData>();
        }

        void Start()
        {
            cameraManager = CameraManager.Instance;
            inputController = player.GetComponent<InputController>();
            ApplyShipControlOverrides(ControlOverrides);

            foreach (var shipGeometry in shipGeometries)
                shipGeometry.AddComponent<ShipGeometry>().Ship = this;

            ShipControlActions = new Dictionary<InputEvents, List<ShipActionAbstractBase>> {
                { InputEvents.FullSpeedStraightAction, maxSpeedStraightActions },
                { InputEvents.MinimumSpeedStraightAction, minSpeedStraightActions },
                { InputEvents.LeftStickAction, leftStickShipActions },
                { InputEvents.RightStickAction, rightStickShipActions },
                { InputEvents.FlipAction, flipShipActions },
                { InputEvents.IdleAction, idleShipActions },
            };

            foreach (var key in ShipControlActions.Keys)
                foreach (var shipAction in ShipControlActions[key])
                    shipAction.Ship = this;
        }

        void Update()
        {
            ApplySpeedModifiers();
        }

        void ApplyShipControlOverrides(List<ShipControlOverrides> controlOverrides)
        {
            foreach (ShipControlOverrides effect in controlOverrides)
            {
                switch (effect)
                {
                    case ShipControlOverrides.CloseCam:
                        cameraManager.CloseCamDistance = closeCamDistance;
                        cameraManager.SetCloseCameraDistance(closeCamDistance);
                        break;
                    case ShipControlOverrides.FarCam:
                        cameraManager.FarCamDistance = farCamDistance;
                        cameraManager.SetFarCameraDistance(farCamDistance);
                        break;
                }
            }
        }

        public void PerformCrystalImpactEffects(CrystalProperties crystalProperties)
        {
            if (StatsManager.Instance != null)
                StatsManager.Instance.CrystalCollected(this, crystalProperties);

            foreach (CrystalImpactEffects effect in crystalImpactEffects)
            {
                switch (effect)
                {
                    case CrystalImpactEffects.PlayHaptics:
                        HapticController.PlayCrystalImpactHaptics();
                        break;
                    case CrystalImpactEffects.AreaOfEffectExplosion:
                        var AOEExplosion = Instantiate(AOEPrefab).GetComponent<AOEExplosion>();
                        AOEExplosion.Material = AOEExplosionMaterial;
                        AOEExplosion.Team = team;
                        AOEExplosion.Ship = this;
                        AOEExplosion.SetPositionAndRotation(transform.position, transform.rotation);
                        AOEExplosion.MaxScale =  Mathf.Max(minExplosionScale, ResourceSystem.CurrentAmmo * maxExplosionScale);

                        if (AOEExplosion is AOEBlockCreation aoeBlockcreation)
                            aoeBlockcreation.SetBlockMaterial(TrailSpawner.GetBlockMaterial());
                        if (AOEExplosion is AOEFlowerCreation aoeFlowerCreation)
                        {
                            StartCoroutine(CreateTunnelCoroutine(aoeFlowerCreation, 3));
                        }
                        break;
                    case CrystalImpactEffects.IncrementLevel:
                        IncrementLevel();
                        break;
                    case CrystalImpactEffects.FillCharge:
                        ResourceSystem.ChangeBoostAmount(crystalProperties.fuelAmount);
                        break;
                    case CrystalImpactEffects.Boost:
                        SpeedModifiers.Add(new ShipSpeedModifier(crystalProperties.speedBuffAmount, 4 * speedModifierDuration, 0));
                        break;
                    case CrystalImpactEffects.DrainAmmo:
                        ResourceSystem.ChangeAmmoAmount(-ResourceSystem.CurrentAmmo);
                        break;
                    case CrystalImpactEffects.GainOneThirdMaxAmmo:
                        ResourceSystem.ChangeAmmoAmount(ResourceSystem.MaxAmmo/3f);
                        break;
                    case CrystalImpactEffects.ResetAggression:
                        if (gameObject.TryGetComponent<AIPilot>(out var aiPilot))
                        {
                            aiPilot.lerp = aiPilot.defaultLerp;
                            aiPilot.throttle = aiPilot.defaultThrottle;
                        }
                        break;
                }
            }
        }

        public void PerformTrailBlockImpactEffects(TrailBlockProperties trailBlockProperties)
        {
            foreach (TrailBlockImpactEffects effect in trailBlockImpactEffects)
            {
                switch (effect)
                {
                    case TrailBlockImpactEffects.PlayHaptics:
                        HapticController.PlayBlockCollisionHaptics();
                        break;
                    case TrailBlockImpactEffects.DrainHalfAmmo:
                        ResourceSystem.ChangeAmmoAmount(-ResourceSystem.CurrentAmmo / 2f);
                        break;
                    case TrailBlockImpactEffects.DebuffSpeed:
                        ModifySpeed(trailBlockProperties.speedDebuffAmount, speedModifierDuration);
                        break;
                    case TrailBlockImpactEffects.DeactivateTrailBlock:
                        break;
                    case TrailBlockImpactEffects.ActivateTrailBlock:
                        break;
                    case TrailBlockImpactEffects.OnlyBuffSpeed:
                        if (trailBlockProperties.speedDebuffAmount > 1) SpeedModifiers.Add(new ShipSpeedModifier(trailBlockProperties.speedDebuffAmount, speedModifierDuration, 0));
                        break;
                    case TrailBlockImpactEffects.ChangeBoost:
                        ResourceSystem.ChangeBoostAmount(blockChargeChange);
                        break;
                    case TrailBlockImpactEffects.DecrementLevel:
                        DecrementLevel();
                        break;
                    case TrailBlockImpactEffects.Attach:
                        Attach(trailBlockProperties.trailBlock);
                        shipData.GunsActive = true;
                        break;
                    case TrailBlockImpactEffects.ChangeAmmo:
                        ResourceSystem.ChangeAmmoAmount(blockChargeChange);
                        break;
                }
            }
        }

        public void PerformShipControllerActions(InputEvents controlType)
        {
            abilityStartTime = Time.time;
            var shipControlActions = ShipControlActions[controlType];
            foreach (var action in shipControlActions)
                action.StartAction();
        }

        public void StopShipControllerActions(InputEvents controlType)
        {
            // TODO: p1 ability activation tracking doesn't work - needs to have separate time keeping for each control type
            if (StatsManager.Instance != null)
                StatsManager.Instance.AbilityActivated(Team, player.PlayerName, controlType, Time.time-abilityStartTime);

            var shipControlActions = ShipControlActions[controlType];
            foreach (var action in shipControlActions)
                action.StopAction();
        }

        public void ToggleCollision(bool enabled)
        {
            foreach (var collider in GetComponentsInChildren<Collider>(true))
                collider.enabled = enabled;
        }

        public void SetShipMaterial(Material material)
        {
            ShipMaterial = material;
            ApplyShipMaterial();
        }

        public void SetBlockMaterial(Material material)
        {
            TrailSpawner.SetBlockMaterial(material);
        }

        public void SetAOEExplosionMaterial(Material material)
        {
            AOEExplosionMaterial = material;
        }

        public void FlipShipUpsideDown()
        {
            OrientationHandle.transform.localRotation = Quaternion.Euler(0, 0, 180);
        }
        public void FlipShipRightsideUp()
        {
            OrientationHandle.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        public void Teleport(Transform _transform)
        {
            transform.SetPositionAndRotation(_transform.position, _transform.rotation);
        }

        // TODO: need to be able to disable ship abilities as well for minigames
        public void DisableSkimmer()
        {
            nearFieldSkimmer?.gameObject.SetActive(false);
            farFieldSkimmer?.gameObject.SetActive(false);
        }

        //
        // Speed Modification
        //
        public void ModifySpeed(float amount, float duration)
        {
            SpeedModifiers.Add(new ShipSpeedModifier(amount, duration, 0));
        }

        void ApplySpeedModifiers()
        {
            float accumulatedSpeedModification = 1;
            for (int i = SpeedModifiers.Count - 1; i >= 0; i--)
            {
                var modifier = SpeedModifiers[i];
                modifier.elapsedTime += Time.deltaTime;
                SpeedModifiers[i] = modifier;

                if (modifier.elapsedTime >= modifier.duration)
                    SpeedModifiers.RemoveAt(i);
                else
                    accumulatedSpeedModification *= Mathf.Lerp(modifier.initialValue, 1f, modifier.elapsedTime / modifier.duration);
            }

            accumulatedSpeedModification = Mathf.Min(accumulatedSpeedModification, speedModifierMax);
            shipData.SpeedMultiplier = accumulatedSpeedModification;
        }

        void ApplyShipMaterial()
        {
            if (ShipMaterial == null)
                return;

            foreach (var shipGeometry in shipGeometries)
                shipGeometry.GetComponent<MeshRenderer>().material = ShipMaterial;
        }

        //
        // Attach and Detach
        //
        void Attach(TrailBlock trailBlock) 
        {
            if (trailBlock.Trail != null)
            {
                shipData.Attached = true;
                shipData.AttachedTrailBlock = trailBlock;
            }
        }

        //
        // level up and down
        //
        void UpdateLevel()
        {
            foreach (ShipLevelEffects effect in LevelEffects)
            {
                switch (effect)
                {
                    case ShipLevelEffects.ScaleSkimmers:
                        ScaleSkimmersWithLevel();
                        break;
                    case ShipLevelEffects.ScaleGap:
                        ScaleGapWithLevel();
                        break;
                    case ShipLevelEffects.ScaleProjectiles:
                        ScaleProjectilesWithLevel();
                        break;
                    case ShipLevelEffects.ScaleProjectileBlocks:
                        ScaleProjectileBlocksWithLevel();
                        break;
                }
            }
        }

        void IncrementLevel()
        {
            ResourceSystem.ChangeLevel(ChargeDisplay.OneFuelUnit);
            UpdateLevel();
        }

        void DecrementLevel()
        {
            ResourceSystem.ChangeLevel(-ChargeDisplay.OneFuelUnit);
            UpdateLevel();
        }

        void ScaleSkimmersWithLevel()
        {
            if (nearFieldSkimmer != null)
                nearFieldSkimmer.transform.localScale = Vector3.one * (minNearFieldSkimmerScale + ((ResourceSystem.CurrentLevel / ResourceSystem.MaxLevel) * (maxNearFieldSkimmerScale - minNearFieldSkimmerScale)));
            if (farFieldSkimmer != null)
                farFieldSkimmer.transform.localScale = Vector3.one * (maxFarFieldSkimmerScale - ((ResourceSystem.CurrentLevel / ResourceSystem.MaxLevel) * (maxFarFieldSkimmerScale - minFarFieldSkimmerScale)));
        }
        void ScaleGapWithLevel()
        {
            TrailSpawner.gap = maxGap - ((ResourceSystem.CurrentLevel / ResourceSystem.MaxLevel) * (maxGap - minGap));
        }

        void ScaleProjectilesWithLevel()
        {
            // TODO: 
            if (shipController is GunShipController controller)
                controller.ProjectileScale = minProjectileScale + ((ResourceSystem.CurrentLevel / ResourceSystem.MaxLevel) * (maxProjectileScale - minProjectileScale));
            else
                Debug.LogWarning("Trying to scale projectile of ShipController that is not a GunShipController");
        }

        void ScaleProjectileBlocksWithLevel()
        {
            if (shipController is GunShipController controller)
                controller.BlockScale = minProjectileBlockScale + ((ResourceSystem.CurrentLevel / ResourceSystem.MaxLevel) * (maxProjectileBlockScale - minProjectileBlockScale));
            else
                Debug.LogWarning("Trying to scale projectile block of ShipController that is not a GunShipController");
        }

        IEnumerator CreateTunnelCoroutine(AOEFlowerCreation aoeFlowerCreation, float amount)
        {
            var count = 0f;
            int currentPosition = TrailSpawner.TrailLength - 1;
            while (count < amount)
            { 
                if (currentPosition < TrailSpawner.TrailLength)
                {
                    count++;
                    currentPosition++;
                    aoeFlowerCreation.SetBlockDimensions(TrailSpawner.InnerDimensions);
                    aoeFlowerCreation.SeedBlocks(TrailSpawner.GetLastTwoBlocks());
                }
                yield return null;
            }
        }
    }
}