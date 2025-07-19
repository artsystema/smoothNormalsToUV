using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SmoothNormalsToUV : EditorWindow
{
    private int uvChannel = 1; // Default UV2 (index 1)
    private bool normalize = false;
    private bool useAngle = false;
    private float angleThreshold = 60f;

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

        if (GUILayout.Button("Process Selected Meshes"))
        {
            ProcessSelection();
        }
    }

    private void ProcessSelection()
    {
        foreach (var go in Selection.gameObjects)
        {
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
                continue;

            Mesh newMesh = Instantiate(mf.sharedMesh);
            newMesh.name = mf.sharedMesh.name + "_SmoothNormals";

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
            newMesh.SetUVs(uvChannel, uvData);
            mf.sharedMesh = newMesh;
            Debug.Log($"[{go.name}] Stored smoothed normals in UV{uvChannel + 1}");
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

