using UnityEngine;
using TMPro;

public class InfoPopupController : MonoBehaviour {
    public TextMeshProUGUI text;

    public void SetText(string content) {
        text.text = content;
    }
}