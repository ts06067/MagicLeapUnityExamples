using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlanePrefabExample : MonoBehaviour
{
    private const int GRAY_PLANE_QUEUE = 3001;
    private const int DEFAULT_PLANE_QUEUE = 3000;

    [SerializeField] private GameObject spatialAnchorsManager;
    [SerializeField] private Canvas canvas;
    [SerializeField] private TextMeshProUGUI text;

    private float hDist;
    private float vDist;

    public static List<TrackableId> trackableIds = new();

    public static int Count { get; private set; } = 0;

    private void Start()
    {
        ColorClassify();
        Count++;

    }

    private void OnDestroy()
    {
        Count--;
    }

    private void ColorClassify()
    {
        // get its ARPlane component
        if (TryGetComponent<ARPlane>(out var plane))
        {
            var mat = GetComponent<MeshRenderer>().material;
            Color color;

            if (trackableIds.Contains(plane.trackableId))
            {
                // change color to green
                color = Color.green;
            }
            else
            {
                color = plane.classification switch
                {
                    PlaneClassification.Floor => Color.green,
                    PlaneClassification.Ceiling => Color.blue,
                    PlaneClassification.Wall => Color.red,
                    PlaneClassification.Table => Color.yellow,
                    PlaneClassification.Seat => Color.white,
                    PlaneClassification.Door => Color.cyan,
                    PlaneClassification.Window => Color.magenta,
                    _ => Color.gray
                };

                color.a = 0.3f;
            }

            mat.color = color;
            mat.renderQueue = color == Color.gray ? GRAY_PLANE_QUEUE : DEFAULT_PLANE_QUEUE;
        }
    }

    private void Update()
    {
        ColorClassify();

        ARPlane plane = GetComponent<ARPlane>();

        Vector3 planeCenter = plane.center;
        Vector3 planeNormal = plane.normal;
        Vector2 planeSize = plane.size;
        float planeArea = planeSize.x * planeSize.y;

        Vector3 origin = SpatialAnchorsExample.origin;
        Vector3 axisPoint = SpatialAnchorsExample.axisPoint;
        float hAngle = SpatialAnchorsExample.hAngle;

        (hDist, vDist) = SpatialAnchorsExample.GetPlanarDistanceFromOrigin(origin, axisPoint, planeCenter, hAngle);

        // set the text to display the plane's center, normal, and size
        text.text = $"ID: {plane.trackableId}\nhDist: {hDist}\nvDist: {vDist}\nArea: {planeArea}\nNormal: {plane.normal}";

        // rotate the canvas to face the camera
        canvas.transform.position = planeCenter + planeNormal * 0.5f;
        canvas.transform.LookAt(Camera.main.transform);
    }
}
