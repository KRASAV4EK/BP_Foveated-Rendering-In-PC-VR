using UnityEngine;
using TMPro;
using UnityEngine.Android;

public class VRGazePoint : MonoBehaviour
{
    [Header("Scene refs")]
    public Camera mainCamera;
    public TextMeshProUGUI debugText;

    public RectTransform foveaMask;
    public RectTransform gazePoint;
    public Transform OVRTransform;                // root of the HMD rig

    [Header("Tuning")]
    public float smoothTime = 0.1f;
    public float maxRayDistance = 20f;
    public LayerMask gazeLayers = ~0;

#if UNITY_ANDROID && !UNITY_EDITOR
    const string EyePerm = "com.oculus.permission.EYE_TRACKING";
#endif

    const float fallbackDistance = 0.5f;          // fixed depth for the MASK
    Vector3 velDot, velMask;                      // separate smoothing
    bool maskDisplayed = true, gazeDisplayed = true;
    Vector3 gazeScale = Vector3.zero;

    void Start()
    {
        gazeScale = gazePoint.localScale;

        gazePoint.gameObject.SetActive(gazeDisplayed);
        foveaMask.gameObject.SetActive(maskDisplayed);

        if (!mainCamera) mainCamera = Camera.main;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(EyePerm))
            Permission.RequestUserPermission(EyePerm);
#endif
        OVRPlugin.StartEyeTracking();
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.RThumbstick))
        {
            maskDisplayed = !maskDisplayed;
            foveaMask.gameObject.SetActive(maskDisplayed);
        }
        if (OVRInput.GetDown(OVRInput.RawButton.LThumbstick))
        {
            gazeDisplayed = !gazeDisplayed;
            gazePoint.gameObject.SetActive(gazeDisplayed);
        }
        UpdateHUD();
    }

    void UpdateHUD()
    {
        // Fallback when ETFR off  – dot/mask stay on fixed plane
        if (!OVRManager.eyeTrackedFoveatedRenderingEnabled)
        {
            PlaceDefault();
            return;
        }

        if (!OVRPlugin.eyeTrackingSupported || !OVRPlugin.eyeTrackingEnabled) return;

        var st = new OVRPlugin.EyeGazesState();
        if (!OVRPlugin.GetEyeGazesState(OVRPlugin.Step.Render, -1, ref st)) return;

        var L = st.EyeGazes[(int)OVRPlugin.Eye.Left];
        var R = st.EyeGazes[(int)OVRPlugin.Eye.Right];
        if (!L.IsValid && !R.IsValid) return;

        Vector3 origin = mainCamera.transform.position;

        // Gaze direction (average of quats) in WORLD space
        Quaternion rot =
            (L.IsValid && R.IsValid)
            ? Quaternion.Slerp(ToUnity(L.Pose.Orientation),
                               ToUnity(R.Pose.Orientation), 0.5f)
            : (L.IsValid ? ToUnity(L.Pose.Orientation)
                         : ToUnity(R.Pose.Orientation));

        Vector3 dirWorld = (OVRTransform.rotation * (rot * Vector3.forward)).normalized;

        // Ray-cast for accurate dot; if miss – still use plane 
        if (Physics.Raycast(origin, dirWorld, out var hit, maxRayDistance, gazeLayers, QueryTriggerInteraction.Ignore)) // hit
        {
            PlaceDot(hit.point);
        }
        else // miss
        {
            Vector3 planePt = origin + dirWorld * maxRayDistance;
            PlaceDot(planePt);
        }

        // Mask always on same plane, centred under PRG ray 
        PlaceMask(rot);
    }

    void PlaceDefault()
    {
        Vector3 origin = mainCamera.transform.position;
        Vector3 dirWorld = mainCamera.transform.forward;

        // Ray-cast for accurate dot; if miss – still use plane 
        if (Physics.Raycast(origin, dirWorld, out var hit, maxRayDistance, gazeLayers, QueryTriggerInteraction.Ignore)) // hit
        {
            PlaceDot(hit.point);
        }
        else // miss
        {
            Vector3 planePt = origin + dirWorld * maxRayDistance;
            PlaceDot(planePt);
        }

        Vector3 maskPosition = mainCamera.transform.position + mainCamera.transform.forward * fallbackDistance;

        foveaMask.transform.position = Vector3.SmoothDamp(foveaMask.transform.position, maskPosition,
                                                          ref velMask, smoothTime);
    }

    //    DOT
    void PlaceDot(Vector3 worldPos)
    {
        Vector3 newScale = gazeScale * Vector3.Distance(mainCamera.transform.position, worldPos);

        gazePoint.localScale = Vector3.Slerp(gazePoint.localScale, newScale, smoothTime);

        gazePoint.position = Vector3.SmoothDamp(gazePoint.position, worldPos, ref velDot, smoothTime);
    }

    //    MASK 
    void PlaceMask(Quaternion rot)
    {
        Vector3 gazeDir = rot * Vector3.forward;
        // Set fovea mask position
        Vector3 targetWorld = mainCamera.transform.position + OVRTransform.rotation * gazeDir.normalized * fallbackDistance;
        foveaMask.position = Vector3.SmoothDamp(foveaMask.position, targetWorld, ref velMask, smoothTime);
    }

    // helpers
    static Quaternion ToUnity(OVRPlugin.Quatf q)
        => new Quaternion(-q.x, -q.y, q.z, q.w);

    static Vector3 ToVector(OVRPlugin.Vector3f v)
        => new Vector3(v.x, v.y, -v.z);          // right->left‐handed
}
