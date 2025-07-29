using UnityEngine;
using UnityEngine.InputSystem;


namespace CosmicShore.Game.IO
{
    public class GamepadInputStrategy : BaseInputStrategy
    {
        private bool leftStickEffectsStarted;
        private bool rightStickEffectsStarted;
        private bool fullSpeedStraightEffectsStarted;
        private bool minimumSpeedStraightEffectsStarted;

        private Vector2 leftStickRaw;
        private Vector2 rightStickRaw;

        public override void Initialize(IInputStatus inputStatus)
        {
            base.Initialize(inputStatus);
            ResetInput();
        }

        public override void ProcessInput()
        {
            if (Gamepad.current == null) return;

            ProcessStickInput();
            ProcessButtonInput();
            Reparameterize();
            PerformSpeedAndDirectionalEffects();
        }

        private void ProcessStickInput()
        {
            inputStatus.Throttle = Gamepad.current.rightShoulder.ReadValue();
            leftStickRaw = Gamepad.current.leftStick.ReadValue();
            rightStickRaw = Gamepad.current.rightStick.ReadValue();
        }

        private void ProcessButtonInput()
        {
            // Primary action buttons
            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                inputStatus.OnButtonPressed.Raise(InputEvents.Button1Action);
                //_ship.PerformShipControllerActions(InputEvents.Button1Action);
            if (Gamepad.current.buttonSouth.wasReleasedThisFrame)
                inputStatus.OnButtonReleased.Raise(InputEvents.Button1Action);
                // _ship.StopShipControllerActions(InputEvents.Button1Action);

            if (Gamepad.current.buttonEast.wasPressedThisFrame)
                inputStatus.OnButtonPressed.Raise(InputEvents.Button2Action);
                // _ship.PerformShipControllerActions(InputEvents.Button2Action);
            if (Gamepad.current.buttonEast.wasReleasedThisFrame)
                inputStatus.OnButtonReleased.Raise(InputEvents.Button2Action);
                // _ship.StopShipControllerActions(InputEvents.Button2Action);

            if (Gamepad.current.buttonWest.wasPressedThisFrame)
                inputStatus.OnButtonPressed.Raise(InputEvents.Button3Action);
                // _ship.PerformShipControllerActions(InputEvents.Button3Action);
            if (Gamepad.current.buttonWest.wasReleasedThisFrame)
                inputStatus.OnButtonReleased.Raise(InputEvents.Button3Action);
                // _ship.StopShipControllerActions(InputEvents.Button3Action);

            // Shoulder buttons and triggers
            if (Gamepad.current.leftShoulder.wasPressedThisFrame)
            {
                inputStatus.Idle = true;
                inputStatus.OnButtonPressed.Raise(InputEvents.IdleAction);;
                // _ship.PerformShipControllerActions(InputEvents.IdleAction);
            }
            if (Gamepad.current.leftShoulder.wasReleasedThisFrame)
            {
                inputStatus.Idle = false;
                inputStatus.OnButtonReleased.Raise(InputEvents.IdleAction);;
                // _ship.StopShipControllerActions(InputEvents.IdleAction);
            }

            // Right shoulder for flip action
            if (Gamepad.current.rightShoulder.wasPressedThisFrame)
                inputStatus.OnButtonPressed.Raise(InputEvents.FlipAction);
                // _ship.PerformShipControllerActions(InputEvents.FlipAction);
            if (Gamepad.current.rightShoulder.wasReleasedThisFrame)
                inputStatus.OnButtonReleased.Raise(InputEvents.FlipAction);
                // _ship.StopShipControllerActions(InputEvents.FlipAction);

            // Triggers for stick actions
            if (Gamepad.current.leftTrigger.wasPressedThisFrame)
                inputStatus.OnButtonPressed.Raise(InputEvents.LeftStickAction);
                // _ship.PerformShipControllerActions(InputEvents.LeftStickAction);
            if (Gamepad.current.leftTrigger.wasReleasedThisFrame)
                inputStatus.OnButtonReleased.Raise(InputEvents.LeftStickAction);
                // _ship.StopShipControllerActions(InputEvents.LeftStickAction);

            if (Gamepad.current.rightTrigger.wasPressedThisFrame)
                inputStatus.OnButtonPressed.Raise(InputEvents.RightStickAction);
                // _ship.PerformShipControllerActions(InputEvents.RightStickAction);
            if (Gamepad.current.rightTrigger.wasReleasedThisFrame)
                inputStatus.OnButtonReleased.Raise(InputEvents.RightStickAction);
                // _ship.StopShipControllerActions(InputEvents.RightStickAction);
        }

        private void Reparameterize()
        {
            // Calculate eased joystick positions
            inputStatus.EasedLeftJoystickPosition = new Vector2(
                Ease(2 * leftStickRaw.x),
                Ease(2 * leftStickRaw.y)
            );
            inputStatus.EasedRightJoystickPosition = new Vector2(
                Ease(2 * rightStickRaw.x),
                Ease(2 * rightStickRaw.y)
            );

            // Calculate sums and differences exactly as touch input does
            inputStatus.XSum = Ease(rightStickRaw.x + leftStickRaw.x);
            inputStatus.YSum = -Ease(rightStickRaw.y + leftStickRaw.y);
            inputStatus.XDiff = (rightStickRaw.x - leftStickRaw.x + 2) / 4;
            inputStatus.YDiff = Ease(rightStickRaw.y - leftStickRaw.y);
        }

        private void PerformSpeedAndDirectionalEffects()
        {
            float threshold = .3f;
            float sumOfRotations = Mathf.Abs(inputStatus.YDiff) + Mathf.Abs(inputStatus.YSum) + Mathf.Abs(inputStatus.XSum);
            float DeviationFromFullSpeedStraight = (1 - inputStatus.XDiff) + sumOfRotations;
            float DeviationFromMinimumSpeedStraight = inputStatus.XDiff + sumOfRotations;

            if (DeviationFromFullSpeedStraight < threshold && !fullSpeedStraightEffectsStarted)
            {
                fullSpeedStraightEffectsStarted = true;
                inputStatus.OnButtonPressed.Raise(InputEvents.FullSpeedStraightAction);
                // _ship.PerformShipControllerActions(InputEvents.FullSpeedStraightAction);
            }
            else if (DeviationFromMinimumSpeedStraight < threshold && !minimumSpeedStraightEffectsStarted)
            {
                minimumSpeedStraightEffectsStarted = true;
                inputStatus.OnButtonPressed.Raise(InputEvents.MinimumSpeedStraightAction);
                // _ship.PerformShipControllerActions(InputEvents.MinimumSpeedStraightAction);
            }
            else
            {
                if (fullSpeedStraightEffectsStarted && DeviationFromFullSpeedStraight > threshold)
                {
                    fullSpeedStraightEffectsStarted = false;
                    inputStatus.OnButtonReleased.Raise(InputEvents.FullSpeedStraightAction);
                    // _ship.StopShipControllerActions(InputEvents.FullSpeedStraightAction);
                }
                if (minimumSpeedStraightEffectsStarted && DeviationFromMinimumSpeedStraight > threshold)
                {
                    minimumSpeedStraightEffectsStarted = false;
                    inputStatus.OnButtonReleased.Raise(InputEvents.MinimumSpeedStraightAction);
                    // _ship.StopShipControllerActions(InputEvents.MinimumSpeedStraightAction);
                }
            }
        }

        public override void SetPortrait(bool portrait)
        {
            // Gamepad doesn't need to handle portrait mode changes
        }
    }
}
