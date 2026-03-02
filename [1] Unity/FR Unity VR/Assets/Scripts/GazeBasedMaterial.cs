using UnityEngine;
using static OVRPlugin;

[RequireComponent(typeof(Collider), typeof(Renderer))]
public class GazeBasedMaterial : MonoBehaviour
{
    [Header("Scene refs")]
    [Tooltip("Centre-eye camera (defaults to Camera.main at runtime)")]
    public Camera centerEye;
    [Tooltip("Root of the HMD rig (OVRCameraRig) - used to convert gaze to world space")]
    public Transform hmdRoot;

    [Header("Materials")]
    public Material matA;   // first material 
    public Material matB;   // second material 

    [Header("Params")]
    [Tooltip("Max distance for the gaze ray")]
    public float maxRayDistance = 15f;

    public LayerMask rayLayers = ~0;

    Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (!centerEye) centerEye = Camera.main;
        if (!hmdRoot) hmdRoot = centerEye.transform.parent;

        // start with the first material
        if (matA && matB) rend.material = matA;
    }

    void Update()
    {
        Vector3 origin = centerEye.transform.position;
        Vector3 dir = GetGazeDirWorld();

        // ray-cast into the scene
        if (Physics.Raycast(origin, dir, out RaycastHit hit,
                            maxRayDistance, rayLayers, QueryTriggerInteraction.Ignore)
            && hit.collider.gameObject == gameObject)
        {
            SetMaterial(true);
        }
        else
        {
            SetMaterial(false);
        }
    }

    void SetMaterial(bool isFocused)
    {
        rend.material = isFocused ? matB : matA;
    }

    Vector3 GetGazeDirWorld()
    {
        // ETFR available - real combined gaze vector
        if (OVRManager.eyeTrackedFoveatedRenderingEnabled &&
            eyeTrackingSupported && eyeTrackingEnabled)
        {
            EyeGazesState st = new EyeGazesState();
            if (GetEyeGazesState(Step.Render, -1, ref st))
            {
                var l = st.EyeGazes[(int)Eye.Left];
                var r = st.EyeGazes[(int)Eye.Right];

                if (l.IsValid && r.IsValid)
                {
                    Quaternion qL = ToUnity(l.Pose.Orientation);
                    Quaternion qR = ToUnity(r.Pose.Orientation);
                    Quaternion avg = Quaternion.Slerp(qL, qR, 0.5f);
                    return hmdRoot.rotation * (avg * Vector3.forward);
                }
            }
        }

        // fallback - plain camera forward
        return centerEye.transform.forward;
    }

    static Quaternion ToUnity(Quatf q) => new Quaternion(-q.x, -q.y, q.z, q.w);
}
