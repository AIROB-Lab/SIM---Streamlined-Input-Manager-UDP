# Optional SIM research tracking

This package observes XR poses and eye gaze. It never writes to the XR Origin, headset, controller or tracker transforms, and never starts/stops the XR runtime. Keep Unity's Tracked Pose Driver and XR Interaction Toolkit in charge of movement and interaction. Install this package only in projects that need acquisition; `org.airob.sim` remains independent of Unity/XR.

## Pose acquisition

Create a `SimXrPoseReader` with head-mounted, left/right controller, or explicit tracker-role characteristics, then call `Read(trackingOrigin)` on the Unity main thread. Assign the XR Origin's camera-offset transform, the parent space of the tracked camera/controllers. Null explicitly means identity; it does not discover the origin for you. Results include tracking-local and world poses in meters, host acquisition time, and availability. Missing, ambiguous, untracked and incomplete poses are invalid, never fresh zero/previous poses. Devices are rediscovered every 0.5 seconds and after disconnection. Custom Vive role bits require the host project's tracker interaction profile; this package does not invent roles.

## Gaze acquisition

Attach `SimOpenXrGazeSource` and assign the same tracking origin. Enable Eye Gaze Interaction in the host project's OpenXR settings. `TryGetLatest(maximumAgeSeconds, out sample)` rejects disabled, invalid, future-dated and stale samples. No gaze-capable device means unavailable; there is no fallback to head direction. Combined gaze does not imply separate left/right validity: those fields are null when not exposed. A gaze provider for another SDK implements the small `SimGazeSource` boundary. Vendor SDKs, consent/permissions and calibration remain the host application's responsibility.

Local frame identifiers are explicit: OpenXR uses `tracking_origin`; a vendor eye source may use `head_local`. Rays are Unity left-handed, normalized, with origins in meters. Host time is UTC-anchored monotonic `SimClock.Now`, not a device-native sample time. World poses/rays reflect the origin transform at host processing time. Do not treat this as a prediction-corrected or externally synchronized measurement.

Keep logging sinks, filenames, task labels and regions of interest in the application. The package does not impose a CSV schema, scientific protocol, MuJoCo or a vendor eye SDK.
