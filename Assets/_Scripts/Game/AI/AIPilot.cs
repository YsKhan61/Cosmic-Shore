using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using CosmicShore.Core;

namespace CosmicShore.Game.AI
{
    public class AIPilot : MonoBehaviour
    {
        public float SkillLevel;

        //public float defaultThrottle = .6f;
        [SerializeField] float defaultThrottleHigh = .6f;
        [SerializeField] float defaultThrottleLow  = .6f;

        //public float defaultAggressiveness = .035f;
        [SerializeField] float defaultAggressivenessHigh = .035f;
        [SerializeField] float defaultAggressivenessLow  = .035f;

        //public float throttleIncrease = .001f;
        [SerializeField] float throttleIncreaseHigh = .001f;
        [SerializeField] float throttleIncreaseLow  = .001f;

        //public float avoidance = 2.5f;
        [SerializeField] float avoidanceHigh = 2.5f;
        [SerializeField] float avoidanceLow = 2.5f;

        //public float aggressivenessIncrease = .001f;
        [SerializeField] float aggressivenessIncreaseHigh = .001f;
        [SerializeField] float aggressivenessIncreaseLow  = .001f;

        [HideInInspector] public float throttle;
        [HideInInspector] public float aggressiveness;

        public float XSum;
        public float YSum;
        public float XDiff;
        public float YDiff;

        public float X;
        public float Y;

        public float defaultThrottle => Mathf.Lerp(defaultThrottleLow, defaultThrottleHigh, SkillLevel);
        public float defaultAggressiveness => Mathf.Lerp(defaultAggressivenessLow, defaultAggressivenessHigh, SkillLevel);
        float throttleIncrease => Mathf.Lerp(throttleIncreaseLow, throttleIncreaseHigh, SkillLevel);
        float avoidance => Mathf.Lerp(avoidanceLow, avoidanceHigh, SkillLevel);
        float aggressivenessIncrease => Mathf.Lerp(aggressivenessIncreaseLow, aggressivenessIncreaseHigh, SkillLevel);

        [SerializeField] float raycastHeight;
        [SerializeField] float raycastWidth;

        public bool AutoPilotEnabled;
        [SerializeField] bool LookingAtCrystal;
        [SerializeField] bool ram;
        [SerializeField] bool drift;
        [HideInInspector] public bool SingleStickControls;

        [SerializeField] bool useAbility;
        [SerializeField] float abilityCooldown;
        [SerializeField] ShipAction ability;

        enum Corner 
        {
            TopRight,
            BottomRight,
            BottomLeft,
            TopLeft,
        };

        ShipStatus shipStatus;
        public IShip Ship { get; set; }

        float lastPitchTarget;
        float lastYawTarget;
        float lastRollTarget;

        RaycastHit hit;
        float maxDistance = 50f;
        float maxDistanceSquared;

        // TODO: rename to 'TargetTransform'
        [HideInInspector] public Transform CrystalTransform;
        Vector3 distance;

        [HideInInspector] public FlowFieldData flowFieldData;

        Dictionary<Corner, AvoidanceBehavior> CornerBehaviors;

        #region Avoidance Stuff
        const float Clockwise = -1;
        const float CounterClockwise = 1;
        struct AvoidanceBehavior
        {
            public float width;
            public float height;
            public float spin;
            public Vector3 direction;

            public AvoidanceBehavior(float width, float height, float spin, Vector3 direction)
            {
                this.width = width;
                this.height = height;
                this.spin = spin;
                this.direction = direction;
            }
        }
        #endregion

        

        public void NodeContentUpdated()
        {
            //Debug.Log($"NodeContentUpdated - transform.position: {transform.position}");
            var activeNode = NodeControlManager.Instance.GetNodeByPosition(transform.position);

            if (activeNode == null)
                activeNode = NodeControlManager.Instance.GetNearestNode(transform.position);

            var nodeItems = activeNode.GetItems();
            float MinDistance = Mathf.Infinity;
            NodeItem closestItem = null;

            foreach (var item in nodeItems.Values)
            {
                // Debuffs are disguised as desireable to the other team
                // So, if it's good, or if it's bad but made by another team, go for it
                if (item.ItemType != ItemType.Buff &&
                    (item.ItemType != ItemType.Debuff || item.Team == Ship.Team)) continue;
                var distance = Vector3.Distance(item.transform.position, transform.position);
                if (distance < MinDistance)
                {
                    closestItem = item;
                    MinDistance = distance;
                }
            }

            CrystalTransform = closestItem == null ? activeNode.transform : closestItem.transform;
        }

        public void Initialize(IShip ship)
        {
            Ship = ship;
            shipStatus = Ship.ShipStatus;
        }

        void Start()
        {
            maxDistanceSquared = maxDistance * maxDistance;
            aggressiveness = defaultAggressiveness;
            throttle = defaultThrottle;
            
            CornerBehaviors = new Dictionary<Corner, AvoidanceBehavior>() {
                { Corner.TopRight, new AvoidanceBehavior (raycastWidth, raycastHeight, Clockwise, Vector3.zero ) },
                { Corner.BottomRight, new AvoidanceBehavior (raycastWidth, -raycastHeight, CounterClockwise, Vector3.zero ) },
                { Corner.BottomLeft, new AvoidanceBehavior (-raycastWidth, -raycastHeight, Clockwise, Vector3.zero ) },
                { Corner.TopLeft, new AvoidanceBehavior (-raycastWidth, raycastHeight, CounterClockwise, Vector3.zero ) }
            };

            var activeNode = NodeControlManager.Instance.GetNodeByPosition(transform.position);
            activeNode.RegisterForUpdates(this);

            if (useAbility) StartCoroutine(UseAbilityCoroutine(ability));
        }

        void Update()
        {
            if (AutoPilotEnabled)
            {
                Ship.InputController.AutoPilotEnabled = true;
                Ship.ShipStatus.AutoPilotEnabled = true;

                var targetPosition = CrystalTransform.position;
                //Vector3 currentDirection = shipStatus.Course;
                distance = targetPosition - transform.position;
                Vector3 desiredDirection = distance.normalized;

                LookingAtCrystal = Vector3.Dot(desiredDirection, shipStatus.Course) >= .9f;
                if (LookingAtCrystal && drift && !shipStatus.Drifting)
                {
                    shipStatus.Course = desiredDirection;
                    Ship.PerformShipControllerActions(InputEvents.LeftStickAction);
                    desiredDirection *= -1;
                }
                else if (LookingAtCrystal && shipStatus.Drifting) desiredDirection *= -1;
                else if (shipStatus.Drifting) Ship.StopShipControllerActions(InputEvents.LeftStickAction);
                

                if (distance.magnitude < float.Epsilon) // Avoid division by zero
                    return;

                Vector3 combinedLocalCrossProduct = Vector3.zero;
                float sqrMagnitude = distance.sqrMagnitude;
                //float combinedRoll;
                Vector3 crossProduct = Vector3.Cross(transform.forward, desiredDirection);
                Vector3 localCrossProduct = transform.InverseTransformDirection(crossProduct);
                combinedLocalCrossProduct += localCrossProduct;
                
                float angle = Mathf.Asin(Mathf.Clamp(combinedLocalCrossProduct.sqrMagnitude * aggressiveness * 12 / Mathf.Min(sqrMagnitude, maxDistance), -1f, 1f)) * Mathf.Rad2Deg;

                if (SingleStickControls)
                {
                    X = Mathf.Clamp(angle * combinedLocalCrossProduct.y, -1, 1);
                    Y = -Mathf.Clamp(angle * combinedLocalCrossProduct.x, -1, 1);
                }
                else
                {
                    YSum = Mathf.Clamp(angle * combinedLocalCrossProduct.x, -1, 1);
                    XSum = Mathf.Clamp(angle * combinedLocalCrossProduct.y, -1, 1);
                    YDiff = Mathf.Clamp(angle * combinedLocalCrossProduct.y, -1, 1);

                    XDiff = (LookingAtCrystal && ram) ? 1 : Mathf.Clamp(throttle, 0, 1);
                }
   
                aggressiveness += aggressivenessIncrease * Time.deltaTime;
                throttle += throttleIncrease * Time.deltaTime;
            }
        }

        Vector3 ShootLaser(Vector3 position)
        {
            if (Physics.Raycast(transform.position + position, transform.forward, out hit, maxDistance))
            {
                Debug.DrawLine(transform.position + position, hit.point, Color.red);
                return hit.point - (transform.position + position);
            }

            Debug.DrawLine(transform.position + position, transform.position + position + transform.forward * maxDistance, Color.green);
            return transform.forward * maxDistance - (transform.position + position);
        }

        float CalculateRollAdjustment(Dictionary<Corner, Vector3> obstacleDirections)
        {
            float rollAdjustment = 0f;

            // Example logic: If top right and bottom left corners detect obstacles, induce a roll.
            if (obstacleDirections[Corner.TopRight].magnitude > 0 && obstacleDirections[Corner.BottomLeft].magnitude > 0)
                rollAdjustment -= 1; // Roll left
            if (obstacleDirections[Corner.TopLeft].magnitude > 0 && obstacleDirections[Corner.BottomRight].magnitude > 0)
                rollAdjustment += 1; // Roll right

            return rollAdjustment;
        }


        IEnumerator UseAbilityCoroutine(ShipAction action) 
        {
            while (useAbility)
            {
                yield return new WaitForSeconds(abilityCooldown); // wait first to give the resource system time to load
                action.StartAction();
            }
            action.StopAction();
        }


        float SigmoidResponse(float input)
        {
            float output = 2 * (1 / (1 + Mathf.Exp(-0.1f * input)) - 0.5f);
            return output;
        }

    }
}