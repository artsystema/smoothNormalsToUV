using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;

public class SmoothNormalsToUV : EditorWindow
{
    private int normalChannel = 5; // Default to UV set 5 (index 4)
    private bool normalize = false;
    private bool useAngle = false;
    private float angleThreshold = 60f;
    private bool overwriteSource = false;
    private GUIStyle indicatorStyle;

    private static readonly List<Vector3> uvCheckList = new List<Vector3>();

    [MenuItem("Tools/Smooth Normals to UV")]
    public static void ShowWindow()
    {
        GetWindow<SmoothNormalsToUV>(false, "Smooth Normals to UV", true);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Store Smoothed Normals", EditorStyles.boldLabel);

        normalChannel = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Normal Channel"), normalChannel), 1, 8);
        DrawChannelIndicator();
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
        int channelIndex = Mathf.Clamp(normalChannel - 1, 0, 7);
        int channelDisplay = channelIndex + 1;

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
            // Store normals in the desired channel
            newMesh.SetUVs(channelIndex, uvData);

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
                    Debug.Log($"[{go.name}] Stored smoothed normals in UV{channelDisplay} and saved as asset: {newAssetPath}");
                }
            }
            else
            {
                string assetPath = $"{saveFolder}/{newMesh.name}.asset";
                AssetDatabase.CreateAsset(newMesh, assetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[{go.name}] Stored smoothed normals in UV{channelDisplay} and saved as asset: {assetPath}");
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

    private void DrawChannelIndicator()
    {
        if (indicatorStyle == null)
        {
            indicatorStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true
            };
        }

        int channelIndex = Mathf.Clamp(normalChannel - 1, 0, 7);
        bool hasChannel = true;
        bool meshFound = false;

        foreach (var go in Selection.gameObjects)
        {
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
                continue;

            meshFound = true;

            if (!MeshHasUVChannel(mf.sharedMesh, channelIndex))
            {
                hasChannel = false;
                break;
            }
        }

        if (!meshFound)
            hasChannel = false;

        string color = hasChannel ? "#00AA00" : "#AA0000";
        string status = hasChannel ? "present" : "absent";
        EditorGUILayout.LabelField($"<color={color}>●</color> Normal channel {status}", indicatorStyle);
    }

    private bool MeshHasUVChannel(Mesh mesh, int channelIndex)
    {
        if (channelIndex < 0 || channelIndex > 7)
            return false;
        uvCheckList.Clear();
        mesh.GetUVs(channelIndex, uvCheckList);
        return uvCheckList.Count > 0;
    }
}
