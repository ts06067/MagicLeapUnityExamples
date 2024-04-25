using MagicLeap.Android;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR.Features.MagicLeapSupport;
using Utils = MagicLeap.Examples.Utils;

public class PlaneExample : MonoBehaviour
{
    private static ARPlaneManager planeManager;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private GameObject controllerObject;
    [SerializeField] private GameObject createAnchorPopUp;
    private InputActionMap controllerMap;

    [SerializeField, Tooltip("Maximum number of planes to return each query")]
    private uint maxResults = 100;

    [SerializeField, Tooltip("Minimum plane area to treat as a valid plane")]
    private float minPlaneArea = 3f;

    [SerializeField]
    private Text status;

    [SerializeField]
    private TextMeshProUGUI text;

    private Camera mainCamera;
    private bool permissionGranted = false;

    public static ARPlane selectedPerpendicularARPlane;
    public static ARPlane selectedAdjacentARPlane;
    private ARPlane tmpSelectedARPlane;

    private IEnumerator Start()
    {
        mainCamera = Camera.main;
        yield return new WaitUntil(Utils.AreSubsystemsLoaded<XRPlaneSubsystem>);
        planeManager = FindObjectOfType<ARPlaneManager>();
        if (planeManager == null)
        {
            Debug.LogError("Failed to find ARPlaneManager in scene. Disabling Script");
            enabled = false;
        }
        else
        {
            // disable planeManager until we have successfully requested required permissions
            planeManager.enabled = false;
        }

        if (inputActions != null)
        {
            controllerMap = inputActions.FindActionMap("Controller");
            if (controllerMap == null)
            {
                Debug.LogError("Couldn't find Controller action map");
                enabled = false;
            }
            else
            {
                controllerMap.FindAction("Bumper").performed += OnBumper;
            }
        }

        permissionGranted = false;
        Permissions.RequestPermission(MLPermission.SpatialAnchors, OnPermissionGranted, OnPermissionDenied);
    }


    private static bool IsNearlyPerpendicular(Vector3 n1, Vector3 n2)
    {
        return Vector3.Dot(n1.normalized, n2.normalized) < 0.1f;
    }

    private static bool IsNearlyParallel(Vector3 n1, Vector3 n2)
    {
        return Vector3.Dot(n1.normalized, n2.normalized) > 0.5f;
    }

    public static ARPlane GetAdjacentPlane(ARPlane plane, string direction)
    {
        if (plane == null)
        {
            return null;
        }

        if (SpatialAnchorsExample.VDistance < 7)
        {
            return null;
        }

        ARPlane closestPlane = null;
        float closestDistance = float.MaxValue;

        foreach (ARPlane p in planeManager.trackables)
        {

            if (p.subsumedBy != null)
            {
                continue;
            }

            if (p.classification != PlaneClassification.Wall)
            {
                continue;
            }

            if (p.size.x * p.size.y < 5f)
            {
                continue;
            }

            (float hDist, float vDist) = SpatialAnchorsExample.GetPlanarDistanceFromOrigin(SpatialAnchorsExample.origin, SpatialAnchorsExample.axisPoint, p.center, SpatialAnchorsExample.hAngle);

            if (vDist < 7)
            {
                continue;
            }

            float distance = Vector3.Distance(plane.transform.position, p.transform.position);
            float signedAngle = Vector3.SignedAngle(plane.normal, p.normal, Vector3.up);

            if (!IsNearlyParallel(plane.normal, p.normal))
            {
                continue;
            }

            float absSignedAngle = Mathf.Abs(signedAngle);

            if (absSignedAngle % 180 > 20 || absSignedAngle % 180 < 160)
            {
                continue;
            }

            switch (direction)
            {
                case "right":
                    if (distance < closestDistance && signedAngle < 0)
                    {
                        closestDistance = distance;
                        closestPlane = p;
                    }
                    break;
                case "left":
                    if (distance < closestDistance && signedAngle > 0)
                    {
                        closestDistance = distance;
                        closestPlane = p;
                    }
                    break;
            }
        }

        return closestPlane;
    }

    public static ARPlane GetPerpendicularPlane(ARPlane plane, string direction)
    {
        if (plane == null)
        {
            return null;
        }

        if (SpatialAnchorsExample.VDistance < 7)
        {
            return null;
        }

        ARPlane closestPlane = null;
        float closestDistance = float.MaxValue;

        foreach (ARPlane p in planeManager.trackables)
        {

            if (p.subsumedBy != null)
            {
                continue;
            }

            if (p.classification != PlaneClassification.Wall)
            {
                continue;
            }

            if (p.size.x * p.size.y < 5f)
            {
                continue;
            }

            (float hDist, float vDist) = SpatialAnchorsExample.GetPlanarDistanceFromOrigin(SpatialAnchorsExample.origin, SpatialAnchorsExample.axisPoint, p.center, SpatialAnchorsExample.hAngle);

            if (vDist < 7)
            {
                continue;
            }

            float distance = Vector3.Distance(plane.transform.position, p.transform.position);
            float signedAngle = Vector3.SignedAngle(plane.normal, p.normal, Vector3.up);

            if (!IsNearlyPerpendicular(plane.normal, p.normal))
            {
                continue;
            }

            float absSignedAngle = Mathf.Abs(signedAngle);

            if (absSignedAngle % 180 < 70 || absSignedAngle % 180 > 110)
            {
                continue;
            }

            switch (direction)
            {
                case "right":
                    if (distance < closestDistance && signedAngle < 0)
                    {
                        closestDistance = distance;
                        closestPlane = p;
                    }
                    break;
                case "left":
                    if (distance < closestDistance && signedAngle > 0)
                    {
                        closestDistance = distance;
                        closestPlane = p;
                    }
                    break;
            }
        }

        return closestPlane;
    }
    
    private void Update()
    {
        UpdateQuery();

        text.text = $"Walls Detected: {planeManager.trackables.count}";

    }

    private void OnBumper(InputAction.CallbackContext _)
    {
        /*
        Pose currentPose = new Pose(controllerObject.transform.position, controllerObject.transform.rotation);

        GameObject newAnchor = Instantiate(anchorPrefab, currentPose.position, currentPose.rotation);

        ARAnchor newAnchorComponent = newAnchor.AddComponent<ARAnchor>();

        newAnchorComponent.GetComponent<MeshRenderer>().material.color = Color.grey;

        activeAnchors.Add(newAnchorComponent);
        localAnchors.Add(newAnchorComponent);
        */

        // if a raycast hits a plane, activate the pop-up
        if (Physics.Raycast(controllerObject.transform.position, controllerObject.transform.forward, out RaycastHit hit, 10f))
        {
            if (hit.collider.gameObject.CompareTag("Plane"))
            {
                if (hit.collider.gameObject.TryGetComponent(out tmpSelectedARPlane))
                {
                    createAnchorPopUp.SetActive(true);

                    // place the pop-up in front of the controller
                    createAnchorPopUp.transform.position = controllerObject.transform.position + controllerObject.transform.forward * 0.2f;
                    // createAnchorPopUp.transform.LookAt(mainCamera.transform);

                    PlanePrefabExample.trackableIds = new();
                }
            }
        }
    }

    public void SelectPerpendicularARPlane()
    {
        PlanePrefabExample.trackableIds = new();
        selectedPerpendicularARPlane = tmpSelectedARPlane;
    }

    public void SelectAdjacentARPlane()
    {
        PlanePrefabExample.trackableIds = new();
        selectedAdjacentARPlane = tmpSelectedARPlane;
    }

    private void UpdateQuery()
    {
        if (planeManager != null && planeManager.enabled && permissionGranted)
        {
            var newQuery = new MLXrPlaneSubsystem.PlanesQuery
            {
                Flags = planeManager.requestedDetectionMode.ToMLXrQueryFlags() | MLXrPlaneSubsystem.MLPlanesQueryFlags.SemanticWall,
                BoundsCenter = mainCamera.transform.position,
                BoundsRotation = mainCamera.transform.rotation,
                BoundsExtents = Vector3.one * 100f,
                MaxResults = maxResults,
                MinPlaneArea = minPlaneArea,
            };

            MLXrPlaneSubsystem.Query = newQuery;
            status.text = $"Detection Mode:\n<B>{planeManager.requestedDetectionMode}</B>\n\n" +
                          $"Query Flags:\n<B>{newQuery.Flags.ToString().Replace(" ", "\n")}</B>\n\n" +
                          $"Query MaxResultss:\n<B>{newQuery.MaxResults}</B>\n\n" +
                          $"Query MinPlaneArea:\n<B>{newQuery.MinPlaneArea}</B>\n\n" +
                          $"Plane GameObjects:\n<B>{PlanePrefabExample.Count}</B>";
        }
    }

    private void OnPermissionGranted(string permission)
    {
        planeManager.enabled = true;
        permissionGranted = true;
    }

    private void OnPermissionDenied(string permission)
    {
        Debug.LogError($"Failed to create Planes Subsystem due to missing or denied {MLPermission.SpatialMapping} permission. Please add to manifest. Disabling script.");
        enabled = false;
    }

    private void OnDestroy()
    {
        if (inputActions != null && controllerMap != null)
        {
            controllerMap.FindAction("Bumper").performed -= OnBumper;
        }
    }
}
