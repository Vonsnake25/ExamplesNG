# BallisticNG Code Mod Examples
**These examples are for BallisticNG 1.3 and above. These will not work on previous versions of the game**

For more information, see the documentation here: https://ballisticng-documentation.readthedocs.io/en/latest/code_mods/getting_started.html

## Binary Folders
The binary folders contain the ready to use mods, including any assets that you need to use them.

**The assets inside the NGA files and the tool to build them are shipped with the game's Unity Tools package. You will need a copy of the game for source access to these assets.**

### Install
* Click the green code button at the top right of this repository and then click **Download ZIP**
* Navigate to your game's install folder, go into the **User** folder, then the **Mods** folder and then finally the **Code Mods** folder
* In the ZIP file go into the Binraries folder
* Extract the folder for the mod you want into the **Code Mods** folder
* Launch BallisticNG
* Navigate to the **Manage Mods** menu
* Set the mod state to **Enabled** and then restart the game

## Source Folders
The source folders contain the source code to each mod. These are just the .cs files and you will need to create the visual studio solution and project yourself.

**Use .NET 4.7 for your visual studio projects.**

**Also make sure you disable copy local for every dependency. Don't ship the dependencies with your code mods.**

### Dependencies
The only dependencies are .dll files that ship with BallisticNG. Everything you need can be found in **GameDir/BalllisticNG_Data/Managed.** You will want the following libraries:

* Assembly-CSharp.dll (all of the game's code compiled by Unity)
* BallisticSource.dll (contains the mod register class you need to get your mod into the game)
* BallisticUnityTools.dll (contains the Assets API)
* Every DLL with UnityEngine in the name.
