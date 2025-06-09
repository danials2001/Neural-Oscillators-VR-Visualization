using System.IO;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;


public class PhiTrajectoryManager : MonoBehaviour
{
    [Header("References")]
    public PhiBinFileVisualizer phiAnimator;
    
    [Header("Visualization Settings")]
    public Material trajectoryMaterial;
    public float lineWidth = 0.02f;
    public float heightOffset = 0.1f;
    public bool showStartEndMarkers = true;
    
    [Header("Debug")]
    public bool debugMode = false;
    
    private Dictionary<string, TrajectoryData> trajectoryCache;
    private GameObject currentTrajectory;
    private List<GameObject> trajectoryMarkers;
    
    [Header("XR")]
    public XRRayInteractor rayInteractor;      // For ray hits
    public ActionBasedController controller;
    public InputHelpers.Button activationButton = InputHelpers.Button.Trigger;
    public float activationThreshold = 0.1f;
    
    private GameObject hitMarker;
    public GameObject hitMarkerPrefab; // optional, can assign a sphere prefab
    private class TrajectoryData
    {
        public float[] time;
        public float[] V;
        public float[] n;
    }
    
    void Start()
    {
        trajectoryCache = new Dictionary<string, TrajectoryData>();
        trajectoryMarkers = new List<GameObject>();
        
        if (phiAnimator == null)
        {
            Debug.LogError("TrajectoryManager: No PhiEvolutionAnimator assigned!");
            enabled = false;
        }
        
        if (!hitMarker)
        {
            hitMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hitMarker.transform.localScale = Vector3.one * 0.03f;
            hitMarker.GetComponent<Renderer>().material.color = Color.yellow;
            Destroy(hitMarker.GetComponent<Collider>()); // don't interfere with raycasts
        }

        hitMarker.SetActive(false);
    }
    
    void Update()
    {
        UpdateHitMarker();

        // XR trigger press
        if (rayInteractor && IsActivated())
        {
            CheckForXRRaycastHit();
        }

        // Clear with keyboard (for debugging)
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearTrajectory();
        }
    }
    void UpdateHitMarker()
    {
        if (rayInteractor && rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            hitMarker.SetActive(true);
            hitMarker.transform.position = hit.point + hit.normal * 0.005f; // offset slightly above surface
            hitMarker.transform.up = hit.normal;
            Debug.DrawLine(rayInteractor.transform.position, hitMarker.transform.position, Color.green);
        }
        else
        {
            hitMarker.SetActive(false);
        }
    }

    bool IsActivated()
    {
        if (controller.activateActionValue == null)
            return false;

        return controller.activateActionValue.action.ReadValue<float>() > activationThreshold;
    }
    
    void CheckForXRRaycastHit()
    {
        if (!rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            return;

        Debug.Log("XR ray hit: " + hit.collider?.name);

        MeshCollider meshCollider = hit.collider as MeshCollider;
        if (meshCollider == null || meshCollider.sharedMesh == null)
            return;

        Mesh mesh = meshCollider.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        int triangleIndex = hit.triangleIndex;

        Vector3 p0 = vertices[triangles[triangleIndex * 3 + 0]];
        Vector3 p1 = vertices[triangles[triangleIndex * 3 + 1]];
        Vector3 p2 = vertices[triangles[triangleIndex * 3 + 2]];

        Transform hitTransform = hit.collider.transform;
        p0 = hitTransform.TransformPoint(p0);
        p1 = hitTransform.TransformPoint(p1);
        p2 = hitTransform.TransformPoint(p2);

        Debug.DrawLine(p0, p1, Color.red, 2f);
        Debug.DrawLine(p1, p2, Color.green, 2f);
        Debug.DrawLine(p2, p0, Color.blue, 2f);

        // Create debug triangle
        GameObject triObj = new GameObject("XRDebugTriangle");
        MeshFilter mf = triObj.AddComponent<MeshFilter>();
        MeshRenderer mr = triObj.AddComponent<MeshRenderer>();

        Mesh triMesh = new Mesh();
        triMesh.vertices = new Vector3[] { p0, p1, p2 };
        triMesh.triangles = new int[] { 0, 1, 2 };
        triMesh.RecalculateNormals();
        triMesh.RecalculateBounds();
        mf.mesh = triMesh;

        Material debugMat = new Material(Shader.Find("Unlit/Color"));
        debugMat.color = Color.yellow;
        mr.material = debugMat;

        // Slight offset
        triObj.transform.position += Vector3.up * 0.01f;
        
        Destroy(triObj, 5f);

        // Load trajectory
        if (hit.transform.IsChildOf(phiAnimator.transform))
        {
            ShowTrajectoryAtPoint(hit.point);
        }
    }
    
    void CheckForPhiClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log("click hit");
            MeshCollider meshCollider = hit.collider as MeshCollider;
            if (meshCollider == null || meshCollider.sharedMesh == null)
                return;

            Mesh mesh = meshCollider.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            int triangleIndex = hit.triangleIndex;

            Vector3 p0 = vertices[triangles[triangleIndex * 3 + 0]];
            Vector3 p1 = vertices[triangles[triangleIndex * 3 + 1]];
            Vector3 p2 = vertices[triangles[triangleIndex * 3 + 2]];

            Transform hitTransform = hit.collider.transform;
            p0 = hitTransform.TransformPoint(p0);
            p1 = hitTransform.TransformPoint(p1);
            p2 = hitTransform.TransformPoint(p2);

            // Draw lines for debug
            Debug.DrawLine(p0, p1, Color.red, 2f);
            Debug.DrawLine(p1, p2, Color.green, 2f);
            Debug.DrawLine(p2, p0, Color.blue, 2f);

            Debug.Log("Triangle points: " + p0 + ", " + p1 + ", " + p2);

            // Create visible triangle mesh GameObject
            GameObject triObj = new GameObject("DebugTriangle");
            MeshFilter mf = triObj.AddComponent<MeshFilter>();
            MeshRenderer mr = triObj.AddComponent<MeshRenderer>();

            Mesh triMesh = new Mesh();
            triMesh.vertices = new Vector3[] { p0, p1, p2 };
            triMesh.triangles = new int[] { 0, 1, 2 };
            triMesh.RecalculateNormals();
            triMesh.RecalculateBounds();
            mf.mesh = triMesh;

            // Material so you can see it
            Material debugMat = new Material(Shader.Find("Unlit/Color"));
            debugMat.color = Color.yellow;
            mr.material = debugMat;

            // Optional: scale it slightly above the surface to avoid z-fighting
            triObj.transform.position += Vector3.up * 0.01f;

            // Optional: destroy after a few seconds
            // Destroy(triObj, 5f);

            // Show trajectory if relevant
            if (hit.transform.IsChildOf(phiAnimator.transform))
            {
                Debug.Log("phiAnimator.transform is child of trajectory");
                ShowTrajectoryAtPoint(hit.point);
            }
        }
    }
    
    void ShowTrajectoryAtPoint(Vector3 worldPoint)
    {
        // Convert to grid coordinates
        Vector3 localPoint = phiAnimator.transform.InverseTransformPoint(worldPoint);
        
        // Map to grid indices (assuming phi mesh goes from -2 to 2 in X, 0 to 2 in Z)
        int gridI = Mathf.RoundToInt((localPoint.x + 2f) / 4f * 200f);
        int gridJ = Mathf.RoundToInt(localPoint.z / 2f * 100f);
        
        // Snap to sampled grid points
        gridI = Mathf.RoundToInt(gridI / 20f) * 20;
        gridJ = Mathf.RoundToInt(gridJ / 10f) * 10;
        gridI = Mathf.Clamp(gridI, 0, 200);
        gridJ = Mathf.Clamp(gridJ, 0, 100);
        
        // Load and display trajectory
        string filename = $"traj_{gridI:D3}_{gridJ:D3}.bin";
        string path = Path.Combine(Application.streamingAssetsPath,
                                  "trajectories",
                                  filename);
        
        Debug.Log("Path:"+ path);
        
        if (File.Exists(path))
        {
            LoadAndDisplayTrajectory(filename, path);
            
            if (debugMode)
            {
                Debug.Log($"Loaded trajectory at grid point ({gridI}, {gridJ})");
            }
        }
        else if (debugMode)
        {
            Debug.Log($"No trajectory at grid point ({gridI}, {gridJ})");
        }
    }
    
    void LoadAndDisplayTrajectory(string filename, string path)
    {
        // Load from cache or file
        TrajectoryData traj;
        
        if (trajectoryCache.ContainsKey(filename))
        {
            traj = trajectoryCache[filename];
        }
        else
        {
            traj = LoadTrajectoryFromFile(path);
            trajectoryCache[filename] = traj;
        }
        
        // Display
        ClearTrajectory();
        CreateTrajectoryVisualization(traj);
    }
    
    TrajectoryData LoadTrajectoryFromFile(string path)
    {
        using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
        {
            int nPoints = reader.ReadInt32();
            
            TrajectoryData traj = new TrajectoryData
            {
                time = new float[nPoints],
                V = new float[nPoints],
                n = new float[nPoints]
            };
            
            for (int i = 0; i < nPoints; i++)
                traj.time[i] = reader.ReadSingle();
            for (int i = 0; i < nPoints; i++)
                traj.V[i] = reader.ReadSingle();
            for (int i = 0; i < nPoints; i++)
                traj.n[i] = reader.ReadSingle();
            
            return traj;
        }
    }
    
    void CreateTrajectoryVisualization(TrajectoryData traj)
    {
        currentTrajectory = new GameObject("Trajectory");
        LineRenderer line = currentTrajectory.AddComponent<LineRenderer>();
        
        // Setup line renderer
        line.material = trajectoryMaterial;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.positionCount = traj.V.Length;
        
        // Convert to world positions
        Vector3[] positions = new Vector3[traj.V.Length];
        
        for (int i = 0; i < traj.V.Length; i++)
        {
            // Scale to match phi surface
            float x = Mathf.Lerp(-2f, 2f, (traj.V[i] + 100f) / 200f);
            float z = Mathf.Lerp(0f, 2f, traj.n[i]);
            float y = heightOffset + i * 0.0001f;
            
            positions[i] = phiAnimator.transform.TransformPoint(new Vector3(x, y, z));
        }
        
        line.SetPositions(positions);
        
        // Add gradient
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.blue, 0.0f),
                new GradientColorKey(Color.cyan, 0.5f),
                new GradientColorKey(Color.red, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 1.0f)
            }
        );
        line.colorGradient = gradient;
        
        // Add start/end markers
        if (showStartEndMarkers)
        {
            CreateMarker(positions[0], Color.green, "Start");
            CreateMarker(positions[positions.Length - 1], Color.red, "End");
        }
    }
    
    void CreateMarker(Vector3 position, Color color, string name)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = name;
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * 0.05f;
        marker.GetComponent<Renderer>().material.color = color;
        Destroy(marker.GetComponent<Collider>());
        trajectoryMarkers.Add(marker);
    }
    
    void ClearTrajectory()
    {
        if (currentTrajectory != null)
            Destroy(currentTrajectory);
        
        foreach (var marker in trajectoryMarkers)
            if (marker != null)
                Destroy(marker);
        
        trajectoryMarkers.Clear();
    }
    
    void OnDestroy()
    {
        ClearTrajectory();
    }
}