# SIM UDP runtime package

This optional package separates the SIM wire protocol, packet history and UDP transport from the legacy Unity example scripts. It has no Unity, MuJoCo, XR or eye SDK dependency. Existing examples are retained unchanged.

Use the Packages/org.airob.sim subdirectory as a pinned UPM Git dependency or a package within a pinned repository submodule. Create SimUdpConnection(receivePort), drain TryReceive on the main thread, and dispose it on disable. Use a send-only connection for feedback. SimPacketCodec preserves numeric payload types and uses explicit little-endian encoding. History delays are zero-based; unavailable history returns null. History is single-owner and intended for the thread draining the socket queue. Receipt time and source time are distinct.

The bounded network queue reports invalid and dropped packet counts. Surface these in the host application; do not silently treat missing data as a fresh command. Task meanings, tracking providers, UI and CSV schemas remain outside this package. Encode currently sends double arrays; Decode accepts the protocol's supported numeric types. Empty and nonfinite payloads are rejected.

This feature is additive: clients can migrate without loading the legacy monolithic scripts. No change to the UDP wire layout is required.
