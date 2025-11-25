# AsembSim Lib
This small library contains the classes used to compile and run AsembSim 8-bit assembly code.

You can get AsembSim here: https://ali107.itch.io/asembsim

## How to use Library
The following instructions shows how to use the library on Windows with Visual Studio 2022. The library should work on non-windows platforms, though it is untested.
There are two ways to use this library, please choose one.
### Steps using source code
- Clone this repository through this command `git clone https://github.com/AAli107/AsembSim-Lib.git` or use any GUI-based source control software like GitHub Desktop.
- In your solution/project within Visual Studio, right-click on your solution within solution explorer and go to `Add > Existing Project...`.
- Navigate and select the `AsembSimLib.sln` file within the cloned repo folder.
- Now right-click on your project within the solution explorer, and go to `Add > Project Reference...` and make sure the checkbox next to "AsembSimLib" is ticked.
### Steps using compiled binary
- Clone this repository through this command `git clone https://github.com/AAli107/AsembSim-Lib.git` or use any GUI-based source control software like GitHub Desktop.
- Open the cloned solution and build the library. (On windows, you should get a .dll file within `bin/[Release/Debug]/net6.0/AsembSimLib.dll`)
- Right-click your project within the solution explorer, and go to `Add > Project Reference...`.
- Click the "Browse" button and navigate to the library file and select it.
- Once selected, it should be listed in the Reference Manager, make sure that the checkbox next to the name is checked.

