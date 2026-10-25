using NUnit.Framework;
using UnityEngine;

public sealed class PrototypeRobotTests
{
    [Test]
    public void RobotStartsInRobotState()
    {
        var stateMachine = new PrototypeTransformStateMachine(1f);

        Assert.AreEqual(PrototypeRobotTransformState.Robot, stateMachine.State);
        Assert.AreEqual(PrototypeRobotForm.Robot, stateMachine.Form);
        Assert.IsFalse(stateMachine.IsTransforming);
    }

    [Test]
    public void RobotToCarAndBackTransformationCompletesRepeatedly()
    {
        var stateMachine = new PrototypeTransformStateMachine(1f);

        Assert.IsTrue(stateMachine.TryRequestToggle(true, 0f, false));
        Assert.AreEqual(PrototypeRobotTransformState.TransformingToCar, stateMachine.State);
        Assert.IsFalse(stateMachine.TryRequestToggle(true, 0f, false));
        stateMachine.Advance(1f);
        Assert.AreEqual(PrototypeRobotTransformState.Car, stateMachine.State);

        Assert.IsTrue(stateMachine.TryRequestToggle(true, 0f, false));
        stateMachine.Advance(1f);
        Assert.AreEqual(PrototypeRobotTransformState.Robot, stateMachine.State);
    }

    [Test]
    public void InvalidTransformationRequestsAreRejected()
    {
        var stateMachine = new PrototypeTransformStateMachine(1f);

        Assert.IsFalse(stateMachine.TryRequestToggle(false, 0f, false));
        Assert.IsFalse(stateMachine.TryRequestToggle(true, 2f, false));
        Assert.IsFalse(stateMachine.TryRequestToggle(true, 0f, true));
        Assert.AreEqual(PrototypeRobotTransformState.Robot, stateMachine.State);
    }

    [Test]
    public void InterruptedTransformationRecoversToStableForm()
    {
        var stateMachine = new PrototypeTransformStateMachine(1f);

        stateMachine.TryRequestToggle(true, 0f, false);
        stateMachine.Advance(0.4f);
        stateMachine.InterruptAndRecover();
        Assert.AreEqual(PrototypeRobotTransformState.Robot, stateMachine.State);
        Assert.IsFalse(stateMachine.IsTransforming);

        stateMachine.TryRequestToggle(true, 0f, false);
        stateMachine.Advance(1f);
        stateMachine.TryRequestToggle(true, 0f, false);
        stateMachine.Advance(0.5f);
        stateMachine.InterruptAndRecover();
        Assert.AreEqual(PrototypeRobotTransformState.Car, stateMachine.State);
    }

    [Test]
    public void VehicleMathHandlesAccelerationBrakingDirectionAndSteering()
    {
        Assert.AreEqual(18f, PrototypeCarController.CalculateTargetForwardSpeed(1f, 18f, 7f));
        Assert.AreEqual(-7f, PrototypeCarController.CalculateTargetForwardSpeed(-1f, 18f, 7f));
        Assert.Greater(PrototypeCarController.CalculateSteerAngle(1f, 0f, 18f, 70f), PrototypeCarController.CalculateSteerAngle(1f, 18f, 18f, 70f));
    }

    [Test]
    public void HealthAndDamageRemainUsableForRobotCombat()
    {
        var robot = new GameObject("RobotCombatHealthTest");
        var health = robot.AddComponent<PrototypeHealth>();
        health.Initialize(135f);
        health.ApplyDamage(42f);
        Assert.AreEqual(93f, health.CurrentHealth);
        Assert.IsTrue(health.IsAlive);
        Object.DestroyImmediate(robot);
    }
}
