using UnityEngine;
using TMPro;

public class GazeMarker : MonoBehaviour
{
    [Header("Gaze")]
    public Vector3 gazeSize = new Vector3(0.02f, 0.02f, 0.02f);
    public float gazeMaxDistance = 10.0f;
    public float lerpSpeed = 10.0f;
    public LayerMask gazeLayers = ~0;

    public static bool gazeEnabled = true;
    public static bool etfrEnabled = false;
    public static Vector2 currentGazeUV = new Vector2(0.5f, 0.5f);

    private Camera mainCamera;
    private GameObject gazeSphere;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        gazeSphere = GameObject.Find("Gaze");

        if (mainCamera == null)
            Debug.LogError("[GazeMarker] Main camera not found!");

        if (gazeSphere == null)
            Debug.LogError("[GazeMarker] Gaze sphere (GameObject 'Gaze') not found!");
    }


    void Update()
    {
        if (gazeSphere == null || mainCamera == null) return;
        if (!gazeEnabled)
        {
            gazeSphere.transform.localScale = Vector3.zero;
            return;
        }

        // Fallback when ETFR off  – dot/mask stay on fixed plane
        if (!etfrEnabled)
        {
            PlaceDefault();
            return;
        }

        Vector3 screenPos = new Vector3(
            currentGazeUV.x * Screen.width,
            currentGazeUV.y * Screen.height,
            0f
        );

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        Vector3 worldDir = ray.direction.normalized;
        Vector3 worldOrigin = ray.origin;

        // Ray-cast for accurate dot; if miss – still use plane 
        if (Physics.Raycast(worldOrigin, worldDir, out var hit, gazeMaxDistance, gazeLayers, QueryTriggerInteraction.Ignore)) // hit
        {
            PlaceDot(hit.point);
        }
        else // miss
        {
            Vector3 planePt = worldOrigin + worldDir * gazeMaxDistance;
            PlaceDot(planePt);
        }
    }

    void PlaceDefault()
    {
        Vector3 origin = mainCamera.transform.position;
        Vector3 dirWorld = mainCamera.transform.forward;

        // Ray-cast for accurate dot; if miss – still use plane 
        if (Physics.Raycast(origin, dirWorld, out var hit, gazeMaxDistance, gazeLayers, QueryTriggerInteraction.Ignore)) // hit
        {
            PlaceDot(hit.point);
        }
        else // miss
        {
            Vector3 planePt = origin + dirWorld * gazeMaxDistance;
            PlaceDot(planePt);
        }
    }

    void PlaceDot(Vector3 worldPos)
    {
        float distance = Vector3.Distance(mainCamera.transform.position, worldPos);
        Vector3 targetScale = gazeSize * distance;

        gazeSphere.transform.localScale = Vector3.Lerp(
            gazeSphere.transform.localScale,
            targetScale,
            Time.deltaTime * lerpSpeed
        );

        if (gazeSphere.transform.localScale.x < gazeSize.x) gazeSphere.transform.localScale = gazeSize;

        gazeSphere.transform.position = worldPos;
    }

}
