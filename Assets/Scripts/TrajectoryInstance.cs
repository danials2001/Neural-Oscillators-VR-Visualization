using UnityEngine;
using UnityEngine.InputSystem; // for new Input System
using System.IO;
public class TrajectoryInstance : MonoBehaviour {
    public string jsonFileName;

    public GameObject marker;
    public LineRenderer lineRenderer;

    public float timeScale = 0.2f;
    public float vScale = 0.01f;
    public float nScale = 1.0f;
    public float eScale = 0.002f;

    private TrajectoryPoint[] points;
    private float timer = 0f;
    private int currentIndex = 0;
    
    public float joystickSensitivity = 1.0f;
    public float minSpeed = 0.1f;
    public float maxSpeed = 10.0f;

    private InputAction thumbstickY;

    void Awake() {
        // Setup thumbstick Y axis (left hand)
        thumbstickY = new InputAction(type: InputActionType.Value, binding: "<XRController>{LeftHand}/thumbstick/y");
        thumbstickY.Enable();
    }

    void Start() {
        LoadTrajectory();
        DrawFullTrajectory();
        marker.transform.position = GetPosition(points[0]);
        
        var trail = marker.GetComponent<TrailRenderer>();
        if (trail != null) {
            trail.startWidth = 0.01f;
            trail.endWidth = 0f;
            trail.time = 3f;
            trail.alignment = LineAlignment.TransformZ;

            // Optional: Set a material
            Material trailMat = new Material(Shader.Find("Unlit/Color"));
            trailMat.color = Color.cyan;
            trail.material = trailMat;
        }
    }

    void Update() {
        if (points == null || points.Length < 2) return;

        float input = thumbstickY.ReadValue<float>();

        // Deadzone filter
        if (Mathf.Abs(input) > 0.2f) {
            timeScale += input * joystickSensitivity * Time.deltaTime;
            timeScale = Mathf.Clamp(timeScale, minSpeed, maxSpeed);
        }
        timer += Time.deltaTime * timeScale;

        while (currentIndex < points.Length - 2 && timer > points[currentIndex + 1].t) {
            currentIndex++;
        }

        var p0 = points[currentIndex];
        var p1 = points[currentIndex + 1];
        float alpha = Mathf.InverseLerp(p0.t, p1.t, timer);
        marker.transform.position = Vector3.Lerp(GetPosition(p0), GetPosition(p1), alpha);
    }
    
    void OnGUI() {
        GUI.Label(new Rect(10, 10, 300, 20), $"Speed: {timeScale:F2}x");
    }

    void LoadTrajectory() {
        string path = Path.Combine(Application.streamingAssetsPath, jsonFileName);
        string json = File.ReadAllText(path);
        points = JsonUtility.FromJson<TrajectoryWrapper>(json).trajectory;
    }

    Vector3 GetPosition(TrajectoryPoint p) {
        return new Vector3(p.v * vScale, p.n * nScale, p.e * eScale);
    }

    void DrawFullTrajectory() {
        if (lineRenderer == null || points == null) return;

        lineRenderer.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++) {
            lineRenderer.SetPosition(i, GetPosition(points[i]));
        }
    }
}