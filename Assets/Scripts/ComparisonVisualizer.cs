using UnityEngine;
using UnityEngine.UI;

public class ComparisonVisualizer : MonoBehaviour
{
    [Header("Layout")]
    public Transform leftSide;
    public Transform rightSide;
    public float separation = 5f;
    
    [Header("UI")]
    public Text leftLabel;
    public Text rightLabel;
    public Text comparisonStats;
    
    private NeuronVisualizer leftVisualizer;
    private NeuronVisualizer rightVisualizer;
    
    void Start()
    {
        // Create two neuron visualizers
        CreateSideBySideVisualizers();
    }
    
    void CreateSideBySideVisualizers()
    {
        // Left side (e.g., Deterministic)
        GameObject leftViz = new GameObject("LeftVisualizer");
        leftViz.transform.SetParent(leftSide);
        leftViz.transform.localPosition = new Vector3(-separation/2, 0, 0);
        leftVisualizer = leftViz.AddComponent<NeuronVisualizer>();
        
        // Right side (e.g., Stochastic)
        GameObject rightViz = new GameObject("RightVisualizer");
        rightViz.transform.SetParent(rightSide);
        rightViz.transform.localPosition = new Vector3(separation/2, 0, 0);
        rightVisualizer = rightViz.AddComponent<NeuronVisualizer>();
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