using CampfireFriends.Climbing;
using NUnit.Framework;
using UnityEngine;

namespace CampfireFriends.Tests
{
    /// <summary>Hand-over-hand climbing and the fall afterwards.</summary>
    public class ClimbingTests
    {
        private static void AssertNear(Vector3 expected, Vector3 actual) =>
            Assert.That(Vector3.Distance(expected, actual), Is.LessThan(0.0001f), $"expected {expected}, got {actual}");

        [Test]
        public void PullingTheHandDown_LiftsTheBody()
        {
            var climb = new ClimbState();
            climb.Grip(HandSide.Right, new Vector3(0f, 1.5f, 0.3f));

            var delta = climb.BodyDelta(Vector3.zero, new Vector3(0f, 1.2f, 0.3f));

            AssertNear(new Vector3(0f, 0.3f, 0f), delta);
        }

        [Test]
        public void TheLastGrip_Leads()
        {
            var climb = new ClimbState();
            climb.Grip(HandSide.Left, new Vector3(-0.2f, 1.4f, 0.3f));
            climb.Grip(HandSide.Right, new Vector3(0.2f, 1.8f, 0.3f));

            var delta = climb.BodyDelta(new Vector3(-0.2f, 1.0f, 0.3f), new Vector3(0.2f, 1.7f, 0.3f));

            Assert.AreEqual(HandSide.Right, climb.Leading);
            AssertNear(new Vector3(0f, 0.1f, 0f), delta);
        }

        [Test]
        public void LettingGoWithOneHand_PassesTheClimbToTheOther()
        {
            var climb = new ClimbState();
            climb.Grip(HandSide.Left, new Vector3(0f, 1f, 0f));
            climb.Grip(HandSide.Right, new Vector3(0f, 2f, 0f));

            bool fell = climb.Release(HandSide.Right);

            Assert.IsFalse(fell);
            Assert.IsTrue(climb.IsClimbing);
            Assert.AreEqual(HandSide.Left, climb.Leading);
        }

        [Test]
        public void LettingGoWithBothHands_EndsTheClimb()
        {
            var climb = new ClimbState();
            climb.Grip(HandSide.Left, Vector3.one);

            Assert.IsTrue(climb.Release(HandSide.Left));
            Assert.IsFalse(climb.IsClimbing);
            AssertNear(Vector3.zero, climb.BodyDelta(Vector3.zero, Vector3.zero));
        }

        [Test]
        public void ReleasingAHandThatWasntHolding_ChangesNothing()
        {
            var climb = new ClimbState();
            climb.Grip(HandSide.Left, Vector3.one);

            Assert.IsFalse(climb.Release(HandSide.Right));
            Assert.IsTrue(climb.IsGripping(HandSide.Left));
        }

        [Test]
        public void Regrip_MovesTheHoldToWhereTheHandIsNow()
        {
            var climb = new ClimbState();
            climb.Grip(HandSide.Left, new Vector3(0f, 1f, 0f));
            climb.Grip(HandSide.Right, new Vector3(0f, 2f, 0f));
            climb.Release(HandSide.Right);

            climb.Regrip(HandSide.Left, new Vector3(0f, 1.3f, 0f));

            AssertNear(Vector3.zero, climb.BodyDelta(new Vector3(0f, 1.3f, 0f), Vector3.zero));
        }

        [Test]
        public void Fall_SpeedsUp_AndLandsExactlyOnTheGround()
        {
            var fall = new Fall();
            float height = 3f, fallen = 0f;
            int frames = 0;
            while (fallen < height && frames < 1000)
            {
                fallen += fall.Step(1f / 72f, height - fallen);
                frames++;
            }

            Assert.AreEqual(height, fallen, 0.0001f, "Never through the ground.");
            Assert.Less(frames, 72, "Three metres takes under a second.");
            Assert.AreEqual(0f, fall.Speed, "Landing stops the fall.");
        }

        [Test]
        public void Fall_OnTheGround_DoesNothing()
        {
            var fall = new Fall();
            Assert.AreEqual(0f, fall.Step(0.1f, 0f));
        }

        [Test]
        public void Fall_IsCappedAtTerminalSpeed()
        {
            var fall = new Fall();
            for (int i = 0; i < 1000; i++) fall.Step(0.02f, 10000f, terminalSpeed: 5f);
            Assert.AreEqual(5f, fall.Speed, 0.0001f);
        }
    }
}
