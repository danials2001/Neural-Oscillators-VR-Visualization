using UnityEngine;
using System.IO;
using System.Linq;

public class TrajectoryManager : MonoBehaviour {
    public GameObject markerPrefab;  // Your sphere prefab (with a Renderer)
    public Material[] lineMaterials; // Array of materials for variety, or a single material
    public GameObject lineRendererPrefab; // GameObject that has a LineRenderer component

    void Start() {
        string dataPath = Application.streamingAssetsPath;
        string[] jsonFiles = Directory.GetFiles(dataPath, "trajectory_D*.json");

        for (int i = 0; i < jsonFiles.Length; i++) {
            string fileName = System.IO.Path.GetFileName(jsonFiles[i]);

            // Create a container GameObject for this trajectory
            GameObject container = new GameObject($"Trajectory_{i}");

            // Instantiate the LineRenderer from prefab
            GameObject lineObj = Instantiate(lineRendererPrefab, container.transform);
            LineRenderer lr = lineObj.GetComponent<LineRenderer>();

            // Configure your LineRenderer (example settings)
            lr.positionCount = 0;
            lr.useWorldSpace = true;
            lr.startWidth = 0.01f;
            lr.endWidth = 0.01f;
            lr.alignment = LineAlignment.View;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            // Assign a material from the array (if available)
            if (lineMaterials != null && lineMaterials.Length > i)
                lr.material = lineMaterials[i];
            else if (lineMaterials != null && lineMaterials.Length > 0)
                lr.material = lineMaterials[0];

            // Instantiate the marker (the sphere) from prefab
            GameObject marker = Instantiate(markerPrefab, container.transform);

            // Now assign the same material to the marker's Renderer.
            Renderer markerRenderer = marker.GetComponent<Renderer>();
            if (markerRenderer != null) {
                // Option 1: Direct reference (if you want both to share the same material instance)
                markerRenderer.material = lr.material; 
                // Option 2: Use Instantiate to create a copy if you plan to modify one independently:
                // markerRenderer.material = Instantiate(lr.material);
            }

            // Attach the trajectory instance script that handles animation.
            // Assuming the script is added dynamically as done before:
            TrajectoryInstance instance = container.AddComponent<TrajectoryInstance>();
            instance.jsonFileName = fileName;
            instance.marker = marker;
            instance.lineRenderer = lr;
        }
    }
}
