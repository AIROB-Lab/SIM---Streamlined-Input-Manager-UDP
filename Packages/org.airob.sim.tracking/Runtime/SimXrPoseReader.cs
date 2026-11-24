using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Sim.Tracking
{
    /// <summary>Observes XR devices without starting XR or moving any scene object.
    /// A role must resolve to exactly one device; never choose by enumeration order.</summary>
    public sealed class SimXrPoseReader
    {
        readonly InputDeviceCharacteristics characteristics;
        readonly bool gaze;
        readonly List<InputDevice> matches = new List<InputDevice>();
        double nextDiscovery;
        // These names are defined by OpenXR EyeGazeInteraction, not devicePosition/deviceRotation.
        static readonly InputFeatureUsage<Vector3> gazePosition = new InputFeatureUsage<Vector3>("gazePosition");
        static readonly InputFeatureUsage<Quaternion> gazeRotation = new InputFeatureUsage<Quaternion>("gazeRotation");
        public SimXrPoseReader(InputDeviceCharacteristics characteristics, bool gaze = false)
        {
            this.characteristics = characteristics;
            this.gaze = gaze;
        }

        public SimPoseSample Read(Transform trackingOrigin)
        {
            // Rediscover after hot-plug and periodically detect newly ambiguous roles.
            if (SimClock.Now >= nextDiscovery || (matches.Count == 1 && !matches[0].isValid))
            {
                matches.Clear();
                InputDevices.GetDevicesWithCharacteristics(characteristics, matches);
                nextDiscovery = SimClock.Now + 0.5;
            }

            if (matches.Count == 0)
            {
                return SimPoseSample.Missing(TrackingAvailability.Unavailable);
            }

            if (matches.Count != 1)
            {
                return SimPoseSample.Missing(TrackingAvailability.Ambiguous);
            }

            InputDevice device = matches[0];
            if (!device.isValid || !device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) || !tracked)
            {
                return SimPoseSample.Missing(TrackingAvailability.Untracked);
            }

            if (!device.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState state) || (state & (InputTrackingState.Position | InputTrackingState.Rotation)) != (InputTrackingState.Position | InputTrackingState.Rotation))
            {
                return SimPoseSample.Missing(TrackingAvailability.Untracked);
            }

            if (!device.TryGetFeatureValue(gaze ? gazePosition : CommonUsages.devicePosition, out Vector3 position) || !device.TryGetFeatureValue(gaze ? gazeRotation : CommonUsages.deviceRotation, out Quaternion rotation))
            {
                return SimPoseSample.Missing(TrackingAvailability.InvalidData);
            }

            return SimPoseSample.FromLocal(SimClock.Now, position, rotation, trackingOrigin);
        }
    }
}
