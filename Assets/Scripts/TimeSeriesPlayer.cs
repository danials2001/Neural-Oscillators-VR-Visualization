using UnityEngine;
using System.Collections;
using System.IO;


public class TimeSeriesPlayer : MonoBehaviour {
    public GameObject marker;
    public string jsonFileName = "trajectory_D0.json";
    public float timeScale = 1f; // 1 = real time, >1 = faster

    private TrajectoryPoint[] points;
    private float timer = 0f;
    private int currentIndex = 0;

    void Start() {
        string path = Path.Combine(Application.streamingAssetsPath, jsonFileName);
        string json = File.ReadAllText(path);
        points = JsonUtility.FromJson<TrajectoryWrapper>(json).trajectory;
    }

    void Update() {
        if (points == null || points.Length < 2) return;

        timer += Time.deltaTime * timeScale;

        // Move through the time series
        while (currentIndex < points.Length - 2 && timer > points[currentIndex + 1].t) {
            currentIndex++;
        }

        TrajectoryPoint p0 = points[currentIndex];
        TrajectoryPoint p1 = points[currentIndex + 1];

        float alpha = Mathf.InverseLerp(p0.t, p1.t, timer);

        // Linear interpolation
        float v = Mathf.Lerp(p0.v, p1.v, alpha);
        float n = Mathf.Lerp(p0.n, p1.n, alpha);
        float e = Mathf.Lerp(p0.e, p1.e, alpha);

        // Position marker
        marker.transform.position = new Vector3(v, n, e);

        // Optional: Show time or energy
        Debug.Log($"Time: {timer:F2}, Energy: {e:F2}");
    }
}