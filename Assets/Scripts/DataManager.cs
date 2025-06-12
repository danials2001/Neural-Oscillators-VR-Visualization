using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using TMPro;

public class DataManager : MonoBehaviour
{
    [Header("Data Files")]
    public string dataFolder = "StreamingAssets/XRData";
    private Dictionary<string, NeuralData> loadedDatasets = new Dictionary<string, NeuralData>();
    private List<LandscapePoint> landscapeData;

    [Header("UI")]
    public TMP_Dropdown  datasetDropdown;
    public TextMeshPro infoText;

    [Header("Visualization Modes")]
    public GameObject individualMode;
    public GameObject comparisonMode;
    public GameObject landscapeMode;

    private NeuralData currentData;
    private string currentDatasetName;

    public static DataManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        LoadAvailableDatasets();
        LoadLandscapeData();
        
        SetVisualizationMode(0); // 0 = individualMode, 1 = comparisonMode, 2 = landscapeMode

    }

    void LoadAvailableDatasets()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "XRData");
        string[] files = Directory.GetFiles(path, "*_XR.json");

        datasetDropdown.options.Clear();
        foreach (string file in files)
        {
            string filename = Path.GetFileNameWithoutExtension(file);
            datasetDropdown.options.Add(new TMP_Dropdown.OptionData(filename));
        }

        datasetDropdown.onValueChanged.AddListener(OnDatasetSelected);

        if (files.Length > 0)
        {
            LoadDataset(datasetDropdown.options[0].text);
        }
    }

    public void LoadDataset(string datasetName)
    {
        if (loadedDatasets.ContainsKey(datasetName))
        {
            currentData = loadedDatasets[datasetName];
            currentDatasetName = datasetName;
            UpdateVisualization();
            return;
        }

        string path = Path.Combine(Application.streamingAssetsPath, "XRData", datasetName + ".json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            NeuralData data = JsonUtility.FromJson<NeuralData>(json);
            loadedDatasets[datasetName] = data;
            currentData = data;
            currentDatasetName = datasetName;
            UpdateVisualization();

            UpdateInfoText();
        }
    }

    void LoadLandscapeData()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "XRData", "landscape.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            landscapeData = JsonUtility.FromJson<List<LandscapePoint>>(json);
        }
    }

    void OnDatasetSelected(int index)
    {
        LoadDataset(datasetDropdown.options[index].text);
    }

    void UpdateInfoText()
    {
        if (currentData != null)
        {
            infoText.text = $"Dataset: {currentData.metadata.filename}\n" +
                           $"Network: {currentData.metadata.network}\n" +
                           $"D_noise: {currentData.metadata.D_noise:F2}\n" +
                           $"Alpha: {currentData.metadata.alpha:F2}\n" +
                           $"Control: {currentData.metadata.control_type}\n" +
                           $"Neurons: {currentData.neurons.Count}";
        }
    }

    void UpdateVisualization()
    {
        // Update active visualizer
        if (individualMode.activeSelf)
        {
            individualMode.GetComponent<NeuronVisualizer>().LoadData(currentData);
        }
        else if (comparisonMode.activeSelf)
        {
            // Extract D and alpha values from current dataset
            float D = currentData.metadata.D_noise;
            float alpha = currentData.metadata.alpha;
            comparisonMode.GetComponent<ComparisonVisualizer>().LoadComparison(D, alpha);
        }
    }

    public void SetVisualizationMode(int mode)
    {
        individualMode.SetActive(mode == 0);
        comparisonMode.SetActive(mode == 1);
        landscapeMode.SetActive(mode == 2);
        
        if (mode == 1 && currentData != null) // Comparison mode
        {
            float D = currentData.metadata.D_noise;
            float alpha = currentData.metadata.alpha;
            comparisonMode.GetComponent<ComparisonVisualizer>().LoadComparison(D, alpha);
        }
        else if (mode == 2) // Landscape mode
        {
            landscapeMode.GetComponent<LandscapeVisualizer>().LoadLandscape(landscapeData);
        }
    }

    public NeuralData GetCurrentData() => currentData;
    public List<LandscapePoint> GetLandscapeData() => landscapeData;

    public NeuralData LoadSpecificDataset(string datasetName)
{
    // Check if already loaded
    if (loadedDatasets.ContainsKey(datasetName))
    {
        return loadedDatasets[datasetName];
    }
    
    // Load from file
    string path = Path.Combine(Application.streamingAssetsPath, "XRData", datasetName + ".json");
    if (File.Exists(path))
    {
        string json = File.ReadAllText(path);
        NeuralData data = JsonUtility.FromJson<NeuralData>(json);
        loadedDatasets[datasetName] = data;
        return data;
    }
    
    Debug.LogWarning($"Dataset not found: {datasetName}");
    return null;
}
}

