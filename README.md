# smoothNormalsToUV

A Unity editor tool that calculates smoothed normals and stores them in a custom UV channel without modifying the original mesh shading.

## Usage

1. Copy the `Assets` folder into your Unity project.
2. Select the objects you want to process in the hierarchy.
3. Open **Tools > Smooth Normals to UV** from the Unity menu.
4. Choose the target UV channel and whether the data should be normalized to the `[0,1]` range.
5. Click **Process Selected Meshes** to generate a cloned mesh with smoothed normals stored in the chosen UV channel.

The original shading normals remain untouched and the new mesh is assigned back to the selected objects.
