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
    public int[,] evolution;
    public float[] time_points;
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