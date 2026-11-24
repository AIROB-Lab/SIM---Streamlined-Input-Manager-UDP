using UnityEngine;

namespace Sim.Tracking
{
    /// <summary>Small provider boundary for research gaze. Providers observe, never drive a rig.
    /// Native SDK callbacks must transfer data to the main thread before publishing here.</summary>
    public abstract class SimGazeSource : MonoBehaviour
    {
        public SimGazeSample Latest { get; protected set; }
        public string Status { get; protected set; } = "Waiting for device";

        public bool TryGetLatest(double maximumAgeSeconds, out SimGazeSample sample)
        {
            sample = Latest;
            double age = SimClock.Now - sample.TimestampSeconds;
            return isActiveAndEnabled && sample.IsValid && maximumAgeSeconds > 0 &&
                age >= 0 && age <= maximumAgeSeconds;
        }

        protected virtual void OnDisable()
        {
            Latest = default;
            Status = "Disabled";
        }
    }
}
