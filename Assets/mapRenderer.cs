using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class mapScript1: MonoBehaviour {
    [Header("Assign 6 Materials By Direction")]
    [Tooltip("0: Up (Y+)\n1: Down (Y-)\n2: Forward (Z+)\n3: Back (Z-)\n4: Right (X+)\n5: Left (X-)")]
    public Material[] materials = new Material[6];

    void Start() {
        ProcessMesh();
    }

    public void ProcessMesh() {

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Mesh originalMesh = meshFilter.sharedMesh;

        if (materials == null || materials.Length != 6) {
            Debug.LogError("You must assign exactly 6 materials to the 'materials' array in the Inspector.", this);
            return;
        }

        var submeshTriangles = new List<List<int>> {
            new List<int>(), new List<int>(), new List<int>(),
            new List<int>(), new List<int>(), new List<int>()
        };

        Vector3[] vertices = originalMesh.vertices;
        int[] triangles = originalMesh.triangles;

        for (int i = 0; i < triangles.Length; i += 3) {

            int index0 = triangles[i];
            int index1 = triangles[i + 1];
            int index2 = triangles[i + 2];

            Vector3 v0 = vertices[index0];
            Vector3 v1 = vertices[index1];
            Vector3 v2 = vertices[index2];

            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            int submeshIndex = GetSubmeshIndexFromNormal(normal);

            submeshTriangles[submeshIndex].Add(index0);
            submeshTriangles[submeshIndex].Add(index1);
            submeshTriangles[submeshIndex].Add(index2);
        }

        Mesh newMesh = new Mesh();
        newMesh.name = "ProcedurallyMapped_Mesh";

        newMesh.vertices = originalMesh.vertices;
        newMesh.uv = originalMesh.uv;
        newMesh.normals = originalMesh.normals;
        newMesh.tangents = originalMesh.tangents;

        newMesh.subMeshCount = 6;

        for (int i = 0; i < 6; i++) {
            newMesh.SetTriangles(submeshTriangles[i], i);
        }

        meshFilter.mesh = newMesh;

        meshRenderer.materials = materials;
    }

    private int GetSubmeshIndexFromNormal(Vector3 normal) {
        float absX = Mathf.Abs(normal.x);
        float absY = Mathf.Abs(normal.y);
        float absZ = Mathf.Abs(normal.z);

        if (absY > absX && absY > absZ) {
            return (normal.y > 0) ? 0 : 1;
        }

        else if (absX > absY && absX > absZ) {
            return (normal.x > 0) ? 4 : 5;
        }

        else {
            return (normal.z > 0) ? 2 : 3;
        }
    }
}