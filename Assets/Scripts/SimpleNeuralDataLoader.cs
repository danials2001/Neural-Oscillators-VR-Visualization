using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SimpleNeuralDataLoader : MonoBehaviour
{
    private int nNeurons = 100;
    private float[,] voltageData;
    private Vector3[] neuronPositions;
    private List<Vector3> edges = new List<Vector3>();
    
    void Start()
    {
        LoadData();
        CreateVisualization();
    }
    
    void LoadData()
    {
        string dataPath = Path.Combine(Application.streamingAssetsPath, "NeuralData/unity_export_simple");
        
        // Load positions
        string positionsText = File.ReadAllText(Path.Combine(dataPath, "neural_positions.csv"));
        string[] posLines = positionsText.Split('\n');
        neuronPositions = new Vector3[nNeurons];
        
        for (int i = 0; i < nNeurons && i < posLines.Length; i++)
        {
            string[] coords = posLines[i].Split(',');
            if (coords.Length >= 3)
            {
                neuronPositions[i] = new Vector3(
                    float.Parse(coords[0]),
                    float.Parse(coords[2]), // Y and Z swapped for Unity
                    float.Parse(coords[1])
                );
            }
        }
        
        // Load edges
        string edgesText = File.ReadAllText(Path.Combine(dataPath, "network_edges.csv"));
        string[] edgeLines = edgesText.Split('\n');
        
        foreach (string line in edgeLines)
        {
            string[] parts = line.Split(',');
            if (parts.Length >= 3)
            {
                edges.Add(new Vector3(
                    float.Parse(parts[0]), // source
                    float.Parse(parts[1]), // target
                    float.Parse(parts[2])  // weight
                ));
            }
        }
        
        Debug.Log($"Loaded {nNeurons} neurons and {edges.Count} edges");
    }
    
    void CreateVisualization()
    {
        // Create spheres for neurons
        for (int i = 0; i < nNeurons; i++)
        {
            GameObject neuron = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            neuron.name = $"Neuron_{i}";
            neuron.transform.position = neuronPositions[i];
            neuron.transform.localScale = Vector3.one * 0.2f;
        }
        
        // Create lines for edges (simplified - just show connections)
        foreach (Vector3 edge in edges)
        {
            int source = (int)edge.x;
            int target = (int)edge.y;
            
            if (source < nNeurons && target < nNeurons)
            {
                Debug.DrawLine(neuronPositions[source], neuronPositions[target], Color.blue, 1000f);
            }
        }
    }
}
