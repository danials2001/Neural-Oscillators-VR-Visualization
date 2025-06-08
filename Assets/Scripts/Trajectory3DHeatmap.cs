using UnityEngine;
using System.IO;

public class Trajectory3DHeatmap : MonoBehaviour {
    public GameObject particlePrefab;
    public string jsonFileName = "trajectory_D15.json";
    public float scale = 1f;

    private float minE = float.MaxValue;
    private float maxE = float.MinValue;

    void Start() {
        // Load and parse JSON
        string path = Path.Combine(Application.streamingAssetsPath, jsonFileName);
        string json = File.ReadAllText(path);
        TrajectoryWrapper wrapper = JsonUtility.FromJson<TrajectoryWrapper>(json);
        var points = wrapper.trajectory;

        // Find min/max energy for normalization
        foreach (var point in points) {
            if (point.e < minE) minE = point.e;
            if (point.e > maxE) maxE = point.e;
        }

        // Visualize each point
        foreach (var point in points) {
            Vector3 pos = new Vector3(point.v, point.n, point.e) * scale;

            GameObject p = Instantiate(particlePrefab, pos, Quaternion.identity, transform);

            // Normalize energy
            float normE = Mathf.InverseLerp(minE, maxE, point.e);

            // Set color
            Renderer r = p.GetComponent<Renderer>();
            if (r != null) {
                r.material.color = Color.Lerp(Color.blue, Color.red, normE);
            }

            // Set scale based on energy
            float size = Mathf.Lerp(0.1f, 0.3f, normE);
            p.transform.localScale = Vector3.one * size;
        }
    }
}
