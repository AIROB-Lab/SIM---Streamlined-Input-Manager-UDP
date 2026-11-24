using UnityEngine;

namespace Sim.Tracking
{
    /// <summary>Availability is separate from pose values: zero is not evidence of tracking.</summary>
    public enum TrackingAvailability { Unavailable, Ambiguous, Untracked, InvalidData, Valid }

    /// <summary>Read-only observation in tracking-local and Unity world space, in meters.
    /// TimestampSeconds is host acquisition time, not a headset's native timestamp.</summary>
    public readonly struct SimPoseSample
    {
        public readonly double TimestampSeconds;
        public readonly TrackingAvailability Availability;
        public readonly Pose LocalPose;
        public readonly Pose WorldPose;
        public bool IsValid => Availability == TrackingAvailability.Valid;

        public SimPoseSample(double time, TrackingAvailability availability, Pose local, Pose world)
        {
            TimestampSeconds = time;
            Availability = availability;
            LocalPose = local;
            WorldPose = world;
        }

        public static SimPoseSample Missing(TrackingAvailability status)
            => new SimPoseSample(SimClock.Now, status, default, default);

        /// <summary>Null origin explicitly means an identity tracking-to-world transform.</summary>
        public static SimPoseSample FromLocal(double time, Vector3 position, Quaternion rotation, Transform origin)
        {
            if (!IsFinite(position) || !IsFinite(rotation) || Quaternion.Dot(rotation, rotation) < 0.000001f)
                return new SimPoseSample(time, TrackingAvailability.InvalidData, default, default);

            rotation = Quaternion.Normalize(rotation);
            Pose local = new Pose(position, rotation);
            Pose world = origin == null ? local : new Pose(origin.TransformPoint(position), origin.rotation * rotation);
            if (!IsFinite(world.position) || !IsFinite(world.rotation))
                return new SimPoseSample(time, TrackingAvailability.InvalidData, default, default);
            return new SimPoseSample(time, TrackingAvailability.Valid, local, world);
        }

        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        public static bool IsFinite(Quaternion value)
            => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
    }
}
