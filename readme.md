# YOLO Computer Vision on a Meta Quest 3 Headset

A Unity 6000.0.20f1 project at root. Custom scripts and assets are in two folders, YOLO and Scripts at the root of the Assets folder.

## Build instructions

### Requirements

- [Git LFS](https://git-lfs.com/)
- [Unity 6000.0.20f1](https://unity.com/releases/editor/whats-new/6000.0.20#installs) with Android Build Support
  - Note: Whilst it is possible to open the project in Unity Editor Version 6000.0.20f1 or *later*, it is not recommended as this can cause bugs. Proceed at your own risk.
- Windows
- Packages:
  - Listed in [Packages/manifest.json](Packages/manifest.json)
  - Necessary packages will be installed automatically by the Unity Editor

### Build steps

To build the project, first [clone](https://docs.github.com/en/repositories/creating-and-managing-repositories/cloning-a-repository) the repo using ssh:

    git clone git@github.com:AdmiralCasio/YOLOQuestUnity.git

Open the project in the Unity Editor - this may take some time.

Go to File/Build Profiles, make sure the target platform is set to Android, then click build.

Alternatively, you can use the command line:

### Test steps

List steps needed to show your software works. This might be running a test suite, or just starting the program; but something that could be used to verify your code is working correctly.

Examples:

* Run automated tests by running `pytest`
* Start the software by running `bin/editor.exe` and opening the file `examples/example_01.bin`

