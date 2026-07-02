using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ProceduralTrackGenerator : MonoBehaviour
{
    public GameObject waypointPrefab;
    public float trackRadius = 20.0f;
    public int segments = 100;
    public int sides = 16;
    
    private List<Transform> waypoints = new List<Transform>();

    void Start()
    {
        GenerateTrack();
    }

    private void GenerateTrack()
    {
        List<Vector3> splinePoints = new List<Vector3>();
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float z = t * 1000f; // Unity is left-handed, +Z is forward
            float y = Mathf.Sin(t * Mathf.PI * 4f) * 50f;
            float x = Mathf.Cos(t * Mathf.PI * 2f) * 50f;
            
            splinePoints.Add(new Vector3(x, y, z));
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < segments; i++)
        {
            Vector3 p0 = splinePoints[i];
            Vector3 p1 = splinePoints[i + 1];

            Vector3 forward = (p1 - p0).normalized;
            Vector3 arbitrary = Vector3.up;
            if (Mathf.Abs(forward.y) > 0.99f) arbitrary = Vector3.right;

            Vector3 right = Vector3.Cross(arbitrary, forward).normalized;
            Vector3 up = Vector3.Cross(forward, right).normalized;

            for (int j = 0; j <= sides; j++)
            {
                float angle = ((float)j / sides) * Mathf.PI * 2f;
                float cosA = Mathf.Cos(angle);
                float sinA = Mathf.Sin(angle);

                Vector3 offset = (right * cosA * trackRadius) + (up * sinA * trackRadius);
                Vector3 vertPos = p0 + offset;
                
                // Track is inside the tube, so normal points towards the center (negative offset)
                Vector3 normal = -offset.normalized;

                vertices.Add(vertPos);
                normals.Add(normal);
                uvs.Add(new Vector2((float)j / sides, (float)i / segments));
            }

            if (i < segments - 1)
            {
                int row1 = i * (sides + 1);
                int row2 = (i + 1) * (sides + 1);

                for (int j = 0; j < sides; j++)
                {
                    triangles.Add(row1 + j);
                    triangles.Add(row2 + j);
                    triangles.Add(row1 + j + 1);

                    triangles.Add(row1 + j + 1);
                    triangles.Add(row2 + j);
                    triangles.Add(row2 + j + 1);
                }
            }

            // Spawn Waypoints
            if (waypointPrefab != null && i % 5 == 0)
            {
                GameObject wpObj = Instantiate(waypointPrefab, p0, Quaternion.LookRotation(forward, up), transform);
                waypoints.Add(wpObj.transform);
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh; // Collision for the tube
    }

    public List<Transform> GetWaypoints()
    {
        return waypoints;
    }
}
