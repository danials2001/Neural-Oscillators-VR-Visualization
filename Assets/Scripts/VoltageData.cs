using System.IO;

public class VoltageData {
    public float[,] voltages; // [time, neuron]

    public void LoadFromBinary(string filepath) {
        voltages = new float[49, 100];

        using (FileStream fs = new FileStream(filepath, FileMode.Open))
        using (BinaryReader reader = new BinaryReader(fs)) {
            for (int t = 0; t < 49; t++) {
                for (int n = 0; n < 100; n++) {
                    voltages[t, n] = reader.ReadSingle();
                }
            }
        }
    }

    public float GetVoltage(int timeIndex, int neuronId) {
        return voltages[timeIndex, neuronId];
    }
}
