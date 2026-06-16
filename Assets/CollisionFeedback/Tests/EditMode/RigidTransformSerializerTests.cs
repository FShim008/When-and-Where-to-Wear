using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    public class RigidTransformSerializerTests
    {
        [Test]
        public void RoundTrip_PreservesTransformAction()
        {
            // A 90-degree rotation about Z plus a translation.
            var t = new RigidTransform(
                new Vector3(0f, -1f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(0f, 0f, 1f),
                new Vector3(1.25f, -0.5f, 3.75f));

            string text = RigidTransformSerializer.Format(t);
            Assert.IsTrue(RigidTransformSerializer.TryParse(text, out RigidTransform back));

            var p = new Vector3(0.3f, 0.4f, 0.5f);
            Assert.That((back.Apply(p) - t.Apply(p)).magnitude, Is.LessThan(1e-5f));
        }

        [Test]
        public void TryParse_RejectsMalformedInput()
        {
            Assert.IsFalse(RigidTransformSerializer.TryParse("1 2 3", out _));        // too few fields
            Assert.IsFalse(RigidTransformSerializer.TryParse("", out _));             // empty
            Assert.IsFalse(RigidTransformSerializer.TryParse("a b c d e f g h i j k l", out _)); // non-numeric
        }
    }
}
