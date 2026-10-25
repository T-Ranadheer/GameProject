using UnityEngine;
using UnityEngine.InputSystem;

public enum PrototypeTouchCommand
{
    MoveLeft,
    MoveRight,
    MoveForward,
    MoveBack,
    Jump,
    ToggleFlight,
    Attack,
    RobotMelee,
    Transform,
    DeployRobot,
    Brake,
    Pause,
    Ability2,
    Ability3,
    Ability4,
    SwitchCharacter,
    TriggerHostileEncounter,
    SecondaryAttack,
    VehicleAscend,
    VehicleDescend
}

public static class PrototypeInput
{
    private static bool moveLeft;
    private static bool moveRight;
    private static bool moveForward;
    private static bool moveBack;
    private static bool jumpPressed;
    private static bool flightPressed;
    private static bool attackPressed;
    private static bool meleePressed;
    private static bool transformPressed;
    private static bool deployRobotPressed;
    private static bool brakeHeld;
    private static bool pausePressed;
    private static bool ability2Pressed;
    private static bool ability3Pressed;
    private static bool ability4Pressed;
    private static bool switchCharacterPressed;
    private static bool triggerHostileEncounterPressed;
    private static bool secondaryAttackPressed;
    private static bool vehicleAscendHeld;
    private static bool vehicleDescendHeld;

    public static void SetTouchState(PrototypeTouchCommand command, bool pressed)
    {
        switch (command)
        {
            case PrototypeTouchCommand.MoveLeft:
                moveLeft = pressed;
                break;
            case PrototypeTouchCommand.MoveRight:
                moveRight = pressed;
                break;
            case PrototypeTouchCommand.MoveForward:
                moveForward = pressed;
                break;
            case PrototypeTouchCommand.MoveBack:
                moveBack = pressed;
                break;
            case PrototypeTouchCommand.Jump:
                if (pressed) jumpPressed = true;
                break;
            case PrototypeTouchCommand.ToggleFlight:
                if (pressed) flightPressed = true;
                break;
            case PrototypeTouchCommand.Attack:
                if (pressed) attackPressed = true;
                break;
            case PrototypeTouchCommand.RobotMelee:
                if (pressed) meleePressed = true;
                break;
            case PrototypeTouchCommand.Transform:
                if (pressed) transformPressed = true;
                break;
            case PrototypeTouchCommand.DeployRobot:
                if (pressed) deployRobotPressed = true;
                break;
            case PrototypeTouchCommand.Brake:
                brakeHeld = pressed;
                break;
            case PrototypeTouchCommand.Pause:
                if (pressed) pausePressed = true;
                break;
            case PrototypeTouchCommand.Ability2:
                if (pressed) ability2Pressed = true;
                break;
            case PrototypeTouchCommand.Ability3:
                if (pressed) ability3Pressed = true;
                break;
            case PrototypeTouchCommand.Ability4:
                if (pressed) ability4Pressed = true;
                break;
            case PrototypeTouchCommand.SwitchCharacter:
                if (pressed) switchCharacterPressed = true;
                break;
            case PrototypeTouchCommand.TriggerHostileEncounter:
                if (pressed) triggerHostileEncounterPressed = true;
                break;
            case PrototypeTouchCommand.SecondaryAttack:
                if (pressed) secondaryAttackPressed = true;
                break;
            case PrototypeTouchCommand.VehicleAscend:
                vehicleAscendHeld = pressed;
                break;
            case PrototypeTouchCommand.VehicleDescend:
                vehicleDescendHeld = pressed;
                break;
        }
    }

    public static Vector2 Move
    {
        get
        {
            var value = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                value.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
                value.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            }

            if (moveLeft) value.x -= 1f;
            if (moveRight) value.x += 1f;
            if (moveForward) value.y += 1f;
            if (moveBack) value.y -= 1f;
            return Vector2.ClampMagnitude(value, 1f);
        }
    }

    public static Vector2 Look
    {
        get
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.rightButton.isPressed)
            {
                return Vector2.zero;
            }

            return mouse.delta.ReadValue();
        }
    }

    public static float FlightVertical
    {
        get
        {
            var value = 0f;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                value += keyboard.spaceKey.isPressed ? 1f : 0f;
                value -= keyboard.leftCtrlKey.isPressed ? 1f : 0f;
            }

            if (vehicleAscendHeld) value += 1f;
            if (vehicleDescendHeld) value -= 1f;
            return Mathf.Clamp(value, -1f, 1f);
        }
    }

    public static bool IsRunning
    {
        get
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.leftShiftKey.isPressed;
        }
    }

    public static bool JumpPressed
    {
        get
        {
            var keyboard = Keyboard.current;
            var value = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
            return value || Consume(ref jumpPressed);
        }
    }

    public static bool ToggleFlightPressed
    {
        get
        {
            var keyboard = Keyboard.current;
            var value = keyboard != null && keyboard.fKey.wasPressedThisFrame;
            return value || Consume(ref flightPressed);
        }
    }

    public static bool AttackPressed
    {
        get
        {
            var mouse = Mouse.current;
            var value = mouse != null && mouse.leftButton.wasPressedThisFrame;
            return value || Consume(ref attackPressed);
        }
    }

    public static bool RobotMeleePressed
    {
        get
        {
            var keyboard = Keyboard.current;
            var value = keyboard != null && keyboard.eKey.wasPressedThisFrame;
            return value || Consume(ref meleePressed);
        }
    }

    public static bool TransformPressed
    {
        get
        {
            var keyboard = Keyboard.current;
            var value = keyboard != null && keyboard.tKey.wasPressedThisFrame;
            return value || Consume(ref transformPressed);
        }
    }

    public static bool DeployRobotPressed
    {
        get
        {
            var keyboard = Keyboard.current;
            var value = keyboard != null && keyboard.rKey.wasPressedThisFrame;
            return value || Consume(ref deployRobotPressed);
        }
    }

    public static bool BrakeHeld
    {
        get
        {
            var keyboard = Keyboard.current;
            return (keyboard != null && keyboard.spaceKey.isPressed) || brakeHeld;
        }
    }

    public static bool PausePressed
    {
        get
        {
            var keyboard = Keyboard.current;
            var value = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            return value || Consume(ref pausePressed);
        }
    }

    public static bool Ability2Pressed
    {
        get
        {
            var keyboard = Keyboard.current;
            var value = keyboard != null && keyboard.gKey.wasPressedThisFrame;
            return value || Consume(ref ability2Pressed);
        }
    }

    public static bool Ability3Pressed
    {
        get
        {
            var keyboard = Keyboard.current;
            var value = keyboard != null && keyboard.cKey.wasPressedThisFrame;
            return value || Consume(ref ability3Pressed);
        }
    }

    public static bool Ability4Pressed
    {
        get
        {
            var keyboard = Keyboard.current;
            var value = keyboard != null && keyboard.vKey.wasPressedThisFrame;
            return value || Consume(ref ability4Pressed);
        }
    }

    public static bool SecondaryAttackPressed
    {
        get
        {
            var keyboard = Keyboard.current;
            var value = keyboard != null && keyboard.gKey.wasPressedThisFrame;
            return value || Consume(ref secondaryAttackPressed);
        }
    }

    public static bool SwitchCharacterPressed
    {
        get
        {
            var keyboard = Keyboard.current;
            var value = keyboard != null && keyboard.tabKey.wasPressedThisFrame;
            return value || Consume(ref switchCharacterPressed);
        }
    }

    public static bool SelectOriginalPressed => WasKeyPressed(Key.Digit1);
    public static bool SelectElectricPressed => WasKeyPressed(Key.Digit2);
    public static bool SelectGiantPressed => WasKeyPressed(Key.Digit3);
    public static bool SelectAtlasPressed => WasKeyPressed(Key.Digit4);
    public static bool SelectHelicopterPressed => WasKeyPressed(Key.Digit5);
    public static bool SelectTankPressed => WasKeyPressed(Key.Digit6);
    public static bool SelectMotorcyclePressed => WasKeyPressed(Key.Digit7);
    public static bool SelectJetPressed => WasKeyPressed(Key.Digit8);
    public static bool TriggerHostileEncounterPressed
    {
        get
        {
            var keyboard = Keyboard.current;
            var value = keyboard != null && keyboard.hKey.wasPressedThisFrame;
            return value || Consume(ref triggerHostileEncounterPressed);
        }
    }

    private static bool WasKeyPressed(Key key)
    {
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard[key].wasPressedThisFrame;
    }

    private static bool Consume(ref bool value)
    {
        if (!value)
        {
            return false;
        }

        value = false;
        return true;
    }
}
