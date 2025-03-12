## Manual

### YOLOHandler

The built-in `YOLOHandler` can be used with any YOLO11 or YOLOv10 model version.

This table outlines the configurable options available from the Unity inspector.

| Field | Type | Description | Example |
| --- | --- | --- | --- |
| Model | `ModelAsset` | The YOLO Model to run | `yolo11n.onnx` |
| Input Size | `int` | The size of the input image to the model. This will be overwritten if the model has a fixed input size. | `640` |
| Layers Per Frame | `uint` | The number of model layers to run per frame. Increasing this value will decrease performance. | `10` |
| Confidence Threshold | `float` | The threshold at which a detection is accepted. | `0.5` |
| Class JSON | `TextAsset` | A JSON containing a mapping of class numbers to class names | A JSON containing COCO mappings from indexes to names |
| YOLOCamera | `VideoFeedManager` | The VideoFeedManager to analyse frames from. | An `IPCamera` object |
|Reference Camera|`Camera`|The base camera for scene analysis |A `Camera` component attatched to the `YOLOHandler` object|
|Display Manager|`ObjectDisplayManager`|The ObjectDisplayManager that will handle the spawning of digital double models.| An object containing an `ObjectDisplayManager` or subclass of `ObjectDisplayManager`. |

### IPCameraManager

The `IPCameraManager` can be used to connect to an IP Camera stream over IP, given the HTTP URL, including a specified port number if a non-standard HTTP port is used. Currently, only HTTP is supported.

| Field | Type | Description | Example |
| --- | --- | --- | --- |
|Image Url|`string`|The URL of the IP Camera's static image feed.|`http://ip:port/shot.jpg`|
|Download As Image|`bool`|Controls whether the stream is static images or a video stream such as MJPEG. This is not currently used.|`true`|
|Username|`string`|The username to use if the IP Camera requires Basic HTTP authentication.|`default`|
|Password|`string`|The password to use if the IP Camera requires Basic HTTP authentication.|`password`|

### PassthroughManager

The `PassthroughManager` wraps an existing `QuestScreenCaptureTextureManager` object from the [trev3d QuestDisplayAccessDemo GitHub repository](https://github.com/trev3d/QuestDisplayAccessDemo).

| Field | Type | Description | Example |
| --- | --- | --- | --- |
|Screen Capture Texture Manager|`QuestScreenCaptureTextureManager`|The `QuestScreenCaptureTextureManager` to wrap in this `PassthroughManager`.|The `QuestScreenCaptureTextureManager` from trev3d.|

### ObjectDisplayManager

The built-in `ObjectDisplayManager` can be used to display any defined models for any list of `DetectedObject` objects.

This table outlines the configurable options available from the Unity inspector.

| Field | Type | Description | Example |
| --- | --- | --- | --- |
|Max Model Count|`int`|The maximum number of models which can spawn at once.|`10`|
|Distance Threshold|`float`|The minimum distance from an existing model at which a model of the same class can spawn.|`0.5`|
|Model Objects|`Dictionary<string, GameObject>`|The names of the classes to detect and their associated models.||
|Moving Objects|`bool`|Use existing models when a new object is detected.|`false`|
|Scale Type|`ScaleType`|The scaling method to use:<br>`MIN`: Use the minimum of the x and y scale change.<br>`MAX`: Use the maximum of the x and y scale change.<br>`AVERAGE`: Use the average of both the x and y scale change.<br>`WIDTH`: Use the x scale change.<br>`HEIGHT`: Use the y scale change.|`MIN`|
|Video Feed Manager|`VideoFeedManager`|The VideoFeedManager used to capture input frames.|An `IPCamera` object|


