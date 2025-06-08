using System.IO;
using UnityEngine;

public class NetworkLoader : MonoBehaviour {
    private string dataPath;
    private string positionsFile;
    private string edgesFile;
    private string voltageFile;

    private GameObject[] neurons = new GameObject[100];
    private VoltageData voltageData;

    // Animation fields
    public bool playing = true;
    public float playbackSpeed = 1.0f;
    public float frameInterval = 0.1f; // 10 FPS
    private float currentTime = 0f;

    void Start() {
        dataPath = Path.Combine(Application.streamingAssetsPath, "NeuralData/unity_export_simple");
        positionsFile = Path.Combine(dataPath, "neural_positions.csv");
        edgesFile = Path.Combine(dataPath, "network_edges.csv");
        voltageFile = Path.Combine(dataPath, "voltage_with_control.bin");

        LoadNeurons();
        LoadEdges();
        LoadVoltageData();
    }

    void Update()
    {
        if (!playing || voltageData == null) return;

        currentTime += Time.deltaTime * playbackSpeed;
        int frameIndex = (int)(currentTime / frameInterval) % 49;

        for (int i = 0; i < 100; i++)
        {
            float voltage = voltageData.GetVoltage(frameIndex, i);
            neurons[i].GetComponent<Renderer>().material.color = VoltageToColor(voltage);
            neurons[i].GetComponent<NeuronInfo>().lastVoltage = voltage;
        }

    }

    void LoadNeurons() {
        string[] lines = File.ReadAllLines(positionsFile);
        for (int i = 0; i < lines.Length; i++)
        {
            string[] coords = lines[i].Split(',');
            float x = float.Parse(coords[0]);
            float y = float.Parse(coords[1]);
            float z = float.Parse(coords[2]) * 5f;

            neurons[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            neurons[i].name = $"Neuron_{i}";
            neurons[i].transform.position = new Vector3(x, z, y);
            neurons[i].transform.localScale = Vector3.one * 0.2f;
            
            // Add info script
            NeuronInfo info = neurons[i].AddComponent<NeuronInfo>();
            info.neuronIndex = i;
        }
    }

    void LoadEdges() {
        string[] lines = File.ReadAllLines(edgesFile);
        foreach (string line in lines) {
            string[] parts = line.Split(',');
            int source = int.Parse(parts[0]);
            int target = int.Parse(parts[1]);
            float weight = float.Parse(parts[2]);

            GameObject edge = new GameObject($"Edge_{source}_to_{target}");
            LineRenderer lr = edge.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, neurons[source].transform.position);
            lr.SetPosition(1, neurons[target].transform.position);
            lr.startWidth = lr.endWidth = Mathf.Abs(weight) * 0.05f;

            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = weight > 0 ? Color.blue : Color.red;
            lr.material = mat;
        }
    }

    void LoadVoltageData() {
        voltageData = new VoltageData();
        voltageData.LoadFromBinary(voltageFile);
    }

    Color VoltageToColor(float voltage) {
        float normalized = (voltage + 80f) / 120f;
        if (normalized < 0.33f)
            return Color.Lerp(Color.blue, Color.green, normalized * 3f);
        else if (normalized < 0.66f)
            return Color.Lerp(Color.green, Color.yellow, (normalized - 0.33f) * 3f);
        else
            return Color.Lerp(Color.yellow, Color.red, (normalized - 0.66f) * 3f);
    }
}
