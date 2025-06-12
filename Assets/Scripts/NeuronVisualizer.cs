using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
//using Oculus;
//using Oculus.Platform;


public class NeuronVisualizer : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject neuronPrefab;
    public LineRenderer trajectoryPrefab;
    public GameObject clusterCenterPrefab;
    
    [Header("Materials")]
    public Material[] clusterMaterials;
    public Material superResponderMaterial;
    public Material resistantMaterial;
    public Gradient syncGradient;
    
    [Header("Layout")]
    public float neuronSpacing = 0.5f;
    public float trajectoryScale = 10f;
    public Transform visualizationRoot;
    
    [Header("Time Control")]
    public UnityEngine.UI.Slider timeSlider;
    public TMP_Text timeText;
    public UnityEngine.UI.Toggle playToggle;
    public float playbackSpeed = 1f;
    public TextMeshPro IndividualNeuronTextData;

    [Header("Display Options")]
    public UnityEngine.UI.Toggle showTrajectoriesToggle;
    public UnityEngine.UI.Toggle showClusteringToggle;
    public UnityEngine.UI.Toggle showDesyncWaveToggle;
    public TMP_Dropdown trajectoryModeDropdown; // WCWN, WOCWN, Both, Divergence
    
    [Header("XR Interaction")]
    public XRRayInteractor leftRay;
    public XRRayInteractor rightRay;
    public ActionBasedController controller;
    public InputHelpers.Button activationButton = InputHelpers.Button.Trigger;
    public float activationThreshold = 0.1f;
    
    private NeuralData data;
    private List<GameObject> neuronObjects = new List<GameObject>();
    private List<TrajectoryRenderer> trajectories = new List<TrajectoryRenderer>();
    private Dictionary<int, NeuronController> neuronControllers = new Dictionary<int, NeuronController>();
    
    private int currentTimeIndex = 0;
    private bool isPlaying = false;
    private float playbackTimer = 0;
    
    // Visualization state
    private bool showingTrajectories = false;
    private bool showingClustering = false;
    private bool showingDesyncWave = false;
    private int trajectoryMode = 0; // 0=WCWN, 1=WOCWN, 2=Both, 3=Divergence
    
    void Start()
    {
        SetupUI();
        SetupXRInteraction();
    }
    
    void SetupUI()
    {
        timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);
        playToggle.onValueChanged.AddListener(OnPlayToggleChanged);
        showTrajectoriesToggle.onValueChanged.AddListener(OnShowTrajectoriesChanged);
        showClusteringToggle.onValueChanged.AddListener(OnShowClusteringChanged);
        showDesyncWaveToggle.onValueChanged.AddListener(OnShowDesyncWaveChanged);
        trajectoryModeDropdown.onValueChanged.AddListener(OnTrajectoryModeChanged);
    }
    
    void SetupXRInteraction()
    {
        // Setup hover and selection events
        if (leftRay != null)
        {
            leftRay.hoverEntered.AddListener(OnHoverEnter);
            leftRay.hoverExited.AddListener(OnHoverExit);
            leftRay.selectEntered.AddListener(OnSelect);
        }
    }
    
    public void LoadData(NeuralData newData)
    {
        ClearVisualization();
        data = newData;
        Debug.Log("Clusttering" + data.clustering.ToString());
        if (data == null || data.neurons == null) return;
        
        CreateNeurons();
        CreateTrajectories();
        UpdateTimeSlider();
        UpdateVisualization();
    }
    
    void ClearVisualization()
    {
        foreach (var obj in neuronObjects)
        {
            Destroy(obj);
        }
        neuronObjects.Clear();
        trajectories.Clear();
        neuronControllers.Clear();
    }
    
    void CreateNeurons()
    {
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(data.neurons.Count));
        
        for (int i = 0; i < data.neurons.Count; i++)
        {
            var neuronData = data.neurons[i];
            
            // Grid position
            int row = i / gridSize;
            int col = i % gridSize;
            Vector3 basePos = new Vector3(
                (col - gridSize / 2f) * neuronSpacing,
                0,
                (row - gridSize / 2f) * neuronSpacing
            );
            
            // Create neuron object
            GameObject neuronObj = Instantiate(neuronPrefab, visualizationRoot);
            neuronObj.transform.localPosition = basePos;
            neuronObj.transform.localScale = Vector3.one * 0.2f; // or 0.1
            neuronObj.name = $"Neuron_{neuronData.id}";
            
            // Add controller
            NeuronController controller = neuronObj.AddComponent<NeuronController>();
            controller.SharedInfoText = IndividualNeuronTextData;
            controller.Initialize(neuronData, i);
            
            // Set material based on response type
            Renderer renderer = neuronObj.GetComponent<Renderer>();
            if (neuronData.response_type == "super_responder")
            {
                renderer.material = superResponderMaterial;
            }
            else if (neuronData.response_type == "resistant")
            {
                renderer.material = resistantMaterial;
            }
            else
            {
                renderer.material = clusterMaterials[neuronData.final_cluster % clusterMaterials.Length];
            }
            
            Rigidbody rb = neuronObj.GetComponent<Rigidbody>();
            if (rb != null)
                Destroy(rb);
            
            neuronObjects.Add(neuronObj);
            neuronControllers[neuronData.id] = controller;
        }
    }
    
    void CreateTrajectories()
    {
        foreach (var neuronData in data.neurons)
        {
            GameObject trajObj = new GameObject($"Trajectory_{neuronData.id}");
            trajObj.transform.SetParent(visualizationRoot);
            
            TrajectoryRenderer traj = trajObj.AddComponent<TrajectoryRenderer>();
            traj.Initialize(neuronData, data.time, trajectoryScale);
            traj.SetVisualizationMode(trajectoryMode);
            
            trajectories.Add(traj);
        }
    }
    
    void UpdateTimeSlider()
    {
        if (data != null && data.time != null)
        {
            timeSlider.minValue = 0;
            timeSlider.maxValue = data.time.Length - 1;
            timeSlider.wholeNumbers = true;
        }
    }

    void Update()
    {
        if (isPlaying && data != null)
        {
            playbackTimer += Time.deltaTime * playbackSpeed;

            if (playbackTimer >= 0.1f) // Update every 100ms
            {
                playbackTimer = 0;
                currentTimeIndex++;

                if (currentTimeIndex >= data.time.Length)
                {
                    currentTimeIndex = 0; // Loop
                }

                timeSlider.value = currentTimeIndex;
                UpdateVisualization();
            }
        }
        if (rightRay != null && IsActivated())
        {
            CheckForXRRaycastHit();
        }



        // XR Controller shortcuts
        // if (OVRInput.GetDown(OVRInput.Button.One)) // A button
        // {
        //     showTrajectoriesToggle.isOn = !showTrajectoriesToggle.isOn;
        // }
        // if (OVRInput.GetDown(OVRInput.Button.Two)) // B button
        // {
        //     playToggle.isOn = !playToggle.isOn;
        // }
       
    }
    
    // Find current clustering window
    int GetClusteringWindow(float currentTimeMs)
    {
        if (data.clustering?.time_points == null) return 0;

        for (int i = 0; i < data.clustering.time_points.Length - 1; i++)
        {
            if (currentTimeMs >= data.clustering.time_points[i] &&
                currentTimeMs < data.clustering.time_points[i + 1])
            {
                return i;
            }
        }
        return data.clustering.time_points.Length - 1;
    }

    // Update neuron colors based on time
    void UpdateClusterColors(float currentTime)
    {
        if (!showingClustering || data.clustering?.evolution == null) return;

        int clusterWindow = GetClusteringWindow(currentTime);

        for (int i = 0; i < neuronObjects.Count; i++)
        {
            int cluster = data.clustering.GetCluster(i, clusterWindow);

            Renderer renderer = neuronObjects[i].GetComponent<Renderer>();
            Material oldMat = renderer.material;
            if (renderer != null && cluster < clusterMaterials.Length)
            {
                if (clusterMaterials[cluster] != oldMat)
                {
                    Debug.Log($"Cluster changed to {clusterMaterials[cluster]} from cluster {oldMat}");
                }
                renderer.material = clusterMaterials[cluster];
            }
        }
    }

    
    bool IsActivated()
    {
        if (controller == null || controller.activateActionValue == null)
            return false;

        return controller.activateActionValue.action.ReadValue<float>() > activationThreshold;
    }
    
    void CheckForXRRaycastHit()
    {
        if (!rightRay.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            return;

        Debug.Log("XR ray hit: " + hit.collider?.name);

        MeshCollider meshCollider = hit.collider as MeshCollider;
        if (meshCollider == null || meshCollider.sharedMesh == null)
            return;

        Mesh mesh = meshCollider.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        int triangleIndex = hit.triangleIndex;

        Vector3 p0 = vertices[triangles[triangleIndex * 3 + 0]];
        Vector3 p1 = vertices[triangles[triangleIndex * 3 + 1]];
        Vector3 p2 = vertices[triangles[triangleIndex * 3 + 2]];

        Transform hitTransform = hit.collider.transform;
        p0 = hitTransform.TransformPoint(p0);
        p1 = hitTransform.TransformPoint(p1);
        p2 = hitTransform.TransformPoint(p2);

        Debug.DrawLine(p0, p1, Color.red, 2f);
        Debug.DrawLine(p1, p2, Color.green, 2f);
        Debug.DrawLine(p2, p0, Color.blue, 2f);

        GameObject triObj = new GameObject("XRDebugTriangle");
        MeshFilter mf = triObj.AddComponent<MeshFilter>();
        MeshRenderer mr = triObj.AddComponent<MeshRenderer>();

        Mesh triMesh = new Mesh();
        triMesh.vertices = new Vector3[] { p0, p1, p2 };
        triMesh.triangles = new int[] { 0, 1, 2 };
        triMesh.RecalculateNormals();
        triMesh.RecalculateBounds();
        mf.mesh = triMesh;

        Material debugMat = new Material(Shader.Find("Unlit/Color"));
        debugMat.color = Color.yellow;
        mr.material = debugMat;

        Destroy(triObj, 5f);
    }
    
    void UpdateVisualization()
    {
        if (data == null) return;

        float currentTime = data.time[currentTimeIndex];
        timeText.text = $"Time: {currentTime:F2} ms";

        // 1. UPDATE CLUSTER COLORS (if enabled)
        if (showingClustering && data.clustering != null)
        {
            UpdateClusterColors(currentTime);
        }

        // 2. UPDATE EACH NEURON
        foreach (var kvp in neuronControllers)
        {
            var controller = kvp.Value;
            controller.UpdateVisualization(currentTimeIndex, currentTime);

            // Show desync wave effect (if enabled)
            if (showingDesyncWave && controller.neuronData.desync_time <= currentTime)
            {
                float timeSinceDesync = currentTime - controller.neuronData.desync_time;
                if (timeSinceDesync < 1f)
                {
                    controller.ShowDesyncEffect(timeSinceDesync);
                }
            }
        }

        // 3. UPDATE TRAJECTORIES
        foreach (var traj in trajectories)
        {
            traj.UpdateTime(currentTimeIndex);
            traj.gameObject.SetActive(showingTrajectories);
        }

        // 4. UPDATE GLOBAL ENVIRONMENT (fog, control, lighting)
        UpdateGlobalEffects(currentTime);

        // 5. UPDATE SYNC INDICATOR
        UpdateSyncIndicator();
    }

    
    /*void UpdateVisualization()
    {
        if (data == null) return;

        float currentTime = data.time[currentTimeIndex];
        timeText.text = $"Time: {currentTime:F2} ms";

        // 1. UPDATE CLUSTERING COLORS
        UpdateClusterColors(currentTime);

        // 2. UPDATE EACH NEURON
        for (int i = 0; i < neuronObjects.Count; i++)
        {
            GameObject neuronObj = neuronObjects[i];
            NeuronController controller = neuronControllers[i + 1]; // 1-based IDs
            NeuronData neuronData = data.neurons[i];

            // === POSITION UPDATES ===
            Vector3 newPos = controller.basePosition;

            // Voltage -> Height
            float voltage = neuronData.V_wcwn[currentTimeIndex];
            float normalizedVoltage = (voltage + 80f) / 60f; // -80 to -20 mV -> 0 to 1
            newPos.y = normalizedVoltage * 0.5f;

            // Gating variable -> Phase space offset
            float n = neuronData.n_wcwn[currentTimeIndex];
            newPos.x += (n - 0.5f) * 0.1f; // Small offset
            newPos.z += (n - 0.5f) * 0.1f;

            // Desynchronization elevation
            if (currentTime > neuronData.desync_time)
            {
                float timeSinceDesync = currentTime - neuronData.desync_time;
                float desyncProgress = Mathf.Clamp01(timeSinceDesync / 200f);
                newPos.y += desyncProgress * 2f; // Rise up to 2 units

                // Trigger desync effect once
                if (timeSinceDesync < 1f)
                {
                    controller.TriggerDesyncEffect();
                }
            }

            // Apply position with smoothing
            controller.transform.localPosition = Vector3.Lerp(
                controller.transform.localPosition,
                newPos,
                Time.deltaTime * 5f
            );

            // === VISUAL EFFECTS ===
            Renderer renderer = neuronObj.GetComponent<Renderer>();

            // Divergence -> Emission intensity
            float divergence = neuronData.divergence[currentTimeIndex];
            float maxDivergence = 10f; // Adjust based on your data
            float emissionIntensity = divergence / maxDivergence;

            if (renderer.material.HasProperty("_EmissionColor"))
            {
                Color baseColor = clusterMaterials[0].color; // or current cluster color
                renderer.material.SetColor("_EmissionColor", baseColor * emissionIntensity);
            }

            // Spike detection -> Pulse
            if (voltage > 0) // Spike threshold
            {
                controller.TriggerSpikePulse();
            }

            // Control response visualization
            float controlMagnitude = Mathf.Abs(data.control[currentTimeIndex]);
            if (controlMagnitude > 0.1f && i < 3) // Show on hub neurons
            {
                controller.ShowControlResponse(controlMagnitude);
            }
        }

        // 3. UPDATE GLOBAL ENVIRONMENT
        UpdateGlobalEffects(currentTime);

        // 4. UPDATE TRAJECTORIES
        if (showingTrajectories)
        {
            UpdateTrajectoryVisualizations(currentTimeIndex);
        }
    }*/

    void UpdateGlobalEffects(float currentTime)
    {
        // 1. Synchronization level -> Fog, ambient lighting, color shift
        if (data.sync != null && currentTimeIndex < data.sync.r_wcwn.Length)
        {
            float syncLevel = data.sync.r_wcwn[currentTimeIndex];

            // Fog density inversely reflects sync level
            RenderSettings.fogDensity = 0.02f * (1f - syncLevel);

            // Color shift from red (desync) to blue (sync)
            Color fogColor = Color.Lerp(Color.red, Color.blue, syncLevel);
            RenderSettings.fogColor = fogColor;

            // Ambient light intensity
            RenderSettings.ambientIntensity = 0.5f + syncLevel * 0.5f;
        }

        // 2. Optional: You can log control signal for debugging or later use
        // float controlSignal = data.control[currentTimeIndex];
        // if (Mathf.Abs(controlSignal) > 0.1f)
        // {
        //     Debug.Log($"Control signal active: {controlSignal}");
        // }
    }

    
    void UpdateSyncIndicator()
    {
        if (data.sync != null && currentTimeIndex < data.sync.r_wcwn.Length)
        {
            float syncLevel = data.sync.r_wcwn[currentTimeIndex];
            // Update global lighting or background based on sync level
            RenderSettings.fogColor = syncGradient.Evaluate(syncLevel);
        }
    }
    
    // UI Callbacks
    void OnTimeSliderChanged(float value)
    {
        currentTimeIndex = Mathf.RoundToInt(value);
        UpdateVisualization();
    }
    
    void OnPlayToggleChanged(bool playing)
    {
        isPlaying = playing;
    }
    
    void OnShowTrajectoriesChanged(bool show)
    {
        showingTrajectories = show;
        UpdateVisualization();
    }
    
    void OnShowClusteringChanged(bool show)
    {
        showingClustering = show;
        // Update neuron colors based on clustering
        foreach (var controller in neuronControllers.Values)
        {
            controller.ShowClustering(show);
        }
    }
    
    void OnShowDesyncWaveChanged(bool show)
    {
        showingDesyncWave = show;
        UpdateVisualization();
    }
    
    void OnTrajectoryModeChanged(int mode)
    {
        trajectoryMode = mode;
        foreach (var traj in trajectories)
        {
            traj.SetVisualizationMode(mode);
        }
    }
    
    // XR Interaction
    void OnHoverEnter(HoverEnterEventArgs args)
    {
        Component comp = args.interactableObject as Component;
        if (comp != null)
        {
            NeuronController controller = comp.GetComponent<NeuronController>();
            if (controller != null)
            {
                controller.OnHover(true);
            }
        }
    }

    void OnHoverExit(HoverExitEventArgs args)
    {
        Component comp = args.interactableObject as Component;
        if (comp != null)
        {
            NeuronController controller = comp.GetComponent<NeuronController>();
            if (controller != null)
            {
                controller.OnHover(false);
            }
        }
    }

    void OnSelect(SelectEnterEventArgs args)
    {
        Component comp = args.interactableObject as Component;
        if (comp != null)
        {
            NeuronController controller = comp.GetComponent<NeuronController>();
            if (controller != null)
            {
                controller.OnSelect();
            }
        }
    }

}