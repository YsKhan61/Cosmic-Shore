﻿using StarWriter.Core.HangerBuilder;
using System.Collections;
using UnityEngine;

namespace StarWriter.Core
{
    public class Trail : MonoBehaviour
    {
        [SerializeField] GameObject FossilBlock;
        [SerializeField] GameObject ParticleEffect;
        [SerializeField] Material material;
        [SerializeField] TrailBlockProperties trailBlockProperties;

        public string ownerId;  // TODO: is the ownerId the player name? I hope it is.
        public float waitTime = .6f;
        public bool embiggen;
        public bool destroyed = false;
        public float MaxSize = 1f;
        public string ID;
        public Vector3 Dimensions;

        public bool warp = false;
        GameObject shards;

        public delegate void OnCollisionIncreaseScore(string uuid, float amount);
        public static event OnCollisionIncreaseScore AddToScore;

        private int scoreChange = 1;
        private static GameObject fossilBlockContainer;
        private MeshRenderer meshRenderer;
        private BoxCollider blockCollider;
        Teams team;
        public Teams Team { get => team; set => team = value; }
        string playerName;
        public string PlayerName { get => playerName; set => playerName = value; }

        void Start()
        {
            if (warp) shards = GameObject.FindGameObjectWithTag("field");

            if (fossilBlockContainer == null)
            {
                fossilBlockContainer = new GameObject();
                fossilBlockContainer.name = "FossilBlockContainer";
            }

            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.enabled = false;

            blockCollider = GetComponent<BoxCollider>();
            blockCollider.enabled = false;

            trailBlockProperties.volume = MaxSize * Dimensions.x * Dimensions.y * Dimensions.z;
            trailBlockProperties.position = transform.position;
            trailBlockProperties.trail = this;

            StartCoroutine(CreateBlockCoroutine(MaxSize));
        }

        IEnumerator CreateBlockCoroutine(float MaxSize)
        {
            var DefaultTransformScale = transform.localScale;
            var size = 0.01f;

            if (warp) DefaultTransformScale *= shards.GetComponent<WarpFieldData>().HybridVector(transform).magnitude;

            yield return new WaitForSeconds(waitTime);

            transform.localScale = DefaultTransformScale * size;
            meshRenderer.enabled = true;
            blockCollider.enabled = true;

            while (size < MaxSize)
            {
                transform.localScale = DefaultTransformScale * size;
                size += .5f * Time.deltaTime;
                
                yield return null;
            }

            // Add block to team score when created
            if (StatsManager.Instance != null)
                StatsManager.Instance.BlockCreated(team, playerName, trailBlockProperties);

            if (NodeControlManager.Instance != null)
                NodeControlManager.Instance.AddBlock(team, playerName, trailBlockProperties);
        }

        public void DisplaySkimParticleEffect(Transform skimmer)
        {
            StartCoroutine(DisplaySkimParticleEffectCoroutine(skimmer));
        }

        IEnumerator DisplaySkimParticleEffectCoroutine(Transform skimmerTransform)
        {
            var particle = Instantiate(ParticleEffect);
            particle.transform.parent = transform;

            var time = 50;
            var timer = 0;
            while (timer < time)
            {
                var distance = transform.position - skimmerTransform.position;
                particle.transform.localScale = new Vector3(1, 1, distance.magnitude);
                particle.transform.rotation = Quaternion.LookRotation(distance, transform.up);
                particle.transform.position = skimmerTransform.position;
                timer++;

                yield return null;
            }
            Destroy(particle);
        }

        // TODO: none of the collision detection should be on the trail
        void OnTriggerEnter(Collider other)
        {
            if (IsShip(other.gameObject))
            {
                var ship = other.GetComponent<ShipGeometry>().Ship;
                var impactVector = ship.transform.forward * ship.GetComponent<ShipData>().speed;

                Collide(ship);
                Explode(impactVector, ship.Team, ship.Player.PlayerName);
            }
            else if (IsSkimmer(other.gameObject))
            {
                other.GetComponent<Skimmer>().PerformSkimmerImpactEffects(trailBlockProperties);
            }
            else if (IsExplosion(other.gameObject))
            {
                if (other.GetComponent<AOEExplosion>().Team == Team)
                    return;

                var speed = other.GetComponent<AOEExplosion>().speed * 10;
                var impactVector = (transform.position - other.transform.position).normalized * speed;

                Explode(impactVector, other.GetComponent<AOEExplosion>().Team, other.GetComponent<AOEExplosion>().Ship.Player.PlayerName);
            }
            else if (IsProjectile(other.gameObject))
            {
                if (other.GetComponent<Projectile>().Team == Team)
                    return;

                var speed = other.GetComponent<Projectile>().Velocity;
                var impactVector = speed;

                Explode(impactVector, other.GetComponent<Projectile>().Team, other.GetComponent<Projectile>().Ship.Player.PlayerName); // TODO: need to attribute the explosion color to the team that made the explosion
            }
        }

        public void Collide(Ship ship)
        {
            if (ownerId == ship.Player.PlayerUUID)
            {
                Debug.Log($"You hit you're teams tail - ownerId: {ownerId}, team: {team}");
            }
            else
            {
                Debug.Log($"Player ({ship.Player.PlayerUUID}) just gave player({ownerId}) a point via tail collision");
                AddToScore?.Invoke(ownerId, scoreChange);
            }

            ship.PerformTrailBlockImpactEffects(trailBlockProperties);
        }

        public void Explode(Vector3 impactVector, Teams team, string playerName)
        {
            // We don't destroy the trail blocks, we keep the objects around so they can be restored
            gameObject.GetComponent<BoxCollider>().enabled = false;
            gameObject.GetComponent<MeshRenderer>().enabled = false;

            // Make exploding block
            var explodingBlock = Instantiate(FossilBlock);
            explodingBlock.transform.position = transform.position;
            explodingBlock.transform.localEulerAngles = transform.localEulerAngles;
            explodingBlock.transform.localScale = transform.localScale;
            explodingBlock.transform.parent = fossilBlockContainer.transform;
            explodingBlock.GetComponent<Renderer>().material = new Material(material);
            explodingBlock.GetComponent<BlockImpact>().HandleImpact(impactVector, team);

            destroyed = true;

            if (StatsManager.Instance != null)
                StatsManager.Instance.BlockDestroyed(team, playerName, trailBlockProperties);

            if (NodeControlManager.Instance != null)
                NodeControlManager.Instance.RemoveBlock(team, playerName, trailBlockProperties);
        }

        public void Steal(string playerName, Teams team)
        {
            if (StatsManager.Instance != null)
                StatsManager.Instance.BlockStolen(team, playerName, trailBlockProperties);

            if (NodeControlManager.Instance != null)
                //NodeControlManager.Instance.RemoveBlock(team, playerName, trailBlockProperties);
                Debug.Log("TODO: Notify NodeControlManager that a block was stolen");

            this.team = team;
            this.playerName = playerName;

            gameObject.GetComponent<MeshRenderer>().material = Hangar.Instance.GetTeamBlockMaterial(team);
        }

        public void Restore()
        {
            if (StatsManager.Instance != null)
                StatsManager.Instance.BlockRestored(team, playerName, trailBlockProperties);

            if (NodeControlManager.Instance != null)
                //NodeControlManager.Instance.RemoveBlock(team, playerName, trailBlockProperties);
                Debug.Log("TODO: Notify NodeControlManager that a block was restored");

            gameObject.GetComponent<BoxCollider>().enabled = true;
            gameObject.GetComponent<MeshRenderer>().enabled = true;

            destroyed = false;
        }

        // TODO: utility class needed to hold these
        private bool IsShip(GameObject go)
        {
            return go.layer == LayerMask.NameToLayer("Ships");
        }
        private bool IsSkimmer(GameObject go)
        {
            return go.layer == LayerMask.NameToLayer("Skimmers");
        }
        private bool IsExplosion(GameObject go)
        {
            return go.layer == LayerMask.NameToLayer("Explosions");
        }
        private bool IsProjectile(GameObject go)
        {
            return go.layer == LayerMask.NameToLayer("Projectiles");
        }
    }
}