# smoothNormalsToUV

A Unity editor tool that calculates smoothed normals and stores them in a custom UV channel without modifying the original mesh shading.

## Usage

1. Copy the `Assets` folder into your Unity project.
2. Select the objects you want to process in the hierarchy.
3. Open **Tools > Smooth Normals to UV** from the Unity menu.
4. Choose the target UV channel and whether the data should be normalized to the `[0,1]` range.
5. Click **Process Selected Meshes** to generate a cloned mesh with smoothed normals stored in the chosen UV channel.

The original shading normals remain untouched and the new mesh is assigned back to the selected objects.

## Features

- Works on any selected GameObject with a **MeshFilter** component.
- Clones the source mesh so project assets stay untouched.
- Optional smoothing angle threshold for more control over which normals are averaged.
- Writes the computed normals into any UV channel you choose.
- Optionally normalizes the values to the `[0,1]` range for texture storage.

## Editor Window

Open **Tools > Smooth Normals to UV** to adjust settings:

1. Pick the target UV channel (e.g. UV2, UV3).
2. Toggle normalization to `[0,1]` if needed.
3. Enable an angle threshold if you only want to average similar directions.
4. Click **Process Selected Meshes** to create and assign the new mesh.

Each processed mesh gets a new asset name ending with `_SmoothNormals` and is automatically assigned back to the selected objects.

