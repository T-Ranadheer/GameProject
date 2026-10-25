using System;

public enum PrototypeRobotForm
{
    Robot,
    Car,
    Helicopter,
    Tank,
    Motorcycle,
    Jet
}

public enum PrototypeRobotTransformState
{
    Robot,
    TransformingToCar,
    Car,
    TransformingToRobot,
    TransformingToHelicopter,
    Helicopter,
    TransformingToTank,
    Tank,
    TransformingToMotorcycle,
    Motorcycle,
    TransformingToJet,
    Jet
}

public sealed class PrototypeTransformStateMachine
{
    private readonly float duration;
    private float elapsed;

    public PrototypeRobotTransformState State { get; private set; } = PrototypeRobotTransformState.Robot;
    public PrototypeRobotForm Form
    {
        get
        {
            switch (State)
            {
                case PrototypeRobotTransformState.Car:
                case PrototypeRobotTransformState.TransformingToRobot:
                    return PrototypeRobotForm.Car;
                case PrototypeRobotTransformState.Helicopter:
                case PrototypeRobotTransformState.TransformingToHelicopter:
                    return PrototypeRobotForm.Helicopter;
                case PrototypeRobotTransformState.Tank:
                case PrototypeRobotTransformState.TransformingToTank:
                    return PrototypeRobotForm.Tank;
                case PrototypeRobotTransformState.Motorcycle:
                case PrototypeRobotTransformState.TransformingToMotorcycle:
                    return PrototypeRobotForm.Motorcycle;
                case PrototypeRobotTransformState.Jet:
                case PrototypeRobotTransformState.TransformingToJet:
                    return PrototypeRobotForm.Jet;
                default:
                    return PrototypeRobotForm.Robot;
            }
        }
    }
    public bool IsTransforming => State == PrototypeRobotTransformState.TransformingToCar || State == PrototypeRobotTransformState.TransformingToRobot;
    public float Progress
    {
        get
        {
            if (!IsTransforming)
            {
                return 1f;
            }

            var value = elapsed / duration;
            return Math.Max(0f, Math.Min(1f, value));
        }
    }

    public PrototypeTransformStateMachine(float transformDuration)
        : this(transformDuration, PrototypeRobotForm.Car)
    {
    }

    public PrototypeTransformStateMachine(float transformDuration, PrototypeRobotForm vehicleForm)
    {
        duration = Math.Max(0.05f, transformDuration);
        VehicleForm = vehicleForm == PrototypeRobotForm.Robot ? PrototypeRobotForm.Car : vehicleForm;
    }

    public PrototypeRobotForm VehicleForm { get; }

    public bool TryRequestToggle(bool grounded, float speed, bool blocked)
    {
        if (IsTransforming || !grounded || speed > 1.5f || blocked)
        {
            return false;
        }

        elapsed = 0f;
        State = State == PrototypeRobotTransformState.Robot
            ? GetTransformingToVehicleState(VehicleForm)
            : PrototypeRobotTransformState.TransformingToRobot;
        return true;
    }

    public bool Advance(float deltaTime)
    {
        if (!IsTransforming)
        {
            return false;
        }

        elapsed += Math.Max(0f, deltaTime);
        if (elapsed < duration)
        {
            return false;
        }

        State = State == PrototypeRobotTransformState.TransformingToRobot
            ? PrototypeRobotTransformState.Robot
            : GetVehicleState(VehicleForm);
        elapsed = duration;
        return true;
    }

    public void InterruptAndRecover()
    {
        State = Form == PrototypeRobotForm.Car ? PrototypeRobotTransformState.Car : PrototypeRobotTransformState.Robot;
        elapsed = duration;
    }

    private static PrototypeRobotTransformState GetTransformingToVehicleState(PrototypeRobotForm form)
    {
        switch (form)
        {
            case PrototypeRobotForm.Helicopter:
                return PrototypeRobotTransformState.TransformingToHelicopter;
            case PrototypeRobotForm.Tank:
                return PrototypeRobotTransformState.TransformingToTank;
            case PrototypeRobotForm.Motorcycle:
                return PrototypeRobotTransformState.TransformingToMotorcycle;
            case PrototypeRobotForm.Jet:
                return PrototypeRobotTransformState.TransformingToJet;
            default:
                return PrototypeRobotTransformState.TransformingToCar;
        }
    }

    private static PrototypeRobotTransformState GetVehicleState(PrototypeRobotForm form)
    {
        switch (form)
        {
            case PrototypeRobotForm.Helicopter:
                return PrototypeRobotTransformState.Helicopter;
            case PrototypeRobotForm.Tank:
                return PrototypeRobotTransformState.Tank;
            case PrototypeRobotForm.Motorcycle:
                return PrototypeRobotTransformState.Motorcycle;
            case PrototypeRobotForm.Jet:
                return PrototypeRobotTransformState.Jet;
            default:
                return PrototypeRobotTransformState.Car;
        }
    }
}
