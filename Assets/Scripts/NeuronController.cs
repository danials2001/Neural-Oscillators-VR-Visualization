using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NeuronController : MonoBehaviour
{
    public NeuronData neuronData { get; private set; }
    private int neuronIndex;
    
    // Visual components
    private Renderer neuronRenderer;
    private Transform visualTransform;
    private GameObject infoPanel;
    private TextMeshProUGUI infoText;
    
    // Effects
    private ParticleSystem desyncEffect;
    private Light pulseLight;
    
    // States
    private bool isHovered = false;
    private bool isSelected = false;
    private Vector3 basePosition;
    private Color baseColor;
    
    // Animation
    private float hoverScale = 1.5f;
    private float pulseSpeed = 2f;
    private float desyncRiseHeight = 2f;
    
    public void Initialize(NeuronData data, int index)
    {
        neuronData = data;
        neuronIndex = index;
        
        // Cache components
        neuronRenderer = GetComponent<Renderer>();
        visualTransform = transform.GetChild(0); // Assuming visual is first child
        basePosition = transform.localPosition;
        baseColor = neuronRenderer.material.color;
        
        // Create info panel
        CreateInfoPanel();
        
        // Create effects
        CreateEffects();
        
        // Make interactable
        gameObject.layer = LayerMask.NameToLayer("XRInteractable");
        
        // Add XR components if needed
        if (!GetComponent<UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable>())
        {
            var grabInteractable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable>();
            grabInteractable.movementType = UnityEngine.XR.Interaction.Toolkit.XRBaseInteractable.MovementType.VelocityTracking;
        }
    }
    
    void CreateInfoPanel()
    {
        // Create canvas for info
        GameObject canvasObj = new GameObject("InfoCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.up * 0.5f;
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(2, 1);
        canvas.transform.localScale = Vector3.one * 0.01f;
        
        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(canvasObj.transform);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // Text
        GameObject textObj = new GameObject("InfoText");
        textObj.transform.SetParent(canvasObj.transform);
        infoText = textObj.AddComponent<TextMeshProUGUI>();
        infoText.text = GenerateInfoText();
        infoText.fontSize = 24;
        infoText.color = Color.white;
        infoText.alignment = TextAlignmentOptions.Center;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        infoPanel = canvasObj;
        infoPanel.SetActive(false);
    }
    
    void CreateEffects()
    {
        // Desync particle effect
        GameObject particleObj = new GameObject("DesyncEffect");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;
        
        desyncEffect = particleObj.AddComponent<ParticleSystem>();
        var main = desyncEffect.main;
        main.startLifetime = 2f;
        main.startSpeed = 1f;
        main.startSize = 0.1f;
        main.startColor = Color.yellow;
        
        var emission = desyncEffect.emission;
        emission.enabled = false;
        
        var shape = desyncEffect.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;
        
        // Pulse light
        GameObject lightObj = new GameObject("PulseLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.zero;
        
        pulseLight = lightObj.AddComponent<Light>();
        pulseLight.type = LightType.Point;
        pulseLight.color = Color.white;
        pulseLight.intensity = 0;
        pulseLight.range = 2f;
    }
    
    string GenerateInfoText()
    {
        return $"Neuron {neuronData.id}\n" +
               $"Type: {neuronData.response_type}\n" +
               $"Cluster: {neuronData.final_cluster}\n" +
               $"Desync: {neuronData.desync_time:F2}ms\n" +
               $"Controls: {neuronData.control_count}";
    }
    
    public void UpdateVisualization(int timeIndex, float currentTime)
    {
        if (neuronData == null) return;
        
        // Update position based on voltage
        float voltage = neuronData.V_wcwn[timeIndex];
        float normalizedV = (voltage + 80f) / 60f; // Normalize roughly -80 to -20mV
        
        // Vertical displacement based on voltage
        Vector3 newPos = basePosition;
        newPos.y = normalizedV * 0.5f;
        
        // Add desync displacement
        if (currentTime > neuronData.desync_time)
        {
            float timeSinceDesync = currentTime - neuronData.desync_time;
            float rise = Mathf.Min(timeSinceDesync / 1000f, 1f) * desyncRiseHeight;
            newPos.y += rise;
        }
        
        transform.localPosition = Vector3.Lerp(transform.localPosition, newPos, Time.deltaTime * 5f);
        
        // Pulse on spike (simple threshold detection)
        if (voltage > 0)
        {
            StartCoroutine(SpikePulse());
        }
    }
    
    System.Collections.IEnumerator SpikePulse()
    {
        pulseLight.intensity = 2f;
        float timer = 0;
        
        while (timer < 0.2f)
        {
            timer += Time.deltaTime;
            pulseLight.intensity = Mathf.Lerp(2f, 0f, timer / 0.2f);
            yield return null;
        }
        
        pulseLight.intensity = 0;
    }
    
    public void OnHover(bool hovering)
    {
        isHovered = hovering;
        
        // Scale effect
        float targetScale = hovering ? hoverScale : 1f;
        visualTransform.localScale = Vector3.one * targetScale;
        
        // Show info panel
        if (infoPanel != null)
        {
            infoPanel.SetActive(hovering);
        }
        
        // Highlight effect
        if (hovering)
        {
            neuronRenderer.material.SetColor("_EmissionColor", baseColor * 2f);
        }
        else
        {
            neuronRenderer.material.SetColor("_EmissionColor", Color.black);
        }
    }
    
    public void OnSelect()
    {
        isSelected = !isSelected;
        
        // Could trigger detailed view, trajectory highlight, etc.
        Debug.Log($"Selected Neuron {neuronData.id}: {neuronData.response_type}");
        
        // Highlight trajectory
        TrajectoryRenderer trajectory = transform.parent.Find($"Trajectory_{neuronData.id}")?.GetComponent<TrajectoryRenderer>();
        if (trajectory != null)
        {
            // Make trajectory more prominent
        }
    }
    
    public void ShowDesyncEffect(float timeSinceDesync)
    {
        // Particle burst on desync
        if (timeSinceDesync < 0.1f && desyncEffect != null)
        {
            desyncEffect.Emit(20);
        }
        
        // Color shift
        float desyncProgress = Mathf.Clamp01(timeSinceDesync / 2f);
        Color desyncColor = Color.Lerp(baseColor, Color.red, desyncProgress);
        neuronRenderer.material.color = desyncColor;
    }
    
    public void ShowClustering(bool show)
    {
        if (show)
        {
            // Color by cluster
            int cluster = neuronData.final_cluster;
            Color[] clusterColors = { Color.red, Color.green, Color.blue };
            neuronRenderer.material.color = clusterColors[cluster % clusterColors.Length];
        }
        else
        {
            // Restore base color
            neuronRenderer.material.color = baseColor;
        }
    }
}