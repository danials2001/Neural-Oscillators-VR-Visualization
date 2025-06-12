using UnityEngine;
using System.Collections.Generic;

public class TrajectoryRenderer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private LineRenderer divergenceRenderer;
    private NeuronData neuronData;
    private float[] timeArray;
    private float scale;
    private int visualizationMode = 0;
    
    // Trajectory colors
    public Color wcwnColor = new Color(0.2f, 0.6f, 1f, 0.8f);
    public Color wocwnColor = new Color(1f, 0.4f, 0.2f, 0.8f);
    public Gradient divergenceGradient;
    
    public void Initialize(NeuronData data, float[] time, float trajectoryScale)
    {
        neuronData = data;
        timeArray = time;
        scale = trajectoryScale;
        
        CreateLineRenderers();
        UpdateTrajectory();
    }
    
    void CreateLineRenderers()
    {
        // Main trajectory
        GameObject mainLine = new GameObject("MainTrajectory");
        mainLine.transform.SetParent(transform);
        lineRenderer = mainLine.AddComponent<LineRenderer>();
        lineRenderer.positionCount = neuronData.V_wcwn.Length;
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = wcwnColor;
        lineRenderer.endColor = wcwnColor;
        
        // Divergence trajectory
        GameObject divLine = new GameObject("DivergenceTrajectory");
        divLine.transform.SetParent(transform);
        divergenceRenderer = divLine.AddComponent<LineRenderer>();
        divergenceRenderer.positionCount = neuronData.V_wcwn.Length;
        divergenceRenderer.startWidth = 0.03f;
        divergenceRenderer.endWidth = 0.03f;
        divergenceRenderer.material = new Material(Shader.Find("Sprites/Default"));
        
        // Setup divergence gradient
        divergenceGradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0] = new GradientColorKey(Color.green, 0.0f);
        colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);
        colorKeys[2] = new GradientColorKey(Color.red, 1.0f);
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(0.8f, 0.0f);
        alphaKeys[1] = new GradientAlphaKey(0.8f, 1.0f);
        
        divergenceGradient.SetKeys(colorKeys, alphaKeys);
    }
    
    void UpdateTrajectory()
    {
        Vector3[] positions = new Vector3[neuronData.V_wcwn.Length];
        
        switch (visualizationMode)
        {
            case 0: // WCWN
                for (int i = 0; i < positions.Length; i++)
                {
                    positions[i] = new Vector3(
                        neuronData.V_wcwn[i] / 100f * scale,
                        neuronData.n_wcwn[i] * scale,
                        timeArray[i] / 1000f * scale
                    );
                }
                lineRenderer.SetPositions(positions);
                lineRenderer.enabled = true;
                divergenceRenderer.enabled = false;
                break;
                
            case 1: // WOCWN
                for (int i = 0; i < positions.Length; i++)
                {
                    positions[i] = new Vector3(
                        neuronData.V_wocwn[i] / 100f * scale,
                        neuronData.n_wocwn[i] * scale,
                        timeArray[i] / 1000f * scale
                    );
                }
                lineRenderer.SetPositions(positions);
                lineRenderer.startColor = wocwnColor;
                lineRenderer.endColor = wocwnColor;
                lineRenderer.enabled = true;
                divergenceRenderer.enabled = false;
                break;
                
            case 2: // Both
                // Show both trajectories
                CreateDualTrajectory();
                break;
                
            case 3: // Divergence
                ShowDivergence();
                break;
        }
    }
    
    void CreateDualTrajectory()
    {
        // Create two separate line renderers for WCWN and WOCWN
        Vector3[] wcwnPositions = new Vector3[neuronData.V_wcwn.Length];
        Vector3[] wocwnPositions = new Vector3[neuronData.V_wocwn.Length];
        
        for (int i = 0; i < wcwnPositions.Length; i++)
        {
            wcwnPositions[i] = new Vector3(
                neuronData.V_wcwn[i] / 100f * scale,
                neuronData.n_wcwn[i] * scale,
                timeArray[i] / 1000f * scale
            );
            
            wocwnPositions[i] = new Vector3(
                neuronData.V_wocwn[i] / 100f * scale,
                neuronData.n_wocwn[i] * scale,
                timeArray[i] / 1000f * scale
            );
        }
        
        lineRenderer.SetPositions(wcwnPositions);
        lineRenderer.startColor = wcwnColor;
        lineRenderer.endColor = wcwnColor;
        lineRenderer.enabled = true;
        
        // You'd need a second LineRenderer for WOCWN here
    }
    
    /*
    public void UpdateTimePosition(int timeIndex)
    {
        if (currentPositionMarker == null)
        {
            // Create a sphere to show current position
            currentPositionMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            currentPositionMarker.transform.parent = transform;
            currentPositionMarker.transform.localScale = Vector3.one * 0.15f;

            // Make it glow
            Renderer r = currentPositionMarker.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Unlit/Color"));
            r.material.color = Color.yellow;
        }

        // Move marker to current position
        if (timeIndex < lineRenderer.positionCount)
        {
            Vector3 currentPos = lineRenderer.GetPosition(timeIndex);
            currentPositionMarker.transform.position = currentPos;

            // Pulse effect
            float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.2f;
            currentPositionMarker.transform.localScale = Vector3.one * 0.15f * pulse;
        }

        // Fade trajectory based on time
        if (trajectoryMode == 3) // Divergence mode
        {
            UpdateDivergenceColors(timeIndex);
        }
    }*/

    void UpdateDivergenceColors(int currentTime)
    {
        // Create gradient showing past (faded) to present (bright)
        Gradient timeGradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[3];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];

        // Past - faded
        colorKeys[0] = new GradientColorKey(Color.gray, 0.0f);
        alphaKeys[0] = new GradientAlphaKey(0.3f, 0.0f);

        // Present - bright
        float currentT = (float)currentTime / lineRenderer.positionCount;
        colorKeys[1] = new GradientColorKey(Color.yellow, currentT);
        alphaKeys[1] = new GradientAlphaKey(1.0f, currentT);

        // Future - invisible
        colorKeys[2] = new GradientColorKey(Color.gray, 1.0f);
        alphaKeys[2] = new GradientAlphaKey(0.1f, 1.0f);

        timeGradient.SetKeys(colorKeys, alphaKeys);
        lineRenderer.colorGradient = timeGradient;
    }

    
    void ShowDivergence()
    {
        Vector3[] positions = new Vector3[neuronData.divergence.Length];
        Color[] colors = new Color[positions.Length];
        
        float maxDivergence = Mathf.Max(neuronData.divergence);
        
        for (int i = 0; i < positions.Length; i++)
        {
            // Position based on WCWN but height shows divergence
            positions[i] = new Vector3(
                neuronData.V_wcwn[i] / 100f * scale,
                neuronData.divergence[i] / maxDivergence * scale,
                timeArray[i] / 1000f * scale
            );
            
            // Color based on divergence magnitude
            float normalizedDiv = neuronData.divergence[i] / maxDivergence;
            colors[i] = divergenceGradient.Evaluate(normalizedDiv);
        }
        
        divergenceRenderer.SetPositions(positions);
        divergenceRenderer.colorGradient = CreateGradientFromColors(colors);
        divergenceRenderer.enabled = true;
        lineRenderer.enabled = false;
    }
    
    Gradient CreateGradientFromColors(Color[] colors)
    {
        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[Mathf.Min(colors.Length, 8)];
        
        for (int i = 0; i < colorKeys.Length; i++)
        {
            float time = (float)i / (colorKeys.Length - 1);
            int colorIndex = Mathf.FloorToInt(time * (colors.Length - 1));
            colorKeys[i] = new GradientColorKey(colors[colorIndex], time);
        }
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(0.8f, 0.0f);
        alphaKeys[1] = new GradientAlphaKey(0.8f, 1.0f);
        
        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }
    
    public void SetVisualizationMode(int mode)
    {
        visualizationMode = mode;
        UpdateTrajectory();
    }
    
    public void UpdateTime(int timeIndex)
    {
        // Could animate or highlight current position
        // For now, just ensure visibility
    }
}