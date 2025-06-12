<h2 id="header-overview">Overview</h2>

[What Is It](#header-what-is-it)

[Important Information](#header-important-information)

[Setup](#header-setup)

[Getting Started](#header-getting-started)

[Examples](#header-examples)

[SIM-UDP Message Protocol](#header-protocol)

[Authors and Acknowledgement](#header-authors)

[License](#header-license)

<h2 id="header-what-is-it">:information_source: What It Is</h2>

The **Streamlined Input Manager (SIM)** for Unity can be used whenever you want to **access and/or log input data** from your **VR devices** or an User Datagram Protocol (**UDP**) sender sending to you.\
Information that can be accessed and logged in particular:
1. Momentary **position** and **rotation** of
    * VR HMD
    * Left Controller
    * Right Controller
    * External Vive Trackers
2. **Eyetracking** Data of the **Vive Eye Pro** Headset (including gaze direction, eye openness, etc.)
3. **Any information** you want to receive **over UDP**

<h2 id="header-important-information">:bangbang: Important Information</h2>

All development has been performed on a VIVE Pro Eye HMD together with VIVE Controllers and External Trackers which comes with some limitations. For the Unity editor version 2021.3.22
* For the tracking of the VR HMD and the VR Controllers [OpenXR](https://www.khronos.org/openxr/) is used such that **any OpenXR compatible HMD and Controller can be used** instead of the VIVE devices.
* The tracking of the external VIVE Trackers requires a [C#-script](https://forum.unity.com/threads/openxr-and-openvr-together.1113136/#post-7803057) developed by the community. At the moment the **SIM is only working for VIVE Trackers** and only when the script is included in the project.
* The Eyetracking uses the SRanipal Framework for VIVE eytracking devices. The only version of the necessary SDK working with the SIM is **1.3.3.0**. This enables us to access the **eyetracking data** on a seperate thread **at a sampling rate of around 120Hz**. However, this **limits the SIM to eyetracking of the VIVE Eye Pro**.

<h2 id="header-setup">:wrench: Setup</h2>

Include the SIM into an **existing project** of yours

Required Packages and Scripts:
* Packages
    * **OpenXR Plugin**
        * Enables use of OpenXR standard to access XR-platforms and -devices.
    * (**XR Interaction Toolkit** (including import of "**Starter Assets**" sample))
        * Provides a framework that makes 3D and UI interactions available from Unity input events.
    * **SRanipal**
        * Enables us to use the Eyetracking
* Scripts
    * **StreamlinedInputManager.cs**
        * Component providing the reading access to the desired input information.
    * **Logging.cs**
        * Background script with functionality to log the desired input information for future analysis. 
    * **HTCViveTrackerProfile.cs**
        * Background script enabling the tracking of external VIVE trackers.

### Include SIM into an Existing Project
1. **Include** necessary **scripts** into your project
    * SIM, Logging
    * (optional) HTCViveTrackerProfile
2. **Attach SIM component** to an empty or existing GameObject
3. In the Package Manager, **import Packages**:
    * OpenXR Plugin
    * XR Interaction Toolkit (including import of "Starter Assets" sample)
4. **Activate OpenXR** in Project Setting -> XR Plug-in Management (Caution: Do not fix the Pose Control error that might show up, because it will break the tracking of the Vive trackers)
5. Under Project Setting -> XR Plug-in Management -> OpenXR, **add interaction profiles** for your device (e.g. HTC Vive Controller Profile, HTC Vive Tracker Profile)
6. Download the **1.3.3** version of the [VIVE Eye and Facial Tracking SDK](https://developer.vive.com/resources/vive-sense/eye-and-facial-tracking-sdk/download/archive/1_3_3_0/)
7. Include the SDK plugin (Assets -> Import Package -> Custom Package) that can be found in your download under SDK-v1.3.3.0/SDK/02_Unity/Vive-SRanipal-Unity-Plugin.unitypackage

<h2 id="header-getting-started">:airplane: Getting Started</h2>

After Setup it is now time to make use of the SIM. In the screenshot below you see how the component for the SIM looks inside Unity's "Inspector" window:

![SIM_component](media/SIM%20component.jpg)

Let's go through the options there one by one:
1. **Activate Logging**
    * When checked, all input that is selected to be received (UDP) or tracked (XR) is logged and written into .csv files located in a /data folder in your project's root directory.
2. **Receive UDP** + **Port**
    * When checked, input that arrives over UDP at the selected port is received (and potentially logged) by the SIM.
3. **Track Eyes**, **Track Head**, **Track Left Controller**, **Track Right Controller**
    * When checked, input from the HMD, the left controller, the right controller and/or the VIVE Pro Eye eyetracking framework is tracked (and potentially logged).
4. **Used Trackers**
    * List containing an element for each possible SteamVR tracker location.
    * If you want to track (and potentially log) the input of an external VIVE tracker, the *used* checkbox needs to be checked for the element with the specific tracker position you want to use.

After you have checked the necessary for your project, you can now start your application and access what you are tracking/receiving from anywhere in your project. :sparkles: \
In general there is one `public static` member of the SIM that you can access from anywhere in your project to retain the input information you are looking for. \
These members are called:
* headTracker
* leftControllerTracker
* rightControllerTracker
* eyeTracker
* trackerTrackers
* udpReceiver

Following is a list containing accessible information for every member:
1. **headTracker**
    * Timestamp (property): Unix timestamp generated using the [Stopwatch](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch?view=net-7.0) class.
    * Position (property): Position (Vector3) of the HMD in the real world coordinate system (left-handed, same as Unity).
    * Rotation (property): Rotation (Quaternion) of the HMD.
2. **leftControllerTracker**
    * Timestamp (property): Unix timestamp generated using the [Stopwatch](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch?view=net-7.0) class.
    * Position (property): Position (Vector3) of the left controller in the real world coordinate system (left-handed, same as Unity).
    * Rotation (property): Rotation (Quaternion) of the left controller.
3. **rightControllerTracker**
    * Timestamp (property): Unix timestamp generated using the [Stopwatch](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch?view=net-7.0) class.
    * Position (property): Position (Vector3) of the right controller in the real world coordinate system (left-handed, same as Unity).
    * Rotation (property): Rotation (Quaternion) of the right controller.
4. **eyeTracker**
    * Timestamp (property): Unix timestamp generated using the [Stopwatch](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch?view=net-7.0) class.
    * EyeData (property): EyeData instance of the ViveSR.anipal.Eye.EyeData struct. Contains all eyetracking information available.
5. **trackerTrackers** \
**This member is a Dictionary** with as many key-value pairs as you are tracking external trackers (selected in the inspector). To access a certain tracker and its input information **you first have to specify the tracker using a string-key (e.g. "TrackerRightElbow", "TrackerLeftShoulder", etc.)**. The resulting value then has the same properties as the HMD and the controllers.
    * Timestamp (property): Unix timestamp generated using the [Stopwatch](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch?view=net-7.0) class.
    * Position (property): Position (Vector3) of the specified tracker in the real world coordinate system (left-handed, same as Unity).
    * Rotation (property): Rotation (Quaternion) of the specified tracker.

6. **udpReceiver**
* getData(byte category, byte subcategory) (method): Using this method you can retain the last data of a particular data (category, subcategory)-pair that has been received. The method returns an object array that already contains the data as the datatype you want your data to have. How the encoding/decoding of udp messages works is described in [SIM-UDP Message Protocol](#header-protocol).

Let's see some examples on accessing input information next
<h2 id="header-examples">:flashlight: Examples</h2>

#### UDP
![UDP_Example](media/Screenshot%202023-08-07%20130124.jpg)

#### Eye Tracking
![Eye-Tracking-Example](media/Screenshot%202023-08-07%20125907.jpg)

#### Trackingof External Vive Trackers
![Tracker-Tracking-Example](media/Screenshot%202023-08-07%20131403.jpg)

<h2 id="header-protocol">:scroll: SIM-UDP Message Protocol</h2>

When sending UDP messages to the port you selected in the inspector, you have to stick to a defined **message protocol**. \
One **UDP message is** essentially a **byte array**. To make sense of the bytes we have come up with an **contract every message has to stick to**. \
No matter what kind of **numeric** data you send, your messages (byte arrays) are always looking like this: \
**Number_of_data_points** | **Datatype_of_data_points** | **Category_of_data** | **Subcategory_of_data** | **Timestamp** | **Datapoints** | **Counter** \
The message **always** consists of these different sections which are explained below. The only part of the message varying in size is the Datapoints section.
* **Number_of_data_points** (message[0]) : 1 byte saying how many values are sent in this message
* **Datatype_of_data_points** (message[1]): 1 byte saying which datatype is sent
    * The byte carries following information: 
        1. How many bytes (between 1 and 8) is each number (----XXXX)
        2. Is the datatype signed(1) or unsigned(0) (---X----)
        3. Is the datatype floating point(1) or integer(0) (--X-----)
* **Category_of_data** (message[2]): 1 byte specifying the category the data (e.g. EMG data)
* **Subcategory_of_data** (message[3]): 1 byte saying which sub-category the data is (e.g. raw EMG data)
* **Timestamp** (message[4 to 11]): 8 bytes corresponding to one double which holds the timestamp of the datavalues sent in this message
* **Datapoint** (message[5 to (-3)]): All the datapoints you are sending.
* **Counter** (message[(-2) to (-1)]): Last 2 bytes corresponding to an UInt16 value holding a counter. This counter is incremented with every sent message to see if a message has been lost while sending it.

This protocol gives you the flexibility but also forces you to come up with your own categories and sub-categories and their corresponding encodings.

<h2 id="header-authors">:seedling: Authors and Acknowledgement</h2>

This software was developed by:

Fabio Egle & Clark Bäker, AIROB Lab