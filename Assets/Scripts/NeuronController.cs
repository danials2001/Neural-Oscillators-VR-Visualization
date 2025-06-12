using System.Collections;
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
    public TextMeshPro SharedInfoText;
    
    private GameObject infoPanel;
    private TextMeshPro infoText;
    
    // Effects
    private ParticleSystem desyncEffect;
    private Light pulseLight;
    
    // States
    private bool isHovered = false;
    private bool isSelected = false;
    public Vector3 basePosition;
    private Color baseColor;
    
    // Animation
    private float hoverScale = 1.5f;
    private float pulseSpeed = 2f;
    private float desyncRiseHeight = 2f;
    private Vector3 baseScale;
    
    // Animation parameters
    private float spikeTimer = 0;
    private float controlResponseTimer = 0;
    private bool hasDesynced = false;

    
    public void Initialize(NeuronData data, int index)
    {
        neuronData = data;
        neuronIndex = index;
        
        // Cache components
        neuronRenderer = GetComponent<Renderer>();
        visualTransform = transform; // Assuming visual is first child
        basePosition = transform.localPosition;
        baseColor = neuronRenderer.material.color;
        baseScale = visualTransform.localScale;

        
        // Create info panel
        // CreateInfoPanel();
        
        // Create effects
        CreateEffects();
        
        // Make interactable
        // gameObject.layer = LayerMask.NameToLayer("XRInteractable");
        
        // Add XR components if needed
        if (!TryGetComponent(out UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable interactable))
        {
            interactable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable>();

            // Register XR interaction callbacks
            interactable.hoverEntered.AddListener(args => OnHover(true));
            interactable.hoverExited.AddListener(args => OnHover(false));
            interactable.selectEntered.AddListener(args => OnSelect());
        }
    }
    
    void CreateInfoPanel()
    {
        // Create canvas
        GameObject canvasObj = new GameObject("InfoCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.up * 0.5f;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 💡 Make it wider (e.g., 0.8 width and 0.25 height)
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(0.8f, 0.25f);
        canvasObj.transform.localScale = Vector3.one * 0.01f; // small in world space

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);

        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Text
        GameObject textObj = new GameObject("InfoText");
        textObj.transform.SetParent(canvasObj.transform, false);
        infoText = textObj.AddComponent<TextMeshPro>();
        infoText.text = GenerateInfoText();
        infoText.fontSize = 2.5f;
        infoText.color = Color.white;
        infoText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        infoPanel = canvasObj;
        infoPanel.SetActive(false);
    }
    
    public void TriggerSpikePulse()
    {
        if (spikeTimer <= 0)
        {
            spikeTimer = 0.2f;
            StartCoroutine(SpikePulseCoroutine());
        }
    }
    IEnumerator SpikePulseCoroutine()
    {
        // Scale pulse
        Vector3 originalScale = visualTransform.localScale;
        visualTransform.localScale = originalScale * 1.3f;

        // Light pulse
        if (pulseLight != null)
        {
            pulseLight.intensity = 2f;
        }

        // Particle burst
        /*if (spikeParticles != null)
        {
            spikeParticles.Emit(10);
        }*/

        // Animate back
        float timer = 0;
        while (timer < 0.2f)
        {
            timer += Time.deltaTime;
            float t = timer / 0.2f;

            visualTransform.localScale = Vector3.Lerp(
                originalScale * 1.3f,
                originalScale,
                t
            );

            if (pulseLight != null)
            {
                pulseLight.intensity = Mathf.Lerp(2f, 0f, t);
            }

            yield return null;
        }

        spikeTimer = 0;
    }

    public void TriggerDesyncEffect()
    {
        if (!hasDesynced)
        {
            hasDesynced = true;

            // Particle burst
            if (desyncEffect != null)
            {
                desyncEffect.Emit(50);
            }

            // Permanent color shift
            baseColor = Color.Lerp(baseColor, Color.red, 0.3f);

            // Add persistent glow
            neuronRenderer.material.EnableKeyword("_EMISSION");
        }
    }

    public void ShowControlResponse(float magnitude)
    {
        controlResponseTimer = magnitude;

        /*// Visual feedback for control application
        if (controlRing != null)
        {
            controlRing.SetActive(true);
            controlRing.transform.localScale = Vector3.one * magnitude * 2f;
        }*/
    }

    public void UpdateDivergenceGlow(float divergence, float maxDivergence)
    {
        float intensity = divergence / maxDivergence;

        // Set emission based on divergence
        if (neuronRenderer.material.HasProperty("_EmissionColor"))
        {
            Color emission = baseColor * intensity * 2f;
            neuronRenderer.material.SetColor("_EmissionColor", emission);
        }

        // Scale slightly based on divergence
        float scaleBoost = 1f + intensity * 0.2f;
        visualTransform.localScale = Vector3.one * scaleBoost;
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

        // --- Voltage -> Height ---
        float voltage = neuronData.V_wcwn[timeIndex];
        float normalizedV = (voltage + 80f) / 60f;

        Vector3 newPos = basePosition;
        newPos.y = normalizedV * 0.5f;

        // --- Gating variable (n) -> XY offset (optional phase-space) ---
        if (neuronData.n_wcwn != null && timeIndex < neuronData.n_wcwn.Length)
        {
            float n = neuronData.n_wcwn[timeIndex];
            newPos.x += (n - 0.5f) * 0.1f;
            newPos.z += (n - 0.5f) * 0.1f;
        }

        // --- Desync lift effect ---
        if (currentTime > neuronData.desync_time)
        {
            float timeSinceDesync = currentTime - neuronData.desync_time;
            float rise = Mathf.Clamp01(timeSinceDesync / 1000f) * desyncRiseHeight;
            newPos.y += rise;
        }

        // --- Smooth movement ---
        transform.localPosition = Vector3.Lerp(transform.localPosition, newPos, Time.deltaTime * 5f);

        // --- Emission color from divergence ---
        if (neuronData.divergence != null && timeIndex < neuronData.divergence.Length)
        {
            float div = neuronData.divergence[timeIndex];
            float intensity = Mathf.Clamp01(div / 10f);  // adjust scale if needed
            if (neuronRenderer.material.HasProperty("_EmissionColor"))
            {
                neuronRenderer.material.SetColor("_EmissionColor", baseColor * intensity);
            }
        }

        // --- Control pulse (optional visual cue) ---
        if (neuronData.control_count > 0)  // if applicable
        {
            float controlMag = Mathf.Abs(neuronData.control_count);  // placeholder if not indexed
            if (controlMag > 0.1f)
            {
                ShowControlResponse(controlMag);
            }
        }

        // --- Spike flash ---
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
        visualTransform.localScale = baseScale * targetScale;

        // Highlight effect
        if (hovering)
        {
            neuronRenderer.material.SetColor("_EmissionColor", baseColor * 2f);
            if (SharedInfoText != null)
            {
                SharedInfoText.text = GenerateInfoText();
            }
        }
        else
        {
            neuronRenderer.material.SetColor("_EmissionColor", Color.black);
            if (SharedInfoText != null)
            {
                SharedInfoText.text = ""; // Clear text when no longer hovered
            }
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