using UnityEngine;

public class NeuronInfo : MonoBehaviour {
    public int neuronIndex;
    public float lastVoltage;

    private void OnMouseDown() {
        Debug.Log($"Neuron {neuronIndex} clicked. Voltage: {lastVoltage:F2} mV");

        // Optional: show a floating label or UI popup here
        AnnotationManager.Instance.ShowAnnotation(transform.position, $"Neuron {neuronIndex}\nV={lastVoltage:F2} mV");
    }
}
