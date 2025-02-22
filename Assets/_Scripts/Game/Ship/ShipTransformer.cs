using UnityEngine;
using CosmicShore.Core;
using CosmicShore.Game.IO;
using System.Collections.Generic;

public class ShipTransformer : MonoBehaviour
{
    Quaternion inverseInitialRotation = new(0, 0, 0, 0);

    #region Ship
    public IShip Ship { get; set; }
    protected ShipStatus shipStatus;
    protected ResourceSystem resourceSystem;

    #endregion

    protected InputController inputController;
    protected IInputStatus inputStatus => inputController.InputStatus;

    protected readonly float lerpAmount = 2f;
    protected Quaternion accumulatedRotation;

    [HideInInspector] public float MinimumSpeed;
    [HideInInspector] public float ThrottleScaler;

    public float DefaultMinimumSpeed = 10f;
    public ElementalFloat DefaultThrottleScaler = new(50f);

    public float PitchScaler = 130f;
    public float YawScaler = 130f;
    public float RollScaler = 130f;
    public float RotationThrottleScaler = 0;

    List<ShipThrottleModifier> ThrottleModifiers = new();
    List<ShipVelocityModifier> VelocityModifiers = new();
    float speedModifierMax = 6f;
    float velocityModifierMax = 100;
    protected float throttleMultiplier = 1;
    public float SpeedMultiplier => throttleMultiplier;
    protected Vector3 velocityShift = Vector3.zero;

    public void Initialize(IShip Ship)
    {
        this.Ship = Ship;
        shipStatus = Ship.ShipStatus;
        resourceSystem = Ship.ResourceSystem;
        inputController = Ship.InputController;
    }

    protected virtual void Start()
    {
        MinimumSpeed = DefaultMinimumSpeed;
        ThrottleScaler = DefaultThrottleScaler.Value;
        accumulatedRotation = transform.rotation;
    }

    public void Reset()
    {
        MinimumSpeed = DefaultMinimumSpeed;
        ThrottleScaler = DefaultThrottleScaler.Value;
        accumulatedRotation = transform.rotation;
        resourceSystem.Reset();
        shipStatus.Reset();
    }

    protected virtual void Update()
    {
        if (Ship == null)
            return;

        if (inputController == null)
        {
            inputController = Ship.InputController;
        }

        if (inputController == null)
            return;

        if (inputStatus.Paused)
            return;

        if (shipStatus.Stationary)
            return;

        RotateShip();
        shipStatus.blockRotation = transform.rotation;

        ApplyThrottleModifiers();
        ApplyVelocityModifiers();

        MoveShip();
    }

    protected virtual void RotateShip()
    {
        
        if (inputController != null)
        {
            
            Roll();
            Yaw();
            Pitch();

            if (inputStatus.IsGyroEnabled) //&& !Equals(inverseInitialRotation, new Quaternion(0, 0, 0, 0)))
            {
                // Updates GameObjects blockRotation from input device's gyroscope
                transform.rotation = Quaternion.Lerp(
                                            transform.rotation,
                                            accumulatedRotation * inputController.GetGyroRotation(),
                                            lerpAmount * Time.deltaTime);
            }
            else
            {
                transform.rotation = Quaternion.Lerp(
                                            transform.rotation,
                                            accumulatedRotation,
                                            lerpAmount * Time.deltaTime);
            }
        }
        else
        {
            transform.rotation = Quaternion.Lerp(
                                        transform.rotation,
                                        accumulatedRotation,
                                        lerpAmount * Time.deltaTime);
        }

        // Debug.Log("Accumulated rotation: " + accumulatedRotation);
        // Debug.Log("Ship rotation: " + transform.rotation);
    }

    public void FlatSpinShip(float YAngle)
    {
        accumulatedRotation = Quaternion.AngleAxis(
                            180,
                            transform.up) * accumulatedRotation;
    }

    public void SpinShip(Vector3 newDirection)
    {
        transform.localRotation = Quaternion.LookRotation(newDirection);
    }

    public void GentleSpinShip(Vector3 newDirection, Vector3 newUp, float amount)
    {
        accumulatedRotation = Quaternion.Lerp(accumulatedRotation, Quaternion.LookRotation(newDirection,Ship.Transform.up), amount);
    }

    protected virtual void Pitch() // These need to not use *= because quaternions are not commutative
    {
        accumulatedRotation = Quaternion.AngleAxis(
                            inputStatus.YSum * (shipStatus.Speed * RotationThrottleScaler + PitchScaler) * Time.deltaTime,
                            transform.right) * accumulatedRotation;

        // Debug.Log("Pitch Y Sum: " + inputController.YSum);
        // Debug.Log("Pitch accumulated rotation: " + accumulatedRotation);
    }

    protected virtual void Yaw()  // TODO: test replacing these AngleAxis calls with eulerangles
    {
        accumulatedRotation = Quaternion.AngleAxis(
                            inputStatus.XSum * (shipStatus.Speed * RotationThrottleScaler + YawScaler)  * Time.deltaTime,
                            transform.up) * accumulatedRotation;

        Debug.Log("Yaw X Sum: " + inputStatus.XSum);
        // Debug.Log("Yaw accumulated rotation: " + accumulatedRotation);
    }

    protected virtual void Roll()
    {
        accumulatedRotation = Quaternion.AngleAxis(
                            inputStatus.YDiff * (shipStatus.Speed * RotationThrottleScaler + RollScaler) * Time.deltaTime,
                            transform.forward) * accumulatedRotation;

        // Debug.Log("Roll Y Diff: " + inputController.YDiff);
        // Debug.Log("Roll accumulated rotation: " + accumulatedRotation);
    }

    protected virtual void MoveShip()
    {
        if (!inputController.HasThrottleInput)
        {
            shipStatus.Speed = 0;
            return;
        }
            

        float boostAmount = 1f;
        if (shipStatus.Boosting) // TODO: if we run out of fuel while full speed and straight the ship data still thinks we are boosting
        {
            boostAmount = Ship.BoostMultiplier;
        }
        if (shipStatus.ChargedBoostDischarging) boostAmount *= shipStatus.ChargedBoostCharge;
        if (inputController != null)
        shipStatus.Speed = Mathf.Lerp(shipStatus.Speed, inputStatus.XDiff * ThrottleScaler * boostAmount + MinimumSpeed, lerpAmount * Time.deltaTime);

        shipStatus.Speed *= throttleMultiplier;

        if (!shipStatus.Drifting)
        {
            shipStatus.Course = transform.forward;
        }

        transform.position += (shipStatus.Speed * shipStatus.Course + velocityShift) * Time.deltaTime;
    }

    public void ModifyThrottle(float amount, float duration)
    {
        ThrottleModifiers.Add(new ShipThrottleModifier(amount, duration, 0));
    }

    void ApplyThrottleModifiers()
    {
        float accumulatedThrottleModification = 1;
        for (int i = ThrottleModifiers.Count - 1; i >= 0; i--)
        {
            var modifier = ThrottleModifiers[i];
            modifier.elapsedTime += Time.deltaTime;
            ThrottleModifiers[i] = modifier;

            if (modifier.elapsedTime >= modifier.duration)
            {
                ThrottleModifiers.RemoveAt(i);
                if (ThrottleModifiers.Count == 0)
                {
                    shipStatus.Slowed = false;
                    Hangar.Instance.SlowedShipTransforms.Remove(transform);
                }
            }
            else if (modifier.initialValue < 1) // multiplicative for debuff and additive for buff 
            {
                accumulatedThrottleModification *= Mathf.Lerp(modifier.initialValue, 1f, modifier.elapsedTime / modifier.duration);
                shipStatus.Slowed = true;
                Hangar.Instance.SlowedShipTransforms.Add(transform);
            }
            else
                accumulatedThrottleModification += Mathf.Lerp(modifier.initialValue - 1, 0f, modifier.elapsedTime / modifier.duration);
        }

        accumulatedThrottleModification = Mathf.Min(accumulatedThrottleModification, speedModifierMax);
        if (accumulatedThrottleModification < 0f)
        {
            shipStatus.Slowed = false;
            Hangar.Instance.SlowedShipTransforms.Remove(transform);
        }
        throttleMultiplier = Mathf.Max(accumulatedThrottleModification, 0) ;
    }

    public void ModifyVelocity(Vector3 amount, float duration)
    {
        VelocityModifiers.Add(new ShipVelocityModifier(amount, duration, 0));
    }

    void ApplyVelocityModifiers()
    {
        Vector3 accumulatedVelocityModification = Vector3.zero;
        for (int i = VelocityModifiers.Count - 1; i >= 0; i--)
        {
            var modifier = VelocityModifiers[i];
            modifier.elapsedTime += Time.deltaTime;
            VelocityModifiers[i] = modifier;

            if (modifier.elapsedTime >= modifier.duration)
                VelocityModifiers.RemoveAt(i);
            else
                accumulatedVelocityModification += Vector3.Lerp(modifier.initialValue, Vector3.zero, modifier.elapsedTime / modifier.duration);
        }

        velocityShift = Mathf.Min(accumulatedVelocityModification.magnitude, velocityModifierMax) * accumulatedVelocityModification.normalized;
    }

    private void OnDisable()
    {
        Hangar.Instance.SlowedShipTransforms.Remove(transform);
    }
}