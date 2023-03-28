using System.Collections;
using UnityEngine;
using StarWriter.Core;
using StarWriter.Core.Input;

public class ShipController : MonoBehaviour
{
    Quaternion inverseInitialRotation = new(0, 0, 0, 0);

    #region Ship
    protected Ship ship;
    protected ShipData shipData;
    protected ResourceSystem resourceSystem;
    #endregion

    protected string uuid;  // TODO: remove this
    protected InputController inputController;
    protected float speed;
    protected readonly float lerpAmount = 2f;
    protected Quaternion displacementQuaternion;

    [HideInInspector] public float MinimumSpeed;
    [HideInInspector] public float ThrottleScaler;

    public float DefaultMinimumSpeed = 10f;
    public float DefaultThrottleScaler = 50;
    public float BoostDecay = 1;
    public float MaxBoostDecay = 10;
    public float BoostDecayGrowthRate = .03f;

    public float PitchScaler = 130f;
    public float YawScaler = 130f;
    public float RollScaler = 130f;
    public float RotationThrottleScaler = 0;

    protected virtual void Start()
    {
        uuid = ship.Player.PlayerUUID;
        ship = GetComponent<Ship>();
        shipData = ship.GetComponent<ShipData>();
        resourceSystem = ship.GetComponent<ResourceSystem>();

        MinimumSpeed = DefaultMinimumSpeed;
        ThrottleScaler = DefaultThrottleScaler;
        displacementQuaternion = transform.rotation;
        inputController = ship.inputController;
    }

    public void Reset()
    {
        MinimumSpeed = DefaultMinimumSpeed;
        ThrottleScaler = DefaultThrottleScaler;
        displacementQuaternion = transform.rotation;
        shipData.Reset();
    }

    protected virtual void Update()
    {
        if (inputController == null) 
            inputController = ship.inputController;

        if (inputController.Paused) 
            return;

        if (inputController.Idle) 
            return;

        if (shipData.BoostCharging)
            ChargeBoost();

        RotateShip();
        shipData.blockRotation = transform.rotation;

        MoveShip();
    }

    protected void RotateShip()
    {
        Pitch();
        Yaw();
        Roll();

        if (inputController.isGyroEnabled && !Equals(inverseInitialRotation, new Quaternion(0, 0, 0, 0)))
        {
            // Updates GameObjects blockRotation from input device's gyroscope
            transform.rotation = Quaternion.Lerp(
                                        transform.rotation,
                                        displacementQuaternion * inputController.GetGyroRotation(),
                                        lerpAmount);
        }
        else
        {
            transform.rotation = Quaternion.Lerp(
                                        transform.rotation,
                                        displacementQuaternion,
                                        lerpAmount);
        }
    }

    public void StartChargedBoost() 
    {
        StartCoroutine(DecayingBoostCoroutine());
    }

    void ChargeBoost()
    {
        BoostDecay += BoostDecayGrowthRate;
        resourceSystem.ChangeBoostAmount(BoostDecayGrowthRate);
    }

    IEnumerator DecayingBoostCoroutine()
    {
        shipData.BoostDecaying = true;
        while (BoostDecay > 1)
        {
            BoostDecay = Mathf.Clamp(BoostDecay - Time.deltaTime, 1, MaxBoostDecay);
            resourceSystem.ChangeBoostAmount(-Time.deltaTime);
            yield return null;
        }
        shipData.BoostDecaying = false;
        resourceSystem.ChangeBoostAmount(-resourceSystem.CurrentBoost);

    }

    protected virtual void Pitch() // These need to not use *= because quaternions are not commutative
    {
        displacementQuaternion = Quaternion.AngleAxis(
                            inputController.YSum * -(speed * RotationThrottleScaler + PitchScaler) * Time.deltaTime,
                            transform.right) * displacementQuaternion;
    }

    protected virtual void Yaw()  
    {
        displacementQuaternion = Quaternion.AngleAxis(
                            inputController.XSum * (speed * RotationThrottleScaler + YawScaler) *
                                (Screen.currentResolution.width / Screen.currentResolution.height) * Time.deltaTime,
                            transform.up) * displacementQuaternion;
    }

    protected virtual void Roll()
    {
        displacementQuaternion = Quaternion.AngleAxis(
                            inputController.YDiff * (speed * RotationThrottleScaler + RollScaler) * Time.deltaTime,
                            transform.forward) * displacementQuaternion;
    }

    public void Rotate(Vector3 euler)
    {
        displacementQuaternion = Quaternion.Euler(euler) * displacementQuaternion;
    }

    public void Rotate(Quaternion rotation, bool replace = false)
    {
        if (replace) displacementQuaternion = rotation;
        else displacementQuaternion = rotation * displacementQuaternion;
    }

    protected virtual void MoveShip()
    {
        float boostAmount = 1f;
        if (shipData.Boosting && resourceSystem.CurrentBoost > 0) // TODO: if we run out of fuel while full speed and straight the ship data still thinks we are boosting
        {
            boostAmount = ship.boostMultiplier;
            resourceSystem.ChangeBoostAmount(ship.boostFuelAmount);
        }
        if (shipData.BoostDecaying) boostAmount *= BoostDecay;
        speed = Mathf.Lerp(speed, inputController.XDiff * ThrottleScaler * boostAmount + MinimumSpeed, lerpAmount * Time.deltaTime);

        // Move ship velocityDirection
        shipData.InputSpeed = speed;

        if (shipData.Drifting)
        {
            ship.GetComponent<TrailSpawner>().SetDotProduct(Vector3.Dot(shipData.Course, transform.forward));
        }
        else
        {
            shipData.Course = transform.forward;
        }

        transform.position += shipData.Speed * Time.deltaTime * shipData.Course;
    }
}