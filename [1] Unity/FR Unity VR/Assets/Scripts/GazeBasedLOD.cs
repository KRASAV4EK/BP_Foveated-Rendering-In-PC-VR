using UnityEngine;
using static OVRPlugin;

[RequireComponent(typeof(LODGroup))]
public class GazeBasedLOD : MonoBehaviour
{
    [Header("Deps")]
    public Transform hmdRoot;        // OVRCameraRig
    public Camera centerEye;         // main camera
    public LayerMask rayLayers = ~0;

    private float[][] FFR_LOD;
    private float[][] ETFR_LOD;

    [Header("FFR Angles")]
    [Range(1, 90)] private float LOD0_FFR1 = 25f;
    [Range(1, 90)] private float LOD1_FFR1 = 30f;
    [Range(1, 90)] private float LOD2_FFR1 = 35f;
    [Range(1, 90)] private float LOD3_FFR1 = 40f;

    [Range(1, 90)] private float LOD0_FFR2 = 20f;
    [Range(1, 90)] private float LOD1_FFR2 = 25f;
    [Range(1, 90)] private float LOD2_FFR2 = 30f;
    [Range(1, 90)] private float LOD3_FFR2 = 35f;

    [Range(1, 90)] private float LOD0_FFR3 = 15f;
    [Range(1, 90)] private float LOD1_FFR3 = 20f;
    [Range(1, 90)] private float LOD2_FFR3 = 25f;
    [Range(1, 90)] private float LOD3_FFR3 = 30f;

    [Header("ETFR Angles")]
    [Range(1, 90)] private float LOD0_ETFR1 = 15f;
    [Range(1, 90)] private float LOD1_ETFR1 = 20f;
    [Range(1, 90)] private float LOD2_ETFR1 = 25f;
    [Range(1, 90)] private float LOD3_ETFR1 = 30f;

    [Range(1, 90)] private float LOD0_ETFR2 = 10f;
    [Range(1, 90)] private float LOD1_ETFR2 = 15f;
    [Range(1, 90)] private float LOD2_ETFR2 = 20f;
    [Range(1, 90)] private float LOD3_ETFR2 = 25f;

    [Range(1, 90)] private float LOD0_ETFR3 = 5f;
    [Range(1, 90)] private float LOD1_ETFR3 = 10f;
    [Range(1, 90)] private float LOD2_ETFR3 = 15f;
    [Range(1, 90)] private float LOD3_ETFR3 = 20f;

    LODGroup lod;
    private readonly int lod0 = 0, lod1 = 1, lod2 = 2, lod3 = 3;

    void Awake()
    {
        FFR_LOD = new float[3][] {
            new [] { LOD0_FFR1, LOD1_FFR1, LOD2_FFR1, LOD3_FFR1 },
            new [] { LOD0_FFR2, LOD1_FFR2, LOD2_FFR2, LOD3_FFR2 },
            new [] { LOD0_FFR3, LOD1_FFR3, LOD2_FFR3, LOD3_FFR3 }
        };

        ETFR_LOD = new float[3][] {
            new [] { LOD0_ETFR1, LOD1_ETFR1, LOD2_ETFR1, LOD3_ETFR1 },
            new [] { LOD0_ETFR2, LOD1_ETFR2, LOD2_ETFR2, LOD3_ETFR2 },
            new [] { LOD0_ETFR3, LOD1_ETFR3, LOD2_ETFR3, LOD3_ETFR3 }
        };

        lod = GetComponent<LODGroup>();
        if (!centerEye) centerEye = Camera.main;
        if (!hmdRoot) hmdRoot = centerEye.transform.parent;
    }

    void LateUpdate()
    {
        Vector3 gazeDir = GetGazeDirWorld();
        Vector3 toObj = (transform.position - centerEye.transform.position).normalized;
        float angle = Mathf.Acos(Mathf.Clamp01(Vector3.Dot(gazeDir, toObj))) * Mathf.Rad2Deg;

        bool hitThis = IsDirectHit(gazeDir);

        bool etfr = OVRManager.eyeTrackedFoveatedRenderingEnabled;
        bool ffr = !etfr && OVRManager.foveatedRenderingLevel != OVRManager.FoveatedRenderingLevel.Off;

        int ffrLevelIdx = OVRPlugin.foveatedRenderingLevel switch
        {
            OVRPlugin.FoveatedRenderingLevel.Low => 0,
            OVRPlugin.FoveatedRenderingLevel.Medium => 1,
            OVRPlugin.FoveatedRenderingLevel.High => 2,
            _ => -1   // Off
        };

        if (ffrLevelIdx == -1)
        {
            lod.ForceLOD(-1);
            return;
        }

        if (etfr)
        {   // ETFR policy
            if (hitThis || angle < ETFR_LOD[ffrLevelIdx][0]) lod.ForceLOD(lod0);
            else if (angle > ETFR_LOD[ffrLevelIdx][3]) lod.ForceLOD(lod3);
            else if (angle > ETFR_LOD[ffrLevelIdx][2]) lod.ForceLOD(lod2);
            else if (angle > ETFR_LOD[ffrLevelIdx][1]) lod.ForceLOD(lod1);
        }
        else if (ffr)
        {   // FFR-only policy
            if (hitThis || angle < FFR_LOD[ffrLevelIdx][0]) lod.ForceLOD(lod0);
            else if (angle > FFR_LOD[ffrLevelIdx][3]) lod.ForceLOD(lod3);
            else if (angle > FFR_LOD[ffrLevelIdx][2]) lod.ForceLOD(lod2);
            else if (angle > FFR_LOD[ffrLevelIdx][1]) lod.ForceLOD(lod1);
        }
    }

    // ray-cast from eye; returns true if first hit is this renderer/collider
    bool IsDirectHit(Vector3 gazeDir)
    {
        if (Physics.Raycast(centerEye.transform.position, gazeDir,
                            out RaycastHit hit, 100f, rayLayers,
                            QueryTriggerInteraction.Ignore))
        {
            return hit.transform == transform ||
                   hit.transform.IsChildOf(transform);
        }
        return false;
    }

    Vector3 GetGazeDirWorld()
    {
        // if ETFR enabled then take real direction
        if (OVRManager.eyeTrackedFoveatedRenderingEnabled &&
            eyeTrackingSupported && eyeTrackingEnabled)
        {
            EyeGazesState st = new EyeGazesState();
            if (GetEyeGazesState(Step.Render, -1, ref st) &&
                st.EyeGazes[(int)Eye.Left].IsValid &&
                st.EyeGazes[(int)Eye.Right].IsValid)
            {
                var L = st.EyeGazes[(int)Eye.Left].Pose.Orientation;
                var R = st.EyeGazes[(int)Eye.Right].Pose.Orientation;
                Quaternion q = Quaternion.Slerp(ToUnity(L), ToUnity(R), 0.5f);
                return (hmdRoot.rotation * (q * Vector3.forward)).normalized;
            }
        }

        // else return camera forward
        return centerEye.transform.forward;
    }

    static Quaternion ToUnity(Quatf q) => new Quaternion(-q.x, -q.y, q.z, q.w);
}
