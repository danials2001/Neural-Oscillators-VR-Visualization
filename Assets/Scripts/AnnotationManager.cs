using UnityEngine;

public class AnnotationManager : MonoBehaviour {
    public static AnnotationManager Instance;

    public GameObject annotationPrefab;
    private GameObject currentAnnotation;

    private void Awake() {
        Instance = this;
    }

    public void ShowAnnotation(Vector3 position, string message) {
        if (currentAnnotation != null) Destroy(currentAnnotation);

        currentAnnotation = Instantiate(annotationPrefab);
        currentAnnotation.transform.position = position + Vector3.up * 0.3f;

        TextMesh textMesh = currentAnnotation.GetComponent<TextMesh>();
        textMesh.text = message;
    }
}
