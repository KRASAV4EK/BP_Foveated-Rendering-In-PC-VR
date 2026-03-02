using UnityEngine;
using TMPro;

public class VRDebugText : MonoBehaviour
{
    public TextMeshProUGUI debugText;

    void Start()
    {
        if (debugText != null)
            debugText.text = "";
    }

    public void Log(string message)
    {
        Debug.Log(message); 
        if (debugText != null)
            debugText.text = message;
    }

    public void LogContd(string message)
    {
        Debug.Log(message);
        if (debugText != null)
            debugText.text += message;
    }
}
