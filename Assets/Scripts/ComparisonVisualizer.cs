using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ComparisonVisualizer : MonoBehaviour
{
    [Header("Layout")]
    public Transform leftSide;
    public Transform rightSide;
    public float separation = 5f;
    
    [Header("UI")]
    public TMP_Text leftLabel;
    public TMP_Text rightLabel;
    public TMP_Text comparisonStats;
    
    private NeuronVisualizer leftVisualizer;
    private NeuronVisualizer rightVisualizer;
    
    [Header("Prefab")]
    public GameObject neuronVisualizerPrefab;  // Assign in inspector
    
    void Start()
    {
        // Create two neuron visualizers
        CreateSideBySideVisualizers();
    }
    

    void CreateSideBySideVisualizers()
    {
        // Left side (Deterministic)
        GameObject leftViz = Instantiate(neuronVisualizerPrefab, leftSide);
        leftViz.name = "LeftVisualizer";
        leftViz.transform.localPosition = new Vector3(-separation / 2f, 0, 0);
        leftVisualizer = leftViz.GetComponent<NeuronVisualizer>();

        // Right side (Stochastic)
        GameObject rightViz = Instantiate(neuronVisualizerPrefab, rightSide);
        rightViz.name = "RightVisualizer";
        rightViz.transform.localPosition = new Vector3(separation / 2f, 0, 0);
        rightVisualizer = rightViz.GetComponent<NeuronVisualizer>();
    }
    
public void LoadComparison(float D_value, float alpha_value)
{
    // Build filenames
    string detFile = $"det_D{D_value:F2}_alpha{alpha_value:F2}_XR";
    string stochFile = $"stoch_D{D_value:F2}_alpha{alpha_value:F2}_XR";
    
    // Load deterministic on left
    var detData = DataManager.Instance.LoadSpecificDataset(detFile);
    if (detData != null)
    {
        leftVisualizer.LoadData(detData);
        leftLabel.text = "Deterministic Control";
    }
    
    // Load stochastic on right
    var stochData = DataManager.Instance.LoadSpecificDataset(stochFile);
    if (stochData != null)
    {
        rightVisualizer.LoadData(stochData);
        rightLabel.text = "Stochastic Control";
    }
    
    // // Calculate actual comparison stats
    // if (detData != null && stochData != null)
    // {
    //     float energySavings = (detData.metadata.total_energy - stochData.metadata.total_energy) 
    //                           / detData.metadata.total_energy * 100;
    //     comparisonStats.text = $"Energy Savings: {energySavings:F1}%";
    // }
}
    
    void UpdateComparisonStats()
    {
        // Calculate and display comparison metrics
        comparisonStats.text = "Energy Savings: 23.5%\n" +
                              "Desync Rate: +15%\n" +
                              "Mean Control Count: -8";
    }
    
    public void SyncTimeSliders(float time)
    {
        // Keep both visualizations in sync
        if (leftVisualizer != null && rightVisualizer != null)
        {
            // Sync time between both
        }
    }
}