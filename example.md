# Examples

These examples are all based on Unity 6000.0.20f1

## Remote Object Detection and Display

### Importing YOLO Tools

To import the YOLO Tools toolkit, open the Unity Package Manager by selecting
Window > Package Manager from the top bar menu.


#### Installing From Disk

Select the `+` icon in the top left of the Package Manager window and select `Install package from disk...`.
Select the `package.json` file in the `YOLOTools` and click `Open` to install the package. This 
will install all necessary dependencies from the Unity Registry.

![example-17.png](docs/example-17.png)

#### Installing from Git

Select the `+` icon in the top left of the Package Manager window and select `Install package from git URL...`.
Enter the url of a git repository hosting the package ([feel free to use this one](https://github.com/matthewlyon23/YOLOTools)) and 
click `Install`. If the package is in a subdirectory of the repository, this can be specified with the [`path` query
parameter](https://docs.unity3d.com/6000.2/Documentation/Manual/upm-git.html#subfolder).

```
https://github.com/matthewlyon23/YOLOQuestUnity.git?path=/Assets/YOLOTools
```

![example-18.png](docs/example-18.png)

### Scene Setup

The following example shows how you might set up the project for the default
object detection and display use case. It shows every step required to create a project.

First, create a valid Unity XR scene. How you achieve this depends on your needs, but the
easiest way is to use the Mixed Reality template available in the Unity Hub.

![example-1.png](docs/example-1.png)

### Permissions

To use the toolkit, the relevant permissions must be obtained on the device. The list of permissions required is as follows:

- `com.oculus.permission.USE_ANCHOR_API`
- `com.oculus.permission.USE_SCENE`
- `horizonos.permission.HEADSET_CAMERA`
- `android.permission.CAMERA`

Note that other permissions may be required depending on your application.

The easiest way to guarantee that these permissions are requested is by adding to the AndroidManifest file.
This is located under the Assets/Plugins/Android folder and is named AndroidManifest.xml.

An example Android Manifest which contains the necessary permissions:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android" xmlns:tools="http://schemas.android.com/tools">
  <application android:label="@string/app_name" android:icon="@mipmap/app_icon" android:allowBackup="false">
    <!--Used when Application Entry is set to Activity, otherwise remove this activity block-->
    <activity android:name="com.unity3d.player.UnityPlayerGameActivity" android:theme="@style/UnityThemeSelector">
      <intent-filter>
        <action android:name="android.intent.action.MAIN" />
        <category android:name="android.intent.category.LAUNCHER" />
        <category android:name="com.oculus.intent.category.VR" />
      </intent-filter>
      <meta-data android:name="unityplayer.UnityActivity" android:value="true" />
      <meta-data android:name="com.oculus.vr.focusaware" android:value="true" />
    </activity>
    <meta-data android:name="com.oculus.handtracking.frequency" android:value="LOW" />
    <meta-data android:name="com.oculus.ossplash.background" android:value="passthrough-contextual" />
    <meta-data android:name="com.oculus.telemetry.project_guid" android:value="490203b2-885e-4cc4-a3bd-6b90031780d3" />
    <meta-data android:name="com.oculus.supportedDevices" android:value="quest3|quest3s" tools:replace="android:value" />
  </application>
  <uses-feature android:name="android.hardware.vr.headtracking" android:version="1" android:required="true" />
  <uses-feature android:name="oculus.software.handtracking" android:required="false" />
  <uses-permission android:name="com.oculus.permission.HAND_TRACKING" />
  <uses-permission android:name="com.oculus.permission.USE_ANCHOR_API" />
  <uses-feature android:name="com.oculus.feature.PASSTHROUGH" android:required="true" />
  <uses-permission android:name="com.oculus.permission.USE_SCENE" />
  <uses-permission android:name="horizonos.permission.HEADSET_CAMERA" />
  <uses-permission android:name="android.permission.CAMERA" />
</manifest>
```

### OVR and Passthrough Setup

Once you have a valid scene, you must add the OVRCameraRig and OVRPassthroughLayer components
from the Meta SDK. The easiest way to do this is via the building blocks menu. Simply drag the
Camera Rig and Passthrough building blocks into the project and use the Meta Setup Tool to ensure they are configured
correctly.

![example-2.png](docs/example-2.png)
![example-3.png](docs/example-3.png)

In the `OVRCameraRig` component, select the `Use Per Eye Cameras` option. In the `Quest Features` section
of the `OVRManager` component, ensure that Scene, Passthrough and Boundary Visibility Support are all set
to `Required`. Ensure `Enable Passthrough` and `Should Boundary Visibility Be Supressed` are selected.

![example-4.png](docs/example-4.png)

![example-5.png](docs/example-5.png)

### VideoFeedManager and Passthrough Capture

With Insight Passthrough set up and enabled, the YOLOTools components can now be added to the scene.

First, add a `VideoFeedManager` of your choice that will provide that textures which will be analysed
using YOLO. In this case, we will use the `WebCamTextureManager` component to get the 
feed from the Meta Quest front-facing cameras.

![example-6.png](docs/example-6.png)

Create an empty object by right-clicking in the scene hierarchy and selecting 
`Create Empty`. Name this object `PassthroughCameraManager`. Click `Add Component` in
the inspector for the object and search for `WebCamTextureManager` and add it to the object.
Ensure you also add the `PassthroughCameraPermissions` script in the same way.

![example-7.png](docs/example-7.png)

Finally, drag the `PassthroughCameraPermissions` component into the `PermissionsManager` field of the `WebCamTextureManager`
component. You can optionally select either the left or the right eye to use for Passthrough Capture.

![example-8.png](docs/example-8.png)

### YOLO Analysis

To perform YOLO analysis, you can use either the `YOLOHandler` component or the
`RemoteYOLOHandler` component depending on your needs. For this example,
we will use the `RemoteYOLOHandler`.

![example-9.png](docs/example-9.png)

To use the `RemoteYOLOHandler`, drag the `RemoteYOLOHandler` prefab into the scene hierarchy. Select
the `RemoteYOLOHandler` object in the scene hierarchy and fill in the `Remote YOLO Processor Address`,
`YOLO Format`, `YOLO Model` and `Use Custom Model` fields.

![example-10.png](docs/example-10.png)

Drag the `PassthroughCameraManager` object (or your `VideoFeedManager` of choice) into the `Video Feed Manager`
field of the `RemoteYOLOHandler`. 

![example-14.png](docs/example-14.png)

Click on the circle icon next to the `Reference Camera` field of the `RemoteYOLOHandler`
and select the `Camera` object you wish to use as the virtual camera in the scene. This should be the
camera most closely aligned with the real `VideoFeedManager` source. In this example, the `Left Eye Camera`
object of the OVRCameraRig is used.

![example-15.png](docs/example-15.png)

### Object Display

Right click in the scene hierarchy and select `Create Empty`. This time, name the object `ObjectDisplayManager`.
Click on the object to open the inspector, select `Add Component` and search for `ObjectDisplayManager` to add the `ObjectDisplayManager`
component.

![example-11.png](docs/example-11.png)

Use the same method to add an `EnvironmentRaycastManager` component from the Meta SDK to the `ObjectDisplayManager`
object.

Tweak the `ObjectDisplayManager` settings as desired. See the [manual](manual.md) for information on the configuration
options that are available.

Drag the `PassthroughCameraManager` object (or your `VideoFeedManager` object of choice) into the `Video Feed Manager` field of the `ObjectDisplayManager`.

![example-12.png](docs/example-12.png)

Finally, drag the `ObjectDisplayManager` into the `Object Display Manager` field of the `RemoteYOLOHandler`.

![example-16.png](docs/example-16.png)
