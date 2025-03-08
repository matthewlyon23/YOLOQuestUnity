# Bringing Object Detection into AR

Level 4 Computer Science dissertation project at the University of Glasgow.

This project seeks to explore the capabilities of the Quest 3 in running accurate and precise real-time object detection entirely onboard. The project also explores the use of digital doubling as a method of using object detection to manipulate the world around the user and create unique and tailored experiences.

## Features

The project features several components which provide functionality for a number of possible approaches. The program code is highly extensible and is designed to be extended and added to by developers wishing to customise the input sources, object detection models, inference methods or display rules.

- An abstract VideoFeedManager which can be subclassed to provide specific input sources. Currently, there are two built in input sources:
    - Screen Capture from the Quest 3 view (Passthrough)
    - An image feed from a webcam over IP (IP Camera)
        - Can be used with things like Raspberry Pi Cameras
- An abstract inference handler which can be subclasses to provide specific functionality for different needs. Currently includes:
    - A YOLO inference handler for YOLO11 which provides both immediate and layered inference control using Unity's Sentis API. It also includes the ability to specify the input size of the model for dynamic models.
- A texture analyser to run the inference specified by the inference handler on a Texture2D.
- A detected object class which provides information about an object identified by a model.
- An object display manager which handles the spawning of models associated objects, defined by a mapping available in the Unity inspector.
- A YOLO handler which runs inference, manages object display and manages post processing for YOLO11 models.

## Installation

### Full Project

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

### Incorporate into your own project

The easiest way to use the project is to start a new project using the Unity MR Template. This will ensure that all the necessary objects are in the scene.

There are also additional dependencies which must be satisfied to use certain components.

To ensure best compatibility, use the Meta Project Validation Tool to set all settings correctly. After this, ignore this tool as it incorrectly reports errors with the following settings.

ObjectDisplayManager:
- The scene must use the [OVR Camera Rig](https://developers.meta.com/horizon/documentation/unity/unity-ovrcamerarig/) from the Oculus plugin as the main camera.
- The XR Plugin must be set to Oculus.
- An EnvironmentRaycastManager from the [Meta MR Utility Kit](https://developer.oculus.com/documentation/unity/unity-meta-mr-utility-kit/) must be added to the object containing the ObjectDisplayManager

YOLOHandler:
- The object containing the YOLO Handler must also contain one Camera component (not any of the AR cameras).

PassthroughManager:
- The `allow unsafe code` option must be selected in the player settings.

## Contributing

Please follow these steps:

1. Fork the repository.
2. Create a new branch (`git checkout -b feature/feature-name`).
3. Make your changes.
4. Commit your changes (`git commit -m 'Add some feature'`).
5. Push to the branch (`git push origin feature-branch`).
6. Open a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contact

For any inquiries or feedback, please contact us at [lyonmatthew2003@hotmail.com](mailto:lyonmatthew2003@hotmail.com).

## Attribution

"Bike - Untextured" (https://skfb.ly/oMtoN) by Mert Tetik is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

"Low Poly 1950s Car" (https://skfb.ly/oI9IY) by Dezryelle is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

"Motorcycle" (https://skfb.ly/otWPH) by animanyarty is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

"1950's Styled Greyhound Bus" (https://skfb.ly/6YuSn) by GameDev Nick is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

"Traffic Light" (https://skfb.ly/6WnDx) by Lyskilde is licensed under Creative Commons Attribution-NonCommercial (http://creativecommons.org/licenses/by-nc/4.0/).

"Trail of the Wretched Asset - Bench" (https://skfb.ly/pqQtX) by J.Rea14 is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

"American shorthair cat Untextured" (https://skfb.ly/p8FYC) by Mud The Superdurty King is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

"Low Poly Dog" (https://skfb.ly/orIDQ) by gallacs is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

"Backpack" (https://skfb.ly/o6LKA) by irovetskiy is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

"Elegant umbrella" (https://skfb.ly/UnEo) by Araon is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

"Pok3r Simple model" (https://skfb.ly/MwHt) by catanddogsoup is licensed under Creative Commons Attribution-NonCommercial (http://creativecommons.org/licenses/by-nc/4.0/).

"Laptop" (https://skfb.ly/6RVFt) by Aullwen is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

"TV" (https://skfb.ly/oxAqG) by CN Entertainment is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).
