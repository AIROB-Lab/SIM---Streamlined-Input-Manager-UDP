using UnityEngine;

namespace Sim.Tracking
{
    /// <summary>Combined gaze, with an explicit source and local coordinate frame.
    /// Nullable per-eye validity means the provider does not expose that measurement.</summary>
    public readonly struct SimGazeSample
    {
        public readonly double TimestampSeconds;
        public readonly string Source;
        public readonly string LocalFrame;
        public readonly TrackingAvailability Availability;
        public readonly Ray LocalRay;
        public readonly Ray WorldRay;
        public readonly bool? LeftEyeValid;
        public readonly bool? RightEyeValid;
        public bool IsValid => Availability == TrackingAvailability.Valid;

        public SimGazeSample(double time, string source, string localFrame, TrackingAvailability availability, Ray local, Ray world, bool? left = null, bool? right = null)
        {
            TimestampSeconds = time;
            Source = source;
            LocalFrame = localFrame;
            Availability = availability;
            LocalRay = local;
            WorldRay = world;
            LeftEyeValid = left;
            RightEyeValid = right;
        }

        public static SimGazeSample FromLocal(double time, string source, string frame, Vector3 origin, Vector3 direction, Transform reference, bool valid, bool? left = null, bool? right = null)
        {
            valid &= SimPoseSample.IsFinite(origin) && SimPoseSample.IsFinite(direction) && direction.sqrMagnitude > 0.000001f;
            if (!valid)
            {
                return new SimGazeSample(time, source, frame, TrackingAvailability.InvalidData, default, default, left, right);
            }

            direction.Normalize();
            Ray local = new Ray(origin, direction);
            Ray world = reference == null ? local : new Ray(reference.TransformPoint(origin), reference.TransformDirection(direction).normalized);
            if (!SimPoseSample.IsFinite(world.origin) || !SimPoseSample.IsFinite(world.direction) || world.direction.sqrMagnitude < 0.000001f)
            {
                return new SimGazeSample(time, source, frame, TrackingAvailability.InvalidData, default, default, left, right);
            }

            return new SimGazeSample(time, source, frame, TrackingAvailability.Valid, local, world, left, right);
        }
    }
}
