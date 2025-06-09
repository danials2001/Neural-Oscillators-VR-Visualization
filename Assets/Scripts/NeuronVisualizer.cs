using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
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
    public Slider timeSlider;
    public Text timeText;
    public Toggle playToggle;
    public float playbackSpeed = 1f;
    
    [Header("Display Options")]
    public Toggle showTrajectoriesToggle;
    public Toggle showClusteringToggle;
    public Toggle showDesyncWaveToggle;
    public Dropdown trajectoryModeDropdown; // WCWN, WOCWN, Both, Divergence
    
    [Header("XR Interaction")]
    public XRRayInteractor leftRay;
    public XRRayInteractor rightRay;
    
    private NeuralData data;
    private List<GameObject> neuronObjects = new List<GameObject>();
    private List<TrajectoryRenderer> trajectories = new List<TrajectoryRenderer>();
    private Dictionary<int, NeuronController> neuronControllers = new Dictionary<int, NeuronController>();
    
    private int currentTimeIndex = 0;
    private bool isPlaying = false;
    private float playbackTimer = 0;
    
    // Visualization state
    private bool showingTrajectories = true;
    private bool showingClustering = true;
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
            neuronObj.name = $"Neuron_{neuronData.id}";
            
            // Add controller
            NeuronController controller = neuronObj.AddComponent<NeuronController>();
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
        if (leftRay != null && leftRay.isActiveAndEnabled)
        {
            // Primary button (typically A/X button)
            if (leftRay.xrController.inputDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool primaryPressed) && primaryPressed)
            {
                showTrajectoriesToggle.isOn = !showTrajectoriesToggle.isOn;
            }

            // Secondary button (typically B/Y button)
            if (leftRay.xrController.inputDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool secondaryPressed) && secondaryPressed)
            {
                playToggle.isOn = !playToggle.isOn;
            }
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
    
    void UpdateVisualization()
    {
        if (data == null) return;
        
        float currentTime = data.time[currentTimeIndex];
        timeText.text = $"Time: {currentTime:F2} ms";
        
        // Update neurons
        foreach (var kvp in neuronControllers)
        {
            var controller = kvp.Value;
            controller.UpdateVisualization(currentTimeIndex, currentTime);
            
            // Desync wave effect
            if (showingDesyncWave && controller.neuronData.desync_time <= currentTime)
            {
                float timeSinceDesync = currentTime - controller.neuronData.desync_time;
                controller.ShowDesyncEffect(timeSinceDesync);
            }
        }
        
        // Update trajectories
        foreach (var traj in trajectories)
        {
            traj.UpdateTime(currentTimeIndex);
            traj.gameObject.SetActive(showingTrajectories);
        }
        
        // Update sync indicator
        UpdateSyncIndicator();
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