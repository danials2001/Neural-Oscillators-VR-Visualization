SIMPLE UNITY NEURAL DATA EXPORT
==============================

This folder contains simplified neural network data for Unity visualization.

FILES:
- metadata.txt: Basic parameters
- neural_positions.csv: 3D positions of neurons (100 rows x 3 columns)
- network_edges.csv: Network connections (source, target, weight)
- voltage_with_control.bin: Binary float32 data (time x neurons)
- voltage_without_control.bin: Binary float32 data (time x neurons)
- time_points.csv: Time values in milliseconds
- neuron_metrics.csv: Firing rate and control response per neuron
- SimpleNeuralDataLoader.cs: Unity C# script to load and visualize

UNITY SETUP:
1. Create a new Unity project
2. Create a "StreamingAssets" folder in Assets
3. Copy this entire folder into StreamingAssets
4. Add SimpleNeuralDataLoader.cs to an empty GameObject
5. Press Play to see the network

DATA FORMAT:
- Neurons: 100
- Time points: 49 (downsampled)
- Time step: 8.74000 ms
- Total time: 419.5 ms

VISUALIZATION IDEAS:
- Color neurons by firing rate or control response
- Animate voltage over time
- Show edges with width proportional to weight
- Add particle effects for spikes
- Use VR controllers to select and inspect neurons
