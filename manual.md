# Manual

Supports the Quest 3 and Quest 3S. To use the application, start it from the Quest application menu. Detection and model spawning will begin immediately.

## Configuration Options

The full configuration can be defined in the Unity inspector using the configuration options listed below. Select configuration options are available using a menu placed on the back of the controllers.

### YOLOHandler

The built-in `YOLOHandler` can be used with any YOLO11 or YOLOv10 model version.

This table outlines the configurable options available from the Unity inspector.

| Field                | Type                   | Description                                                                                             | Example                                                                               |
|----------------------|------------------------|---------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------|
| Model                | `ModelAsset`           | The YOLO Model to run                                                                                   | `yolo11n.onnx`                                                                        |
| Input Size           | `int`                  | The size of the input image to the model. This will be overwritten if the model has a fixed input size. | `640`                                                                                 |
| Layers Per Frame     | `uint`                 | The number of model layers to run per frame. Increasing this value will decrease performance.           | `10`                                                                                  |
| Confidence Threshold | `float`                | The threshold at which a detection is accepted.                                                         | `0.5`                                                                                 |
| Class JSON           | `TextAsset`            | A JSON containing a mapping of class numbers to class names                                             | A JSON containing COCO mappings from indexes to names                                 |
| YOLOCamera           | `VideoFeedManager`     | The VideoFeedManager to analyse frames from.                                                            | An `IPCamera` object                                                                  |
| Reference Camera     | `Camera`               | The base camera for scene analysis                                                                      | A `Camera` component attached to the `YOLOHandler` object                             |
| Display Manager      | `ObjectDisplayManager` | The ObjectDisplayManager that will handle the spawning of digital double models.                        | An object containing an `ObjectDisplayManager` or subclass of `ObjectDisplayManager`. |

### RemoteYOLOHandler

The built-in `RemoteYOLOHandler` can be used with any YOLO11 model or any custom model supported by Ultralytics YOLO and defined in a `.pt` file.

The `RemoteYOLOHandler` is designed to be used with the [remoteyolo](https://github.com/matthewlyon23/remoteyolo) server. Currently, only HTTP is supported.

This table outlines the configuration options available from the Unity inspector.

| Field                         | Type                   | Description                                                                                                                                                           | Example                                                                               |
|-------------------------------|------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------|
| Remote YOLO Processor Address | `string`               | The network address of the device running the [remoteyolo](https://github.com/matthewlyon23/remoteyolo) processing server.                                            | `remoteyolo.local`                                                                    |
| YOLO Format                   | `YOLOFormat`           | The format in which to run the YOLO model                                                                                                                             | `NCNN`                                                                                |
| YOLO Model                    | `YOLOModel`            | The YOLO model to use for analysis. Can only be selected if Use Custom Model is not selected.                                                                         | `YOLO11N`                                                                             |
| Custom Model                  | `TextAsset`            | The custom model to use. Must be a `.pt` file imported into the project, which will add a `.bytes` extension. Will only be available if Use Custom Model is selected. | `doors_and_windows.pt`                                                                |
| Use Custom Model              | `bool`                 | Whether or not a custom model should be uploaded for use.                                                                                                             | `true`                                                                                |
| Confidence Threshold          | `float`                | The threshold at which a detection is accepted.                                                                                                                       | `0.5`                                                                                 |
| YOLOCamera                    | `VideoFeedManager`     | The VideoFeedManager to analyse frames from.                                                                                                                          | A `WebCamTextureManager` object                                                       |
| Reference Camera              | `Camera`               | The base camera for scene analysis                                                                                                                                    | A `Camera` component attached to the `YOLOHandler` object                             |
| Object Display Manager        | `ObjectDisplayManager` | The ObjectDisplayManager that will handle the spawning of digital double models.                                                                                      | An object containing an `ObjectDisplayManager` or subclass of `ObjectDisplayManager`. |

### WebCamTextureManager

The WebCamTextureManager is a VideoFeedManager which exposes the Passthrough Camera API camera feed.

| Field                | Type                   | Description                                                                                                                                                                                                                                                           | Example |
|----------------------|------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------|
| Eye                  | `PassthroughCameraEye` | The eye from which to capture the passthrough feed.                                                                                                                                                                                                                   | `Left`  |
| Requested Resolution | `Vector2Int`           | The resolution to request from the headset passthrough. The requested resolution of the camera may not be supported by the chosen camera. In such cases, the closest available values will be used. When set to (0,0), the highest supported resolution will be used. | `(0,0)` |

### IPCameraManager

The `IPCameraManager` can be used to connect to an IP Camera stream over IP, given the HTTP URL, including a specified port number if a non-standard HTTP port is used. Currently, only HTTP is supported.

This table outlines the configurable options available from the Unity inspector.

| Field             | Type     | Description                                                                                               | Example                   |
|-------------------|----------|-----------------------------------------------------------------------------------------------------------|---------------------------|
| Image Url         | `string` | The URL of the IP Camera's static image feed.                                                             | `http://ip:port/shot.jpg` |
| Download As Image | `bool`   | Controls whether the stream is static images or a video stream such as MJPEG. This is not currently used. | `true`                    |
| Username          | `string` | The username to use if the IP Camera requires Basic HTTP authentication.                                  | `default`                 |
| Password          | `string` | The password to use if the IP Camera requires Basic HTTP authentication.                                  | `password`                |

#### PiCameraManager

The `PiCameraManager` is a built-in subclass of the `IPCameraManager` which provides specific functionality for a horizontally mounted [Pi Camera Module 3](https://www.raspberrypi.com/products/camera-module-3/), including rotating the image from the camera to match the orientation of the corresponding virtual camera.

### ObjectDisplayManager

The built-in `ObjectDisplayManager` can be used to display any defined models for any list of `DetectedObject` objects.

This table outlines the configurable options available from the Unity inspector.

| Field              | Type                             | Description                                                                                                                                                                                                                                                                       | Example              |
|--------------------|----------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------|
| Max Model Count    | `int`                            | The maximum number of models which can spawn at once.                                                                                                                                                                                                                             | `10`                 |
| Distance Threshold | `float`                          | The minimum distance from an existing model at which a model of the same class can spawn.                                                                                                                                                                                         | `0.5`                |
| Model Objects      | `Dictionary<string, GameObject>` | The names of the classes to detect and their associated models.                                                                                                                                                                                                                   |                      |
| Moving Objects     | `bool`                           | Use existing models when a new object is detected.                                                                                                                                                                                                                                | `false`              |
| Scale Type         | `ScaleType`                      | The scaling method to use:<br>`MIN`: Use the minimum of the x and y scale change.<br>`MAX`: Use the maximum of the x and y scale change.<br>`AVERAGE`: Use the average of both the x and y scale change.<br>`WIDTH`: Use the x scale change.<br>`HEIGHT`: Use the y scale change. | `MIN`                |
| Video Feed Manager | `VideoFeedManager`               | The VideoFeedManager used to capture input frames.                                                                                                                                                                                                                                | An `IPCamera` object |

## Usage

### Requirements

- [Git LFS](https://git-lfs.com/)
- [Unity 6000.0.20f1](https://unity.com/releases/editor/whats-new/6000.0.20#installs) with Android Build Support
  - Note: Whilst it is possible to open the project in Unity Editor Version 6000.0.20f1 or *later*, it is not recommended as this can cause bugs. Proceed at your own risk.
- Windows
- Packages:
  - Listed in [Packages/manifest.json](Packages/manifest.json)
  - Necessary packages will be installed automatically by the Unity Editor

### Installation

#### Full Project

1. Clone the repository:
    ```sh
    git clone https://github.com/AdmiralCasio/YOLOQuestUnity.git
    ```
2. Open the project in Unity:
    - Launch Unity Hub.
    - Click on "Add" and select the cloned project folder.
    - Open the project.
    - Dependencies will be installed automatically

3. Build for Android from the Build Profile menu:
    - Select File > Build Profile
    - Select Android
    - Ensure that the Main Scene is selected in the Scene List
    - Select Build

#### Incorporate into your own project

The easiest way to use the project is to start a new project using the Unity MR Template. This will ensure that all the necessary objects are in the scene.

There are also additional dependencies which must be satisfied to use certain components.

To ensure best compatibility, use the Meta Project Validation Tool to set all settings correctly. After this, ignore this tool as it incorrectly reports errors with the following settings.

ObjectDisplayManager:
- The scene must use the [OVR Camera Rig](https://developers.meta.com/horizon/documentation/unity/unity-ovrcamerarig/) from the Oculus plugin as the main camera.
- The XR Plugin must be set to Oculus, and Oculus XR must be the only installed XR Plugin.
- An EnvironmentRaycastManager from the [Meta MR Utility Kit](https://developer.oculus.com/documentation/unity/unity-meta-mr-utility-kit/) must be added to the object containing the ObjectDisplayManager

YOLOHandler:
- The object containing the YOLO Handler must also contain one Camera component (not any of the AR cameras).

RemoteYOLOHandler:
- The object containing the Remote YOLO Handler must also contain one Camera component (not any of the AR cameras).

PassthroughManager:
- The `allow unsafe code` option must be selected in the player settings.

### Object Models

Object Models are attached to the corresponding YOLO detection via the Model Object dictionary in the Object Display Manager.

The object model is placed with its centre (i.e. the (0,0,0) coordinate) at the centre of the YOLO detection. Therefore, 3D models should be aligned such that the point on the model which should spawn at the centre of the detection is at the zero coordinate.

### Prefabs

The project provides prefabs for the instantiation of both the [RemoteYOLOHandler](Assets/YOLO/RemoteYOLOHandler.prefab) and the [YOLOHandler](Assets/YOLO/YOLOHandler.prefab), including all necessary dependencies.

Simply drag these into the scene to use them.

### Custom Models

The [RemoteYOLOHandler](Assets/YOLO/Scripts/RemoteYOLOHandler.cs) and the [remoteyolo](https://github.com/matthewlyon23/remoteyolo) project support `.pt` YOLO model files created directly from the [ultralytics](https://github.com/ultralytics/ultralytics) Python module, based on any base YOLO model supported by Ultralytics.

Simply import a `.pt` extension model into the project to automatically convert it into a format which can be attached to the Custom Model field of the RemoteYOLOHandler.

#### Training Custom Models

To train a custom model, a custom dataset is needed. YOLO datasets consist of two folders, images and labels. Each folder must contain a train and val folder. These folders contain images and their corresponding text file (.txt), with one text file per image.

Labels for this format should be exported to YOLO format with one *.txt file per image. If there are no objects in an image, no *.txt file is required. The *.txt file should be formatted with one row per object in class x_center y_center width height format. Box coordinates must be in normalized xywh format (from 0 to 1). If your boxes are in pixels, you should divide x_center and width by image width, and y_center and height by image height. Class numbers should be zero-indexed (start with 0).

Finally, the data.yaml file must be placed at the top-level of the dataset, describing the locations of the train and val images and the class ID to name mappings.

Below is an example of a dataset YAML file.

```yaml
# Ultralytics 🚀 AGPL-3.0 License - https://ultralytics.com/license

# COCO8 dataset (first 8 images from COCO train2017) by Ultralytics
# Documentation: https://docs.ultralytics.com/datasets/detect/coco8/
# Example usage: yolo train data=coco8.yaml
# parent
# ├── ultralytics
# └── datasets
#     └── coco8 ← downloads here (1 MB)

# Train/val/test sets as 1) dir: path/to/imgs, 2) file: path/to/imgs.txt, or 3) list: [path/to/imgs1, path/to/imgs2, ..]
path: coco8 # dataset root dir
train: images/train # train images (relative to 'path') 4 images
val: images/val # val images (relative to 'path') 4 images
test: # test images (optional)

# Classes
names:
  0: person
  1: bicycle
  2: car
  3: motorcycle
  4: airplane
  5: bus
  6: train
  7: truck
  8: boat
  9: traffic light
  10: fire hydrant
  11: stop sign
  12: parking meter
  13: bench
  14: bird
  15: cat
  16: dog
  17: horse
  18: sheep
  19: cow
  20: elephant
  21: bear
  22: zebra
  23: giraffe
  24: backpack
  25: umbrella
  26: handbag
  27: tie
  28: suitcase
  29: frisbee
  30: skis
  31: snowboard
  32: sports ball
  33: kite
  34: baseball bat
  35: baseball glove
  36: skateboard
  37: surfboard
  38: tennis racket
  39: bottle
  40: wine glass
  41: cup
  42: fork
  43: knife
  44: spoon
  45: bowl
  46: banana
  47: apple
  48: sandwich
  49: orange
  50: broccoli
  51: carrot
  52: hot dog
  53: pizza
  54: donut
  55: cake
  56: chair
  57: couch
  58: potted plant
  59: bed
  60: dining table
  61: toilet
  62: tv
  63: laptop
  64: mouse
  65: remote
  66: keyboard
  67: cell phone
  68: microwave
  69: oven
  70: toaster
  71: sink
  72: refrigerator
  73: book
  74: clock
  75: vase
  76: scissors
  77: teddy bear
  78: hair drier
  79: toothbrush
```

Further details on training with Ultralytics YOLO can be found [here](https://docs.ultralytics.com/modes/train/) and dataset format can be found [here](https://docs.ultralytics.com/datasets/detect/)
