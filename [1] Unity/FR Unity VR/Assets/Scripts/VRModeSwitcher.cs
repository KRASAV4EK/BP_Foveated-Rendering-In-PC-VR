using UnityEngine;
using UnityEngine.XR.Management;

public class VRModeSwitcher : MonoBehaviour
{
    public GameObject vrRig;
    public GameObject desktopRig;
    public VRDebugText vrDebug;

    void Start()
    {
        SelectAppMode();
    }

    private void SelectAppMode()
    {
        if (XRGeneralSettings.Instance != null &&
            XRGeneralSettings.Instance.Manager != null &&
            XRGeneralSettings.Instance.Manager.isInitializationComplete)
        {
            vrRig.SetActive(true);
            desktopRig.SetActive(false);
            vrDebug.Log("VR Mode active.\n");

            CheckVRSupport();
        }
        else
        {
            vrRig.SetActive(false);
            desktopRig.SetActive(true);
            Debug.Log("Desktop Mode active.");

            CheckDesktopSupport();
        }

    }

    void CheckVRSupport() 
    {
        if (OVRManager.fixedFoveatedRenderingSupported)
        {
            vrDebug.LogContd("FFR supported.\n");
        }
        else
        {
            vrDebug.LogContd("FFR NOT supported.\n");
        }

        if (OVRManager.eyeTrackedFoveatedRenderingSupported)
        {
            vrDebug.LogContd("ETFR supported.\n");
        }
        else
        {
            vrDebug.LogContd("ETFR NOT supported.\n");
        }
    }

    void CheckDesktopSupport()
    {
        // TODO
    }
}
