using UnityEngine;
using UnityEngine.XR;

namespace Sim.Tracking
{
    /// <summary>Combined OpenXR gaze, when the runtime exposes Eye Gaze Interaction.
    /// No head-direction fallback: a headset pose is not an eye measurement.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class SimOpenXrGazeSource : SimGazeSource
    {
        [Tooltip("XR Origin camera-offset transform. Null explicitly means identity tracking space.")]
        public Transform trackingOrigin;
        readonly SimXrPoseReader reader = new SimXrPoseReader(InputDeviceCharacteristics.EyeTracking, true);

        void Update()
        {
            SimPoseSample pose = reader.Read(trackingOrigin);
            Status = pose.Availability.ToString();
            if (!pose.IsValid)
            {
                Latest = new SimGazeSample(pose.TimestampSeconds, "OpenXR", "tracking_origin", pose.Availability, default, default);
                return;
            }
            Latest = new SimGazeSample(pose.TimestampSeconds, "OpenXR", "tracking_origin", pose.Availability,
                new Ray(pose.LocalPose.position, pose.LocalPose.rotation * Vector3.forward),
                new Ray(pose.WorldPose.position, pose.WorldPose.rotation * Vector3.forward));
        }
    }
}
