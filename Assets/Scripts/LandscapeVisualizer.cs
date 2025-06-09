using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LandscapeVisualizer : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int terrainResolution = 50;
    public float terrainSize = 10f;
    public float heightScale = 5f;
    public Gradient efficiencyGradient;
    
    [Header("Prefabs")]
    public GameObject dataPointPrefab;
    public LineRenderer gridLinePrefab;
    
    [Header("Materials")]
    public Material terrainMaterial;
    public Material wireframeMaterial;
    
    private List<LandscapePoint> landscapeData;
    private Mesh terrainMesh;
    private GameObject terrainObject;
    private Dictionary<Vector2, LandscapePoint> dataLookup;
    
    public void LoadLandscape(List<LandscapePoint> data)
    {
        landscapeData = data;
        CreateDataLookup();
        GenerateTerrain();
        PlaceDataPoints();
    }
    
    void CreateDataLookup()
    {
        dataLookup = new Dictionary<Vector2, LandscapePoint>();
        
        foreach (var point in landscapeData)
        {
            Vector2 key = new Vector2(point.D, point.alpha);
            dataLookup[key] = point;
        }
    }
    
    void GenerateTerrain()
    {
        // Create terrain mesh
        terrainObject = new GameObject("ParameterTerrain");
        terrainObject.transform.SetParent(transform);
        
        MeshFilter meshFilter = terrainObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainObject.AddComponent<MeshRenderer>();
        meshRenderer.material = terrainMaterial;
        
        // Generate mesh
        terrainMesh = CreateTerrainMesh();
        meshFilter.mesh = terrainMesh;
        
        // Add collider for interaction
        MeshCollider collider = terrainObject.AddComponent<MeshCollider>();
        collider.sharedMesh = terrainMesh;
    }
    
    Mesh CreateTerrainMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "ParameterLandscape";
        
        // Get data ranges
        float minD = landscapeData.Min(p => p.D);
        float maxD = landscapeData.Max(p => p.D);
        float minAlpha = landscapeData.Min(p => p.alpha);
        float maxAlpha = landscapeData.Max(p => p.alpha);
        
        // Create vertices
        Vector3[] vertices = new Vector3[terrainResolution * terrainResolution];
        Vector2[] uvs = new Vector2[vertices.Length];
        Color[] colors = new Color[vertices.Length];
        
        for (int z = 0; z < terrainResolution; z++)
        {
            for (int x = 0; x < terrainResolution; x++)
            {
                int index = z * terrainResolution + x;
                
                // Map to parameter space
                float normalizedX = (float)x / (terrainResolution - 1);
                float normalizedZ = (float)z / (terrainResolution - 1);
                
                float D = Mathf.Lerp(minD, maxD, normalizedX);
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, normalizedZ);
                
                // Get efficiency at this point (interpolate from nearest data)
                float efficiency = InterpolateEfficiency(D, alpha);
                
                // Set vertex position
                vertices[index] = new Vector3(
                    normalizedX * terrainSize - terrainSize / 2,
                    efficiency * heightScale,
                    normalizedZ * terrainSize - terrainSize / 2
                );
                
                uvs[index] = new Vector2(normalizedX, normalizedZ);
                colors[index] = efficiencyGradient.Evaluate(efficiency);
            }
        }
        
        // Create triangles
        int[] triangles = new int[(terrainResolution - 1) * (terrainResolution - 1) * 6];
        int triIndex = 0;
        
        for (int z = 0; z < terrainResolution - 1; z++)
        {
            for (int x = 0; x < terrainResolution - 1; x++)
            {
                int topLeft = z * terrainResolution + x;
                int topRight = topLeft + 1;
                int bottomLeft = topLeft + terrainResolution;
                int bottomRight = bottomLeft + 1;
                
                // First triangle
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topRight;
                
                // Second triangle
                triangles[triIndex++] = topRight;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = bottomRight;
            }
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    float InterpolateEfficiency(float D, float alpha)
    {
        // Find nearest data points
        var nearest = landscapeData.OrderBy(p => 
            Mathf.Sqrt(Mathf.Pow(p.D - D, 2) + Mathf.Pow(p.alpha - alpha, 2))
        ).Take(4).ToList();
        
        if (nearest.Count == 0) return 0;
        
        // Simple inverse distance weighting
        float totalWeight = 0;
        float weightedSum = 0;
        
        foreach (var point in nearest)
        {
            float distance = Mathf.Sqrt(
                Mathf.Pow(point.D - D, 2) + 
                Mathf.Pow(point.alpha - alpha, 2)
            );
            
            if (distance < 0.001f) return point.efficiency;
            
            float weight = 1f / distance;
            totalWeight += weight;
            weightedSum += point.efficiency * weight;
        }
        
        return weightedSum / totalWeight;
    }
    
    void PlaceDataPoints()
    {
        float minD = landscapeData.Min(p => p.D);
        float maxD = landscapeData.Max(p => p.D);
        float minAlpha = landscapeData.Min(p => p.alpha);
        float maxAlpha = landscapeData.Max(p => p.alpha);
        
        foreach (var point in landscapeData)
        {
            // Map to terrain coordinates
            float x = Mathf.InverseLerp(minD, maxD, point.D) * terrainSize - terrainSize / 2;
            float z = Mathf.InverseLerp(minAlpha, maxAlpha, point.alpha) * terrainSize - terrainSize / 2;
            float y = point.efficiency * heightScale + 0.1f; // Slightly above terrain
            
            // Create marker
            GameObject marker = Instantiate(dataPointPrefab, transform);
            marker.transform.localPosition = new Vector3(x, y, z);
            marker.name = $"DataPoint_D{point.D}_A{point.alpha}";
            
            // Add interaction
            DataPointController controller = marker.AddComponent<DataPointController>();
            controller.Initialize(point);
            
            // Color by network type
            Renderer renderer = marker.GetComponent<Renderer>();
            switch (point.network)
            {
                case "Homogeneous":
                    renderer.material.color = Color.red;
                    break;
                case "Heterogeneous":
                    renderer.material.color = Color.green;
                    break;
                case "Sparse Heterogeneous":
                    renderer.material.color = Color.blue;
                    break;
            }
        }
    }
    
    public void ShowGridLines(bool show)
    {
        // Create grid lines for better readability
        if (show)
        {
            CreateGridLines();
        }
        else
        {
            // Destroy grid lines
            foreach (Transform child in transform)
            {
                if (child.name.Contains("GridLine"))
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }
    
    void CreateGridLines()
    {
        // Add grid lines at regular intervals
        int gridCount = 5;
        
        for (int i = 0; i <= gridCount; i++)
        {
            float t = (float)i / gridCount;
            
            // D-axis lines
            GameObject dLine = new GameObject($"GridLine_D_{i}");
            dLine.transform.SetParent(transform);
            LineRenderer lr = dLine.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = 0.01f;
            lr.endWidth = 0.01f;
            lr.material = wireframeMaterial;
            lr.SetPosition(0, new Vector3(t * terrainSize - terrainSize/2, 0, -terrainSize/2));
            lr.SetPosition(1, new Vector3(t * terrainSize - terrainSize/2, 0, terrainSize/2));
            
            // Alpha-axis lines
            GameObject aLine = new GameObject($"GridLine_A_{i}");
            aLine.transform.SetParent(transform);
            LineRenderer lr2 = aLine.AddComponent<LineRenderer>();
            lr2.positionCount = 2;
            lr2.startWidth = 0.01f;
            lr2.endWidth = 0.01f;
            lr2.material = wireframeMaterial;
            lr2.SetPosition(0, new Vector3(-terrainSize/2, 0, t * terrainSize - terrainSize/2));
            lr2.SetPosition(1, new Vector3(terrainSize/2, 0, t * terrainSize - terrainSize/2));
        }
    }
}

// Helper class for data point interaction
public class DataPointController : MonoBehaviour
{
    private LandscapePoint data;
    
    public void Initialize(LandscapePoint point)
    {
        data = point;
        
        // Make interactable
        gameObject.layer = LayerMask.NameToLayer("XRInteractable");
        var grabInteractable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable>();
        
        // Add hover/select events
        grabInteractable.hoverEntered.AddListener((args) => OnHover(true));
        grabInteractable.hoverExited.AddListener((args) => OnHover(false));
        grabInteractable.selectEntered.AddListener((args) => OnSelect());
    }
    
    void OnHover(bool hovering)
    {
        // Scale up on hover
        transform.localScale = Vector3.one * (hovering ? 1.5f : 1f);
    }
    
    void OnSelect()
    {
        // Load this specific dataset
        string filename = $"{(data.energy < 1000 ? "det" : "stoch")}_D{data.D:F2}_alpha{data.alpha:F2}_XR";
        DataManager.Instance.LoadDataset(filename);
        DataManager.Instance.SetVisualizationMode(0); // Switch to individual mode
    }
}