using UnityEngine;
using UnityEngine.UI;
using static OVRPlugin;

public class FoveatedRenderingSwitcher : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Image-component on FoveaMask")]
    public Image foveaMaskImage;

    [Header("FFR Mask Sprites (Off, Low, Medium, High)")]
    public Sprite[] ffrMasks = new Sprite[4];

    [Header("ETFR Mask Sprites (Off, Low, Medium, High)")]
    public Sprite[] etfrMasks = new Sprite[4];

    public VRDebugText vrDebug;

    // FFR settings 
    private readonly FoveatedRenderingLevel[] levels =
    {
        FoveatedRenderingLevel.Off,
        FoveatedRenderingLevel.Low,
        FoveatedRenderingLevel.Medium,
        FoveatedRenderingLevel.High,
    };

    private int currentLevelIndex = 0;

    void Start()
    {
        ApplyFoveation();
        LogStartConfig();
    }

    void Update()
    {
        HandleModeToggle();        // Y / X
        HandleLevelToggle();       // A / B
    }

    void HandleModeToggle()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.Y) && eyeTrackedFoveatedRenderingSupported)
        {
            eyeTrackedFoveatedRenderingEnabled = true;
            vrDebug?.Log("ETFR enabled\n");
            RefreshMask();
        }
        else if (OVRInput.GetDown(OVRInput.RawButton.X) && eyeTrackedFoveatedRenderingSupported)
        {
            eyeTrackedFoveatedRenderingEnabled = false;
            vrDebug?.Log("ETFR disabled\n");
            RefreshMask();
        }
    }

    void HandleLevelToggle()
    {
        bool changed = false;

        if (OVRInput.GetDown(OVRInput.RawButton.B))
        {
            currentLevelIndex = Mathf.Min(currentLevelIndex + 1, levels.Length - 1);
            changed = true;
        }
        else if (OVRInput.GetDown(OVRInput.RawButton.A))
        {
            currentLevelIndex = Mathf.Max(currentLevelIndex - 1, 0);
            changed = true;
        }

        if (changed) ApplyFoveation();
    }

    void ApplyFoveation()
    {
        foveatedRenderingLevel = levels[currentLevelIndex];
        vrDebug?.Log($"FR level: {foveatedRenderingLevel}\n");
        RefreshMask();
    }

    void RefreshMask()
    {
        if (!foveaMaskImage) return;

        Sprite[] table = eyeTrackedFoveatedRenderingEnabled && etfrMasks.Length == 4
                         ? etfrMasks
                         : ffrMasks;

        if (table.Length != 4 || table[currentLevelIndex] == null)
        {
            Debug.LogWarning("Mask sprite not assigned!");
            return;
        }

        foveaMaskImage.sprite = table[currentLevelIndex];
    }

    void LogStartConfig()
    {
        string msg = $"\nStart config:\n" +
                     $"FFR supported: {fixedFoveatedRenderingSupported}\n" +
                     $"ETFR supported: {eyeTrackedFoveatedRenderingSupported}\n" +
                     $"ETFR enabled: {eyeTrackedFoveatedRenderingEnabled}\n" +
                     $"DFR enabled: {useDynamicFoveatedRendering}\n";
        vrDebug?.LogContd(msg);
    }
}
