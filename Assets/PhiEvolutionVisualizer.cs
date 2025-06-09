using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Example usage for visualization
public class PhiEvolutionVisualizer : MonoBehaviour
{
    public PhiEvolutionReader phiReader;
    public MeshFilter meshFilter;
    public Gradient colorGradient;
    
    private Mesh surfaceMesh;
    private int currentTimeStep = 0;
    private float animationSpeed = 50f; // time steps per second
    
    void Start()
    {
        // Load data for D=1.0
        phiReader.LoadPhiEvolution(1.0f);
        
        // Create mesh
        CreateSurfaceMesh();
    }
    
    void CreateSurfaceMesh()
    {
        surfaceMesh = new Mesh();
        
        // Create vertices
        Vector3[] vertices = new Vector3[201 * 101];
        Vector2[] uvs = new Vector2[201 * 101];
        
        for (int i = 0; i < 201; i++)
        {
            for (int j = 0; j < 101; j++)
            {
                int idx = i * 101 + j;
                
                // Position based on grid
                float x = Mathf.Lerp(-2f, 2f, i / 200f);  // V axis
                float z = Mathf.Lerp(0f, 1f, j / 100f);   // n axis
                vertices[idx] = new Vector3(x, 0, z);
                
                uvs[idx] = new Vector2(i / 200f, j / 100f);
            }
        }
        
        // Create triangles
        int[] triangles = new int[(200 * 100) * 6];
        int t = 0;
        
        for (int i = 0; i < 200; i++)
        {
            for (int j = 0; j < 100; j++)
            {
                int v0 = i * 101 + j;
                int v1 = (i + 1) * 101 + j;
                int v2 = i * 101 + (j + 1);
                int v3 = (i + 1) * 101 + (j + 1);
                
                triangles[t++] = v0;
                triangles[t++] = v1;
                triangles[t++] = v2;
                
                triangles[t++] = v1;
                triangles[t++] = v3;
                triangles[t++] = v2;
            }
        }
        
        surfaceMesh.vertices = vertices;
        surfaceMesh.uv = uvs;
        surfaceMesh.triangles = triangles;
        
        meshFilter.mesh = surfaceMesh;
    }
    
    void Update()
    {
        // Animate through time
        float timeProgress = Time.time * animationSpeed;
        currentTimeStep = Mathf.FloorToInt(timeProgress) % phiReader.nTimeSteps;
        
        UpdateSurfaceHeights();
    }
    
    void UpdateSurfaceHeights()
    {
        Vector3[] vertices = surfaceMesh.vertices;
        Color[] colors = new Color[vertices.Length];
        
        float[,] phiSurface = phiReader.GetPhiSurfaceAtTime(currentTimeStep);
        
        // Find min/max for normalization
        float minPhi = float.MaxValue;
        float maxPhi = float.MinValue;
        
        for (int i = 0; i < 201; i++)
        {
            for (int j = 0; j < 101; j++)
            {
                float phi = phiSurface[i, j];
                minPhi = Mathf.Min(minPhi, phi);
                maxPhi = Mathf.Max(maxPhi, phi);
            }
        }
        
        // Update vertices and colors
        for (int i = 0; i < 201; i++)
        {
            for (int j = 0; j < 101; j++)
            {
                int idx = i * 101 + j;
                
                // Height based on phi value
                float phi = phiSurface[i, j];
                float normalizedPhi = (phi - minPhi) / (maxPhi - minPhi);
                
                vertices[idx].y = normalizedPhi * 0.5f; // Scale height
                colors[idx] = colorGradient.Evaluate(normalizedPhi);
            }
        }
        
        surfaceMesh.vertices = vertices;
        surfaceMesh.colors = colors;
        surfaceMesh.RecalculateNormals();
        
        // Update time display
        float actualTime = phiReader.GetActualTime(currentTimeStep);
        Debug.Log($"Time: {actualTime:F2}ms (index {currentTimeStep})");
    }
}