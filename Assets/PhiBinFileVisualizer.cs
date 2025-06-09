using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class PhiBinFileVisualizer : MonoBehaviour
{
    [Header("File Settings")]
    [Tooltip("Which D value to load (0.0, 1.0, 5.0, or 10.0)")]
    public float DValue = 1.0f;
    
    [Header("Visual Settings")]
    public Material surfaceMaterial;
    public AnimationCurve heightCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public Gradient colorGradient;
    public float maxHeight = 2f;
    
    [Header("Animation")]
    public float playbackSpeed = 1f;
    public bool autoPlay = true;
    public bool loop = true;
    
    // Data from file
    private float[,,] phiData;
    private int nV, nN, nTimeSteps;
    private int startIdx, endIdx;
    
    // Mesh components
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private MeshCollider meshCollider;
    
    // Animation state
    private float currentTime = 0f;
    private bool isPlaying = false;
    
    private int frameCounter = 0;

    
    void Start()
    {
        // Create mesh components
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();

        meshCollider.sharedMesh = mesh;
        
        if (surfaceMaterial == null)
        {
            surfaceMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        meshRenderer.material = surfaceMaterial;
        
        // Load the data
        LoadPhiData();
        
        // Create the mesh
        CreateMesh();
        
        // Start playing if auto
        if (autoPlay)
        {
            isPlaying = true;
        }
    }
    
    void LoadPhiData()
    {
        // Construct file path
        string filename = $"phi_evolution_D{DValue:F1}.bin";
        string folderPath = Path.Combine(Application.dataPath, "StreamingAssets", "unity_export_phi_evolution");
        string filePath = Path.Combine(folderPath, filename);
        
        // Alternative path if not in StreamingAssets
        if (!File.Exists(filePath))
        {
            // Try looking in the project root
            folderPath = Path.Combine(Application.dataPath, "..", "unity_export_phi_evolution");
            filePath = Path.Combine(folderPath, filename);
        }
        
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Cannot find file: {filename}. Tried paths:\n" +
                         $"1. {Path.Combine(Application.dataPath, "StreamingAssets", "unity_export_phi_evolution")}\n" +
                         $"2. {folderPath}");
            return;
        }
        
        Debug.Log($"Loading phi data from: {filePath}");
        
        try
        {
            using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                // Read header (5 integers)
                nV = reader.ReadInt32();        // Should be 201
                nN = reader.ReadInt32();        // Should be 101
                nTimeSteps = reader.ReadInt32(); // 737 for D=0, 369 for others
                startIdx = reader.ReadInt32();
                endIdx = reader.ReadInt32();
                
                Debug.Log($"Loaded dimensions: V={nV}, n={nN}, TimeSteps={nTimeSteps}");
                
                // Allocate array
                phiData = new float[nTimeSteps, nV, nN];
                
                // Read all data
                for (int t = 0; t < nTimeSteps; t++)
                {
                    for (int i = 0; i < nV; i++)
                    {
                        for (int j = 0; j < nN; j++)
                        {
                            phiData[t, i, j] = reader.ReadSingle();
                        }
                    }
                    
                    // Progress log
                    if (t % 50 == 0)
                    {
                        Debug.Log($"Loading time step {t}/{nTimeSteps}");
                    }
                }
                
                Debug.Log("Data loaded successfully!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading file: {e.Message}");
        }
    }
    
    void CreateMesh()
    {
        if (phiData == null) return;
        
        mesh = new Mesh();
        mesh.name = "Phi Surface";
        
        // Create vertices
        int vertexCount = nV * nN;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        
        // Create grid
        for (int i = 0; i < nV; i++)
        {
            for (int j = 0; j < nN; j++)
            {
                int idx = i * nN + j;
                
                // Map to Unity coordinates
                // V goes from -100 to 100 mV (scaled by 1/100 in data)
                // n goes from 0 to 1
                float x = Mathf.Lerp(-2f, 2f, i / (float)(nV - 1));
                float z = Mathf.Lerp(0f, 2f, j / (float)(nN - 1));
                
                vertices[idx] = new Vector3(x, 0, z);
                uvs[idx] = new Vector2(i / (float)(nV - 1), j / (float)(nN - 1));
            }
        }
        
        // Create triangles
        List<int> triangles = new List<int>();
        for (int i = 0; i < nV - 1; i++)
        {
            for (int j = 0; j < nN - 1; j++)
            {
                int bottomLeft = i * nN + j;
                int bottomRight = (i + 1) * nN + j;
                int topLeft = i * nN + (j + 1);
                int topRight = (i + 1) * nN + (j + 1);
                
                // First triangle
                triangles.Add(bottomLeft);
                triangles.Add(topLeft);
                triangles.Add(bottomRight);
                
                // Second triangle
                triangles.Add(bottomRight);
                triangles.Add(topLeft);
                triangles.Add(topRight);
            }
        }
        
        // Assign to mesh
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        
        meshFilter.mesh = mesh;
        
// ✅ This must come after the mesh is fully built:
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();

        meshCollider.sharedMesh = null; // Important: clear first to force update
        meshCollider.sharedMesh = mesh;
        meshCollider.providesContacts = true;
        
        // Update with first frame
        UpdateMeshHeights(0);
    }
    
    void Update()
    {
        if (phiData == null || !isPlaying) return;
        
        // Update time
        // Animate forward in time index
        currentTime += Time.deltaTime * playbackSpeed * 50f;

        if (currentTime >= nTimeSteps)
        {
            if (loop)
                currentTime = 0f;
            else
                currentTime = nTimeSteps - 1;
        }

        int timeIndex = Mathf.FloorToInt(currentTime);
        int reversedTimeIndex = nTimeSteps - 1 - timeIndex;
        UpdateMeshHeights(reversedTimeIndex);
    }
    
    void UpdateMeshHeights(int timeStep)
    {
        if (mesh == null || phiData == null) return;
        
        timeStep = Mathf.Clamp(timeStep, 0, nTimeSteps - 1);
        
        Vector3[] vertices = mesh.vertices;
        Color[] colors = new Color[vertices.Length];
        
        // Find min/max for normalization
        float minPhi = float.MaxValue;
        float maxPhi = float.MinValue;
        
        for (int i = 0; i < nV; i++)
        {
            for (int j = 0; j < nN; j++)
            {
                float val = phiData[timeStep, i, j];
                if (val < minPhi) minPhi = val;
                if (val > maxPhi) maxPhi = val;
            }
        }
        
        // Update vertices
        for (int i = 0; i < nV; i++)
        {
            for (int j = 0; j < nN; j++)
            {
                int idx = i * nN + j;
                
                float phi = phiData[timeStep, i, j];
                float normalized = (phi - minPhi) / (maxPhi - minPhi + 0.0001f);
                
                // Apply height curve for non-linear scaling if desired
                float height = heightCurve.Evaluate(normalized) * maxHeight;
                vertices[idx].y = height;
                
                // Set color
                if (colorGradient != null)
                {
                    colors[idx] = colorGradient.Evaluate(normalized);
                }
            }
        }
        
        // Update mesh
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        if (frameCounter++ % 10 == 0) // every 5 frames
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }
    
    // Public controls
    public void Play() { isPlaying = true; }
    public void Pause() { isPlaying = false; }
    public void TogglePlayPause() { isPlaying = !isPlaying; }
    
    public void SetTimeStep(int step)
    {
        currentTime = Mathf.Clamp(step, 0, nTimeSteps - 1);
        UpdateMeshHeights((int)currentTime);
    }
    
    public void Reset()
    {
        currentTime = 0f;
        UpdateMeshHeights(0);
    }
    
    // Get info for UI
    public string GetTimeInfo()
    {
        if (phiData == null) return "No data loaded";
        
        int currentStep = Mathf.FloorToInt(currentTime);
        
        // Calculate actual time (HJB solved backward)
        float dt = 7.0f / (nTimeSteps - 1);
        float actualTime = 7.0f - (currentStep * dt);
        
        return $"Time: {actualTime:F2}ms (Step {currentStep}/{nTimeSteps})";
    }
}
