using System.IO;
using UnityEngine;

public class PhiEvolutionReader : MonoBehaviour
{
    // Data storage
    private float[,,] phiData;  // [time, V_index, n_index]
    public int nV, nN, nTimeSteps;
    private int startTimeIdx, endTimeIdx;
    
    // Grid parameters (from your MATLAB code)
    private const float K = 100f;
    private float[] V_grid;  // Scaled voltage values
    private float[] n_grid;  // Gating variable values
    
    public void LoadPhiEvolution(float D_value)
    {
        string filename = $"phi_evolution_D{D_value:F1}.bin";
        string path = Path.Combine(Application.streamingAssetsPath, 
                                  "unity_export_phi_evolution", filename);
        
        if (!File.Exists(path))
        {
            Debug.LogError($"File not found: {path}");
            return;
        }
        
        using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
        {
            // Read header (5 int32 values = 20 bytes)
            nV = reader.ReadInt32();           // Should be 201
            nN = reader.ReadInt32();           // Should be 101
            nTimeSteps = reader.ReadInt32();   // 737 for D=0, 369 for others
            startTimeIdx = reader.ReadInt32(); // 0
            endTimeIdx = reader.ReadInt32();   // 736 or 368
            
            Debug.Log($"Loaded phi data: V={nV}, n={nN}, TimeSteps={nTimeSteps}");
            
            // Initialize grid arrays
            V_grid = new float[nV];
            n_grid = new float[nN];
            
            // Generate grid values (matching MATLAB)
            for (int i = 0; i < nV; i++)
            {
                V_grid[i] = Mathf.Lerp(-100f, 100f, i / (float)(nV - 1)) / K;
            }
            for (int j = 0; j < nN; j++)
            {
                n_grid[j] = j / (float)(nN - 1);
            }
            
            // Allocate 3D array
            phiData = new float[nTimeSteps, nV, nN];
            
            // Read all time slices
            for (int t = 0; t < nTimeSteps; t++)
            {
                // Data is stored in row-major order (MATLAB transposes for this)
                for (int i = 0; i < nV; i++)
                {
                    for (int j = 0; j < nN; j++)
                    {
                        phiData[t, i, j] = reader.ReadSingle();
                    }
                }
                
                if (t % 50 == 0)  // Progress update
                {
                    Debug.Log($"Loaded time step {t}/{nTimeSteps}");
                }
            }
        }
        
        Debug.Log($"Successfully loaded phi evolution for D={D_value}");
    }
    
    // Get phi value at specific point and time
    public float GetPhiValue(int timeStep, int vIndex, int nIndex)
    {
        if (phiData == null) return 0f;
        
        // Bounds checking
        timeStep = Mathf.Clamp(timeStep, 0, nTimeSteps - 1);
        vIndex = Mathf.Clamp(vIndex, 0, nV - 1);
        nIndex = Mathf.Clamp(nIndex, 0, nN - 1);
        
        return phiData[timeStep, vIndex, nIndex];
    }
    
    // Get 2D surface at specific time
    public float[,] GetPhiSurfaceAtTime(int timeStep)
    {
        float[,] surface = new float[nV, nN];
        
        for (int i = 0; i < nV; i++)
        {
            for (int j = 0; j < nN; j++)
            {
                surface[i, j] = phiData[timeStep, i, j];
            }
        }
        
        return surface;
    }
    
    // Convert grid indices to physical values
    public Vector2 GridToPhysical(int vIndex, int nIndex)
    {
        float V_physical = V_grid[vIndex] * K;  // Unscale voltage
        float n_physical = n_grid[nIndex];
        return new Vector2(V_physical, n_physical);
    }
    
    // Get interpolated phi value at any (V,n) point
    public float GetPhiInterpolated(float V_physical, float n_value, int timeStep)
    {
        // Convert physical V to grid V
        float V_scaled = V_physical / K;
        
        // Find nearest grid points
        float v_idx_float = Mathf.InverseLerp(-1f, 1f, V_scaled) * (nV - 1);
        float n_idx_float = n_value * (nN - 1);
        
        int v0 = Mathf.FloorToInt(v_idx_float);
        int v1 = Mathf.Min(v0 + 1, nV - 1);
        int n0 = Mathf.FloorToInt(n_idx_float);
        int n1 = Mathf.Min(n0 + 1, nN - 1);
        
        float vt = v_idx_float - v0;
        float nt = n_idx_float - n0;
        
        // Bilinear interpolation
        float phi00 = phiData[timeStep, v0, n0];
        float phi10 = phiData[timeStep, v1, n0];
        float phi01 = phiData[timeStep, v0, n1];
        float phi11 = phiData[timeStep, v1, n1];
        
        float phi0 = Mathf.Lerp(phi00, phi10, vt);
        float phi1 = Mathf.Lerp(phi01, phi11, vt);
        
        return Mathf.Lerp(phi0, phi1, nt);
    }
    
    // Important: HJB is solved backward in time!
    public float GetActualTime(int timeIndex)
    {
        // Index 0 = final time (t=7ms)
        // Index 368/736 = initial time (t=0ms)
        float totalTime = 7.0f; // 7ms total
        float dt = totalTime / (nTimeSteps - 1);
        return totalTime - (timeIndex * dt);
    }
}
