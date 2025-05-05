using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class TrajectoryPoint {
    public float t;
    public float v;
    public float n;
    public float e;
}

[System.Serializable]
public class TrajectoryWrapper {
    public TrajectoryPoint[] trajectory;
}

public class DataHandler : MonoBehaviour {
    public GameObject markerPrefab;
    public float scale = 1.0f;

    void Start() {
        string path = Path.Combine(Application.streamingAssetsPath, "trajectory_D0.json");
        string json = File.ReadAllText(path);
        TrajectoryWrapper data = JsonUtility.FromJson<TrajectoryWrapper>(json);

        foreach (var point in data.trajectory) {
            Vector3 position = new Vector3(point.v, point.n, point.e) * scale;
            Instantiate(markerPrefab, position, Quaternion.identity);
        }
    }
}