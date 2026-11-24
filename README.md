# SIM — Streamlined Input Manager

SIM provides small, reusable modules for receiving research input over UDP and observing XR poses and eye gaze. Application-specific control, task rules and recording stay in the consuming project.

The `dev` branch brings together the modular UDP runtime, optional XR acquisition and shared-code cleanup. The original loose-script examples remain available for existing integrations.

## Choose the modules you need

| Module | Purpose | Dependencies |
| --- | --- | --- |
| [SIM UDP Runtime](Packages/org.airob.sim/README.md) | Packet codec, bounded per-channel history and UDP transport | Standard C# libraries; no Unity, XR, eye SDK or MuJoCo API dependency |
| [SIM Research Tracking](Packages/org.airob.sim.tracking/README.md) | Read-only headset, controller, tracker and gaze samples | SIM UDP Runtime and Unity's XR module; a configured XR runtime for device input |
| [Legacy Unity examples](Unity) | Original combined input/logging component, sender and device-specific helpers | Depends on the scripts selected; see the legacy section below |
| [Python examples](Python) | UDP sending and receiving | See [pyproject.toml](Python/pyproject.toml) and each script's imports |

Unity's existing Tracked Pose Driver and XR Interaction Toolkit components own scene movement. SIM's tracking package observes devices: it never moves the XR Origin, headset, controllers or trackers, and never starts or stops XR. The packages do not impose a CSV schema, study protocol or logging location.

## Installation

The Unity packages declare Unity 6000.0 or newer. Integration has been checked in PAVE with Unity **6000.3.29f1**; this is the tested Editor version, not a claim that every Unity 6 release or headset has been validated.

### Install through Unity Package Manager

Use **Add package from Git URL** for the UDP runtime:

```text
https://github.com/AIROB-Lab/SIM---Streamlined-Input-Manager-UDP.git?path=/Packages/org.airob.sim#dev
```

If you also need XR acquisition, install the tracking package after installing the runtime:

```text
https://github.com/AIROB-Lab/SIM---Streamlined-Input-Manager-UDP.git?path=/Packages/org.airob.sim.tracking#dev
```

Install both URLs explicitly when using tracking. Its dependency declares the runtime package by name/version; that declaration does not supply the runtime's Git repository URL. UDP-only projects do not need the tracking package or a vendor SDK.

For reproducible research, replace `#dev` in both URLs with the same full commit SHA and retain the Unity package lock file. Following `dev` deliberately allows later updates.

### Use a repository checkout or submodule

```powershell
git clone --branch dev https://github.com/AIROB-Lab/SIM---Streamlined-Input-Manager-UDP.git
```

In Package Manager, use **Add package from disk** and select the required package's `package.json` under `Packages/`. Add the UDP runtime before tracking. A host project can instead pin this repository as a Git submodule and reference both package directories with relative paths, as PAVE does. Initialize submodules when cloning such a host project.

A repository clone alone does not install a Unity Editor, configure XR, or retrieve a vendor eye-tracking SDK. The loose scripts under `Unity/` are examples, not a complete Unity project to open.

## Receive UDP in Unity

Attach a small application-owned component such as this one. Choose a receive port that matches your sender; `11000` is only an example.

```csharp
using Sim;
using UnityEngine;

public sealed class SimInputExample : MonoBehaviour
{
    private SimUdpConnection connection;
    private readonly SimHistory history = new SimHistory { Capacity = 100 };

    private void OnEnable()
    {
        history.Clear(); // Do not reuse samples from an earlier recording session.
        connection = new SimUdpConnection(11000);
    }

    private void Update()
    {
        // Socket callbacks only queue packets. Consume them on the Unity main thread.
        while (connection.TryReceive(out SimPacket packet))
        {
            history.Add(packet);
        }

        SimPacket latest = history.Get(category: 1, subcategory: 0);
        if (latest == null || SimClock.Now - latest.ReceivedAtSeconds > 0.5)
        {
            return; // The application decides what to do when input is unavailable.
        }

        // Interpret latest.Values here using your application's channel contract.
        // Values retain their declared numeric types; a sample is not always a double.
    }

    private void OnDisable()
    {
        connection?.Dispose(); // Close the socket and discard queued packets.
        connection = null;
        history.Clear();
    }
}
```

Keep one owning thread for `SimHistory`, normally the main thread that drains the connection. `Get(..., samplesBack: 0)` selects the newest packet; `1` selects the previous packet. A time delay uses source timestamps and selects the nearest available sample to the requested time. Use either a sample delay or a time delay. Missing or insufficient history returns `null`; invalid delays throw. `ReadAll(category, subcategory, clear: true)` reads and drains a channel batch.

Inspect `InvalidPackets`, `DroppedPackets` and `LastError` on the connection in your application's diagnostics. The receive queue is bounded to 2048 packets. Missing input, an old receipt time and queue overflow must not be presented as fresh measurements.

`TimestampSeconds` is the sender-provided source time. `ReceivedAtSeconds` is host receipt time from `SimClock.Now`: UTC-anchored seconds advanced by a monotonic clock. These are different measurements; SIM does not synchronize clocks between computers. Use host receipt age for freshness checks, and preserve source time for analysis.

### Send UDP

A connection without a receive port is send-only. The caller supplies the source timestamp and packet counter:

```csharp
using (var sender = new Sim.SimUdpConnection())
{
    byte[] bytes = Sim.SimPacketCodec.Encode(
        new double[] { 0.25, -0.5 },
        category: 1, subcategory: 0,
        timestamp: Sim.SimClock.Now, counter: 0);
    sender.Send(bytes, "127.0.0.1", 11000);
}
```

Reuse and dispose a connection for continuous streaming rather than opening one per packet. Increment the counter in the sending application. Channel meanings, units and control behavior belong to the application, not the transport.

## Observe XR poses

Configure XR Plug-in Management and the appropriate OpenXR runtime and interaction profiles in the host project. Keep the existing rig's movement components. Create a reader for an explicit device role and call it on the main thread:

```csharp
var reader = new Sim.Tracking.SimXrPoseReader(
    UnityEngine.XR.InputDeviceCharacteristics.HeadMounted);

// Use the camera-offset transform that parents the tracked camera/controllers.
Sim.Tracking.SimPoseSample sample = reader.Read(trackingOrigin);
if (sample.IsValid)
{
    UnityEngine.Pose worldPose = sample.WorldPose;
    // Record or inspect the observation; do not use acquisition to drive the rig.
}
```

Create readers once and reuse them. Use `Controller | Left` or `Controller | Right` characteristics for the corresponding controller. Tracker roles need the host project's matching interaction profile and role characteristics; SIM does not assign Vive roles itself.

Samples contain local and world poses, host acquisition time and an explicit availability state. Missing, ambiguous, untracked and incomplete devices are invalid observations, not fresh zero poses. A role must resolve to exactly one device. Devices are rediscovered periodically and after disconnection.

Positions are in meters. Supply the actual tracking-to-world reference; a null origin explicitly means identity, not automatic origin discovery. World values reflect that reference at acquisition time. See the [tracking package guide](Packages/org.airob.sim.tracking/README.md) for coordinate-frame and timing details.

## Observe eye gaze

Attach `SimOpenXrGazeSource`, assign its `TrackingOrigin`, and enable Eye Gaze Interaction in the host project's OpenXR settings. Read `TryGetLatest(maximumAgeSeconds, out sample)` on the main thread. Disabled, invalid, future-dated or stale samples are rejected. No gaze-capable device means unavailable: head direction is never substituted for an eye measurement.

The generic source provides combined gaze when exposed by the runtime. Separate left/right validity is unspecified when not supplied. Headset/controller pose support does not imply that a device supports eye tracking; permissions, calibration and feature availability remain specific to the host and device.

A vendor provider can implement `SimGazeSource`. Native callbacks must transfer data to the main thread before publishing. The modular tracking package does not include SRanipal or a vendor SDK, does not promise a fixed eye-sampling rate, and does not change an existing HTC/SRanipal integration's origins or calibration.

## SIM UDP wire format

The modular runtime preserves the existing little-endian layout:

```text
count | type | category | subcategory | timestamp | values | counter
```

| Field | Byte offset | Size |
| --- | --- | --- |
| Number of values | 0 | 1 byte; 1–255 values |
| Numeric type | 1 | 1 byte |
| Category / subcategory | 2 / 3 | 1 byte each |
| Source timestamp | 4 | 8-byte double |
| Numeric values | 12 | count × numeric width |
| Packet counter | 12 + payload size | 2-byte unsigned integer |

The envelope is 14 bytes, plus the payload. In the type byte, the low four bits specify numeric width, `0x10` marks signed integers and `0x20` marks floating-point values. The decoder accepts float32/float64, signed integers of 1/2/4/8 bytes and unsigned integers of 1/2/4/8 bytes, preserving their boxed numeric types. The current encoder writes double arrays (`0x28`). Empty payloads, unsupported type codes, invalid lengths and nonfinite timestamps/values are rejected.

The [Python sender](Python/SIM_send.py) and [receiver](Python/SIM_recv.py) illustrate the application channel contract. Match destination address, port, numeric encoding and channel meanings when connecting another research tool.

## Existing loose-script integrations

The original scripts remain under `Unity/` for projects that already depend on their combined component API. Their `StreamlinedInputManager`, logging behavior, public static members and vendor assumptions are separate from the modular packages. Existing integrations can migrate module by module; importing the new packages does not automatically replace those components or migrate scenes and CSV consumers.

The legacy examples include OpenXR input, the HTC Vive tracker-profile helper and SRanipal eye acquisition. The original eye setup targeted **SRanipal 1.3.3.0** and Vive Pro Eye. Vendor plugins are not bundled here. `PubSensorData.cs` is a MuJoCo-specific example; MuJoCo is not a dependency of the modular runtime or tracking package. Import only the examples and vendor dependencies your project uses.

Earlier versions of this README reported HTC Vive Pro Eye and Meta Quest 3 use with the legacy solution. That historical report is not a new hardware validation of the modular packages or a guarantee of eye tracking on either device.

## Validation and remaining checks

The merged package code was compiled in PAVE with Unity 6000.3.29f1. Automated runtime checks cover packet round-trip, truncated-packet rejection, bounded sample/time history, unavailable history, invalid delay handling, batch draining, real UDP loopback, invalid-packet accounting and connection disposal. These checks were rerun on the consolidated code on **24 November 2026**. The small check harness lives outside this repository; this repository does not yet provide a packaged test suite or CI workflow.

PAVE's Windows build also packaged and loaded the native MuJoCo library, but that is a consuming-project check, not a SIM dependency or proof of hardware tracking. The recent no-headset runs do not validate Meta/HTC device behavior, tracker roles, gaze accuracy/rate, permissions or calibration. Those require the intended devices and runtime. Do not infer current hardware compatibility from successful compilation alone.

## Contributing

Build features on feature branches and merge reviewed, tested work into `dev`. Keep generic transport, history and acquisition here; keep task rules, actuator mappings, recording schemas and scientific outcomes in the consuming application. Prefer small, commented classes and explicit units, frames, timestamps and missing-data behavior. Preserve package `.meta` files and document changes that affect existing integrations.

## Authors and license

Developed by **Fabio Egle and Clark Bäker, AIROB Lab**. Distributed under the [MIT License](LICENSE).
