using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NeuralData
{
    public Metadata metadata;
    public float[] time;
    public float[] control;
    public SyncData sync;
    public ClusteringData clustering;
    public List<NeuronData> neurons;
    public PhaseSpaceData phase_space;
}

[Serializable]
public class Metadata
{
    public string filename;
    public float D_noise;
    public float alpha;
    public string network;
    public string control_type;

}

[Serializable]
public class SyncData
{
    public float[] r_wcwn;
    public float[] r_wocwn;
    public float[] difference;
}

[Serializable]
public class ClusteringData
{
    public int[] evolution;      // Now 1D array (flattened)
    public int num_neurons;      // Need these to reconstruct
    public int num_windows;
    public float[] time_points;

    // Private 2D array for reconstructed data
    private int[,] evolution2D = null;

    // Method to get the reconstructed 2D array
    public int[,] GetEvolution2D()
    {
        if (evolution2D == null && evolution != null)
        {
            // Reconstruct 2D array from flattened data
            evolution2D = new int[num_neurons, num_windows];

            for (int w = 0; w < num_windows; w++)
            {
                for (int n = 0; n < num_neurons; n++)
                {
                    evolution2D[n, w] = evolution[w * num_neurons + n];
                }
            }
        }
        return evolution2D;
    }

    // Helper to get cluster for specific neuron and window
    public int GetCluster(int neuronIdx, int windowIdx)
    {
        if (evolution == null && neuronIdx >= num_neurons && windowIdx >= num_windows)
            return 0;

        return evolution[windowIdx * num_neurons + neuronIdx];
    }
    
    public override string ToString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine("ClusteringData:");
        sb.AppendLine($"  Neurons: {num_neurons}");
        sb.AppendLine($"  Time Windows: {num_windows}");

        if (evolution == null)
        {
            sb.AppendLine("  Evolution: null");
            return sb.ToString();
        }

        sb.AppendLine("  Evolution (flattened): length = " + evolution.Length);

        // Preview first few entries from the reconstructed 2D array
        int[,] evo2D = GetEvolution2D();
        int previewNeurons = Mathf.Min(3, num_neurons);
        int previewWindows = Mathf.Min(5, num_windows);

        for (int n = 0; n < previewNeurons; n++)
        {
            sb.Append($"    Neuron {n}: ");
            for (int w = 0; w < previewWindows; w++)
            {
                sb.Append(evo2D[n, w]);
                if (w < previewWindows - 1) sb.Append(", ");
            }
            sb.AppendLine();
        }

        // Time points preview
        if (time_points != null)
        {
            sb.AppendLine($"  Time Points: {time_points.Length} total");
            sb.Append("    First few: ");
            for (int i = 0; i < Mathf.Min(5, time_points.Length); i++)
            {
                sb.Append($"{time_points[i]:F1}ms");
                if (i < Mathf.Min(5, time_points.Length) - 1) sb.Append(", ");
            }
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("  Time Points: null");
        }

        return sb.ToString();
    }

}


[Serializable]
public class NeuronData
{
    public int id;
    public float[] V_wcwn;
    public float[] V_wocwn;
    public float[] n_wcwn;
    public float[] n_wocwn;
    public float[] divergence;
    public float desync_time;
    public float desync_delay;
    public string response_type;
    public int control_count;
    public int final_cluster;
}

[Serializable]
public class PhaseSpaceData
{
    public float[,] V_grid;
    public float[,] n_grid;
    public float[,] density;
}

[Serializable]
public class LandscapePoint
{
    public float D;
    public float alpha;
    public string network;
    public float energy;
    public float desync_rate;
    public float mean_desync_time;
    public float efficiency;
}