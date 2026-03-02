using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class InputManager : MonoBehaviour
{
    [Header("Input")]
    public KeyCode methodKey = KeyCode.F;
    public KeyCode decreaseKey = KeyCode.Q;
    public KeyCode increaseKey = KeyCode.E;
    public KeyCode maskKey = KeyCode.R;
    public KeyCode gazeKey = KeyCode.T;

    [Header("URP Renderer")]
    public UniversalRendererData rendererData;  // Renderer asset

    private FoveatedVrsFeature foveatedFeature;

    void Awake()
    {
        if (rendererData == null)
        {
            Debug.LogError("[InputManager] RendererData is not assigned.");
            return;
        }

        // Find the feature by its name in the renderer's feature list
        foveatedFeature = rendererData.rendererFeatures
                .OfType<FoveatedVrsFeature>()
                .FirstOrDefault();
    }

    private void UpdateQuit()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif        
        }
    }

    private void UpdateMethod()
    {
        if (Input.GetKeyDown(methodKey))
        {
            foveatedFeature.ToggleMethod();
            GazeMarker.etfrEnabled = !GazeMarker.etfrEnabled;
        }
    }

    private void UpdateLevel()
    {
        if (Input.GetKeyDown(decreaseKey))
        {
            foveatedFeature.DecreaseLevel();
        }
        if (Input.GetKeyDown(increaseKey))
        {
            foveatedFeature.IncreaseLevel();
        }
    }

    private void UpdateMask()
    {
        if (Input.GetKeyDown(maskKey))
        {
            foveatedFeature.ToggleMask();
        }
    }
    private void UpdateGaze()
    {
        if (Input.GetKeyDown(gazeKey))
        {
            GazeMarker.gazeEnabled = !GazeMarker.gazeEnabled;
        }
    }

    void Update()
    {
        UpdateQuit();
        if (foveatedFeature == null)
        {
            Debug.LogError($"[InputManager] Feature FoveatedVrsFeature not found in rendererData.");
            return;
        }

        UpdateMethod();
        UpdateLevel();
        UpdateMask();
        UpdateGaze();
    }
}