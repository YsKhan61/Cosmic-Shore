using CosmicShore.Game;
using CosmicShore.Game.IO;
using CosmicShore.Game.Projectiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicShore.Core
{
    public class Skimmer : ElementalShipComponent
    {
        [SerializeField] List<TrailBlockImpactEffects> blockImpactEffects;
        [SerializeField] List<SkimmerStayEffects> blockStayEffects;
        [SerializeField] List<ShipImpactEffects> shipImpactEffects;
        [SerializeField] float particleDurationAtSpeedOne = 300f;
        [SerializeField] bool affectSelf = true;
        [SerializeField] float chargeAmount;
        [SerializeField] float MultiSkimMultiplier = 0f;
        [SerializeField] bool visible;
        [SerializeField] ElementalFloat Scale = new ElementalFloat(1);
        public IShip Ship { get; set; }

        public IPlayer Player { get; set; }
        public Teams Team { get; set; }

        float appliedScale;
        ResourceSystem resourceSystem;

        Dictionary<string, float> skimStartTimes = new();
        CameraManager cameraManager;

        public int activelySkimmingBlockCount = 0;
        public int ActivelySkimmingBlockCount { get { return activelySkimmingBlockCount; } }

        [Header("Optional Skimmer Components")]
        [SerializeField] GameObject AOEPrefab;
        [SerializeField] float AOEPeriod;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private GameObject markerPrefab;

        [SerializeField] int resourceIndex = 0;

        float minMatureBlockDistance = Mathf.Infinity;
        TrailBlock minMatureBlock;
        float fuel = 0;

        float distanceWeight;
        float directionWeight;

        float sweetSpot;
        float FWHM;
        float sigma;
        float radius;

        float initialGap;

        public void Initialize(IShip ship)
        {
            Ship = ship;
            if (Ship != null)
            {
                Player = Ship.Player;
                Team = Ship.Team;
                BindElementalFloats(Ship);
                resourceSystem = Ship.ResourceSystem;
                if (visible)
                    GetComponent<MeshRenderer>().material = new Material(Ship.SkimmerMaterial);

                initialGap = Ship.TrailSpawner.Gap;
            }
        }

        void Start()
        {
            cameraManager = CameraManager.Instance;

            sweetSpot = transform.localScale.x / 4;
            FWHM = transform.localScale.x / 4; //Full Width at Half Max
            sigma = FWHM / 2.355f;
            radius = transform.localScale.x / 2;
            
            if (appliedScale != Scale.Value)
            {
                appliedScale = Scale.Value;
                transform.localScale = Vector3.one * appliedScale;
            }
        }

        void Update()
        {
            if (appliedScale != Scale.Value)
            {
                appliedScale = Scale.Value;
                transform.localScale = Vector3.one * appliedScale;
            }
        }

        // TODO: p1- review -- Maja added this to try and enable shark skimmer smashing
        void PerformBlockImpactEffects(TrailBlockProperties trailBlockProperties)
        {
            foreach (TrailBlockImpactEffects effect in blockImpactEffects)
            {
                switch (effect)
                {
                    case TrailBlockImpactEffects.PlayHaptics:
                        if (!Ship.ShipStatus.AutoPilotEnabled) HapticController.PlayHaptic(HapticType.BlockCollision);//.PlayBlockCollisionHaptics();
                        break;
                    case TrailBlockImpactEffects.DeactivateTrailBlock:
                        trailBlockProperties.trailBlock.Damage(Ship.ShipStatus.Course * Ship.ShipStatus.Speed * Ship.GetInertia, Team, Player.PlayerName);
                        break;
                    case TrailBlockImpactEffects.Steal:
                        //Debug.Log($"steal: playername {Player.PlayerName} team: {team}");
                        trailBlockProperties.trailBlock.Steal(Player, Team);
                        break;
                    case TrailBlockImpactEffects.GainResourceByVolume:
                        resourceSystem.ChangeResourceAmount(resourceIndex, (chargeAmount * trailBlockProperties.volume) + (activelySkimmingBlockCount * MultiSkimMultiplier));
                        break;
                    case TrailBlockImpactEffects.GainResource:
                        resourceSystem.ChangeResourceAmount(resourceIndex, chargeAmount + (activelySkimmingBlockCount * MultiSkimMultiplier));
                        break;
                    case TrailBlockImpactEffects.FX:
                        StartCoroutine(DisplaySkimParticleEffectCoroutine(trailBlockProperties.trailBlock));
                        break;
                        // This is actually redundant with Skimmer's built in "Fuel Amount" variable
                        //case TrailBlockImpactEffects.ChangeFuel:
                        //FuelSystem.ChangeFuelAmount(ship.blockFuelChange);
                        //break;
                }
            }
        }

        void PerformShipImpactEffects(ShipGeometry shipGeometry)
        {
            if (StatsManager.Instance != null)
                StatsManager.Instance.SkimmerShipCollision(Ship, shipGeometry.Ship);
            foreach (ShipImpactEffects effect in shipImpactEffects)
            {
                switch (effect)
                {
                    case ShipImpactEffects.TrailSpawnerCooldown:
                        shipGeometry.Ship.TrailSpawner.PauseTrailSpawner();
                        shipGeometry.Ship.TrailSpawner.RestartTrailSpawnerAfterDelay(10);
                        break;
                    case ShipImpactEffects.PlayHaptics:
                        if (!Ship.ShipStatus.AutoPilotEnabled) HapticController.PlayHaptic(HapticType.ShipCollision);//.PlayShipCollisionHaptics();
                        break;
                    case ShipImpactEffects.AreaOfEffectExplosion:
                        if (onCoolDown || shipGeometry.Ship.Team == Team) break;

                        var AOEExplosion = Instantiate(AOEPrefab).GetComponent<AOEExplosion>();
                        AOEExplosion.Ship = Ship;
                        AOEExplosion.SetPositionAndRotation(transform.position, transform.rotation);
                        AOEExplosion.MaxScale = Ship.ShipStatus.Speed - shipGeometry.Ship.ShipStatus.Speed;
                        StartCoroutine(CooldownCoroutine(AOEPeriod));
                        break;
                }
            }
        }


        bool onCoolDown = false;
        IEnumerator CooldownCoroutine(float Period)
        {
            onCoolDown = true;
            yield return new WaitForSeconds(Period);
            onCoolDown = false;
        }

        void PerformBlockStayEffects(float combinedWeight)
        {
            foreach (SkimmerStayEffects effect in blockStayEffects)
            {
                switch (effect)
                {
                    case SkimmerStayEffects.ChangeResource:
                        resourceSystem.ChangeResourceAmount(resourceIndex, fuel);
                        break;
                    case SkimmerStayEffects.Boost:
                        Boost(combinedWeight);
                        break;
                    case SkimmerStayEffects.ScaleTrailAndCamera:
                        ScaleTrailAndCamera();
                        break;
                    case SkimmerStayEffects.VizualizeDistance:
                        VizualizeDistance(combinedWeight);
                        break;
                    case SkimmerStayEffects.ScaleHapticWithDistance:
                        ScaleHapticWithDistance(combinedWeight);
                        break;
                    case SkimmerStayEffects.ScalePitchAndYaw:
                        ScalePitchAndYaw(combinedWeight);
                        break;
                    case SkimmerStayEffects.Align:
                        if (Ship.ShipStatus.AlignmentEnabled) AlignAndNudge(combinedWeight);
                        break;
                    case SkimmerStayEffects.ScaleGap:
                        ScaleGap(combinedWeight);
                        break;
                }
            }
        }

        void StartSkim(TrailBlock trailBlock)
        {
            if (trailBlock == null) return;

            if (skimStartTimes.ContainsKey(trailBlock.ID)) return;
            activelySkimmingBlockCount++;
            skimStartTimes.Add(trailBlock.ID, Time.time);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<ShipGeometry>(out var shipGeometry))
            {
                PerformShipImpactEffects(shipGeometry);
            }

            if (other.TryGetComponent<TrailBlock>(out var trailBlock) && (affectSelf || trailBlock.Team != Team))
            {
                StartSkim(trailBlock);
                PerformBlockImpactEffects(trailBlock.TrailBlockProperties);
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (Ship == null) return;
            if (Ship.Player == null) return;
            if (!other.TryGetComponent<TrailBlock>(out var trailBlock)) return;
            if (trailBlock.Team == Team && !affectSelf) return;

            float skimDecayDuration = 1;
            
            // Occasionally, seeing a KeyNotFoundException, so maybe we miss the OnTriggerEnter event (note: always seems to be for AOE blocks)
            if(!skimStartTimes.ContainsKey(trailBlock.ID))   
                StartSkim(trailBlock);

            float distance = Vector3.Distance(transform.position, other.transform.position);

            if (trailBlock.ownerId != Ship.Player.PlayerUUID || Time.time - trailBlock.TrailBlockProperties.TimeCreated > 7)
            {
                minMatureBlockDistance = Mathf.Min(minMatureBlockDistance, distance);
                if (distance == minMatureBlockDistance) minMatureBlock = trailBlock;
            }

            if (!trailBlock.GetComponent<LineRenderer>() && Ship.ShipStatus.AlignmentEnabled
                && Ship.Player.IsActive) // TODO: ditch line renderer
            {
                CreateLineRendererAroundBlock(trailBlock);

                var lineRenderer = trailBlock.GetComponent<LineRenderer>();
                //AdjustOpacity(lineRenderer, distance);
            }

            foreach (Transform child in trailBlock.transform)
            {
                if (child.gameObject.CompareTag("Shard")) // Make sure to tag your marker prefabs
                {
                    AdjustOpacity(child.gameObject, distance);
                }
            }

            // start with a baseline fuel amount the ranges from 0-1 depending on proximity of the skimmer to the trail block
            fuel = chargeAmount * (1 - (distance / transform.localScale.x)); // x is arbitrary, just need radius of skimmer

            // apply decay
            fuel *= Mathf.Min(0, (skimDecayDuration - (Time.time - skimStartTimes[trailBlock.ID])) / skimDecayDuration);

            // apply multiskim multiplier
            fuel += (activelySkimmingBlockCount * MultiSkimMultiplier);
        }

        private void FixedUpdate()
        {
            if (minMatureBlock)
            {
                distanceWeight = ComputeGaussian(minMatureBlockDistance, sweetSpot, sigma);
                directionWeight = Vector3.Dot(Ship.Transform.forward, minMatureBlock.transform.forward);
                var combinedWeight = distanceWeight * Mathf.Abs(directionWeight);
                PerformBlockStayEffects(combinedWeight);
            }
            minMatureBlock = null;
            minMatureBlockDistance = Mathf.Infinity;
            fuel = 0;
        }

        void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<TrailBlock>(out var trailBlock) && (affectSelf || trailBlock.Team != Team))
            {
                if (skimStartTimes.ContainsKey(trailBlock.ID))
                {
                    skimStartTimes.Remove(trailBlock.ID);
                    activelySkimmingBlockCount--;
                }
                
            

                if (trailBlock.TryGetComponent<LineRenderer>(out var lineRenderer))
                {
                    Destroy(lineRenderer);
                }

                foreach (Transform child in trailBlock.transform)
                {
                    if (child.gameObject.CompareTag("Shard")) // Make sure to tag your marker prefabs
                    {
                        Destroy(child.gameObject);
                    }
                }

            }
        }

        void ScaleTrailAndCamera()
        {
            var normalizedDistance = Mathf.Clamp(Mathf.InverseLerp(15f, radius, minMatureBlockDistance), 0,1);

            Ship.TrailSpawner.SetNormalizedXScale(normalizedDistance);

            if (cameraManager != null && !Ship.ShipStatus.AutoPilotEnabled) 
                cameraManager.SetNormalizedCloseCameraDistance(normalizedDistance);
        }

        void ScaleGap(float combinedWeight)
        {
            Ship.TrailSpawner.Gap = Mathf.Lerp(initialGap, Ship.TrailSpawner.MinimumGap, combinedWeight);
        }

        void AlignAndNudge(float combinedWeight)  // unused but ready to put in the flip phone effect for squirrel
        {
            //align
            Ship.ShipTransformer.GentleSpinShip(minMatureBlock.transform.forward * directionWeight, Ship.Transform.up, combinedWeight * Time.deltaTime);

            //nudge
            if (minMatureBlockDistance < sweetSpot)
                Ship.ShipTransformer.ModifyVelocity(-(minMatureBlock.transform.position - transform.position).normalized * distanceWeight * Mathf.Abs(directionWeight), Time.deltaTime * 10);
            else Ship.ShipTransformer.ModifyVelocity((minMatureBlock.transform.position - transform.position).normalized * distanceWeight * Mathf.Abs(directionWeight), Time.deltaTime * 10);
        }

        void VizualizeDistance(float combinedWeight)
        {
            Ship.ResourceSystem.ChangeResourceAmount(resourceIndex, - Ship.ResourceSystem.Resources[resourceIndex].CurrentAmount);
            Ship.ResourceSystem.ChangeResourceAmount(resourceIndex, combinedWeight);
        }

        void ScalePitchAndYaw(float combinedWeight)
        {
            Ship.ShipTransformer.PitchScaler = Ship.ShipTransformer.YawScaler = 40 * (1 + (.5f*combinedWeight));
        }

        void ScaleHapticWithDistance(float combinedWeight)
        {
            if (!Ship.ShipStatus.AutoPilotEnabled) HapticController.PlayConstant(combinedWeight/3, combinedWeight/3, Time.deltaTime);
        }

        void Boost(float combinedWeight)
        {
            Ship.ShipStatus.Boosting = true;
            Ship.SetBoostMultiplier(1 + (3 * combinedWeight));
        }

        // Function to compute the Gaussian value at a given x
        public static float ComputeGaussian(float x, float b, float c)
        {
            return Mathf.Exp(-Mathf.Pow(x - b, 2) / (2 * c * c));
        }

        IEnumerator DisplaySkimParticleEffectCoroutine(TrailBlock trailBlock)
        {
            if(trailBlock == null) yield break;
            var particle = Instantiate(trailBlock.ParticleEffect);
            particle.transform.parent = trailBlock.transform;

            int timer = 0;
            float scaledTime;
            do
            {
                var distance = trailBlock.transform.position - transform.position;
                scaledTime = particleDurationAtSpeedOne / Ship.ShipStatus.Speed; // TODO: divide by zero possible
                particle.transform.localScale = new Vector3(1, 1, distance.magnitude);
                particle.transform.SetPositionAndRotation(transform.position, Quaternion.LookRotation(distance, trailBlock.transform.up));
                timer++;

                yield return null;
            } 
            while (timer < scaledTime);

            Destroy(particle);
        }

        private void CreateLineRendererAroundBlock(TrailBlock trailBlock)
        {
            var lineRenderer = trailBlock.gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(lineMaterial);
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.useWorldSpace = true;

            DrawCircle(lineRenderer, trailBlock.transform, sweetSpot); // radius can be adjusted
        }

        private void DrawCircle(LineRenderer lineRenderer, Transform blockTransform, float radius)
        {
            int segments = 20;
            //lineRenderer.positionCount = segments + 1;

            Vector3 forward = blockTransform.forward;
            Vector3 up = blockTransform.up;
            Vector3 right = blockTransform.right;

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 localPosition = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * up) * radius;
                Vector3 worldPosition = blockTransform.position + localPosition;
                //lineRenderer.SetPosition(i, worldPosition);

                GameObject marker = Instantiate(markerPrefab, worldPosition, Quaternion.LookRotation(forward, localPosition));
                marker.transform.parent = blockTransform; // Optional: Make the marker a child of the block
            }
        }

        private void AdjustOpacity(LineRenderer lineRenderer, float distance)
        {
            float opacity = .01f - .01f*(distance / radius); // Closer blocks are less transparent
            lineRenderer.material.SetFloat("_Opacity", opacity);
        }
        private void AdjustOpacity(GameObject marker, float distance)
        {
            float opacity = .1f - .1f*(distance / radius); // Closer blocks are less transparent
            marker.GetComponent<MeshRenderer>().material.SetFloat("_Opacity", opacity);
        }


    }
}