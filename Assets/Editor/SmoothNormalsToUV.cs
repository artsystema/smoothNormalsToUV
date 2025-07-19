using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;

public class SmoothNormalsToUV : EditorWindow
{
    private int uvChannel = 1; // Default UV2 (index 1)
    private bool normalize = false;
    private bool useAngle = false;
    private float angleThreshold = 60f;
    private bool overwriteSource = false;

    [MenuItem("Tools/Smooth Normals to UV")]
    public static void ShowWindow()
    {
        GetWindow<SmoothNormalsToUV>(false, "Smooth Normals to UV", true);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Store Smoothed Normals", EditorStyles.boldLabel);

        uvChannel = EditorGUILayout.IntField(new GUIContent("Target UV Channel"), uvChannel);
        normalize = EditorGUILayout.Toggle(new GUIContent("Normalize 0-1"), normalize);
        useAngle = EditorGUILayout.Toggle(new GUIContent("Use Angle Threshold"), useAngle);
        if (useAngle)
        {
            angleThreshold = EditorGUILayout.FloatField(new GUIContent("Smoothing Angle"), angleThreshold);
        }
        overwriteSource = EditorGUILayout.Toggle(new GUIContent("Overwrite Source Asset"), overwriteSource);

        if (GUILayout.Button("Process Selected Meshes"))
        {
            ProcessSelection();
        }
        if (GUILayout.Button("Export Selected as FBX"))
        {
            ExportSelectionAsFbx();
        }
    }

    private void ProcessSelection()
    {
        string saveFolder = "Assets/ProcessedMeshes";
        if (!System.IO.Directory.Exists(saveFolder))
            System.IO.Directory.CreateDirectory(saveFolder);
        foreach (var go in Selection.gameObjects)
        {
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
                continue;

            Mesh mesh = mf.sharedMesh;
            Mesh newMesh = Instantiate(mesh);
            newMesh.name = mesh.name + "_SmoothNormals";

            Vector3[] verts = newMesh.vertices;
            Vector3[] norms = newMesh.normals;
            Vector3[] smoothed = new Vector3[norms.Length];

            var groups = BuildPositionGroups(verts);

            for (int i = 0; i < norms.Length; i++)
            {
                Vector3 sum = Vector3.zero;
                var idxs = groups[i];
                foreach (int j in idxs)
                {
                    if (!useAngle || Vector3.Angle(norms[i], norms[j]) <= angleThreshold)
                        sum += norms[j];
                }
                smoothed[i] = sum.normalized;
            }

            List<Vector3> uvData = new List<Vector3>(smoothed.Length);
            foreach (var n in smoothed)
            {
                if (normalize)
                    uvData.Add(n * 0.5f + Vector3.one * 0.5f);
                else
                    uvData.Add(n);
            }
            // Remove clamping, use entered value directly
            newMesh.SetUVs(uvChannel, uvData);

            if (overwriteSource)
            {
                string assetPath = AssetDatabase.GetAssetPath(mesh);
                if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".fbx") == false)
                {
                    // Overwrite mesh asset (not FBX)
                    EditorUtility.CopySerialized(newMesh, mesh);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[{go.name}] Overwrote source mesh asset: {assetPath}");
                }
                else
                {
                    Debug.LogWarning($"[{go.name}] Cannot overwrite mesh in FBX or non-asset mesh. Creating new asset instead.");
                    string newAssetPath = $"{saveFolder}/{newMesh.name}.asset";
                    AssetDatabase.CreateAsset(newMesh, newAssetPath);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[{go.name}] Stored smoothed normals in UV{uvChannel + 1} and saved as asset: {newAssetPath}");
                }
            }
            else
            {
                string assetPath = $"{saveFolder}/{newMesh.name}.asset";
                AssetDatabase.CreateAsset(newMesh, assetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[{go.name}] Stored smoothed normals in UV{uvChannel + 1} and saved as asset: {assetPath}");
            }
            mf.sharedMesh = newMesh;
        }
    }

    private void ExportSelectionAsFbx()
    {
        string saveFolder = "Assets/ProcessedMeshes";
        if (!System.IO.Directory.Exists(saveFolder))
            System.IO.Directory.CreateDirectory(saveFolder);
        foreach (var go in Selection.gameObjects)
        {
            string fbxPath = $"{saveFolder}/{go.name}_Processed.fbx";
            ModelExporter.ExportObject(fbxPath, go);
            Debug.Log($"Exported {go.name} as FBX: {fbxPath}");
        }
    }

    private Dictionary<int, List<int>> BuildPositionGroups(Vector3[] verts)
    {
        const float precision = 0.00001f;
        Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();
        for (int i = 0; i < verts.Length; i++)
        {
            int hash = HashPosition(verts[i], precision);
            if (!dict.TryGetValue(hash, out var list))
            {
                list = new List<int>();
                dict.Add(hash, list);
            }
            list.Add(i);
        }

        Dictionary<int, List<int>> result = new Dictionary<int, List<int>>();
        for (int i = 0; i < verts.Length; i++)
        {
            int hash = HashPosition(verts[i], precision);
            result[i] = dict[hash];
        }
        return result;
    }

    private int HashPosition(Vector3 v, float precision)
    {
        int x = Mathf.RoundToInt(v.x / precision);
        int y = Mathf.RoundToInt(v.y / precision);
        int z = Mathf.RoundToInt(v.z / precision);
        int hash = x * 73856093 ^ y * 19349663 ^ z * 83492791;
        return hash;
    }
}

